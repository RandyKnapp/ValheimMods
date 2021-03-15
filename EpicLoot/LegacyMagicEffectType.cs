namespace EpicLoot
{
    /*public enum LegacyMagicEffectType
    {
        DvergerCirclet,
        Megingjord,
        Wishbone,
        ModifyDamage,
        ModifyPhysicalDamage,
        ModifyElementalDamage,
        ModifyDurability,
        ReduceWeight,
        RemoveSpeedPenalty,
        ModifyBlockPower,
        ModifyParry,
        ModifyArmor,
        ModifyBackstab,
        IncreaseHealth,
        IncreaseStamina,
        ModifyHealthRegen,
        ModifyStaminaRegen,
        AddBluntDamage,
        AddSlashingDamage,
        AddPiercingDamage,
        AddFireDamage,
        AddFrostDamage,
        AddLightningDamage,
        AddPoisonDamage,
        AddSpiritDamage,
        AddFireResistance,
        AddFrostResistance,
        AddLightningResistance,
        AddPoisonResistance,
        AddSpiritResistance,
        ModifyMovementSpeed,
        ModifySprintStaminaUse,
        ModifyJumpStaminaUse,
        ModifyAttackStaminaUse,
        ModifyBlockStaminaUse,
        Indestructible,
        Weightless,
        AddCarryWeight
    }

    public static class LegacyMagicEffectTypeHelper
    {
        public static bool IsLegacyEffect(MagicItemEffect effect)
        {
            return effect.IntType >= 0;
        }

        public static string GetTypeFromLegacyType(int legacyIntType)
        {
            return GetTypeFromLegacyType((LegacyMagicEffectType)legacyIntType);
        }

        private static string GetTypeFromLegacyType(LegacyMagicEffectType legacyType)
        {
            return legacyType.ToString();
        }

        public static void UpdateLegacyMagicItemEffect(MagicItemEffect effect)
        {
            if (IsLegacyEffect(effect))
            {
                effect.EffectType = GetTypeFromLegacyType(effect.IntType);
                effect.IntType = -1;
            }
        }
    }*/
}
