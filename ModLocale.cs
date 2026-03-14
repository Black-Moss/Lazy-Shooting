using System.Reflection;
using BepInEx.Logging;
using MossLib.Base;

namespace LazyShooting;

public class ModLocale : ModLocaleBase
{
    private static ModLocale _instance;

    private static ModLocale Instance { get; set; } = new();

    public static void Initialize(ManualLogSource logger)
    {
        if (_instance != null)
            return;
        _instance = new ModLocale();
        Instance = _instance;
        _instance.Initialize(logger, Plugin.Guid, Plugin.Name, Assembly.GetExecutingAssembly());
    }
    
    public static string GetFormat(string key, params object[] args)
    {
        return Instance.GetStringFormatted(key, args);
    }
}
