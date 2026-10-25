using System.Collections.Generic;
using EpicLoot.ShardStones;

namespace EpicLoot;

// The rolled effects and the sockets. Both read the MagicItem alone, so the enchanting panel's rune list can
// use them without an item.
internal partial class TooltipGatherer
{
    internal static List<TooltipEffect> GatherEffects(MagicItem magicItem, bool includeDetails)
    {
        List<TooltipEffect> effects = new List<TooltipEffect>(magicItem.Effects.Count);
        for (int index = 0; index < magicItem.Effects.Count; index++)
        {
            MagicItemEffect effect = magicItem.Effects[index];
            MagicItemEffectDefinition.ValueDef range = MagicItemEffectDefinitions.GetRollRange(
                MagicItemEffectDefinitions.Get(effect.EffectType), magicItem.Rarity, magicItem.LegendaryID, null);

            effects.Add(new TooltipEffect
            {
                EffectType = effect.EffectType,
                Value = effect.EffectValue,
                Text = MagicItem.GetEffectText(effect, magicItem.Rarity, false, magicItem.LegendaryID),
                Marker = magicItem.GetEffectMarker(index),
                RollMin = range?.MinValue,
                RollMax = range?.MaxValue,
                Detail = includeDetails
                    ? MagicItem.GetEffectDetailBlock(effect, magicItem.Rarity, magicItem.LegendaryID, null, "   ")
                    : null
            });
        }

        return effects;
    }

    internal static TooltipSockets GatherSockets(MagicItem magicItem, bool includeDetails)
    {
        if (magicItem.SocketCount <= 0)
        {
            return null;
        }

        TooltipSockets sockets = new TooltipSockets
        {
            Count = magicItem.SocketCount,
            Used = magicItem.GetUsedSocketCount(),
            Open = magicItem.GetOpenSocketCount()
        };

        for (int socketIndex = 0; socketIndex < magicItem.Sockets.Count; socketIndex++)
        {
            SocketedEffect socket = magicItem.Sockets[socketIndex];
            if (socket == null)
            {
                continue;
            }

            TooltipSocket entry = new TooltipSocket
            {
                SourcePrefab = socket.SourcePrefab,
                Rarity = (int)socket.SourceRarity,
                // Marks sockets the current config won't let the player simply empty. Driven by the removal
                // policy rather than by the kind of line: an effect with no rarity-scaled value (Warmth, say)
                // renders as a normal effect line yet is break-only under ShardSocketMode.BreakValueless.
                Removal = ToRemoval(ShardSocketManager.GetRemovalPolicy(socket))
            };

            MagicItemEffect socketEffect = magicItem.GetSocketEffect(socketIndex);
            if (socketEffect != null)
            {
                entry.EffectType = socketEffect.EffectType;
                entry.Value = socketEffect.EffectValue;
                entry.Text = MagicItem.GetEffectText(socketEffect, socket.SourceRarity, false);

                if (!socket.IsActive())
                {
                    // A rune held inert by Allow Rune Socketing: still listed, so the player can see what it
                    // would grant and that it is not granting it.
                    entry.State = TooltipSocketState.InactiveRune;
                }
                else
                {
                    entry.StackMultiplier = magicItem.GetSocketStackMultiplier(socketIndex);
                    if (includeDetails)
                    {
                        // A shardstone's value is fixed by the shard grid, not rolled, so it has no range to
                        // show; the rarity's roll range read as though it did. A runestone keeps its range.
                        MagicItemEffectDefinition.ValueDef fixedValue = socket.ShardType != ShardType.None
                            ? new MagicItemEffectDefinition.ValueDef { MinValue = socketEffect.EffectValue, MaxValue = socketEffect.EffectValue }
                            : null;
                        entry.Detail = MagicItem.GetEffectDetailBlock(socketEffect, socket.SourceRarity, null, fixedValue, "     ");
                    }
                }
            }
            else if (socket.ShardType != ShardType.None)
            {
                // An inert shard: socketed but has no defined effect for this item type.
                entry.State = TooltipSocketState.InertShard;
            }
            else
            {
                continue;
            }

            sockets.Entries.Add(entry);
        }

        return sockets;
    }
}
