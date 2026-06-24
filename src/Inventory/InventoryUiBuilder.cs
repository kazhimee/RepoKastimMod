using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using TMPro;
using UnityEngine;

namespace RepoKastimMod.Slots;

internal static class InventoryUiBuilder
{
    private static readonly FieldInfo AllChildrenField = AccessTools.Field(typeof(SemiUI), "allChildren");
    private static readonly FieldInfo CurrentStateField = AccessTools.Field(typeof(InventorySpot), "currentState");
    private static readonly FieldInfo CurrentItemField = AccessTools.Field(typeof(InventorySpot), "<CurrentItem>k__BackingField");
    private static readonly FieldInfo StateStartField = AccessTools.Field(typeof(InventorySpot), "stateStart");

    internal static void Rebuild(InventoryUI inventoryUi)
    {
        if (inventoryUi == null)
        {
            return;
        }

        var slotCount = Plugin.EffectiveSlotCount;
        var alignment = Plugin.InventoryAlignment?.Value ?? RepoKastimMod.InventoryAlignment.Center;
        var needsExtraSlots = slotCount > Plugin.VanillaSlotCount;
        var needsRealignment = alignment != RepoKastimMod.InventoryAlignment.Center;
        if (!needsExtraSlots && !needsRealignment)
        {
            return;
        }

        try
        {
            var root = inventoryUi.transform;
            var existingSpots = root.GetComponentsInChildren<InventorySpot>(true)
                .OrderBy(spot => spot.inventorySpotIndex)
                .ThenBy(spot => spot.name)
                .ToList();

            var template = existingSpots.FirstOrDefault(spot => spot.inventorySpotIndex == 0) ?? existingSpots.FirstOrDefault();
            if (template == null)
            {
                Plugin.Log.LogWarning("Could not find an InventorySpot template in InventoryUI.");
                return;
            }

            var allChildren = AllChildrenField?.GetValue(inventoryUi) as List<GameObject>;
            var spacing = CalculateSpacing(existingSpots);
            var centerX = CalculateCenterX(existingSpots, template.transform.localPosition.x);
            var alignmentOffset = GetAlignmentOffset(alignment);
            var startX = centerX - spacing * (slotCount - 1) * 0.5f + alignmentOffset;
            var y = template.transform.localPosition.y;
            var z = template.transform.localPosition.z;

            var iterations = Math.Max(slotCount, Plugin.VanillaSlotCount);
            for (var i = 0; i < iterations; i++)
            {
                var spot = GetOrCreateSpot(root, template, existingSpots, i);
                if (spot == null)
                {
                    continue;
                }

                if (i >= slotCount)
                {
                    // We only need to reposition existing vanilla slots when alignment forces it;
                    // never create extras beyond the configured count.
                    continue;
                }

                ConfigureSpot(spot, i, startX + spacing * i, y, z);

                var spotObject = spot.gameObject;
                if (allChildren != null && !allChildren.Contains(spotObject))
                {
                    allChildren.Add(spotObject);
                }

                if (i >= Plugin.VanillaSlotCount)
                {
                    SlotRegistry.Track(spot);
                }
            }

            InventorySlotList.EnsureSlots(Inventory.instance, slotCount);
            SlotRegistry.EnsureRegistered(Inventory.instance);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Failed to rebuild inventory slots: {ex.Message}");
        }
    }

    private static InventorySpot GetOrCreateSpot(
        Transform root,
        InventorySpot template,
        List<InventorySpot> existingSpots,
        int index)
    {
        var existing = existingSpots.FirstOrDefault(spot => spot.inventorySpotIndex == index);
        if (existing != null)
        {
            return existing;
        }

        var namedChild = root.Find($"Inventory Spot {index + 1}");
        if (namedChild != null && namedChild.TryGetComponent(out InventorySpot namedSpot))
        {
            if (!existingSpots.Contains(namedSpot))
            {
                existingSpots.Add(namedSpot);
            }

            return namedSpot;
        }

        var clone = UnityEngine.Object.Instantiate(template.transform, template.transform.parent);
        clone.name = $"Inventory Spot {index + 1}";

        foreach (var photonView in clone.GetComponents<PhotonView>())
        {
            UnityEngine.Object.Destroy(photonView);
        }

        var spot = clone.GetComponent<InventorySpot>();
        var templateVisual = template.GetComponentsInChildren<BatteryVisualLogic>(true).FirstOrDefault();
        InventoryBatteryBinding.PrepareFreshClone(spot, templateVisual);
        ClearCopiedRuntimeState(spot);
        existingSpots.Add(spot);
        return spot;
    }

    private static void ClearCopiedRuntimeState(InventorySpot spot)
    {
        if (spot == null)
        {
            return;
        }

        CurrentItemField?.SetValue(spot, null);
        if (CurrentStateField != null)
        {
            CurrentStateField.SetValue(spot, Enum.ToObject(CurrentStateField.FieldType, 0));
        }

        StateStartField?.SetValue(spot, true);
    }

    private static void ConfigureSpot(InventorySpot spot, int index, float x, float y, float z)
    {
        spot.inventorySpotIndex = index;
        spot.name = $"Inventory Spot {index + 1}";
        spot.transform.localPosition = new Vector3(x, y, z);
        spot.gameObject.SetActive(true);
        InventoryBatteryBinding.Bind(spot, activateForVanillaStart: false);

        var label = GetSlotLabel(index);
        if (spot.noItem != null)
        {
            spot.noItem.text = label;
        }

        var numbers = spot.transform.Find("Numbers");
        if (numbers != null && numbers.TryGetComponent(out TextMeshProUGUI numberText))
        {
            numberText.text = label;
        }
    }

    private static float CalculateSpacing(List<InventorySpot> spots)
    {
        var positions = spots
            .Where(spot => spot.inventorySpotIndex >= 0 && spot.inventorySpotIndex < 3)
            .OrderBy(spot => spot.inventorySpotIndex)
            .Select(spot => spot.transform.localPosition.x)
            .ToList();

        if (positions.Count >= 2)
        {
            var spacing = Mathf.Abs(positions[1] - positions[0]);
            if (spacing > 1f)
            {
                return spacing;
            }
        }

        return 40f;
    }

    private static float CalculateCenterX(List<InventorySpot> spots, float fallback)
    {
        var positions = spots
            .Where(spot => spot.inventorySpotIndex >= 0 && spot.inventorySpotIndex < 3)
            .OrderBy(spot => spot.inventorySpotIndex)
            .Select(spot => spot.transform.localPosition.x)
            .ToList();

        if (positions.Count >= 3)
        {
            return (positions[0] + positions[2]) * 0.5f;
        }

        return fallback;
    }

    private static string GetSlotLabel(int index) => index == 9 ? "0" : (index + 1).ToString();

    private static float GetAlignmentOffset(RepoKastimMod.InventoryAlignment alignment)
    {
        var magnitude = Plugin.InventoryAlignmentOffset?.Value ?? 350f;
        return alignment switch
        {
            RepoKastimMod.InventoryAlignment.Left => -magnitude,
            RepoKastimMod.InventoryAlignment.Right => magnitude,
            _ => 0f
        };
    }
}
