using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RepoKastimMod.Slots;

/// <summary>
/// In R.E.P.O., the local player's <see cref="Inventory"/> is rebuilt every time the player
/// avatar respawns (which happens on truck-in for each level / round). The singleton
/// <c>Inventory.instance</c> therefore changes mid-session.
///
/// Vanilla <c>Inventory.Awake</c> only seeds 3 slot placeholders. Extra slots that we cloned
/// into <see cref="InventoryUI"/> would otherwise stay bound to the old, destroyed Inventory,
/// which is why slot 4+ silently failed to bind on the second level (and in every multiplayer
/// match where players spawn after the lobby).
///
/// This registry keeps a persistent list of every extra slot we own and re-binds them to the
/// current Inventory whenever it changes.
/// </summary>
internal static class SlotRegistry
{
    private static readonly List<InventorySpot> ExtraSlots = new();
    private static Inventory _lastInventory;

    internal static void Track(InventorySpot spot)
    {
        if (spot == null || spot.inventorySpotIndex < Plugin.VanillaSlotCount)
        {
            return;
        }

        if (!ExtraSlots.Contains(spot))
        {
            ExtraSlots.Add(spot);
        }
    }

    internal static void Forget(InventorySpot spot)
    {
        if (spot != null)
        {
            ExtraSlots.Remove(spot);
        }
    }

    internal static void Clear()
    {
        ExtraSlots.Clear();
        _lastInventory = null;
    }

    internal static void EnsureRegistered(Inventory inventory = null)
    {
        var target = inventory != null ? inventory : Inventory.instance;
        if (target == null)
        {
            return;
        }

        if (_lastInventory != target)
        {
            _lastInventory = target;
        }

        ExtraSlots.RemoveAll(spot => spot == null);
        if (ExtraSlots.Count == 0)
        {
            return;
        }

        var maxIndex = ExtraSlots.Max(spot => spot.inventorySpotIndex);
        InventorySlotList.EnsureSlots(target, maxIndex + 1);

        var slots = target.GetAllSpots();
        foreach (var spot in ExtraSlots)
        {
            if (spot == null)
            {
                continue;
            }

            var idx = spot.inventorySpotIndex;
            if (idx < Plugin.VanillaSlotCount || idx >= slots.Count)
            {
                continue;
            }

            if (slots[idx] != spot)
            {
                slots[idx] = spot;
            }
        }
    }

    /// <summary>
    /// Called when the host requests our local inventory equip a slot the local player owns.
    /// Forces UI/slot creation if necessary so vanilla equip code can resolve the slot.
    /// </summary>
    internal static bool TryEnsureLocalSlot(int slotIndex)
    {
        if (slotIndex < 0)
        {
            return false;
        }

        if (Inventory.instance == null)
        {
            return false;
        }

        InventorySlotList.EnsureSlots(Inventory.instance, slotIndex + 1);

        if (slotIndex >= Plugin.VanillaSlotCount && InventoryUI.instance != null)
        {
            var slots = Inventory.instance.GetAllSpots();
            if (slotIndex >= slots.Count || slots[slotIndex] == null)
            {
                InventoryUiBuilder.Rebuild(InventoryUI.instance);
            }
        }

        EnsureRegistered(Inventory.instance);

        var refreshed = Inventory.instance.GetAllSpots();
        return slotIndex < refreshed.Count && refreshed[slotIndex] != null;
    }
}
