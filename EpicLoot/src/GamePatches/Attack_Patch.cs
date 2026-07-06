using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch]
    public static class Attack_Patch
    {
        public static Attack ActiveAttack = null;

        [HarmonyPatch(typeof(Attack), nameof(Attack.DoMeleeAttack))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        public static void Attack_DoMeleeAttack_Prefix(Attack __instance)
        {
            ActiveAttack = __instance;
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.DoMeleeAttack))]
        [HarmonyPostfix]
        public static void Attack_DoMeleeAttack_Postfix()
        {
            ActiveAttack = null;
        }
    }
}
