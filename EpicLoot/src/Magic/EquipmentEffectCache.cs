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

    // Mono's ConditionalWeakTable takes a lock and probes an ephemeron table on every lookup, and the
    // hot readers (SEMan.ModifyMaxCarryWeight, SEMan.ModifyStaminaRegen, ItemData.GetArmor) each ask
    // for a handful of effects per fixed tick, always for the same player. Hold on to the table that
    // was looked up last so those runs cost a reference compare instead. Cleared by Reset, so this can
    // never hand back a dictionary the weak table has already dropped.
    private static Player _lastPlayer;
    private static Dictionary<string, float?> _lastValues;

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
        if (player == null)
        {
            return;
        }

        EquippedValues.Remove(player);

        if (ReferenceEquals(player, _lastPlayer))
        {
            _lastPlayer = null;
            _lastValues = null;
        }
    }

    private static Dictionary<string, float?> ValuesFor(Player player)
    {
        if (ReferenceEquals(player, _lastPlayer))
        {
            return _lastValues;
        }

        Dictionary<string, float?> values = EquippedValues.GetOrCreateValue(player);
        _lastPlayer = player;
        _lastValues = values;
        return values;
    }

    /// <summary>
    /// Memoized read that does not need the <see cref="Func{T}"/> the <see cref="Get"/> overload takes.
    /// That delegate is built on every call, hit or miss, because the argument is evaluated before the
    /// lookup happens -- two heap allocations each time. Callers on per-tick paths use this and
    /// <see cref="Store"/> instead, so only an actual miss pays for building the value.
    /// </summary>
    public static bool TryGetValue(Player player, string effect, out float? value)
    {
        if (effect == null || player == null)
        {
            value = 0f; // default fail out if the requested key is null
            return true;
        }

        return ValuesFor(player).TryGetValue(effect, out value);
    }

    public static void Store(Player player, string effect, float? value)
    {
        if (effect == null || player == null)
        {
            return;
        }

        ValuesFor(player)[effect] = value;
    }

    public static float? Get(Player player, string effect, Func<float?> calculate)
    {
        if (TryGetValue(player, effect, out float? value))
        {
            return value;
        }

        value = calculate();
        Store(player, effect, value);
        return value;
    }
}
