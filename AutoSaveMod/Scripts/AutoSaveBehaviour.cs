using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AutoSaveMod
{
    /// <summary>
    /// 自动存档控制台 - F4 开关
    /// 独立 mod，提供自动存档设置、快速读档、手动读档功能
    /// </summary>
    public class AutoSaveBehaviour : MonoBehaviour
    {
        public static AutoSaveBehaviour Instance { get; private set; }

        private bool showConsole = false;
        private Vector2 logScrollPos = Vector2.zero;
        private List<string> logMessages = new List<string>();
        private const int MAX_LOG_MESSAGES = 20;

        // 窗口位置和大小（统一规范 680x860）
        private Rect windowRect = new Rect(10, 10, 680, 860);

        // 内容滚动位置
        private Vector2 tabScrollPos = Vector2.zero;

        // 存档内容
        private AutoSaveContent saveContent = new AutoSaveContent();

        // 样式缓存
        private GUIStyle headerStyle;
        private GUIStyle normalStyle;
        private GUIStyle logStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smallHintStyle;
        private GUIStyle warningStyle;
        private GUIStyle textFieldStyle;
        private bool stylesInitialized = false;

        // 延迟存档控制
        private bool delayedSaveInProgress = false;

        void Awake()
        {
            Instance = this;
            Debug.Log("[AutoSaveMod] AutoSaveBehaviour.Awake 完成, Instance 已设置");
        }

        void Update()
        {
            try
            {
                // 只监听 F4 键
                if (Input.GetKeyDown(KeyCode.F4))
                {
                    showConsole = !showConsole;
                    Log("自动存档控制台 " + (showConsole ? "已打开" : "已关闭"));
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
            // 窗口 ID = 9996（避免与 F1=9999、F2=9998、F3=9997 冲突）
            windowRect = GUI.Window(9996, windowRect, DrawConsoleWindow, "自动存档控制台（F4：开关）");
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

            warningStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 25,
                fontStyle = FontStyle.Bold
            };
            warningStyle.normal.textColor = new Color(1f, 0.5f, 0.5f);

            textFieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 25 };

            stylesInitialized = true;
        }

        void DrawConsoleWindow(int windowId)
        {
            GUILayout.BeginVertical();

            // === 内容区（可滚动）===
            tabScrollPos = GUILayout.BeginScrollView(tabScrollPos, GUILayout.ExpandHeight(true));

            // 调用存档内容绘制
            saveContent.DrawContent(headerStyle, normalStyle, buttonStyle, smallHintStyle,
                warningStyle, textFieldStyle, Log);

            GUILayout.Space(16);

            // === 切换提示 ===
            GUILayout.Label("═══ 切换其他控制台 ═══", headerStyle);
            GUILayout.Label("F1 → 审判官事件调试（DebugConsoleMod）", smallHintStyle);
            GUILayout.Label("F2 → Boss替换控制台（BossReplaceMod）", smallHintStyle);
            GUILayout.Label("F3 → 卷轴获取控制台（ScrollConsoleMod）", smallHintStyle);
            GUILayout.Label("再按 F4 → 关闭本窗口", smallHintStyle);

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

        /// <summary>启动延迟存档协程（防篝火崩溃）</summary>
        public void StartDelayedSave(string stageKey)
        {
            if (delayedSaveInProgress)
            {
                Log("延迟存档已在进行中，跳过本次（关卡：" + stageKey + "）");
                return;
            }

            float delay = AutoSaveManager.Config != null ? AutoSaveManager.Config.StageSaveDelay : 2.0f;
            StartCoroutine(DelayedSaveCoroutine(stageKey, delay));
        }

        /// <summary>延迟存档协程</summary>
        private IEnumerator DelayedSaveCoroutine(string stageKey, float delay)
        {
            delayedSaveInProgress = true;
            Log("延迟存档启动（关卡：" + stageKey + "，延迟：" + delay + "秒）");

            yield return new WaitForSeconds(delay);

            // 延迟后再次检查配置（可能在等待期间被关闭）
            if (AutoSaveManager.Config != null && AutoSaveManager.Config.AutoSaveOnStageStart)
            {
                AutoSaveManager.PerformAutoSave("StageStart_" + stageKey);
                Log("延迟存档完成（关卡：" + stageKey + "）");
            }

            delayedSaveInProgress = false;
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

            Debug.Log("[AutoSaveMod] " + message);
        }

        void OnDestroy()
        {
            Instance = null;
            Debug.Log("[AutoSaveMod] AutoSaveBehaviour.OnDestroy, Instance 已清除");
        }
    }
}
