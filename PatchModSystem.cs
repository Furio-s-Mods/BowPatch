using Vintagestory.API.Client;
using Vintagestory.API.Common;
using HarmonyLib;
using System.Reflection;

namespace PatchMod;

public class PatchModSystem : ModSystem
{
    private Harmony? harmony;
    private const string ModName = "bowpatch";
    private const string HarmonyId = $"com.furio.{ModName}";
    private int disposed;
    public static ICoreClientAPI? ClientAPI { get; private set; }

    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;

    public override void StartClientSide(ICoreClientAPI api)
    {
        ClientAPI = api;
        try
        {
            CombatOverhaulHelper.IsCombatOverhaulLoaded = api.ModLoader.IsModEnabled(CombatOverhaulHelper.ModId);

            harmony = new Harmony(HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            PatchAllGuiDialogs(harmony);
            PatchAllRenderers(harmony);
        }
        catch (Exception ex)
        {
            api.Logger.Error($"[{ModName}] Failed to apply Harmony patches! (e: {ex})");
        }
    }

    
    private void PatchAllGuiDialogs(Harmony harmony)
    {
        var prefixMethod = new HarmonyMethod(typeof(PatchModSystem).GetMethod(nameof(UniversalHudPrefix), BindingFlags.Static | BindingFlags.NonPublic));

        var baseRenderMethod = typeof(GuiDialog).GetMethod("OnRenderGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (baseRenderMethod != null)
        {
            try { harmony.Patch(baseRenderMethod, prefix: prefixMethod); }
            catch { }
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (IsIgnoredAssembly(assembly)) continue;

            Type[] types;
            try { types = assembly.GetTypes(); }
            catch { continue; }

            foreach (var type in types)
            {
                if (typeof(GuiDialog).IsAssignableFrom(type) && type != typeof(GuiDialog))
                {
                    var renderMethod = type.GetMethod("OnRenderGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (renderMethod != null && !renderMethod.IsAbstract)
                    {
                        try { harmony.Patch(renderMethod, prefix: prefixMethod); }
                        catch { }
                    }
                }
            }
        }
    }

    private void PatchAllRenderers(Harmony harmony)
    {
        var prefixMethod = new HarmonyMethod(typeof(PatchModSystem).GetMethod(nameof(UniversalRendererPrefix), BindingFlags.Static | BindingFlags.NonPublic));

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (IsIgnoredAssembly(assembly)) continue;

            Type[] types;
            try { types = assembly.GetTypes(); }
            catch { continue; }

            foreach (var type in types)
            {
                if (typeof(IRenderer).IsAssignableFrom(type) && type != typeof(IRenderer))
                {
                    var renderMethod = type.GetMethod("OnRenderFrame", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                    if (renderMethod != null && !renderMethod.IsAbstract)
                    {
                        try { harmony.Patch(renderMethod, prefix: prefixMethod); }
                        catch { }
                    }
                }
            }
        }
    }

    private static bool IsIgnoredAssembly(Assembly assembly)
    {
        // Skip current execution assembly
        if (assembly == Assembly.GetExecutingAssembly()) return true;

        string asmName = assembly.FullName ?? string.Empty;

        // Skip system and base engine assemblies
        if (asmName.StartsWith("System")
            || asmName.StartsWith("Microsoft")
            || asmName.StartsWith("mscorlib")
            // || asmName.StartsWith("Vintagestory")
            // || asmName.StartsWith("VSEssentials")
           )
        {
            return true;
        }

        // Returns AssemblyName[] array and checks if any entry matches VintagestoryAPI
        bool referencesVsApi = Array.Exists(
            assembly.GetReferencedAssemblies(), 
            refAsm => refAsm.Name != null && refAsm.Name.StartsWith("VintagestoryAPI", StringComparison.OrdinalIgnoreCase)
        );

        // If it does not reference Vintage Story API, ignore it
        return !referencesVsApi;
    }

    private static bool UniversalHudPrefix(GuiDialog __instance)
    {
        try
        {
            if (__instance.DialogType == EnumDialogType.HUD)
            {
                return !BowAimTracker.IsPlayerAiming(ClientAPI);
            }
        }
        catch { /* Fail-safe to avoid breaking game rendering */ }
        return true;
    }

    private static bool UniversalRendererPrefix(EnumRenderStage stage)
    {
        try
        {
            if (stage == EnumRenderStage.Ortho || stage == EnumRenderStage.AfterFinalComposition)
            {
                return !BowAimTracker.IsPlayerAiming(ClientAPI);
            }
        }
        catch { /* Fail-safe to avoid breaking game rendering */ }
        return true;
    }

    public override void Dispose() 
    {
        if (Interlocked.Exchange(ref disposed, 1) == 1) return;

        Patch_HideOffhandItem.ClearCaches();
        CombatOverhaulHelper.Reset();
        
        harmony?.UnpatchAll(HarmonyId);
        harmony = null;
        
        ClientAPI = null;
        
        base.Dispose();
    }
}