using System.Diagnostics;
using System.Linq;
using HarmonyLib;

namespace RepoKastimMod.Slots;

[HarmonyPatch(typeof(Inventory), "Awake")]
internal static class InventoryAwakePatch
{
    private static void Postfix(Inventory __instance) => InventorySlotList.EnsureSlots(__instance);
}

[HarmonyPatch(typeof(Inventory), "InventorySpotAddAtIndex")]
internal static class InventorySpotAddAtIndexPatch
{
    private static void Prefix(Inventory __instance, int index) => InventorySlotList.EnsureSlots(__instance, index + 1);
}

[HarmonyPatch(typeof(InventoryUI), "Start")]
internal static class InventoryUiStartPatch
{
    private static void Postfix(InventoryUI __instance) => InventoryUiBuilder.Rebuild(__instance);
}

[HarmonyPatch(typeof(InventorySpot), "Start")]
internal static class InventorySpotStartPatch
{
    private static void Prefix(InventorySpot __instance) => InventoryBatteryBinding.Bind(__instance, activateForVanillaStart: true);

    private static void Postfix(InventorySpot __instance) => InventoryBatteryBinding.Refresh(__instance);
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

[HarmonyPatch(typeof(ItemEquippable), "RPC_RequestEquip")]
internal static class ItemEquippableRequestEquipPatch
{
    private static bool Prefix(int spotIndex)
    {
        if (IsRestoringFromItemNameLogic() && Plugin.KeepItemsInTruck.Value)
        {
            return false;
        }

        if (SemiFunc.IsMultiplayer() && Plugin.HostProtection.Value && spotIndex >= Plugin.EffectiveSlotCount)
        {
            return false;
        }

        return true;
    }

    private static bool IsRestoringFromItemNameLogic()
    {
        return new StackTrace().GetFrames()?.Any(frame => frame.GetMethod()?.Name == "SetItemNameLOGIC") ?? false;
    }
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
    private static void Postfix() => ExtraSlotState.Clear();
}
