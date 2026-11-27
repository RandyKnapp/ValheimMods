using EpicLoot.Crafting;
using EpicLoot.GatedItemType;
using EpicLoot.General;
using EpicLoot.LegendarySystem;
using EpicLoot.MagicItemEffects;
using EpicLoot.ShardStones;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using static ItemDrop;
using System.Text.RegularExpressions;

namespace EpicLoot
{
    // Why a requirement check failed, so callers (e.g. shard socketing) can give specific feedback
    // instead of a single generic "not allowed" message. Other is the catch-all for any failure path
    // that wasn't explicitly categorized, including checks added to CheckRequirements later.
    public enum RequirementFailure
    {
        None,
        Other,
        NoRoll,
        ConflictingEffect,
        MissingRequiredEffect,
        ItemTypeNotAllowed,
        RarityNotAllowed,
        ItemPropertyMismatch
    }

    [Serializable]
    public class MagicItemEffectRequirements
    {
        public bool NoRoll = false; // If true, no check made with checklootroll accepts this effect
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
        [JsonConverter(typeof(StaffRoleListConverter))]
        public List<StaffRole> AllowedStaffRoles = new List<StaffRole>();
        public bool? ItemHasPhysicalDamage;
        public bool? ItemHasElementalDamage;
        public bool? ItemHasNonSpiritElementalDamage;
        public bool? ItemHasChopDamage;
        public bool? ItemHasBluntDamage;
        public bool? ItemHasSlashDamage;
        public bool? ItemHasPierceDamage;
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
        public bool? ItemGivesAdrenaline;
        public bool? ItemHasAdrenaline;

        public List<string> CustomFlags;
        public List<string> ExternalRequirements;

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

        // Deliberately the CONFIGURED type only, not ItemTypeClassifier.GetItemInfoType: this gates
        // loot/augment rolls, and letting the raw-field heuristic answer would newly subject every
        // item missing from iteminfo.json to type requirements it currently escapes.
        public bool AllowedByItemInfoType(ItemDrop.ItemData itemData)
        {
            return ItemTypeClassifier.TryGetConfiguredType(itemData, out var typeName) &&
                AllowedItemTypes.Contains(typeName);
        }

        public bool ExcludeByItemType([NotNull] ItemDrop.ItemData itemData)
        {
            if (ExcludedItemTypes == null)
                return false;

            if (ExcludedItemTypes.Count == 0)
                return false;

            if (ExcludedByItemInfoType(itemData))
                return true;

            var itemIsStaff = itemData.m_shared.m_skillType == Skills.SkillType.BloodMagic ||
                itemData.m_shared.m_skillType == Skills.SkillType.ElementalMagic;
            if (itemIsStaff && ExcludedItemTypes.Contains("Staff"))
                return true;

            return ExcludedItemTypes.Contains(itemData.m_shared.m_itemType.ToString());
        }

        // Configured type only, for the same reason as AllowedByItemInfoType above.
        public bool ExcludedByItemInfoType(ItemDrop.ItemData itemData)
        {
            return ItemTypeClassifier.TryGetConfiguredType(itemData, out var typeName) &&
                ExcludedItemTypes.Contains(typeName);
        }

        // Whether two different effects may not share an item: either definition lists the other among its
        // ExclusiveEffectTypes. The configs carry one-sided pairs (every damage effect lists
        // ModifyDamageLowHealth, which does not list them back), so both sides are read.
        public static bool EffectsExclude(string effectType, string otherType)
        {
            if (effectType == null || otherType == null || effectType == otherType)
            {
                return false;
            }

            return ListsAsExclusive(effectType, otherType) || ListsAsExclusive(otherType, effectType);
        }

        // The first effect in `effects`, other than `effectType` itself, whose definition lists `effectType`
        // as exclusive; null when there is none.
        public static string FindEffectExcluding(IEnumerable<MagicItemEffect> effects, string effectType)
        {
            if (effectType == null || effects == null)
            {
                return null;
            }

            foreach (var effect in effects)
            {
                if (effect != null && effect.EffectType != effectType && ListsAsExclusive(effect.EffectType, effectType))
                {
                    return effect.EffectType;
                }
            }

            return null;
        }

        private static bool ListsAsExclusive(string effectType, string listedType)
        {
            return MagicItemEffectDefinitions.TryGet(effectType, out var def) &&
                def.Requirements?.ExclusiveEffectTypes?.Contains(listedType) == true;
        }

        public bool CheckRequirements([NotNull] ItemDrop.ItemData itemData, [NotNull] MagicItem magicItem, string magicEffectType = null, bool checklootroll = true, bool checkaugmentroll = false, bool checkruneroll = false, bool checkItemTypeGating = true)
        {
            return CheckRequirements(itemData, magicItem, out _, out _, magicEffectType, checklootroll, checkaugmentroll, checkruneroll, checkItemTypeGating);
        }

        // Also reports why it failed. For ConflictingEffect, `conflictEffectType` names the offending effect
        // already on the item. checkItemTypeGating false skips the host-item gating (CheckItemTypeRequirements)
        // and keeps the effect-composition rules: shard socketing passes it, because the shard grid decides
        // placement there. `socketedEffectTypes` are the effects the item's other sockets grant; they meet
        // MustHaveEffectTypes (a Recall rune beside a Throwable rune) and nothing else, since exclusivity and
        // duplicates between sockets are ShardSocketManager's.
        public bool CheckRequirements([NotNull] ItemDrop.ItemData itemData, [NotNull] MagicItem magicItem,
            out RequirementFailure failure, out string conflictEffectType, string magicEffectType = null,
            bool checklootroll = true, bool checkaugmentroll = false, bool checkruneroll = false, bool checkItemTypeGating = true,
            ICollection<string> socketedEffectTypes = null)
        {
            failure = RequirementFailure.Other;
            conflictEffectType = null;

            if (checklootroll && NoRoll) {
                failure = RequirementFailure.NoRoll;
                return false;
            }

            // A different effect that excludes this one, checked before the same-effect rule so a caller that
            // waives "the item already has this effect" (shard socketing) still sees it. Exclusion is mutual:
            // an effect already on the item that lists this one blocks it as well, even when this effect's
            // own list does not name it back.
            var conflict = ExclusiveEffectTypes?.FirstOrDefault(t => t != magicEffectType && magicItem.HasEffect(t))
                ?? FindEffectExcluding(magicItem.Effects, magicEffectType);
            if (conflict != null)
            {
                failure = RequirementFailure.ConflictingEffect;
                conflictEffectType = conflict;
                return false;
            }

            if ((ExclusiveSelf || ExclusiveEffectTypes?.Contains(magicEffectType) == true) &&
                magicItem.HasEffect(magicEffectType))
            {
                failure = RequirementFailure.ConflictingEffect;
                conflictEffectType = magicEffectType;
                return false;
            }

            if (MustHaveEffectTypes?.Count > 0)
            {
                foreach (var effect in MustHaveEffectTypes)
                {
                    // Vanilla spears already throw.
                    var spearThrows = effect.Equals("Throwable", StringComparison.InvariantCultureIgnoreCase) &&
                        itemData.m_shared.m_skillType == Skills.SkillType.Spears;
                    if (!spearThrows && !magicItem.HasEffect(effect) && socketedEffectTypes?.Contains(effect) != true)
                    {
                        failure = RequirementFailure.MissingRequiredEffect;
                        return false;
                    }
                }
            }

            if (checkItemTypeGating && !CheckItemTypeRequirements(itemData, magicItem, out failure))
            {
                return false;
            }

            // Other mods' predicates (API.RegisterMagicEffectRequirement) hold on every path, shard socketing
            // included.
            if (!API.CheckMagicEffectExternalRequirements(ExternalRequirements, itemData, magicItem,
                magicEffectType, checklootroll, checkaugmentroll, checkruneroll))
            {
                failure = RequirementFailure.Other;
                return false;
            }

            failure = RequirementFailure.None;
            return true;
        }

        // Whether the effect fits this host item and rarity; false with a categorized `failure` on the first
        // unmet requirement.
        private bool CheckItemTypeRequirements([NotNull] ItemDrop.ItemData itemData, [NotNull] MagicItem magicItem,
            out RequirementFailure failure)
        {
            failure = RequirementFailure.Other;

            if (!AllowByItemType(itemData))
            {
                failure = RequirementFailure.ItemTypeNotAllowed;
                return false;
            }

            if (ExcludeByItemType(itemData))
            {
                failure = RequirementFailure.ItemTypeNotAllowed;
                return false;
            }

            if (AllowedRarities?.Count > 0 && !AllowedRarities.Contains(magicItem.Rarity))
            {
                failure = RequirementFailure.RarityNotAllowed;
                return false;
            }

            if (ExcludedRarities?.Count > 0 && ExcludedRarities.Contains(magicItem.Rarity))
            {
                failure = RequirementFailure.RarityNotAllowed;
                return false;
            }

            if (AllowedSkillTypes?.Count > 0 && !AllowedSkillTypes.Contains(itemData.m_shared.m_skillType))
            {
                failure = RequirementFailure.ItemTypeNotAllowed;
                return false;
            }

            if (ExcludedSkillTypes?.Count > 0 && ExcludedSkillTypes.Contains(itemData.m_shared.m_skillType))
            {
                failure = RequirementFailure.ItemTypeNotAllowed;
                return false;
            }

            if (AllowedItemNames?.Count > 0 && !(AllowedItemNames.Contains(itemData.m_shared.m_name) ||
                AllowedItemNames.Contains(itemData.m_dropPrefab?.name)))
            {
                failure = RequirementFailure.ItemTypeNotAllowed;
                return false;
            }

            if (ExcludedItemNames?.Count > 0 && (ExcludedItemNames.Contains(itemData.m_shared.m_name) ||
                ExcludedItemNames.Contains(itemData.m_dropPrefab?.name)))
            {
                failure = RequirementFailure.ItemTypeNotAllowed;
                return false;
            }

            if (AllowedStaffRoles?.Count > 0 && StaffRoles.IsStaff(itemData) &&
                !StaffRoles.HasAnyRole(itemData, AllowedStaffRoles))
            {
                failure = RequirementFailure.ItemTypeNotAllowed;
                return false;
            }

            if (ItemHasPhysicalDamage != null &&
                (ItemHasPhysicalDamage == itemData.m_shared.m_damages.GetTotalPhysicalDamage() <= 0))
            {
                failure = RequirementFailure.ItemPropertyMismatch;
                return false;
            }

            if (ItemHasElementalDamage != null &&
                (ItemHasElementalDamage == !itemData.EpicLootHasElementalDamage()))
            {
                failure = RequirementFailure.ItemPropertyMismatch;
                return false;
            }

            if (ItemHasNonSpiritElementalDamage != null &&
                (ItemHasNonSpiritElementalDamage == !HasNonSpiritElementalDamage(itemData)))
            {
                failure = RequirementFailure.ItemPropertyMismatch;
                return false;
            }

            if (ItemHasChopDamage != null &&
                (ItemHasChopDamage == itemData.m_shared.m_damages.m_chop <= 0))
            {
                failure = RequirementFailure.ItemPropertyMismatch;
                return false;
            }

            if (ItemHasBluntDamage != null &&
                (ItemHasBluntDamage == itemData.m_shared.m_damages.m_blunt <= 0))
            {
                failure = RequirementFailure.ItemPropertyMismatch;
                return false;
            }

            if (ItemHasSlashDamage != null &&
                (ItemHasSlashDamage == itemData.m_shared.m_damages.m_slash <= 0))
            {
                failure = RequirementFailure.ItemPropertyMismatch;
                return false;
            }

            if (ItemHasPierceDamage != null &&
                (ItemHasPierceDamage == itemData.m_shared.m_damages.m_pierce <= 0))
            {
                failure = RequirementFailure.ItemPropertyMismatch;
                return false;
            }

            if (ItemUsesDurability != null &&
                (ItemUsesDurability == !itemData.m_shared.m_useDurability))
            {
                failure = RequirementFailure.ItemPropertyMismatch;
                return false;
            }

            if (ItemHasNegativeMovementSpeedModifier != null &&
                (ItemHasNegativeMovementSpeedModifier == itemData.m_shared.m_movementModifier >= 0))
            {
                failure = RequirementFailure.ItemPropertyMismatch;
                return false;
            }

            if (ItemHasBlockPower != null && (ItemHasBlockPower == itemData.m_shared.m_blockPower <= 0))
            {
                failure = RequirementFailure.ItemPropertyMismatch;
                return false;
            }

            if (ItemHasParryPower != null && (ItemHasParryPower == itemData.m_shared.m_timedBlockBonus <= 0))
            {
                failure = RequirementFailure.ItemPropertyMismatch;
                return false;
            }

            if (ItemHasNoParryPower != null && (ItemHasNoParryPower == itemData.m_shared.m_timedBlockBonus > 0))
            {
                failure = RequirementFailure.ItemPropertyMismatch;
                return false;
            }

            if (ItemHasArmor != null && (ItemHasArmor == (itemData.m_shared.m_armor <= 0 || !IsArmorType(itemData.m_shared.m_itemType))))
            {
                failure = RequirementFailure.ItemPropertyMismatch;
                return false;
            }

            if (ItemHasBackstabBonus != null && (ItemHasBackstabBonus == itemData.m_shared.m_backstabBonus <= 0))
            {
                failure = RequirementFailure.ItemPropertyMismatch;
                return false;
            }

            if (ItemUsesStaminaOnAttack != null)
            {
                bool hasStamina = itemData.m_shared.m_attack.m_attackStamina > 0 ||
                    itemData.m_shared.m_secondaryAttack.m_attackStamina > 0;
                if (ItemUsesStaminaOnAttack.Value != hasStamina)
                {
                    failure = RequirementFailure.ItemPropertyMismatch;
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
                    failure = RequirementFailure.ItemPropertyMismatch;
                    return false;
                }
            }

            if (ItemUsesHealthOnAttack != null)
            {
                bool usesHealth = itemData.m_shared.m_attack.m_attackHealth > 0 ||
                    itemData.m_shared.m_secondaryAttack.m_attackHealth > 0 ||
                    itemData.m_shared.m_attack.m_attackHealthPercentage > 0 ||
                    itemData.m_shared.m_secondaryAttack.m_attackHealthPercentage > 0 ||
                    itemData.HasMagicEffect(MagicEffectType.Bloodlust, includeSocketed: false);

                if (ItemUsesHealthOnAttack.Value != usesHealth)
                {
                    failure = RequirementFailure.ItemPropertyMismatch;
                    return false;
                }
            }

            if (ItemUsesDrawStaminaOnAttack != null)
            {
                bool drawStamina = itemData.m_shared.m_attack.m_drawStaminaDrain > 0 ||
                    itemData.m_shared.m_secondaryAttack.m_drawStaminaDrain > 0;

                if (ItemUsesDrawStaminaOnAttack.Value != drawStamina)
                {
                    failure = RequirementFailure.ItemPropertyMismatch;
                    return false;
                }
            }

            if (ItemGivesAdrenaline != null)
            {
                // m_attackAdrenaline defaults to 1 on every attack; only above-default values count as
                // deliberate adrenaline gain.
                bool givesAdrenaline = itemData.m_shared.m_attack.m_attackAdrenaline > 1 ||
                    itemData.m_shared.m_attack.m_attackUseAdrenaline > 1 ||
                    itemData.m_shared.m_secondaryAttack.m_attackAdrenaline > 1 ||
                    itemData.m_shared.m_secondaryAttack.m_attackUseAdrenaline > 1;

                if (ItemGivesAdrenaline.Value != givesAdrenaline)
                {
                    failure = RequirementFailure.ItemPropertyMismatch;
                    return false;
                }
            }

            if (ItemHasAdrenaline != null)
            {
                bool hasAdrenaline = itemData.m_shared.m_maxAdrenaline > 0;

                if (ItemHasAdrenaline.Value != hasAdrenaline)
                {
                    failure = RequirementFailure.ItemPropertyMismatch;
                    return false;
                }
            }

            failure = RequirementFailure.None;
            return true;
        }

        // The damage Spirit imbue builds on. A staff counts what its spawned orbs and blasts carry as well.
        private static bool HasNonSpiritElementalDamage(ItemDrop.ItemData itemData)
        {
            HitData.DamageTypes damage = StaffRoles.IsStaff(itemData)
                ? StaffRoles.GetDealtDamage(itemData)
                : itemData.m_shared.m_damages;
            return damage.m_fire + damage.m_frost + damage.m_lightning + damage.m_poison > 0f;
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

            /// <summary>How many increments above MinValue a roll can land; 0 when there is no increment.</summary>
            public int GetStepCount()
            {
                // Float division lands a hair under a whole count when the step divides the range (2.6 / 0.2 is
                // 12.999999), and truncating that would leave the top value unrollable.
                return Increment > 0f ? UnityEngine.Mathf.FloorToInt((MaxValue - MinValue) / Increment + 0.001f) : 0;
            }
        }

        [Serializable]
        public class ValuesPerRarityDef
        {
            public ValueDef Magic;
            public ValueDef Rare;
            public ValueDef Epic;
            public ValueDef Legendary;
            public ValueDef Mythic;
            public ValueDef Ancient;

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
                    case ItemRarity.Ancient:
                        return Ancient;
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

        // The per-effect token exists for keys whose meaning differs between effects (Riches values).
        public string GetConfigLabel(string key) {
            var lowerKey = key.ToLowerInvariant();
            if (Extensions.TryLocalize($"mod_epicloot_me_{Type.ToLowerInvariant()}_config_{lowerKey}", out var perEffect)) {
                return perEffect;
            }

            if (Extensions.TryLocalize($"mod_epicloot_config_{lowerKey}", out var generic)) {
                return generic;
            }

            return key;
        }

        // Splits a PascalCase effect type into words: "LifeGainOnHit" -> "Life Gain On Hit". Breaks before
        // a capital that follows a lower-case letter or digit, and before the last capital of a run that
        // starts a new word ("AoEDamage" -> "AoE Damage"), so acronyms survive intact.
        private static readonly Regex PascalCaseBoundary =
            new Regex("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled);

        // The effect's short name, for places that identify an effect rather than state what it does --
        // the shardstone compendium lists a name per slot and puts the numbers in the description below
        // it. Prefers the authored token (mod_epicloot_me_<type>_name, the same per-effect convention as
        // _display/_desc) and falls back to the type name split into words, so an effect whose name has
        // not been written yet still reads as English rather than as an identifier.
        public string GetName() {
            if (!string.IsNullOrEmpty(Type) &&
                Extensions.TryLocalize($"mod_epicloot_me_{Type.ToLowerInvariant()}_name", out var localized)) {
                return localized;
            }

            return string.IsNullOrEmpty(Type) ? string.Empty : PascalCaseBoundary.Replace(Type, " ");
        }

        public bool CheckRequirements(ItemDrop.ItemData itemData, MagicItem magicItem, bool lootroll = true, bool augmentroll = false, bool runeroll = false)
        {
            if (Requirements == null)
            {
                return true;
            }

            return Requirements.CheckRequirements(itemData, magicItem, Type, lootroll, augmentroll, runeroll);
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
                case ItemRarity.Ancient:
                    return ValuesPerRarity.Ancient;
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
            // A reload may drop definitions that were present before; let each one warn again.
            FallbackDefinitions.Clear();
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
            MagicEffectRarity.InvalidateAnchor();
        }

        // Stand-ins handed out by Get() for types with no registered definition, one per type so the
        // "definition missing" warning is logged once rather than on every tooltip redraw.
        private static readonly Dictionary<string, MagicItemEffectDefinition> FallbackDefinitions =
            new Dictionary<string, MagicItemEffectDefinition>();

        /// <summary>
        /// The registered definition for <paramref name="type"/>, if there is one. Use this wherever an
        /// unknown effect must be left out (rolling, tempering, rune extraction, augmenting): unlike
        /// <see cref="Get"/> it never fabricates a stand-in.
        /// </summary>
        public static bool TryGet(string type, out MagicItemEffectDefinition effectDef)
        {
            effectDef = null;
            return type != null && AllDefinitions.TryGetValue(type, out effectDef) && effectDef != null;
        }

        /// <summary>
        /// Never null: an unknown type gets a synthesized stand-in (NoRoll, no value ranges, a display text
        /// naming the raw type and value) so display code cannot NRE on an effect whose definition was
        /// removed. Anything that acts on the definition rather than showing it should use <see cref="TryGet"/>.
        /// </summary>
        public static MagicItemEffectDefinition Get(string type)
        {
            // Registered definitions are checked first on every call, so one that arrives after a
            // fallback was handed out (a late API registration, a config reload) takes over at once.
            if (TryGet(type, out MagicItemEffectDefinition effectDef))
            {
                return effectDef;
            }

            string key = type ?? string.Empty;
            if (!FallbackDefinitions.TryGetValue(key, out effectDef))
            {
                EpicLoot.LogWarning($"Enchantment definition missing for: {type}");
                // ValueDefs stay null, which every reader treats as "no range". Braces in the type are escaped
                // for the string.Format the display text goes through.
                string escapedType = key.Replace("{", "{{").Replace("}", "}}");
                effectDef = new MagicItemEffectDefinition() {
                    DisplayText = $"$mod_epicloot_me_unknown_display {escapedType} ({{0:0.##}})",
                    Requirements = new MagicItemEffectRequirements() { NoRoll = true },
                    Type = type,
                };
                FallbackDefinitions[key] = effectDef;
            }

            return effectDef;
        }

        /// <summary>
        /// The value range an effect on an item rolls and tempers against. In order: an explicit
        /// override (the shard preview passes a zero-width one on purpose), then the item's unique's
        /// guaranteed effect at this rarity (its ValuesPerRarity entry, else its flat Values) when it
        /// declares one, then the rarity table. Stock legendaries.json gives no guaranteed effect a
        /// Values block, so a unique's effects normally land on the rarity table too. Null means the
        /// effect carries no value at that rarity.
        /// </summary>
        public static MagicItemEffectDefinition.ValueDef GetRollRange(MagicItemEffectDefinition def,
            ItemRarity rarity, string legendaryID = null, MagicItemEffectDefinition.ValueDef valuesOverride = null)
        {
            if (valuesOverride != null)
            {
                return valuesOverride;
            }

            if (def == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(legendaryID))
            {
                MagicItemEffectDefinition.ValueDef legendaryValues =
                    UniqueLegendaryHelper.GetLegendaryEffectValues(legendaryID, def.Type, rarity);
                if (legendaryValues != null)
                {
                    return legendaryValues;
                }
            }

            return def.GetValuesForRarity(rarity);
        }

        /// <summary><see cref="GetRollRange(MagicItemEffectDefinition, ItemRarity, string, MagicItemEffectDefinition.ValueDef)"/>
        /// by type. An effect with no registered definition has no range (unless overridden).</summary>
        public static MagicItemEffectDefinition.ValueDef GetRollRange(string effectType, ItemRarity rarity,
            string legendaryID = null, MagicItemEffectDefinition.ValueDef valuesOverride = null)
        {
            return TryGet(effectType, out MagicItemEffectDefinition def)
                ? GetRollRange(def, rarity, legendaryID, valuesOverride)
                : valuesOverride;
        }

        /// <summary>The roll range of <paramref name="effectType"/> on this particular item.</summary>
        public static MagicItemEffectDefinition.ValueDef GetRollRange(MagicItem magicItem, string effectType)
        {
            return magicItem == null ? null : GetRollRange(effectType, magicItem.Rarity, magicItem.LegendaryID);
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

            // A rune etch moves an effect some item already had instead of rolling a new one, so an effect
            // family switched off in the config (health critical) only leaves the rolled pools.
            bool rolling = !checkruneroll;
            var results = AllDefinitions.Values.Where(x => x.CheckRequirements(itemData, magicItem, checklootroll, checkaugment, checkruneroll) &&
                FitsSocketedEffects(x, magicItem) &&
                !EnchantCostsHelper.EffectIsDeprecated(x) && (!rolling || ModifyWithLowHealth.MayRoll(x.Type))).ToList();

            if (effect != null)
            {
                magicItem.Effects.Insert(ignoreEffectIndex, effect);
                // The replaced effect is offered back as a re-roll of its own value, so not when it has no value
                // at this rarity: that "re-roll" would change nothing. It is still left out of the checks above
                // either way, since it is the one being replaced.
                if (AllDefinitions.TryGetValue(effect.EffectType, out var ignoredEffectDef) &&
                    ignoredEffectDef.ValuesPerRarity?.GetValueDefForRarity(magicItem.Rarity) != null)
                {
                    if (!results.Contains(ignoredEffectDef) && !EnchantCostsHelper.EffectIsDeprecated(ignoredEffectDef) &&
                        (!rolling || ModifyWithLowHealth.MayRoll(ignoredEffectDef.Type)))
                    {
                        results.Add(ignoredEffectDef);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// The effects on the item, rolled or socketed, that keep <paramref name="candidate"/> off it when
        /// exclusivity is the only thing that does: with them gone it would pass every other check
        /// GetAvailableEffects makes. Empty when it fits already, or when something else (item type, rarity, a
        /// missing effect) rules it out too. The effect at <paramref name="ignoreEffectIndex"/> is the one being
        /// replaced and never counts.
        /// </summary>
        public static List<MagicItemEffect> GetExclusiveBlockers(ItemDrop.ItemData itemData, MagicItem magicItem,
            int ignoreEffectIndex, MagicItemEffectDefinition candidate, bool checklootroll = true,
            bool checkaugment = false, bool checkruneroll = false)
        {
            var blockers = new List<MagicItemEffect>();
            if (candidate == null || magicItem == null || EnchantCostsHelper.EffectIsDeprecated(candidate))
            {
                return blockers;
            }

            var requirements = candidate.Requirements;
            var exclusiveSelf = requirements != null &&
                (requirements.ExclusiveSelf || requirements.ExclusiveEffectTypes?.Contains(candidate.Type) == true);
            bool BlocksRolled(string other) => other == candidate.Type
                ? exclusiveSelf
                : MagicItemEffectRequirements.EffectsExclude(candidate.Type, other);
            bool BlocksSocket(SocketedEffect socket) => socket != null && socket.IsActive() &&
                (socket.Effect.EffectType == candidate.Type
                    ? exclusiveSelf && !ShardSocketManager.AllowMatchingItemEffect(socket.ShardType)
                    : MagicItemEffectRequirements.EffectsExclude(candidate.Type, socket.Effect.EffectType));

            var effects = magicItem.Effects;
            var sockets = magicItem.Sockets;
            var keptEffects = new List<MagicItemEffect>();
            for (var i = 0; i < effects.Count; i++)
            {
                if (i == ignoreEffectIndex)
                {
                    continue;
                }

                if (BlocksRolled(effects[i].EffectType))
                {
                    blockers.Add(effects[i]);
                }
                else
                {
                    keptEffects.Add(effects[i]);
                }
            }

            var keptSockets = new List<SocketedEffect>();
            foreach (var socket in sockets ?? new List<SocketedEffect>())
            {
                if (BlocksSocket(socket))
                {
                    blockers.Add(socket.Effect);
                }
                else
                {
                    keptSockets.Add(socket);
                }
            }

            if (blockers.Count == 0)
            {
                return blockers;
            }

            bool fits;
            magicItem.Effects = keptEffects;
            magicItem.Sockets = keptSockets;
            try
            {
                fits = candidate.CheckRequirements(itemData, magicItem, checklootroll, checkaugment, checkruneroll) &&
                    FitsSocketedEffects(candidate, magicItem);
            }
            finally
            {
                magicItem.Effects = effects;
                magicItem.Sockets = sockets;
            }

            if (!fits)
            {
                blockers.Clear();
            }

            return blockers;
        }

        // Whether a rolled effect of this definition may join the item's active socketed effects. Sockets
        // are kept through an augment or rune etch, so the exclusivity rules count them as well: a different
        // effect either side excludes is refused, and the same effect follows the setting that lets a stone
        // join a matching rolled effect, read from the other side.
        private static bool FitsSocketedEffects(MagicItemEffectDefinition candidate, MagicItem magicItem)
        {
            if (magicItem.Sockets == null || magicItem.Sockets.Count == 0)
            {
                return true;
            }

            var requirements = candidate.Requirements;
            var exclusiveSelf = requirements != null &&
                (requirements.ExclusiveSelf || requirements.ExclusiveEffectTypes?.Contains(candidate.Type) == true);
            foreach (var socket in magicItem.Sockets)
            {
                if (socket == null || !socket.IsActive())
                {
                    continue;
                }

                var socketedType = socket.Effect.EffectType;
                if (socketedType == candidate.Type)
                {
                    if (exclusiveSelf && !ShardSocketManager.AllowMatchingItemEffect(socket.ShardType))
                    {
                        return false;
                    }
                }
                else if (MagicItemEffectRequirements.EffectsExclude(candidate.Type, socketedType))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// True only for an effect that carries no value at <i>any</i> rarity -- the binary grants
        /// (Indestructible, Weightless, Warmth, Throwable...) whose magiceffects.json entry has no
        /// ValuesPerRarity block at all.
        ///
        /// Deliberately not "has no value at rarity X": an effect with values at some rarities but not
        /// that one is a config gap, not a binary effect, and treating the two the same is how a
        /// legitimately rolled value gets destroyed (see MagicItemComponent.FixupValuelessEffects).
        /// Callers that really do want the per-rarity question ask GetValuesForRarity directly.
        /// </summary>
        public static bool IsValuelessEffect(string effectType)
        {
            // An unknown effect is never treated as binary, so its stored value is left alone. Not warned
            // here: every item load passes through this, and Get() already reports the missing
            // definition once per type wherever the effect is shown.
            if (!TryGet(effectType, out var effectDef))
            {
                return false;
            }

            if (effectDef.ValuesPerRarity == null)
            {
                EpicLoot.LogWarning($"Checking if effect without a ValuesPerRarity block is valueless ({effectType})");
                return false;
            }

            foreach (var rarity in Rarities.All)
            {
                if (effectDef.ValuesPerRarity.GetValueDefForRarity(rarity) != null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
