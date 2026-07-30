using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace GekitoLib.Stances;

/// <summary>
/// 退出姿态时触发的钩子。遗物/能力实现此接口后，
/// <see cref="StanceCmd"/> 会在退出任意已注册姿态时回调。
/// </summary>
public interface IAfterExitStance
{
    /// <param name="stancePower">刚被移除的姿态能力实例，用 is 模式匹配判断具体姿态。</param>
    Task AfterExitStance(PlayerChoiceContext choiceContext, Creature creature, PowerModel stancePower);
}
