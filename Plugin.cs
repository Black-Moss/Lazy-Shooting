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
    private ConfigEntry<int> _testNumber;

    public void Awake()
    {
        Logger = base.Logger;
        Instance = this;
        _harmony.PatchAll();
            
        _testNumber = Config.Bind(
            "General",                // 配置节名称
            "TEST Number",         // 配置项名称
            60,                   // 默认值
            "Default 1 minute."    // 描述信息
        );
        Logger.LogInfo($"Here's Black Moss! {_testNumber.Value}");
    }
}