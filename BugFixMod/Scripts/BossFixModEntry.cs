using System;
using ChronoArkMod.Plugin;
using HarmonyLib;
using UnityEngine;

namespace BossFixMod
{
    // ChronoArkPlugin插件入口
    [PluginConfig("BossFixMod", "cmz", "1.0.0")]
    public class BossFixModPlugin : ChronoArkPlugin
    {
        public static Harmony harmonyInstance;

        public override void Initialize()
        {
            try
            {
                Debug.Log("[BugFixMod] ========== Initialize 开始 ==========");

                // 初始化Harmony
                harmonyInstance = new Harmony("com.bossfixmod.chronoark");
                Debug.Log("[BugFixMod] Harmony 实例已创建");

                // 应用所有补丁
                harmonyInstance.PatchAll();
                Debug.Log("[BugFixMod] PatchAll 完成");

                // 手动应用补丁（以防PatchAll失败）
                ApplyManualPatches();

                Debug.Log("[BugFixMod] ========== Initialize 完成 ==========");
            }
            catch (Exception ex)
            {
                Debug.LogError("[BugFixMod] Initialize 异常: " + ex);
            }
        }

        public override void Dispose()
        {
            try
            {
                Debug.Log("[BugFixMod] Dispose called");
                if (harmonyInstance != null)
                {
                    harmonyInstance.UnpatchAll("com.bossfixmod.chronoark");
                    Debug.Log("[BugFixMod] All patches removed");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[BugFixMod] Dispose 异常: " + ex);
            }
        }

        public override void OnModLoaded()
        {
            Debug.Log("[BugFixMod] OnModLoaded 调用");
        }

        public override void OnModSettingUpdate()
        {
            Debug.Log("[BugFixMod] OnModSettingUpdate 调用");
        }

        private void ApplyManualPatches()
        {
            Debug.Log("[BugFixMod] 开始应用手动补丁...");

            // 补丁1: RE_TheInquisition.UseButton1 Postfix
            var reType = typeof(RE_TheInquisition);
            var useButton1Method = reType.GetMethod("UseButton1",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var postfixMethod = typeof(BossReplaceFixPatch).GetMethod("UseButton1_Postfix",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (useButton1Method != null && postfixMethod != null)
            {
                harmonyInstance.Patch(useButton1Method, null, new HarmonyMethod(postfixMethod));
                Debug.Log("[BugFixMod] 手动补丁 UseButton1: 已应用");
            }
            else
            {
                Debug.LogWarning("[BugFixMod] 警告: 找不到 UseButton1 方法或补丁, useMethod=" + (useButton1Method != null) + ", postfix=" + (postfixMethod != null));
            }

            // 补丁2: StageSystem.BossEnterFunc Prefix
            var ssType = typeof(StageSystem);
            var bossEnterFuncMethod = ssType.GetMethod("BossEnterFunc",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var prefixMethod = typeof(BossReplaceFixPatch).GetMethod("BossEnterFunc_Prefix",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (bossEnterFuncMethod != null && prefixMethod != null)
            {
                harmonyInstance.Patch(bossEnterFuncMethod, new HarmonyMethod(prefixMethod), null);
                Debug.Log("[BugFixMod] 手动补丁 BossEnterFunc: 已应用");
            }
            else
            {
                Debug.LogWarning("[BugFixMod] 警告: 找不到 BossEnterFunc 方法或补丁, bossMethod=" + (bossEnterFuncMethod != null) + ", prefix=" + (prefixMethod != null));
            }

            Debug.Log("[BugFixMod] 手动补丁完成");
        }
    }
}
