using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

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
    
    [HarmonyPatch(typeof(GunScript), "Update")]
    private static class GunScriptUpdatePatch
    {
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        private static void Prefix(GunScript __instance)
        {
             // 启用永远上膛且不是泵动式时 重置上膛状态
            if (NeverRack.Value
                && __instance.firingMode == GunScript.FiringMode.Pump
                )
            {
                __instance.racked = false;
            }
        }
    }
    
    [HarmonyPatch(typeof(GunScript), "JamChance")]
    private static class GunScriptJamChancePatch
    {
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        private static void Postfix(ref float __result)
        {
            // 如果启用了永不卡壳，则将卡壳几率设置为0
            
            if (!NeverJam.Value) return;
            
            __result = 0;
        }
    }
}