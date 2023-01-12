using System;
using System.Text;
using EpicLoot.Crafting;
using EpicLoot.Data;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot
{
    // Set the topic of the tooltip with the decorated name
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip), typeof(ItemDrop.ItemData), typeof(UITooltip))]
    public static class InventoryGrid_CreateItemTooltip_MagicItemComponent_Patch
    {
        public static bool Prefix(ItemDrop.ItemData item, UITooltip tooltip)
        {
            string tooltipText;
            if (item.IsEquipable() && !item.m_equiped && Player.m_localPlayer != null && Player.m_localPlayer.HasEquipmentOfType(item.m_shared.m_itemType) && Input.GetKey(KeyCode.LeftControl))
            {
                var otherItem = Player.m_localPlayer.GetEquipmentOfType(item.m_shared.m_itemType);
                tooltipText = item.GetTooltip() + $"\n\n<color=#AAA><i>$mod_epicloot_currentlyequipped:</i></color>\n<size=18>{otherItem.GetDecoratedName()}</size>\n" + otherItem.GetTooltip();
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
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool))]
    public static class MagicItemTooltip_ItemDrop_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(ref string __result, ItemDrop.ItemData item, int qualityLevel, bool crafting)
        {
            if (item == null)
                return true;

            var localPlayer = Player.m_localPlayer;
            var text = new StringBuilder(256);

            var magicItem = item.GetMagicItem();

            if (magicItem == null)
                return true;

            var magicColor = magicItem.GetColorString();
            var itemTypeName = magicItem.GetItemTypeName(item.Extended());

            var skillLevel = Player.m_localPlayer.GetSkillLevel(item.m_shared.m_skillType);

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
                text.AppendFormat("\n$item_crafter: <color=orange>{0}</color>", item.GetCrafterName());
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
                text.Append($"\n$item_durability: <color={magicColor}>$mod_epicloot_me_indestructible_display</color>");
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

                    var consumeStatusEffectTooltip = item.GetStatusEffectTooltip(qualityLevel, skillLevel);
                    if (consumeStatusEffectTooltip.Length > 0)
                    {
                        text.Append("\n\n");
                        text.Append(consumeStatusEffectTooltip);
                    }

                    break;

                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Torch:
                    text.Append(GetDamageTooltipString(magicItem, item.GetDamage(qualityLevel), item.m_shared.m_skillType, magicColor));

                    var magicAttackStamina = magicItem.HasEffect(MagicEffectType.ModifyAttackStaminaUse) || magicItem.HasEffect(MagicEffectType.ModifyBlockStaminaUse);
                    var magicAttackStaminaColor = magicAttackStamina ? magicColor : "orange";
                    var staminaUsePercentage = 1 - magicItem.GetTotalEffectValue(MagicEffectType.ModifyAttackStaminaUse, 0.01f);
                    var totalStaminaUse = staminaUsePercentage * item.m_shared.m_attack.m_attackStamina;
                    if (item.m_shared.m_attack.m_attackStamina > 0.0)
                        text.Append($"\n$item_staminause: <color={magicAttackStaminaColor}>{totalStaminaUse:#.#}</color>");
                    if (item.m_shared.m_attack.m_attackEitr > 0.0)
                        text.Append($"\n$item_eitruse: <color=orange>{item.m_shared.m_attack.m_attackEitr}</color>");
                    if (item.m_shared.m_attack.m_attackHealth > 0.0)
                        text.Append($"\n$item_healthuse: <color=orange>{item.m_shared.m_attack.m_attackHealth}</color>");
                    if (item.m_shared.m_attack.m_attackHealthPercentage > 0.0)
                        text.Append($"\n$item_healthuse: <color=orange>{item.m_shared.m_attack.m_attackHealthPercentage:0.0}%</color>");
                    if (item.m_shared.m_attack.m_drawStaminaDrain > 0.0)
                        text.Append($"\n$item_staminahold: <color=orange>{item.m_shared.m_attack.m_drawStaminaDrain}</color>/s");

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

                    var statusEffectTooltip2 = item.GetStatusEffectTooltip(qualityLevel, skillLevel);
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

                    var statusEffectTooltip3 = item.GetStatusEffectTooltip(qualityLevel, skillLevel);
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

            var magicEitrRegen = magicItem.HasEffect(MagicEffectType.ModifyEitrRegen);
            if ((magicEitrRegen || item.m_shared.m_eitrRegenModifier != 0) && localPlayer != null)
            {
                var itemEitrRegenModDisplay = GetEitrRegenModifier(item, magicItem, out _);

                var equipEitrRegenModifier = localPlayer.GetEquipmentEitrRegenModifier() * 100.0f;
                var equipMagicEitrRegenModifier = localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyEitrRegen);
                var totalEitrRegenModifier = equipEitrRegenModifier + equipMagicEitrRegenModifier;
                var color = (magicEitrRegen) ? magicColor : "orange";
                var totalColor = equipMagicEitrRegenModifier > 0 ? magicColor : "yellow";
                text.Append($"\n$item_eitrregen_modifier: <color={color}>{itemEitrRegenModDisplay}</color> ($item_total: <color={totalColor}>{totalEitrRegenModifier:+0;-0}%</color>)");
            }

            var magicMovement = magicItem.HasEffect(MagicEffectType.ModifyMovementSpeed);
            if ((magicMovement || item.m_shared.m_movementModifier != 0) && localPlayer != null)
            {
                var itemMovementModDisplay = GetMovementModifier(item, magicItem, out _, out var removePenalty);

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
                text.Append(item.GetSetTooltip());
            }

            __result = text.ToString();

            return false;
        }

        [UsedImplicitly]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref string __result, ItemDrop.ItemData item)
        {
            if (item == null)
                return;

            __result = EIDFLegacy.FormatCrafterName(__result);

            if (item.IsMagicCraftingMaterial() || item.IsRunestone())
            {
                var rarityDisplay = EpicLoot.GetRarityDisplayName(item.GetCraftingMaterialRarity());
                __result = $"<color={item.GetCraftingMaterialRarityColor()}>{rarityDisplay} $mod_epicloot_craftingmaterial\n</color>" + __result;
            }

            if (!item.IsMagic())
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
                    text.Append(item.GetSetTooltip());
                }

                __result += text.ToString();
            }
            
            __result = __result.Replace("<color=orange>", "<color=lightblue>");
            __result = __result.Replace("<color=yellow>", "<color=lightblue>");
            __result = __result.Replace("\n\n\n", "\n\n");
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
            return $"<color={color1}>{Mathf.RoundToInt(damage)}</color> <color={color2}>({num1}-{num2}) </color>";
        }

        public static string GetEitrRegenModifier(ItemDrop.ItemData item, MagicItem magicItem, out bool magicEitrRegen)
        {
            magicEitrRegen = magicItem?.HasEffect(MagicEffectType.ModifyEitrRegen) ?? false;
            var itemEitrRegenModifier = item.m_shared.m_eitrRegenModifier * 100f;
            if (magicEitrRegen && magicItem != null)
                itemEitrRegenModifier += magicItem.GetTotalEffectValue(MagicEffectType.ModifyEitrRegen);

            return (itemEitrRegenModifier == 0) ? "0%" : $"{itemEitrRegenModifier:+0;-0}%";
        }

        public static string GetMovementModifier(ItemDrop.ItemData item, MagicItem magicItem, out bool magicMovement, out bool removePenalty)
        {
            magicMovement = magicItem.HasEffect(MagicEffectType.ModifyMovementSpeed);
            removePenalty = magicItem.HasEffect(MagicEffectType.RemoveSpeedPenalty);

            var itemMovementModifier = removePenalty ? 0 : item.m_shared.m_movementModifier * 100f;
            if (magicMovement)
            {
                itemMovementModifier += magicItem.GetTotalEffectValue(MagicEffectType.ModifyMovementSpeed);
            }

            return (itemMovementModifier == 0) ? "0%" : $"{itemMovementModifier:+0;-0}%";
        }
    }

    public static class AugaTooltipPreprocessor
    {
        public static Tuple<string, string> PreprocessTooltipStat(ItemDrop.ItemData item, string label, string value)
        {
            var localPlayer = Player.m_localPlayer;

            if (item.IsMagic(out var magicItem))
            {
                var magicColor = magicItem.GetColorString();

                var allMagic = magicItem.HasEffect(MagicEffectType.ModifyDamage);
                var physMagic = magicItem.HasEffect(MagicEffectType.ModifyPhysicalDamage);
                var elemMagic = magicItem.HasEffect(MagicEffectType.ModifyElementalDamage);
                var bluntMagic = magicItem.HasEffect(MagicEffectType.AddBluntDamage);
                var slashMagic = magicItem.HasEffect(MagicEffectType.AddSlashingDamage);
                var pierceMagic = magicItem.HasEffect(MagicEffectType.AddPiercingDamage);
                var fireMagic = magicItem.HasEffect(MagicEffectType.AddFireDamage);
                var frostMagic = magicItem.HasEffect(MagicEffectType.AddFrostDamage);
                var lightningMagic = magicItem.HasEffect(MagicEffectType.AddLightningDamage);
                var poisonMagic = magicItem.HasEffect(MagicEffectType.AddPoisonDamage);
                var spiritMagic = magicItem.HasEffect(MagicEffectType.AddSpiritDamage);
                switch (label)
                {
                    case "$item_durability":
                        if (magicItem.HasEffect(MagicEffectType.Indestructible))
                        {
                            value = $"<color={magicColor}>Indestructible</color>";
                        }
                        else if (magicItem.HasEffect(MagicEffectType.ModifyDurability))
                        {
                            value = $"<color={magicColor}>{value}</color>";
                        }
                        break;

                    case "$item_weight":
                        if (magicItem.HasEffect(MagicEffectType.ReduceWeight) || magicItem.HasEffect(MagicEffectType.Weightless))
                        {
                            value = $"<color={magicColor}>{value}</color>";
                        }
                        break;

                    case "$inventory_damage":
                        if (allMagic)
                        {
                            value = $"<color={magicColor}>{value}</color>";
                        }
                        break;

                    case "$inventory_blunt":
                        if (allMagic || physMagic || bluntMagic)
                        {
                            value = $"<color={magicColor}>{value}</color>";
                        }
                        break;

                    case "$inventory_slash":
                        if (allMagic || physMagic || slashMagic)
                        {
                            value = $"<color={magicColor}>{value}</color>";
                        }
                        break;

                    case "$inventory_pierce":
                        if (allMagic || physMagic || pierceMagic)
                        {
                            value = $"<color={magicColor}>{value}</color>";
                        }
                        break;

                    case "$inventory_fire":
                        if (allMagic || elemMagic || fireMagic)
                        {
                            value = $"<color={magicColor}>{value}</color>";
                        }
                        break;

                    case "$inventory_frost":
                        if (allMagic || elemMagic || frostMagic)
                        {
                            value = $"<color={magicColor}>{value}</color>";
                        }
                        break;

                    case "$inventory_lightning":
                        if (allMagic || elemMagic || lightningMagic)
                        {
                            value = $"<color={magicColor}>{value}</color>";
                        }
                        break;

                    case "$inventory_poison":
                        if (allMagic || elemMagic || poisonMagic)
                        {
                            value = $"<color={magicColor}>{value}</color>";
                        }
                        break;

                    case "$inventory_spirit":
                        if (allMagic || elemMagic || spiritMagic)
                        {
                            value = $"<color={magicColor}>{value}</color>";
                        }
                        break;

                    case "$item_backstab":
                        if (magicItem.HasEffect(MagicEffectType.ModifyBackstab))
                        {
                            var totalBackstabBonusMod = magicItem.GetTotalEffectValue(MagicEffectType.ModifyBackstab, 0.01f);
                            var backstabValue = item.m_shared.m_backstabBonus * (1.0f + totalBackstabBonusMod);
                            value = $"<color={magicColor}>{backstabValue:0.#}x</color>";
                        }
                        break;

                    case "$item_blockpower":
                        if (magicItem.HasEffect(MagicEffectType.ModifyBlockPower))
                        {
                            var baseBlockPower = item.GetBaseBlockPower(item.m_quality);
                            var blockPowerPercentageString = item.GetBlockPowerTooltip(item.m_quality).ToString("0");
                            value = $"<color={magicColor}>{baseBlockPower}</color> <color={magicColor}>({blockPowerPercentageString})</color>";
                        }
                        break;

                    case "$item_deflection":
                        if (magicItem.HasEffect(MagicEffectType.ModifyParry))
                        {
                            value = $"<color={magicColor}>{item.GetDeflectionForce(item.m_quality)}</color>";
                        }
                        break;

                    case "$item_parrybonus":
                        if (magicItem.HasEffect(MagicEffectType.ModifyParry))
                        {
                            var totalParryBonusMod = magicItem.GetTotalEffectValue(MagicEffectType.ModifyParry, 0.01f);
                            var timedBlockBonus = item.m_shared.m_timedBlockBonus * (1.0f + totalParryBonusMod);
                            value = $"<color={magicColor}>{timedBlockBonus:0.#}x</color>";
                        }
                        break;

                    case "$item_armor":
                        if (magicItem.HasEffect(MagicEffectType.ModifyArmor))
                        {
                            value = $"<color={magicColor}>{value}</color>";
                        }
                        break;

                    case "$item_staminause":
                        if (magicItem.HasEffect(MagicEffectType.ModifyAttackStaminaUse) || magicItem.HasEffect(MagicEffectType.ModifyBlockStaminaUse))
                        {
                            value = $"<color={magicColor}>{value}</color>";
                        }
                        break;

                    case "$item_crafter":
                        value = EIDFLegacy.GetCrafterName(value);
                        break;
                }
                
                if (label.StartsWith("$item_movement_modifier") && (magicItem.HasEffect(MagicEffectType.RemoveSpeedPenalty) || magicItem.HasEffect(MagicEffectType.ModifyMovementSpeed)))
                {
                    var colorIndex = label.IndexOf("<color", StringComparison.Ordinal);
                    if (colorIndex >= 0)
                    {
                        var sb = new StringBuilder(label);
                        sb.Remove(colorIndex, "<color=#XXXXXX>".Length);
                        sb.Insert(colorIndex, $"<color={magicColor}>");

                        var itemMovementModDisplay = MagicItemTooltip_ItemDrop_Patch.GetMovementModifier(item, magicItem, out _, out _);
                        var valueIndex = colorIndex + "<color=#XXXXXX>".Length;
                        var percentIndex = label.IndexOf("%", valueIndex, StringComparison.Ordinal);
                        sb.Remove(valueIndex, percentIndex - valueIndex + 1);
                        sb.Insert(valueIndex, itemMovementModDisplay);

                        label = sb.ToString();
                    }
                }
            }

            var magicEitrRegen = magicItem?.HasEffect(MagicEffectType.ModifyEitrRegen) ?? false;
            if (label.StartsWith("$item_eitrregen_modifier") && (magicEitrRegen || item.m_shared.m_eitrRegenModifier != 0) && localPlayer != null)
            {
                var itemEitrRegenModDisplay = MagicItemTooltip_ItemDrop_Patch.GetEitrRegenModifier(item, magicItem, out _);

                var equipEitrRegenModifier = localPlayer.GetEquipmentEitrRegenModifier() * 100.0f;
                var equipMagicEitrRegenModifier = localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyEitrRegen);
                var totalEitrRegenModifier = equipEitrRegenModifier + equipMagicEitrRegenModifier;
                if (magicEitrRegen && magicItem != null)
                    itemEitrRegenModDisplay = $"<color={magicItem.GetColorString()}>{itemEitrRegenModDisplay}</color>";
                label = $"$item_eitrregen_modifier: {itemEitrRegenModDisplay} ($item_total: <color={Auga.API.Brown3}>{totalEitrRegenModifier:+0;-0}%</color>)";
            }

            switch (label)
            {
                case "$item_crafter":
                    value = EIDFLegacy.GetCrafterName(value);
                    break;
            }

            return new Tuple<string, string>(label, value);
        }
    }
}
