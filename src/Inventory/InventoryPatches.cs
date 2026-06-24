using HarmonyLib;

namespace RepoKastimMod.Slots;

[HarmonyPatch(typeof(InventoryUI), "Start")]
internal static class InventoryUiStartPatch
{
    private static void Postfix(InventoryUI __instance) => InventoryUiBuilder.Rebuild(__instance);
}

[HarmonyPatch(typeof(InventorySpot), "Start")]
internal static class InventorySpotStartPatch
{
    private static void Prefix(InventorySpot __instance) => InventoryBatteryBinding.Bind(__instance, activateForVanillaStart: true);

    private static void Postfix(InventorySpot __instance)
    {
        InventoryBatteryBinding.Refresh(__instance);
        if (__instance != null && __instance.inventorySpotIndex >= Plugin.VanillaSlotCount)
        {
            SlotRegistry.Track(__instance);
            SlotRegistry.EnsureRegistered();
        }
    }
}

[HarmonyPatch(typeof(InventorySpot), "Update")]
internal static class InventorySpotUpdatePatch
{
    private static void Postfix(InventorySpot __instance) => ExtraSlotInput.HandleExtraSlotHotkey(__instance);
}

[HarmonyPatch(typeof(InventorySpot), "HandleInput")]
internal static class InventorySpotHandleInputPatch
{
    private static bool Prefix(InventorySpot __instance) => AutoItemSwap.PrefixHandleInput(__instance);
}

[HarmonyPatch(typeof(InventorySpot), "StateOccupied")]
internal static class InventorySpotStateOccupiedPatch
{
    private static void Prefix(InventorySpot __instance) => InventoryBatteryBinding.Bind(__instance, activateForVanillaStart: false);
}

[HarmonyPatch(typeof(InventorySpot), "EquipItem")]
internal static class InventorySpotEquipItemPatch
{
    private static void Postfix(InventorySpot __instance) => InventoryBatteryBinding.Refresh(__instance);
}

[HarmonyPatch(typeof(InventorySpot), "UpdateUI")]
internal static class InventorySpotUpdateUiPatch
{
    private static void Postfix(InventorySpot __instance) => InventoryBatteryBinding.Refresh(__instance);
}

[HarmonyPatch(typeof(StatsManager), "PlayerInventoryUpdate")]
internal static class StatsManagerPlayerInventoryUpdatePatch
{
    private static void Postfix(string _steamID, string itemName, int spot) =>
        ExtraSlotState.TrackInventoryUpdate(_steamID, itemName, spot);
}

[HarmonyPatch(typeof(MainMenuOpen), "Start")]
internal static class MainMenuOpenStartPatch
{
    private static void Postfix()
    {
        ExtraSlotState.Clear();
        SlotRegistry.Clear();
    }
}
