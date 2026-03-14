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
    public static ConfigEntry<bool> NeverJam;
    // ReSharper disable once MemberCanBePrivate.Global
    public static ConfigEntry<bool> AlwaysRack;
    // ReSharper disable once MemberCanBePrivate.Global
    public static ConfigEntry<bool> AmmunitionUi;
    // ReSharper disable once MemberCanBePrivate.Global
    public static ConfigEntry<bool> InfiniteAmmunition;

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
        AlwaysRack = Config.Bind(
            "General",
            "Always Rack",
            false,
            "If true, guns will automatically rack and stay racked when ammo is available."
        );
        AmmunitionUi = Config.Bind(
            "General",
            "Ammunition UI",
            true,
            "Display your ammunition in real time!"
        );
        InfiniteAmmunition = Config.Bind(
            "General",
            "Infinite Ammunition",
            false,
            "INFINITE"
        );
    }
    
    [HarmonyPatch(typeof(GunScript), "Update")]
    private static class GunScriptUpdatePatch
    {
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        private static void Prefix(GunScript __instance)
        {
            _hasOne = __instance.roundInChamber == GunScript.RoundInChamber.Round;
            
            if (AlwaysRack.Value 
                && __instance.roundInChamber 
                    is GunScript.RoundInChamber.Casing or GunScript.RoundInChamber.None
                && __instance.roundsInMag > 0)
            {
                __instance.roundsInMag--;
                __instance.roundInChamber = GunScript.RoundInChamber.Round;
                __instance.racked = false;
            }
            
            if (InfiniteAmmunition.Value)
            {
                __instance.roundsInMag = __instance.magCapacity;
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
            if (!NeverJam.Value) return;
            
            __result = 0;
        }
    }
    
    [HarmonyPatch(typeof(PlayerCamera), "HandleGunMenu")]
    private static class PlayerCameraHandleGunMenuPatch
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
            if (!AmmunitionUi.Value) return;
            
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
                _ammunitionText.fontSize = 32;
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
            
            _ammunitionText.text = $"{realremainingAmmunition} / {_maximumAmmunition + 1}";
        }
        
        private static void SyncVisibility(GameObject gunMenu)
        {
            if (_ammunitionUiObject == null || gunMenu == null)
                return;
                
            _ammunitionUiObject.SetActive(gunMenu.activeSelf);
        }
        
        private static void DestroyAmmunitionUi()
        {
            if (_ammunitionUiObject == null) return;
            Destroy(_ammunitionUiObject);
            _ammunitionUiObject = null;
            _ammunitionText = null;
        }
    }
}