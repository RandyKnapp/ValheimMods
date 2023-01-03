# EpicLoot Data v0.9.3

*Author: RandyKnapp*
*Source: [Github](https://github.com/RandyKnapp/ValheimMods/tree/main/EpicLoot)*

# Magic Effect Count Weights Per Rarity

Each time a **MagicItem** is rolled a number of **MagicItemEffects** are added based on its **Rarity**. The percent chance to roll each number of effects is found on the following table. These values are hardcoded.

The raw weight value is shown first, followed by the calculated percentage chance in parentheses.

|Rarity|1|2|3|4|5|6|
|--|--|--|--|--|--|--|
|Magic|80 (80%)|18 (18%)|2 (2%)| | | |
|Rare| |80 (80%)|18 (18%)|2 (2%)| | |
|Epic| | |80 (80%)|18 (18%)|2 (2%)| |
|Legendary| | | |80 (80%)|18 (18%)|2 (2%)|
|Mythic| | | |50 (55.6%)|30 (33.3%)|10 (11.1%)|

# MagicItemEffect List

The following lists all the built-in **MagicItemEffects**. MagicItemEffects are defined in `magiceffects.json` and are parsed and added to `MagicItemEffectDefinitions` on Awake. EpicLoot uses an string for the types of magic effects. You can add your own new types using your own identifiers.

Listen to the event `MagicItemEffectDefinitions.OnSetupMagicItemEffectDefinitions` (which gets called in `EpicLoot.Awake`) to add your own using instances of MagicItemEffectDefinition.

  * **Display Text:** This text appears in the tooltip for the magic item, with {0:?} replaced with the rolled value for the effect, formatted using the shown C# string format.
  * **Requirements:** A set of requirements.
    * **Flags:** A set of predefined flags to check certain weapon properties. The list of flags is: `NoRoll, ExclusiveSelf, ItemHasPhysicalDamage, ItemHasElementalDamage, ItemUsesDurability, ItemHasNegativeMovementSpeedModifier, ItemHasBlockPower, ItemHasNoParryPower, ItemHasParryPower, ItemHasArmor, ItemHasBackstabBonus, ItemUsesStaminaOnAttack`
    * **ExclusiveEffectTypes:** This effect may not be rolled on an item that has already rolled on of these effects
    * **AllowedItemTypes:** This effect may only be rolled on items of a the types in this list. When this list is empty, this is usually done because this is a special effect type added programmatically  or currently not allowed to roll. Options are: `Helmet, Chest, Legs, Shoulder, Utility, Bow, OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Shield, Tool, Torch`
    * **ExcludedItemTypes:** This effect may only be rolled on items that are not one of the types on this list.
    * **AllowedRarities:** This effect may only be rolled on an item of one of these rarities. Options are: `Magic, Rare, Epic, Legendary, Mythic`
    * **ExcludedRarities:** This effect may only be rolled on an item that is not of one of these rarities.
    * **AllowedSkillTypes:** This effect may only be rolled on an item that uses one of these skill types. Options are: `Swords, Knives, Clubs, Polearms, Spears, Blocking, Axes, Bows, ElementalMagic, BloodMagic, Unarmed, Pickaxes, Crossbows, Fishing, Ride`
    * **ExcludedSkillTypes:** This effect may only be rolled on an item that does not use one of these skill types.
    * **AllowedItemNames:** This effect may only be rolled on an item with one of these names. Use the unlocalized shared name, i.e.: `$item_sword_iron`
    * **ExcludedItemNames:** This effect may only be rolled on an item that does not have one of these names.
    * **CustomFlags:** A set of any arbitrary strings for future use
  * **Value Per Rarity:** This effect may only be rolled on items of a rarity included in this table. The value is rolled using a linear distribution between Min and Max and divisible by the Increment.

## DvergerCirclet

> **Display Text:** Perpetual Lightsource
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> 
> ***Notes:*** *Can't be rolled. Just added to make a Magic Item version of this item.*

## Megingjord

> **Display Text:** Carry Weight +150
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> 
> ***Notes:*** *Can't be rolled. Just added to make a Magic Item version of this item.*

## Wishbone

> **Display Text:** Secret Finding
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> 
> ***Notes:*** *Can't be rolled. Just added to make a Magic Item version of this item.*

## Andvaranaut

> **Display Text:** Haldor's Treasure Finder
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> 
> ***Notes:*** *Can't be rolled. Just added to make a Magic Item version of this item.*

## ModifyDamage

> **Display Text:** All Damage +{0:0.#}%
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|20|5|
> |Rare|15|25|5|
> |Epic|20|30|5|
> |Legendary|25|40|5|
> 
> ***Notes:*** *Can't be rolled. Too powerful?*

## ModifyPhysicalDamage

> **Display Text:** Physical Damage +{0:0.#}%
> 
> **Prefixes:** Strong, Powerful, Mighty, Heavy, Forceful, Wicked, Fighter's, Warrior
> **Suffixes:** Strength, Power, Might, Force, the Warrior
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemHasPhysicalDamage`
> > **ExclusiveEffectTypes:** `AddBluntDamage, AddSlashingDamage, AddPiercingDamage`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|

## ModifyElementalDamage

> **Display Text:** Elemental Damage +{0:0.#}%
> 
> **Prefixes:** Energized, Empowered, Charged, Intensified
> **Suffixes:** Energy, Intensity, Brilliance
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemHasElementalDamage`
> > **ExclusiveEffectTypes:** `AddFireDamage, AddFrostDamage, AddLightningDamage`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff`
> > **ExcludedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|

## ModifyDurability

> **Display Text:** Durability +{0:0.#}%
> 
> **Prefixes:** Sturdy, Stout, Robust
> **Suffixes:** Sturdiness, Robustness, Stability, the Ox
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemUsesDurability`
> > **ExclusiveEffectTypes:** `Indestructible`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder`
> > **AllowedRarities:** `Magic, Rare, Epic`

## ReduceWeight

> **Display Text:** Weight -{0:0.#}% 
> 
> **Prefixes:** Light, Slender
> **Suffixes:** the Feather
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **ExclusiveEffectTypes:** `Weightless`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder`
> > **AllowedRarities:** `Magic, Rare, Epic`

## RemoveSpeedPenalty

> **Display Text:** No Movement Speed Penalty
> 
> **Prefixes:** Agile, Easy, Graceful
> **Suffixes:** Agility, Grace
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemHasNegativeMovementSpeedModifier`
> > **ExclusiveEffectTypes:** `ModifyMovementSpeed`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder`

## ModifyBlockPower

> **Display Text:** Block +{0:0.#}%
> 
> **Prefixes:** Stopping, Defender's
> **Suffixes:** Stopping, the Defender
> 
> **Allowed Item Types:** Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemHasBlockPower`
> > **AllowedItemTypes:** `Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|

## ModifyParry

> **Display Text:** Parry +{0:0.#}%
> 
> **Prefixes:** Elusive, Rebuking
> **Suffixes:** Repelling
> 
> **Allowed Item Types:** Shield, TwoHandedWeapon
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemHasParryPower`
> > **AllowedItemTypes:** `Shield, TwoHandedWeapon`
> > **ExcludedSkillTypes:** `Pickaxes`
> > **ExcludedItemNames:** `$item_shield_serpentscale`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|

## ModifyArmor

> **Display Text:** Armor +{0:0.#}%
> 
> **Prefixes:** Heavy, Protected
> **Suffixes:** Protection
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemHasArmor`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|

## ModifyBackstab

> **Display Text:** Backstab +{0:0.#}%
> 
> **Prefixes:** Assassin's, Stealthy
> **Suffixes:** the Assassin, Stealth
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemHasBackstabBonus`
> > **AllowedSkillTypes:** `Knives, Bows`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|

## IncreaseHealth

> **Display Text:** Health +{0:0}
> 
> **Prefixes:** Healthy
> **Suffixes:** Health
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|25|5|
> |Rare|15|30|5|
> |Epic|20|35|5|
> |Legendary|25|50|5|

## IncreaseStamina

> **Display Text:** Stamina +{0:0}
> 
> **Prefixes:** Enduring
> **Suffixes:** Endurance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Tool
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Tool`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|25|5|
> |Rare|15|30|5|
> |Epic|20|35|5|
> |Legendary|25|50|5|

## IncreaseEitr

> **Display Text:** Eitr +{0:0}
> 
> **Prefixes:** Eitr
> **Suffixes:** Eitr
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Staff
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Staff`
> > **AllowedRarities:** `Epic, Legendary`

## ModifyHealthRegen

> **Display Text:** Health Regen +{0:0.#}%
> 
> **Prefixes:** Vigorous
> **Suffixes:** Vigor
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|

## AddHealthRegen

> **Display Text:** Health Regen +{0:0.#}/tick
> 
> **Prefixes:** Rejuvenating
> **Suffixes:** Rejuvenation
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|0.5|1.5|0.5|
> |Rare|1|3|0.5|
> |Epic|2|4.5|0.5|
> |Legendary|3|6|0.5|

## ModifyStaminaRegen

> **Display Text:** Stamina Regen +{0:0.#}%
> 
> **Prefixes:** Recovering
> **Suffixes:** Recovery
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Tool
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Tool`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|

## ModifyEitrRegen

> **Display Text:** Eitr Regen +{0:0.#}%
> 
> **Prefixes:** Focusing
> **Suffixes:** Focus
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Staff
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Staff`
> > **AllowedRarities:** `Epic, Legendary`

## AddBluntDamage

> **Display Text:** Add Blunt +{0:0.#}%
> 
> **Prefixes:** Brute
> **Suffixes:** Bludgeoning
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **ExclusiveEffectTypes:** `AddBluntDamage, AddSlashingDamage, AddPiercingDamage, AddFireDamage, AddFrostDamage, AddLightningDamage, AddPoisonDamage, AddSpiritDamage`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Torch`
> > **ExcludedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddSlashingDamage

> **Display Text:** Add Slash +{0:0.#}%
> 
> **Prefixes:** Sharp, Keen
> **Suffixes:** Slashing, Cutting
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **ExclusiveEffectTypes:** `AddBluntDamage, AddSlashingDamage, AddPiercingDamage, AddFireDamage, AddFrostDamage, AddLightningDamage, AddPoisonDamage, AddSpiritDamage`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Torch`
> > **ExcludedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddPiercingDamage

> **Display Text:** Add Pierce +{0:0.#}%
> 
> **Prefixes:** Spiked, Barbed
> **Suffixes:** Piercing
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **ExclusiveEffectTypes:** `AddBluntDamage, AddSlashingDamage, AddPiercingDamage, AddFireDamage, AddFrostDamage, AddLightningDamage, AddPoisonDamage, AddSpiritDamage`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Torch`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddFireDamage

> **Display Text:** Imbue Fire +{0:0.#}%
> 
> **Prefixes:** Blazing
> **Suffixes:** Fire
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **ExclusiveEffectTypes:** `AddBluntDamage, AddSlashingDamage, AddPiercingDamage, AddFireDamage, AddFrostDamage, AddLightningDamage, AddPoisonDamage, AddSpiritDamage`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff`
> > **ExcludedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddFrostDamage

> **Display Text:** Imbue Frost +{0:0.#}%
> 
> **Prefixes:** Frigid
> **Suffixes:** Frost
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **ExclusiveEffectTypes:** `AddBluntDamage, AddSlashingDamage, AddPiercingDamage, AddFireDamage, AddFrostDamage, AddLightningDamage, AddPoisonDamage, AddSpiritDamage`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff`
> > **ExcludedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddLightningDamage

> **Display Text:** Imbue Lightning +{0:0.#}%
> 
> **Prefixes:** Shocking
> **Suffixes:** Lightning
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **ExclusiveEffectTypes:** `AddBluntDamage, AddSlashingDamage, AddPiercingDamage, AddFireDamage, AddFrostDamage, AddLightningDamage, AddPoisonDamage, AddSpiritDamage`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff`
> > **ExcludedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddPoisonDamage

> **Display Text:** Imbue Poison +{0:0.#}%
> 
> **Prefixes:** Infected
> **Suffixes:** Poison
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **ExclusiveEffectTypes:** `AddBluntDamage, AddSlashingDamage, AddPiercingDamage, AddFireDamage, AddFrostDamage, AddLightningDamage, AddPoisonDamage, AddSpiritDamage`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff`
> > **ExcludedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|2|4|1|
> |Rare|4|8|1|
> |Epic|8|15|1|
> |Legendary|15|20|1|

## AddSpiritDamage

> **Display Text:** Imbue Spirit +{0:0.#}%
> 
> **Prefixes:** Spirit
> **Suffixes:** the Spirits
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **ExclusiveEffectTypes:** `AddBluntDamage, AddSlashingDamage, AddPiercingDamage, AddFireDamage, AddFrostDamage, AddLightningDamage, AddPoisonDamage, AddSpiritDamage`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff`
> > **ExcludedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddFireResistance

> **Display Text:** Gain fire resistance
> 
> **Prefixes:** Extinguishing
> **Suffixes:** Fire Resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`

## AddFrostResistance

> **Display Text:** Gain frost resistance
> 
> **Prefixes:** Insulated
> **Suffixes:** Frost Resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`

## AddLightningResistance

> **Display Text:** Gain lightning resistance
> 
> **Prefixes:** Grounded
> **Suffixes:** Lightning Resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`

## AddPoisonResistance

> **Display Text:** Gain poison resistance
> 
> **Prefixes:** Curing
> **Suffixes:** Poison Resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`

## AddSpiritResistance

> **Display Text:** Gain spirit resistance
> 
> **Prefixes:** Serene
> **Suffixes:** Spirit Resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`

## AddFireResistancePercentage

> **Display Text:** Fire Damage Reduction +{0:0.#}%
> 
> **Prefixes:** Extinguishing
> **Suffixes:** Fire Resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddFrostResistancePercentage

> **Display Text:** Frost Damage Reduction +{0:0.#}%
> 
> **Prefixes:** Insulated
> **Suffixes:** Frost Resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddLightningResistancePercentage

> **Display Text:** Lightning Damage Reduction +{0:0.#}%
> 
> **Prefixes:** Grounded
> **Suffixes:** Lightning Resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddPoisonResistancePercentage

> **Display Text:** Poison Damage Reduction +{0:0.#}%
> 
> **Prefixes:** Curing
> **Suffixes:** Poison Resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddSpiritResistancePercentage

> **Display Text:** Spirit Damage Reduction +{0:0.#}%
> 
> **Prefixes:** Serene
> **Suffixes:** Spirit Resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddElementalResistancePercentage

> **Display Text:** Elemental Damage Reduction +{0:0.#}%
> 
> **Prefixes:** Warding
> **Suffixes:** Elemental Resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|2|1|
> |Rare|2|4|1|
> |Epic|4|7|1|
> |Legendary|8|10|1|

## AddBluntResistancePercentage

> **Display Text:** Blunt Damage Reduction +{0:0.#}%
> 
> **Prefixes:** Defensive
> **Suffixes:** Blunt Resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddSlashingResistancePercentage

> **Display Text:** Slash Damage Reduction +{0:0.#}%
> 
> **Prefixes:** Impervious
> **Suffixes:** Slashing Resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddPiercingResistancePercentage

> **Display Text:** Pierce Damage Reduction +{0:0.#}%
> 
> **Prefixes:** Impenetrable
> **Suffixes:** Piercing Resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddChoppingResistancePercentage

> **Display Text:** +{0:0.#}% chopping Damage Reduction
> 
> **Prefixes:** 
> **Suffixes:** 
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddPhysicalResistancePercentage

> **Display Text:** Physical Damage Reduction +{0:0.#}% 
> 
> **Prefixes:** Resistant
> **Suffixes:** Physical Resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|2|1|
> |Rare|2|4|1|
> |Epic|4|7|1|
> |Legendary|8|10|1|

## ModifyMovementSpeed

> **Display Text:** Move Speed +{0:0.#}%
> 
> **Prefixes:** Quick
> **Suffixes:** Speed
> 
> **Allowed Item Types:** Legs, Utility
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **ExclusiveEffectTypes:** `RemoveSpeedPenalty`
> > **AllowedItemTypes:** `Legs, Utility`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|1|
> |Rare|6|11|1|
> |Epic|7|12|1|
> |Legendary|8|13|1|

## ModifySprintStaminaUse

> **Display Text:** Sprint Stamina Use -{0:0.#}%
> 
> **Prefixes:** Sprinter's
> **Suffixes:** Sprinting
> 
> **Allowed Item Types:** Legs, Utility
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Legs, Utility`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|1|
> |Rare|8|13|1|
> |Epic|11|16|1|
> |Legendary|14|19|1|

## ModifyJumpStaminaUse

> **Display Text:** Jump Stamina Use -{0:0.#}%
> 
> **Prefixes:** Acrobat's
> **Suffixes:** the Acrobat
> 
> **Allowed Item Types:** Legs, Utility
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Legs, Utility`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|1|
> |Rare|8|13|1|
> |Epic|11|16|1|
> |Legendary|14|19|1|

## ModifyAttackStaminaUse

> **Display Text:** Attack Stamina Use -{0:0.#}%
> 
> **Prefixes:** Gladiator's
> **Suffixes:** the Gladiator
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Tool
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemUsesStaminaOnAttack`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Tool`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|1|
> |Rare|8|13|1|
> |Epic|11|16|1|
> |Legendary|14|19|1|

## ModifyBlockStaminaUse

> **Display Text:** Block Stamina Use -{0:0.#}%
> 
> **Prefixes:** Guardian's
> **Suffixes:** the Guardian
> 
> **Allowed Item Types:** Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemHasBlockPower`
> > **AllowedItemTypes:** `Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|1|
> |Rare|8|13|1|
> |Epic|11|16|1|
> |Legendary|14|19|1|

## Indestructible

> **Display Text:** Indestructible
> 
> **Prefixes:** Indestructible
> **Suffixes:** Indestructibility
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Tool, Shield, Helmet, Chest, Legs, Shoulder
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **ExclusiveEffectTypes:** `ModifyDurability`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Tool, Shield, Helmet, Chest, Legs, Shoulder`
> > **AllowedRarities:** `Epic, Legendary`

## Weightless

> **Display Text:** Weightless
> 
> **Prefixes:** Weightless
> **Suffixes:** Weightlessness
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Tool, Shield, Helmet, Chest, Legs, Shoulder
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **ExclusiveEffectTypes:** `ReduceWeight`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Tool, Shield, Helmet, Chest, Legs, Shoulder`

## AddCarryWeight

> **Display Text:** Carry Weight +{0}
> 
> **Prefixes:** Pocketed
> **Suffixes:** Pockets
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|5|
> |Rare|10|15|5|
> |Epic|15|25|5|
> |Legendary|20|50|5|

## LifeSteal

> **Display Text:** Lifesteal +{0:0.#}%
> 
> **Prefixes:** Valravn's
> **Suffixes:** Valravn
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemUsesStaminaOnAttack`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow`
> > **AllowedRarities:** `Rare, Epic, Legendary`
> > **ExcludedSkillTypes:** `Pickaxes`

## ModifyAttackSpeed

> **Display Text:** Attack Speed +{0:0.#}%
> 
> **Prefixes:** Striker's
> **Suffixes:** Striking
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Tool
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Tool`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|1|
> |Rare|8|13|1|
> |Epic|11|16|1|
> |Legendary|14|19|1|

## Throwable

> **Display Text:** Throwable
> 
> **Prefixes:** Throwing
> **Suffixes:** Throwing
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon`
> > **ExcludedItemTypes:** `Staff`
> > **ExcludedSkillTypes:** `Pickaxes, Spears`

## Waterproof

> **Display Text:** Waterproof
> 
> **Prefixes:** Waterproof
> **Suffixes:** Water-repelling
> 
> **Allowed Item Types:** Shoulder
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Shoulder`

## Paralyze

> **Display Text:** Paralyze for {0:0.#} seconds
> 
> **Prefixes:** Paralyzing
> **Suffixes:** Paralysis
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow`
> > **ExcludedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|0.5|2|0.5|
> |Rare|1|2.5|0.5|
> |Epic|1.5|3|0.5|
> |Legendary|2|4|0.5|

## DoubleJump

> **Display Text:** Air Jump
> 
> **Prefixes:** Tyr's
> **Suffixes:** of the Wind
> 
> **Allowed Item Types:** Legs
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Legs`
> > **AllowedRarities:** `Epic, Legendary`

## WaterWalking

> **Display Text:** Water Walking
> 
> **Prefixes:** Njord's, Water Walking
> **Suffixes:** of Water Walking, of Njord's Smooth Sea
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`

## ExplosiveArrows

> **Display Text:** Explosive Shot +{0:0.#}%
> 
> **Prefixes:** Exploding
> **Suffixes:** Explosive
> 
> **Allowed Item Types:** Bow
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Bow`
> > **ExcludedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## QuickDraw

> **Display Text:** Quick Draw +{0:0.#}%
> 
> **Prefixes:** Quick-draw
> **Suffixes:** Quickness
> 
> **Allowed Item Types:** Bow
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Bow`
> > **ExcludedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|2|8|1|
> |Rare|6|16|1|
> |Epic|14|30|1|
> |Legendary|30|40|1|

## AddSwordsSkill

> **Display Text:** Swords Skill +{0}
> 
> **Prefixes:** Blademaster's
> **Suffixes:** the Blademaster
> 
> **Allowed Item Types:** OneHandedWeapon
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `OneHandedWeapon`
> > **AllowedSkillTypes:** `Swords`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddKnivesSkill

> **Display Text:** Knives Skill +{0}
> 
> **Prefixes:** Scoundrel's
> **Suffixes:** the Scoundrel
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedSkillTypes:** `Knives`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddClubsSkill

> **Display Text:** Clubs Skill +{0}
> 
> **Prefixes:** Bruiser's
> **Suffixes:** the Bruiser
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedSkillTypes:** `Clubs`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddPolearmsSkill

> **Display Text:** Polearms Skill +{0}
> 
> **Prefixes:** Halberdier's
> **Suffixes:** the Halberdier
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedSkillTypes:** `Polearms`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddSpearsSkill

> **Display Text:** Spears Skill +{0}
> 
> **Prefixes:** Hunter's
> **Suffixes:** the Hunter
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedSkillTypes:** `Spears`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddBlockingSkill

> **Display Text:** Blocking Skill +{0}
> 
> **Prefixes:** Shieldmaiden's
> **Suffixes:** the Shieldmaiden
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedSkillTypes:** `Blocking`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddAxesSkill

> **Display Text:** Axes Skill +{0}
> 
> **Prefixes:** Viking's
> **Suffixes:** the Viking
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedSkillTypes:** `Axes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddBowsSkill

> **Display Text:** Bows Skill +{0}
> 
> **Prefixes:** Archer's
> **Suffixes:** Archery
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedSkillTypes:** `Bows`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddBloodMagicSkill

> **Display Text:** Blood Magic Skill +{0}
> 
> **Prefixes:** Summoner's
> **Suffixes:** the Summoner
> 
> **Allowed Item Types:** Chest, Legs, Shoulder, Utility, Staffs
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Chest, Legs, Shoulder, Utility, Staffs`
> > **AllowedSkillTypes:** `BloodMagic`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddElementalMagicSkill

> **Display Text:** Elemental Magic Skill +{0}
> 
> **Prefixes:** Sorceror's
> **Suffixes:** the Sorceror
> 
> **Allowed Item Types:** Chest, Legs, Shoulder, Utility, Staffs
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Chest, Legs, Shoulder, Utility, Staffs`
> > **AllowedSkillTypes:** `ElementalMagic`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddUnarmedSkill

> **Display Text:** Unarmed Skill +{0}
> 
> **Prefixes:** Brawler's
> **Suffixes:** Brawling
> 
> **Allowed Item Types:** Chest
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> > **AllowedItemTypes:** `Chest`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddPickaxesSkill

> **Display Text:** Pickaxes Skill +{0}
> 
> **Prefixes:** Mining
> **Suffixes:** the Miner
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddMovementSkills

> **Display Text:** Movement Skills +{0}
> 
> **Prefixes:** Adventuring
> **Suffixes:** the Adventurer
> 
> **Allowed Item Types:** Chest, Legs, Shoulder, Utility
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Chest, Legs, Shoulder, Utility`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## ModifyStaggerDuration

> **Display Text:** Stagger Duration +{0:0.#}%
> 
> **Prefixes:** Basher's
> **Suffixes:** Staggering
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Shield`
> > **ExcludedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|1|
> |Rare|8|13|1|
> |Epic|11|16|1|
> |Legendary|14|19|1|

## QuickLearner

> **Display Text:** Quick Learner +{0:0.#}%
> 
> **Prefixes:** Student's
> **Suffixes:** the Student
> 
> **Allowed Item Types:** Helmet
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|1|
> |Rare|8|13|1|
> |Epic|11|16|1|
> |Legendary|14|19|1|

## FreeBuild

> **Display Text:** Free Build
> 
> **Prefixes:** Builder's
> **Suffixes:** the Builder
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedRarities:** `Legendary`
> > **AllowedItemNames:** `$item_hammer, $item_hoe`

## RecallWeapon

> **Display Text:** Recalling
> 
> **Prefixes:** Recaller's
> **Suffixes:** Recalling
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon`
> > **ExcludedItemTypes:** `Staff`
> > **ExcludedSkillTypes:** `Pickaxes`

## ReflectDamage

> **Display Text:** Thorns +{0:0.#}%
> 
> **Prefixes:** Reflector's
> **Suffixes:** Reflecting
> 
> **Allowed Item Types:** Chest, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Chest, Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|1|
> |Rare|8|13|1|
> |Epic|11|16|1|
> |Legendary|14|19|1|

## AvoidDamageTaken

> **Display Text:** Feint +{0:0.#}%
> 
> **Prefixes:** Artful Dodger's
> **Suffixes:** the Artful Dodger
> 
> **Allowed Item Types:** Chest, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Chest, Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|1|
> |Rare|8|13|1|
> |Epic|11|16|1|
> |Legendary|14|19|1|

## StaggerOnDamageTaken

> **Display Text:** Stagger Chance +{0:0.#}%
> 
> **Prefixes:** Avenger's
> **Suffixes:** Vengeance
> 
> **Allowed Item Types:** Chest, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Chest, Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|1|
> |Rare|8|13|1|
> |Epic|11|16|1|
> |Legendary|14|19|1|

## FeatherFall

> **Display Text:** Feather Fall
> 
> **Prefixes:** Feathery
> **Suffixes:** the Feather
> 
> **Allowed Item Types:** Legs
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Legs`

## ModifyDiscoveryRadius

> **Display Text:** Discovery Radius +{0:0.#}%
> 
> **Prefixes:** Explorer's
> **Suffixes:** the Explorer
> 
> **Allowed Item Types:** Helmet
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|1|
> |Rare|8|13|1|
> |Epic|11|16|1|
> |Legendary|14|19|1|

## Comfortable

> **Display Text:** Comfort +{0}
> 
> **Prefixes:** Comfortable
> **Suffixes:** Comfort
> 
> **Allowed Item Types:** Chest, Legs, Shoulder, Helmet
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Chest, Legs, Shoulder, Helmet`
> > **AllowedRarities:** `Epic, Legendary`

## ModifyMovementSpeedLowHealth

> **Display Text:** Move Speed +{0:0.#}% (Health Critical)
> 
> **Prefixes:** Quick
> **Suffixes:** Speed
> 
> **Allowed Item Types:** Legs, Utility
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **ExclusiveEffectTypes:** `RemoveSpeedPenalty`
> > **AllowedItemTypes:** `Legs, Utility`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|7|12|1|
> |Rare|8|13|1|
> |Epic|9|14|1|
> |Legendary|10|15|1|

## ModifyHealthRegenLowHealth

> **Display Text:** Health Regen +{0:0.#}% (Health Critical)
> 
> **Prefixes:** Vigorous
> **Suffixes:** Vigor
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|15|25|1|
> |Rare|15|25|1|
> |Epic|15|25|1|
> |Legendary|25|30|1|

## ModifyStaminaRegenLowHealth

> **Display Text:** Stamina Regen +{0:0.#}% (Health Critical)
> 
> **Prefixes:** Recovering
> **Suffixes:** Recovery
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Tool
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Tool`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|15|25|1|
> |Rare|15|25|1|
> |Epic|15|25|1|
> |Legendary|25|30|1|

## ModifyEitrRegenLowHealth

> **Display Text:** Eitr Regen +{0:0.#}% (Health Critical)
> 
> **Prefixes:** Focusing
> **Suffixes:** Focus
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Staff
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Staff`
> > **AllowedRarities:** `Epic, Legendary`

## ModifyArmorLowHealth

> **Display Text:** All Armor +{0:0.#}% (Health Critical)
> 
> **Prefixes:** Heavy, Protected
> **Suffixes:** Protection
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemHasArmor`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|15|25|1|
> |Rare|15|25|1|
> |Epic|15|25|1|
> |Legendary|25|30|1|

## ModifyDamageLowHealth

> **Display Text:** Damage +{0:0.#}% (Health Critical)
> 
> **Prefixes:** Berserker's
> **Suffixes:** the Berserker
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow`
> > **ExcludedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|3|6|1|
> |Rare|7|10|1|
> |Epic|11|14|1|
> |Legendary|15|20|1|

## ModifyBlockPowerLowHealth

> **Display Text:** Block +{0:0.#}% (Health Critical)
> 
> **Prefixes:** Stopping, Defender's
> **Suffixes:** Stopping, the Defender
> 
> **Allowed Item Types:** Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemHasBlockPower`
> > **AllowedItemTypes:** `Shield`
> > **ExcludedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|15|25|1|
> |Rare|15|25|1|
> |Epic|15|25|1|
> |Legendary|25|30|1|

## ModifyParryLowHealth

> **Display Text:** Parry +{0:0.#}% (Health Critical)
> 
> **Prefixes:** Elusive, Rebuking
> **Suffixes:** Repelling
> 
> **Allowed Item Types:** Shield, TwoHandedWeapon
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemHasParryPower`
> > **AllowedItemTypes:** `Shield, TwoHandedWeapon`
> > **ExcludedSkillTypes:** `Pickaxes`
> > **ExcludedItemNames:** `$item_shield_serpentscale`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|15|25|1|
> |Rare|15|25|1|
> |Epic|15|25|1|
> |Legendary|25|30|1|

## ModifyAttackSpeedLowHealth

> **Display Text:** Attack Speed +{0:0.#}% (Health Critical)
> 
> **Prefixes:** Striker's
> **Suffixes:** Striking
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Tool
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Tool`
> > **ExcludedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|7|15|1|
> |Rare|16|22|1|
> |Epic|23|30|1|
> |Legendary|31|40|1|

## AvoidDamageTakenLowHealth

> **Display Text:** Feint +{0:0.#}% (Health Critical)
> 
> **Prefixes:** Artful Dodger's
> **Suffixes:** the Artful Dodger
> 
> **Allowed Item Types:** Chest, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Chest, Shield`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|7|15|1|
> |Rare|16|22|1|
> |Epic|23|30|1|
> |Legendary|31|40|1|

## LifeStealLowHealth

> **Display Text:** Lifesteal {0:0.0}% (Health Critical)
> 
> **Prefixes:** Valravn's
> **Suffixes:** Valravn
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemUsesStaminaOnAttack`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow`
> > **AllowedRarities:** `Rare, Epic, Legendary`
> > **ExcludedSkillTypes:** `Pickaxes`

## Glowing

> **Display Text:** Glowing
> 
> **Prefixes:** Glowing
> **Suffixes:** Light
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility`

## Executioner

> **Display Text:** Executioner +{0}%
> 
> **Prefixes:** Executioner's
> **Suffixes:** Execution
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemUsesStaminaOnAttack`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow`
> > **AllowedRarities:** `Epic, Legendary`
> > **ExcludedSkillTypes:** `Pickaxes`

## Riches

> **Display Text:** Riches +{0:0.#}%
> 
> **Prefixes:** Rich
> **Suffixes:** Riches
> 
> **Allowed Item Types:** Utility
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Utility`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|3|1|
> |Rare|4|6|1|
> |Epic|7|9|1|
> |Legendary|10|15|1|

## Opportunist

> **Display Text:** Opportunist +{0:0.#}%
> 
> **Prefixes:** Opportunist's
> **Suffixes:** Opportunity
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedRarities:** `Epic, Legendary`
> > **AllowedSkillTypes:** `Knives`

## Duelist

> **Display Text:** Duelist +{0:0.#}%
> 
> **Prefixes:** Duelist's
> **Suffixes:** Dueling
> 
> **Allowed Item Types:** OneHandedWeapon
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `OneHandedWeapon`
> > **AllowedSkillTypes:** `Swords`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|20|30|1|
> |Rare|31|40|1|
> |Epic|41|50|1|
> |Legendary|51|60|1|

## Immovable

> **Display Text:** Immovable
> 
> **Prefixes:** Pillar's
> **Suffixes:** the Pillar
> 
> **Allowed Item Types:** Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemHasNoParryPower`
> > **AllowedItemTypes:** `Shield`

## ModifyStaggerDamage

> **Display Text:** Stagger Damage +{0:0.#}%
> 
> **Prefixes:** Cunning
> **Suffixes:** Cunning
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow`
> > **ExcludedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|7|15|1|
> |Rare|16|22|1|
> |Epic|23|30|1|
> |Legendary|31|40|1|

## Luck

> **Display Text:** Luck +{0}
> 
> **Prefixes:** Lucky
> **Suffixes:** Luck
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|5|1|
> |Rare|5|10|1|
> |Epic|10|15|1|
> |Legendary|15|20|1|

## ModifyParryWindow

> **Display Text:** Parry Window +{0} ms
> 
> **Prefixes:** Parrying Expert's
> **Suffixes:** Expert Parrying
> 
> **Allowed Item Types:** Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemHasParryPower`
> > **AllowedItemTypes:** `Shield`
> > **ExcludedItemNames:** `$item_shield_serpentscale`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|40|1|
> |Rare|20|60|1|
> |Epic|40|80|1|
> |Legendary|50|100|1|

## Slow

> **Display Text:** Slow +{0:0.#}%
> 
> **Prefixes:** Sluggish
> **Suffixes:** Lethargy
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow`
> > **ExcludedSkillTypes:** `Pickaxes`
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|30|50|1|
> |Rare|40|60|1|
> |Epic|50|70|1|
> |Legendary|60|80|1|

## Bulwark

> **Display Text:** Bulwark [Activated]: Prevent all damage for 5 seconds. Cooldown: 60 seconds.
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`

## Undying

> **Display Text:** Undying [Passive]: On death, regain full health. Cooldown: 20 minutes.
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`

## FrostDamageAOE

> **Display Text:** Melee attacks that deal frost damage deal an additional 50% of weapon damage as frost in a cone in front of the attacker.
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`

## Berserker

> **Display Text:** Berserker [Activated]: For 10 seconds, you cannot regenerate health, but you gain +50% to +200% damage based on health missing. Cooldown: 3 minutes.
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`

# Item Sets

Sets of loot drop data that can be referenced in the loot tables
## EnchantingMats

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mats | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier1Mats | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier2Mats | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier3Mats | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier0Mats

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | DustMagic | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | EssenceMagic | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | ReagentMagic | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier1Mats

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | DustRare | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | EssenceRare | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | ReagentRare | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier2Mats

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | DustEpic | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | EssenceEpic | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | ReagentEpic | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier3Mats

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | DustLegendary | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | EssenceLegendary | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | ReagentLegendary | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## EnchantingRunestones

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | RunestoneMagic | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | RunestoneRare | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | RunestoneEpic | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | RunestoneLegendary | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier0Runestone

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | RunestoneMagic | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier1Runestone

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | RunestoneRare | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier2Runestone

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | RunestoneEpic | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier3Runestone

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | RunestoneLegendary | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## ModUtility

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | LeatherBelt | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | SilverRing | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | GoldRubyRing | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier0Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Club | 1 (33.3%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |
> | AxeStone | 1 (33.3%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |
> | Torch | 1 (33.3%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |


## Tier0Tools

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Hammer | 1 (50%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |
> | Hoe | 1 (50%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |


## Tier0Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorRagsLegs | 1 (50%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |
> | ArmorRagsChest | 1 (50%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |


## Tier0Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ShieldWood | 1 (50%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |
> | ShieldWoodTower | 1 (50%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |


## Tier0Everything

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Weapons | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier0Tools | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier0Armor | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier0Shields | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier1Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | AxeFlint | 1 (25%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) |
> | SpearFlint | 1 (25%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) |
> | KnifeFlint | 1 (25%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) |
> | Bow | 1 (25%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) |


## Tier1Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorLeatherLegs | 1 (25%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) |
> | ArmorLeatherChest | 1 (25%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) |
> | HelmetLeather | 1 (25%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) |
> | CapeDeerHide | 1 (25%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) |


## Tier1Tools

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | PickaxeAntler | 1 (100%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) |


## Tier1Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ShieldBoneTower | 1 (100%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) |


## Tier1Everything

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Weapons | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier1Shields | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier1Armor | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier1Tools | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## TrollArmor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorTrollLeatherLegs | 1 (25%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |
> | ArmorTrollLeatherChest | 1 (25%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |
> | HelmetTrollLeather | 1 (25%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |
> | CapeTrollHide | 1 (25%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |


## Tier2Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | KnifeCopper | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |
> | SledgeStagbreaker | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |
> | SwordBronze | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |
> | AxeBronze | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |
> | MaceBronze | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |
> | AtgeirBronze | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |
> | SpearBronze | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |
> | BowFineWood | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |
> | KnifeChitin | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |
> | SpearChitin | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |


## Tier2Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorBronzeLegs | 1 (33.3%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |
> | ArmorBronzeChest | 1 (33.3%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |
> | HelmetBronze | 1 (33.3%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |


## Tier2Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ShieldBronzeBuckler | 1 (100%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |


## Tier2Tools

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | PickaxeBronze | 1 (50%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |
> | Cultivator | 1 (50%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) |


## Tier2Everything

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | TrollArmor | 1 (20%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier2Weapons | 1 (20%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier2Armor | 1 (20%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier2Shields | 1 (20%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier2Tools | 1 (20%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier3Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Battleaxe | 1 (12.5%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | SwordIron | 1 (12.5%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | AxeIron | 1 (12.5%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | SledgeIron | 1 (12.5%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | MaceIron | 1 (12.5%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | AtgeirIron | 1 (12.5%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | SpearElderbark | 1 (12.5%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | BowHuntsman | 1 (12.5%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |


## Tier3Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorRootChest | 1 (16.7%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | ArmorRootLegs | 1 (16.7%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | HelmetRoot | 1 (16.7%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | ArmorIronLegs | 1 (16.7%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | ArmorIronChest | 1 (16.7%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | HelmetIron | 1 (16.7%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |


## Tier3Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ShieldBanded | 1 (50%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | ShieldIronTower | 1 (50%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |


## Tier3Tools

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | PickaxeIron | 1 (100%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |


## Tier3Everything

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Weapons | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier3Armor | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier3Shields | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier3Tools | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier4Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | SwordSilver | 5 (27.8%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | SpearWolfFang | 5 (27.8%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | MaceSilver | 1 (5.6%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | KnifeSilver | 5 (27.8%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | FistFenrirClaw | 1 (5.6%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | BowDraugrFang | 1 (5.6%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |


## Tier4Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorFenringChest | 1 (14.3%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | ArmorFenringLegs | 1 (14.3%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | HelmetFenring | 1 (14.3%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | ArmorWolfLegs | 1 (14.3%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | ArmorWolfChest | 1 (14.3%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | HelmetDrake | 1 (14.3%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | CapeWolf | 1 (14.3%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |


## Tier4Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ShieldSilver | 5 (83.3%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | ShieldSerpentscale | 1 (16.7%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |


## Tier4Everything

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Weapons | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier4Armor | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier4Shields | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier5Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | AtgeirBlackmetal | 3 (18.8%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |
> | CrossbowArbalest | 3 (18.8%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |
> | AxeBlackMetal | 3 (18.8%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |
> | KnifeBlackMetal | 3 (18.8%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |
> | SwordBlackmetal | 3 (18.8%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |
> | MaceNeedle | 1 (6.3%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |


## Tier5Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorPaddedGreaves | 1 (20%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |
> | ArmorPaddedCuirass | 1 (20%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |
> | HelmetPadded | 1 (20%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |
> | CapeLinen | 1 (20%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |
> | CapeLox | 1 (20%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |


## Tier5Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ShieldBlackmetal | 1 (50%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |
> | ShieldBlackmetalTower | 1 (50%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |


## Tier5Everything

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Weapons | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier5Armor | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier5Shields | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier6Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | SwordMistwalker | 1 (8.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | THSwordKrom | 1 (8.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | StaffFireball | 1 (8.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | StaffIceShards | 1 (8.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | StaffShield | 1 (8.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | StaffSkeleton | 1 (8.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | AtgeirHimminAfl | 1 (8.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | KnifeSkollAndHati | 1 (8.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | AxeJotunBane | 1 (8.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | SpearCarapace | 1 (8.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | SledgeDemolisher | 1 (8.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | BowSpineSnap | 1 (8.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |


## Tier6Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorCarapaceChest | 1 (14.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | ArmorCarapaceLegs | 1 (14.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | HelmetCarapace | 1 (14.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | ArmorMageChest | 1 (14.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | ArmorMageLegs | 1 (14.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | HelmetMage | 1 (14.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | CapeFeather | 1 (14.3%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |


## Tier6Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ShieldCarapace | 1 (50%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |
> | ShieldCarapaceBuckler | 1 (50%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |


## Tier6Tools

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | PickaxeBlackmetal | 1 (100%) | 0 (0%) | 10 (10%) | 50 (50%) | 40 (40%) |


## Tier6Everything

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Weapons | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier6Armor | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier6Shields | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier6Tools | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


# Loot Tables

A list of every built-in loot table from the mod. The name of the loot table is the object name followed by a number signifying the level of the object.
## Tier0Mob

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 100 (100%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 95 (95%) |
> | 1 | 5 (5%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 10 (10%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 39 (39%) |
> | 2 | 1 (1%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 100 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 90 (90%) | 10 (10%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 80 (80%) | 15 (15%) | 5 (5%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 70 (70%) | 19 (19%) | 10 (10%) | 1 (1%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 60 (60%) | 20 (20%) | 15 (15%) | 5 (5%) |


## Tier1Mob

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 95 (95%) |
> | 1 | 5 (5%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 10 (10%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 85 (85%) |
> | 1 | 15 (15%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 25 (25%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 55 (55%) |
> | 1 | 43 (43%) |
> | 2 | 2 (2%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 10 (90.9%) | 75 (75%) | 25 (25%) | 0 (0%) | 0 (0%) |
> | Tier1Everything | 1 (9.1%) | 75 (75%) | 25 (25%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 5 (83.3%) | 70 (70%) | 29 (29%) | 1 (1%) | 0 (0%) |
> | Tier1Everything | 1 (16.7%) | 70 (70%) | 29 (29%) | 1 (1%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 3 (75%) | 63 (63%) | 34 (34%) | 2 (2%) | 1 (1%) |
> | Tier1Everything | 1 (25%) | 63 (63%) | 34 (34%) | 2 (2%) | 1 (1%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (50%) | 53 (53%) | 40 (40%) | 5 (5%) | 2 (2%) |
> | Tier1Everything | 1 (50%) | 53 (53%) | 40 (40%) | 5 (5%) | 2 (2%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (16.7%) | 45 (45%) | 43 (43%) | 9 (9%) | 3 (3%) |
> | Tier1Everything | 5 (83.3%) | 45 (45%) | 43 (43%) | 9 (9%) | 3 (3%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (9.1%) | 35 (35%) | 45 (45%) | 15 (15%) | 5 (5%) |
> | Tier1Everything | 10 (90.9%) | 35 (35%) | 45 (45%) | 15 (15%) | 5 (5%) |


## Tier2Mob

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 10 (10%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 55 (55%) |
> | 1 | 44 (44%) |
> | 2 | 1 (1%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 40 (40%) |
> | 1 | 58 (58%) |
> | 2 | 2 (2%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 25 (25%) |
> | 1 | 71 (71%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (50%) | 50 (50%) | 47 (47%) | 2 (2%) | 1 (1%) |
> | Tier0Shields | 1 (50%) | 50 (50%) | 47 (47%) | 2 (2%) | 1 (1%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 5 (45.5%) | 40 (40%) | 53 (53%) | 5 (5%) | 2 (2%) |
> | Tier0Shields | 5 (45.5%) | 40 (40%) | 53 (53%) | 5 (5%) | 2 (2%) |
> | TrollArmor | 1 (9.1%) | 40 (40%) | 53 (53%) | 5 (5%) | 2 (2%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 3 (42.9%) | 29 (29%) | 59 (59%) | 9 (9%) | 3 (3%) |
> | Tier0Shields | 3 (42.9%) | 29 (29%) | 59 (59%) | 9 (9%) | 3 (3%) |
> | TrollArmor | 1 (14.3%) | 29 (29%) | 59 (59%) | 9 (9%) | 3 (3%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (33.3%) | 15 (15%) | 65 (65%) | 15 (15%) | 5 (5%) |
> | Tier0Shields | 1 (33.3%) | 15 (15%) | 65 (65%) | 15 (15%) | 5 (5%) |
> | TrollArmor | 1 (33.3%) | 15 (15%) | 65 (65%) | 15 (15%) | 5 (5%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (16.7%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |
> | Tier2Everything | 5 (83.3%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (9.1%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |
> | Tier2Everything | 10 (90.9%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |


## Tier3Mob

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 85 (85%) |
> | 1 | 15 (15%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 25 (25%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 48 (48%) |
> | 2 | 2 (2%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 35 (35%) |
> | 1 | 61 (61%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 20 (20%) |
> | 1 | 74 (74%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (100%) | 24 (24%) | 73 (73%) | 2 (2%) | 1 (1%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 5 (83.3%) | 15 (15%) | 78 (78%) | 5 (5%) | 2 (2%) |
> | Tier3Everything | 1 (16.7%) | 15 (15%) | 78 (78%) | 5 (5%) | 2 (2%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 3 (75%) | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) |
> | Tier3Everything | 1 (25%) | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (50%) | 0 (0%) | 80 (80%) | 15 (15%) | 5 (5%) |
> | Tier3Everything | 1 (50%) | 0 (0%) | 80 (80%) | 15 (15%) | 5 (5%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (16.7%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |
> | Tier3Everything | 5 (83.3%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (9.1%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |
> | Tier3Everything | 10 (90.9%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |


## Tier4Mob

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 48 (48%) |
> | 2 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 66 (66%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 10 (10%) |
> | 1 | 84 (84%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 92 (92%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (100%) | 0 (0%) | 75 (74.3%) | 24 (23.8%) | 2 (2%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 5 (83.3%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |
> | Tier4Everything | 1 (16.7%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 3 (75%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |
> | Tier4Everything | 1 (25%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |
> | Tier4Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (16.7%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |
> | Tier4Everything | 5 (83.3%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (9.1%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |
> | Tier4Everything | 10 (90.9%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |


## Tier5Mob

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 24 (24%) |
> | 2 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 38 (38%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 45 (45%) |
> | 1 | 51 (51%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 25 (25%) |
> | 1 | 69 (69%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (100%) | 0 (0%) | 50 (48.5%) | 49 (47.6%) | 4 (3.9%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 5 (83.3%) | 0 (0%) | 30 (30%) | 65 (65%) | 5 (5%) |
> | Tier5Everything | 1 (16.7%) | 0 (0%) | 30 (30%) | 65 (65%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 3 (75%) | 0 (0%) | 10 (10%) | 80 (80%) | 10 (10%) |
> | Tier5Everything | 1 (25%) | 0 (0%) | 10 (10%) | 80 (80%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (50%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |
> | Tier5Everything | 1 (50%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (16.7%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |
> | Tier5Everything | 5 (83.3%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (9.1%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |
> | Tier5Everything | 10 (90.9%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |


## Tier6Mob

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 28 (28%) |
> | 2 | 2 (2%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 46 (46%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 64 (64%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 80 (80%) |
> | 2 | 15 (15%) |
> | 3 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 25 (25%) | 65 (65%) | 10 (10%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 0 (0%) | 65 (65%) | 35 (35%) |


## Tier7Mob

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 28 (28%) |
> | 2 | 2 (2%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 46 (46%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 64 (64%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 80 (80%) |
> | 2 | 15 (15%) |
> | 3 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 25 (25%) | 65 (65%) | 10 (10%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 65 (65%) | 35 (35%) |


## Eikthyr

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 1 | 70 (70%) |
> | 2 | 30 (30%) |
> | 4 | 0 (0%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 1 | 60 (60%) |
> | 2 | 40 (40%) |
> | 4 | 0 (0%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 1 | 50 (50%) |
> | 2 | 50 (50%) |
> | 4 | 0 (0%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 1 | 35 (35%) |
> | 2 | 60 (60%) |
> | 3 | 5 (5%) |
> | 4 | 0 (0%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 1 | 20 (20%) |
> | 2 | 70 (70%) |
> | 3 | 10 (10%) |
> | 4 | 0 (0%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 1 | 5 (5%) |
> | 2 | 80 (80%) |
> | 3 | 15 (15%) |
> | 4 | 0 (0%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (33.3%) | 177 (88.5%) | 20 (10%) | 2 (1%) | 1 (0.5%) |
> | Tier0Shields | 1 (33.3%) | 177 (88.5%) | 20 (10%) | 2 (1%) | 1 (0.5%) |
> | SledgeStagbreaker | 1 (33.3%) | 177 (88.5%) | 20 (10%) | 2 (1%) | 1 (0.5%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Eikthyr.1 | 1 (100%) | 79 (79%) | 17 (17%) | 3 (3%) | 1 (1%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Eikthyr.1 | 1 (100%) | 69 (69%) | 24 (24%) | 5 (5%) | 2 (2%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Eikthyr.1 | 1 (100%) | 53 (53%) | 32 (32%) | 10 (10%) | 5 (5%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Eikthyr.1 | 1 (100%) | 35 (35%) | 40 (40%) | 15 (15%) | 10 (10%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Eikthyr.1 | 1 (100%) | 20 (20%) | 45 (45%) | 20 (20%) | 15 (15%) |


## gd_king

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 1 | 35 (35%) |
> | 2 | 55 (55%) |
> | 3 | 10 (10%) |
> | 4 | 0 (0%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 1 | 20 (20%) |
> | 2 | 65 (65%) |
> | 3 | 15 (15%) |
> | 4 | 0 (0%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 1 | 5 (5%) |
> | 2 | 75 (75%) |
> | 3 | 20 (20%) |
> | 4 | 0 (0%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 1 | 0 (0%) |
> | 2 | 70 (70%) |
> | 3 | 30 (30%) |
> | 4 | 0 (0%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 1 | 0 (0%) |
> | 2 | 60 (60%) |
> | 3 | 40 (40%) |
> | 4 | 0 (0%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 1 | 0 (0%) |
> | 2 | 40 (40%) |
> | 3 | 50 (50%) |
> | 4 | 10 (10%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (100%) | 59 (59%) | 29 (29%) | 10 (10%) | 2 (2%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | gd_king.1 | 1 (100%) | 43 (43%) | 37 (37%) | 15 (15%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | gd_king.1 | 1 (100%) | 25 (25%) | 45 (45%) | 20 (20%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | gd_king.1 | 1 (100%) | 4 (4%) | 50 (50.5%) | 30 (30.3%) | 15 (15.2%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | gd_king.1 | 1 (100%) | 0 (0%) | 40 (40%) | 40 (40%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | gd_king.1 | 1 (100%) | 0 (0%) | 25 (25%) | 50 (50%) | 25 (25%) |


## Bonemass

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 1 | 0 (0%) |
> | 2 | 60 (60%) |
> | 3 | 35 (35%) |
> | 4 | 5 (5%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 1 | 0 (0%) |
> | 2 | 45 (45%) |
> | 3 | 45 (45%) |
> | 4 | 10 (10%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 1 | 0 (0%) |
> | 2 | 30 (30%) |
> | 3 | 55 (55%) |
> | 4 | 15 (15%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 1 | 0 (0%) |
> | 2 | 10 (10%) |
> | 3 | 65 (65%) |
> | 4 | 25 (25%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 1 | 0 (0%) |
> | 3 | 65 (65%) |
> | 4 | 35 (35%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 1 | 0 (0%) |
> | 3 | 50 (50%) |
> | 4 | 50 (50%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (100%) | 20 (20%) | 60 (60%) | 15 (15%) | 5 (5%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Bonemass.1 | 1 (100%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Bonemass.1 | 1 (100%) | 0 (0%) | 50 (50%) | 35 (35%) | 15 (15%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Bonemass.1 | 1 (100%) | 0 (0%) | 30 (30%) | 50 (50%) | 20 (20%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Bonemass.1 | 1 (100%) | 0 (0%) | 10 (10%) | 65 (65%) | 25 (25%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Bonemass.1 | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |


## Dragon

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 2 | 20 (20%) |
> | 3 | 40 (40%) |
> | 4 | 30 (30%) |
> | 5 | 10 (10%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 2 | 20 (17.4%) |
> | 3 | 50 (43.5%) |
> | 4 | 30 (26.1%) |
> | 5 | 15 (13%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 3 | 50 (50%) |
> | 4 | 30 (30%) |
> | 5 | 20 (20%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 3 | 20 (20%) |
> | 4 | 55 (55%) |
> | 5 | 25 (25%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 3 | 5 (5%) |
> | 4 | 65 (65%) |
> | 5 | 30 (30%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 4 | 60 (60%) |
> | 5 | 35 (35%) |
> | 6 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (100%) | 0 (0%) | 40 (40%) | 50 (50%) | 10 (10%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Dragon.1 | 1 (100%) | 0 (0%) | 20 (20%) | 65 (65%) | 15 (15%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Dragon.1 | 1 (100%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Dragon.1 | 1 (100%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Dragon.1 | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Dragon.1 | 1 (100%) | 0 (0%) | 0 (0%) | 60 (60%) | 40 (40%) |


## GoblinKing

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 3 | 20 (20%) |
> | 4 | 60 (60%) |
> | 5 | 15 (15%) |
> | 6 | 5 (5%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 4 | 70 (70%) |
> | 5 | 20 (20%) |
> | 6 | 10 (10%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 4 | 45 (45%) |
> | 5 | 40 (40%) |
> | 6 | 15 (15%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 4 | 25 (25%) |
> | 5 | 50 (50%) |
> | 6 | 25 (25%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 4 | 5 (5%) |
> | 5 | 60 (60%) |
> | 6 | 30 (30%) |
> | 7 | 5 (5%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 5 | 50 (50%) |
> | 6 | 40 (40%) |
> | 7 | 10 (10%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | GoblinKing.1 | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | GoblinKing.1 | 1 (100%) | 0 (0%) | 0 (0%) | 60 (60%) | 40 (40%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | GoblinKing.1 | 1 (100%) | 0 (0%) | 0 (0%) | 40 (40%) | 60 (60%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | GoblinKing.1 | 1 (100%) | 0 (0%) | 0 (0%) | 20 (20%) | 80 (80%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | GoblinKing.1 | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) | 100 (100%) |


## SeekerQueen

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 3 | 20 (20%) |
> | 4 | 60 (60%) |
> | 5 | 15 (15%) |
> | 6 | 5 (5%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 4 | 70 (70%) |
> | 5 | 20 (20%) |
> | 6 | 10 (10%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 4 | 45 (45%) |
> | 5 | 40 (40%) |
> | 6 | 15 (15%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 4 | 25 (25%) |
> | 5 | 50 (50%) |
> | 6 | 25 (25%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 4 | 5 (5%) |
> | 5 | 60 (60%) |
> | 6 | 30 (30%) |
> | 7 | 5 (5%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 5 | 50 (50%) |
> | 6 | 40 (40%) |
> | 7 | 10 (10%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | SeekerQueen.1 | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | SeekerQueen.1 | 1 (100%) | 0 (0%) | 0 (0%) | 60 (60%) | 40 (40%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | SeekerQueen.1 | 1 (100%) | 0 (0%) | 0 (0%) | 40 (40%) | 60 (60%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | SeekerQueen.1 | 1 (100%) | 0 (0%) | 0 (0%) | 20 (20%) | 80 (80%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | SeekerQueen.1 | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) | 100 (100%) |


## TreasureChest_meadows

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 78 (78%) |
> | 1 | 20 (20%) |
> | 2 | 2 (2%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 78 (78%) |
> | 1 | 20 (20%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 78 (78%) |
> | 1 | 20 (20%) |
> | 2 | 2 (2%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 4 (80%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |
> | Tier1Everything | 1 (20%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 4 (80%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |
> | Tier1Everything | 1 (20%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 4 (80%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |
> | Tier1Everything | 1 (20%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |


## TreasureChest_blackforest

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Shields | 1 (33.3%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | Tier1Everything | 2 (66.7%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Shields | 1 (33.3%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | Tier1Everything | 2 (66.7%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Shields | 1 (33.3%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | Tier1Everything | 2 (66.7%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |


## TreasureChest_forestcrypt

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 68 (68%) |
> | 1 | 20 (20%) |
> | 2 | 10 (10%) |
> | 3 | 2 (2%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 68 (68%) |
> | 1 | 20 (20%) |
> | 2 | 10 (10%) |
> | 3 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 68 (68%) |
> | 1 | 20 (20%) |
> | 2 | 10 (10%) |
> | 3 | 2 (2%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 2 (66.7%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | Tier2Everything | 1 (33.3%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 2 (66.7%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | Tier2Everything | 1 (33.3%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 2 (66.7%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | Tier2Everything | 1 (33.3%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |


## TreasureChest_fCrypt

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 68 (68%) |
> | 1 | 20 (20%) |
> | 2 | 10 (10%) |
> | 3 | 2 (2%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 68 (68%) |
> | 1 | 20 (20%) |
> | 2 | 10 (10%) |
> | 3 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 68 (68%) |
> | 1 | 20 (20%) |
> | 2 | 10 (10%) |
> | 3 | 2 (2%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | TreasureChest_forestcrypt.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | TreasureChest_forestcrypt.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | TreasureChest_forestcrypt.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## TreasureChest_trollcave

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 48 (43.6%) |
> | 1 | 40 (36.4%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 48 (43.6%) |
> | 1 | 40 (36.4%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 48 (43.6%) |
> | 1 | 40 (36.4%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | Tier2Everything | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | Tier2Everything | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | Tier2Everything | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |


## shipwreck_karve_chest

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 48 (43.6%) |
> | 1 | 40 (36.4%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 48 (43.6%) |
> | 1 | 40 (36.4%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 48 (43.6%) |
> | 1 | 40 (36.4%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | Tier1Everything | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | Tier1Everything | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | Tier1Everything | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |


## TreasureChest_meadows_buried

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 48 (43.6%) |
> | 1 | 40 (36.4%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 48 (43.6%) |
> | 1 | 40 (36.4%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 48 (43.6%) |
> | 1 | 40 (36.4%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 3 (60%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | Tier1Everything | 2 (40%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 3 (60%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | Tier1Everything | 2 (40%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 3 (60%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | Tier1Everything | 2 (40%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |


## TreasureChest_sunkencrypt

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 3 (75%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Tier3Everything | 1 (25%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 3 (75%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Tier3Everything | 1 (25%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 3 (75%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Tier3Everything | 1 (25%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |


## TreasureChest_swamp

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 5 (83.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier3Everything | 1 (16.7%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 5 (83.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier3Everything | 1 (16.7%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 5 (83.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier3Everything | 1 (16.7%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## TreasureChest_mountains

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 4 (80%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier4Everything | 1 (20%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 4 (80%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier4Everything | 1 (20%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 4 (80%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier4Everything | 1 (20%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## TreasureChest_mountaincave

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 4 (80%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier4Everything | 1 (20%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 4 (80%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier4Everything | 1 (20%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 4 (80%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier4Everything | 1 (20%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## TreasureChest_plains_stone

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | TreasureChest_heath.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | TreasureChest_heath.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | TreasureChest_heath.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## TreasureChest_heath

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 3 (75%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier5Everything | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 3 (75%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier5Everything | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 3 (75%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier5Everything | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## TreasureChest_dvergrtown

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 3 (75%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier6Everything | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 3 (75%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier6Everything | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 3 (75%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier6Everything | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## TreasureChest_dvergrtower

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | TreasureChest_dvergrtown.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | TreasureChest_dvergrtown.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | TreasureChest_dvergrtown.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## TreasureChest_dvergr_loose_stone

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | TreasureChest_dvergrtown.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | TreasureChest_dvergrtown.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | TreasureChest_dvergrtown.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## TreasureMapChest_Meadows

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (50%) | 68 (68%) | 30 (30%) | 2 (2%) | 0 (0%) |
> | Tier1Everything | 1 (50%) | 68 (68%) | 30 (30%) | 2 (2%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (50%) | 68 (68%) | 30 (30%) | 2 (2%) | 0 (0%) |
> | Tier1Everything | 1 (50%) | 68 (68%) | 30 (30%) | 2 (2%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (50%) | 68 (68%) | 30 (30%) | 2 (2%) | 0 (0%) |
> | Tier1Everything | 1 (50%) | 68 (68%) | 30 (30%) | 2 (2%) | 0 (0%) |


## TreasureMapChest_BlackForest

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (50%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | Tier2Everything | 1 (50%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (50%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | Tier2Everything | 1 (50%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (50%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | Tier2Everything | 1 (50%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |


## TreasureMapChest_Swamp

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (50%) | 10 (9.1%) | 45 (40.9%) | 50 (45.5%) | 5 (4.5%) |
> | Tier3Everything | 1 (50%) | 10 (9.1%) | 45 (40.9%) | 50 (45.5%) | 5 (4.5%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (50%) | 10 (9.1%) | 45 (40.9%) | 50 (45.5%) | 5 (4.5%) |
> | Tier3Everything | 1 (50%) | 10 (9.1%) | 45 (40.9%) | 50 (45.5%) | 5 (4.5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (50%) | 10 (9.1%) | 45 (40.9%) | 50 (45.5%) | 5 (4.5%) |
> | Tier3Everything | 1 (50%) | 10 (9.1%) | 45 (40.9%) | 50 (45.5%) | 5 (4.5%) |


## TreasureMapChest_Mountain

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (50%) | 0 (0%) | 30 (30%) | 60 (60%) | 10 (10%) |
> | Tier4Everything | 1 (50%) | 0 (0%) | 30 (30%) | 60 (60%) | 10 (10%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (50%) | 0 (0%) | 30 (30%) | 60 (60%) | 10 (10%) |
> | Tier4Everything | 1 (50%) | 0 (0%) | 30 (30%) | 60 (60%) | 10 (10%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (50%) | 0 (0%) | 30 (30%) | 60 (60%) | 10 (10%) |
> | Tier4Everything | 1 (50%) | 0 (0%) | 30 (30%) | 60 (60%) | 10 (10%) |


## TreasureMapChest_Plains

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |
> | Tier5Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |
> | Tier5Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |
> | Tier5Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |


## TreasureMapChest_Mistlands

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (50%) | 0 (0%) | 5 (5%) | 70 (70%) | 25 (25%) |
> | Tier6Everything | 1 (50%) | 0 (0%) | 5 (5%) | 70 (70%) | 25 (25%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (50%) | 0 (0%) | 5 (5%) | 70 (70%) | 25 (25%) |
> | Tier6Everything | 1 (50%) | 0 (0%) | 5 (5%) | 70 (70%) | 25 (25%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (50%) | 0 (0%) | 5 (5%) | 70 (70%) | 25 (25%) |
> | Tier6Everything | 1 (50%) | 0 (0%) | 5 (5%) | 70 (70%) | 25 (25%) |


## Greyling

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 100 (100%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 95 (95%) |
> | 1 | 5 (5%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 10 (10%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 39 (39%) |
> | 2 | 1 (1%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 100 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 90 (90%) | 10 (10%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 80 (80%) | 15 (15%) | 5 (5%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 70 (70%) | 19 (19%) | 10 (10%) | 1 (1%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 60 (60%) | 20 (20%) | 15 (15%) | 5 (5%) |


## Deer

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 100 (100%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 95 (95%) |
> | 1 | 5 (5%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 10 (10%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 39 (39%) |
> | 2 | 1 (1%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 100 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 90 (90%) | 10 (10%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 80 (80%) | 15 (15%) | 5 (5%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 70 (70%) | 19 (19%) | 10 (10%) | 1 (1%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 60 (60%) | 20 (20%) | 15 (15%) | 5 (5%) |


## Boar

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 100 (100%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 95 (95%) |
> | 1 | 5 (5%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 10 (10%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 39 (39%) |
> | 2 | 1 (1%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 100 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 90 (90%) | 10 (10%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 80 (80%) | 15 (15%) | 5 (5%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 70 (70%) | 19 (19%) | 10 (10%) | 1 (1%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 60 (60%) | 20 (20%) | 15 (15%) | 5 (5%) |


## Neck

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 100 (100%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 95 (95%) |
> | 1 | 5 (5%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 10 (10%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 39 (39%) |
> | 2 | 1 (1%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 100 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 90 (90%) | 10 (10%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 80 (80%) | 15 (15%) | 5 (5%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 70 (70%) | 19 (19%) | 10 (10%) | 1 (1%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 60 (60%) | 20 (20%) | 15 (15%) | 5 (5%) |


## Greydwarf

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 95 (95%) |
> | 1 | 5 (5%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 10 (10%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 85 (85%) |
> | 1 | 15 (15%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 25 (25%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 55 (55%) |
> | 1 | 43 (43%) |
> | 2 | 2 (2%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 10 (90.9%) | 75 (75%) | 25 (25%) | 0 (0%) | 0 (0%) |
> | Tier1Everything | 1 (9.1%) | 75 (75%) | 25 (25%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 5 (83.3%) | 70 (70%) | 29 (29%) | 1 (1%) | 0 (0%) |
> | Tier1Everything | 1 (16.7%) | 70 (70%) | 29 (29%) | 1 (1%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 3 (75%) | 63 (63%) | 34 (34%) | 2 (2%) | 1 (1%) |
> | Tier1Everything | 1 (25%) | 63 (63%) | 34 (34%) | 2 (2%) | 1 (1%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (50%) | 53 (53%) | 40 (40%) | 5 (5%) | 2 (2%) |
> | Tier1Everything | 1 (50%) | 53 (53%) | 40 (40%) | 5 (5%) | 2 (2%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (16.7%) | 45 (45%) | 43 (43%) | 9 (9%) | 3 (3%) |
> | Tier1Everything | 5 (83.3%) | 45 (45%) | 43 (43%) | 9 (9%) | 3 (3%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (9.1%) | 35 (35%) | 45 (45%) | 15 (15%) | 5 (5%) |
> | Tier1Everything | 10 (90.9%) | 35 (35%) | 45 (45%) | 15 (15%) | 5 (5%) |


## Skeleton

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 95 (95%) |
> | 1 | 5 (5%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 10 (10%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 85 (85%) |
> | 1 | 15 (15%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 25 (25%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 55 (55%) |
> | 1 | 43 (43%) |
> | 2 | 2 (2%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 10 (90.9%) | 75 (75%) | 25 (25%) | 0 (0%) | 0 (0%) |
> | Tier1Everything | 1 (9.1%) | 75 (75%) | 25 (25%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 5 (83.3%) | 70 (70%) | 29 (29%) | 1 (1%) | 0 (0%) |
> | Tier1Everything | 1 (16.7%) | 70 (70%) | 29 (29%) | 1 (1%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 3 (75%) | 63 (63%) | 34 (34%) | 2 (2%) | 1 (1%) |
> | Tier1Everything | 1 (25%) | 63 (63%) | 34 (34%) | 2 (2%) | 1 (1%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (50%) | 53 (53%) | 40 (40%) | 5 (5%) | 2 (2%) |
> | Tier1Everything | 1 (50%) | 53 (53%) | 40 (40%) | 5 (5%) | 2 (2%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (16.7%) | 45 (45%) | 43 (43%) | 9 (9%) | 3 (3%) |
> | Tier1Everything | 5 (83.3%) | 45 (45%) | 43 (43%) | 9 (9%) | 3 (3%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (9.1%) | 35 (35%) | 45 (45%) | 15 (15%) | 5 (5%) |
> | Tier1Everything | 10 (90.9%) | 35 (35%) | 45 (45%) | 15 (15%) | 5 (5%) |


## Greydwarf_Elite

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 10 (10%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 55 (55%) |
> | 1 | 44 (44%) |
> | 2 | 1 (1%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 40 (40%) |
> | 1 | 58 (58%) |
> | 2 | 2 (2%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 25 (25%) |
> | 1 | 71 (71%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (50%) | 50 (50%) | 47 (47%) | 2 (2%) | 1 (1%) |
> | Tier0Shields | 1 (50%) | 50 (50%) | 47 (47%) | 2 (2%) | 1 (1%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 5 (45.5%) | 40 (40%) | 53 (53%) | 5 (5%) | 2 (2%) |
> | Tier0Shields | 5 (45.5%) | 40 (40%) | 53 (53%) | 5 (5%) | 2 (2%) |
> | TrollArmor | 1 (9.1%) | 40 (40%) | 53 (53%) | 5 (5%) | 2 (2%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 3 (42.9%) | 29 (29%) | 59 (59%) | 9 (9%) | 3 (3%) |
> | Tier0Shields | 3 (42.9%) | 29 (29%) | 59 (59%) | 9 (9%) | 3 (3%) |
> | TrollArmor | 1 (14.3%) | 29 (29%) | 59 (59%) | 9 (9%) | 3 (3%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (33.3%) | 15 (15%) | 65 (65%) | 15 (15%) | 5 (5%) |
> | Tier0Shields | 1 (33.3%) | 15 (15%) | 65 (65%) | 15 (15%) | 5 (5%) |
> | TrollArmor | 1 (33.3%) | 15 (15%) | 65 (65%) | 15 (15%) | 5 (5%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (16.7%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |
> | Tier2Everything | 5 (83.3%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (9.1%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |
> | Tier2Everything | 10 (90.9%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |


## Greydwarf_Shaman

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 10 (10%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 55 (55%) |
> | 1 | 44 (44%) |
> | 2 | 1 (1%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 40 (40%) |
> | 1 | 58 (58%) |
> | 2 | 2 (2%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 25 (25%) |
> | 1 | 71 (71%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (50%) | 50 (50%) | 47 (47%) | 2 (2%) | 1 (1%) |
> | Tier0Shields | 1 (50%) | 50 (50%) | 47 (47%) | 2 (2%) | 1 (1%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 5 (45.5%) | 40 (40%) | 53 (53%) | 5 (5%) | 2 (2%) |
> | Tier0Shields | 5 (45.5%) | 40 (40%) | 53 (53%) | 5 (5%) | 2 (2%) |
> | TrollArmor | 1 (9.1%) | 40 (40%) | 53 (53%) | 5 (5%) | 2 (2%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 3 (42.9%) | 29 (29%) | 59 (59%) | 9 (9%) | 3 (3%) |
> | Tier0Shields | 3 (42.9%) | 29 (29%) | 59 (59%) | 9 (9%) | 3 (3%) |
> | TrollArmor | 1 (14.3%) | 29 (29%) | 59 (59%) | 9 (9%) | 3 (3%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (33.3%) | 15 (15%) | 65 (65%) | 15 (15%) | 5 (5%) |
> | Tier0Shields | 1 (33.3%) | 15 (15%) | 65 (65%) | 15 (15%) | 5 (5%) |
> | TrollArmor | 1 (33.3%) | 15 (15%) | 65 (65%) | 15 (15%) | 5 (5%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (16.7%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |
> | Tier2Everything | 5 (83.3%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (9.1%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |
> | Tier2Everything | 10 (90.9%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |


## Skeleton_Poison

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 10 (10%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 55 (55%) |
> | 1 | 44 (44%) |
> | 2 | 1 (1%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 40 (40%) |
> | 1 | 58 (58%) |
> | 2 | 2 (2%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 25 (25%) |
> | 1 | 71 (71%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (50%) | 50 (50%) | 47 (47%) | 2 (2%) | 1 (1%) |
> | Tier0Shields | 1 (50%) | 50 (50%) | 47 (47%) | 2 (2%) | 1 (1%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 5 (45.5%) | 40 (40%) | 53 (53%) | 5 (5%) | 2 (2%) |
> | Tier0Shields | 5 (45.5%) | 40 (40%) | 53 (53%) | 5 (5%) | 2 (2%) |
> | TrollArmor | 1 (9.1%) | 40 (40%) | 53 (53%) | 5 (5%) | 2 (2%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 3 (42.9%) | 29 (29%) | 59 (59%) | 9 (9%) | 3 (3%) |
> | Tier0Shields | 3 (42.9%) | 29 (29%) | 59 (59%) | 9 (9%) | 3 (3%) |
> | TrollArmor | 1 (14.3%) | 29 (29%) | 59 (59%) | 9 (9%) | 3 (3%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (33.3%) | 15 (15%) | 65 (65%) | 15 (15%) | 5 (5%) |
> | Tier0Shields | 1 (33.3%) | 15 (15%) | 65 (65%) | 15 (15%) | 5 (5%) |
> | TrollArmor | 1 (33.3%) | 15 (15%) | 65 (65%) | 15 (15%) | 5 (5%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (16.7%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |
> | Tier2Everything | 5 (83.3%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (9.1%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |
> | Tier2Everything | 10 (90.9%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |


## Troll

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 85 (85%) |
> | 1 | 15 (15%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 25 (25%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 48 (48%) |
> | 2 | 2 (2%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 35 (35%) |
> | 1 | 61 (61%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 20 (20%) |
> | 1 | 74 (74%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (100%) | 24 (24%) | 73 (73%) | 2 (2%) | 1 (1%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 5 (83.3%) | 15 (15%) | 78 (78%) | 5 (5%) | 2 (2%) |
> | Tier3Everything | 1 (16.7%) | 15 (15%) | 78 (78%) | 5 (5%) | 2 (2%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 3 (75%) | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) |
> | Tier3Everything | 1 (25%) | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (50%) | 0 (0%) | 80 (80%) | 15 (15%) | 5 (5%) |
> | Tier3Everything | 1 (50%) | 0 (0%) | 80 (80%) | 15 (15%) | 5 (5%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (16.7%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |
> | Tier3Everything | 5 (83.3%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (9.1%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |
> | Tier3Everything | 10 (90.9%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |


## Ghost

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 85 (85%) |
> | 1 | 15 (15%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 25 (25%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 48 (48%) |
> | 2 | 2 (2%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 35 (35%) |
> | 1 | 61 (61%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 20 (20%) |
> | 1 | 74 (74%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (100%) | 24 (24%) | 73 (73%) | 2 (2%) | 1 (1%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 5 (83.3%) | 15 (15%) | 78 (78%) | 5 (5%) | 2 (2%) |
> | Tier3Everything | 1 (16.7%) | 15 (15%) | 78 (78%) | 5 (5%) | 2 (2%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 3 (75%) | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) |
> | Tier3Everything | 1 (25%) | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (50%) | 0 (0%) | 80 (80%) | 15 (15%) | 5 (5%) |
> | Tier3Everything | 1 (50%) | 0 (0%) | 80 (80%) | 15 (15%) | 5 (5%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (16.7%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |
> | Tier3Everything | 5 (83.3%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (9.1%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |
> | Tier3Everything | 10 (90.9%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |


## Blob

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 85 (85%) |
> | 1 | 15 (15%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 25 (25%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 48 (48%) |
> | 2 | 2 (2%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 35 (35%) |
> | 1 | 61 (61%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 20 (20%) |
> | 1 | 74 (74%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (100%) | 24 (24%) | 73 (73%) | 2 (2%) | 1 (1%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 5 (83.3%) | 15 (15%) | 78 (78%) | 5 (5%) | 2 (2%) |
> | Tier3Everything | 1 (16.7%) | 15 (15%) | 78 (78%) | 5 (5%) | 2 (2%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 3 (75%) | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) |
> | Tier3Everything | 1 (25%) | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (50%) | 0 (0%) | 80 (80%) | 15 (15%) | 5 (5%) |
> | Tier3Everything | 1 (50%) | 0 (0%) | 80 (80%) | 15 (15%) | 5 (5%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (16.7%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |
> | Tier3Everything | 5 (83.3%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (9.1%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |
> | Tier3Everything | 10 (90.9%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |


## Draugr

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 85 (85%) |
> | 1 | 15 (15%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 25 (25%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 48 (48%) |
> | 2 | 2 (2%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 35 (35%) |
> | 1 | 61 (61%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 20 (20%) |
> | 1 | 74 (74%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (100%) | 24 (24%) | 73 (73%) | 2 (2%) | 1 (1%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 5 (83.3%) | 15 (15%) | 78 (78%) | 5 (5%) | 2 (2%) |
> | Tier3Everything | 1 (16.7%) | 15 (15%) | 78 (78%) | 5 (5%) | 2 (2%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 3 (75%) | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) |
> | Tier3Everything | 1 (25%) | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (50%) | 0 (0%) | 80 (80%) | 15 (15%) | 5 (5%) |
> | Tier3Everything | 1 (50%) | 0 (0%) | 80 (80%) | 15 (15%) | 5 (5%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (16.7%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |
> | Tier3Everything | 5 (83.3%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (9.1%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |
> | Tier3Everything | 10 (90.9%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |


## Draugr_Ranged

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 85 (85%) |
> | 1 | 15 (15%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 25 (25%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 48 (48%) |
> | 2 | 2 (2%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 35 (35%) |
> | 1 | 61 (61%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 20 (20%) |
> | 1 | 74 (74%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (100%) | 24 (24%) | 73 (73%) | 2 (2%) | 1 (1%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 5 (83.3%) | 15 (15%) | 78 (78%) | 5 (5%) | 2 (2%) |
> | Tier3Everything | 1 (16.7%) | 15 (15%) | 78 (78%) | 5 (5%) | 2 (2%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 3 (75%) | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) |
> | Tier3Everything | 1 (25%) | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (50%) | 0 (0%) | 80 (80%) | 15 (15%) | 5 (5%) |
> | Tier3Everything | 1 (50%) | 0 (0%) | 80 (80%) | 15 (15%) | 5 (5%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (16.7%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |
> | Tier3Everything | 5 (83.3%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (9.1%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |
> | Tier3Everything | 10 (90.9%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |


## Leech

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 85 (85%) |
> | 1 | 15 (15%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 25 (25%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 48 (48%) |
> | 2 | 2 (2%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 35 (35%) |
> | 1 | 61 (61%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 20 (20%) |
> | 1 | 74 (74%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (100%) | 24 (24%) | 73 (73%) | 2 (2%) | 1 (1%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 5 (83.3%) | 15 (15%) | 78 (78%) | 5 (5%) | 2 (2%) |
> | Tier3Everything | 1 (16.7%) | 15 (15%) | 78 (78%) | 5 (5%) | 2 (2%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 3 (75%) | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) |
> | Tier3Everything | 1 (25%) | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (50%) | 0 (0%) | 80 (80%) | 15 (15%) | 5 (5%) |
> | Tier3Everything | 1 (50%) | 0 (0%) | 80 (80%) | 15 (15%) | 5 (5%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (16.7%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |
> | Tier3Everything | 5 (83.3%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (9.1%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |
> | Tier3Everything | 10 (90.9%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |


## Surtling

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 85 (85%) |
> | 1 | 15 (15%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 25 (25%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 48 (48%) |
> | 2 | 2 (2%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 35 (35%) |
> | 1 | 61 (61%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 20 (20%) |
> | 1 | 74 (74%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (100%) | 24 (24%) | 73 (73%) | 2 (2%) | 1 (1%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 5 (83.3%) | 15 (15%) | 78 (78%) | 5 (5%) | 2 (2%) |
> | Tier3Everything | 1 (16.7%) | 15 (15%) | 78 (78%) | 5 (5%) | 2 (2%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 3 (75%) | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) |
> | Tier3Everything | 1 (25%) | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (50%) | 0 (0%) | 80 (80%) | 15 (15%) | 5 (5%) |
> | Tier3Everything | 1 (50%) | 0 (0%) | 80 (80%) | 15 (15%) | 5 (5%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (16.7%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |
> | Tier3Everything | 5 (83.3%) | 0 (0%) | 70 (70%) | 20 (20%) | 10 (10%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (9.1%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |
> | Tier3Everything | 10 (90.9%) | 0 (0%) | 60 (60%) | 25 (25%) | 15 (15%) |


## Draugr_Elite

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 48 (48%) |
> | 2 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 66 (66%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 10 (10%) |
> | 1 | 84 (84%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 92 (92%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (100%) | 0 (0%) | 75 (74.3%) | 24 (23.8%) | 2 (2%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 5 (83.3%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |
> | Tier4Everything | 1 (16.7%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 3 (75%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |
> | Tier4Everything | 1 (25%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |
> | Tier4Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (16.7%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |
> | Tier4Everything | 5 (83.3%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (9.1%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |
> | Tier4Everything | 10 (90.9%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |


## Abomination

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 48 (48%) |
> | 2 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 66 (66%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 10 (10%) |
> | 1 | 84 (84%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 92 (92%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (100%) | 0 (0%) | 75 (74.3%) | 24 (23.8%) | 2 (2%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 5 (83.3%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |
> | Tier4Everything | 1 (16.7%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 3 (75%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |
> | Tier4Everything | 1 (25%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |
> | Tier4Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (16.7%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |
> | Tier4Everything | 5 (83.3%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (9.1%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |
> | Tier4Everything | 10 (90.9%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |


## Ulv

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 48 (48%) |
> | 2 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 66 (66%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 10 (10%) |
> | 1 | 84 (84%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 92 (92%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (100%) | 0 (0%) | 75 (74.3%) | 24 (23.8%) | 2 (2%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 5 (83.3%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |
> | Tier4Everything | 1 (16.7%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 3 (75%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |
> | Tier4Everything | 1 (25%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |
> | Tier4Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (16.7%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |
> | Tier4Everything | 5 (83.3%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (9.1%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |
> | Tier4Everything | 10 (90.9%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |


## Fenring_Cultist

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 48 (48%) |
> | 2 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 66 (66%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 10 (10%) |
> | 1 | 84 (84%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 92 (92%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (100%) | 0 (0%) | 75 (74.3%) | 24 (23.8%) | 2 (2%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 5 (83.3%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |
> | Tier4Everything | 1 (16.7%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 3 (75%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |
> | Tier4Everything | 1 (25%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |
> | Tier4Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (16.7%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |
> | Tier4Everything | 5 (83.3%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (9.1%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |
> | Tier4Everything | 10 (90.9%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |


## BlobElite

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 48 (48%) |
> | 2 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 66 (66%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 10 (10%) |
> | 1 | 84 (84%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 92 (92%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (100%) | 0 (0%) | 75 (74.3%) | 24 (23.8%) | 2 (2%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 5 (83.3%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |
> | Tier4Everything | 1 (16.7%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 3 (75%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |
> | Tier4Everything | 1 (25%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |
> | Tier4Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (16.7%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |
> | Tier4Everything | 5 (83.3%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (9.1%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |
> | Tier4Everything | 10 (90.9%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |


## Wraith

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 48 (48%) |
> | 2 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 66 (66%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 10 (10%) |
> | 1 | 84 (84%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 92 (92%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (100%) | 0 (0%) | 75 (74.3%) | 24 (23.8%) | 2 (2%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 5 (83.3%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |
> | Tier4Everything | 1 (16.7%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 3 (75%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |
> | Tier4Everything | 1 (25%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |
> | Tier4Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (16.7%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |
> | Tier4Everything | 5 (83.3%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (9.1%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |
> | Tier4Everything | 10 (90.9%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |


## Wolf

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 48 (48%) |
> | 2 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 66 (66%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 10 (10%) |
> | 1 | 84 (84%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 92 (92%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (100%) | 0 (0%) | 75 (74.3%) | 24 (23.8%) | 2 (2%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 5 (83.3%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |
> | Tier4Everything | 1 (16.7%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 3 (75%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |
> | Tier4Everything | 1 (25%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |
> | Tier4Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (16.7%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |
> | Tier4Everything | 5 (83.3%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (9.1%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |
> | Tier4Everything | 10 (90.9%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |


## Hatchling

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 48 (48%) |
> | 2 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 66 (66%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 10 (10%) |
> | 1 | 84 (84%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 92 (92%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (100%) | 0 (0%) | 75 (74.3%) | 24 (23.8%) | 2 (2%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 5 (83.3%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |
> | Tier4Everything | 1 (16.7%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 3 (75%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |
> | Tier4Everything | 1 (25%) | 0 (0%) | 35 (35%) | 55 (55%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |
> | Tier4Everything | 1 (50%) | 0 (0%) | 10 (10%) | 75 (75%) | 15 (15%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (16.7%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |
> | Tier4Everything | 5 (83.3%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (9.1%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |
> | Tier4Everything | 10 (90.9%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |


## StoneGolem

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 24 (24%) |
> | 2 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 38 (38%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 45 (45%) |
> | 1 | 51 (51%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 25 (25%) |
> | 1 | 69 (69%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (100%) | 0 (0%) | 50 (48.5%) | 49 (47.6%) | 4 (3.9%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 5 (83.3%) | 0 (0%) | 30 (30%) | 65 (65%) | 5 (5%) |
> | Tier5Everything | 1 (16.7%) | 0 (0%) | 30 (30%) | 65 (65%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 3 (75%) | 0 (0%) | 10 (10%) | 80 (80%) | 10 (10%) |
> | Tier5Everything | 1 (25%) | 0 (0%) | 10 (10%) | 80 (80%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (50%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |
> | Tier5Everything | 1 (50%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (16.7%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |
> | Tier5Everything | 5 (83.3%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (9.1%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |
> | Tier5Everything | 10 (90.9%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |


## Fenring

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 24 (24%) |
> | 2 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 38 (38%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 45 (45%) |
> | 1 | 51 (51%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 25 (25%) |
> | 1 | 69 (69%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (100%) | 0 (0%) | 50 (48.5%) | 49 (47.6%) | 4 (3.9%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 5 (83.3%) | 0 (0%) | 30 (30%) | 65 (65%) | 5 (5%) |
> | Tier5Everything | 1 (16.7%) | 0 (0%) | 30 (30%) | 65 (65%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 3 (75%) | 0 (0%) | 10 (10%) | 80 (80%) | 10 (10%) |
> | Tier5Everything | 1 (25%) | 0 (0%) | 10 (10%) | 80 (80%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (50%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |
> | Tier5Everything | 1 (50%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (16.7%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |
> | Tier5Everything | 5 (83.3%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (9.1%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |
> | Tier5Everything | 10 (90.9%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |


## Serpent

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 24 (24%) |
> | 2 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 38 (38%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 45 (45%) |
> | 1 | 51 (51%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 25 (25%) |
> | 1 | 69 (69%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (100%) | 0 (0%) | 50 (48.5%) | 49 (47.6%) | 4 (3.9%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 5 (83.3%) | 0 (0%) | 30 (30%) | 65 (65%) | 5 (5%) |
> | Tier5Everything | 1 (16.7%) | 0 (0%) | 30 (30%) | 65 (65%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 3 (75%) | 0 (0%) | 10 (10%) | 80 (80%) | 10 (10%) |
> | Tier5Everything | 1 (25%) | 0 (0%) | 10 (10%) | 80 (80%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (50%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |
> | Tier5Everything | 1 (50%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (16.7%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |
> | Tier5Everything | 5 (83.3%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (9.1%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |
> | Tier5Everything | 10 (90.9%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |


## Deathsquito

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 24 (24%) |
> | 2 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 38 (38%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 45 (45%) |
> | 1 | 51 (51%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 25 (25%) |
> | 1 | 69 (69%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (100%) | 0 (0%) | 50 (48.5%) | 49 (47.6%) | 4 (3.9%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 5 (83.3%) | 0 (0%) | 30 (30%) | 65 (65%) | 5 (5%) |
> | Tier5Everything | 1 (16.7%) | 0 (0%) | 30 (30%) | 65 (65%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 3 (75%) | 0 (0%) | 10 (10%) | 80 (80%) | 10 (10%) |
> | Tier5Everything | 1 (25%) | 0 (0%) | 10 (10%) | 80 (80%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (50%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |
> | Tier5Everything | 1 (50%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (16.7%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |
> | Tier5Everything | 5 (83.3%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (9.1%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |
> | Tier5Everything | 10 (90.9%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |


## Lox

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 24 (24%) |
> | 2 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 38 (38%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 45 (45%) |
> | 1 | 51 (51%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 25 (25%) |
> | 1 | 69 (69%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (100%) | 0 (0%) | 50 (48.5%) | 49 (47.6%) | 4 (3.9%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 5 (83.3%) | 0 (0%) | 30 (30%) | 65 (65%) | 5 (5%) |
> | Tier5Everything | 1 (16.7%) | 0 (0%) | 30 (30%) | 65 (65%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 3 (75%) | 0 (0%) | 10 (10%) | 80 (80%) | 10 (10%) |
> | Tier5Everything | 1 (25%) | 0 (0%) | 10 (10%) | 80 (80%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (50%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |
> | Tier5Everything | 1 (50%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (16.7%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |
> | Tier5Everything | 5 (83.3%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (9.1%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |
> | Tier5Everything | 10 (90.9%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |


## Goblin

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 24 (24%) |
> | 2 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 38 (38%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 45 (45%) |
> | 1 | 51 (51%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 25 (25%) |
> | 1 | 69 (69%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (100%) | 0 (0%) | 50 (48.5%) | 49 (47.6%) | 4 (3.9%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 5 (83.3%) | 0 (0%) | 30 (30%) | 65 (65%) | 5 (5%) |
> | Tier5Everything | 1 (16.7%) | 0 (0%) | 30 (30%) | 65 (65%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 3 (75%) | 0 (0%) | 10 (10%) | 80 (80%) | 10 (10%) |
> | Tier5Everything | 1 (25%) | 0 (0%) | 10 (10%) | 80 (80%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (50%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |
> | Tier5Everything | 1 (50%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (16.7%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |
> | Tier5Everything | 5 (83.3%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (9.1%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |
> | Tier5Everything | 10 (90.9%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |


## GoblinArcher

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 24 (24%) |
> | 2 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 38 (38%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 45 (45%) |
> | 1 | 51 (51%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 25 (25%) |
> | 1 | 69 (69%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (100%) | 0 (0%) | 50 (48.5%) | 49 (47.6%) | 4 (3.9%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 5 (83.3%) | 0 (0%) | 30 (30%) | 65 (65%) | 5 (5%) |
> | Tier5Everything | 1 (16.7%) | 0 (0%) | 30 (30%) | 65 (65%) | 5 (5%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 3 (75%) | 0 (0%) | 10 (10%) | 80 (80%) | 10 (10%) |
> | Tier5Everything | 1 (25%) | 0 (0%) | 10 (10%) | 80 (80%) | 10 (10%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (50%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |
> | Tier5Everything | 1 (50%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (16.7%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |
> | Tier5Everything | 5 (83.3%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (9.1%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |
> | Tier5Everything | 10 (90.9%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |


## GoblinBrute

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 28 (28%) |
> | 2 | 2 (2%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 46 (46%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 64 (64%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 80 (80%) |
> | 2 | 15 (15%) |
> | 3 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 25 (25%) | 65 (65%) | 10 (10%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 0 (0%) | 65 (65%) | 35 (35%) |


## GoblinShaman

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 28 (28%) |
> | 2 | 2 (2%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 46 (46%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 64 (64%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 80 (80%) |
> | 2 | 15 (15%) |
> | 3 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 25 (25%) | 65 (65%) | 10 (10%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 0 (0%) | 65 (65%) | 35 (35%) |


## Seeker

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 28 (28%) |
> | 2 | 2 (2%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 46 (46%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 64 (64%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 80 (80%) |
> | 2 | 15 (15%) |
> | 3 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 25 (25%) | 65 (65%) | 10 (10%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 65 (65%) | 35 (35%) |


## SeekerBrute

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 28 (28%) |
> | 2 | 2 (2%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 46 (46%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 64 (64%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 80 (80%) |
> | 2 | 15 (15%) |
> | 3 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 25 (25%) | 65 (65%) | 10 (10%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 65 (65%) | 35 (35%) |


## Dverger

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 28 (28%) |
> | 2 | 2 (2%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 46 (46%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 64 (64%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 80 (80%) |
> | 2 | 15 (15%) |
> | 3 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 25 (25%) | 65 (65%) | 10 (10%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 65 (65%) | 35 (35%) |


## DvergerMage

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 28 (28%) |
> | 2 | 2 (2%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 46 (46%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 64 (64%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 80 (80%) |
> | 2 | 15 (15%) |
> | 3 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 25 (25%) | 65 (65%) | 10 (10%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 65 (65%) | 35 (35%) |


## DvergerMageFire

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 28 (28%) |
> | 2 | 2 (2%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 46 (46%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 64 (64%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 80 (80%) |
> | 2 | 15 (15%) |
> | 3 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 25 (25%) | 65 (65%) | 10 (10%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 65 (65%) | 35 (35%) |


## DvergerMageIce

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 28 (28%) |
> | 2 | 2 (2%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 46 (46%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 64 (64%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 80 (80%) |
> | 2 | 15 (15%) |
> | 3 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 25 (25%) | 65 (65%) | 10 (10%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 65 (65%) | 35 (35%) |


## DvergerMageSupport

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 28 (28%) |
> | 2 | 2 (2%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 46 (46%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 64 (64%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 80 (80%) |
> | 2 | 15 (15%) |
> | 3 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 25 (25%) | 65 (65%) | 10 (10%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 65 (65%) | 35 (35%) |


## Gjall

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 28 (28%) |
> | 2 | 2 (2%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 46 (46%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 64 (64%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 80 (80%) |
> | 2 | 15 (15%) |
> | 3 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 25 (25%) | 65 (65%) | 10 (10%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 65 (65%) | 35 (35%) |


## Tick

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 28 (28%) |
> | 2 | 2 (2%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 46 (46%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 64 (64%) |
> | 2 | 4 (4%) |
> | 3 | 2 (2%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 0 | 5 (5%) |
> | 1 | 87 (87%) |
> | 2 | 5 (5%) |
> | 3 | 3 (3%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 86 (86%) |
> | 2 | 10 (10%) |
> | 3 | 4 (4%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 0 | 0 (0%) |
> | 1 | 80 (80%) |
> | 2 | 15 (15%) |
> | 3 | 5 (5%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 25 (25%) | 65 (65%) | 10 (10%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 85 (85%) | 15 (15%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Everything | 1 (100%) | 0 (0%) | 0 (0%) | 65 (65%) | 35 (35%) |


