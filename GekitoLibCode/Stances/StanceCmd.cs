using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace GekitoLib.Stances;

/// <summary>
/// 姿态切换命令。参考 StS1 观者：重复进入当前所在姿态不触发任何效果；
/// 切换到另一姿态时先退出旧姿态（触发其退出钩子）再进入新姿态。
/// 姿态必须先经 <see cref="StanceRegistry"/> 注册；所有已注册姿态互斥。
/// </summary>
public static class StanceCmd
{
    /// <summary>进入指定姿态。已处于该姿态时不触发任何效果。</summary>
    public static async Task EnterStance<TPower>(PlayerChoiceContext choiceContext, Creature creature) where TPower : PowerModel
    {
        var definition = StanceRegistry.All.FirstOrDefault(s => s.PowerType == typeof(TPower))
            ?? throw new InvalidOperationException($"Stance power {typeof(TPower).Name} is not registered in StanceRegistry.");
        if (creature.GetPower<TPower>() != null) return;

        // 互斥：先退出当前所在的其他姿态
        foreach (var other in StanceRegistry.All)
        {
            if (other.PowerType == definition.PowerType) continue;
            var current = FindStancePower(creature, other);
            if (current == null) continue;
            await PowerCmd.Remove(current);
            StanceTint.Clear(creature);
            await FireExitHooks(choiceContext, creature, current);
        }

        await PowerCmd.Apply<TPower>(choiceContext, creature, 1m, creature, null);
        if (definition.Tint.HasValue)
            StanceTint.Apply(creature, definition.Tint.Value);
        definition.OnEnterFeedback?.Invoke();

        var player = creature.Player;
        if (player == null) return;

        var stancePower = creature.GetPower<TPower>();
        if (stancePower == null) return;

        foreach (var relic in player.Relics)
        {
            if (relic is IAfterEnterStance hook)
                await hook.AfterEnterStance(choiceContext, creature, stancePower);
        }
        foreach (var power in creature.Powers.ToList())
        {
            if (power is IAfterEnterStance hook)
                await hook.AfterEnterStance(choiceContext, creature, stancePower);
        }

        // 卡牌钩子（如入某姿态时从各牌堆回手的效果）
        foreach (var pileType in new[] { PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust })
        {
            foreach (var card in pileType.GetPile(player).Cards.ToList())
            {
                if (card is IAfterEnterStance hook)
                    await hook.AfterEnterStance(choiceContext, creature, stancePower);
            }
        }
    }

    /// <summary>退出当前姿态（若有）。</summary>
    public static async Task ExitStance(Creature creature)
    {
        var ctx = new ThrowingPlayerChoiceContext();
        foreach (var definition in StanceRegistry.All)
        {
            var current = FindStancePower(creature, definition);
            if (current == null) continue;
            await PowerCmd.Remove(current);
            StanceTint.Clear(creature);
            await FireExitHooks(ctx, creature, current);
        }
    }

    private static PowerModel? FindStancePower(Creature creature, StanceDefinition definition)
    {
        return creature.Powers.FirstOrDefault(p => definition.PowerType.IsInstanceOfType(p));
    }

    /// <summary>触发所有遗物/能力的退出姿态钩子（与进入钩子的遍历方式一致）。</summary>
    private static async Task FireExitHooks(PlayerChoiceContext choiceContext, Creature creature, PowerModel stancePower)
    {
        var player = creature.Player;
        if (player == null) return;

        foreach (var relic in player.Relics)
        {
            if (relic is IAfterExitStance hook)
                await hook.AfterExitStance(choiceContext, creature, stancePower);
        }
        foreach (var power in creature.Powers.ToList())
        {
            if (power is IAfterExitStance hook)
                await hook.AfterExitStance(choiceContext, creature, stancePower);
        }
    }
}
