# Chrono Ark 游戏系统技术分析文档

## 一、游戏核心架构

### 1.1 引擎与运行时
- **游戏引擎**: Unity 2018.4.32f1
- **运行时**: Mono (兼容.NET Framework 3.5)
- **开发商**: AlFine
- **游戏类型**: Roguelike RPG（时间循环主题）

### 1.2 核心管理器架构（无GameManager，分布式设计）

| 管理器类 | 基类 | 职责 |
|---------|------|------|
| **FieldSystem** | MonoBehaviour | **场景总管理器**（最核心）：场景切换、战斗触发、队伍/物品管理 |
| **StageSystem** | MonoBehaviour | 关卡内部管理：等距地图、瓦片、视野迷雾、Boss |
| **BattleSystem** | MonoBehaviour | 战斗系统核心 |
| **SaveManager** | MonoBehaviour | 存档管理 |
| **InventoryManager** | MonoBehaviour | 物品栏管理（基类，13个子类） |
| **UIManager** | MonoBehaviour | UI总管理 |
| **LoopScript** | MonoBehaviour | 时间循环机制核心 |
| **MainSceneScript** | MonoBehaviour | 主场景脚本（含GameStart入口） |

### 1.3 关键单例访问点
```csharp
PartyInventory.Ins      // PartyInventory单例
PartyInventory.InvenM   // 主背包管理器（静态）
PlayData.*              // 所有字段均为静态（全局玩家状态）
SaveManager.NowData     // 当前存档数据
```

---

## 二、游戏初始化流程

### 2.1 启动顺序
```
阶段1 (Awake):  SteamManager → SaveManager → LocalizeManager → FieldSystem → StageSystem
阶段2 (Start):  LoopScript.Init() → SaveManager.Load() → FieldSystem.Start() → StageSystem.Start()
阶段3 (入口):   MainSceneScript.GameStart() → PlayData.init()
阶段4 (场景):   FieldSystem.Load() → FieldSystem.RogurLikeGameStart() / StageStart()
```

### 2.2 场景流转方法（FieldSystem.Go*系列）
- `GoArk_LucyWakeUp()` - 露西苏醒
- `GoLucyStartRoom()` - 露西起始房间
- `GoMonoMap1()` - 第一章地图
- `GoMasterSDMap()` - 主线SD地图
- `GoMaster3DMap(StoryNum)` - 3D地图

---

## 三、道具系统完整分析

### 3.1 道具继承体系
```
ItemBase (基类，System.Object)
├── Item_Scroll      (卷轴)
├── Item_Passive     (被动遗物)
├── Item_Misc        (杂项/货币)
├── Item_Equip       (装备)
└── Item_SkillBase   (技能道具基类)
    ├── Item_Active  (主动道具)
    ├── Item_Consume (消耗品)
    ├── Item_Potions (药水)
    └── Item_Skill   (技能书)
```

### 3.2 道具创建API
```csharp
// 按key创建道具
ItemBase item = ItemBase.GetItem("Scroll_Mapping");

// 按key+堆叠数创建
ItemBase item = ItemBase.GetItem("Scroll_Mapping", 5);

// 从技能数据创建技能书
Item_Skill skillItem = ItemBase.GetItem(skillData);

// 随机药水
ItemBase potion = ItemBase.GetPotionRandom();
```

### 3.3 道具数据结构
每个道具类型有对应的GDE数据类：
- `GDEItem_ScrollData` - 卷轴数据（Lock, name, Description, SubClassName）
- `GDEItem_ActiveData` - 主动道具数据（含Charge充能系统）
- `GDEItem_PassiveData` - 被动道具数据（含passive_script）
- `GDEItem_PotionsData` - 药水数据
- `GDEItem_MiscData` - 杂项数据（含MaxStack, Price）

---

## 四、背包系统完整分析

### 4.1 InventoryManager（背包管理器基类）

**核心字段**：
- `List<ItemBase> _items` - 当前背包道具列表
- `Boolean IsActive` - 是否主动道具栏
- `Boolean Equip` - 是否装备栏

**道具添加方法**：
```csharp
bool AddNewItem(String key)              // 按key添加
bool AddNewItem(ItemBase Item)           // 添加道具对象
bool AddNewItem(Int32 ItemNum, ItemBase Item)  // 添加到指定槽位
bool FullStackItemAdd(ItemBase Item)     // 满堆叠添加
```

**道具删除方法**：
```csharp
void DelItem(Int32 ItemNum)              // 按槽位删除
void DelItem(ItemBase Item)              // 按对象删除
void DelItem(ItemBase Item, Int32 num)   // 删除指定数量
void DelItem(String key, Int32 num)      // 按key删除指定数量
```

**道具查找方法**：
```csharp
Int32 FindItem(String Key)               // 查找返回槽位号
ItemBase ReturnItem(String Key)          // 获取道具对象
```

**静态奖励方法**：
```csharp
void Reward(ItemBase Item)               // 奖励单个道具
void Reward(List<ItemBase> Items)        // 奖励多个道具
void Reward(String rewardkey)            // 按奖励key奖励
```

### 4.2 PartyInventory（队伍背包）

**静态访问点**：
- `PartyInventory.InvenM` - 主背包管理器实例
- `PartyInventory.Ins` - PartyInventory单例

### 4.3 InventoryManager子类（13个）
- `ActiveInventory` - 主动道具栏
- `ArkPartsInven` - 方舟部件栏
- `CharEquipInven` - 角色装备栏
- `UseNecklaceInven` - 项链使用栏
- 事件类：`AnvilEvent`, `CombineEvent`, `EnchantEvent`, `RelicChestEvent` 等

---

## 五、道具ID编号体系

### 5.1 道具类型统计（共8种Schema，398个道具）

| Schema | 数量 | 说明 |
|--------|------|------|
| Item_Scroll | 11 | 卷轴 |
| Item_Consume | 28 | 消耗品 |
| Item_Equip | 159 | 装备 |
| Item_Passive | 123 | 被动遗物 |
| Item_Misc | 20 | 杂项/货币 |
| Item_Active | 15 | 主动道具 |
| Item_Potions | 16 | 药水 |
| Item_Friendship | 26 | 羁绊礼物 |

### 5.2 卷轴类道具完整列表

| 道具Key | 中文名 | 功能 |
|---------|--------|------|
| Scroll_Enchant | 附魔卷轴 | 附魔装备 |
| Scroll_Identify | 鉴定卷轴 | 鉴定道具 |
| Scroll_Item | 道具卷轴 | 获得随机道具 |
| **Scroll_Mapping** | **地图制作卷轴** | **完全探查本区域** |
| Scroll_Midas | 点金术卷轴 | 道具换金币 |
| Scroll_Purification | 净化卷轴 | 解除装备诅咒 |
| Scroll_Quick | 加速卷轴 | 区域内加速 |
| Scroll_Teleport | 瞬移卷轴 | 移动至随机地砖 |
| Scroll_Transfer | 传送卷轴 | Boss前召唤遗物台 |
| Scroll_Uncurse | 诅咒解除卷轴 | 解除诅咒发现秘密墙 |
| Scroll_Vitality | 活力卷轴 | 下次战斗释放露西技能 |

### 5.3 地图制作卷轴数据
```json
"Scroll_Mapping": {
    "_gdeSchema": "Item_Scroll",
    "Lock": false,
    "name": "지도 제작 스크롤",
    "Description": "모든 구역을 밝힌다",
    "SubClassName": ""
}
```

---

## 六、初始道具获取机制

### 6.1 游戏开始入口
- `FieldSystem.RogurLikeGameStart()` - Roguelike新游戏开始
- `FieldSystem.StageStart(String StageKey)` - 关卡开始
- `FieldSystem.LucyRoomInit()` - 露西房间初始化

### 6.2 道具添加调用链
```
FieldSystem.RogurLikeGameStart()
  └── FieldSystem.ItemAdd(String key)        // 游戏自带方法
        └── InventoryManager.AddNewItem(String key)  // 加入背包
              └── ItemBase.GetItem(String key)       // 创建道具实例
```

### 6.3 存档系统结构
- **SaveManager** - 存档管理器
- **NewSaveData** - 持久化存档（跨Run）
- **TempSaveData** - 单次Run数据（含背包、装备、货币）
- **PlayData** - 运行时静态数据

TempSaveData中的背包字段：
- `List<ItemBase> Inventory` - 主背包
- `List<ItemBase> ActiveInventory` - 主动道具栏
- `List<ItemBase> Data_PassiveItems` - 被动道具
- `Int32 _Gold/_Soul/_AP` - 金币/灵魂/AP

---

## 七、Mod加载系统

### 7.1 ModManager工作流程
```
ModManager.Init()
  ├─ ReadLocalMods()           // 扫描本地Mod目录
  ├─ ReadWorkShopMods()        // 扫描Steam创意工坊
  ├─ LoadAllEnabledAtStarting() // 加载启用的Mod
  │    └─ LoadMod(modInfo)
  │         ├─ ModAssemblyInfo.init()  // 加载DLL
  │         ├─ AssetManager             // 加载资源
  │         ├─ ModGDEInfo               // 加载GDE数据
  │         └─ ModLocalizationInfo      // 加载本地化
  └─ DataBaseInit()            // 初始化Mod数据库
```

### 7.2 Mod配置文件 (ChronoArkMod.json)
```json
{
    "Name": "ModName",
    "Author": "Author",
    "Version": "1.0.0",
    "Assemblies": ["ModName.dll", "0Harmony.dll"],
    "UseMod": true
}
```

---

## 八、关键技术要点

### 8.1 补丁目标选择
1. **新游戏**: `FieldSystem.RogurLikeGameStart()` (无参数)
2. **加载存档**: `FieldSystem.StageStart(String StageKey)` (有参数)
3. **露西房间**: `FieldSystem.LucyRoomInit()` (无参数)

### 8.2 道具添加最佳实践
```csharp
// 方法1：使用游戏自带方法（推荐）
FieldSystem.Instance.ItemAdd("Scroll_Mapping");

// 方法2：直接调用背包管理器
PartyInventory.InvenM.AddNewItem("Scroll_Mapping");

// 方法3：创建道具对象后添加
ItemBase item = ItemBase.GetItem("Scroll_Mapping");
PartyInventory.InvenM.AddNewItem(item);
```

### 8.3 注意事项
- 游戏使用.NET Framework 3.5，Harmony需使用net35版本
- 道具基类是`ItemBase`，不是`Item`
- `PartyInventory.InvenM`是静态字段，全局可访问
- `PlayData`所有字段都是静态的
