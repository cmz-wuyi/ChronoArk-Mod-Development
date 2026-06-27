# Chrono Ark Mod 开发方案 - 游戏开始时添加道具

## 问题诊断与修复

### 原始问题
Mod加载失败，错误信息：`Fail to load MyFirstMod`

### 根本原因
通过分析游戏日志 `C:\Users\cmz\AppData\LocalLow\AlFine\ChronoArk\UnityLogOutPut\output_log_0.txt`，发现：

```
System.Reflection.ReflectionTypeLoadException: Exception of type 'System.Reflection.ReflectionTypeLoadException' was thrown.
    at System.Reflection.Assembly.GetTypes()
    at ChronoArkMod.ModData.ModAssemblyInfo.init()
    at ChronoArkMod.ModData.ModInfo.LoadAtVeryBegining()
    at ChronoArkMod.ModManager.LoadMod()
```

**根本原因**：HarmonyX 2.10.1 与游戏的 Unity 2018.4.32f1 Mono运行时不兼容。

### 修复方案
1. **移除 HarmonyX 2.10.1** - 与游戏不兼容
2. **使用 Harmony 2.0.0 (net35版本)** - 兼容游戏的Mono运行时
3. **目标框架改为 .NET Framework 3.5** - 匹配游戏的运行时版本
4. **语言版本改为 C# 7.3** - 匹配.NET 3.5

### 修复后的配置
- 项目文件: [MyFirstMod.csproj](file:///d:/Games/Chrono%20Ark/ModDevelopment/MyFirstMod/MyFirstMod.csproj)
  - TargetFrameworkVersion: v3.5
  - LangVersion: 7.3
  - 引用: 0Harmony.dll (net35版本)

---

## 游戏分析结果

### 游戏信息
- **引擎**: Unity 2018.4.32f1
- **运行时**: Mono (兼容.NET Framework 3.5)
- **开发商**: AlFine
- **Mod系统**: ChronoArkMod.ModManager

### Mod加载流程
1. 游戏读取 `ChronoArkMod.json` 配置
2. 加载 `Assemblies` 目录中的DLL
3. 调用 `ModAssemblyInfo.init()` 初始化程序集
4. 调用 `Assembly.GetTypes()` 获取所有类型
5. 查找并执行Mod入口点

### 道具系统分析

#### 地图制作卷轴
- **ID**: `Scroll_Mapping`
- **Schema**: `Item_Scroll`
- **名称**: 地图制作卷轴 (지도 제작 스크롤)
- **描述**: 모든 구역을 밝힌다 (照亮所有区域)
- **语言键**: `Item_Scroll/Scroll_Mapping_name`

#### 其他卷轴
- `Scroll_Item` - 道具卷轴
- `Scroll_Enchant` - 附魔卷轴
- `Scroll_Vitality` - 活力卷轴

---

## 道具添加Mod开发方案

### 文件结构
```
MyFirstMod/
├── Scripts/
│   ├── ModEntry.cs          # Mod入口
│   └── ItemAddPatch.cs      # 道具添加补丁
├── Assemblies/
│   ├── MyFirstMod.dll       # 编译后的mod
│   └── 0Harmony.dll         # Harmony 2.0.0 (net35)
├── MyFirstMod.csproj        # 项目文件
└── ChronoArkMod.json        # Mod配置
```

### 实现步骤

#### 步骤1: 使用dnSpy查找游戏API

1. 下载并运行 [dnSpy](https://github.com/dnSpy/dnSpy/releases/tag/v6.1.8)
2. 打开文件: `d:\Games\Chrono Ark\ChronoArk_Data\Managed\Assembly-CSharp.dll`
3. 搜索以下类:
   - `PlayData` - 玩家数据类
   - `Inventory` 或 `ItemManager` - 背包/道具管理
   - `GameManager` - 游戏管理器
   - `Item_Scroll` - 卷轴道具类

4. 查找以下方法:
   - 游戏开始时调用的方法（如 `GameStart`, `NewGame`, `Init`）
   - 添加道具的方法（如 `AddItem`, `AddToInventory`）

#### 步骤2: 启用道具添加补丁

编辑 [ItemAddPatch.cs](file:///d:/Games/Chrono%20Ark/ModDevelopment/MyFirstMod/Scripts/ItemAddPatch.cs)，根据dnSpy查找到的API取消注释并调整代码：

```csharp
// 示例：假设找到 GameManager.GameStart 方法
[HarmonyPatch(typeof(GameManager), "GameStart")]
[HarmonyPostfix]
public static void GameStart_Postfix()
{
    AddScrollMappingItems();
}

// 示例：假设找到 PlayData.AddItem 方法
private static void AddScrollMappingItems()
{
    for (int i = 0; i < 5; i++)
    {
        PlayData.Instance.AddItem("Scroll_Mapping");
    }
    Debug.Log("[MyFirstMod] 已添加5个地图制作卷轴!");
}
```

#### 步骤3: 构建并测试

```powershell
cd "d:\Games\Chrono Ark\ModDevelopment"
.\BuildAndDeploy.ps1
```

---

## 当前Mod状态

### 已完成
- ✅ 修复了mod加载失败问题（HarmonyX → Harmony 2.0.0）
- ✅ 创建了mod框架（ModEntry.cs + ItemAddPatch.cs）
- ✅ 配置了正确的项目文件（.NET 3.5 + C# 7.3）
- ✅ 构建和部署成功

### 待完成（需要用户使用dnSpy确认API）
- ⏳ 确定游戏开始时调用的具体方法
- ⏳ 确定添加道具的具体API
- ⏳ 启用并测试道具添加功能

---

## 测试验证

### 验证Mod加载
1. 启动游戏: `d:\Games\Chrono Ark\x86\Master\ChronoArk.exe`
2. 主菜单 → Workshop → 启用 MyFirstMod
3. 查看游戏日志: `C:\Users\cmz\AppData\LocalLow\AlFine\ChronoArk\UnityLogOutPut\output_log_0.txt`
4. 搜索 `[MyFirstMod]` 确认mod已加载

### 验证道具添加
启用道具添加补丁后：
1. 开始新游戏
2. 检查背包是否有5个地图制作卷轴
3. 查看日志确认道具添加成功

---

## 常见问题

### Q: Mod仍然加载失败？
A: 查看游戏日志，搜索 `ReflectionTypeLoadException`。如果仍有错误，可能需要：
1. 检查0Harmony.dll是否为net35版本
2. 确认目标框架为v3.5
3. 移除不必要的引用

### Q: 如何找到正确的游戏方法？
A: 使用dnSpy反编译 `Assembly-CSharp.dll`：
1. 按 `Ctrl+Shift+K` 搜索类型
2. 搜索 `GameManager`, `PlayData` 等关键词
3. 查看方法定义和调用关系

### Q: 补丁不生效？
A: 检查以下几点：
1. 确认补丁方法有正确的 `[HarmonyPatch]` 特性
2. 确认方法签名匹配（参数类型、返回类型）
3. 查看日志是否有补丁应用错误

---

## 参考资源

- [Harmony文档](https://harmony.pardeike.net/articles/intro.html)
- [dnSpy下载](https://github.com/dnSpy/dnSpy/releases/tag/v6.1.8)
- [Chrono Ark Mod教程](https://zerol.me/2023/09/16/Modding-Tutorial-for-Chrono-Ark/)
- [游戏日志位置](file:///C:/Users/cmz/AppData/LocalLow/AlFine/ChronoArk/UnityLogOutPut)
