using System;

namespace RepoKastimMod.Slots;

internal static class InventorySlotList
{
    internal static void EnsureSlots(Inventory inventory, int minimumSlotCount = -1)
    {
        if (inventory == null)
        {
            return;
        }

        var slots = inventory.GetAllSpots();
        var targetCount = Math.Max(Plugin.EffectiveSlotCount, minimumSlotCount);

        while (slots.Count < targetCount)
        {
            slots.Add(null);
        }
    }
}
