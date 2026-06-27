using System;
using System.Collections.Generic;
using System.Reflection;
using GameDataEditor;
using UnityEngine;

namespace DebugConsoleMod
{
    /// <summary>
    /// 审判官事件调试控制台 - F1 开关
    /// 独立 mod，只负责审判官事件相关调试
    /// </summary>
    public class DebugConsoleBehaviour : MonoBehaviour
    {
        private bool showConsole = false;
        private Vector2 logScrollPos = Vector2.zero;
        private List<string> logMessages = new List<string>();
        private const int MAX_LOG_MESSAGES = 20;

        // 窗口位置和大小
        private Rect windowRect = new Rect(10, 10, 680, 860);

        // 内容滚动位置
        private Vector2 tabScrollPos = Vector2.zero;

        // 清除状态二次确认
        private bool confirmReset = false;

        // 样式缓存
        private GUIStyle headerStyle;
        private GUIStyle normalStyle;
        private GUIStyle logStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smallHintStyle;
        private GUIStyle warningStyle;
        private bool stylesInitialized = false;

        void Update()
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    showConsole = !showConsole;
                    Log("审判官控制台 " + (showConsole ? "已打开" : "已关闭"));
                }
            }
            catch (Exception ex)
            {
                Log("Update 异常: " + ex.Message);
            }
        }

        void OnGUI()
        {
            if (!showConsole) return;

            EnsureStylesInitialized();
            windowRect = GUI.Window(9999, windowRect, DrawConsoleWindow, "审判官事件调试（F1：开关）");
        }

        private void EnsureStylesInitialized()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = new Color(1f, 0.92f, 0.4f);

            normalStyle = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            normalStyle.normal.textColor = Color.white;

            logStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            logStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };

            smallHintStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            smallHintStyle.normal.textColor = new Color(0.7f, 0.85f, 1f);

            warningStyle = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            warningStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);

            stylesInitialized = true;
        }

        void DrawConsoleWindow(int windowId)
        {
            GUILayout.BeginVertical();

            // === 内容区（可滚动）===
            tabScrollPos = GUILayout.BeginScrollView(tabScrollPos, GUILayout.ExpandHeight(true));

            DrawInquisitionTab();

            GUILayout.EndScrollView();

            GUILayout.Space(5);

            // === 日志区域（固定在底部）===
            GUILayout.Label("═══ 操作日志 ═══", headerStyle);

            logScrollPos = GUILayout.BeginScrollView(logScrollPos, GUILayout.Height(120));

            foreach (string msg in logMessages)
            {
                GUILayout.Label(msg, logStyle);
            }

            GUILayout.EndScrollView();

            if (GUILayout.Button("清空日志", buttonStyle, GUILayout.Height(32)))
            {
                logMessages.Clear();
            }

            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, 10000, 30));
        }

        private void DrawInquisitionTab()
        {
            // === 状态警告区（顶部）===
            DrawWarningArea();

            GUILayout.Space(10);

            // === 新手引导区 ===
            GUILayout.Label("═══ 新手快速测试（3步）═══", headerStyle);
            GUILayout.Label("1. 点击下方「跳转到第三章·白色墓地」", smallHintStyle);
            GUILayout.Label("2. 点击「完整测试（触发事件+修复Bug）」", smallHintStyle);
            GUILayout.Label("3. 进入Boss战 → 应该出现犹大", smallHintStyle);

            GUILayout.Space(10);

            // === 状态显示区域 ===
            GUILayout.Label("═══ 当前游戏状态 ═══", headerStyle);
            DrawGameState();

            GUILayout.Space(10);

            // === A. 快速测试区 ===
            GUILayout.Label("═══ A. 快速测试（推荐新手）═══", headerStyle);

            if (GUILayout.Button("跳转到第三章·白色墓地", buttonStyle, GUILayout.Height(48)))
            {
                JumpToStage3();
            }
            GUILayout.Label("执行：直接传送到第三章白色墓地关卡", smallHintStyle);
            GUILayout.Label("用途：不用从头玩到Stage3，方便快速测试Boss替换", smallHintStyle);

            GUILayout.Space(6);

            if (GUILayout.Button("完整测试（触发事件+修复Bug）", buttonStyle, GUILayout.Height(48)))
            {
                ApplyBossFix();
            }
            GUILayout.Label("执行：设置审判官事件=true + 乔汉任务完成=true + 给予火炬装备", smallHintStyle);
            GUILayout.Label("原因：游戏Bug导致乔汉任务永远不会标记完成，Boss无法替换", smallHintStyle);
            GUILayout.Label("结果：满足Boss替换条件，进入Boss战时会出现犹大", smallHintStyle);

            GUILayout.Space(10);

            // === B. 单项操作区 ===
            GUILayout.Label("═══ B. 单项操作（高级调试）═══", headerStyle);

            // B1. 事件模拟
            GUILayout.Label("── B1. 事件模拟 ──", normalStyle);

            if (GUILayout.Button("模拟触发：异端审判官事件", buttonStyle, GUILayout.Height(44)))
            {
                SafeAction(() =>
                {
                    PlayData.TSavedata.TheInquisition = true;
                    GiveInquisitionTorch();
                    Log("已设置 TheInquisition=true，并给予审判官火炬装备");
                });
            }
            GUILayout.Label("执行：设置审判官事件=true + 给予审判官火炬装备", smallHintStyle);
            GUILayout.Label("背景：正常游戏中需要在Stage3探索时随机遇到此事件", smallHintStyle);

            GUILayout.Space(6);

            if (GUILayout.Button("修复乔汉任务Bug（只设置完成标志）", buttonStyle, GUILayout.Height(44)))
            {
                SafeAction(() =>
                {
                    SaveManager.NowData.storydata.JohanQuestClear = true;
                    Log("已设置 JohanQuestClear=true（不触发事件）");
                });
            }
            GUILayout.Label("执行：只设置乔汉任务完成标志=true", smallHintStyle);
            GUILayout.Label("原因：游戏Bug！此处永远不会自动设为true，需手动设置", smallHintStyle);
            GUILayout.Label("区别：不会触发审判官事件，只修复Bug", smallHintStyle);

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("设置进度：1（第一次遇到）", buttonStyle, GUILayout.Height(44)))
            {
                SafeAction(() =>
                {
                    SaveManager.NowData.storydata.JohanQuestProgress = 1;
                    Log("已设置 JohanQuestProgress=1");
                });
            }
            if (GUILayout.Button("设置进度：2（第二次遇到）", buttonStyle, GUILayout.Height(44)))
            {
                SafeAction(() =>
                {
                    SaveManager.NowData.storydata.JohanQuestProgress = 2;
                    Log("已设置 JohanQuestProgress=2");
                });
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("进度0=未开始，1=第一次遇到（开始任务），2=第二次遇到（触发事件）", smallHintStyle);

            GUILayout.Space(6);

            // B2. 直接操作
            GUILayout.Label("── B2. 直接操作 ──", normalStyle);

            if (GUILayout.Button("暴力替换：直接换成犹大Boss", buttonStyle, GUILayout.Height(48)))
            {
                ForceBossReplace();
            }
            GUILayout.Label("执行：清空当前敌人队列，直接填入 Queue_FanaticBoss", smallHintStyle);
            GUILayout.Label("区别：绕过所有条件检查，立即生效（不经过BossReplaceMod规则）", smallHintStyle);

            GUILayout.Space(10);

            // === C. 状态重置区 ===
            GUILayout.Label("═══ C. 状态重置 ═══", headerStyle);

            if (!confirmReset)
            {
                if (GUILayout.Button("重置审判官事件标记", buttonStyle, GUILayout.Height(44)))
                {
                    confirmReset = true;
                    Log("确认重置审判官事件标记？再次点击执行");
                }
                GUILayout.Label("执行：审判官事件=false（保留乔汉任务完成状态）", smallHintStyle);
                GUILayout.Label("用途：清除审判官事件触发标记，让事件可在白色墓地重新随机触发", smallHintStyle);
            }
            else
            {
                GUILayout.Label("确认重置？仅清除审判官事件标记，保留任务完成状态", warningStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("确认清除", buttonStyle, GUILayout.Height(44)))
                {
                    ResetAllFlags();
                    confirmReset = false;
                }
                if (GUILayout.Button("取消", buttonStyle, GUILayout.Height(44)))
                {
                    confirmReset = false;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8);

            // === 切换提示 ===
            GUILayout.Label("═══ 切换其他控制台 ═══", headerStyle);
            GUILayout.Label("F2 → Boss替换控制台（BossReplaceMod）", smallHintStyle);
            GUILayout.Label("F3 → 卷轴获取控制台（ScrollConsoleMod）", smallHintStyle);
            GUILayout.Label("再按 F1 → 关闭本窗口", smallHintStyle);
        }

        /// <summary>状态警告区</summary>
        private void DrawWarningArea()
        {
            try
            {
                bool theInquisition = false;
                SafeAction(() =>
                {
                    theInquisition = PlayData.TSavedata.TheInquisition;
                });

                if (theInquisition)
                {
                    GUILayout.Label("═══ 警告 ═══", warningStyle);
                    GUILayout.Label("检测到审判官事件已触发(TheInquisition=true)！", warningStyle);
                    GUILayout.Label("这可能是之前测试残留的状态。", warningStyle);
                    GUILayout.Label("如果这不是你正常游戏触发的，", warningStyle);
                    GUILayout.Label("请点击下方「清除测试状态」按钮清除。", warningStyle);
                }
            }
            catch { }
        }

        private void DrawGameState()
        {
            try
            {
                string stageKey = GetCurrentStageKey();
                GUILayout.Label("当前关卡：" + stageKey, normalStyle);
                GUILayout.Label("（当前所在的关卡，如 Stage3 = 白色墓地）", smallHintStyle);

                GUILayout.Space(10);

                bool theInquisition = false;
                bool johanQuestClear = false;
                int johanQuestProgress = 0;
                bool isJohanQuest = false;
                bool bossClear = false;

                SafeAction(() =>
                {
                    theInquisition = PlayData.TSavedata.TheInquisition;
                    isJohanQuest = PlayData.TSavedata.IsJohanQuest;
                    bossClear = PlayData.TSavedata.BossClear;
                    johanQuestClear = SaveManager.NowData.storydata.JohanQuestClear;
                    johanQuestProgress = SaveManager.NowData.storydata.JohanQuestProgress;
                });

                // === Boss替换条件汇总 ===
                GUILayout.Label("═══ Boss替换条件汇总 ═══", headerStyle);
                string condition1 = theInquisition ? "✓ 审判官事件" : "✗ 审判官事件";
                string condition2 = johanQuestClear ? "✓ 乔汉任务完成" : "✗ 乔汉任务完成";
                string result = (theInquisition && johanQuestClear) ? "→ Boss会替换为犹大！" : "→ Boss不会替换";
                GUILayout.Label(condition1 + "  +  " + condition2 + "  " + result, normalStyle);
                if (!(theInquisition && johanQuestClear))
                {
                    GUILayout.Label("提示：点击「完整测试」可同时设置这两项", smallHintStyle);
                }

                GUILayout.Space(10);

                // === 详细状态 ===
                GUILayout.Label("═══ 详细状态 ═══", headerStyle);

                GUILayout.Label("审判官事件已触发：" + ColoredBool(theInquisition), normalStyle);
                GUILayout.Label("  含义：玩家在白色墓地遇到了异端审判官事件", smallHintStyle);

                GUILayout.Space(4);

                GUILayout.Label("乔汉任务已完成：" + ColoredBool(johanQuestClear), normalStyle);
                GUILayout.Label("  含义：游戏Bug！此处永远为false，需手动设为true", smallHintStyle);
                GUILayout.Label("  关键：这是Boss替换的必要条件，必须为true才会替换", smallHintStyle);

                GUILayout.Space(4);

                GUILayout.Label("乔汉任务进度：" + johanQuestProgress, normalStyle);
                string progressMeaning = johanQuestProgress == 0 ? "（未开始）" :
                    johanQuestProgress == 1 ? "（第一次遇到，任务开始）" :
                    johanQuestProgress == 2 ? "（第二次遇到，触发事件）" : "（未知）";
                GUILayout.Label("  含义：0=未开始, 1=开始任务, 2=触发事件" + progressMeaning, smallHintStyle);

                GUILayout.Space(4);

                GUILayout.Label("乔汉任务进行中：" + ColoredBool(isJohanQuest), normalStyle);
                GUILayout.Label("  含义：当前是否正在执行乔汉任务剧情", smallHintStyle);

                GUILayout.Space(4);

                GUILayout.Label("Boss已击败：" + ColoredBool(bossClear), normalStyle);
                GUILayout.Label("  含义：当前关卡的Boss是否已被击败", smallHintStyle);

                GUILayout.Space(10);

                // 显示NowBossQueueKeys
                GUILayout.Label("── 预定Boss队列 ──", normalStyle);
                GUILayout.Label("  含义：游戏预设的Boss队列Key列表", smallHintStyle);
                SafeAction(() =>
                {
                    var keys = PlayData.TSavedata.NowBossQueueKeys;
                    if (keys != null && keys.Count > 0)
                    {
                        for (int i = 0; i < keys.Count; i++)
                        {
                            GUILayout.Label("  [" + i + "] " + keys[i], normalStyle);
                        }
                    }
                    else
                    {
                        GUILayout.Label("  (空)", normalStyle);
                    }
                });

                GUILayout.Space(6);

                // 显示EnemyQueue
                GUILayout.Label("── 当前关卡敌人队列 ──", normalStyle);
                GUILayout.Label("  含义：当前关卡实际的敌人队列（Boss战时显示）", smallHintStyle);
                SafeAction(() =>
                {
                    var ssType = typeof(StageSystem);
                    var field = ssType.GetField("EnemyQueue",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (field != null && StageSystem.instance != null)
                    {
                        var queue = field.GetValue(StageSystem.instance) as List<GDEEnemyQueueData>;
                        if (queue != null && queue.Count > 0)
                        {
                            for (int i = 0; i < queue.Count; i++)
                            {
                                GUILayout.Label("  [" + i + "] " + queue[i].Key, normalStyle);
                            }
                        }
                        else
                        {
                            GUILayout.Label("  (空或null)", normalStyle);
                        }
                    }
                    else
                    {
                        GUILayout.Label("  (关卡系统未初始化)", normalStyle);
                    }
                });
            }
            catch (Exception ex)
            {
                GUILayout.Label("错误：" + ex.Message, normalStyle);
            }
        }

        private string ColoredBool(bool value)
        {
            return value ? "<color=green>是(true)</color>" : "<color=red>否(false)</color>";
        }

        private string GetCurrentStageKey()
        {
            try
            {
                if (StageSystem.instance != null)
                {
                    var ssType = typeof(StageSystem);
                    var field = ssType.GetField("StageData",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        var stageData = field.GetValue(StageSystem.instance) as GDEStageData;
                        if (stageData != null)
                        {
                            return stageData.Key;
                        }
                    }
                }
            }
            catch { }
            return "(未知)";
        }

        private void JumpToStage3()
        {
            SafeAction(() =>
            {
                var fsType = typeof(FieldSystem);
                object instance = null;

                var instanceField = fsType.GetField("instance",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (instanceField != null)
                {
                    instance = instanceField.GetValue(null);
                }

                if (instance == null)
                {
                    var instanceProp = fsType.GetProperty("Instance",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (instanceProp != null)
                    {
                        instance = instanceProp.GetValue(null, null);
                    }
                }

                if (instance == null)
                {
                    Log("错误：FieldSystem.instance 为 null（需要先进入游戏）");
                    return;
                }

                var stageStartMethod = fsType.GetMethod("StageStart",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (stageStartMethod != null)
                {
                    stageStartMethod.Invoke(instance, new object[] { "Stage3" });
                    Log("已跳转到 Stage3");
                }
                else
                {
                    Log("错误：找不到 StageStart 方法");
                }
            });
        }

        private void ApplyBossFix()
        {
            SafeAction(() =>
            {
                PlayData.TSavedata.TheInquisition = true;
                SaveManager.NowData.storydata.JohanQuestClear = true;
                GiveInquisitionTorch();
                Log("完整测试已应用：TheInquisition=true, JohanQuestClear=true, 已给予审判官火炬装备");
            });
        }

        private void GiveInquisitionTorch()
        {
            try
            {
                var fieldInfo = typeof(GDEItemKeys).GetField("Item_Equip_Torch_FanaticBoss",
                    BindingFlags.Public | BindingFlags.Static);
                if (fieldInfo == null)
                {
                    Log("警告：找不到 GDEItemKeys.Item_Equip_Torch_FanaticBoss 字段");
                    return;
                }

                string itemId = fieldInfo.GetValue(null) as string;
                if (string.IsNullOrEmpty(itemId))
                {
                    Log("警告：GDEItemKeys.Item_Equip_Torch_FanaticBoss 值为空");
                    return;
                }

                ItemBase item = ItemBase.GetItem(itemId);
                if (item == null)
                {
                    Log("警告：GetItem 返回 null（GDE 数据未加载此装备）");
                    return;
                }

                InventoryManager.Reward(item);
                Log("已给予装备：" + itemId);
            }
            catch (Exception ex)
            {
                Log("给予装备异常：" + ex.Message);
            }
        }

        private void ForceBossReplace()
        {
            SafeAction(() =>
            {
                var ssType = typeof(StageSystem);
                var field = ssType.GetField("EnemyQueue",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (field != null && StageSystem.instance != null)
                {
                    var queue = field.GetValue(StageSystem.instance) as List<GDEEnemyQueueData>;
                    if (queue != null)
                    {
                        var newQueue = new GDEEnemyQueueData("Queue_FanaticBoss");
                        queue.Clear();
                        queue.Add(newQueue);
                        Log("Boss队列已替换为 Queue_FanaticBoss");
                    }
                    else
                    {
                        Log("错误：EnemyQueue 为 null");
                    }
                }
                else
                {
                    Log("错误：StageSystem.instance 或 EnemyQueue 字段未找到");
                }
            });
        }

        private void ResetAllFlags()
        {
            SafeAction(() =>
            {
                // 只重置审判官事件触发标记，让事件可在白色墓地重新随机触发
                PlayData.TSavedata.TheInquisition = false;
                // 保留 JohanQuestClear 和 JohanQuestProgress（任务保持已完成状态）
                Log("审判官事件标记已重置（TheInquisition=false）");
                Log("乔汉任务完成状态保留（JohanQuestClear=" +
                    SaveManager.NowData.storydata.JohanQuestClear + ", Progress=" +
                    SaveManager.NowData.storydata.JohanQuestProgress + "）");
            });
        }

        private void SafeAction(System.Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log("错误：" + ex.Message);
                Debug.Log("[DebugConsole] " + ex.StackTrace);
            }
        }

        private void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = "[" + timestamp + "] " + message;

            logMessages.Add(logEntry);

            if (logMessages.Count > MAX_LOG_MESSAGES)
            {
                logMessages.RemoveAt(0);
            }

            logScrollPos.y = float.MaxValue;

            Debug.Log("[DebugConsole] " + message);
        }
    }
}
