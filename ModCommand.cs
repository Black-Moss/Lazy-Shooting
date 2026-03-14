using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MossLib;
using MossLib.Base;
using UnityEngine;

namespace LazyShooting;

public class ModCommand : ModCommandBase
{
    private static ModCommand _instance;
    private static ModCommand Instance { get; set; } = new();
    private const string LocalePre = "command.lazyshooting.";
    private const string LocalePreType = LocalePre + "type.";

    public static void Initialize(ManualLogSource logger)
    {
        if (_instance != null)
            return;
        _instance = new ModCommand();
        Instance = _instance;
        _instance.Initialize(logger, Plugin.Guid, Plugin.Name, Assembly.GetExecutingAssembly());
    }

    [HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
    public class ConsoleScriptRegisterAllCommandsPatcher
    {
        [HarmonyPostfix]
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once InconsistentNaming
        public static void RegisterCustomCommands(ConsoleScript __instance)
        {
            void Action(string[] args)
            {
                Tools.CheckArgumentCount(args, 1);
                switch (args[1])
                {
                    case "ammunitionui":
                        SwitchType(Plugin.AmmunitionUi, "ammunitionui", __instance);
                        break;
                    case "autosrack":
                        SwitchType(Plugin.AutoRack, "autosrack",  __instance);
                        break;
                    case "indestructiblegun":
                        SwitchType(Plugin.IndestructibleGun, "indestructiblegun",  __instance);
                        break;
                    case "infiniteammunition":
                        SwitchType(Plugin.InfiniteAmmunition, "infiniteammunition",  __instance);
                        break;
                    case "neverjam":
                        SwitchType(Plugin.NeverJam, "neverjam",  __instance);
                        break;
                    case "recoiless":
                        SwitchType(Plugin.Recoiless, "recoiless",  __instance);
                        break;
                    default:
                        throw new Exception(ModLocale.GetFormat($"{LocalePreType}exception"));
                }
            }
            Dictionary<int, List<string>> argAutofill2 = new Dictionary<int, List<string>>
            { { 0, [
                "ammunitionui",
                "autosrack",
                "indestructiblegun",
                "infiniteammunition",
                "neverjam",
                "recoiless"
            ] } };
            (string, string)[] valueTupleArray =
            [
                ("type", ModLocale.GetFormat($"{LocalePreType}name"))
            ];
            Command lazyshooting = new Command("lazyshooting", ModLocale.GetFormat($"{LocalePre}description"), Action, argAutofill2, valueTupleArray);
            ConsoleScript.Commands.Add(lazyshooting);
        }
    }

    [HarmonyPatch(typeof(ConsoleScript), "Awake")]
    public new class ConsoleScriptAwakePatcher
    {
        [HarmonyPostfix]
        // ReSharper disable once UnusedMember.Global
        public static void AddCustomLogCallback()
        {
            Application.logMessageReceived += Instance.ApplicationLogCallback;
        }
    }
    
    private static void SwitchType(ConfigEntry<bool> configEntry, string configName, ConsoleScript consoleScript)
    {
        Tools.SwitchType(Plugin.Guid, configEntry, ModLocale.GetFormat($"{LocalePreType}{configName}", configName), Plugin.Logger, consoleScript);
        ModConfigs.Update();
    }
}
