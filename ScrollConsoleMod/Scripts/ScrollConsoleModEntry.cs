using System;
using ChronoArkMod.Plugin;
using UnityEngine;

namespace ScrollConsoleMod
{
    /// <summary>
    /// ScrollConsoleMod 插件入口
    /// 独立 mod，只负责卷轴获取控制台（F3 开关）
    /// 不依赖其他任何 mod
    /// </summary>
    [PluginConfig("ScrollConsoleMod", "cmz", "1.0.0")]
    public class ScrollConsoleModPlugin : ChronoArkPlugin
    {
        private GameObject consoleObject;

        public override void Initialize()
        {
            try
            {
                Debug.Log("[ScrollConsoleMod] ========== Initialize 开始 ==========");

                // 创建控制台 GameObject
                consoleObject = new GameObject("ScrollConsole");
                consoleObject.AddComponent<ScrollConsoleBehaviour>();
                UnityEngine.Object.DontDestroyOnLoad(consoleObject);

                Debug.Log("[ScrollConsoleMod] Console GameObject created: " + (consoleObject != null));
                Debug.Log("[ScrollConsoleMod] Behaviour added: " + (consoleObject.GetComponent<ScrollConsoleBehaviour>() != null));
                Debug.Log("[ScrollConsoleMod] 快捷键: F3=卷轴获取控制台");
                Debug.Log("[ScrollConsoleMod] ========== Initialize 完成 ==========");
            }
            catch (Exception ex)
            {
                Debug.LogError("[ScrollConsoleMod] Initialize 异常: " + ex);
            }
        }

        public override void Dispose()
        {
            try
            {
                Debug.Log("[ScrollConsoleMod] Dispose called");
                if (consoleObject != null)
                {
                    UnityEngine.Object.Destroy(consoleObject);
                    consoleObject = null;
                    Debug.Log("[ScrollConsoleMod] Console GameObject destroyed");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[ScrollConsoleMod] Dispose 异常: " + ex);
            }
        }

        public override void OnModLoaded()
        {
            Debug.Log("[ScrollConsoleMod] OnModLoaded 调用");
        }

        public override void OnModSettingUpdate()
        {
            Debug.Log("[ScrollConsoleMod] OnModSettingUpdate 调用");
        }
    }
}
