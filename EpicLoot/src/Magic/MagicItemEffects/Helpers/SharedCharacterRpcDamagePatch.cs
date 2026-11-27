using System;
using EpicLoot.Magic.MagicItemEffects;
using EpicLoot.MagicItemEffects;
using EpicLoot.MagicItemEffects.Shards;
using HarmonyLib;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // Every effect's Character.RPC_Damage handler, on the victim's owner.
    //
    //  * The prefix runs the incoming-hit modifiers in a fixed order and can cancel the hit entirely
    //    (AvoidDamageTaken). IncomingPhysicalConversion must move physical damage onto an element BEFORE
    //    ModifyResistance reduces it.
    //  * The postfix runs the on-damage-taken reactions (slow, adrenaline, boss retributions), for hits that
    //    landed: not for one AvoidDamageTaken cancelled, nor one vanilla discards itself.
    //  * The resource-spending mitigations (EitrShield, Coinplated) and AutoMeads are not here: they run from
    //    SharedPlayerPostArmorDamagePatch below, once the hit has been through the bubble, block and armor. The Eitr
    //    Barrier set bonus is not here either: like vanilla's bubble, its status effect takes its share of the raw hit.
    //
    // The prefix runs before vanilla's own checks, so it first asks whether vanilla will discard the hit
    // (WillLand) -- otherwise a dodged hit, one from a despawned attacker or a PvP-off hit still spent
    // resources and triggered reactions. A self-inflicted hit (Blood Block's cost) is a price, not an attack,
    // so it triggers nothing here either.
    //
    // Each effect keeps its own guard (is-local-victim / has-effect / attacker checks) inside its handler.
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    internal static class SharedCharacterRpcDamagePatch {
        // Whether the hit landed, and its attacker, resolved once for both halves. Per call, so a re-entrant
        // RPC_Damage from a handler (ReflectDamage) cannot clobber an outer one's.
        private struct HitState {
            public bool Landed;
            public Character Attacker;
        }

        // Returns false to cancel RPC_Damage (AvoidDamageTaken rolled an avoid).
        [HarmonyPrefix]
        private static bool Prefix(Character __instance, HitData hit, out HitState __state) {
            __state = default;
            try {
                return PrefixBody(__instance, hit, ref __state);
            } catch (Exception e) {
                DispatchGuard.Report("RPC_Damage prefix", e);
                __state.Landed = false;
                return true;
            }
        }

        private static bool PrefixBody(Character __instance, HitData hit, ref HitState __state) {
            if (hit == null) {
                return true;
            }
            Character attacker = hit.GetAttacker();
            __state.Attacker = attacker;
            if (!WillLand(__instance, hit, attacker) || IsSelfInflicted(__instance, hit, attacker)) {
                return true;
            }

            // A spirit animal's effective health, and the combat signal for the local player being hit.
            SpiritAnimal.ModifyIncoming(__instance, hit, attacker);

            // Victim-side mitigations (each self-guards on __instance == local player).
            // Convert physical -> element first, so the resistance step below reduces the converted damage.
            IncomingPhysicalConversion.ModifyIncoming(__instance, hit);

            // Avoidance is decided before anything is spent; an avoided hit cancels the whole method.
            if (!AvoidDamageTaken_Character_RPC_Damage_Patch.ShouldTakeDamage(__instance, hit, attacker)) {
                return false;
            }

            ModifyResistance.ModifyIncoming(__instance, hit);
            DamageReductionAtNight.ModifyIncoming(__instance, hit);

            // After the avoid check, so an avoided hit doesn't waste a reflect; ReflectDamage reflects the hit
            // after the reductions above.
            OffSetAttack.ReduceIncomingHit(__instance, hit);
            ReflectiveDamage_Character_Damage_Patch.OnIncomingHit(__instance, hit);
            CoilingVenom.OnIncomingHit(__instance, hit, attacker);

            // Reads the ATTACKER's mirrored value and siphons the target here, on its owner, before vanilla's
            // ApplyDamage, so the hit that siphons also heals.
            LifeSiphon.OnHitLanding(__instance, hit, attacker);

            __state.Landed = true;
            return true;
        }

        [HarmonyPostfix]
        private static void Postfix(Character __instance, HitData hit, HitState __state) {
            try {
                PostfixBody(__instance, hit, __state);
            } catch (Exception e) {
                DispatchGuard.Report("RPC_Damage postfix", e);
            }
        }

        private static void PostfixBody(Character __instance, HitData hit, HitState __state) {
            if (hit == null || !__state.Landed) {
                return;
            }
            DamageTakenGivesAdrenaline.OnDamageTaken(__instance, hit);
            Bloodrage.OnDamageTaken(__instance, hit);
            CoilingVenom.OnDamageTaken(__instance);
            // Retaliation needs someone to retaliate against: not a fall, lava, drowning or the like.
            if (__state.Attacker != null) {
                ElderForestsAid.OnDamageTaken(__instance, hit);
                ModerIcyRetribution.OnDamageTaken(__instance, hit);
            }
        }

        // Mirrors the early returns at the top of vanilla Character.RPC_Damage: debug flying, not the owner,
        // dead/teleporting/in a cutscene, dodge invincibility, an attacker that no longer exists, and a player
        // hit by a player with PvP off. Keep in step with vanilla.
        private static bool WillLand(Character target, HitData hit, Character attacker) {
            if (target.IsDebugFlying() || target.m_nview == null || !target.m_nview.IsOwner()) {
                return false;
            }
            if (target.GetHealth() <= 0f || target.IsDead() || target.IsTeleporting() || target.InCutscene() ||
                (hit.m_dodgeable && target.IsDodgeInvincible())) {
                return false;
            }
            if (hit.HaveAttacker() && attacker == null) {
                return false;
            }
            return !(target.IsPlayer() && !target.IsPVPEnabled() && attacker != null && attacker.IsPlayer() &&
                !hit.m_ignorePVP);
        }

        internal static bool IsSelfInflicted(Character target, HitData hit, Character attacker) {
            return hit.m_hitType == HitData.HitType.Self || (attacker != null && attacker == target);
        }
    }

    // The resource-spending mitigations, run where the hit is final: Player.DamageArmorDurability is called
    // from exactly one place, in Character.RPC_Damage right after ApplyArmor, and only for players. By then
    // vanilla has applied its discard checks, difficulty scaling, the Staff of Protection bubble, the block,
    // resistances and armor, so eitr and coins are only spent on damage that would otherwise get through. Fire,
    // poison and spirit are still in the hit here (vanilla splits them off into damage over time next), so the
    // shields cover those too.
    //
    // A postfix rather than a prefix, so another mod's skipping prefix on this method can't skip these.
    // AvoidDamageTaken cancels RPC_Damage outright, so an avoided hit never gets here.
    [HarmonyPatch(typeof(Player), nameof(Player.DamageArmorDurability))]
    internal static class SharedPlayerPostArmorDamagePatch {
        [HarmonyPostfix]
        private static void Postfix(Player __instance, HitData hit) {
            try {
                PostfixBody(__instance, hit);
            } catch (Exception e) {
                DispatchGuard.Report("DamageArmorDurability postfix", e);
            }
        }

        private static void PostfixBody(Player __instance, HitData hit) {
            if (hit == null || __instance != Player.m_localPlayer ||
                SharedCharacterRpcDamagePatch.IsSelfInflicted(__instance, hit, hit.GetAttacker())) {
                return;
            }

            // EitrShield covers its share before Coinplated, so eitr is drained before the purse. (The Eitr Barrier set
            // bonus already took its share of the raw hit, from SE_EitrBarrier.OnDamaged.)
            EitrShield.ModifyIncoming(__instance, hit);
            Coinplated.ModifyIncoming(__instance, hit);
            // Last: it judges the damage that will actually land.
            AutoMeads.OnIncomingHit(__instance, hit);
        }
    }
}
