using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace GekitoLib.Keywords;

/// <summary>
/// 激类库共享卡牌关键词。BaseLib 通过 [CustomEnum] 字段注入枚举值，
/// 本地化在 GekitoLib/localization/*/card_keywords.json（key = GEKITOLIB-字段名大写）。
/// 所有使用本机制的 mod 共用同一关键词实例，跨 mod 的牌可以互相联动。
/// 使用方式与卡牌其他关键词一致：在 <c>CanonicalKeywords</c> 中直接声明即可。
/// </summary>
public static class GekitoLibKeywords
{
    /// <summary>焊接：打出该牌时，同时打出手牌、弃牌堆以及抽牌堆中所有焊接牌。</summary>
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Weld;

    /// <summary>沉底：战斗开始时，将这张牌放在初始抽牌堆的底部。</summary>
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Bottom;
}
