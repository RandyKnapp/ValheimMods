using HarmonyLib;

namespace EpicLoot
{
    /// <summary>
    /// Vanilla records a discovered biome by the localized, AltBiome-decorated sector name
    /// (<see cref="BiomeSector.GetName"/>), so what lands in <see cref="Player.m_knownBiome"/> depends
    /// on the display language at the moment of discovery and on which sector the player walked into.
    /// Switching language, or a mod giving a sector an AltBiome name prefix, therefore makes a biome
    /// the player has already visited read as undiscovered.
    ///
    /// This writes the plain "$biome_x" token alongside vanilla's entry, which is stable against both.
    /// Nothing in vanilla reads the set except <see cref="Player.AddKnownBiome"/>'s own duplicate check
    /// - and that compares against the decorated name, never this token, so the "biome found" message
    /// and the tutorials it triggers are untouched. <see cref="Player.IsBiomeKnown"/> has no vanilla
    /// callers at all.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.AddKnownBiome), typeof(BiomeSector))]
    public static class Player_AddKnownBiome_RecordStableToken
    {
        public static void Postfix(Player __instance, BiomeSector biome)
        {
            if (__instance == null || biome == null)
            {
                return;
            }

            __instance.m_knownBiome.Add(BiomeSector.GetBiomeName(biome.Biome));
        }
    }
}
