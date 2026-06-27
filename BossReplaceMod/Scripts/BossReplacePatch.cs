using System;
using System.Collections.Generic;
using System.Reflection;
using GameDataEditor;
using HarmonyLib;
using UnityEngine;

namespace BossReplaceMod
{
    /// <summary>
    /// Boss替换核心补丁
    /// 在BossEnter阶段拦截并替换Boss队列
    /// </summary>
    public class BossReplacePatch
    {
        /// <summary>
        /// 补丁1: StageSystem.BossEnterFunc Prefix
        /// 在Boss进入前检查并替换Boss队列
        /// </summary>
        [HarmonyPatch(typeof(StageSystem), "BossEnterFunc")]
        [HarmonyPrefix]
        public static void BossEnterFunc_Prefix()
        {
            try
            {
                if (!BossReplaceManager.IsLoaded)
                {
                    Debug.Log("[BossReplaceMod] Config not loaded, skipping");
                    return;
                }

                Debug.Log("[BossReplaceMod] ========== BossEnterFunc Prefix ==========");

                var tsd = PlayData.TSavedata;
                var ssType = typeof(StageSystem);

                // 获取当前关卡Key
                string stageKey = "";
                var stageDataField = ssType.GetField("StageData",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (stageDataField != null && StageSystem.instance != null)
                {
                    var stageData = stageDataField.GetValue(StageSystem.instance) as GDEStageData;
                    if (stageData != null)
                    {
                        stageKey = stageData.Key;
                        Debug.Log("[BossReplaceMod] Current stage: " + stageKey);
                    }
                }

                // 获取当前Boss队列列表
                var enemyQueueField = ssType.GetField("EnemyQueue",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (enemyQueueField != null && StageSystem.instance != null)
                {
                    var enemyQueue = enemyQueueField.GetValue(StageSystem.instance) as List<GDEEnemyQueueData>;
                    if (enemyQueue != null && enemyQueue.Count > 0)
                    {
                        Debug.Log("[BossReplaceMod] EnemyQueue count: " + enemyQueue.Count);

                        // 检查每个Boss队列是否需要替换
                        for (int i = 0; i < enemyQueue.Count; i++)
                        {
                            var queue = enemyQueue[i];
                            string originalKey = queue.Key;

                            string newKey = BossReplaceManager.CheckReplace(stageKey, originalKey);
                            if (!string.IsNullOrEmpty(newKey))
                            {
                                Debug.Log("[BossReplaceMod] *** REPLACING Boss[" + i + "] ***");
                                Debug.Log("[BossReplaceMod]   From: " + originalKey);
                                Debug.Log("[BossReplaceMod]   To: " + newKey);

                                // 创建新的Boss队列数据
                                try
                                {
                                    var newQueue = new GDEEnemyQueueData(newKey);
                                    enemyQueue[i] = newQueue;
                                    Debug.Log("[BossReplaceMod] *** Boss replaced successfully! ***");
                                }
                                catch (Exception ex)
                                {
                                    Debug.Log("[BossReplaceMod] ERROR replacing boss: " + ex.Message);
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("[BossReplaceMod] EnemyQueue is null or empty");
                    }
                }

                Debug.Log("[BossReplaceMod] ========== BossEnterFunc Prefix Complete ==========");
            }
            catch (Exception ex)
            {
                Debug.Log("[BossReplaceMod] ERROR in BossEnterFunc_Prefix: " + ex.Message);
                Debug.Log("[BossReplaceMod] Stack: " + ex.StackTrace);
            }
        }

        /// <summary>
        /// 补丁2: RE_TheInquisition.UseButton1 Postfix
        /// 监听异端审判所事件触发
        /// </summary>
        [HarmonyPatch(typeof(RE_TheInquisition), "UseButton1")]
        [HarmonyPostfix]
        public static void UseButton1_Postfix()
        {
            try
            {
                Debug.Log("[BossReplaceMod] ========== RE_TheInquisition.UseButton1 Postfix ==========");

                string newBoss = BossReplaceManager.CheckEventTriggeredReplace("RE_TheInquisition");
                if (!string.IsNullOrEmpty(newBoss))
                {
                    Debug.Log("[BossReplaceMod] Event triggered, will replace with: " + newBoss);

                    // 确保TheInquisition标志被设置
                    var tsd = PlayData.TSavedata;
                    if (!tsd.TheInquisition)
                    {
                        Debug.Log("[BossReplaceMod] Setting TheInquisition = true");
                        tsd.TheInquisition = true;
                    }

                    // 确保JohanQuestClear为true（修复游戏bug）
                    var storydata = SaveManager.NowData.storydata;
                    if (!storydata.JohanQuestClear)
                    {
                        Debug.Log("[BossReplaceMod] Fixing: Setting JohanQuestClear = true");
                        storydata.JohanQuestClear = true;
                    }
                }

                Debug.Log("[BossReplaceMod] ========== UseButton1 Postfix Complete ==========");
            }
            catch (Exception ex)
            {
                Debug.Log("[BossReplaceMod] ERROR in UseButton1_Postfix: " + ex.Message);
            }
        }
    }
}
