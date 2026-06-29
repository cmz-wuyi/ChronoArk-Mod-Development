using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScrollConsoleMod
{
    /// <summary>
    /// 卷轴获取控制台 - F3 开关
    /// 独立 mod，只负责提供 11 种卷轴的获取界面
    /// </summary>
    public class ScrollConsoleBehaviour : MonoBehaviour
    {
        private bool showConsole = false;
        private Vector2 logScrollPos = Vector2.zero;
        private List<string> logMessages = new List<string>();
        private const int MAX_LOG_MESSAGES = 20;

        // 窗口位置和大小（统一规范 680x860）
        private Rect windowRect = new Rect(10, 10, 680, 860);

        // 内容滚动位置
        private Vector2 tabScrollPos = Vector2.zero;

        // 卷轴获取内容
        private ScrollConsoleContent scrollContent = new ScrollConsoleContent();

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
                // 只监听 F3 键
                if (Input.GetKeyDown(KeyCode.F3))
                {
                    showConsole = !showConsole;
                    Log("卷轴获取控制台 " + (showConsole ? "已打开" : "已关闭"));
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
            // 窗口 ID = 9997（避免与 F1=9999、F2=9998 冲突）
            windowRect = GUI.Window(9997, windowRect, DrawConsoleWindow, "卷轴获取控制台（F3：开关）");
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

            // 调用卷轴内容绘制（注意参数顺序：headerStyle, normalStyle, buttonStyle, smallHintStyle, textFieldStyle, logger）
            scrollContent.DrawContent(headerStyle, normalStyle, buttonStyle, smallHintStyle, textFieldStyle, Log);

            GUILayout.Space(16);

            // === 切换提示 ===
            GUILayout.Label("═══ 切换其他控制台 ═══", headerStyle);
            GUILayout.Label("F1 → 审判官事件调试（DebugConsoleMod）", smallHintStyle);
            GUILayout.Label("F2 → Boss替换控制台（BossReplaceMod）", smallHintStyle);
            GUILayout.Label("再按 F3 → 关闭本窗口", smallHintStyle);

            GUILayout.EndScrollView();

            GUILayout.Space(8);

            // === 日志区域（固定在底部，120px）===
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

            Debug.Log("[ScrollConsole] " + message);
        }
    }
}
