using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AutoSaveMod
{
    /// <summary>
    /// 自动存档核心管理器
    /// 负责配置加载/保存、存档/读档操作、备份管理
    /// </summary>
    public static class AutoSaveManager
    {
        private static AutoSaveConfig config;
        private static string configPath;
        private static string backupRootDir;

        /// <summary>配置是否已加载</summary>
        public static bool IsLoaded { get; private set; }

        /// <summary>当前配置</summary>
        public static AutoSaveConfig Config
        {
            get
            {
                if (!IsLoaded) LoadConfig();
                return config;
            }
        }

        /// <summary>备份根目录</summary>
        public static string BackupRootDir
        {
            get
            {
                if (string.IsNullOrEmpty(backupRootDir))
                    backupRootDir = Path.Combine(Application.persistentDataPath, "AutoSaves");
                return backupRootDir;
            }
        }

        // ============ 存档文件路径 ============

        /// <summary>主存档路径（NewSaveData）</summary>
        public static string GetSave1Path()
        {
            return Path.Combine(Application.persistentDataPath, "Save1.sav");
        }

        /// <summary>临时存档路径（TempSaveData）</summary>
        public static string GetSave0Path()
        {
            return Path.Combine(Application.persistentDataPath, "Save0.sav");
        }

        // ============ 初始化 ============

        public static void Initialize()
        {
            configPath = Path.Combine(Application.persistentDataPath, "AutoSaveConfig.json");
            backupRootDir = Path.Combine(Application.persistentDataPath, "AutoSaves");
            LoadConfig();

            // 确保备份目录存在
            try
            {
                if (!Directory.Exists(backupRootDir))
                {
                    Directory.CreateDirectory(backupRootDir);
                    Debug.Log("[AutoSaveMod] 备份目录已创建: " + backupRootDir);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] 创建备份目录异常: " + ex.Message);
            }

            Debug.Log("[AutoSaveMod] Manager 初始化完成, MaxSaves=" + config.MaxSaves);
        }

        // ============ 配置管理 ============

        public static void LoadConfig()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonConvert.DeserializeObject<AutoSaveConfig>(json);
                    // 钳制 MaxSaves 到合法范围
                    if (config.MaxSaves < 1) config.MaxSaves = 1;
                    if (config.MaxSaves > 20) config.MaxSaves = 20;
                    Debug.Log("[AutoSaveMod] 配置已加载: " + configPath);
                }
                else
                {
                    config = AutoSaveConfig.GetDefault();
                    SaveConfig();
                    Debug.Log("[AutoSaveMod] 默认配置已创建: " + configPath);
                }
                IsLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] 加载配置异常: " + ex.Message);
                config = AutoSaveConfig.GetDefault();
                IsLoaded = true;
            }
        }

        public static void SaveConfig()
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, json);
                Debug.Log("[AutoSaveMod] 配置已保存");
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] 保存配置异常: " + ex.Message);
            }
        }

        // ============ 自动存档 ============

        /// <summary>
        /// 执行自动存档
        /// 1. 调用游戏原生 Save() 保存当前状态
        /// 2. 复制 Save1.sav + Save0.sav 到备份目录
        /// 3. 清理超额旧备份
        /// </summary>
        public static void PerformAutoSave(string triggerReason)
        {
            try
            {
                Debug.Log("[AutoSaveMod] ========== 自动存档开始（触发：" + triggerReason + "）==========");

                // 1. 调用游戏原生保存
                SaveCurrentGame();

                // 2. 创建备份
                CreateBackup(triggerReason);

                // 3. 清理旧备份
                CleanupOldBackups();

                Debug.Log("[AutoSaveMod] ========== 自动存档完成 ==========");
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] 自动存档异常: " + ex.Message);
                Debug.LogError("[AutoSaveMod] StackTrace: " + ex.StackTrace);
            }
        }

        /// <summary>
        /// 调用游戏原生保存方法
        /// 【关键修复】使用 SaveManager.ProgressOneSave() 同时保存 Save1.sav 和 Save0.sav
        ///
        /// IL 分析证实：
        ///   - SaveManager.Save() 只写 Save1.sav，不写 Save0.sav
        ///   - TempSaveData.Save() 也不写 Save0.sav（它内部调用 SaveManager.Save() 写 Save1.sav）
        ///   - 只有 SaveManager.ProgressOneSave() 会同时写两个存档：
        ///     1. TempSave.Save() → SaveManager.Save() → 写 Save1.sav
        ///     2. XMLSerialize(TempSave, DataPath2_Steam) → 加密写 Save0.sav
        ///
        /// 之前调用 Save() + TempSaveData.Save() 导致 Save0.sav 从未被写入，
        /// 备份的 Save0.sav 始终是 19.63 KB 的空存档，读档后 Party.Count==0，
        /// FieldSystem.Load() 协程走 LucyRoomInit() 分支，回到最开始场景。
        /// </summary>
        private static void SaveCurrentGame()
        {
            try
            {
                var smType = typeof(SaveManager);
                var smField = smType.GetField("savemanager",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                if (smField == null)
                {
                    Debug.LogError("[AutoSaveMod] 找不到 SaveManager.savemanager 字段");
                    return;
                }

                var smInstance = smField.GetValue(null) as SaveManager;
                if (smInstance == null)
                {
                    Debug.LogError("[AutoSaveMod] SaveManager.savemanager 为 null（游戏未初始化）");
                    return;
                }

                // 调用 ProgressOneSave() 同时保存主存档（Save1.sav）和临时存档（Save0.sav）
                var progressOneSaveMethod = smType.GetMethod("ProgressOneSave",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (progressOneSaveMethod != null)
                {
                    // 保存前诊断日志（确认保存时 PlayData.TSavedata 状态有效）
                    if (PlayData.TSavedata != null)
                    {
                        int prePartyCount = PlayData.TSavedata.Party != null ? PlayData.TSavedata.Party.Count : 0;
                        Debug.Log("[AutoSaveMod] 保存前状态：NowStageMapKey=" + PlayData.TSavedata.NowStageMapKey
                            + ", StageNum=" + PlayData.TSavedata.StageNum
                            + ", Party.Count=" + prePartyCount);
                    }

                    progressOneSaveMethod.Invoke(smInstance, null);
                    Debug.Log("[AutoSaveMod] 已调用 SaveManager.ProgressOneSave()（Save1.sav + Save0.sav 已同时保存）");
                }
                else
                {
                    Debug.LogError("[AutoSaveMod] 找不到 SaveManager.ProgressOneSave() 方法，使用回退方案");
                    FallbackSave(smInstance, smType);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] SaveCurrentGame 异常: " + ex.Message);
                Debug.LogError("[AutoSaveMod] StackTrace: " + ex.StackTrace);
            }
        }

        /// <summary>
        /// 回退保存方案（仅当 ProgressOneSave 不可用时使用）
        /// 手动调用 Save() + 反射调用 XMLSerialize 写 Save0.sav
        /// </summary>
        private static void FallbackSave(SaveManager smInstance, Type smType)
        {
            try
            {
                Debug.LogWarning("[AutoSaveMod] 使用回退保存方案（Save + 手动 XMLSerialize）");

                // 1. 调用 Save() 写 Save1.sav
                var saveMethod = smType.GetMethod("Save",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null, Type.EmptyTypes, null);
                saveMethod?.Invoke(smInstance, null);
                Debug.Log("[AutoSaveMod] 已调用 SaveManager.Save()（Save1.sav 已保存）");

                // 2. 同步 TempSave = PlayData.TSavedata
                var tempSaveField = smType.GetField("TempSave",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (tempSaveField != null && PlayData.TSavedata != null)
                {
                    tempSaveField.SetValue(smInstance, PlayData.TSavedata);
                    Debug.Log("[AutoSaveMod] 已同步 SaveManager.TempSave = PlayData.TSavedata");
                }

                // 3. 反射调用 XMLSerialize(TempSaveData, string) 写 Save0.sav（加密）
                var dataPath2Field = smType.GetField("DataPath2_Steam",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var xmlSerializeMethod = smType.GetMethod("XMLSerialize",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null, new Type[] { typeof(TempSaveData), typeof(string) }, null);

                if (dataPath2Field != null && xmlSerializeMethod != null && PlayData.TSavedata != null)
                {
                    string save0Path = dataPath2Field.GetValue(smInstance) as string;
                    xmlSerializeMethod.Invoke(smInstance, new object[] { PlayData.TSavedata, save0Path });
                    Debug.Log("[AutoSaveMod] 已调用 XMLSerialize(TempSaveData, DataPath2_Steam)（Save0.sav 已加密保存）");
                }
                else
                {
                    Debug.LogError("[AutoSaveMod] 回退方案失败：找不到必要字段或方法（DataPath2_Steam / XMLSerialize）");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] FallbackSave 异常: " + ex.Message);
                Debug.LogError("[AutoSaveMod] StackTrace: " + ex.StackTrace);
            }
        }

        // ============ 备份管理 ============

        /// <summary>创建备份（复制 Save1.sav + Save0.sav 到备份目录）</summary>
        private static void CreateBackup(string triggerReason)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string safeReason = SanitizeDirName(triggerReason);
                string backupDirName = timestamp + "_" + safeReason;
                string backupDir = Path.Combine(BackupRootDir, backupDirName);

                Directory.CreateDirectory(backupDir);

                string save1Src = GetSave1Path();
                string save0Src = GetSave0Path();
                string save1Dst = Path.Combine(backupDir, "Save1.sav");
                string save0Dst = Path.Combine(backupDir, "Save0.sav");

                long save1Size = 0, save0Size = 0;

                // 复制 Save1.sav（主存档）
                if (File.Exists(save1Src))
                {
                    File.Copy(save1Src, save1Dst, true);
                    save1Size = new FileInfo(save1Dst).Length / 1024;
                    Debug.Log("[AutoSaveMod] 已备份 Save1.sav (" + save1Size + " KB)");
                }
                else
                {
                    Debug.LogWarning("[AutoSaveMod] Save1.sav 不存在，跳过主存档备份");
                }

                // 复制 Save0.sav（临时存档）
                if (File.Exists(save0Src))
                {
                    File.Copy(save0Src, save0Dst, true);
                    save0Size = new FileInfo(save0Dst).Length / 1024;
                    Debug.Log("[AutoSaveMod] 已备份 Save0.sav (" + save0Size + " KB)");
                }
                else
                {
                    Debug.LogWarning("[AutoSaveMod] Save0.sav 不存在，跳过临时存档备份");
                }

                // 创建 info.json 元数据
                var info = new SaveBackupInfo
                {
                    DirName = backupDirName,
                    Trigger = triggerReason,
                    SaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    LoopNum = SafeGetLoopNum(),
                    StageKey = SafeGetStageKey(),
                    StageNum = SafeGetStageNum(),
                    Gold = SafeGetGold(),
                    Soul = SafeGetSoul(),
                    PlayMode = SafeGetPlayMode(),
                    HopeMode = SafeGetHopeMode(),
                    HopeModeLevel = SafeGetHopeModeLevel(),
                    BloodyMistLevel = SafeGetBloodyMistLevel(),
                    TimeTick = SafeGetTimeTick(),
                    PartyInfo = SafeGetPartyInfo(),
                    Save1SizeKB = save1Size,
                    Save0SizeKB = save0Size
                };

                string infoPath = Path.Combine(backupDir, "info.json");
                string infoJson = JsonConvert.SerializeObject(info, Formatting.Indented);
                File.WriteAllText(infoPath, infoJson);

                // 备份有效性验证日志（对比运行时状态与备份元数据）
                int runtimePartyCount = (PlayData.TSavedata != null && PlayData.TSavedata.Party != null)
                    ? PlayData.TSavedata.Party.Count : 0;
                string runtimeStageMapKey = (PlayData.TSavedata != null)
                    ? PlayData.TSavedata.NowStageMapKey : "null";
                Debug.Log("[AutoSaveMod] 备份已创建: " + backupDirName
                    + " (关卡=" + info.StageKey + "(运行时=" + runtimeStageMapKey + ")"
                    + ", StageNum=" + info.StageNum
                    + ", 金币=" + info.Gold + ", 灵魂石=" + info.Soul
                    + ", 角色=" + info.PartyInfo.Count + "(运行时=" + runtimePartyCount + "))");
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] CreateBackup 异常: " + ex.Message);
            }
        }

        /// <summary>清理超额旧备份（保留 MaxSaves 个最近的）</summary>
        private static void CleanupOldBackups()
        {
            try
            {
                var backups = GetBackupList();
                int maxSaves = config != null ? config.MaxSaves : 5;

                if (backups.Count <= maxSaves)
                {
                    return;
                }

                // 按目录名（时间戳）排序，旧的在前
                backups.Sort((a, b) => string.Compare(a.DirName, b.DirName, StringComparison.Ordinal));

                int toRemove = backups.Count - maxSaves;
                for (int i = 0; i < toRemove; i++)
                {
                    string dirPath = Path.Combine(BackupRootDir, backups[i].DirName);
                    if (Directory.Exists(dirPath))
                    {
                        Directory.Delete(dirPath, true);
                        Debug.Log("[AutoSaveMod] 已删除旧备份: " + backups[i].DirName);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] CleanupOldBackups 异常: " + ex.Message);
            }
        }

        /// <summary>获取所有备份列表（按时间倒序，最新的在前）</summary>
        public static List<SaveBackupInfo> GetBackupList()
        {
            var result = new List<SaveBackupInfo>();

            try
            {
                if (!Directory.Exists(BackupRootDir))
                    return result;

                var dirs = Directory.GetDirectories(BackupRootDir);
                foreach (var dir in dirs)
                {
                    string infoPath = Path.Combine(dir, "info.json");
                    if (File.Exists(infoPath))
                    {
                        try
                        {
                            string json = File.ReadAllText(infoPath);
                            var info = JsonConvert.DeserializeObject<SaveBackupInfo>(json);
                            if (info != null)
                            {
                                info.DirName = Path.GetFileName(dir);
                                result.Add(info);
                            }
                        }
                        catch
                        {
                            // info.json 损坏，创建基本信息
                            result.Add(new SaveBackupInfo
                            {
                                DirName = Path.GetFileName(dir),
                                Trigger = "(未知)",
                                SaveTime = "(损坏)"
                            });
                        }
                    }
                    else
                    {
                        // 无 info.json，从目录名推断
                        result.Add(new SaveBackupInfo
                        {
                            DirName = Path.GetFileName(dir),
                            Trigger = "(无元数据)",
                            SaveTime = "(未知)"
                        });
                    }
                }

                // 按目录名倒序（最新的在前）
                result.Sort((a, b) => string.Compare(b.DirName, a.DirName, StringComparison.Ordinal));
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] GetBackupList 异常: " + ex.Message);
            }

            return result;
        }

        // ============ 读档操作 ============

        /// <summary>快速读档（读取最近一次存档）</summary>
        public static bool QuickLoad()
        {
            var backups = GetBackupList();
            if (backups.Count == 0)
            {
                Debug.LogWarning("[AutoSaveMod] 没有可用的备份");
                return false;
            }

            return LoadFromBackup(backups[0].DirName);
        }

        /// <summary>从指定备份读档</summary>
        public static bool LoadFromBackup(string backupDirName)
        {
            try
            {
                string backupDir = Path.Combine(BackupRootDir, backupDirName);
                if (!Directory.Exists(backupDir))
                {
                    Debug.LogError("[AutoSaveMod] 备份目录不存在: " + backupDirName);
                    return false;
                }

                string save1Backup = Path.Combine(backupDir, "Save1.sav");
                string save0Backup = Path.Combine(backupDir, "Save0.sav");
                string save1Target = GetSave1Path();
                string save0Target = GetSave0Path();

                Debug.Log("[AutoSaveMod] ========== 读档开始（备份：" + backupDirName + "）==========");

                // 1. 复制备份文件覆盖当前存档
                if (File.Exists(save1Backup))
                {
                    File.Copy(save1Backup, save1Target, true);
                    Debug.Log("[AutoSaveMod] 已恢复 Save1.sav");
                }
                else
                {
                    Debug.LogError("[AutoSaveMod] 备份中 Save1.sav 不存在");
                    return false;
                }

                if (File.Exists(save0Backup))
                {
                    File.Copy(save0Backup, save0Target, true);
                    Debug.Log("[AutoSaveMod] 已恢复 Save0.sav");
                }
                else
                {
                    Debug.LogWarning("[AutoSaveMod] 备份中 Save0.sav 不存在（可能原存档就没有临时数据）");
                }

                // 2. 调用游戏加载方法
                LoadGameFromFiles();

                Debug.Log("[AutoSaveMod] ========== 读档完成 ==========");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] LoadFromBackup 异常: " + ex.Message);
                Debug.LogError("[AutoSaveMod] StackTrace: " + ex.StackTrace);
                return false;
            }
        }

        /// <summary>从文件加载游戏（调用 SaveManager.Load + TempSaveLoad + 场景重载）</summary>
        private static void LoadGameFromFiles()
        {
            try
            {
                var smType = typeof(SaveManager);
                var smField = smType.GetField("savemanager",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                if (smField == null)
                {
                    Debug.LogError("[AutoSaveMod] 找不到 SaveManager.savemanager 字段");
                    return;
                }

                var smInstance = smField.GetValue(null) as SaveManager;
                if (smInstance == null)
                {
                    Debug.LogError("[AutoSaveMod] SaveManager.savemanager 为 null");
                    return;
                }

                // 调用 Load() 加载主存档
                var loadMethod = smType.GetMethod("Load",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (loadMethod != null)
                {
                    loadMethod.Invoke(smInstance, null);
                    Debug.Log("[AutoSaveMod] 已调用 SaveManager.Load()（主存档已加载）");
                }

                // 【关键修复】不调用 TempSaveLoad()，改为直接调用 XMLDeserialize_TempData
                //
                // 原因：TempSaveLoad() 内部检查 StageNum==0，如果是 0 就丢弃反序列化数据创建新空对象：
                //   IL_008E: ldfld TempSaveData::StageNum
                //   IL_0093: brtrue.s IL_00A0  (StageNum != 0 → 保留)
                //   IL_0096: newobj TempSaveData::.ctor()  (StageNum==0 → 新建空对象)
                //   IL_009B: stfld SaveManager::TempSave   (丢弃反序列化结果)
                //
                // 但 StageNum==0 是第一章第一节的正常值（StageStart("") + StageNum==0 → StageKey=Stage_Stage1_1）
                // TempSaveData.Save() 不写入 StageNum，所以保存时 StageNum 完全取决于 PlayData.TSavedata.StageNum
                // 在第一章第一节，StageNum==0 是正常的，但 TempSaveLoad() 会把它当作无效存档丢弃
                //
                // 解决方案：直接调用 XMLDeserialize_TempData 反序列化 Save0.sav
                // 该方法是实例方法，内部会设置 SaveManager.TempSave 字段
                // 这样可以绕过 StageNum==0 检查，保留反序列化的完整数据
                var dataPath2Field = smType.GetField("DataPath2_Steam",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var deserializeTempMethod = smType.GetMethod("XMLDeserialize_TempData",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null, new Type[] { typeof(string), typeof(bool) }, null);

                if (dataPath2Field != null && deserializeTempMethod != null)
                {
                    string save0Path = dataPath2Field.GetValue(smInstance) as string;
                    if (File.Exists(save0Path))
                    {
                        var tempSave = deserializeTempMethod.Invoke(smInstance, new object[] { save0Path, false }) as TempSaveData;
                        if (tempSave != null)
                        {
                            int partyCount = tempSave.Party != null ? tempSave.Party.Count : 0;
                            Debug.Log("[AutoSaveMod] 已直接反序列化 Save0.sav（绕过 StageNum==0 检查）");
                            Debug.Log("[AutoSaveMod] TempSave 状态：NowStageMapKey=" + tempSave.NowStageMapKey
                                + ", StageNum=" + tempSave.StageNum
                                + ", Party.Count=" + partyCount);
                        }
                        else
                        {
                            Debug.LogError("[AutoSaveMod] 反序列化 Save0.sav 返回 null");
                        }
                    }
                    else
                    {
                        Debug.LogError("[AutoSaveMod] Save0.sav 不存在：" + save0Path);
                    }
                }
                else
                {
                    Debug.LogError("[AutoSaveMod] 找不到 DataPath2_Steam 字段或 XMLDeserialize_TempData 方法");
                }

                // 【关键修复】不手动调用 OneSaveLoad()！
                //
                // 原因：FieldSystem.Load() 协程内部会调用 OneSaveLoad()。如果我们也手动调用，
                // 会导致 OneSaveLoad() 被调用两次：
                //   第一次（手动）：V0 备份原始反序列化角色 → SavePassing_Load 成功
                //   第二次（协程内）：V0 备份第一次重建后的新角色 → SavePassing 不完整 → NRE
                //
                // FieldSystem.Load() 协程的调用链（只调用一次）：
                //   检查 TempSave.Party.Count != 0 → OneSaveLoad() → TempSaveData.Load():
                //     V0 = TempSave.Party（原始反序列化角色，SavePassing 完整）
                //     Clear Party → PartyAdd(V0[i].KeyData) → SavePassing_Load(V0[i]) → 成功
                //   → IsLoaded=true → LoadOneSaveMap() → IsLoaded=false → FadeBlack_In → SkinUpdate
                //
                // PlayData.TSavedata 的同步由 FieldSystem.Load() 协程内部的 OneSaveLoad() 完成，
                // 此处不需要手动同步。

                // 验证 TempSave 状态（PlayData.TSavedata 将在 FieldSystem.Load() 协程中同步）
                var tempSaveField = smType.GetField("TempSave",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var verifyTempSave = tempSaveField != null ? tempSaveField.GetValue(smInstance) as TempSaveData : null;
                if (verifyTempSave != null)
                {
                    int partyCount = verifyTempSave.Party != null ? verifyTempSave.Party.Count : 0;
                    Debug.Log("[AutoSaveMod] TempSave 验证：NowStageMapKey=" + verifyTempSave.NowStageMapKey
                        + ", StageNum=" + verifyTempSave.StageNum
                        + ", Party.Count=" + partyCount);
                }
                else
                {
                    Debug.LogWarning("[AutoSaveMod] TempSave 验证：TempSave 为 null");
                }

                // 重载当前场景以确保状态完整（FieldSystem.Load 协程会处理 OneSaveLoad）
                ReloadCurrentScene();
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] LoadGameFromFiles 异常: " + ex.Message);
            }
        }

        /// <summary>读档后处理场景与地图刷新</summary>
        private static void ReloadCurrentScene()
        {
            try
            {
                // 停止所有 AudioSource（防止 BGM 重复播放）
                StopAllAudioSources();

                Scene currentScene = SceneManager.GetActiveScene();
                string sceneName = currentScene.name;
                Debug.Log("[AutoSaveMod] 当前场景: " + sceneName + "，准备读档刷新");

                // 记录 TempSave 状态（PlayData.TSavedata 将在 FieldSystem.Load 协程中同步）
                var smType3 = typeof(SaveManager);
                var smField3 = smType3.GetField("savemanager",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var smInstance3 = smField3 != null ? smField3.GetValue(null) as SaveManager : null;
                var tempSaveField3 = smType3.GetField("TempSave",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var tempSave3 = smInstance3 != null && tempSaveField3 != null
                    ? tempSaveField3.GetValue(smInstance3) as TempSaveData : null;
                if (tempSave3 != null)
                {
                    int partyCount = tempSave3.Party != null ? tempSave3.Party.Count : 0;
                    Debug.Log("[AutoSaveMod] 读档前 TempSave 状态：NowStageMapKey=" + tempSave3.NowStageMapKey
                        + ", StageNum=" + tempSave3.StageNum
                        + ", Party.Count=" + partyCount);
                }

                if (sceneName == "Battle")
                {
                    // 战斗场景：重载到 Main 场景（让玩家从主菜单继续）
                    Debug.Log("[AutoSaveMod] 当前在战斗场景，重载到 Main 场景");
                    SceneManager.LoadScene("Main");
                }
                else if (sceneName == "Field")
                {
                    // Field 场景：不重载场景！直接刷新地图
                    // 关键教训：重载 Field 场景会触发 FieldSystem.Start() 协程，该协程会：
                    //   1. 无条件重置 StageNum = 0
                    //   2. 根据 MainStoryProgress 分6+个分支判断（可能走非 Load 分支）
                    //   3. 游戏自身执行 Save() + TempSaveLoad() 覆盖恢复的状态
                    // 正确做法：调用 ClearMap() 清空当前地图，然后调用 LoadOneSaveMap()
                    // 直接根据 PlayData.TSavedata.NowStageMapKey 加载存档所在地图
                    ReloadFieldMap();
                }
                else
                {
                    // 其他场景（Main 等）：重载到 Field 场景，让 FieldSystem.Load 协程处理
                    Debug.Log("[AutoSaveMod] 当前在 " + sceneName + " 场景，重载到 Field 场景");
                    SceneManager.LoadScene("Field");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] ReloadCurrentScene 异常: " + ex.Message);
                Debug.LogError("[AutoSaveMod] StackTrace: " + ex.StackTrace);
            }
        }

        /// <summary>
        /// 在 Field 场景内刷新地图（不重载场景）
        /// 【关键修复】使用游戏原生 FieldSystem.Load() 协程代替直接调用 LoadOneSaveMap()。
        ///
        /// 原因：直接调用 LoadOneSaveMap() 缺少 IsLoaded=true 守卫，会走灾难路径：
        ///   LoadOneSaveMap("Stage_Camp") → 不匹配 camp key 分支 → StageStart("")
        ///   → StageNum==0 → switch → StageKey=Stage_Stage1_1
        ///   → 新游戏初始化 → PlayData::init() 把 Party 覆盖为空列表（IL_0051 stfld Party = new List<Character>()）
        ///   → 进入战斗时 BattleActWindow.Update() 抛 ArgumentOutOfRangeException
        ///
        /// FieldSystem.Load() 协程包含完整守卫和 IsLoaded 信号灯：
        ///   1. WaitForSeconds(0.1f) 让一帧完成
        ///   2. 检查 SaveManager.savemanager.TempSave 的 null/Party.Count==0/DeletedFile/QuickRestart
        ///   3. 主分支：OneSaveLoad() → IsLoaded=true → LoadOneSaveMap() → IsLoaded=false → FadeBlack_In → SkinUpdate
        ///   4. 异常分支：LucyRoomInit()
        ///   5. 全程不调用 StageStart()，避免新游戏初始化清空 Party
        /// </summary>
        private static void ReloadFieldMap()
        {
            try
            {
                if (FieldSystem.instance == null)
                {
                    Debug.LogError("[AutoSaveMod] FieldSystem.instance 为 null，无法刷新地图");
                    return;
                }

                Debug.Log("[AutoSaveMod] 启动 FieldSystem.Load() 协程（包含守卫和 IsLoaded 信号灯）");
                FieldSystem.instance.StartCoroutine(FieldSystem.instance.Load());
                Debug.Log("[AutoSaveMod] 已启动 FieldSystem.Load() 协程");

                // 启动验证协程（延迟 0.5s 检查 Party 状态，确认修复是否生效）
                FieldSystem.instance.StartCoroutine(VerifyLoadResult());
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] ReloadFieldMap 异常: " + ex.Message);
                Debug.LogError("[AutoSaveMod] StackTrace: " + ex.StackTrace);
            }
        }

        /// <summary>
        /// 读档后验证协程：等待 Load() 协程完成后输出 Party 状态
        /// 用途：验证修复是否生效（Party.Count > 0 表示成功）
        /// </summary>
        private static IEnumerator VerifyLoadResult()
        {
            // 等待 Load() 协程完成（其内部有 0.1s WaitForSeconds + 主分支同步执行）
            yield return new WaitForSeconds(0.5f);

            try
            {
                if (PlayData.TSavedata != null)
                {
                    int partyCount = PlayData.TSavedata.Party != null ? PlayData.TSavedata.Party.Count : 0;
                    string stageMapKey = PlayData.TSavedata.NowStageMapKey;
                    int stageNum = PlayData.TSavedata.StageNum;

                    Debug.Log("[AutoSaveMod] ========== 读档后验证 ==========");
                    Debug.Log("[AutoSaveMod] PlayData.TSavedata：NowStageMapKey=" + stageMapKey
                        + ", StageNum=" + stageNum
                        + ", Party.Count=" + partyCount);

                    // 同时输出 TempSave 状态用于对比
                    var smType2 = typeof(SaveManager);
                    var smField2 = smType2.GetField("savemanager",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    var smInstance2 = smField2 != null ? smField2.GetValue(null) as SaveManager : null;
                    var tempSaveField = smType2.GetField("TempSave",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var tempSave = smInstance2 != null && tempSaveField != null
                        ? tempSaveField.GetValue(smInstance2) as TempSaveData : null;
                    if (tempSave != null)
                    {
                        int tempPartyCount = tempSave.Party != null ? tempSave.Party.Count : 0;
                        Debug.Log("[AutoSaveMod] SaveManager.TempSave：NowStageMapKey=" + tempSave.NowStageMapKey
                            + ", StageNum=" + tempSave.StageNum
                            + ", Party.Count=" + tempPartyCount);
                    }

                    if (partyCount > 0)
                    {
                        Debug.Log("[AutoSaveMod] 修复成功：Party 数据已保留（" + partyCount + " 个角色）");
                    }
                    else
                    {
                        Debug.LogWarning("[AutoSaveMod] 警告：Party.Count=0，可能仍走新游戏初始化路径");
                        Debug.LogWarning("[AutoSaveMod] 建议：检查 SaveManager.savemanager.TempSave.Party.Count 是否 > 0");
                    }
                    Debug.Log("[AutoSaveMod] ==============================");
                }
                else
                {
                    Debug.LogError("[AutoSaveMod] 读档后验证失败：PlayData.TSavedata 仍为 null");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] VerifyLoadResult 异常: " + ex.Message);
            }
        }

        /// <summary>停止所有 AudioSource（防止 BGM 重复播放）</summary>
        private static void StopAllAudioSources()
        {
            try
            {
                var allAudio = UnityEngine.Object.FindObjectsOfType<AudioSource>();
                int stoppedCount = 0;
                foreach (var audio in allAudio)
                {
                    if (audio != null && audio.isPlaying)
                    {
                        audio.Stop();
                        stoppedCount++;
                    }
                }
                Debug.Log("[AutoSaveMod] 已停止 " + stoppedCount + " 个正在播放的 AudioSource（共 " + allAudio.Length + " 个）");
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] StopAllAudioSources 异常: " + ex.Message);
            }
        }

        /// <summary>删除指定备份</summary>
        public static bool DeleteBackup(string backupDirName)
        {
            try
            {
                string dirPath = Path.Combine(BackupRootDir, backupDirName);
                if (Directory.Exists(dirPath))
                {
                    Directory.Delete(dirPath, true);
                    Debug.Log("[AutoSaveMod] 已删除备份: " + backupDirName);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoSaveMod] DeleteBackup 异常: " + ex.Message);
                return false;
            }
        }

        // ============ 辅助方法 ============

        /// <summary>清理目录名中的非法字符</summary>
        private static string SanitizeDirName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Unknown";
            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder();
            foreach (char c in name)
            {
                if (Array.IndexOf(invalid, c) >= 0)
                    sb.Append('_');
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>安全获取循环数</summary>
        private static int SafeGetLoopNum()
        {
            try
            {
                return SaveManager.LoopNum;
            }
            catch { return 0; }
        }

        /// <summary>安全获取当前关卡 Key</summary>
        private static string SafeGetStageKey()
        {
            try
            {
                var tsd = PlayData.TSavedata;
                if (tsd != null && !string.IsNullOrEmpty(tsd.NowStageMapKey))
                    return tsd.NowStageMapKey;
                return "(未知)";
            }
            catch { return "(异常)"; }
        }

        /// <summary>安全获取金币</summary>
        private static int SafeGetGold()
        {
            try
            {
                var tsd = PlayData.TSavedata;
                if (tsd != null)
                    return tsd._Gold;
                return 0;
            }
            catch { return 0; }
        }

        /// <summary>安全获取灵魂石</summary>
        private static int SafeGetSoul()
        {
            try
            {
                var tsd = PlayData.TSavedata;
                if (tsd != null)
                    return tsd._Soul;
                return 0;
            }
            catch { return 0; }
        }

        /// <summary>安全获取关卡编号</summary>
        private static int SafeGetStageNum()
        {
            try
            {
                var tsd = PlayData.TSavedata;
                if (tsd != null)
                    return tsd.StageNum;
                return 0;
            }
            catch { return 0; }
        }

        /// <summary>安全获取游戏模式</summary>
        private static string SafeGetPlayMode()
        {
            try
            {
                var tsd = PlayData.TSavedata;
                if (tsd != null)
                {
                    // EPlayMode: StoryMode=0, FreeMode=1
                    return tsd.NowPlayMode.ToString();
                }
                return "(未知)";
            }
            catch { return "(异常)"; }
        }

        /// <summary>安全获取血雾模式开关</summary>
        private static bool SafeGetHopeMode()
        {
            try
            {
                var tsd = PlayData.TSavedata;
                if (tsd != null)
                    return tsd.HopeMode;
                return false;
            }
            catch { return false; }
        }

        /// <summary>安全获取血雾等级</summary>
        private static int SafeGetHopeModeLevel()
        {
            try
            {
                var tsd = PlayData.TSavedata;
                if (tsd != null)
                    return tsd.HopeModeLevel;
                return 0;
            }
            catch { return 0; }
        }

        /// <summary>安全获取血雾难度等级（BloodyMist.Level，需反射）</summary>
        private static int SafeGetBloodyMistLevel()
        {
            try
            {
                var tsd = PlayData.TSavedata;
                if (tsd == null || tsd.bMist == null) return 0;

                // BloodyMist.Level 是公共字段
                var levelField = tsd.bMist.GetType().GetField("Level",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (levelField != null)
                {
                    return (int)levelField.GetValue(tsd.bMist);
                }
                return 0;
            }
            catch { return 0; }
        }

        /// <summary>安全获取游戏时间（ticks）</summary>
        private static long SafeGetTimeTick()
        {
            try
            {
                var tsd = PlayData.TSavedata;
                if (tsd != null)
                    return tsd.TimeTick;
                return 0;
            }
            catch { return 0; }
        }

        /// <summary>安全获取出战角色信息列表</summary>
        private static List<CharInfo> SafeGetPartyInfo()
        {
            var result = new List<CharInfo>();
            try
            {
                var tsd = PlayData.TSavedata;
                if (tsd == null || tsd.Party == null) return result;

                foreach (var character in tsd.Party)
                {
                    if (character == null) continue;
                    var info = new CharInfo();
                    // 角色名
                    try { info.Name = character.Name ?? "(无名)"; } catch { info.Name = "(异常)"; }
                    // 角色等级
                    try { info.Level = character.LV; } catch { info.Level = 0; }
                    // 角色KeyData（可用于加载图标）
                    try { info.KeyData = character.KeyData ?? ""; } catch { info.KeyData = ""; }
                    result.Add(info);
                }
                return result;
            }
            catch { return result; }
        }
    }
}
