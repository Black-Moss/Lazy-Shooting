using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace LazyShooting;

[BepInPlugin(Guid, Name, "1.0.0")]
[BepInDependency(MossLib.Plugin.Guid)]
public class Plugin : BaseUnityPlugin
{
    // ReSharper disable once MemberCanBePrivate.Global
    internal new static ManualLogSource Logger;
    // ReSharper disable once MemberCanBePrivate.Global
    public const string Guid = "blackmoss.lazyshooting";
    // ReSharper disable once MemberCanBePrivate.Global
    public const string Name = "Lazy Shooting";
    private readonly Harmony _harmony = new(Guid);
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public static Plugin Instance { get; private set; } = null!;
    
    // ReSharper disable once MemberCanBePrivate.Global
    public static ConfigEntry<bool> NeverJam;
    // ReSharper disable once MemberCanBePrivate.Global
    public static ConfigEntry<bool> NeverRack;

    public void Awake()
    {
        Logger = base.Logger;
        Instance = this;
        _harmony.PatchAll();
            
        NeverJam = Config.Bind(
            "General",
            "Never Jam",
            false,
            "If true, guns will never jam."
        );
        
        NeverRack = Config.Bind(
            "General",
            "Never Rack",
            false,
            "If true, guns will never rack."
        );
    }
    
    [HarmonyPatch(typeof(GunScript), "JamChance")]
    private static class JamChancePatch
    {
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once RedundantAssignment
        // ReSharper disable once InconsistentNaming
        private static bool Prefix(ref float __result)
        {
            if (!NeverJam.Value) return true;
            __result = 0f;
            return false;
        }
    }
    
    [HarmonyPatch(typeof(GunScript), "Update")]
    private static class GunScriptUpdatePatch
    {
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        private static void Postfix(GunScript __instance)
        {
            if (NeverRack.Value 
                && !__instance.racked
                && (
                    __instance.roundsInMag > 0 
                    || __instance.feedType == GunScript.FeedType.Direct)
                && __instance.roundInChamber == GunScript.RoundInChamber.None
                && __instance.triggerPressed)
            {
                __instance.TryRack();
            }
        }
    }
}