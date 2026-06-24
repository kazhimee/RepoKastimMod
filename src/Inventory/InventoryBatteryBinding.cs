using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RepoKastimMod.Slots;

internal static class InventoryBatteryBinding
{
    private static readonly FieldInfo BatteryVisualLogicField = AccessTools.Field(typeof(InventorySpot), "batteryVisualLogic");
    private static readonly FieldInfo BarsField = AccessTools.Field(typeof(BatteryVisualLogic), "bars");
    private static readonly FieldInfo TargetScaleField = AccessTools.Field(typeof(BatteryVisualLogic), "targetScale");
    private static readonly FieldInfo TargetScaleOriginalField = AccessTools.Field(typeof(BatteryVisualLogic), "targetScaleOriginal");
    private static readonly FieldInfo TargetRotationField = AccessTools.Field(typeof(BatteryVisualLogic), "targetRotation");
    private static readonly FieldInfo TargetRotationOriginalField = AccessTools.Field(typeof(BatteryVisualLogic), "targetRotationOriginal");
    private static readonly FieldInfo TargetPositionField = AccessTools.Field(typeof(BatteryVisualLogic), "targetPosition");
    private static readonly FieldInfo TargetPositionOriginalField = AccessTools.Field(typeof(BatteryVisualLogic), "targetPositionOriginal");
    private static readonly FieldInfo DoOutroField = AccessTools.Field(typeof(BatteryVisualLogic), "doOutro");
    private static readonly FieldInfo SpringScaleField = AccessTools.Field(typeof(BatteryVisualLogic), "springScale");
    private static readonly FieldInfo SpringRotationField = AccessTools.Field(typeof(BatteryVisualLogic), "springRotation");
    private static readonly FieldInfo SpringPositionField = AccessTools.Field(typeof(BatteryVisualLogic), "springPosition");
    private static readonly FieldInfo SpringFloatLastPositionField = AccessTools.Field(typeof(SpringFloat), "lastPosition");
    private static readonly FieldInfo SpringVectorLastPositionField = AccessTools.Field(typeof(SpringVector3), "lastPosition");

    internal static BatteryVisualLogic Bind(InventorySpot spot, bool activateForVanillaStart)
    {
        if (spot == null)
        {
            return null;
        }

        if (BatteryVisualLogicField?.GetValue(spot) is BatteryVisualLogic cached &&
            cached != null &&
            cached.transform.IsChildOf(spot.transform))
        {
            if (activateForVanillaStart && !cached.gameObject.activeSelf)
            {
                cached.gameObject.SetActive(true);
            }

            return cached;
        }

        var visual = spot.GetComponentsInChildren<BatteryVisualLogic>(true).FirstOrDefault();
        if (visual == null)
        {
            return null;
        }

        if (activateForVanillaStart && !visual.gameObject.activeSelf)
        {
            visual.gameObject.SetActive(true);
        }

        BatteryVisualLogicField?.SetValue(spot, visual);
        return visual;
    }

    internal static void PrepareFreshClone(InventorySpot spot, BatteryVisualLogic templateVisual)
    {
        var visual = Bind(spot, activateForVanillaStart: true);
        if (visual == null)
        {
            Plugin.Log.LogWarning($"Inventory slot {spot.inventorySpotIndex + 1} has no BatteryVisualLogic child.");
            return;
        }

        BarsField?.SetValue(visual, new List<GameObject>());
        ResetCloneTargets(visual, templateVisual);
        ResetSprings(visual, GetTargetScale(visual, templateVisual));
    }

    internal static void Refresh(InventorySpot spot)
    {
        var visual = Bind(spot, activateForVanillaStart: false);
        if (visual == null)
        {
            return;
        }

        var battery = spot.CurrentItem == null
            ? null
            : spot.CurrentItem.GetComponent<ItemBattery>();

        if (battery == null)
        {
            return;
        }

        visual.itemBattery = battery;
        if (!visual.gameObject.activeSelf)
        {
            visual.gameObject.SetActive(true);
        }

        visual.ResetOutro();
        visual.BatteryBarsSet();
        visual.BatteryBarsUpdate(-1, true);
    }

    private static void ResetCloneTargets(BatteryVisualLogic visual, BatteryVisualLogic templateVisual)
    {
        var targetPosition = GetTargetPosition(visual, templateVisual);
        var targetScale = GetTargetScale(visual, templateVisual);
        var targetRotation = GetFieldValue(TargetRotationOriginalField, templateVisual, 0f);

        visual.transform.localPosition = targetPosition;
        visual.transform.localScale = new Vector3(targetScale, targetScale, targetScale);
        visual.transform.localRotation = Quaternion.Euler(0f, 0f, targetRotation);

        TargetPositionField?.SetValue(visual, targetPosition);
        TargetPositionOriginalField?.SetValue(visual, targetPosition);
        TargetScaleField?.SetValue(visual, targetScale);
        TargetScaleOriginalField?.SetValue(visual, targetScale);
        TargetRotationField?.SetValue(visual, targetRotation);
        TargetRotationOriginalField?.SetValue(visual, targetRotation);
        DoOutroField?.SetValue(visual, false);
    }

    private static Vector3 GetTargetPosition(BatteryVisualLogic visual, BatteryVisualLogic templateVisual)
    {
        var position = GetFieldValue(TargetPositionOriginalField, templateVisual, visual.transform.localPosition);
        if (float.IsNaN(position.x) || float.IsInfinity(position.x))
        {
            return visual.transform.localPosition;
        }

        return position;
    }

    private static float GetTargetScale(BatteryVisualLogic visual, BatteryVisualLogic templateVisual)
    {
        var scale = GetFieldValue(TargetScaleOriginalField, templateVisual, 0f);
        if (scale > 0.001f)
        {
            return scale;
        }

        scale = GetFieldValue(TargetScaleField, templateVisual, 0f);
        if (scale > 0.001f)
        {
            return scale;
        }

        scale = Mathf.Max(visual.transform.localScale.x, visual.transform.localScale.y);
        return scale > 0.001f ? scale : 0.5f;
    }

    private static T GetFieldValue<T>(FieldInfo field, object instance, T fallback)
    {
        if (field == null || instance == null)
        {
            return fallback;
        }

        var value = field.GetValue(instance);
        return value is T typed ? typed : fallback;
    }

    private static void ResetSprings(BatteryVisualLogic visual, float targetScale)
    {
        var scaleSpring = new SpringFloat { damping = 0.4f, speed = 30f };
        SpringFloatLastPositionField?.SetValue(scaleSpring, targetScale);
        SpringScaleField?.SetValue(visual, scaleSpring);

        var rotationSpring = new SpringFloat { damping = 0.3f, speed = 40f };
        SpringFloatLastPositionField?.SetValue(rotationSpring, 0f);
        SpringRotationField?.SetValue(visual, rotationSpring);

        var positionSpring = new SpringVector3 { damping = 0.35f, speed = 30f };
        SpringVectorLastPositionField?.SetValue(positionSpring, visual.transform.localPosition);
        SpringPositionField?.SetValue(visual, positionSpring);
    }
}
