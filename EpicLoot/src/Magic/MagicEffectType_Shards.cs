namespace EpicLoot
{
    // New magic-effect identifiers introduced by the Shardstone system. These are declared here so
    // ShardDefinitions (src/ShardStones/Shards.cs) can reference them; the behaviors are stubs (see
    // src/Magic/MagicItemEffects/Shards/) and are inert until implemented. Grouped by subsystem.
    public static partial class MagicEffectType
    {
        // --- Adrenaline resource (new player meter; builds on DodgeBuff / "AdrenalineRush") ---
        public static string DamageTakenGivesAdrenaline = nameof(DamageTakenGivesAdrenaline);
        public static string UseAdrenalineAsStamina = nameof(UseAdrenalineAsStamina);
        public static string EitrUseGivesAdrenaline = nameof(EitrUseGivesAdrenaline);
        public static string BurningAdrenaline = nameof(BurningAdrenaline);
        public static string GainAdrenalineFromHarvesting = nameof(GainAdrenalineFromHarvesting);
        public static string SummonBatWhenActivatingAdrenaline = nameof(SummonBatWhenActivatingAdrenaline);
        public static string AdrenalineFrostWave = nameof(AdrenalineFrostWave);
        public static string GainAdrenalineWhenApplyingPoison = nameof(GainAdrenalineWhenApplyingPoison);
        public static string GainAdrenalineWhenSacrificingHealth = nameof(GainAdrenalineWhenSacrificingHealth);
        // Flat adrenaline pulse plus decay suppression while a storm rages. The name deliberately does not
        // contain "Adrenaline", so ShardEffectDefinitions lists it explicitly to keep ItemHasAdrenaline.
        public static string StormFury = nameof(StormFury);
        public static string AdrenalineIncreasesHealthRegen = nameof(AdrenalineIncreasesHealthRegen);
        public static string AdrenalineCharge = nameof(AdrenalineCharge);

        // --- Perfect-dodge system (Pink) ---
        public static string PerfectDodge = nameof(PerfectDodge);
        public static string PerfectDodgeGivesHealth = nameof(PerfectDodgeGivesHealth);
        public static string PerfectDodgeGivesStamina = nameof(PerfectDodgeGivesStamina);
        public static string PerfectDodgeGivesEitr = nameof(PerfectDodgeGivesEitr);
        public static string DecreaseDodgeCost = nameof(DecreaseDodgeCost);

        // --- Time-of-day (gated on EnvMan.IsDay()/IsNight()) ---
        public static string IncreaseDamageDuringNighttime = nameof(IncreaseDamageDuringNighttime);
        public static string NightStaminaRegenIncrease = nameof(NightStaminaRegenIncrease);
        public static string DamageReductionAtNight = nameof(DamageReductionAtNight);
        public static string IncreaseDamageDuringDaytime = nameof(IncreaseDamageDuringDaytime);
        public static string DayDiscovery = nameof(DayDiscovery);
        public static string DayArmor = nameof(DayArmor);
        public static string DayStaminaRegen = nameof(DayStaminaRegen);
        public static string DayHealthRegen = nameof(DayHealthRegen);
        public static string DayBlocker = nameof(DayBlocker);
        public static string NightBlocker = nameof(NightBlocker);

        // --- Percent pools / regen ---
        public static string PercentHealth = nameof(PercentHealth);
        public static string PercentStamina = nameof(PercentStamina);
        public static string PercentEitr = nameof(PercentEitr);
        public static string HealthGainPerXDamageDone = nameof(HealthGainPerXDamageDone);
        public static string HealthOnEitrUse = nameof(HealthOnEitrUse);
        public static string LifeGainOnHit = nameof(LifeGainOnHit);

        // --- Damage-type conversion / eitr-imbue ---
        public static string PhysToFire = nameof(PhysToFire);
        public static string PhysToFrost = nameof(PhysToFrost);
        public static string PhysToPoison = nameof(PhysToPoison);
        public static string PhysToLightning = nameof(PhysToLightning);
        public static string PhysToFireOnBlock = nameof(PhysToFireOnBlock);
        public static string PhysToFrostOnBlock = nameof(PhysToFrostOnBlock);
        public static string PhysToPoisonOnBlock = nameof(PhysToPoisonOnBlock);
        public static string PhysToLightningOnBlock = nameof(PhysToLightningOnBlock);
        public static string ConvertPhysicalDamageToLightning = nameof(ConvertPhysicalDamageToLightning);
        public static string EitrImbueAttack = nameof(EitrImbueAttack);

        // --- Resource conversion / sustain ---
        public static string StaminaReturnFromEitr = nameof(StaminaReturnFromEitr);
        public static string EnergeticEitr = nameof(EnergeticEitr);
        public static string ConvertEitrCostToStaminaCost = nameof(ConvertEitrCostToStaminaCost);
        public static string ConsumeEitrFirstForBloodCosts = nameof(ConsumeEitrFirstForBloodCosts);
        public static string RunningOnEmpty = nameof(RunningOnEmpty);
        public static string EveryXPointsOfEitrIncreasesStamina = nameof(EveryXPointsOfEitrIncreasesStamina);
        public static string EitrShield = nameof(EitrShield);
        public static string LifeGainOnBlock = nameof(LifeGainOnBlock);
        public static string StaminaGainOnBlock = nameof(StaminaGainOnBlock);
        public static string EitrGainOnBlock = nameof(EitrGainOnBlock);
        public static string Kindling = nameof(Kindling);                                       // Orange

        // --- Weight / movement-penalty scaling ---
        public static string DamageBonusFromPlayerWeight = nameof(DamageBonusFromPlayerWeight);
        public static string StaminaRegenBonusFromPlayerWeight = nameof(StaminaRegenBonusFromPlayerWeight);
        public static string GainMaxStaminaBasedOnPlayerMaxHealth = nameof(GainMaxStaminaBasedOnPlayerMaxHealth);
        public static string GainMaxCarryWeightFromRested = nameof(GainMaxCarryWeightFromRested);
        public static string DamageIncreaseFromMovementPenalty = nameof(DamageIncreaseFromMovementPenalty);
        public static string IncreaseXPGainFromMovementPenalty = nameof(IncreaseXPGainFromMovementPenalty);
        public static string CarryWeightForMovementPenalty = nameof(CarryWeightForMovementPenalty);
        public static string StaminaIncreaseForMovementPenalty = nameof(StaminaIncreaseForMovementPenalty);
        public static string BurdenedBlock = nameof(BurdenedBlock);
        public static string AnchoredBlock = nameof(AnchoredBlock);


        // --- Harvest / gather / economy ---
        public static string IncreaseHarvestDamage = nameof(IncreaseHarvestDamage);
        public static string IncreaseHarvestXPGain = nameof(IncreaseHarvestXPGain);
        public static string BountifulHarvest = nameof(BountifulHarvest);
        // The three coin-economy effects below are no longer assigned by the shard grid -- Golden's kit was
        // re-themed from coins to luck (weapons -> ChanceDoubleDamage, head -> Inspiration, chest ->
        // LuckyLoot). They are kept live, and keep their Config blocks in ShardEffectDefinitions, so a slot
        // assignment alone brings any of them back. Unassigned effects get no definition, so nothing can
        // roll or grant them in the meantime.
        public static string Mercenary = nameof(Mercenary); // was Golden weapons: spend coins for bonus hit damage
        public static string Coinplated = nameof(Coinplated); // was Golden chest: spend coins to absorb incoming damage
        public static string Wager = nameof(Wager); // was Golden head: stake coins on a hit, refunded on a kill
        public static string LuckWhileFishing = nameof(LuckWhileFishing);
        public static string ChanceDoubleDamage = nameof(ChanceDoubleDamage); // Golden weapons
        public static string Inspiration = nameof(Inspiration); // Golden head: chance of bonus skill XP on any XP gain
        public static string LuckyLoot = nameof(LuckyLoot); // Golden chest: chance to multiply a creature's drops
        public static string SailingSpeed = nameof(SailingSpeed);
        public static string PotionEfficacy = nameof(PotionEfficacy);
        public static string IncreaseMeleeSkills = nameof(IncreaseMeleeSkills); // new melee-skill bundle
        public static string IncreaseRangedSkills = nameof(IncreaseRangedSkills); // new ranged-skill bundle
        

        // --- Block
        public static string BloodStaggerBlock = nameof(BloodStaggerBlock); // consume hp reduced stagger dmg reduction while bloock
        public static string BloodBaseBlock = nameof(BloodBaseBlock); // consume hp add base block 
        public static string Warding = nameof(Warding);
        public static string ElementalWarding = nameof(ElementalWarding);
        public static string LuckyBlock = nameof(LuckyBlock); // Golden shield: chance to stagger the attacker on a block
        public static string BlockAsDodgeAsBlock = nameof(BlockAsDodgeAsBlock); // block and dodge bundle BADAB
        public static string BlockAsWoodCuttingAndPickaxes = nameof(BlockAsWoodCuttingAndPickaxes);
        public static string StaggerOnBlock = nameof(StaggerOnBlock);

        // --- Element / status mechanics ---
        public static string IncreaseAllPoisonDamageDone = nameof(IncreaseAllPoisonDamageDone);
        public static string KillsReduceNextBloodCost = nameof(KillsReduceNextBloodCost);
        public static string BloodMagicLevelIncreasesHealthRegen = nameof(BloodMagicLevelIncreasesHealthRegen);
        // ChanceToCritOnHit is no longer assigned by the shard grid (DarkRed's chest slot moved to
        // Bloodrage); kept live, like the coin-economy trio above, so it can be reassigned later.
        public static string ChanceToCritOnHit = nameof(ChanceToCritOnHit);
        public static string Bloodrage = nameof(Bloodrage); // DarkRed chest: stacking damage buff on damage taken
        public static string BloodDrinker = nameof(BloodDrinker); // DarkRed legs: max-health-for-lifesteal tradeoff

        // --- Movement / misc ---
        public static string Trailblazer = nameof(Trailblazer);                                 // Firewalker (unique)
        public static string ReduceFallDamage = nameof(ReduceFallDamage);
        public static string RollCleanse = nameof(RollCleanse);
        public static string StormRider = nameof(StormRider);

        // --- Shoulder (Shoulders slot) effects (one per non-boss shard; Red reuses BulkUp) ---
        public static string StaminaOnKill = nameof(StaminaOnKill);                             // Yellow
        public static string HeartyEitr = nameof(HeartyEitr);                                   // Cyan
        public static string BurningSpeed = nameof(BurningSpeed);                               // Orange
        public static string PerfectDodgeGivesSpeed = nameof(PerfectDodgeGivesSpeed);           // Pink
        public static string NightCarryWeight = nameof(NightCarryWeight);                       // Black
        public static string DaySailingSpeed = nameof(DaySailingSpeed);                         // White
        public static string ArmorFromMovementPenalty = nameof(ArmorFromMovementPenalty);       // Green
        public static string ReduceEitrCost = nameof(ReduceEitrCost);                           // Purple
        public static string ReduceFishingStaminaCost = nameof(ReduceFishingStaminaCost);       // Grey
        public static string ReduceArmorIncreaseDamage = nameof(ReduceArmorIncreaseDamage);     // DarkRed
        public static string PoisonToTrueDamage = nameof(PoisonToTrueDamage);                   // DarkGreen
        public static string IcyWeight = nameof(IcyWeight);                                     // DarkBlue
        public static string GainEitrWhenSacrificingHealth = nameof(GainEitrWhenSacrificingHealth); // DarkPurple
        public static string LuckyCraft = nameof(LuckyCraft);                                   // Golden
        public static string Conduit = nameof(Conduit);                                         // LightBlue
        public static string RestingHealthRegen = nameof(RestingHealthRegen);                   // LightGreen
        public static string TravelLight = nameof(TravelLight);                                 // Peach
        public static string StrikeCausesLightning = nameof(StrikeCausesLightning);       // Stormcaller (unique)
        // BatteringRam is no longer assigned to a shard slot (Peach's shoulder slot moved to TravelLight);
        // kept live, like ChanceDoubleDamage above, so it can be reassigned later. Unassigned effects get no
        // definition from ShardEffectDefinitions, so nothing can roll or grant them until a shard uses them.
        public static string BatteringRam = nameof(BatteringRam);

        // --- Boss signature effects (boss-tier power) ---
        public static string ShockingCharge = nameof(ShockingCharge);
        public static string ForestsAid = nameof(ForestsAid);
        public static string CorpseRot = nameof(CorpseRot);
        public static string IcyRetribution = nameof(IcyRetribution);
        public static string MeteorSummoner = nameof(MeteorSummoner);
        public static string Everflow = nameof(Everflow);
        public static string NecroticFire = nameof(NecroticFire);
    }
}
