using SettingsMenu.Components;
using SettingsMenu.Models;

namespace BetterSpeedometer.Patchers;

[HarmonyPatch(typeof(SettingsItemBuilder))]
public static class SettingsItemBuilderPatcher
{
    public const int SPEEDOMETER_ITEM_INDEX = 4;

    [HarmonyPostfix]
    [HarmonyPatch("Awake")]
    public static void AwakePostfix(SettingsItemBuilder __instance) // This doesn't seem to do anything but its here just in case
    {
        if (__instance.name == "Speedometer")
        {
            Plugin.Logger.LogDebug("Patching Speedometer settings item");
            __instance.asset.dropdownList = __instance.asset.dropdownList.AddToArray("FULL SPEEDOMETER");
            __instance.asset.dropdownList[1] = "SPEED ONLY";
        }
    }
    [HarmonyPrefix]
    [HarmonyPatch("ConfigureFrom")]
    public static void ConfigureFromPrefix(SettingsItemBuilder __instance, SettingsItem item)
    {
        if (item.name == "Speedometer")
        {
            Plugin.Logger.LogDebug("Patching Speedometer settings item!!");
            item.dropdownList = item.dropdownList.AddToArray("FULL SPEEDOMETER");
            item.dropdownList[1] = "SPEED ONLY";
        }
    }
}