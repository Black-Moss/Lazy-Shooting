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
        
        private static int _remainingAmmunition;
        private static int _maximumAmmunition;
        
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        private static void Postfix(PlayerCamera __instance)
        {
            if (!__instance.body.HoldingItem(__instance.body.handSlot) ||
                !__instance.body.GetItem(__instance.body.handSlot).Stats.HasTag("gun")) return;
            GunScript component = __instance.body.GetItem(__instance.body.handSlot).GetComponent<GunScript>();

            _remainingAmmunition = component.roundsInMag;
            _maximumAmmunition = component.magCapacity;
            
            CreateOrUpdateAmmunitionUi(__instance);
        }
        
        private static void CreateOrUpdateAmmunitionUi(PlayerCamera camera)
        {
            if (_ammunitionUiObject == null)
            {
                GameObject ammunitionUi = new GameObject("AmmunitionUi");
                DontDestroyOnLoad(ammunitionUi);
        
                Canvas canvas = ammunitionUi.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
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
    
            _ammunitionText.fontSize = 32;
            _ammunitionText.alignment = TextAlignmentOptions.Center;
            _ammunitionText.color = Color.white;
            _ammunitionText.outlineWidth = 0.2f;
            _ammunitionText.outlineColor = Color.black;
        }
        
        private static Vector2 GetGunMenuPosition(PlayerCamera camera)
        {
            if (camera.gunMenu != null)
            {
                RectTransform gunMenuRect = camera.gunMenu.GetComponent<RectTransform>();
                if (gunMenuRect != null)
                {
                    Vector2 pos = gunMenuRect.anchoredPosition;
                    pos.y -= gunMenuRect.rect.height * 0.5f;
                    return pos;
                }
            }
            
            return new Vector2(0f, 50f);
        }
        
        private static void UpdateAmmunitionUi()
        {
            if (_ammunitionText == null)
                return;

            _ammunitionText.color = _remainingAmmunition switch
            {
                >= 10 => Color.green,
                >= 3 => Color.yellow,
                _ => Color.red
            };
            
            _ammunitionText.text = $"{_remainingAmmunition} / {_maximumAmmunition}";
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