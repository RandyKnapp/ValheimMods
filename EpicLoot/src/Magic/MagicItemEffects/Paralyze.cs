using EpicLoot.General;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    public class SE_Paralyzed : StatusEffect
    {
        public void Setup(float lifetime)
        {
            m_ttl = Mathf.Max(lifetime, GetRemaningTime());
            ResetTime();
        }

        public override void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
        {
            speed *= 0;
        }
    }

    public static class Paralyze
    {
        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        public static class Paralyze_Character_Damage_Patch
        {
            public static void Postfix(Character __instance, HitData hit)
            {
                OnDamaged(__instance, hit);
            }
        }

        public static void OnDamaged(Character __instance, HitData hit)
        {
            if ((hit.GetAttacker() != null && !hit.GetAttacker().IsPlayer()) || hit.m_damage.EpicLootGetTotalDamage() <= 0.0)
            {
                return;
            }

            var player = (Player)hit.GetAttacker();
            if (player.HasActiveMagicEffect(MagicEffectType.Paralyze, out float effectValue))
            {
                var seParalyze = __instance.m_seman.GetStatusEffect("Paralyze".GetHashCode()) as SE_Paralyzed;
                if (seParalyze == null)
                {
                    seParalyze = __instance.m_seman.AddStatusEffect("Paralyze".GetHashCode()) as SE_Paralyzed;
                    if (seParalyze == null)
                    {
                        EpicLoot.LogError("Could not add paralyze effect");
                        return;
                    }
                }

                // TODO: this does not work
                /*var fx = __instance.transform.Find("fx_Lightning(Clone)/Sparcs");
                if (fx != null)
                {
                    var ps = fx.GetComponent<ParticleSystem>();
                    var main = ps.main;
                    main.startColor = Color.yellow;
                }*/

                float totalParalyzeTime = effectValue;
                if (Attack_Patch.ActiveAttack != null)
                {
                    totalParalyzeTime = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                        player, Attack_Patch.ActiveAttack.m_weapon, MagicEffectType.Paralyze);
                }

                seParalyze.Setup(totalParalyzeTime);
            }
        }
    }
}
