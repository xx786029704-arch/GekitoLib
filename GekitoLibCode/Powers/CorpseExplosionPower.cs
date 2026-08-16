using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace GekitoLib.Powers;

/// <summary>
/// 尸爆术（参考 StS1 的 Corpse Explosion）：
/// 持有者死亡时，对所有其他敌人造成「持有者最大生命值 × 层数」的伤害。
/// </summary>
public sealed class CorpseExplosionPower : CustomPowerModel
{
    public override string CustomPackedIconPath => "res://GekitoLib/images/powers/corpseexplosionpower.png";
    public override string CustomBigIconPath => "res://GekitoLib/images/powers/big/corpseexplosionpower.png";

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (creature != Owner || wasRemovalPrevented || Owner.CombatState is not { } combatState)
            return;

        var targets = combatState.Enemies.Where(enemy => enemy != Owner && enemy.IsAlive).ToList();
        if (targets.Count == 0)
            return;

        Flash();
        await CreatureCmd.Damage(
            choiceContext, targets, Owner.MaxHp * Amount,
            ValueProp.Unpowered, null, null, null);
    }
}
