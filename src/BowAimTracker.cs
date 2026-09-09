using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace PatchMod;

public static class BowAimTracker
{
    public static bool IsPlayerAiming(ICoreClientAPI? api)
    {
        if (api?.World?.Player?.Entity == null) return false;

        if (CombatOverhaulHelper.IsCombatOverhaulAiming(api))
        {
            return true;
        }

        EntityAgent entity = api.World.Player.Entity;
        if (entity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
        {
            Item? heldItem = entity.RightHandItemSlot?.Itemstack?.Item;
            if (heldItem is ItemBow)
            {
                return entity.Attributes.GetInt("aiming", 0) == 1;
            }
        }

        return false;
    }
}