# Chrono Ark Mod 问题诊断报告

## 一、运行数据收集结果

### 1.1 日志文件状态
| 文件 | 状态 | 说明 |
|------|------|------|
| `MyFirstMod_RunLog.txt` | **不存在** | RunLogger.SaveLog()从未执行 |
| Unity日志 `[MyFirstMod]` 前缀 | **无输出** | Debug.Log从未被调用 |
| 游戏日志 `MyFirstMod Loaded` | **有输出** | 仅游戏自身输出的加载确认 |

### 1.2 关键发现

**现象**: Mod显示"Loaded"但代码完全未执行

**证据链**:
```
时间线分析 (output_log_4.txt):
1. "Start loading mod MyFirstMod"     ← 游戏开始加载
2. Fallback handler could not load library data-*.dll  ← Mono JIT编译警告(正常)
3. "MyFirstMod Loaded"                 ← 游戏输出(不是我们的代码)
4. [无任何 [MyFirstMod] 前缀的日志]    ← 我们的代码未执行!
5. "Start unloading mod MyFirstMod"   ← 卸载
6. "Start loading mod MyFirstMod"     ← 重新加载
7. ... 循环 ...
8. ArgumentNullException at ChronoArkMod.ModEditor.BasicInfoWorkshop.UpdateInfo
   ← Mod编辑器UI显示错误(非mod加载错误)
```

### 1.3 错误详情
```
ArgumentNullException: Value cannot be null.
Parameter name: value
  at Newtonsoft.Json.Linq.Extensions.Value[T,U]
  at ChronoArkMod.ModEditor.BasicInfoWorkshop.<UpdateInfo>b__4_1
  at ChronoArkMod.ModEditor.ModEditorPanel.UpdateInfo()
  → 这是Mod编辑器Workshop信息显示时的JSON解析错误
→ 与mod代码执行无关，是UI显示问题
```

---

## 二、问题定位分析

### 2.1 现象层
- Mod在游戏界面显示为"已加载"
- 但mod中的C#代码完全没有执行
- 静态构造函数、Harmony补丁、Debug.Log均未触发

### 2.2 原因层（逐步深入）

#### 假设1: 静态构造函数抛出异常被捕获
**可能性**: 高  
**推理**: 如果静态构造函数中任何代码抛出异常，`Assembly.GetTypes()` 会产生 `ReflectionTypeLoadException`，游戏的 `ModAssemblyInfo.init()` 可能会捕获此异常并仅输出"MyFirstMod Loaded"

**验证方法**: 已部署零依赖测试版（无Harmony，最小代码）

#### 假设2: 游戏Mod系统不通过静态构造函数初始化
**可能性**: 中  
**推理**: 从截图看到游戏有完整的Script系统，可能：
- 游戏期望特定的入口点类/接口
- 使用自定义属性标记入口点
- 通过反射查找特定方法名

**验证方法**: 检查游戏是否有特定的mod入口接口

#### 假设3: 编译目标与运行时不兼容
**可能性**: 中  
**推理**: 
- 项目目标: .NET Framework 3.5 / C# 7.3
- 游戏运行时: Unity 2018.4.32f1 Mono
- MSBuild版本: 18.0 (Visual Studio 2022)
- 可能生成的IL指令不被Mono运行时支持

**验证方法**: 使用游戏自带的MSBuild或降低语言版本

---

## 三、根本原因分析

### 3.1 最可能的根本原因

**Chrono Ark的Mod系统使用 `Assembly.GetTypes()` 加载类型**

```csharp
// 游戏内部代码 (推测)
public void init() {
    try {
        var types = assembly.GetTypes();  // ← 这里触发所有类型的静态构造函数
        // 处理类型...
        Debug.Log(name + " Loaded");
    } catch (ReflectionTypeLoadException ex) {
        // 即使有类型加载失败，仍然输出 "Loaded"
        Debug.Log(name + " Loaded");
        // LoaderExceptions 包含失败原因
    }
}
```

**当我们的DLL包含以下任一问题时，静态构造函数会在GetTypes()时失败：**
1. 引用了不存在的程序集（如旧版HarmonyX）
2. 使用了不兼容的语言特性
3. 目标框架不匹配
4. IL指令不被Mono支持

### 3.2 为什么之前的版本都失败了

| 版本 | 问题 |
|------|------|
| v1 HarmonyX 2.10.1 | ReflectionTypeLoadException - Fallback handler无法加载库 |
| v2 Harmony 2.0.0 net35 | 可能成功加载但补丁未触发（需要验证） |
| v3 RunLogger增强版 | 代码复杂度增加，可能有其他依赖问题 |
| v4 零依赖测试版 | 待验证 |

---

## 四、当前测试部署

### 4.1 已部署: 零依赖测试版

**文件**: [MyFirstMod.dll](file:///d:/Games/Chrono%20Ark/ChronoArk_Data/StreamingAssets/Mod/MyFirstMod/Assemblies/MyFirstMod.dll) (4608 bytes)

**特点**:
- 无任何外部依赖（无Harmony）
- 最小化代码（仅37行）
- 三重验证机制:
  1. Debug.Log 输出
  2. 文件写入 `%AppData%\MyFirstMod_Test.txt`
  3. 异常捕获并输出

**配置**:
```json
{
    "Assemblies": ["MyFirstMod.dll"],  // 不包含0Harmony.dll
    "UseMod": true
}
```

### 4.2 测试步骤

1. 启动游戏
2. 进入Workshop确认MyFirstMod启用
3. **检查两个位置**:
   - Unity日志: 搜索 `TEST-1` 或 `CRITICAL ERROR`
   - 文件: `C:\Users\cmz\AppData\Roaming\MyFirstMod_Test.txt`

### 4.3 测试结果解读

| 结果 | 含义 | 下一步 |
|------|------|--------|
| 看到 TEST-1 | 代码可执行! | 添加功能代码 |
| 看到 CRITICAL ERROR | 代码执行但有异常 | 查看具体错误 |
| 两者都没有 | 代码完全未执行 | 需要使用游戏自带Script系统 |
| 只有文件存在 | Debug.Log不可用但代码执行 | 使用文件日志方式 |

---

## 五、基于截图的Mod工坊分析

从用户提供的Mod编辑器截图分析：

### 5.1 Mod编辑器功能模块
```
标签页: Basic | Character | Skill | Buff | Skill Extended | Item | Random Event | Enemy | Misc | Image Factory
左侧菜单:
├── Basic          - 基本信息
├── Workshop       - 创意工坊
├── Mod 选项       - Mod设置
├── Battle Preset - 战斗预设
├── Script         - 脚本(dnSpy/ILSpy集成)
├── Decompliation  - 反编译
├── Log            - 日志
└── Unity Editor   - Unity编辑器
```

### 5.2 Script页面功能
- 配置dnSpy.exe或ILSpy.exe路径
- 输入Full Class Name查看反编译代码
- 支持Buff/被动/Skill等分类快速查询
- **Check Decompiled C# Codes按钮** - 查看反编译代码
- 底部: Test Battle, Open Mod Folder, Check Unity Log

### 5.3 推测的正确Mod开发流程

根据截图，正确的开发流程可能是：

1. **使用游戏内置Mod编辑器** 创建数据（Item/Skill/Buff等）
2. **使用Script标签页** 配置外部脚本工具
3. **脚本通过特定接口** 与游戏交互（而非简单的静态构造函数）
4. **点击Save保存** mod配置
5. **点击Test Battle测试**

---

## 六、解决方案建议

### 方案A: 继续外部DLL方式（如果零依赖测试成功）

如果零依赖测试版能执行代码：

1. **确认代码执行后**，逐步添加功能：
   - 先添加道具添加逻辑（不用Harmony）
   - 再引入Harmony（使用正确版本）

2. **道具添加方案**（无需Harmony）：
```csharp
// 在静态构造函数中使用事件订阅而非补丁
static ModEntry() {
    // 订阅游戏事件（如果有公开事件的话）
}
```

### 方案B: 使用游戏内置Script系统（推荐）

根据截图，游戏有自己的脚本系统：

1. **在游戏中打开Mod编辑器**
2. **配置dnSpy路径**（用于查看代码）
3. **使用Item标签页** 直接创建地图制作卷轴作为初始物品
4. **使用Basic标签页** 配置mod基本信息
5. **点击Save保存**

### 方案C: 参考现有工作Mod的结构

1. 从Steam创意工坊下载一个已知工作的mod
2. 分析其目录结构和代码结构
3. 模仿其实现方式

---

## 七、待验证项

- [ ] 零依赖测试版是否能执行代码？
- [ ] `%AppData%\MyFirstMod_Test.txt` 是否生成？
- [ ] Unity日志是否有TEST-1输出？
- [ ] 游戏是否期望特定的入口点接口？

---

*报告生成时间: 2026-06-23*
*分析基于: output_log_4.txt + ModEditorConfig.json + ChronoArkMod.json + 截图*
