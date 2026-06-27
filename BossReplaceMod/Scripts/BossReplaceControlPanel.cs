using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace BossReplaceMod
{
    /// <summary>
    /// BossReplaceMod 控制面板
    /// 直接访问 BossReplaceManager，提供可视化操作界面
    /// 样式由外部 BossReplaceConsoleBehaviour 通过 DrawPanel 参数传入
    /// </summary>
    public class BossReplaceControlPanel
    {
        // 添加规则表单临时状态
        private string newStageKey = "";
        private string newTriggerEventKey = "RE_TheInquisition";
        private string newOriginalBossQueue = "";
        private string newNewBossQueue = "Queue_FanaticBoss";
        private bool newReplaceEnabled = true;

        // 规则列表滚动位置
        private Vector2 rulesScrollPos = Vector2.zero;

        // Manager 类型缓存
        private Type cachedManagerType;
        private float lastTypeLookupTime = -1f;
        private const float TYPE_CACHE_DURATION = 5f;

        // 当前样式（从 DrawPanel 参数赋值，供内部辅助方法使用）
        private GUIStyle headerStyle;
        private GUIStyle normalStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smallHintStyle;
        private GUIStyle textFieldStyle;

        // 错误样式缓存（首次使用时创建一次）
        private GUIStyle errorStyle;
        private bool errorStyleInitialized = false;

        /// <summary>
        /// 查找 BossReplaceManager 类型
        /// </summary>
        private Type GetManagerType()
        {
            // 缓存 5 秒，避免每次 OnGUI 都遍历程序集
            if (cachedManagerType != null && Time.realtimeSinceStartup - lastTypeLookupTime < TYPE_CACHE_DURATION)
                return cachedManagerType;

            lastTypeLookupTime = Time.realtimeSinceStartup;
            cachedManagerType = null;

            // 直接查找
            try
            {
                cachedManagerType = Type.GetType("BossReplaceMod.BossReplaceManager");
            }
            catch { }

            // 遍历所有程序集查找
            if (cachedManagerType == null)
            {
                try
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            var t = asm.GetType("BossReplaceMod.BossReplaceManager");
                            if (t != null)
                            {
                                cachedManagerType = t;
                                break;
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }

            return cachedManagerType;
        }

        /// <summary>BossReplaceMod 是否已加载</summary>
        public bool IsLoaded
        {
            get { return GetManagerType() != null; }
        }

        /// <summary>获取 Config 对象</summary>
        private object GetConfig()
        {
            var type = GetManagerType();
            if (type == null) return null;
            try
            {
                var prop = type.GetProperty("Config", BindingFlags.Public | BindingFlags.Static);
                if (prop != null) return prop.GetValue(null, null);
            }
            catch { }
            return null;
        }

        /// <summary>调用 Manager 的静态方法</summary>
        private object InvokeMethod(string methodName, params object[] args)
        {
            var type = GetManagerType();
            if (type == null) return null;
            try
            {
                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (method != null) return method.Invoke(null, args);
            }
            catch { }
            return null;
        }

        /// <summary>获取 Manager 的私有静态字段</summary>
        private object GetStaticField(string fieldName)
        {
            var type = GetManagerType();
            if (type == null) return null;
            try
            {
                var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null) return field.GetValue(null);
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 绘制控制面板
        /// </summary>
        /// <param name="logger">日志回调</param>
        /// <param name="headerStyle">标题样式</param>
        /// <param name="normalStyle">正文样式</param>
        /// <param name="buttonStyle">按钮样式</param>
        /// <param name="smallHintStyle">小提示样式</param>
        /// <param name="textFieldStyle">输入框样式</param>
        public void DrawPanel(Action<string> logger, GUIStyle headerStyle, GUIStyle normalStyle, GUIStyle buttonStyle, GUIStyle smallHintStyle, GUIStyle textFieldStyle)
        {
            // 保存外部传入的样式到字段，供 DrawRuleCard / DrawTextFieldWithHint 等辅助方法使用
            this.headerStyle = headerStyle;
            this.normalStyle = normalStyle;
            this.buttonStyle = buttonStyle;
            this.smallHintStyle = smallHintStyle;
            this.textFieldStyle = textFieldStyle;

            if (logger == null) logger = s => Debug.Log("[BossReplacePanel] " + s);

            // === 顶部说明区 ===
            GUILayout.Label("═══ Boss替换控制台 ═══", headerStyle);
            GUILayout.Label("Boss替换 = 在进入Boss战时，把原版Boss换成指定的Boss", smallHintStyle);
            GUILayout.Label("通过「规则」来决定：在什么条件下、把哪个Boss、换成哪个Boss", smallHintStyle);

            GUILayout.Space(10);

            object config;
            try
            {
                config = GetConfig();
            }
            catch (Exception ex)
            {
                GUILayout.Label("获取配置失败：" + ex.Message, GetErrorStyle());
                return;
            }

            if (config == null)
            {
                GUILayout.Label("错误：配置为 null", GetErrorStyle());
                return;
            }

            // === 什么是Boss替换 ===
            GUILayout.Label("═══ 什么是Boss替换 ═══", headerStyle);
            GUILayout.Label("Boss替换 = 在进入Boss战时，把原版Boss换成指定的Boss", normalStyle);
            GUILayout.Label("本Mod通过「规则」来决定：在什么条件下、把哪个Boss、换成哪个Boss", smallHintStyle);

            GUILayout.Space(5);

            // === 替换流程图 ===
            GUILayout.Label("═══ 替换流程图 ═══", headerStyle);
            GUILayout.Label("进入Boss战 → 检查每条规则 → 匹配成功 → 替换Boss", smallHintStyle);
            GUILayout.Label("                            ↓", smallHintStyle);
            GUILayout.Label("                      匹配失败 → 不替换，用原版Boss", smallHintStyle);

            GUILayout.Space(8);

            // === 顶部状态区 ===
            GUILayout.Label("═══ Mod状态 ═══", headerStyle);

            var configPath = GetStaticField("configPath") as string;
            GUILayout.Label("配置文件位置：", normalStyle);
            GUILayout.Label(configPath ?? "(未知)", smallHintStyle);

            var configType = config.GetType();
            var enabledField = configType.GetField("Enabled");
            var debugLogField = configType.GetField("DebugLog");

            if (enabledField == null || debugLogField == null)
            {
                GUILayout.Label("错误：配置字段未找到", GetErrorStyle());
                return;
            }

            bool enabled = (bool)enabledField.GetValue(config);
            bool debugLog = (bool)debugLogField.GetValue(config);

            GUILayout.BeginHorizontal();
            GUILayout.Label("总开关：" + (enabled ? "开启（替换生效）" : "关闭（不替换）"), normalStyle, GUILayout.Width(280));
            if (GUILayout.Button(enabled ? "关闭" : "开启", buttonStyle, GUILayout.Width(100), GUILayout.Height(38)))
            {
                try
                {
                    enabledField.SetValue(config, !enabled);
                    logger("已设置 总开关=" + !enabled);
                }
                catch (Exception ex)
                {
                    logger("设置总开关失败：" + ex.Message);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("调试日志：" + (debugLog ? "开启（输出详细日志）" : "关闭"), normalStyle, GUILayout.Width(280));
            if (GUILayout.Button(debugLog ? "关闭" : "开启", buttonStyle, GUILayout.Width(100), GUILayout.Height(38)))
            {
                try
                {
                    debugLogField.SetValue(config, !debugLog);
                    logger("已设置 调试日志=" + !debugLog);
                }
                catch (Exception ex)
                {
                    logger("设置调试日志失败：" + ex.Message);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // === 规则匹配逻辑说明 ===
            GUILayout.Label("═══ 规则匹配逻辑 ═══", headerStyle);
            GUILayout.Label("对每条启用的规则，按顺序检查：", smallHintStyle);
            GUILayout.Label("  ① 关卡键匹配？（留空=跳过此检查）→ 不匹配则看下一条", smallHintStyle);
            GUILayout.Label("  ② 原Boss匹配？（留空=跳过此检查）→ 不匹配则看下一条", smallHintStyle);
            GUILayout.Label("  ③ 匹配成功！→ 用此规则的新Boss替换", smallHintStyle);
            GUILayout.Label("  所有规则都不匹配 → 不替换", smallHintStyle);

            GUILayout.Space(8);

            // === 规则列表区 ===
            var rulesField = configType.GetField("Rules");
            if (rulesField == null)
            {
                GUILayout.Label("错误：规则字段未找到", GetErrorStyle());
                return;
            }

            IList rules;
            try
            {
                rules = rulesField.GetValue(config) as IList;
            }
            catch (Exception ex)
            {
                GUILayout.Label("获取规则失败：" + ex.Message, GetErrorStyle());
                return;
            }

            if (rules == null)
            {
                GUILayout.Label("规则列表为 null", normalStyle);
                return;
            }

            GUILayout.Label("═══ 替换规则 (" + rules.Count + "条) ═══", headerStyle);
            GUILayout.Label("默认规则含义：当触发「审判官事件」时，无论在哪个关卡、", smallHintStyle);
            GUILayout.Label("无论原Boss是谁，都替换成「犹大」", smallHintStyle);

            for (int i = 0; i < rules.Count; i++)
            {
                DrawRuleCard(rules, i, logger);
            }

            GUILayout.Space(8);

            // === 添加新规则区 ===
            GUILayout.Label("═══ 添加新规则 ═══", headerStyle);
            GUILayout.Label("填写以下字段后点「添加这条新规则」", smallHintStyle);

            newStageKey = DrawTextFieldWithHint("关卡键：", newStageKey,
                "在哪个关卡生效。填 Stage3 = 只在白色墓地。留空 = 所有关卡");
            newTriggerEventKey = DrawTextFieldWithHint("触发事件键：", newTriggerEventKey,
                "由哪个事件触发。填 RE_TheInquisition = 审判官事件时触发。留空 = 进入Boss战就替换");
            newOriginalBossQueue = DrawTextFieldWithHint("原Boss队列：", newOriginalBossQueue,
                "要替换掉哪个Boss。留空 = 替换所有Boss");
            newNewBossQueue = DrawTextFieldWithHint("新Boss队列：", newNewBossQueue,
                "替换成哪个Boss。Queue_FanaticBoss = 异端审判官犹大");

            GUILayout.BeginHorizontal();
            newReplaceEnabled = GUILayout.Toggle(newReplaceEnabled, "启用此规则");
            if (GUILayout.Button("添加这条新规则", buttonStyle, GUILayout.Height(42)))
            {
                try
                {
                    var ruleType = FindRuleType();
                    if (ruleType == null)
                    {
                        logger("错误：BossReplaceRule 类型未找到");
                    }
                    else
                    {
                        var rule = Activator.CreateInstance(ruleType);
                        ruleType.GetField("StageKey").SetValue(rule, newStageKey ?? "");
                        ruleType.GetField("TriggerEventKey").SetValue(rule, newTriggerEventKey ?? "");
                        ruleType.GetField("OriginalBossQueue").SetValue(rule, newOriginalBossQueue ?? "");
                        ruleType.GetField("NewBossQueue").SetValue(rule, newNewBossQueue ?? "");
                        ruleType.GetField("ReplaceEnabled").SetValue(rule, newReplaceEnabled);
                        rules.Add(rule);
                        logger("已添加新规则：触发事件=" + newTriggerEventKey + ", 新Boss=" + newNewBossQueue);
                    }
                }
                catch (Exception ex)
                {
                    logger("添加规则失败：" + ex.Message);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // === 操作按钮区 ===
            GUILayout.Label("═══ 操作按钮 ═══", headerStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("保存到配置文件", buttonStyle, GUILayout.Height(44)))
            {
                try
                {
                    InvokeMethod("SaveConfig");
                    logger("配置已保存到：" + (configPath ?? "(未知)"));
                }
                catch (Exception ex)
                {
                    logger("保存失败：" + ex.Message);
                }
            }
            if (GUILayout.Button("从配置文件重新加载", buttonStyle, GUILayout.Height(44)))
            {
                try
                {
                    InvokeMethod("LoadConfig");
                    logger("配置已重新加载");
                }
                catch (Exception ex)
                {
                    logger("加载失败：" + ex.Message);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("保存：把当前规则写入JSON文件（永久生效）", smallHintStyle);
            GUILayout.Label("重新加载：从JSON文件读取规则（放弃当前修改）", smallHintStyle);

            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("测试：当前关卡会替换成什么", buttonStyle, GUILayout.Height(44)))
            {
                try
                {
                    string stage = GetCurrentStageKey();
                    string result = InvokeMethod("CheckReplace", stage, "Queue_Test") as string;
                    if (string.IsNullOrEmpty(result))
                        logger("测试结果：当前关卡不匹配任何规则");
                    else
                        logger("测试结果：会替换为 " + result);
                }
                catch (Exception ex)
                {
                    logger("测试失败：" + ex.Message);
                }
            }
            if (GUILayout.Button("一键测试完整流程", buttonStyle, GUILayout.Height(44)))
            {
                OneClickVerify(logger);
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("测试：检查当前关卡是否匹配规则", smallHintStyle);
            GUILayout.Label("一键测试：跳转Stage3 + 设置标志 + 显示EnemyQueue", smallHintStyle);

            GUILayout.Space(4);

            if (GUILayout.Button("显示配置文件位置", buttonStyle, GUILayout.Height(40)))
            {
                logger("BossReplaceConfig.json 路径：");
                logger("  " + (configPath ?? "(未知)"));
            }

            GUILayout.Space(8);

            // === 使用教程 ===
            GUILayout.Label("═══ 使用教程 ═══", headerStyle);

            GUILayout.Label("方式1：用默认规则（最简单）", normalStyle);
            GUILayout.Label("  1. 确保总开关=开启", smallHintStyle);
            GUILayout.Label("  2. 点「一键测试完整流程」", smallHintStyle);
            GUILayout.Label("  3. 进入Boss战 → 犹大出现", smallHintStyle);

            GUILayout.Space(3);

            GUILayout.Label("方式2：自定义规则", normalStyle);
            GUILayout.Label("  1. 编辑上方规则的字段（每项下方有说明）", smallHintStyle);
            GUILayout.Label("  2. 点「保存到配置文件」", smallHintStyle);
            GUILayout.Label("  3. 进入对应关卡的Boss战", smallHintStyle);

            GUILayout.Space(3);

            GUILayout.Label("方式3：添加新规则", normalStyle);
            GUILayout.Label("  1. 在下方填写5个字段", smallHintStyle);
            GUILayout.Label("  2. 点「添加这条新规则」", smallHintStyle);
            GUILayout.Label("  3. 点「保存到配置文件」", smallHintStyle);
            GUILayout.Label("  4. 进入对应关卡的Boss战", smallHintStyle);

            GUILayout.Space(5);

            GUILayout.Label("═══ 与通用调试标签页的区别 ═══", headerStyle);
            GUILayout.Label("通用调试的「暴力替换」= 直接改队列，立即生效，不管规则", smallHintStyle);
            GUILayout.Label("本标签页的「规则替换」= 按规则匹配，进入Boss战时自动触发", smallHintStyle);
            GUILayout.Label("推荐：用本标签页的规则方式，更可控", smallHintStyle);
        }

        /// <summary>绘制带标签和解释的文本输入框</summary>
        private string DrawTextFieldWithHint(string label, string value, string hint)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, normalStyle, GUILayout.Width(140));
            string newValue = GUILayout.TextField(value ?? "", textFieldStyle);
            GUILayout.EndHorizontal();
            GUILayout.Label("  " + hint, smallHintStyle);
            return newValue;
        }

        /// <summary>绘制单条规则卡片</summary>
        private void DrawRuleCard(IList rules, int index, Action<string> logger)
        {
            object rule = rules[index];
            var ruleType = rule.GetType();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("规则 #" + (index + 1), headerStyle);

            var ruleEnabledField = ruleType.GetField("ReplaceEnabled");
            var stageKeyField = ruleType.GetField("StageKey");
            var triggerEventKeyField = ruleType.GetField("TriggerEventKey");
            var originalBossQueueField = ruleType.GetField("OriginalBossQueue");
            var newBossQueueField = ruleType.GetField("NewBossQueue");

            if (ruleEnabledField != null)
            {
                bool ruleEnabled = (bool)ruleEnabledField.GetValue(rule);
                GUILayout.BeginHorizontal();
                bool newEnabled = GUILayout.Toggle(ruleEnabled, "启用此规则");
                if (newEnabled != ruleEnabled)
                {
                    try
                    {
                        ruleEnabledField.SetValue(rule, newEnabled);
                        logger("规则[" + index + "] 启用=" + newEnabled);
                    }
                    catch (Exception ex)
                    {
                        logger("错误：" + ex.Message);
                    }
                }
                if (GUILayout.Button("删除此规则", buttonStyle, GUILayout.Width(120), GUILayout.Height(36)))
                {
                    try
                    {
                        rules.RemoveAt(index);
                        logger("规则 #" + (index + 1) + " 已删除");
                    }
                    catch (Exception ex)
                    {
                        logger("删除失败：" + ex.Message);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    return;
                }
                GUILayout.EndHorizontal();
            }

            if (stageKeyField != null)
            {
                stageKeyField.SetValue(rule, DrawTextFieldWithHint("关卡键：",
                    (string)stageKeyField.GetValue(rule),
                    "在哪个关卡生效。填 Stage3 = 只在白色墓地。留空 = 所有关卡"));
            }
            if (triggerEventKeyField != null)
            {
                triggerEventKeyField.SetValue(rule, DrawTextFieldWithHint("触发事件键：",
                    (string)triggerEventKeyField.GetValue(rule),
                    "由哪个事件触发。填 RE_TheInquisition = 审判官事件时触发。留空 = 进入Boss战就替换"));
            }
            if (originalBossQueueField != null)
            {
                originalBossQueueField.SetValue(rule, DrawTextFieldWithHint("原Boss队列：",
                    (string)originalBossQueueField.GetValue(rule),
                    "要替换掉哪个Boss。留空 = 替换所有Boss"));
            }
            if (newBossQueueField != null)
            {
                newBossQueueField.SetValue(rule, DrawTextFieldWithHint("新Boss队列：",
                    (string)newBossQueueField.GetValue(rule),
                    "替换成哪个Boss。Queue_FanaticBoss = 异端审判官犹大"));
            }

            GUILayout.EndVertical();
        }

        /// <summary>查找 BossReplaceRule 类型</summary>
        private Type FindRuleType()
        {
            try
            {
                var t = Type.GetType("BossReplaceMod.BossReplaceRule");
                if (t != null) return t;

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var rt = asm.GetType("BossReplaceMod.BossReplaceRule");
                        if (rt != null) return rt;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        /// <summary>获取当前关卡 Key</summary>
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
                        var stageData = field.GetValue(StageSystem.instance) as GameDataEditor.GDEStageData;
                        if (stageData != null) return stageData.Key;
                    }
                }
            }
            catch { }
            return "(未知)";
        }

        /// <summary>一键验证流程：跳转 Stage3 + 设置标志</summary>
        private void OneClickVerify(Action<string> logger)
        {
            try
            {
                // 1. 跳转 Stage3
                var fsType = typeof(FieldSystem);
                object instance = null;
                var instanceField = fsType.GetField("instance",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (instanceField != null) instance = instanceField.GetValue(null);

                if (instance == null)
                {
                    var instanceProp = fsType.GetProperty("Instance",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (instanceProp != null) instance = instanceProp.GetValue(null, null);
                }

                if (instance == null)
                {
                    logger("错误：FieldSystem.instance 为 null");
                    return;
                }

                var stageStartMethod = fsType.GetMethod("StageStart",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (stageStartMethod != null)
                {
                    stageStartMethod.Invoke(instance, new object[] { "Stage3" });
                    logger("已跳转到 Stage3");
                }

                // 2. 设置 TheInquisition=true
                PlayData.TSavedata.TheInquisition = true;
                logger("已设置 TheInquisition=true");

                // 3. 给予审判官专属装备（对应游戏 RE_TheInquisition.OnlyEvent 行为）
                GiveInquisitionTorch(logger);

                // 4. 设置 JohanQuestClear=true (修复游戏bug)
                SaveManager.NowData.storydata.JohanQuestClear = true;
                logger("已设置 JohanQuestClear=true");

                // 5. 显示当前 EnemyQueue
                var ssType = typeof(StageSystem);
                var enemyQueueField = ssType.GetField("EnemyQueue",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (enemyQueueField != null && StageSystem.instance != null)
                {
                    var queue = enemyQueueField.GetValue(StageSystem.instance) as System.Collections.Generic.List<GameDataEditor.GDEEnemyQueueData>;
                    if (queue != null)
                    {
                        logger("当前敌人队列数量：" + queue.Count);
                        for (int i = 0; i < queue.Count; i++)
                        {
                            logger("  [" + i + "] " + queue[i].Key);
                        }
                    }
                }

                logger("现在可以进入Boss战观察替换效果");
            }
            catch (Exception ex)
            {
                logger("一键验证失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 给予审判官专属装备（异端审判官火炬）
        /// 用反射读取 GDEItemKeys.Item_Equip_Torch_FanaticBoss 的运行时值
        /// </summary>
        private void GiveInquisitionTorch(Action<string> logger)
        {
            try
            {
                var fieldInfo = typeof(GameDataEditor.GDEItemKeys).GetField("Item_Equip_Torch_FanaticBoss",
                    BindingFlags.Public | BindingFlags.Static);
                if (fieldInfo == null)
                {
                    logger("警告：找不到 GDEItemKeys.Item_Equip_Torch_FanaticBoss 字段");
                    return;
                }

                string itemId = fieldInfo.GetValue(null) as string;
                if (string.IsNullOrEmpty(itemId))
                {
                    logger("警告：GDEItemKeys.Item_Equip_Torch_FanaticBoss 值为空");
                    return;
                }

                logger("调试：装备ID = '" + itemId + "'");

                ItemBase item = ItemBase.GetItem(itemId);
                if (item == null)
                {
                    logger("警告：GetItem 返回 null（GDE 数据未加载此装备）");
                    return;
                }

                InventoryManager.Reward(item);
                logger("已给予装备：" + itemId);
            }
            catch (Exception ex)
            {
                logger("给予装备异常：" + ex.Message);
            }
        }

        /// <summary>获取错误样式（带缓存，避免每帧创建）</summary>
        private GUIStyle GetErrorStyle()
        {
            if (errorStyleInitialized) return errorStyle;

            errorStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            errorStyle.normal.textColor = Color.red;
            errorStyleInitialized = true;
            return errorStyle;
        }
    }
}
