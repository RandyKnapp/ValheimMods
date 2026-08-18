using EpicLoot.Magic.MagicItemEffects;
using EpicLoot.MagicItemEffects;
using EpicLoot.MagicItemEffects.Shards;
using HarmonyLib;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // Single consolidated Harmony patch for Character.RPC_Damage. It replaces the ~12 individual
    // [HarmonyPatch] classes that each effect used to declare on this same method.
    //
    //  * The prefix runs the incoming-hit modifiers in a fixed order and can cancel the hit entirely
    //    (AvoidDamageTaken). The one load-bearing ordering constraint from the old code is preserved by
    //    call order: IncomingPhysicalConversion moves physical damage onto an element BEFORE
    //    ModifyResistance reduces it (previously done with Priority.High). Avoidance is checked before the
    //    resource-spending mitigations (EitrShield, Coinplated) so a fully-avoided hit never spends eitr or
    //    coins. EitrShield runs first of those two, so eitr is drained before the purse.
    //  * The postfix runs the on-damage-taken reactions (slow, adrenaline, boss retributions). These run
    //    even when the prefix cancels the hit -- matching the old behavior, where these were independent
    //    postfixes that Harmony always executes.
    //
    // Each effect keeps its own guard (is-local-victim / has-effect / attacker checks) inside its handler.
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    internal static class SharedCharacterRpcDamagePatch {
        // Returns false to cancel RPC_Damage (AvoidDamageTaken rolled an avoid).
        [HarmonyPrefix]
        private static bool Prefix(Character __instance, HitData hit) {
            if (hit == null) {
                return true;
            }
            // Resolved once per hit; a local (not a static) so a re-entrant RPC_Damage from a handler
            // below (e.g. ReflectDamage) can never clobber an outer invocation's attacker.
            Character attacker = hit.GetAttacker();

            // Attacker-side / universal: these tag or bonus the hit regardless of who the victim is.
            Opportunist_Character_RPC_Damage_Patch.ModifyIncoming(__instance, hit, attacker);
            RPC_TagCharacterOnHit_Character_RPC_Damage_Patch.TagStaggerDuration(__instance, hit, attacker);

            // Victim-side mitigations (each self-guards on __instance == local player).
            // Convert physical -> element first, so the resistance step below reduces the converted damage.
            IncomingPhysicalConversion.ModifyIncoming(__instance, hit);

            // Avoidance is decided before eitr is spent; an avoided hit cancels the whole method.
            if (!AvoidDamageTaken_Character_RPC_Damage_Patch.ShouldTakeDamage(__instance, hit, attacker)) {
                return false;
            }

            ModifyResistance.ModifyIncoming(__instance, hit);
            EitrShield.ModifyIncoming(__instance, hit);
            Coinplated.ModifyIncoming(__instance, hit);
            DamageReductionAtNight.ModifyIncoming(__instance, hit);

            // Victim-side reactions moved off Character.Damage so they fire on the victim's own client
            // regardless of who owns the attacker. Run after the avoid check so an avoided hit doesn't
            // waste a mead or reflect. ReflectDamage runs last (it reflects the fully-mitigated hit).
            OffSetAttack.ReduceIncomingHit(__instance, hit);
            AutoMeads.OnIncomingHit(__instance, hit);
            ReflectiveDamage_Character_Damage_Patch.OnIncomingHit(__instance, hit);

            return true;
        }

        [HarmonyPostfix]
        private static void Postfix(Character __instance, HitData hit) {
            if (hit == null) {
                return;
            }
            // On-damage-taken reactions.
            DamageTakenGivesAdrenaline.OnDamageTaken(__instance, hit);
            Bloodrage.OnDamageTaken(__instance, hit);
            ElderForestsAid.OnDamageTaken(__instance, hit);
            ModerIcyRetribution.OnDamageTaken(__instance, hit);
        }
    }
}
