using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;


namespace EpicLoot.MagicItemEffects
{
    public class ChainLightning
    {
        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        public static class ChainLightningEffect_Damage_Patch
        {

            public static void Postfix(Character __instance, HitData hit)

            {

                var attacker = hit.GetAttacker();
                if (attacker == null || !attacker.IsPlayer())
                    return;

                var player = attacker as Player;
                var weapon = player?.GetCurrentWeapon();
                if (weapon == null || !weapon.GetMagicItem()?.HasEffect(nameof(MagicEffectType.ChainLightning)) == true)
                    return;

                //float procChance = player.GetTotalActiveMagicEffectValue(MagicEffectType.ChainLightning, .01f) / 2f; - based off buff effect is too strong
                float procChance = .15f;

                if (Random.value <= procChance && player.GetTotalActiveMagicEffectValue(MagicEffectType.ChainLightning, 1f) > 0)
                {
                    TriggerChainLightningEffect(__instance, player);
                }
            }

        }

        private static void TriggerChainLightningEffect(Character target, Player player)
        {
            var prefab = ZNetScene.instance.GetPrefab("ChainLightning");
            if (prefab == null)
                return;

            var instance = Object.Instantiate(prefab, target.transform.position, Quaternion.identity);

            var aoe = instance.GetComponent<Aoe>();
            if (aoe != null)
            {
                aoe.m_chainChance = 0.8f;
                aoe.m_chainStartChanceFalloff = 0.5f;
                aoe.m_owner = player;
                aoe.m_damage.m_lightning *= player.GetTotalActiveMagicEffectValue(MagicEffectType.ChainLightning, .01f);
            }

        }
    }

}