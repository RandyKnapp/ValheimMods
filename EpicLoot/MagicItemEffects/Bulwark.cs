using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
	public class Bulwark_ObjectDB_Awake_Patch
	{
		private static void Postfix(ObjectDB __instance)
		{
			var bulwark = ScriptableObject.CreateInstance<StatusEffect>();
			bulwark.name = "Bulwark";
			bulwark.m_name = "Bulwark";
			bulwark.m_ttl = 4f;
			// TODO: Add icon for the undying status effect here
			bulwark.m_icon = ObjectDB.instance.GetStatusEffect("Lightning").m_icon;
            
			// TODO: Add visual effect for bulwark here
			SkipAwakeInit_ZNetView_Awake_Patch.skipAwake = true;
			GameObject protectPrefab = ZNetScene.instance.GetPrefab("vfx_GoblinShield");
			GameObject protect = Object.Instantiate(protectPrefab);
			protect.name = "bulwark effect";
			SkipAwakeInit_ZNetView_Awake_Patch.skipAwake = false;
			Transform particles = protect.transform.Find("Sphere");
			particles.GetComponent<Renderer>().material.color = new Color(1f, 0f, 1f, 0.05f);

			bulwark.m_startEffects = new EffectList
			{
				m_effectPrefabs = new[]
				{
					new EffectList.EffectData
					{
						m_prefab = protect,
						m_attach = true,
						m_inheritParentScale = true,
					}
				}
			};
            
			__instance.m_StatusEffects.Add(bulwark);
		}
	}

	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	public class Bulwark_Character_RPC_Damage_Patch
	{
		[UsedImplicitly]
		[HarmonyPriority(Priority.VeryHigh)]
		private static bool Prefix(Character __instance, HitData hit)
		{
			return !(__instance is Player player) || !player.m_seman.HaveStatusEffect("Bulwark");
		}
	}

	public class Bulwark
	{
		public void Trigger()
		{
			Player.m_localPlayer.GetSEMan().AddStatusEffect("Bulwark", true);
		}
	}
}