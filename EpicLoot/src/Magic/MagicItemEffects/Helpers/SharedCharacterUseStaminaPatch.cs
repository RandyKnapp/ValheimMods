using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EpicLoot.MagicItemEffects.Shards;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // Single consolidated patch for the player's stamina economy, implementing "virtual stamina": sources
    // other than the stamina pool (adrenaline, health) can stand in for it.
    //
    // Vanilla splits this across two steps and both have to be handled or the effects are dead:
    //
    //  * Player.HaveStamina is the GATE. Vanilla asks it before nearly every stamina-gated action -- attack
    //    (Attack.Start), jump (Character.Jump), bow draw, dodge, block, build, moving while encumbered -- and
    //    refuses the action on false, long before UseStamina is ever reached. The postfix widens it to count
    //    virtual sources. It REPORTS ONLY; it must never spend.
    //  * Player.UseStamina is the PAYMENT. The prefix is the only place virtual sources are actually charged.
    //  * PlayerController.FixedUpdate is a third, separate gate for sprinting: it reads Player.GetStamina()
    //    directly rather than calling HaveStamina, so the postfix cannot reach it -- hence the transpiler.
    //
    // Order among the effects IS load-bearing here, unlike SharedCharacterDamagePatch: each draws from a
    // different pool, so whoever runs first is spent first. Adrenaline before health -- you shouldn't burn a
    // 30s health charge while you still have adrenaline to spend.
    [HarmonyPatch]
    internal static class SharedCharacterUseStaminaPatch {

        // How much stamina the virtual sources could stand in for right now. PURE READ -- never spends.
        //
        // Every source is gated on the player actually carrying the effect before it is consulted, so a
        // player without the shard falls straight through without touching the adrenaline pool or SEMan.
        // Use the EquipmentEffectCache-backed HasActiveMagicEffect overload (the parameterless one does a
        // full LINQ walk and is NOT cached -- it must not be used here): this runs on every failed
        // affordability check and three times per PlayerController.FixedUpdate.
        private static float GetCoverable(Player player) {
            float coverable = 0f;

            if (player.HasActiveMagicEffect(MagicEffectType.UseAdrenalineAsStamina, out float efficiency, 0.01f)) {
                coverable += UseAdrenalineAsStamina.GetCoverable(player, efficiency);
            }

            if (player.HasActiveMagicEffect(MagicEffectType.RunningOnEmpty, out float fraction, 0.01f)) {
                coverable += RunningOnEmpty.GetCoverable(player, fraction);
            }

            return coverable;
        }

        // Widens vanilla's affordability check to count virtual sources.
        //
        // Two invariants, both load-bearing: this must never SPEND (the prefix below owns payment; spending
        // here too would let one point of adrenaline pass a gate and then pay for it a second time), and it
        // must never call HaveStamina on any Player -- that re-enters this postfix forever. Everything it
        // touches is a plain field read or an equipment-effect lookup, none of which reach stamina.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.HaveStamina))]
        [UsedImplicitly]
        private static void PostHaveStaminaPatch(Player __instance, float amount, ref bool __result) {
            if (__result || __instance != Player.m_localPlayer) {
                return;
            }

            // Mirror vanilla's strict `>` (Player.HaveStamina) so relaxed gates behave identically at the
            // boundary to the ones we don't touch.
            __result = __instance.GetStamina() + GetCoverable(__instance) > amount;
        }

        // Charges the shortfall to the virtual sources. Priority.Last so cost-MODIFYING prefixes
        // (ReduceFishingStaminaCost) shrink `v` before we decide how much of it needs covering; covering
        // first and discounting after would over-spend the pools.
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
        [UsedImplicitly]
        private static void PreUseStaminaPatch(Player __instance, ref float v) {
            if (v <= 0f || float.IsNaN(v) || __instance != Player.m_localPlayer) {
                return;
            }

            // Vanilla multiplies by Game.m_staminaRate AFTER this prefix, so the pool is charged v*rate, not
            // v. Compare and pay in charged units, or the gate and the payment disagree whenever a server
            // moves the StaminaRate global key off 1.
            float rate = Game.m_staminaRate;
            if (rate <= 0f) {
                return;
            }

            float shortfall = v * rate - __instance.GetStamina();
            if (shortfall <= 0f) {
                return; // enough real stamina; nothing to cover
            }

            // Adrenaline discounts the cost directly. Gated on the effect the same way GetCoverable is, so
            // the gate and the payment can never disagree about which sources are in play.
            if (__instance.HasActiveMagicEffect(MagicEffectType.UseAdrenalineAsStamina, out float efficiency, 0.01f)) {
                float covered = UseAdrenalineAsStamina.Pay(__instance, shortfall, efficiency);
                if (covered > 0f) {
                    v -= covered / rate;
                    shortfall -= covered;
                }
            }

            // Health tops up the stamina POOL rather than discounting the cost, so the unused remainder of
            // the buffer stays available for the next action -- vanilla's own subtraction then draws from the
            // refilled pool. Nothing to report back, hence no return value.
            if (shortfall > 0f
                && __instance.HasActiveMagicEffect(MagicEffectType.RunningOnEmpty, out float fraction, 0.01f)) {
                RunningOnEmpty.TryRefill(__instance, fraction);
            }
        }

        // PlayerController.FixedUpdate compares this against 0 to decide whether sprinting is allowed. Report
        // stamina + coverable so this agrees exactly with the HaveStamina(0f) gate in Player.CheckRun -- if
        // the two disagree, the run flickers on and off frame to frame.
        private static float RunInputStamina(Player player) {
            float stamina = player.GetStamina();
            return stamina > 0f ? stamina : GetCoverable(player);
        }

        private static readonly MethodInfo GetStaminaMethod =
            AccessTools.DeclaredMethod(typeof(Player), nameof(Player.GetStamina));
        private static readonly MethodInfo RunInputStaminaMethod =
            AccessTools.DeclaredMethod(typeof(SharedCharacterUseStaminaPatch), nameof(RunInputStamina));

        // The run-input latch reads m_character.GetStamina() directly (three times) to decide m_run, then
        // feeds m_run to SetControls WITHIN THE SAME METHOD -- so a postfix runs too late and a prefix too
        // early. Rewrite those three reads to report virtual stamina instead.
        //
        // Matched on the method operand rather than on surrounding instruction shape, so a vanilla reshuffle
        // of that block still patches cleanly. FixedUpdate is a private Unity message, so it's targeted by
        // string (same as ReduceFishingStaminaCost's FishingFloat.FixedUpdate patch).
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerController), "FixedUpdate")]
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> RunInputTranspiler(IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);

            if (GetStaminaMethod == null || RunInputStaminaMethod == null) {
                EpicLoot.LogErrorForce("Unable to resolve Player.GetStamina or RunInputStamina. Sprinting on " +
                    "adrenaline or a health buffer WILL NOT WORK.");
                return code;
            }

            int replaced = 0;
            for (int i = 0; i < code.Count; i++) {
                if ((code[i].opcode != OpCodes.Callvirt && code[i].opcode != OpCodes.Call)
                    || !code[i].OperandIs(GetStaminaMethod)) {
                    continue;
                }

                // The Player instance is already on the stack (ldarg.0; ldfld m_character), so a static
                // taking a Player is a drop-in for the instance call. Mutating in place keeps any labels and
                // exception blocks attached to this instruction.
                code[i].opcode = OpCodes.Call;
                code[i].operand = RunInputStaminaMethod;
                replaced++;
            }

            if (replaced == 0) {
                EpicLoot.LogErrorForce("Mod conflict or game update detected! Unable to patch any " +
                    "Player.GetStamina call site in PlayerController.FixedUpdate. Sprinting on adrenaline or " +
                    "a health buffer WILL NOT WORK.");
            } else if (replaced != 3) {
                EpicLoot.LogWarningForce($"Expected 3 Player.GetStamina call sites in " +
                    $"PlayerController.FixedUpdate, patched {replaced}. Sprint behaviour at zero stamina may " +
                    "be inconsistent.");
            }

            return code;
        }
    }
}
