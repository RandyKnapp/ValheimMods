using JetBrains.Annotations;
using System;
using System.Collections.Generic;

namespace EpicLootAPI;

/// <summary>
/// Mirror of Epic Loot's serialized magic item payload. Kept field-for-field in sync with
/// EpicLoot/src/Magic/MagicItem.cs -- a missing field is silently dropped when this round-trips through
/// <see cref="EpicLoot.GetMagicItem"/>, so socketed and tempered data would be lost.
/// </summary>
[Serializable]
[PublicAPI]
public class MagicItem
{
    public int Version = 3;
    public ItemRarity Rarity;
    public List<MagicItemEffect> Effects = new();
    public string TypeNameOverride = "";
    public int AugmentedEffectIndex = -1;
    public List<int> AugmentedEffectIndices = new();

    /// <summary>Indices into <see cref="Effects"/> whose value has been rerolled by tempering.</summary>
    public List<int> TemperedEffectIndices = new();

    public string DisplayName = "";
    public string LegendaryID = "";
    public string SetID = "";

    /// <summary>An unidentified item reports as magic but its effects are not revealed yet.</summary>
    public bool IsUnidentified;

    /// <summary>How many sockets the item rolled; <see cref="Sockets"/> may be shorter.</summary>
    public int SocketCount;

    public List<SocketedEffect> Sockets = new();
}

/// <summary>
/// One occupied socket: the granted effect plus enough of the source stone to rebuild it on removal.
/// </summary>
[Serializable]
[PublicAPI]
public class SocketedEffect
{
    public int Version = 2;

    /// <summary>Null for a shard sitting inert -- a shard's effect is derived from its host item.</summary>
    public MagicItemEffect? Effect;

    public string SourcePrefab = "";
    public ItemRarity SourceRarity;

    /// <summary>
    /// The shard color, or <see cref="EpicLootAPI.ShardType.None"/> for a runestone.
    /// </summary>
    public ShardType ShardType = ShardType.None;

    /// <summary>Same-color stacking decay already applied to <see cref="Effect"/>'s value.</summary>
    public float StackMultiplier = 1f;

    public SocketedEffect() { }

    public SocketedEffect(MagicItemEffect effect, string sourcePrefab, ItemRarity sourceRarity)
    {
        Effect = effect;
        SourcePrefab = sourcePrefab;
        SourceRarity = sourceRarity;
    }
}
