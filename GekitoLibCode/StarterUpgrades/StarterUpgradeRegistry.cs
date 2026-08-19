using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace GekitoLib.StarterUpgrades;

/// <summary>
/// 初始卡 / 初始遗物升级链注册。
/// 「先古之齿（ArchaicTooth）」与「奥罗巴斯之触（TouchOfOrobas）」的替换映射
/// 都是每次访问新建表达式属性的私有字典，无法反射注入，只能以 Postfix 修改 getter 结果。
/// 本注册表统一收集各 mod 的替换关系，由两个 patch 分别注入。
/// ⚠️ 注册表存 <see cref="Type"/> 而非模型实例：ModInitializer 阶段模型尚未注册进
/// <see cref="ModelDb"/>（此处查询会抛 ModelNotFoundException），须延迟到 patch 触发时（游戏运行中）解析。
/// ⚠️ 初始卡的升级状态继承是原版行为、无需注册方处理：
/// <see cref="ArchaicTooth.GetTranscendenceTransformedCard"/> 内部会按
/// 初始卡是否已升级对新卡执行 Upgrade / 拷贝附魔（见 src 源码），本库只注入字典本体，
/// 升级继承自动生效。注入字典本体（而非仅替换结果）的意义见 EMK 决策记录 2026-08-05。
/// </summary>
public static class StarterUpgradeRegistry
{
    /// <summary>初始卡替换映射：初始卡类型 → 先古卡类型。先古卡与配套选项本地化由使用方提供。</summary>
    private static readonly Dictionary<Type, Type> StarterCardUpgrades = [];

    /// <summary>初始遗物替换映射：初始遗物类型 → 升级遗物类型。升级遗物与配套选项本地化由使用方提供。</summary>
    private static readonly Dictionary<Type, Type> StarterRelicUpgrades = [];

    /// <summary>注册初始卡 → 先古卡替换（先古之齿事件）。重复注册同一初始卡会覆盖旧映射。</summary>
    public static void RegisterCardUpgrade(Type starter, Type upgraded)
        => StarterCardUpgrades[starter] = upgraded;

    /// <summary>泛型便捷重载：按卡牌类型注册替换。</summary>
    public static void RegisterCardUpgrade<TStarter, TUpgraded>()
        where TStarter : CardModel
        where TUpgraded : CardModel
        => RegisterCardUpgrade(typeof(TStarter), typeof(TUpgraded));

    /// <summary>注册初始遗物 → 升级遗物替换（奥罗巴斯之触事件）。重复注册同一初始遗物会覆盖旧映射。</summary>
    public static void RegisterRelicUpgrade(Type starter, Type upgraded)
        => StarterRelicUpgrades[starter] = upgraded;

    /// <summary>泛型便捷重载：按遗物类型注册替换。</summary>
    public static void RegisterRelicUpgrade<TStarter, TUpgraded>()
        where TStarter : RelicModel
        where TUpgraded : RelicModel
        => RegisterRelicUpgrade(typeof(TStarter), typeof(TUpgraded));

    internal static IReadOnlyDictionary<Type, Type> CardUpgrades => StarterCardUpgrades;
    internal static IReadOnlyDictionary<Type, Type> RelicUpgrades => StarterRelicUpgrades;
}

/// <summary>
/// 把注册的初始卡替换注入原版 ArchaicTooth.TranscendenceUpgrades getter。
/// getter 每次访问都新建字典，Postfix 在返回前直接追加条目即可，
/// 同时覆盖 GetTranscendenceStarterCard（按 key 识别可替换初始牌）、
/// GetTranscendenceTransformedCard（执行替换）与 TranscendenceCards（尘封典籍排除名单）。
/// </summary>
[HarmonyPatch(typeof(ArchaicTooth), "TranscendenceUpgrades", MethodType.Getter)]
internal static class ArchaicToothPatch
{
    private static void Postfix(Dictionary<ModelId, CardModel> __result)
    {
        foreach (var pair in StarterUpgradeRegistry.CardUpgrades)
        {
            ModelId starterId = ModelDb.GetId(pair.Key);
            CardModel upgraded = ModelDb.GetById<CardModel>(ModelDb.GetId(pair.Value));
            __result[starterId] = upgraded;
        }
    }
}

/// <summary>
/// 把注册的初始遗物替换注入原版 TouchOfOrobas.GetUpgradedStarterRelic。
/// SetupForPlayer 与 AfterObtained 都经此方法取升级遗物，postfix 改返回值即可两处生效。
/// 原版默认返回 <see cref="Circlet"/>（头环），注册表命中时覆盖之。
/// </summary>
[HarmonyPatch(typeof(TouchOfOrobas), nameof(TouchOfOrobas.GetUpgradedStarterRelic))]
internal static class TouchOfOrobasPatch
{
    private static void Postfix(RelicModel starterRelic, ref RelicModel __result)
    {
        foreach (var pair in StarterUpgradeRegistry.RelicUpgrades)
        {
            if (starterRelic.Id == ModelDb.GetId(pair.Key))
                __result = ModelDb.GetById<RelicModel>(ModelDb.GetId(pair.Value));
        }
    }
}