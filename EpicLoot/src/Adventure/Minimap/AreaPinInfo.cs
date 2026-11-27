using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.Adventure;

public class AreaPinInfo
{
    public Minimap.PinData Pin { get; set; }
    public Minimap.PinData Area { get; set; }
    public Minimap.PinData DebugPin { get; set; }
    public GameObject RewardRow { get; set; }

    public Vector3 Position { get; set; }
    public Minimap.PinType Type { get; set; }
    public string Name { get; set; }
    public Sprite Icon { get; set; }
    public Sprite AreaIcon { get; set; }
    public List<KeyValuePair<Sprite, int>> Rewards { get; set; }

    public AreaPinInfo()
    {
        Name = string.Empty;
    }
}