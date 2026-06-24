using System;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace RepoKastimMod.Slots;

[HarmonyPatch(typeof(PunManager), "SetItemNameLOGIC")]
internal static class PunManagerSetItemNameLogicPatch
{
    private static void Postfix(string _name, int photonViewID, ItemAttributes _itemAttributes)
    {
        if (Plugin.KeepItemsInTruck.Value || string.IsNullOrEmpty(_name))
        {
            return;
        }

        try
        {
            var item = ResolveItemEquippable(photonViewID, _itemAttributes);
            if (item == null || item.IsEquipped())
            {
                return;
            }

            if (!ExtraSlotState.TryFindItemOwnerAndSpot(_name.GetHashCode(), out var owner, out var spot))
            {
                return;
            }

            if (spot < Plugin.VanillaSlotCount || spot >= Plugin.EffectiveSlotCount)
            {
                return;
            }

            var ownerViewId = -1;
            if (SemiFunc.IsMultiplayer())
            {
                if (owner.photonView == null)
                {
                    return;
                }

                ownerViewId = owner.photonView.ViewID;
            }

            item.RequestEquip(spot, ownerViewId);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Failed to restore extra-slot item '{_name}': {ex.Message}");
        }
    }

    private static ItemEquippable ResolveItemEquippable(int photonViewID, ItemAttributes itemAttributes)
    {
        var attributes = itemAttributes;
        if (SemiFunc.IsMultiplayer())
        {
            var view = PhotonView.Find(photonViewID);
            if (view == null)
            {
                return null;
            }

            attributes = view.GetComponent<ItemAttributes>();
        }

        return attributes == null ? null : attributes.GetComponent<ItemEquippable>();
    }
}
