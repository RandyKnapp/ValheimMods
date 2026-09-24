﻿using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch]
    public static class Attack_Patch
    {
        // The local player's melee attack in progress. DoMeleeAttack also runs for every creature this client
        // owns, and a creature's swing lands on the local player synchronously -- so recording those made a
        // Reflect (or anything else that reads this during the victim-side handling) resolve the creature's
        // natural attack as "the local player's weapon".
        public static Attack ActiveAttack = null;

        [HarmonyPatch(typeof(Attack), nameof(Attack.DoMeleeAttack))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        public static void Attack_DoMeleeAttack_Prefix(Attack __instance, out Attack __state)
        {
            __state = ActiveAttack;
            if (__instance.m_character != null && __instance.m_character == Player.m_localPlayer)
            {
                ActiveAttack = __instance;
            }
        }

        // A finalizer, so an exception inside the attack cannot leave a stale attack behind.
        [HarmonyPatch(typeof(Attack), nameof(Attack.DoMeleeAttack))]
        [HarmonyFinalizer]
        public static void Attack_DoMeleeAttack_Finalizer(Attack __state)
        {
            ActiveAttack = __state;
        }
    }
}
