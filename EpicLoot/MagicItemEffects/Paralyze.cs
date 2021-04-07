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

        public override void ModifySpeed(ref float speed)
        {
            speed *= 0;
        }
    }

    public static class Paralyze
    {
        //public virtual void OnDamaged(HitData hit)
        [HarmonyPatch(typeof(Character), nameof(Character.OnDamaged))]
        public static class Paralyze_Character_OnDamaged_Patch
        {
            public static void Postfix(Character __instance, HitData hit)
            {
                OnDamaged(__instance, hit);
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.OnDamaged))]
        public static class Paralyze_Humanoid_OnDamaged_Patch
        {
            public static void Postfix(Character __instance, HitData hit)
            {
                OnDamaged(__instance, hit);
            }
        }

        public static void OnDamaged(Character __instance, HitData hit)
        {
            var attacker = hit.GetAttacker() as Humanoid;
            if (attacker == null)
            {
                return;
            }

            var weapon = attacker.GetCurrentWeapon();
            if (weapon != null && weapon.IsMagic() && weapon.HasMagicEffect(MagicEffectType.Paralyze))
            {
                if (hit.GetTotalDamage() <= 0.0)
                {
                    return;
                }

                var seParalyze = __instance.m_seman.GetStatusEffect("Paralyze") as SE_Paralyzed;
                if (seParalyze == null)
                {
                    seParalyze = __instance.m_seman.AddStatusEffect("Paralyze") as SE_Paralyzed;
                    if (seParalyze == null)
                    {
                        EpicLoot.LogError("Could not add paralyze effect");
                        return;
                    }
                }

                // TODO: this does not work
                var fx = __instance.transform.Find("fx_Lightning(Clone)/Sparcs");
                if (fx != null)
                {
                    var ps = fx.GetComponent<ParticleSystem>();
                    var main = ps.main;
                    main.startColor = Color.yellow;
                }

                var totalParalyzeTime = weapon.GetMagicItem().GetTotalEffectValue(MagicEffectType.Paralyze);
                seParalyze.Setup(totalParalyzeTime);
            }
        }
    }
}
