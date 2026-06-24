using System.Collections.Generic;

namespace RepoKastimMod.Slots;

internal static class ExtraSlotState
{
    private static readonly Dictionary<string, Dictionary<int, int>> TrackedItems = new();

    internal static void TrackInventoryUpdate(string steamId, string itemName, int spot)
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer() || spot < Plugin.VanillaSlotCount || string.IsNullOrEmpty(steamId))
        {
            return;
        }

        if (string.IsNullOrEmpty(itemName))
        {
            if (TrackedItems.TryGetValue(steamId, out var slots))
            {
                slots.Remove(spot);
                if (slots.Count == 0)
                {
                    TrackedItems.Remove(steamId);
                }
            }

            return;
        }

        if (!TrackedItems.TryGetValue(steamId, out var playerSlots))
        {
            playerSlots = new Dictionary<int, int>();
            TrackedItems[steamId] = playerSlots;
        }

        playerSlots[spot] = itemName.GetHashCode();
    }

    internal static bool TryFindItemOwnerAndSpot(int itemHash, out PlayerAvatar owner, out int spot)
    {
        owner = null;
        spot = -1;

        var players = SemiFunc.PlayerGetList();
        if (players == null)
        {
            return false;
        }

        foreach (var player in players)
        {
            var steamId = SemiFunc.PlayerGetSteamID(player);
            if (!TrackedItems.TryGetValue(steamId, out var slots))
            {
                continue;
            }

            foreach (var entry in slots)
            {
                if (entry.Value != itemHash)
                {
                    continue;
                }

                owner = player;
                spot = entry.Key;
                return true;
            }
        }

        return false;
    }

    internal static void Clear()
    {
        TrackedItems.Clear();
    }
}
