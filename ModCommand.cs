using System;
using System.Collections.Generic;
using System.Reflection;
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
            string localePreType = $"{LocalePre}type.";
            void Action(string[] args)
            {
                Tools.CheckArgumentCount(args, 1);
                Tools.CheckForWorld();
                switch (args[1])
                {
                    case "autosrack":
                        ModConfigs.AutoRack = !ModConfigs.AutoRack;
                        Output(ModLocale.GetFormat($"{localePreType}autosrack"), ModConfigs.AutoRack, __instance);
                        break;
                    case "ammunitionui":
                        ModConfigs.AmmunitionUi = !ModConfigs.AmmunitionUi;
                        Output(ModLocale.GetFormat($"{localePreType}ammunitionui"), ModConfigs.AutoRack, __instance);
                        break;
                    case "indestructiblegun":
                        ModConfigs.IndestructibleGun = !ModConfigs.IndestructibleGun;
                        Output(ModLocale.GetFormat($"{localePreType}indestructiblegun"), ModConfigs.AutoRack, __instance);
                        break;
                    case "recoiless":
                        ModConfigs.Recoiless = !ModConfigs.Recoiless;
                        Output(ModLocale.GetFormat($"{localePreType}recoiless"), ModConfigs.AutoRack, __instance);
                        break;
                    case "infiniteammunition":
                        ModConfigs.InfiniteAmmunition = !ModConfigs.InfiniteAmmunition;
                        Output(ModLocale.GetFormat($"{localePreType}infiniteammunition"), ModConfigs.AutoRack, __instance);
                        break;
                    case "neverjam":
                        ModConfigs.NeverJam = !ModConfigs.NeverJam;
                        Output(ModLocale.GetFormat($"{localePreType}neverjam"), ModConfigs.AutoRack, __instance);
                        break;
                    default:
                        throw new Exception(ModLocale.GetFormat($"{localePreType}exception"));
                }
            }
            Dictionary<int, List<string>> argAutofill2 = new Dictionary<int, List<string>>
            { { 0, [
                "autosrack",
                "ammunitionui",
                "indestructiblegun",
                "recoiless",
                "infiniteammunition",
                "neverjam"
            ] } };
            (string, string)[] valueTupleArray =
            [
                ("type", ModLocale.GetFormat($"{localePreType}name"))
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

    private static void Output(string type, bool state, ConsoleScript instance)
    {
        Tools.LogToConsoleAndLog(ModLocale.GetFormat($"{LocalePre}output", type, state), instance);
    }
}
