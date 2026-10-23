using EpicLoot.MagicItemEffects;
using EpicLoot.MagicItemEffects.Shards;

namespace EpicLoot;

public partial class MagicTooltip {
    private void AddDamages() {
        HitData.DamageTypes damages = ModifyDamage.GetDamageWithMagicEffects(item);

        // Eitr Imbue is applied to the hit itself (attacker-side, paid for with eitr) rather than in
        // GetDamage, so fold its bonus in here -- otherwise a purely physical weapon shows no spirit
        // damage at all despite dealing it on every affordable swing.
        float eitrImbueSpirit = EitrImbueAttack.GetSpiritBonus(magicItem, damages);
        damages.m_spirit += eitrImbueSpirit;

        localPlayer.GetSkills().GetRandomSkillRange(out float min, out float max, item.m_shared.m_skillType);

        bool allDamage = magicItem.HasEffect(MagicEffectType.ModifyDamage, true);
        bool physDamage = magicItem.HasEffect(MagicEffectType.ModifyPhysicalDamage);
        bool elemDamage = magicItem.HasEffect(MagicEffectType.ModifyElementalDamage);

        bool coinHoarderDamage = CoinHoarder.GetDamageBonus(localPlayer) > 0f;
        bool spellswordDamage = magicItem.HasEffect(MagicEffectType.SpellSword);

        bool allCheck = allDamage || coinHoarderDamage || spellswordDamage;

        if (damages.m_damage != 0f) {
            bool isMagic = allCheck;

            text.AppendFormat("\n{0}: {1}",
                "$inventory_damage",
                DamageRange(damages.m_damage, min, max, isMagic, magicColor));
        }

        if (damages.m_blunt != 0f) {
            bool isMagic = allCheck || physDamage || magicItem.HasEffect(MagicEffectType.AddBluntDamage);
            text.AppendFormat("\n{0}: {1}",
                "$inventory_blunt",
                DamageRange(damages.m_blunt, min, max, isMagic, magicColor));
        }

        if (damages.m_slash != 0f) {
            bool isMagic = allCheck || physDamage || magicItem.HasEffect(MagicEffectType.AddSlashingDamage);
            text.AppendFormat("\n{0}: {1}",
                "$inventory_slash",
                DamageRange(damages.m_slash, min, max, isMagic, magicColor));
        }

        if (damages.m_pierce != 0f) {
            bool isMagic = allCheck || physDamage || magicItem.HasEffect(MagicEffectType.AddPiercingDamage);
            text.AppendFormat("\n{0}: {1}",
                "$inventory_pierce",
                DamageRange(damages.m_pierce, min, max, isMagic, magicColor));
        }

        if (damages.m_fire != 0f) {
            bool isMagic = allCheck || elemDamage || magicItem.HasEffect(MagicEffectType.AddFireDamage);
            text.AppendFormat("\n{0}: {1}",
                "$inventory_fire",
                DamageRange(damages.m_fire, min, max, isMagic, magicColor));
        }
        if (damages.m_frost != 0f) {
            bool isMagic = allCheck || elemDamage || magicItem.HasEffect(MagicEffectType.AddFrostDamage);
            text.AppendFormat("\n{0}: {1}",
                "$inventory_frost",
                DamageRange(damages.m_frost, min, max, isMagic, magicColor));
        }
        if (damages.m_lightning != 0f) {
            bool isMagic = allCheck || elemDamage || magicItem.HasEffect(MagicEffectType.AddLightningDamage);
            text.AppendFormat("\n{0}: {1}",
                "$inventory_lightning",
                DamageRange(damages.m_lightning, min, max, isMagic, magicColor));
        }
        if (damages.m_poison != 0f) {
            bool isMagic = allCheck || elemDamage || magicItem.HasEffect(MagicEffectType.AddPoisonDamage);
            text.AppendFormat("\n{0}: {1}",
                "$inventory_poison",
                DamageRange(damages.m_poison, min, max, isMagic, magicColor));
        }

        if (damages.m_spirit != 0f) {
            bool isMagic = allCheck || elemDamage || eitrImbueSpirit > 0f ||
                magicItem.HasEffect(MagicEffectType.AddSpiritDamage);
            text.AppendFormat("\n{0}: {1}",
                "$inventory_spirit",
                DamageRange(damages.m_spirit, min, max, isMagic, magicColor));
        }
    }

    private void AddDamageMultiplierByTotalHealthMissing() {
        if (item.m_shared.m_attack.m_damageMultiplierByTotalHealthMissing > 0f) {
            text.Append(
                $"\n$item_damagemultipliertotal: <color=orange>" +
                $"{item.m_shared.m_attack.m_damageMultiplierByTotalHealthMissing * 100}%</color>");
        }
    }

    private void AddDamageMultiplierPerMissingHP() {
        if (item.m_shared.m_attack.m_damageMultiplierPerMissingHP > 0f) {
            text.Append(
                $"\n$item_damagemultiplierhp: <color=orange>" +
                $"{item.m_shared.m_attack.m_damageMultiplierPerMissingHP * 100}%</color>");
        }
    }

    private void AddAttackStaminaUse() {
        // TODO: place logic into helper method
        if (magicItem.HasEffect(MagicEffectType.Bloodlust)) {
            float stamina = Bloodlust.GetBloodlustStamina();
            text.Append($"\n$item_staminause: <color=red>{stamina:0.#}</color>");
        } else if (item.m_shared.m_attack.m_attackStamina > 0f) {
            bool hasAttackStaminaModifiers = magicItem.HasEffect(MagicEffectType.ModifyAttackStaminaUse) ||
                magicItem.HasEffect(MagicEffectType.ModifyBlockStaminaUse);
            string magicAttackStaminaColor = hasAttackStaminaModifiers ? magicColor : "orange";
            float staminaUsePercentage = 1 - magicItem.GetTotalEffectValue(MagicEffectType.ModifyAttackStaminaUse, 0.01f);
            float totalStaminaUse = staminaUsePercentage * item.m_shared.m_attack.m_attackStamina;

            bool hasSpellSword = magicItem.HasEffect(MagicEffectType.SpellSword);
            if (hasSpellSword) {
                totalStaminaUse = Spellsword.GetSpellswordAttackStamina(totalStaminaUse);
            }

            text.Append($"\n$item_staminause: <color={magicAttackStaminaColor}>{totalStaminaUse:0.#}</color>");
        }
    }

    private void AddEitrUse() {
        // Conjured Arrows (a set bonus) makes every shot of a bow cost eitr. The figure is the attack's own cost
        // call, so every modifier is already in it.
        if (ConjuredArrows.TryGetShotCost(item, out float conjuredCost)) {
            text.Append($"\n$item_eitruse: <color={magicColor}>{conjuredCost:0.#}</color>");
            return;
        }

        // TODO: place logic into helper method
        bool hasSpellSword = magicItem.HasEffect(MagicEffectType.SpellSword);

        // Spellsword gives a weapon an eitr cost it would not otherwise have.
        if (item.m_shared.m_attack.m_attackEitr <= 0f && !hasSpellSword) {
            return;
        }

        bool hasDoubleMagicShot = magicItem.HasEffect(MagicEffectType.DoubleMagicShot);
        bool hasEitrUseModifier = magicItem.HasEffect(MagicEffectType.ModifyAttackEitrUse);
        bool hasAttackEitrModifier = hasEitrUseModifier || hasDoubleMagicShot || hasSpellSword;

        string magicAttackEitrColor = hasAttackEitrModifier ? magicColor : "orange";

        // Same order as the game: Spellsword's extra eitr (half the base stamina cost) is added first,
        // then ModifyAttackEitrUse scales the whole cost.
        float baseEitrUse = item.m_shared.m_attack.m_attackEitr;
        if (hasSpellSword) {
            baseEitrUse += Spellsword.GetAdditionalSpellswordAttackEitr(item.m_shared.m_attack.m_attackStamina);
        }

        float eitrUsePercentage = 1 - magicItem.GetTotalEffectValue(MagicEffectType.ModifyAttackEitrUse, 0.01f);
        float totalEitrUse = eitrUsePercentage * baseEitrUse;

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

        text.Append($"\n$item_eitruse: <color={magicAttackEitrColor}>{totalEitrUse:0.#}</color>");
    }

    private void AddHealthUse() {
        // TODO: place logic into helper method
        bool hasBloodlust = magicItem.HasEffect(MagicEffectType.Bloodlust);
        float healthUsage = item.m_shared.m_attack.m_attackHealth;

        if (hasBloodlust) {
            healthUsage = Bloodlust.GetBloodlustHealth(healthUsage, item.m_shared.m_attack.m_attackStamina);
        }

        if (item.m_shared.m_attack.m_attackHealth > 0f || hasBloodlust) {
            bool magicAttackHealth = magicItem.HasEffect(MagicEffectType.ModifyAttackHealthUse);
            string magicAttackHealthColor = magicAttackHealth ? magicColor : "orange";

            float effectValue = magicItem.GetTotalEffectValue(MagicEffectType.ModifyAttackHealthUse, 0.01f);
            float healthUsageModifier = ModifyAttackCosts.GetEffectPercentage(effectValue);
            healthUsage = healthUsageModifier * healthUsage;

            text.Append($"\n$item_healthuse: <color={magicAttackHealthColor}>{healthUsage}</color>");
        }
    }

    private void AddHealthHitReturn() {
        if (item.m_shared.m_attack.m_attackHealthReturnHit > 0f) {
            text.Append(
                $"\n$item_healthhitreturn: <color=orange>{item.m_shared.m_attack.m_attackHealthReturnHit}</color>");
        }
    }

    private void AddHealthUsePercentage() {
        if (item.m_shared.m_attack.m_attackHealthPercentage > 0f) {
            // Already a percentage (40 = 40% of current health), so no "%" format specifier, which would
            // multiply by 100. Attack.GetAttackHealth folds this into the same cost ModifyAttackHealthUse
            // scales, so the reduction applies here too.
            bool magicAttackHealth = magicItem.HasEffect(MagicEffectType.ModifyAttackHealthUse);
            string magicAttackHealthColor = magicAttackHealth ? magicColor : "orange";

            float effectValue = magicItem.GetTotalEffectValue(MagicEffectType.ModifyAttackHealthUse, 0.01f);
            float healthUsePercentage = ModifyAttackCosts.GetEffectPercentage(effectValue) *
                item.m_shared.m_attack.m_attackHealthPercentage;

            text.Append($"\n$item_healthuse: <color={magicAttackHealthColor}>{healthUsePercentage:0.#}%</color>");
        }
    }

    private void AddDrawStaminaUse() {
        if (item.m_shared.m_attack.m_drawStaminaDrain > 0f) {
            bool hasDrawStaminaUseModifier = magicItem.HasEffect(MagicEffectType.ModifyDrawStaminaUse);
            string attackDrawStaminaColor = hasDrawStaminaUseModifier ? magicColor : "orange";

            float attackDrawStaminaPercentage = 1 - magicItem.GetTotalEffectValue(MagicEffectType.ModifyDrawStaminaUse, 0.01f);
            float totalAttackDrawStamina = attackDrawStaminaPercentage * item.m_shared.m_attack.m_drawStaminaDrain;

            text.Append($"\n$item_staminahold: " +
                $"<color={attackDrawStaminaColor}>{totalAttackDrawStamina:0.#}/s</color>");
        }
    }

    private void AddBackstab() {
        bool hasBackstabModifier = magicItem.HasEffect(MagicEffectType.ModifyBackstab);
        float totalBackstabBonusMod = magicItem.GetTotalEffectValue(MagicEffectType.ModifyBackstab, 0.01f);
        string magicBackstabColor = hasBackstabModifier ? magicColor : "orange";
        float backstabValue = item.m_shared.m_backstabBonus * (1.0f + totalBackstabBonusMod);
        text.Append($"\n$item_backstab: <color={magicBackstabColor}>{backstabValue:0.#}x</color>");
    }

    private void AddProjectileTooltip() {
        string projectileTooltip = item.GetProjectileTooltip(qualityLevel);
        if (projectileTooltip.Length > 0 && item.m_shared.m_projectileToolTip) {
            text.Append("\n\n");
            text.Append(projectileTooltip);
        }
    }

    private void AddTameOnly() {
        if (item.m_shared.m_tamedOnly) {
            text.Append($"\n<color=orange>$item_tamedonly</color>");
        }
    }

    private void AddKnockback() {
        if (item.m_shared.m_attackForce > 0f) {
            bool hasCloseQuarter = magicItem.HasEffect(MagicEffectType.CloseQuarter, includeSocketed: true);
            string knockbackColor = hasCloseQuarter ? magicColor : "orange";
            text.AppendFormat("\n$item_knockback: <color={0}>{1:0.#}</color>",
                knockbackColor, CloseQuarter.GetKnockback(item, magicItem));
        }
    }
}