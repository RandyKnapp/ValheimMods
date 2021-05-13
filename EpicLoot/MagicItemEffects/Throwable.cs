using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    public class Spinny : MonoBehaviour
    {
        private const float RotationSpeed = 720;

        public void Awake()
        {
            transform.Rotate(-90, 270, 0);
        }

        public void Update()
        {
            transform.Rotate(0, -RotationSpeed * Time.deltaTime, 0);
        }
    }

    //public override bool StartAttack(Character target, bool secondaryAttack)
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    public static class Throwable_Humanoid_StartAttack_Patch
    {
        public static bool Prefix(Humanoid __instance, ref bool __result, bool secondaryAttack)
        {
            if (!secondaryAttack)
            {
                return true;
            }

            __instance.AbortEquipQueue();
            if (__instance.InAttack() && !__instance.HaveQueuedChain() || __instance.InDodge() || !__instance.CanMove() || __instance.IsKnockedBack() || __instance.IsStaggering() || __instance.InMinorAction())
            {
                return true;
            }

            var currentWeapon = __instance.GetCurrentWeapon();
            if (currentWeapon == null || currentWeapon.m_dropPrefab == null)
            {
                EpicLoot.Log("Weapon or weapon's dropPrefab is null");
                return true;
            }

            if (!currentWeapon.IsMagic() || !currentWeapon.GetMagicItem().HasEffect(MagicEffectType.Throwable))
            {
                return true;
            }

            var spearPrefab = ObjectDB.instance?.GetItemPrefab("SpearFlint");
            if (spearPrefab == null)
            {
                return true;
            }

            if (__instance.m_currentAttack != null)
            {
                __instance.m_currentAttack.Stop();
                __instance.m_previousAttack = __instance.m_currentAttack;
                __instance.m_currentAttack = null;
            }

            var attack = spearPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_secondaryAttack.Clone();

            if (!attack.Start(__instance, __instance.m_body, __instance.m_zanim, __instance.m_animEvent, __instance.m_visEquipment, currentWeapon, __instance.m_previousAttack, __instance.m_timeSinceLastAttack, __instance.GetAttackDrawPercentage()))
            {
                return false;
            }

            __instance.m_currentAttack = attack;
            __instance.m_lastCombatTimer = 0.0f;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.ProjectileAttackTriggered))]
    public static class Throwable_Attack_ProjectileAttackTriggered_Patch
    {
	    public static void Prefix(Attack __instance, ref EffectList __state)
	    {
		    if (__instance.m_weapon.IsMagic() && __instance.m_weapon.GetMagicItem().HasEffect(MagicEffectType.Throwable))
		    {
			    __state = __instance.m_weapon.m_shared.m_triggerEffect;
			    __instance.m_weapon.m_shared.m_triggerEffect = new EffectList();
		    }
	    }
		    
	    public static void Postfix(Attack __instance, EffectList __state)
	    {
		    if (__instance.m_weapon.IsMagic() && __instance.m_weapon.GetMagicItem().HasEffect(MagicEffectType.Throwable))
		    {
			    if (__instance.m_weapon.m_lastProjectile.GetComponent<Projectile>() is Projectile projectile)
			    {
				    projectile.m_spawnOnHitEffects = new EffectList { m_effectPrefabs = projectile.m_spawnOnHitEffects.m_effectPrefabs.Concat(__state.m_effectPrefabs).ToArray() };
				    projectile.m_aoe = __instance.m_weapon.m_shared.m_attack.m_attackRayWidth;
			    }

			    __instance.m_weapon.m_shared.m_triggerEffect = __state;
		    }
	    }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
    public static class Throwable_Attack_FireProjectileBurst_Patch
    {
        public static void Postfix(Attack __instance)
        {
            if (__instance.m_weapon.m_lastProjectile != null && __instance.m_weapon.IsMagic() && __instance.m_weapon.GetMagicItem().HasEffect(MagicEffectType.Throwable))
            {
                var existingMesh = __instance.m_weapon.m_lastProjectile.transform.Find("spear");
                if (existingMesh != null)
                {
                    Object.Destroy(existingMesh.gameObject);
                }

                var weaponMesh = __instance.m_weapon.m_dropPrefab.transform.Find("attach");
                if (weaponMesh == null)
                {
                    EpicLoot.Log("Could not find 'attach' object");
                    return;
                }

                var newMesh = Object.Instantiate(weaponMesh.gameObject, __instance.m_weapon.m_lastProjectile.transform, false);
                newMesh.AddComponent<Spinny>();
            }
        }
    }
}