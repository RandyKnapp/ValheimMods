using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot.LegendarySystem;
using EpicLoot.MagicItemEffects;
using UnityEngine;

namespace EpicLoot;

// The set section of a set piece (vanilla sets and Epic Loot's legendary sets) and the description of the set
// a set rune carries.
public partial class TooltipGatherer
{
    private void GatherSets()
    {
        try
        {
            GatherMundaneSet();
            GatherMagicSet();
        }
        catch (Exception e)
        {
            EpicLoot.LogWarning($"[TooltipGatherer] Set section failed for {item.m_shared?.m_name} - {e.Message}");
        }
    }

    private void GatherMundaneSet()
    {
        if (!string.IsNullOrEmpty(item.m_shared.m_setName))
        {
            GatherListedSet(item.m_shared.m_setName, item.m_shared.m_setSize, true);
        }
    }

    private void GatherMagicSet()
    {
        string setID = item.GetSetID(out bool isMundane);
        if (isMundane)
        {
            return;
        }

        LegendarySetInfo setInfo = item.GetLegendarySetInfo();
        if (setInfo != null)
        {
            GatherLegendarySet(setInfo);
        }
        else
        {
            // A magic set only gets here when its ID is not in legendaries.json (any more): no bonuses to list.
            GatherListedSet(setID, item.GetSetSize(setID, false), false);
        }
    }

    // A vanilla set, or an Epic Loot set id that legendaries.json no longer has.
    private void GatherListedSet(string setID, int setSize, bool isMundane)
    {
        List<ItemDrop.ItemData> currentSetEquipped = player.GetEquippedSetPieces(setID);
        TooltipSet set = new TooltipSet
        {
            SetId = setID,
            Name = GetSetDisplayName(isMundane),
            IsMundane = isMundane,
            Known = isMundane,
            Equipped = currentSetEquipped.Count,
            Full = setSize
        };

        foreach (string setItemName in GetSetPieces(setID, isMundane))
        {
            set.Pieces.Add(new TooltipSetPiece
            {
                Id = setItemName,
                Name = GetSetItemDisplayName(setItemName, isMundane),
                Equipped = IsSetItemEquipped(currentSetEquipped, setItemName, isMundane)
            });
        }

        if (isMundane)
        {
            float skillLevel = player.GetSkillLevel(item.m_shared.m_skillType);
            set.Bonuses.Add(new TooltipSetBonus
            {
                Count = setSize,
                EffectType = item.m_shared.m_setStatusEffect != null ? item.m_shared.m_setStatusEffect.name : "",
                Text = item.GetSetStatusEffectTooltip(item.m_quality, skillLevel).Replace("\n", " "),
                Active = currentSetEquipped.Count == setSize
            });
        }

        data.Sets.Add(set);
    }

    // Counts the way bonus application does (SetBonusEvaluator): equipment providers included, each piece
    // once, each bonus at its own tier.
    private void GatherLegendarySet(LegendarySetInfo setInfo)
    {
        LegendarySetProgress progress = SetBonusEvaluator.GetSetProgress(player, setInfo);
        TooltipSet set = new TooltipSet
        {
            SetId = setInfo.ID,
            Name = setInfo.Name,
            Equipped = progress.Count,
            Full = progress.FullCount
        };

        foreach (string pieceID in setInfo.LegendaryIDs.Distinct())
        {
            // An equipped piece shows in the rarity it is worn at, which is what its bonuses tier by.
            bool equipped = progress.EquippedPieces.TryGetValue(pieceID, out ItemRarity pieceRarity);
            set.Pieces.Add(new TooltipSetPiece
            {
                Id = pieceID,
                Name = GetSetItemDisplayName(pieceID, false),
                Equipped = equipped,
                Rarity = equipped ? (int)pieceRarity : -1
            });
        }

        ItemRarity itemRarity = item.GetRarity();
        foreach (SetBonusInfo setBonusInfo in setInfo.SetBonuses.OrderBy(x => x.Count))
        {
            if (setBonusInfo.Effect == null)
            {
                continue;
            }

            if (!MagicItemEffectDefinitions.TryGet(setBonusInfo.Effect.Type, out MagicItemEffectDefinition effectDef))
            {
                EpicLoot.LogError($"Set Tooltip: Could not find effect ({setBonusInfo.Effect.Type}) " +
                    $"for set ({setInfo.ID}) bonus ({setBonusInfo.Count})!");
                continue;
            }

            // An inactive bonus previews the value it would have at the hovered item's rarity.
            ItemRarity? tier = progress.GetTier(setBonusInfo);
            float value = SetBonusEvaluator.GetBonusValue(setBonusInfo, tier ?? itemRarity);

            // Details: each bonus's description, as for the item's own effects. No range, since a bonus has one
            // value per tier, and no Config lines: the description states the numbers that matter, and the bigger
            // bonuses carry a dozen tuning keys.
            set.Bonuses.Add(new TooltipSetBonus
            {
                Count = setBonusInfo.Count,
                EffectType = setBonusInfo.Effect.Type,
                Value = value,
                Text = MagicItem.GetEffectText(effectDef, value),
                Active = tier.HasValue,
                Tier = tier.HasValue ? (int)tier.Value : -1,
                // The tier only changes anything when the bonus has per-rarity values.
                TierVaries = setBonusInfo.Effect.ValuesPerRarity != null,
                Detail = includeDetails ? SetDescriptions.DescribeSetBonusDetail(effectDef, value) : null,
                ActivationKey = includeDetails ? SetAbilityKeys.DescribeActivationKey(effectDef) : null
            });
        }

        data.Sets.Add(set);
    }

    // A set rune (Rune page, Set Extract) carries a set rather than effects. It says which set, what it can be
    // etched onto and what the set grants. No worn-piece count: the rune is not a piece, and "(0/4)" would read
    // as progress.
    private void GatherSetRune()
    {
        TooltipSetRune rune = new TooltipSetRune { SetId = magicItem.SetID };
        data.SetRune = rune;

        if (!UniqueLegendaryHelper.TryGetLegendarySetInfo(magicItem.SetID, out LegendarySetInfo set))
        {
            return;
        }

        List<ItemRarity> rarities = UniqueLegendaryHelper.GetRarities(set);
        rune.Known = true;
        rune.Name = set.Name;
        rune.Rarities = SetDescriptions.FormatRarities(rarities);

        foreach (LegendaryInfo piece in UniqueLegendaryHelper.GetDefinedSetPieces(set))
        {
            string requirements = SetDescriptions.DescribePieceRequirements(piece, out bool furtherRestricted);
            rune.Pieces.Add(new TooltipSetRunePiece
            {
                Name = piece.Name,
                Requirements = requirements,
                FurtherRestricted = furtherRestricted
            });
        }

        foreach (SetBonusInfo bonus in set.SetBonuses.Where(x => x?.Effect != null).OrderBy(x => x.Count))
        {
            if (!MagicItemEffectDefinitions.TryGet(bonus.Effect.Type, out MagicItemEffectDefinition definition))
            {
                continue;
            }

            rune.Bonuses.Add(new TooltipSetBonus
            {
                Count = bonus.Count,
                EffectType = bonus.Effect.Type,
                Text = SetDescriptions.DescribeSetBonus(definition, bonus, rarities),
                Active = true,
                Detail = includeDetails ? SetDescriptions.DescribeSetBonusDetail(definition, bonus, rarities) : null,
                ActivationKey = includeDetails ? SetAbilityKeys.DescribeActivationKey(definition) : null
            });
        }
    }

    private string GetSetDisplayName(bool isMundane)
    {
        if (!isMundane)
        {
            LegendarySetInfo setInfo = item.GetLegendarySetInfo();
            return setInfo != null ? setInfo.Name : $"{item.GetSetID()}";
        }

        if (item.m_shared.m_setStatusEffect != null && !item.m_shared.m_setStatusEffect.m_name.IsNullOrWhiteSpace())
        {
            return item.m_shared.m_setStatusEffect.m_name;
        }

        if (!item.m_shared.m_setName.IsNullOrWhiteSpace())
        {
            return item.m_shared.m_setName;
        }

        return "<unknown set>";
    }

    private static List<string> GetSetPieces(string setName, bool isMundane)
    {
        if (!isMundane && UniqueLegendaryHelper.TryGetLegendarySetInfo(setName, out LegendarySetInfo setInfo))
        {
            return setInfo.LegendaryIDs;
        }

        return GetMundaneSetPieces(setName);
    }

    private static List<string> GetMundaneSetPieces(string setName)
    {
        // TODO: improve performace of this call
        List<string> results = new List<string>();
        // One entry per display token, not per prefab. Deep North registers SP_/FW_ NPC prop copies of
        // set armour (SP_ArmorTrollLeatherChest, FW_CapeTrollHide, ...) that keep the real item's
        // m_name and m_setName, so without this the troll set listed its tunic, pants and cape three times.
        HashSet<string> seen = new HashSet<string>();
        foreach (GameObject itemPrefab in ObjectDB.instance.m_items)
        {
            if (itemPrefab == null)
            {
                EpicLoot.LogError("Null Item left in ObjectDB! (This means that a prefab was deleted and not an instance)");
                continue;
            }

            // Vanilla registers some non-item prefabs in ObjectDB.m_items (Deep North added
            // PropFeastDeepNorth, SnowRoller and FrozenKing_Summon); its own lookups skip them too.
            if (!itemPrefab.TryGetComponent(out ItemDrop itemDrop))
            {
                continue;
            }

            if (itemDrop.m_itemData.m_shared.m_setName == setName && seen.Add(itemDrop.m_itemData.m_shared.m_name))
            {
                results.Add(itemDrop.m_itemData.m_shared.m_name);
            }
        }

        return results;
    }

    private static string GetSetItemDisplayName(string setItemName, bool isMundane)
    {
        if (!isMundane && UniqueLegendaryHelper.TryGetLegendaryInfo(setItemName, out LegendaryInfo itemInfo))
        {
            return itemInfo.Name;
        }

        return setItemName;
    }

    private static bool IsSetItemEquipped(List<ItemDrop.ItemData> currentSetEquipped, string setItemName, bool isMundane)
    {
        if (isMundane)
        {
            return currentSetEquipped.Find(x => x.m_shared.m_name == setItemName) != null;
        }

        return currentSetEquipped.Find(x => x.IsMagic(out MagicItem magicItem) && magicItem.LegendaryID == setItemName) != null;
    }
}
