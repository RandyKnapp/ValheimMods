using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicLoot
{
    public enum MagicEffectType
    {
        DvergerCirclet,
        Megingjord,
        Wishbone,
        ModifyDamage,
        ModifyPhysicalDamage,
        ModifyElementalDamage,
        ModifyDurability,
        ReduceWeight,
        RemoveSpeedPenalty,
        ModifyBlockPower,
        ModifyParry,
        ModifyArmor,
        ModifyBackstab,
        IncreaseHealth,
        IncreaseStamina,
        ModifyHealthRegen,
        ModifyStaminaRegen,
        AddBluntDamage,
        AddSlashingDamage,
        AddPiercingDamage,
        AddFireDamage,
        AddFrostDamage,
        AddLightningDamage,
        AddPoisonDamage,
        AddSpiritDamage,
        AddFireResistance,
        AddFrostResistance,
        AddLightningResistance,
        AddPoisonResistance,
        AddSpiritResistance,
        ModifyMovementSpeed,
        ModifySprintStaminaUse,
        ModifyJumpStaminaUse,
        ModifyAttackStaminaUse,
        ModifyBlockStaminaUse,
        Indestructible,
        Weightless,
        AddCarryWeight,

        MagicEffectEnumEnd
    }

    public static partial class MagicItemEffectDefinitions
    {
        public static event Action OnSetupMagicItemEffectDefinitions;

        public static void SetupMagicItemEffectDefinitions()
        {
            InitializeSortedDefs();

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.DvergerCirclet,
                DisplayText = "Perpetual lightsource",
                Comment = "Can't be rolled. Just added to make a Legendary Magic Item version of this item."
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.Megingjord,
                DisplayText = "Carry weight increased by +150",
                Comment = "Can't be rolled. Just added to make a Legendary Magic Item version of this item."
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.Wishbone,
                DisplayText = "Finds secrets",
                Comment = "Can't be rolled. Just added to make a Legendary Magic Item version of this item."
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.ModifyDamage,
                DisplayText = "All damage increased by +{0:0.#}%",
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 15, MaxValue = 25, Increment = 1 } }
                },
                Comment = "Can't be rolled. Too powerful?"
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.ModifyPhysicalDamage,
                DisplayText = "Physical damage increased by +{0:0.#}%",
                AllowedItemTypes = Weapons,
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 15, MaxValue = 25, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.ModifyElementalDamage,
                DisplayText = "Elemental damage increased by +{0:0.#}%",
                AllowedItemTypes = Weapons,
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 15, MaxValue = 25, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.ModifyDurability,
                DisplayText = "Max durability increased by +{0:0.#}%",
                AllowedItemTypes = Weapons.Concat(Shields).Concat(Tools).Concat(Armor).ToList(),
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 50, MaxValue = 100, Increment = 5 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 50, MaxValue = 100, Increment = 5 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 50, MaxValue = 100, Increment = 5 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.ReduceWeight,
                DisplayText = "Weight reduced by -{0:0.#}% ",
                AllowedItemTypes = Weapons.Concat(Shields).Concat(Tools).Concat(Armor).ToList(),
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 70, MaxValue = 90, Increment = 5 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 70, MaxValue = 90, Increment = 5 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 70, MaxValue = 90, Increment = 5 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.RemoveSpeedPenalty,
                DisplayText = "Movement speed penalty removed",
                AllowedItemTypes = Weapons.Concat(Shields).Concat(Tools).Concat(Armor).ToList(),
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.ModifyBlockPower,
                DisplayText = "Block improved by +{0:0.#}%",
                AllowedItemTypes = Weapons.Concat(Shields).ToList(),
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 15, MaxValue = 25, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.ModifyParry,
                DisplayText = "Parry improved by +{0:0.#}%",
                AllowedItemTypes = Weapons.Concat(Shields).ToList(),
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 15, MaxValue = 25, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.ModifyArmor,
                DisplayText = "Armor increased by +{0:0.#}%",
                AllowedItemTypes = Armor,
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 15, MaxValue = 25, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.ModifyBackstab,
                DisplayText = "Backstab improved by +{0:0.#}%",
                AllowedItemTypes = { ItemDrop.ItemData.ItemType.OneHandedWeapon, ItemDrop.ItemData.ItemType.Bow },
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 15, MaxValue = 25, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.IncreaseHealth,
                DisplayText = "Health increased by +{0:0}",
                AllowedItemTypes = Armor.Concat(Shields).ToList(),
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 25, Increment = 5 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 15, MaxValue = 30, Increment = 5 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 20, MaxValue = 35, Increment = 5 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 25, MaxValue = 50, Increment = 5 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.IncreaseStamina,
                DisplayText = "Stamina increased by +{0:0}",
                AllowedItemTypes = Armor.Concat(Tools).ToList(),
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 25, Increment = 5 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 15, MaxValue = 30, Increment = 5 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 20, MaxValue = 35, Increment = 5 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 25, MaxValue = 50, Increment = 5 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.ModifyHealthRegen,
                DisplayText = "Health regen improved by +{0:0.#}%",
                AllowedItemTypes = Armor,
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 15, MaxValue = 25, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.ModifyStaminaRegen,
                DisplayText = "Stamina regen improved by +{0:0.#}%",
                AllowedItemTypes = Armor.Concat(Tools).ToList(),
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 20, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 15, MaxValue = 25, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.AddBluntDamage,
                DisplayText = "Add +{0:0.#} blunt damage",
                AllowedItemTypes = Weapons,
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 5, MaxValue = 10, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 8, MaxValue = 13, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 12, MaxValue = 17, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 16, MaxValue = 21, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.AddSlashingDamage,
                DisplayText = "Add +{0:0.#} slashing damage",
                AllowedItemTypes = Weapons,
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 5, MaxValue = 10, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 8, MaxValue = 13, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 12, MaxValue = 17, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 16, MaxValue = 21, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.AddPiercingDamage,
                DisplayText = "Add +{0:0.#} piercing damage",
                AllowedItemTypes = Weapons,
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 5, MaxValue = 10, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 8, MaxValue = 13, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 12, MaxValue = 17, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 16, MaxValue = 21, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.AddFireDamage,
                DisplayText = "Add +{0:0.#} fire damage",
                AllowedItemTypes = Weapons,
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 5, MaxValue = 10, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 8, MaxValue = 13, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 12, MaxValue = 17, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 16, MaxValue = 21, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.AddFrostDamage,
                DisplayText = "Add +{0:0.#} frost damage",
                AllowedItemTypes = Weapons,
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 5, MaxValue = 10, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 8, MaxValue = 13, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 12, MaxValue = 17, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 16, MaxValue = 21, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.AddLightningDamage,
                DisplayText = "Add +{0:0.#} lightning damage",
                AllowedItemTypes = Weapons,
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 5, MaxValue = 10, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 8, MaxValue = 13, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 12, MaxValue = 17, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 16, MaxValue = 21, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.AddPoisonDamage,
                DisplayText = "Add +{0:0.#} poison damage",
                AllowedItemTypes = Weapons,
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 5, MaxValue = 10, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 8, MaxValue = 13, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 12, MaxValue = 17, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 16, MaxValue = 21, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.AddSpiritDamage,
                DisplayText = "Add +{0:0.#} spirit damage",
                AllowedItemTypes = Weapons,
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 5, MaxValue = 10, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 8, MaxValue = 13, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 12, MaxValue = 17, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 16, MaxValue = 21, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.AddFireResistance,
                DisplayText = "Gain fire resistance",
                AllowedItemTypes = Armor.Concat(Shields).ToList(),
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef()},
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef()},
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef()},
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef()}
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.AddFrostResistance,
                DisplayText = "Gain frost resistance",
                AllowedItemTypes = Armor.Concat(Shields).ToList(),
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef()},
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef()},
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef()},
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef()}
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.AddLightningResistance,
                DisplayText = "Gain lightning resistance",
                AllowedItemTypes = Armor.Concat(Shields).ToList(),
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef()},
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef()},
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef()},
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef()}
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.AddPoisonResistance,
                DisplayText = "Gain poison resistance",
                AllowedItemTypes = Armor.Concat(Shields).ToList(),
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef()},
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef()},
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef()},
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef()}
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.AddSpiritResistance,
                DisplayText = "Gain spirit resistance",
                AllowedItemTypes = Armor.Concat(Shields).ToList(),
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.ModifyMovementSpeed,
                DisplayText = "Movement increased by +{0:0.#}%",
                AllowedItemTypes = new List<ItemDrop.ItemData.ItemType>() { ItemDrop.ItemData.ItemType.Legs },
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 5, MaxValue = 15, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 15, MaxValue = 30, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 30, MaxValue = 45, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 45, MaxValue = 60, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.ModifySprintStaminaUse,
                DisplayText = "Reduce sprint stamina use by -{0:0.#}%",
                AllowedItemTypes = new List<ItemDrop.ItemData.ItemType>() { ItemDrop.ItemData.ItemType.Legs },
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 5, MaxValue = 10, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 8, MaxValue = 13, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 11, MaxValue = 16, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 14, MaxValue = 19, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.ModifyJumpStaminaUse,
                DisplayText = "Reduce jump stamina use by -{0:0.#}%",
                AllowedItemTypes = new List<ItemDrop.ItemData.ItemType>() { ItemDrop.ItemData.ItemType.Legs },
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 5, MaxValue = 10, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 8, MaxValue = 13, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 11, MaxValue = 16, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 14, MaxValue = 19, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.ModifyAttackStaminaUse,
                DisplayText = "Reduce attack stamina use by -{0:0.#}%",
                AllowedItemTypes = Weapons.Concat(Tools).ToList(),
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 5, MaxValue = 10, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 8, MaxValue = 13, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 11, MaxValue = 16, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 14, MaxValue = 19, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.ModifyBlockStaminaUse,
                DisplayText = "Reduce block stamina use by -{0:0.#}%",
                AllowedItemTypes = Shields,
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 5, MaxValue = 10, Increment = 1 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 8, MaxValue = 13, Increment = 1 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 11, MaxValue = 16, Increment = 1 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 14, MaxValue = 19, Increment = 1 } }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.Indestructible,
                DisplayText = "Indestructible",
                AllowedItemTypes = Weapons.Concat(Tools).Concat(Shields).Concat(Armor).ToList(),
                ValuesPerRarity = {
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.Weightless,
                DisplayText = "Weightless",
                AllowedItemTypes = Weapons.Concat(Tools).Concat(Shields).Concat(Armor).ToList(),
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() }
                }
            });

            Add(new MagicItemEffectDefinition()
            {
                Type = MagicEffectType.AddCarryWeight,
                DisplayText = "Increase carry weight by +{0}",
                AllowedItemTypes = Armor,
                ValuesPerRarity = {
                    { ItemRarity.Magic,     new MagicItemEffectDefinition.ValueDef() { MinValue = 5, MaxValue = 10, Increment = 5 } },
                    { ItemRarity.Rare,      new MagicItemEffectDefinition.ValueDef() { MinValue = 10, MaxValue = 15, Increment = 5 } },
                    { ItemRarity.Epic,      new MagicItemEffectDefinition.ValueDef() { MinValue = 15, MaxValue = 25, Increment = 5 } },
                    { ItemRarity.Legendary, new MagicItemEffectDefinition.ValueDef() { MinValue = 20, MaxValue = 50, Increment = 5 } }
                }
            });

            OnSetupMagicItemEffectDefinitions?.Invoke();
        }
    }
}
