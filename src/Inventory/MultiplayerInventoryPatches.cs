using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using Photon.Pun;

namespace RepoKastimMod.Slots;

internal static class MultiplayerInventorySupport
{
    internal static bool IsLocalEquipOwner(int ownerId)
    {
        if (!SemiFunc.IsMultiplayer())
        {
            return true;
        }

        var grabber = PhysGrabber.instance;
        return grabber != null &&
               grabber.photonView != null &&
               grabber.photonView.ViewID == ownerId;
    }
}

/// <summary>
/// Inventory is recreated every level transition. Re-bind all extra slots to whichever
/// instance is now the singleton.
/// </summary>
[HarmonyPatch(typeof(Inventory), "Awake")]
internal static class InventoryAwakePatch
{
    private static void Postfix(Inventory __instance)
    {
        InventorySlotList.EnsureSlots(__instance);
        SlotRegistry.EnsureRegistered(__instance);
    }
}

[HarmonyPatch(typeof(Inventory), "Start")]
internal static class InventoryStartPatch
{
    private static void Postfix(Inventory __instance)
    {
        SlotRegistry.EnsureRegistered(__instance);
        Plugin.Instance?.ScheduleInventoryRebuild();
    }
}

/// <summary>
/// Safety net: any lookup against the inventory list ensures size and re-registration.
/// Fixes the case where vanilla <see cref="ItemEquippable.RPC_UpdateItemState"/> resolves
/// slot 4+ on the equipping client before our coroutines have caught up.
/// </summary>
[HarmonyPatch(typeof(Inventory), "GetSpotByIndex")]
internal static class InventoryGetSpotByIndexPatch
{
    private static void Prefix(Inventory __instance, int index)
    {
        InventorySlotList.EnsureSlots(__instance, index + 1);
        SlotRegistry.EnsureRegistered(__instance);
    }
}

[HarmonyPatch(typeof(Inventory), "InventorySpotAddAtIndex")]
internal static class InventorySpotAddAtIndexPatch
{
    private static void Prefix(Inventory __instance, int index) =>
        InventorySlotList.EnsureSlots(__instance, index + 1);
}

/// <summary>
/// When the host broadcasts an equip RPC for an extra slot, make sure the equipping
/// client actually has that slot registered before the vanilla method runs.
/// </summary>
[HarmonyPatch(typeof(ItemEquippable), "RPC_UpdateItemState")]
internal static class ItemEquippableRpcUpdateItemStatePatch
{
    private static void Prefix(int spotIndex, int ownerId)
    {
        if (spotIndex < Plugin.VanillaSlotCount)
        {
            return;
        }

        if (!MultiplayerInventorySupport.IsLocalEquipOwner(ownerId))
        {
            return;
        }

        SlotRegistry.TryEnsureLocalSlot(spotIndex);
    }
}

/// <summary>
/// Host-side validation. The master processes equip requests from clients and forwards them
/// to everyone. Only enforce the host's slot cap when host protection is enabled.
/// </summary>
[HarmonyPatch(typeof(ItemEquippable), "RPC_RequestEquip")]
internal static class ItemEquippableRequestEquipPatch
{
    private static bool Prefix(int spotIndex)
    {
        if (IsRestoringFromItemNameLogic() && Plugin.KeepItemsInTruck.Value)
        {
            return false;
        }

        if (!SemiFunc.IsMultiplayer() || !PhotonNetwork.IsMasterClient || !Plugin.HostProtection.Value)
        {
            return true;
        }

        return spotIndex < Plugin.EffectiveSlotCount;
    }

    private static bool IsRestoringFromItemNameLogic()
    {
        return new StackTrace()
            .GetFrames()?
            .Any(frame => frame.GetMethod()?.Name == "SetItemNameLOGIC") ?? false;
    }
}
