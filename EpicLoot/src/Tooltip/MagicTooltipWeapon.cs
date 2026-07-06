using EpicLoot.MagicItemEffects;

namespace EpicLoot;

public partial class MagicTooltip
{
    private void AddDamages()
    {
        HitData.DamageTypes damages = ModifyDamage.GetDamageWithMagicEffects(item);

        localPlayer.GetSkills().GetRandomSkillRange(out float min, out float max, item.m_shared.m_skillType);

        bool allDamage = magicItem.HasEffect(MagicEffectType.ModifyDamage, true);
        bool physDamage = magicItem.HasEffect(MagicEffectType.ModifyPhysicalDamage);
        bool elemDamage = magicItem.HasEffect(MagicEffectType.ModifyElementalDamage);

        bool coinHoarderDamage = localPlayer.HasActiveMagicEffect(MagicEffectType.CoinHoarder, out float _cv);
        bool spellswordDamage = magicItem.HasEffect(MagicEffectType.SpellSword);

        bool allCheck = allDamage || coinHoarderDamage || spellswordDamage;

        if (damages.m_damage != 0f)
        {
            bool isMagic = allCheck;

            text.AppendFormat("\n{0}: {1}",
                "$inventory_damage",
                DamageRange(damages.m_damage, min, max, isMagic, magicColor));
        }

        if (damages.m_blunt != 0f)
        {
            bool isMagic = allCheck || physDamage || magicItem.HasEffect(MagicEffectType.AddBluntDamage);
            text.AppendFormat("\n{0}: {1}",
                "$inventory_blunt",
                DamageRange(damages.m_blunt, min, max, isMagic, magicColor));
        }

        if (damages.m_slash != 0f)
        {
            bool isMagic = allCheck || physDamage || magicItem.HasEffect(MagicEffectType.AddSlashingDamage);
            text.AppendFormat("\n{0}: {1}", 
                "$inventory_slash",
                DamageRange(damages.m_slash, min, max, isMagic, magicColor));
        }

        if (damages.m_pierce != 0f)
        {
            bool isMagic = allCheck || physDamage || magicItem.HasEffect(MagicEffectType.AddPiercingDamage);
            text.AppendFormat("\n{0}: {1}",
                "$inventory_pierce",
                DamageRange(damages.m_pierce, min, max, isMagic, magicColor));
        }

        if (damages.m_fire != 0f)
        {
            bool isMagic = allCheck || elemDamage || magicItem.HasEffect(MagicEffectType.AddFireDamage);
            text.AppendFormat("\n{0}: {1}",
                "$inventory_fire",
                DamageRange(damages.m_fire, min, max, isMagic, magicColor));
        }
        if (damages.m_frost != 0f)
        {
            bool isMagic = allCheck || elemDamage || magicItem.HasEffect(MagicEffectType.AddFrostDamage);
            text.AppendFormat("\n{0}: {1}",
                "$inventory_frost",
                DamageRange(damages.m_frost, min, max, isMagic, magicColor));
        }
        if (damages.m_lightning != 0f)
        {
            bool isMagic = allCheck || elemDamage || magicItem.HasEffect(MagicEffectType.AddLightningDamage);
            text.AppendFormat("\n{0}: {1}",
                "$inventory_lightning",
                DamageRange(damages.m_lightning, min, max, isMagic, magicColor));
        }
        if (damages.m_poison != 0f)
        {
            bool isMagic = allCheck || elemDamage || magicItem.HasEffect(MagicEffectType.AddPoisonDamage);
            text.AppendFormat("\n{0}: {1}",
                "$inventory_poison",
                DamageRange(damages.m_poison, min, max, isMagic, magicColor));
        }
        
        if (damages.m_spirit != 0f)
        {
            bool isMagic = allCheck || elemDamage || magicItem.HasEffect(MagicEffectType.AddSpiritDamage);
            text.AppendFormat("\n{0}: {1}",
                "$inventory_spirit",
                DamageRange(damages.m_spirit, min, max, isMagic, magicColor));
        }
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
        // TODO: place logic into helper method
        if (magicItem.HasEffect(MagicEffectType.Bloodlust))
        {
            float stamina = Bloodlust.GetBloodlustStamina();
            text.Append($"\n$item_staminause: <color=red>{stamina:0.#}</color>");
        }
        else if (item.m_shared.m_attack.m_attackStamina > 0f)
        {
            bool hasAttackStaminaModifiers = magicItem.HasEffect(MagicEffectType.ModifyAttackStaminaUse) ||
                magicItem.HasEffect(MagicEffectType.ModifyBlockStaminaUse);
            string magicAttackStaminaColor = hasAttackStaminaModifiers ? magicColor : "orange";
            float staminaUsePercentage = 1 - magicItem.GetTotalEffectValue(MagicEffectType.ModifyAttackStaminaUse, 0.01f);
            float totalStaminaUse = staminaUsePercentage * item.m_shared.m_attack.m_attackStamina;

            bool hasSpellSword = magicItem.HasEffect(MagicEffectType.SpellSword);
            if (hasSpellSword)
            {
                totalStaminaUse = Spellsword.GetSpellswordAttackStamina(totalStaminaUse);
            }

            text.Append($"\n$item_staminause: <color={magicAttackStaminaColor}>{totalStaminaUse:0.#}</color>");
        }
    }

    private void AddDodge()
    {
        bool hasDodgeBuff = magicItem.HasEffect(MagicEffectType.DodgeBuff);
        if (hasDodgeBuff)
        {
            float dodgeBuffValue = magicItem.GetTotalEffectValue(MagicEffectType.DodgeBuff, 1f);
            // TODO: if using this tooltip, localize this
            text.Append($"\n$mod_epicloot_dodge: <color={magicColor}>{dodgeBuffValue:0.#}</color>");
        }
    }

    private void AddOffset()
    {
        bool hasOffSetAttack = magicItem.HasEffect(MagicEffectType.OffSetAttack);
        if (hasOffSetAttack)
        {
            float offSetAttackValue = magicItem.GetTotalEffectValue(MagicEffectType.OffSetAttack, 1f);
            // TODO: if using this tooltip, localize this
            text.Append($"\n$mod_epicloot_offset: <color={magicColor}>{offSetAttackValue:0.#}</color>");
        }
    }

    private void AddChainLightning()
    {
        bool hasChainLightning = magicItem.HasEffect(MagicEffectType.ChainLightning);
        if (hasChainLightning)
        {
            float ChainLightningValue = magicItem.GetTotalEffectValue(MagicEffectType.ChainLightning, 1f);
            // TODO: if using this tooltip, localize this
            text.Append(
                $"\n$mod_epicloot_chainlightning: <color={magicColor}>{ChainLightningValue:0.#}</color>");
        }
    }

    private void AddApportation()
    {
        bool hasApportation = magicItem.HasEffect(MagicEffectType.Apportation);
        if (hasApportation)
        {
            float ApportationValue = magicItem.GetTotalEffectValue(MagicEffectType.Apportation, 1f);
            // TODO: if adding this tooltip, localize this
            text.Append($"\n$mod_epicloot_apportation: <color={magicColor}>{ApportationValue:0.#}</color>");
        }
    }

    private void AddEitrUse()
    {
        // TODO: place logic into helper method
        bool hasSpellSword = magicItem.HasEffect(MagicEffectType.SpellSword);

        if (item.m_shared.m_attack.m_attackEitr <= 0f || !hasSpellSword)
        {
            return;
        }

        bool hasDoubleMagicShot = magicItem.HasEffect(MagicEffectType.DoubleMagicShot);
        bool hasEitrUseModifier = magicItem.HasEffect(MagicEffectType.ModifyAttackEitrUse);
        bool hasAttackEitrModifier = hasEitrUseModifier || hasDoubleMagicShot || hasSpellSword;

        string magicAttackEitrColor = hasAttackEitrModifier ? magicColor : "orange";

        float eitrUsePercentage = 1 - magicItem.GetTotalEffectValue(MagicEffectType.ModifyAttackEitrUse, 0.01f);
        float totalEitrUse = eitrUsePercentage * item.m_shared.m_attack.m_attackEitr;

        // TODO: find an appropriate way to display all the information from multishot.
        // This is half implemented and untested here.
        /*string additionalEtir = string.Empty;

        if (hasDoubleMagicShot && MagicItemEffectDefinitions.AllDefinitions != null &&
            MagicItemEffectDefinitions.AllDefinitions.ContainsKey(MagicEffectType.DoubleMagicShot))
        {
            Dictionary<string, float> configuration = MagicItemEffectDefinitions.AllDefinitions[MagicEffectType.DoubleMagicShot].Config;
            int projectiles = Mathf.RoundToInt(configuration[MultiShot.PROJECTILES_KEY]);
            additionalEtir = $"/{totalEitrUse * projectiles}:0.#";
        }*/

        if (hasSpellSword)
        {
            totalEitrUse += Spellsword.GetAdditionalSpellswordAttackEitr(totalEitrUse);
        }

        text.Append($"\n$item_eitruse: <color={magicAttackEitrColor}>{totalEitrUse:0.#}</color>");
    }

    private void AddHealthUse()
    {
        // TODO: place logic into helper method
        bool hasBloodlust = magicItem.HasEffect(MagicEffectType.Bloodlust);
        float healthUsage = item.m_shared.m_attack.m_attackHealth;

        if (hasBloodlust)
        {
            healthUsage = Bloodlust.GetBloodlustHealth(healthUsage, item.m_shared.m_attack.m_attackStamina);
        }

        if (item.m_shared.m_attack.m_attackHealth > 0f || hasBloodlust)
        {
            bool magicAttackHealth = magicItem.HasEffect(MagicEffectType.ModifyAttackHealthUse);
            string magicAttackHealthColor = magicAttackHealth ? magicColor : "orange";

            float effectValue = magicItem.GetTotalEffectValue(MagicEffectType.ModifyAttackHealthUse, 0.01f);
            float healthUsageModifier = ModifyAttackCosts.GetEffectPercentage(effectValue);
            healthUsage = healthUsageModifier * healthUsage;

            text.Append($"\n$item_healthuse: <color={magicAttackHealthColor}>{healthUsage}</color>");
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
        if (item.m_shared.m_attack.m_attackHealthPercentage > 0f)
        {
            text.Append($"\n$item_healthuse: <color=orange>{item.m_shared.m_attack.m_attackHealthPercentage:0.#%}</color>");
        }
    }

    private void AddDrawStaminaUse()
    {
        if (item.m_shared.m_attack.m_drawStaminaDrain > 0f)
        {
            bool hasDrawStaminaUseModifier = magicItem.HasEffect(MagicEffectType.ModifyDrawStaminaUse);
            string attackDrawStaminaColor = hasDrawStaminaUseModifier ? magicColor : "orange";

            float attackDrawStaminaPercentage = 1 - magicItem.GetTotalEffectValue(MagicEffectType.ModifyDrawStaminaUse, 0.01f);
            float totalAttackDrawStamina = attackDrawStaminaPercentage * item.m_shared.m_attack.m_drawStaminaDrain;

            text.Append($"\n$item_staminahold: " +
                $"<color={attackDrawStaminaColor}>{totalAttackDrawStamina:0.#}/s</color>");
        }
    }

    private void AddBackstab()
    {
        bool hasBackstabModifier = magicItem.HasEffect(MagicEffectType.ModifyBackstab);
        float totalBackstabBonusMod = magicItem.GetTotalEffectValue(MagicEffectType.ModifyBackstab, 0.01f);
        string magicBackstabColor = hasBackstabModifier ? magicColor : "orange";
        float backstabValue = item.m_shared.m_backstabBonus * (1.0f + totalBackstabBonusMod);
        text.Append($"\n$item_backstab: <color={magicBackstabColor}>{backstabValue:0.#}x</color>");
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

    private void AddTameOnly()
    {
        if (item.m_shared.m_tamedOnly)
        {
            text.Append($"\n<color=orange>$item_tamedonly</color>");
        }
    }
    
    private void AddKnockback()
    {
        if (item.m_shared.m_attackForce > 0f)
        {
            text.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", item.m_shared.m_attackForce);
        }
    }
}