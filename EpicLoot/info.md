# EpicLoot Data v0.16.0

*Author: RandyKnapp*
*Source: [Github](https://github.com/RandyKnapp/ValheimMods/tree/main/EpicLoot)*

# Magic Effect Count Weights Per Rarity

Each time a **MagicItem** is rolled a number of **MagicItemEffects** are added based on its **Rarity**. The percent chance to roll each number of effects is found on the following table, from the `MagicEffectsCount` block of `loottables.json`. An item that runs out of effects it can take gets fewer. Enchanting at a table with the Enchant upgrade moves weight from the lowest count to the two highest.

The raw weight value is shown first, followed by the calculated percentage chance in parentheses.

|Rarity|1|2|3|4|5|6|7|8|
|--|--|--|--|--|--|--|--|--|
|Magic|80 (80%)|18 (18%)|2 (2%)| | | | | |
|Rare| |80 (80%)|18 (18%)|2 (2%)| | | | |
|Epic| | |80 (80%)|18 (18%)|2 (2%)| | | |
|Legendary| | | |80 (80%)|18 (18%)|2 (2%)| | |
|Mythic| | | | |80 (80%)|18 (18%)|2 (2%)| |
|Ancient| | | | | |80 (80%)|18 (18%)|2 (2%)|

# MagicItemEffect List

The following lists all 183 built-in **MagicItemEffects**, in the order of `magiceffects.json`. Each Balance Template has its own copy, under `config/overhauls/minimal`, `balanced` and `legendary`, with the same effects; the server's `Balance Template` setting picks one, and Balanced is the default. Requirements are Balanced's, with a line for a template whose requirements differ.

MagicItemEffects are parsed and added to `MagicItemEffectDefinitions` each time the file loads. EpicLoot uses a string for the types of magic effects, so you can add your own new types using your own identifiers: through `API.AddMagicEffect` (see [API.md](docs/API.md)), a patch in `<BepInEx>/config/EpicLoot/patches/`, or by listening to the event `MagicItemEffectDefinitions.OnSetupMagicItemEffectDefinitions` and adding instances of `MagicItemEffectDefinition`.

  * **Display Text:** This text appears in the tooltip for the magic item, with {0:?} replaced with the rolled value for the effect, formatted using the shown C# string format. Higher placeholders such as {1:0.#} are other numbers the effect fills in.
  * **Description:** The longer explanation shown in the effect details (hold Shift over the item) and the Compendium; **X** stands for the rolled value.
  * **Requirements:** A set of requirements.
    * **Flags:** A set of predefined flags to check certain weapon properties. The list of flags is: `NoRoll, ExclusiveSelf, ItemHasPhysicalDamage, ItemHasElementalDamage, ItemHasNonSpiritElementalDamage, ItemHasChopDamage, ItemHasBluntDamage, ItemHasSlashDamage, ItemHasPierceDamage, ItemUsesDurability, ItemHasNegativeMovementSpeedModifier, ItemHasBlockPower, ItemHasParryPower, ItemHasNoParryPower, ItemHasArmor, ItemHasBackstabBonus, ItemUsesStaminaOnAttack, ItemUsesEitrOnAttack, ItemUsesHealthOnAttack, ItemUsesDrawStaminaOnAttack, ItemGivesAdrenaline, ItemHasAdrenaline`. `NoRoll` is shown as *Can't be rolled*, and `ExclusiveSelf` (the effect can be on an item only once) is on unless an entry shows `!ExclusiveSelf`. The other flags check a property of the item: `Flag` requires it and `!Flag` requires its absence.
    * **ExclusiveEffectTypes:** This effect may not be rolled on an item that already has one of these effects. It works both ways: an effect listed here can't join an item that has this one either.
    * **MustHaveEffectTypes:** This effect may only be rolled on an item that already has every one of these effects. A spear counts as having `Throwable`. When a rune or shardstone with this effect is socketed, an effect from another socket on the item counts too.
    * **AllowedItemTypes:** This effect may only be rolled on items of one of the types in this list. When this list is empty any item type is allowed. Options are the item types `Helmet, Chest, Legs, Shoulder, Utility, Trinket, Bow, OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Shield, Tool, Torch`, `Staff` (any ElementalMagic or BloodMagic weapon), and the item's type in `iteminfo.json`: `Swords, Axes, TwoHandAxes, Knives, Fists, Staffs, Clubs, Sledges, Polearms, Spears, Pickaxes, Bows, Bucklers, RoundShields, TowerShields, ChestArmor, LegsArmor, HeadArmor, ShouldersArmor, Torches, Tools, Utility, Trinket`
    * **ExcludedItemTypes:** This effect may only be rolled on items that are not one of the types on this list.
    * **AllowedRarities:** This effect may only be rolled on an item of one of these rarities. Options are: `Magic, Rare, Epic, Legendary, Mythic, Ancient`
    * **ExcludedRarities:** This effect may only be rolled on an item that is not of one of these rarities.
    * **AllowedSkillTypes:** This effect may only be rolled on an item that uses one of these skill types. Options are: `Swords, Knives, Clubs, Polearms, Spears, Blocking, Axes, Bows, ElementalMagic, BloodMagic, Unarmed, Pickaxes, Crossbows, Fishing, Ride`
    * **ExcludedSkillTypes:** This effect may only be rolled on an item that does not use one of these skill types.
    * **AllowedItemNames:** This effect may only be rolled on an item with one of these names. Use the unlocalized shared name, i.e.: `$item_sword_iron`, or the prefab name, i.e.: `SwordIron`
    * **ExcludedItemNames:** This effect may only be rolled on an item that does not have one of these names.
    * **AllowedStaffRoles:** On a staff (ElementalMagic or BloodMagic), this effect may only be rolled if the staff fills one of these roles; other items ignore it. Options are: `Damage` (the staff or a projectile it spawns deals damage), `Summon` (its attack summons creatures), `Support` (neither, e.g. Staff of Protection). A name that is not one of these is skipped with a warning in the log.
    * **CustomFlags:** A set of arbitrary strings for other mods to read. EpicLoot does not check them.
    * **ExternalRequirements:** Requirement keys registered by external plugins through `API.RegisterMagicEffectRequirement`. Every listed key must be registered and must return true for the effect to roll.
  * **Value Per Rarity:** One row per rarity and one column per Balance Template, each `min–max (increment)`. The value is rolled using a linear distribution between Min and Max and divisible by the Increment. A single number is a fixed value, and — means no value at that rarity in that template: an effect rolled there gets a value of 1. An effect without this table has no value; it is simply on.
  * **Selection Weight:** How likely this effect is to be picked among the effects an item can roll (1 when not set).
  * **Ability:** The ability from `abilities.json` the effect grants.
  * **Config:** Per-effect tunables such as cooldowns, radii and caps, with the label the tooltip shows for each (hold Shift over the item).
  * ***Can't be augmented, disenchanted or extracted to a rune:*** The effect is left out of those enchanting table actions.

## DvergerCirclet

> **Display Text:** Perpetual Lightsource
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## Megingjord

> **Display Text:** Carry Weight +150
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## Wishbone

> **Display Text:** Secret Finding
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## Andvaranaut

> **Display Text:** Haldor's Treasure Finder
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## SkuldsThread

> **Display Text:** Skuld's Bounty Tether
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## ChainLightning

> **Display Text:** Chain Lightning +{0}%
>
> **Description:** 15% chance on hit to spawn chain lightning that deals +{0}% damage.
>
> **Requirements:**
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bows`
> * **ExcludedItemNames:** `$item_graplinghook`
> * **AllowedStaffRoles:** `Damage`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|5–15 (1)|5–15 (1)|5–15 (1)|
> |Rare|15–25 (1)|15–25 (1)|15–25 (1)|
> |Epic|25–35 (1)|25–35 (1)|25–35 (1)|
> |Legendary|35–45 (1)|35–45 (1)|35–45 (1)|
> |Mythic|45–55 (1)|45–55 (1)|45–55 (1)|
> |Ancient|55–65 (1)|55–65 (1)|55–65 (1)|
>
> **Selection Weight:** 1

## DodgeBuff

> **Display Text:** Dodge Damage Buff +{0}%
>
> **Description:** Dodging an enemy melee attack grants a buff increasing damage for 10 seconds. Cannot be refreshed until the buff expires or is removed.
>
> **Requirements:**
> * **AllowedItemTypes:** `Legs, Utility, Trinket`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|5–10 (1)|5–10 (1)|5–10 (1)|
> |Rare|10–15 (1)|10–15 (1)|10–15 (1)|
> |Epic|15–20 (1)|15–20 (1)|15–20 (1)|
> |Legendary|20–25 (1)|20–25 (1)|20–25 (1)|
> |Mythic|25–30 (1)|25–30 (1)|25–30 (1)|
> |Ancient|30–35 (1)|30–35 (1)|30–35 (1)|
>
> **Selection Weight:** 1

## ModifyDamage

> **Display Text:** All Damage +{0:0.#}%
>
> **Description:** Increase the total damage done by the magic weapon by +**X**%, including damage from other enchantments.
>
> **Requirements:**
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff`
> * **ExcludedItemNames:** `$item_graplinghook`
> * **AllowedStaffRoles:** `Damage`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|0.5–1 (0.1)|0.5–1 (0.1)|0.8–1.5 (0.1)|
> |Rare|1–2 (0.1)|1–2 (0.1)|1.5–3 (0.1)|
> |Epic|1–2 (0.1)|2–3 (0.1)|3–4.5 (0.1)|
> |Legendary|1–3 (0.1)|3–4 (0.1)|4.5–6 (0.1)|
> |Mythic|2–3 (0.1)|4–5 (0.1)|6–7.5 (0.1)|
> |Ancient|3|5–6 (0.1)|7.5–9 (0.1)|
>
> **Selection Weight:** 1

## ModifyPhysicalDamage

> **Display Text:** Physical Damage Dealt +{0:0.#}%
>
> **Description:** Increase the physical damage (blunt, slash, pierce, chop, pickaxe) done by the magic weapon by +**X**%, including damage from other enchantments.
>
> **Requirements:**
> * **Flags:** `ItemHasPhysicalDamage`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff`
> * **ExcludedItemNames:** `$item_fishingrod, $item_graplinghook`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|2–4 (0.5)|2–4 (1)|
> |Rare|1–3 (0.5)|3–6 (0.5)|3–8 (1)|
> |Epic|1–4 (0.5)|4–8 (0.5)|4–12 (1)|
> |Legendary|1–5 (0.5)|5–10 (0.5)|5–16 (1)|
> |Mythic|1–6 (0.5)|6–12 (0.5)|6–20 (1)|
> |Ancient|1–7 (0.5)|7–14 (0.5)|8–24 (1)|
>
> **Selection Weight:** 10

## ModifyElementalDamage

> **Display Text:** Elemental Damage Dealt +{0:0.#}%
>
> **Description:** Increase the elemental damage (fire, frost, lightning, poison, spirit) done by the magic weapon by +**X**%, including damage from other enchantments.
>
> **Requirements:**
> * **Flags:** `ItemHasElementalDamage`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Torch, TwoHandedWeaponLeft, Staff`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_fishingrod`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|2–4 (0.5)|2–4 (1)|
> |Rare|1–3 (0.5)|3–6 (0.5)|3–8 (1)|
> |Epic|1–4 (0.5)|4–8 (0.5)|4–12 (1)|
> |Legendary|1–5 (0.5)|5–10 (0.5)|5–16 (1)|
> |Mythic|1–6 (0.5)|6–12 (0.5)|6–20 (1)|
> |Ancient|1–7 (0.5)|7–14 (0.5)|8–24 (1)|
>
> **Selection Weight:** 10

## ModifyDurability

> **Display Text:** Durability +{0:0.#}%
>
> **Description:** Increase the maximum durability of the magic item by +**X**%.
>
> **Requirements:**
> * **Flags:** `ItemUsesDurability`
> * **ExclusiveEffectTypes:** `Indestructible`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder, Staff`
> * **AllowedRarities:** `Magic, Rare, Epic, Legendary, Mythic, Ancient`
> * **ExcludedItemNames:** `$item_fishingrod`
> * *Legendary:* no AllowedRarities.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|5–30 (5)|10–80 (5)|25–120 (5)|
> |Rare|20–40 (5)|20–120 (5)|50–180 (5)|
> |Epic|25–50 (5)|30–140 (5)|50–210 (5)|
> |Legendary|30–60 (5)|40–160 (5)|60–240 (5)|
> |Mythic|40–70 (5)|50–180 (5)|75–270 (5)|
> |Ancient|50–80 (5)|60–200 (5)|90–300 (5)|
>
> **Selection Weight:** 10

## ReduceWeight

> **Display Text:** Item Weight -{0:0.#}%
>
> **Description:** Reduce the weight of the magic item by **X**%.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `Weightless`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder, Staff`
> * **AllowedRarities:** `Magic, Rare`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|20–35 (5)|25–40 (5)|40–60 (5)|
> |Rare|25–45 (5)|30–50 (5)|60–80 (5)|
>
> **Selection Weight:** 5

## RemoveSpeedPenalty

> **Display Text:** Unhindered
>
> **Description:** Wearing or wielding this magic item does not penalize the player's movement speed.
>
> **Requirements:**
> * **Flags:** `ItemHasNegativeMovementSpeedModifier`
> * **ExclusiveEffectTypes:** `ModifyMovementSpeed`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Shield, Tool, Helmet, Chest, Legs, Shoulder, Staff`
> * *Minimal:* AllowedRarities `Ancient`.
>
> **Selection Weight:** 2

## ModifyBlockPower

> **Display Text:** Block +{0:0.#}%
>
> **Description:** Increase the block armor of the magic item by **X**%.
>
> **Requirements:**
> * **Flags:** `ItemHasBlockPower`
> * **ExclusiveEffectTypes:** `Duelist`
> * **AllowedItemTypes:** `Shield, OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Staff`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|2–4 (0.5)|3–6 (1)|
> |Rare|2–4 (0.5)|4–8 (0.5)|6–12 (1)|
> |Epic|3–6 (0.5)|6–12 (0.5)|9–18 (1)|
> |Legendary|4–8 (0.5)|8–16 (0.5)|12–24 (1)|
> |Mythic|5–10 (0.5)|10–20 (0.5)|15–30 (1)|
> |Ancient|6–12 (0.5)|12–24 (0.5)|15–36 (1)|
>
> **Selection Weight:** 10

## ModifyBlockForce

> **Display Text:** Block Force +{0:0.#}%
>
> **Description:** Increase block force by +**X**%.
>
> **Requirements:**
> * **Flags:** `ItemHasBlockPower`
> * **AllowedItemTypes:** `Shield`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|5–7 (1)|5–7 (1)|5–7 (1)|
> |Rare|7–10 (1)|7–10 (1)|7–10 (1)|
> |Epic|10–13 (1)|10–13 (1)|10–15 (1)|
> |Legendary|13–16 (1)|13–16 (1)|15–20 (1)|
> |Mythic|16–20 (1)|16–20 (1)|20–25 (1)|
> |Ancient|19–24 (1)|19–24 (1)|25–30 (1)|
>
> **Selection Weight:** 1

## ModifyParry

> **Display Text:** Parry +{0:0.#}%
>
> **Description:** Increase the parry block armor, parry force, and parry bonus of the magic item by +**X**%.
>
> **Requirements:**
> * **Flags:** `ItemHasParryPower`
> * **ExclusiveEffectTypes:** `Duelist`
> * **AllowedItemTypes:** `Shield, OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_fishingrod`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|2–4 (0.5)|3–6 (1)|
> |Rare|2–4 (0.5)|4–8 (0.5)|6–12 (1)|
> |Epic|3–6 (0.5)|6–12 (0.5)|9–18 (1)|
> |Legendary|4–8 (0.5)|8–16 (0.5)|12–24 (1)|
> |Mythic|5–10 (0.5)|10–20 (0.5)|15–30 (1)|
> |Ancient|6–12 (0.5)|12–24 (0.5)|15–36 (1)|
>
> **Selection Weight:** 10

## ModifyArmor

> **Display Text:** Armor +{0:0.#}%
>
> **Description:** Increase the armor value of the magic equipment by **X**%.
>
> **Requirements:**
> * **Flags:** `ItemHasArmor`
> * **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Trinket`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|2–6 (0.5)|3–9 (1)|
> |Rare|1–4 (1)|4–8 (0.5)|6–12 (1)|
> |Epic|2–6 (1)|6–10 (0.5)|9–15 (1)|
> |Legendary|2–8 (1)|8–12 (0.5)|12–18 (1)|
> |Mythic|3–10 (1)|10–15 (0.5)|15–23 (1)|
> |Ancient|4–12 (1)|12–18 (0.5)|18–27 (1)|
>
> **Selection Weight:** 10

## ModifyBackstab

> **Display Text:** Backstab +{0:0.#}%
>
> **Description:** Increase the backstab multiplier against enemies not in combat by **X**%.
>
> **Requirements:**
> * **Flags:** `ItemHasBackstabBonus`
> * **AllowedSkillTypes:** `Knives, Bows`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|0.25–1 (0.25)|1–2 (0.25)|2–4 (0.5)|
> |Rare|0.5–1.5 (0.25)|2–4 (0.25)|3–6 (0.5)|
> |Epic|0.75–2 (0.25)|3–6 (0.25)|4–8 (0.5)|
> |Legendary|1–3 (0.25)|4–8 (0.25)|5–10 (0.5)|
> |Mythic|1.25–4 (0.25)|6–12 (0.25)|6–12 (0.5)|
> |Ancient|1.5–5 (0.25)|8–16 (0.25)|12–24 (0.5)|
>
> **Selection Weight:** 10

## IncreaseMiningDrop

> **Display Text:** Ore Yield +{0:0.##}%
>
> **Description:** Mined resources drop **X**% more. Stone only gets the bonus in rocks with nothing better.
>
> **Requirements:**
> * **AllowedRarities:** `Epic, Legendary, Mythic, Ancient`
> * **AllowedSkillTypes:** `Pickaxes`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Epic|12.5|25|50|
> |Legendary|25|50|100|
> |Mythic|37.5|75|150|
> |Ancient|50|100|200|
>
> **Selection Weight:** 10

## IncreaseTreeDrop

> **Display Text:** Wood Yield +{0:0.##}%
>
> **Description:** Trees and logs drop **X**% more. Plain wood only gets the bonus when there is no better wood.
>
> **Requirements:**
> * **Flags:** `ItemHasChopDamage`
> * **AllowedSkillTypes:** `Axes`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|5|10|20|
> |Rare|10|20|40|
> |Epic|15|30|60|
> |Legendary|20|40|80|
> |Mythic|25|50|100|
> |Ancient|30|60|120|
>
> **Selection Weight:** 10

## IncreaseHealth

> **Display Text:** Health +{0:0}
>
> **Description:** Increase the player's base maximum health by **X**.
>
> **Requirements:**
> * **AllowedItemTypes:** `Chest, Shoulder, Utility, Trinket, Helmet, Legs, Shield, Tool, Torch, TwoHandedWeaponLeft`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|4–8 (1)|4–12 (1)|
> |Rare|2–5 (1)|6–12 (1)|6–16 (1)|
> |Epic|3–6 (1)|8–14 (1)|8–20 (1)|
> |Legendary|4–8 (1)|10–16 (1)|10–24 (1)|
> |Mythic|5–10 (1)|12–18 (1)|18–27 (1)|
> |Ancient|6–12 (1)|14–20 (1)|21–30 (1)|
>
> **Selection Weight:** 10

## IncreaseHeatResistance

> **Display Text:** Heat Resistance +{0:0}%
>
> **Description:** Increase the player's heat resistance by **X**%. Additive, not multiplicative.
>
> **Requirements:**
> * **AllowedItemTypes:** `Shoulder`
> * **AllowedRarities:** `Epic, Legendary, Mythic, Ancient`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Epic|5–10 (1)|15–20 (0.5)|15–20 (1)|
> |Legendary|7–15 (1)|18–25 (0.5)|27–38 (1)|
> |Mythic|10–20 (1)|22–30 (0.5)|33–45 (1)|
> |Ancient|13–25 (1)|26–35 (0.5)|39–53 (1)|
>
> **Selection Weight:** Minimal 0.5 · Balanced 2.75 · Legendary 5

## IncreaseStamina

> **Display Text:** Stamina +{0:0}
>
> **Description:** Increase the player's base maximum stamina by **X**.
>
> **Requirements:**
> * **AllowedItemTypes:** `Legs, Tool, Utility, Trinket, Helmet, Chest, Shoulder, Shield, Torch, TwoHandedWeaponLeft`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|4–10 (1)|4–12 (1)|
> |Rare|2–5 (1)|6–12 (1)|6–16 (1)|
> |Epic|3–6 (1)|8–16 (1)|8–20 (1)|
> |Legendary|4–8 (1)|10–20 (1)|10–24 (1)|
> |Mythic|5–10 (1)|12–24 (1)|12–28 (1)|
> |Ancient|6–12 (1)|14–28 (1)|14–32 (1)|
>
> **Selection Weight:** 10

## IncreaseEitr

> **Display Text:** Eitr +{0:0}
>
> **Description:** Increase the player's base maximum eitr by **X**.
>
> **Requirements:**
> * **AllowedItemTypes:** `Helmet, Staff, Utility, Trinket, Chest, Legs, Shoulder, Shield, Tool, Torch, TwoHandedWeaponLeft`
> * **ExcludedItemNames:** `$item_hammer, $item_hoe, $item_cultivator, $item_fishingrod`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|6–12 (1)|6–12 (1)|
> |Rare|2–5 (1)|8–16 (1)|8–16 (1)|
> |Epic|3–6 (1)|12–24 (1)|12–24 (1)|
> |Legendary|4–8 (1)|16–32 (1)|16–32 (1)|
> |Mythic|5–10 (1)|18–36 (1)|18–40 (1)|
> |Ancient|6–12 (1)|20–40 (1)|20–48 (1)|
>
> **Selection Weight:** 2

## DecreaseForsakenCooldown

> **Display Text:** Forsaken Power Cooldown -{0:0.#}%
>
> **Description:** Reduce the cooldown of the player's Forsaken Power by **X**%.
>
> **Requirements:**
> * **AllowedItemTypes:** `Utility, Trinket, Helmet`
> * **AllowedRarities:** `Magic, Rare, Epic, Legendary, Mythic, Ancient`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|2–4 (1)|3–6 (0.5)|
> |Rare|2–8 (1)|4–8 (1)|6–12 (1)|
> |Epic|3–10 (1)|6–12 (1)|9–18 (1)|
> |Legendary|4–12 (1)|8–16 (1)|12–24 (1)|
> |Mythic|5–14 (1)|10–20 (1)|15–30 (1)|
> |Ancient|6–16 (1)|12–24 (1)|18–36 (1)|
>
> **Selection Weight:** Minimal 0.5 · Balanced 2.75 · Legendary 5

## DecreaseMeadCooldown

> **Display Text:** Mead Cooldown -{0:0.#}%
>
> **Description:** Decrease mead cooldowns by **X**%.
>
> **Requirements:**
> * **AllowedItemTypes:** `Utility, Trinket`
> * **AllowedRarities:** `Magic, Rare, Epic, Legendary, Mythic, Ancient`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|2–3 (1)|5–7 (1)|5–7 (1)|
> |Rare|3–5 (1)|7–10 (1)|7–10 (1)|
> |Epic|5–7 (1)|10–15 (1)|10–15 (1)|
> |Legendary|7–10 (1)|15–20 (1)|15–20 (1)|
> |Mythic|10–15 (1)|20–25 (1)|20–25 (1)|
> |Ancient|13–20 (1)|25–30 (1)|25–30 (1)|
>
> **Selection Weight:** 10

## ModifyHealthRegen

> **Display Text:** Health Regen +{0:0.#}%
>
> **Description:** Increase the player's health regeneration by **X**%.
>
> **Requirements:**
> * **AllowedItemTypes:** `Helmet, Shoulder, Chest, Legs, Utility, Trinket, Shield, Tool, Torch, TwoHandedWeaponLeft`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|2–4 (1)|3–6 (1)|
> |Rare|2–6 (1)|4–8 (1)|4–12 (1)|
> |Epic|3–8 (1)|6–12 (1)|6–14 (1)|
> |Legendary|4–10 (1)|8–16 (1)|8–18 (1)|
> |Mythic|5–12 (1)|10–20 (1)|10–25 (1)|
> |Ancient|6–14 (1)|12–24 (1)|12–32 (1)|
>
> **Selection Weight:** Minimal 5 · Balanced 1 · Legendary 5

## ModifyLowHealth

> **Display Text:** Health Critical Threshold +{0:0.#}% (now {1:0.#}%)
>
> **Description:** Increase the threshold for health critical effects by **X**%. Additive, not multiplicative.
>
> **Requirements:**
> * **AllowedItemTypes:** `Helmet, Utility, Trinket`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–5 (1)|2–5 (1)|3–8 (1)|
> |Rare|2–8 (1)|5–8 (1)|8–12 (1)|
> |Epic|4–12 (1)|8–12 (1)|12–18 (1)|
> |Legendary|6–14 (1)|12–16 (1)|18–24 (1)|
> |Mythic|8–20 (1)|16–20 (1)|24–30 (1)|
> |Ancient|10–22 (1)|20–24 (1)|30–36 (1)|
>
> **Selection Weight:** 10
>
> **Config:**
>
> |Setting|Key|Minimal|Balanced|Legendary|
> |--|--|--|--|--|
> |Max Low Health Threshold|`Max`|0.5|0.75|1|

## AddHealthRegen

> **Display Text:** Health Regen +{0:0.#}/tick
>
> **Description:** Increase the player's base health regeneration by **X** health per tick.
>
> **Requirements:**
> * **AllowedItemTypes:** `Helmet, Shoulder, Chest, Legs, Utility, Trinket, Shield, Tool, Torch, TwoHandedWeaponLeft`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|0.3–0.9 (0.3)|0.3–1.5 (0.3)|0.6–2.1 (0.3)|
> |Rare|0.3–2.7 (0.3)|0.3–3 (0.3)|0.9–3.9 (0.3)|
> |Epic|0.6–3.9 (0.3)|0.6–4.5 (0.3)|1.5–6 (0.3)|
> |Legendary|0.6–5.4 (0.3)|0.9–6 (0.3)|3–8.1 (0.3)|
> |Mythic|1.2–7.5 (0.3)|1.5–9.9 (0.3)|4.5–9.9 (0.3)|
> |Ancient|1.8–9 (0.3)|2.1–14.1 (0.3)|6–21 (0.3)|
>
> **Selection Weight:** Minimal 1 · Balanced 5.5 · Legendary 10

## ModifyStaminaRegen

> **Display Text:** Stamina Regen +{0:0.#}%
>
> **Description:** Increase the player's stamina regeneration by +**X**%.
>
> **Requirements:**
> * **AllowedItemTypes:** `Legs, Utility, Trinket, Tool, Helmet, Chest, Shoulder, Shield, Torch, TwoHandedWeaponLeft`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3.5 (0.5)|2–4 (1)|3–6 (1)|
> |Rare|2–8 (0.5)|4–8 (1)|4–12 (1)|
> |Epic|3–10 (0.5)|6–12 (1)|8–18 (1)|
> |Legendary|4–12 (0.5)|8–16 (1)|12–24 (1)|
> |Mythic|5–15 (0.5)|10–20 (1)|15–30 (1)|
> |Ancient|6–18 (0.5)|12–24 (1)|18–36 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 5 · Legendary 10

## AmmoConservation

> **Display Text:** Ammo Consumption Chance -{0:0.#}%
>
> **Description:** Adds a chance to not consume ammo when firing bow or crossbow.
>
> **Requirements:**
> * **AllowedItemTypes:** `Bow`
> * **ExcludedItemNames:** `$item_graplinghook`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|5–7 (1)|5–7 (1)|5–7 (1)|
> |Rare|7–10 (1)|7–10 (1)|7–10 (1)|
> |Epic|10–12 (1)|10–20 (1)|10–20 (1)|
> |Legendary|12–15 (1)|20–30 (1)|20–30 (1)|
> |Mythic|15–20 (1)|30–40 (1)|30–40 (1)|
> |Ancient|18–25 (1)|40–50 (1)|40–50 (1)|
>
> **Selection Weight:** 10

## InstantMead

> **Display Text:** Instant Mead (below {1:0.#}% health)
>
> **Description:** Meads that restore health, stamina, or eitr apply their effects instantly when health is critical.
>
> **Requirements:**
> * **AllowedItemTypes:** `Utility, Trinket`
> * **AllowedRarities:** `Rare, Epic, Legendary, Mythic, Ancient`
>
> **Selection Weight:** 10

## AutoMead

> **Display Text:** Automatic Mead Consumption (below {1:0.#}% health)
>
> **Description:** Meads that restore health are consumed automatically when health is critical, or when a hit would bring health to critical.
>
> **Requirements:**
> * **AllowedItemTypes:** `Utility, Trinket`
> * **AllowedRarities:** `Rare, Epic, Legendary, Mythic, Ancient`
>
> **Selection Weight:** 10

## ModifyNoise

> **Display Text:** Noise -{0:0.#}%
>
> **Description:** Reduce the distance the player's noise travels by **X**%.
>
> **Requirements:**
> * **AllowedItemTypes:** `Legs, Utility, Trinket`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|2–10 (1)|2–10 (1)|15–20 (1)|
> |Rare|5–13 (1)|6–15 (1)|20–25 (1)|
> |Epic|10–20 (1)|10–20 (1)|25–40 (1)|
> |Legendary|15–25 (1)|15–30 (1)|40–50 (1)|
> |Mythic|18–36 (1)|20–40 (1)|50–60 (1)|
> |Ancient|22–45 (1)|25–50 (1)|60–70 (1)|
>
> **Selection Weight:** 1

## ModifyBuildDistance

> **Display Text:** Build Distance +{0:0.#}%
>
> **Description:** Increase the player's build distance by +**X**%.
>
> **Requirements:**
> * **AllowedItemTypes:** `Tool`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|2–6 (1)|5–10 (1)|5–10 (1)|
> |Rare|4–10 (1)|10–15 (1)|10–15 (1)|
> |Epic|6–15 (1)|15–20 (1)|15–20 (1)|
> |Legendary|8–20 (1)|25–30 (1)|25–30 (1)|
> |Mythic|10–25 (1)|30–50 (1)|30–50 (1)|
> |Ancient|12–30 (1)|35–70 (1)|35–70 (1)|
>
> **Selection Weight:** 1

## ModifyPickupRange

> **Display Text:** Pickup Range +{0:0.#}%
>
> **Description:** Increase auto pickup range by +**X**%.
>
> **Requirements:**
> * **AllowedItemTypes:** `Utility, Trinket`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|5–10 (1)|10–20 (1)|10–20 (1)|
> |Rare|10–20 (1)|20–30 (1)|20–30 (1)|
> |Epic|15–30 (5)|30–50 (5)|30–50 (5)|
> |Legendary|20–40 (5)|50–75 (5)|50–75 (5)|
> |Mythic|25–50 (5)|75–100 (5)|75–100 (5)|
> |Ancient|30–60 (5)|100|100|
>
> **Selection Weight:** 1

## ModifyFireRate

> **Display Text:** Fire Rate +{0:0.#}%
>
> **Description:** Increase the rate at which projectiles fire by +**X**%.
>
> **Requirements:**
> * **AllowedItemTypes:** `Bow`
> * **ExcludedItemNames:** `$item_graplinghook`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–4 (1)|2–6 (1)|
> |Rare|3–4 (1)|4–7 (1)|7–10 (1)|
> |Epic|4–6 (1)|7–10 (1)|10–15 (1)|
> |Legendary|6–8 (1)|10–13 (1)|15–20 (1)|
> |Mythic|8–10 (1)|13–16 (1)|20–24 (1)|
> |Ancient|10–12 (1)|16–19 (1)|24–29 (1)|
>
> **Selection Weight:** Minimal 1 · Balanced 1 · Legendary 10

## ModifyMagicFireRate

> **Display Text:** Fire Rate +{0:0.#}%
>
> **Description:** Increase the rate at which magic weapons fire projectiles by +**X**%.
>
> **Requirements:**
> * **AllowedItemTypes:** `Staff`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–4 (1)|1–5 (1)|2–8 (1)|
> |Rare|2–7 (1)|3–10 (1)|5–15 (1)|
> |Epic|5–10 (1)|6–14 (1)|9–21 (1)|
> |Legendary|7–13 (1)|8–18 (1)|12–27 (1)|
> |Mythic|9–16 (1)|10–24 (1)|15–36 (1)|
> |Ancient|10–19 (1)|12–30 (1)|18–45 (1)|
>
> **Selection Weight:** 1

## ModifyProjectileSpeed

> **Display Text:** Projectile Speed +{0:0.#}%
>
> **Description:** Increase the speed of projectiles by +**X**%.
>
> **Requirements:**
> * **AllowedItemTypes:** `Staff, Bow`
> * **AllowedStaffRoles:** `Damage`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|5–7 (1)|5–7 (1)|8–11 (1)|
> |Rare|7–10 (1)|7–10 (1)|11–15 (1)|
> |Epic|10–15 (1)|10–15 (1)|15–23 (1)|
> |Legendary|15–20 (1)|15–20 (1)|23–30 (1)|
> |Mythic|20–30 (1)|20–30 (1)|30–45 (1)|
> |Ancient|25–40 (1)|25–40 (1)|30–60 (1)|
>
> **Selection Weight:** Minimal 1 · Balanced 10 · Legendary 10

## ModifyEitrRegen

> **Display Text:** Eitr Regen +{0:0.#}%
>
> **Description:** Increase Eitr Regen by {0:0.#}%.
>
> **Requirements:**
> * **AllowedItemTypes:** `Shoulder, Staff, Helmet, Chest, Legs, Utility, Trinket, Shield, Tool, Torch, TwoHandedWeaponLeft`
> * **ExcludedItemNames:** `$item_hammer, $item_hoe, $item_cultivator, $item_fishingrod`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|2–5 (1)|3–6 (1)|3–8 (1)|
> |Rare|6–12 (1)|6–12 (1)|6–12 (1)|
> |Epic|7–14 (1)|9–18 (1)|9–18 (1)|
> |Legendary|8–16 (1)|12–24 (1)|13–26 (1)|
> |Mythic|9–18 (1)|15–30 (1)|18–35 (1)|
> |Ancient|10–20 (1)|18–36 (1)|23–44 (1)|
>
> **Selection Weight:** 10

## AddBluntDamage

> **Display Text:** Blunt Damage +{0:0.#}%
>
> **Description:** Increase this weapon's blunt damage by **X**%. Only rolls on weapons that deal blunt damage.
>
> **Requirements:**
> * **Flags:** `ItemHasBluntDamage`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Torch`
> * **ExcludedItemTypes:** `Staff`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_fishingrod, $item_graplinghook`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|2–4 (0.5)|2–6 (1)|
> |Rare|1–4 (0.5)|3–6 (0.5)|3–9 (1)|
> |Epic|2–6 (0.5)|4–8 (0.5)|4–12 (1)|
> |Legendary|2–8 (0.5)|5–10 (0.5)|8–15 (1)|
> |Mythic|3–10 (0.5)|6–12 (0.5)|9–18 (1)|
> |Ancient|4–12 (0.5)|7–14 (0.5)|11–21 (1)|
>
> **Selection Weight:** 10

## AddSlashingDamage

> **Display Text:** Slash Damage +{0:0.#}%
>
> **Description:** Increase this weapon's slash damage by **X**%, and an axe's chop damage by the same percentage. Only rolls on weapons that deal slash damage.
>
> **Requirements:**
> * **Flags:** `ItemHasSlashDamage`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Torch`
> * **ExcludedItemTypes:** `Staff`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_fishingrod, $item_graplinghook`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|2–4 (0.5)|2–6 (1)|
> |Rare|1–4 (0.5)|3–6 (0.5)|3–9 (1)|
> |Epic|2–6 (0.5)|4–8 (0.5)|4–12 (1)|
> |Legendary|2–8 (0.5)|5–10 (0.5)|8–15 (1)|
> |Mythic|3–10 (0.5)|6–12 (0.5)|9–18 (1)|
> |Ancient|4–12 (0.5)|7–14 (0.5)|11–21 (1)|
>
> **Selection Weight:** 10

## AddPiercingDamage

> **Display Text:** Pierce Damage +{0:0.#}%
>
> **Description:** Increase this weapon's pierce damage by **X**%, and a pickaxe's mining damage by the same percentage. Only rolls on weapons that deal pierce damage.
>
> **Requirements:**
> * **Flags:** `ItemHasPierceDamage`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Torch`
> * **ExcludedItemTypes:** `Staff`
> * **ExcludedItemNames:** `$item_fishingrod, $item_graplinghook`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|2–4 (0.5)|2–6 (1)|
> |Rare|1–4 (0.5)|3–6 (0.5)|3–9 (1)|
> |Epic|2–6 (0.5)|4–8 (0.5)|4–12 (1)|
> |Legendary|2–8 (0.5)|5–10 (0.5)|8–15 (1)|
> |Mythic|3–10 (0.5)|6–12 (0.5)|9–18 (1)|
> |Ancient|4–12 (0.5)|7–14 (0.5)|11–21 (1)|
>
> **Selection Weight:** 10

## AddFireDamage

> **Display Text:** Imbue Fire Damage {0:0.#}%
>
> **Description:** Add **X**% of the total unenchanted damage of this weapon as fire damage to attacks made by the magic weapon.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `AddFrostDamage`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_fishingrod, $item_graplinghook`
> * **AllowedStaffRoles:** `Damage`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|2–4 (0.5)|2–6 (1)|
> |Rare|1–4 (0.5)|3–6 (0.5)|3–9 (1)|
> |Epic|2–6 (0.5)|4–8 (0.5)|4–12 (1)|
> |Legendary|2–8 (0.5)|5–10 (0.5)|8–15 (1)|
> |Mythic|3–10 (0.5)|6–12 (0.5)|9–18 (1)|
> |Ancient|4–12 (0.5)|7–14 (0.5)|11–21 (1)|
>
> **Selection Weight:** 10

## AddFrostDamage

> **Display Text:** Imbue Frost Damage {0:0.#}%
>
> **Description:** Add **X**% of the total unenchanted damage of this weapon as frost damage to attacks made by the magic weapon.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `AddFireDamage`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_fishingrod, $item_graplinghook`
> * **AllowedStaffRoles:** `Damage`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|2–4 (0.5)|2–6 (1)|
> |Rare|1–4 (0.5)|3–6 (0.5)|3–9 (1)|
> |Epic|2–6 (0.5)|4–8 (0.5)|4–12 (1)|
> |Legendary|2–8 (0.5)|5–10 (0.5)|8–15 (1)|
> |Mythic|3–10 (0.5)|6–12 (0.5)|9–18 (1)|
> |Ancient|4–12 (0.5)|7–14 (0.5)|11–21 (1)|
>
> **Selection Weight:** 10

## AddLightningDamage

> **Display Text:** Imbue Lightning Damage {0:0.#}%
>
> **Description:** Add lightning damage to attacks made by the magic weapon, on average **X**% of the total unenchanted damage of this weapon. Each hit rolls anywhere from none to twice that.
>
> **Requirements:**
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_fishingrod, $item_graplinghook`
> * **AllowedStaffRoles:** `Damage`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|2–4 (0.5)|2–6 (1)|
> |Rare|1–4 (0.5)|3–6 (0.5)|3–9 (1)|
> |Epic|2–6 (0.5)|4–8 (0.5)|4–12 (1)|
> |Legendary|2–8 (0.5)|5–10 (0.5)|8–15 (1)|
> |Mythic|3–10 (0.5)|6–12 (0.5)|9–18 (1)|
> |Ancient|4–12 (0.5)|7–14 (0.5)|11–21 (1)|
>
> **Selection Weight:** 10

## AddPoisonDamage

> **Display Text:** Imbue Poison Damage {0:0.#}%
>
> **Description:** Add poison damage equal to **X**% of this weapon's unenchanted physical damage, counting pierce three times, slash twice and blunt once. Elemental damage does not count.
>
> **Requirements:**
> * **Flags:** `ItemHasPhysicalDamage`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_fishingrod, $item_graplinghook`
> * **AllowedStaffRoles:** `Damage`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|2–4 (0.5)|2–6 (1)|
> |Rare|1–4 (0.5)|3–6 (0.5)|3–9 (1)|
> |Epic|2–6 (0.5)|4–8 (0.5)|4–12 (1)|
> |Legendary|2–8 (0.5)|5–10 (0.5)|8–15 (1)|
> |Mythic|3–10 (0.5)|6–12 (0.5)|9–18 (1)|
> |Ancient|4–12 (0.5)|7–14 (0.5)|11–21 (1)|
>
> **Selection Weight:** 10

## AddSpiritDamage

> **Display Text:** Imbue Spirit Damage {0:0.#}%
>
> **Description:** Add spirit damage equal to **X**% of this weapon's fire, frost, lightning and poison damage, including what other imbues add.
>
> **Requirements:**
> * **Flags:** `ItemHasNonSpiritElementalDamage`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_fishingrod, $item_graplinghook`
> * **AllowedStaffRoles:** `Damage`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|2–4 (0.5)|2–6 (1)|
> |Rare|1–4 (0.5)|3–6 (0.5)|3–9 (1)|
> |Epic|2–6 (0.5)|4–8 (0.5)|4–12 (1)|
> |Legendary|2–8 (0.5)|5–10 (0.5)|8–15 (1)|
> |Mythic|3–10 (0.5)|6–12 (0.5)|9–18 (1)|
> |Ancient|4–12 (0.5)|7–14 (0.5)|11–21 (1)|
>
> **Selection Weight:** 10

## AddFireResistancePercentage

> **Display Text:** Fire Resistance {0:0.#}%
>
> **Description:** The player reduces all fire damage taken by +**X**%. This effect stacks additively with other damage reduction effects equipped by the player.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `AddElementalResistancePercentage, AddFrostResistancePercentage, AddLightningResistancePercentage, AddPoisonResistancePercentage`
> * **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Trinket, Shield, Tool, Torch, TwoHandedWeaponLeft`
> * *Legendary:* ExclusiveEffectTypes adds `AddSpiritResistancePercentage`.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|1–3 (0.5)|2–6 (1)|
> |Rare|2–4 (0.5)|2–6 (0.5)|3–9 (1)|
> |Epic|3–6 (0.5)|3–9 (0.5)|4–12 (1)|
> |Legendary|4–8 (0.5)|4–12 (0.5)|5–15 (1)|
> |Mythic|5–10 (0.5)|5–15 (0.5)|6–18 (1)|
> |Ancient|6–12 (0.5)|6–18 (0.5)|7–21 (1)|
>
> **Selection Weight:** 10
>
> **Config:**
>
> |Setting|Key|Minimal|Balanced|Legendary|
> |--|--|--|--|--|
> |Max Resistance|`MaxResistance`|25|50|75|

## AddFrostResistancePercentage

> **Display Text:** Frost Resistance {0:0.#}%
>
> **Description:** The player reduces all frost damage taken by +**X**%. This effect does not provide protection from environmental effects. This effect stacks additively with other damage reduction effects equipped by the player.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `AddElementalResistancePercentage, AddFireResistancePercentage, AddLightningResistancePercentage, AddPoisonResistancePercentage`
> * **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Trinket, Shield, Tool, Torch, TwoHandedWeaponLeft`
> * *Legendary:* ExclusiveEffectTypes adds `AddSpiritResistancePercentage`.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|1–3 (0.5)|2–6 (1)|
> |Rare|2–4 (0.5)|2–6 (0.5)|3–9 (1)|
> |Epic|3–6 (0.5)|3–9 (0.5)|4–12 (1)|
> |Legendary|4–8 (0.5)|4–12 (0.5)|5–15 (1)|
> |Mythic|5–10 (0.5)|5–15 (0.5)|6–18 (1)|
> |Ancient|6–12 (0.5)|6–18 (0.5)|7–21 (1)|
>
> **Selection Weight:** 10
>
> **Config:**
>
> |Setting|Key|Minimal|Balanced|Legendary|
> |--|--|--|--|--|
> |Max Resistance|`MaxResistance`|25|50|75|

## AddLightningResistancePercentage

> **Display Text:** Lightning Resistance {0:0.#}%
>
> **Description:** The player reduces all lightning damage taken by +**X**%. This effect stacks additively with other damage reduction effects equipped by the player.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `AddElementalResistancePercentage, AddFireResistancePercentage, AddFrostResistancePercentage, AddPoisonResistancePercentage`
> * **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Trinket, Shield, Tool, Torch, TwoHandedWeaponLeft`
> * *Legendary:* ExclusiveEffectTypes adds `AddSpiritResistancePercentage`.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|1–3 (0.5)|2–6 (1)|
> |Rare|2–4 (0.5)|2–6 (0.5)|3–9 (1)|
> |Epic|3–6 (0.5)|3–9 (0.5)|4–12 (1)|
> |Legendary|4–8 (0.5)|4–12 (0.5)|5–15 (1)|
> |Mythic|5–10 (0.5)|5–15 (0.5)|6–18 (1)|
> |Ancient|6–12 (0.5)|6–18 (0.5)|7–21 (1)|
>
> **Selection Weight:** 10
>
> **Config:**
>
> |Setting|Key|Minimal|Balanced|Legendary|
> |--|--|--|--|--|
> |Max Resistance|`MaxResistance`|25|50|75|

## AddPoisonResistancePercentage

> **Display Text:** Poison Resistance {0:0.#}%
>
> **Description:** The player reduces all poison damage taken by +**X**%. This effect stacks additively with other damage reduction effects equipped by the player.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `AddElementalResistancePercentage, AddFireResistancePercentage, AddFrostResistancePercentage, AddLightningResistancePercentage`
> * **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Trinket, Shield, Tool, Torch, TwoHandedWeaponLeft`
> * *Legendary:* ExclusiveEffectTypes adds `AddSpiritResistancePercentage`.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|1–3 (0.5)|2–6 (1)|
> |Rare|2–4 (0.5)|2–6 (0.5)|3–9 (1)|
> |Epic|3–6 (0.5)|3–9 (0.5)|4–12 (1)|
> |Legendary|4–8 (0.5)|4–12 (0.5)|5–15 (1)|
> |Mythic|5–10 (0.5)|5–15 (0.5)|6–18 (1)|
> |Ancient|6–12 (0.5)|6–18 (0.5)|7–21 (1)|
>
> **Selection Weight:** 10
>
> **Config:**
>
> |Setting|Key|Minimal|Balanced|Legendary|
> |--|--|--|--|--|
> |Max Resistance|`MaxResistance`|25|50|75|

## AddSpiritResistancePercentage

> **Display Text:** Spirit Resistance {0:0.#}%
>
> **Description:** The player reduces all spirit damage taken by +**X**%. This effect stacks additively with other damage reduction effects equipped by the player.
>
> **Requirements:**
> * **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Trinket, Shield, Tool, Torch, TwoHandedWeaponLeft`
> * *Legendary:* can be rolled; ExclusiveEffectTypes `AddElementalResistancePercentage, AddFireResistancePercentage, AddFrostResistancePercentage, AddLightningResistancePercentage, AddPoisonResistancePercentage`.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|1–3 (0.5)|2–6 (1)|
> |Rare|2–4 (0.5)|2–6 (0.5)|3–9 (1)|
> |Epic|3–6 (0.5)|3–9 (0.5)|4–12 (1)|
> |Legendary|4–8 (0.5)|4–12 (0.5)|5–15 (1)|
> |Mythic|5–10 (0.5)|5–15 (0.5)|6–18 (1)|
> |Ancient|6–12 (0.5)|6–18 (0.5)|7–21 (1)|
>
> **Selection Weight:** 10
>
> **Config:**
>
> |Setting|Key|Minimal|Balanced|Legendary|
> |--|--|--|--|--|
> |Max Resistance|`MaxResistance`|25|50|75|
>
> ***Can't be rolled.***

## AddElementalResistancePercentage

> **Display Text:** Elemental Resistance {0:0.#}%
>
> **Description:** The player reduces all elemental damage (fire, frost, lightning, poison) taken by **X**%. This effect stacks additively with other damage reduction effects equipped by the player.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `AddFireResistancePercentage, AddFrostResistancePercentage, AddLightningResistancePercentage, AddPoisonResistancePercentage`
> * **AllowedItemTypes:** `Helmet, Utility`
> * **AllowedRarities:** `Epic, Legendary, Mythic, Ancient`
> * *Legendary:* ExclusiveEffectTypes adds `AddSpiritResistancePercentage`; AllowedRarities adds `Rare`.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|—|2–4 (0.2)|—|
> |Rare|2–4 (0.5)|3–6 (0.2)|5–9 (1)|
> |Epic|3–6 (0.5)|4–8 (0.2)|6–12 (1)|
> |Legendary|4–8 (0.5)|5–10 (0.2)|8–15 (1)|
> |Mythic|5–10 (0.5)|6–12 (0.2)|9–18 (1)|
> |Ancient|6–12 (0.5)|7–14 (0.2)|11–21 (1)|
>
> **Selection Weight:** Minimal 0.2 · Balanced 2.6 · Legendary 5
>
> **Config:**
>
> |Setting|Key|Minimal|Balanced|Legendary|
> |--|--|--|--|--|
> |Max Resistance|`MaxResistance`|25|50|75|

## AddBluntResistancePercentage

> **Display Text:** Blunt Resistance {0:0.#}%
>
> **Description:** The player reduces all blunt damage taken by **X**%. This effect stacks additively with other damage reduction effects equipped by the player.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `AddPhysicalResistancePercentage, AddSlashingResistancePercentage, AddPiercingResistancePercentage`
> * **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Trinket, Shield, Tool, Torch, TwoHandedWeaponLeft`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|1–3 (0.5)|2–6 (1)|
> |Rare|2–4 (0.5)|2–6 (0.5)|3–9 (1)|
> |Epic|3–6 (0.5)|3–9 (0.5)|4–12 (1)|
> |Legendary|4–8 (0.5)|4–12 (0.5)|5–15 (1)|
> |Mythic|5–10 (0.5)|5–15 (0.5)|6–18 (1)|
> |Ancient|6–12 (0.5)|6–18 (0.5)|7–21 (1)|
>
> **Selection Weight:** Minimal 0.5 · Balanced 5.25 · Legendary 10
>
> **Config:**
>
> |Setting|Key|Minimal|Balanced|Legendary|
> |--|--|--|--|--|
> |Max Resistance|`MaxResistance`|25|50|75|

## AddSlashingResistancePercentage

> **Display Text:** Slashing Resistance {0:0.#}%
>
> **Description:** The player reduces all slash damage taken by **X**%. This effect stacks additively with other damage reduction effects equipped by the player.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `AddPhysicalResistancePercentage, AddBluntResistancePercentage, AddPiercingResistancePercentage`
> * **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Trinket, Shield, Tool, Torch, TwoHandedWeaponLeft`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|1–3 (0.5)|2–6 (1)|
> |Rare|2–4 (0.5)|2–6 (0.5)|3–9 (1)|
> |Epic|3–6 (0.5)|3–9 (0.5)|4–12 (1)|
> |Legendary|4–8 (0.5)|4–12 (0.5)|5–15 (1)|
> |Mythic|5–10 (0.5)|5–15 (0.5)|6–18 (1)|
> |Ancient|6–12 (0.5)|6–18 (0.5)|7–21 (1)|
>
> **Selection Weight:** Minimal 0.5 · Balanced 5.25 · Legendary 10
>
> **Config:**
>
> |Setting|Key|Minimal|Balanced|Legendary|
> |--|--|--|--|--|
> |Max Resistance|`MaxResistance`|25|50|75|

## AddPiercingResistancePercentage

> **Display Text:** Piercing Resistance {0:0.#}%
>
> **Description:** The player reduces all pierce damage taken by **X**%. This effect stacks additively with other damage reduction effects equipped by the player.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `AddPhysicalResistancePercentage, AddBluntResistancePercentage, AddSlashingResistancePercentage`
> * **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Trinket, Shield, Tool, Torch, TwoHandedWeaponLeft`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|1–3 (0.5)|2–6 (1)|
> |Rare|2–4 (0.5)|2–6 (0.5)|3–9 (1)|
> |Epic|3–6 (0.5)|3–9 (0.5)|4–12 (1)|
> |Legendary|4–8 (0.5)|4–12 (0.5)|5–15 (1)|
> |Mythic|5–10 (0.5)|5–15 (0.5)|6–18 (1)|
> |Ancient|6–12 (0.5)|6–18 (0.5)|7–21 (1)|
>
> **Selection Weight:** Minimal 0.5 · Balanced 5.25 · Legendary 10
>
> **Config:**
>
> |Setting|Key|Minimal|Balanced|Legendary|
> |--|--|--|--|--|
> |Max Resistance|`MaxResistance`|25|50|75|

## AddPhysicalResistancePercentage

> **Display Text:** Physical Resistance {0:0.#}%
>
> **Description:** The player reduces all physical damage (blunt, slash, pierce) taken by **X**%. This effect stacks additively with other damage reduction effects equipped by the player.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `AddBluntResistancePercentage, AddSlashingResistancePercentage, AddPiercingResistancePercentage`
> * **AllowedItemTypes:** `Chest, Legs`
> * **AllowedRarities:** `Epic, Legendary, Mythic, Ancient`
> * *Legendary:* AllowedRarities adds `Rare`.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|—|2–4 (0.2)|—|
> |Rare|2–4 (0.5)|3–6 (0.2)|5–9 (1)|
> |Epic|3–6 (0.5)|4–8 (0.2)|6–12 (1)|
> |Legendary|4–8 (0.5)|5–10 (0.2)|8–15 (1)|
> |Mythic|5–10 (0.5)|6–12 (0.2)|9–18 (1)|
> |Ancient|6–12 (0.5)|7–14 (0.2)|11–21 (1)|
>
> **Selection Weight:** Minimal 0.2 · Balanced 5.1 · Legendary 10
>
> **Config:**
>
> |Setting|Key|Minimal|Balanced|Legendary|
> |--|--|--|--|--|
> |Max Resistance|`MaxResistance`|25|50|75|

## ModifyMovementSpeed

> **Display Text:** Movement Speed +{0:0.#}%
>
> **Description:** Increase the player's movement speed by **X**%.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `RemoveSpeedPenalty`
> * **AllowedItemTypes:** `Legs, Utility, Trinket, Tool, Torch, TwoHandedWeaponLeft`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1|1–2 (0.5)|1–3 (1)|
> |Rare|2|2–4 (0.5)|2–5 (1)|
> |Epic|3|3–6 (0.5)|3–7 (1)|
> |Legendary|4|4–8 (0.5)|4–8 (1)|
> |Mythic|5|5–10 (0.5)|5–10 (1)|
> |Ancient|6|6–12 (0.5)|6–12 (1)|
>
> **Selection Weight:** 5

## ModifySprintStaminaUse

> **Display Text:** Sprint Stamina Use -{0:0.#}%
>
> **Description:** Reduce the player's sprint stamina usage by **X**%.
>
> **Requirements:**
> * **AllowedItemTypes:** `Legs, Utility, Trinket, Tool, Torch, TwoHandedWeaponLeft`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (1)|4–8 (0.5)|6–12 (1)|
> |Rare|2–4 (1)|5–10 (0.5)|8–15 (1)|
> |Epic|3–6 (1)|6–12 (0.5)|8–15 (1)|
> |Legendary|4–8 (1)|8–16 (0.5)|8–18 (1)|
> |Mythic|5–10 (1)|10–20 (0.5)|10–25 (1)|
> |Ancient|6–12 (1)|12–24 (0.5)|12–32 (1)|
>
> **Selection Weight:** 10

## ModifyDodgeStaminaUse

> **Display Text:** Dodge Stamina Use -{0:0.#}%
>
> **Description:** Reduce the player's dodge stamina usage by **X**%.
>
> **Requirements:**
> * **AllowedItemTypes:** `Legs, Shield, Utility, Trinket, Tool, Torch, TwoHandedWeaponLeft`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–4 (0.5)|1–6 (1)|
> |Rare|2–6 (1)|2–8 (0.5)|2–9 (1)|
> |Epic|4–9 (1)|4–10 (0.5)|4–12 (1)|
> |Legendary|6–12 (1)|6–12 (0.5)|6–18 (1)|
> |Mythic|7–14 (1)|8–16 (0.5)|9–25 (1)|
> |Ancient|9–18 (1)|10–20 (0.5)|12–32 (1)|
>
> **Selection Weight:** Minimal 0.5 · Balanced 5.25 · Legendary 10

## ModifyJumpStaminaUse

> **Display Text:** Jump Stamina Use -{0:0.#}%
>
> **Description:** Reduce the player's jump stamina usage by **X**%.
>
> **Requirements:**
> * **AllowedItemTypes:** `Legs, Utility, Trinket, Tool, Torch, TwoHandedWeaponLeft`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|2–6 (0.5)|2–6 (1)|
> |Rare|2–6 (1)|3–9 (0.5)|3–12 (1)|
> |Epic|4–9 (1)|4–12 (0.5)|4–18 (1)|
> |Legendary|4–12 (1)|5–15 (0.5)|6–24 (1)|
> |Mythic|5–15 (1)|6–18 (0.5)|9–30 (1)|
> |Ancient|6–18 (1)|7–21 (0.5)|12–36 (1)|
>
> **Selection Weight:** 10

## ModifyAttackEitrUse

> **Display Text:** Attack Eitr Use -{0:0.#}%
>
> **Description:** Reduce the player's attack eitr usage by **X**%.
>
> **Requirements:**
> * **Flags:** `ItemUsesEitrOnAttack`
> * *Minimal:* AllowedRarities `Epic, Legendary, Mythic, Ancient`.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|—|1–3 (1)|2–5 (1)|
> |Rare|1–4 (1)|2–6 (1)|3–9 (1)|
> |Epic|2–8 (1)|4–9 (1)|6–14 (1)|
> |Legendary|3–12 (1)|6–12 (1)|9–18 (1)|
> |Mythic|4–15 (1)|9–15 (1)|14–23 (1)|
> |Ancient|5–18 (1)|12–18 (1)|18–27 (1)|
>
> **Selection Weight:** Minimal 1 · Balanced 5.5 · Legendary 10

## ModifyAttackHealthUse

> **Display Text:** Attack Health Use -{0:0.#}%
>
> **Description:** Reduce the player's attack health usage by **X**%.
>
> **Requirements:**
> * **Flags:** `ItemUsesHealthOnAttack`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–3 (1)|2–5 (1)|
> |Rare|2–6 (1)|2–6 (1)|3–9 (1)|
> |Epic|4–9 (1)|4–9 (1)|6–14 (1)|
> |Legendary|6–12 (1)|6–12 (1)|9–18 (1)|
> |Mythic|9–15 (1)|9–15 (1)|14–23 (1)|
> |Ancient|12–18 (1)|12–18 (1)|18–27 (1)|
>
> **Selection Weight:** Minimal 0.5 · Balanced 5.25 · Legendary 10

## ModifyAttackStaminaUse

> **Display Text:** Attack Stamina Use -{0:0.#}%
>
> **Description:** Reduce the player's attack stamina usage by **X**%.
>
> **Requirements:**
> * **Flags:** `ItemUsesStaminaOnAttack, !ItemUsesEitrOnAttack`
> * **ExclusiveEffectTypes:** `Bloodlust`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Torch, Tool`
> * **ExcludedItemNames:** `$item_hammer, $item_hoe, $item_cultivator, $item_fishingrod`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|2–6 (0.5)|2–6 (1)|
> |Rare|2–6 (1)|4–10 (0.5)|4–10 (1)|
> |Epic|4–9 (1)|6–12 (0.5)|6–16 (1)|
> |Legendary|6–12 (1)|8–16 (0.5)|8–22 (1)|
> |Mythic|9–15 (1)|10–20 (0.5)|10–30 (1)|
> |Ancient|12–18 (1)|12–24 (0.5)|12–38 (1)|
>
> **Selection Weight:** 10

## Bloodlust

> **Display Text:** Bloodletting
>
> **Description:** Attacks use health instead of stamina equal to original stamina use.
>
> **Requirements:**
> * **Flags:** `ItemUsesStaminaOnAttack, !ItemUsesEitrOnAttack`
> * **ExclusiveEffectTypes:** `ModifyAttackStaminaUse`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_hammer, $item_hoe, $item_cultivator, $item_fishingrod`
>
> **Selection Weight:** 5

## ModifyDrawStaminaUse

> **Display Text:** Draw Stamina -{0:0.#}%
>
> **Description:** Reduce the draw stamina usage of the magic item by **X**%.
>
> **Requirements:**
> * **Flags:** `ItemUsesDrawStaminaOnAttack`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|2–5 (1)|3–8 (1)|
> |Rare|2–6 (1)|4–8 (1)|6–12 (1)|
> |Epic|4–9 (1)|6–10 (1)|9–15 (1)|
> |Legendary|6–12 (1)|8–12 (1)|9–15 (1)|
> |Mythic|9–15 (1)|10–15 (1)|10–15 (1)|
> |Ancient|12–18 (1)|12–18 (1)|12–18 (1)|
>
> **Selection Weight:** Minimal 0.5 · Balanced 2.75 · Legendary 5

## ModifyBlockStaminaUse

> **Display Text:** Block Stamina Use -{0:0.#}%
>
> **Description:** Reduce the player's block stamina usage by **X**%.
>
> **Requirements:**
> * **Flags:** `ItemHasBlockPower`
> * **AllowedItemTypes:** `Shield, OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Staff, Tool`
> * **ExcludedItemNames:** `$item_fishingrod`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|2–6 (0.5)|2–6 (1)|
> |Rare|2–6 (1)|3–9 (0.5)|3–12 (1)|
> |Epic|4–9 (1)|4–12 (0.5)|4–18 (1)|
> |Legendary|4–12 (1)|5–15 (0.5)|6–24 (1)|
> |Mythic|5–15 (1)|6–18 (0.5)|9–30 (1)|
> |Ancient|6–18 (1)|7–21 (0.5)|12–36 (1)|
>
> **Selection Weight:** 10

## Indestructible

> **Display Text:** Indestructible
>
> **Description:** The magic item does not use durability and will never break.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `ModifyDurability`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Tool, Shield, Helmet, Chest, Legs, Shoulder, Staff`
> * **AllowedRarities:** `Legendary, Mythic, Ancient`
> * **ExcludedItemNames:** `$item_fishingrod`
> * *Minimal:* AllowedRarities drops `Legendary, Mythic`.
> * *Legendary:* AllowedRarities adds `Epic`.
>
> **Selection Weight:** Minimal 0.2 · Balanced 1.1 · Legendary 2

## CoinHoarder

> **Display Text:** Coin Weighted Weapon {0:0.#}
>
> **Description:** Increases all weapon damage the more coins you carry, with strongly diminishing returns on large hoards. A higher value raises the bonus. The bracketed figure is the bonus at the coins you carry now.
>
> **Requirements:**
> * **AllowedItemTypes:** `Utility, Trinket, Helmet`
> * **AllowedRarities:** `Epic, Legendary, Mythic, Ancient`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Epic|1.5–3 (0.5)|4–6 (0.5)|4–6 (0.5)|
> |Legendary|2–4 (0.5)|6–8 (0.5)|6–8 (0.5)|
> |Mythic|3–5 (0.5)|8–10 (0.5)|8–10 (0.5)|
> |Ancient|4–6 (0.5)|10–12 (0.5)|10–12 (0.5)|
>
> **Selection Weight:** 10
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Linear Growth (0 = Off)|`LinearGrowth`|1|
> |Linear Growth Until (Coins)|`LinearCutoff`|1000|
> |Bonus Scalar|`Scalar`|6.258|

## Weightless

> **Display Text:** Weightless
>
> **Description:** The magic item has a weight of 0.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `ReduceWeight`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Tool, Shield, Helmet, Chest, Legs, Shoulder, Staff`
> * **AllowedRarities:** `Epic, Legendary, Mythic, Ancient`
>
> **Selection Weight:** 5

## AddCarryWeight

> **Display Text:** Carry Weight +{0}
>
> **Description:** Increase the player's maximum carry weight capacity by **X**.
>
> **Requirements:**
> * **AllowedItemTypes:** `Shoulder, Utility, Trinket, Helmet, Chest, Legs`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|5–10 (1)|25–50 (5)|25–50 (5)|
> |Rare|8–16 (1)|40–75 (5)|40–75 (5)|
> |Epic|10–20 (1)|70–100 (5)|70–100 (5)|
> |Legendary|12–24 (1)|90–125 (5)|90–125 (5)|
> |Mythic|14–30 (1)|120–150 (5)|120–150 (5)|
> |Ancient|16–36 (1)|150–175 (5)|150–175 (5)|
>
> **Selection Weight:** 5

## LifeSteal

> **Display Text:** Lifesteal {0:0.#}%
>
> **Description:** The player heals for **X**% of total damage done with each attack.
>
> **Requirements:**
> * **Flags:** `ItemUsesStaminaOnAttack`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow`
> * **AllowedRarities:** `Rare, Epic, Legendary, Mythic, Ancient`
> * **ExcludedSkillTypes:** `Pickaxes, BloodMagic, Bows, Crossbows`
> * **ExcludedItemNames:** `$item_fishingrod, $item_graplinghook`
> * **AllowedStaffRoles:** `Damage`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Rare|0.5–1 (0.5)|1.5–3 (0.5)|1.5–4 (0.5)|
> |Epic|1–2 (0.5)|2.5–5 (0.5)|2.5–6 (0.5)|
> |Legendary|1–3 (0.5)|4.5–7 (0.5)|4.5–10 (0.5)|
> |Mythic|2–4 (0.5)|6.5–10 (0.5)|6.5–12 (0.5)|
> |Ancient|3–5 (0.5)|8.5–13 (0.5)|8.5–14 (0.5)|
>
> **Selection Weight:** Minimal 5 · Balanced 1 · Legendary 5

## LifeAfterKill

> **Display Text:** Heal {0:0.#} on Kill
>
> **Description:** The player heals **X** health each time one of their hits kills an enemy.
>
> **Requirements:**
> * **Flags:** `ItemUsesStaminaOnAttack`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow`
> * **AllowedRarities:** `Rare, Epic, Legendary, Mythic, Ancient`
> * **ExcludedSkillTypes:** `Pickaxes, BloodMagic, Bows, Crossbows`
> * **ExcludedItemNames:** `$item_fishingrod, $item_graplinghook`
> * **AllowedStaffRoles:** `Damage`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Rare|2–4 (1)|5–10 (1)|5–12 (1)|
> |Epic|3–6 (1)|8–15 (1)|8–18 (1)|
> |Legendary|4–8 (1)|12–20 (1)|12–25 (1)|
> |Mythic|6–10 (1)|16–25 (1)|16–32 (1)|
> |Ancient|8–12 (1)|20–30 (1)|20–40 (1)|
>
> **Selection Weight:** Minimal 5 · Balanced 1 · Legendary 5

## EitrLeech

> **Display Text:** Eitr Leech {0:0.#}%
>
> **Description:** The player gains eitr on hit equal to **X**% of total damage done with each attack.
>
> **Requirements:**
> * **Flags:** `!ItemUsesEitrOnAttack`
> * **ExclusiveEffectTypes:** `EitrWeave`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow`
> * **AllowedRarities:** `Rare, Epic, Legendary, Mythic, Ancient`
> * **ExcludedSkillTypes:** `Pickaxes, Bows, Crossbows`
> * **ExcludedItemNames:** `$item_fishingrod, $item_graplinghook`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Rare|0.5–1.5 (0.5)|0.5–2 (0.5)|1–3 (0.5)|
> |Epic|1.5–2 (0.5)|1.5–3 (0.5)|2.5–4.5 (0.5)|
> |Legendary|2–2.5 (0.5)|2.5–4 (0.5)|4–6 (0.5)|
> |Mythic|2.5–3 (0.5)|3.5–5 (0.5)|5.5–7.5 (0.5)|
> |Ancient|3–3.5 (0.5)|4.5–6 (0.5)|7–9 (0.5)|
>
> **Selection Weight:** 10

## ModifyAttackSpeed

> **Display Text:** Attack Speed +{0:0.#}%
>
> **Description:** Attacks made with the magic weapon are **X**% faster.
>
> **Requirements:**
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Tool, Staff`
> * **ExcludedItemNames:** `$item_fishingrod, $item_hammer, $item_hoe, $item_cultivator`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (1)|1–2 (0.5)|2–3 (1)|
> |Rare|1–4 (1)|2–4 (0.5)|3–6 (1)|
> |Epic|2–6 (1)|3–6 (0.5)|5–9 (1)|
> |Legendary|2–8 (1)|4–8 (0.5)|6–12 (1)|
> |Mythic|3–10 (1)|5–10 (0.5)|8–15 (1)|
> |Ancient|4–12 (1)|6–12 (0.5)|9–18 (1)|
>
> **Selection Weight:** Minimal 0.5 · Balanced 5.25 · Legendary 10

## Throwable

> **Display Text:** Throwable
>
> **Description:** The magic weapon's secondary attack is now Throw Weapon.
>
> **Requirements:**
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon`
> * **ExcludedItemTypes:** `Staff`
> * **ExcludedSkillTypes:** `Pickaxes, Spears`
> * **ExcludedItemNames:** `$item_fishingrod, $item_knife_butcher`
>
> **Selection Weight:** 2

## Waterproof

> **Display Text:** Waterproof
>
> **Description:** While wearing the magical equipment, the player will not get wet from the rain. Immersion in water will still make the player wet.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `Warmth`
> * **AllowedItemTypes:** `Shoulder`
> * **AllowedRarities:** `Epic, Legendary, Mythic, Ancient`
> * *Minimal:* AllowedRarities drops `Epic, Legendary, Mythic`.
> * *Legendary:* no AllowedRarities.
>
> **Selection Weight:** Minimal 0.2 · Balanced 2.6 · Legendary 5

## Warmth

> **Display Text:** Warmth
>
> **Description:** While wearing the magical equipment, the player will not get cold. Will not remove an existing freezing effect. Does not grant cold resistance.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `Waterproof`
> * **AllowedItemTypes:** `Shoulder`
> * **AllowedRarities:** `Epic, Legendary, Mythic, Ancient`
> * *Minimal:* AllowedRarities drops `Epic`.
>
> **Selection Weight:** Minimal 0.2 · Balanced 5.1 · Legendary 10

## Paralyze

> **Display Text:** Paralyze {0:0.#} Seconds
>
> **Description:** Enemies hit with attacks made by the magic weapon will be paralyzed, unable to move or attack, for **X** seconds.
>
> **Requirements:**
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow`
> * **ExcludedSkillTypes:** `Pickaxes`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Rare|0.5–2 (0.1)|0.5–2 (0.1)|0.5–2 (0.1)|
> |Epic|1.5–3 (0.1)|1.5–3 (0.1)|1.5–3 (0.1)|
> |Legendary|2–4 (0.1)|2–4 (0.1)|2–4 (0.1)|
> |Mythic|3–5 (0.1)|3–5 (0.1)|3–5 (0.1)|
> |Ancient|4–6 (0.1)|4–6 (0.1)|4–6 (0.1)|
>
> ***Can't be rolled.***

## DoubleJump

> **Display Text:** Air Jump
>
> **Description:** While in the air, the player may jump an additional time.
>
> **Requirements:**
> * **AllowedItemTypes:** `Legs`
> * **AllowedRarities:** `Epic, Legendary, Mythic, Ancient`
> * *Minimal:* AllowedRarities drops `Epic, Legendary, Mythic`.
>
> **Selection Weight:** Minimal 0.2 · Balanced 2.6 · Legendary 5

## ExplosiveArrows

> **Display Text:** Explosive Shot +{0:0.#}%
>
> **Description:** Ranged attacks made by the magic weapon deal +**X**% attack damage in an area of effect around the projectile impact.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `TripleBowShot`
> * **AllowedItemTypes:** `Bow`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_graplinghook`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|2–3 (0.5)|3–5 (1)|
> |Rare|2–6 (1)|3–6 (0.5)|5–9 (1)|
> |Epic|3–9 (1)|4–9 (0.5)|6–14 (1)|
> |Legendary|4–12 (1)|6–12 (0.5)|9–18 (1)|
> |Mythic|5–15 (1)|8–15 (0.5)|12–23 (1)|
> |Ancient|6–18 (1)|10–18 (0.5)|15–27 (1)|
>
> **Selection Weight:** 10

## QuickDraw

> **Display Text:** Quick Draw +{0:0.#}%
>
> **Description:** Increases the draw speed of the magic bow by +**X**%.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `TripleBowShot`
> * **AllowedItemTypes:** `Bow`
> * **ExcludedSkillTypes:** `Pickaxes`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|2–4 (0.5)|3–6 (1)|
> |Rare|1–5 (1)|4–8 (0.5)|6–12 (1)|
> |Epic|2–8 (1)|6–12 (0.5)|9–18 (1)|
> |Legendary|2–12 (1)|8–16 (0.5)|12–24 (1)|
> |Mythic|4–16 (1)|10–20 (0.5)|15–30 (1)|
> |Ancient|6–20 (1)|12–24 (0.5)|18–36 (1)|
>
> **Selection Weight:** Minimal 0.5 · Balanced 5.25 · Legendary 10

## AddSwordsSkill

> **Display Text:** Swords Skill +{0}
>
> **Description:** Increases the player's swords skill level by +**X**. This may cause the skill level to increase above 100.
>
> **Requirements:**
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon`
> * **AllowedSkillTypes:** `Swords`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–3 (1)|1–6 (1)|
> |Rare|2–6 (1)|2–6 (1)|2–9 (1)|
> |Epic|2–8 (1)|3–9 (1)|4–12 (1)|
> |Legendary|3–10 (1)|4–12 (1)|6–18 (1)|
> |Mythic|4–13 (1)|5–15 (1)|9–25 (1)|
> |Ancient|5–16 (1)|6–18 (1)|12–32 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 2 · Legendary 10

## AddKnivesSkill

> **Display Text:** Knives Skill +{0}
>
> **Description:** Increases the player's knives skill level by +**X**. This may cause the skill level to increase above 100.
>
> **Requirements:**
> * **AllowedSkillTypes:** `Knives`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–3 (1)|1–6 (1)|
> |Rare|2–6 (1)|2–6 (1)|2–9 (1)|
> |Epic|2–8 (1)|3–9 (1)|4–12 (1)|
> |Legendary|3–10 (1)|4–12 (1)|6–18 (1)|
> |Mythic|4–13 (1)|5–15 (1)|9–25 (1)|
> |Ancient|5–16 (1)|6–18 (1)|12–32 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 2 · Legendary 10

## AddClubsSkill

> **Display Text:** Clubs Skill +{0}
>
> **Description:** Increases the player's clubs skill level by +**X**. This may cause the skill level to increase above 100.
>
> **Requirements:**
> * **AllowedSkillTypes:** `Clubs`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–3 (1)|1–6 (1)|
> |Rare|2–6 (1)|2–6 (1)|2–9 (1)|
> |Epic|2–8 (1)|3–9 (1)|4–12 (1)|
> |Legendary|3–10 (1)|4–12 (1)|6–18 (1)|
> |Mythic|4–13 (1)|5–15 (1)|9–25 (1)|
> |Ancient|5–16 (1)|6–18 (1)|12–32 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 2 · Legendary 10

## AddPolearmsSkill

> **Display Text:** Polearms Skill +{0}
>
> **Description:** Increases the player's polearms skill level by +**X**. This may cause the skill level to increase above 100.
>
> **Requirements:**
> * **AllowedSkillTypes:** `Polearms`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–3 (1)|1–6 (1)|
> |Rare|2–6 (1)|2–6 (1)|2–9 (1)|
> |Epic|2–8 (1)|3–9 (1)|4–12 (1)|
> |Legendary|3–10 (1)|4–12 (1)|6–18 (1)|
> |Mythic|4–13 (1)|5–15 (1)|9–25 (1)|
> |Ancient|5–16 (1)|6–18 (1)|12–32 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 2 · Legendary 10

## AddSpearsSkill

> **Display Text:** Spears Skill +{0}
>
> **Description:** Increases the player's spears skill level by +**X**. This may cause the skill level to increase above 100.
>
> **Requirements:**
> * **AllowedSkillTypes:** `Spears`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–3 (1)|1–6 (1)|
> |Rare|2–6 (1)|2–6 (1)|2–9 (1)|
> |Epic|2–8 (1)|3–9 (1)|4–12 (1)|
> |Legendary|3–10 (1)|4–12 (1)|6–18 (1)|
> |Mythic|4–13 (1)|5–15 (1)|9–25 (1)|
> |Ancient|5–16 (1)|6–18 (1)|12–32 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 2 · Legendary 10

## AddBlockingSkill

> **Display Text:** Blocking Skill +{0}
>
> **Description:** Increases the player's blocking skill level by +**X**. This may cause the skill level to increase above 100.
>
> **Requirements:**
> * **AllowedSkillTypes:** `Blocking`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–3 (1)|1–6 (1)|
> |Rare|2–6 (1)|2–6 (1)|2–9 (1)|
> |Epic|2–8 (1)|3–9 (1)|4–12 (1)|
> |Legendary|3–10 (1)|4–12 (1)|6–18 (1)|
> |Mythic|4–13 (1)|5–15 (1)|9–25 (1)|
> |Ancient|5–16 (1)|6–18 (1)|12–32 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 2 · Legendary 10

## AddAxesSkill

> **Display Text:** Axes Skill +{0}
>
> **Description:** Increases the player's axes and woodcutting skill levels by +**X**. This may cause the skills' level to increase above 100.
>
> **Requirements:**
> * **AllowedSkillTypes:** `Axes`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–3 (1)|1–6 (1)|
> |Rare|2–6 (1)|2–6 (1)|2–9 (1)|
> |Epic|2–8 (1)|3–9 (1)|4–12 (1)|
> |Legendary|3–10 (1)|4–12 (1)|6–18 (1)|
> |Mythic|4–13 (1)|5–15 (1)|9–25 (1)|
> |Ancient|5–16 (1)|6–18 (1)|12–32 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 2 · Legendary 10

## AddBowsSkill

> **Display Text:** Bows Skill +{0}
>
> **Description:** Increases the player's bows skill level by +**X**. This may cause the skill level to increase above 100.
>
> **Requirements:**
> * **AllowedSkillTypes:** `Bows`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–3 (1)|1–6 (1)|
> |Rare|2–6 (1)|2–6 (1)|2–9 (1)|
> |Epic|2–8 (1)|3–9 (1)|4–12 (1)|
> |Legendary|3–10 (1)|4–12 (1)|6–18 (1)|
> |Mythic|4–13 (1)|5–15 (1)|9–25 (1)|
> |Ancient|5–16 (1)|6–18 (1)|12–32 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 2 · Legendary 10

## AddCrossbowsSkill

> **Display Text:** Crossbows Skill +{0}
>
> **Description:** Increases the player's crossbows skill level by +**X**. This may cause the skill level to increase above 100.
>
> **Requirements:**
> * **AllowedSkillTypes:** `Crossbows`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–3 (1)|1–6 (1)|
> |Rare|2–6 (1)|2–6 (1)|2–9 (1)|
> |Epic|2–8 (1)|3–9 (1)|4–12 (1)|
> |Legendary|3–10 (1)|4–12 (1)|6–18 (1)|
> |Mythic|4–13 (1)|5–15 (1)|9–25 (1)|
> |Ancient|5–16 (1)|6–18 (1)|12–32 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 2 · Legendary 10

## AddBloodMagicSkill

> **Display Text:** Blood Magic Skill +{0}
>
> **Description:** Increases the player's blood magic skill level by +**X**. This may cause the skill level to increase above 100.
>
> **Requirements:**
> * **AllowedSkillTypes:** `BloodMagic`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–3 (1)|1–6 (1)|
> |Rare|2–6 (1)|2–6 (1)|2–9 (1)|
> |Epic|2–8 (1)|3–9 (1)|4–12 (1)|
> |Legendary|3–10 (1)|4–12 (1)|6–18 (1)|
> |Mythic|4–13 (1)|5–15 (1)|9–25 (1)|
> |Ancient|5–16 (1)|6–18 (1)|12–32 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 2 · Legendary 10

## AddCrafterSkills

> **Display Text:** Crafting Skills +{0}
>
> **Description:** Increases the player's crafting and cooking skill levels by +**X**. This may cause the skill level to increase above 100.
>
> **Requirements:**
> * **AllowedItemTypes:** `Chest, Legs, Shoulder, Utility, Trinket`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–3 (1)|2–5 (1)|
> |Rare|2–6 (1)|2–6 (1)|2–9 (1)|
> |Epic|4–9 (1)|4–9 (1)|4–12 (1)|
> |Legendary|6–12 (1)|6–12 (1)|6–18 (1)|
> |Mythic|9–15 (1)|9–15 (1)|14–23 (1)|
> |Ancient|12–18 (1)|12–18 (1)|18–27 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 5 · Legendary 10

## ModifySummonHealth

> **Display Text:** Summons Health +{0:0.#}%
>
> **Description:** Increases the health of summoned creatures by +**X**%.
>
> **Requirements:**
> * **AllowedSkillTypes:** `BloodMagic`
> * **AllowedStaffRoles:** `Summon`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–4 (1)|1–5 (1)|1–5 (1)|
> |Rare|2–8 (1)|3–10 (1)|3–10 (1)|
> |Epic|4–12 (1)|8–15 (1)|8–15 (1)|
> |Legendary|6–18 (1)|13–20 (1)|13–20 (1)|
> |Mythic|8–24 (1)|18–25 (1)|18–25 (1)|
> |Ancient|10–30 (1)|23–30 (1)|23–30 (1)|
>
> **Selection Weight:** Minimal 1 · Balanced 5.5 · Legendary 10

## ModifySummonDamage

> **Display Text:** Summons Damage +{0:0.#}%
>
> **Description:** Increases the damage of summoned creatures by +**X**%.
>
> **Requirements:**
> * **AllowedSkillTypes:** `BloodMagic`
> * **AllowedStaffRoles:** `Summon`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–4 (1)|1–5 (1)|1–5 (1)|
> |Rare|2–8 (1)|3–10 (1)|3–10 (1)|
> |Epic|3–10 (1)|8–15 (1)|8–15 (1)|
> |Legendary|4–12 (1)|13–20 (1)|13–20 (1)|
> |Mythic|5–15 (1)|18–25 (1)|18–25 (1)|
> |Ancient|6–18 (1)|23–30 (1)|23–30 (1)|
>
> **Selection Weight:** Minimal 1 · Balanced 5.5 · Legendary 10

## AddElementalMagicSkill

> **Display Text:** Elemental Magic Skill +{0}
>
> **Description:** Increases the player's elemental magic skill level by +**X**. This may cause the skill level to increase above 100.
>
> **Requirements:**
> * **AllowedSkillTypes:** `ElementalMagic`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–3 (1)|1–6 (1)|
> |Rare|2–6 (1)|2–6 (1)|2–9 (1)|
> |Epic|2–8 (1)|3–9 (1)|4–12 (1)|
> |Legendary|3–10 (1)|4–12 (1)|6–18 (1)|
> |Mythic|4–13 (1)|5–15 (1)|9–25 (1)|
> |Ancient|5–16 (1)|6–18 (1)|12–32 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 2 · Legendary 10

## AddUnarmedSkill

> **Display Text:** Unarmed Skill +{0}
>
> **Description:** Increases the player's unarmed skill level by +**X**. This may cause the skill level to increase above 100.
>
> **Requirements:**
> * **AllowedSkillTypes:** `Unarmed`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–3 (1)|2–5 (1)|
> |Rare|2–6 (1)|2–6 (1)|2–9 (1)|
> |Epic|4–9 (1)|4–9 (1)|4–12 (1)|
> |Legendary|6–12 (1)|6–12 (1)|6–18 (1)|
> |Mythic|9–15 (1)|9–15 (1)|14–23 (1)|
> |Ancient|12–18 (1)|12–18 (1)|18–27 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 5 · Legendary 10

## AddPickaxesSkill

> **Display Text:** Pickaxes Skill +{0}
>
> **Description:** Increases the player's pickaxes skill level by +**X**. This may cause the skill level to increase above 100.
>
> **Requirements:**
> * **AllowedSkillTypes:** `Pickaxes`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–3 (1)|1–6 (1)|
> |Rare|2–6 (1)|2–6 (1)|2–9 (1)|
> |Epic|2–8 (1)|3–9 (1)|4–12 (1)|
> |Legendary|3–10 (1)|4–12 (1)|6–18 (1)|
> |Mythic|4–13 (1)|5–15 (1)|9–25 (1)|
> |Ancient|5–16 (1)|6–18 (1)|12–32 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 5 · Legendary 10

## AddFishingSkill

> **Display Text:** Fishing Skill +{0}
>
> **Description:** Increases the player's fishing skill level by +**X**. This may cause the skill level to increase above 100.
>
> **Requirements:**
> * **AllowedItemNames:** `$item_fishingrod`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–3 (1)|1–6 (1)|
> |Rare|2–6 (1)|2–6 (1)|2–9 (1)|
> |Epic|2–8 (1)|3–9 (1)|4–12 (1)|
> |Legendary|3–10 (1)|4–12 (1)|6–18 (1)|
> |Mythic|4–13 (1)|5–15 (1)|9–25 (1)|
> |Ancient|5–16 (1)|6–18 (1)|12–32 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 5 · Legendary 10

## ModifyWispRange

> **Display Text:** Demisting Range +{0:0.#}%
>
> **Description:** Increases the demisting range by +**X**%.
>
> **Requirements:**
> * **AllowedItemNames:** `$item_demister`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|5–20 (5)|25–50 (5)|40–75 (5)|
> |Rare|15–30 (5)|50–75 (5)|75–115 (5)|
> |Epic|20–40 (5)|75–100 (5)|115–150 (5)|
> |Legendary|30–60 (5)|100–150 (5)|150–225 (5)|
> |Mythic|40–80 (5)|150–200 (5)|225–300 (5)|
> |Ancient|50–100 (5)|200–250 (5)|300–375 (5)|
>
> **Selection Weight:** Minimal 1 · Balanced 5.5 · Legendary 10

## ModifyGrapplePullSpeed

> **Display Text:** Grapple Pull Speed +{0:0.#}%
>
> **Description:** Increases how fast the grappling hook reels you in by +**X**%.
>
> **Requirements:**
> * **AllowedItemNames:** `$item_graplinghook`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|10–20 (1)|10–20 (1)|10–20 (1)|
> |Rare|15–25 (1)|15–25 (1)|15–25 (1)|
> |Epic|20–35 (1)|20–35 (1)|20–35 (1)|
> |Legendary|30–45 (1)|30–45 (1)|30–45 (1)|
> |Mythic|40–60 (1)|40–60 (1)|40–60 (1)|
> |Ancient|50–75 (1)|50–75 (1)|50–75 (1)|
>
> **Selection Weight:** 10

## ModifyGrappleFlightTime

> **Display Text:** Grapple Flight Time +{0:0.#}%
>
> **Description:** Keeps the grappling hook in flight +**X**% longer, reaching farther on a higher arc.
>
> **Requirements:**
> * **AllowedItemNames:** `$item_graplinghook`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|10–20 (1)|10–20 (1)|10–20 (1)|
> |Rare|15–30 (1)|15–30 (1)|15–30 (1)|
> |Epic|25–40 (1)|25–40 (1)|25–40 (1)|
> |Legendary|35–55 (1)|35–55 (1)|35–55 (1)|
> |Mythic|50–75 (1)|50–75 (1)|50–75 (1)|
> |Ancient|60–100 (1)|60–100 (1)|60–100 (1)|
>
> **Selection Weight:** 10

## AddMovementSkills

> **Display Text:** Movement Skills +{0}
>
> **Description:** Increases the player's run, jump, swim, and sneak skill levels by +**X**. This may cause the skills' level to increase above 100.
>
> **Requirements:**
> * **AllowedItemTypes:** `Legs, Utility, Trinket, Chest, Shoulder`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (1)|1–3 (1)|1–6 (1)|
> |Rare|2–6 (1)|2–6 (1)|2–9 (1)|
> |Epic|2–8 (1)|3–9 (1)|4–12 (1)|
> |Legendary|3–10 (1)|4–12 (1)|6–18 (1)|
> |Mythic|4–13 (1)|5–15 (1)|9–25 (1)|
> |Ancient|5–16 (1)|6–18 (1)|12–32 (1)|
>
> **Selection Weight:** Minimal 1 · Balanced 5.5 · Legendary 10

## ModifyStaggerDuration

> **Display Text:** Stagger Duration +{0:0.#}%
>
> **Description:** Increase the stagger duration by **X**% against enemies when staggered by attacks from the magic weapon.
>
> **Requirements:**
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow, Shield`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_fishingrod, $item_graplinghook`
> * **AllowedStaffRoles:** `Damage`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (0.5)|1–3 (0.5)|1–3 (0.5)|
> |Rare|2–6 (0.5)|2–6 (0.5)|2–6 (0.5)|
> |Epic|4–9 (1)|4–9 (0.5)|4–9 (1)|
> |Legendary|6–12 (1)|6–12 (0.5)|6–12 (1)|
> |Mythic|9–15 (1)|9–15 (0.5)|9–15 (1)|
> |Ancient|12–18 (1)|12–18 (0.5)|12–18 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 3 · Legendary 10

## QuickLearner

> **Display Text:** Quick Learner +{0:0.#}%
>
> **Description:** All skills improve **X**% faster.
>
> **Requirements:**
> * **AllowedItemTypes:** `Helmet`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (1)|1–3 (0.5)|1–4 (1)|
> |Rare|1–4 (1)|2–5 (0.5)|2–8 (1)|
> |Epic|2–6 (1)|3–7 (0.5)|6–12 (1)|
> |Legendary|3–9 (1)|4–10 (0.5)|10–16 (1)|
> |Mythic|4–13 (1)|5–15 (0.5)|14–20 (1)|
> |Ancient|5–18 (1)|6–20 (0.5)|18–24 (1)|
>
> **Selection Weight:** 5

## FreeBuild

> **Display Text:** Free Build
>
> **Description:** When using the magic hammer, buildings can be built without the required crafting station nearby.
>
> **Requirements:**
> * **AllowedRarities:** `Mythic, Ancient`
> * **AllowedItemNames:** `$item_hammer, $item_hoe`
> * *Minimal:* AllowedRarities drops `Mythic`.
> * *Legendary:* AllowedRarities adds `Legendary`.
>
> **Selection Weight:** Minimal 0.1 · Balanced 2.55 · Legendary 5

## GreenThumb

> **Display Text:** Plant Grow Time -{0:0.#}%
>
> **Description:** Seeds and saplings you plant take {0}% less time to grow. The bonus is set when the plant goes in and stays with it.
>
> **Requirements:**
> * **AllowedItemNames:** `$item_cultivator`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|2–6 (1)|5–10 (1)|5–15 (1)|
> |Rare|4–10 (1)|10–15 (1)|10–20 (1)|
> |Epic|6–15 (1)|15–20 (1)|15–30 (1)|
> |Legendary|8–20 (1)|20–30 (1)|25–40 (1)|
> |Mythic|10–25 (1)|30–40 (1)|35–50 (1)|
> |Ancient|12–30 (1)|40–50 (1)|45–60 (1)|
>
> **Selection Weight:** 10
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Cap|`Max`|75|

## RecallWeapon

> **Display Text:** Recalling
>
> **Description:** The thrown magic weapon automatically returns to the player after being thrown.
>
> **Requirements:**
> * **MustHaveEffectTypes:** `Throwable`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon`
> * **ExcludedItemTypes:** `Staff`
> * **ExcludedSkillTypes:** `Pickaxes`
>
> **Selection Weight:** 5

## HomingMissile

> **Display Text:** Homing Missile {0:0} m
>
> **Description:** When thrown, the magic weapon locks onto the enemy nearest your aim within **X** metres and steers toward it, turning ever more sharply the longer it flies.
>
> **Requirements:**
> * **MustHaveEffectTypes:** `Throwable`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon`
> * **ExcludedItemTypes:** `Staff`
> * **AllowedRarities:** `Mythic, Ancient`
> * **ExcludedSkillTypes:** `Pickaxes`
> * *Minimal:* AllowedRarities drops `Mythic`.
> * *Legendary:* AllowedRarities adds `Legendary`.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|6–8 (1)|6–8 (1)|6–8 (1)|
> |Rare|8–10 (1)|8–10 (1)|8–10 (1)|
> |Epic|10–12 (1)|10–12 (1)|10–12 (1)|
> |Legendary|12–15 (1)|12–15 (1)|12–15 (1)|
> |Mythic|15–18 (1)|15–18 (1)|15–18 (1)|
> |Ancient|18–20 (1)|18–20 (1)|18–20 (1)|
>
> **Selection Weight:** Minimal 0.2 · Balanced 2.6 · Legendary 5
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Lock-on Cone (degrees)|`AcquireAngle`|25|
> |Lose Lock Beyond (x range)|`LoseRangeMultiplier`|1.6|
> |Turn Rate at Launch (deg/s)|`StartTurnRate`|45|
> |Turn Rate when Ramped (deg/s)|`FullTurnRate`|1440|
> |Ramp Time (s)|`RampSeconds`|3|

## Apportation

> **Display Text:** Apportation
>
> **Description:** Teleports player to thrown weapon when an enemy is hit.
>
> **Requirements:**
> * **MustHaveEffectTypes:** `Throwable`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon`
> * **ExcludedItemTypes:** `Staff`
> * **ExcludedSkillTypes:** `Pickaxes`
>
> **Selection Weight:** 1

## OffSetAttack

> **Display Text:** Offset Attack +{0}%
>
> **Description:** Your third attack in an attack string provides stagger immunity and damage reduction if timed against an enemy melee attack.
>
> **Requirements:**
> * **Flags:** `ItemUsesStaminaOnAttack`
> * **AllowedItemTypes:** `Helmet, Shoulder, Utility, Trinket, OneHandedWeapon, TwoHandedWeapon`
> * **ExcludedSkillTypes:** `BloodMagic`
> * **ExcludedItemNames:** `$item_fishingrod`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|15–20 (1)|15–20 (1)|15–20 (1)|
> |Rare|20–25 (1)|20–25 (1)|20–25 (1)|
> |Epic|25–30 (1)|25–30 (1)|25–30 (1)|
> |Legendary|30–35 (1)|30–35 (1)|30–35 (1)|
> |Mythic|35–40 (1)|35–40 (1)|35–40 (1)|
> |Ancient|40–45 (1)|40–45 (1)|40–45 (1)|
>
> **Selection Weight:** 1

## ReflectDamage

> **Display Text:** Thorn Damage +{0:0.#}%
>
> **Description:** Reflect **X**% of damage back to the attacker.
>
> **Requirements:**
> * **AllowedItemTypes:** `Chest, Shield`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|2–4 (1)|2–4 (0.5)|2–4 (1)|
> |Rare|3–6 (1)|3–6 (0.5)|3–6 (1)|
> |Epic|3–7 (1)|4–8 (0.5)|5–8 (1)|
> |Legendary|4–9 (1)|5–10 (0.5)|7–10 (1)|
> |Mythic|5–10 (1)|6–12 (0.5)|9–12 (1)|
> |Ancient|6–12 (1)|7–14 (0.5)|11–14 (1)|
>
> **Selection Weight:** 10

## AvoidDamageTaken

> **Display Text:** Feint {0:0.#}%
>
> **Description:** The player has a **X**% chance to automatically avoid all damage from an attack.
>
> **Requirements:**
> * **AllowedItemTypes:** `Chest, Shield`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–4 (1)|2–4 (0.5)|2–4 (1)|
> |Rare|1–6 (1)|3–6 (0.5)|3–8 (1)|
> |Epic|1–8 (1)|4–8 (0.5)|6–12 (1)|
> |Legendary|1–10 (1)|5–10 (0.5)|10–16 (1)|
> |Mythic|1–12 (1)|6–12 (0.5)|14–20 (1)|
> |Ancient|1–14 (1)|7–14 (0.5)|18–24 (1)|
>
> **Selection Weight:** 2

## StaggerOnDamageTaken

> **Display Text:** Stagger Chance +{0:0.#}%
>
> **Description:** Increase the chance to stagger the enemy by **X**% for attacks made with the magic weapon.
>
> **Requirements:**
> * **AllowedItemTypes:** `Chest, Shield, TwoHandedWeapon`
> * **AllowedStaffRoles:** `Damage`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (1)|1–2 (0.5)|2–3 (1)|
> |Rare|1–3 (1)|2–4 (0.5)|3–6 (1)|
> |Epic|2–4 (1)|3–6 (0.5)|5–9 (1)|
> |Legendary|2–5 (1)|4–8 (0.5)|5–12 (1)|
> |Mythic|3–6 (1)|5–10 (0.5)|5–15 (1)|
> |Ancient|4–7 (1)|6–12 (0.5)|6–18 (1)|
>
> **Selection Weight:** Minimal 5 · Balanced 1 · Legendary 5

## FeatherFall

> **Display Text:** Feather Fall
>
> **Description:** The player falls slowly and is immune to fall damage.
>
> **Requirements:**
> * **AllowedItemTypes:** `Legs`
> * **AllowedRarities:** `Epic, Legendary, Mythic, Ancient`
> * *Minimal:* AllowedRarities drops `Epic, Legendary`.
> * *Legendary:* no AllowedRarities.
>
> **Selection Weight:** Minimal 0.2 · Balanced 0.6 · Legendary 1

## ModifyDiscoveryRadius

> **Display Text:** Discovery Radius +{0:0.#}%
>
> **Description:** The player's map discovery radius is increased by **X**%.
>
> **Requirements:**
> * **AllowedItemTypes:** `Helmet`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–4 (1)|1–5 (1)|1–10 (1)|
> |Rare|4–10 (1)|4–10 (1)|4–15 (1)|
> |Epic|5–13 (1)|6–15 (1)|8–20 (1)|
> |Legendary|7–18 (1)|8–20 (1)|10–30 (1)|
> |Mythic|9–22 (1)|10–25 (1)|14–40 (1)|
> |Ancient|10–26 (1)|12–30 (1)|18–50 (1)|
>
> **Selection Weight:** 10

## Comfortable

> **Display Text:** Comfort +{0}
>
> **Description:** When resting and wearing the magic equipment, the player's comfort level is increased by +**X**.
>
> **Requirements:**
> * **AllowedItemTypes:** `Utility, Trinket, Chest, Legs, Shoulder, Helmet`
> * **AllowedRarities:** `Epic, Legendary, Mythic, Ancient`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Epic|1|1|1|
> |Legendary|1|1–2 (1)|2–3 (1)|
> |Mythic|1–2 (1)|2–3 (1)|3–5 (1)|
> |Ancient|2–3 (1)|3–4 (1)|5–6 (1)|
>
> **Selection Weight:** Minimal 0.5 · Balanced 5.25 · Legendary 10

## ModifyMovementSpeedLowHealth

> **Display Text:** Movement Speed +{0:0.#}% (below {1:0.#}% health)
>
> **Description:** The player's movement speed is increased by **X**% when below {1:0.#}% health.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `RemoveSpeedPenalty`
> * **AllowedItemTypes:** `Legs, Utility, Trinket, Tool, Torch, TwoHandedWeaponLeft`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–6 (1)|1–6 (1)|1–6 (1)|
> |Rare|4–9 (1)|4–9 (1)|4–9 (1)|
> |Epic|7–12 (1)|7–12 (1)|7–12 (1)|
> |Legendary|10–15 (1)|10–15 (1)|10–15 (1)|
> |Mythic|13–18 (1)|13–18 (1)|13–18 (1)|
> |Ancient|16–21 (1)|16–21 (1)|16–21 (1)|
>
> **Selection Weight:** 10

## ModifyHealthRegenLowHealth

> **Display Text:** Health Regen +{0:0.#}% (below {1:0.#}% health)
>
> **Description:** The player's health regen is increased by **X**% when below {1:0.#}% health.
>
> **Requirements:**
> * **AllowedItemTypes:** `Helmet, Shoulder, Chest, Legs, Utility, Trinket, Shield, Tool, Torch, TwoHandedWeaponLeft`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|4–8 (1)|4–8 (1)|4–8 (1)|
> |Rare|6–10 (1)|6–13 (1)|6–13 (1)|
> |Epic|8–12 (1)|11–18 (1)|11–18 (1)|
> |Legendary|10–14 (1)|16–23 (1)|16–23 (1)|
> |Mythic|12–16 (1)|21–28 (1)|21–28 (1)|
> |Ancient|14–18 (1)|26–33 (1)|26–33 (1)|
>
> **Selection Weight:** 10

## ModifyStaminaRegenLowHealth

> **Display Text:** Stamina Regen +{0:0.#}% (below {1:0.#}% health)
>
> **Description:** The player's stamina regen is increased by **X**% when below {1:0.#}% health.
>
> **Requirements:**
> * **AllowedItemTypes:** `Legs, Utility, Trinket, Tool, Helmet, Chest, Shoulder, Shield, Torch, TwoHandedWeaponLeft`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|4–8 (1)|4–8 (1)|4–8 (1)|
> |Rare|6–10 (1)|6–13 (1)|6–13 (1)|
> |Epic|8–12 (1)|11–18 (1)|11–18 (1)|
> |Legendary|10–14 (1)|16–23 (1)|16–23 (1)|
> |Mythic|12–16 (1)|21–28 (1)|21–28 (1)|
> |Ancient|14–18 (1)|26–33 (1)|26–33 (1)|
>
> **Selection Weight:** 10

## ModifyEitrRegenLowHealth

> **Display Text:** Eitr Regen +{0:0.#}% (below {1:0.#}% health)
>
> **Description:** The player's eitr regen is increased by **X**% when below {1:0.#}% health.
>
> **Requirements:**
> * **AllowedItemTypes:** `Shoulder, Staff, Helmet, Chest, Legs, Utility, Trinket, Shield, Tool, Torch, TwoHandedWeaponLeft`
> * **ExcludedItemNames:** `$item_hammer, $item_hoe, $item_cultivator, $item_fishingrod`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|4–8 (1)|4–10 (1)|6–15 (1)|
> |Rare|6–10 (1)|6–15 (1)|9–23 (1)|
> |Epic|8–12 (1)|11–20 (1)|17–30 (1)|
> |Legendary|10–14 (1)|16–30 (1)|24–45 (1)|
> |Mythic|12–16 (1)|25–40 (1)|38–60 (1)|
> |Ancient|14–18 (1)|34–50 (1)|51–75 (1)|
>
> **Selection Weight:** 10

## ModifyArmorLowHealth

> **Display Text:** Armor +{0:0.#}% (below {1:0.#}% health)
>
> **Description:** The player's total armor is increased by **X**% when below {1:0.#}% health.
>
> **Requirements:**
> * **AllowedItemTypes:** `Chest, Utility, Trinket`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|4–8 (1)|4–9 (1)|4–9 (1)|
> |Rare|6–10 (1)|7–12 (1)|7–12 (1)|
> |Epic|8–12 (1)|9–15 (1)|9–15 (1)|
> |Legendary|10–14 (1)|12–18 (1)|12–18 (1)|
> |Mythic|12–16 (1)|15–21 (1)|15–21 (1)|
> |Ancient|14–18 (1)|18–24 (1)|18–24 (1)|
>
> **Selection Weight:** Minimal 10 · Balanced 2 · Legendary 10

## ModifyDamageLowHealth

> **Display Text:** Damage +{0:0.#}% (below {1:0.#}% health)
>
> **Description:** The player's total damage is increased by **X**% when below {1:0.#}% health.
>
> **Requirements:**
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow, Torch, Staff`
> * **ExcludedItemNames:** `$item_graplinghook`
> * **AllowedStaffRoles:** `Damage`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|4–8 (1)|4–9 (1)|4–9 (1)|
> |Rare|6–10 (1)|7–12 (1)|7–12 (1)|
> |Epic|8–12 (1)|9–15 (1)|9–15 (1)|
> |Legendary|10–14 (1)|12–18 (1)|12–18 (1)|
> |Mythic|12–16 (1)|15–21 (1)|15–21 (1)|
> |Ancient|14–18 (1)|18–24 (1)|18–24 (1)|
>
> **Selection Weight:** 10

## ModifyBlockPowerLowHealth

> **Display Text:** Block +{0:0.#}% (below {1:0.#}% health)
>
> **Description:** The player's block armor is increased by **X**% when below {1:0.#}% health.
>
> **Requirements:**
> * **Flags:** `ItemHasBlockPower`
> * **ExclusiveEffectTypes:** `Duelist`
> * **AllowedItemTypes:** `Shield, OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Staff`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|4–8 (1)|4–9 (1)|4–9 (1)|
> |Rare|6–10 (1)|7–12 (1)|7–12 (1)|
> |Epic|8–12 (1)|9–15 (1)|9–15 (1)|
> |Legendary|10–14 (1)|12–18 (1)|12–18 (1)|
> |Mythic|12–16 (1)|15–21 (1)|15–21 (1)|
> |Ancient|14–18 (1)|18–24 (1)|18–24 (1)|
>
> **Selection Weight:** 10

## ModifyParryLowHealth

> **Display Text:** Parry {0:0.#}% (below {1:0.#}% health)
>
> **Description:** The player's parry block armor, parry force, and parry bonus are increased by **X**% when below {1:0.#}% health.
>
> **Requirements:**
> * **Flags:** `ItemHasParryPower`
> * **ExclusiveEffectTypes:** `Duelist`
> * **AllowedItemTypes:** `Shield, OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_fishingrod`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|4–8 (1)|4–9 (1)|4–9 (1)|
> |Rare|6–10 (1)|7–12 (1)|7–12 (1)|
> |Epic|8–12 (1)|9–15 (1)|9–15 (1)|
> |Legendary|10–14 (1)|12–18 (1)|12–18 (1)|
> |Mythic|12–16 (1)|15–21 (1)|15–21 (1)|
> |Ancient|14–18 (1)|18–24 (1)|18–24 (1)|
>
> **Selection Weight:** 10

## ModifyAttackSpeedLowHealth

> **Display Text:** Attack Speed {0:0.#}% (below {1:0.#}% health)
>
> **Description:** The player's attack speed is increased by **X**% when below {1:0.#}% health.
>
> **Requirements:**
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Tool, Staff`
> * **ExcludedItemNames:** `$item_fishingrod, $item_hammer, $item_hoe, $item_cultivator`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|4–8 (1)|4–9 (1)|4–9 (1)|
> |Rare|6–10 (1)|7–12 (1)|7–12 (1)|
> |Epic|8–12 (1)|9–15 (1)|9–15 (1)|
> |Legendary|10–14 (1)|12–18 (1)|12–18 (1)|
> |Mythic|12–16 (1)|15–21 (1)|15–21 (1)|
> |Ancient|14–18 (1)|18–24 (1)|18–24 (1)|
>
> **Selection Weight:** 10

## AvoidDamageTakenLowHealth

> **Display Text:** Feint Increased by {0:0.#}% (below {1:0.#}% health)
>
> **Description:** The player's chance to avoid incoming attacks is increased by **X**% when below {1:0.#}% health.
>
> **Requirements:**
> * **AllowedItemTypes:** `Chest, Shield`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|3–6 (1)|4–7 (1)|4–7 (1)|
> |Rare|4–9 (1)|5–11 (1)|5–11 (1)|
> |Epic|8–12 (1)|9–15 (1)|9–15 (1)|
> |Legendary|10–14 (1)|13–19 (1)|13–19 (1)|
> |Mythic|12–16 (1)|17–23 (1)|17–23 (1)|
> |Ancient|14–18 (1)|21–27 (1)|21–27 (1)|
>
> **Selection Weight:** 10

## LifeStealLowHealth

> **Display Text:** Lifesteal {0:0.0}% (below {1:0.#}% health)
>
> **Description:** The player heals for **X**% of attack damage when below {1:0.#}% health.
>
> **Requirements:**
> * **Flags:** `ItemUsesStaminaOnAttack`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft, Bow`
> * **AllowedRarities:** `Rare, Epic, Legendary, Mythic, Ancient`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_fishingrod, $item_graplinghook`
> * **AllowedStaffRoles:** `Damage`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Rare|1–2.5 (0.5)|2–8 (0.5)|3–12 (0.5)|
> |Epic|2–3.5 (0.5)|3–10 (0.5)|4.5–15 (0.5)|
> |Legendary|3–4.5 (0.5)|4–12 (0.5)|6–18 (0.5)|
> |Mythic|4–5.5 (0.5)|5–15 (0.5)|7.5–22.5 (0.5)|
> |Ancient|5–6.5 (0.5)|6–18 (0.5)|9–27 (0.5)|
>
> **Selection Weight:** 10

## Glowing

> **Display Text:** Glowing
>
> **Description:** The player glows with a faint light.
>
> **Requirements:**
> * **AllowedItemTypes:** `Helmet, Chest, Legs, Shoulder, Utility, Trinket`
>
> **Selection Weight:** 5 · **Equip FX:** `GlowingAura`

## Executioner

> **Display Text:** Executioner +{0}%
>
> **Description:** The first attack on an enemy that is below 20% health deals +**X**% damage.
>
> **Requirements:**
> * **Flags:** `ItemUsesStaminaOnAttack`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow`
> * **AllowedRarities:** `Epic, Legendary, Mythic, Ancient`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_fishingrod, $item_graplinghook`
> * **AllowedStaffRoles:** `Damage`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Epic|20–100 (5)|50–100 (5)|50–150 (5)|
> |Legendary|40–150 (5)|100–150 (5)|100–200 (5)|
> |Mythic|80–200 (5)|150–200 (5)|150–300 (5)|
> |Ancient|100–250 (5)|200–250 (5)|300–375 (5)|
>
> **Selection Weight:** Minimal 0.2 · Balanced 5.1 · Legendary 10

## Riches

> **Display Text:** Riches +{0:0.#}%
>
> **Description:** Slain enemies have a +**X**% increased chance to drop coins or treasure.
>
> **Requirements:**
> * **AllowedItemTypes:** `Utility, Trinket`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|1–2 (0.5)|1–2 (0.5)|
> |Rare|1–3 (0.5)|2–4 (0.5)|2–4 (0.5)|
> |Epic|1–4 (0.5)|3–6 (0.5)|4–6 (0.5)|
> |Legendary|1–5 (0.5)|4–8 (0.5)|6–8 (0.5)|
> |Mythic|1–6 (0.5)|5–10 (0.5)|8–10 (0.5)|
> |Ancient|1–7 (0.5)|6–12 (0.5)|10–12 (0.5)|
>
> **Selection Weight:** Minimal 1 · Balanced 5.5 · Legendary 10
>
> **Config:**
>
> |Setting|Key|Minimal|Balanced|Legendary|
> |--|--|--|--|--|
> |Silver Necklace Value|`SilverNecklace`|900|300|30|
> |Ruby Value|`Ruby`|800|200|20|
> |Amber Pearl Value|`AmberPearl`|700|100|10|
> |Amber Value|`Amber`|600|50|5|
> |Coin Value|`Coins`|10|8|1|

## Opportunist

> **Display Text:** Opportunist +{0:0.#}%
>
> **Description:** Attacks made against staggered enemies have a +**X**% chance to deal backstab damage.
>
> **Requirements:**
> * **AllowedRarities:** `Rare, Epic, Legendary, Mythic, Ancient`
> * **AllowedSkillTypes:** `Knives`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Rare|2–4 (1)|4–8 (0.5)|6–10 (1)|
> |Epic|2–8 (1)|6–12 (0.5)|8–16 (1)|
> |Legendary|4–12 (1)|8–16 (0.5)|12–24 (1)|
> |Mythic|4–16 (1)|10–20 (0.5)|16–32 (1)|
> |Ancient|4–20 (1)|12–24 (0.5)|20–40 (1)|
>
> **Selection Weight:** 10

## Duelist

> **Display Text:** Duelist +{0:0.#}%
>
> **Description:** When using the magic weapon without anything in the player's off hand, its block armor is increased by +**X**% of the weapon's attack power, and its parry force is increased by +**X/2**% of the weapon's attack power.
>
> **Requirements:**
> * **Flags:** `ItemUsesStaminaOnAttack`
> * **ExclusiveEffectTypes:** `ModifyBlockPower, ModifyBlockPowerLowHealth, ModifyParry, ModifyParryLowHealth, SpellSword, EitrWeave`
> * **AllowedItemTypes:** `OneHandedWeapon`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|3–7 (1)|4–8 (0.5)|6–12 (1)|
> |Rare|5–10 (1)|6–12 (0.5)|9–18 (1)|
> |Epic|7–14 (1)|8–16 (0.5)|12–24 (1)|
> |Legendary|9–18 (1)|10–20 (0.5)|15–30 (1)|
> |Mythic|10–21 (1)|12–24 (0.5)|18–36 (1)|
> |Ancient|12–25 (1)|14–28 (0.5)|21–42 (1)|
>
> **Selection Weight:** Minimal 0.5 · Balanced 5.25 · Legendary 10

## Immovable

> **Display Text:** Immovable
>
> **Description:** The player is immune to knockback and stagger while blocking with the magic shield.
>
> **Requirements:**
> * **Flags:** `ItemHasNoParryPower`
> * **AllowedItemTypes:** `Shield`
>
> **Selection Weight:** 5

## ModifyStaggerDamage

> **Display Text:** Stagger Damage +{0:0.#}%
>
> **Description:** Increase damage to staggered enemies by +**X**%.
>
> **Requirements:**
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_fishingrod, $item_graplinghook`
> * **AllowedStaffRoles:** `Damage`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–6 (1)|2–6 (0.5)|2–6 (1)|
> |Rare|3–9 (1)|3–9 (0.5)|3–9 (1)|
> |Epic|3–10 (1)|4–12 (0.5)|6–12 (1)|
> |Legendary|4–13 (1)|5–15 (0.5)|9–15 (1)|
> |Mythic|12–18 (1)|12–18 (0.5)|12–18 (1)|
> |Ancient|15–21 (1)|19–21 (0.5)|19–21 (1)|
>
> **Selection Weight:** 10

## CloseQuarter

> **Display Text:** Knockback -{0:0.#}%
>
> **Description:** Hits with this weapon knock enemies back {0}% less, keeping them within reach.
>
> **Requirements:**
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft`
> * **ExcludedSkillTypes:** `Pickaxes, ElementalMagic, BloodMagic`
> * **ExcludedItemNames:** `$item_fishingrod`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|20–30 (1)|20–30 (1)|20–30 (1)|
> |Rare|30–40 (1)|30–40 (1)|30–40 (1)|
> |Epic|40–55 (1)|40–55 (1)|40–55 (1)|
> |Legendary|55–70 (1)|55–70 (1)|55–70 (1)|
> |Mythic|70–85 (1)|70–85 (1)|70–85 (1)|
> |Ancient|85–100 (1)|85–100 (1)|85–100 (1)|
>
> **Selection Weight:** 10

## Luck

> **Display Text:** Luck +{0}
>
> **Description:** Increase the chance to find higher rarity magic items.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `Prosperity`
> * **AllowedItemTypes:** `Utility`
> * **AllowedRarities:** `Epic, Legendary, Mythic, Ancient`
> * *Legendary:* no AllowedRarities.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (1)|1–3 (1)|1–9 (1)|
> |Rare|2–5 (1)|3–6 (1)|3–12 (1)|
> |Epic|5–8 (1)|6–9 (1)|9–14 (1)|
> |Legendary|8–10 (1)|9–12 (1)|14–18 (1)|
> |Mythic|10–13 (1)|12–15 (1)|18–23 (1)|
> |Ancient|13–16 (1)|15–18 (1)|23–27 (1)|
>
> **Selection Weight:** 5

## Prosperity

> **Display Text:** Prosperity +{0}%
>
> **Description:** Creatures slain and chests opened near you have a {0}% chance to roll their magic loot an extra time. Every 100% is a guaranteed extra roll. Stacks with nearby players.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `Luck`
> * **AllowedItemTypes:** `Trinket`
> * **AllowedRarities:** `Epic, Legendary, Mythic, Ancient`
> * *Legendary:* no AllowedRarities.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (1)|1–3 (1)|1–9 (1)|
> |Rare|2–5 (1)|3–6 (1)|3–12 (1)|
> |Epic|5–8 (1)|6–9 (1)|9–14 (1)|
> |Legendary|8–10 (1)|9–12 (1)|14–18 (1)|
> |Mythic|10–13 (1)|12–15 (1)|18–23 (1)|
> |Ancient|13–16 (1)|15–18 (1)|23–27 (1)|
>
> **Selection Weight:** 5
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Max Bonus Loot Rolls|`BonusRollsMax`|3|

## ModifyParryWindow

> **Display Text:** Parry Window +{0} ms
>
> **Description:** Increase the time window for blocks to count as a parry by +**X** ms.
>
> **Requirements:**
> * **Flags:** `ItemHasParryPower`
> * **AllowedItemTypes:** `Shield, OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft`
> * **ExcludedItemNames:** `$item_fishingrod`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|40–60 (1)|40–60 (1)|40–60 (1)|
> |Rare|50–70 (1)|50–70 (1)|50–70 (1)|
> |Epic|60–80 (1)|60–80 (1)|60–80 (1)|
> |Legendary|70–90 (1)|70–90 (1)|70–90 (1)|
> |Mythic|80–100 (1)|80–100 (1)|80–100 (1)|
> |Ancient|90–100 (1)|90–100 (1)|90–100 (1)|
>
> **Selection Weight:** 10

## Slow

> **Display Text:** Slow +{0:0.#}%
>
> **Description:** Slows the attack and movement speed of creatures being hit by +**X**% for 2 seconds. Bosses are immune to this effect.
>
> **Requirements:**
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, Bow`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_fishingrod, $item_graplinghook`
> * **AllowedStaffRoles:** `Damage`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–2 (0.5)|4–8 (0.5)|4–8 (1)|
> |Rare|2–4 (0.5)|6–10 (0.5)|8–16 (1)|
> |Epic|4–8 (0.5)|8–12 (0.5)|16–24 (1)|
> |Legendary|6–12 (0.5)|10–16 (0.5)|24–32 (1)|
> |Mythic|8–16 (0.5)|12–20 (0.5)|32–40 (1)|
> |Ancient|10–20 (0.5)|14–24 (0.5)|40–48 (1)|
>
> **Selection Weight:** 2

## DoubleMagicShot

> **Display Text:** Double Magic Shot
>
> **Description:** Elemental Magic attacks fire twice and use twice the amount of eitr.
>
> **Requirements:**
> * **AllowedRarities:** `Epic, Legendary, Mythic, Ancient`
> * **AllowedSkillTypes:** `ElementalMagic`
>
> **Selection Weight:** 2
>
> **Config:**
>
> |Setting|Key|Minimal|Balanced|Legendary|
> |--|--|--|--|--|
> |Extra Shot Chance|`Chance`|0.5|0.75|1|
> |Damage Multiplier|`Damage`|0.6|0.66|1|
> |Spread Multiplier|`Accuracy`|1.2|1.2|1.1|
> |Projectile Multiplier|`Projectiles`|2|2|2|
> |Attack Cost Multiplier|`CostScale`|2|2|1.2|

## TripleBowShot

> **Display Text:** Triple Shot: {0:0}% damage per arrow
>
> **Description:** Bow attacks always fire three projectiles, each dealing **X**% of the shot's damage. Higher values also keep the arrows closer together.
>
> **Requirements:**
> * **ExclusiveEffectTypes:** `QuickDraw, ExplosiveArrows`
> * **AllowedItemTypes:** `Bow`
> * **AllowedRarities:** `Epic, Legendary, Mythic, Ancient`
> * **ExcludedItemNames:** `$item_graplinghook`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Epic|34–36 (1)|40–47 (1)|50–62 (1)|
> |Legendary|36–37 (1)|47–53 (1)|62–75 (1)|
> |Mythic|37–39 (1)|53–60 (1)|75–88 (1)|
> |Ancient|39–40 (1)|60–67 (1)|88–100 (1)|
>
> **Selection Weight:** 2
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Spread Multiplier|`Accuracy`|1.5|
> |Projectile Multiplier|`Projectiles`|3|
> |Attack Cost Multiplier|`CostScale`|2|

## SpellSword

> **Display Text:** Spellblade +{0}
>
> **Description:** Converts 50% of the attack's cost to eitr and increases damage by +**X%**.
>
> **Requirements:**
> * **Flags:** `ItemUsesStaminaOnAttack`
> * **ExclusiveEffectTypes:** `Duelist`
> * **AllowedItemTypes:** `OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft`
> * **AllowedRarities:** `Rare, Epic, Legendary, Mythic, Ancient`
> * **ExcludedSkillTypes:** `BloodMagic, ElementalMagic, Pickaxes`
> * **ExcludedItemNames:** `$item_fishingrod`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Rare|1–8 (0.5)|4–10 (0.5)|6–15 (0.5)|
> |Epic|3–10 (0.5)|6–15 (0.5)|9–22.5 (0.5)|
> |Legendary|7–15 (0.5)|8–20 (0.5)|12–30 (0.5)|
> |Mythic|10–20 (0.5)|10–25 (0.5)|15–37.5 (0.5)|
> |Ancient|10.5–25 (0.5)|12–30 (0.5)|18–45 (0.5)|
>
> **Selection Weight:** Minimal 5 · Balanced 1 · Legendary 5

## BulkUp

> **Display Text:** Bulk: +{0}% Max HP, -{1}% Regen
>
> **Description:** Converts a portion of your regeneration into maximum health.
>
> **Requirements:**
> * **AllowedItemTypes:** `Chest, Legs`
> * **AllowedRarities:** `Rare, Epic, Legendary, Mythic, Ancient`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Rare|0.5–2 (0.5)|4–10 (1)|5–15 (1)|
> |Epic|1–3 (0.5)|10–18 (1)|15–30 (1)|
> |Legendary|1.5–4 (0.5)|18–32 (1)|30–60 (1)|
> |Mythic|2–5 (0.5)|32–45 (1)|60–90 (1)|
> |Ancient|2.5–6 (0.5)|46–58 (1)|90–100 (1)|
>
> **Selection Weight:** 5

## EitrWeave

> **Display Text:** Eitr Weaving +{0:0.#}%
>
> **Description:** Performing a parry restores **{0:0.#}%** of your max Eitr.
>
> **Requirements:**
> * **Flags:** `ItemHasParryPower`
> * **ExclusiveEffectTypes:** `EitrLeech`
> * **AllowedItemTypes:** `Shield, OneHandedWeapon, TwoHandedWeapon, TwoHandedWeaponLeft`
> * **AllowedRarities:** `Rare, Epic, Legendary, Mythic, Ancient`
> * **ExcludedSkillTypes:** `Pickaxes`
> * **ExcludedItemNames:** `$item_fishingrod`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Rare|15–20 (0.5)|15–20 (0.5)|15–30 (0.5)|
> |Epic|20–35 (0.5)|20–35 (0.5)|20–40 (0.5)|
> |Legendary|35–50 (0.5)|35–50 (0.5)|35–60 (0.5)|
> |Mythic|50–65 (1)|50–65 (1)|50–80 (1)|
> |Ancient|65–80 (1.5)|65–80 (1.5)|65–100 (1)|
>
> **Selection Weight:** Minimal 1 · Balanced 5.5 · Legendary 10
>
> **Config:**
>
> |Setting|Key|Minimal|Balanced|Legendary|
> |--|--|--|--|--|
> |Max Eitr % per Value|`MaxEitrPercentPerValue`|0.12|0.2|0.2|

## DartingThoughts

> **Display Text:** Darting Thoughts: +{0}% Eitr Regen, -{1}% Max Eitr
>
> **Description:** Increases Eitr Regen by **{0}%** while decreasing max Eitr by **{1}%**.
>
> **Requirements:**
> * **AllowedItemTypes:** `Helmet, Utility, Trinket`
> * **AllowedRarities:** `Rare, Epic, Legendary, Mythic, Ancient`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Rare|10–20 (1)|10–20 (1)|10–20 (1)|
> |Epic|20–30 (1)|20–40 (1)|20–40 (1)|
> |Legendary|30–40 (1)|40–60 (1)|40–60 (1)|
> |Mythic|40–50 (1)|60–80 (1)|60–80 (1)|
> |Ancient|50–60 (1)|80–100 (1)|80–100 (1)|
>
> **Selection Weight:** 10

## HeadHunter

> **Display Text:** Headhunter +{0}
>
> **Description:** Increases chance to drop trophies by +**X**%.
>
> **Requirements:**
> * **AllowedItemTypes:** `Helmet`
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1–3 (0.5)|1–3 (0.5)|1–3 (0.5)|
> |Rare|2–6 (0.5)|2–6 (0.5)|2–6 (0.5)|
> |Epic|4–9 (0.5)|4–9 (0.5)|4–9 (0.5)|
> |Legendary|6–12 (0.5)|6–12 (0.5)|6–12 (0.5)|
> |Mythic|9–15 (0.5)|9–15 (0.5)|9–15 (0.5)|
> |Ancient|12–18 (0.5)|12–18 (0.5)|12–18 (0.5)|
>
> **Selection Weight:** 10

## Bulwark

> **Display Text:** Bulwark [Activated]: Immune to damage for 5s
>
> **Description:** Bulwark [Activated]: Prevent all damage for 5 seconds. Cooldown: 60 seconds.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Legendary|5|5|5|
> |Mythic|6|6|6|
> |Ancient|7|7|7|
>
> **Ability:** `Bulwark`
>
> ***Can't be rolled.***

## Undying

> **Display Text:** Undying [Passive]: Survive a killing blow
>
> **Description:** Undying [Passive]: On death, regain full health. Cooldown: 20 minutes.
>
> **Ability:** `Undying`
>
> ***Can't be rolled.***

## FrostDamageAOE

> **Display Text:** Frost Cone: +{0:0.#}% weapon damage
>
> **Description:** Melee attacks that deal frost damage deal an additional {0:0.#}% of weapon damage as frost in a cone in front of the attacker.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Legendary|40|40|40|
> |Mythic|50|50|50|
> |Ancient|60|60|60|
>
> ***Can't be rolled.***

## Berserker

> **Display Text:** Berserker [Activated]: Up to +200% damage
>
> **Description:** Berserker [Activated]: For 10 seconds, you cannot regenerate health, but you gain +50% to +200% damage based on health missing. Cooldown: 3 minutes.
>
> **Ability:** `Berserker`
>
> ***Can't be rolled.***

## OverwhelmingLaunch

> **Display Text:** Overwhelming Launch [Activated]: Weapon throw +{0}%
>
> **Description:** Overwhelming Launch [Activated]: Hurl your melee weapon where you aim. It slams down for its weapon damage +{0}% in an area, then flies back to your hand, and cannot attack until it returns. Costs {1}x a normal attack.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Legendary|50|50|50|
> |Mythic|50|50|50|
> |Ancient|50|50|50|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Max Launch Distance (m)|`MaxDistance`|30|
> |Projectile Speed (m/s)|`ProjectileSpeed`|25|
> |Hang Time (s)|`HangTime`|0.2|
> |Radius (m)|`Radius`|4|
> |Stagger Multiplier|`StaggerMultiplier`|2|
> |Attack Cost Multiplier|`CostScale`|3|
> |Throw Animation Speed (x)|`ThrowAnimationSpeed`|1.3|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## Artillery

> **Display Text:** Artillery: Crossbow fireballs, +{0}% damage
>
> **Description:** Artillery: Your crossbow fires lobbed fireballs that come down where you aim, dealing +{0}% damage to everything within {1}m of the impact.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|10|10|10|
> |Rare|15|15|15|
> |Epic|20|20|20|
> |Legendary|25|25|25|
> |Mythic|30|30|30|
> |Ancient|35|35|35|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Max Launch Distance (m)|`MaxDistance`|60|
> |Gravity|`Gravity`|30|
> |Arc Height (per m of range)|`ArcHeight`|0.2|
> |Radius (m)|`Radius`|2.5|
> |Triple Shot Scatter (x range)|`TripleShotSpread`|0.15|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## Assassin

> **Display Text:** Assassin: Blink strike, {0:0.#}% damage
>
> **Description:** Assassin: Keep your aim on an enemy within {2:0.#}m for {3:0.#} seconds with a melee weapon drawn to vanish and reappear behind it, striking for {0:0.#}% weapon damage. Cooldown: {1:0} seconds.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|125|125|125|
> |Rare|150|150|150|
> |Epic|175|175|175|
> |Legendary|200|200|200|
> |Mythic|225|225|225|
> |Ancient|250|250|250|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Max Teleport Distance (m)|`MaxDistance`|20|
> |Hover Time (s)|`HoverTime`|2|
> |Cooldown at 0 Value (s)|`Cooldown`|340|
> |Cooldown per Value (s)|`CooldownPerValue`|0.8|
> |Min Cooldown (s)|`MinCooldown`|30|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## CoilingVenom

> **Display Text:** Coiling Venom: {0:0.#}% physical as venom
>
> **Description:** Coiling Venom: {0:0.#}% of your physical damage is dealt as poison instead. Your poison ignores resistance and adds to the venom already in an enemy instead of replacing it.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|20|20|20|
> |Rare|30|30|30|
> |Epic|40|40|40|
> |Legendary|50|50|50|
> |Mythic|60|60|60|
> |Ancient|70|70|70|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## NineSteps

> **Display Text:** Nine Steps: Venom past {0:0.#}% health dooms
>
> **Description:** Nine Steps: When the venom in an enemy reaches {0:0.#}% of its remaining health, it is doomed. It walks {1:0} more steps, {2:0.#} seconds, and falls dead.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|145|145|145|
> |Rare|130|130|130|
> |Epic|115|115|115|
> |Legendary|100|100|100|
> |Mythic|85|85|85|
> |Ancient|70|70|70|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Steps|`Steps`|9|
> |Step Interval (s)|`StepInterval`|0.5|
> |Doomed Slow (%)|`Slow`|40|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## SlightlyWeakToFire

> **Display Text:** Slightly weak VS Fire
>
> **Description:** Damage modifier: Slightly weak VS Fire
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## WeakToFire

> **Display Text:** Weak VS Fire
>
> **Description:** Damage modifier: Weak VS Fire
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## VeryWeakToFire

> **Display Text:** Very weak VS Fire
>
> **Description:** Damage modifier: Very weak VS Fire
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## GiantsBlood

> **Display Text:** Jotunn's Blood +{0:0.#}%
>
> **Description:** Jotunn's Blood courses through your veins. Gain +{0:0.#}% Size, +{1:0.#}% Carry Weight, +{2:0.#}% Health Regen, +{3:0.#}% Physical Damage Dealt.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Legendary|10|10|10|
> |Mythic|10|10|10|
> |Ancient|10|10|10|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## Hexen

> **Display Text:** Hexen: Jotun Witch form, {0:0.#} Eitr/s flight
>
> **Description:** Hexen: You wear the body of a Jotun Witch for as long as you wear the set. Your attacks become her magic blast and lightning bolt, and block or sneak with jump blinks you the way you move. Jump to take flight, holding jump or crouch to rise or sink. Flight drains {0:0.#} Eitr per second, needs {1:0} to take off, and when your Eitr runs dry you sink to the ground.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|6|6|6|
> |Rare|5|5|5|
> |Epic|4|4|4|
> |Legendary|3|3|3|
> |Mythic|2.5|2.5|2.5|
> |Ancient|2|2|2|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Witch Size (x her own)|`Scale`|0.6|
> |Eitr Needed to Take Off|`MinEitr`|10|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## Sniper

> **Display Text:** Sniper: +{0:0.#}% damage per metre past {3:0.#} m
>
> **Description:** Sniper: Your arrows fly {1:0.##}x as fast, and those that hit beyond {3:0.#} m deal +{0:0.#}% damage per metre flown, but your bow takes {2:0.##}x as long to draw.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|0.5|0.5|0.5|
> |Rare|0.6|0.6|0.6|
> |Epic|0.7|0.7|0.7|
> |Legendary|0.8|0.8|0.8|
> |Mythic|1|1|1|
> |Ancient|1.2|1.2|1.2|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Extra Arrow Speed per Value (x)|`SpeedBonusPerValue`|1.25|
> |Extra Draw Time per Value (x)|`DrawTimeBonusPerValue`|1.25|
> |Min Shot Distance (m)|`MinDistance`|15|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## Frostwalker

> **Display Text:** Frostwalker: Walk on water, {0:0.#} Eitr/s
>
> **Description:** Frostwalker: The water freezes beneath your feet so you can walk on it, draining {0:0.#} Eitr per second ({1:0.#} over the boiling sea). Crouch to break through. Your Eitr does not regenerate on or in the water.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|4.5|4.5|4.5|
> |Rare|4|4|4|
> |Epic|3.5|3.5|3.5|
> |Legendary|3|3|3|
> |Mythic|2.5|2.5|2.5|
> |Ancient|2|2|2|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Boiling Sea Drain (x)|`AshlandsDrainMultiplier`|3|
> |Eitr Needed to Freeze|`MinEitr`|10|
> |Radius (m)|`Radius`|3|
> |Ice Chunk Size (m)|`ChunkSize`|4|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## SpiritAnimal

> **Display Text:** Spirit Animal [Activated]: Fylgja fights at {0:0}%
>
> **Description:** Spirit Animal [Activated]: Bind a tamed creature as your fylgja. In battle its spirit fights beside you at {0:0}% strength for {1:0} seconds, is summoned again if the fight goes on, and returns {2:0} seconds after it falls.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|100|100|100|
> |Rare|100|100|100|
> |Epic|100|100|100|
> |Legendary|100|100|100|
> |Mythic|115|115|115|
> |Ancient|130|130|130|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Spirit Lifetime (s)|`Lifetime`|120|
> |Return Delay (s)|`ReturnDelay`|30|
> |Combat Timeout (s)|`CombatTimeout`|10|
> |Leash Distance (m)|`LeashDistance`|60|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## Earthshaker

> **Display Text:** Earthshaker [Activated]: Dive slam, {0:0} damage
>
> **Description:** Earthshaker [Activated]: In mid-air, dive where you aim, steering with your aim as you fall, and slam the ground for {0:0} damage, half blunt and half pierce, within {1:0.#}m, +1m per {9:0}m fallen. +1% damage per {4:0.#} weight carried over {5:0} (up to +{6:0}%), and +{7:0.#}% per metre fallen past {8:0.#}m. Diving with less than {2:0.#}m of clear air below you deals {10:0}% less damage. Costs {3:0} stamina.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|50|50|50|
> |Rare|100|100|100|
> |Epic|150|150|150|
> |Legendary|200|200|200|
> |Mythic|250|250|250|
> |Ancient|300|300|300|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Radius (m)|`Radius`|5|
> |Fall per +1m Radius (m)|`FallPerRadiusMetre`|15|
> |Max Radius (m, 0 = none)|`MaxRadius`|0|
> |Height for Full Damage (m)|`MinHeight`|3|
> |Low Dive Damage Lost (%)|`LowDivePenalty`|50|
> |Stamina Cost|`StaminaCost`|25|
> |Dive Speed (m/s)|`DiveSpeed`|30|
> |Minimum Dive Angle (°)|`MinDiveAngle`|45|
> |Steering (°/s, 0 = none)|`SteerRate`|60|
> |Stagger Multiplier|`StaggerMultiplier`|2|
> |Knockback|`PushForce`|80|
> |Weight Bonus Starts At|`WeightThreshold`|300|
> |Weight per +1%|`WeightPerPercent`|10|
> |Max Weight Bonus (%)|`MaxWeightBonus`|300|
> |Fall Bonus Starts At (m)|`FallThreshold`|5|
> |Fall Bonus per Metre (%)|`FallBonusPerMetre`|2|
> |Max Fall Bonus (%, 0 = none)|`MaxFallBonus`|0|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## PiercingBolts

> **Display Text:** Piercing Bolts: Bolts pierce, keeping {0:0}%
>
> **Description:** Piercing Bolts: Your crossbow bolts pass through every creature in their path, striking each once and keeping {0:0}% of their damage after each one.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|50|50|50|
> |Rare|55|55|55|
> |Epic|65|65|65|
> |Legendary|75|75|75|
> |Mythic|85|85|85|
> |Ancient|95|95|95|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## ElementalArchery

> **Display Text:** Elemental Archery: Bows use Elemental Magic
>
> **Description:** Elemental Archery: Your bows use Elemental Magic instead of Bows for damage, draw speed and stamina, and hitting creatures with them trains Elemental Magic.
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## ConjuredArrows

> **Display Text:** Conjured Arrows: Eitr arrows, +{0:0}% frost
>
> **Description:** Conjured Arrows: Your bows fire frost arrows conjured from eitr instead of real arrows, adding {0:0}% of the bow's damage as frost. Each shot costs {1:0.#} Eitr, less with Elemental Magic skill.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|50|50|50|
> |Rare|60|60|60|
> |Epic|70|70|70|
> |Legendary|80|80|80|
> |Mythic|90|90|90|
> |Ancient|100|100|100|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Eitr per Shot (before skill)|`EitrCost`|8|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## ConvertHealthToEitr

> **Display Text:** {0:0}% of Max Health converted to Max Eitr
>
> **Description:** {0:0}% of your maximum Health is moved into your maximum Eitr, one for one. Works without any Eitr food.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|20|20|20|
> |Rare|20|20|20|
> |Epic|20|20|20|
> |Legendary|20|20|20|
> |Mythic|20|20|20|
> |Ancient|20|20|20|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## DeepWell

> **Display Text:** Deep Well: +{0:0}% Max Eitr, -{1:0}% Eitr Regen
>
> **Description:** Your maximum Eitr grows by {0:0}%, but your Eitr regenerates {1:0}% slower.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|30|30|30|
> |Rare|30|30|30|
> |Epic|30|30|30|
> |Legendary|30|30|30|
> |Mythic|30|30|30|
> |Ancient|30|30|30|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Eitr Regen Reduction (%)|`EitrRegenReduction`|30|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## EitrBarrier

> **Display Text:** Eitr Barrier: Eitr absorbs {0:0.#}% of hits
>
> **Description:** Eitr Barrier: A purple barrier takes {0:0.#}% of every hit from your Eitr before your block, resistances and armor, absorbing {2:0.##} damage per Eitr. It collapses for {1:0.#}s whenever your Eitr runs dry.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|10|10|10|
> |Rare|15|15|15|
> |Epic|20|20|20|
> |Legendary|25|25|25|
> |Mythic|30|30|30|
> |Ancient|35|35|35|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Cooldown (s)|`Cooldown`|5|
> |Eitr Needed to Rise|`MinEitr`|5|
> |Damage Absorbed per Eitr|`DamagePerEitr`|1|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## Interconnected

> **Display Text:** Interconnected: {0:0}% of partner skill level
>
> **Description:** Interconnected: The skill of the weapon in your hands gains {0:0}% of its partner skill's level. Bows draws on Axes or Polearms, Blood Magic on Clubs or Unarmed, Elemental Magic on Swords or Spears, and Crossbows on Knives, and each of those draws on its partner in turn. A skill with two partners uses the higher one.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|5|5|5|
> |Rare|10|10|10|
> |Epic|15|15|15|
> |Legendary|20|20|20|
> |Mythic|25|25|25|
> |Ancient|30|30|30|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## AddAllSkills

> **Display Text:** All Skills +{0:0}
>
> **Description:** Increases all of the player's skill levels by +{0:0}. This may cause skill levels to increase above 100.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|5|5|5|
> |Rare|5|5|5|
> |Epic|5|5|5|
> |Legendary|5|5|5|
> |Mythic|5|5|5|
> |Ancient|5|5|5|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## EitrInfusion

> **Display Text:** Eitr Infusion [Activated]: +{0:0}% infused damage
>
> **Description:** Eitr Infusion [Activated]: Pour all your Eitr into your weapon, at least {1:0}, for a second of infusion per {2:0.#} Eitr. While infused your hits deal +{0:0}% of the weapon's highest damage type as that type, or as fire, frost or lightning if you were burning, frozen or shocked, or near a fire, when it began. Your Eitr does not regenerate, Spellsword swings cost no Eitr, and pressing again pours in the Eitr you have regained.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|25|25|25|
> |Rare|30|30|30|
> |Epic|35|35|35|
> |Legendary|40|40|40|
> |Mythic|45|45|45|
> |Ancient|50|50|50|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Eitr Needed to Start|`MinEitr`|20|
> |Eitr per Second of Infusion|`EitrPerSecond`|4|
> |Max Duration (s, 0 = none)|`MaxSeconds`|0|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## QuarryMark

> **Display Text:** Quarry Mark: +{0:0}% on marked foes
>
> **Description:** Quarry Mark: Your arrows mark creatures for {1:0}s. Your next axe or polearm hit on a marked creature deals +{0:0}% damage and restores {2:0} stamina.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|25|25|25|
> |Rare|50|50|50|
> |Epic|75|75|75|
> |Legendary|100|100|100|
> |Mythic|125|125|125|
> |Ancient|150|150|150|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Mark Duration (s)|`MarkDuration`|10|
> |Stamina Refund|`StaminaRefund`|25|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## HelsHarvest

> **Display Text:** Hel's Harvest: Raise up to {0:0} thralls
>
> **Description:** Hel's Harvest: Creatures slain by you or your summons rise as thralls of the same star level that fight for you for {1:0} seconds, stronger with Blood Magic skill. Up to {0:0} at once; a new one replaces the oldest.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1|1|1|
> |Rare|1|1|1|
> |Epic|1|1|1|
> |Legendary|2|2|2|
> |Mythic|3|3|3|
> |Ancient|4|4|4|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Thrall Lifetime (s)|`Lifetime`|30|
> |Strength per Blood Magic Level|`SkillFactorPerLevel`|0.02|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## Vanish

> **Display Text:** Vanish: Dodge to vanish for {0:0.#}s
>
> **Description:** Vanish: Dodging near an alerted enemy fills the air with smoke, and enemies within {1:0}m lose sight of you for {0:0.#} seconds. Your next hit from hiding is a sneak attack. Cooldown: {2:0} seconds.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1.5|1.5|1.5|
> |Rare|2|2|2|
> |Epic|2.5|2.5|2.5|
> |Legendary|3|3|3|
> |Mythic|4|4|4|
> |Ancient|5|5|5|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Cooldown (s)|`Cooldown`|20|
> |Radius (m)|`Radius`|15|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## Firebrand

> **Display Text:** Firebrand: Third swing erupts, {0:0}% damage
>
> **Description:** Firebrand: The third swing of every combo with a two-handed axe or sword erupts in Brenna's fire, burning enemies within {1:0.#}m for {0:0}% of your weapon's damage. Your secondary attack erupts for {2:0.#}%.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|25|25|25|
> |Rare|35|35|35|
> |Epic|50|50|50|
> |Legendary|75|75|75|
> |Mythic|100|100|100|
> |Ancient|125|125|125|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Radius (m)|`Radius`|3|
> |Secondary Attack Power (x)|`SecondaryPower`|0.5|
> |Knockback|`PushForce`|40|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## LifeSiphon

> **Display Text:** Life Siphon: Drain foes, heal {0:0.#}%
>
> **Description:** Life Siphon: Your hits drain a foe for {1:0} seconds. While drained it cannot heal, and every player who hits it heals for {0:0.#}% of the damage dealt.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|1|1|1|
> |Rare|2|2|2|
> |Epic|3|3|3|
> |Legendary|4|4|4|
> |Mythic|5|5|5|
> |Ancient|6|6|6|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Siphon Duration (s)|`Duration`|30|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## KineticQuake

> **Display Text:** Kinetic Quake: Shockwave every {0:0} blocks
>
> **Description:** Kinetic Quake: Each attack you block stores a charge. At {0:0} charges, the next attack you block releases a rock shockwave {2:0.#}m ahead, dealing {1:0}% of your block power as blunt damage.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Magic|22|22|22|
> |Rare|20|20|20|
> |Epic|18|18|18|
> |Legendary|16|16|16|
> |Mythic|14|14|14|
> |Ancient|12|12|12|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Damage (% of Block Power)|`DamagePercent`|300|
> |Wave Length (m)|`Length`|12|
> |Wave Width (m)|`Width`|4|
> |Eruption Spacing (m)|`StepSpacing`|2|
> |Wave Speed (m/s)|`WaveSpeed`|30|
> |Knockback|`PushForce`|80|
> |Stagger Multiplier|`StaggerMultiplier`|2|
> |Max Slope per Step (m)|`MaxStepRise`|2.5|
> |Eruption Size|`FxScale`|0.5|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## ShieldThrow

> **Display Text:** Shield Throw [Activated]: {0:0}% block power
>
> **Description:** Shield Throw [Activated]: Hurl your shield where you aim. It flies through enemies until it strikes something solid, then returns to your hand, hitting each enemy on the way out and again on the way back for {0:0}% of your block power as blunt damage. Damage you block fills a gauge that the throw spends for up to +{1:0}% damage. You cannot block until it returns. Costs {2:0} stamina.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Legendary|100|100|100|
> |Mythic|125|125|125|
> |Ancient|150|150|150|
>
> **Config:**
>
> |Setting|Key|Value|
> |--|--|--|
> |Max Launch Distance (m)|`MaxDistance`|15|
> |Projectile Speed (m/s)|`ProjectileSpeed`|30|
> |Hang Time (s)|`HangTime`|0.1|
> |Throw Animation Speed (x)|`ThrowAnimationSpeed`|1.3|
> |Stagger Multiplier|`StaggerMultiplier`|1.5|
> |Hit Width (m)|`HitRadius`|0.75|
> |Stamina Cost|`StaminaCost`|20|
> |Full Gauge Bonus (%)|`MaxGaugeBonusPercent`|300|
> |Gauge Size (x Block Power)|`GaugeCapacity`|5|
> |Gauge Decay Delay (s)|`GaugeDecayDelay`|10|
> |Gauge Decay (% per s)|`GaugeDecayRate`|20|
> |Knockback|`PushForce`|40|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

## ReduceStaggerTaken

> **Display Text:** Stagger Damage Taken -{0:0.#}%
>
> **Description:** Reduce the stagger damage the player takes, blocking or not, by **X**%.
>
> |Rarity|Minimal|Balanced|Legendary|
> |--|--|--|--|
> |Legendary|30|30|30|
> |Mythic|30|30|30|
> |Ancient|30|30|30|
>
> ***Can't be rolled.*** *Can't be augmented, disenchanted or extracted to a rune.*

# Item Sets

Sets of loot drop data that the loot tables and other sets refer to by name. A drop that lands on a set picks one of its entries by weight, repeating until it reaches an item. See [LootTables.md](docs/LootTables.md) for the details.

  * The rarity columns are an entry's `Rarity` weights. They count only when nothing earlier in the roll had a `Rarity`; usually the loot table entry already has one.
  * **By rarity** is an entry's `RarityItems` map: the rarity the drop rolled picks the item, and a rarity the map lacks uses the nearest one it has. `{Rarity}` stands for the rarity's name.

## EnchantingMats

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier0Mats](#tier0mats) | 1 (100%) | 1 (16.7%) | 1 (16.7%) | 1 (16.7%) | 1 (16.7%) | 1 (16.7%) | 1 (16.7%) | Magic: [Tier0Mats](#tier0mats) · Rare: [Tier1Mats](#tier1mats) · Epic: [Tier2Mats](#tier2mats) · Legendary: [Tier3Mats](#tier3mats) · Mythic: [Tier4Mats](#tier4mats) · Ancient: [Tier5Mats](#tier5mats) |

## Tier0Mats

> | Items | Weight (Chance) |
> | -- | -- |
> | DustMagic | 1 (33.3%) |
> | EssenceMagic | 1 (33.3%) |
> | ReagentMagic | 1 (33.3%) |

## Tier1Mats

> | Items | Weight (Chance) |
> | -- | -- |
> | DustRare | 1 (33.3%) |
> | EssenceRare | 1 (33.3%) |
> | ReagentRare | 1 (33.3%) |

## Tier2Mats

> | Items | Weight (Chance) |
> | -- | -- |
> | DustEpic | 1 (33.3%) |
> | EssenceEpic | 1 (33.3%) |
> | ReagentEpic | 1 (33.3%) |

## Tier3Mats

> | Items | Weight (Chance) |
> | -- | -- |
> | DustLegendary | 1 (33.3%) |
> | EssenceLegendary | 1 (33.3%) |
> | ReagentLegendary | 1 (33.3%) |

## Tier4Mats

> | Items | Weight (Chance) |
> | -- | -- |
> | DustMythic | 1 (33.3%) |
> | EssenceMythic | 1 (33.3%) |
> | ReagentMythic | 1 (33.3%) |

## Tier5Mats

> | Items | Weight (Chance) |
> | -- | -- |
> | DustAncient | 1 (33.3%) |
> | EssenceAncient | 1 (33.3%) |
> | ReagentAncient | 1 (33.3%) |

## EnchantingRunestones

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | RunestoneMagic | 1 (100%) | 1 (16.7%) | 1 (16.7%) | 1 (16.7%) | 1 (16.7%) | 1 (16.7%) | 1 (16.7%) | `Runestone{Rarity}`: Magic–Ancient |

## Tier0Runestone

> | Items | Weight (Chance) |
> | -- | -- |
> | RunestoneMagic | 1 (100%) |

## Tier1Runestone

> | Items | Weight (Chance) |
> | -- | -- |
> | RunestoneRare | 1 (100%) |

## Tier2Runestone

> | Items | Weight (Chance) |
> | -- | -- |
> | RunestoneEpic | 1 (100%) |

## Tier3Runestone

> | Items | Weight (Chance) |
> | -- | -- |
> | RunestoneLegendary | 1 (100%) |

## Tier4Runestone

> | Items | Weight (Chance) |
> | -- | -- |
> | RunestoneMythic | 1 (100%) |

## Tier5Runestone

> | Items | Weight (Chance) |
> | -- | -- |
> | RunestoneAncient | 1 (100%) |

## ModUtility

> | Items | Weight (Chance) |
> | -- | -- |
> | LeatherBelt | 1 (33.3%) |
> | SilverRing | 1 (33.3%) |
> | GoldRubyRing | 1 (33.3%) |

## ShardT1

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | Yellow_Magic_ShardStone | 1 (33.3%) | 100 (100%) | 0 | 0 | 0 | 0 | 0 | `Yellow_{Rarity}_ShardStone`: Magic–Mythic |
> | Cyan_Magic_ShardStone | 1 (33.3%) | 100 (100%) | 0 | 0 | 0 | 0 | 0 | `Cyan_{Rarity}_ShardStone`: Magic–Mythic |
> | Green_Magic_ShardStone | 1 (33.3%) | 100 (100%) | 0 | 0 | 0 | 0 | 0 | `Green_{Rarity}_ShardStone`: Magic–Mythic |

## ShardT2

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | Red_Rare_ShardStone | 4 (26.7%) | 0 | 100 (100%) | 0 | 0 | 0 | 0 | `Red_{Rarity}_ShardStone`: Magic–Mythic |
> | Grey_Rare_ShardStone | 4 (26.7%) | 0 | 100 (100%) | 0 | 0 | 0 | 0 | `Grey_{Rarity}_ShardStone`: Magic–Mythic |
> | Golden_Rare_ShardStone | 4 (26.7%) | 0 | 100 (100%) | 0 | 0 | 0 | 0 | `Golden_{Rarity}_ShardStone`: Magic–Mythic |
> | [ShardT1](#shardt1) | 3 (20%) | — | — | — | — | — | — |   |

## ShardT3

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | White_Epic_ShardStone | 4 (26.7%) | 0 | 0 | 100 (100%) | 0 | 0 | 0 | `White_{Rarity}_ShardStone`: Magic–Mythic |
> | LightBlue_Epic_ShardStone | 4 (26.7%) | 0 | 0 | 100 (100%) | 0 | 0 | 0 | `LightBlue_{Rarity}_ShardStone`: Magic–Mythic |
> | DarkBlue_Epic_ShardStone | 4 (26.7%) | 0 | 0 | 100 (100%) | 0 | 0 | 0 | `DarkBlue_{Rarity}_ShardStone`: Magic–Mythic |
> | [ShardT2](#shardt2) | 3 (20%) | — | — | — | — | — | — |   |

## ShardT4

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | Black_Legendary_ShardStone | 4 (26.7%) | 0 | 0 | 0 | 100 (100%) | 0 | 0 | `Black_{Rarity}_ShardStone`: Magic–Mythic |
> | Peach_Legendary_ShardStone | 4 (26.7%) | 0 | 0 | 0 | 100 (100%) | 0 | 0 | `Peach_{Rarity}_ShardStone`: Magic–Mythic |
> | DarkGreen_Legendary_ShardStone | 4 (26.7%) | 0 | 0 | 0 | 100 (100%) | 0 | 0 | `DarkGreen_{Rarity}_ShardStone`: Magic–Mythic |
> | [ShardT3](#shardt3) | 3 (20%) | — | — | — | — | — | — |   |

## ShardT5

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | LightGreen_Legendary_ShardStone | 4 (26.7%) | 0 | 0 | 0 | 100 (100%) | 0 | 0 | `LightGreen_{Rarity}_ShardStone`: Magic–Mythic |
> | Purple_Legendary_ShardStone | 4 (26.7%) | 0 | 0 | 0 | 100 (100%) | 0 | 0 | `Purple_{Rarity}_ShardStone`: Magic–Mythic |
> | Pink_Legendary_ShardStone | 4 (26.7%) | 0 | 0 | 0 | 100 (100%) | 0 | 0 | `Pink_{Rarity}_ShardStone`: Magic–Mythic |
> | [ShardT4](#shardt4) | 3 (20%) | — | — | — | — | — | — |   |

## ShardT6

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | DarkPurple_Mythic_ShardStone | 4 (26.7%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `DarkPurple_{Rarity}_ShardStone`: Magic–Mythic |
> | Orange_Mythic_ShardStone | 4 (26.7%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `Orange_{Rarity}_ShardStone`: Magic–Mythic |
> | DarkRed_Mythic_ShardStone | 4 (26.7%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `DarkRed_{Rarity}_ShardStone`: Magic–Mythic |
> | [ShardT5](#shardt5) | 3 (20%) | — | — | — | — | — | — |   |

## ShardT7

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | Red_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `Red_{Rarity}_ShardStone`: Magic–Ancient |
> | Yellow_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `Yellow_{Rarity}_ShardStone`: Magic–Ancient |
> | Cyan_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `Cyan_{Rarity}_ShardStone`: Magic–Ancient |
> | Orange_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `Orange_{Rarity}_ShardStone`: Magic–Ancient |
> | Pink_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `Pink_{Rarity}_ShardStone`: Magic–Ancient |
> | Black_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `Black_{Rarity}_ShardStone`: Magic–Ancient |
> | White_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `White_{Rarity}_ShardStone`: Magic–Ancient |
> | Green_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `Green_{Rarity}_ShardStone`: Magic–Ancient |
> | Purple_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `Purple_{Rarity}_ShardStone`: Magic–Ancient |
> | Grey_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `Grey_{Rarity}_ShardStone`: Magic–Ancient |
> | DarkRed_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `DarkRed_{Rarity}_ShardStone`: Magic–Ancient |
> | DarkGreen_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `DarkGreen_{Rarity}_ShardStone`: Magic–Ancient |
> | DarkBlue_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `DarkBlue_{Rarity}_ShardStone`: Magic–Ancient |
> | DarkPurple_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `DarkPurple_{Rarity}_ShardStone`: Magic–Ancient |
> | Golden_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `Golden_{Rarity}_ShardStone`: Magic–Ancient |
> | LightBlue_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `LightBlue_{Rarity}_ShardStone`: Magic–Ancient |
> | LightGreen_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `LightGreen_{Rarity}_ShardStone`: Magic–Ancient |
> | Peach_Mythic_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 100 (100%) | 0 | `Peach_{Rarity}_ShardStone`: Magic–Ancient |

## ShardT8

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | Red_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `Red_{Rarity}_ShardStone`: Magic–Ancient |
> | Yellow_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `Yellow_{Rarity}_ShardStone`: Magic–Ancient |
> | Cyan_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `Cyan_{Rarity}_ShardStone`: Magic–Ancient |
> | Orange_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `Orange_{Rarity}_ShardStone`: Magic–Ancient |
> | Pink_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `Pink_{Rarity}_ShardStone`: Magic–Ancient |
> | Black_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `Black_{Rarity}_ShardStone`: Magic–Ancient |
> | White_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `White_{Rarity}_ShardStone`: Magic–Ancient |
> | Green_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `Green_{Rarity}_ShardStone`: Magic–Ancient |
> | Purple_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `Purple_{Rarity}_ShardStone`: Magic–Ancient |
> | Grey_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `Grey_{Rarity}_ShardStone`: Magic–Ancient |
> | DarkRed_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `DarkRed_{Rarity}_ShardStone`: Magic–Ancient |
> | DarkGreen_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `DarkGreen_{Rarity}_ShardStone`: Magic–Ancient |
> | DarkBlue_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `DarkBlue_{Rarity}_ShardStone`: Magic–Ancient |
> | DarkPurple_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `DarkPurple_{Rarity}_ShardStone`: Magic–Ancient |
> | Golden_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `Golden_{Rarity}_ShardStone`: Magic–Ancient |
> | LightBlue_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `LightBlue_{Rarity}_ShardStone`: Magic–Ancient |
> | LightGreen_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `LightGreen_{Rarity}_ShardStone`: Magic–Ancient |
> | Peach_Ancient_ShardStone | 1 (5.6%) | 0 | 0 | 0 | 0 | 0 | 100 (100%) | `Peach_{Rarity}_ShardStone`: Magic–Ancient |

## ShardUnique

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | Firewalker_Epic_ShardStone | 1 (50%) | 0 | 0 | 55 (55%) | 35 (35%) | 8 (8%) | 2 (2%) | `Firewalker_{Rarity}_ShardStone`: Epic–Ancient |
> | Stormcaller_Epic_ShardStone | 1 (50%) | 0 | 0 | 55 (55%) | 35 (35%) | 8 (8%) | 2 (2%) | `Stormcaller_{Rarity}_ShardStone`: Epic–Ancient |

## ShardSlotChisel

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | ShardSlotChiselLegendary | 1 (100%) | 0 | 0 | 0 | 75 (75%) | 20 (20%) | 5 (5%) | `ShardSlotChisel{Rarity}`: Legendary–Ancient |

## ShardStone_None

> | Items | Weight (Chance) |
> | -- | -- |
> | [ShardT1](#shardt1) | 1 (100%) |

## ShardStone_Meadows

> | Items | Weight (Chance) |
> | -- | -- |
> | [ShardT1](#shardt1) | 1 (100%) |

## ShardStone_BlackForest

> | Items | Weight (Chance) |
> | -- | -- |
> | [ShardT1](#shardt1) | 1 (100%) |

## ShardStone_Ocean

> | Items | Weight (Chance) |
> | -- | -- |
> | [ShardT1](#shardt1) | 1 (50%) |
> | [ShardT2](#shardt2) | 1 (50%) |

## ShardStone_Swamp

> | Items | Weight (Chance) |
> | -- | -- |
> | [ShardT2](#shardt2) | 1 (100%) |

## ShardStone_Mountain

> | Items | Weight (Chance) |
> | -- | -- |
> | [ShardT3](#shardt3) | 1 (100%) |

## ShardStone_Plains

> | Items | Weight (Chance) |
> | -- | -- |
> | [ShardT4](#shardt4) | 1 (100%) |

## ShardStone_Mistlands

> | Items | Weight (Chance) |
> | -- | -- |
> | [ShardT5](#shardt5) | 1 (100%) |

## ShardStone_AshLands

> | Items | Weight (Chance) |
> | -- | -- |
> | [ShardT6](#shardt6) | 1 (100%) |

## ShardStone_DeepNorth

> | Items | Weight (Chance) |
> | -- | -- |
> | [ShardT7](#shardt7) | 1 (50%) |
> | [ShardT8](#shardt8) | 1 (50%) |

## Tier0Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | Club | 1 (33.3%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 | 0 | 0 |
> | AxeStone | 1 (33.3%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 | 0 | 0 |
> | Torch | 1 (33.3%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 | 0 | 0 |

## Tier0Tools

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | Hammer | 1 (50%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 | 0 | 0 |
> | Hoe | 1 (50%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 | 0 | 0 |

## Tier0Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ArmorRagsLegs | 1 (50%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 | 0 | 0 |
> | ArmorRagsChest | 1 (50%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 | 0 | 0 |

## Tier0Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ShieldWood | 1 (50%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 | 0 | 0 |
> | ShieldWoodTower | 1 (50%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 | 0 | 0 |

## Tier0Everything

> | Items | Weight (Chance) |
> | -- | -- |
> | [Tier0Weapons](#tier0weapons) | 1 (25%) |
> | [Tier0Tools](#tier0tools) | 1 (25%) |
> | [Tier0Armor](#tier0armor) | 1 (25%) |
> | [Tier0Shields](#tier0shields) | 1 (25%) |

## Tier1Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | AxeFlint | 1 (25%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) | 0 | 0 |
> | SpearFlint | 1 (25%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) | 0 | 0 |
> | KnifeFlint | 1 (25%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) | 0 | 0 |
> | Bow | 1 (25%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) | 0 | 0 |

## Tier1Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ArmorLeatherLegs | 1 (25%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) | 0 | 0 |
> | ArmorLeatherChest | 1 (25%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) | 0 | 0 |
> | HelmetLeather | 1 (25%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) | 0 | 0 |
> | CapeDeerHide | 1 (25%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) | 0 | 0 |

## Tier1Tools

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | PickaxeAntler | 1 (100%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) | 0 | 0 |

## Tier1Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ShieldBoneTower | 1 (100%) | 94 (94%) | 3 (3%) | 2 (2%) | 1 (1%) | 0 | 0 |

## Tier1Everything

> | Items | Weight (Chance) |
> | -- | -- |
> | [Tier1Weapons](#tier1weapons) | 1 (25%) |
> | [Tier1Shields](#tier1shields) | 1 (25%) |
> | [Tier1Armor](#tier1armor) | 1 (25%) |
> | [Tier1Tools](#tier1tools) | 1 (25%) |

## Tier2Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | KnifeCopper | 1 (9.1%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | SledgeStagbreaker | 1 (9.1%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | SwordBronze | 1 (9.1%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | AxeBronze | 1 (9.1%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | MaceBronze | 1 (9.1%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | AtgeirBronze | 1 (9.1%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | SpearBronze | 1 (9.1%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | BowFineWood | 1 (9.1%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | KnifeChitin | 1 (9.1%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | SpearChitin | 1 (9.1%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | FistBjornClaw | 1 (9.1%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |

## Tier2Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ArmorBronzeLegs | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | ArmorBronzeChest | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | HelmetBronze | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | HelmetBerserkerHood | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | ArmorBerserkerChest | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | ArmorBerserkerLegs | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | ArmorTrollLeatherLegs | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | ArmorTrollLeatherChest | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | HelmetTrollLeather | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | CapeTrollHide | 1 (10%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |

## Tier2Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ShieldBronzeBuckler | 1 (100%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |

## Tier2Tools

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | PickaxeBronze | 1 (50%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | Cultivator | 1 (50%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |

## Tier2Trinkets

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | TrinketBronzeHealth | 1 (33.3%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | TrinketBronzeStamina | 1 (33.3%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |
> | TrinketChitinSwim | 1 (33.3%) | 80 (80%) | 14 (14%) | 4 (4%) | 2 (2%) | 0 | 0 |

## Tier2Everything

> | Items | Weight (Chance) |
> | -- | -- |
> | [Tier2Weapons](#tier2weapons) | 1 (20%) |
> | [Tier2Armor](#tier2armor) | 1 (20%) |
> | [Tier2Shields](#tier2shields) | 1 (20%) |
> | [Tier2Tools](#tier2tools) | 1 (20%) |
> | [Tier2Trinkets](#tier2trinkets) | 1 (20%) |

## Tier3Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | Battleaxe | 1 (12.5%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |
> | SwordIron | 1 (12.5%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |
> | AxeIron | 1 (12.5%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |
> | SledgeIron | 1 (12.5%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |
> | MaceIron | 1 (12.5%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |
> | AtgeirIron | 1 (12.5%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |
> | SpearElderbark | 1 (12.5%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |
> | BowHuntsman | 1 (12.5%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |

## Tier3Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ArmorRootChest | 1 (16.7%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |
> | ArmorRootLegs | 1 (16.7%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |
> | HelmetRoot | 1 (16.7%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |
> | ArmorIronLegs | 1 (16.7%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |
> | ArmorIronChest | 1 (16.7%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |
> | HelmetIron | 1 (16.7%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |

## Tier3Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ShieldBanded | 1 (50%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |
> | ShieldIronTower | 1 (50%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |

## Tier3Tools

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | PickaxeIron | 1 (100%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |

## Tier3Trinkets

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | TrinketIronHealth | 1 (50%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |
> | TrinketIronStamina | 1 (50%) | 38 (38%) | 50 (50%) | 8 (8%) | 3 (3%) | 1 (1%) | 0 |

## Tier3Everything

> | Items | Weight (Chance) |
> | -- | -- |
> | [Tier3Weapons](#tier3weapons) | 1 (20%) |
> | [Tier3Armor](#tier3armor) | 1 (20%) |
> | [Tier3Shields](#tier3shields) | 1 (20%) |
> | [Tier3Tools](#tier3tools) | 1 (20%) |
> | [Tier3Trinkets](#tier3trinkets) | 1 (20%) |

## Tier4Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | SwordSilver | 5 (27.8%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 17 (15.5%) | 3 (2.7%) | 0 |
> | SpearWolfFang | 5 (27.8%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 17 (15.5%) | 3 (2.7%) | 0 |
> | MaceSilver | 1 (5.6%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 17 (15.5%) | 3 (2.7%) | 0 |
> | KnifeSilver | 5 (27.8%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 17 (15.5%) | 3 (2.7%) | 0 |
> | FistFenrirClaw | 1 (5.6%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 17 (15.5%) | 3 (2.7%) | 0 |
> | BowDraugrFang | 1 (5.6%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 17 (15.5%) | 3 (2.7%) | 0 |

## Tier4Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ArmorFenringChest | 1 (14.3%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 17 (15.5%) | 3 (2.7%) | 0 |
> | ArmorFenringLegs | 1 (14.3%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 17 (15.5%) | 3 (2.7%) | 0 |
> | HelmetFenring | 1 (14.3%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 17 (15.5%) | 3 (2.7%) | 0 |
> | ArmorWolfLegs | 1 (14.3%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 17 (15.5%) | 3 (2.7%) | 0 |
> | ArmorWolfChest | 1 (14.3%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 17 (15.5%) | 3 (2.7%) | 0 |
> | HelmetDrake | 1 (14.3%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 17 (15.5%) | 3 (2.7%) | 0 |
> | CapeWolf | 1 (14.3%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 17 (15.5%) | 3 (2.7%) | 0 |

## Tier4Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ShieldSilver | 5 (83.3%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 17 (15.5%) | 3 (2.7%) | 0 |
> | ShieldSerpentscale | 1 (16.7%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 17 (15.5%) | 3 (2.7%) | 0 |

## Tier4Trinkets

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | TrinketSilverDamage | 5 (83.3%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 17 (15.5%) | 3 (2.7%) | 0 |
> | TrinketSilverResist | 1 (16.7%) | 5 (4.5%) | 35 (31.8%) | 50 (45.5%) | 17 (15.5%) | 3 (2.7%) | 0 |

## Tier4Everything

> | Items | Weight (Chance) |
> | -- | -- |
> | [Tier4Weapons](#tier4weapons) | 1 (25%) |
> | [Tier4Armor](#tier4armor) | 1 (25%) |
> | [Tier4Shields](#tier4shields) | 1 (25%) |
> | [Tier4Trinkets](#tier4trinkets) | 1 (25%) |

## Tier5Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | AtgeirBlackmetal | 3 (21.4%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |
> | AxeBlackMetal | 3 (21.4%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |
> | KnifeBlackMetal | 3 (21.4%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |
> | SwordBlackmetal | 3 (21.4%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |
> | MaceNeedle | 1 (7.1%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |
> | FistBjornUndeadClaw | 1 (7.1%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |

## Tier5Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ArmorPaddedGreaves | 1 (12.5%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |
> | ArmorPaddedCuirass | 1 (12.5%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |
> | HelmetPadded | 1 (12.5%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |
> | ArmorBerserkerUndeadLegs | 1 (12.5%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |
> | ArmorBerserkerUndeadChest | 1 (12.5%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |
> | HelmetBerserkerUndead | 1 (12.5%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |
> | CapeLinen | 1 (12.5%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |
> | CapeLox | 1 (12.5%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |

## Tier5Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ShieldBlackmetal | 1 (50%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |
> | ShieldBlackmetalTower | 1 (50%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |

## Tier5Trinkets

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | TrinketBlackDamageHealth | 1 (50%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |
> | TrinketBlackStamina | 1 (50%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |

## Tier5Everything

> | Items | Weight (Chance) |
> | -- | -- |
> | [Tier5Weapons](#tier5weapons) | 1 (25%) |
> | [Tier5Armor](#tier5armor) | 1 (25%) |
> | [Tier5Shields](#tier5shields) | 1 (25%) |
> | [Tier5Trinkets](#tier5trinkets) | 1 (25%) |

## Tier6Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | SwordMistwalker | 1 (6.7%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | THSwordKrom | 1 (6.7%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | StaffFireball | 1 (6.7%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | StaffIceShards | 1 (6.7%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | StaffShield | 1 (6.7%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | StaffSkeleton | 1 (6.7%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | AtgeirHimminAfl | 1 (6.7%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | KnifeSkollAndHati | 1 (6.7%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | AxeJotunBane | 1 (6.7%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | SpearCarapace | 1 (6.7%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | SledgeDemolisher | 1 (6.7%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | CrossbowArbalest | 3 (20%) | 0 | 15 (15%) | 60 (60%) | 20 (20%) | 5 (5%) | 0 |
> | BowSpineSnap | 1 (6.7%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |

## Tier6Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ArmorCarapaceChest | 1 (14.3%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | ArmorCarapaceLegs | 1 (14.3%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | HelmetCarapace | 1 (14.3%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | ArmorMageChest | 1 (14.3%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | ArmorMageLegs | 1 (14.3%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | HelmetMage | 1 (14.3%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | CapeFeather | 1 (14.3%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |

## Tier6Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ShieldCarapace | 1 (50%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | ShieldCarapaceBuckler | 1 (50%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |

## Tier6Tools

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | PickaxeBlackMetal | 1 (100%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |

## Tier6Utility

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | Demister | 1 (100%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |

## Tier6Trinkets

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | TrinketCarapaceEitr | 1 (50%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |
> | TrinketScaleStaminaDamage | 1 (50%) | 0 | 10 (10%) | 50 (50%) | 30 (30%) | 10 (10%) | 0 |

## Tier6Everything

> | Items | Weight (Chance) |
> | -- | -- |
> | [Tier6Weapons](#tier6weapons) | 1 (16.7%) |
> | [Tier6Armor](#tier6armor) | 1 (16.7%) |
> | [Tier6Shields](#tier6shields) | 1 (16.7%) |
> | [Tier6Tools](#tier6tools) | 1 (16.7%) |
> | [Tier6Utility](#tier6utility) | 1 (16.7%) |
> | [Tier6Trinkets](#tier6trinkets) | 1 (16.7%) |

## Tier7Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | SwordNiedhogg | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | THSwordSlayer | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | SwordDyrnwyn | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | AxeBerzerkr | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | StaffClusterbomb | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | StaffLightning | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | StaffRedTroll | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | StaffGreenRoots | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | MaceEldner | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | SpearSplitner | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | BowAshlands | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |

## Tier7Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ArmorFlametalChest | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | ArmorFlametalLegs | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | HelmetFlametal | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | ArmorAshlandsMediumChest | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | ArmorAshlandsMediumlegs | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | HelmetAshlandsMediumHood | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | ArmorMageChest_Ashlands | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | ArmorMageLegs_Ashlands | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | HelmetMage_Ashlands | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | CapeAsh | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | CapeAsksvin | 1 (9.1%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |

## Tier7Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ShieldFlametal | 1 (50%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | ShieldFlametalTower | 1 (50%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |

## Tier7Trinkets

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | TrinketFlametalEitr | 1 (50%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |
> | TrinketFlametalStaminaHealth | 1 (50%) | 0 | 5 (5%) | 33 (33%) | 42 (42%) | 15 (15%) | 5 (5%) |

## Tier7Everything

> | Items | Weight (Chance) |
> | -- | -- |
> | [Tier7Weapons](#tier7weapons) | 1 (25%) |
> | [Tier7Armor](#tier7armor) | 1 (25%) |
> | [Tier7Shields](#tier7shields) | 1 (25%) |
> | [Tier7Trinkets](#tier7trinkets) | 1 (25%) |

## Tier8Weapons

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | SwordGold | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | SwordGold_FrostFire | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | SwordGold_BloodLightning | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | THSwordGold | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | THSwordGold_FrostFire | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | THSwordGold_BloodLightning | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | AxeGold | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | AxeGold_FrostFire | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | AxeGold_BloodLightning | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | BattleaxeGold | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | BattleaxeGold_FrostFire | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | BattleaxeGold_BloodLightning | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | KnifeGold | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | KnifeGold_FrostFire | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | KnifeGold_BloodLightning | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | FistGold | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | FistGold_FrostFire | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | FistGold_BloodLightning | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | StaffFrostOrbs | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | StaffOrbofAhri | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | StaffSpiritCaller | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | StaffThunderBlood | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | MaceGold | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | MaceGold_FrostFire | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | MaceGold_BloodLightning | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | SledgeGold | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | SledgeGold_FrostFire | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | SledgeGold_BloodLightning | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | AtgeirGold | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | AtgeirGold_FrostFire | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | AtgeirGold_BloodLightning | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | SpearGold | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | SpearGold_FrostFire | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | SpearGold_BloodLightning | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | BowGold | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | BowGold_FrostFire | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | BowGold_BloodLightning | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | CrossbowGold | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | CrossbowGold_FrostFire | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | CrossbowGold_BloodLightning | 1 (2.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |

## Tier8Armor

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ArmorDeepNorthHeavyChest | 1 (9.1%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | ArmorDeepNorthHeavylegs | 1 (9.1%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | HelmetDNHeavy | 1 (9.1%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | ArmorDeepNorthMediumChest | 1 (9.1%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | ArmorDeepNorthMediumlegs | 1 (9.1%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | HelmetDNMediumHood | 1 (9.1%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | ArmorDeepNorthMageChest | 1 (9.1%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | ArmorDeepNorthMagelegs | 1 (9.1%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | HelmetDNMage | 1 (9.1%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | CapeDeepNorth | 1 (9.1%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | CapeDeepNorthMage | 1 (9.1%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |

## Tier8Shields

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | ShieldGoldBuckler | 1 (33.3%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | ShieldGold | 1 (33.3%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | ShieldGoldTower | 1 (33.3%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |

## Tier8Trinkets

> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | TrinketBloodGoldHealth | 1 (50%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |
> | TrinketBloodGoldStamina | 1 (50%) | 0 | 0 | 20 (20%) | 40 (40%) | 30 (30%) | 10 (10%) |

## Tier8Everything

> | Items | Weight (Chance) |
> | -- | -- |
> | [Tier8Weapons](#tier8weapons) | 1 (25%) |
> | [Tier8Armor](#tier8armor) | 1 (25%) |
> | [Tier8Shields](#tier8shields) | 1 (25%) |
> | [Tier8Trinkets](#tier8trinkets) | 1 (25%) |

# Loot Tables

Every loot table in `loottables.json`. [LootTables.md](docs/LootTables.md) explains the format and how a roll works. In short:

  * A creature's level is its stars plus one. Each `Drops (lvl …)` and `Items (lvl …)` table is an anchor, used from its level up to the next anchor; `StarScaling` extrapolates the levels past it.
  * **Drops** is the chance of each number of drops. **Items** is the list each drop is picked from, by weight, and `WeightPerStar` is added to an entry's weight for each star past the anchor.
  * An item is a prefab, an [item set](#item-sets) or a table reference: `Tier3Mob.2` rolls Tier3Mob as a level 2 creature would, and `Tier3Mob.*` rolls it at the level being rolled.
  * The rarity columns are the entry's `Rarity` weights before stars and luck promote them. **By rarity** is its `RarityItems` map, as in the item sets.
  * An object with several tables rolls each of them.
  * A table without a `StarScaling` line, and any field a table leaves out, uses `DefaultStarScaling`: DropChance 0.149 · BonusDrops 0.016 · RarityShift 0.28 · RarityFalloff 1.
  * With the server option `Elite Runestone Drops` on, each elite template also rolls a runestone table built in code, which is not listed here.

## Creatures using a template

Most creatures roll another table's loot through `RefObject`.

| Table | Creatures |
| -- | -- |
| [Tier0Mob](#tier0mob) | Neck, Boar, Greyling |
| [Tier1Mob](#tier1mob) | Skeleton_Meadows, Skeleton_Meadows_noarcher, Greydwarf, Skeleton, Skeleton_NoArcher |
| [Tier2Mob](#tier2mob) | Ghost, Ghost_sleeping, Greydwarf_Shaman, Skeleton_Poison, Greydwarf_Elite |
| [Tier3Mob](#tier3mob) | Bat_Swamp, Surtling, Blob, Leech, Leech_cave, Skeleton_Swamps, Skeleton_Swamps_noarcher, Draugr, Draugr_Ranged, Draugr_Ranged_sleeping, Draugr_sleeping, Wraith |
| [Tier4Mob](#tier4mob) | BlobElite, Draugr_Elite, Draugr_Elite_sleeping, Bat, Ulv, Skeleton_Mountains, Skeleton_Mountains_noarcher, Wolf, Hatchling |
| [Tier5Mob](#tier5mob) | Serpent, Fenring_Cultist, Deathsquito, BlobTar, GoblinShaman, Goblin, GoblinArcher |
| [Tier6Mob](#tier6mob) | Tick |
| [Tier7Mob](#tier7mob) | Seeker, Dverger, DvergerMage, DvergerMageFire, DvergerMageIce, DvergerMageSupport, Charred_Archer, Volture, Charred_Twitcher, BlobLava |
| [Tier8Mob](#tier8mob) | Charred_Mage, Charred_Melee, Asksvin, DvergerAshlands, BonemawSerpent, BlobMorkMini, Frysling, Greydwarf_Frozen, Skeleton_DeepNorth, Greydwarf_Shaman_Frozen, BlobMork, GoblinDeepNorth, Elaking, ElakingLantern |
| [Tier9Mob](#tier9mob) | FallenWarrior, ShadowPerson, JotunWitch |
| [Tier3EliteMob](#tier3elitemob) | Bjorn, Bjorn_sleeping, Skeleton_Hildir_nochest, Troll, Troll_sleeping, Skeleton_Hildir |
| [Tier4EliteMob](#tier4elitemob) | Writhan, BogWitchKvastur, Abomination |
| [Tier5EliteMob](#tier5elitemob) | Fenring, StoneGolem, Fenring_Cultist_Hildir_nochest, Fenring_Cultist_Hildir |
| [Tier6EliteMob](#tier6elitemob) | GoblinBrute, GoblinBrute_Hildir, Lox, GoblinShaman_Hildir_nochest, Unbjorn, GoblinBruteBros_nochest, GoblinShaman_Hildir, GoblinBruteBros |
| [Tier7EliteMob](#tier7elitemob) | Gjall, SeekerBrute |
| [Tier8EliteMob](#tier8elitemob) | FallenValkyrie, Morgen, Morgen_NonSleeping, Charred_Melee_Dyrnwyn |
| [Tier9EliteMob](#tier9elitemob) | Moose, JotunWarrior, JotunWarriorDualWield, ElakingMole, DvergerDeepNorth, Barka, TrollFrost |

## Tier0Mob

> **StarScaling:** DropChance 0.057 · BonusDrops 0 · RarityShift 0.05 · RarityFalloff 0.02 · MaxRarity Legendary
>
> | Drops (lvl 1–4) | Weight (Chance) |
> | -- | -- |
> | 0 | 100 (100%) |
>
> | Items (lvl 1–4) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier0Everything](#tier0everything) | 1 (100%) | 100 (100%) | 0 | 0 | 0 | 0 | 0 |
>
> | Drops (lvl 5+) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |
>
> | Items (lvl 5+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier0Everything](#tier0everything) | 1 (100%) | 70 (70%) | 19 (19%) | 10 (10%) | 1 (1%) | 0 | 0 |

## Tier1Mob

> **StarScaling:** DropChance 0.057 · BonusDrops 0 · RarityShift 0.08 · RarityFalloff 0.5 · MaxRarity Legendary
>
> | Drops (lvl 1–4) | Weight (Chance) |
> | -- | -- |
> | 0 | 95 (95%) |
> | 1 | 5 (5%) |
>
> | Items (lvl 1–4) | Weight (Chance) | WeightPerStar | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier0Everything](#tier0everything) | 10 (90.9%) | -1.063 | 75 (75%) | 25 (25%) | 0 | 0 | 0 | 0 |
> | [Tier1Everything](#tier1everything) | 1 (9.1%) | 1.063 | 75 (75%) | 25 (25%) | 0 | 0 | 0 | 0 |
>
> | Drops (lvl 5+) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |
>
> | Items (lvl 5+) | Weight (Chance) | WeightPerStar | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier0Everything](#tier0everything) | 1 (16.7%) | -0.455 | 45 (45%) | 43 (43%) | 9 (9%) | 3 (3%) | 0 | 0 |
> | [Tier1Everything](#tier1everything) | 5 (83.3%) | 0.455 | 45 (45%) | 43 (43%) | 9 (9%) | 3 (3%) | 0 | 0 |

## Tier2Mob

> **StarScaling:** DropChance 0.107 · BonusDrops 0.003 · RarityShift 0.21 · RarityFalloff 0.3 · MaxRarity Legendary
>
> | Drops (lvl 1–4) | Weight (Chance) |
> | -- | -- |
> | 0 | 90 (90%) |
> | 1 | 10 (10%) |
>
> | Items (lvl 1–4) | Weight (Chance) | WeightPerStar | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier1Everything](#tier1everything) | 1 (100%) | -0.086 | 50 (50%) | 47 (47%) | 2 (2%) | 1 (1%) | 0 | 0 |
> | [Tier2Armor](#tier2armor) | 0 (0%) | 0.173 | 50 (50%) | 47 (47%) | 2 (2%) | 1 (1%) | 0 | 0 |
>
> | Drops (lvl 5+) | Weight (Chance) |
> | -- | -- |
> | 0 | 40 (40%) |
> | 1 | 58 (58%) |
> | 2 | 2 (2%) |
>
> | Items (lvl 5+) | Weight (Chance) | WeightPerStar | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier1Everything](#tier1everything) | 1 (16.7%) | -0.455 | 0 | 70 (70%) | 20 (20%) | 10 (10%) | 0 | 0 |
> | [Tier2Everything](#tier2everything) | 5 (83.3%) | 0.455 | 0 | 70 (70%) | 20 (20%) | 10 (10%) | 0 | 0 |

## Tier3Mob

> **StarScaling:** DropChance 0.139 · BonusDrops 0.018 · RarityShift 0.38 · RarityFalloff 0.15 · MaxRarity Legendary
>
> | Drops (lvl 1–2) | Weight (Chance) |
> | -- | -- |
> | 0 | 85 (85%) |
> | 1 | 15 (15%) |
>
> | Items (lvl 1–2) | Weight (Chance) | WeightPerStar | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier2Everything](#tier2everything) | 1 (100%) | -0.167 | 24 (24%) | 73 (73%) | 2 (2%) | 1 (1%) | 0 | 0 |
> | [Tier3Everything](#tier3everything) | 0 (0%) | 0.167 | 24 (24%) | 73 (73%) | 2 (2%) | 1 (1%) | 0 | 0 |
>
> | Drops (lvl 3+) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 34 (34%) |
> | 2 | 1 (1%) |
>
> | Items (lvl 3+) | Weight (Chance) | WeightPerStar | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier2Everything](#tier2everything) | 3 (75%) | -0.97 | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) | 0 | 0 |
> | [Tier3Everything](#tier3everything) | 1 (25%) | 0.97 | 5 (5%) | 83 (83%) | 9 (9%) | 3 (3%) | 0 | 0 |

## Tier4Mob

> **StarScaling:** DropChance 0.152 · BonusDrops 0.021 · RarityShift 0.28 · RarityFalloff 0.25 · MaxRarity Legendary
>
> | Drops (lvl 1–3) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |
>
> | Items (lvl 1–3) | Weight (Chance) | WeightPerStar | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier3Everything](#tier3everything) | 1 (100%) | -0.133 | 0 | 75 (74.3%) | 24 (23.8%) | 2 (2%) | 0 | 0 |
> | [Tier4Everything](#tier4everything) | 0 (0%) | 0.133 | 0 | 75 (74.3%) | 24 (23.8%) | 2 (2%) | 0 | 0 |
>
> | Drops (lvl 4+) | Weight (Chance) |
> | -- | -- |
> | 0 | 30 (30%) |
> | 1 | 66 (66%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |
>
> | Items (lvl 4+) | Weight (Chance) | WeightPerStar | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier3Everything](#tier3everything) | 1 (50%) | -0.461 | 0 | 10 (10%) | 75 (75%) | 15 (15%) | 0 | 0 |
> | [Tier4Everything](#tier4everything) | 1 (50%) | 0.461 | 0 | 10 (10%) | 75 (75%) | 15 (15%) | 0 | 0 |

## Tier5Mob

> **StarScaling:** DropChance 0.178 · BonusDrops 0.021 · RarityShift 0.39 · RarityFalloff 0.1 · MaxRarity Legendary
>
> | Drops (lvl 1–2) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 24 (24%) |
> | 2 | 1 (1%) |
>
> | Items (lvl 1–2) | Weight (Chance) | WeightPerStar | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier4Everything](#tier4everything) | 1 (100%) | -0.167 | 0 | 50 (48.5%) | 49 (47.6%) | 4 (3.9%) | 0 | 0 |
> | [Tier5Everything](#tier5everything) | 0 (0%) | 0.167 | 0 | 50 (48.5%) | 49 (47.6%) | 4 (3.9%) | 0 | 0 |
>
> | Drops (lvl 3+) | Weight (Chance) |
> | -- | -- |
> | 0 | 45 (45%) |
> | 1 | 51 (51%) |
> | 2 | 3 (3%) |
> | 3 | 1 (1%) |
>
> | Items (lvl 3+) | Weight (Chance) | WeightPerStar | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier4Everything](#tier4everything) | 3 (75%) | -0.97 | 0 | 10 (10%) | 80 (80%) | 10 (10%) | 0 | 0 |
> | [Tier5Everything](#tier5everything) | 1 (25%) | 0.97 | 0 | 10 (10%) | 80 (80%) | 10 (10%) | 0 | 0 |

## Tier6Mob

> **StarScaling:** DropChance 0.205 · BonusDrops 0.029 · RarityShift 0.89 · RarityFalloff 0.08 · MaxRarity Legendary
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 28 (28%) |
> | 2 | 2 (2%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier5Everything](#tier5everything) | 1 (100%) | 0 | 25 (25%) | 65 (65%) | 10 (10%) | 0 | 0 |

## Tier7Mob

> **StarScaling:** DropChance 0.2 · BonusDrops 0.021 · RarityShift 1 · RarityFalloff 0.08 · MaxRarity Mythic
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 32 (32%) |
> | 2 | 3 (3%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier6Everything](#tier6everything) | 1 (100%) | 0 | 15 (15%) | 70 (70%) | 15 (15%) | 0 | 0 |

## Tier8Mob

> **StarScaling:** DropChance 0.21 · BonusDrops 0.028 · RarityShift 0.18 · RarityFalloff 0.7 · MaxRarity Ancient
>
> | Drops (lvl 1) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 36 (36%) |
> | 2 | 4 (4%) |
>
> | Items (lvl 1) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier7Everything](#tier7everything) | 3 (75%) | 0 | 5 (5%) | 25 (25%) | 65 (65%) | 5 (5%) | 0 |
> | [Tier8Everything](#tier8everything) | 1 (25%) | 0 | 5 (5%) | 25 (25%) | 65 (65%) | 5 (5%) | 0 |
>
> | Drops (lvl 2+) | Weight (Chance) |
> | -- | -- |
> | 0 | 40 (44.4%) |
> | 1 | 46 (51.1%) |
> | 2 | 3 (3.3%) |
> | 3 | 1 (1.1%) |
>
> | Items (lvl 2+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier7Everything](#tier7everything) | 1 (50%) | 0 | 0 | 30 (30%) | 60 (60%) | 10 (10%) | 0 |
> | [Tier8Everything](#tier8everything) | 1 (50%) | 0 | 0 | 30 (30%) | 60 (60%) | 10 (10%) | 0 |

## Tier9Mob

> **StarScaling:** DropChance 0.183 · BonusDrops 0.018 · RarityShift 0.27 · RarityFalloff 0.7 · MaxRarity Ancient
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 36 (36%) |
> | 2 | 4 (4%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier8Everything](#tier8everything) | 1 (100%) | 0 | 0 | 20 (20%) | 55 (55%) | 20 (20%) | 5 (5%) |

## Tier3EliteMob

> **StarScaling:** DropChance 0.119 · BonusDrops 0.017 · RarityShift 0
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 85 (85%) |
> | 1 | 15 (15%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [Tier3Mob.\*](#tier3mob) | 20 (93.5%) |
> | [ShardT1](#shardt1) | 1 (4.7%) |
> | [ShardUnique](#shardunique) | 0.4 (1.9%) |

## Tier4EliteMob

> **StarScaling:** DropChance 0.162 · BonusDrops 0.022 · RarityShift 0
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 80 (80%) |
> | 1 | 20 (20%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [Tier4Mob.\*](#tier4mob) | 20 (93.5%) |
> | [ShardT2](#shardt2) | 1 (4.7%) |
> | [ShardUnique](#shardunique) | 0.4 (1.9%) |

## Tier5EliteMob

> **StarScaling:** DropChance 0.162 · BonusDrops 0.024 · RarityShift 0
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 75 (75%) |
> | 1 | 24 (24%) |
> | 2 | 1 (1%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [Tier5Mob.\*](#tier5mob) | 20 (93.5%) |
> | [ShardT3](#shardt3) | 1 (4.7%) |
> | [ShardUnique](#shardunique) | 0.4 (1.9%) |

## Tier6EliteMob

> **StarScaling:** DropChance 0.205 · BonusDrops 0.029 · RarityShift 0
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 28 (28%) |
> | 2 | 2 (2%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [Tier6Mob.\*](#tier6mob) | 20 (93.5%) |
> | [ShardT4](#shardt4) | 1 (4.7%) |
> | [ShardUnique](#shardunique) | 0.4 (1.9%) |

## Tier7EliteMob

> **StarScaling:** DropChance 0.2 · BonusDrops 0.021 · RarityShift 0
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 65 (65%) |
> | 1 | 32 (32%) |
> | 2 | 3 (3%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [Tier7Mob.\*](#tier7mob) | 20 (93.5%) |
> | [ShardT5](#shardt5) | 1 (4.7%) |
> | [ShardUnique](#shardunique) | 0.4 (1.9%) |

## Tier8EliteMob

> **StarScaling:** DropChance 0.183 · BonusDrops 0.018 · RarityShift 0
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 36 (36%) |
> | 2 | 4 (4%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [Tier8Mob.\*](#tier8mob) | 20 (89.3%) |
> | [ShardT6](#shardt6) | 2 (8.9%) |
> | [ShardUnique](#shardunique) | 0.4 (1.8%) |

## Tier9EliteMob

> **StarScaling:** DropChance 0.183 · BonusDrops 0.018 · RarityShift 0
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 60 (60%) |
> | 1 | 36 (36%) |
> | 2 | 4 (4%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [Tier9Mob.\*](#tier9mob) | 20 (89.3%) |
> | [ShardT7](#shardt7) | 1 (4.5%) |
> | [ShardT8](#shardt8) | 1 (4.5%) |
> | [ShardUnique](#shardunique) | 0.4 (1.8%) |

## Eikthyr

> ***Table 1 of 2***
>
> **StarScaling:** DropChance 0 · BonusDrops 0.127 · RarityShift 0.14 · RarityFalloff 1.5 · MaxRarity Legendary
>
> | Drops (lvl 1–4) | Weight (Chance) |
> | -- | -- |
> | 1 | 70 (70%) |
> | 2 | 30 (30%) |
> | 4 | 0 (0%) |
>
> | Items (lvl 1–4) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier1Everything](#tier1everything) | 1 (50%) | 177 (88.5%) | 20 (10%) | 2 (1%) | 1 (0.5%) | 0 | 0 |
> | SledgeStagbreaker | 1 (50%) | 177 (88.5%) | 20 (10%) | 2 (1%) | 1 (0.5%) | 0 | 0 |
>
> | Drops (lvl 5+) | Weight (Chance) |
> | -- | -- |
> | 1 | 20 (20%) |
> | 2 | 70 (70%) |
> | 3 | 10 (10%) |
> | 4 | 0 (0%) |
>
> | Items (lvl 5+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier1Everything](#tier1everything) | 1 (50%) | 35 (35%) | 40 (40%) | 15 (15%) | 10 (10%) | 0 | 0 |
> | SledgeStagbreaker | 1 (50%) | 35 (35%) | 40 (40%) | 15 (15%) | 10 (10%) | 0 | 0 |

> ***Table 2 of 2***
>
> **StarScaling:** DropChance 0 · BonusDrops 0 · RarityShift 0
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 50 (50%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | Eikthyr_Rare_ShardStone | 1 (100%) | 0 | 70 (70%) | 25 (25%) | 5 (5%) | 0 | 0 | `Eikthyr_{Rarity}_ShardStone`: Rare–Mythic |

## gd_king

> ***Table 1 of 2***
>
> **StarScaling:** DropChance 0 · BonusDrops 0.182 · RarityShift 0.42 · RarityFalloff 0.7 · MaxRarity Mythic
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 1 | 35 (35%) |
> | 2 | 55 (55%) |
> | 3 | 10 (10%) |
> | 4 | 0 (0%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier2Everything](#tier2everything) | 1 (100%) | 59 (59%) | 29 (29%) | 10 (10%) | 2 (2%) | 0 | 0 |

> ***Table 2 of 2***
>
> **StarScaling:** DropChance 0 · BonusDrops 0 · RarityShift 0
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 50 (50%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | Elder_Rare_ShardStone | 1 (100%) | 0 | 60 (60%) | 30 (30%) | 10 (10%) | 0 | 0 | `Elder_{Rarity}_ShardStone`: Rare–Mythic |

## Bonemass

> ***Table 1 of 2***
>
> **StarScaling:** DropChance 0 · BonusDrops 0.217 · RarityShift 1 · RarityFalloff 0.3 · MaxRarity Mythic
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 1 | 0 (0%) |
> | 2 | 60 (60%) |
> | 3 | 35 (35%) |
> | 4 | 5 (5%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier3Everything](#tier3everything) | 1 (100%) | 20 (20%) | 60 (60%) | 15 (15%) | 5 (5%) | 0 | 0 |

> ***Table 2 of 2***
>
> **StarScaling:** DropChance 0 · BonusDrops 0 · RarityShift 0
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 50 (50%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | Bonemass_Epic_ShardStone | 1 (100%) | 0 | 0 | 70 (70%) | 25 (25%) | 5 (5%) | 0 | `Bonemass_{Rarity}_ShardStone`: Epic–Mythic |

## Dragon

> ***Table 1 of 2***
>
> **StarScaling:** DropChance 0 · BonusDrops 0.23 · RarityShift 0.71 · RarityFalloff 0.1 · MaxRarity Mythic
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 2 | 20 (20%) |
> | 3 | 40 (40%) |
> | 4 | 30 (30%) |
> | 5 | 10 (10%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier4Everything](#tier4everything) | 1 (100%) | 0 | 40 (40%) | 50 (50%) | 10 (10%) | 0 | 0 |

> ***Table 2 of 2***
>
> **StarScaling:** DropChance 0 · BonusDrops 0 · RarityShift 0
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 50 (50%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | Moder_Epic_ShardStone | 1 (100%) | 0 | 0 | 60 (60%) | 30 (30%) | 10 (10%) | 0 | `Moder_{Rarity}_ShardStone`: Epic–Mythic |

## GoblinKing

> ***Table 1 of 2***
>
> **StarScaling:** DropChance 0 · BonusDrops 0.323 · RarityShift 0.2 · RarityFalloff 0.3 · MaxRarity Mythic
>
> | Drops (lvl 1–5) | Weight (Chance) |
> | -- | -- |
> | 3 | 20 (20%) |
> | 4 | 60 (60%) |
> | 5 | 15 (15%) |
> | 6 | 5 (5%) |
>
> | Items (lvl 1–5) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier5Everything](#tier5everything) | 1 (100%) | 0 | 0 | 80 (80%) | 19 (19%) | 1 (1%) | 0 |
>
> | Drops (lvl 6+) | Weight (Chance) |
> | -- | -- |
> | 5 | 50 (50%) |
> | 6 | 40 (40%) |
> | 7 | 10 (10%) |
>
> | Items (lvl 6+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier5Everything](#tier5everything) | 1 (100%) | 0 | 0 | 0 | 80 (80%) | 20 (20%) | 0 |

> ***Table 2 of 2***
>
> **StarScaling:** DropChance 0 · BonusDrops 0 · RarityShift 0
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 50 (50%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | Yagluth_Legendary_ShardStone | 1 (100%) | 0 | 0 | 0 | 80 (80%) | 20 (20%) | 0 | `Yagluth_{Rarity}_ShardStone`: Legendary, Mythic |

## SeekerQueen

> ***Table 1 of 2***
>
> **StarScaling:** DropChance 0 · BonusDrops 0.317 · RarityShift 0.55 · RarityFalloff 0.5 · MaxRarity Mythic
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 3 | 20 (20%) |
> | 4 | 60 (60%) |
> | 5 | 15 (15%) |
> | 6 | 5 (5%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier6Everything](#tier6everything) | 1 (100%) | 0 | 0 | 20 (20%) | 70 (70%) | 10 (10%) | 0 |

> ***Table 2 of 2***
>
> **StarScaling:** DropChance 0 · BonusDrops 0 · RarityShift 0
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 50 (50%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | Queen_Legendary_ShardStone | 1 (100%) | 0 | 0 | 0 | 70 (70%) | 30 (30%) | 0 | `Queen_{Rarity}_ShardStone`: Legendary, Mythic |

## Fader

> ***Table 1 of 2***
>
> **StarScaling:** DropChance 0 · BonusDrops 0.323 · RarityShift 0.2 · RarityFalloff 0.2 · MaxRarity Ancient
>
> | Drops (lvl 1–5) | Weight (Chance) |
> | -- | -- |
> | 3 | 20 (20%) |
> | 4 | 60 (60%) |
> | 5 | 15 (15%) |
> | 6 | 5 (5%) |
>
> | Items (lvl 1–5) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier7Everything](#tier7everything) | 1 (100%) | 0 | 0 | 0 | 80 (80%) | 20 (20%) | 0 |
>
> | Drops (lvl 6+) | Weight (Chance) |
> | -- | -- |
> | 5 | 50 (50%) |
> | 6 | 40 (40%) |
> | 7 | 10 (10%) |
>
> | Items (lvl 6+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier7Everything](#tier7everything) | 1 (100%) | 0 | 0 | 0 | 0 | 80 (80%) | 20 (20%) |

> ***Table 2 of 2***
>
> **StarScaling:** DropChance 0 · BonusDrops 0 · RarityShift 0
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 50 (50%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient | By rarity |
> | -- | -- | -- | -- | -- | -- | -- | -- | -- |
> | Fader_Mythic_ShardStone | 1 (100%) | 0 | 0 | 0 | 0 | 80 (80%) | 20 (20%) | `Fader_{Rarity}_ShardStone`: Mythic, Ancient |

## FrozenKing_p3

> **StarScaling:** DropChance 0 · BonusDrops 0.317 · RarityShift 0.35 · RarityFalloff 0.4 · MaxRarity Ancient
>
> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 4 | 20 (20%) |
> | 5 | 60 (60%) |
> | 6 | 15 (15%) |
> | 7 | 5 (5%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier8Everything](#tier8everything) | 1 (100%) | 0 | 0 | 0 | 60 (60%) | 35 (35%) | 5 (5%) |

## TreasureChest_meadows

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 78 (78%) |
> | 1 | 20 (20%) |
> | 2 | 2 (2%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier0Everything](#tier0everything) | 4 (80%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 | 0 | 0 |
> | [Tier1Everything](#tier1everything) | 1 (20%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 | 0 | 0 |

## TreasureChest_meadows_combat

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 78 (78%) |
> | 1 | 20 (20%) |
> | 2 | 2 (2%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier0Everything](#tier0everything) | 4 (80%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 | 0 | 0 |
> | [Tier1Everything](#tier1everything) | 1 (20%) | 97 (97%) | 2 (2%) | 1 (1%) | 0 | 0 | 0 |

## TreasureChest_blackforest

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 70 (70%) |
> | 1 | 30 (30%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier1Everything](#tier1everything) | 1 (100%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 | 0 | 0 |

## TreasureChest_forestcrypt

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 68 (68%) |
> | 1 | 20 (20%) |
> | 2 | 10 (10%) |
> | 3 | 2 (2%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier1Everything](#tier1everything) | 2 (66.7%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 | 0 | 0 |
> | [Tier2Everything](#tier2everything) | 1 (33.3%) | 95 (95%) | 4 (4%) | 1 (1%) | 0 | 0 | 0 |

## TreasureChest_fCrypt

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 68 (68%) |
> | 1 | 20 (20%) |
> | 2 | 10 (10%) |
> | 3 | 2 (2%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [TreasureChest_forestcrypt.1](#treasurechest_forestcrypt) | 1 (100%) |

## TreasureChest_trollcave

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 48 (43.6%) |
> | 1 | 40 (36.4%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier1Everything](#tier1everything) | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 | 0 | 0 |
> | [Tier2Everything](#tier2everything) | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 | 0 | 0 |

## shipwreck_karve_chest

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 48 (43.6%) |
> | 1 | 40 (36.4%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier0Everything](#tier0everything) | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 | 0 | 0 |
> | [Tier1Everything](#tier1everything) | 1 (50%) | 90 (90%) | 9 (9%) | 1 (1%) | 0 | 0 | 0 |

## TreasureChest_meadows_buried

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 48 (43.6%) |
> | 1 | 40 (36.4%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier0Everything](#tier0everything) | 3 (60%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 | 0 | 0 |
> | [Tier1Everything](#tier1everything) | 2 (40%) | 70 (70%) | 25 (25%) | 5 (5%) | 0 | 0 | 0 |

## TreasureChest_sunkencrypt

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier2Everything](#tier2everything) | 3 (74.3%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 | 0 | 0 |
> | [Tier3Everything](#tier3everything) | 1 (24.8%) | 80 (80%) | 17 (17%) | 3 (3%) | 0 | 0 | 0 |
> | [ShardUnique](#shardunique) | 0.04 (1%) | — | — | — | — | — | — |

## TreasureChest_swamp

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [Tier2Everything](#tier2everything) | 5 (82.8%) |
> | [Tier3Everything](#tier3everything) | 1 (16.6%) |
> | [ShardUnique](#shardunique) | 0.04 (0.7%) |

## TreasureChest_mountains

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [Tier3Everything](#tier3everything) | 4 (79.4%) |
> | [Tier4Everything](#tier4everything) | 1 (19.8%) |
> | [ShardUnique](#shardunique) | 0.04 (0.8%) |

## TreasureChest_mountaincave

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [Tier3Everything](#tier3everything) | 4 (79.4%) |
> | [Tier4Everything](#tier4everything) | 1 (19.8%) |
> | [ShardUnique](#shardunique) | 0.04 (0.8%) |

## TreasureChest_plains_stone

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [TreasureChest_heath.1](#treasurechest_heath) | 1 (100%) |

## TreasureChest_heath

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [Tier4Everything](#tier4everything) | 3 (74.3%) |
> | [Tier5Everything](#tier5everything) | 1 (24.8%) |
> | [ShardUnique](#shardunique) | 0.04 (1%) |

## TreasureChest_dvergrtown

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [Tier5Everything](#tier5everything) | 3 (73.5%) |
> | [Tier6Everything](#tier6everything) | 1 (24.5%) |
> | [ShardUnique](#shardunique) | 0.04 (1%) |
> | [ShardSlotChisel](#shardslotchisel) | 0.04 (1%) |

## TreasureChest_dvergrtower

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [TreasureChest_dvergrtown.1](#treasurechest_dvergrtown) | 1 (100%) |

## TreasureChest_dvergr_loose_stone

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [TreasureChest_dvergrtown.1](#treasurechest_dvergrtown) | 1 (100%) |

## TreasureChest_charredfortress

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [Tier6Everything](#tier6everything) | 3 (73.5%) |
> | [Tier7Everything](#tier7everything) | 1 (24.5%) |
> | [ShardUnique](#shardunique) | 0.04 (1%) |
> | [ShardSlotChisel](#shardslotchisel) | 0.04 (1%) |

## TreasureChest_ashland_stone

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 58 (52.7%) |
> | 1 | 30 (27.3%) |
> | 2 | 20 (18.2%) |
> | 3 | 2 (1.8%) |
>
> | Items (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | [Tier6Everything](#tier6everything) | 3 (73.5%) |
> | [Tier7Everything](#tier7everything) | 1 (24.5%) |
> | [ShardUnique](#shardunique) | 0.04 (1%) |
> | [ShardSlotChisel](#shardslotchisel) | 0.04 (1%) |

## TreasureChest_deepnorth_village

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 55 (55%) |
> | 1 | 30 (30%) |
> | 2 | 12 (12%) |
> | 3 | 3 (3%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier7Everything](#tier7everything) | 0.75 (69.4%) | 0 | 0 | 45 (45%) | 35 (35%) | 15 (15%) | 5 (5%) |
> | [Tier8Everything](#tier8everything) | 0.25 (23.1%) | 0 | 0 | 45 (45%) | 35 (35%) | 15 (15%) | 5 (5%) |
> | [ShardUnique](#shardunique) | 0.04 (3.7%) | — | — | — | — | — | — |
> | [ShardSlotChisel](#shardslotchisel) | 0.04 (3.7%) | — | — | — | — | — | — |

## TreasureChest_memorial_buried

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 55 (55%) |
> | 1 | 30 (30%) |
> | 2 | 12 (12%) |
> | 3 | 3 (3%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier7Everything](#tier7everything) | 0.75 (69.4%) | 0 | 0 | 45 (45%) | 35 (35%) | 15 (15%) | 5 (5%) |
> | [Tier8Everything](#tier8everything) | 0.25 (23.1%) | 0 | 0 | 45 (45%) | 35 (35%) | 15 (15%) | 5 (5%) |
> | [ShardUnique](#shardunique) | 0.04 (3.7%) | — | — | — | — | — | — |
> | [ShardSlotChisel](#shardslotchisel) | 0.04 (3.7%) | — | — | — | — | — | — |

## Morkhalla_RandomLoot

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 50 (50%) |
> | 1 | 32 (32%) |
> | 2 | 14 (14%) |
> | 3 | 4 (4%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier7Everything](#tier7everything) | 0.75 (67%) | 0 | 0 | 35 (35%) | 38 (38%) | 20 (20%) | 7 (7%) |
> | [Tier8Everything](#tier8everything) | 0.25 (22.3%) | 0 | 0 | 35 (35%) | 38 (38%) | 20 (20%) | 7 (7%) |
> | [ShardUnique](#shardunique) | 0.06 (5.4%) | — | — | — | — | — | — |
> | [ShardSlotChisel](#shardslotchisel) | 0.06 (5.4%) | — | — | — | — | — | — |

## Morkhalla_ChestAncient

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 0 | 40 (40%) |
> | 1 | 35 (35%) |
> | 2 | 19 (19%) |
> | 3 | 6 (6%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier7Everything](#tier7everything) | 0.75 (62.5%) | 0 | 0 | 20 (20%) | 40 (40%) | 28 (28%) | 12 (12%) |
> | [Tier8Everything](#tier8everything) | 0.25 (20.8%) | 0 | 0 | 20 (20%) | 40 (40%) | 28 (28%) | 12 (12%) |
> | [ShardUnique](#shardunique) | 0.1 (8.3%) | — | — | — | — | — | — |
> | [ShardSlotChisel](#shardslotchisel) | 0.1 (8.3%) | — | — | — | — | — | — |

## TreasureMapChest_Meadows

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier0Everything](#tier0everything) | 1 (50%) | 68 (68%) | 30 (30%) | 2 (2%) | 0 | 0 | 0 |
> | [Tier1Everything](#tier1everything) | 1 (50%) | 68 (68%) | 30 (30%) | 2 (2%) | 0 | 0 | 0 |

## TreasureMapChest_BlackForest

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier1Everything](#tier1everything) | 1 (50%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) | 0 | 0 |
> | [Tier2Everything](#tier2everything) | 1 (50%) | 40 (40%) | 50 (50%) | 9 (9%) | 1 (1%) | 0 | 0 |

## TreasureMapChest_Swamp

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier2Everything](#tier2everything) | 1 (49%) | 10 (9.1%) | 45 (40.9%) | 50 (45.5%) | 5 (4.5%) | 0 | 0 |
> | [Tier3Everything](#tier3everything) | 1 (49%) | 10 (9.1%) | 45 (40.9%) | 50 (45.5%) | 5 (4.5%) | 0 | 0 |
> | [ShardUnique](#shardunique) | 0.04 (2%) | — | — | — | — | — | — |

## TreasureMapChest_Mountain

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier3Everything](#tier3everything) | 1 (49%) | 0 | 30 (30%) | 60 (60%) | 10 (10%) | 0 | 0 |
> | [Tier4Everything](#tier4everything) | 1 (49%) | 0 | 30 (30%) | 60 (60%) | 10 (10%) | 0 | 0 |
> | [ShardUnique](#shardunique) | 0.04 (2%) | — | — | — | — | — | — |

## TreasureMapChest_Plains

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier4Everything](#tier4everything) | 1 (49%) | 0 | 10 (10%) | 75 (75%) | 14 (14%) | 1 (1%) | 0 |
> | [Tier5Everything](#tier5everything) | 1 (49%) | 0 | 10 (10%) | 75 (75%) | 14 (14%) | 1 (1%) | 0 |
> | [ShardUnique](#shardunique) | 0.04 (2%) | — | — | — | — | — | — |

## TreasureMapChest_Mistlands

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier5Everything](#tier5everything) | 1 (48.1%) | 0 | 5 (5%) | 70 (70%) | 20 (20%) | 5 (5%) | 0 |
> | [Tier6Everything](#tier6everything) | 1 (48.1%) | 0 | 5 (5%) | 70 (70%) | 20 (20%) | 5 (5%) | 0 |
> | [ShardUnique](#shardunique) | 0.04 (1.9%) | — | — | — | — | — | — |
> | [ShardSlotChisel](#shardslotchisel) | 0.04 (1.9%) | — | — | — | — | — | — |

## TreasureMapChest_AshLands

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 3 | 80 (80%) |
> | 4 | 15 (15%) |
> | 5 | 5 (5%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier6Everything](#tier6everything) | 1 (48.1%) | 0 | 0 | 60 (60%) | 25 (25%) | 10 (10%) | 5 (5%) |
> | [Tier7Everything](#tier7everything) | 1 (48.1%) | 0 | 0 | 60 (60%) | 25 (25%) | 10 (10%) | 5 (5%) |
> | [ShardUnique](#shardunique) | 0.04 (1.9%) | — | — | — | — | — | — |
> | [ShardSlotChisel](#shardslotchisel) | 0.04 (1.9%) | — | — | — | — | — | — |

## TreasureMapChest_DeepNorth

> | Drops (lvl 1+) | Weight (Chance) |
> | -- | -- |
> | 3 | 75 (75%) |
> | 4 | 18 (18%) |
> | 5 | 7 (7%) |
>
> | Items (lvl 1+) | Weight (Chance) | Magic | Rare | Epic | Legendary | Mythic | Ancient |
> | -- | -- | -- | -- | -- | -- | -- | -- |
> | [Tier7Everything](#tier7everything) | 0.5 (44.6%) | 0 | 0 | 30 (30%) | 40 (40%) | 22 (22%) | 8 (8%) |
> | [Tier8Everything](#tier8everything) | 0.5 (44.6%) | 0 | 0 | 30 (30%) | 40 (40%) | 22 (22%) | 8 (8%) |
> | [ShardUnique](#shardunique) | 0.06 (5.4%) | — | — | — | — | — | — |
> | [ShardSlotChisel](#shardslotchisel) | 0.06 (5.4%) | — | — | — | — | — | — |
