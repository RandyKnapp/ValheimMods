using JetBrains.Annotations;

namespace EpicLootAPI;

/// <summary>
/// A shard stone's color, which determines the set of effects it can grant.
/// </summary>
/// <remarks>
/// Mirror of EpicLoot.ShardStones.ShardType. Serialized <b>by ordinal</b> onto live gear, so the explicit
/// numbers and the gaps between them (10-29, 35-39, 43-89) are load-bearing: never renumber a member or
/// fill a gap out of order.
/// </remarks>
[PublicAPI]
public enum ShardType
{
    // Core shards
    Red = 0,        // Vitality
    Yellow = 1,     // Stamina
    Cyan = 2,       // Eitr

    // Standard shards
    Black = 3,      // Night time
    Green = 4,      // Movement
    Orange = 5,     // Fire
    Pink = 6,       // Dodge
    Purple = 7,     // Eitr
    White = 8,      // Daytime
    Grey = 9,       // Harvesting

    // Dark shards
    DarkGreen = 30,
    DarkPurple = 31,
    DarkRed = 32,   // Berserker
    DarkBlue = 33,  // Cold resistance
    Golden = 34,    // Luck

    // Light shards
    LightBlue = 40,
    LightGreen = 41,
    Peach = 42,

    // Unique shards -- one signature effect on every slot, one worn at a time
    Firewalker = 70,
    Stormcaller = 71,

    // Boss shards
    Eikthyr = 90,
    Elder = 91,
    Bonemass = 92,
    Moder = 93,
    Yagluth = 94,
    Queen = 95,
    Fader = 96,

    /// <summary>Not a shard -- a socketed runestone, or an unresolved value.</summary>
    None
}
