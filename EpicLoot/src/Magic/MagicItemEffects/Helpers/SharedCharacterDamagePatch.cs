using EpicLoot.MagicItemEffects;
using EpicLoot.MagicItemEffects.Shards;
using HarmonyLib;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // Single consolidated Harmony patch for Character.Damage. It replaces the ~21 individual [HarmonyPatch]
    // classes that each effect used to declare on this same method, calling every effect's handler in a
    // fixed, explicit order.
    //
    //  * Prefix handlers run before the damage is applied: attacker-side handlers modify the OUTGOING hit
    //    (crits, conversions, imbues). This method runs on the ATTACKER's client, so victim-side (incoming
    //    damage) handlers live on the Character.RPC_Damage dispatcher instead -- that method runs on the
    //    victim's own client, which is the only place they fire regardless of who owns the attacker.
    //  * Postfix handlers run after the damage is applied: on-hit reactions (heals, procs, adrenaline). A few
    //    apply an effect to the TARGET (Slow, Paralyze); those check the local player's effect here (attacker
    //    side) but apply it via an RPC to the target's owner, where movement/AI is authoritative.
    //
    // Each effect keeps its own guard (is-local-attacker / has-effect) inside its handler, so the order among
    // Normal-priority handlers is not load-bearing. Executioner keeps its original ordering relative to other
    // mods via the Priority.Last prefix below.
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    internal static class SharedCharacterDamagePatch {
        [HarmonyPrefix]
        private static void EarlyPreDamagePatch(Character __instance, HitData hit) {
            if (hit == null) {
                return;
            }
            Character attacker = hit.GetAttacker();
            // Attacker side -- modify the outgoing hit before it lands.
            EitrImbueAttack.ModifyOutgoingHit(hit, attacker);
            IncreaseAllPoisonDamageDone.ModifyOutgoingHit(hit, attacker);
            PoisonToTrueDamage.ModifyOutgoingHit(__instance, hit, attacker);
            Mercenary.ModifyOutgoingHit(hit, attacker);
            Wager.ModifyOutgoingHit(__instance, hit, attacker);
            ChanceDoubleDamage.ModifyOutgoingHit(hit, attacker);
            ChanceToCritOnHit.ModifyOutgoingHit(hit, attacker);
            ModifyStaggerDamage_Character_Damage_Patch.ApplyStaggerModifier(__instance, hit, attacker);

            // NOTE: victim-side incoming-hit handlers (AutoMeads, OffSet, ReflectDamage) live on the
            // Character.RPC_Damage dispatcher instead -- Character.Damage runs on the attacker's client, so a
            // victim-side handler here never fires when a remote client owns the attacker.
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void LastPreDamagePatch(Character __instance, HitData hit) {
            if (hit == null) {
                return;
            }
            ExecutionerCheckDamage_Character_Damage_Patch.ModifyOutgoingHit(__instance, hit);
        }

        [HarmonyPostfix]
        private static void PostDamagePatch(Character __instance, HitData hit) {
            if (hit == null) {
                return;
            }
            Character attacker = hit.GetAttacker();
            // Wager settles first: it consumes the stake its own prefix parked for THIS hit, and several
            // handlers below (ChainLightning, MeteorSummoner) deal further damage, which re-enters this
            // patch and would otherwise clear the stake before it could be refunded.
            Wager.OnDamageDealt(__instance, hit);

            // On-hit reactions (attacker side unless the handler guards otherwise).
            AddLifeSteal.CheckAndDoLifeSteal(hit, attacker);
            BloodDrinker.OnDamageDealt(hit, attacker);
            AddEitrLeech.OnDamageDealt(hit, attacker);
            ChainLightning.OnDamageDealt(__instance, hit, attacker);
            StrikeCausesLightning.OnDamageDealt(__instance, hit, attacker);
            ApplySlow_Character_Damage_Patch.OnDamageDealt(__instance, hit, attacker);
            Paralyze.OnDamaged(__instance, hit, attacker);
            StaggerOnDamageTaken_Character_Damage_Patch.OnDamageDealt(__instance, hit, attacker);
            HealthGainPerXDamageDone.OnDamageDealt(hit, attacker);
            LifeGainOnHit.OnDamageDealt(hit, attacker);
            GainAdrenalineWhenApplyingPoison.OnDamageDealt(__instance, hit, attacker);
            BurningAdrenaline.OnDamageDealt(hit, attacker);
            StaminaOnKill.OnDamageDealt(__instance, hit, attacker);
            Conduit.OnDamageDealt(__instance, hit, attacker);
            EikthyrShockingCharge.OnDamageDealt(__instance, hit, attacker);
            MeteorSummoner.OnDamageDealt(__instance, hit, attacker);
            QueenEverflow.OnDamageDealt(__instance, hit, attacker);
        }
    }
}
