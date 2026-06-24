using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RepoKastimMod.Slots;

/// <summary>
/// SemiUI runs its own per-frame position animation (<c>UpdatePositionLogic</c>) that
/// resets <c>transform.localPosition</c> using cached anchor fields captured during
/// <c>Start()</c>. Setting localPosition from the outside is therefore overridden a frame
/// later.
///
/// This helper updates the cached anchors (<c>showPosition</c>, <c>hidePosition</c>,
/// <c>hidePositionCurrent</c>, <c>initialPosition</c>) so the new local position becomes
/// the SemiUI's idle anchor and survives every Update tick.
/// </summary>
internal static class SemiUiPositionPatcher
{
    private static readonly FieldInfo InitialPositionField = AccessTools.Field(typeof(SemiUI), "initialPosition");
    private static readonly FieldInfo ShowPositionField = AccessTools.Field(typeof(SemiUI), "showPosition");
    private static readonly FieldInfo HidePositionField = AccessTools.Field(typeof(SemiUI), "hidePosition");
    private static readonly FieldInfo HidePositionCurrentField = AccessTools.Field(typeof(SemiUI), "hidePositionCurrent");

    internal static void Reposition(SemiUI ui, Vector3 newLocalPosition)
    {
        if (ui == null)
        {
            return;
        }

        var newShow = (Vector2)newLocalPosition;
        var oldShow = TryReadVector2(ShowPositionField, ui);
        var delta = newShow - oldShow;

        ((Component)ui).transform.localPosition = newLocalPosition;

        if (InitialPositionField != null)
        {
            InitialPositionField.SetValue(ui, (Vector3)newLocalPosition);
        }

        if (ShowPositionField != null)
        {
            ShowPositionField.SetValue(ui, newShow);
        }

        if (HidePositionField != null)
        {
            var oldHide = TryReadVector2(HidePositionField, ui);
            HidePositionField.SetValue(ui, oldHide + delta);
        }

        if (HidePositionCurrentField != null)
        {
            var oldHideCurrent = TryReadVector2(HidePositionCurrentField, ui);
            HidePositionCurrentField.SetValue(ui, oldHideCurrent + delta);
        }
    }

    private static Vector2 TryReadVector2(FieldInfo field, object instance)
    {
        if (field == null || instance == null)
        {
            return Vector2.zero;
        }

        try
        {
            var value = field.GetValue(instance);
            return value is Vector2 v ? v : Vector2.zero;
        }
        catch
        {
            return Vector2.zero;
        }
    }
}
