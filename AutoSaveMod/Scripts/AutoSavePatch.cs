using HarmonyLib;
using UnityEngine;

namespace AutoSaveMod
{
    /// <summary>
    /// 自动存档 Harmony 补丁
    /// 补丁目标：
    /// 1. FieldSystem.BattleEnd (Postfix) - 战斗结束后自动存档
    /// 2. FieldSystem.StageStart (Postfix) - 进入关卡后延迟自动存档（防篝火崩溃）
    /// 3. SaveManager.OnApplicationQuit (Prefix) - 退出游戏前自动存档
    /// </summary>
    public static class AutoSavePatch
    {
        // ============ 战斗结束补丁 ============

        /// <summary>
        /// 战斗结束后自动存档
        /// 注意：BattleEnd 返回 IEnumerator（协程），Postfix 在协程完成后执行
        /// </summary>
        [HarmonyPatch(typeof(FieldSystem), "BattleEnd")]
        [HarmonyPostfix]
        public static void BattleEnd_Postfix(bool NoSaveAfterEnd, bool isDefeat)
        {
            // 战败不存档（避免保存死亡状态）
            if (isDefeat)
            {
                Debug.Log("[AutoSaveMod] 战斗失败，跳过自动存档");
                return;
            }

            // 如果游戏标记为不保存（NoSaveAfterEnd），跳过
            if (NoSaveAfterEnd)
            {
                Debug.Log("[AutoSaveMod] NoSaveAfterEnd=true，跳过自动存档");
                return;
            }

            if (AutoSaveManager.Config != null && AutoSaveManager.Config.AutoSaveOnBattleEnd)
            {
                Debug.Log("[AutoSaveMod] 补丁触发：BattleEnd");
                AutoSaveManager.PerformAutoSave("BattleEnd");
            }
        }

        // ============ 进入关卡补丁 ============

        /// <summary>
        /// 进入关卡后延迟自动存档（防篝火崩溃）
        /// 不直接存档，而是通知 Behaviour 启动延迟协程
        /// </summary>
        [HarmonyPatch(typeof(FieldSystem), "StageStart")]
        [HarmonyPostfix]
        public static void StageStart_Postfix(string StageKey)
        {
            if (AutoSaveManager.Config != null && AutoSaveManager.Config.AutoSaveOnStageStart)
            {
                Debug.Log("[AutoSaveMod] 补丁触发：StageStart(" + StageKey + ")，启动延迟存档");
                // 通知 Behaviour 启动延迟存档协程
                if (AutoSaveBehaviour.Instance != null)
                {
                    AutoSaveBehaviour.Instance.StartDelayedSave(StageKey);
                }
                else
                {
                    Debug.LogWarning("[AutoSaveMod] AutoSaveBehaviour.Instance 为 null，无法启动延迟存档");
                }
            }
        }

        // ============ 退出游戏补丁 ============

        /// <summary>
        /// 退出游戏前自动存档（Prefix，在原方法执行前保存）
        /// </summary>
        [HarmonyPatch(typeof(SaveManager), "OnApplicationQuit")]
        [HarmonyPrefix]
        public static void OnApplicationQuit_Prefix()
        {
            if (AutoSaveManager.Config != null && AutoSaveManager.Config.AutoSaveOnQuit)
            {
                Debug.Log("[AutoSaveMod] 补丁触发：OnApplicationQuit（退出前存档）");
                AutoSaveManager.PerformAutoSave("Quit");
            }
        }
    }
}
