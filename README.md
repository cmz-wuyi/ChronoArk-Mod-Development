# ChronoArk Mod Development

> Chrono Ark（时之幻境）游戏 Mod 开发合集 - 包含 Boss 修复、调试控制台、Boss 替换、卷轴获取等功能 mod

本仓库包含 6 个独立的 Chrono Ark mod，涵盖 bug 修复、游戏内调试控制台、自定义 Boss 替换和道具获取等功能。所有 mod 均使用 Harmony 2.0.0 补丁框架开发，兼容游戏的 Unity 2018.4 Mono 运行时。

## Mod 列表

| Mod 名称 | 功能 | 快捷键 | 说明 |
|----------|------|--------|------|
| **MyFirstMod** | 最小骨架模板 | - | 供新 mod 开发参考的空模板，无任何功能 |
| **BugFixMod** | Boss 替换 bug 修复 | - | 修复异端审判所事件触发后 Boss 不替换为犹大的游戏 bug |
| **DebugConsoleMod** | 审判官事件调试控制台 | F1 | 提供状态查看、关卡跳转、装备给予等调试功能 |
| **BossReplaceMod** | 自定义 Boss 替换 | F2 | 通过 JSON 配置文件自定义 Boss 替换规则 |
| **ScrollConsoleMod** | 卷轴获取控制台 | F3 | 提供 11 种卷轴的快速获取功能 |

## 功能详解

### MyFirstMod - 最小骨架模板

供新 mod 开发参考的最小化模板，只包含 `Initialize()`、`Dispose()`、`OnModLoaded()`、`OnModSettingUpdate()` 四个生命周期的日志输出，无任何实际功能。

**用途**：复制此目录作为新 mod 的起点，避免重复搭建项目结构。

### BugFixMod - Boss 替换 Bug 修复

**修复的问题**：游戏中 `StoryData.JohanQuestClear` 字段从未被设置为 `true`，导致异端审判所事件触发后，关底 Boss 不会替换为"异端审判官犹大"，而是走普通剧情分支。

**修复原理**：通过 Harmony Postfix 补丁 `RE_TheInquisition.UseButton1`，在审判官事件触发时强制设置 `JohanQuestClear = true`。

**技术细节**：
- 补丁目标：`RE_TheInquisition.UseButton1`
- 补丁类型：Postfix
- 触发条件：`PlayData.TSavedata.TheInquisition == true && !SaveManager.NowData.storydata.JohanQuestClear`

### DebugConsoleMod - 审判官事件调试控制台（F1）

按 **F1** 键打开/关闭调试控制台，主要功能包括：

- **状态查看**：查看 `TheInquisition`、`JohanQuestClear` 等关键游戏状态
- **状态重置**：两步确认后重置审判官事件状态（避免误触）
- **关卡跳转**：跳转到白色墓地等指定关卡
- **装备给予**：给予审判官火炬等专属装备
- **事件触发**：手动触发审判官事件（默认不触发，仅手动激活）

**控制台规范**：窗口尺寸 680×860，标题字体 20，正文字体 18，日志字体 16，包含滚动条和新手引导区。

### BossReplaceMod - 自定义 Boss 替换（F2）

按 **F2** 键打开/关闭 Boss 替换控制台，主要功能包括：

- **JSON 配置**：通过 `BossReplaceConfig.json` 自定义 Boss 替换规则
- **替换管理**：启用/禁用 Boss 替换功能
- **规则查看**：查看当前生效的替换规则
- **即时测试**：控制台内即时测试替换效果

**Boss 替换机制**：
1. 通过反射访问 `StageSystem.EnemyQueue`（私有字段）
2. 替换队列中的 `GDEEnemyQueueData` 为目标 Boss 队列
3. 支持 `StageSystem.BossEnterFunc` 的 Postfix 补丁

### ScrollConsoleMod - 卷轴获取控制台（F3）

按 **F3** 键打开/关闭卷轴获取控制台，主要功能包括：

- **11 种卷轴**：支持所有游戏内卷轴类型的获取
- **数量自定义**：可输入需要获取的卷轴数量
- **选择网格**：使用 `SelectionGrid` 直观选择卷轴类型
- **即时获取**：点击按钮立即将卷轴加入背包

**道具给予 API**：
- 普通道具：`PartyInventory.InvenM.AddNewItem(key)`
- 装备类道具：`InventoryManager.Reward(ItemBase.GetItem(key))`（注意：装备不能用 AddNewItem）

## 目录结构

```
ModDevelopment/
├── MyFirstMod/                    # 最小骨架模板
│   ├── Scripts/
│   │   └── ModEntry.cs            # 插件入口
│   ├── Assemblies/                # 编译产物（含 DLL）
│   ├── MyFirstMod.csproj
│   └── ChronoArkMod.json          # Mod 配置
├── BugFixMod/                     # Boss 修复 mod
│   ├── Scripts/
│   │   ├── BossFixModEntry.cs
│   │   └── BossReplaceFixPatch.cs
│   ├── Assemblies/
│   ├── BugFixMod.csproj
│   └── ChronoArkMod.json
├── DebugConsoleMod/               # F1 调试控制台
│   ├── Scripts/
│   │   ├── DebugConsoleModEntry.cs
│   │   └── DebugConsoleBehaviour.cs
│   ├── Assemblies/
│   ├── DebugConsoleMod.csproj
│   └── ChronoArkMod.json
├── BossReplaceMod/                # F2 Boss 替换
│   ├── Scripts/
│   │   ├── BossReplaceModEntry.cs
│   │   ├── BossReplaceConsoleBehaviour.cs
│   │   ├── BossReplaceControlPanel.cs
│   │   ├── BossReplaceManager.cs
│   │   ├── BossReplacePatch.cs
│   │   └── BossReplaceConfig.cs
│   ├── Assemblies/
│   ├── BossReplaceMod.csproj
│   └── ChronoArkMod.json
├── ScrollConsoleMod/              # F3 卷轴获取
│   ├── Scripts/
│   │   ├── ScrollConsoleModEntry.cs
│   │   ├── ScrollConsoleBehaviour.cs
│   │   └── ScrollConsoleContent.cs
│   ├── Assemblies/
│   ├── ScrollConsoleMod.csproj
│   └── ChronoArkMod.json
├── Build.ps1                      # 构建脚本
├── Deploy.ps1                     # 部署脚本
├── BuildAndDeploy.ps1             # 一键构建部署
├── DEVELOPMENT_PLAN.md            # 开发方案文档
├── DIAGNOSTIC_REPORT.md           # 问题诊断报告
└── GAME_ANALYSIS.md               # 游戏系统技术分析
```

## 安装方式

### 方式一：直接使用已编译的 mod（推荐）

本仓库已包含编译好的 DLL，无需自行编译。

1. 将对应 mod 目录下的 `Assemblies/` 文件夹和 `ChronoArkMod.json` 复制到游戏 mod 目录：
   ```
   <游戏安装目录>\ChronoArk_Data\StreamingAssets\Mod\<ModName>\
   ├── Assemblies\
   │   ├── <ModName>.dll
   │   └── 0Harmony.dll
   └── ChronoArkMod.json
   ```
2. 启动游戏
3. 主菜单 → Workshop（创意工坊）→ 启用对应 mod
4. 游戏内按对应快捷键（F1/F2/F3）打开控制台

### 方式二：自行编译

如需修改源码后编译，请按以下步骤操作：

1. **准备依赖库**：将游戏目录 `<游戏安装目录>\ChronoArk_Data\Managed\` 下的以下 DLL 复制到 `libs/` 目录（需自行创建）：
   ```
   libs/
   ├── Assembly-CSharp.dll
   ├── UnityEngine.dll
   ├── UnityEngine.CoreModule.dll
   ├── UnityEngine.IMGUIModule.dll
   ├── UnityEngine.InputLegacyModule.dll
   ├── UnityEngine.TextRenderingModule.dll
   ├── 0Harmony.dll          (版本 2.0.0 net35)
   ├── Newtonsoft.Json.dll
   ├── mscorlib.dll
   ├── netstandard.dll
   ├── System.dll
   └── System.Core.dll
   ```

2. **编译**：使用 MSBuild 编译各个 mod 项目
   ```powershell
   # 进入 mod 目录
   cd "ModDevelopment\BossReplaceMod"
   
   # 使用 MSBuild 编译
   & "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
       BossReplaceMod.csproj /p:Configuration=Release /nologo /v:minimal
   ```

3. **部署**：编译后的 DLL 位于 `bin/Release/`，将其复制到 `Assemblies/` 目录，然后按方式一部署到游戏。

## 使用说明

### 快捷键

| 快捷键 | 功能 |
|--------|------|
| F1 | 打开/关闭审判官事件调试控制台 |
| F2 | 打开/关闭 Boss 替换控制台 |
| F3 | 打开/关闭卷轴获取控制台 |

### 控制台操作

- 所有控制台使用 Unity OnGUI 即时模式渲染
- 窗口可拖动（点击标题栏拖动）
- 控制台之间互不冲突（使用不同窗口 ID：F1=9999, F2=9998, F3=9997, F4=9996）
- 可同时开启多个控制台

### 重要提示

- **BugFixMod** 是修复游戏本身 bug 的 mod，建议常驻启用
- **审判官事件**默认不会被 DebugConsoleMod 触发，仅通过控制台手动激活
- **Boss 替换**需要在 F2 控制台中启用替换规则后才会生效

## 技术栈

| 项目 | 版本/说明 |
|------|-----------|
| 游戏引擎 | Unity 2018.4.32f1（Mono 运行时） |
| .NET Framework | 3.5 |
| C# 语言版本 | 7.3 |
| Harmony | 2.0.0（net35 版本，**非** HarmonyX） |
| Mod 框架 | ChronoArkMod.Plugin.ChronoArkPlugin |

## 编译要求

- **MSBuild**（Visual Studio 2022 自带）
- 目标框架：.NET Framework 3.5（`<TargetFrameworkVersion>v3.5</TargetFrameworkVersion>`）
- 语言版本：C# 7.3（`<LangVersion>7.3</LangVersion>`）
- FrameworkPathOverride：需指向游戏的 `Managed` 目录
- **关键**：必须使用 Harmony 2.0.0 net35 版本，HarmonyX 不兼容游戏的 Mono 运行时

## 踩坑记录

开发过程中遇到的关键问题及解决方案：

1. **Mod 加载失败（Fail to load）**：HarmonyX 2.10.1 与游戏 Mono 运行时不兼容，必须使用 Harmony 2.0.0 net35
2. **DLL 部署位置错误**：必须放在 `Mod/<ModName>/Assemblies/` 子目录，不能放在 mod 根目录
3. **道具添加时机**：`GoLucyStartRoom` 太早（背包未初始化），应使用 `GoMonoMap1` 或 `StageStart`
4. **装备给予失败**：`AddNewItem()` 不支持装备，必须使用 `InventoryManager.Reward()`
5. **GDEItemKeys 值不匹配**：字段名与运行时值不同（如 `Item_Equip_Torch_FanaticBoss` → `Torch_FanaticBoss`），需用反射读取
6. **PatchAll 无效**：Harmony 补丁必须手动 `Patch()`，不能依赖 `PatchAll()`
7. **GetTypes 异常被静默捕获**：编译错误会导致 `Assembly.GetTypes()` 抛异常，但游戏静默捕获不报错，需仔细检查日志
8. **Save0.sav 不被写入**：`SaveManager.Save()` 只写 Save1.sav，必须用 `ProgressOneSave()` 同时写两个存档
9. **TempSaveLoad() 丢弃有效存档**：检查 `StageNum==0` 并丢弃数据，但 0 是第一章第一节正常值，需用 `XMLDeserialize_TempData()` 绕过
10. **双重 OneSaveLoad() 导致 NRE**：手动调用 + `FieldSystem.Load()` 协程内调用 = 第二次 V0 备份重建后角色 → NRE，应让协程独占处理
11. **LoadOneSaveMap() 缺守卫清空 Party**：直接调用会触发新游戏初始化，应用 `FieldSystem.Load()` 协程（含 IsLoaded 守卫）
12. **读档后 BGM 重复**：读档前需调用 `FindObjectsOfType<AudioSource>()` 停止所有正在播放的音频

## 许可证

- **Mod 源代码**：MIT License
- **0Harmony.dll**：遵循 LGPL-3.0 License（Andreas Pardeike）
- **游戏相关 DLL**：版权归 AlFine 所有，本仓库不包含游戏版权文件，用户需自行提供

## 相关资源

- [Harmony 文档](https://harmony.pardeike.net/articles/intro.html)
- [Chrono Ark Mod 教程（英文）](https://zerol.me/2023/09/16/Modding-Tutorial-for-Chrono-Ark/)
- [官方 Discord](https://discord.gg/4NAZkZ4)

---

作者：cmz (wuyi)
