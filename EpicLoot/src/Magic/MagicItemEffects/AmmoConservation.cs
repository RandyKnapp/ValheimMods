using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Magic.MagicItemEffects;

public class AmmoConservation
{
    private static float randomRollValue;
    private static bool skipReload = false;
    
    [HarmonyPatch(typeof(Attack), nameof(Attack.UseAmmo))]
    public static class AmmoConservation_Attack_UseAmmo_Patch
    {
        public static void Postfix(Attack __instance, ref bool __result, ItemDrop.ItemData ammoItem)
        {
            if (__result == false) return;
            
            Player player = __instance.m_character as Player;
            if (player != Player.m_localPlayer) return;

            float effectValue = player.GetTotalActiveMagicEffectValue(MagicEffectType.AmmoConservation, 0.01f);
            if (effectValue == 0) return;

            randomRollValue = UnityEngine.Random.Range(0, 100);
            randomRollValue *= 0.01f;

            if (randomRollValue < effectValue)
            {
                player.GetInventory().AddItem(ammoItem.m_dropPrefab, 1);
                skipReload = true;
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UpdateWeaponLoading))]
    public static class AmmoConservation_Player_UpdateWeaponLoading_Patch
    {
        public static bool Prefix(Player __instance)
        {
            if (skipReload)
            {
                skipReload = false;
                
                var currentWeapon = __instance.GetCurrentWeapon();
                if (currentWeapon != null && currentWeapon.m_shared.m_attack.m_requiresReload)
                {
                    var setWeaponLoadedMethod = typeof(Player).GetMethod("SetWeaponLoaded", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    setWeaponLoadedMethod?.Invoke(__instance, new object[] { currentWeapon });
                }
                
                return false;
            }
            return true;
        }
    }
}
