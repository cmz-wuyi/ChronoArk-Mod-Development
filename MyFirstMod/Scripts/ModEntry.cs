using System;
using ChronoArkMod.Plugin;
using UnityEngine;

namespace MyFirstMod
{
    /// <summary>
    /// 最小骨架模板 - 供新 mod 开发参考
    /// 只有 Plugin 基础生命周期方法，无任何功能
    /// 复制此项目并修改即可创建新 mod
    /// </summary>
    [PluginConfig("MyFirstMod", "cmz", "1.0.0")]
    public class MyFirstModPlugin : ChronoArkPlugin
    {
        public override void Initialize()
        {
            Debug.Log("[MyFirstMod] ========== Initialize 开始 ==========");
            Debug.Log("[MyFirstMod] 这是最小骨架模板，无任何功能");
            Debug.Log("[MyFirstMod] ========== Initialize 完成 ==========");
        }

        public override void Dispose()
        {
            Debug.Log("[MyFirstMod] Dispose called");
        }

        public override void OnModLoaded()
        {
            Debug.Log("[MyFirstMod] OnModLoaded 调用");
        }

        public override void OnModSettingUpdate()
        {
            Debug.Log("[MyFirstMod] OnModSettingUpdate 调用");
        }
    }
}
