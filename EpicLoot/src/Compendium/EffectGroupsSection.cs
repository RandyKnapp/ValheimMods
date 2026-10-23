using System.Collections.Generic;
using System.Linq;
using EpicLoot.LegendarySystem;

namespace EpicLoot.Compendium;

public static class EffectGroupsSection
{
    private const string CategoryColor = "#e0c080ff";
    private const string OtherCategory = "$mod_epicloot_effects_other";

    private class Source
    {
        public MagicItemEffect Effect;
        public ItemRarity Rarity;
        public string Line;
    }

    private static readonly (string Category, string[] Effects)[] Categories =
    {
        ("$mod_epicloot_effects_onhit", new[]
        {
            MagicEffectType.ChanceToCritOnHit, MagicEffectType.ChanceDoubleDamage, MagicEffectType.Executioner,
            MagicEffectType.Opportunist, MagicEffectType.Assassin, MagicEffectType.Sniper, MagicEffectType.HeadHunter,
            MagicEffectType.Firebrand, MagicEffectType.StrikeCausesLightning, MagicEffectType.ChainLightning,
            MagicEffectType.ShockingCharge, MagicEffectType.StormFury, MagicEffectType.StormRider, MagicEffectType.FrostDamageAOE,
            MagicEffectType.IcyRetribution, MagicEffectType.Frostwalker, MagicEffectType.Earthshaker, MagicEffectType.KineticQuake,
            MagicEffectType.BatteringRam, MagicEffectType.OverwhelmingLaunch, MagicEffectType.Paralyze, MagicEffectType.Slow,
            MagicEffectType.Vanish, MagicEffectType.Mercenary, MagicEffectType.NineSteps, MagicEffectType.Conduit,
            MagicEffectType.EitrWeave, MagicEffectType.OffSetAttack, MagicEffectType.Hexen, MagicEffectType.SpiritAnimal,
            MagicEffectType.MeteorSummoner, MagicEffectType.LifeSiphon,
            MagicEffectType.LifeGainOnHit, MagicEffectType.HealthGainPerXDamageDone, MagicEffectType.EitrLeech,
            MagicEffectType.StaminaOnKill, MagicEffectType.LifeAfterKill, MagicEffectType.IncreaseAllPoisonDamageDone, MagicEffectType.PhysToFire,
            MagicEffectType.PhysToFrost, MagicEffectType.PhysToLightning, MagicEffectType.PhysToPoison, MagicEffectType.PoisonToTrueDamage,
            MagicEffectType.ModifyDamage, MagicEffectType.ModifyDamageLowHealth, MagicEffectType.ModifyPhysicalDamage,
            MagicEffectType.ModifyElementalDamage, MagicEffectType.AddBluntDamage, MagicEffectType.AddSlashingDamage,
            MagicEffectType.AddPiercingDamage, MagicEffectType.AddFireDamage, MagicEffectType.AddFrostDamage,
            MagicEffectType.AddLightningDamage, MagicEffectType.AddPoisonDamage, MagicEffectType.AddSpiritDamage,
            MagicEffectType.SpellSword, MagicEffectType.CoinHoarder, MagicEffectType.DodgeBuff, MagicEffectType.EitrInfusion,
            MagicEffectType.EitrImbueAttack, MagicEffectType.IncreaseDamageDuringDaytime, MagicEffectType.IncreaseDamageDuringNighttime,
            MagicEffectType.DamageBonusFromPlayerWeight, MagicEffectType.DamageIncreaseFromMovementPenalty,
            MagicEffectType.IncreaseHarvestDamage, MagicEffectType.ConvertPhysicalDamageToLightning, MagicEffectType.CoilingVenom,
            MagicEffectType.IcyWeight, MagicEffectType.ReduceArmorIncreaseDamage, MagicEffectType.NecroticFire,
        }),
        ("$mod_epicloot_effects_onblock", new[]
        {
            MagicEffectType.StaggerOnBlock, MagicEffectType.LuckyBlock, MagicEffectType.AnchoredBlock, MagicEffectType.BloodBaseBlock,
            MagicEffectType.BloodStaggerBlock, MagicEffectType.BurdenedBlock, MagicEffectType.DayBlocker, MagicEffectType.NightBlocker,
            MagicEffectType.PhysToFireOnBlock, MagicEffectType.PhysToFrostOnBlock, MagicEffectType.PhysToLightningOnBlock,
            MagicEffectType.PhysToPoisonOnBlock, MagicEffectType.EitrBarrier, MagicEffectType.EitrShield, MagicEffectType.Warding,
            MagicEffectType.ElementalWarding, MagicEffectType.Bulwark, MagicEffectType.Immovable, MagicEffectType.ReduceStaggerTaken,
            MagicEffectType.LifeGainOnBlock, MagicEffectType.EitrGainOnBlock, MagicEffectType.StaminaGainOnBlock,
        }),
        ("$mod_epicloot_effects_ondodge", new[]
        {
            MagicEffectType.PerfectDodge, MagicEffectType.PerfectDodgeGivesHealth, MagicEffectType.PerfectDodgeGivesStamina,
            MagicEffectType.PerfectDodgeGivesEitr, MagicEffectType.PerfectDodgeGivesSpeed, MagicEffectType.DecreaseDodgeCost,
            MagicEffectType.ModifyDodgeStaminaUse,
        }),
        ("$mod_epicloot_effects_adrenaline", new[]
        {
            MagicEffectType.AdrenalineCharge, MagicEffectType.AdrenalineFrostWave, MagicEffectType.AdrenalineIncreasesHealthRegen,
            MagicEffectType.BurningAdrenaline, MagicEffectType.DamageTakenGivesAdrenaline, MagicEffectType.EitrUseGivesAdrenaline,
            MagicEffectType.GainAdrenalineFromHarvesting, MagicEffectType.GainAdrenalineWhenApplyingPoison,
            MagicEffectType.GainAdrenalineWhenSacrificingHealth, MagicEffectType.UseAdrenalineAsStamina,
            MagicEffectType.SummonBatWhenActivatingAdrenaline,
        }),
        ("$mod_epicloot_effects_protection", new[]
        {
            MagicEffectType.HealthOnEitrUse, MagicEffectType.StaminaReturnFromEitr, MagicEffectType.GainEitrWhenSacrificingHealth,
            MagicEffectType.Everflow, MagicEffectType.RunningOnEmpty, MagicEffectType.Undying, MagicEffectType.Kindling,
            MagicEffectType.DamageReductionAtNight, MagicEffectType.DayArmor, MagicEffectType.ArmorFromMovementPenalty,
            MagicEffectType.Coinplated, MagicEffectType.FeatherFall, MagicEffectType.ReduceFallDamage,
            MagicEffectType.IncreaseHeatResistance, MagicEffectType.Warmth, MagicEffectType.Waterproof,
        }),
        ("$mod_epicloot_effects_ranged", new[]
        {
            MagicEffectType.QuickDraw, MagicEffectType.DoubleMagicShot, MagicEffectType.TripleBowShot, MagicEffectType.ExplosiveArrows,
            MagicEffectType.PiercingBolts, MagicEffectType.ElementalArchery, MagicEffectType.Artillery, MagicEffectType.AmmoConservation,
            MagicEffectType.ModifyProjectileSpeed, MagicEffectType.ModifyFireRate, MagicEffectType.ModifyMagicFireRate,
            MagicEffectType.ReduceEitrCost, MagicEffectType.ConvertEitrCostToStaminaCost, MagicEffectType.ConsumeEitrFirstForBloodCosts,
            MagicEffectType.KillsReduceNextBloodCost, MagicEffectType.ShieldThrow, MagicEffectType.Throwable, MagicEffectType.RecallWeapon,
            MagicEffectType.ModifySummonDamage, MagicEffectType.ModifySummonHealth,
        }),
        ("$mod_epicloot_effects_movement", new[]
        {
            MagicEffectType.ModifySprintStaminaUse, MagicEffectType.ModifyJumpStaminaUse, MagicEffectType.DoubleJump,
            MagicEffectType.BurningSpeed, MagicEffectType.Trailblazer, MagicEffectType.SailingSpeed, MagicEffectType.DaySailingSpeed,
            MagicEffectType.ModifyGrapplePullSpeed, MagicEffectType.ModifyGrappleFlightTime, MagicEffectType.Megingjord,
        }),
        ("$mod_epicloot_effects_gathering", new[]
        {
            MagicEffectType.IncreaseMiningDrop, MagicEffectType.IncreaseTreeDrop, MagicEffectType.BountifulHarvest,
            MagicEffectType.HelsHarvest, MagicEffectType.ForestsAid, MagicEffectType.GreenThumb, MagicEffectType.QuarryMark,
            MagicEffectType.CorpseRot, MagicEffectType.LuckyCraft, MagicEffectType.FreeBuild, MagicEffectType.ModifyBuildDistance,
            MagicEffectType.RollCleanse,
            MagicEffectType.LuckWhileFishing, MagicEffectType.ReduceFishingStaminaCost,
        }),
        ("$mod_epicloot_effects_utility", new[]
        {
            MagicEffectType.ModifyNoise, MagicEffectType.ModifyPickupRange, MagicEffectType.ModifyDiscoveryRadius,
            MagicEffectType.DayDiscovery, MagicEffectType.ModifyWispRange, MagicEffectType.Glowing, MagicEffectType.Comfortable,
            MagicEffectType.Apportation, MagicEffectType.Luck, MagicEffectType.LuckyLoot, MagicEffectType.Riches,
            MagicEffectType.Prosperity, MagicEffectType.Wager, MagicEffectType.Andvaranaut, MagicEffectType.Wishbone,
            MagicEffectType.DvergerCirclet, MagicEffectType.DecreaseForsakenCooldown, MagicEffectType.DecreaseMeadCooldown,
            MagicEffectType.InstantMead, MagicEffectType.AutoMead, MagicEffectType.PotionEfficacy, MagicEffectType.ModifyLowHealth,
        }),
    };

    private static readonly HashSet<string> Hidden = new()
    {
        MagicEffectType.ModifyDurability, MagicEffectType.Indestructible, MagicEffectType.Weightless, MagicEffectType.ReduceWeight,
    };


    private static readonly Dictionary<string, string> CategoryOf = Categories
        .SelectMany(c => c.Effects.Select(e => (e, c.Category)))
        .GroupBy(x => x.e)
        .ToDictionary(g => g.Key, g => g.First().Category);

    public static void Build(MagicPages instance)
    {
        Player player = Player.m_localPlayer;
        if (!player)
        {
            return;
        }

        Dictionary<string, List<Source>> byEffect = new();
        foreach (ItemDrop.ItemData item in player.GetMagicEquipment())
        {
            foreach (MagicItemEffect effect in item.GetMagicItem().GetEffects(null, includeSocketed: true))
            {
                Add(byEffect, effect, item.GetRarity(),
                    $"{MagicItem.GetEffectText(effect, item.GetRarity(), false)} ({item.GetDecoratedName()})");
            }
        }

        foreach (LegendarySetProgress progress in SetBonusEvaluator.GetEquippedSetProgress(player))
        {
            foreach (SetBonusInfo bonus in progress.Set.SetBonuses)
            {
                ItemRarity? tier = bonus.Effect == null ? null : progress.GetTier(bonus);
                if (!tier.HasValue)
                {
                    continue;
                }

                MagicItemEffect effect = new MagicItemEffect(bonus.Effect.Type, SetBonusEvaluator.GetBonusValue(bonus, tier.Value));
                MagicItemEffectDefinition definition = MagicItemEffectDefinitions.Get(effect.EffectType);
                Add(byEffect, effect, tier.Value, $"{MagicItem.GetEffectText(definition, effect.EffectValue)} ({progress.Set.Name})");
            }
        }

        Dictionary<string, List<string>> byCategory = new();
        foreach (string effectType in byEffect.Keys)
        {
            if (StatSourceLines.Shown.Contains(effectType) || Hidden.Contains(effectType))
            {
                continue;
            }

            string category = CategoryOf.TryGetValue(effectType, out string found) ? found : OtherCategory;
            if (!byCategory.TryGetValue(category, out List<string> list))
            {
                list = new List<string>();
                byCategory.Add(category, list);
            }

            list.Add(effectType);
        }

        if (byCategory.Count == 0)
        {
            return;
        }

        instance.MagicPagesTextArea.Add("");
        instance.MagicPagesTextArea.Add($"<size={MagicPages.LARGE_FONT_SIZE}>$mod_epicloot_misc_header</size>");
        foreach (string category in Categories.Select(x => x.Category).Concat(new[] { OtherCategory }))
        {
            if (!byCategory.TryGetValue(category, out List<string> effectTypes))
            {
                continue;
            }

            instance.MagicPagesTextArea.Add($"<size={MagicPages.MEDIUM_FONT_SIZE}><color={CategoryColor}>{category}</color></size>");
            foreach (string effectType in effectTypes)
            {
                AddGroup(instance, effectType, byEffect[effectType]);
            }
        }
    }

    private static void Add(Dictionary<string, List<Source>> byEffect, MagicItemEffect effect, ItemRarity rarity, string line)
    {
        if (!byEffect.TryGetValue(effect.EffectType, out List<Source> list))
        {
            list = new List<Source>();
            byEffect.Add(effect.EffectType, list);
        }

        list.Add(new Source { Effect = effect, Rarity = rarity, Line = line });
    }

    private static void AddGroup(MagicPages instance, string effectType, List<Source> sources)
    {
        MagicItemEffectDefinition definition = MagicItemEffectDefinitions.Get(effectType);
        float sum = sources.Sum(x => x.Effect.EffectValue);
        string totalText = MagicItem.GetEffectText(definition, sum);
        ItemRarity highest = (ItemRarity)sources.Max(x => (int)x.Rarity);
        bool dormant = StatSourceLines.IsDormant(effectType);
        string lineColor = dormant ? StatSourceLines.DormantColor : StatSourceLines.SourceColor;

        string title = dormant
            ? $"<color={StatSourceLines.DormantColor}><i>{totalText}</i></color>"
            : $"<color={EpicLoot.GetRarityColor(highest)}>{totalText}</color>";
        string[] content = sources.Select(x => $" <color={lineColor}>- {x.Line}</color>").ToArray();
        instance.MagicPagesTextArea.Add($"<size={MagicPages.FONT_SIZE}>{title}</size>", content);
    }
}
