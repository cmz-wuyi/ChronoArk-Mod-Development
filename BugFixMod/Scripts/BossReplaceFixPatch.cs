using System;
using System.Collections.Generic;
using System.Reflection;
using GameDataEditor;
using HarmonyLib;
using UnityEngine;

namespace BossFixMod
{
    /// <summary>
    /// Boss替换修复补丁
    /// 根本原因: StoryData.JohanQuestClear 从未被设置为true
    /// 导致BossEnter永远走剧情分支，而不是Boss替换分支
    /// </summary>
    public class BossReplaceFixPatch
    {
        /// <summary>
        /// 补丁1: RE_TheInquisition.UseButton1 Postfix
        /// 在事件触发后输出调试信息并确保关键标志被设置
        /// </summary>
        [HarmonyPatch(typeof(RE_TheInquisition), "UseButton1")]
        [HarmonyPostfix]
        public static void UseButton1_Postfix()
        {
            try
            {
                Debug.Log("[BossFixMod] ========== UseButton1 Postfix ==========");

                // 获取并输出当前状态
                var tsd = PlayData.TSavedata;
                var storydata = SaveManager.NowData.storydata;

                Debug.Log("[BossFixMod] === Event Triggered State ===");
                Debug.Log("[BossFixMod] TempSaveData.TheInquisition: " + tsd.TheInquisition);
                Debug.Log("[BossFixMod] TempSaveData.IsJohanQuest: " + tsd.IsJohanQuest);
                Debug.Log("[BossFixMod] TempSaveData.BossClear: " + tsd.BossClear);
                Debug.Log("[BossFixMod] StoryData.JohanQuestProgress: " + storydata.JohanQuestProgress);
                Debug.Log("[BossFixMod] StoryData.JohanQuestClear: " + storydata.JohanQuestClear);

                // 关键修复: 如果TheInquisition已被设置为true，但JohanQuestClear仍为false
                // 则强制设置JohanQuestClear为true
                if (tsd.TheInquisition && !storydata.JohanQuestClear)
                {
                    Debug.Log("[BossFixMod] *** FIX APPLYING: Setting JohanQuestClear = true ***");
                    storydata.JohanQuestClear = true;
                    Debug.Log("[BossFixMod] *** JohanQuestClear is now: " + storydata.JohanQuestClear + " ***");

                    // 输出NowBossQueueKeys状态
                    if (tsd.NowBossQueueKeys != null)
                    {
                        Debug.Log("[BossFixMod] NowBossQueueKeys count: " + tsd.NowBossQueueKeys.Count);
                        for (int i = 0; i < tsd.NowBossQueueKeys.Count; i++)
                        {
                            Debug.Log("[BossFixMod]   Queue[" + i + "]: " + tsd.NowBossQueueKeys[i]);
                        }
                    }
                    else
                    {
                        Debug.Log("[BossFixMod] NowBossQueueKeys is null");
                    }
                }

                Debug.Log("[BossFixMod] ========== UseButton1 Postfix Complete ==========");
            }
            catch (Exception ex)
            {
                Debug.Log("[BossFixMod] ERROR in UseButton1_Postfix: " + ex.Message);
                Debug.Log("[BossFixMod] Stack: " + ex.StackTrace);
            }
        }

        /// <summary>
        /// 补丁2: StageSystem.BossEnterFunc Prefix
        /// 在Boss进入前检查并修复条件
        /// </summary>
        [HarmonyPatch(typeof(StageSystem), "BossEnterFunc")]
        [HarmonyPrefix]
        public static void BossEnterFunc_Prefix()
        {
            try
            {
                Debug.Log("[BossFixMod] ========== BossEnterFunc Prefix ==========");

                var tsd = PlayData.TSavedata;
                var storydata = SaveManager.NowData.storydata;

                Debug.Log("[BossFixMod] === BossEnter State ===");
                Debug.Log("[BossFixMod] TempSaveData.TheInquisition: " + tsd.TheInquisition);
                Debug.Log("[BossFixMod] StoryData.JohanQuestClear: " + storydata.JohanQuestClear);

                // 关键修复: 如果TheInquisition为true但JohanQuestClear为false
                // 强制设置JohanQuestClear为true，确保Boss替换分支被执行
                if (tsd.TheInquisition && !storydata.JohanQuestClear)
                {
                    Debug.Log("[BossFixMod] *** FIX: Setting JohanQuestClear = true before BossEnter ***");
                    storydata.JohanQuestClear = true;
                    Debug.Log("[BossFixMod] *** Boss replacement should now trigger! ***");
                }

                // 输出EnemyQueue状态
                var ssType = typeof(StageSystem);
                var enemyQueueField = ssType.GetField("EnemyQueue",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (enemyQueueField != null && StageSystem.instance != null)
                {
                    var enemyQueue = enemyQueueField.GetValue(StageSystem.instance) as List<GDEEnemyQueueData>;
                    if (enemyQueue != null)
                    {
                        Debug.Log("[BossFixMod] StageSystem.EnemyQueue count: " + enemyQueue.Count);
                        for (int i = 0; i < enemyQueue.Count; i++)
                        {
                            var queue = enemyQueue[i];
                            Debug.Log("[BossFixMod]   Queue[" + i + "] Key: " + queue.Key);
                        }
                    }
                }

                Debug.Log("[BossFixMod] ========== BossEnterFunc Prefix Complete ==========");
            }
            catch (Exception ex)
            {
                Debug.Log("[BossFixMod] ERROR in BossEnterFunc_Prefix: " + ex.Message);
                Debug.Log("[BossFixMod] Stack: " + ex.StackTrace);
            }
        }
    }
}
