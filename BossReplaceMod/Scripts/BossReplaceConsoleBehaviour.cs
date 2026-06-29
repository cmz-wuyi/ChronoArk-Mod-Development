using System;
using System.Collections.Generic;
using UnityEngine;

namespace BossReplaceMod
{
    /// <summary>
    /// Boss替换控制台 - F2 开关
    /// 独立 mod，提供 BossReplaceMod 的可视化操作界面
    /// </summary>
    public class BossReplaceConsoleBehaviour : MonoBehaviour
    {
        private bool showConsole = false;
        private Vector2 logScrollPos = Vector2.zero;
        private List<string> logMessages = new List<string>();
        private const int MAX_LOG_MESSAGES = 20;

        // 窗口位置和大小
        private Rect windowRect = new Rect(10, 10, 680, 860);

        // 内容滚动位置
        private Vector2 tabScrollPos = Vector2.zero;

        // BossReplace 控制面板
        private BossReplaceControlPanel controlPanel = new BossReplaceControlPanel();

        // 样式缓存
        private GUIStyle headerStyle;
        private GUIStyle normalStyle;
        private GUIStyle logStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smallHintStyle;
        private GUIStyle textFieldStyle;
        private bool stylesInitialized = false;

        void Update()
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.F2))
                {
                    showConsole = !showConsole;
                    Log("Boss替换控制台 " + (showConsole ? "已打开" : "已关闭"));
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
            windowRect = GUI.Window(9998, windowRect, DrawConsoleWindow, "Boss替换控制台（F2：开关）");
        }

        private void EnsureStylesInitialized()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = new Color(1f, 0.97f, 0.6f);

            normalStyle = new GUIStyle(GUI.skin.label) { fontSize = 25 };
            normalStyle.normal.textColor = Color.white;

            logStyle = new GUIStyle(GUI.skin.label) { fontSize = 23 };
            logStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f);

            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 25 };

            smallHintStyle = new GUIStyle(GUI.skin.label) { fontSize = 23 };
            smallHintStyle.normal.textColor = new Color(0.85f, 0.95f, 1f);

            textFieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 25 };

            stylesInitialized = true;
        }

        void DrawConsoleWindow(int windowId)
        {
            GUILayout.BeginVertical();

            // === 内容区（可滚动）===
            tabScrollPos = GUILayout.BeginScrollView(tabScrollPos, GUILayout.ExpandHeight(true));

            // 调用控制面板绘制
            controlPanel.DrawPanel(Log, headerStyle, normalStyle, buttonStyle, smallHintStyle, textFieldStyle);

            GUILayout.Space(16);

            // === 切换提示 ===
            GUILayout.Label("═══ 切换其他控制台 ═══", headerStyle);
            GUILayout.Label("F1 → 审判官事件调试（DebugConsoleMod）", smallHintStyle);
            GUILayout.Label("F3 → 卷轴获取控制台（ScrollConsoleMod）", smallHintStyle);
            GUILayout.Label("再按 F2 → 关闭本窗口", smallHintStyle);

            GUILayout.EndScrollView();

            GUILayout.Space(8);

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

            Debug.Log("[BossReplaceConsole] " + message);
        }
    }
}
