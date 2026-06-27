using System;
using UnityEngine;

namespace ScrollConsoleMod
{
    /// <summary>
    /// 卷轴获取控制台内容 - F3 窗口
    /// 提供所有 11 种卷轴的选择、数量输入、获取按钮
    /// 样式由外部 ScrollConsoleBehaviour 通过 DrawContent 参数传入
    /// </summary>
    public class ScrollConsoleContent
    {
        /// <summary>卷轴信息</summary>
        private struct ScrollInfo
        {
            public string Key;
            public string DisplayName;
            public string Description;
            public ScrollInfo(string key, string displayName, string description)
            {
                Key = key;
                DisplayName = displayName;
                Description = description;
            }
        }

        // 11 种卷轴列表
        private static readonly ScrollInfo[] ScrollList = new ScrollInfo[]
        {
            new ScrollInfo("Scroll_Identify", "鉴定卷轴", "可以鉴定物品"),
            new ScrollInfo("Scroll_Mapping", "地图制作卷轴", "点亮所有区域"),
            new ScrollInfo("Scroll_Item", "物品卷轴", "可以获得物品"),
            new ScrollInfo("Scroll_Enchant", "附魔卷轴", "可以附魔装备物品"),
            new ScrollInfo("Scroll_Quick", "神速卷轴", "本关增加速度"),
            new ScrollInfo("Scroll_Uncurse", "解诅咒卷轴", "解除装备诅咒"),
            new ScrollInfo("Scroll_Purification", "净化卷轴", "解除诅咒并消除负面效果"),
            new ScrollInfo("Scroll_Teleport", "瞬间移动卷轴", "移动到随机位置"),
            new ScrollInfo("Scroll_Transfer", "传输卷轴", "将物资传送到据点"),
            new ScrollInfo("Scroll_Vitality", "活力卷轴", "下次战斗随机获得技能"),
            new ScrollInfo("Scroll_Midas", "迈达斯卷轴", "将物品换成金币")
        };

        private int selectedScrollIndex = 1; // 默认地图制作卷轴
        private string countText = "1";

        /// <summary>
        /// 绘制卷轴获取内容
        /// 注意参数顺序：headerStyle, normalStyle, buttonStyle, smallHintStyle, textFieldStyle, logger
        /// </summary>
        public void DrawContent(
            GUIStyle headerStyle, GUIStyle normalStyle,
            GUIStyle buttonStyle, GUIStyle smallHintStyle,
            GUIStyle textFieldStyle, Action<string> logger)
        {
            // === 说明区 ===
            GUILayout.Label("═══ 卷轴获取控制台 ═══", headerStyle);
            GUILayout.Label("选择卷轴类型和数量，点击获取按钮添加到背包", smallHintStyle);

            GUILayout.Space(10);

            // === 选择卷轴类型 ===
            GUILayout.Label("═══ 选择卷轴类型 ═══", headerStyle);

            // 2列网格显示卷轴
            string[] displayNames = new string[ScrollList.Length];
            for (int i = 0; i < ScrollList.Length; i++)
            {
                displayNames[i] = ScrollList[i].DisplayName;
            }

            selectedScrollIndex = GUILayout.SelectionGrid(
                selectedScrollIndex, displayNames, 2, buttonStyle,
                GUILayout.Height(40 * ((ScrollList.Length + 1) / 2)));

            GUILayout.Space(6);

            // 显示选中卷轴的说明
            GUILayout.Label("当前选中：" + ScrollList[selectedScrollIndex].DisplayName, normalStyle);
            GUILayout.Label("说明：" + ScrollList[selectedScrollIndex].Description, smallHintStyle);
            GUILayout.Label("Key：" + ScrollList[selectedScrollIndex].Key, smallHintStyle);

            GUILayout.Space(10);

            // === 选择数量 ===
            GUILayout.Label("═══ 选择数量（1-99）═══", headerStyle);

            GUILayout.BeginHorizontal();
            countText = GUILayout.TextField(countText, textFieldStyle, GUILayout.Width(120), GUILayout.Height(40));

            if (GUILayout.Button("+1", buttonStyle, GUILayout.Width(60), GUILayout.Height(40)))
            {
                int c = ParseCount();
                if (c < 99) countText = (c + 1).ToString();
            }
            if (GUILayout.Button("+5", buttonStyle, GUILayout.Width(60), GUILayout.Height(40)))
            {
                int c = ParseCount();
                if (c + 5 <= 99) countText = (c + 5).ToString();
                else countText = "99";
            }
            if (GUILayout.Button("+10", buttonStyle, GUILayout.Width(65), GUILayout.Height(40)))
            {
                int c = ParseCount();
                if (c + 10 <= 99) countText = (c + 10).ToString();
                else countText = "99";
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // === 获取按钮 ===
            if (GUILayout.Button("获取卷轴", buttonStyle, GUILayout.Height(48)))
            {
                GiveScrolls(logger);
            }
            GUILayout.Label("点击后将选中类型的卷轴添加到背包", smallHintStyle);
        }

        /// <summary>解析数量输入</summary>
        private int ParseCount()
        {
            int count;
            if (!int.TryParse(countText, out count))
            {
                count = 1;
                countText = "1";
            }
            if (count < 1) { count = 1; countText = "1"; }
            if (count > 99) { count = 99; countText = "99"; }
            return count;
        }

        /// <summary>获取卷轴</summary>
        private void GiveScrolls(Action<string> logger)
        {
            try
            {
                var scroll = ScrollList[selectedScrollIndex];
                int count = ParseCount();

                Debug.Log("[ScrollConsole] === 开始获取卷轴 ===");
                Debug.Log("[ScrollConsole] 类型：" + scroll.DisplayName + " (" + scroll.Key + ")");
                Debug.Log("[ScrollConsole] 数量：" + count);

                if (PartyInventory.InvenM == null)
                {
                    logger("错误：背包未初始化（需要先进入游戏）");
                    return;
                }

                int success = 0;
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        bool ok = PartyInventory.InvenM.AddNewItem(scroll.Key);
                        if (ok) success++;
                        Debug.Log("[ScrollConsole] 第 " + (i + 1) + " 个: " + (ok ? "成功" : "失败"));
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("[ScrollConsole] 第 " + (i + 1) + " 个异常: " + ex.Message);
                    }
                }

                logger(string.Format("已获取 {0}/{1} 个 {2}", success, count, scroll.DisplayName));
                Debug.Log("[ScrollConsole] === 获取完成：成功 " + success + "/" + count + " ===");
            }
            catch (Exception ex)
            {
                logger("获取异常：" + ex.Message);
                Debug.Log("[ScrollConsole] GiveScrolls 异常: " + ex);
            }
        }
    }
}
