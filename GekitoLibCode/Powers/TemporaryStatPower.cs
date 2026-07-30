using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace GekitoLib.Powers;

/// <summary>
/// 临时属性能力基类：能力存续期间给予宿主等量的另一属性能力（力量/敏捷等），
/// 本能力被移除时等额回收（AfterApplied/AfterRemoved 对称结算，silent 不播图标弹跳）。
/// 层数（Amount）即临时属性数值；需要与层数解耦时重写 <see cref="TempAmount"/> 改用 DynamicVars。
/// </summary>
/// <typeparam name="TStatPower">临时给予的属性能力类型（如 StrengthPower/DexterityPower）。</typeparam>
public abstract class TemporaryStatPower<TStatPower> : CustomPowerModel where TStatPower : PowerModel
{
    /// <summary>临时属性的数值。默认取本能力层数。</summary>
    protected virtual decimal TempAmount => Amount;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await PowerCmd.Apply<TStatPower>(new ThrowingPlayerChoiceContext(), Owner, TempAmount, Owner, null, silent: true);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        await PowerCmd.Apply<TStatPower>(new ThrowingPlayerChoiceContext(), oldOwner, -TempAmount, oldOwner, null, silent: true);
    }
}
