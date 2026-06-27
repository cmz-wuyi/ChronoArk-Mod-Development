using System;
using ChronoArkMod.Plugin;
using UnityEngine;

namespace DebugConsoleMod
{
    [PluginConfig("DebugConsoleMod", "cmz", "1.0.0")]
    public class DebugConsoleModPlugin : ChronoArkPlugin
    {
        private GameObject consoleObject;

        public override void Initialize()
        {
            try
            {
                Debug.Log("[DebugConsoleMod] ========== Initialize 开始 ==========");

                // 创建控制台GameObject
                consoleObject = new GameObject("DebugConsole");
                consoleObject.AddComponent<DebugConsoleBehaviour>();
                UnityEngine.Object.DontDestroyOnLoad(consoleObject);

                Debug.Log("[DebugConsoleMod] Console GameObject created: " + (consoleObject != null));
                Debug.Log("[DebugConsoleMod] Behaviour added: " + (consoleObject.GetComponent<DebugConsoleBehaviour>() != null));
                Debug.Log("[DebugConsoleMod] 快捷键: F1=审判官控制台");
                Debug.Log("[DebugConsoleMod] ========== Initialize 完成 ==========");
            }
            catch (Exception ex)
            {
                Debug.LogError("[DebugConsoleMod] Initialize 异常: " + ex);
            }
        }

        public override void Dispose()
        {
            try
            {
                Debug.Log("[DebugConsoleMod] Dispose called");
                if (consoleObject != null)
                {
                    UnityEngine.Object.Destroy(consoleObject);
                    consoleObject = null;
                    Debug.Log("[DebugConsoleMod] Console GameObject destroyed");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[DebugConsoleMod] Dispose 异常: " + ex);
            }
        }

        public override void OnModLoaded()
        {
            Debug.Log("[DebugConsoleMod] OnModLoaded 调用");
        }

        public override void OnModSettingUpdate()
        {
            Debug.Log("[DebugConsoleMod] OnModSettingUpdate 调用");
        }
    }
}
