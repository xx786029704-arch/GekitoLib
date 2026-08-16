using System.Collections.Generic;
using BaseLib.Abstracts;
using GekitoLib.Keywords;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace GekitoLib.Enchantments;

/// <summary>
/// 导管附魔：为卡牌添加「焊接」关键词（跨 mod 共用，见 GekitoLib.Keywords）。
/// 参考 RoyallyApproved（附魔改关键词），反序列化后 ModifyCard 会重跑 OnEnchant，无需额外持久化。
/// </summary>
public class DaoGuan : CustomEnchantmentModel
{
    protected override string? CustomIconPath => $"{MainFile.ResPath}/images/enchantments/dao_guan.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(GekitoLibKeywords.Weld)];

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType is CardType.Attack or CardType.Skill;
    }

    public override bool CanEnchant(CardModel card)
    {
        return base.CanEnchant(card) && !card.Keywords.Contains(GekitoLibKeywords.Weld);
    }

    protected override void OnEnchant()
    {
        Card.AddKeyword(GekitoLibKeywords.Weld);
    }
}
