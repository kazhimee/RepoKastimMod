using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RepoKastimMod;

[BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
public sealed class Plugin : BaseUnityPlugin
{
    internal const int VanillaSlotCount = 3;
    internal const int MaxSlotCount = 10;

    private Harmony _harmony;

    internal static Plugin Instance { get; private set; }
    internal static ManualLogSource Log { get; private set; }

    internal static ConfigEntry<int> SlotCount { get; private set; }
    internal static ConfigEntry<bool> HostProtection { get; private set; }
    internal static ConfigEntry<bool> KeepItemsInTruck { get; private set; }
    internal static ConfigEntry<bool> AutoSwapItems { get; private set; }
    internal static ConfigEntry<bool> ExtraHotkeys { get; private set; }
    internal static ConfigEntry<bool> NumpadHotkeys { get; private set; }
    internal static ConfigEntry<InventoryAlignment> InventoryAlignment { get; private set; }
    internal static ConfigEntry<float> InventoryAlignmentOffset { get; private set; }

    internal static int EffectiveSlotCount =>
        Mathf.Clamp(SlotCount?.Value ?? VanillaSlotCount, VanillaSlotCount, MaxSlotCount);

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        SlotCount = Config.Bind(
            "Inventory",
            "Number Of Slots",
            5,
            new ConfigDescription(
                "Total inventory slots (3-10). Changes after a scene or round reload.",
                new AcceptableValueRange<int>(3, 10)));

        HostProtection = Config.Bind(
            "Inventory",
            "Host Protection",
            true,
            "If true, the host blocks clients from equipping into slots above the host's configured count.");

        KeepItemsInTruck = Config.Bind(
            "Inventory",
            "Keep Items In Truck",
            false,
            "If true, extra-slot items stay in the truck when ownership is rebuilt between rounds.");

        AutoSwapItems = Config.Bind(
            "Inventory",
            "Auto Swap Items",
            true,
            "If true, equipping into an occupied slot swaps with the item already there.");

        ExtraHotkeys = Config.Bind(
            "Controls",
            "Extra Slot Hotkeys",
            true,
            "If true, number keys 4-9 and 0 control extra inventory slots.");

        NumpadHotkeys = Config.Bind(
            "Controls",
            "Numpad Hotkeys",
            true,
            "If true, numpad keys also control matching extra inventory slots.");

        InventoryAlignment = Config.Bind(
            "Inventory",
            "Alignment",
            global::RepoKastimMod.InventoryAlignment.Center,
            "Where the inventory slot row sits on screen: Left, Center or Right.");

        InventoryAlignmentOffset = Config.Bind(
            "Inventory",
            "Alignment Offset",
            350f,
            new ConfigDescription(
                "How far Left/Right alignment shifts the slots, in UI units. Ignored when alignment is Center.",
                new AcceptableValueRange<float>(0f, 800f)));

        InventoryAlignment.SettingChanged += (_, _) => Slots.InventoryUiBuilder.ReapplyAlignment();
        InventoryAlignmentOffset.SettingChanged += (_, _) => Slots.InventoryUiBuilder.ReapplyAlignment();

        _harmony = new Harmony(PluginInfo.Guid);
        _harmony.PatchAll();

        Log.LogInfo($"{PluginInfo.Name} v{PluginInfo.Version} loaded.");
    }

    internal void ScheduleInventoryRebuild()
    {
        StartCoroutine(RebuildInventoryWhenReady());
    }

    private IEnumerator RebuildInventoryWhenReady()
    {
        for (var i = 0; i < 120; i++)
        {
            if (InventoryUI.instance != null && Inventory.instance != null)
            {
                Slots.InventoryUiBuilder.Rebuild(InventoryUI.instance);
                yield break;
            }

            yield return new WaitForSeconds(0.25f);
        }

        Log.LogWarning("Timed out waiting to rebuild inventory slots.");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}

internal static class PluginInfo
{
    internal const string Guid = "kazhime.repokastimmod";
    internal const string Name = "Repo Kastim Mod";
    internal const string Version = "1.4.2";
}

public enum InventoryAlignment
{
    Left,
    Center,
    Right
}
