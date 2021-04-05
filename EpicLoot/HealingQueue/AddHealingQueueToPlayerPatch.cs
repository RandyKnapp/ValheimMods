using System;
using EpicLoot.HealingQueue;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    public class AddHealingQueueToPlayerPatch
    {
        [HarmonyPatch(typeof(Player), nameof(Humanoid.Start))]
        public static class Player_Start_Patch
        {
            private static void Postfix(Player __instance)
            {
                var queue = __instance.gameObject.AddComponent<HealingQueueMono>();
                queue.player = __instance;
            }
        }
    }
}