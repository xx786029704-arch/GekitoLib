using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace GekitoLib.Events;

/// <summary>
/// 色彩哲学家候选池注册。原版 <see cref="MegaCrit.Sts2.Core.Models.Events.ColorfulPhilosophers"/> 的候选池
/// <c>CardPoolColorOrder</c> 是硬编码 5 个原版池的表达式属性，无法反射注入；
/// 本注册表收集各 mod 的卡池，patch 以 Postfix 追加进候选池。
/// 使用方除注册卡池外，还须在自己的本地化中提供事件选项文案：
/// <c>COLORFUL_PHILOSOPHERS.pages.INITIAL.options.&lt;卡池 EnergyColorName 大写&gt;</c>。
/// ⚠️ 注册表存 <see cref="Type"/> 而非模型实例：ModInitializer 阶段模型尚未注册进
/// <see cref="ModelDb"/>（此处查询会抛 ModelNotFoundException），须延迟到 patch 触发时（游戏运行中）解析。
/// </summary>
public static class ColorfulPhilosophersPools
{
    private static readonly List<Type> Pools = [];

    /// <summary>把卡池类型加入色彩哲学家候选池。重复注册同一类型忽略。</summary>
    public static void AddPool(Type pool)
    {
        if (pool != null && !Pools.Contains(pool))
            Pools.Add(pool);
    }

    /// <summary>泛型便捷重载：按池类型注册。</summary>
    public static void AddPool<TPool>() where TPool : CardPoolModel
        => AddPool(typeof(TPool));

    internal static IEnumerable<Type> ExtraPools => Pools;
}

[HarmonyPatch(typeof(ColorfulPhilosophers), "get_CardPoolColorOrder")]
internal static class ColorfulPhilosophersPatch
{
    private static void Postfix(ref IEnumerable<CardPoolModel> __result)
    {
        List<CardPoolModel> pools = __result.ToList();
        foreach (Type type in ColorfulPhilosophersPools.ExtraPools)
        {
            CardPoolModel? pool = ModelDb.GetByIdOrNull<CardPoolModel>(ModelDb.GetId(type));
            if (pool != null && !pools.Contains(pool))
                pools.Add(pool);
        }
        __result = pools;
    }
}