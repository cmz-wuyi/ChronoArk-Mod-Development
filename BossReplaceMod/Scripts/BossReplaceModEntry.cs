using System;
using ChronoArkMod.Plugin;
using HarmonyLib;
using UnityEngine;

namespace BossReplaceMod
{
    [PluginConfig("BossReplaceMod", "cmz", "1.0.0")]
    public class BossReplaceModPlugin : ChronoArkPlugin
    {
        public static Harmony harmonyInstance;
        private GameObject consoleObject;

        public override void Initialize()
        {
            try
            {
                Debug.Log("[BossReplaceMod] ========== Initialize 开始 ==========");

                // 初始化配置管理器
                BossReplaceManager.Initialize();
                Debug.Log("[BossReplaceMod] 配置管理器已初始化, IsLoaded=" + BossReplaceManager.IsLoaded);

                // 初始化Harmony
                harmonyInstance = new Harmony("com.bossreplacemod.chronoark");
                Debug.Log("[BossReplaceMod] Harmony 实例已创建");

                // 应用所有补丁
                harmonyInstance.PatchAll();
                Debug.Log("[BossReplaceMod] PatchAll 完成");

                // 手动应用补丁（确保补丁正确应用）
                ApplyManualPatches();

                // 创建 F2 控制台 GameObject
                consoleObject = new GameObject("BossReplaceConsole");
                consoleObject.AddComponent<BossReplaceConsoleBehaviour>();
                UnityEngine.Object.DontDestroyOnLoad(consoleObject);
                Debug.Log("[BossReplaceMod] F2 控制台已创建");
                Debug.Log("[BossReplaceMod] 快捷键: F2=Boss替换控制台");

                Debug.Log("[BossReplaceMod] ========== Initialize 完成 ==========");
            }
            catch (Exception ex)
            {
                Debug.LogError("[BossReplaceMod] Initialize 异常: " + ex);
            }
        }

        public override void Dispose()
        {
            try
            {
                Debug.Log("[BossReplaceMod] Dispose called");
                if (consoleObject != null)
                {
                    UnityEngine.Object.Destroy(consoleObject);
                    consoleObject = null;
                    Debug.Log("[BossReplaceMod] Console GameObject destroyed");
                }
                if (harmonyInstance != null)
                {
                    harmonyInstance.UnpatchAll("com.bossreplacemod.chronoark");
                    Debug.Log("[BossReplaceMod] All patches removed");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[BossReplaceMod] Dispose 异常: " + ex);
            }
        }

        public override void OnModLoaded()
        {
            Debug.Log("[BossReplaceMod] OnModLoaded 调用");
        }

        public override void OnModSettingUpdate()
        {
            Debug.Log("[BossReplaceMod] OnModSettingUpdate 调用");
        }

        private void ApplyManualPatches()
        {
            Debug.Log("[BossReplaceMod] 开始应用手动补丁...");

            // 补丁1: StageSystem.BossEnterFunc Prefix
            var ssType = typeof(StageSystem);
            var bossEnterFuncMethod = ssType.GetMethod("BossEnterFunc",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var prefixMethod = typeof(BossReplacePatch).GetMethod("BossEnterFunc_Prefix",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (bossEnterFuncMethod != null && prefixMethod != null)
            {
                harmonyInstance.Patch(bossEnterFuncMethod, new HarmonyMethod(prefixMethod), null);
                Debug.Log("[BossReplaceMod] 手动补丁 BossEnterFunc: 已应用");
            }
            else
            {
                Debug.LogWarning("[BossReplaceMod] 警告: 找不到 BossEnterFunc 方法或补丁, bossMethod=" + (bossEnterFuncMethod != null) + ", prefix=" + (prefixMethod != null));
            }

            // 补丁2: RE_TheInquisition.UseButton1 Postfix
            var reType = typeof(RE_TheInquisition);
            var useButton1Method = reType.GetMethod("UseButton1",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var postfixMethod = typeof(BossReplacePatch).GetMethod("UseButton1_Postfix",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (useButton1Method != null && postfixMethod != null)
            {
                harmonyInstance.Patch(useButton1Method, null, new HarmonyMethod(postfixMethod));
                Debug.Log("[BossReplaceMod] 手动补丁 UseButton1: 已应用");
            }
            else
            {
                Debug.LogWarning("[BossReplaceMod] 警告: 找不到 UseButton1 方法或补丁, useMethod=" + (useButton1Method != null) + ", postfix=" + (postfixMethod != null));
            }

            Debug.Log("[BossReplaceMod] 手动补丁完成");
        }
    }
}
