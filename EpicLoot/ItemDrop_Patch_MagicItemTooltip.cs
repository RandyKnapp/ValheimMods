using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using EpicLoot.Crafting;
using EpicLoot.LegendarySystem;
using ExtendedItemDataFramework;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot
{
    // Set the topic of the tooltip with the decorated name
    //public void CreateItemTooltip(ItemDrop.ItemData item, UITooltip tooltip) => tooltip.Set(item.m_shared.m_name, item.GetTooltip());
    [HarmonyPatch(typeof(InventoryGrid), "CreateItemTooltip", typeof(ItemDrop.ItemData), typeof(UITooltip))]
    public static class InventoryGrid_CreateItemTooltip_MagicItemComponent_Patch
    {
        public static bool Prefix(ItemDrop.ItemData item, UITooltip tooltip)
        {
            string tooltipText;
            if (item.IsEquipable() && !item.m_equiped && Player.m_localPlayer != null && Player.m_localPlayer.HasEquipmentOfType(item.m_shared.m_itemType) && Input.GetKey(KeyCode.LeftControl))
            {
                var otherItem = Player.m_localPlayer.GetEquipmentOfType(item.m_shared.m_itemType);
                tooltipText = item.GetTooltip() + $"\n\n<color=#AAA><i>Currently Equipped:</i></color>\n<size=18>{otherItem.GetDecoratedName()}</size>\n" + otherItem.GetTooltip();
            }
            else
            {
                tooltipText = item.GetTooltip();
            }
            tooltip.Set(item.GetDecoratedName(), tooltipText);
            return false;
        }
    }

    // Set the content of the tooltip
    //public static string GetTooltip(ItemDrop.ItemData item, int qualityLevel, bool crafting)
    [HarmonyPatch(typeof(ItemDrop.ItemData), "GetTooltip", typeof(ItemDrop.ItemData), typeof(int), typeof(bool))]
    public static class MagicItemTooltip_ItemDrop_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(ref string __result, ItemDrop.ItemData item, int qualityLevel, bool crafting)
        {
            if (!item.IsMagic())
            {
                return true;
            }

            var localPlayer = Player.m_localPlayer;
            var text = new StringBuilder(256);

            var magicItem = item.GetMagicItem();
            var magicColor = magicItem.GetColorString();
            var itemTypeName = magicItem.GetItemTypeName(item.Extended());

            text.Append($"<color={magicColor}>{magicItem.GetRarityDisplay()} {itemTypeName}</color>\n");
            if (item.IsLegendarySetItem())
            {
                text.Append($"<color={EpicLoot.GetSetItemColor()}>$mod_epicloot_legendarysetlabel</color>\n");
            }
            text.Append(item.GetDescription());
            
            text.Append("\n");
            if (item.m_shared.m_dlc.Length > 0)
            {
                text.Append("\n<color=aqua>$item_dlc</color>");
            }

            ItemDrop.ItemData.AddHandedTip(item, text);
            if (item.m_crafterID != 0L)
            {
                text.AppendFormat("\n$item_crafter: <color=orange>{0}</color>", item.m_crafterName);
            }

            if (!item.m_shared.m_teleportable)
            {
                text.Append("\n<color=orange>$item_noteleport</color>");
            }

            if (item.m_shared.m_value > 0)
            {
                text.AppendFormat("\n$item_value: <color=orange>{0} ({1})</color>", item.GetValue(), item.m_shared.m_value);
            }

            var weightColor = magicItem.HasEffect(MagicEffectType.ReduceWeight) || magicItem.HasEffect(MagicEffectType.Weightless) ? magicColor : "orange";
            text.Append($"\n$item_weight: <color={weightColor}>{item.GetWeight():0.0}</color>");

            if (item.m_shared.m_maxQuality > 1)
            {
                text.AppendFormat("\n$item_quality: <color=orange>{0}</color>", qualityLevel);
            }

            var indestructible = magicItem.HasEffect(MagicEffectType.Indestructible);
            if (!indestructible && item.m_shared.m_useDurability)
            {
                var maxDurabilityColor1 = magicItem.HasEffect(MagicEffectType.ModifyDurability) ? magicColor : "orange";
                var maxDurabilityColor2 = magicItem.HasEffect(MagicEffectType.ModifyDurability) ? magicColor : "yellow";

                var maxDurability = item.GetMaxDurability(qualityLevel);
                var durability = item.m_durability;
                var currentDurabilityPercentage = item.GetDurabilityPercentage() * 100f;
                var durabilityPercentageString = currentDurabilityPercentage.ToString("0");
                var durabilityValueString = durability.ToString("0");
                var durabilityMaxString = maxDurability.ToString("0");
                text.Append($"\n$item_durability: <color={maxDurabilityColor1}>{durabilityPercentageString}%</color> <color={maxDurabilityColor2}>({durabilityValueString}/{durabilityMaxString})</color>");

                if (item.m_shared.m_canBeReparied)
                {
                    var recipe = ObjectDB.instance.GetRecipe(item);
                    if (recipe != null)
                    {
                        var minStationLevel = recipe.m_minStationLevel;
                        text.AppendFormat("\n$item_repairlevel: <color=orange>{0}</color>", minStationLevel.ToString());
                    }
                }
            }
            else if (indestructible)
            {
                text.Append($"\n$item_durability: <color={magicColor}>Indestructible</color>");
            }

            var magicBlockPower = magicItem.HasEffect(MagicEffectType.ModifyBlockPower);
            var magicBlockColor1 = magicBlockPower ? magicColor : "orange";
            var magicBlockColor2 = magicBlockPower ? magicColor : "yellow";
            var magicParry = magicItem.HasEffect(MagicEffectType.ModifyParry);
            var totalParryBonusMod = magicItem.GetTotalEffectValue(MagicEffectType.ModifyParry, 0.01f);
            var magicParryColor = magicParry ? magicColor : "orange";
            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.Consumable:
                    if (item.m_shared.m_food > 0.0)
                    {
                        text.AppendFormat("\n$item_food_health: <color=orange>{0}</color>", item.m_shared.m_food);
                        text.AppendFormat("\n$item_food_stamina: <color=orange>{0}</color>", item.m_shared.m_foodStamina);
                        text.AppendFormat("\n$item_food_duration: <color=orange>{0}s</color>", item.m_shared.m_foodBurnTime);
                        text.AppendFormat("\n$item_food_regen: <color=orange>{0} hp/tick</color>", item.m_shared.m_foodRegen);
                    }

                    var consumeStatusEffectTooltip = item.GetStatusEffectTooltip();
                    if (consumeStatusEffectTooltip.Length > 0)
                    {
                        text.Append("\n\n");
                        text.Append(consumeStatusEffectTooltip);
                    }

                    break;

                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.Torch:
                    text.Append(GetDamageTooltipString(magicItem, item.GetDamage(qualityLevel), item.m_shared.m_skillType, magicColor));
                    var baseBlockPower1 = item.GetBaseBlockPower(qualityLevel);
                    var blockPowerTooltipValue = item.GetBlockPowerTooltip(qualityLevel);
                    var blockPowerPercentageString = blockPowerTooltipValue.ToString("0");
                    text.Append($"\n$item_blockpower: <color={magicBlockColor1}>{baseBlockPower1}</color> <color={magicBlockColor2}>({blockPowerPercentageString})</color>");
                    if (item.m_shared.m_timedBlockBonus > 1.0)
                    {
                        text.Append($"\n$item_deflection: <color={magicParryColor}>{item.GetDeflectionForce(qualityLevel)}</color>");

                        var timedBlockBonus = item.m_shared.m_timedBlockBonus;
                        if (magicParry)
                        {
                            timedBlockBonus *= 1.0f + totalParryBonusMod;
                        }

                        text.Append($"\n$item_parrybonus: <color={magicParryColor}>{timedBlockBonus:0.#}x</color>");
                    }

                    text.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", item.m_shared.m_attackForce);

                    var magicBackstab = magicItem.HasEffect(MagicEffectType.ModifyBackstab);
                    var totalBackstabBonusMod = magicItem.GetTotalEffectValue(MagicEffectType.ModifyBackstab, 0.01f);
                    var magicBackstabColor = magicBackstab ? magicColor : "orange";
                    var backstabValue = item.m_shared.m_backstabBonus * (1.0f + totalBackstabBonusMod);
                    text.Append($"\n$item_backstab: <color={magicBackstabColor}>{backstabValue:0.#}x</color>");

                    var projectileTooltip = item.GetProjectileTooltip(qualityLevel);
                    if (projectileTooltip.Length > 0)
                    {
                        text.Append("\n\n");
                        text.Append(projectileTooltip);
                    }

                    var statusEffectTooltip2 = item.GetStatusEffectTooltip();
                    if (statusEffectTooltip2.Length > 0)
                    {
                        text.Append("\n\n");
                        text.Append(statusEffectTooltip2);
                    }

                    break;

                case ItemDrop.ItemData.ItemType.Shield:
                    var baseBlockPower2 = item.GetBaseBlockPower(qualityLevel);
                    blockPowerTooltipValue = item.GetBlockPowerTooltip(qualityLevel);
                    var str5 = blockPowerTooltipValue.ToString("0");
                    text.Append($"\n$item_blockpower: <color={magicBlockColor1}>{baseBlockPower2}</color> <color={magicBlockColor2}>({str5})</color>");
                    if (item.m_shared.m_timedBlockBonus > 1.0)
                    {
                        text.Append($"\n$item_deflection: <color={magicParryColor}>{item.GetDeflectionForce(qualityLevel)}</color>");

                        var timedBlockBonus = item.m_shared.m_timedBlockBonus;
                        if (magicParry)
                        {
                            timedBlockBonus *= 1.0f + totalParryBonusMod;
                        }

                        text.Append($"\n$item_parrybonus: <color={magicParryColor}>{timedBlockBonus:0.#}x</color>");
                    }

                    break;

                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder:
                    var magicArmorColor = magicItem.HasEffect(MagicEffectType.ModifyArmor) ? magicColor : "orange";
                    text.Append($"\n$item_armor: <color={magicArmorColor}>{item.GetArmor(qualityLevel):0.#}</color>");
                    var modifiersTooltipString = SE_Stats.GetDamageModifiersTooltipString(item.m_shared.m_damageModifiers);
                    if (modifiersTooltipString.Length > 0)
                    {
                        text.Append(modifiersTooltipString);
                    }

                    var statusEffectTooltip3 = item.GetStatusEffectTooltip();
                    if (statusEffectTooltip3.Length > 0)
                    {
                        text.Append("\n");
                        text.Append(statusEffectTooltip3);
                    }

                    break;

                case ItemDrop.ItemData.ItemType.Ammo:
                    text.Append(item.GetDamage(qualityLevel).GetTooltipString(item.m_shared.m_skillType));
                    text.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", item.m_shared.m_attackForce);
                    break;
            }

            var magicMovement = magicItem.HasEffect(MagicEffectType.ModifyMovementSpeed);
            if ((magicMovement || item.m_shared.m_movementModifier != 0) && localPlayer != null)
            {
                var removePenalty = magicItem.HasEffect(MagicEffectType.RemoveSpeedPenalty);

                var itemMovementModifier = removePenalty ? 0 : item.m_shared.m_movementModifier * 100f;
                if (magicMovement)
                {
                    itemMovementModifier += magicItem.GetTotalEffectValue(MagicEffectType.ModifyMovementSpeed);
                }

                var itemMovementModDisplay = (itemMovementModifier == 0) ? "0%" : $"{itemMovementModifier:+0;-0}%";

                var movementModifier = localPlayer.GetEquipmentMovementModifier();
                var totalMovementModifier = movementModifier * 100f;
                var color = (removePenalty || magicMovement) ? magicColor : "orange";
                text.Append($"\n$item_movement_modifier: <color={color}>{itemMovementModDisplay}</color> ($item_total:<color=yellow>{totalMovementModifier:+0;-0}%</color>)");
            }

            // Add magic item effects here
            text.Append(magicItem.GetTooltip());

            // Set stuff
            if (item.IsSetItem())
            {
                AddSetTooltip(item, text);
            }

            __result = text.ToString();
            return false;
        }

        public static void Postfix(ref string __result, ItemDrop.ItemData item)
        {
            if (item != null && (item.IsMagicCraftingMaterial() || item.IsRunestone()))
            {
                var rarityDisplay = EpicLoot.GetRarityDisplayName(item.GetCraftingMaterialRarity());
                __result = $"<color={item.GetCraftingMaterialRarityColor()}>{rarityDisplay} crafting material\n</color>" + __result;
            }

            if (item != null && !item.IsMagic())
            {
                var text = new StringBuilder();

                // Set stuff
                if (item.IsSetItem())
                {
                    // Remove old set stuff
                    var index = __result.IndexOf("\n\n$item_seteffect", StringComparison.InvariantCulture);
                    if (index >= 0)
                    {
                         __result = __result.Remove(index);
                    }

                    // Create new
                    AddSetTooltip(item, text);
                }

                __result += text.ToString();
            }
            
            __result = __result.Replace("<color=orange>", "<color=lightblue>");
            __result = __result.Replace("<color=yellow>", "<color=lightblue>");
            __result = __result.Replace("\n\n\n", "\n\n");
        }

        private static void AddSetTooltip(ItemDrop.ItemData item, StringBuilder text)
        {
            var setID = item.GetSetID(out var isMundane);
            var setSize = item.GetSetSize();

            var setPieces = ItemDataExtensions.GetSetPieces(setID);
            var currentSetEquipped = Player.m_localPlayer.GetEquippedSetPieces(setID);

            var setDisplayName = GetSetDisplayName(item, isMundane);
            text.Append($"\n\n<color={EpicLoot.GetSetItemColor()}> Set: {setDisplayName} ({currentSetEquipped.Count}/{setSize}):</color>");

            foreach (var setItemName in setPieces)
            {
                var isEquipped = IsSetItemEquipped(currentSetEquipped, setItemName, isMundane);
                var color = isEquipped ? "white" : "grey";
                var displayName = GetSetItemDisplayName(setItemName, isMundane);
                text.Append($"\n  <color={color}>{displayName}</color>");
            }

            if (isMundane)
            {
                var setEffectColor = currentSetEquipped.Count == setSize ? EpicLoot.GetSetItemColor() : "grey";
                text.Append($"\n<color={setEffectColor}>({setSize}) ‣ {item.GetSetStatusEffectTooltip().Replace("\n", " ")}</color>");
            }
            else
            {
                var setInfo = item.GetLegendarySetInfo();
                foreach (var setBonusInfo in setInfo.SetBonuses.OrderBy(x => x.Count))
                {
                    var hasEquipped = currentSetEquipped.Count >= setBonusInfo.Count;
                    var effectDef = MagicItemEffectDefinitions.Get(setBonusInfo.Effect.Type);
                    if (effectDef == null)
                    {
                        EpicLoot.LogError($"Set Tooltip: Could not find effect ({setBonusInfo.Effect.Type}) for set ({setInfo.ID}) bonus ({setBonusInfo.Count})!");
                        continue;
                    }

                    var display = MagicItem.GetEffectText(effectDef, setBonusInfo.Effect.Values?.MinValue ?? 0);
                    text.Append($"\n<color={(hasEquipped ? EpicLoot.GetSetItemColor() : "grey")}>({setBonusInfo.Count}) ‣ {display}</color>");
                }
            }
        }

        private static string GetSetItemDisplayName(string setItemName, bool isMundane)
        {
            if (isMundane)
            {
                return setItemName;
            }
            else if (UniqueLegendaryHelper.TryGetLegendaryInfo(setItemName, out var legendaryInfo))
            {
                return legendaryInfo.Name;
            }

            return setItemName;
        }

        public static string GetSetDisplayName(ItemDrop.ItemData item, bool isMundane)
        {
            if (isMundane)
            {
                var textInfo = new CultureInfo("en-US", false).TextInfo;
                return textInfo.ToTitleCase(item.m_shared.m_setName);
            }

            var setInfo = item.GetLegendarySetInfo();
            if (setInfo != null)
            {
                return Localization.instance.Localize(setInfo.Name);
            }
            else
            {
                return $"<unknown set:{item.GetSetID()}>";
            }
        }

        public static bool IsSetItemEquipped(List<ItemDrop.ItemData> currentSetEquipped, string setItemName, bool isMundane)
        {
            if (isMundane)
            {
                return currentSetEquipped.Find(x => x.m_shared.m_name == setItemName) != null;
            }
            else
            {
                return currentSetEquipped.Find(x => x.IsMagic(out var magicItem) && magicItem.LegendaryID == setItemName) != null;
            }
        }

        public static string GetDamageTooltipString(MagicItem item, HitData.DamageTypes instance, Skills.SkillType skillType, string magicColor)
        {
            if (Player.m_localPlayer == null)
            {
                return "";
            }

            var allMagic = item.HasEffect(MagicEffectType.ModifyDamage);
            var physMagic = item.HasEffect(MagicEffectType.ModifyPhysicalDamage);
            var elemMagic = item.HasEffect(MagicEffectType.ModifyElementalDamage);
            var bluntMagic = item.HasEffect(MagicEffectType.AddBluntDamage);
            var slashMagic = item.HasEffect(MagicEffectType.AddSlashingDamage);
            var pierceMagic = item.HasEffect(MagicEffectType.AddPiercingDamage);
            var fireMagic = item.HasEffect(MagicEffectType.AddFireDamage);
            var frostMagic = item.HasEffect(MagicEffectType.AddFrostDamage);
            var lightningMagic = item.HasEffect(MagicEffectType.AddLightningDamage);
            var poisonMagic = item.HasEffect(MagicEffectType.AddPoisonDamage);
            var spiritMagic = item.HasEffect(MagicEffectType.AddSpiritDamage);
            Player.m_localPlayer.GetSkills().GetRandomSkillRange(out var min, out var max, skillType);
            var str = "";
            if (instance.m_damage != 0.0)
            {
                str = str + "\n$inventory_damage: " + DamageRange(instance.m_damage, min, max, allMagic, magicColor);
            }
            if (instance.m_blunt != 0.0)
            {
                var magic = allMagic || physMagic || bluntMagic;
                str = str + "\n$inventory_blunt: " + DamageRange(instance.m_blunt, min, max, magic, magicColor);
            }
            if (instance.m_slash != 0.0)
            {
                var magic = allMagic || physMagic || slashMagic;
                str = str + "\n$inventory_slash: " + DamageRange(instance.m_slash, min, max, magic, magicColor);
            }
            if (instance.m_pierce != 0.0)
            {
                var magic = allMagic || physMagic || pierceMagic;
                str = str + "\n$inventory_pierce: " + DamageRange(instance.m_pierce, min, max, magic, magicColor);
            }
            if (instance.m_fire != 0.0)
            {
                var magic = allMagic || elemMagic || fireMagic;
                str = str + "\n$inventory_fire: " + DamageRange(instance.m_fire, min, max, magic, magicColor);
            }
            if (instance.m_frost != 0.0)
            {
                var magic = allMagic || elemMagic || frostMagic;
                str = str + "\n$inventory_frost: " + DamageRange(instance.m_frost, min, max, magic, magicColor);
            }
            if (instance.m_lightning != 0.0)
            {
                var magic = allMagic || elemMagic || lightningMagic;
                str = str + "\n$inventory_lightning: " + DamageRange(instance.m_lightning, min, max, magic, magicColor);
            }
            if (instance.m_poison != 0.0)
            {
                var magic = allMagic || elemMagic || poisonMagic;
                str = str + "\n$inventory_poison: " + DamageRange(instance.m_poison, min, max, magic, magicColor);
            }
            if (instance.m_spirit != 0.0)
            {
                var magic = allMagic || elemMagic || spiritMagic;
                str = str + "\n$inventory_spirit: " + DamageRange(instance.m_spirit, min, max, magic, magicColor);
            }
            return str;
        }

        public static string DamageRange(float damage, float minFactor, float maxFactor, bool magic = false, string magicColor = "")
        {
            var num1 = Mathf.RoundToInt(damage * minFactor);
            var num2 = Mathf.RoundToInt(damage * maxFactor);
            var color1 = magic ? magicColor : "orange";
            var color2 = magic ? magicColor : "yellow";
            return $"<color={color1}>{(object) Mathf.RoundToInt(damage)}</color> <color={color2}>({num1}-{num2}) </color>";
        }
    }
}
