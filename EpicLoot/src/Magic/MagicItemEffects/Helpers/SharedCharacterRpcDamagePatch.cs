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
    //    ModifyResistance reduces it (previously done with Priority.High).
    //  * The postfix runs the on-damage-taken reactions (slow, adrenaline, boss retributions), for hits that
    //    landed: not for one AvoidDamageTaken cancelled, nor one vanilla discards itself.
    //  * The resource-spending mitigations (EitrShield, Coinplated) and AutoMeads are not here: they run from
    //    SharedPlayerPostArmorDamagePatch below, once the hit has been through the bubble, block and armor.
    //
    // The prefix runs before vanilla's own checks, so it first asks whether vanilla will discard the hit
    // (WillLand) -- otherwise a dodged hit, one from a despawned attacker or a PvP-off hit still spent
    // resources and triggered reactions. A self-inflicted hit (Blood Block's cost) is a price, not an attack,
    // so it triggers nothing here either.
    //
    // Each effect keeps its own guard (is-local-victim / has-effect / attacker checks) inside its handler.
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    internal static class SharedCharacterRpcDamagePatch {
        // Returns false to cancel RPC_Damage (AvoidDamageTaken rolled an avoid). __state tells the postfix
        // whether the hit landed; it is per call, so a re-entrant RPC_Damage (ReflectDamage) can't clobber it.
        [HarmonyPrefix]
        private static bool Prefix(Character __instance, HitData hit, out bool __state) {
            __state = false;
            if (hit == null) {
                return true;
            }
            // Resolved once per hit; a local (not a static) so a re-entrant RPC_Damage from a handler
            // below (e.g. ReflectDamage) can never clobber an outer invocation's attacker.
            Character attacker = hit.GetAttacker();
            if (!WillLand(__instance, hit, attacker) || IsSelfInflicted(__instance, hit, attacker)) {
                return true;
            }

            // NOTE: Opportunist and the melee stagger-duration tagger moved to the attacker-side
            // dispatcher (SharedCharacterDamagePatch): they read the ATTACKER's magic effects, which
            // are empty here whenever a remote client owns the attacker.

            // Victim-side mitigations (each self-guards on __instance == local player).
            // Convert physical -> element first, so the resistance step below reduces the converted damage.
            IncomingPhysicalConversion.ModifyIncoming(__instance, hit);

            // Avoidance is decided before anything is spent; an avoided hit cancels the whole method.
            if (!AvoidDamageTaken_Character_RPC_Damage_Patch.ShouldTakeDamage(__instance, hit, attacker)) {
                return false;
            }

            ModifyResistance.ModifyIncoming(__instance, hit);
            DamageReductionAtNight.ModifyIncoming(__instance, hit);

            // Victim-side reactions moved off Character.Damage so they fire on the victim's own client
            // regardless of who owns the attacker. Run after the avoid check so an avoided hit doesn't
            // waste a reflect. ReflectDamage runs last (it reflects the hit after these reductions).
            OffSetAttack.ReduceIncomingHit(__instance, hit);
            ReflectiveDamage_Character_Damage_Patch.OnIncomingHit(__instance, hit);

            __state = true;
            return true;
        }

        [HarmonyPostfix]
        private static void Postfix(Character __instance, HitData hit, bool __state) {
            if (hit == null || !__state) {
                return;
            }
            // On-damage-taken reactions.
            DamageTakenGivesAdrenaline.OnDamageTaken(__instance, hit);
            Bloodrage.OnDamageTaken(__instance, hit);
            // Retaliation needs someone to retaliate against: not a fall, lava, drowning or the like.
            if (hit.GetAttacker() != null) {
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
    // resistances and armor, so eitr and coins are only spent on damage that would otherwise get through --
    // they used to be charged on the raw hit, before all of that. Fire, poison and spirit are still in the hit
    // here (vanilla splits them off into damage over time next), so the shields cover those as before.
    //
    // A postfix rather than a prefix, so another mod's skipping prefix on this method can't skip these.
    // AvoidDamageTaken cancels RPC_Damage outright, so an avoided hit never gets here.
    [HarmonyPatch(typeof(Player), nameof(Player.DamageArmorDurability))]
    internal static class SharedPlayerPostArmorDamagePatch {
        [HarmonyPostfix]
        private static void Postfix(Player __instance, HitData hit) {
            if (hit == null || __instance != Player.m_localPlayer ||
                SharedCharacterRpcDamagePatch.IsSelfInflicted(__instance, hit, hit.GetAttacker())) {
                return;
            }

            // EitrShield first, so eitr is drained before the purse.
            EitrShield.ModifyIncoming(__instance, hit);
            Coinplated.ModifyIncoming(__instance, hit);
            // Last: it judges the damage that will actually land.
            AutoMeads.OnIncomingHit(__instance, hit);
        }
    }
}
