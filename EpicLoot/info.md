# EpicLoot Data v0.7.7

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

# MagicItemEffect List

The following lists all the built-in **MagicItemEffects**. MagicItemEffects are defined in `magiceffects.json` and are parsed and added to `MagicItemEffectDefinitions` on Awake. EpicLoot uses an string for the types of magic effects. You can add your own new types using your own identifiers.

Listen to the event `MagicItemEffectDefinitions.OnSetupMagicItemEffectDefinitions` (which gets called in `EpicLoot.Awake`) to add your own using instances of MagicItemEffectDefinition.

  * **Display Text:** This text appears in the tooltip for the magic item, with {0:?} replaced with the rolled value for the effect, formatted using the shown C# string format.
  * **Requirements:** A set of requirements.
    * **Flags:** A set of predefined flags to check certain weapon properties. The list of flags is: `NoRoll, ExclusiveSelf, ItemHasPhysicalDamage, ItemHasElementalDamage, ItemUsesDurability, ItemHasNegativeMovementSpeedModifier, ItemHasBlockPower, ItemHasParryPower, ItemHasArmor, ItemHasBackstabBonus, ItemUsesStaminaOnAttack`
    * **ExclusiveEffectTypes:** This effect may not be rolled on an item that has already rolled on of these effects
    * **AllowedItemTypes:** This effect may only be rolled on items of a the types in this list. When this list is empty, this is usually done because this is a special effect type added programmatically  or currently not allowed to roll. Options are: `Helmet, Chest, Legs, Shoulder, Utility, Bow, OneHandedWeapon, TwoHandedWeapon, Shield, Tool, Torch`
    * **ExcludedItemTypes:** This effect may only be rolled on items that are not one of the types on this list.
    * **AllowedRarities:** This effect may only be rolled on an item of one of these rarities. Options are: `Magic, Rare, Epic, Legendary`
    * **ExcludedRarities:** This effect may only be rolled on an item that is not of one of these rarities.
    * **AllowedSkillTypes:** This effect may only be rolled on an item that uses one of these skill types. Options are: `Swords, Knives, Clubs, Polearms, Spears, Blocking, Axes, Bows, Unarmed, Pickaxes`
    * **ExcludedSkillTypes:** This effect may only be rolled on an item that does not use one of these skill types.
    * **AllowedItemNames:** This effect may only be rolled on an item with one of these names. Use the unlocalized shared name, i.e.: `$item_sword_iron`
    * **ExcludedItemNames:** This effect may only be rolled on an item that does not have one of these names.
    * **CustomFlags:** A set of any arbitrary strings for future use
  * **Value Per Rarity:** This effect may only be rolled on items of a rarity included in this table. The value is rolled using a linear distribution between Min and Max and divisible by the Increment.

## DvergerCirclet

> **Display Text:** Perpetual lightsource
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> 
> ***Notes:*** *Can't be rolled. Just added to make a Magic Item version of this item.*

## Megingjord

> **Display Text:** Carry weight increased by +150
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> 
> ***Notes:*** *Can't be rolled. Just added to make a Magic Item version of this item.*

## Wishbone

> **Display Text:** Finds secrets
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> 
> ***Notes:*** *Can't be rolled. Just added to make a Magic Item version of this item.*

## Andvaranaut

> **Display Text:** Finds Haldor's treasure chests
> 
> **Allowed Item Types:** *None*
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> 
> ***Notes:*** *Can't be rolled. Just added to make a Magic Item version of this item.*

## ModifyDamage

> **Display Text:** All damage increased by +{0:0.#}%
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
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|
> 
> ***Notes:*** *Can't be rolled. Too powerful?*

## ModifyPhysicalDamage

> **Display Text:** Physical damage increased by +{0:0.#}%
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemHasPhysicalDamage`
> > **ExclusiveEffectTypes:** `AddBluntDamage, AddSlashingDamage, AddPiercingDamage`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Torch`
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

> **Display Text:** Elemental damage increased by +{0:0.#}%
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemHasElementalDamage`
> > **ExclusiveEffectTypes:** `AddFireDamage, AddFrostDamage, AddLightningDamage`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Torch`
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

> **Display Text:** Max durability increased by +{0:0.#}%
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemUsesDurability`
> > **ExclusiveEffectTypes:** `Indestructible`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder`
> > **AllowedRarities:** `Magic, Rare, Epic`

## ReduceWeight

> **Display Text:** Weight reduced by -{0:0.#}% 
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **ExclusiveEffectTypes:** `Weightless`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder`
> > **AllowedRarities:** `Magic, Rare, Epic`

## RemoveSpeedPenalty

> **Display Text:** Movement speed penalty removed
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemHasNegativeMovementSpeedModifier`
> > **ExclusiveEffectTypes:** `ModifyMovementSpeed`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder`

## ModifyBlockPower

> **Display Text:** Block improved by +{0:0.#}%
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
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|

## ModifyParry

> **Display Text:** Parry improved by +{0:0.#}%
> 
> **Allowed Item Types:** Shield, TwoHandedWeapon
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemHasParryPower`
> > **AllowedItemTypes:** `Shield, TwoHandedWeapon`
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

## ModifyArmor

> **Display Text:** Armor increased by +{0:0.#}%
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

> **Display Text:** Backstab improved by +{0:0.#}%
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

> **Display Text:** Health increased by +{0:0}
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

> **Display Text:** Stamina increased by +{0:0}
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

## ModifyHealthRegen

> **Display Text:** Health regen improved by +{0:0.#}%
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

## ModifyStaminaRegen

> **Display Text:** Stamina regen improved by +{0:0.#}%
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

## AddBluntDamage

> **Display Text:** Add +{0:0.#} blunt damage
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

> **Display Text:** Add +{0:0.#} slashing damage
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

> **Display Text:** Add +{0:0.#} piercing damage
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

> **Display Text:** Add +{0:0.#} fire damage
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

## AddFrostDamage

> **Display Text:** Add +{0:0.#} frost damage
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

## AddLightningDamage

> **Display Text:** Add +{0:0.#} lightning damage
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

## AddPoisonDamage

> **Display Text:** Attacks deal poison damage for {0:0} seconds
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
> |Magic|2|4|1|
> |Rare|4|8|1|
> |Epic|8|15|1|
> |Legendary|15|20|1|

## AddSpiritDamage

> **Display Text:** Add +{0:0.#} spirit damage
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

## AddFireResistance

> **Display Text:** Gain fire resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`

## AddFrostResistance

> **Display Text:** Gain frost resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`

## AddLightningResistance

> **Display Text:** Gain lightning resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`

## AddPoisonResistance

> **Display Text:** Gain poison resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`

## AddSpiritResistance

> **Display Text:** Gain spirit resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Requirements:**
> > **Flags:** `NoRoll, ExclusiveSelf`
> > **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Shield`

## ModifyMovementSpeed

> **Display Text:** Movement increased by +{0:0.#}%
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

> **Display Text:** Reduce sprint stamina use by -{0:0.#}%
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

> **Display Text:** Reduce jump stamina use by -{0:0.#}%
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

> **Display Text:** Reduce attack stamina use by -{0:0.#}%
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Tool
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemUsesStaminaOnAttack`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Tool`
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

> **Display Text:** Reduce block stamina use by -{0:0.#}%
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
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Tool, Shield, Helmet, Chest, Legs, Shoulder
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **ExclusiveEffectTypes:** `ModifyDurability`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Tool, Shield, Helmet, Chest, Legs, Shoulder`
> > **AllowedRarities:** `Epic, Legendary`

## Weightless

> **Display Text:** Weightless
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Tool, Shield, Helmet, Chest, Legs, Shoulder
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **ExclusiveEffectTypes:** `ReduceWeight`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Tool, Shield, Helmet, Chest, Legs, Shoulder`

## AddCarryWeight

> **Display Text:** Increase carry weight by +{0}
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

> **Display Text:** Heal for {0:0.0}% of damage done
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf, ItemUsesStaminaOnAttack`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow`
> > **AllowedRarities:** `Rare, Epic, Legendary`
> > **ExcludedSkillTypes:** `Pickaxes`

## ModifyAttackSpeed

> **Display Text:** Attack speed increased by +{0:0.#}%
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Tool
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Tool`
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
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon`
> > **ExcludedSkillTypes:** `Pickaxes, Spears`

## Waterproof

> **Display Text:** Waterproof
> 
> **Allowed Item Types:** Shoulder
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Shoulder`

## Paralyze

> **Display Text:** Paralyze for {0:0.#} seconds
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

> **Display Text:** $mod_epicloot_me_doublejump
> 
> **Allowed Item Types:** Legs
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Legs`
> > **AllowedRarities:** `Epic, Legendary`

## WaterWalking

> **Display Text:** $mod_epicloot_me_waterwalk
> 
> **Allowed Item Types:** Legs
> 
> **Requirements:**
> > **Flags:** `ExclusiveSelf`
> > **AllowedItemTypes:** `Legs`
> > **AllowedRarities:** `Rare, Epic, Legendary`

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


## Tier1Everything

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Weapons | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier1Armor | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier1Tools | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Battleaxe | 1 (14.3%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | SwordIron | 1 (14.3%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | AxeIron | 1 (14.3%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | SledgeIron | 1 (14.3%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | MaceIron | 1 (14.3%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | AtgeirIron | 1 (14.3%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | SpearElderbark | 1 (14.3%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |


## Tier3Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorIronLegs | 1 (33.3%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | ArmorIronChest | 1 (33.3%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |
> | HelmetIron | 1 (33.3%) | 38 (38%) | 50 (50%) | 8 (8%) | 4 (4%) |


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
> | SwordSilver | 10 (45.5%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | SpearWolfFang | 10 (45.5%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | MaceSilver | 1 (4.5%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | BowDraugrFang | 1 (4.5%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |


## Tier4Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorWolfLegs | 1 (25%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | ArmorWolfChest | 1 (25%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | HelmetDrake | 1 (25%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |
> | CapeWolf | 1 (25%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 20 (18.2%) |


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
> | AtgeirBlackmetal | 3 (23.1%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |
> | AxeBlackMetal | 3 (23.1%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |
> | KnifeBlackMetal | 3 (23.1%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |
> | SwordBlackmetal | 3 (23.1%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |
> | MaceNeedle | 1 (7.7%) | 0 (0%) | 15 (15%) | 60 (60%) | 25 (25%) |


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
> | Tier0Everything | 1 (100%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 92 (92%) | 5 (5%) | 2 (2%) | 1 (1%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 85 (85%) | 10 (10%) | 3 (3%) | 2 (2%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 77 (77%) | 15 (15%) | 5 (5%) | 3 (3%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 75 (68.2%) | 20 (18.2%) | 10 (9.1%) | 5 (4.5%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 60 (57.1%) | 20 (19%) | 15 (14.3%) | 10 (9.5%) |


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
> | Tier0Everything | 10 (90.9%) | 70 (70%) | 25 (25%) | 3 (3%) | 2 (2%) |
> | Tier1Everything | 1 (9.1%) | 70 (70%) | 25 (25%) | 3 (3%) | 2 (2%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 5 (83.3%) | 62 (62%) | 30 (30%) | 5 (5%) | 3 (3%) |
> | Tier1Everything | 1 (16.7%) | 62 (62%) | 30 (30%) | 5 (5%) | 3 (3%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 3 (75%) | 65 (65%) | 35 (35%) | 0 (0%) | 0 (0%) |
> | Tier1Everything | 1 (25%) | 65 (65%) | 35 (35%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (50%) | 55 (55%) | 40 (40%) | 5 (5%) | 0 (0%) |
> | Tier1Everything | 1 (50%) | 55 (55%) | 40 (40%) | 5 (5%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (16.7%) | 45 (45%) | 44 (44%) | 10 (10%) | 1 (1%) |
> | Tier1Everything | 5 (83.3%) | 45 (45%) | 44 (44%) | 10 (10%) | 1 (1%) |

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
> | Tier1Everything | 1 (50%) | 50 (50%) | 50 (50%) | 0 (0%) | 0 (0%) |
> | Tier0Shields | 1 (50%) | 50 (50%) | 50 (50%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 5 (45.5%) | 40 (40%) | 55 (55%) | 5 (5%) | 0 (0%) |
> | Tier0Shields | 5 (45.5%) | 40 (40%) | 55 (55%) | 5 (5%) | 0 (0%) |
> | TrollArmor | 1 (9.1%) | 40 (40%) | 55 (55%) | 5 (5%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 3 (42.9%) | 30 (30%) | 60 (60%) | 9 (9%) | 1 (1%) |
> | Tier0Shields | 3 (42.9%) | 30 (30%) | 60 (60%) | 9 (9%) | 1 (1%) |
> | TrollArmor | 1 (14.3%) | 30 (30%) | 60 (60%) | 9 (9%) | 1 (1%) |

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
> | Tier2Everything | 1 (100%) | 25 (25%) | 75 (75%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 5 (83.3%) | 15 (15%) | 80 (80%) | 5 (5%) | 0 (0%) |
> | Tier3Everything | 1 (16.7%) | 15 (15%) | 80 (80%) | 5 (5%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 3 (75%) | 5 (5%) | 85 (85%) | 9 (9%) | 1 (1%) |
> | Tier2Everything | 1 (25%) | 5 (5%) | 85 (85%) | 9 (9%) | 1 (1%) |

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
> | Tier3Everything | 1 (100%) | 0 (0%) | 75 (75%) | 24 (24%) | 1 (1%) |

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
> | Tier4Everything | 1 (100%) | 0 (0%) | 50 (50%) | 49 (49%) | 1 (1%) |

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
> | Tier0Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier0Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier0Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier0Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier1Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier1Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier2Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier2Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier2Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier3Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier3Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier3Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier3Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier3Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier3Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier4Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier4Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier4Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier4Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier4Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier5Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier5Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier5Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier5Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier5Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier5Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier6Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | Tier6Mob.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Mob.2 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Mob.3 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Mob.4 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Mob.5 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier6Mob.6 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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


## Moder

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 2 | 40 (40%) |
> | 3 | 40 (40%) |
> | 4 | 20 (20%) |
> | 5 | 0 (0%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 2 | 20 (20%) |
> | 3 | 50 (50%) |
> | 4 | 30 (30%) |
> | 5 | 0 (0%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 2 | 0 (0%) |
> | 3 | 60 (60%) |
> | 4 | 40 (40%) |
> | 5 | 0 (0%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 2 | 0 (0%) |
> | 3 | 35 (35%) |
> | 4 | 60 (60%) |
> | 5 | 5 (5%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 2 | 0 (0%) |
> | 3 | 10 (10%) |
> | 4 | 80 (80%) |
> | 5 | 10 (10%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 2 | 0 (0%) |
> | 4 | 85 (85%) |
> | 5 | 15 (15%) |

> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (100%) | 0 (0%) | 40 (40%) | 50 (50%) | 10 (10%) |

> | Items (lvl 2) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Moder.1 | 1 (100%) | 0 (0%) | 20 (20%) | 65 (65%) | 15 (15%) |

> | Items (lvl 3) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Moder.1 | 1 (100%) | 0 (0%) | 0 (0%) | 80 (80%) | 20 (20%) |

> | Items (lvl 4) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Moder.1 | 1 (100%) | 0 (0%) | 0 (0%) | 75 (75%) | 25 (25%) |

> | Items (lvl 5) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Moder.1 | 1 (100%) | 0 (0%) | 0 (0%) | 70 (70%) | 30 (30%) |

> | Items (lvl 6) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Moder.1 | 1 (100%) | 0 (0%) | 0 (0%) | 60 (60%) | 40 (40%) |


## GoblinKing

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 2 | 20 (20%) |
> | 3 | 60 (60%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 2 | 0 (0%) |
> | 3 | 70 (70%) |
> | 4 | 20 (20%) |
> | 5 | 10 (10%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 2 | 0 (0%) |
> | 3 | 45 (45%) |
> | 4 | 40 (40%) |
> | 5 | 15 (15%) |

> | Drops (lvl 4) | Weight (Chance) |
> | -- | -- |
> | 2 | 0 (0%) |
> | 3 | 25 (25%) |
> | 4 | 50 (50%) |
> | 5 | 25 (25%) |

> | Drops (lvl 5) | Weight (Chance) |
> | -- | -- |
> | 2 | 0 (0%) |
> | 3 | 5 (5%) |
> | 4 | 60 (60%) |
> | 5 | 35 (35%) |

> | Drops (lvl 6) | Weight (Chance) |
> | -- | -- |
> | 2 | 0 (0%) |
> | 4 | 55 (55%) |
> | 5 | 45 (45%) |

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


