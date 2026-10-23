using System.Collections.Generic;
using System.Linq;
using EpicLoot.General;

namespace EpicLoot.LegendarySystem
{
    /// <summary>
    /// Display text for sets and their pieces, shared by the Compendium's set page and the set rune
    /// tooltip. Returns unlocalized strings with tokens; the caller localizes the finished text.
    /// </summary>
    public static class SetDescriptions
    {
        /// <summary>
        /// What a piece can be made from, e.g. "Two-Handed Weapon (Axes)". The allowed item types, skills
        /// and item names are spelled out; any other rule on the piece (excluded types, an item-property
        /// check such as the Heimdall shield's no-parry rule, an external requirement) is only flagged
        /// through <paramref name="furtherRestricted"/>.
        /// </summary>
        public static string DescribePieceRequirements(LegendaryInfo piece, out bool furtherRestricted)
        {
            MagicItemEffectRequirements requirements = piece?.Requirements;
            furtherRestricted = requirements != null && HasFurtherRestrictions(requirements);
            if (requirements == null)
            {
                return "$mod_epicloot_setrune_anyitem";
            }

            List<string> parts = new List<string>();
            if (requirements.AllowedItemTypes?.Count > 0)
            {
                parts.Add(string.Join(", ", requirements.AllowedItemTypes.Distinct().Select(DescribeItemType)));
            }

            if (requirements.AllowedItemNames?.Count > 0)
            {
                parts.Add(string.Join(", ", requirements.AllowedItemNames.Distinct().Select(DescribeItemName)));
            }

            string skills = requirements.AllowedSkillTypes?.Count > 0
                ? string.Join(", ", requirements.AllowedSkillTypes.Distinct().Select(DescribeSkill))
                : "";

            if (parts.Count == 0)
            {
                return skills.Length > 0 ? skills : "$mod_epicloot_setrune_anyitem";
            }

            string text = string.Join(", ", parts);
            return skills.Length > 0 ? $"{text} ({skills})" : text;
        }

        /// <summary>The real value, or one value per rarity when the bonus has per-rarity overrides that differ.</summary>
        public static string DescribeSetBonus(MagicItemEffectDefinition definition, SetBonusInfo bonus, List<ItemRarity> rarities)
        {
            List<float> values = rarities.Select(r => SetBonusEvaluator.GetBonusValue(bonus, r)).ToList();
            if (values.Distinct().Count() <= 1)
            {
                return MagicItem.GetEffectText(definition, values.FirstOrDefault());
            }

            string perRarity = string.Join(" / ", rarities.Select((r, i) =>
                $"<color={EpicLoot.GetRarityColor(r)}>{values[i]:0.#}</color>"));
            return $"{MagicItem.GetEffectTextGeneric(definition, "<b><color=yellow>X</color></b>")} (X = {perRarity})";
        }

        /// <summary>
        /// The bonus's extended description at one value: the text a set tooltip shows under the bonus line while
        /// Shift is held. Empty when the effect has no description.
        /// </summary>
        public static string DescribeSetBonusDetail(MagicItemEffectDefinition definition, float value)
        {
            return string.IsNullOrEmpty(definition.Description) ? "" : MagicItem.GetEffectDescription(definition, value);
        }

        /// <summary>
        /// The extended description across the set's rarities, filled in the way <see cref="DescribeSetBonus"/>
        /// fills the line above it: the real value when every rarity agrees, its X otherwise.
        /// </summary>
        public static string DescribeSetBonusDetail(MagicItemEffectDefinition definition, SetBonusInfo bonus, List<ItemRarity> rarities)
        {
            if (string.IsNullOrEmpty(definition.Description))
            {
                return "";
            }

            List<float> values = rarities.Select(r => SetBonusEvaluator.GetBonusValue(bonus, r)).Distinct().ToList();
            return values.Count <= 1
                ? MagicItem.GetEffectDescription(definition, values.FirstOrDefault())
                : MagicItem.GetEffectDescriptionGeneric(definition, "<b><color=yellow>X</color></b>");
        }

        /// <summary>The rarities, each in its own color, comma separated.</summary>
        public static string FormatRarities(IEnumerable<ItemRarity> rarities)
        {
            return string.Join(", ", rarities.Select(r =>
                $"<color={EpicLoot.GetRarityColor(r)}>{EpicLoot.GetRarityDisplayName(r)}</color>"));
        }

        private static bool HasFurtherRestrictions(MagicItemEffectRequirements r)
        {
            return r.ExcludedItemTypes?.Count > 0 || r.ExcludedSkillTypes?.Count > 0 || r.ExcludedItemNames?.Count > 0 ||
                r.AllowedRarities?.Count > 0 || r.ExcludedRarities?.Count > 0 ||
                r.ExternalRequirements?.Count > 0 || r.CustomFlags?.Count > 0 ||
                r.ItemHasPhysicalDamage.HasValue || r.ItemHasElementalDamage.HasValue || r.ItemHasChopDamage.HasValue ||
                r.ItemUsesDurability.HasValue || r.ItemHasNegativeMovementSpeedModifier.HasValue ||
                r.ItemHasBlockPower.HasValue || r.ItemHasParryPower.HasValue || r.ItemHasNoParryPower.HasValue ||
                r.ItemHasArmor.HasValue || r.ItemHasBackstabBonus.HasValue || r.ItemUsesStaminaOnAttack.HasValue ||
                r.ItemUsesEitrOnAttack.HasValue || r.ItemUsesHealthOnAttack.HasValue ||
                r.ItemUsesDrawStaminaOnAttack.HasValue || r.ItemGivesAdrenaline.HasValue || r.ItemHasAdrenaline.HasValue;
        }

        // An ItemType enum name has a display token; an iteminfo.json type or "Staff" is shown as written.
        private static string DescribeItemType(string itemType)
        {
            return Extensions.TryLocalize($"mod_epicloot_itemtype_{itemType.ToLowerInvariant()}", out string localized)
                ? localized
                : itemType;
        }

        private static string DescribeSkill(Skills.SkillType skill)
        {
            return Extensions.TryLocalize($"skill_{skill.ToString().ToLowerInvariant()}", out string localized)
                ? localized
                : skill.ToString();
        }

        // Requirements match on the item's shared name token; a prefab name is resolved to its token when
        // the prefab exists.
        private static string DescribeItemName(string itemName)
        {
            if (string.IsNullOrEmpty(itemName) || itemName.StartsWith("$"))
            {
                return itemName;
            }

            ItemDrop itemDrop = ObjectDB.instance?.GetItemPrefab(itemName)?.GetComponent<ItemDrop>();
            return itemDrop != null ? itemDrop.m_itemData.m_shared.m_name : itemName;
        }
    }
}
