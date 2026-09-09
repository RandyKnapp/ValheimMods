using System.Collections.Generic;
using System.Linq;
using System.Text;
using EpicLoot.Biomes;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static void PrintBiomes(Terminal.ConsoleEventArgs args)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Biomes in progression order (biomedata.json):");
        foreach (BiomeDefinition definition in BiomeDataManager.BiomesInOrder)
        {
            string keys = definition.BossDefeatedKeys.Count == 0 ? "(ungated)" : string.Join(", ", definition.BossDefeatedKeys);
            string defeated = BiomeDataManager.HasAllBossKeys(definition.Biome) ? "defeated" : "not defeated";
            string known = BiomeDataManager.IsDiscoveredBy(Player.m_localPlayer, definition.Biome) ?
                "known" : "not known";
            string origin = definition.IsLegacy ? ", legacy Bounties.Bosses" : definition.IsVanilla ? "" : ", custom";
            string display = Localization.instance.Localize(BiomeDataManager.GetLocalizationToken(definition.Biome));
            sb.AppendLine($"{definition.Index} - {definition.Name} (id {(int)definition.Biome}{origin}): " +
                $"keys {keys}, {defeated}, {known}, color {definition.Color}, shown as \"{display}\"");
        }

        // The raw set vanilla actually keeps, and the two strings IsDiscoveredBy matches against it.
        // Printing both sides is the only way to tell "the player has been nowhere" apart from
        // "the player has been there but the recorded name does not match what we look for".
        Player player = Player.m_localPlayer;
        sb.AppendLine();
        if (player == null)
        {
            sb.AppendLine("Player.m_knownBiome: no local player");
        }
        else
        {
            sb.AppendLine($"Player.m_knownBiome holds {player.m_knownBiome.Count} entry(s):");
            foreach (string entry in player.m_knownBiome)
            {
                sb.AppendLine($"    \"{entry}\"");
            }

            sb.AppendLine("Looked up as:");
            foreach (BiomeDefinition definition in BiomeDataManager.BiomesInOrder)
            {
                string token = BiomeSector.GetBiomeName(definition.Biome);
                string localized = Localization.instance == null ? "(no Localization)" : Localization.instance.Localize(token);
                sb.AppendLine($"    {definition.Name,-14} token \"{token}\" -> localized \"{localized}\"");
            }
        }

        args.Context.PrintInfo(sb.ToString());
    }

    private static void KnowBiome(Terminal.ConsoleEventArgs args)
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            args.Context.PrintInfo("> No local player");
            return;
        }

        string arg = args.GetString(1, "");
        if (!BiomeDataManager.TryResolve(arg, out Heightmap.Biome biome) || biome == Heightmap.Biome.None)
        {
            args.Context.PrintInfo($"> Unknown biome '{arg}'. Use 'biomes' to list them.");
            return;
        }

        // The same path the game takes when the player first walks into a biome, so bounties and
        // treasure maps for a custom biome can be tested without the biome mod generating terrain.
        BiomeDataManager.MarkDiscoveredBy(player, biome);
        args.Context.PrintInfo($"> {BiomeDataManager.GetName(biome)} is now a known biome");
    }

    private static List<string> GetKnowBiomeOptions(string[] args)
    {
        return args.Length switch
        {
            2 => BiomeDataManager.BiomesInOrder.Select(x => x.Name).ToList(),
            _ => []
        };
    }
}
