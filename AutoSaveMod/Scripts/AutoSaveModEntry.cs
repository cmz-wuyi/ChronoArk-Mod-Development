using System;
using System.Reflection;
using ChronoArkMod.Plugin;
using HarmonyLib;
using UnityEngine;

namespace AutoSaveMod
{
    [PluginConfig("AutoSaveMod", "cmz", "1.0.0")]
    public class AutoSaveModPlugin : ChronoArkPlugin
    {
        public static Harmony harmonyInstance;
        private GameObject consoleObject;

        public override void Initialize()
        {
            try
            {
                Debug.Log("[AutoSaveMod] ========== Initialize 开始 ==========");

                // 1. 初始化配置管理器
                AutoSaveManager.Initialize();
                Debug.Log("[AutoSaveMod] 配置管理器已初始化, IsLoaded=" + AutoSaveManager.IsLoaded);

                // 2. 初始化 Harmony
                harmonyInstance = new Harmony("com.autosavemod.chronoark");
                Debug.Log("[AutoSaveMod] Harmony 实例已创建");

                // 3. 应用所有补丁
                harmonyInstance.PatchAll();
                Debug.Log("[AutoSaveMod] PatchAll 完成");

                // 4. 手动应用补丁（双保险）
                ApplyManualPatches();

                // 5. 创建 F4 控制台 GameObject
                consoleObject = new GameObject("AutoSaveConsole");
                consoleObject.AddComponent<AutoSaveBehaviour>();
                UnityEngine.Object.DontDestroyOnLoad(consoleObject);
                Debug.Log("[AutoSaveMod] F4 控制台已创建");
                Debug.Log("[AutoSaveMod] 快捷键: F4=自动存档控制台");

                Debug.Log("[AutoSaveMod] ========== Initialize 完成 ==========");
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] Initialize 异常: " + ex);
            }
        }

        public override void Dispose()
        {
            try
            {
                Debug.Log("[AutoSaveMod] ========== Dispose 开始 ==========");

                if (consoleObject != null)
                {
                    UnityEngine.Object.Destroy(consoleObject);
                    consoleObject = null;
                    Debug.Log("[AutoSaveMod] Console GameObject destroyed");
                }

                if (harmonyInstance != null)
                {
                    harmonyInstance.UnpatchAll("com.autosavemod.chronoark");
                    Debug.Log("[AutoSaveMod] All patches removed");
                }

                Debug.Log("[AutoSaveMod] ========== Dispose 完成 ==========");
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] Dispose 异常: " + ex);
            }
        }

        public override void OnModLoaded()
        {
            Debug.Log("[AutoSaveMod] OnModLoaded 调用");
        }

        public override void OnModSettingUpdate()
        {
            Debug.Log("[AutoSaveMod] OnModSettingUpdate 调用");
        }

        /// <summary>手动应用补丁（确保 PatchAll 失败时补丁仍能生效）</summary>
        private void ApplyManualPatches()
        {
            Debug.Log("[AutoSaveMod] 开始应用手动补丁...");

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            // === 补丁1: FieldSystem.BattleEnd (Postfix) ===
            var fsType = typeof(FieldSystem);
            var battleEndMethod = fsType.GetMethod("BattleEnd", flags);
            var battleEndPostfix = typeof(AutoSavePatch).GetMethod("BattleEnd_Postfix", flags);

            if (battleEndMethod != null && battleEndPostfix != null)
            {
                harmonyInstance.Patch(battleEndMethod, null, new HarmonyMethod(battleEndPostfix));
                Debug.Log("[AutoSaveMod] 手动补丁 BattleEnd: 已应用 (Postfix)");
            }
            else
            {
                Debug.LogWarning("[AutoSaveMod] 警告: 找不到 BattleEnd, method=" + (battleEndMethod != null) + ", postfix=" + (battleEndPostfix != null));
            }

            // === 补丁2: FieldSystem.StageStart (Postfix) ===
            var stageStartMethod = fsType.GetMethod("StageStart", flags);
            var stageStartPostfix = typeof(AutoSavePatch).GetMethod("StageStart_Postfix", flags);

            if (stageStartMethod != null && stageStartPostfix != null)
            {
                harmonyInstance.Patch(stageStartMethod, null, new HarmonyMethod(stageStartPostfix));
                Debug.Log("[AutoSaveMod] 手动补丁 StageStart: 已应用 (Postfix)");
            }
            else
            {
                Debug.LogWarning("[AutoSaveMod] 警告: 找不到 StageStart, method=" + (stageStartMethod != null) + ", postfix=" + (stageStartPostfix != null));
            }

            // === 补丁3: SaveManager.OnApplicationQuit (Prefix) ===
            var smType = typeof(SaveManager);
            var quitMethod = smType.GetMethod("OnApplicationQuit", flags);
            var quitPrefix = typeof(AutoSavePatch).GetMethod("OnApplicationQuit_Prefix", flags);

            if (quitMethod != null && quitPrefix != null)
            {
                harmonyInstance.Patch(quitMethod, new HarmonyMethod(quitPrefix), null);
                Debug.Log("[AutoSaveMod] 手动补丁 OnApplicationQuit: 已应用 (Prefix)");
            }
            else
            {
                Debug.LogWarning("[AutoSaveMod] 警告: 找不到 OnApplicationQuit, method=" + (quitMethod != null) + ", prefix=" + (quitPrefix != null));
            }

            Debug.Log("[AutoSaveMod] 手动补丁完成");
        }
    }
}
