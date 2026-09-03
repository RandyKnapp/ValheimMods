using EpicLoot.MagicItemEffects;
using EpicLoot.MagicItemEffects.Shards;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // Single consolidated Harmony patch for SEMan.ModifyMaxCarryWeight, replacing the five individual
    // [HarmonyPatch] classes each carry-weight effect used to declare on this same method.
    //
    // This is one of the hottest vanilla methods the mod touches. Player.FixedUpdate -> UpdateStats ->
    // IsEncumbered -> GetMaxCarryWeight reaches it every fixed tick (50Hz), the inventory weight bar
    // reaches it every frame the inventory is open, and PenaltyScaling.WeightFactor reaches it again
    // from the per-tick stamina regen path. So the guards are ordered cheapest-first and every handler
    // reads its memoized effect value before doing any work that is not a field compare.
    //
    // Order is load-bearing at one point: TravelLight runs last. It clamps the running total against a
    // floor rather than adding to it, so it has to see the bonuses the other four contributed. As five
    // separate postfixes that ordering was whatever Harmony happened to pick.
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyMaxCarryWeight))]
    internal static class SharedSEManModifyMaxCarryWeightPatch {

        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(SEMan __instance, float baseLimit, ref float limit) {
            if (!(__instance.m_character is Player player)) {
                return;
            }

            AddCarryWeight.ModifyMaxCarryWeight(player, ref limit);

            // The shard effects below read the local player's own equipment, which is not replicated for
            // remote players, so they are gated to the local player exactly as their own patches were.
            if (player != Player.m_localPlayer) {
                return;
            }

            NightCarryWeight.ModifyMaxCarryWeight(player, baseLimit, ref limit);
            GainMaxCarryWeightFromRested.ModifyMaxCarryWeight(player, __instance, ref limit);
            CarryWeightForMovementPenalty.ModifyMaxCarryWeight(player, baseLimit, ref limit);

            // Last -- see the note above.
            TravelLight.ModifyMaxCarryWeight(player, ref limit);
        }
    }
}
