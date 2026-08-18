using EpicLoot.MagicItemEffects.Shards;
using HarmonyLib;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // Single consolidated Harmony patch for Humanoid.BlockAttack. It replaces the ~7 and counting individual [HarmonyPatch]
    // classes that each effect used to declare on this same method, calling every effect's handler in a
    // fixed, explicit order.
    //
    //  * Prefix handler can be added when we need to get the raw damage numbers for shenanigans. This will be fun but I'll
    //    never be able to convince the committee balnce wise so I'm leaving it out for now. Can just use Character.Damage to get
    //    that number
    //  * Postfix handler runs after the block happens: blocks, hit data, blocker, attacker, parry flag, and block flag
    //    Block is routed through RPC_Damage and is done locally 
    //  * Hit in Postfix handler values are post block mitigated numbers before resistance and armor. This hit is then
    //    routed into RPC_Damage where those reductions are used. Typically means this is a weaker mitigation than if
    //    numbers were mitigated post RPC_Damage.
    //

    // Harmony patch for all other actual block interactions.
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
    internal static class SharedHumanoidBlockAttackPatch {

        [HarmonyPrefix]
        private static void PreBlockPatch(Humanoid __instance, Character attacker, HitData hit) {

        }

        [HarmonyPostfix]
        private static void PostBlockPatch(Humanoid __instance, Character attacker, HitData hit, bool __result) {
            if (!__result) { return; }

            // BlockAttack runs on the victim's owner for EVERY humanoid -- blocking enemies included --
            // and on a dedicated server there is no local player at all. Every handler below acts on the
            // local player's own block (gain resources, spend the player's pools, stagger the player's
            // attacker), so gate once here: without this, a locally-owned enemy raising its shield would
            // pay out of / into the local player's pools, and each NPC block on a server would throw.
            if (Player.m_localPlayer == null || __instance != Player.m_localPlayer) { return; }

            IncomingPhysicalConversion.ModifyIncoming(__instance, hit, IsBlocked: true); // orange, dark blue, light blue, dark green
            GainOnBlockResource.GainOnBlock(__instance, IsBlocked: true); // red / yellow / cyan
            Warding.UseMoreStaminaOnBlock(__instance, hit);
            ElementalWarding.UseEitrOnBlock(__instance, hit);
            // Golden. Order-independent: unlike the three above it never touches `hit`, so it cannot
            // perturb the conversion -> warding sequence.
            LuckyBlock.OnBlock(__instance, hit, attacker);

            ChanceToStaggerOnBlock.StaggerOnBlock(attacker);
        }

        
    }
    // Route flat bonuses here. Reflects in tool tip but is bundled with GetBlockPower displaying total after all bonuses applied
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

    // Harmony patch to modify total block power and display on tooltip. This is a more modifier that multiplies the total after
    // all bonuses like skill factor and magic effect increases
    // If block is enhanced or modified at time of block use BlockAttack Harmony patch
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBlockPower), typeof(int), typeof(float))]

    internal static class BlockPowerMultiplerPatch{
        [HarmonyPostfix]
        private static void BlockPowerPatch(ref float __result) {

        }
    }
}
