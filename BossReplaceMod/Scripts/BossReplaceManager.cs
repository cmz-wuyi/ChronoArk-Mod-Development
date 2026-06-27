using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace BossReplaceMod
{
    /// <summary>
    /// Boss替换管理器
    /// 负责加载配置和管理替换规则
    /// </summary>
    public static class BossReplaceManager
    {
        private static BossReplaceConfig config;
        private static string configPath;

        /// <summary>配置是否已加载</summary>
        public static bool IsLoaded { get; private set; }

        /// <summary>当前配置</summary>
        public static BossReplaceConfig Config
        {
            get
            {
                if (!IsLoaded) LoadConfig();
                return config;
            }
        }

        /// <summary>初始化管理器</summary>
        public static void Initialize()
        {
            configPath = Path.Combine(Application.persistentDataPath, "BossReplaceConfig.json");
            LoadConfig();
        }

        /// <summary>加载配置文件</summary>
        public static void LoadConfig()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonConvert.DeserializeObject<BossReplaceConfig>(json);
                    Debug.Log("[BossReplaceMod] Config loaded from: " + configPath);
                }
                else
                {
                    config = BossReplaceConfig.GetDefault();
                    SaveConfig();
                    Debug.Log("[BossReplaceMod] Default config created at: " + configPath);
                }
                IsLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.Log("[BossReplaceMod] Error loading config: " + ex.Message);
                config = BossReplaceConfig.GetDefault();
                IsLoaded = true;
            }
        }

        /// <summary>保存配置文件</summary>
        public static void SaveConfig()
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Debug.Log("[BossReplaceMod] Error saving config: " + ex.Message);
            }
        }

        /// <summary>
        /// 检查是否应该替换Boss
        /// </summary>
        /// <param name="stageKey">当前关卡Key</param>
        /// <param name="originalBossQueue">原Boss队列Key</param>
        /// <returns>替换后的Boss队列Key，如果不替换则返回null</returns>
        public static string CheckReplace(string stageKey, string originalBossQueue)
        {
            if (!IsLoaded || config == null || !config.Enabled)
                return null;

            foreach (var rule in config.Rules)
            {
                if (!rule.ReplaceEnabled)
                    continue;

                // 检查关卡Key（留空表示匹配所有关卡）
                if (!string.IsNullOrEmpty(rule.StageKey) && rule.StageKey != stageKey)
                    continue;

                // 检查原Boss队列（留空表示替换所有Boss）
                if (!string.IsNullOrEmpty(rule.OriginalBossQueue) && rule.OriginalBossQueue != originalBossQueue)
                    continue;

                if (config.DebugLog)
                {
                    Debug.Log("[BossReplaceMod] Replace match found:");
                    Debug.Log("[BossReplaceMod]   Stage: " + stageKey);
                    Debug.Log("[BossReplaceMod]   Original: " + originalBossQueue);
                    Debug.Log("[BossReplaceMod]   New: " + rule.NewBossQueue);
                }

                return rule.NewBossQueue;
            }

            return null;
        }

        /// <summary>
        /// 检查是否有通过事件触发的替换规则
        /// </summary>
        /// <param name="eventKey">触发的事件Key</param>
        /// <returns>需要替换的Boss队列Key</returns>
        public static string CheckEventTriggeredReplace(string eventKey)
        {
            if (!IsLoaded || config == null || !config.Enabled)
                return null;

            foreach (var rule in config.Rules)
            {
                if (!rule.ReplaceEnabled)
                    continue;

                if (string.IsNullOrEmpty(rule.TriggerEventKey))
                    continue;

                if (rule.TriggerEventKey == eventKey)
                {
                    if (config.DebugLog)
                    {
                        Debug.Log("[BossReplaceMod] Event-triggered replace:");
                        Debug.Log("[BossReplaceMod]   Event: " + eventKey);
                        Debug.Log("[BossReplaceMod]   New Boss: " + rule.NewBossQueue);
                    }
                    return rule.NewBossQueue;
                }
            }

            return null;
        }
    }
}
