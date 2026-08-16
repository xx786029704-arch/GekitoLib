# GekitoLib 内容文档

激类库（GekitoLib）是 Slay the Spire 2 的共享机制库 Mod，把多个角色 Mod 会重复实现的通用机制收敛到同一处实现、同一命名空间（`GekitoLib.*`）。使用方 Mod 在自己的 `.json` 清单里声明依赖：

```json
"dependencies": [
  {"id": "BaseLib", "min_version": "3.3.8"},
  {"id": "GekitoLib", "min_version": "0.1.0"}
]
```

并在 csproj 中引用游戏 mods 目录下的 `GekitoLib.dll`（`<Private>false</Private>`，不要随自己 Mod 重复分发 dll）。

当前包含以下机制：

## 1. 焊接连锁（Weld Chain） — `GekitoLib.Weld`

**机制**：打出一张带「焊接」关键词的牌时，依次自动打出持有者手牌、弃牌堆、抽牌堆中所有其他焊接牌。

**组成**：

| 成员 | 说明 |
|------|------|
| `GekitoLibKeywords.Weld` | 共享焊接关键词（`[CustomEnum]` 注入，所有 Mod 共用同一实例，跨 Mod 的焊接牌可互相连锁；定义于 `GekitoLib.Keywords`）。本地化 key：`GEKITOLIB-WELD` |
| `KeywordChainPlay` | 连锁引擎。内置已注册 Weld；其他 Mod 可用 `KeywordChainPlay.Register(keyword, piles...)` 注册自己的连锁关键词（piles 缺省为 手牌/弃牌堆/抽牌堆） |
| `DaoGuan`（`GekitoLib.Enchantments`） | 导管附魔：为攻击/技能牌添加「焊接」关键词。本地化 key：`GEKITOLIB-DAO_GUAN` |

**实现要点**（改动机制前必读）：
- 连锁候选在打出的 `OnPlayWrapper` **执行前**快照（Postfix 里同步取候选列表，再把连锁续接到原 Task 之后）——防止「打出时为其他牌添加焊接」的效果导致刚加焊接的牌被立刻连锁，也防止连锁途中被打进弃牌堆的牌被二次枚举。
- 连锁打出的牌带防重入标记，其打出不再触发连锁；其他效果（AutoPlay 等）打出的焊接牌仍可正常触发连锁。
- X 费牌（能量 X 或辉星 X）连锁时先 `SpendResources()` 再 `AutoPlay(skipXCapture: true)`。
- 原目标已死时以 null 目标 AutoPlay。

**典型用法**：

```csharp
// 卡牌效果：为一张牌添加「焊接」
CardCmd.ApplyKeyword(card, GekitoLibKeywords.Weld);

// 遗物：从牌组选牌附魔「导管」
var enchantment = ModelDb.Enchantment<DaoGuan>();
foreach (var card in await CardSelectCmd.FromDeckForEnchantment(Owner, enchantment, 1, prefs))
{
    CardCmd.Enchant(enchantment.ToMutable(), card, 1);
    CardCmd.Preview(card);
}
```

## 2. 互斥姿态框架（Stance Framework） — `GekitoLib.Stances`

**机制**：参考 StS1 观者的姿态系统。姿态以「姿态能力」（PowerModel 子类）为载体，所有已注册姿态互斥——同一时刻至多处于一种姿态；重复进入当前姿态不触发任何效果；切换到另一姿态时先退出旧姿态（触发退出钩子）再进入新姿态。

**组成**：

| 成员 | 说明 |
|------|------|
| `StanceRegistry.Register<TPower>(tint, onEnterFeedback)` | 在使用方 Mod 的 ModInitializer 中注册姿态。`tint` 为进入姿态时的角色身体染色（可空）；`onEnterFeedback` 为进入反馈回调（音效/特效，可空）。重复注册同一类型被忽略 |
| `StanceCmd.EnterStance<TPower>(choiceContext, creature)` | 进入姿态。未注册的类型抛 `InvalidOperationException` |
| `StanceCmd.ExitStance(creature)` | 退出当前姿态（若有） |
| `IAfterEnterStance` / `IAfterExitStance` | 进出姿态钩子。遗物/能力/卡牌实现后由框架回调（进入钩子额外遍历 抽牌堆/手牌/弃牌堆/消耗堆 中的卡牌）。回调参数 `stancePower` 为姿态能力实例，用 `is not MyStancePower` 模式匹配判断具体姿态 |
| `StanceTint` | 生物身体贴图染色工具（要求战斗形象由 BaseLib `NCreatureVisualsFactory` 从单张 Texture2D 构建，子节点 "Visuals" 为身体 Sprite2D）。一般无需直接调用，注册时给 `tint` 即可 |

**使用流程**：

```csharp
// 1. 定义姿态能力（StackType.Single）
public class MyStancePower : CustomPowerModel { /* Type/StackType/图标/本地化由你的 Mod 提供 */ }

// 2. 在你的 ModInitializer 中注册（身体染色 + 进入音效均可选）
StanceRegistry.Register<MyStancePower>(new Color("b3d1ff"), () => myEnterSfx.Play());

// 3. 卡牌/遗物效果中进入姿态
await StanceCmd.EnterStance<MyStancePower>(choiceContext, Owner.Creature);

// 4. 「每当进入某姿态时…」的效果：遗物/能力/卡牌实现钩子接口
public async Task AfterEnterStance(PlayerChoiceContext choiceContext, Creature creature, PowerModel stancePower)
{
    if (stancePower is not MyStancePower) return;
    // …
}
```

**注意**：姿态能力的本地化（title/description/smartDescription）与图标由使用方 Mod 自己提供，框架不含任何具体姿态。

## 3. 临时属性能力 — `GekitoLib.Powers.TemporaryStatPower<TStatPower>`

**机制**：能力存续期间给予宿主等量的另一属性能力（力量/敏捷等），本能力被移除时等额回收（`AfterApplied`/`AfterRemoved` 对称结算，`silent: true` 不播图标弹跳）。叠层/减层时内部属性随层数差值实时同步，移除时按当前层数回收，不会出现内部属性越过 0 翻转。

- 默认临时属性数值 = 本能力层数（`Amount`）；需与层数解耦时重写 `protected virtual decimal TempAmount`（如改用 `DynamicVars`）。
- 派生类需要额外的进入/退出效果时重写 `AfterApplied`/`AfterRemoved` 并调用 `base.`。
- 图标路径等 `CustomPowerModel` 成员由派生类自行重写（本基类不管美术）。

**实现要点**（改动机制前必读）：
- 叠层同步在 `AfterPowerAmountChanged` 中完成，两个过滤条件缺一不可：
  - `power != this`：内部 `TStatPower` 的 Apply 也会触发全局 `Hook.AfterPowerAmountChanged`，必须过滤，防止内部属性变化引发连锁误同步（死循环）。
  - `amount == Amount`：首次应用时 `AfterApplied` 已同步过，随后钩子会带相同的量再触发一次，跳过可防止双重结算。
- 参考实现：原版 `TemporaryStrengthPower` / BaseLib `CustomTemporaryPowerModel` 的 `AfterPowerAmountChanged` 均为同款「差值同步 + 双条件过滤」模式。
- `StackType` 使用方通常设为 `Counter`（叠层才有意义）；`Single` 等不叠层的用法不受此同步影响。

**典型用法**：

```csharp
// 存续期间 +1 敏捷、移除时收回（数值 = 层数）
public class MyGuardPower : TemporaryStatPower<DexterityPower>
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}

// 数值与层数解耦（层数恒为 1，数值来自 DynamicVars），且附加额外结算
public class MyRagePower : TemporaryStatPower<StrengthPower>
{
    protected override decimal TempAmount => DynamicVars[nameof(StrengthPower)].BaseValue;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        // 额外效果…
    }
}
```

## 4. 沉底词条 — `GekitoLib.Keywords`

**机制**：带 `GekitoLibKeywords.Bottom` 关键词的卡牌在战斗开始（初始洗牌）时被放到初始抽牌堆底部。使用方式与焊接等关键词完全一致——在卡牌 `CanonicalKeywords` 中显式声明即可，**无需实现接口或覆写基类**；行为由 `BottomPlaceCardPatches` 以 Harmony Postfix 全局实现。

**组成**：

| 成员 | 说明 |
|------|------|
| `GekitoLibKeywords.Bottom` | 共享「沉底」关键词（`[CustomEnum]` 注入，所有 Mod 共用同一实例）。本地化 key：`GEKITOLIB-BOTTOM` |
| `BottomPlaceCardPatches` | 全局实现：`ModifyShuffleOrder` Postfix（检测卡牌带 Bottom 关键词则初始洗牌沉底，幂等） |

**实现要点**（改动机制前必读）：
- Postfix 是**追加式**（不跳过原方法），不覆盖使用方自身的 `ModifyShuffleOrder` 覆写逻辑；`cards.Remove` 失败（已被使用方逻辑移走）则静默跳过，幂等。
- 沉底语义限定「初始洗牌」（`isInitialShuffle == true`），战斗中洗牌不受影响。
- 关键词位置为 `AutoKeywordPosition.After`；从旧 Mod 迁入时卡牌关键词 ID 会变化，旧存档兼容性按「通用注意事项」处理。

**典型用法**：

```csharp
// 使用方卡牌：在 CanonicalKeywords 中显式声明（与焊接一致）
public sealed class MyRitualCard : MyBaseCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [GekitoLibKeywords.Bottom];
}
```

## 5. 尸爆术 — `GekitoLib.Powers.CorpseExplosionPower`

**机制**：持有者死亡时，对所有其他敌人造成「持有者最大生命值 × 层数」的伤害（参考 StS1 的 Corpse Explosion）。

**组成**：

| 成员 | 说明 |
|------|------|
| `CorpseExplosionPower` | Debuff / Counter。`AfterDeath` 中结算，伤害 `ValueProp.Unpowered`（不吃力量等修正）。本地化 key：`GEKITOLIB-CORPSE_EXPLOSION_POWER` |

**实现要点**：
- `wasRemovalPrevented` 为 true（死亡被阻止，如重生）时不触发；目标排除持有者自身且仅存活敌人。
- 图标：`images/powers/corpseexplosionpower.png`（小）+ `big/`（大）。

**典型用法**：

```csharp
await PowerCmd.Apply<CorpseExplosionPower>(choiceContext, target, stacks, Owner.Creature, this);
```

## 6. 多层护甲 — `GekitoLib.Powers.LayeredArmorPower`

**机制**：StS1 的 Plated Armor——回合结束时获得「层数」点格挡；受到未被格挡的攻击伤害后层数 -1（不会像 StS2 镀层那样每回合衰减）。

**组成**：

| 成员 | 说明 |
|------|------|
| `LayeredArmorPower` | Buff / Counter。`BeforeSideTurnEndEarly` 获得格挡、`AfterDamageReceived` 未格挡攻击伤害后 `Decrement`。本地化 key：`GEKITOLIB-LAYERED_ARMOR_POWER` |

**实现要点**：
- 只对 `ValueProp.Move` 的攻击伤害（且非自身/非无来源）触发减层；`result.UnblockedDamage <= 0` 不触发。
- 自带 StS1 风格音效（`audio/powers/layered_armor.ogg`）与格挡 HoverTip。
- 图标：`images/powers/layeredarmor.png`（小）+ `big/`（大）。

**典型用法**：

```csharp
await PowerCmd.Apply<LayeredArmorPower>(choiceContext, Owner.Creature, stacks, Owner.Creature, this);
```

## 通用注意事项

- **ID 稳定性**：关键词/附魔的本地化 key 以 `GEKITOLIB-` 为前缀，是所有使用方 Mod 共用的公共资源，改名即破坏性变更。从别的 Mod 迁入本库的内容其 ID 会变化，旧存档中已附魔的卡牌会失效——EA 阶段可接受，发布说明需注明。
- **加载顺序**：使用方 Mod 声明 `dependencies` 后，游戏保证 GekitoLib 先于其初始化（`StanceRegistry.Register` 在自己 Mod 的 ModInitializer 中调用是安全的）。
- **构建顺序**：本库 dll 输出到游戏 `mods/GekitoLib/`；使用方 Mod 通过 HintPath 引用该 dll，因此**必须先构建 GekitoLib 再构建使用方 Mod**。
- 本库的 Harmony patch：`KeywordChainPlay` 内部的 `CardModel.OnPlayWrapper` Postfix、`BottomPlaceCardPatches`（`GekitoLib.Patches`）的 `CardModel.ModifyShuffleOrder` Postfix，均为追加式，与使用方 Mod 自身的 patch 无冲突。
