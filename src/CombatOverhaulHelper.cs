using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace PatchMod;

public static class CombatOverhaulHelper
{
    public const string ModId = "combatoverhaulfork";

    public static bool IsCombatOverhaulLoaded { get; set; }

    private static object? _clientAimingSystem;
    private static PropertyInfo? _aimingProperty;
    private static bool _initialized;

    public static void Initialize(ICoreClientAPI api)
    {
        if (_initialized || !IsCombatOverhaulLoaded) return;
        _initialized = true;

        foreach (var system in api.ModLoader.Systems)
        {
            if (system.GetType().Name == "CombatOverhaulSystem")
            {
                var aimingSysProp = system.GetType().GetProperty("AimingSystem");
                _clientAimingSystem = aimingSysProp?.GetValue(system);

                if (_clientAimingSystem != null)
                {
                    _aimingProperty = _clientAimingSystem.GetType().GetProperty("Aiming");
                }
                break;
            }
        }
    }

    /// <summary>
    /// Returns true if Combat Overhaul's ClientAimingSystem reports the player is currently aiming.
    /// </summary>
    public static bool IsCombatOverhaulAiming(ICoreClientAPI api)
    {
        if (!IsCombatOverhaulLoaded) return false;

        if (!_initialized)
        {
            Initialize(api);
        }

        if (_clientAimingSystem == null || _aimingProperty == null) return false;

        return _aimingProperty.GetValue(_clientAimingSystem) is bool isAiming && isAiming;
    }

    public static bool IsCombatOverhaulRangedWeapon(this Item item)
    {
        if (!IsCombatOverhaulLoaded || item == null) return false;

        // Save item code for logging
        string itemCode = item.Code?.ToString() ?? "unknown_item";

        try
        {
            if (item.Attributes == null || !item.Attributes.Exists)
            {
                return false;
            }

            JsonObject attrs = item.Attributes;

            JsonObject aimingByType = attrs["AimingByType"];
            JsonObject aiming = attrs["Aiming"];

            if ((aimingByType != null && aimingByType.Exists) || 
                (aiming != null && aiming.Exists))
            {
                // logger?.Notification($"[COCheck] SUCCESS: '{itemCode}' has Combat Overhaul Aiming attributes.");
                return true;
            }
        }
        catch (Exception)
        // catch (Exception ex)
        {
            // logger?.Error($"[COCheck] Error while checking item '{itemCode}': {ex.Message}");
        }

        return false;
    }

    public static void Reset()
    {
        _clientAimingSystem = null;
        _aimingProperty = null;
        _initialized = false;
        IsCombatOverhaulLoaded = false;
    }
}


public static class AmmoHelper
{
    /// <summary>
    /// Universal check for ANY Combat Overhaul ammo (arrows, bolts, bullets, cartridges, etc.)
    /// </summary>
    public static bool IsCombatOverhaulAmmo(Item? offhandItem, Item? mainHandItem)
    {
        if (offhandItem == null) return false;

        // UNIVERSAL BEHAVIOR CHECK
        if (offhandItem.CollectibleBehaviors != null)
        {
            foreach (var behavior in offhandItem.CollectibleBehaviors)
            {
                string typeName = behavior.GetType().Name;
                string fullName = behavior.GetType().FullName ?? "";

                if (typeName.Contains("Projectile", StringComparison.OrdinalIgnoreCase) || 
                    (fullName.Contains("CombatOverhaul", StringComparison.OrdinalIgnoreCase) && typeName.Contains("Projectile")))
                {
                    return true;
                }
            }
        }

        return false;
    }
}