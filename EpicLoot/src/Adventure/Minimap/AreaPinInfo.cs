using UnityEngine;

namespace EpicLoot.Adventure;

public class AreaPinInfo
{
    public Minimap.PinData Pin { get; set; }
    public Minimap.PinData Area { get; set; }
    public Minimap.PinData DebugPin { get; set; }

    //Pin Data
    public Vector3 Position { get; set; }
    public Minimap.PinType Type { get; set; }
    public string Name { get; set; }
    public bool Save { get; set; }
    public bool Checked { get; set; }
    public long OwnerId { get; set; }

    public AreaPinInfo()
    {
        Name = string.Empty;
        Save = false;
        Checked = false;
        OwnerId = 0L;
    }
}