using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;

namespace GekitoLib.Powers;

/// <summary>
/// 多层护甲（StS1 的 Plated Armor）：回合结束时获得「层数」点格挡；
/// 受到未被格挡的攻击伤害后层数 -1（不会像 StS2 镀层那样每回合衰减）。
/// </summary>
public sealed class LayeredArmorPower : CustomPowerModel
{
    private const string Sts1SfxPath = "res://GekitoLib/audio/powers/layered_armor.ogg";

    public override string CustomPackedIconPath => "res://GekitoLib/images/powers/layeredarmor.png";
    public override string CustomBigIconPath => "res://GekitoLib/images/powers/big/layeredarmor.png";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        PlaySts1Sfx();
        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnEndEarly(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Amount <= 0)
            return;

        FlashWithSts1Presentation();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || dealer is null || dealer == Owner || result.UnblockedDamage <= 0 ||
            !props.HasFlag(ValueProp.Move))
            return;

        FlashWithSts1Presentation();
        await PowerCmd.Decrement(this);
    }

    private void FlashWithSts1Presentation()
    {
        // PowerModel.Flash drives both STS2 equivalents of the STS1 flash:
        // the large icon over the owner and the particle burst on the power icon.
        Flash();
        PlaySts1Sfx();
    }

    private static void PlaySts1Sfx()
    {
        NCombatRoom? room = NCombatRoom.Instance;
        if (room is null)
            return;

        float linearVolume = Mathf.Clamp(
            SaveManager.Instance.SettingsSave.VolumeMaster * SaveManager.Instance.SettingsSave.VolumeSfx,
            0f,
            1f);
        if (linearVolume <= 0f)
            return;

        AudioStream? stream = ResourceLoader.Load<AudioStream>(Sts1SfxPath);
        if (stream is null)
            return;

        AudioStreamPlayer player = new()
        {
            Stream = stream,
            VolumeDb = Mathf.LinearToDb(linearVolume),
            // STS1's play(key, 0.05f) randomizes pitch between 0.95 and 1.05.
            PitchScale = (float)GD.RandRange(0.95, 1.05)
        };
        player.Finished += player.QueueFree;
        room.AddChild(player);
        player.Play();
    }
}
