using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace RepoKastimMod.Slots;

internal static class ExtraSlotInput
{
    private static readonly MethodInfo HandleInputMethod =
        AccessTools.Method(typeof(InventorySpot), "HandleInput");

    internal static void HandleExtraSlotHotkey(InventorySpot spot)
    {
        if (!Plugin.ExtraHotkeys.Value || spot == null)
        {
            return;
        }

        var index = spot.inventorySpotIndex;
        if (index < Plugin.VanillaSlotCount || index >= Plugin.EffectiveSlotCount)
        {
            return;
        }

        if (!WasSlotKeyPressed(index))
        {
            return;
        }

        HandleInputMethod?.Invoke(spot, Array.Empty<object>());
    }

    private static bool WasSlotKeyPressed(int index)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        if (WasPressed(keyboard, MainKeyForSlot(index)))
        {
            return true;
        }

        return Plugin.NumpadHotkeys.Value && WasPressed(keyboard, NumpadKeyForSlot(index));
    }

    private static bool WasPressed(Keyboard keyboard, Key key)
    {
        if (key == Key.None)
        {
            return false;
        }

        var control = keyboard[key];
        return control != null && control.wasPressedThisFrame;
    }

    private static Key MainKeyForSlot(int index) => index switch
    {
        3 => Key.Digit4,
        4 => Key.Digit5,
        5 => Key.Digit6,
        6 => Key.Digit7,
        7 => Key.Digit8,
        8 => Key.Digit9,
        9 => Key.Digit0,
        _ => Key.None
    };

    private static Key NumpadKeyForSlot(int index) => index switch
    {
        3 => Key.Numpad4,
        4 => Key.Numpad5,
        5 => Key.Numpad6,
        6 => Key.Numpad7,
        7 => Key.Numpad8,
        8 => Key.Numpad9,
        9 => Key.Numpad0,
        _ => Key.None
    };
}
