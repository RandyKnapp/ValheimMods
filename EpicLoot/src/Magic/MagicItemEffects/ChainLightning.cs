using UnityEngine;


namespace EpicLoot.MagicItemEffects {
    public class ChainLightning {
        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(Character __instance, HitData hit, Character attacker) {
            if (attacker == null || !attacker.IsPlayer()) {
                return;
            }

            var player = attacker as Player;
            var weapon = player?.GetCurrentWeapon();
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

            var aoe = instance.GetComponent<Aoe>();
            if (aoe != null) {
                aoe.m_chainChance = 0.8f; // I dont even know what this actually does. Testing remains inconclusive
                aoe.m_chainStartChanceFalloff = 0.8f; // .5->.8 which is default. Chains fall off sooner now. Same as Vanilla
                aoe.m_owner = player;
                aoe.m_damage.m_lightning *= player.GetTotalActiveMagicEffectValue(MagicEffectType.ChainLightning, .01f); // damage of lightning. This works.
            }
        }
    }
}