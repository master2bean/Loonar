using System.Collections.Generic;
using AmongUs.GameOptions;
using Il2CppSystem;
using TOHTOR.API;
using TOHTOR.API.Odyssey;
using TOHTOR.Extensions;
using TOHTOR.Factions;
using TOHTOR.GUI;
using TOHTOR.GUI.Name;
using TOHTOR.GUI.Name.Impl;
using TOHTOR.Managers.History.Events;
using TOHTOR.Roles.Interactions;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.RoleGroups.Vanilla;
using TOHTOR.Utilities;
using UnityEngine;
using VentLib.Logging;
using VentLib.Options.Game;

namespace TOHTOR.Roles.RoleGroups.Crew;

public class Sheriff : Crewmate
{
    private int totalShots;
    private bool oneShotPerRound;
    private bool canKillCrewmates;
    private bool isSheriffDesync;

    private bool shotThisRound;
    private int shotsRemaining;

    [UIComponent(UI.Cooldown)]
    private Cooldown shootCooldown;

    protected override void Setup(PlayerControl player)
    {
        if (!isSheriffDesync) base.Setup(player);
        shotsRemaining = totalShots;
    }

    private bool HasShots() => !(oneShotPerRound && shotThisRound) && shotsRemaining >= 0;

    [UIComponent(UI.Counter, ViewMode.Additive, GameState.Roaming, GameState.InMeeting)]
    public string RemainingShotCounter() => RoleUtils.Counter(shotsRemaining, totalShots);

    [RoleAction(RoleActionType.RoundStart)]
    public bool RefreshShotThisRound() => shotThisRound = false;

    [RoleAction(RoleActionType.OnPet)]
    public bool TryKillWithPet(ActionHandle handle)
    {
        VentLogger.Trace("Sheriff Shoot Ability (Pet)", "SheriffAbility");
        handle.Cancel();
        if (isSheriffDesync || !shootCooldown.IsReady() || !HasShots()) return false;
        List<PlayerControl> closestPlayers = MyPlayer.GetPlayersInAbilityRangeSorted();
        if (closestPlayers.Count == 0) return false;
        PlayerControl target = closestPlayers[0];
        return TryKill(target, handle);
    }

    [RoleAction(RoleActionType.Attack)]
    public bool TryKill(PlayerControl target, ActionHandle handle)
    {
        handle.Cancel();
        if (!shootCooldown.IsReady() || !HasShots()) return false;
        shotsRemaining--;
        shootCooldown.Start();

        if (target.Relationship(MyPlayer) is Relation.FullAllies) Suicide(target);
        return MyPlayer.InteractWith(target, DirectInteraction.FatalInteraction.Create(this)) is InteractionResult.Proceed;
    }

    private void Suicide(PlayerControl target)
    {
        MyPlayer.RpcMurderPlayer(MyPlayer);

        if (!canKillCrewmates) return;
        bool killed = MyPlayer.InteractWith(target, DirectInteraction.FatalInteraction.Create(this)) is InteractionResult.Proceed;
        Game.GameHistory.AddEvent(new KillEvent(MyPlayer, target, killed));
    }
    // OPTIONS

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Color(RoleColor)
            .SubOption(sub => sub
                .Name("Can Kill Crewmates")
                .Bind(v => canKillCrewmates = (bool)v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .Name("Kill Cooldown")
                .BindFloat(this.shootCooldown.SetDuration)
                .AddFloatRange(0, 120, 2.5f, 12, "s")
                .Build())
            .SubOption(sub => sub
                .Name("Total Shots")
                .Bind(v => this.totalShots = (int)v)
                .AddIntRange(1, 60, 1, 4)
                .Build())
            .SubOption(sub => sub
                .Name("One Shot Per Round")
                .Bind(v => this.oneShotPerRound = (bool)v)
                .AddOnOffValues()
                .Build())
            .SubOption(sub => sub
                .Name("Sheriff Action Button")
                .Bind(v => isSheriffDesync = (bool)v)
                .Value(v => v.Text("Kill Button (legacy)").Value(true).Color(Color.green).Build())
                .Value(v => v.Text("Pet Button").Value(false).Color(Color.cyan).Build())
                .Build());

    // Sheriff is not longer a desync role for simplicity sake && so that they can do tasks
    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .VanillaRole(RoleTypes.Crewmate)
            .DesyncRole(!isSheriffDesync ? null : RoleTypes.Impostor)
            .CanVent(false)
            .RoleColor(new Color(0.97f, 0.8f, 0.27f));
}