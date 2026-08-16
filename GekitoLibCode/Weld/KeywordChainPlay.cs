using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GekitoLib.Keywords;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace GekitoLib.Weld;

/// <summary>
/// 关键词连锁打出引擎。通过 <see cref="Register"/> 注册关键词后，
/// 打出一张带该关键词的牌时，会依次自动打出持有者指定牌堆中所有其他带该关键词的牌。
/// 连锁打出的牌带防重入标记，其打出不再触发连锁，避免无限循环；
/// 其他效果（AutoPlay 等）打出的同关键词牌仍可正常触发连锁。
/// 激类库内置注册「焊接」（<see cref="GekitoLibKeywords.Weld"/>），其他 mod 可直接使用该关键词，
/// 也可注册自己的连锁关键词。
/// </summary>
public static class KeywordChainPlay
{
    private static readonly Dictionary<CardKeyword, PileType[]> Registered = new();

    static KeywordChainPlay()
    {
        Register(GekitoLibKeywords.Weld);
    }

    /// <summary>注册连锁关键词。piles 缺省为手牌/弃牌堆/抽牌堆。</summary>
    public static void Register(CardKeyword keyword, params PileType[] piles)
    {
        Registered[keyword] = piles is { Length: > 0 } ? piles : [PileType.Hand, PileType.Discard, PileType.Draw];
    }

    /// <summary>该牌是否带有任一已注册的连锁关键词。</summary>
    internal static IEnumerable<KeyValuePair<CardKeyword, PileType[]>> MatchingChains(CardModel card)
    {
        return Registered.Where(pair => card.Keywords.Contains(pair.Key));
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
    private static class ChainPatch
    {
        private static readonly HashSet<CardModel> ChainingCards = [];

        [HarmonyPostfix]
        public static void Postfix(ref Task __result, CardModel __instance, PlayerChoiceContext choiceContext, Creature? target, bool isAutoPlay)
        {
            if (ChainingCards.Contains(__instance)) return;

            // 在打出效果执行前快照连锁候选：「为手牌添加关键词」这类效果在 OnPlay 中才生效，
            // 若等原任务完成再扫描，刚被添加关键词的牌会被立刻连锁打出；
            // 且逐堆延迟扫描会让连锁中被打进弃牌堆的牌再次被枚举到（同一张牌打两次）。
            var candidates = MatchingChains(__instance)
                .SelectMany(pair => pair.Value.SelectMany(pileType => pileType.GetPile(__instance.Owner).Cards)
                    .Where(c => c != __instance && c.Keywords.Contains(pair.Key)))
                .Distinct()
                .ToList();
            if (candidates.Count == 0) return;
            __result = Chain(__result, candidates, choiceContext, target);
        }

        private static async Task Chain(Task original, List<CardModel> candidates, PlayerChoiceContext choiceContext, Creature? target)
        {
            await original;

            foreach (var card in candidates)
            {
                // 连锁途中卡牌可能被其他效果移动，打出前确认仍在可连锁的牌堆
                if (card.Pile == null) continue;
                if (card.Pile.Type != PileType.Hand && card.Pile.Type != PileType.Discard && card.Pile.Type != PileType.Draw) continue;
                await PlayCardViaChain(choiceContext, card, target);
            }
        }

        private static async Task PlayCardViaChain(PlayerChoiceContext choiceContext, CardModel card, Creature? target)
        {
            var validTarget = target is { IsAlive: true } ? target : null;

            ChainingCards.Add(card);
            try
            {
                if (card.EnergyCost.CostsX || card.HasStarCostX)
                {
                    await card.SpendResources();
                    await CardCmd.AutoPlay(choiceContext, card, validTarget, skipXCapture: true);
                }
                else
                {
                    await CardCmd.AutoPlay(choiceContext, card, validTarget);
                }
            }
            finally
            {
                ChainingCards.Remove(card);
            }
        }
    }
}
