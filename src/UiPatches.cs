using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace PatchMod;

[HarmonyPatch(typeof(EntityShapeRenderer), "RenderHeldItem")]
public class Patch_HideOffhandItem
{
    public enum RangedWeaponType
    {
        None,
        VanillaRanged,
        CombatOverhaulRanged
    }

    // Fast O(1) Cache: Item.Id -> RangedWeaponType
    private static readonly Dictionary<int, RangedWeaponType> WeaponCache = new();

    // Fast O(1) Cache: Item.Id -> IsAmmo (bool)
    private static readonly Dictionary<int, bool> AmmoCache = new();

    [HarmonyPrefix]
    public static bool Prefix(EntityShapeRenderer __instance, bool right)
    {
        if (right) return true;

        if (__instance.entity is not EntityAgent agent) return true;

        Item? mainHandItem = agent.RightHandItemSlot?.Itemstack?.Item;
        if (mainHandItem == null) return true;

        if (!WeaponCache.TryGetValue(mainHandItem.Id, out RangedWeaponType weaponType))
        {
            weaponType = ClassifyWeapon(mainHandItem);
            WeaponCache[mainHandItem.Id] = weaponType;
        }

        switch (weaponType)
        {
            case RangedWeaponType.CombatOverhaulRanged:
            {
                Item? offhandItem = agent.LeftHandItemSlot?.Itemstack?.Item;

                if (offhandItem != null && IsCombatOverhaulAmmoCached(offhandItem, mainHandItem))
                {
                    return true;
                }

                return false;
            }

            case RangedWeaponType.VanillaRanged:
            {
                if (agent.Controls.HandUse == EnumHandInteract.HeldItemInteract)
                {
                    return false;
                }
                break;
            }

            case RangedWeaponType.None:
            default:
                break;
        }

        return true;
    }

    #region Weapon & Ammo Classification

    private static RangedWeaponType ClassifyWeapon(Item item)
    {
        if (item.IsCombatOverhaulRangedWeapon())
        {
            return RangedWeaponType.CombatOverhaulRanged;
        }

        if (item is ItemBow)
        {
            return RangedWeaponType.VanillaRanged;
        }

        return RangedWeaponType.None;
    }

    private static bool IsCombatOverhaulAmmoCached(Item offhandItem, Item mainHandItem)
    {
        if (offhandItem == null) return false;

        if (!AmmoCache.TryGetValue(offhandItem.Id, out bool isAmmo))
        {
            isAmmo = AmmoHelper.IsCombatOverhaulAmmo(offhandItem, mainHandItem);
            AmmoCache[offhandItem.Id] = isAmmo;
        }

        return isAmmo;
    }

    public static void ClearCaches()
    {
        WeaponCache.Clear();
        AmmoCache.Clear();
    }

    #endregion
}

[HarmonyPatch(typeof(HudDialogChat), nameof(HudDialogChat.OnRenderGUI))]
public static class ChatRenderPatch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        return !BowAimTracker.IsPlayerAiming(PatchModSystem.ClientAPI);
    }
}

[HarmonyPatch(typeof(HudElementCoordinates), nameof(HudElementCoordinates.OnRenderGUI))]
public static class CoordinatesRenderPatch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        return !BowAimTracker.IsPlayerAiming(PatchModSystem.ClientAPI);
    }
}

[HarmonyPatch(typeof(HudHotbar), nameof(HudHotbar.OnRenderGUI))]
public static class HotbarRenderPatch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        return !BowAimTracker.IsPlayerAiming(PatchModSystem.ClientAPI);
    }
}

[HarmonyPatch(typeof(HudStatbar), nameof(HudStatbar.OnRenderGUI))]
public static class StatbarRenderPatch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        return !BowAimTracker.IsPlayerAiming(PatchModSystem.ClientAPI);
    }
}

[HarmonyPatch(typeof(GuiDialogWorldMap), nameof(GuiDialogWorldMap.OnRenderGUI))]
public static class GuiDialogWorldMapRenderPatch
{
    [HarmonyPrefix]
    public static bool Prefix(GuiDialogWorldMap __instance)
    {
        return !(BowAimTracker.IsPlayerAiming(PatchModSystem.ClientAPI) && __instance.DialogType == EnumDialogType.HUD);
    }
}