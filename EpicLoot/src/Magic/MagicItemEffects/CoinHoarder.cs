using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

// CoinHoarder -- every weapon the wearer swings deals more damage the more coins they carry. The bonus joins
// the shared all-damage multiplier in ModifyDamage, so it adds to ModifyDamage, SpellSword and the like.
//
//   curve(coins)  = log10(value * coins) * Scalar / 150
//   bonus         = curve(coins)                          past LinearCutoff coins, or always without LinearGrowth
//                 = curve(LinearCutoff) * coins / cutoff  below it, so the ramp meets the curve with no jump
//
// The value is summed over everything worn, and only then goes into the log, so a second CoinHoarder item adds
// far less than the first one did.
public static class CoinHoarder
{
    // All tunable in this effect's Config block in magiceffects.json, under these key names.
    public const float DefaultLinearGrowth = 1f;    // non-zero: a straight ramp up to the cutoff; 0: the curve from the first coin
    public const float DefaultLinearCutoff = 1000f; // coins at which the ramp hands over to the curve
    public const float DefaultScalar = 6.258f;      // scales the curve, and with it the whole bonus

    private const string LinearGrowthKey = "LinearGrowth";
    private const string LinearCutoffKey = "LinearCutoff";
    private const string ScalarKey = "Scalar";

    // The fixed half of the curve's Scalar / 150.
    private const float ScalarDivisor = 150f;

    // The effect line shows the rolled value, then what it adds to damage at the coins the player carries now.
    public static void RegisterDisplayValues()
    {
        MagicItem.RegisterLiveReadout(MagicEffectType.CoinHoarder, "$mod_epicloot_me_modifydamage_display",
            value => Player.m_localPlayer == null
                ? null
                : new object[] { GetDamageBonus(value, CoinPurse.GetTotalCoins(Player.m_localPlayer)) * 100f });
    }

    /// <summary>
    /// The damage bonus the player's worn CoinHoarder gives at the coins they carry, as a fraction (0.15 = +15%).
    /// Zero without the effect or without coins.
    /// </summary>
    public static float GetDamageBonus(Player player)
    {
        if (player == null || !player.HasActiveMagicEffect(MagicEffectType.CoinHoarder, out float effectValue))
        {
            return 0f;
        }

        return GetDamageBonus(effectValue, CoinPurse.GetTotalCoins(player));
    }

    public static float GetDamageBonus(float effectValue, int coins)
    {
        if (effectValue <= 0f || coins <= 0)
        {
            return 0f;
        }

        // Player-editable: a cutoff below one coin would divide by zero, a negative scalar would cut damage.
        bool linearGrowth = EffectConfig.Get(MagicEffectType.CoinHoarder, LinearGrowthKey, DefaultLinearGrowth) != 0f;
        float cutoff = Mathf.Max(1f, EffectConfig.Get(MagicEffectType.CoinHoarder, LinearCutoffKey, DefaultLinearCutoff));
        float scalar = Mathf.Max(0f, EffectConfig.Get(MagicEffectType.CoinHoarder, ScalarKey, DefaultScalar));

        if (linearGrowth && coins < cutoff)
        {
            return Curve(effectValue, cutoff, scalar) * coins / cutoff;
        }

        return Curve(effectValue, coins, scalar);
    }

    // Floored at zero: below one coin per point of value (a small roll with a handful of coins) the log goes
    // negative, and a damage bonus must never turn into a penalty.
    private static float Curve(float effectValue, float coins, float scalar)
    {
        return Mathf.Max(0f, Mathf.Log10(effectValue * coins) * scalar / ScalarDivisor);
    }
}
