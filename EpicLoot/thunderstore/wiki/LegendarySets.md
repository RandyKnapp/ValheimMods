# Legendary Sets

Every legendary set in Epic Loot, with its pieces and the bonuses it gives. All values are the defaults from `legendaries.json` (in `BepInEx/config/EpicLoot/baseconfig/`) with the balanced template; a server can change any of them.

## How sets work

- Each set has four pieces. Wearing two, three or all four of them unlocks that tier's bonuses.
- The tiers add up: with the full set you have the two-piece, three-piece and four-piece bonuses together.
- Set pieces drop at Legendary, Mythic or Ancient rarity, and pieces of different rarities count toward the same set.
- When a bonus is shown as three values, they are its strength at Legendary / Mythic / Ancient. A bonus uses the rarity of your weakest piece among those it needs, so a three-piece bonus uses your third-best piece.
- A copy of a piece you already wear does not count twice.
- A set weapon only counts while it is in your hands. Stowed on your back it keeps the set's look but not its bonuses.
- Abilities marked **[Activated]** need a key press. The newer sets use T (Y for Spirit Animal), and Bulwark and Berserker use the ability bar keys (G, H, J). On a gamepad, hold the modifier (LT, or LB on the Alternative 1 layout) and click the right stick. Each bonus shows its key under its description: hold Shift over a set piece, or open the Compendium. Keys are set in the config under Abilities.
- "Always rolls with" lists the enchantments a piece is guaranteed to have; its other enchantments are random.
- The rune feature at the enchanting table can extract a set from an item and etch it onto another.

## Interconnected

Five sets share the Interconnected three-piece bonus (Einherjar, Ullr's Hunt, Hel's Covenant, Huldufólk, Gullveig's Rebirth). The skill of the weapon in your hands gains a share of its partner skill's level: Bows draws on Axes or Polearms, Blood Magic on Clubs or Unarmed, Elemental Magic on Swords or Spears, and Crossbows on Knives, and each of those draws on its partner in turn. A skill with two partners uses the higher one.

## All sets

| Set | Built for | Weapon or shield | Full set |
|---|---|---|---|
| [Heimdall's Legacy](#heimdalls-legacy) | Tower-shield tank | Shield without parry (tower shield) | Undying: come back to full health when you would die, once every 20 minutes. |
| [Ragnar's Fury](#ragnars-fury) | Two-handed axe berserker | Two-handed axe | Frost-dealing melee swings also hit a cone in front of you with frost. |
| [Wrath of the Thunderer](#wrath-of-the-thunderer) | Two-handed club | Two-handed club (sledge) | Overwhelming Launch: hurl your melee weapon, even mid-swing, to slam down where you aim. |
| [Dverger Arsenal](#dverger-arsenal) | Crossbow | Crossbow | Artillery: crossbow bolts become lobbed fireballs that explode where you aim. |
| [The Trickster's Guile](#the-tricksters-guile) | Knife assassin | Knife | Assassin: keep your aim on an enemy to vanish and reappear behind it for a heavy strike. |
| [Coils of the World Serpent](#coils-of-the-world-serpent) | Spear, poison | Spear | Nine Steps: heavily envenomed enemies walk nine more steps and fall dead. |
| [Egil's Mark](#egils-mark) | Long-range bow | Bow | Sniper: arrows fly faster and hit harder the further they travel, but draw slower. |
| [Rime of Élivágar](#rime-of-élivágar) | Frost mage armor | None (armor only) | Frostwalker: the water freezes under your feet so you can walk on it for a steady Eitr cost. |
| [Fylgja's Bond](#fylgjas-bond) | Ranger with a tame | Bow or any melee weapon | Spirit Animal: a bound tame fights beside you as a spirit. |
| [Freyja's Falcon](#freyjas-falcon) | Spear, aerial | Spear | Earthshaker: dive from the air and slam the ground where you aim. |
| [Bölþorn's Bane](#bölþorns-bane) | Crossbow | Crossbow | Piercing Bolts: crossbow bolts pass through every creature in their path. |
| [Heiðr's Seiðr](#heiðrs-seiðr) | Mage with a bow | Bow | Conjured Arrows: your bow fires frost arrows made of Eitr instead of real arrows. |
| [Mímir's Well](#mímirs-well) | Eitr-tank mage armor | None (armor only) | Eitr Barrier: part of every hit is taken from your Eitr before your armor. |
| [Einherjar](#einherjar) | Spellsword | Sword (one- or two-handed) | Eitr Infusion: pour your Eitr into your weapon for a spell of extra damage. |
| [Ullr's Hunt](#ullrs-hunt) | Bow plus axe or polearm | Bow | Quarry Mark: arrows mark creatures for a heavy axe or polearm hit. |
| [Hel's Covenant](#hels-covenant) | Blood mage | Blood Magic staff | Hel's Harvest: creatures you or your summons kill rise as thralls that fight for you. |
| [Huldufólk](#huldufólk) | Knife rogue | Knife | Huge sneak attack damage, on top of vanishing in smoke when you dodge. |
| [Surtr's Brand](#surtrs-brand) | Two-handed axe or sword, fire | Two-handed axe or sword | Firebrand: combo finishers and secondary attacks erupt in Brenna's fire. |
| [Hel's Court](#hels-court) | Blood fighter armor | None (armor only) | Life Siphon: drained enemies cannot heal, and everyone hitting them heals. |
| [Hrungnir's Stand](#hrungnirs-stand) | Tower-shield blocker | Shield without parry (tower shield) | Kinetic Quake: blocks build charge that releases a rock shockwave. |
| [Svalinn's Arc](#svalinns-arc) | Parry-shield fighter | Shield with parry (buckler or round shield) | Shield Throw: throw your shield through enemies and back. |
| [Gullveig's Rebirth](#gullveigs-rebirth) | Elemental mage | Elemental Magic staff | Hexen: take the body of a Jotun Witch, with her attacks and Eitr-fuelled flight. |
| [Jotunn's Blood](#jotunns-blood) | Two-handed melee bruiser | Two-handed axe, club, sword or polearm | Grow larger and stronger, but much weaker to fire. |

## Heimdall's Legacy

*Tower-shield tank.* Undying: come back to full health when you would die, once every 20 minutes.

| Piece | Slot | Always rolls with |
|---|---|---|
| Guardian of the Gods | Shield without parry (tower shield) | Immovable |
| Son of Nine Mothers | Head | Armor (below health), Stamina Regen (below health) |
| Heart of the Bifrost | Chest | Armor, Stamina |
| Stride of the Aesir | Legs | Movement Skills, Movement Speed (below health) |

| Pieces | Bonus |
|---|---|
| 2 | Block +40% (more block armor on your shield) |
| 3 | Bulwark [Activated]: Prevent all damage for 5 seconds. Cooldown: 60 seconds. |
| 4 | Undying [Passive]: On death, regain full health. Cooldown: 20 minutes. |

Full-set look: tinted armor.

Bulwark is an activated ability: it takes one of the ability hotkeys (G, H, J by default).

## Ragnar's Fury

*Two-handed axe berserker.* Frost-dealing melee swings also hit a cone in front of you with frost.

| Piece | Slot | Always rolls with |
|---|---|---|
| Serpentsbane | Two-handed axe | Physical Damage Dealt, Imbue Frost Damage |
| Ragnar's Boneless Wrap | Cape | - |
| Ragnar's White Shirt | Chest | - |
| Ragnar's Ironsides | Legs | - |

| Pieces | Bonus |
|---|---|
| 2 | Health Regen +3/tick |
| 2 | Health Regen +50% |
| 3 | Berserker [Activated]: For 10 seconds, you cannot regenerate health, but you gain +50% to +200% damage based on health missing. Cooldown: 3 minutes. |
| 4 | Melee attacks that deal frost damage deal an additional 40% of weapon damage as frost in a cone in front of the attacker. |

Full-set look: tinted armor, aura effects.

Berserker is an activated ability: it takes one of the ability hotkeys (G, H, J by default).

## Wrath of the Thunderer

*Two-handed club.* Overwhelming Launch: hurl your melee weapon, even mid-swing, to slam down where you aim.

| Piece | Slot | Always rolls with |
|---|---|---|
| Thrudhammer | Two-handed club (sledge) | Physical Damage Dealt, Imbue Lightning Damage |
| Crown of the Thunderer | Head | - |
| Megingjord's Embrace | Chest | - |
| Tanngrisnir's Stride | Legs | - |

| Pieces | Bonus |
|---|---|
| 2 | Lightning Resistance 25% |
| 3 | Imbue Lightning Damage 20% |
| 4 | Overwhelming Launch [Activated]: Hurl your melee weapon where you aim. It slams down for its weapon damage +50% in an area, then flies back to your hand, and cannot attack until it returns. Costs 3x a normal attack. |

Full-set look: tinted armor, aura effects.

## Dverger Arsenal

*Crossbow.* Artillery: crossbow bolts become lobbed fireballs that explode where you aim.

| Piece | Slot | Always rolls with |
|---|---|---|
| Emberwright's Arbalest | Crossbow | Physical Damage Dealt, Imbue Fire Damage |
| Lantern of the Deep Halls | Head | - |
| Mistwalker's Mantle | Cape | - |
| Bolt-Bearer's Harness | Chest | - |

| Pieces | Bonus |
|---|---|
| 2 | Ammo Consumption Chance -25% |
| 2 | Crossbows Skill +10 |
| 3 | Quick Draw +20% (faster bow and crossbow draw) |
| 3 | Triple Shot (75% chance for a shot to fire three projectiles at 40% damage each, for double the cost) |
| 4 | Artillery: Your crossbow fires lobbed fireballs that come down where you aim, dealing +25 / 30 / 35% damage to everything within 2.5m of the impact. |

Full-set look: tinted armor, aura effects.

## The Trickster's Guile

*Knife assassin.* Assassin: keep your aim on an enemy to vanish and reappear behind it for a heavy strike.

| Piece | Slot | Always rolls with |
|---|---|---|
| Silvertongue | Knife | Attack Speed, Imbue Poison Damage |
| Loptr's Cowl | Head | - |
| Shapeshifter's Jerkin | Chest | - |
| Sky-Treaders | Legs | - |

| Pieces | Bonus |
|---|---|
| 2 | Noise -30% |
| 2 | Opportunist +15% (chance for hits on staggered enemies to deal backstab damage) |
| 3 | Dodge Damage Buff +25% (dodging an enemy melee attack grants a 10 second damage buff) |
| 3 | Backstab +10% |
| 4 | Assassin: Keep your aim on an enemy within 20m for 2 seconds with a melee weapon drawn to vanish and reappear behind it, striking for 200 / 225 / 250% weapon damage. Cooldown: 180 / 160 / 140 seconds. |

Full-set look: aura effects.

## Coils of the World Serpent

*Spear, poison.* Nine Steps: heavily envenomed enemies walk nine more steps and fall dead.

| Piece | Slot | Always rolls with |
|---|---|---|
| Eitrfang | Spear | Physical Damage Dealt, Imbue Poison Damage |
| Brow of the Encircler | Head | - |
| Ocean-Girdler's Scales | Chest | - |
| Wake of the Deep | Legs | - |

| Pieces | Bonus |
|---|---|
| 2 | Poison Resistance 25% |
| 2 | +20% Poison Damage |
| 3 | Coiling Venom: 50 / 60 / 70% of your physical damage is dealt as poison instead. Your poison ignores resistance and immunity, and adds to the venom already in an enemy instead of replacing it. |
| 4 | Nine Steps: When the venom in an enemy reaches 100 / 85 / 70% of its remaining health, it is doomed. It walks 9 more steps, 4.5 seconds, and falls dead. |

Full-set look: tinted armor, aura effects.

## Egil's Mark

*Long-range bow.* Sniper: arrows fly faster and hit harder the further they travel, but draw slower.

| Piece | Slot | Always rolls with |
|---|---|---|
| Kingsdread | Bow | Physical Damage Dealt, Backstab |
| Wolfdale Hood | Head | - |
| Feather-Hame | Cape | - |
| Eastward Striders | Legs | - |

| Pieces | Bonus |
|---|---|
| 2 | Bows Skill +10 |
| 2 | Draw Stamina -30% |
| 3 | Backstab +20% |
| 3 | Movement Skills +10 |
| 4 | Sniper: Your arrows fly 2 / 2.25 / 2.5x as fast, and those that hit beyond 15 m deal +0.8 / 1 / 1.2% damage per metre flown, but your bow takes 2 / 2.25 / 2.5x as long to draw. |

Full-set look: glowing weapon.

## Rime of Élivágar

*Frost mage armor.* Frostwalker: the water freezes under your feet so you can walk on it for a steady Eitr cost.

| Piece | Slot | Always rolls with |
|---|---|---|
| Aurgelmir's Cowl | Head | Eitr |
| Hvergelmir's Shroud | Chest | Eitr |
| Aurvandil's Crossing | Legs | Eitr |
| Svöl's Mantle | Cape | Eitr |

| Pieces | Bonus |
|---|---|
| 2 | +10% of Max Stamina as Max Eitr |
| 2 | Eitr Regen +15% |
| 3 | +10% of Max Health as Max Eitr |
| 4 | Frostwalker: The water freezes beneath your feet so you can walk on it, draining 3 / 2.5 / 2 Eitr per second (9 / 7.5 / 6 over the boiling sea). Crouch to break through. Your Eitr does not regenerate on or in the water. |

Full-set look: tinted armor, aura effects.

## Fylgja's Bond

*Ranger with a tame.* Spirit Animal: a bound tame fights beside you as a spirit.

| Piece | Slot | Always rolls with |
|---|---|---|
| Wolfsight | Bow | Physical Damage Dealt, Ammo Consumption Chance |
| Packfang | Any melee weapon | Physical Damage Dealt, Attack Stamina Use |
| Hood of the Watcher | Head | - |
| Hidebound Jerkin | Chest | - |
| Mantle of Kin | Cape | - |

| Pieces | Bonus |
|---|---|
| 2 | +10 Ranged Skills |
| 2 | +10 Melee Skills |
| 3 | Quick Draw +20% (faster bow and crossbow draw) |
| 3 | Attack Speed +10% |
| 4 | Spirit Animal [Activated]: Bind a tamed creature as your fylgja. In battle its spirit fights beside you at 100 / 115 / 130% strength for 120 seconds, is summoned again if the fight goes on, and returns 30 seconds after it falls. |

Full-set look: glowing weapon, falling leaves around you.

Fylgja's Bond has two weapon pieces, a bow and a melee weapon. Either one counts toward the set, so the full set is one weapon plus the three armor pieces.

## Freyja's Falcon

*Spear, aerial.* Earthshaker: dive from the air and slam the ground where you aim.

| Piece | Slot | Always rolls with |
|---|---|---|
| Valfreyja's Talon | Spear | Physical Damage Dealt, Attack Stamina Use |
| Helm of Sessrúmnir | Head | - |
| Hauberk of the Host-Field | Chest | - |
| Valshamr | Cape | - |

| Pieces | Bonus |
|---|---|
| 2 | Spears Skill +20 |
| 2 | Piercing Resistance 20% |
| 3 | Air Jump (jump once more while in the air) |
| 3 | Jump Stamina Use -25% |
| 4 | Earthshaker [Activated]: In mid-air, dive where you aim, steering with your aim as you fall, and slam the ground for 200 / 250 / 300 damage, half blunt and half pierce, within 5m, +1m per 15m fallen. +1% damage per 10 weight carried over 300 (up to +300%), and +2% per metre fallen past 5m. Diving with less than 3m of clear air below you deals 50% less damage. Costs 25 stamina. |

Full-set look: tinted armor.

## Bölþorn's Bane

*Crossbow.* Piercing Bolts: crossbow bolts pass through every creature in their path.

| Piece | Slot | Always rolls with |
|---|---|---|
| Balethorn | Crossbow | Physical Damage Dealt, Pierce Damage |
| Bölþorn's Brow | Head | - |
| Thornward Harness | Chest | - |
| Rootbound Greaves | Legs | - |

| Pieces | Bonus |
|---|---|
| 2 | Crossbows Skill +10 |
| 2 | Projectile Speed +20% |
| 3 | Quick Draw +20% (faster bow and crossbow draw) |
| 3 | Stagger Damage +25% (more damage to staggered enemies) |
| 4 | Piercing Bolts: Your crossbow bolts pass through every creature in their path, striking each once and keeping 75 / 85 / 95% of their damage after each one. |

Full-set look: tinted armor, aura effects.

## Heiðr's Seiðr

*Mage with a bow.* Conjured Arrows: your bow fires frost arrows made of Eitr instead of real arrows.

| Piece | Slot | Always rolls with |
|---|---|---|
| Spellstring | Bow | Imbue Frost Damage, Elemental Damage Dealt |
| Völva's Cowl | Head | - |
| Seiðkona's Robe | Chest | - |
| Mantle of Far-Sight | Cape | - |

| Pieces | Bonus |
|---|---|
| 2 | Elemental Magic Skill +10 |
| 2 | Eitr Regen +20% |
| 3 | Elemental Archery: Your bows use Elemental Magic instead of Bows for damage, draw speed and stamina, and hitting creatures with them trains Elemental Magic. |
| 4 | Conjured Arrows: Your bows fire frost arrows conjured from eitr instead of real arrows, adding 80 / 90 / 100% of the bow's damage as frost. Each shot costs 8 Eitr, less with Elemental Magic skill. |

Full-set look: tinted armor, aura effects, glowing hands, conjured arrow on the bowstring.

## Mímir's Well

*Eitr-tank mage armor.* Eitr Barrier: part of every hit is taken from your Eitr before your armor.

| Piece | Slot | Always rolls with |
|---|---|---|
| Mímir's Counsel | Head | - |
| Valföðr's Pledge | Chest | - |
| Wraps of the Third Root | Legs | - |
| Mantle of the Hidden Horn | Cape | - |

| Pieces | Bonus |
|---|---|
| 2 | Eitr +25 |
| 2 | 20% of Max Health converted to Max Eitr |
| 3 | Deep Well: +30% Max Eitr, -30% Eitr Regen |
| 3 | Eitr Leech 5% |
| 4 | Eitr Barrier: A purple barrier takes 25 / 30 / 35% of every hit from your Eitr before your block, resistances and armor, absorbing 1 damage per Eitr. It collapses for 5s whenever your Eitr runs dry. |

Full-set look: tinted armor, aura effects.

## Einherjar

*Spellsword.* Eitr Infusion: pour your Eitr into your weapon for a spell of extra damage.

| Piece | Slot | Always rolls with |
|---|---|---|
| Valgrind | Sword (one- or two-handed) | Parry, Elemental Damage Dealt |
| Helm of the Chosen | Head | Eitr |
| Glaðsheimr Hauberk | Chest | Eitr |
| Vígríðr Greaves | Legs | Eitr |

| Pieces | Bonus |
|---|---|
| 2 | Eitr Weaving +30 (parrying restores Eitr) |
| 2 | Spellblade +15 (half of each attack's cost is paid in Eitr instead, and the attack deals more damage) |
| 3 | Eitr Leech 5% |
| 3 | Interconnected: the skill of the weapon in your hands gains 20 / 25 / 30% of its partner skill's level (see [Interconnected](#interconnected)). |
| 4 | All Skills +5 |
| 4 | Eitr Infusion [Activated]: Pour all your Eitr into your weapon, at least 20, for a second of infusion per 4 Eitr. While infused your hits deal +40 / 45 / 50% of the weapon's highest damage type as that type, or as fire, frost or lightning if you were burning, frozen or shocked, or near a fire, when it began. Your Eitr does not regenerate, Spellsword swings cost no Eitr, and pressing again pours in the Eitr you have regained. |

Full-set look: tinted armor.

## Ullr's Hunt

*Bow plus axe or polearm.* Quarry Mark: arrows mark creatures for a heavy axe or polearm hit.

| Piece | Slot | Always rolls with |
|---|---|---|
| Ýdalir | Bow | Physical Damage Dealt, Quick Draw |
| Öndurgoð's Hood | Head | - |
| Ullr's Hunting Jerkin | Chest | - |
| Skíðfari Leggings | Legs | - |

| Pieces | Bonus |
|---|---|
| 2 | Bows Skill +10 |
| 2 | Axes Skill +10 |
| 3 | Interconnected: the skill of the weapon in your hands gains 20 / 25 / 30% of its partner skill's level (see [Interconnected](#interconnected)). |
| 3 | Sprint Stamina Use -25% |
| 4 | Quarry Mark: Your arrows mark creatures for 10s. Your next axe or polearm hit on a marked creature deals +100 / 125 / 150% damage and restores 25 stamina. |

## Hel's Covenant

*Blood mage.* Hel's Harvest: creatures you or your summons kill rise as thralls that fight for you.

| Piece | Slot | Always rolls with |
|---|---|---|
| Gjallarbrú | Blood Magic staff | Summons Damage, Summons Health |
| Hood of Hunger | Head | - |
| Blíkjandaböl | Chest | - |
| Kör Mantle | Cape | - |

| Pieces | Bonus |
|---|---|
| 2 | Blood Magic Skill +10 |
| 2 | Attack Health Use -20% |
| 3 | Interconnected: the skill of the weapon in your hands gains 20 / 25 / 30% of its partner skill's level (see [Interconnected](#interconnected)). |
| 3 | Lifesteal 5% |
| 4 | Hel's Harvest: Creatures slain by you or your summons rise as thralls of the same star level that fight for you for 30 seconds, stronger with Blood Magic skill. Up to 2 / 3 / 4 at once; a new one replaces the oldest. |

Full-set look: tinted armor, aura effects.

## Huldufólk

*Knife rogue.* Huge sneak attack damage, on top of vanishing in smoke when you dodge.

| Piece | Slot | Always rolls with |
|---|---|---|
| Huldra's Thorn | Knife | Backstab, Opportunist |
| Veil of the Hidden Folk | Head | - |
| Hollowback Jerkin | Chest | - |
| Cowtail Cloak | Cape | - |

| Pieces | Bonus |
|---|---|
| 2 | Knives Skill +10 |
| 2 | Crossbows Skill +10 |
| 3 | Interconnected: the skill of the weapon in your hands gains 20 / 25 / 30% of its partner skill's level (see [Interconnected](#interconnected)). |
| 3 | Vanish: Dodging near an alerted enemy fills the air with smoke, and enemies within 15m lose sight of you for 3 / 4 / 5 seconds. Your next hit from hiding is a sneak attack. Cooldown: 20 seconds. |
| 4 | Backstab +150 / 200 / 250% |

## Surtr's Brand

*Two-handed axe or sword, fire.* Firebrand: combo finishers and secondary attacks erupt in Brenna's fire.

| Piece | Slot | Always rolls with |
|---|---|---|
| Sun of the Slain | Two-handed axe or sword | Physical Damage Dealt, Imbue Fire Damage |
| Crown of Muspell | Head | - |
| Unquenched Heart | Chest | - |
| Ashwalker Greaves | Legs | - |

| Pieces | Bonus |
|---|---|
| 2 | Heat Resistance +25% |
| 2 | Fire Resistance 25% |
| 3 | Imbue Fire Damage 20% |
| 3 | Executioner +100% (the first hit on an enemy below 20% health deals extra damage) |
| 4 | Firebrand: The third swing of every combo with a two-handed axe or sword erupts in Brenna's fire, burning enemies within 3m for 75 / 100 / 125% of your weapon's damage. Your secondary attack erupts for 37.5 / 50 / 62.5%. |

Full-set look: tinted armor, glowing weapon.

## Hel's Court

*Blood fighter armor.* Life Siphon: drained enemies cannot heal, and everyone hitting them heals.

| Piece | Slot | Always rolls with |
|---|---|---|
| Móðguðr's Toll | Head | - |
| Éljúðnir | Chest | - |
| Fallanda Forað | Legs | - |
| Ganglati's Train | Cape | - |

| Pieces | Bonus |
|---|---|
| 2 | Attack Health Use -20% |
| 2 | Blood Magic Skill +10 |
| 3 | Lifesteal 5% |
| 3 | Bloodletting (attacks cost health instead of stamina) |
| 4 | Life Siphon: Your hits drain a foe for 30 seconds. While drained it cannot heal, and every player who hits it heals for 4 / 5 / 6% of the damage dealt. |

Full-set look: tinted armor.

## Hrungnir's Stand

*Tower-shield blocker.* Kinetic Quake: blocks build charge that releases a rock shockwave.

| Piece | Slot | Always rolls with |
|---|---|---|
| Grjótúnagarðar | Shield without parry (tower shield) | Block, Block Force |
| Whetstone Crown | Head | - |
| Three-Cornered Heart | Chest | - |
| Mokkurkálfi's Stance | Legs | - |

| Pieces | Bonus |
|---|---|
| 2 | Block +30% (more block armor on your shield) |
| 2 | Blocking Skill +15 |
| 3 | Block Stamina Use -25% |
| 3 | Thorn Damage +20% (reflects part of the damage you take back to the attacker) |
| 4 | Kinetic Quake: Each attack you block stores a charge. At 16 / 14 / 12 charges, the next attack you block releases a rock shockwave 12m ahead, dealing 300% of your block power as blunt damage. |

Full-set look: tinted armor.

## Svalinn's Arc

*Parry-shield fighter.* Shield Throw: throw your shield through enemies and back.

| Piece | Slot | Always rolls with |
|---|---|---|
| Svalinn | Shield with parry (buckler or round shield) | Block, Parry |
| Árvakr's Brow | Head | - |
| Alsviðr's Harness | Chest | - |
| Sól's Stride | Legs | - |

| Pieces | Bonus |
|---|---|
| 2 | Blocking Skill +15 |
| 2 | Block Stamina Use -25% |
| 3 | Stagger Damage Taken -30% |
| 4 | Shield Throw [Activated]: Hurl your shield where you aim. It flies through enemies until it strikes something solid, then returns to your hand, hitting each enemy on the way out and again on the way back for 100 / 125 / 150% of your block power as blunt damage. Damage you block fills a gauge that the throw spends for up to +300% damage. You cannot block until it returns. Costs 20 stamina. |

Full-set look: tinted armor.

## Gullveig's Rebirth

*Elemental mage.* Hexen: take the body of a Jotun Witch, with her attacks and Eitr-fuelled flight.

| Piece | Slot | Always rolls with |
|---|---|---|
| Spear of the Pyre | Elemental Magic staff | Fire Rate, Eitr Regen |
| Thrice-Burned Cowl | Head | Eitr |
| Robe of the Third Pyre | Chest | Eitr |
| Mantle of the Lingering Flame | Cape | Eitr |

| Pieces | Bonus |
|---|---|
| 2 | Elemental Magic Skill +10 |
| 2 | Eitr Regen +20% |
| 3 | Interconnected: the skill of the weapon in your hands gains 20 / 25 / 30% of its partner skill's level (see [Interconnected](#interconnected)). |
| 3 | Attack Eitr Use -20% |
| 4 | Hexen: You wear the body of a Jotun Witch for as long as you wear the set. Your attacks become her magic blast and lightning bolt, and block or sneak with jump blinks you the way you move. Jump to take flight, holding jump or crouch to rise or sink. Flight drains 3 / 2.5 / 2 Eitr per second, needs 10 to take off, and when your Eitr runs dry you sink to the ground. |

## Jotunn's Blood

*Two-handed melee bruiser.* Grow larger and stronger, but much weaker to fire.

| Piece | Slot | Always rolls with |
|---|---|---|
| Arms of the Jotunn | Two-handed axe, club, sword or polearm | - |
| Helm of the Jotunn | Head | - |
| Chest of the Jotunn | Chest | - |
| Legs of the Jotunn | Legs | - |

| Pieces | Bonus |
|---|---|
| 2 | Jotunn's Blood +5% (gain +5% Increased Size, +5% Carry Weight, +20% Health Regen, +2.5% Physical Damage Dealt) |
| 2 | Slightly weak VS Fire |
| 3 | Jotunn's Blood +10% (gain +10% Increased Size, +10% Carry Weight, +40% Health Regen, +5% Physical Damage Dealt) |
| 3 | Weak VS Fire |
| 4 | Jotunn's Blood +15% (gain +15% Increased Size, +15% Carry Weight, +60% Health Regen, +7.5% Physical Damage Dealt) |
| 4 | Very weak VS Fire |

The three Jotunn's Blood bonuses add up, so the full set gives +30% size, +30% of your base carry weight, +120% health regen and +15% physical damage. Your melee reach grows with your size, and the extra weight means hits knock you back less. Only the strongest fire weakness applies, and any fire resistance from a mead or armor cancels it.
