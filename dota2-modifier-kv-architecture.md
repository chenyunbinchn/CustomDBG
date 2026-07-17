# Dota 2 Modifier + KV 架构体系总结

> 本文基于 Valve Workshop Tools 公开的 Lua API、KV 数据格式、社区 Modding 文档，以及对 Source 2 引擎行为的合理推断。C++ 引擎层实现为推测，已标注。

---

## 一、整体架构分层

```
┌─────────────────────────────────────────────────────────┐
│  KV 数据层         "有什么"                              │
│  npc_heroes.txt / npc_abilities.txt / npc_items.txt     │
│  定义：英雄属性、技能参数、物品数值                         │
│  纯数据，无逻辑，改完重新加载即可生效                       │
├─────────────────────────────────────────────────────────┤
│  Lua 脚本层        "怎么交互"                             │
│  技能施放逻辑（OnSpellStart）                             │
│  Modifier 定义（属性修改 + 事件响应）                      │
│  有逻辑，无需编译，改完重新加载地图即可生效                  │
├─────────────────────────────────────────────────────────┤
│  C++ 引擎层        "底层管线"（推测，闭源）                │
│  伤害计算管线、事件派发机制、Modifier 注册表                │
│  网络同步、渲染、物理、寻路                                │
│  修改需要重新编译，客户端需要下载更新                       │
└─────────────────────────────────────────────────────────┘
```

**设计原则：越上层改得越频繁，越下层越稳定。**

大部分版本更新只碰 KV（调数值），少数版本改 Lua（新增机制），极少数版本碰 C++（新增事件类型/属性类型/管线阶段）。

---

## 二、KV 数据层

### 2.1 英雄定义

```
"npc_dota_hero_axe"
{
    "BaseClass"          "npc_dota_hero"
    "HeroID"             "2"
    "AttributePrimary"   "DOTA_ATTRIBUTE_STRENGTH"
    "MovementSpeed"      "310"
    "ArmorPhysical"      "2"
    "Ability1"           "axe_berserkers_call"
    "Ability2"           "axe_battle_hunger"
    "Ability3"           "axe_counter_helix"
    "Ability4"           "axe_culling_blade"
}
```

英雄不是一个类，而是一组数据条目。引擎在运行时读取这些数据，组装成一个实体。`Ability1-4` 是字符串引用，指向技能数据条目。

### 2.2 技能定义

```
"axe_counter_helix"
{
    "AbilityBehavior"    "DOTA_ABILITY_BEHAVIOR_PASSIVE"
    "AbilitySpecial"
    {
        "01" { "var_type" "FIELD_INTEGER" "trigger_chance"  "20" }
        "02" { "var_type" "FIELD_INTEGER" "damage"          "95 130 165 200" }
        "03" { "var_type" "FIELD_FLOAT"   "radius"          "275" }
    }
}
```

`AbilityBehavior` 是行为标签，告诉引擎这是被动技能。`AbilitySpecial` 是参数表，Lua 通过 `GetSpecialValueFor("damage")` 读取，自动按技能等级返回对应值。

### 2.3 KV 的边界

KV 只能表达**静态参数和行为标签**。以下事情 KV 做不了：

- 基于运行时状态的条件逻辑（"如果 3 秒内再次触发则伤害递增"）
- 跨技能的交互逻辑（"偷取目标技能"）
- 复杂的目标选择逻辑（"选择血量最低的敌人"）

这些需要 Lua。

---

## 三、Modifier 系统

### 3.1 Modifier 是什么

Modifier 是 Dota 2 玩法交互的核心抽象。每个 Modifier 是一个**自描述的回调集合体**，同时承担两种角色：

- **属性修改器（被动查询）：** 引擎算移速、护甲、攻速时来问它要数字
- **事件监听器（主动响应）：** 伤害发生、攻击命中、死亡等事件触发时它做出反应

### 3.2 Modifier 的完整结构

```lua
modifier_example = {
    -- ① 生命周期回调
    OnCreated  = function(self, kv) end,   -- 创建时
    OnRefresh  = function(self, kv) end,   -- 刷新时（重复施加）
    OnDestroy  = function(self) end,        -- 销毁时

    -- ② 内置状态数据
    --    duration       持续时间
    --    stack_count    层数
    --    parent         挂载单位
    --    ability        来源技能
    --    caster         施加者

    -- ③ 声明提供哪些属性/监听哪些事件
    DeclareFunctions = function(self)
        return {
            MODIFIER_PROPERTY_MOVESPEED_BONUS_PERCENTAGE,   -- 属性类
            MODIFIER_PROPERTY_PHYSICAL_ARMOR_BONUS,         -- 属性类
            MODIFIER_EVENT_ON_TAKEDAMAGE,                   -- 事件类
        }
    end,

    -- ④ 属性修改的具体实现
    GetModifierMoveSpeedBonus_Percentage = function(self)
        return self:GetAbility():GetSpecialValueFor("bonus_movespeed")
    end,
    GetModifierPhysicalArmorBonus = function(self)
        return self:GetAbility():GetSpecialValueFor("bonus_armor")
    end,

    -- ⑤ 事件响应的具体实现
    OnTakeDamage = function(self, event)
        if event.unit == self:GetParent() then
            -- 做出响应
        end
    end,
}
```

Modifier 不是 Entity + Components 的组合。不存在内部的组件化拆分，所有属性修改和事件响应都是同一个对象上的方法。`DeclareFunctions` 返回的枚举值不是"组件"，而是**注册索引的标记**。

### 3.3 属性修改的查询机制

引擎计算某个属性时，遍历该单位身上所有声明了对应属性的 Modifier：

```cpp
// 推测的引擎伪代码
float GetArmor(CBaseEntity* unit) {
    float base = unit->m_flBaseArmor;   // 来自 KV
    for (auto* mod : unit->m_PropertyProviders[PHYSICAL_ARMOR_BONUS]) {
        base += mod->GetModifierPhysicalArmorBonus();
    }
    return base;
}
```

属性表是**单位级别**的，因为算 Axe 的护甲只需要看 Axe 身上的 Modifier。

### 3.4 事件监听的派发机制

事件表是**全局级别**的，因为一个单位受伤时，可能有其他单位身上的 Modifier 需要响应（如队友增益光环）。

```cpp
// 推测的引擎伪代码
class CModifierManager {
    vector<CModifier*> m_EventListeners[EVENT_TYPE_COUNT];  // 全局
};

void FireEvent(ModifierEvent type, EventData& data) {
    for (auto* mod : m_EventListeners[type]) {
        mod->HandleEvent(type, data);   // 同步调用
    }
}
```

每个 Modifier 在被添加时注册到对应的列表，被移除时从列表注销：

```cpp
// 推测的引擎伪代码
void OnModifierAdded(CModifier* mod) {
    auto declarations = mod->DeclareFunctions();
    for (auto decl : declarations) {
        if (IsPropertyType(decl))
            mod->GetParent()->m_PropertyProviders[decl].push_back(mod);
        else if (IsEventType(decl))
            g_EventListeners[decl].push_back(mod);
    }
}

void OnModifierRemoved(CModifier* mod) {
    // 对称地从所有列表中移除
}
```

### 3.5 索引表的设计取舍

**为什么不每次全量扫描（ImGui 式的纯粹做法）：**

同一帧内同一个属性会被反复查询（移动要查移速、被攻击要查护甲、攻击要查攻速），全量扫描成本是 `查询次数 × Modifier 总数`。事件派发更严重——没有全局表就得遍历场上所有单位的所有 Modifier。

**为什么可以接受多个列表存同一个指针：**

列表存的是指针而非数据副本，Modifier 对象作为唯一真相只有一份。一个 Modifier 出现在多个列表里，和一本书同时出现在章节目录和关键词索引里是一样的——内容没有重复。

**空间开销：** 枚举类型约上百种，大部分列表在大部分时间为空或只有几个元素，整体几 KB。

**维护代价：** 仅在 AddModifier / RemoveModifier 时更新列表，频率远低于属性查询频率。

---

## 四、伤害管线：管线与决策点的分离

```
ApplyDamage(attacker, victim, damage, type, ability)
│
├── 第一阶段：伤害计算（C++ 管线）
│   ├── 查询 victim 的护甲 → 遍历 victim 的 ARMOR_BONUS Modifier
│   ├── 查询 victim 的魔抗 → 遍历 victim 的 MAGIC_RESIST Modifier
│   ├── 查询伤害格挡     → 遍历 victim 的 DAMAGE_BLOCK Modifier
│   └── 算出 finalDamage
│
├── 第二阶段：扣血
│   └── victim.hp -= finalDamage
│
├── 第三阶段：事件派发
│   └── 遍历 ON_TAKEDAMAGE 全局列表
│       ├── 反刺甲 → 自过滤（是我的持有者被打了吗？）→ ApplyDamage 反弹
│       ├── 薄葬   → 自过滤（血量低于阈值吗？）→ 恢复血量
│       └── ...每个 Listener 自己决定是否响应
│
└── 第四阶段：死亡检查
    └── if hp <= 0 → 派发 ON_DEATH 事件 → 同样的机制
```

**管线本身（阶段顺序、减伤公式）是 C++，固定不变。**
**管线中的决策点（加多少护甲、受伤后做什么）由 Modifier 提供，可通过 Lua 任意定制。**

---

## 五、KV 与 Lua 的分工边界

| 改动类型 | 改哪里 | 是否需要重编译 | 示例 |
|---------|--------|-------------|------|
| 调整已有技能的数值 | KV | 否 | 反旋伤害 200→180 |
| 新增已有机制的参数 | KV | 否 | 给反旋加一个 "stack_window" 参数 |
| 新增行为逻辑/条件分支 | Lua | 否 | 反旋连续触发伤害递增 |
| 新技能使用已有事件类型 | Lua + KV | 否 | 新英雄的被动监听 ON_ATTACK_LANDED |
| 新增事件类型 | C++ | 是 | 新增 ON_SILENCED 事件 |
| 新增属性类型 | C++ | 是 | 新增 HEAL_AMPLIFY 属性 |
| 伤害管线新增阶段 | C++ | 是 | 护甲和魔抗之间插入元素抗性 |
| 新增伤害类型 | C++ | 是 | 新增 DAMAGE_TYPE_ELEMENTAL |
| 网络协议变更 | C++ | 是 | Modifier 序列化格式改变 |

**判断规则：在已有框架的"空位"里填内容 → KV 或 Lua。在框架上开新的"空位" → C++。**

---

## 六、完整场景串联：Axe 团战

```
1. 引擎启动 → 加载 npc_heroes.txt → 组装 Axe 实体
              加载 npc_abilities.txt → 注册技能参数
              创建 modifier_axe_counter_helix（被动）
                → DeclareFunctions → [ON_ATTACK_LANDED]
                → 注册到全局 m_EventListeners[ON_ATTACK_LANDED]

2. Axe 施放嘲讽 → OnSpellStart (Lua)
   → FindUnitsInRadius(300) → 找到 B, C, D
   → 给 Axe 添加 modifier_berserkers_call_armor
     → DeclareFunctions → [PHYSICAL_ARMOR_BONUS]
     → 注册到 Axe.m_PropertyProviders[ARMOR_BONUS]
   → 给 B, C, D 各添加 modifier_berserkers_call_taunt
     → 强制攻击目标 = Axe

3. 敌人 B 被迫攻击 Axe → 攻击命中
   → 引擎计算物理伤害
     → 查询 Axe 护甲：遍历 Axe.providers[ARMOR_BONUS]
       → base 2 + modifier 25 = 27
     → 减伤公式 → finalDmg ≈ 18
   → 扣血：Axe.hp -= 18
   → 派发 ON_ATTACK_LANDED
     → 遍历全局 m_EventListeners[ON_ATTACK_LANDED]
       → modifier_axe_counter_helix.OnAttackLanded()
         → 自过滤：被打的是 Axe ✓
         → 20% 概率：假设触发 ✓
         → FindUnitsInRadius(275) → B, C, D
         → 对每个目标 ApplyDamage(200, PURE)
           → 进入伤害管线 → 扣血 → 派发 ON_TAKEDAMAGE
             → 如果有反刺甲：响应并反弹
             → 如果血量过低：死亡检查 → 派发 ON_DEATH

4. Axe 对残血目标 C 释放淘汰之刃 → OnSpellStart (Lua)
   → C.GetHealth() <= kill_threshold (来自 KV)?
     → 是 → C:Kill() → 派发 ON_DEATH
       → 冥魂大帝复活 Modifier 响应
       → 血石充能 Modifier 响应
     → 给周围队友添加加速 Modifier
```

---

## 七、与 Unity 技术栈的映射

| Dota 2 概念 | Unity 手游对应 |
|-----------|--------------|
| C++ 引擎层（改动需重编译） | IL2CPP 编译后的 C# 代码（改动需重新出包） |
| Lua 脚本层（运行时加载） | xLua 脚本（运行时加载 .lua 文件） |
| KV 数据层（运行时加载） | LuBan 配置表 / JSON / ScriptableObject |
| HybridCLR 的突破 | 把原本需要重编译的 C# 逻辑变成可热更的 DLL（"代码伪装成数据"） |
| Modifier 注册表 | C# event/delegate 多播委托 / 观察者模式 |
| DeclareFunctions 枚举 | 接口声明（IModifyArmor, IOnTakeDamage） |
| 全局事件派发 | 全局 EventBus + 观察者自过滤 |
| 属性表单位级查询 | 单位身上的 Modifier 栈按属性类型索引 |

**共同的架构直觉：管线固定、决策点可配置、数据与逻辑分层、索引在变更时维护而非查询时遍历。**
