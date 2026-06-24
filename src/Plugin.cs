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

    internal static ConfigEntry<float> SprintDrainMultiplier { get; private set; }

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
            "If true, the host blocks clients from using slots above the host's configured count.");

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
            "Inventory",
            "Extra Slot Hotkeys",
            true,
            "If true, number keys 4-9 and 0 control extra inventory slots.");

        NumpadHotkeys = Config.Bind(
            "Inventory",
            "Numpad Hotkeys",
            true,
            "If true, numpad keys also control matching extra inventory slots.");

        SprintDrainMultiplier = Config.Bind(
            "Stamina",
            "Sprint Drain Multiplier",
            0.65f,
            new ConfigDescription(
                "Multiplier for sprint stamina drain. 1.0 = vanilla, 0.65 = 35% less drain.",
                new AcceptableValueRange<float>(0.1f, 2f)));

        _harmony = new Harmony(PluginInfo.Guid);
        _harmony.PatchAll();

        Log.LogInfo($"{PluginInfo.Name} v{PluginInfo.Version} loaded.");
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
    internal const string Version = "1.0.0";
}
