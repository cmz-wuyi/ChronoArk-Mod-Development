using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoSaveMod
{
    /// <summary>
    /// 自动存档控制台内容绘制
    /// 包含：设置区、操作区、存档列表区、新手引导区
    /// </summary>
    public class AutoSaveContent
    {
        // 配置输入字段缓存
        private string maxSavesInput = "";
        private string delayInput = "";
        private bool configLoaded = false;

        // 存档列表缓存
        private List<SaveBackupInfo> backupList = null;
        private float lastRefreshTime = 0f;
        private const float REFRESH_INTERVAL = 2.0f;

        // 确认操作状态
        private string pendingLoadDir = null;
        private string pendingDeleteDir = null;

        /// <summary>
        /// 绘制内容
        /// </summary>
        public void DrawContent(
            GUIStyle headerStyle, GUIStyle normalStyle,
            GUIStyle buttonStyle, GUIStyle smallHintStyle,
            GUIStyle warningStyle, GUIStyle textFieldStyle,
            Action<string> logger)
        {
            // 确保配置加载
            EnsureConfigLoaded();

            // 1. 新手引导区
            DrawHelpSection(headerStyle, normalStyle, smallHintStyle);

            GUILayout.Space(16);

            // 2. 设置区
            DrawSettingsSection(headerStyle, normalStyle, buttonStyle, smallHintStyle, textFieldStyle, logger);

            GUILayout.Space(16);

            // 3. 操作区
            DrawActionSection(headerStyle, buttonStyle, warningStyle, logger);

            GUILayout.Space(16);

            // 4. 存档列表区
            DrawBackupListSection(headerStyle, normalStyle, buttonStyle, smallHintStyle, warningStyle, logger);
        }

        private void EnsureConfigLoaded()
        {
            if (configLoaded) return;
            var config = AutoSaveManager.Config;
            if (config != null)
            {
                maxSavesInput = config.MaxSaves.ToString();
                delayInput = config.StageSaveDelay.ToString("F1");
                configLoaded = true;
            }
        }

        // ============ 新手引导区 ============

        private void DrawHelpSection(GUIStyle headerStyle, GUIStyle normalStyle, GUIStyle smallHintStyle)
        {
            GUILayout.Label("═══ 自动存档说明 ═══", headerStyle);
            GUILayout.Label("本 mod 提供自动存档和读档功能：", normalStyle);
            GUILayout.Label("• 战斗结束 → 自动存档", smallHintStyle);
            GUILayout.Label("• 进入关卡 → 延迟2秒后自动存档（防篝火崩溃）", smallHintStyle);
            GUILayout.Label("• 退出游戏 → 退出前自动存档", smallHintStyle);
            GUILayout.Label("• 最多保留 5 个存档（可在下方设置，最多 20）", smallHintStyle);
            GUILayout.Label("• 快速读档 = 读取最近一次存档", smallHintStyle);
            GUILayout.Label("• 手动读档 = 从存档列表中选择", smallHintStyle);
            GUILayout.Label("• 读档可在战斗中使用（即使战斗卡住也能读档）", smallHintStyle);
        }

        // ============ 设置区 ============

        private void DrawSettingsSection(
            GUIStyle headerStyle, GUIStyle normalStyle,
            GUIStyle buttonStyle, GUIStyle smallHintStyle, GUIStyle textFieldStyle,
            Action<string> logger)
        {
            GUILayout.Label("═══ 存档设置 ═══", headerStyle);

            var config = AutoSaveManager.Config;
            if (config == null)
            {
                GUILayout.Label("配置未加载！", normalStyle);
                return;
            }

            // 最大存档数
            GUILayout.Label("最大存档数（1-20，默认5）：", normalStyle);
            GUILayout.BeginHorizontal();
            maxSavesInput = GUILayout.TextField(maxSavesInput, textFieldStyle, GUILayout.Width(80));
            if (GUILayout.Button("+", buttonStyle, GUILayout.Width(30), GUILayout.Height(24)))
            {
                int val;
                if (int.TryParse(maxSavesInput, out val) && val < 20)
                    maxSavesInput = (val + 1).ToString();
            }
            if (GUILayout.Button("-", buttonStyle, GUILayout.Width(30), GUILayout.Height(24)))
            {
                int val;
                if (int.TryParse(maxSavesInput, out val) && val > 1)
                    maxSavesInput = (val - 1).ToString();
            }
            GUILayout.Label("当前：" + maxSavesInput, smallHintStyle);
            GUILayout.EndHorizontal();
            GUILayout.Label("提示：过多存档可能造成手动读档时卡顿", smallHintStyle);

            GUILayout.Space(8);

            // 自动存档开关
            config.AutoSaveOnBattleEnd = GUILayout.Toggle(config.AutoSaveOnBattleEnd, "  战斗结束自动存档", normalStyle);
            config.AutoSaveOnStageStart = GUILayout.Toggle(config.AutoSaveOnStageStart, "  进入关卡自动存档", normalStyle);
            config.AutoSaveOnQuit = GUILayout.Toggle(config.AutoSaveOnQuit, "  退出游戏自动存档", normalStyle);

            GUILayout.Space(8);

            // 关卡延迟设置
            GUILayout.Label("关卡进入后延迟存档秒数（0.5-10）：", normalStyle);
            GUILayout.BeginHorizontal();
            delayInput = GUILayout.TextField(delayInput, textFieldStyle, GUILayout.Width(80));
            GUILayout.Label("秒（防篝火崩溃，建议2秒）", smallHintStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // 保存设置按钮
            if (GUILayout.Button("保存设置", buttonStyle, GUILayout.Height(36)))
            {
                SaveSettings(logger);
            }
        }

        private void SaveSettings(Action<string> logger)
        {
            try
            {
                var config = AutoSaveManager.Config;
                if (config == null)
                {
                    logger("错误：配置未加载");
                    return;
                }

                // 解析最大存档数
                int maxSaves;
                if (int.TryParse(maxSavesInput, out maxSaves))
                {
                    config.MaxSaves = Mathf.Clamp(maxSaves, 1, 20);
                    maxSavesInput = config.MaxSaves.ToString();
                }

                // 解析延迟
                float delay;
                if (float.TryParse(delayInput, out delay))
                {
                    config.StageSaveDelay = Mathf.Clamp(delay, 0.5f, 10f);
                    delayInput = config.StageSaveDelay.ToString("F1");
                }

                AutoSaveManager.SaveConfig();
                logger("设置已保存（最大存档=" + config.MaxSaves + "，延迟=" + config.StageSaveDelay + "秒）");
            }
            catch (Exception ex)
            {
                logger("保存设置异常: " + ex.Message);
            }
        }

        // ============ 操作区 ============

        private void DrawActionSection(
            GUIStyle headerStyle, GUIStyle buttonStyle,
            GUIStyle warningStyle, Action<string> logger)
        {
            GUILayout.Label("═══ 快捷操作 ═══", headerStyle);

            GUILayout.BeginHorizontal();

            // 快速读档按钮
            if (GUILayout.Button("快速读档\n（最近存档）", buttonStyle, GUILayout.Height(50), GUILayout.Width(200)))
            {
                logger("正在快速读档...");
                bool success = AutoSaveManager.QuickLoad();
                if (success)
                {
                    logger("快速读档成功！游戏将重载。");
                }
                else
                {
                    logger("快速读档失败：没有可用备份");
                }
            }

            // 立即手动存档按钮
            if (GUILayout.Button("立即存档", buttonStyle, GUILayout.Height(50), GUILayout.Width(200)))
            {
                logger("正在执行手动存档...");
                AutoSaveManager.PerformAutoSave("Manual");
                logger("手动存档完成");
                backupList = null; // 刷新列表
            }

            GUILayout.EndHorizontal();

            // 刷新列表按钮
            if (GUILayout.Button("刷新存档列表", buttonStyle, GUILayout.Height(30)))
            {
                backupList = null;
                logger("存档列表已刷新");
            }
        }

        // ============ 存档列表区 ============

        private void DrawBackupListSection(
            GUIStyle headerStyle, GUIStyle normalStyle,
            GUIStyle buttonStyle, GUIStyle smallHintStyle,
            GUIStyle warningStyle, Action<string> logger)
        {
            // 定期刷新列表
            RefreshBackupListIfNeeded();

            GUILayout.Label("═══ 存档列表 ═══", headerStyle);

            if (backupList == null || backupList.Count == 0)
            {
                GUILayout.Label("暂无存档备份", normalStyle);
                GUILayout.Label("战斗结束、进入关卡或退出游戏时会自动创建备份", smallHintStyle);
                return;
            }

            int currentCount = backupList.Count;
            int maxSaves = AutoSaveManager.Config != null ? AutoSaveManager.Config.MaxSaves : 5;
            GUILayout.Label("共 " + currentCount + " / " + maxSaves + " 个存档", normalStyle);

            GUILayout.Space(8);

            // 绘制每个备份条目
            for (int i = 0; i < backupList.Count; i++)
            {
                var backup = backupList[i];
                DrawBackupEntry(backup, i + 1, headerStyle, normalStyle, buttonStyle,
                    smallHintStyle, warningStyle, logger);
                GUILayout.Space(6);
            }
        }

        private void DrawBackupEntry(
            SaveBackupInfo backup, int index,
            GUIStyle headerStyle, GUIStyle normalStyle, GUIStyle buttonStyle,
            GUIStyle smallHintStyle, GUIStyle warningStyle,
            Action<string> logger)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            // 第一行：序号 + 时间 + 触发原因
            GUILayout.BeginHorizontal();
            GUILayout.Label("#" + index, headerStyle, GUILayout.Width(40));
            GUILayout.Label(backup.SaveTime, normalStyle, GUILayout.Width(180));
            GUILayout.Label("触发：" + backup.Trigger, smallHintStyle);
            GUILayout.EndHorizontal();

            // 第二行：循环 + 关卡 + 金币 + 灵魂石
            GUILayout.BeginHorizontal();
            GUILayout.Label("循环：" + backup.LoopNum, smallHintStyle, GUILayout.Width(70));
            GUILayout.Label("关卡：" + backup.StageKey + "(第" + backup.StageNum + "关)", smallHintStyle, GUILayout.Width(180));
            GUILayout.Label("金币：" + backup.Gold, smallHintStyle, GUILayout.Width(90));
            GUILayout.Label("灵魂石：" + backup.Soul, smallHintStyle, GUILayout.Width(90));
            GUILayout.EndHorizontal();

            // 第三行：游戏模式 + 血雾难度 + 游戏时间
            GUILayout.BeginHorizontal();
            string modeText = backup.PlayMode;
            if (backup.PlayMode == "StoryMode") modeText = "剧情模式";
            else if (backup.PlayMode == "FreeMode") modeText = "自由模式";
            GUILayout.Label("模式：" + modeText, smallHintStyle, GUILayout.Width(100));
            if (backup.HopeMode)
            {
                GUILayout.Label("血雾：等级" + backup.BloodyMistLevel, smallHintStyle, GUILayout.Width(120));
            }
            else
            {
                GUILayout.Label("血雾：未开启", smallHintStyle, GUILayout.Width(120));
            }
            GUILayout.Label("时间：" + FormatGameTime(backup.TimeTick), smallHintStyle);
            GUILayout.EndHorizontal();

            // 第四行：出战角色
            GUILayout.BeginHorizontal();
            GUILayout.Label("出战：", smallHintStyle, GUILayout.Width(50));
            if (backup.PartyInfo != null && backup.PartyInfo.Count > 0)
            {
                foreach (var charInfo in backup.PartyInfo)
                {
                    GUILayout.Label(charInfo.Name + "(Lv." + charInfo.Level + ")", smallHintStyle, GUILayout.Width(120));
                }
            }
            else
            {
                GUILayout.Label("(无角色数据)", smallHintStyle);
            }
            GUILayout.EndHorizontal();

            // 第五行：文件大小
            GUILayout.Label("大小：Save1=" + backup.Save1SizeKB + "KB + Save0=" + backup.Save0SizeKB + "KB", smallHintStyle);

            // 第六行：操作按钮
            GUILayout.BeginHorizontal();

            // 读档确认
            if (pendingLoadDir == backup.DirName)
            {
                GUILayout.Label("确认读档？", warningStyle);
                if (GUILayout.Button("确认", buttonStyle, GUILayout.Width(60), GUILayout.Height(28)))
                {
                    logger("正在从备份读档：" + backup.DirName);
                    bool success = AutoSaveManager.LoadFromBackup(backup.DirName);
                    if (success)
                    {
                        logger("读档成功：" + backup.SaveTime);
                    }
                    else
                    {
                        logger("读档失败：" + backup.DirName);
                    }
                    pendingLoadDir = null;
                }
                if (GUILayout.Button("取消", buttonStyle, GUILayout.Width(60), GUILayout.Height(28)))
                {
                    pendingLoadDir = null;
                }
            }
            // 删除确认
            else if (pendingDeleteDir == backup.DirName)
            {
                GUILayout.Label("确认删除？", warningStyle);
                if (GUILayout.Button("删除", buttonStyle, GUILayout.Width(60), GUILayout.Height(28)))
                {
                    bool success = AutoSaveManager.DeleteBackup(backup.DirName);
                    if (success)
                    {
                        logger("已删除备份：" + backup.DirName);
                        backupList = null;
                    }
                    else
                    {
                        logger("删除失败：" + backup.DirName);
                    }
                    pendingDeleteDir = null;
                }
                if (GUILayout.Button("取消", buttonStyle, GUILayout.Width(60), GUILayout.Height(28)))
                {
                    pendingDeleteDir = null;
                }
            }
            // 正常按钮
            else
            {
                if (GUILayout.Button("读档", buttonStyle, GUILayout.Width(80), GUILayout.Height(28)))
                {
                    pendingLoadDir = backup.DirName;
                }
                if (GUILayout.Button("删除", buttonStyle, GUILayout.Width(80), GUILayout.Height(28)))
                {
                    pendingDeleteDir = backup.DirName;
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        // ============ 列表刷新 ============

        private void RefreshBackupListIfNeeded()
        {
            if (Time.realtimeSinceStartup - lastRefreshTime < REFRESH_INTERVAL && backupList != null)
                return;

            try
            {
                backupList = AutoSaveManager.GetBackupList();
                lastRefreshTime = Time.realtimeSinceStartup;
            }
            catch { }
        }

        /// <summary>格式化游戏时间（ticks → HH:MM:SS）</summary>
        private string FormatGameTime(long timeTick)
        {
            try
            {
                if (timeTick <= 0) return "00:00:00";
                // TimeSpan 以 100ns 为单位，1 tick = 100ns
                var ts = new System.TimeSpan(timeTick);
                return string.Format("{0:D2}:{1:D2}:{2:D2}",
                    (int)ts.TotalHours, ts.Minutes, ts.Seconds);
            }
            catch { return "00:00:00"; }
        }
    }
}
