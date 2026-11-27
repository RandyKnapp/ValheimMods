using System;
using EpicLoot.MagicItemEffects;
using EpicLoot.MagicItemEffects.Shards;
using HarmonyLib;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // Every effect's Humanoid.BlockAttack handler, called in a fixed order.
    //
    //  * BlockAttack runs inside RPC_Damage on the blocker's owner. The postfix's hit holds the damage left after the
    //    block, before resistances and armor, which RPC_Damage applies next; mitigation here is therefore weaker than
    //    the same reduction after them.
    //  * Kinetic Quake uses both halves: the prefix reads whether it is armed before the block, and its postfix
    //    handler runs ahead of the __result gate so its per-block state always clears.
    //  * Shield Throw does the same: the prefix starts measuring the local player's block (its HitData.BlockDamage
    //    prefix reads the amount blocked) and the ungated postfix call stops it.
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
    internal static class SharedHumanoidBlockAttackPatch {

        [HarmonyPrefix]
        private static void PreBlockPatch(Humanoid __instance) {
            try {
                KineticQuake.BeforeBlock(__instance);
                ShieldThrow.BeforeBlock(__instance);
            } catch (Exception e) {
                DispatchGuard.Report("BlockAttack prefix", e);
            }
        }

        [HarmonyPostfix]
        private static void PostBlockPatch(Humanoid __instance, Character attacker, HitData hit, bool __result) {
            try {
                PostBlock(__instance, attacker, hit, __result);
            } catch (Exception e) {
                DispatchGuard.Report("BlockAttack postfix", e);
            }
        }

        private static void PostBlock(Humanoid __instance, Character attacker, HitData hit, bool __result) {
            // Does its own local-player gate, and must see failed blocks too.
            KineticQuake.AfterBlock(__instance, attacker, __result);
            ShieldThrow.AfterBlock();

            if (!__result) { return; }

            // BlockAttack runs on the victim's owner for EVERY humanoid -- blocking enemies included --
            // and on a dedicated server there is no local player at all. Every handler below acts on the
            // local player's own block (gain resources, spend the player's pools, stagger the player's
            // attacker), so gate once here: without this, a locally-owned enemy raising its shield would
            // pay out of / into the local player's pools, and each NPC block on a server would throw.
            if (Player.m_localPlayer == null || __instance != Player.m_localPlayer) { return; }

            IncomingPhysicalConversion.ModifyIncoming(__instance, hit, IsBlocked: true);
            GainOnBlockResource.GainOnBlock(__instance, IsBlocked: true);
            Warding.UseMoreStaminaOnBlock(hit);
            ElementalWarding.UseEitrOnBlock(hit);
            // Order-independent: unlike the three above it never touches `hit`, so it cannot
            // perturb the conversion -> warding sequence.
            LuckyBlock.OnBlock(__instance, hit, attacker);

            ChanceToStaggerOnBlock.StaggerOnBlock(attacker);
        }

        
    }
    // Flat block bonuses. The tooltip shows them inside GetBlockPower's total.
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBaseBlockPower), typeof(int))]
    internal static class BlockBASEPowerPatch {
        [HarmonyPostfix]
        private static void BlockBasePower(ItemDrop.ItemData __instance, ref float __result) 
        {
            BurdenedBlock.Apply(__instance, ref __result); 
            AnchoredBlock.Apply(__instance, ref __result);
            BloodBaseBlock.Apply(__instance, ref __result);
        }
    }
}
