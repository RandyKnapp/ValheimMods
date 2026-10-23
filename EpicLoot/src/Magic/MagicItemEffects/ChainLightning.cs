using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;


namespace EpicLoot.MagicItemEffects {
    public class ChainLightning {
        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(Character __instance, HitData hit, Character attacker) {
            if (attacker == null || !attacker.IsPlayer()) {
                return;
            }

            var player = attacker as Player;
            // The weapon that dealt this hit -- for an arrow or a thrown weapon, the one that fired it -- not
            // whatever is in the right hand now.
            var weapon = MagicEffectsHelper.GetActiveWeapon(player);
            if (weapon == null || weapon.GetMagicItem()?.HasEffect(nameof(MagicEffectType.ChainLightning), includeSocketed: true) != true) {
                return;
            }

            //float procChance = player.GetTotalActiveMagicEffectValue(MagicEffectType.ChainLightning, .01f) / 2f; - based off buff effect is too strong
            float procChance = .15f;
            //hmmm

            //Check if damage is from weapon
            Skills.SkillType skill = hit.m_skill;
            if (skill != Skills.SkillType.Swords &&
                skill != Skills.SkillType.Clubs &&
                skill != Skills.SkillType.Knives &&
                skill != Skills.SkillType.Unarmed &&
                skill != Skills.SkillType.Axes &&
                skill != Skills.SkillType.Polearms &&
                skill != Skills.SkillType.Spears &&
                skill != Skills.SkillType.Bows &&
                skill != Skills.SkillType.Crossbows &&
                skill != Skills.SkillType.ElementalMagic &&
                skill != Skills.SkillType.BloodMagic
                ) {
                return;
            }

            if (Random.value <= procChance) {
                TriggerChainLightningEffect(__instance, player);
            }
        }
        private static void TriggerChainLightningEffect(Character target, Player player) {
            var prefab = ZNetScene.instance.GetPrefab("ChainLightning");
            if (prefab == null) {
                return;
            }

            var instance = Object.Instantiate(prefab, target.transform.position, Quaternion.identity);
            // Its hits (and every jump's, which clones this live object) are bonus hits, not weapon strikes.
            HitSource.MarkBonusSource(instance);
            // Cloned into every jump as well, which is what lets the patch below find this chain's strikes.
            instance.AddComponent<ChainStrike>();

            var aoe = instance.GetComponent<Aoe>();
            if (aoe != null) {
                // The chance this strike jumps on, falling by the falloff with each jump (applied below).
                aoe.m_chainChance = 0.8f;
                aoe.m_chainStartChanceFalloff = 0.8f;
                aoe.m_owner = player;
                aoe.m_damage.m_lightning *= player.GetTotalActiveMagicEffectValue(MagicEffectType.ChainLightning, .01f); // damage of lightning. This works.
            }
        }

        // Marks a strike of an EpicLoot chain. Instantiate copies components, so each jump carries it too.
        public sealed class ChainStrike : MonoBehaviour {
        }

        // Vanilla hands each jump its parent's chance times the falloff (m_chainChance) but rolls the jump against
        // the prefab's fixed m_chainStartChance, so the chance and falloff set above were never used and every
        // strike jumped at the prefab's 80%. Roll this chain's strikes against their own falling chance. Only
        // checked until the strike's one chain roll (m_chainDelay goes negative after it).
        [HarmonyPatch(typeof(Aoe), nameof(Aoe.CustomFixedUpdate))]
        private static class Aoe_CustomFixedUpdate_Patch {
            [UsedImplicitly]
            private static void Prefix(Aoe __instance) {
                if (__instance.m_chainStartChance <= 0f || __instance.m_chainDelay < 0f ||
                    __instance.m_chainChance <= 0f || __instance.m_chainStartChance == __instance.m_chainChance ||
                    !__instance.TryGetComponent(out ChainStrike _)) {
                    return;
                }

                __instance.m_chainStartChance = __instance.m_chainChance;
            }
        }
    }
}