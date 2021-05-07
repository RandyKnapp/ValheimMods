## TODO

- [X] Streamline enchanting UI. Use selectable rarity per item.
- [X] Configurable magic effects
- [X] More exclusions and configurable requirements for magic effects
- [X] Show current equipment with tooltip
- [X] Augment items: change/reroll magic item effects (transmute? modify?)
- [ ] Infuse items: rarity to next rarity (like diablo upgrade rare to legendary)
- [X] List of active magic effects on the player status screen
- [X] Limit drop types by actual player progression
- [X] Gamble for magic items from Merchant
- [ ] Custom crafting station for enchanting
- [ ] Create effects for in-game models of magic items
- [ ] Balance, balance, balance
- [ ] Move tooltip code to postfix, parse and inject rather than redo from scratch
- [ ] Custom item sets (replace troll too)
- [X] Rename item if magic (Legendary still to-do)
- [ ] New Runes skill (enchanting)
- [ ] New Seidr skill (for what?)

## Magic Effects:

#### Weapons
- [X] Paralyze (not rolling, too powerful)
- [ ] Add Knockback
- [ ] Slow
- [ ] Blink (bow or spear, teleport to impact point)
- [X] Life steal
- [ ] Exploding shot (bows, deal aoe damage on arrow impact)
- [ ] Multishot (bows, shoot multiple arrows from one)
- [ ] Quick draw (bows, draw speed dramatically increased)
- [ ] Recall (spear, automaticallyl pick up spear after throwing)
- [ ] Immobilize
- [X] Modify Attack Speed
- [ ] Increase damage vs staggered enemies
- [ ] Duelist (sword, when off hand is empty, increase parry and block power by a lot)
- [ ] Opportunist (knife, add backstab damaged to staggered enemies)
- [X] Throwing (change alterate attack to throw like spear)
- [ ] Increase stagger duration
- [ ] Blind
- [ ] Immovable (tower shield, immune to stagger and knockback while blocking)
- [ ] Glowing

#### Armor
- [ ] Warm (Prevent freezing effect)
- [X] Waterproof (cape, prevent Wet effect from rain)
- [ ] Waterwalk (legs)
- [X] Double Jump (legs)
- [ ] Thorns damage
- [ ] Sneak increase (legs)
- [ ] Dodge improvement (legs)
- [ ] Feather Fall (legs)
- [ ] Nightvision (helmet)
- [ ] Discovery radius increase (helmet)
- [ ] Quick learner (helmet, increase xp gain)
- [ ] Increase armor when below HP threshold
- [ ] % chance to stagger attackers
- [ ] % chance to ignore incoming damage
- [ ] Improve Skill Level
- [ ] Comfortable (Increase Comfort level when resting)
- [ ] Glowing

#### Utility
- [ ] Luck (increase drop rate and rarity chance)
- [ ] Riches (add chance to drop treasure on all mobs)
- [ ] Add bonus armor (magic shield?)

#### Tools
- [ ] Build freedom (hammer, don't require crafting station)
- [ ] No stamina cost

## Legendary Items:

- [ ] Sleipnir's Hoof (Club, increase move speed)
- [ ] Gungnir (Ancient Bark Spear)
- [ ] Mjolnir (Iron Sledge)
- [ ] Skofnung (Iron Sword)
- [ ] Dainslief (Silver Sword)
- [ ] Angurvadal (Sword)
- [ ] Vidar's Shoes (Iron Legs, huge kick damage boost?)
- [ ] Skidbaldnir (ship that can turn into an item)
- [ ] Hofund (Sword, charges with each kill, then discharges on heavy attack)
- [ ] Gjallarhorn (Tankard, when used (how?) makes all enemies flee from the player)
- [ ] Draupner (Ring, Luck and increased gold drops)
- [ ] Grídarvöl (staff/club, ??? nobody knows that this does?)
- [ ] Járngreipr (gauntlets (utility?), Fire-immunity, can throw any weapon?)
- [ ] Sword of Freyr (sword)
- [ ] Falcon Cloak of Freyja (cape, actually transform into bird?!)
- [ ] Brísingamen (necklace/utility, no enemies attack you, unless attacked)
- [ ] Eldhrimnir (cauldron material, used to built a special cauldron that makes double or triple amount)
- [ ] Odrerir (fermentor material, unlocks the Mead of Poetry recipe and allows fermenting the mead of poetry)
- [ ] Mead of Poetry (mead, double XP for a time)
- [ ] Aegishjalmarr (helm, ability to paralyze all enemies in range)
- [ ] Gram/Fafnir's Bane (sword)
- [ ] Ridill and Hrotti (paired swords, just two swords? set item swords? can't even dual wield in game)
- [ ] Refil (sword)
- [ ] Balmung (sword)
- [ ] Shield of Nuodung (shield)
- [ ] Naglhring (sword)
- [ ] Ekkisax (sword)
- [ ] Hildigrim (helm)
- [ ] Lagulf (sword)
- [ ] Blodgang (sword)
- [ ] Finnsleif (armor)
- [ ] Hildigolt (helm)
- [ ] Hildisvin (helm)
- [ ] Ring of Helgi (ring, waterbreathing? Frost immunity?)
- [ ] Sviagris (ring)
- [ ] Tarnkappe (cape, "Cloak of Darkness" sneaking?)
- [ ] Mimung (sword)
- [ ] Naegling (sword, Beowulf, twigs and serpents design)


## Basic Item Sets:

none (do I even want to do this? I don't think so, not at this point)

## Legendary Sets:

**Heimdall (tank):** Tower Shield (Guardian of the Gods), Head (Son of Nine Mothers), Chest (Heart of the Bifrost), Legs (Stride of the Aesir)

2. +200% Block Power
3. Bulwark - activate: take no damage for N seconds
4. Undying - On death, gain full health instantly, but long cooldown

**Ragnar (aoe):** Battleaxe (Serpentsbane), Cape (Ragnar's Boneless Wrap), Chest (Ragnar's White Shirt), Legs (Ragnar's Ironsides)

2. +2 HP/tick, +100% health regen
3. Attacks that deal frost damage deal 50% of their damage in an aoe around the attack
4. Berserker - activate: stop all health regen but gain +50% to +200% damage based on health missing

**Bloodaxe (dps):** Knife (Fratricide), Head (Bloodaxe's Crown), Cape (Bloodaxe's Royal Mantle), Legs (Bloodaxe's Boots)

2. +25% attack speed, -50% attack stamina use
3. +200% Lightning Damage
4. All lightning damage has a chance to paralyze enemies

**Agilaz (bow):** Bow (King's Warning/Second Arrow), Cape (Völund's Wings), Chest (Skadi's Hunting Coat), Legs (Ullr's Hunting Skis)

2. Bow draw speed reduced by +50%
3. Enemies with two or more negative effects take +30% damage from all sources
4. Frost and Poison effects last twice as long

**Eir (healing):** Head (Gaze of Frigg), Chest (Feast of Protection), Legs (Arrival of Aid), Cape (Wings of the Valkyrie)

2. Totem - activate: drop a healing totem where you stand that applies rapid health regen to allies within it
3. Can drop two totems simultaneously
4. Enemies in totem area are slowed by -30%

**Wayfarer (exploration):** Head (), Chest (), Legs(), Cape ()

2. Movement speed increase, sprint stamina reduction
3. Discovery Range Increase
4. activate: For N minutes, all allies in range get +500 carry weight and all movement speed penalties removed

**Coxswain (sailing):** Head (), Chest (), Legs(), Cape ()

2. Increase boat speed and turning speed
3. Wind is always at back (like moder)
4. Summon indestructible longboat (in water), destroys previously summoned one
