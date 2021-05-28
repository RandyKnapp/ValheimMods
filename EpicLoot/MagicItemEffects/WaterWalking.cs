using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch]
    public static class WaterWalking
    {
        public static bool IsWaterWalking;
        public static bool IsEquippingWaterWalkingItem;

        [HarmonyPatch(typeof(Character), nameof(Character.IsSwiming))]
        public static class Character_IsSwiming_Patch
        {
            public static bool Prefix(Character __instance, ref bool __result)
            {
                if (__instance.IsPlayer() && (IsWaterWalking || IsEquippingWaterWalkingItem))
                {
                    __result = false;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.IsOnGround))]
        public static class Character_IsOnGround_Patch
        {
            public static bool Prefix(Character __instance, ref bool __result)
            {
                if (__instance.IsPlayer() && (IsWaterWalking || IsEquippingWaterWalkingItem))
                {
                    __result = true;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.UpdateGroundContact))]
        public static class Character_UpdateGroundContact_Patch
        {
            public static bool Prefix(Character __instance)
            {
                if (__instance.IsPlayer() && IsWaterWalking && __instance is Player player && player.IsSitting())
                {
                    SetPostionToWaterLevel(__instance);
                    __instance.m_body.velocity = Vector3.zero;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.ApplyGroundForce))]
        public static class Character_ApplyGroundForce_Patch
        {
            public static bool Prefix(Character __instance, ref Vector3 vel)
            {
                if (__instance.IsPlayer() && __instance is Player player)
                {
                    var previousWaterWalking = IsWaterWalking;
                    if (!player.HasActiveMagicEffect(MagicEffectType.WaterWalking))
                    {
                        IsWaterWalking = false;
                        return true;
                    }

                    if (__instance.m_body.position.y > 4500)
                    {
                        IsWaterWalking = false;
                        return true;
                    }

                    var waterLevel = WaterVolume.GetWaterLevel(__instance.m_body.position);
                    IsWaterWalking = waterLevel > __instance.m_body.position.y;
                    if (IsWaterWalking)
                    {
                        vel.y = Mathf.Max(0, vel.y);
                        if (vel.y <= 0)
                        {
                            SetPostionToWaterLevel(__instance, waterLevel);
                        }
                        __instance.m_maxAirAltitude = -1000;
                        __instance.ResetGroundContact();
                    }

                    if (previousWaterWalking != IsWaterWalking)
                    {
                        EpicLoot.Log($"{(IsWaterWalking ? "Begin" : "End")} Waterwalking");
                    }

                    return !IsWaterWalking;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.ApplySlide))]
        public static class Character_ApplySlide_Patch
        {
            public static bool Prefix(Character __instance)
            {
                if (__instance.IsPlayer() && IsWaterWalking)
                {
                    __instance.m_sliding = false;
                    __instance.m_slippage = 0;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        public static class Humanoid_EquipItem_Patch
        {
            public static bool Prefix(Humanoid __instance, ItemDrop.ItemData item)
            {
                IsEquippingWaterWalkingItem = __instance.IsPlayer() && item.HasMagicEffect(MagicEffectType.WaterWalking);
                return true;
            }

            public static void Postfix(Humanoid __instance)
            {
                IsEquippingWaterWalkingItem = false;
                if (__instance.IsPlayer() && __instance is Player player && player.HasActiveMagicEffect(MagicEffectType.WaterWalking))
                {
                    if (player.IsSwiming())
                    {
                        IsWaterWalking = true;
                    }
                }
            }
        }

        public static void SetPostionToWaterLevel(Character __instance)
        {
            var waterLevel = WaterVolume.GetWaterLevel(__instance.m_body.position);
            SetPostionToWaterLevel(__instance, waterLevel);
        }

        public static void SetPostionToWaterLevel(Character __instance, float waterLevel)
        {
            __instance.m_body.position = new Vector3(__instance.m_body.position.x, waterLevel, __instance.m_body.position.z);
        }
    }
}
