using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace GekitoLib.Stances;

/// <summary>
/// 生物身体贴图染色工具：修改战斗形象 "Visuals" 子节点（Sprite2D）的 Modulate。
/// 要求战斗形象由 BaseLib NCreatureVisualsFactory 从单张 Texture2D 构建（其子节点 "Visuals" 即身体贴图）。
/// </summary>
public static class StanceTint
{
    public static void Apply(Creature creature, Color color)
    {
        var sprite = GetBodySprite(creature);
        if (sprite != null)
            sprite.Modulate = color;
    }

    public static void Clear(Creature creature)
    {
        var sprite = GetBodySprite(creature);
        if (sprite != null)
            sprite.Modulate = Colors.White;
    }

    private static Sprite2D? GetBodySprite(Creature creature)
    {
        if (TestMode.IsOn) return null;
        NCreatureVisuals? visuals = NCombatRoom.Instance?.GetCreatureNode(creature)?.Visuals;
        return visuals?.GetNodeOrNull<Sprite2D>("Visuals");
    }
}
