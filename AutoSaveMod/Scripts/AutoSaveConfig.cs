using System;
using System.Collections.Generic;

namespace AutoSaveMod
{
    /// <summary>
    /// 自动存档配置（JSON 序列化）
    /// </summary>
    [Serializable]
    public class AutoSaveConfig
    {
        /// <summary>最大存档数（1-20）</summary>
        public int MaxSaves = 5;

        /// <summary>战斗结束自动存档</summary>
        public bool AutoSaveOnBattleEnd = true;

        /// <summary>进入关卡自动存档</summary>
        public bool AutoSaveOnStageStart = true;

        /// <summary>退出游戏自动存档</summary>
        public bool AutoSaveOnQuit = true;

        /// <summary>关卡进入后延迟存档秒数（防篝火崩溃）</summary>
        public float StageSaveDelay = 2.0f;

        public static AutoSaveConfig GetDefault()
        {
            return new AutoSaveConfig();
        }
    }

    /// <summary>
    /// 角色信息（用于存档列表显示）
    /// </summary>
    [Serializable]
    public class CharInfo
    {
        /// <summary>角色名</summary>
        public string Name = "";

        /// <summary>角色等级</summary>
        public int Level = 0;

        /// <summary>角色 KeyData（可用于加载图标）</summary>
        public string KeyData = "";
    }

    /// <summary>
    /// 存档备份元数据（每个备份目录的 info.json）
    /// </summary>
    [Serializable]
    public class SaveBackupInfo
    {
        /// <summary>备份目录名（时间戳_原因）</summary>
        public string DirName = "";

        /// <summary>触发原因</summary>
        public string Trigger = "";

        /// <summary>保存时间（可读格式）</summary>
        public string SaveTime = "";

        /// <summary>循环数</summary>
        public int LoopNum = 0;

        /// <summary>当前关卡 Key</summary>
        public string StageKey = "";

        /// <summary>关卡编号</summary>
        public int StageNum = 0;

        /// <summary>金币</summary>
        public int Gold = 0;

        /// <summary>灵魂石</summary>
        public int Soul = 0;

        /// <summary>游戏模式</summary>
        public string PlayMode = "";

        /// <summary>是否血雾模式</summary>
        public bool HopeMode = false;

        /// <summary>血雾等级</summary>
        public int HopeModeLevel = 0;

        /// <summary>血雾难度等级</summary>
        public int BloodyMistLevel = 0;

        /// <summary>游戏时间（ticks）</summary>
        public long TimeTick = 0;

        /// <summary>出战角色列表</summary>
        public List<CharInfo> PartyInfo = new List<CharInfo>();

        /// <summary>Save1.sav 文件大小（KB）</summary>
        public long Save1SizeKB = 0;

        /// <summary>Save0.sav 文件大小（KB）</summary>
        public long Save0SizeKB = 0;
    }
}
