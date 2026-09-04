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
            string known = Player.m_localPlayer != null && Player.m_localPlayer.m_knownBiome.Contains(definition.Biome) ?
                "known" : "not known";
            string origin = definition.IsLegacy ? ", legacy Bounties.Bosses" : definition.IsVanilla ? "" : ", custom";
            string display = Localization.instance.Localize(BiomeDataManager.GetLocalizationToken(definition.Biome));
            sb.AppendLine($"{definition.Index} - {definition.Name} (id {(int)definition.Biome}{origin}): " +
                $"keys {keys}, {defeated}, {known}, color {definition.Color}, shown as \"{display}\"");
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
        player.AddKnownBiome(biome);
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
