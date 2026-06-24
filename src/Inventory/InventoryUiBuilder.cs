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

    private static Vector3 _vanillaInventoryUiPosition;
    private static bool _vanillaInventoryUiPositionCaptured;

    internal static void Rebuild(InventoryUI inventoryUi)
    {
        if (inventoryUi == null)
        {
            return;
        }

        var slotCount = Plugin.EffectiveSlotCount;
        var alignment = Plugin.InventoryAlignment?.Value ?? RepoKastimMod.InventoryAlignment.Center;

        try
        {
            CaptureVanillaInventoryUiPosition(inventoryUi);

            BuildSlots(inventoryUi, slotCount);
            ApplyAlignment(inventoryUi, alignment);

            InventorySlotList.EnsureSlots(Inventory.instance, slotCount);
            SlotRegistry.EnsureRegistered(Inventory.instance);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Failed to rebuild inventory slots: {ex.Message}");
        }
    }

    /// <summary>
    /// Re-runs the alignment step against the current InventoryUI without rebuilding slots.
    /// Used for live config updates.
    /// </summary>
    internal static void ReapplyAlignment()
    {
        if (InventoryUI.instance == null)
        {
            return;
        }

        try
        {
            CaptureVanillaInventoryUiPosition(InventoryUI.instance);
            var alignment = Plugin.InventoryAlignment?.Value ?? RepoKastimMod.InventoryAlignment.Center;
            ApplyAlignment(InventoryUI.instance, alignment);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Failed to reapply inventory alignment: {ex.Message}");
        }
    }

    private static void CaptureVanillaInventoryUiPosition(InventoryUI inventoryUi)
    {
        if (_vanillaInventoryUiPositionCaptured)
        {
            return;
        }

        _vanillaInventoryUiPosition = inventoryUi.transform.localPosition;
        _vanillaInventoryUiPositionCaptured = true;
    }

    private static void ApplyAlignment(InventoryUI inventoryUi, RepoKastimMod.InventoryAlignment alignment)
    {
        var slotCount = Plugin.EffectiveSlotCount;
        var horizontalOffset = ComputeHorizontalOffset(inventoryUi, alignment, slotCount);
        var verticalOffset = Plugin.InventoryVerticalOffset?.Value ?? 0f;
        var target = _vanillaInventoryUiPosition + new Vector3(horizontalOffset, verticalOffset, 0f);
        SemiUiPositionPatcher.Reposition(inventoryUi, target);
    }

    /// <summary>
    /// Compute a canvas-aware shift so the slot row lands near the requested edge regardless
    /// of resolution. We work in the canvas's local UI units (CanvasScaler handles physical
    /// pixels), so the result automatically scales with the user's screen.
    /// </summary>
    private static float ComputeHorizontalOffset(
        InventoryUI inventoryUi,
        RepoKastimMod.InventoryAlignment alignment,
        int slotCount)
    {
        var fine = Plugin.InventoryAlignmentFineOffset?.Value ?? 0f;
        if (alignment == RepoKastimMod.InventoryAlignment.Center)
        {
            return fine;
        }

        var canvasWidth = TryGetCanvasWidth(inventoryUi, fallback: 1920f);
        var spacing = TryGetCurrentSpacing(inventoryUi, fallback: 40f);

        // Row spans (slotCount - 1) * spacing between centres; add one slot width on each
        // side so the leftmost/rightmost slot icons don't get clipped by the screen edge.
        var rowWidth = (slotCount - 1) * spacing + spacing * 1.5f;
        var edgeMargin = 30f;

        var maxShift = Mathf.Max(0f, (canvasWidth - rowWidth) * 0.5f - edgeMargin);
        var strength = Mathf.Clamp01(Plugin.InventoryAlignmentStrength?.Value ?? 0.85f);
        var directional = maxShift * strength;

        return alignment switch
        {
            RepoKastimMod.InventoryAlignment.Left => -directional + fine,
            RepoKastimMod.InventoryAlignment.Right => directional + fine,
            _ => fine
        };
    }

    private static float TryGetCanvasWidth(InventoryUI inventoryUi, float fallback)
    {
        if (inventoryUi == null)
        {
            return fallback;
        }

        var canvas = inventoryUi.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return fallback;
        }

        var rootCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
        if (rootCanvas.transform is RectTransform rect)
        {
            var width = rect.rect.width;
            return width > 1f ? width : fallback;
        }

        return fallback;
    }

    private static float TryGetCurrentSpacing(InventoryUI inventoryUi, float fallback)
    {
        if (inventoryUi == null)
        {
            return fallback;
        }

        try
        {
            var spots = inventoryUi.transform
                .GetComponentsInChildren<InventorySpot>(true)
                .Where(s => s.inventorySpotIndex >= 0 && s.inventorySpotIndex < 3)
                .OrderBy(s => s.inventorySpotIndex)
                .ToList();

            if (spots.Count >= 2)
            {
                var spacing = Mathf.Abs(spots[1].transform.localPosition.x - spots[0].transform.localPosition.x);
                if (spacing > 1f)
                {
                    return spacing;
                }
            }
        }
        catch
        {
            /* ignored */
        }

        return fallback;
    }

    private static void BuildSlots(InventoryUI inventoryUi, int slotCount)
    {
        if (slotCount <= Plugin.VanillaSlotCount)
        {
            return;
        }

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
        var startX = centerX - spacing * (slotCount - 1) * 0.5f;
        var y = template.transform.localPosition.y;
        var z = template.transform.localPosition.z;

        for (var i = 0; i < slotCount; i++)
        {
            var spot = GetOrCreateSpot(root, template, existingSpots, i);
            if (spot == null)
            {
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
        SemiUiPositionPatcher.Reposition(spot, new Vector3(x, y, z));
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
}
