using System;
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
    // Three kinds of hit skip handlers:
    //  * A hit vanilla drops triggers none. Character.Damage sends nothing when the target's view has lost its
    //    ZDO, which is what a creature that died earlier in the frame looks like: it is destroyed from its own
    //    FixedUpdate, and ZNetScene.Destroy clears the ZDO at once while its colliders linger to the end of the
    //    frame, so an Aoe or cleave resolving later in that frame still hits it. Such a hit goes nowhere, so
    //    nothing is staked, healed or procced on it, and nothing here touches the missing ZDO.
    //  * A self-inflicted hit (Blood Block's cost) triggers none: it is a price, not an attack, so it must not
    //    crit itself, pay Mercenary, lifesteal its own blood back, or Slow/Paralyze the player.
    //  * A bonus hit (HitSource.IsBonusHit -- damage EpicLoot deals on its own: Reflect, novas, the meteor,
    //    chain lightning, ...) skips the weapon-strike handlers. It keeps the ones keyed on the damage type
    //    (poison, fire, lightning) and the kill rewards.
    //
    // Each effect keeps its own guard (is-local-attacker / has-effect) inside its handler, so the order among
    // Normal-priority handlers is not load-bearing. Executioner keeps its original ordering relative to other
    // mods via the Priority.Last prefix below.
    //
    // A handler's exception is logged and stops at the patch method that ran it, as HelsHarvest's OnDeath hook
    // does. Escaping, it would abort whatever vanilla loop called Character.Damage, such as the rest of an
    // Aoe's hits for that frame.
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    internal static class SharedCharacterDamagePatch {
        private static bool IsSelfInflicted(Character target, Character attacker) {
            return attacker != null && attacker == target;
        }

        private static bool IsDroppedHit(Character target, HitData hit) {
            return hit == null || target.m_nview == null || !target.m_nview.IsValid();
        }

        private static void LogHandlerError(string stage, Character target, Exception e) {
            EpicLoot.LogErrorForce($"[SharedCharacterDamagePatch] {stage} on {target.m_name}: {e}");
        }

        [HarmonyPrefix]
        private static void EarlyPreDamagePatch(Character __instance, HitData hit) {
            if (IsDroppedHit(__instance, hit)) {
                return;
            }
            try {
                EarlyPreDamage(__instance, hit);
            } catch (Exception e) {
                LogHandlerError("prefix", __instance, e);
            }
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void LastPreDamagePatch(Character __instance, HitData hit) {
            if (IsDroppedHit(__instance, hit)) {
                return;
            }
            try {
                LastPreDamage(__instance, hit);
            } catch (Exception e) {
                LogHandlerError("late prefix", __instance, e);
            }
        }

        [HarmonyPostfix]
        private static void PostDamagePatch(Character __instance, HitData hit) {
            if (IsDroppedHit(__instance, hit)) {
                return;
            }
            try {
                PostDamage(__instance, hit);
            } catch (Exception e) {
                LogHandlerError("postfix", __instance, e);
            }
        }

        private static void EarlyPreDamage(Character __instance, HitData hit) {
            Character attacker = hit.GetAttacker();
            if (IsSelfInflicted(__instance, attacker)) {
                return;
            }
            bool strike = !HitSource.IsBonusHit;
            // Attacker side -- modify the outgoing hit before it lands.
            if (strike) {
                EitrImbueAttack.ModifyOutgoingHit(hit, attacker);
            }
            IncreaseAllPoisonDamageDone.ModifyOutgoingHit(hit, attacker);
            PoisonToTrueDamage.ModifyOutgoingHit(__instance, hit, attacker);
            if (strike) {
                Mercenary.ModifyOutgoingHit(hit, attacker);
                Wager.ModifyOutgoingHit(__instance, hit, attacker);
                ChanceDoubleDamage.ModifyOutgoingHit(hit, attacker);
                ChanceToCritOnHit.ModifyOutgoingHit(hit, attacker);
            }
            ModifySummonDamage.ModifyOutgoingHit(hit, attacker);
            if (!strike) {
                return;
            }
            // Before Opportunist: a sneak attack from hiding uses the hit's backstab bonus up, so Opportunist and
            // vanilla's own backstab cannot apply it again.
            Vanish.ModifyOutgoingHit(__instance, hit, attacker);
            // Attacker-side reads that used to live (incorrectly) on the RPC_Damage dispatcher:
            // both read the local attacker's magic effects; the stagger tagger routes its write to
            // the target's owner.
            Opportunist_Character_RPC_Damage_Patch.ModifyIncoming(__instance, hit, attacker);
            RPC_TagCharacterOnHit_Character_RPC_Damage_Patch.TagStaggerDuration(__instance, hit, attacker);
            ModifyStaggerDamage_Character_Damage_Patch.ApplyStaggerModifier(__instance, hit, attacker);
            CloseQuarter.ModifyOutgoingHit(hit, attacker);
            QuarryMark.ModifyOutgoingHit(__instance, hit, attacker);

            // NOTE: victim-side incoming-hit handlers (AutoMeads, OffSet, ReflectDamage) live on the
            // Character.RPC_Damage dispatcher instead -- Character.Damage runs on the attacker's client, so a
            // victim-side handler here never fires when a remote client owns the attacker.
        }

        private static void LastPreDamage(Character __instance, HitData hit) {
            if (!HitSource.IsBonusHit && !IsSelfInflicted(__instance, hit.GetAttacker())) {
                ExecutionerCheckDamage_Character_Damage_Patch.ModifyOutgoingHit(__instance, hit);
            }
            // Late, so a spirit's strength also scales the flat bonuses other mods' prefixes added.
            SpiritAnimal.ModifyOutgoingHit(__instance, hit, hit.GetAttacker());
            // Last, so it sees fire/lightning that any earlier prefix converted into the hit.
            ElementalHitVariant.ModifyOutgoingHit(hit, hit.GetAttacker());
            CoilingVenom.TagOutgoingHit(__instance, hit, hit.GetAttacker());
        }

        private static void PostDamage(Character __instance, HitData hit) {
            Character attacker = hit.GetAttacker();
            if (IsSelfInflicted(__instance, attacker)) {
                return;
            }
            bool strike = !HitSource.IsBonusHit;
            // Wager settles first: it consumes the stake its own prefix parked for THIS hit, and several
            // handlers below (ChainLightning, MeteorSummoner) deal further damage, which re-enters this
            // patch and would otherwise clear the stake before it could be refunded.
            if (strike) {
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
                QuarryMark.OnDamageDealt(__instance, hit, attacker);
            }
            // Keyed on the damage type or the kill, so a bonus hit earns these too.
            GainAdrenalineWhenApplyingPoison.OnDamageDealt(__instance, hit, attacker);
            BurningAdrenaline.OnDamageDealt(hit, attacker);
            StaminaOnKill.OnDamageDealt(__instance, hit, attacker);
            LifeAfterKill.OnDamageDealt(__instance, hit, attacker);
            Conduit.OnDamageDealt(__instance, hit, attacker);
            if (strike) {
                EikthyrShockingCharge.OnDamageDealt(__instance, hit, attacker);
                MeteorSummoner.OnDamageDealt(__instance, hit, attacker);
            }
            QueenEverflow.OnDamageDealt(__instance, hit, attacker);
        }
    }
}
