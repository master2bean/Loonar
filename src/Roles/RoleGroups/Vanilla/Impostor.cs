using AmongUs.GameOptions;
using TOHTOR.API;
using TOHTOR.API.Odyssey;
using TOHTOR.Factions;
using TOHTOR.Managers.History.Events;
using TOHTOR.Roles.Interactions;
using TOHTOR.Roles.Interfaces;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;

namespace TOHTOR.Roles.RoleGroups.Vanilla;

public partial class Impostor : CustomRole, IModdable, ISabotagerRole
{
    public virtual bool CanSabotage() => canSabotage;
    public virtual bool CanKill() => canKill;
    protected bool canSabotage = true;
    protected bool canKill = true;
    public float KillCooldown
    {
        set => _killCooldown = value;
        get => _killCooldown ?? AUSettings.KillCooldown();
    }
    private float? _killCooldown;

    [RoleAction(RoleActionType.Attack, Subclassing = false)]
    public virtual bool TryKill(PlayerControl target)
    {
        InteractionResult result = MyPlayer.InteractWith(target, DirectInteraction.FatalInteraction.Create(this));
        Game.GameHistory.AddEvent(new KillEvent(MyPlayer, target, result is InteractionResult.Proceed));
        return result is InteractionResult.Proceed;
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .VanillaRole(RoleTypes.Impostor)
            .Faction(FactionInstances.Impostors)
            .CanVent(true)
            .OptionOverride(Override.KillCooldown, KillCooldown)
            .RoleColor(Color.red);

}