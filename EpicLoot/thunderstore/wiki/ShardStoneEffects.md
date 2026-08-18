# ShardStone Effects by Slot

All data is sourced from [`config/shardstones.json`](config/shardstones.json), keyed by shard color → slot → effect.
Standard shards (Core/Dark/Light) define one effect per broad slot; Boss shards use a single **uniform** effect that
applies to any socket. Effect power scales by rarity (Magic → Rare → Epic → Legendary → Mythic).

## Core shards

| Shard | Melee Wpn | Ranged Wpn | Magic Wpn | Shield | Head | Chest | Legs | Shoulders | Trinket | Utility |
|---|---|---|---|---|---|---|---|---|---|---|
| **Red** (Vitality) | LifeGainOnHit | LifeGainOnHit | HealthOnEitrUse | LifeGainOnBlock | ModifyHealthRegen | PercentHealth | IncreaseHealth | BulkUp | DamageTakenGivesAdrenaline | AddHealthRegen |
| **Yellow** (Stamina) | ModifyAttackStaminaUse | ModifyDrawStaminaUse | EnergeticEitr | StaminaGainOnBlock | ModifyStaminaRegen | PercentStamina | IncreaseStamina | StaminaOnKill | UseAdrenalineAsStamina | ModifySprintStaminaUse |
| **Cyan** (Eitr) | EitrImbueAttack | EitrImbueAttack | ModifyAttackEitrUse | EitrGainOnBlock | PercentEitr | IncreaseEitr | ModifyEitrRegen | HeartyEitr | EitrUseGivesAdrenaline | EitrShield |
| **Orange** (Fire) | AddFireDamage | AddFireDamage | AddFireDamage | PhysToFireOnBlock | AddFireResistancePercentage | PhysToFire | Kindling | BurningSpeed | BurningAdrenaline | IncreaseHeatResistance |
| **Pink** (Dodge) | PerfectDodgeGivesStamina | PerfectDodgeGivesStamina | PerfectDodgeGivesEitr | BlockAsDodgeAsBlock | DecreaseDodgeCost | ReduceFallDamage | DodgeBuff | PerfectDodgeGivesSpeed | PerfectDodge | RollCleanse |
| **Black** (Night) | IncreaseDamageDuringNighttime  | IncreaseDamageDuringNighttime | IncreaseDamageDuringNighttime  | NightBlocker | NightStaminaRegenIncrease | DamageReductionAtNight | AddKnivesSkill  | NightCarryWeight  | SummonBatWhenActivatingAdrenaline | ModifyNoise  |
| **White**  (Day) | IncreaseDamageDuringDaytime | IncreaseDamageDuringDaytime | IncreaseDamageDuringDaytime | DayBlocker | DayDiscovery | DayArmor | DayStaminaRegen | DaySailingSpeed | DayHealthRegen | AddCrafterSkills |
| **Green** (Movement) | DamageIncreaseFromMovementPenalty | DamageIncreaseFromMovementPenalty | DamageIncreaseFromMovementPenalty | AnchoredBlock | IncreaseXPGainFromMovementPenalty | CarryWeightForMovementPenalty | StaminaIncreaseForMovementPenalty | ArmorFromMovementPenalty | AddMovementSkills | ModifyJumpStaminaUse |
| **Purple** (Eitr/Blood) | EitrLeech | EitrLeech | ModifyMagicFireRate | ElementalWarding | DartingThoughts | ConsumeEitrFirstForBloodCosts | EveryXPointsOfEitrIncreasesStamina | ReduceEitrCost | ConvertEitrCostToStaminaCost | RunningOnEmpty |
| **Grey** (Harvest) | IncreaseHarvestDamage | IncreaseHarvestDamage | IncreaseHarvestDamage | BlockAsWoodCuttingAndPickaxes | IncreaseMiningDrop | AddFishingSkill | IncreaseTreeDrop | ReduceFishingStaminaCost | GainAdrenalineFromHarvesting | IncreaseHarvestXPGain |

## Dark shards

| Shard | Melee Wpn | Ranged Wpn | Magic Wpn | Shield | Head | Chest | Legs | Shoulders | Trinket | Utility |
|---|---|---|---|---|---|---|---|---|---|---|
| **DarkRed** (Berserk) |  IncreaseMeleeSkills | IncreaseRangedSkills | AddBluntDamage | BloodBaseBlock | HeadHunter | Bloodrage | BloodDrinker | ReduceArmorIncreaseDamage | AdrenalineCharge | OffSetAttack |
| **DarkGreen** (Poison) | AddPoisonDamage | AddPoisonDamage | AddPoisonDamage | PhysToPoisonOnBlock | AddPoisonResistancePercentage | PhysToPoison | AddBlockingSkill | PoisonToTrueDamage | GainAdrenalineWhenApplyingPoison | IncreaseAllPoisonDamageDone |
| **DarkBlue** (Frost) | AddFrostDamage | AddFrostDamage | AddFrostDamage | PhysToFrostOnBlock | AddFrostResistancePercentage | PhysToFrost | AddElementalMagicSkill | IcyWeight | AdrenalineFrostWave | Warmth |
| **DarkPurple** (Blood) | ModifyAttackHealthUse | ModifyAttackHealthUse | ModifyAttackHealthUse | BloodStaggerBlock | KillsReduceNextBloodCost | ReflectDamage | BloodMagicLevelIncreasesHealthRegen | GainEitrWhenSacrificingHealth | GainAdrenalineWhenSacrificingHealth | AddBloodMagicSkill |
| **Golden** (Luck) | ChanceDoubleDamage | ChanceDoubleDamage | ChanceDoubleDamage | LuckyBlock | Inspiration | LuckyLoot | LuckWhileFishing | LuckyCraft | Luck | Riches |

## Light shards

| Shard | Melee Wpn | Ranged Wpn | Magic Wpn | Shield | Head | Chest | Legs | Shoulders | Trinket | Utility |
|---|---|---|---|---|---|---|---|---|---|---|
| **LightBlue** (Lightning) | AddLightningDamage | AddLightningDamage | AddLightningDamage | PhysToLightningOnBlock | AddLightningResistancePercentage | PhysToLightning | StormRider | Conduit | StormFury | ConvertPhysicalDamageToLightning |
| **LightGreen** (Regeneration) | HealthGainPerXDamageDone | HealthGainPerXDamageDone | HealthGainPerXDamageDone | Warding | PotionEfficacy | Comfortable | AddPickaxesSkill | RestingHealthRegen | AdrenalineIncreasesHealthRegen | BountifulHarvest |
| **Peach** (Weight) | DamageBonusFromPlayerWeight | DamageBonusFromPlayerWeight | DamageBonusFromPlayerWeight | BurdenedBlock | GainMaxStaminaBasedOnPlayerMaxHealth | StaminaRegenBonusFromPlayerWeight | GainMaxCarryWeightFromRested | TravelLight | SailingSpeed | AddCarryWeight |

## Boss shards (uniform — one effect on any slot)

Boss shards use `UniformEffect`, they provide their designated effect regardless of slot. They are also unique, only one boss shard can be equipped.

| Shard | Rarity | Effect (all slots) |
|---|---|---|
| **Eikthyr** | Rare | ShockingCharge |
| **Elder** | Rare | ForestsAid |
| **Bonemass** | Epic | CorpseRot |
| **Moder** | Epic | IcyRetribution |
| **Yagluth** | Legendary | MeteorSummoner |
| **Queen** | Legendary | Everflow |
| **Fader** | Mythic | NecroticFire |

## Unique shards (uniform — one effect on any slot)

`ShardCategory.Unique` is exclusive, but exclusivity is enforced **per category**
([`ShardSocketManager.CheckExclusiveCategory`](src/ShardStones/ShardSocketManager.cs)) — so a unique shard
and a boss shard may be worn together, one of each, and never two uniques.

| Shard | Rarity | Effect (all slots) |
|---|---|---|
| **Firewalker** | Epic | Trailblazer |
| **Stormcaller** | Epic | StrikeCausesLightning |

## Effect reference

Every effect the shard grid above actually grants, with its in-game tooltip and description lifted verbatim from
[`localizations/English.json`](localizations/English.json) (`mod_epicloot_me_<effect>_display` / `_desc`).
`{0}`, `{1}`, ... and **X** are the rarity-scaled values from
[`config/shardstones.json`](config/shardstones.json); an effect with more than one placeholder pulls its extra
numbers from its `Config` block in
[`ShardEffectDefinitions`](src/Magic/MagicItemEffects/Helpers/ShardEffectDefinitions.cs). Effect ids are globally
unique — no effect is shared by two shards.

### Core shard effects

| Effect | Shard | Slot(s) | Tooltip | What it does |
|---|---|---|---|---|
| `LifeGainOnHit` | Red | Melee Wpn, Ranged Wpn | Heal {0:0.#} on Hit | Heal {0:0.#} health each time you hit an enemy with this weapon. |
| `HealthOnEitrUse` | Red | Magic Wpn | Heal {0} per {1} Eitr Spent | Heal {0} health for every {1} eitr you spend with this weapon. |
| `LifeGainOnBlock` | Red | Shield | Heal {0} hp on block | Heal {0} health on block |
| `ModifyHealthRegen` | Red | Head | Health Regen +{0:0.#}% | Increase the player's health regeneration by **X**%. |
| `PercentHealth` | Red | Chest | +{0}% Max Health | Increase max health by {0}%. |
| `IncreaseHealth` | Red | Legs | Health +{0:0} | Increase the player's base maximum health by **X**. |
| `BulkUp` | Red | Shoulders | Bulk: +{0}% Max HP, -{1}% Regen | Converts a portion of your regeneration into maximum health. |
| `DamageTakenGivesAdrenaline` | Red | Trinket | Gain {0} Adrenaline When Damaged | Gain {0} adrenaline each time you take damage. |
| `AddHealthRegen` | Red | Utility | Health Regen +{0:0.#}/tick | Increase the player's base health regeneration by **X** health per tick. |
| `ModifyAttackStaminaUse` | Yellow | Melee Wpn | Attack Stamina Use -{0:0.#}% | Reduce the player's attack stamina usage by **X**%. |
| `ModifyDrawStaminaUse` | Yellow | Ranged Wpn | Draw Stamina -{0:0.#}% | Reduce the draw stamina usage of the magic item by **X**%. |
| `EnergeticEitr` | Yellow | Magic Wpn | +{0}% of Max Stamina as Max Eitr | Your vigor feeds your magic: increases maximum Eitr by {0}% of your maximum Stamina. |
| `StaminaGainOnBlock` | Yellow | Shield | Gain {0} stamina on block | Gain {0} stamina on block. |
| `ModifyStaminaRegen` | Yellow | Head | Stamina Regen +{0:0.#}% | Increase the player's stamina regeneration by +**X**%. |
| `PercentStamina` | Yellow | Chest | +{0}% Max Stamina | Increase max stamina by {0}%. |
| `IncreaseStamina` | Yellow | Legs | Stamina +{0:0} | Increase the player's base maximum stamina by **X**. |
| `StaminaOnKill` | Yellow | Shoulders | Restore {0}% Stamina on Kill | Restore {0}% of max stamina each time you kill an enemy. |
| `UseAdrenalineAsStamina` | Yellow | Trinket | Adrenaline as Stamina: {0}% Efficiency | When stamina runs short, convert adrenaline into stamina at {0}% efficiency, spending up to your entire adrenaline pool. |
| `ModifySprintStaminaUse` | Yellow | Utility | Sprint Stamina Use -{0:0.#}% | Reduce the player's sprint stamina usage by **X**%. |
| `EitrImbueAttack` | Cyan | Melee Wpn, Ranged Wpn | Eitr Imbue: +{0}% Physical as Spirit | Spend eitr to add spirit damage equal to {0}% of the hit's physical damage. |
| `ModifyAttackEitrUse` | Cyan | Magic Wpn | Attack Eitr Use -{0:0.#}% | Reduce the player's attack eitr usage by **X**%. |
| `EitrGainOnBlock` | Cyan | Shield | Gain {0} eitr on block | Gain {0} eitr on block. |
| `PercentEitr` | Cyan | Head | +{0}% Max Eitr | Increase max eitr by {0}%. |
| `IncreaseEitr` | Cyan | Chest | Eitr +{0:0} | Increase the player's base maximum eitr by **X**. |
| `ModifyEitrRegen` | Cyan | Legs | Eitr Regen +{0:0.#}% | Increase Eitr Regen by {0:0.#}% |
| `HeartyEitr` | Cyan | Shoulders | +{0}% of Max Health as Max Eitr | Your heartiness feeds your magic: increases maximum Eitr by {0}% of your maximum Health. |
| `EitrUseGivesAdrenaline` | Cyan | Trinket | Gain Adrenaline: {0}% of Eitr Spent | Gain adrenaline equal to {0}% of the eitr you spend. |
| `EitrShield` | Cyan | Utility | Absorb {0}% of Hits with Eitr | Spend eitr to absorb {0}% of an incoming hit. |
| `AddFireDamage` | Orange | Melee Wpn, Ranged Wpn, Magic Wpn | Imbue Fire Damage {0:0.#}% | Add **X**% of the total unenchanted damage of this weapon as fire damage to attacks made by the magic weapon. |
| `PhysToFireOnBlock` | Orange | Shield | Convert {0}% Incoming Physical to Fire on block | Move {0}% of incoming physical damage to fire before resistances on block |
| `AddFireResistancePercentage` | Orange | Head | Fire Resistance {0:0.#}% | The player reduces all fire damage taken by +**X**%. This effect stacks additively with other damage reduction effects equipped by the player. |
| `PhysToFire` | Orange | Chest | Convert {0}% Incoming Physical to Fire | Move {0}% of incoming physical damage to fire before resistances. |
| `Kindling` | Orange | Legs | Kindling: Restore {0} Stamina per {1} Fire Damage Taken | Restore {0} stamina for every {1} fire damage you take. |
| `BurningSpeed` | Orange | Shoulders | +{0}% Move Speed While Burning | Move {0}% faster while you are on fire. |
| `BurningAdrenaline` | Orange | Trinket | Gain Adrenaline: {0}% of Fire Damage | Gain adrenaline equal to {0}% of the fire damage you deal. |
| `IncreaseHeatResistance` | Orange | Utility | Heat Resistance +{0:0}% | Increase the player's heat resistance by **X**%. Additive, not multiplicative. |
| `PerfectDodgeGivesStamina` | Pink | Melee Wpn, Ranged Wpn | Perfect Dodge Restores {0}% Stamina | Restore {0}% of max stamina on a perfect dodge. |
| `PerfectDodgeGivesEitr` | Pink | Magic Wpn | Perfect Dodge Restores {0}% Eitr | Restore {0}% of max eitr on a perfect dodge. |
| `BlockAsDodgeAsBlock` | Pink | Shield | Adds +{1} block skill and +{2} dodge skill | Adds +{0}% of blocking skill as dodge skill and +{0}% of dodge skill as blocking skill. This bonus is rounded down to whole values |
| `DecreaseDodgeCost` | Pink | Head | -{0}% Dodge Stamina Cost | Reduce the stamina cost of dodge rolls by {0}%. |
| `ReduceFallDamage` | Pink | Chest | -{0} Fall Damage | Reduce fall damage taken by a flat {0}. |
| `DodgeBuff` | Pink | Legs | Dodge Damage Buff +{0}% | Dodging an enemy melee attack grants a buff increasing damage for 10 seconds. Can not be refreshed until the buff expires or is removed. |
| `PerfectDodgeGivesSpeed` | Pink | Shoulders | Perfect Dodge: Dodge Agility (+{0}% Speed, 1s) | A perfect dodge grants Dodge Agility: +{0}% movement speed for 1 second. |
| `PerfectDodge` | Pink | Trinket | Perfect Dodge: +{0}% Stamina Regen per Stack (max {1}) | A perfect dodge grants Dodge Momentum: +{0}% stamina regeneration for 10 seconds, stacking up to {1} times. Each perfect dodge refreshes the duration. |
| `RollCleanse` | Pink | Utility | Dodge Cleanses {0}s of DoTs | Each dodge roll removes {0} seconds from your poison and burning effects. |
| `IncreaseDamageDuringNighttime` | Black | Melee Wpn, Ranged Wpn, Magic Wpn | +{0}% Damage at Night | +{0}% weapon damage while it is night. |
| `NightBlocker` | Black | Shield | Gain {0}% Block XP during Nighttime | Gain {0}% increased block experience during Nighttime. |
| `NightStaminaRegenIncrease` | Black | Head | +{0}% Stamina Regen at Night | +{0}% stamina regeneration while it is night. |
| `DamageReductionAtNight` | Black | Chest | -{0}% Damage Taken at Night | Reduce incoming damage by {0}% while it is night. |
| `AddKnivesSkill` | Black | Legs | Knives Skill +{0} | Increases the player's knives skill level by +**X**. This may cause the skill level to increase above 100. |
| `NightCarryWeight` | Black | Shoulders | +{0}% Carry Weight at Night | Increases max carry weight by {0}% during the night. |
| `SummonBatWhenActivatingAdrenaline` | Black | Trinket | Summon a Bat at Full Adrenaline | When your adrenaline fills, summon a bat to fight for you. |
| `ModifyNoise` | Black | Utility | Noise -{0:0.#}% | Reduce the distance player noise travels +**X**%. |
| `IncreaseDamageDuringDaytime` | White | Melee Wpn, Ranged Wpn, Magic Wpn | +{0}% Damage in Daylight | +{0}% weapon damage while it is day. |
| `DayBlocker` | White | Shield | Gain {0}% Block XP during Daytime | Gain {0}% increased block experience during Daytime. |
| `DayDiscovery` | White | Head | +{0}% Exploration Radius in Daylight | Widen your map-discovery radius by {0}% while it is day. |
| `DayArmor` | White | Chest | +{0}% Armor in Daylight | +{0}% armor while it is day. |
| `DayStaminaRegen` | White | Legs | +{0}% Stamina Regen in Daylight | +{0}% stamina regeneration while it is day. |
| `DaySailingSpeed` | White | Shoulders | +{0}% Sailing Speed by Day | Increases your ship's sailing speed by {0}% during the day. |
| `DayHealthRegen` | White | Trinket | +{0}% Health Regen in Daylight | +{0}% health regeneration while it is day. |
| `AddCrafterSkills` | White | Utility | Crafting Skills +{0} | Increases the player's crafting and cooking skill levels by +**X**. This may cause the skill level to increase above 100. |
| `DamageIncreaseFromMovementPenalty` | Green | Melee Wpn, Ranged Wpn, Magic Wpn | +Up to {0}% Damage from Heavy Gear | Weapon damage scaling with your gear's speed penalty, up to +{0}%. |
| `AnchoredBlock` | Green | Shield | +{0} Base Block per 1% Equipment Speed Penalty | Adds {0} base block for every 1% of movement speed penalty from your equipment. The bonus is not affected by explicit sources of movement speed such as potions, status effects, and magic effects. |
| `IncreaseXPGainFromMovementPenalty` | Green | Head | +Up to {0}% Skill XP from Heavy Gear | Skill XP scaling with your gear's speed penalty, up to +{0}%. |
| `CarryWeightForMovementPenalty` | Green | Chest | +Up to {0}% Carry Weight from Heavy Gear | Max carry weight scaling with your gear's speed penalty, up to +{0}%. |
| `StaminaIncreaseForMovementPenalty` | Green | Legs | +Up to {0}% Max Stamina from Heavy Gear | Max stamina scaling with your gear's speed penalty, up to +{0}%. |
| `ArmorFromMovementPenalty` | Green | Shoulders | +Up to {0}% Armor from Heavy Gear | Armor scaling with your gear's speed penalty, up to +{0}%. |
| `AddMovementSkills` | Green | Trinket | Movement Skills +{0} | Increases the player's run, jump, swim, and sneak skill levels by +**X**. This may cause the skills' level to increase above 100. |
| `ModifyJumpStaminaUse` | Green | Utility | Jump Stamina Use -{0:0.#}% | Reduce the player's jump stamina usage by **X**%. |
| `EitrLeech` | Purple | Melee Wpn, Ranged Wpn | Eitr Leech {0:0.#}% | The player gains eitr on hit equal to **X**% of total damage done with each attack. |
| `ModifyMagicFireRate` | Purple | Magic Wpn | Fire Rate +{0:0.#}% | Increase the rate at which magic weapons fire projectiles by +**X**%. |
| `ElementalWarding` | Purple | Shield | Block: Absorb Elemental with Eitr, up to {0}% of max | Allows for {0}% of maximum eitr to be used on block to mitigate any elemental (fire, frost, lightning) damage taken from blocked attacks. This mitigation is calculated after the block and before resistances and armor calculations take place. Consumes eitr equal to damage mitigated |
| `DartingThoughts` | Purple | Head | Darting Thoughts +{0} | Increases Eitr Regen +**X%** while decreasing max Eitr +**X/2**%. |
| `ConsumeEitrFirstForBloodCosts` | Purple | Chest | Pay {0}% of Blood Costs from Eitr | Pay {0}% of blood-magic health costs from eitr first. |
| `EveryXPointsOfEitrIncreasesStamina` | Purple | Legs | +Max Stamina from {0}% of Max Eitr | Increase max stamina by {0}% of your max eitr. |
| `ReduceEitrCost` | Purple | Shoulders | -{0}% Eitr Costs | Reduces all Eitr costs by {0}%. |
| `ConvertEitrCostToStaminaCost` | Purple | Trinket | Pay {0}% of Eitr Costs from Stamina | Pay {0}% of every eitr cost from stamina instead. |
| `RunningOnEmpty` | Purple | Utility | Second Wind: {0}% of Max Health | When stamina runs out, burn up to {0}% of your max health for that much stamina. 30 second cooldown. |
| `IncreaseHarvestDamage` | Grey | Melee Wpn, Ranged Wpn, Magic Wpn | +{0}% Harvest Damage | +{0}% chopping and mining damage. |
| `BlockAsWoodCuttingAndPickaxes` | Grey | Shield | Adds +{1} block skill | Adds +{0}% of woodcutting and pickaxes skills after all modifiers as block skill. This bonus is rounded down to whole values |
| `IncreaseMiningDrop` | Grey | Head | Mining Drop +{0:0} | Increase total amount of drops from ore and stone mining by **X**. |
| `AddFishingSkill` | Grey | Chest | Fishing Skill +{0} | Increases the player's fishing skill level by +**X**. This may cause the skill level to increase above 100. |
| `IncreaseTreeDrop` | Grey | Legs | Lumberjacking Drop +{0:0} | Increase total amount of drops from trees and logs by **X**. |
| `ReduceFishingStaminaCost` | Grey | Shoulders | -{0}% Fishing Stamina | Reduces the stamina cost of fishing by {0}%. |
| `GainAdrenalineFromHarvesting` | Grey | Trinket | Gain {0} Adrenaline on Harvest | Gain {0} adrenaline each time you strike a tree or rock. |
| `IncreaseHarvestXPGain` | Grey | Utility | +{0}% Gathering Skill XP | +{0}% XP for Woodcutting and Pickaxes. |

### Dark shard effects

| Effect | Shard | Slot(s) | Tooltip | What it does |
|---|---|---|---|---|
| `IncreaseMeleeSkills` | DarkRed | Melee Wpn | +{0} Melee Skills | +{0} to all melee weapon skills. |
| `IncreaseRangedSkills` | DarkRed | Ranged Wpn | +{0} Ranged Skills | +{0} to all ranged weapon skills. |
| `AddBluntDamage` | DarkRed | Magic Wpn | Blunt Damage +{0:0.#}% | Add **X**% of the total unenchanted damage of this weapon as blunt damage to attacks made by the magic weapon. |
| `BloodBaseBlock` | DarkRed | Shield | Raising your guard costs 5% of max HP. Adds {0} to your base block | Damages self for 5% of max HP as true damage each time you raise your guard. This is charged once per block, not per blocked hit. Adds {0} to your base block. |
| `HeadHunter` | DarkRed | Head | Headhunter +{0} | Increases chance to drop trophies by +**X**%. |
| `Bloodrage` | DarkRed | Chest | Bloodrage: +{0}% Damage per Stack (max {1}) | Taking damage sends you into a rage: +{0}% damage on every attack per stack, stacking up to {1} times over 10 seconds. Each hit you take refreshes the duration. |
| `BloodDrinker` | DarkRed | Legs | -{1}% Max Health, +{0}% Lifesteal | Reduces your maximum health by {1}% (at least 10), healing you for {0}% of the damage you deal. |
| `ReduceArmorIncreaseDamage` | DarkRed | Shoulders | -{0}% Armor, +{0}% Damage | Reduces your armor by {0}% but increases weapon damage by {0}%. |
| `AdrenalineCharge` | DarkRed | Trinket | -{0}% Forsaken Cooldown at Full Adrenaline | When your adrenaline fills, reduce the remaining cooldown on your Forsaken Power by {0}%. |
| `OffSetAttack` | DarkRed | Utility | OffSet Attack +{0}% | Your third attack in an attack string provides stagger immunity and damage reduction if timed against an enemy melee attack. |
| `AddPoisonDamage` | DarkGreen | Melee Wpn, Ranged Wpn, Magic Wpn | Imbue Poison Damage {0:0.#}% | Add **X**% of the total unenchanted damage of this weapon as poison damage to attacks made by the magic weapon |
| `PhysToPoisonOnBlock` | DarkGreen | Shield | Convert {0}% Incoming Physical to Poison on block | Move {0}% of incoming physical damage to poison before resistances on block. |
| `AddPoisonResistancePercentage` | DarkGreen | Head | Poison Resistance {0:0.#}% | The player reduces all poison damage taken by +**X**%. This effect stacks additively with other damage reduction effects equipped by the player. |
| `PhysToPoison` | DarkGreen | Chest | Convert {0}% Incoming Physical to Poison | Move {0}% of incoming physical damage to poison before resistances. |
| `AddBlockingSkill` | DarkGreen | Legs | Blocking Skill +{0} | Increases the player's blocking skill level by +**X**. This may cause the skill level to increase above 100. |
| `PoisonToTrueDamage` | DarkGreen | Shoulders | {0}% Poison done as target's weakest damage type | Converts {0}% of your poison damage into the target's least-resisted damage type. |
| `GainAdrenalineWhenApplyingPoison` | DarkGreen | Trinket | Gain {0} Adrenaline every {1}s per Poisoned Foe Nearby (diminishing) | Every {1} seconds, gain {0} adrenaline for each enemy within {2}m suffering from your poison, with diminishing returns for each additional foe. |
| `IncreaseAllPoisonDamageDone` | DarkGreen | Utility | +{0}% Poison Damage | +{0}% to all poison damage you deal. |
| `AddFrostDamage` | DarkBlue | Melee Wpn, Ranged Wpn, Magic Wpn | Imbue Frost Damage {0:0.#}% | Add **X**% of the total unenchanted damage of this weapon as frost damage to attacks made by the magic weapon. |
| `PhysToFrostOnBlock` | DarkBlue | Shield | Convert {0}% Incoming Physical to Frost on block | Move {0}% of incoming physical damage to frost before resistances on block. |
| `AddFrostResistancePercentage` | DarkBlue | Head | Frost Resistance {0:0.#}% | The player reduces all frost damage taken by +**X**%. This effect does not provide protection from environmental effects. This effect stacks additively with other damage reduction effects equipped by the player. |
| `PhysToFrost` | DarkBlue | Chest | Convert {0}% Incoming Physical to Frost | Move {0}% of incoming physical damage to frost before resistances. |
| `AddElementalMagicSkill` | DarkBlue | Legs | Elemental Magic Skill +{0} | Increases the player's elemental magic skill level by +**X**. This may cause the skill level to increase above 100. |
| `IcyWeight` | DarkBlue | Shoulders | +Up to {0}% Frost Damage from Heavy Gear | Adds frost damage scaling with your gear's speed penalty, up to +{0}% of weapon damage. |
| `AdrenalineFrostWave` | DarkBlue | Trinket | Frost Wave: Slow Enemies within {1}m for {0}s | When your adrenaline fills, a wave of frost chills every enemy within {1} meters, slowing them for {0} seconds. |
| `Warmth` | DarkBlue | Utility | Warmth | While wearing the magical equipment, the player will not get cold. Will not remove an existing freezing effect. Does not grant cold resistance. |
| `ModifyAttackHealthUse` | DarkPurple | Melee Wpn, Ranged Wpn, Magic Wpn | Attack Health Use -{0:0.#}% | Reduce the player's attack health usage by **X**%. |
| `BloodStaggerBlock` | DarkPurple | Shield | Consumes 5% of max HP to block. Reduce stagger damage from blocked hits by {0}% | Consumes 5% of max HP to block. Reduce stagger damage from blocked hits by {0}%. This effect is active for the entire duration of any block |
| `KillsReduceNextBloodCost` | DarkPurple | Head | Kills Cut Next Blood Cost by {0}% | Each kill banks a {0}% reduction to your next blood-magic cost. |
| `ReflectDamage` | DarkPurple | Chest | Thorn Damage +{0:0.#}% | Reflect **X**% of damage back to attacker |
| `BloodMagicLevelIncreasesHealthRegen` | DarkPurple | Legs | +Up to {0}% Health Regen by Blood Magic | Health regen scaling with your Blood Magic skill, up to +{0}%. |
| `GainEitrWhenSacrificingHealth` | DarkPurple | Shoulders | Gain Eitr: {0}% of Health Spent | Gain Eitr equal to {0}% of the health you spend on blood-magic costs. |
| `GainAdrenalineWhenSacrificingHealth` | DarkPurple | Trinket | Gain Adrenaline: {0}% of Health Spent | Gain adrenaline equal to {0}% of the health you spend on blood-magic costs. |
| `AddBloodMagicSkill` | DarkPurple | Utility | Blood Magic Skill +{0} | Increases the player's blood magic skill levels by +**X**. This may cause the skills' level to increase above 100. |
| `ChanceDoubleDamage` | Golden | Melee Wpn, Ranged Wpn, Magic Wpn | {0}% Chance to Double Damage | {0}% chance for a hit to deal double damage. |
| `StaggerOnBlock` | Golden | Shield | {0}% chance to stagger enemy on block | {0}% chance to stagger enemy on block |
| `Inspiration` | Golden | Head | Inspiration: {1}% Chance of +{0} Skill XP | Every scrap of experience you earn carries a {1}% chance to inspire you, granting {0} bonus experience to a random skill you have already trained past level 2. Your weakest skills are far likelier to be chosen, and a single flash of inspiration can carry a skill through more than one level. |
| `LuckyLoot` | Golden | Chest | Lucky Loot: {0}% Chance of {1}x Drops | Each creature slain nearby has a {0}% chance to drop {1}x its usual loot and to roll {2}-{3} extra times on its magic item table. Trophies and one-per-player rewards are never multiplied. |
| `LuckWhileFishing` | Golden | Legs | Lucky Fishing: {0}% Bonus Catch & Treasure | While fishing, {0}% chance to reel in an extra fish ({1}% of those land a triple catch), and a separate {0}% chance to hook bonus treasure. Higher values reach richer finds and stop turning up junk. |
| `LuckyCraft` | Golden | Shoulders | {0}% Chance to Save Each Material | When crafting, each required material has a {0}% chance not to be consumed. |
| `Luck` | Golden | Trinket | Luck +{0} | Increase the chance to find higher rarity magic items. |
| `Riches` | Golden | Utility | Riches +{0:0.#}% | Slain enemies have a +**X**% increased chance to drop coins or treasure. |

### Light shard effects

| Effect | Shard | Slot(s) | Tooltip | What it does |
|---|---|---|---|---|
| `AddLightningDamage` | LightBlue | Melee Wpn, Ranged Wpn, Magic Wpn | Imbue Lightning Damage {0:0.#}% | Add **X**% of the total unenchanted damage of this weapon as lightning damage to attacks made by the magic weapon. |
| `PhysToLightningOnBlock` | LightBlue | Shield | Convert {0}% Incoming Physical to Lightning on block | Move {0}% of incoming physical damage to lightning before resistances on block. |
| `AddLightningResistancePercentage` | LightBlue | Head | Lightning Resistance {0:0.#}% | The player reduces all lightning damage taken by +**X**%. This effect stacks additively with other damage reduction effects equipped by the player. |
| `PhysToLightning` | LightBlue | Chest | Convert {0}% Incoming Physical to Lightning | Move {0}% of incoming physical damage to lightning before resistances. |
| `StormRider` | LightBlue | Legs | +{0}% Movement Speed in Storms | +{0}% movement speed while a storm rages. |
| `Conduit` | LightBlue | Shoulders | Conduit: Restore {0} Eitr per {1} Lightning Damage Dealt | Restore {0} eitr for every {1} lightning damage you deal. |
| `StormFury` | LightBlue | Trinket | +{0} Adrenaline every {1}s in Storms, No Adrenaline Decay | Every {1} seconds while a storm rages, gain {0} adrenaline. Your adrenaline does not decay for as long as the storm lasts. |
| `ConvertPhysicalDamageToLightning` | LightBlue | Utility | Convert {0}% Weapon Physical to Lightning | Move {0}% of this weapon's physical damage to lightning. |
| `HealthGainPerXDamageDone` | LightGreen | Melee Wpn, Ranged Wpn, Magic Wpn | Heal {0} per {1} Damage Dealt | Heal {0} health for every {1} damage you deal with this weapon. |
| `Warding` | LightGreen | Shield | Block: Absorb Physical with Stamina, up to {0}% of max | Allows for {0}% of maximum stamina to be used on block to mitigate any physical (pierce, blunt, slash) damage taken from blocked attacks. This mitigation is calculated after the block and before resistances and armor calculations take place. Consumes stamina equal to damage mitigated |
| `PotionEfficacy` | LightGreen | Head | +{0}% Potion Duration | Consumed potions and meads last {0}% longer. |
| `Comfortable` | LightGreen | Chest | Comfort +{0} | When resting and wearing the magic equipment, the player's comfort level is increased by +**X**. |
| `AddPickaxesSkill` | LightGreen | Legs | Pickaxes Skill +{0} | Increases the player's pickaxes skill level by +**X**. This may cause the skill level to increase above 100. |
| `RestingHealthRegen` | LightGreen | Shoulders | +{0}% Health Regen While Rested | Increases health regeneration by {0}% while you are Rested. |
| `AdrenalineIncreasesHealthRegen` | LightGreen | Trinket | Adrenaline Surge: +{0}% Health Regen for {1}s | When your adrenaline fills, gain +{0}% health regen for {1} seconds. Refilling refreshes the buff rather than stacking it. |
| `BountifulHarvest` | LightGreen | Utility | Bountiful Harvest: {0}% Bonus Yield | {0}% chance to gather a bonus item when harvesting. |
| `DamageBonusFromPlayerWeight` | Peach | Melee Wpn, Ranged Wpn, Magic Wpn | +Up to {0}% Damage by Pack Weight | Weapon damage scaling with how loaded your pack is, up to +{0}%. |
| `BurdenedBlock` | Peach | Shield | Adds +{0} base block per 50 Pack Weight over 300 | Adds +{0} base block per 50 Pack Weight over 300 |
| `GainMaxStaminaBasedOnPlayerMaxHealth` | Peach | Head | +Max Stamina from {0}% of Max Health | Increase max stamina by {0}% of your max health. |
| `StaminaRegenBonusFromPlayerWeight` | Peach | Chest | +Up to {0}% Stamina Regen by Pack Weight | Stamina regen scaling with how loaded your pack is, up to +{0}%. |
| `GainMaxCarryWeightFromRested` | Peach | Legs | +{0} Carry Weight per Rested Level | While Rested, increases max carry weight by {0} for each level of your Rested comfort. |
| `TravelLight` | Peach | Shoulders | +{0}% Move Speed, -{1} Carry Weight | Travel light: move {0}% faster, but your max carry weight is reduced by {1}. |
| `SailingSpeed` | Peach | Trinket | +{0}% Sailing Speed | +{0}% ship sail speed while aboard. |
| `AddCarryWeight` | Peach | Utility | Carry Weight +{0} | Increase the player's maximum carry weight capacity by **X**. |

### Boss shard effects

| Effect | Shard | Slot(s) | Tooltip | What it does |
|---|---|---|---|---|
| `ShockingCharge` | Eikthyr | All slots | Shocking Charge | Your combat hits build charges. At max charge, the next hit unleashes a forward lightning shockwave that deals a portion of your previous hits' damage. |
| `ForestsAid` | Elder | All slots | Forest's Aid | When you are struck, ensnaring roots erupt and immobilize nearby enemies. |
| `CorpseRot` | Bonemass | All slots | Corpse Rot | Enemies you kill can burst in a cloud of venom, poisoning nearby foes. |
| `IcyRetribution` | Moder | All slots | Icy Retribution | When you are struck, detonate a frost nova around you. Cooldown scales with rarity. |
| `MeteorSummoner` | Yagluth | All slots | Meteor Summoner: {0}x Hit Damage | Every {1} weapon hits, your next hit calls down a fiery meteor on the target that deals {0}x that hit's damage as fire. |
| `Everflow` | Queen | All slots | Queen's Everflow | Killing a creature grants a stacking buff that boosts your health, stamina and eitr regeneration. |
| `NecroticFire` | Fader | All slots | Necrotic Fire | Strips all physical damage from your weapon and adds that full amount as both fire and poison damage. |

### Unique shard effects

| Effect | Shard | Slot(s) | Tooltip | What it does |
|---|---|---|---|---|
| `Trailblazer` | Firewalker | All slots | Burning Trail: {0} Fire per Tick | Leave a burning trail while running that deals {0} fire damage per tick. |
| `StrikeCausesLightning` | Stormcaller | All slots | {0}% Chance to Call Lightning | Your hits have a {0}% chance to call down a lightning strike on the target. |

### Unassigned effects

These occupy no slot in any shard, so nothing can grant them today — their strings are listed here for
completeness. See [Implemented but unassigned effects](#implemented-but-unassigned-effects) for what each
would take to revive.

| Effect | Tooltip | What it does |
|---|---|---|
| `Wager` | Wager: Stake {0} Coins for +{1} Damage, Refunded on a Kill | Every hit stakes {0} coins to add {1} flat damage. Kill the target and the stake is refunded in full; leave it alive and the coins are lost. Does nothing if you cannot cover the stake. |
| `Mercenary` | Mercenary: Spend {1} + {2}% Coins for +{3}% Damage, +{0}% per 1000 Spent | Each hit spends {1} coins plus {2}% of your purse, dealing +{3}% damage plus a further +{0}% for every 1000 coins spent. Bonus damage past +{4}% suffers sharply diminishing returns, approaching a ceiling of +{5}%. Does nothing if you cannot pay. |
| `Coinplated` | Coinplated: Absorb Hits with Coins ({0} per Coin) | Commit up to {1}% of your coins to each incoming hit, absorbing {0} damage per coin spent. Only the coins actually needed are spent, so small hits cost little. |
| `ChanceToCritOnHit` | {0}% Chance to Crit for {1}x Damage | {0}% chance for a hit to critically strike for double damage. |
| `PerfectDodgeGivesHealth` | Perfect Dodge Restores {0}% Health | Restore {0}% of max health on a perfect dodge. |
| `StaminaReturnFromEitr` | Refund {0}% of Eitr Spent as Stamina | Restore stamina equal to {0}% of the eitr you spend. |
| `BatteringRam` | Battering Ram: Blunt from Weight | Running into enemies deals blunt damage based on your speed and carried weight (scaled by {0}). |

## Implemented but unassigned effects

Seven effect ids are declared in [`MagicEffectType_Shards.cs`](src/Magic/MagicEffectType_Shards.cs) and have
behavior code checked in, but occupy **no slot in any shard above** and have no entry in any
[`config/overhauls/*/magiceffects.json`](config/overhauls/) either. That combination makes them completely
inert: [`ShardEffectDefinitions`](src/Magic/MagicItemEffects/Helpers/ShardEffectDefinitions.cs) builds its
definition list by walking the shard grid, so an unassigned id gets no `MagicItemEffectDefinition` at all —
nothing can roll it, no shard can grant it, and `GetTotalActiveMagicEffectValue` returns 0 for every player.
All seven still have their `_display`/`_desc` strings in
[`localizations/English.json`](localizations/English.json), so assigning one to a grid slot is enough to
make it show up correctly in tooltips.

They are **not** equally close to working. Six are one config edit away; one also needs code restored.

| EffectType | Code | Hook status | To re-enable |
|---|---|---|---|
| **Wager** | [Wager.cs](src/Magic/MagicItemEffects/Shards/Wager.cs) | Live | Assign a slot |
| **Mercenary** | [Mercenary.cs](src/Magic/MagicItemEffects/Shards/Mercenary.cs) | Live | Assign a slot |
| **Coinplated** | [Coinplated.cs](src/Magic/MagicItemEffects/Shards/Coinplated.cs) | Live | Assign a slot |
| **ChanceToCritOnHit** | [ChanceToCritOnHit.cs](src/Magic/MagicItemEffects/Shards/ChanceToCritOnHit.cs) | Live | Assign a slot |
| **PerfectDodgeGivesHealth** | [PerfectDodgeEffects.cs:51](src/Magic/MagicItemEffects/Shards/PerfectDodgeEffects.cs#L51) | Live | Assign a slot |
| **StaminaReturnFromEitr** | [StaminaReturnFromEitr.cs](src/Magic/MagicItemEffects/Shards/StaminaReturnFromEitr.cs) | Live | Assign a slot |
| **BatteringRam** | [BatteringRam.cs](src/Magic/MagicItemEffects/Shards/BatteringRam.cs) | **Whole file commented out** | Uncomment, then assign a slot |

### Ready to assign — config only

The first four were vacated together when Golden's kit was re-themed from coins to luck and DarkRed's
Chest moved to `Bloodrage`. All four are cheaper to revive than the rest of this list: besides keeping
their code and their dispatcher call sites, they keep their per-effect `Config` blocks in
[`ShardEffectDefinitions.EffectConfigs`](src/Magic/MagicItemEffects/Helpers/ShardEffectDefinitions.cs),
which sit dormant (`BuildDefinition` is only reached for effects the grid actually uses) until a slot
assignment brings them back with their tuning intact.

- **Wager** — stakes coins on each hit for flat bonus damage, refunded on a kill. Called from the
  `Character.Damage` dispatcher, both as an outgoing modifier and as the on-kill refund
  ([SharedCharacterDamagePatch.cs:34, :62](src/Magic/MagicItemEffects/Helpers/SharedCharacterDamagePatch.cs#L34)).
  Vacated when Golden's Head slot moved to `Inspiration`.
- **Mercenary** — spends coins per hit for a percentage damage bonus on a soft-capped curve. Called from
  [SharedCharacterDamagePatch.cs:33](src/Magic/MagicItemEffects/Helpers/SharedCharacterDamagePatch.cs#L33).
  Vacated when Golden's three weapon slots moved to `ChanceDoubleDamage`.
- **Coinplated** — commits a share of the purse to absorbing each incoming hit. Called from
  [SharedCharacterRpcDamagePatch.cs:48](src/Magic/MagicItemEffects/Helpers/SharedCharacterRpcDamagePatch.cs#L48).
  Vacated when Golden's Chest slot moved to `LuckyLoot`.
- **ChanceToCritOnHit** — flat proc chance to crit for 2x. Called from
  [SharedCharacterDamagePatch.cs:36](src/Magic/MagicItemEffects/Helpers/SharedCharacterDamagePatch.cs#L36).
  Vacated when DarkRed's Chest slot moved to `Bloodrage`. Note it is the same shape as
  `ChanceDoubleDamage` (Golden weapons) with a different intent, so reassigning it to a weapon slot
  risks a near-duplicate.
- **PerfectDodgeGivesHealth** — restores a % of max health on a perfect dodge. Already called from
  `SharedPerfectDodgeRewardPatch`, alongside its Stamina/Eitr/Speed siblings which *are* assigned (Pink's
  weapon and Shoulders slots). It is the only member of that family without a home.
- **StaminaReturnFromEitr** — refunds a % of spent eitr as stamina. Self-contained `Player.UseEitr` postfix,
  no dispatcher involvement. Never assigned to a slot at any point.

### Needs code restored as well

- **BatteringRam** — blunt damage from running into enemies, scaled by carried weight and speed. The file's
  own comment records why it was disabled: the per-frame `Player.Update` patch it needs was "mildly
  expensive" for an effect nothing granted.

## Known quirks, accepted deliberately

Recorded here so they read as decisions rather than oversights.

- **`ChanceDoubleDamage` and projectiles.** The effect is weapon-scoped — it reads the `MagicItem` of the
  weapon the shard is socketed into, via `MagicEffectsHelper.GetActiveWeapon`, so only that weapon procs.
  For bows and staves the hit resolves when the projectile lands, so the weapon is re-read at that
  moment; firing and then swapping weapons mid-flight reads the wrong one. `Executioner` solves this by
  stamping its multiplier onto the projectile's ZDO, which is the fix if it ever matters here.
- **`Bloodrage` scales chop and pickaxe damage.** `SE_Bloodrage.ModifyAttack` applies the bonus with
  `HitData.DamageTypes.Modify(float)`, which also scales `m_chop` and `m_pickaxe` — so raging speeds up
  tree-felling and mining slightly. Vanilla's own `SE_Stats.ModifyAttack` behaves identically and the
  ceiling is +25%, so a per-damage-type multiplier is more code than the quirk is worth.
- **`Bloodrage` can proc on an avoided hit.** It hangs off the `Character.RPC_Damage` postfix, and Harmony
  runs postfixes even when the dispatcher prefix cancels the method for `AvoidDamageTaken`.
  `DamageTakenGivesAdrenaline` has the identical behaviour today, so this matches its sibling rather than
  special-casing.
- **`LuckyLoot` needs a `CharacterDrop`.** The proc is rolled inside the `CharacterDrop.GenerateDropList`
  postfix, so a creature with no `CharacterDrop` component (or with `Ragdoll.m_dropItems` off) can never
  proc it — including the bonus magic-item half. See the header comment in
  [`LuckyLoot.cs`](src/Magic/MagicItemEffects/Shards/LuckyLoot.cs) for the full two-path timing diagram
  and why the decision has to travel through the ragdoll's ZDO.
- **`Inspiration` is the one effect whose grid value is not a percent.** Its 10/15/20/25/30 ramp is a count
  of raw skill-accumulator points, read with no `0.01f` scale. "Fixing" that for consistency would nerf
  the effect 100x; the warning is repeated at the top of
  [`Inspiration.cs`](src/Magic/MagicItemEffects/Shards/Inspiration.cs). Its proc chance is the percent,
  and lives in the effect's `Config` block so it can be retuned without a rebuild.

## Notes on slot resolution

Slot resolution happens at socket time in [`Shards.GetShardEffect` / `ResolveCategory`](src/ShardStones/Shards.cs).

- The config only defines the **broad group** keys above (`MeleeWeapon`, `RangedWeapon`, `MagicWeapon`,
  `Shield`, `Head`, `Chest`, `Legs`, `Shoulders`, `Trinket`, `Utility`).
- `ResolveCategory` first maps a host item to a *fine* type (Swords, Bows, Bucklers, etc.), then falls back to its
  group — so, e.g., a sword and a club both pick up the `MeleeWeapon` effect since no fine-type effects are defined.
- The three fine shield slots (`Bucklers`, `RoundShields`, `TowerShields`) all fall back to the `Shield`
  group, and no shard defines an effect for any of them — every shield of a given shard gets the same
  effect regardless of subtype.
- The fine type itself comes from [`ItemTypeClassifier`](src/GatedItemType/ItemTypeClassifier.cs), the mod-wide
  answer to "which `iteminfo.json` type is this item?" — the item's configured entry when it has one, else a
  raw-field heuristic. `ItemInfoTypeToSlot` is only the shard-specific mapping over that shared vocabulary.
- An item that cannot be classified at all (unlisted *and* unrecognizable) yields **no slot**: the shard sits in
  the socket inert rather than being handed some other slot's effect.
