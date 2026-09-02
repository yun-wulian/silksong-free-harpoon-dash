using System;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.Mono;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;

namespace FreeHarpoonDash;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "modcraft.silksong.free-harpoon-dash";
    public const string PluginName = "Free Harpoon Dash";
    public const string PluginVersion = "1.0.0";

    private void Awake()
    {
        new Harmony(PluginGuid).PatchAll(Assembly.GetExecutingAssembly());
        Logger.LogInfo($"{PluginName} {PluginVersion} loaded: Harpoon Dash works at zero silk and consumes no silk.");
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
