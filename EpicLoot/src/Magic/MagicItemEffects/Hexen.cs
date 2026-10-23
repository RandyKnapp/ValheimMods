using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string Hexen = nameof(Hexen);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // Hexen (4-piece Gullveig's Rebirth set bonus, NoRoll). The wearer takes the JotunWitch body, moveset and flight
    // for as long as the set is worn; the form itself lives in Transformation.HexenForm, which polls this effect on
    // the owner. The value is the Eitr drained per second of flight.
    public static class Hexen
    {
        // All tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultScale = 0.6f;     // multiplier on the witch's native size
        public const float DefaultMinEitr = 10f;    // Eitr needed before the wearer can take off

        private const string ScaleKey = "Scale";
        private const string MinEitrKey = "MinEitr";

        public static float Scale => EffectConfig.GetClamped(MagicEffectType.Hexen, ScaleKey, DefaultScale, 0.2f, 1.5f);
        public static float MinEitr => EffectConfig.GetClamped(MagicEffectType.Hexen, MinEitrKey, DefaultMinEitr, 1f, 1000f);

        // The drain the hexen console command flies on without the set: the Legendary value.
        public static float FallbackDrain =>
            MagicItemEffectDefinitions.Get(MagicEffectType.Hexen)?.GetValuesForRarity(ItemRarity.Legendary)?.MinValue ?? 3f;

        // Tooltip: "... drains {0} Eitr per second, needs {1} to take off ..."
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.Hexen, value => new object[] { value, MinEitr });
        }
    }
}
