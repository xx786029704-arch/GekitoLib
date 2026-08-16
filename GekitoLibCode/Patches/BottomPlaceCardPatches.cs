using System.Collections.Generic;
using System.Linq;
using GekitoLib.Keywords;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace GekitoLib.Patches;

/// <summary>
/// 「沉底」关键词的全局行为：带 <see cref="GekitoLibKeywords.Bottom"/> 关键词的卡牌
/// 在战斗开始（初始洗牌）时被移到初始抽牌堆底部。
/// 使用方式与焊接一致：卡牌在 <c>CanonicalKeywords</c> 中显式声明关键词即可，无需实现接口或覆写基类。
/// 追加式 Postfix：不覆盖使用方自身的 <c>ModifyShuffleOrder</c> 覆写逻辑；<c>cards.Remove</c> 失败则静默跳过（幂等）。
/// ⚠️ 目标方法声明在 <see cref="AbstractModel"/>（模型基类），patch 必须指向 AbstractModel 而非 CardModel。
/// </summary>
[HarmonyPatch]
internal static class BottomPlaceCardPatches
{
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.ModifyShuffleOrder))]
    [HarmonyPostfix]
    private static void PlaceAtBottom(AbstractModel __instance, Player player, List<CardModel> cards, bool isInitialShuffle)
    {
        if (__instance is not CardModel card || !isInitialShuffle || player != card.Owner ||
            !card.CanonicalKeywords.Contains(GekitoLibKeywords.Bottom))
            return;
        if (cards.Remove(card))
            cards.Add(card);
    }
}
