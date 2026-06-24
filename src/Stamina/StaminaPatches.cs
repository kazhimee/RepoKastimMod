using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace RepoKastimMod.Stamina;

[HarmonyPatch(typeof(PlayerController), "Start")]
internal static class PlayerControllerStartPatch
{
    private static void Postfix(PlayerController __instance)
    {
        if (__instance != null)
        {
            __instance.StartCoroutine(ApplyStaminaTuning(__instance));
        }
    }

    private static IEnumerator ApplyStaminaTuning(PlayerController player)
    {
        yield return new WaitForSeconds(0.5f);

        if (player == null)
        {
            yield break;
        }

        var sprintMultiplier = Plugin.SprintDrainMultiplier.Value;
        if (sprintMultiplier > 0f && Mathf.Abs(sprintMultiplier - 1f) > 0.001f)
        {
            player.EnergySprintDrain *= sprintMultiplier;
        }
    }
}
