using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Harvest yield more items when a pickable is harvested
    public static class BountifulHarvest {
        // The extra item rides on the bonus Pickable.Interact sends with RPC_Pick, as the vanilla farming bonus
        // does, so whichever machine owns the plant drops it with the rest. Checking GetPicked() right after
        // Interact only ever saw a pick on a plant this client owned: a remote owner marks it picked later.
        [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
        private static class Pickable_Interact_Patch {
            [UsedImplicitly]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
                CodeMatcher codeMatcher = new CodeMatcher(instructions);
                // The bonus is the int boxed into RPC_Pick's argument array.
                codeMatcher.MatchStartForward(new CodeMatch(OpCodes.Ldstr, "RPC_Pick"))
                    .ThrowIfNotMatch("Unable to patch Pickable.Interact for Bountiful Harvest.")
                    .MatchStartForward(new CodeMatch(OpCodes.Box, typeof(int)))
                    .ThrowIfNotMatch("Unable to patch Pickable.Interact for Bountiful Harvest.")
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldarg_2),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BountifulHarvest), nameof(AddBonus))));
                return codeMatcher.Instructions();
            }
        }

        private static int AddBonus(int bonus, Pickable pickable, Humanoid character, bool repeat) {
            // Only a fresh pick by the local player (not a held-Use repeat).
            if (repeat || character == null || character != Player.m_localPlayer || pickable.GetPicked() ||
                pickable.m_itemPrefab == null) {
                return bonus;
            }

            var chance = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.BountifulHarvest, 0.01f);
            if (chance <= 0f || Random.value >= chance) {
                return bonus;
            }

            return bonus + 1;
        }
    }
}
