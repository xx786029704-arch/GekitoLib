using System;
using Godot;

namespace GekitoLib.Stances;

/// <summary>
/// 一个已注册姿态的定义。姿态以「姿态能力」（PowerModel 子类）为载体，
/// 所有已注册姿态互斥：同一时刻至多处于一种姿态。
/// </summary>
/// <param name="PowerType">姿态能力的类型。</param>
/// <param name="Tint">进入姿态时给角色身体贴图染的颜色；null 表示不染色。</param>
/// <param name="OnEnterFeedback">进入姿态时的即时反馈回调（音效/特效等），在姿态能力结算后触发。</param>
internal sealed record StanceDefinition(Type PowerType, Color? Tint, Action? OnEnterFeedback);
