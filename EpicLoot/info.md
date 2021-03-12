# EpicLoot Data v0.5.8

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

# Loot Tables

A list of every built-in loot table from the mod. The name of the loot table is the object name followed by a number signifying the level of the object.
## Greyling

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 95 (95%) |
> | 1 | 5 (5%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Club | 1 (16.7%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | AxeStone | 1 (16.7%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | Torch | 1 (16.7%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | ArmorRagsLegs | 1 (16.7%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | ArmorRagsChest | 1 (16.7%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |
> | ShieldWood | 1 (16.7%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |


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

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Club | 1 (8.3%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | AxeStone | 1 (8.3%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | AxeFlint | 1 (8.3%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | SpearFlint | 1 (8.3%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | KnifeFlint | 1 (8.3%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | ShieldWood | 1 (8.3%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | ArmorLeatherLegs | 1 (8.3%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | ArmorLeatherChest | 1 (8.3%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | HelmetLeather | 1 (8.3%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | CapeDeerHide | 1 (8.3%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | Hammer | 1 (8.3%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | Bow | 1 (8.3%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |


## Greydwarf_Elite

> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |

> | Drops (lvl 2) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 40 (40%) |

> | Drops (lvl 3) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 50 (50%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | AxeFlint | 1 (20%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | KnifeFlint | 1 (20%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | ShieldWood | 1 (20%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | SpearFlint | 1 (20%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | Bow | 1 (20%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |

> | Items (lvl 3+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorTrollLeatherLegs | 1 (25%) | 60 (60%) | 30 (30%) | 10 (10%) | 0 (0%) |
> | ArmorTrollLeatherChest | 1 (25%) | 60 (60%) | 30 (30%) | 10 (10%) | 0 (0%) |
> | HelmetTrollLeather | 1 (25%) | 60 (60%) | 30 (30%) | 10 (10%) | 0 (0%) |
> | CapeTrollHide | 1 (25%) | 60 (60%) | 30 (30%) | 10 (10%) | 0 (0%) |


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

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorLeatherLegs | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | ArmorLeatherChest | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | HelmetLeather | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | CapeDeerHide | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | AxeFlint | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | KnifeFlint | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | ShieldWood | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | SpearFlint | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | BowFineWood | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |


## Troll

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

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorLeatherLegs | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | ArmorLeatherChest | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | HelmetLeather | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | CapeDeerHide | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | AxeFlint | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | KnifeFlint | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | ShieldWood | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | SpearFlint | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |
> | BowFineWood | 1 (11.1%) | 99 (99%) | 1 (1%) | 0 (0%) | 0 (0%) |


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
> | 0 | 75 (75%) |
> | 1 | 25 (25%) |

> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ShieldWood | 10 (20%) | 98 (98%) | 2 (2%) | 0 (0%) | 0 (0%) |
> | ArmorLeatherLegs | 10 (20%) | 98 (98%) | 2 (2%) | 0 (0%) | 0 (0%) |
> | ArmorLeatherChest | 10 (20%) | 98 (98%) | 2 (2%) | 0 (0%) | 0 (0%) |
> | HelmetLeather | 10 (20%) | 98 (98%) | 2 (2%) | 0 (0%) | 0 (0%) |
> | Bow | 10 (20%) | 98 (98%) | 2 (2%) | 0 (0%) | 0 (0%) |

> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | SwordBronze | 1 (14.3%) | 98 (98%) | 2 (2%) | 0 (0%) | 0 (0%) |
> | AxeBronze | 1 (14.3%) | 98 (98%) | 2 (2%) | 0 (0%) | 0 (0%) |
> | MaceBronze | 1 (14.3%) | 98 (98%) | 2 (2%) | 0 (0%) | 0 (0%) |
> | AtgeirBronze | 1 (14.3%) | 98 (98%) | 2 (2%) | 0 (0%) | 0 (0%) |
> | SpearBronze | 1 (14.3%) | 98 (98%) | 2 (2%) | 0 (0%) | 0 (0%) |
> | ShieldBronzeBuckler | 1 (14.3%) | 98 (98%) | 2 (2%) | 0 (0%) | 0 (0%) |
> | BowFineWood | 1 (14.3%) | 98 (98%) | 2 (2%) | 0 (0%) | 0 (0%) |


## Ghost

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorLeatherLegs | 1 (9.1%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | ArmorLeatherChest | 1 (9.1%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | HelmetLeather | 1 (9.1%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | CapeDeerHide | 1 (9.1%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | AxeFlint | 1 (9.1%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | KnifeFlint | 1 (9.1%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | ShieldWood | 1 (9.1%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | SpearFlint | 1 (9.1%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | Bow | 3 (27.3%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |


## Blob

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 9 (9%) |
> | 2 | 1 (1%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorBronzeLegs | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | ArmorBronzeChest | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | HelmetBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | SwordBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | AxeBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | MaceBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | AtgeirBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | SpearBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | ShieldBronzeBuckler | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |


## BlobElite

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 9 (9%) |
> | 2 | 1 (1%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorBronzeLegs | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | ArmorBronzeChest | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | HelmetBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | SwordBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | AxeBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | MaceBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | AtgeirBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | SpearBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | ShieldBronzeBuckler | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |


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

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorBronzeLegs | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | ArmorBronzeChest | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | HelmetBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | SwordBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | AxeBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | MaceBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | AtgeirBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | SpearBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | ShieldBronzeBuckler | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | BowFineWood | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Hammer | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Cultivator | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Hoe | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |


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

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorBronzeLegs | 1 (7.7%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | ArmorBronzeChest | 1 (7.7%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | HelmetBronze | 1 (7.7%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | SwordBronze | 1 (7.7%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | AxeBronze | 1 (7.7%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | MaceBronze | 1 (7.7%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | AtgeirBronze | 1 (7.7%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | SpearBronze | 1 (7.7%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | ShieldBronzeBuckler | 1 (7.7%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | BowFineWood | 1 (7.7%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | Hammer | 1 (7.7%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | Cultivator | 1 (7.7%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | Hoe | 1 (7.7%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |


## Leech

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 93 (93%) |
> | 1 | 6 (6%) |
> | 2 | 1 (1%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorBronzeLegs | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | ArmorBronzeChest | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | HelmetBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | SwordBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | AxeBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | MaceBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | AtgeirBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | SpearBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | ShieldBronzeBuckler | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |


## Surtling

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 93 (93%) |
> | 1 | 6 (6%) |
> | 2 | 1 (1%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorBronzeLegs | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | ArmorBronzeChest | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | HelmetBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | SwordBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | AxeBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | MaceBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | AtgeirBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | SpearBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | ShieldBronzeBuckler | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |


## Wraith

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 29 (29%) |
> | 2 | 1 (1%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorBronzeLegs | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | ArmorBronzeChest | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | HelmetBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | SwordBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | AxeBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | MaceBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | AtgeirBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | SpearBronze | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | ShieldBronzeBuckler | 1 (11.1%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |


## Eikthyr

> | Drops | Weight (Chance) |
> | -- | -- |
> | 1 | 70 (70%) |
> | 2 | 30 (30%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | SledgeStagbreaker | 1 (8.3%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | SpearFlint | 2 (16.7%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | KnifeFlint | 2 (16.7%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | Bow | 2 (16.7%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | ShieldWood | 2 (16.7%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | ShieldWoodTower | 2 (16.7%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |
> | PickaxeAntler | 1 (8.3%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 (0%) |


## gd_king

> | Drops | Weight (Chance) |
> | -- | -- |
> | 1 | 50 (50%) |
> | 2 | 40 (40%) |
> | 3 | 10 (10%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | KnifeCopper | 2 (11.8%) | 60 (60%) | 30 (30%) | 9 (9%) | 1 (1%) |
> | AtgeirBronze | 1 (5.9%) | 60 (60%) | 30 (30%) | 9 (9%) | 1 (1%) |
> | AxeBronze | 1 (5.9%) | 60 (60%) | 30 (30%) | 9 (9%) | 1 (1%) |
> | MaceBronze | 1 (5.9%) | 60 (60%) | 30 (30%) | 9 (9%) | 1 (1%) |
> | SpearBronze | 1 (5.9%) | 60 (60%) | 30 (30%) | 9 (9%) | 1 (1%) |
> | SwordBronze | 1 (5.9%) | 60 (60%) | 30 (30%) | 9 (9%) | 1 (1%) |
> | ShieldBronzeBuckler | 1 (5.9%) | 60 (60%) | 30 (30%) | 9 (9%) | 1 (1%) |
> | BowFineWood | 2 (11.8%) | 60 (60%) | 30 (30%) | 9 (9%) | 1 (1%) |
> | ArmorTrollLeatherLegs | 1 (5.9%) | 60 (60%) | 30 (30%) | 9 (9%) | 1 (1%) |
> | ArmorTrollLeatherChest | 1 (5.9%) | 60 (60%) | 30 (30%) | 9 (9%) | 1 (1%) |
> | HelmetTrollLeather | 1 (5.9%) | 60 (60%) | 30 (30%) | 9 (9%) | 1 (1%) |
> | CapeTrollHide | 1 (5.9%) | 60 (60%) | 30 (30%) | 9 (9%) | 1 (1%) |
> | ArmorBronzeLegs | 1 (5.9%) | 60 (60%) | 30 (30%) | 9 (9%) | 1 (1%) |
> | ArmorBronzeChest | 1 (5.9%) | 60 (60%) | 30 (30%) | 9 (9%) | 1 (1%) |
> | HelmetBronze | 1 (5.9%) | 60 (60%) | 30 (30%) | 9 (9%) | 1 (1%) |


## Bonemass

> | Drops | Weight (Chance) |
> | -- | -- |
> | 1 | 20 (20%) |
> | 2 | 60 (60%) |
> | 3 | 15 (15%) |
> | 4 | 5 (5%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Battleaxe | 1 (9.1%) | 19 (19%) | 65 (65%) | 14 (14%) | 2 (2%) |
> | SwordIron | 1 (9.1%) | 19 (19%) | 65 (65%) | 14 (14%) | 2 (2%) |
> | AxeIron | 1 (9.1%) | 19 (19%) | 65 (65%) | 14 (14%) | 2 (2%) |
> | SledgeIron | 1 (9.1%) | 19 (19%) | 65 (65%) | 14 (14%) | 2 (2%) |
> | MaceIron | 1 (9.1%) | 19 (19%) | 65 (65%) | 14 (14%) | 2 (2%) |
> | AtgeirIron | 1 (9.1%) | 19 (19%) | 65 (65%) | 14 (14%) | 2 (2%) |
> | ArmorIronLegs | 1 (9.1%) | 19 (19%) | 65 (65%) | 14 (14%) | 2 (2%) |
> | ArmorIronChest | 1 (9.1%) | 19 (19%) | 65 (65%) | 14 (14%) | 2 (2%) |
> | HelmetIron | 1 (9.1%) | 19 (19%) | 65 (65%) | 14 (14%) | 2 (2%) |
> | ShieldBanded | 1 (9.1%) | 19 (19%) | 65 (65%) | 14 (14%) | 2 (2%) |
> | ShieldIronTower | 1 (9.1%) | 19 (19%) | 65 (65%) | 14 (14%) | 2 (2%) |


## TreasureChest_meadows

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 78 (78%) |
> | 1 | 20 (20%) |
> | 2 | 2 (2%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Club | 1 (12.5%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |
> | AxeStone | 1 (12.5%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |
> | Torch | 1 (12.5%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |
> | ArmorRagsLegs | 1 (12.5%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |
> | ArmorRagsChest | 1 (12.5%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |
> | ShieldWood | 1 (12.5%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |
> | Hammer | 1 (12.5%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |
> | Hoe | 1 (12.5%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 (0%) |


## TreasureChest_blackforest

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Club | 1 (8.3%) | 96 (96%) | 3 (3%) | 1 (1%) | 0 (0%) |
> | AxeStone | 1 (8.3%) | 96 (96%) | 3 (3%) | 1 (1%) | 0 (0%) |
> | AxeFlint | 1 (8.3%) | 96 (96%) | 3 (3%) | 1 (1%) | 0 (0%) |
> | SpearFlint | 1 (8.3%) | 96 (96%) | 3 (3%) | 1 (1%) | 0 (0%) |
> | KnifeFlint | 1 (8.3%) | 96 (96%) | 3 (3%) | 1 (1%) | 0 (0%) |
> | ShieldWood | 1 (8.3%) | 96 (96%) | 3 (3%) | 1 (1%) | 0 (0%) |
> | ArmorLeatherLegs | 1 (8.3%) | 96 (96%) | 3 (3%) | 1 (1%) | 0 (0%) |
> | ArmorLeatherChest | 1 (8.3%) | 96 (96%) | 3 (3%) | 1 (1%) | 0 (0%) |
> | HelmetLeather | 1 (8.3%) | 96 (96%) | 3 (3%) | 1 (1%) | 0 (0%) |
> | CapeDeerHide | 1 (8.3%) | 96 (96%) | 3 (3%) | 1 (1%) | 0 (0%) |
> | Hammer | 1 (8.3%) | 96 (96%) | 3 (3%) | 1 (1%) | 0 (0%) |
> | Bow | 1 (8.3%) | 96 (96%) | 3 (3%) | 1 (1%) | 0 (0%) |


## TreasureChest_forestcrypt

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 68 (68%) |
> | 1 | 20 (20%) |
> | 2 | 10 (10%) |
> | 3 | 2 (2%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | SwordBronze | 1 (9.1%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | AxeBronze | 1 (9.1%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | MaceBronze | 1 (9.1%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | AtgeirBronze | 1 (9.1%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | ShieldBronzeBuckler | 1 (9.1%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | HelmetLeather | 2 (18.2%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | ArmorLeatherLegs | 2 (18.2%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | ArmorLeatherChest | 2 (18.2%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |


## TreasureChest_trollcave

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | SwordBronze | 1 (9.1%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | AxeBronze | 1 (9.1%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | MaceBronze | 1 (9.1%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | AtgeirBronze | 1 (9.1%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | ShieldBronzeBuckler | 1 (9.1%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | HelmetLeather | 2 (18.2%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | ArmorLeatherLegs | 2 (18.2%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |
> | ArmorLeatherChest | 2 (18.2%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 (0%) |


## TreasureChest_meadows_buried

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | Battleaxe | 1 (6.3%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | SwordIron | 1 (6.3%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | AxeIron | 1 (6.3%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | SledgeIron | 1 (6.3%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | MaceIron | 1 (6.3%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | AtgeirIron | 1 (6.3%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | ArmorIronLegs | 2 (12.5%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | ArmorIronChest | 2 (12.5%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | HelmetIron | 2 (12.5%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | ShieldBanded | 2 (12.5%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |
> | ShieldIronTower | 2 (12.5%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 (0%) |


## TreasureChest_sunkencrypt

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorBronzeLegs | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | ArmorBronzeChest | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | HelmetBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | SwordBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | AxeBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | MaceBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | AtgeirBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | SpearBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | ShieldBronzeBuckler | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | BowFineWood | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Hammer | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Cultivator | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Hoe | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |


## TreasureChest_swamp

> | Drops | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |
> | -- | -- | -- | -- | -- | -- |
> | ArmorBronzeLegs | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | ArmorBronzeChest | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | HelmetBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | SwordBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | AxeBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | MaceBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | AtgeirBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | SpearBronze | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | ShieldBronzeBuckler | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | BowFineWood | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Hammer | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Cultivator | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |
> | Hoe | 1 (7.7%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 (0%) |


