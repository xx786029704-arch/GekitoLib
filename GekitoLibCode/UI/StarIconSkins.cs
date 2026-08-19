using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers.Models;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization.Formatters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.addons.mega_text;
using SmartFormat.Core.Extensions;

namespace GekitoLib.UI;

/// <summary>
/// 星图标皮肤：把某 mod 的「辉星」视觉整体换成自定义贴图（仅视觉替换，底层仍是游戏内置 Stars 资源）。
/// 用 <see cref="StarIconSkins.Register{TMarker}"/> 注册一套皮肤后，
/// 该 mod 程序集内定义的所有模型（卡牌/遗物/能力/角色）自动套用：
/// 卡牌星费图标、左下角计数器（主图标/旋转装饰层/数字描边/获得闪光/悬停文案）、
/// 多人玩家状态条、文本内联图标（{Stars:starIcons()} 格式化器 / {singleStarIcon} 变量）与描述内成品串替换。
/// </summary>
public sealed class StarIconSkin
{
    /// <summary>大图标（计数器主图标 / 卡牌星费图标 / 多人面板），对应原版 res://images/ui/combat/energy_star.png。</summary>
    public required string BigIconPath;

    /// <summary>文本内联小图标（{Stars:starIcons()} / {singleStarIcon}），对应原版 res://images/packed/sprite_fonts/star_icon.png。</summary>
    public required string TextIconPath;

    /// <summary>计数器获得星时的闪光特效贴图（白色剪影，由原粒子材质染色）。可空 = 保留原版。</summary>
    public string? GlowPath;

    /// <summary>计数器旋转装饰层 1 贴图（淡金色版）。可空 = 保留原版。</summary>
    public string? Layer2Path;

    /// <summary>计数器旋转装饰层 2 贴图。可空 = 保留原版。</summary>
    public string? Layer3Path;

    /// <summary>星数数字描边色（计数器 %CountLabel + 卡牌星费 %StarLabel 默认态）。可空 = 保留原版。</summary>
    public Color? LabelOutlineColor;

    /// <summary>计数器悬停标题。可空 = 保留原版文案。</summary>
    public LocString? CounterHoverTitle;

    /// <summary>计数器悬停描述。可空 = 保留原版文案（注意原版描述较简短，自定义时最好同时给 title 与 description）。</summary>
    public LocString? CounterHoverDescription;

    /// <summary>文本内联图标的 bbcode 标签。</summary>
    public string TextIconTag => $"[img]{TextIconPath}[/img]";
}

/// <summary>
/// 星图标皮肤注册表。每个使用方 mod 注册一套皮肤，归属判定基于「模型类型所在程序集」：
/// 模型（卡牌/遗物/能力）与角色类型都定义在使用方自己的程序集内，注册时传本 mod 任意类型即可。
/// 未注册皮肤的 mod / 原版内容全部保持原版视觉。
/// </summary>
public static class StarIconSkins
{
    private static readonly List<(Assembly Assembly, StarIconSkin Skin)> Skins = [];

    /// <summary>注册一套皮肤：程序集取 <typeparamref name="TMarker"/> 所在程序集，通常传本 mod 的角色类型或 MainFile。</summary>
    public static void Register<TMarker>(StarIconSkin skin) where TMarker : class
        => Register(typeof(TMarker).Assembly, skin);

    /// <summary>注册一套皮肤（显式程序集）。重复注册同一程序集视为覆盖。</summary>
    public static void Register(Assembly assembly, StarIconSkin skin)
    {
        int index = Skins.FindIndex(e => e.Assembly == assembly);
        if (index >= 0)
            Skins[index] = (assembly, skin);
        else
            Skins.Add((assembly, skin));
    }

    /// <summary>该模型是否属于任一已注册皮肤。</summary>
    internal static StarIconSkin? FindForModel(AbstractModel? model) =>
        model == null ? null : Find(model.GetType().Assembly);

    /// <summary>该角色是否属于任一已注册皮肤（按角色类型程序集）。</summary>
    internal static StarIconSkin? FindForCharacter(CharacterModel? character) =>
        character == null ? null : Find(character.GetType().Assembly);

    private static StarIconSkin? Find(Assembly assembly)
    {
        for (int i = Skins.Count - 1; i >= 0; i--)
        {
            if (Skins[i].Assembly == assembly)
                return Skins[i].Skin;
        }
        return null;
    }

    /// <summary>把成品文本中的原版星图标路径替换为该皮肤路径。</summary>
    internal static string ReplaceVanillaIcons(StarIconSkin skin, string text) =>
        text.Replace(StarIconReskin.VanillaTextIconPath, skin.TextIconPath);
}

/// <summary>
/// 星图标换皮共享实现（内部工具，patch 使用）。判定逻辑集中在注册表 <see cref="StarIconSkins"/>。
/// </summary>
internal static class StarIconReskin
{
    /// <summary>原版文本内联星图标路径（精灵字体内）。</summary>
    public const string VanillaTextIconPath = "res://images/packed/sprite_fonts/star_icon.png";

    /// <summary>原版大星图标路径（计数器/卡牌/多人面板场景固定引用）。</summary>
    public const string VanillaBigIconPath = "res://images/ui/combat/energy_star.png";

    private static readonly FieldInfo? OwnerField = AccessTools.Field(typeof(DynamicVar), "_owner");
    private static readonly FieldInfo? CounterPlayerField = AccessTools.Field(typeof(NStarCounter), "_player");
    private static readonly FieldInfo? CounterHoverTipField = AccessTools.Field(typeof(NStarCounter), "_hoverTip");

    /// <summary>正在格式化悬停提示的 PowerModel（{Amount:starIcons()} 这类无 owner 裸值靠它判定归属）。</summary>
    public static PowerModel? PowerContext;

    /// <summary>格式化上下文判定：优先当前正在格式化的 Power，其次 DynamicVar 的 owner。</summary>
    public static StarIconSkin? FindSkinForContext(object? currentValue)
    {
        if (PowerContext != null)
        {
            var skin = StarIconSkins.FindForModel(PowerContext);
            if (skin != null)
                return skin;
        }
        if (currentValue is DynamicVar var && OwnerField?.GetValue(var) is AbstractModel owner)
            return StarIconSkins.FindForModel(owner);
        return null;
    }

    private static Texture2D? Load(string? path) =>
        path == null ? null : ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);

    /// <summary>计数器换皮：主图标、两层旋转装饰层、数字描边、悬停文案（若皮肤提供）。</summary>
    public static void ReskinCounter(NStarCounter counter, StarIconSkin skin)
    {
        if (Load(skin.BigIconPath) is { } big && counter.GetNodeOrNull<TextureRect>("Icon") is { } icon)
            icon.Texture = big;
        if (Load(skin.Layer2Path) is { } layer2 && counter.GetNodeOrNull<TextureRect>("Icon/RotationLayers/Layer1") is { } l1)
            l1.Texture = layer2;
        if (Load(skin.Layer3Path) is { } layer3 && counter.GetNodeOrNull<TextureRect>("Icon/RotationLayers/Layer2") is { } l2)
            l2.Texture = layer3;
        if (skin.LabelOutlineColor is { } outline && counter.GetNodeOrNull<MegaRichTextLabel>("%CountLabel") is { } label)
            label.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, outline);

        // 悬停文案：仅当皮肤提供了 title+description 才覆盖（保留原版「Stars」语义）。
        if (skin.CounterHoverTitle is { } title && skin.CounterHoverDescription is not null)
        {
            var desc = skin.CounterHoverDescription;
            desc.Add("singleStarIcon", skin.TextIconTag);
            CounterHoverTipField?.SetValue(counter, new HoverTip(title, desc));
        }
    }

    /// <summary>把计数器刚生成的获得闪光特效贴图换成皮肤光晕（白色剪影，仍由原粒子材质染色）。</summary>
    public static void ReskinGainVfx(NStarCounter counter, StarIconSkin skin)
    {
        if (Load(skin.GlowPath) is not { } glow)
            return;
        foreach (Node child in counter.GetChildren())
        {
            if (child is not Node2D)
                continue;
            foreach (Node grandchild in child.GetChildren())
            {
                if (grandchild is GpuParticles2D particles && grandchild.Name.ToString().Contains("StarGainVfx"))
                    particles.Texture = glow;
            }
        }
    }

    public static bool CounterBelongsToSkin(NStarCounter counter, out StarIconSkin? skin)
    {
        skin = CounterPlayerField?.GetValue(counter) is Player player
            ? StarIconSkins.FindForCharacter(player.Character)
            : null;
        return skin != null;
    }
}

/// <summary>卡牌左侧星费图标 + 数字描边：按模型归属换皮肤。</summary>
[HarmonyPatch]
internal static class StarIconCardPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), "Reload")]
    private static void Postfix(NCard __instance)
    {
        if (!__instance.IsNodeReady() || __instance.Model == null)
            return;
        if (__instance.GetNodeOrNull<TextureRect>("%StarIcon") is not { } starIcon)
            return;
        // 归属已注册皮肤 → 皮肤大图标；否则显式还原原版（节点复用防残留）。
        string path = StarIconSkins.FindForModel(__instance.Model) is { } skin
            ? skin.BigIconPath
            : StarIconReskin.VanillaBigIconPath;
        if (ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse) is { } texture)
            starIcon.Texture = texture;
    }

    /// <summary>
    /// 星费数字描边：原版默认描边是配蓝星的蓝青色（StsColors.defaultStarCostOutline）。
    /// 与原版 UpdateStarCostColor 同判据重算状态——仅默认态换皮肤描边色，状态色（涨价蓝/降价绿/不足红/刚升级绿）保留。
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), "UpdateStarCostColor")]
    private static void UpdateStarCostColorPostfix(NCard __instance, PileType pileType)
    {
        if (__instance.Model is not { } model || StarIconSkins.FindForModel(model) is not { } skin)
            return;
        if (skin.LabelOutlineColor is not { } outline)
            return;
        if (!model.HasStarCostX && model.WasStarCostJustUpgraded)
            return;
        if (pileType == PileType.Hand && CardCostHelper.GetStarCostColor(model, model.CombatState) != CardCostColor.Unmodified)
            return;
        if (__instance.GetNodeOrNull<MegaLabel>("%StarLabel") is { } label)
            label.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, outline);
    }
}

/// <summary>左下角辉星计数器：Initialize 与 Activate 双钩子按角色换皮；获得闪光特效在星数变动后换贴图。</summary>
[HarmonyPatch]
internal static class StarIconCounterPatch
{
    private static readonly FieldInfo? StarCounterField = AccessTools.Field(typeof(NCombatUi), "_starCounter");

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NStarCounter), "Initialize")]
    private static void InitializePostfix(NStarCounter __instance, Player player)
    {
        if (StarIconSkins.FindForCharacter(player.Character) is { } skin)
            StarIconReskin.ReskinCounter(__instance, skin);
    }

    /// <summary>
    /// 兜底钩子：Activate 末尾（能量计数器创建、星计数器 Reparent 等全部完成之后）再换皮一次。
    /// 实测只挂 Initialize 会被 Activate 后续流程冲掉（图标回退原版），此钩子必须保留，勿删。
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCombatUi), "Activate")]
    private static void ActivatePostfix(NCombatUi __instance)
    {
        if (StarCounterField?.GetValue(__instance) is NStarCounter counter
            && StarIconReskin.CounterBelongsToSkin(counter, out var skin) && skin != null)
            StarIconReskin.ReskinCounter(counter, skin);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NStarCounter), "UpdateStarCount")]
    private static void UpdateStarCountPostfix(NStarCounter __instance)
    {
        if (StarIconReskin.CounterBelongsToSkin(__instance, out var skin) && skin != null)
            StarIconReskin.ReskinGainVfx(__instance, skin);
    }
}

/// <summary>
/// 多人左上角玩家状态条（NMultiplayerPlayerState）：能量图标走 `卡池.EnergyIconPath` 天然支持 mod，
/// 星图标是场景固定贴图（energy_star.png），按面板所属玩家的角色换成皮肤图标。
/// </summary>
[HarmonyPatch]
internal static class StarIconMultiplayerPanelPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NMultiplayerPlayerState), "_Ready")]
    private static void Postfix(NMultiplayerPlayerState __instance)
    {
        if (__instance.Player?.Character is not { } character
            || StarIconSkins.FindForCharacter(character) is not { } skin)
            return;
        if (ResourceLoader.Load<Texture2D>(skin.BigIconPath, null, ResourceLoader.CacheMode.Reuse) is { } big
            && __instance.GetNodeOrNull<Control>("%StarCountContainer")?.GetNodeOrNull<TextureRect>("Image") is { } image)
            image.Texture = big;
    }
}

/// <summary>{Stars:starIcons()} 格式化器：归属已注册皮肤的内容用皮肤内联图标，其余保持原版。</summary>
[HarmonyPatch]
internal static class StarIconFormatterPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(StarIconsFormatter), "TryEvaluateFormat")]
    private static bool Prefix(IFormattingInfo formattingInfo, ref bool __result)
    {
        if (StarIconReskin.FindSkinForContext(formattingInfo.CurrentValue) is not { } skin)
            return true;
        int count = formattingInfo.CurrentValue switch
        {
            DynamicVar var => (int)var.PreviewValue,
            decimal d => (int)d,
            int i => i,
            _ => -1,
        };
        if (count < 0)
            return true;
        formattingInfo.Write(string.Concat(Enumerable.Repeat(skin.TextIconTag, count)));
        __result = true; // 跳过原版方法时必须显式置 true，否则 SmartFormat 视为无 formatter 受理而整段回退原文
        return false;
    }
}

/// <summary>卡牌描述里的星图标替换：格式化成品串内原版单星路径统一换成皮肤路径。</summary>
[HarmonyPatch]
internal static class StarIconTextPatch
{
    /// <summary>卡牌描述：格式化在私有三参重载内完成（public 两参重载与升级预览都走这里），直接对成品串换路径。</summary>
    private static MethodBase TargetMethod() =>
        AccessTools.GetDeclaredMethods(typeof(CardModel))
            .Single(m => m.Name == "GetDescriptionForPile" && m.GetParameters().Length == 3);

    private static void Postfix(CardModel __instance, ref string __result)
    {
        if (__result != null && StarIconSkins.FindForModel(__instance) is { } skin)
            __result = StarIconSkins.ReplaceVanillaIcons(skin, __result);
    }
}

/// <summary>遗物描述（悬停/检视/事件文本共用这两个 getter）：singleStarIcon 变量重指皮肤图标。</summary>
[HarmonyPatch]
internal static class StarIconRelicTextPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(RelicModel), nameof(RelicModel.DynamicDescription), MethodType.Getter)]
    private static void DynamicDescriptionPostfix(RelicModel __instance, ref LocString __result)
    {
        if (StarIconSkins.FindForModel(__instance) is { } skin)
            __result.Add("singleStarIcon", skin.TextIconTag);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RelicModel), nameof(RelicModel.DynamicEventDescription), MethodType.Getter)]
    private static void DynamicEventDescriptionPostfix(RelicModel __instance, ref LocString __result)
    {
        if (StarIconSkins.FindForModel(__instance) is { } skin)
            __result.Add("singleStarIcon", skin.TextIconTag);
    }
}

/// <summary>
/// 能力描述：singleStarIcon 变量重指皮肤图标；
/// 并在悬停提示格式化期间挂上 Power 上下文，让 {Amount:starIcons()} 这类裸值也能走皮肤。
/// </summary>
[HarmonyPatch]
internal static class StarIconPowerTextPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PowerModel), "AddDumbVariablesToDescription")]
    private static void AddDumbVariablesPostfix(PowerModel __instance, LocString description)
    {
        if (StarIconSkins.FindForModel(__instance) is { } skin)
            description.Add("singleStarIcon", skin.TextIconTag);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.HoverTips), MethodType.Getter)]
    private static void HoverTipsPrefix(PowerModel __instance) => StarIconReskin.PowerContext = __instance;

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.HoverTips), MethodType.Getter)]
    private static void HoverTipsFinalizer() => StarIconReskin.PowerContext = null;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.GetDumbHoverTip))]
    private static void GetDumbHoverTipPrefix(PowerModel __instance) => StarIconReskin.PowerContext = __instance;

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.GetDumbHoverTip))]
    private static void GetDumbHoverTipFinalizer() => StarIconReskin.PowerContext = null;
}