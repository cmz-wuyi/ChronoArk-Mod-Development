using System.Collections.Generic;

namespace BossReplaceMod
{
    /// <summary>
    /// Boss替换规则配置
    /// </summary>
    public class BossReplaceRule
    {
        /// <summary>关卡Key (如 "Stage3_1")</summary>
        public string StageKey = "";

        /// <summary>触发事件Key (如 "RE_TheInquisition")，留空表示无条件替换</summary>
        public string TriggerEventKey = "";

        /// <summary>原Boss队列Key，留空表示替换所有Boss</summary>
        public string OriginalBossQueue = "";

        /// <summary>新Boss队列Key (如 "Queue_FanaticBoss")</summary>
        public string NewBossQueue = "";

        /// <summary>是否启用此规则</summary>
        public bool ReplaceEnabled = true;
    }

    /// <summary>
    /// Mod配置文件结构
    /// </summary>
    public class BossReplaceConfig
    {
        /// <summary>是否启用Boss替换功能</summary>
        public bool Enabled = true;

        /// <summary>是否输出调试日志</summary>
        public bool DebugLog = true;

        /// <summary>替换规则列表</summary>
        public List<BossReplaceRule> Rules = new List<BossReplaceRule>();

        /// <summary>获取默认配置</summary>
        public static BossReplaceConfig GetDefault()
        {
            return new BossReplaceConfig
            {
                Enabled = true,
                DebugLog = true,
                Rules = new List<BossReplaceRule>
                {
                    new BossReplaceRule
                    {
                        StageKey = "",
                        TriggerEventKey = "RE_TheInquisition",
                        OriginalBossQueue = "",
                        NewBossQueue = "Queue_FanaticBoss",
                        ReplaceEnabled = true
                    }
                }
            };
        }
    }
}
