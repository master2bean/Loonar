using System;
using TownOfHost.Extensions;
using TownOfHost.Interface;
using TownOfHost.Interface.Menus.CustomNameMenu;
using TownOfHost.ReduxOptions;
using UnityEngine;


namespace TownOfHost.Roles;

public class Miner: Impostor
{
    [DynElement(UI.Cooldown)]
    private Cooldown minerAbilityCooldown;

    [RoleAction(RoleActionType.AttemptKill)]
    public override bool TryKill(PlayerControl target)
    {
        target.GetRawName().DebugLog("Trying to kill: ");
        return base.TryKill(target);
    }

    [RoleAction(RoleActionType.OnPet)]
    public void MinerVentAction()
    {
        if (minerAbilityCooldown.NotReady()) return;
        minerAbilityCooldown.Start();

        if (!Main.LastEnteredVentLocation.TryGetValue(MyPlayer.PlayerId, out Vector2 position)) return;
        Logger.Info($"{MyPlayer.Data.PlayerName}:{position}", "MinerTeleport");
        Utils.Teleport(MyPlayer.NetTransform, new Vector2(position.x, position.y + 0.3636f));
    }

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream).AddSubOption(sub =>
            sub.Name("Miner Ability Cooldown")
                .Bind(v => minerAbilityCooldown.Duration = Convert.ToSingle(v))
                .AddValues(8, 10f, 12.5, 15f, 17.5, 20f, 22.5, 25f, 27.5, 30f, 32.5, 35f, 37.5, 40f, 42.5, 45f, 47.5, 50f, 52.5, 55f, 57.5, 60f)
                .Build());
}