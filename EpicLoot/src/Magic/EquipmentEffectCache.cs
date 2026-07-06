using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace EpicLoot;

public static class EquipmentEffectCache
{
    public static ConditionalWeakTable<Player, Dictionary<string, float?>> EquippedValues =
        new ConditionalWeakTable<Player, Dictionary<string, float?>>();

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    public static class EquipmentEffectCache_Humanoid_UnequipItem_Patch
    {
        [UsedImplicitly]
        public static void Prefix(Humanoid __instance)
        {
            if (__instance is Player player)
            {
                Reset(player);
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    public static class EquipmentEffectCache_Humanoid_EquipItem_Patch
    {
        [UsedImplicitly]
        public static void Prefix(Humanoid __instance)
        {
            if (__instance is Player player)
            {
                Reset(player);
            }
        }
    }

    public static void Reset(Player player)
    {
        EquippedValues.Remove(player);
    }

    public static float? Get(Player player, string effect, Func<float?> calculate)
    {
        if (effect == null || player == null)
        {
            return 0f; // default fail out if the requested key is null
        }

        Dictionary<string, float?> values = EquippedValues.GetOrCreateValue(player);
        if (values.TryGetValue(effect, out float? value))
        {
            return value;
        }

        return values[effect] = calculate();
    }
}
