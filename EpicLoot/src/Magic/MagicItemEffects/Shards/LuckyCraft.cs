using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a chance to not consume a crafting material when crafting
    public static class LuckyCraft {
        // Player.ConsumeResources is also how vanilla charges for placing a build or cultivator piece, and
        // how the temper panel pays; only the call made from DoCrafting is a craft.
        private static bool _isCrafting;

        // Every vanilla idol (Upgrader{N}Armor/Weapon) carries this token in its name.
        private const string IdolNameToken = "$item_upgrader_name";

        // Idols are never saved. Vanilla flags them m_upgraderResource, the only cost the Forge of Potential
        // charges; the zeroed copy below would drop that flag, and the forge only charges flagged requirements,
        // so a lucky roll on one would skip the idol rather than charge nothing. A Jotunn RequirementConfig
        // cannot set the flag, so an idol another mod's recipe uses as an ordinary ingredient (charged at any
        // station) is recognised by its name instead.
        private static bool IsIdol(Piece.Requirement req) =>
            req.m_upgraderResource || req.m_resItem.m_itemData?.m_shared?.m_name?.Contains(IdolNameToken) == true;

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        private static class InventoryGui_DoCrafting_Patch {
            [UsedImplicitly]
            private static void Prefix() {
                _isCrafting = true;
            }

            // A finalizer rather than a postfix, so an exception inside DoCrafting cannot leave the flag set
            // and let the next piece placed skip its materials.
            [UsedImplicitly]
            private static System.Exception Finalizer(System.Exception __exception) {
                _isCrafting = false;
                return __exception;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
        private static class Player_ConsumeResources_Patch {
            [UsedImplicitly]
            private static void Prefix(Player __instance, ref Piece.Requirement[] requirements) {
                if (!_isCrafting || requirements == null || __instance != Player.m_localPlayer) {
                    return;
                }

                var chance = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.LuckyCraft, 0.01f);
                if (chance <= 0f) {
                    return;
                }

                var replacement = new Piece.Requirement[requirements.Length];
                for (var i = 0; i < requirements.Length; i++) {
                    var req = requirements[i];
                    if (req?.m_resItem != null && !IsIdol(req) && Random.value < chance) {
                        // Copy with a zeroed amount so this one material is skipped; the original recipe
                        // Requirement is left untouched.
                        replacement[i] = new Piece.Requirement {
                            m_resItem = req.m_resItem,
                            m_amount = 0,
                            m_amountPerLevel = 0,
                            m_recover = req.m_recover
                        };
                    } else {
                        replacement[i] = req;
                    }
                }

                requirements = replacement;
            }
        }
    }
}
