using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Magic.MagicItemEffects;

public class AmmoConservation {
    private static bool skipReload = false;

    [HarmonyPatch(typeof(Attack), nameof(Attack.UseAmmo))]
    public static class AmmoConservation_Attack_UseAmmo_Patch {
        public static void Postfix(Attack __instance, ref bool __result, ItemDrop.ItemData ammoItem) {
            if (__result == false || ammoItem == null || ammoItem.m_dropPrefab == null) return;

            Player player = __instance.m_character as Player;
            if (player == null || player != Player.m_localPlayer) return;

            float effectValue = player.GetTotalActiveMagicEffectValue(MagicEffectType.AmmoConservation, 0.01f);
            if (effectValue == 0) return;

            if (UnityEngine.Random.value < effectValue) {
                player.GetInventory().AddItem(ammoItem.m_dropPrefab, 1);

                // The free instant reload only makes sense for weapons that actually reload
                // (crossbows); a bow refund must not touch the loading tick.
                var weapon = __instance.GetWeapon();
                if (weapon != null && weapon.m_shared.m_attack.m_requiresReload) {
                    skipReload = true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UpdateWeaponLoading))]
    public static class AmmoConservation_Player_UpdateWeaponLoading_Patch {
        public static void Prefix(Player __instance) {
            if (skipReload) {
                skipReload = false;

                var currentWeapon = __instance.GetCurrentWeapon();
                if (currentWeapon != null && currentWeapon.m_shared.m_attack.m_requiresReload) {
                    // Publicized assembly: call vanilla's (private) setter directly instead of
                    // reflecting. Marking the weapon loaded and then letting the original run keeps
                    // vanilla's own loaded-weapon bookkeeping (weapon-switch clearing) intact --
                    // the old prefix cancelled the whole tick.
                    __instance.SetWeaponLoaded(currentWeapon);
                }
            }
        }
    }
}
