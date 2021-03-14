# EpicLoot Data v0.5.11

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

The following lists all the built-in **MagicItemEffects**. MagicItemEffects are hardcoded in `MagicItemEffectDefinitions_Setup.cs` and added to `MagicItemEffectDefinitions`. EpicLoot uses an enum for the types of magic effects, but the backing field underneath is an int. You can add your own new types using your own enum that starts after `MagicEffectType.MagicEffectEnumEnd` and cast it to `MagicEffectType` or use your own range of int identifiers.

Listen to the event `MagicItemEffectDefinitions.OnSetupMagicItemEffectDefinitions` (which gets called in `EpicLoot.Awake`) to add your own.

The int value of the type is displayed in parentheses after the name.

  * **Display Text:** This text appears in the tooltip for the magic item, with {0:?} replaced with the rolled value for the effect, formatted using the shown C# string format.
  * **Allowed Item Types:** This effect may only be rolled on items of a the types in this list. When this list is empty, this is usually done because this is a special effect type added programmatically  or currently not allowed to roll.
  * **Requirement:** A function called when attempting to add this effect to an item. The `Requirement` function must return true for this effect to be able to be added to this magic item.
  * **Value Per Rarity:** This effect may only be rolled on items of a rarity included in this table. The value is rolled using a linear distribution between Min and Max and divisible by the Increment.

Some lists of effect types are used in requirements to consolidate code. They are: PhysicalDamageEffects, ElementalDamageEffects, and AllDamageEffects. Included here for your reference:

  * **`PhysicalDamageEffects`:** AddBluntDamage, AddSlashingDamage, AddPiercingDamage
  * **`ElementalDamageEffects`:** AddFireDamage, AddFrostDamage, AddLightningDamage
  * **`AllDamageEffects`:** AddBluntDamage, AddSlashingDamage, AddPiercingDamage, AddFireDamage, AddFrostDamage, AddLightningDamage, AddPoisonDamage, AddSpiritDamage

## DvergerCirclet (0)

> **Display Text:** Perpetual lightsource
> 
> **Allowed Item Types:** *None*
> 
> ***Notes:*** *Can't be rolled. Just added to make a Legendary Magic Item version of this item.*

## Megingjord (1)

> **Display Text:** Carry weight increased by +150
> 
> **Allowed Item Types:** *None*
> 
> ***Notes:*** *Can't be rolled. Just added to make a Legendary Magic Item version of this item.*

## Wishbone (2)

> **Display Text:** Finds secrets
> 
> **Allowed Item Types:** *None*
> 
> ***Notes:*** *Can't be rolled. Just added to make a Legendary Magic Item version of this item.*

## ModifyDamage (3)

> **Display Text:** All damage increased by +{0:0.#}%
> 
> **Allowed Item Types:** *None*
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

## ModifyPhysicalDamage (4)

> **Display Text:** Physical damage increased by +{0:0.#}%
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch
> 
> **Requirement:**
> ```
> (itemData, magicItem) => itemData.m_shared.m_damages.GetTotalPhysicalDamage() > 0 || magicItem.HasAnyEffect(PhysicalDamageEffects)
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|

## ModifyElementalDamage (5)

> **Display Text:** Elemental damage increased by +{0:0.#}%
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch
> 
> **Requirement:**
> ```
> (itemData, magicItem) => itemData.m_shared.m_damages.GetTotalElementalDamage() > 0 || magicItem.HasAnyEffect(ElementalDamageEffects)
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|

## ModifyDurability (6)

> **Display Text:** Max durability increased by +{0:0.#}%
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder, Utility
> 
> **Requirement:**
> ```
> (itemData, magicItem) => !magicItem.HasEffect(MagicEffectType.Indestructible)
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|50|100|5|
> |Rare|50|100|5|
> |Epic|50|100|5|

## ReduceWeight (7)

> **Display Text:** Weight reduced by -{0:0.#}% 
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder, Utility
> 
> **Requirement:**
> ```
> (itemData, magicItem) => !magicItem.HasEffect(MagicEffectType.Weightless)
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|70|90|5|
> |Rare|70|90|5|
> |Epic|70|90|5|

## RemoveSpeedPenalty (8)

> **Display Text:** Movement speed penalty removed
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder, Utility
> 
> **Requirement:**
> ```
> (itemData, magicItem) => itemData.m_shared.m_movementModifier < 0
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|0|0|
> |Rare|1|0|0|
> |Epic|1|0|0|
> |Legendary|1|0|0|

## ModifyBlockPower (9)

> **Display Text:** Block improved by +{0:0.#}%
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Shield
> 
> **Requirement:**
> ```
> (itemData, magicItem) => itemData.m_shared.m_blockPower > 0
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|

## ModifyParry (10)

> **Display Text:** Parry improved by +{0:0.#}%
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Shield
> 
> **Requirement:**
> ```
> (itemData, magicItem) => itemData.m_shared.m_deflectionForce > 0
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|

## ModifyArmor (11)

> **Display Text:** Armor increased by +{0:0.#}%
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility
> 
> **Requirement:**
> ```
> (itemData, magicItem) => itemData.m_shared.m_armor > 0
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|

## ModifyBackstab (12)

> **Display Text:** Backstab improved by +{0:0.#}%
> 
> **Allowed Item Types:** OneHandedWeapon, Bow
> 
> **Requirement:**
> ```
> (itemData, magicItem) => itemData.m_shared.m_backstabBonus > 0
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|

## IncreaseHealth (13)

> **Display Text:** Health increased by +{0:0}
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|25|5|
> |Rare|15|30|5|
> |Epic|20|35|5|
> |Legendary|25|50|5|

## IncreaseStamina (14)

> **Display Text:** Stamina increased by +{0:0}
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Tool
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|25|5|
> |Rare|15|30|5|
> |Epic|20|35|5|
> |Legendary|25|50|5|

## ModifyHealthRegen (15)

> **Display Text:** Health regen improved by +{0:0.#}%
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|

## ModifyStaminaRegen (16)

> **Display Text:** Stamina regen improved by +{0:0.#}%
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Tool
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|10|20|1|
> |Rare|10|20|1|
> |Epic|10|20|1|
> |Legendary|15|25|1|

## AddBluntDamage (17)

> **Display Text:** Add +{0:0.#} blunt damage
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch
> 
> **Requirement:**
> ```
> (itemData, magicItem) => !magicItem.HasAnyEffect(AllDamageEffects)
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddSlashingDamage (18)

> **Display Text:** Add +{0:0.#} slashing damage
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch
> 
> **Requirement:**
> ```
> (itemData, magicItem) => !magicItem.HasAnyEffect(AllDamageEffects)
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddPiercingDamage (19)

> **Display Text:** Add +{0:0.#} piercing damage
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch
> 
> **Requirement:**
> ```
> (itemData, magicItem) => !magicItem.HasAnyEffect(AllDamageEffects)
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddFireDamage (20)

> **Display Text:** Add +{0:0.#} fire damage
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch
> 
> **Requirement:**
> ```
> (itemData, magicItem) => !magicItem.HasAnyEffect(AllDamageEffects)
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddFrostDamage (21)

> **Display Text:** Add +{0:0.#} frost damage
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch
> 
> **Requirement:**
> ```
> (itemData, magicItem) => !magicItem.HasAnyEffect(AllDamageEffects)
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddLightningDamage (22)

> **Display Text:** Add +{0:0.#} lightning damage
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch
> 
> **Requirement:**
> ```
> (itemData, magicItem) => !magicItem.HasAnyEffect(AllDamageEffects)
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddPoisonDamage (23)

> **Display Text:** Attacks deal poison damage for {0:0} seconds
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch
> 
> **Requirement:**
> ```
> (itemData, magicItem) => !magicItem.HasAnyEffect(AllDamageEffects)
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|2|4|1|
> |Rare|4|8|1|
> |Epic|8|15|1|
> |Legendary|15|20|1|

## AddSpiritDamage (24)

> **Display Text:** Add +{0:0.#} spirit damage
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch
> 
> **Requirement:**
> ```
> (itemData, magicItem) => !magicItem.HasAnyEffect(AllDamageEffects)
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|1|4|1|
> |Rare|3|8|1|
> |Epic|7|15|1|
> |Legendary|15|20|1|

## AddFireResistance (25)

> **Display Text:** Gain fire resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|0|0|0|
> |Rare|0|0|0|
> |Epic|0|0|0|
> |Legendary|0|0|0|

## AddFrostResistance (26)

> **Display Text:** Gain frost resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|0|0|0|
> |Rare|0|0|0|
> |Epic|0|0|0|
> |Legendary|0|0|0|

## AddLightningResistance (27)

> **Display Text:** Gain lightning resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|0|0|0|
> |Rare|0|0|0|
> |Epic|0|0|0|
> |Legendary|0|0|0|

## AddPoisonResistance (28)

> **Display Text:** Gain poison resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|0|0|0|
> |Rare|0|0|0|
> |Epic|0|0|0|
> |Legendary|0|0|0|

## AddSpiritResistance (29)

> **Display Text:** Gain spirit resistance
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility, Shield
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|0|0|0|
> |Rare|0|0|0|
> |Epic|0|0|0|
> |Legendary|0|0|0|

## ModifyMovementSpeed (30)

> **Display Text:** Movement increased by +{0:0.#}%
> 
> **Allowed Item Types:** Legs
> 
> **Requirement:**
> ```
> (itemData, magicItem) => !magicItem.HasEffect(MagicEffectType.RemoveSpeedPenalty)
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|15|1|
> |Rare|15|30|1|
> |Epic|30|45|1|
> |Legendary|45|60|1|

## ModifySprintStaminaUse (31)

> **Display Text:** Reduce sprint stamina use by -{0:0.#}%
> 
> **Allowed Item Types:** Legs
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|1|
> |Rare|8|13|1|
> |Epic|11|16|1|
> |Legendary|14|19|1|

## ModifyJumpStaminaUse (32)

> **Display Text:** Reduce jump stamina use by -{0:0.#}%
> 
> **Allowed Item Types:** Legs
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|1|
> |Rare|8|13|1|
> |Epic|11|16|1|
> |Legendary|14|19|1|

## ModifyAttackStaminaUse (33)

> **Display Text:** Reduce attack stamina use by -{0:0.#}%
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Tool
> 
> **Requirement:**
> ```
> (itemData, magicItem) => itemData.m_shared.m_attack.m_attackStamina > 0 || itemData.m_shared.m_secondaryAttack.m_attackStamina > 0
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|1|
> |Rare|8|13|1|
> |Epic|11|16|1|
> |Legendary|14|19|1|

## ModifyBlockStaminaUse (34)

> **Display Text:** Reduce block stamina use by -{0:0.#}%
> 
> **Allowed Item Types:** Shield
> 
> **Requirement:**
> ```
> (itemData, magicItem) => itemData.m_shared.m_blockPower > 0
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|1|
> |Rare|8|13|1|
> |Epic|11|16|1|
> |Legendary|14|19|1|

## Indestructible (35)

> **Display Text:** Indestructible
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Tool, Shield, Helmet, Chest, Legs, Shoulder, Utility
> 
> **Requirement:**
> ```
> (itemData, magicItem) => !magicItem.HasEffect(MagicEffectType.ModifyDurability)
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Epic|0|0|0|
> |Legendary|0|0|0|

## Weightless (36)

> **Display Text:** Weightless
> 
> **Allowed Item Types:** OneHandedWeapon, TwoHandedWeapon, Bow, Torch, Tool, Shield, Helmet, Chest, Legs, Shoulder, Utility
> 
> **Requirement:**
> ```
> (itemData, magicItem) => !magicItem.HasEffect(MagicEffectType.ReduceWeight)
> ```
> 
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|0|0|0|
> |Rare|0|0|0|
> |Epic|0|0|0|
> |Legendary|0|0|0|

## AddCarryWeight (37)

> **Display Text:** Increase carry weight by +{0}
> 
> **Allowed Item Types:** Helmet, Chest, Legs, Shoulder, Utility
> 
> **Value Per Rarity:**
> 
> |Rarity|Min|Max|Increment|
> |--|--|--|--|
> |Magic|5|10|5|
> |Rare|10|15|5|
> |Epic|15|25|5|
> |Legendary|20|50|5|

# Item Sets

Sets of loot drop data that can be referenced in the loot tables
## Tier0Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Club | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | AxeStone | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Torch | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier0Tools

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Hammer | 1 (50%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Hoe | 1 (50%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier0Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorRagsLegs | 1 (50%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | ArmorRagsChest | 1 (50%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier0Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ShieldWood | 1 (50%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | ShieldWoodTower | 1 (50%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | AxeFlint | 1 (25%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | SpearFlint | 1 (25%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | KnifeFlint | 1 (25%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | Bow | 1 (25%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |


## Tier1Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorLeatherLegs | 1 (25%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | ArmorLeatherChest | 1 (25%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | HelmetLeather | 1 (25%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | CapeDeerHide | 1 (25%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |


## Tier1Tools

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | PickaxeAntler | 1 (100%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |


## Tier1Everything

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Weapons | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier1Armor | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier1Tools | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## TrollArmor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorTrollLeatherLegs | 1 (25%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |
> | ArmorTrollLeatherChest | 1 (25%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |
> | HelmetTrollLeather | 1 (25%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |
> | CapeTrollHide | 1 (25%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |


## Tier2Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | KnifeCopper | 1 (10%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |
> | SledgeStagbreaker | 1 (10%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |
> | SwordBronze | 1 (10%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |
> | AxeBronze | 1 (10%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |
> | MaceBronze | 1 (10%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |
> | AtgeirBronze | 1 (10%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |
> | SpearBronze | 1 (10%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |
> | BowFineWood | 1 (10%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |
> | KnifeChitin | 1 (10%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |
> | SpearChitin | 1 (10%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |


## Tier2Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorBronzeLegs | 1 (33.3%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |
> | ArmorBronzeChest | 1 (33.3%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |
> | HelmetBronze | 1 (33.3%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |


## Tier2Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ShieldBronzeBuckler | 1 (100%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |


## Tier2Tools

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | PickaxeBronze | 1 (50%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |
> | Cultivator | 1 (50%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |


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
> | Battleaxe | 1 (14.3%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | SwordIron | 1 (14.3%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | AxeIron | 1 (14.3%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | SledgeIron | 1 (14.3%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | MaceIron | 1 (14.3%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | AtgeirIron | 1 (14.3%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | SpearElderbark | 1 (14.3%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |


## Tier3Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorIronLegs | 1 (33.3%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | ArmorIronChest | 1 (33.3%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | HelmetIron | 1 (33.3%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |


## Tier3Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ShieldBanded | 1 (50%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | ShieldIronTower | 1 (50%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |


## Tier3Tools

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | PickaxeIron | 1 (100%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |


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
> | SwordSilver | 10 (45.5%) | 10 (9.1%) | 40 (36.4%) | 45 (40.9%) | 15 (13.6%) |
> | SpearWolfFang | 10 (45.5%) | 10 (9.1%) | 40 (36.4%) | 45 (40.9%) | 15 (13.6%) |
> | MaceSilver | 1 (4.5%) | 10 (9.1%) | 40 (36.4%) | 45 (40.9%) | 15 (13.6%) |
> | BowDraugrFang | 1 (4.5%) | 10 (9.1%) | 40 (36.4%) | 45 (40.9%) | 15 (13.6%) |


## Tier4Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorWolfLegs | 1 (25%) | 10 (9.1%) | 40 (36.4%) | 45 (40.9%) | 15 (13.6%) |
> | ArmorWolfChest | 1 (25%) | 10 (9.1%) | 40 (36.4%) | 45 (40.9%) | 15 (13.6%) |
> | HelmetDrake | 1 (25%) | 10 (9.1%) | 40 (36.4%) | 45 (40.9%) | 15 (13.6%) |
> | CapeWolf | 1 (25%) | 10 (9.1%) | 40 (36.4%) | 45 (40.9%) | 15 (13.6%) |


## Tier4Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ShieldSilver | 5 (83.3%) | 10 (9.1%) | 40 (36.4%) | 45 (40.9%) | 15 (13.6%) |
> | ShieldSerpentscale | 1 (16.7%) | 10 (9.1%) | 40 (36.4%) | 45 (40.9%) | 15 (13.6%) |


## Tier4Everything

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Weapons | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier4Armor | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier4Shields | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## Tier5Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | AtgeirBlackmetal | 3 (23.1%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |
> | AxeBlackMetal | 3 (23.1%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |
> | KnifeBlackMetal | 3 (23.1%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |
> | SwordBlackmetal | 3 (23.1%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |
> | MaceNeedle | 1 (7.7%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |


## Tier5Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorPaddedGreaves | 1 (20%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |
> | ArmorPaddedCuirass | 1 (20%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |
> | HelmetPadded | 1 (20%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |
> | CapeLinen | 1 (20%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |
> | CapeLox | 1 (20%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |


## Tier5Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ShieldBlackmetal | 1 (50%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |
> | ShieldBlackmetalTower | 1 (50%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |


## Tier5Everything

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Weapons | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier5Armor | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier5Shields | 1 (33.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


# Loot Tables

A list of every built-in loot table from the mod. The name of the loot table is the object name followed by a number signifying the level of the object.
## Greyling

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 95 (95%) |
> | 1 | 5 (5%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Weapons | 1 (20%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | Tier1Weapons | 1 (20%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | Tier0Shields | 1 (20%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | Tier1Armor | 1 (20%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | Tier0Tools | 1 (20%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Greydwarf.1 | 1 (100%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |

> | Items (lvl 3+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Greydwarf.2 | 1 (100%) | 90 (90%) | 8 (8%) | 2 (2%) | 0 (0%) |


## Greydwarf_Elite

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 40 (40%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 50 (50%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 70 (70%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Weapons | 1 (50%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |
> | Tier0Shields | 1 (50%) | 95 (95%) | 5 (5%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Greydwarf_Elite.1 | 1 (50%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | TrollArmor | 1 (50%) | 80 (80%) | 15 (15%) | 5 (5%) | 0 (0%) |

> | Items (lvl 3+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Greydwarf_Elite.2 | 1 (100%) | 60 (60%) | 30 (30%) | 10 (10%) | 0 (0%) |


## Greydwarf_Shaman

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 40 (40%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 50 (50%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 40 (40%) |
> | 1 | 60 (60%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Armor | 1 (25%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | Tier1Weapons | 1 (25%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | Tier0Shields | 1 (25%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | BowFineWood | 1 (25%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Greydwarf_Shaman.1 | 1 (100%) | 80 (80%) | 15 (15%) | 5 (5%) | 0 (0%) |

> | Items (lvl 3+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Greydwarf_Shaman.2 | 1 (100%) | 60 (60%) | 30 (30%) | 10 (10%) | 0 (0%) |


## Troll

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (59.4%) |
> | 1 | 40 (39.6%) |
> | 5 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (49.5%) |
> | 1 | 50 (49.5%) |
> | 5 | 1 (1%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 40 (39.6%) |
> | 1 | 60 (59.4%) |
> | 5 | 1 (1%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Armor | 4 (28.6%) | 90 (90%) | 10 (10%) | 0 (0%) | 0 (0%) |
> | Tier1Weapons | 4 (28.6%) | 90 (90%) | 10 (10%) | 0 (0%) | 0 (0%) |
> | Tier0Shields | 2 (14.3%) | 90 (90%) | 10 (10%) | 0 (0%) | 0 (0%) |
> | TrollArmor | 2 (14.3%) | 90 (90%) | 10 (10%) | 0 (0%) | 0 (0%) |
> | Tier2Armor | 1 (7.1%) | 90 (90%) | 10 (10%) | 0 (0%) | 0 (0%) |
> | Tier2Weapons | 1 (7.1%) | 90 (90%) | 10 (10%) | 0 (0%) | 0 (0%) |


## Skeleton

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 95 (95%) |
> | 1 | 5 (5%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 85 (85%) |
> | 1 | 15 (15%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 40 (40%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Shields | 10 (33.3%) | 98 (98%) | 2 (2%) | 0 (0%) | 0 (0%) |
> | Tier1Armor | 10 (33.3%) | 98 (98%) | 2 (2%) | 0 (0%) | 0 (0%) |
> | Tier1Weapons | 10 (33.3%) | 98 (98%) | 2 (2%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Skeleton.1 | 1 (7.7%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | Tier2Weapons | 4 (30.8%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | Tier2Armor | 4 (30.8%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | Tier2Shields | 4 (30.8%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |

> | Items (lvl 3+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Skeleton.2 | 1 (100%) | 80 (80%) | 15 (15%) | 5 (5%) | 0 (0%) |


## Ghost

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Armor | 1 (25%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | Tier1Weapons | 3 (75%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |


## Blob

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 9 (9%) |
> | 2 | 1 (1%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 5 (83.3%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Tier3Everything | 1 (16.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |


## BlobElite

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 9 (9%) |
> | 2 | 1 (1%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 3 (75%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Tier3Everything | 1 (25%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |


## Draugr

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 9 (9%) |
> | 2 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 23 (23%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 37 (37%) |
> | 2 | 3 (3%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 4 (80%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Tier3Everything | 1 (20%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |

> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 2 (66.7%) | 64 (64%) | 30 (30%) | 6 (6%) | 0 (0%) |
> | Tier3Everything | 1 (33.3%) | 64 (64%) | 30 (30%) | 6 (6%) | 0 (0%) |

> | Items (lvl 3+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (50%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | Tier3Everything | 1 (50%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |


## Draugr_Elite

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 28 (28%) |
> | 2 | 2 (2%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 37 (37%) |
> | 2 | 3 (3%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 46 (46%) |
> | 2 | 4 (4%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 2 (66.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Tier3Everything | 1 (33.3%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |

> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (50%) | 64 (64%) | 30 (30%) | 6 (6%) | 0 (0%) |
> | Tier3Everything | 1 (50%) | 64 (64%) | 30 (30%) | 6 (6%) | 0 (0%) |

> | Items (lvl 3+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (50%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | Tier3Everything | 1 (50%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |


## Leech

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 9 (9%) |
> | 2 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 18 (18%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 27 (27%) |
> | 2 | 3 (3%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 2 (66.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Tier3Everything | 1 (33.3%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |


## Surtling

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 93 (93%) |
> | 1 | 6 (6%) |
> | 2 | 1 (1%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 2 (66.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Tier3Everything | 1 (33.3%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |


## Wraith

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 29 (29%) |
> | 2 | 1 (1%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 3 (60%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Tier3Everything | 2 (40%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |


## Wolf

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 9 (9%) |
> | 2 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 23 (23%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 37 (37%) |
> | 2 | 3 (3%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 4 (80%) | 64 (64%) | 30 (30%) | 6 (6%) | 0 (0%) |
> | Tier4Everything | 1 (20%) | 64 (64%) | 30 (30%) | 6 (6%) | 0 (0%) |

> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 2 (66.7%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | Tier4Everything | 1 (33.3%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |

> | Items (lvl 3+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (50%) | 20 (20%) | 55 (55%) | 23 (23%) | 2 (2%) |
> | Tier4Everything | 1 (50%) | 20 (20%) | 55 (55%) | 23 (23%) | 2 (2%) |


## Hatchling

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 9 (9%) |
> | 2 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 23 (23%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 37 (37%) |
> | 2 | 3 (3%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 4 (80%) | 64 (64%) | 30 (30%) | 6 (6%) | 0 (0%) |
> | Tier4Everything | 1 (20%) | 64 (64%) | 30 (30%) | 6 (6%) | 0 (0%) |

> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 2 (66.7%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | Tier4Everything | 1 (33.3%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |

> | Items (lvl 3+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (50%) | 20 (20%) | 55 (55%) | 23 (23%) | 2 (2%) |
> | Tier4Everything | 1 (50%) | 20 (20%) | 55 (55%) | 23 (23%) | 2 (2%) |


## StoneGolem

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 9 (9%) |
> | 2 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 23 (23%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 37 (37%) |
> | 2 | 3 (3%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 4 (80%) | 64 (64%) | 30 (30%) | 6 (6%) | 0 (0%) |
> | Tier4Everything | 1 (20%) | 64 (64%) | 30 (30%) | 6 (6%) | 0 (0%) |

> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 2 (66.7%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | Tier4Everything | 1 (33.3%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |

> | Items (lvl 3+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (50%) | 20 (20%) | 55 (55%) | 23 (23%) | 2 (2%) |
> | Tier4Everything | 1 (50%) | 20 (20%) | 55 (55%) | 23 (23%) | 2 (2%) |


## Fenring

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 9 (9%) |
> | 2 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 23 (23%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 37 (37%) |
> | 2 | 3 (3%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 4 (80%) | 64 (64%) | 30 (30%) | 6 (6%) | 0 (0%) |
> | Tier4Everything | 1 (20%) | 64 (64%) | 30 (30%) | 6 (6%) | 0 (0%) |

> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 2 (66.7%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |
> | Tier4Everything | 1 (33.3%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) |

> | Items (lvl 3+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (50%) | 20 (20%) | 55 (55%) | 23 (23%) | 2 (2%) |
> | Tier4Everything | 1 (50%) | 20 (20%) | 55 (55%) | 23 (23%) | 2 (2%) |


## Deathsquito

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 9 (9%) |
> | 2 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 23 (23%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 37 (37%) |
> | 2 | 3 (3%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 4 (80%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |
> | Tier5Everything | 1 (20%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |

> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 2 (66.7%) | 0 (0%) | 45 (45%) | 45 (45%) | 10 (10%) |
> | Tier5Everything | 1 (33.3%) | 0 (0%) | 45 (45%) | 45 (45%) | 10 (10%) |

> | Items (lvl 3+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (50%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |
> | Tier5Everything | 1 (50%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |


## Lox

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 9 (9%) |
> | 2 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 23 (23%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 37 (37%) |
> | 2 | 3 (3%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 4 (80%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |
> | Tier5Everything | 1 (20%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |

> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 2 (66.7%) | 0 (0%) | 45 (45%) | 45 (45%) | 10 (10%) |
> | Tier5Everything | 1 (33.3%) | 0 (0%) | 45 (45%) | 45 (45%) | 10 (10%) |

> | Items (lvl 3+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (50%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |
> | Tier5Everything | 1 (50%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |


## Goblin

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 9 (9%) |
> | 2 | 1 (1%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 23 (23%) |
> | 2 | 2 (2%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 37 (37%) |
> | 2 | 3 (3%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 4 (80%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |
> | Tier5Everything | 1 (20%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |

> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 2 (66.7%) | 0 (0%) | 45 (45%) | 45 (45%) | 10 (10%) |
> | Tier5Everything | 1 (33.3%) | 0 (0%) | 45 (45%) | 45 (45%) | 10 (10%) |

> | Items (lvl 3+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (50%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |
> | Tier5Everything | 1 (50%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |


## GoblinBrute

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 40 (40%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 50 (50%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 70 (70%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 4 (80%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |
> | Tier5Everything | 1 (20%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |

> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 2 (66.7%) | 0 (0%) | 45 (45%) | 45 (45%) | 10 (10%) |
> | Tier5Everything | 1 (33.3%) | 0 (0%) | 45 (45%) | 45 (45%) | 10 (10%) |

> | Items (lvl 3+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (50%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |
> | Tier5Everything | 1 (50%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |


## GoblinShaman

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 40 (40%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 50 (50%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 70 (70%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 4 (80%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |
> | Tier5Everything | 1 (20%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |

> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 2 (66.7%) | 0 (0%) | 45 (45%) | 45 (45%) | 10 (10%) |
> | Tier5Everything | 1 (33.3%) | 0 (0%) | 45 (45%) | 45 (45%) | 10 (10%) |

> | Items (lvl 3+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (50%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |
> | Tier5Everything | 1 (50%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |


## Serpent

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 50 (50%) |
> | 2 | 15 (15%) |
> | 3 | 5 (5%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 4 (80%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |
> | Tier4Everything | 1 (20%) | 0 (0%) | 55 (55%) | 40 (40%) | 5 (5%) |

> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 2 (66.7%) | 0 (0%) | 45 (45%) | 45 (45%) | 10 (10%) |
> | Tier4Everything | 1 (33.3%) | 0 (0%) | 45 (45%) | 45 (45%) | 10 (10%) |

> | Items (lvl 3+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (50%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |
> | Tier4Everything | 1 (50%) | 0 (0%) | 30 (30%) | 55 (55%) | 15 (15%) |


## Eikthyr

> | Drops | Weight (Chance) |
> | -- | -- |
> | 1 | 70 (70%) |
> | 2 | 30 (30%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Weapons | 2 (25%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | Tier0Shields | 1 (12.5%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | Tier1Armor | 2 (25%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | Tier1Tools | 2 (25%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | SledgeStagbreaker | 1 (12.5%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |


## gd_king

> | Drops | Weight (Chance) |
> | -- | -- |
> | 1 | 35 (35%) |
> | 2 | 55 (55%) |
> | 3 | 10 (10%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 1 (100%) | 60 (60%) | 30 (30%) | 9 (9%) | 1 (1%) |


## Bonemass

> | Drops | Weight (Chance) |
> | -- | -- |
> | 2 | 60 (60%) |
> | 3 | 25 (25%) |
> | 4 | 15 (15%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 1 (100%) | 19 (19%) | 65 (65%) | 14 (14%) | 2 (2%) |


## Dragon

> | Drops | Weight (Chance) |
> | -- | -- |
> | 2 | 40 (40%) |
> | 3 | 40 (40%) |
> | 4 | 20 (20%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 1 (100%) | 0 (0%) | 40 (40%) | 50 (50%) | 10 (10%) |


## GoblinKing

> | Drops | Weight (Chance) |
> | -- | -- |
> | 2 | 20 (20%) |
> | 3 | 60 (60%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier5Everything | 1 (100%) | 0 (0%) | 10 (10%) | 70 (70%) | 20 (20%) |


## TreasureChest_meadows

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 78 (78%) |
> | 1 | 20 (20%) |
> | 2 | 2 (2%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 4 (80%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |
> | Tier1Everything | 1 (20%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |


## TreasureChest_blackforest

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Shields | 1 (33.3%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | Tier1Everything | 2 (66.7%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |


## TreasureChest_forestcrypt

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 68 (68%) |
> | 1 | 20 (20%) |
> | 2 | 10 (10%) |
> | 3 | 2 (2%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 2 (66.7%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | Tier2Everything | 1 (33.3%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |


## TreasureChest_fCrypt

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 68 (68%) |
> | 1 | 20 (20%) |
> | 2 | 10 (10%) |
> | 3 | 2 (2%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | TreasureChest_forestcrypt.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## TreasureChest_trollcave

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 48 (43.6%) |
> | 1 | 40 (36.4%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier1Everything | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | Tier2Everything | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |


## shipwreck_karve_chest

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 48 (43.6%) |
> | 1 | 40 (36.4%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | Tier1Everything | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |


## TreasureChest_meadows_buried

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 48 (43.6%) |
> | 1 | 40 (36.4%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier0Everything | 3 (60%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | Tier1Everything | 2 (40%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |


## TreasureChest_sunkencrypt

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 3 (75%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Tier3Everything | 1 (25%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |


## TreasureChest_swamp

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier2Everything | 5 (83.3%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier3Everything | 1 (16.7%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## TreasureChest_mountains

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier3Everything | 4 (80%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier4Everything | 1 (20%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## TreasureChest_plains_stone

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | TreasureChest_forestcrypt.1 | 1 (100%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


## TreasureChest_heath

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Tier4Everything | 3 (75%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Tier5Everything | 1 (25%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


