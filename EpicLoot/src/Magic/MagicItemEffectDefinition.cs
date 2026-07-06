using EpicLoot.Crafting;
using EpicLoot.GatedItemType;
using EpicLoot.General;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using static ItemDrop;

namespace EpicLoot
{
    [Serializable]
    public class MagicItemEffectRequirements
    {
        public bool NoRoll = false; // If true, this effect cant be modified with a rune
        public bool ExclusiveSelf = true;
        public List<string> ExclusiveEffectTypes = new List<string>();
        public List<string> MustHaveEffectTypes = new List<string>();
        public List<string> AllowedItemTypes = new List<string>();
        public List<string> ExcludedItemTypes = new List<string>();
        public List<ItemRarity> AllowedRarities = new List<ItemRarity>();
        public List<ItemRarity> ExcludedRarities = new List<ItemRarity>();
        public List<Skills.SkillType> AllowedSkillTypes = new List<Skills.SkillType>();
        public List<Skills.SkillType> ExcludedSkillTypes = new List<Skills.SkillType>();
        public List<string> AllowedItemNames = new List<string>();
        public List<string> ExcludedItemNames = new List<string>();
        public bool? ItemHasPhysicalDamage;
        public bool? ItemHasElementalDamage;
        public bool? ItemHasChopDamage;
        public bool? ItemUsesDurability;
        public bool? ItemHasNegativeMovementSpeedModifier;
        public bool? ItemHasBlockPower;
        public bool? ItemHasParryPower;
        public bool? ItemHasNoParryPower;
        public bool? ItemHasArmor;
        public bool? ItemHasBackstabBonus;
        public bool? ItemUsesStaminaOnAttack;
        public bool? ItemUsesEitrOnAttack;
        public bool? ItemUsesHealthOnAttack;
        public bool? ItemUsesDrawStaminaOnAttack;

        public List<string> CustomFlags;

        public bool AllowByItemType([NotNull] ItemDrop.ItemData itemData)
        {
            if (AllowedItemTypes == null)
                return true;

            if (AllowedItemTypes.Count == 0)
                return true;

            if (AllowedByItemInfoType(itemData))
                return true;

            var itemIsStaff = itemData.m_shared.m_skillType == Skills.SkillType.BloodMagic ||
                itemData.m_shared.m_skillType == Skills.SkillType.ElementalMagic;
            if (itemIsStaff && AllowedItemTypes.Contains("Staff"))
                return true;

            return AllowedItemTypes.Contains(itemData.m_shared.m_itemType.ToString());
        }

        public bool AllowedByItemInfoType(ItemDrop.ItemData itemData)
        {
            var prefabName = string.Empty;
            if (itemData.m_dropPrefab?.name != null)
                prefabName = itemData.m_dropPrefab.name;

            var typeName = !string.IsNullOrEmpty(prefabName) && GatedItemTypeHelper.AllItemsWithDetails.TryGetValue(prefabName,
                out var itemTypeInfo) ? itemTypeInfo.Type : null;

            return !string.IsNullOrEmpty(typeName) && AllowedItemTypes.Contains(typeName);
        }

        public bool ExcludeByItemType([NotNull] ItemDrop.ItemData itemData)
        {
            if (ExcludedItemTypes == null)
                return false;

            if (ExcludedItemTypes.Count == 0)
                return false;

            if (ExcludedByItemInfoType(itemData))
                return false;

            var itemIsStaff = itemData.m_shared.m_skillType == Skills.SkillType.BloodMagic ||
                itemData.m_shared.m_skillType == Skills.SkillType.ElementalMagic;
            if (itemIsStaff && ExcludedItemTypes.Contains("Staff"))
                return true;

            return ExcludedItemTypes.Contains(itemData.m_shared.m_itemType.ToString());
        }

        public bool ExcludedByItemInfoType(ItemDrop.ItemData itemData)
        {
            string prefabName = "";
            if (itemData.m_dropPrefab?.name != null)
                prefabName = itemData.m_dropPrefab.name;

            var typeName = !string.IsNullOrEmpty(prefabName) &&
                GatedItemTypeHelper.AllItemsWithDetails.TryGetValue(prefabName, out var itemTypeInfo) ?
                itemTypeInfo.Type : null;

            return !string.IsNullOrEmpty(typeName) && ExcludedItemTypes.Contains(typeName);
        }

        public bool CheckRequirements([NotNull] ItemDrop.ItemData itemData, [NotNull] MagicItem magicItem, string magicEffectType = null, bool checklootroll = true, bool checkaugmentroll = false, bool checkruneroll = false)
        {
            if (checklootroll && NoRoll) {
                return false;
            }

            if (ExclusiveSelf && magicItem.HasEffect(magicEffectType))
            {
                return false;
            }

            if (ExclusiveEffectTypes?.Count > 0 && magicItem.HasAnyEffect(ExclusiveEffectTypes))
            {
                return false;
            }

            if (MustHaveEffectTypes?.Count > 0)
            {
                foreach(var effect in MustHaveEffectTypes)
                {
                    if (effect.Equals("Throwable", StringComparison.InvariantCultureIgnoreCase) &&
                        itemData.m_shared.m_skillType == Skills.SkillType.Spears)
                    {
                        continue;
                    }
                    else if (magicItem.HasEffect(effect))
                    {
                        continue;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            if (!AllowByItemType(itemData))
            {
                return false;
            }

            if (ExcludeByItemType(itemData))
            {
                return false;
            }

            if (AllowedRarities?.Count > 0 && !AllowedRarities.Contains(magicItem.Rarity))
            {
                return false;
            }

            if (ExcludedRarities?.Count > 0 && ExcludedRarities.Contains(magicItem.Rarity))
            {
                return false;
            }

            if (AllowedSkillTypes?.Count > 0 && !AllowedSkillTypes.Contains(itemData.m_shared.m_skillType))
            {
                return false;
            }

            if (ExcludedSkillTypes?.Count > 0 && ExcludedSkillTypes.Contains(itemData.m_shared.m_skillType))
            {
                return false;
            }

            if (AllowedItemNames?.Count > 0 && !(AllowedItemNames.Contains(itemData.m_shared.m_name) ||
                AllowedItemNames.Contains(itemData.m_dropPrefab?.name)))
            {
                return false;
            }

            if (ExcludedItemNames?.Count > 0 && (ExcludedItemNames.Contains(itemData.m_shared.m_name) ||
                ExcludedItemNames.Contains(itemData.m_dropPrefab?.name)))
            {
                return false;
            }

            if (ItemHasPhysicalDamage != null &&
                (ItemHasPhysicalDamage == itemData.m_shared.m_damages.GetTotalPhysicalDamage() <= 0))
            {
                return false;
            }

            if (ItemHasElementalDamage != null &&
                (ItemHasElementalDamage == !itemData.EpicLootHasElementalDamage()))
            {
                return false;
            }
            
            if (ItemHasChopDamage != null &&
                (ItemHasChopDamage == itemData.m_shared.m_damages.m_chop <= 0))
            {
                return false;
            }

            if (ItemUsesDurability != null &&
                (ItemUsesDurability == !itemData.m_shared.m_useDurability))
            {
                return false;
            }

            if (ItemHasNegativeMovementSpeedModifier != null &&
                (ItemHasNegativeMovementSpeedModifier == itemData.m_shared.m_movementModifier >= 0))
            {
                return false;
            }

            if (ItemHasBlockPower != null && (ItemHasBlockPower == itemData.m_shared.m_blockPower <= 0))
            {
                return false;
            }

            if (ItemHasParryPower != null && (ItemHasParryPower == itemData.m_shared.m_timedBlockBonus <= 0))
            {
                return false;
            }

            if (ItemHasNoParryPower != null && (ItemHasNoParryPower == itemData.m_shared.m_timedBlockBonus > 0))
            {
                return false;
            }

            if (ItemHasArmor != null && (ItemHasArmor == (itemData.m_shared.m_armor <= 0 || !IsArmorType(itemData.m_shared.m_itemType))))
            {
                return false;
            }

            if (ItemHasBackstabBonus != null && (ItemHasBackstabBonus == itemData.m_shared.m_backstabBonus <= 0))
            {
                return false;
            }

            if (ItemUsesStaminaOnAttack != null)
            {
                bool hasStamina = itemData.m_shared.m_attack.m_attackStamina > 0 ||
                    itemData.m_shared.m_secondaryAttack.m_attackStamina > 0;
                if (ItemUsesStaminaOnAttack.Value != hasStamina)
                {
                    return false;
                }
            }

            if (ItemUsesEitrOnAttack != null)
            {
                bool hasEitr = itemData.m_shared.m_attack.m_attackEitr > 0 ||
                    itemData.m_shared.m_attack.m_drawEitrDrain > 0 ||
                    itemData.m_shared.m_attack.m_reloadEitrDrain > 0 ||
                    itemData.m_shared.m_secondaryAttack.m_attackEitr > 0 ||
                    itemData.m_shared.m_secondaryAttack.m_drawEitrDrain > 0 ||
                    itemData.m_shared.m_secondaryAttack.m_reloadEitrDrain > 0;

                if (ItemUsesEitrOnAttack.Value != hasEitr)
                {
                    return false;
                }
            }
            
            if (ItemUsesHealthOnAttack != null)
            {
                bool usesHealth = itemData.m_shared.m_attack.m_attackHealth > 0 ||
                    itemData.m_shared.m_secondaryAttack.m_attackHealth > 0 ||
                    itemData.m_shared.m_attack.m_attackHealthPercentage > 0 ||
                    itemData.m_shared.m_secondaryAttack.m_attackHealthPercentage > 0 ||
                    itemData.HasMagicEffect(MagicEffectType.Bloodlust);

                if (ItemUsesHealthOnAttack.Value != usesHealth)
                {
                    return false;
                }
            }

            if (ItemUsesDrawStaminaOnAttack != null)
            {
                bool drawStamina = itemData.m_shared.m_attack.m_drawStaminaDrain > 0 ||
                    itemData.m_shared.m_secondaryAttack.m_drawStaminaDrain > 0;

                if (ItemUsesDrawStaminaOnAttack.Value != drawStamina)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the item types that apply armor values for the Player.
        /// </summary>
        private bool IsArmorType(ItemData.ItemType type)
        {
            return type == ItemData.ItemType.Helmet ||
                type == ItemData.ItemType.Chest ||
                type == ItemData.ItemType.Legs ||
                type == ItemData.ItemType.Shoulder;
        }
    }

    [Serializable]
    public class MagicItemEffectDefinition
    {
        [Serializable]
        public class ValueDef
        {
            public float MinValue;
            public float MaxValue;
            public float Increment;
        }

        [Serializable]
        public class ValuesPerRarityDef
        {
            public ValueDef Magic;
            public ValueDef Rare;
            public ValueDef Epic;
            public ValueDef Legendary;
            public ValueDef Mythic;

            public ValueDef GetValueDefForRarity(ItemRarity rarity)
            {
                switch (rarity)
                {
                    case ItemRarity.Magic:
                        return Magic;
                    case ItemRarity.Rare:
                        return Rare;
                    case ItemRarity.Epic:
                        return Epic;
                    case ItemRarity.Legendary:
                        return Legendary;
                    case ItemRarity.Mythic:
                        return Mythic;
                    default:
                        EpicLoot.LogWarning($"Unknown rarity: {rarity}, returning Magic values");
                        return Magic;
                }
            }
        }

        public string Type { get; set; }

        public string DisplayText = "";
        public string Description = "";
        public MagicItemEffectRequirements Requirements = new MagicItemEffectRequirements();
        public ValuesPerRarityDef ValuesPerRarity = new ValuesPerRarityDef();
        public float SelectionWeight = 1;
        public bool CanBeAugmented = true;
        public bool CanBeDisenchanted = true;
        public bool CanBeRunified = true;
        public string Comment;
        public List<string> Prefixes = new List<string>();
        public List<string> Suffixes = new List<string>();
        public string EquipFx;
        public FxAttachMode EquipFxMode = FxAttachMode.Player;
        public string Ability;
        public Dictionary<string, float> Config = new Dictionary<string, float>();

        public string GetDescriptionTextWithConfig() {
            string description = Description;
            if (Config != null && Config.Count > 0) {
                foreach(var kvp in Config) {
                    description += $" {kvp.Key}: {kvp.Value}";
                }
            }
            return description;
        }

        public List<string> GetAllowedItemTypes()
        {
            return Requirements?.AllowedItemTypes ?? new List<string>();
        }

        public bool CheckRequirements(ItemDrop.ItemData itemData, MagicItem magicItem, bool lootroll = true, bool augmentroll = false, bool runeroll = false)
        {
            if (Requirements == null)
            {
                return true;
            }

            return Requirements.CheckRequirements(itemData, magicItem, Type, lootroll, augmentroll, runeroll);
        }

        public bool HasRarityValues()
        {
            return ValuesPerRarity.Magic != null && ValuesPerRarity.Epic != null &&
                ValuesPerRarity.Rare != null && ValuesPerRarity.Legendary != null;
        }

        public ValueDef GetValuesForRarity(ItemRarity itemRarity)
        {
            switch (itemRarity)
            {
                case ItemRarity.Magic:
                    return ValuesPerRarity.Magic;
                case ItemRarity.Rare:
                    return ValuesPerRarity.Rare;
                case ItemRarity.Epic:
                    return ValuesPerRarity.Epic;
                case ItemRarity.Legendary:
                    return ValuesPerRarity.Legendary;
                case ItemRarity.Mythic:
                    return ValuesPerRarity.Mythic;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemRarity), itemRarity, null);
            }
        }

        public override string ToString()
        {
            return $"MagicItemEffectDefinition|{Type}";
        }
    }

    public class MagicItemEffectsList
    {
        public List<MagicItemEffectDefinition> MagicItemEffects = new List<MagicItemEffectDefinition>();
    }

    public static class MagicItemEffectDefinitions
    {
        public static Dictionary<string, MagicItemEffectDefinition> AllDefinitions =
            new Dictionary<string, MagicItemEffectDefinition>();
        public static event Action OnSetupMagicItemEffectDefinitions;

        public static void Initialize(MagicItemEffectsList config)
        {
            AllDefinitions.Clear();
            foreach (var magicItemEffectDefinition in config.MagicItemEffects)
            {
                Add(magicItemEffectDefinition);
            }
            OnSetupMagicItemEffectDefinitions?.Invoke();
        }

        public static MagicItemEffectsList GetMagicItemEffectDefinitions()
        {
            return new MagicItemEffectsList() { MagicItemEffects = AllDefinitions.Values.ToList() };
        }

        public static void Add(MagicItemEffectDefinition effectDef)
        {
            if (AllDefinitions.ContainsKey(effectDef.Type))
            {
                EpicLoot.LogWarning($"Removed previously existing magic effect type: {effectDef.Type}");
                AllDefinitions.Remove(effectDef.Type);
            }
            AllDefinitions.Add(effectDef.Type, effectDef);
        }

        public static MagicItemEffectDefinition Get(string type)
        {
            AllDefinitions.TryGetValue(type, out MagicItemEffectDefinition effectDef);
            if (effectDef == null) {
                EpicLoot.LogWarning($"Enchantment definition missing for: {type}");
                effectDef = new MagicItemEffectDefinition() {
                    ValuesPerRarity = new MagicItemEffectDefinition.ValuesPerRarityDef() {
                        Magic = new MagicItemEffectDefinition.ValueDef() { Increment = 1, MaxValue = 10, MinValue = 1 },
                        Rare = new MagicItemEffectDefinition.ValueDef() { Increment = 2, MaxValue = 15, MinValue = 1 },
                        Epic = new MagicItemEffectDefinition.ValueDef() { Increment = 3, MaxValue = 20, MinValue = 1 },
                        Legendary = new MagicItemEffectDefinition.ValueDef() { Increment = 4, MaxValue = 25, MinValue = 1 },
                        Mythic = new MagicItemEffectDefinition.ValueDef() { Increment = 5, MaxValue = 30, MinValue = 1 }
                    },
                    Requirements = new MagicItemEffectRequirements() { NoRoll = true },
                    Type = type,
                };
            }
            return effectDef;
        }

        public static Dictionary<string, float> GetEffectConfig(string type)
        {
            AllDefinitions.TryGetValue(type, out var effectDef);
            if (effectDef != null && effectDef.Config != null) { return effectDef.Config; }
            return null;
        }

        public static List<MagicItemEffectDefinition> GetAvailableEffects(
            ItemDrop.ItemData itemData, MagicItem magicItem, int ignoreEffectIndex = -1, bool checklootroll = true, bool checkaugment = false, bool checkruneroll = false)
        {
            MagicItemEffect effect = null;
            if (ignoreEffectIndex >= 0 && ignoreEffectIndex < magicItem.Effects.Count)
            {
                effect = magicItem.Effects[ignoreEffectIndex];
                magicItem.Effects.RemoveAt(ignoreEffectIndex);
            }

            var results = AllDefinitions.Values.Where(x => x.CheckRequirements(itemData, magicItem, checklootroll, checkaugment, checkruneroll) &&
                !EnchantCostsHelper.EffectIsDeprecated(x)).ToList();

            if (effect != null)
            {
                magicItem.Effects.Insert(ignoreEffectIndex, effect);
                if (AllDefinitions.TryGetValue(effect.EffectType, out var ignoredEffectDef))
                {
                    if (!results.Contains(ignoredEffectDef) && !EnchantCostsHelper.EffectIsDeprecated(ignoredEffectDef))
                    {
                        results.Add(ignoredEffectDef);
                    }
                }
            }

            return results;
        }

        public static bool IsValuelessEffect(string effectType, ItemRarity rarity)
        {
            var effectDef = Get(effectType);
            if (effectDef == null)
            {
                EpicLoot.LogWarning($"Checking if unknown effect is valuless ({effectType}/{rarity})");
                return false;
            }

            return effectDef.GetValuesForRarity(rarity) == null;
        }
    }
}
