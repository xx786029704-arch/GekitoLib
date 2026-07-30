using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace GekitoLib.Stances;

/// <summary>
/// 姿态注册表。各 mod 在自己的 ModInitializer 中注册本 mod 的姿态能力，
/// 注册后即可通过 <see cref="StanceCmd"/> 进出姿态。所有已注册姿态互相排斥。
/// </summary>
public static class StanceRegistry
{
    private static readonly List<StanceDefinition> Stances = [];

    /// <summary>
    /// 注册一个姿态。重复注册同一类型会被忽略。
    /// </summary>
    /// <typeparam name="TPower">姿态能力类型（进入姿态时以 1 层施加，退出时移除）。</typeparam>
    /// <param name="tint">进入姿态时的角色身体染色；不传表示不染色。</param>
    /// <param name="onEnterFeedback">进入姿态时的反馈回调（音效/特效等）。</param>
    public static void Register<TPower>(Color? tint = null, Action? onEnterFeedback = null) where TPower : PowerModel
    {
        var powerType = typeof(TPower);
        foreach (var stance in Stances)
        {
            if (stance.PowerType == powerType) return;
        }
        Stances.Add(new StanceDefinition(powerType, tint, onEnterFeedback));
    }

    internal static IReadOnlyList<StanceDefinition> All => Stances;
}
