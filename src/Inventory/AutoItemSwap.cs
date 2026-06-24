using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RepoKastimMod.Slots;

internal static class AutoItemSwap
{
    private const float EquipWaitTimeout = 0.75f;

    private static readonly FieldInfo GrabbedPhysGrabObjectField =
        AccessTools.Field(typeof(PhysGrabber), "grabbedPhysGrabObject");

    private static readonly FieldInfo PlayerInputDisableTimerField =
        AccessTools.Field(typeof(PlayerController), "InputDisableTimer");

    private static readonly FieldInfo LastEquipTimeField =
        AccessTools.Field(typeof(InventorySpot), "lastEquipTime");

    private static readonly FieldInfo EquipCooldownField =
        AccessTools.Field(typeof(InventorySpot), "equipCooldown");

    private static readonly FieldInfo HandleInputField =
        AccessTools.Field(typeof(InventorySpot), "handleInput");

    internal static bool PrefixHandleInput(InventorySpot spot)
    {
        if (!Plugin.AutoSwapItems.Value || spot == null || ShouldLetVanillaHandle(spot))
        {
            return true;
        }

        var heldItem = GetHeldItem();
        if (heldItem == null || !spot.IsOccupied())
        {
            return true;
        }

        var currentItem = spot.CurrentItem;
        if (currentItem == null || currentItem == heldItem)
        {
            return true;
        }

        var slotIndex = spot.inventorySpotIndex;
        var ownerViewId = SemiFunc.IsMultiplayer() ? PhysGrabber.instance.photonView.ViewID : -1;
        SetInputCooldown(spot);
        currentItem.RequestUnequip();
        Plugin.Instance.StartCoroutine(EquipWhenSlotIsReady(heldItem, slotIndex, ownerViewId, currentItem));
        return false;
    }

    private static bool ShouldLetVanillaHandle(InventorySpot spot)
    {
        if (SemiFunc.RunIsArena())
        {
            return true;
        }

        if (PlayerController.instance != null &&
            PlayerInputDisableTimerField != null &&
            (float)PlayerInputDisableTimerField.GetValue(PlayerController.instance) > 0f)
        {
            return true;
        }

        return IsInCooldown(spot);
    }

    private static bool IsInCooldown(InventorySpot spot)
    {
        if (LastEquipTimeField == null || EquipCooldownField == null || HandleInputField == null)
        {
            return false;
        }

        if ((bool)HandleInputField.GetValue(spot))
        {
            return false;
        }

        var lastEquipTime = (float)LastEquipTimeField.GetValue(spot);
        var equipCooldown = (float)EquipCooldownField.GetValue(spot);
        return Time.time - lastEquipTime < equipCooldown;
    }

    private static void SetInputCooldown(InventorySpot spot)
    {
        LastEquipTimeField?.SetValue(spot, Time.time);
        HandleInputField?.SetValue(spot, false);
    }

    private static ItemEquippable GetHeldItem()
    {
        var grabber = PhysGrabber.instance;
        if (grabber == null || !grabber.grabbed)
        {
            return null;
        }

        var grabbedObject = GrabbedPhysGrabObjectField?.GetValue(grabber) as PhysGrabObject;
        return grabbedObject == null ? null : grabbedObject.GetComponent<ItemEquippable>();
    }

    private static IEnumerator EquipWhenSlotIsReady(
        ItemEquippable heldItem,
        int slotIndex,
        int ownerViewId,
        ItemEquippable previousSlotItem)
    {
        var timeoutAt = Time.time + EquipWaitTimeout;
        while (Time.time < timeoutAt)
        {
            var spot = GetSpot(slotIndex);
            if (spot == null || !spot.IsOccupied() || spot.CurrentItem != previousSlotItem)
            {
                break;
            }

            yield return null;
        }

        if (heldItem != null && !heldItem.IsEquipped())
        {
            heldItem.RequestEquip(slotIndex, ownerViewId);
        }
    }

    private static InventorySpot GetSpot(int slotIndex)
    {
        if (Inventory.instance == null)
        {
            return null;
        }

        InventorySlotList.EnsureSlots(Inventory.instance, slotIndex + 1);
        var slots = Inventory.instance.GetAllSpots();
        if (slotIndex < 0 || slotIndex >= slots.Count)
        {
            return null;
        }

        return slots[slotIndex];
    }
}
