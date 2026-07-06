using System.Text;

namespace EpicLoot;

public partial class MagicTooltip(ItemDrop.ItemData item, MagicItem magicItem, int qualityLevel)
{
    private static Player localPlayer => Player.m_localPlayer;
    private static readonly StringBuilder text = new StringBuilder(256);

    private readonly string magicColor = magicItem.GetColorString();
    private readonly string itemTypeName = magicItem.GetItemTypeName(item.Extended());
    private readonly float skillLevel = localPlayer.GetSkillLevel(item.m_shared.m_skillType);

    public string GetTooltip()
    {
        text.Clear();

        //AddMagicDisplayName();
        AddMagicSetLabel();
        AddDescription();
        text.Append("\n");

        AddDLC();
        AddNewGamePlus();
        AddSubtitle();
        ItemDrop.ItemData.AddHandedTip(item, text);
        AddCrafterName();
        AddTeleportable();
        AddValuable();
        AddStackSizeAndWeight();
        AddQuality();
        AddDurability();

        switch (item.m_shared.m_itemType)
        {
            case ItemDrop.ItemData.ItemType.Consumable:
                if (item.m_shared.m_food > 0f || item.m_shared.m_foodStamina > 0f || item.m_shared.m_foodEitr > 0f)
                {
                    AddFoodHealth();
                    AddFoodStamina();
                    AddFoodEitr();
                    AddFoodBurn();
                    AddFoodRegen();
                }
                break;
            case ItemDrop.ItemData.ItemType.OneHandedWeapon:
            case ItemDrop.ItemData.ItemType.Bow:
            case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
            case ItemDrop.ItemData.ItemType.Torch:
            case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                AddDamages();
                AddDamageMultiplierByTotalHealthMissing();
                AddDamageMultiplierPerMissingHP();
                AddAttackStaminaUse();
        
                // TODO: finish if needed
                // AddDodge();
                // AddOffset();
                // AddChainLightning();
                // AddApportation();

                AddEitrUse();
                AddHealthUse();
                AddHealthHitReturn();
                AddHealthUsePercentage();
                AddDrawStaminaUse();

                AddBlockArmor();
                AddBlockForceAndParry();
                AddParryAdrenaline();

                // weapons typically do not have damage modifiers, but perhaps other mods utilize this
                AddDamageModifiers();

                AddKnockback();
                AddBackstab();
                AddTameOnly();
                AddProjectileTooltip();
                break;
            case ItemDrop.ItemData.ItemType.Shield:
                AddBlockArmor();
                AddBlockForceAndParry();
                AddParryAdrenaline();
                AddDamageModifiers();
                break;
            case ItemDrop.ItemData.ItemType.Helmet:
            case ItemDrop.ItemData.ItemType.Chest:
            case ItemDrop.ItemData.ItemType.Legs:
            case ItemDrop.ItemData.ItemType.Shoulder:
                AddArmor();
                AddDamageModifiers();
                break;
            case ItemDrop.ItemData.ItemType.Ammo:
            case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
                AddDamages();
                AddKnockback();
                break;
            case ItemDrop.ItemData.ItemType.Trinket:
                break;
        }

        AddStatusEffectTooltip();
        AddChainTooltip();

        AddAllEquipmentModifiers();

        MagicEffects();

        AddSetTooltip();
        AddAdrenalineStatusEffectTooltip();

        return text.ToString();
    }

    private void AddMagicDisplayName()
    {
        // removed duplicate display name if it does not have unique name
        if (magicItem.IsUniqueLegendary())
        {
            text.Append($"<color={magicColor}>{magicItem.GetRarityDisplay()} {itemTypeName}</color>\n");
        }
    }

    private void AddMagicSetLabel()
    {
        if (item.IsMagicSetItem())
        {
            switch (item.GetRarity())
            {
                case ItemRarity.Legendary:
                    text.Append($"<color={EpicLoot.GetSetItemColor()}>$mod_epicloot_legendarysetlabel</color>\n");
                    break;
                case ItemRarity.Mythic:
                    text.Append($"<color={EpicLoot.GetSetItemColor()}>$mod_epicloot_mythicsetlabel</color>\n");
                    break;
            }
        }
    }
    
    private void AddDescription()
    {
        text.Append(item.GetDescription());
    }

    private void AddDLC()
    {
        if (item.m_shared.m_dlc.Length > 0)
        {
            text.Append("\n<color=#00ffffff>$item_dlc</color>");
        }
    }

    private void AddNewGamePlus()
    {
        if (item.m_worldLevel > 0)
        {
            string itemWorldLevel = item.m_worldLevel != 1 ? item.m_worldLevel.ToString() : "";
            text.Append($"\n<color=orange>$item_newgameplusitem {itemWorldLevel}</color>");
        }
    }

    private void AddSubtitle()
    {
        // this applies to trophies, but perhaps, other mods utilize this to add extra descriptions.
        if (item.m_shared.m_subtitle.Length > 0)
        {
            text.Append($"\n<color=orange>{item.m_shared.m_subtitle}</color>");
        }
    }

    private void AddCrafterName()
    {
        if (item.m_crafterID != 0L)
        {
            text.AppendFormat("\n$item_crafter: <color=orange>{0}</color>", item.m_crafterName);
        }
    }

    private void AddTeleportable()
    {
        bool isTeleportable = item.m_shared.m_teleportable || ZoneSystem.instance.GetGlobalKey(GlobalKeys.TeleportAll);
        if (!isTeleportable)
        {
            text.Append("\n<color=orange>$item_noteleport</color>");
        }
    }

    private void AddValuable()
    {
        if (item.m_shared.m_value > 0)
        {
            text.AppendFormat("\n$item_value: <color=orange>{0} ({1} $item_total)</color>", item.m_shared.m_value, item.GetValue());
        }
    }

    private void AddStackSizeAndWeight()
    {
        bool hasWeightModifier = magicItem.HasEffect(MagicEffectType.ReduceWeight) ||
            magicItem.HasEffect(MagicEffectType.Weightless);
        string weightColor = hasWeightModifier ? magicColor : "orange";

        if (item.m_shared.m_maxStackSize > 1)
        {
            float nonStackedWeight = item.GetNonStackedWeight();
            float weight = item.GetWeight(item.m_shared.m_maxStackSize);
            text.Append($"\n$item_weight: <color={weightColor}>{nonStackedWeight:0.0} ({weight:0.0} $item_total)</color>");
        }
        else
        {
            float weight = item.GetWeight();
            text.Append($"\n$item_weight: <color={weightColor}>{weight:0.0}</color>");
        }
    }

    private void AddQuality()
    {
        if (item.m_shared.m_maxQuality > 1)
        {
            text.AppendFormat("\n$item_quality: <color=orange>{0}</color>", qualityLevel);
        }
    }

    private void AddDurability()
    {
        if (!item.m_shared.m_useDurability)
        {
            return;
        }

        bool isIndestructible = magicItem.HasEffect(MagicEffectType.Indestructible);

        if (isIndestructible)
        {
            text.Append($"\n$item_durability: <color={magicColor}>$mod_epicloot_me_indestructible_display</color>");
        }
        else
        {
            bool hasDurabilityModifier = magicItem.HasEffect(MagicEffectType.ModifyDurability);

            string maxDurabilityColor1 = hasDurabilityModifier ? magicColor : "orange";
            string maxDurabilityColor2 = hasDurabilityModifier ? magicColor : "yellow";

            float maxDurability = item.GetMaxDurability(qualityLevel);
            float durability = item.m_durability;
            float currentDurabilityPercentage = item.GetDurabilityPercentage() * 100f;
            string durabilityPercentageString = currentDurabilityPercentage.ToString("0");
            string durabilityValueString = durability.ToString("0");
            string durabilityMaxString = maxDurability.ToString("0");

            text.Append($"\n$item_durability: <color={maxDurabilityColor1}>{durabilityPercentageString}%</color> " +
                $"<color={maxDurabilityColor2}>({durabilityValueString}/{durabilityMaxString})</color>");
        }

        if (item.m_shared.m_canBeReparied)
        {
            Recipe recipe = ObjectDB.instance.GetRecipe(item);
            if (recipe != null)
            {
                int minStationLevel = recipe.m_minStationLevel;
                text.AppendFormat("\n$item_repairlevel: <color=orange>{0}</color>", minStationLevel.ToString());
            }
        }
    }
    
    private void AddStatusEffectTooltip()
    {
        string statusEffectTooltip = item.GetStatusEffectTooltip(qualityLevel, skillLevel);
        if (statusEffectTooltip.Length > 0)
        {
            text.Append("\n\n");
            text.Append(statusEffectTooltip);
        }
    }

    private void AddChainTooltip()
    {
        string chainTooltip = item.GetChainTooltip(qualityLevel, skillLevel);
        if (chainTooltip.Length > 0)
        {
            text.Append("\n\n");
            text.Append(chainTooltip);
        }
    }

    private void MagicEffects()
    {
        text.AppendLine(magicItem.GetTooltip());
    }

    private void AddSetTooltip()
    {
        if (item.IsSetItem())
        {
            text.Append(item.GetSetTooltip());
        }
    }

    private void AddAdrenalineStatusEffectTooltip()
    {
        if (item.m_shared.m_fullAdrenalineSE != null)
        {
            text.Append($"\n$item_fulladrenaline: <color=orange>{item.m_shared.m_fullAdrenalineSE.GetTooltipString()}</color>");
        }
    }
}