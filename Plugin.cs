using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;

namespace FreeHarpoonDash;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "modcraft.silksong.free-harpoon-dash";
    public const string PluginName = "Free Harpoon Dash";
    public const string PluginVersion = "1.4.0";

    internal static ConfigEntry<bool> DebugUnlockHarpoonDash { get; private set; } = null!;

    private void Awake()
    {
        DebugUnlockHarpoonDash = Config.Bind(
            "Debug",
            "UnlockHarpoonDashBeforeStory",
            false,
            "Temporarily unlock Harpoon Dash before its story unlock. This never changes the saved PlayerData unlock flag, so disabling it restores the story-controlled availability.");

        new Harmony(PluginGuid).PatchAll(Assembly.GetExecutingAssembly());
        Logger.LogInfo(
            $"{PluginName} {PluginVersion} loaded: free silk cost and eight-direction aiming are enabled; " +
            $"debug unlock={DebugUnlockHarpoonDash.Value}.");
    }

}

[HarmonyPatch(typeof(HeroController), nameof(HeroController.HasHarpoonDash))]
internal static class DebugHarpoonDashUnlockPatch
{
    private static void Postfix(ref bool __result)
    {
        __result = DebugUnlockPolicy.Resolve(
            __result,
            Plugin.DebugUnlockHarpoonDash.Value);
    }
}

[HarmonyPatch(typeof(IntCompare), nameof(IntCompare.OnEnter))]
internal static class HarpoonDashSilkGatePatch
{
    private static bool Prefix(IntCompare __instance)
    {
        if (!IsHarpoonDashState(__instance, "Can Do?"))
        {
            return true;
        }

        __instance.Fsm.Event(__instance.greaterThan);
        __instance.Finish();
        return false;
    }

    private static bool IsHarpoonDashState(IntCompare action, string stateName)
    {
        return string.Equals(action.Fsm?.Name, "Harpoon Dash", StringComparison.Ordinal) &&
               string.Equals(action.State?.Name, stateName, StringComparison.Ordinal);
    }
}

[HarmonyPatch(typeof(TakeSilk), nameof(TakeSilk.OnEnter))]
internal static class HarpoonDashSilkCostPatch
{
    private static bool Prefix(TakeSilk __instance)
    {
        if (!string.Equals(__instance.Fsm?.Name, "Harpoon Dash", StringComparison.Ordinal) ||
            !string.Equals(__instance.State?.Name, "Take Control", StringComparison.Ordinal))
        {
            return true;
        }

        __instance.Finish();
        return false;
    }
}
