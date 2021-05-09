using System;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(Character), "UpdateGroundContact")]
	public class FeatherFallDisableFallDamage_Character_UpdateGroundContact_Patch
	{
		[UsedImplicitly]
		private static void Prefix(Character __instance, ref float ___m_maxAirAltitude)
		{
			if (!__instance.m_groundContact)
			{
				return;
			}
			
			if (__instance is Player player && player.HasMagicEquipmentWithEffect(MagicEffectType.FeatherFall))
			{
				___m_maxAirAltitude = Mathf.Min(3.99f + __instance.transform.position.y, ___m_maxAirAltitude);
			}
		}
	}
	
	[HarmonyPatch(typeof(Player), "FixedUpdate")]
	public class FeatherFallReduceFallSpeed_Player_FixedUpdate_Patch
    {
        public const string FeatherFallEffectName = "FeatherFallAura";
        public const float MaxFallSpeed = -6;

		[UsedImplicitly]
		private static void Postfix(Player __instance)
		{
            var currentFeatherFallAura = __instance.transform.Find(FeatherFallEffectName);

			if (!__instance.IsOnGround() && __instance.HasMagicEquipmentWithEffect(MagicEffectType.FeatherFall) && __instance.m_body)
			{
				var velocity = __instance.m_body.velocity;

                if (velocity.y <= MaxFallSpeed + float.Epsilon * 10)
                {
                    if (currentFeatherFallAura != null && currentFeatherFallAura.GetComponent<ParticleSystem>().isStopped)
                    {
                        currentFeatherFallAura.GetComponent<ParticleSystem>().Play();
                        currentFeatherFallAura.GetComponent<AudioSource>().Play();
                    }
                    else if (currentFeatherFallAura == null)
                    {
                        var effect = Object.Instantiate(EpicLoot.LoadAsset<GameObject>(FeatherFallEffectName), __instance.transform);
                        effect.name = FeatherFallEffectName;
                        effect.GetComponent<AudioSource>().outputAudioMixerGroup = AudioMan.instance.m_ambientMixer;
                    }

                    velocity.x = Mathf.Lerp(velocity.x, 0, 1 / 30f);
                    velocity.z = Mathf.Lerp(velocity.z, 0, 1 / 30f);
                }

				velocity.y = Math.Max(MaxFallSpeed, velocity.y);
				__instance.m_body.velocity = velocity;
			}

            if (__instance.IsOnGround() && currentFeatherFallAura != null)
            {
                currentFeatherFallAura.GetComponent<ParticleSystem>().Stop();
                currentFeatherFallAura.GetComponent<AudioSource>().Stop();
            }
        }
	}
}