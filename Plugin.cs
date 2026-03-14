using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    private static bool _hasOne;
    
    // ReSharper disable once MemberCanBePrivate.Global
    public static ConfigEntry<bool> AmmunitionUi;
    // ReSharper disable once MemberCanBePrivate.Global
    public static ConfigEntry<bool> AutoRack;
    // ReSharper disable once MemberCanBePrivate.Global
    public static ConfigEntry<bool> IndestructibleGun;
    // ReSharper disable once MemberCanBePrivate.Global
    public static ConfigEntry<bool> InfiniteAmmunition;
    // ReSharper disable once MemberCanBePrivate.Global
    public static ConfigEntry<bool> NeverJam;
    // ReSharper disable once MemberCanBePrivate.Global
    public static ConfigEntry<bool> Recoiless;

    public void Awake()
    {
        Logger = base.Logger;
        Instance = this;
        _harmony.PatchAll();
        ModLocale.Initialize(Logger);
        ModCommand.Initialize(Logger);
        
        AmmunitionUi = Config.Bind(
            "General",
            "Ammunition UI",
            true,
            ConfigLocale("ammunitionui")
        );
        AutoRack = Config.Bind(
            "General",
            "Auto Rack",
            false,
            ConfigLocale("autosrack")
        );
        IndestructibleGun = Config.Bind(
            "General",
            "Indestructible Gun",
            false,
            ConfigLocale("indestructiblegun")
        );
        InfiniteAmmunition = Config.Bind(
            "General",
            "Infinite Ammunition",
            false,
            ConfigLocale("infiniteammunition")
        );
        NeverJam = Config.Bind(
            "General",
            "Never Jam",
            false,
            ConfigLocale("neverjam")
        );
        Recoiless = Config.Bind(
            "General",
            "Recoiless",
            false,
            ConfigLocale("recoiless")
        );
        
        ModConfigs.Update();
    }
    
    [HarmonyPatch(typeof(GunScript), "Update")]
    private static class GunScriptUpdatePatch
    {
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        private static void Prefix(GunScript __instance)
        {
            _hasOne = __instance.roundInChamber == GunScript.RoundInChamber.Round;
            
            if (ModConfigs.AutoRack 
                && __instance.roundInChamber 
                    is GunScript.RoundInChamber.Casing or GunScript.RoundInChamber.None
                && __instance.roundsInMag > 0)
            {
                __instance.roundsInMag--;
                __instance.roundInChamber = GunScript.RoundInChamber.Round;
                __instance.racked = false;
            }
            
            if (ModConfigs.InfiniteAmmunition)  __instance.roundsInMag = __instance.magCapacity;
            __instance.knockBack = ModConfigs.Recoiless ? 0 : 8;
            if (ModConfigs.IndestructibleGun) __instance.conditionLossPerShot = 0;
            if (!ModConfigs.AmmunitionUi) PlayerCameraHandleGunMenuPatch.DestroyAmmunitionUi();
        }
    }
    
    [HarmonyPatch(typeof(GunScript), "JamChance")]
    private static class GunScriptJamChancePatch
    {
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        private static void Postfix(ref float __result)
        {
            if (!ModConfigs.NeverJam) return;
            __result = 0;
        }
    }
    
    [HarmonyPatch(typeof(PlayerCamera), "HandleGunMenu")]
    public static class PlayerCameraHandleGunMenuPatch
    {
        private static TextMeshProUGUI _ammunitionText;
        private static GameObject _ammunitionUiObject;
        private static bool _fontInitialized;
        
        private static int _remainingAmmunition;
        private static int _maximumAmmunition;
        
        private static TMP_FontAsset GameFont
        {
            get
            {
                if (_fontInitialized) return field;
                field = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name.Contains("Retro GamingPix"));
                _fontInitialized = true;
                return field;
            }
        }
        
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        private static void Postfix(PlayerCamera __instance)
        {
            if (!ModConfigs.AmmunitionUi) return;
            
            var handSlot = __instance.body.handSlot;
            if (!__instance.body.HoldingItem(handSlot))
            {
                DestroyAmmunitionUi();
                return;
            }
            
            var item = __instance.body.GetItem(handSlot);
            if (!item.Stats.HasTag("gun"))
            {
                DestroyAmmunitionUi();
                return;
            }
            
            GunScript component = item.GetComponent<GunScript>();

            _remainingAmmunition = component.roundsInMag;
            _maximumAmmunition = component.magCapacity;
            
            CreateOrUpdateAmmunitionUi(__instance);
            UpdateAmmunitionUi();
            
            SyncVisibility(__instance.gunMenu);
        }
        
        private static void CreateOrUpdateAmmunitionUi(PlayerCamera camera)
        {
            if (_ammunitionUiObject == null)
            {
                GameObject ammunitionUi = new GameObject("AmmunitionUi");
                DontDestroyOnLoad(ammunitionUi);
        
                Canvas canvas = ammunitionUi.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 0;
        
                CanvasScaler canvasScaler = ammunitionUi.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        
                ammunitionUi.AddComponent<GraphicRaycaster>();
                
                _ammunitionUiObject = ammunitionUi;
                
                var gameObject = new GameObject("AmmunitionText");
                gameObject.transform.SetParent(_ammunitionUiObject.transform, false);
                
                RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = new Vector2(150f, 30f);
                
                _ammunitionText = gameObject.AddComponent<TextMeshProUGUI>();
                _ammunitionText.alignment = TextAlignmentOptions.Center;
            }
            
            Vector2 gunMenuPos = GetGunMenuPosition(camera);
            RectTransform textRectTransform = _ammunitionText.GetComponent<RectTransform>();
            textRectTransform.anchoredPosition = new Vector2(gunMenuPos.x, gunMenuPos.y - 450f);
            
            if (GameFont != null)
            {
                _ammunitionText.font = GameFont;
            }
            
            SyncVisibility(camera.gunMenu);
        }
        
        private static Vector2 GetGunMenuPosition(PlayerCamera camera)
        {
            if (camera.gunMenu == null) return new Vector2(0f, 50f);
            RectTransform gunMenuRect = camera.gunMenu.GetComponent<RectTransform>();
            if (gunMenuRect == null) return new Vector2(0f, 50f);
            Vector2 pos = gunMenuRect.anchoredPosition;
            pos.y -= gunMenuRect.rect.height * 0.5f;
            return pos;

        }
        
        private static void UpdateAmmunitionUi()
        {
            var realremainingAmmunition = _hasOne ? _remainingAmmunition + 1 : _remainingAmmunition;
            if (_ammunitionText == null)
                return;

            if (!ModConfigs.InfiniteAmmunition)
            {
                if (realremainingAmmunition >= 0.8)
                {
                    _ammunitionText.color = Color.green;
                }
                else if (realremainingAmmunition >= 0.5)
                {
                    _ammunitionText.color = Color.yellow;
                }
                else
                {
                    _ammunitionText.color = Color.red;
                }
                _ammunitionText.fontSize = 32;
                _ammunitionText.text = $"{realremainingAmmunition} / {_maximumAmmunition + 1}";
            }
            else
            {
                _ammunitionText.fontSize = 128;
                _ammunitionText.color = Color.black;
                _ammunitionText.text = "∞";
            }
        }
        
        private static void SyncVisibility(GameObject gunMenu)
        {
            if (_ammunitionUiObject == null || gunMenu == null)
                return;
                
            _ammunitionUiObject.SetActive(gunMenu.activeSelf);
        }
        
        public static void DestroyAmmunitionUi()
        {
            if (_ammunitionUiObject == null) return;
            Destroy(_ammunitionUiObject);
            _ammunitionUiObject = null;
            _ammunitionText = null;
        }
    }

    private static string ConfigLocale(string config)
    {
        return ModLocale.GetFormat($"config.lazyshooting.{config}");
    }
}