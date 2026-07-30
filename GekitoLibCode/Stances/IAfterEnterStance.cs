using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace GekitoLib.Stances;

/// <summary>
/// 进入姿态时触发的钩子。遗物/能力/卡牌实现此接口后，
/// <see cref="StanceCmd"/> 会在进入任意已注册姿态时回调。
/// </summary>
public interface IAfterEnterStance
{
    /// <param name="stancePower">刚施加的姿态能力实例，用 is 模式匹配判断具体姿态。</param>
    Task AfterEnterStance(PlayerChoiceContext choiceContext, Creature creature, PowerModel stancePower);
}
