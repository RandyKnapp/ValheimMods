using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtendedItemDataFramework;
using JetBrains.Annotations;
using EpicLoot.LegendarySystem;

namespace EpicLoot
{
    [Serializable]
    public class MagicItemEffectRequirements
    {
        private static StringBuilder _sb = new StringBuilder();
        private static List<string> _flags = new List<string>();

        public bool NoRoll;
        public bool ExclusiveSelf = true;
        public List<string> ExclusiveEffectTypes = new List<string>();
        public List<ItemDrop.ItemData.ItemType> AllowedItemTypes = new List<ItemDrop.ItemData.ItemType>();
        public List<ItemDrop.ItemData.ItemType> ExcludedItemTypes = new List<ItemDrop.ItemData.ItemType>();
        public List<ItemRarity> AllowedRarities = new List<ItemRarity>();
        public List<ItemRarity> ExcludedRarities = new List<ItemRarity>();
        public List<Skills.SkillType> AllowedSkillTypes = new List<Skills.SkillType>();
        public List<Skills.SkillType> ExcludedSkillTypes = new List<Skills.SkillType>();
        public List<string> AllowedItemNames = new List<string>();
        public List<string> ExcludedItemNames = new List<string>();
        public bool ItemHasPhysicalDamage;
        public bool ItemHasElementalDamage;
        public bool ItemUsesDurability;
        public bool ItemHasNegativeMovementSpeedModifier;
        public bool ItemHasBlockPower;
        public bool ItemHasParryPower;
        public bool ItemHasNoParryPower;
        public bool ItemHasArmor;
        public bool ItemHasBackstabBonus;
        public bool ItemUsesStaminaOnAttack;

        public List<string> CustomFlags;

        public override string ToString()
        {
            _sb.Clear();
            _flags.Clear();

            if (NoRoll) _flags.Add(nameof(NoRoll));
            if (ExclusiveSelf) _flags.Add(nameof(ExclusiveSelf));
            if (ItemHasPhysicalDamage) _flags.Add(nameof(ItemHasPhysicalDamage));
            if (ItemHasElementalDamage) _flags.Add(nameof(ItemHasElementalDamage));
            if (ItemUsesDurability) _flags.Add(nameof(ItemUsesDurability));
            if (ItemHasNegativeMovementSpeedModifier) _flags.Add(nameof(ItemHasNegativeMovementSpeedModifier));
            if (ItemHasBlockPower) _flags.Add(nameof(ItemHasBlockPower));
            if (ItemHasParryPower) _flags.Add(nameof(ItemHasParryPower));
            if (ItemHasNoParryPower) _flags.Add(nameof(ItemHasNoParryPower));
            if (ItemHasArmor) _flags.Add(nameof(ItemHasArmor));
            if (ItemHasBackstabBonus) _flags.Add(nameof(ItemHasBackstabBonus));
            if (ItemUsesStaminaOnAttack) _flags.Add(nameof(ItemUsesStaminaOnAttack));

            if (_flags.Count > 0)
            {
                _sb.AppendLine($"> > **Flags:** `{string.Join(", ", _flags)}`");
            }

            if (ExclusiveEffectTypes != null && ExclusiveEffectTypes.Count > 0)
            {
                _sb.AppendLine($"> > **ExclusiveEffectTypes:** `{string.Join(", ", ExclusiveEffectTypes)}`");
            }

            if (AllowedItemTypes != null && AllowedItemTypes.Count > 0)
            {
                _sb.AppendLine($"> > **AllowedItemTypes:** `{string.Join(", ", AllowedItemTypes)}`");
            }

            if (ExcludedItemTypes != null && ExcludedItemTypes.Count > 0)
            {
                _sb.AppendLine($"> > **ExcludedItemTypes:** `{string.Join(", ", ExcludedItemTypes)}`");
            }

            if (AllowedRarities != null && AllowedRarities.Count > 0)
            {
                _sb.AppendLine($"> > **AllowedRarities:** `{string.Join(", ", AllowedRarities)}`");
            }

            if (ExcludedRarities != null && ExcludedRarities.Count > 0)
            {
                _sb.AppendLine($"> > **ExcludedRarities:** `{string.Join(", ", ExcludedRarities)}`");
            }

            if (AllowedSkillTypes != null && AllowedSkillTypes.Count > 0)
            {
                _sb.AppendLine($"> > **AllowedSkillTypes:** `{string.Join(", ", AllowedSkillTypes)}`");
            }

            if (ExcludedSkillTypes != null && ExcludedSkillTypes.Count > 0)
            {
                _sb.AppendLine($"> > **ExcludedSkillTypes:** `{string.Join(", ", ExcludedSkillTypes)}`");
            }

            if (AllowedItemNames != null && AllowedItemNames.Count > 0)
            {
                _sb.AppendLine($"> > **AllowedItemNames:** `{string.Join(", ", AllowedItemNames)}`");
            }

            if (ExcludedItemNames != null && ExcludedItemNames.Count > 0)
            {
                _sb.AppendLine($"> > **ExcludedItemNames:** `{string.Join(", ", ExcludedItemNames)}`");
            }

            if (CustomFlags != null && CustomFlags.Count > 0)
            {
                _sb.AppendLine($"> > **CustomFlags:** `{string.Join(", ", CustomFlags)}`");
            }

            return _sb.ToString();
        }

        public bool CheckRequirements([NotNull] ItemDrop.ItemData itemData, [NotNull] MagicItem magicItem, string magicEffectType = null)
        {
            if (NoRoll)
            {
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

            if (AllowedItemTypes?.Count > 0 && !AllowedItemTypes.Contains(itemData.m_shared.m_itemType))
            {
                return false;
            }

            if (ExcludedItemTypes?.Count > 0 && ExcludedItemTypes.Contains(itemData.m_shared.m_itemType))
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

            if (AllowedItemNames?.Count > 0 && !AllowedItemNames.Contains(itemData.m_shared.m_name))
            {
                return false;
            }

            if (ExcludedItemNames?.Count > 0 && ExcludedItemNames.Contains(itemData.m_shared.m_name))
            {
                return false;
            }

            if (ItemHasPhysicalDamage && itemData.m_shared.m_damages.GetTotalPhysicalDamage() <= 0)
            {
                return false;
            }

            if (ItemHasElementalDamage && itemData.m_shared.m_damages.GetTotalElementalDamage() <= 0)
            {
                return false;
            }

            if (ItemUsesDurability && !itemData.m_shared.m_useDurability)
            {
                return false;
            }

            if (ItemHasNegativeMovementSpeedModifier && itemData.m_shared.m_movementModifier >= 0)
            {
                return false;
            }

            if (ItemHasBlockPower && itemData.m_shared.m_blockPower <= 0)
            {
                return false;
            }

            if (ItemHasParryPower && itemData.m_shared.m_deflectionForce <= 0)
            {
                return false;
            }

            if (ItemHasNoParryPower && itemData.m_shared.m_deflectionForce > 0)
            {
                return false;
            }

            if (ItemHasArmor && itemData.m_shared.m_armor <= 0)
            {
                return false;
            }

            if (ItemHasBackstabBonus && itemData.m_shared.m_backstabBonus <= 0)
            {
                return false;
            }

            if (ItemUsesStaminaOnAttack && itemData.m_shared.m_attack.m_attackStamina <= 0 && itemData.m_shared.m_secondaryAttack.m_attackStamina <= 0)
            {
                return false;
            }

            return true;
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
        }

        public string Type { get; set; }

        public string DisplayText = "";
        public string Description = "";
        public MagicItemEffectRequirements Requirements = new MagicItemEffectRequirements();
        public ValuesPerRarityDef ValuesPerRarity = new ValuesPerRarityDef();
        public float SelectionWeight = 1;
        public bool CanBeAugmented = true;
        public string Comment;
        public List<string> Prefixes = new List<string>();
        public List<string> Suffixes = new List<string>();
        public string EquipFx;
        public FxAttachMode EquipFxMode = FxAttachMode.Player;
        public string Ability;

        public List<ItemDrop.ItemData.ItemType> GetAllowedItemTypes()
        {
            return Requirements?.AllowedItemTypes ?? new List<ItemDrop.ItemData.ItemType>();
        }

        public bool CheckRequirements(ExtendedItemData itemData, MagicItem magicItem)
        {
            if (Requirements == null)
            {
                return true;
            }

            return Requirements.CheckRequirements(itemData, magicItem, Type);
        }

        public bool HasRarityValues()
        {
            return ValuesPerRarity.Magic != null && ValuesPerRarity.Epic != null && ValuesPerRarity.Rare != null && ValuesPerRarity.Legendary != null;
        }

        public ValueDef GetValuesForRarity(ItemRarity itemRarity,string LegendaryID = null)
        {
            ValueDef values = null;
            if (LegendaryID != null) {
                values = UniqueLegendaryHelper.GetLegendaryEffectValues(LegendaryID, Type);
            }

            if(values == null){
                switch (itemRarity) {
                    case ItemRarity.Magic: values = ValuesPerRarity.Magic;break;
                    case ItemRarity.Rare: values = ValuesPerRarity.Rare;break;
                    case ItemRarity.Epic: values = ValuesPerRarity.Epic;break;
                    case ItemRarity.Legendary: values = ValuesPerRarity.Legendary;break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(itemRarity), itemRarity, null);
                }
            }
            return values;
        }
    }

    public class MagicItemEffectsList
    {
        public List<MagicItemEffectDefinition> MagicItemEffects = new List<MagicItemEffectDefinition>();
    }

    public static class MagicItemEffectDefinitions
    {
        public static readonly Dictionary<string, MagicItemEffectDefinition> AllDefinitions = new Dictionary<string, MagicItemEffectDefinition>();
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

        public static void Add(MagicItemEffectDefinition effectDef)
        {
            if (AllDefinitions.ContainsKey(effectDef.Type))
            {
                EpicLoot.LogWarning($"Removed previously existing magic effect type: {effectDef.Type}");
                AllDefinitions.Remove(effectDef.Type);
            }

            EpicLoot.Log($"Added MagicItemEffect: {effectDef.Type}");
            AllDefinitions.Add(effectDef.Type, effectDef);
        }

        public static MagicItemEffectDefinition Get(string type)
        {
            AllDefinitions.TryGetValue(type, out MagicItemEffectDefinition effectDef);
            return effectDef;
        }

        public static List<MagicItemEffectDefinition> GetAvailableEffects(ExtendedItemData itemData, MagicItem magicItem, int ignoreEffectIndex = -1)
        {
            MagicItemEffect effect = null;
            if (ignoreEffectIndex >= 0 && ignoreEffectIndex < magicItem.Effects.Count)
            {
                effect = magicItem.Effects[ignoreEffectIndex];
                magicItem.Effects.RemoveAt(ignoreEffectIndex);
            }

            var results = AllDefinitions.Values.Where(x => x.CheckRequirements(itemData, magicItem)).ToList();

            if (effect != null)
            {
                magicItem.Effects.Insert(ignoreEffectIndex, effect);
                if (AllDefinitions.TryGetValue(effect.EffectType, out var ignoredEffectDef))
                {
                    if (!results.Contains(ignoredEffectDef))
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
