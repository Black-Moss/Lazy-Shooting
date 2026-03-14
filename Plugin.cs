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
    private static bool _hasOne = false;
    
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
            if (__instance.roundInChamber == GunScript.RoundInChamber.Round
                && __instance.roundsInMag == 0)
            {
                _hasOne = true;
            }
            else
            {
                _hasOne = false;
            }
            
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
            // 如果启用了永不卡壳，则将卡壳几率设置为 0
            if (!NeverJam.Value) return;
            
            __result = 0;
        }
    }
    
    [HarmonyPatch(typeof(PlayerCamera), "HandleGunMenu")]
    private static class PlayerCameraHandleGunMenuPatch
    {
        private static TextMeshProUGUI _ammunitionText;
        private static GameObject _ammunitionUiObject;
        private static readonly TMP_FontAsset GameFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name.Contains("Retro GamingPix"));
        
        private static int _remainingAmmunition;
        private static int _maximumAmmunition;
        
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        private static void Postfix(PlayerCamera __instance)
        {
            if (!AmmunitionUi.Value) return;
            if (!__instance.body.HoldingItem(__instance.body.handSlot) ||
                !__instance.body.GetItem(__instance.body.handSlot).Stats.HasTag("gun"))
            {
                DestroyAmmunitionUi();
                return;
            }
            
            GunScript component = __instance.body.GetItem(__instance.body.handSlot).GetComponent<GunScript>();

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
            }
            
            Transform textTransform = _ammunitionUiObject.transform.Find("AmmunitionText");
            GameObject gameObject;
            
            if (textTransform == null)
            {
                gameObject = new GameObject("AmmunitionText");
                gameObject.transform.SetParent(_ammunitionUiObject.transform, false);
            }
            else
            {
                gameObject = textTransform.gameObject;
            }
    
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }
            
            if (_ammunitionText == null)
            {
                _ammunitionText = gameObject.GetComponent<TextMeshProUGUI>();
                if (_ammunitionText == null)
                {
                    _ammunitionText = gameObject.AddComponent<TextMeshProUGUI>();
                }
            }
            
            Vector2 gunMenuPos = GetGunMenuPosition(camera);
            
            rectTransform.anchoredPosition = new Vector2(gunMenuPos.x, gunMenuPos.y - 450f);
            rectTransform.sizeDelta = new Vector2(150f, 30f);
            
            _ammunitionText.font = GameFont;
            _ammunitionText.fontSize = 32;
            _ammunitionText.alignment = TextAlignmentOptions.Center;
            
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
            if (_ammunitionText == null)
                return;
            
            if (_remainingAmmunition >= _maximumAmmunition * 0.8)
            {
                _ammunitionText.color = Color.green;
            }
            else if (_remainingAmmunition >= _maximumAmmunition * 0.5)
            {
                _ammunitionText.color = Color.yellow;
            }
            else
            {
                _ammunitionText.color = Color.red;
            }
            
            _ammunitionText.text = $"{(_hasOne ? _remainingAmmunition + 1 : _remainingAmmunition)} / {_maximumAmmunition + 1}";
        }
        
        private static void SyncVisibility(GameObject gunMenu)
        {
            if (_ammunitionUiObject == null || gunMenu == null)
                return;
                
            _ammunitionUiObject.SetActive(gunMenu.activeSelf);
        }
        
        private static void DestroyAmmunitionUi()
        {
            if (!(_ammunitionUiObject != null))
                return;
            Destroy(_ammunitionUiObject);
            _ammunitionUiObject = null;
            _ammunitionText = null;
        }
    }
}