using System;
using System.Collections.Generic;

namespace EpicLoot.Adventure;

public class PinJob
{
    public MinimapPinQueueTask Task { get; set; }
    public KeyValuePair<Tuple<int, Heightmap.Biome>, AreaPinInfo> TreasurePin { get; set; }
    public KeyValuePair<string, AreaPinInfo> BountyPin { get; set; }
    public bool DebugMode { get; set; }
}