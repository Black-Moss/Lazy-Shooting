using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace BlackMossTemplate;

[BepInPlugin(Guid, Name, "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    internal new static ManualLogSource Logger;
    internal const string Guid = "blackmoss.template";
    internal const string Name = "Black_Moss Template";
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