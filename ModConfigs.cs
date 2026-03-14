namespace LazyShooting;

public static class ModConfigs
{
    public static bool AutoRack;
    public static bool AmmunitionUi;
    public static bool IndestructibleGun;
    public static bool Recoiless;
    public static bool InfiniteAmmunition;
    public static bool NeverJam;
    
    public static void Update()
    { 
        AutoRack = Plugin.AutoRack.Value;
        AmmunitionUi = Plugin.AmmunitionUi.Value;
        IndestructibleGun = Plugin.IndestructibleGun.Value;
        Recoiless = Plugin.Recoiless.Value;
        InfiniteAmmunition = Plugin.InfiniteAmmunition.Value;
        NeverJam = Plugin.NeverJam.Value;
    }
}