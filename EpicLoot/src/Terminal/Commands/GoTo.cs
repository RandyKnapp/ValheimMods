using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static readonly Dictionary<string, string> merchants = new Dictionary<string, string>()
    {
        ["haldor"] = "Vendor_BlackForest",
        ["witch"] = "BogWitch_Camp",
        ["hildir"] = "Hildir_camp"
    };
    private static void GoToMerchant(Terminal.ConsoleEventArgs args)
    {
        Player player = Player.m_localPlayer;
        string merchant = args.GetString(1, "haldor");
        if (!merchants.TryGetValue(merchant, out string locationName))
        {
            args.Context.PrintError("> Invalid merchant");
            return;
        }
        
        if (ZoneSystem.instance.FindClosestLocation(locationName, player.transform.position, out var location))
        {
            args.Context.PrintInfo($"x: {location.m_position.x}, y: {location.m_position.y}, z: {location.m_position.z}");
            player.TeleportTo(location.m_position + Vector3.right * 5, player.transform.rotation, true);
        }
        else
        {
            args.Context.PrintError($"> Failed to find {merchant} location");
        }
    }

    private static List<string> GetGoToOptions(string[] args)
    {
        return args.Length switch
        {
            2 => merchants.Keys.ToList(),
            _ => []
        };
    }
}