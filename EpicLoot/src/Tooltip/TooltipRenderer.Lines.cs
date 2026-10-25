using UnityEngine;

namespace EpicLoot;

// The stat lines, in the order the tooltip draws them. A line magic can change is drawn from data.Stats when it
// is there; the rest are vanilla's lines read off the item.
public partial class TooltipRenderer
{
    private static readonly string[] DamageIds =
    {
        "$inventory_damage", "$inventory_blunt", "$inventory_slash", "$inventory_pierce", "$inventory_fire",
        "$inventory_frost", "$inventory_lightning", "$inventory_poison", "$inventory_spirit"
    };

    private float SkillLevel => localPlayer.GetSkillLevel(item.m_shared.m_skillType);

    private string StatColor(TooltipStat stat, string plainColor = "orange")
    {
        return stat.Modified ? magicColor : plainColor;
    }

    private void AddSetLabel()
    {
        if (data.SetLabel != null)
        {
            text.Append($"<color={data.SetColor}>{data.SetLabel}</color>\n");
        }
    }

    private void AddDescription()
    {
        text.Append(data.Description);
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

    private void AddWeight()
    {
        TooltipStat stat = data.GetStat("$item_weight");
        if (stat == null)
        {
            return;
        }

        string weightColor = StatColor(stat);
        if (stat.Secondary.HasValue)
        {
            text.Append($"\n$item_weight: <color={weightColor}>{stat.Value:0.0} ({stat.Secondary.Value:0.0} $item_total)</color>");
        }
        else
        {
            text.Append($"\n$item_weight: <color={weightColor}>{stat.Value:0.0}</color>");
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
        if (data.GetStat("$item_durability", TooltipStat.Indestructible) != null)
        {
            text.Append($"\n$item_durability: <color={magicColor}>$mod_epicloot_me_indestructible_display</color>");
            return;
        }

        TooltipStat stat = data.GetStat("$item_durability");
        if (stat == null)
        {
            return;
        }

        text.Append($"\n$item_durability: <color={StatColor(stat)}>{stat.Value:0}%</color> " +
            $"<color={StatColor(stat, "yellow")}>({stat.Min.GetValueOrDefault():0}/{stat.Max.GetValueOrDefault():0})</color>");

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

    private void AddFoodHealth()
    {
        if (item.m_shared.m_food > 0f)
        {
            text.AppendFormat("\n$item_food_health: <color=#ff8080ff>{0}</color>  ($item_current:<color=yellow>{1}</color>)",
                item.m_shared.m_food, localPlayer.GetMaxHealth());
        }
    }

    private void AddFoodStamina()
    {
        if (item.m_shared.m_foodStamina > 0f)
        {
            text.AppendFormat("\n$item_food_stamina: <color=#ffff80ff>{0}</color>  ($item_current:<color=yellow>{1}</color>)",
                item.m_shared.m_foodStamina, localPlayer.GetMaxStamina());
        }
    }

    private void AddFoodEitr()
    {
        if (item.m_shared.m_foodEitr > 0f)
        {
            text.AppendFormat("\n$item_food_eitr: <color=#9090ffff>{0}</color>  ($item_current:<color=yellow>{1}</color>)",
                item.m_shared.m_foodEitr, localPlayer.GetMaxEitr());
        }
    }

    private void AddFoodBurn()
    {
        text.AppendFormat("\n$item_food_duration: <color=orange>{0}s</color>", item.m_shared.m_foodBurnTime);
    }

    private void AddFoodRegen()
    {
        if (item.m_shared.m_foodRegen > 0f)
        {
            text.AppendFormat("\n$item_food_regen: <color=orange>{0} hp/tick</color>", item.m_shared.m_foodRegen);
        }
    }

    private void AddDamages()
    {
        foreach (string id in DamageIds)
        {
            TooltipStat stat = data.GetStat(id);
            if (stat != null)
            {
                text.AppendFormat("\n{0}: {1}", id, DamageRange(stat));
            }
        }
    }

    private string DamageRange(TooltipStat stat)
    {
        int min = Mathf.RoundToInt(stat.Min.GetValueOrDefault());
        int max = Mathf.RoundToInt(stat.Max.GetValueOrDefault());
        return $"<color={StatColor(stat)}>{Mathf.RoundToInt(stat.Value)}</color> " +
            $"<color={StatColor(stat, "yellow")}>({min}-{max}) </color>";
    }

    private void AddDamageMultiplierByTotalHealthMissing()
    {
        if (item.m_shared.m_attack.m_damageMultiplierByTotalHealthMissing > 0f)
        {
            text.Append(
                $"\n$item_damagemultipliertotal: <color=orange>" +
                $"{item.m_shared.m_attack.m_damageMultiplierByTotalHealthMissing * 100}%</color>");
        }
    }

    private void AddDamageMultiplierPerMissingHP()
    {
        if (item.m_shared.m_attack.m_damageMultiplierPerMissingHP > 0f)
        {
            text.Append(
                $"\n$item_damagemultiplierhp: <color=orange>" +
                $"{item.m_shared.m_attack.m_damageMultiplierPerMissingHP * 100}%</color>");
        }
    }

    private void AddAttackStaminaUse()
    {
        TooltipStat bloodlust = data.GetStat("$item_staminause", TooltipStat.Bloodlust);
        if (bloodlust != null)
        {
            text.Append($"\n$item_staminause: <color=red>{bloodlust.Value:0.#}</color>");
            return;
        }

        TooltipStat stat = data.GetStat("$item_staminause");
        if (stat != null)
        {
            text.Append($"\n$item_staminause: <color={StatColor(stat)}>{stat.Value:0.#}</color>");
        }
    }

    private void AddEitrUse()
    {
        TooltipStat stat = data.GetStat("$item_eitruse");
        if (stat != null)
        {
            text.Append($"\n$item_eitruse: <color={StatColor(stat)}>{stat.Value:0.#}</color>");
        }
    }

    private void AddHealthUse()
    {
        TooltipStat stat = data.GetStat("$item_healthuse");
        if (stat != null)
        {
            text.Append($"\n$item_healthuse: <color={StatColor(stat)}>{stat.Value}</color>");
        }
    }

    private void AddHealthHitReturn()
    {
        if (item.m_shared.m_attack.m_attackHealthReturnHit > 0f)
        {
            text.Append(
                $"\n$item_healthhitreturn: <color=orange>{item.m_shared.m_attack.m_attackHealthReturnHit}</color>");
        }
    }

    private void AddHealthUsePercentage()
    {
        // Already a percentage (40 = 40% of current health), so no "%" format specifier, which would
        // multiply by 100.
        TooltipStat stat = data.GetStat("$item_healthuse", TooltipStat.Percent);
        if (stat != null)
        {
            text.Append($"\n$item_healthuse: <color={StatColor(stat)}>{stat.Value:0.#}%</color>");
        }
    }

    private void AddDrawStaminaUse()
    {
        TooltipStat stat = data.GetStat("$item_staminahold");
        if (stat != null)
        {
            text.Append($"\n$item_staminahold: <color={StatColor(stat)}>{stat.Value:0.#}/s</color>");
        }
    }

    private void AddBlockArmor()
    {
        TooltipStat stat = data.GetStat("$item_blockarmor");
        if (stat != null)
        {
            text.Append($"\n$item_blockarmor: <color={StatColor(stat)}>{stat.Value:0.#}</color> " +
                $"<color={StatColor(stat, "yellow")}>({stat.Secondary.GetValueOrDefault():0})</color>");
        }
    }

    private void AddBlockForceAndParry()
    {
        TooltipStat blockForce = data.GetStat("$item_blockforce");
        if (blockForce != null)
        {
            text.Append($"\n$item_blockforce: <color={StatColor(blockForce)}>{blockForce.Value:0.#}</color>");
        }

        TooltipStat parry = data.GetStat("$item_parrybonus");
        if (parry != null)
        {
            text.Append($"\n$item_parrybonus: <color={StatColor(parry)}>{parry.Value:0.#}x</color>");
        }
    }

    private void AddParryAdrenaline()
    {
        if (item.m_shared.m_perfectBlockAdrenaline > 0.0)
        {
            text.Append($"\n$item_parryadrenaline: <color=orange>{item.m_shared.m_perfectBlockAdrenaline}</color>");
        }
    }

    private void AddDamageModifiers()
    {
        string modifiersTooltipString = SE_Stats.GetDamageModifiersTooltipString(item.m_shared.m_damageModifiers);
        if (modifiersTooltipString.Length > 0)
        {
            text.Append(modifiersTooltipString);
        }
    }

    private void AddKnockback()
    {
        TooltipStat stat = data.GetStat("$item_knockback");
        if (stat != null)
        {
            text.AppendFormat("\n$item_knockback: <color={0}>{1:0.#}</color>", StatColor(stat), stat.Value);
        }
    }

    private void AddBackstab()
    {
        TooltipStat stat = data.GetStat("$item_backstab");
        if (stat != null)
        {
            text.Append($"\n$item_backstab: <color={StatColor(stat)}>{stat.Value:0.#}x</color>");
        }
    }

    private void AddTameOnly()
    {
        if (item.m_shared.m_tamedOnly)
        {
            text.Append($"\n<color=orange>$item_tamedonly</color>");
        }
    }

    private void AddProjectileTooltip()
    {
        string projectileTooltip = item.GetProjectileTooltip(qualityLevel);
        if (projectileTooltip.Length > 0 && item.m_shared.m_projectileToolTip)
        {
            text.Append("\n\n");
            text.Append(projectileTooltip);
        }
    }

    private void AddArmor()
    {
        TooltipStat stat = data.GetStat("$item_armor");
        if (stat != null)
        {
            text.Append($"\n$item_armor: <color={StatColor(stat)}>{stat.Value:0.#}</color>");
        }
    }

    private void AddStatusEffectTooltip()
    {
        string statusEffectTooltip = item.GetStatusEffectTooltip(qualityLevel, SkillLevel);
        if (statusEffectTooltip.Length > 0)
        {
            text.Append("\n\n");
            text.Append(statusEffectTooltip);
        }
    }

    private void AddChainTooltip()
    {
        string chainTooltip = item.GetChainTooltip(qualityLevel, SkillLevel);
        if (chainTooltip.Length > 0)
        {
            text.Append("\n\n");
            text.Append(chainTooltip);
        }
    }

    private void AddEquipmentModifiers()
    {
        // TODO: add health/stamina regen if needed
        AddModifier("$item_eitrregen_modifier");
        AddModifier("$item_movement_modifier");
        AddVanillaModifier("$base_item_modifier", item.m_shared.m_homeItemsStaminaModifier,
            () => localPlayer.GetEquipmentHomeItemModifier());
        AddModifier("$item_heat_modifier");
        AddModifier("$se_jumpstamina");
        AddModifier("$se_attackstamina");
        AddModifier("$se_blockstamina");
        AddModifier("$se_dodgestamina");
        // TODO: add a magic effect for these two?
        AddVanillaModifier("$se_swimstamina", item.m_shared.m_swimStaminaModifier, () =>
        {
            float playerTotal = localPlayer.GetEquipmentSwimStaminaModifier();
            localPlayer.m_seman.ModifySwimStaminaUsage(1f, ref playerTotal, minZero: false);
            return playerTotal;
        });
        AddVanillaModifier("$se_sneakstamina", item.m_shared.m_sneakStaminaModifier, () =>
        {
            float playerTotal = localPlayer.GetEquipmentSneakStaminaModifier();
            localPlayer.m_seman.ModifySneakStaminaUsage(1f, ref playerTotal, minZero: false);
            return playerTotal;
        });
        AddModifier("$se_runstamina");
        AddMaxAdrenaline();
    }

    private void AddModifier(string id)
    {
        TooltipStat stat = data.GetStat(id);
        if (stat != null)
        {
            ModifierFormat(id, stat.Modified, stat.Value, stat.Secondary.GetValueOrDefault());
        }
    }

    private void AddVanillaModifier(string id, float value, System.Func<float> getPlayerTotal)
    {
        if (value != 0f)
        {
            ModifierFormat(id, false, value, getPlayerTotal());
        }
    }

    private void ModifierFormat(string name, bool hasModifier, float value, float totalValue)
    {
        string color = hasModifier ? magicColor : "orange";
        text.Append($"\n{name}: <color={color}>{value * 100f:+0;-0}%</color> " +
            $"($item_total:<color=yellow>{totalValue * 100f:+0;-0}%</color>)");
    }

    private void AddMaxAdrenaline()
    {
        if (item.m_shared.m_maxAdrenaline > 0f)
        {
            text.AppendFormat("\n$item_maxadrenaline: <color=orange>{0}</color>", item.m_shared.m_maxAdrenaline);
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
