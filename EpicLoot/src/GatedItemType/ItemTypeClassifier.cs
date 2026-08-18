namespace EpicLoot.GatedItemType
{
    // Single source of truth for "which EpicLoot ItemInfo type is this item?".
    //
    // The vocabulary is the set of "Type" strings in config/iteminfo.json (Swords, HeadArmor, Trinket,
    // ...). Two callers need it for different reasons and must not answer it differently:
    //   * AutoAddEnchantableItems, which GENERATES iteminfo.json and so may only look at raw item fields;
    //   * readers (shard socketing, magic-effect requirements), which want the configured answer when
    //     one exists and a best-effort guess otherwise.
    //
    // Only ever return a type that already exists in iteminfo.json, or Unknown. The sorter indexes
    // foundByCategory[type] unguarded, so a new type string invented here would throw there.
    public static class ItemTypeClassifier
    {
        // Returned when an item cannot be classified at all. Callers must treat it as "no answer"
        // rather than as a category: the sorter skips such items, and shard socketing leaves the
        // shard inert instead of guessing a slot for it.
        public const string Unknown = "Unknown";

        // The configured classification, i.e. the item's entry in iteminfo.json. False when the item
        // isn't listed there (an unlisted modded item, or one the sorter has never run over).
        public static bool TryGetConfiguredType(ItemDrop.ItemData item, out string type)
        {
            type = null;

            var prefabName = item?.m_dropPrefab?.name;
            if (string.IsNullOrEmpty(prefabName) ||
                !GatedItemTypeHelper.AllItemsWithDetails.TryGetValue(prefabName, out var details) ||
                string.IsNullOrEmpty(details.Type))
            {
                return false;
            }

            type = details.Type;
            return true;
        }

        // Configured classification first so a hand-edited iteminfo.json stays authoritative, falling
        // back to the raw-field heuristic for items it doesn't list. This is what readers want; the
        // sorter must call ClassifyFromFields directly instead (see its call site).
        public static string GetItemInfoType(ItemDrop.ItemData item)
        {
            return TryGetConfiguredType(item, out var configured) ? configured : ClassifyFromFields(item);
        }

        // Best-effort classification from an item's raw fields alone, resolved in three tiers.
        //
        // m_itemType is read FIRST and the skill switch is nested inside the weapon-shaped case on
        // purpose: SharedData.m_skillType sits under the [Header("Weapon")] block and defaults to
        // Skills.SkillType.Swords, so armor, capes, trinkets and utility items all report Swords.
        // Reading the skill first would classify every one of them as a sword.
        public static string ClassifyFromFields(ItemDrop.ItemData item)
        {
            ItemDrop.ItemData.ItemType itemType = item.m_shared.m_itemType;
            switch (itemType)
            {
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Attach_Atgeir:
                    switch (item.m_shared.m_skillType)
                    {
                        case Skills.SkillType.Spears:
                            return "Spears";
                        case Skills.SkillType.Swords:
                            return "Swords";
                        case Skills.SkillType.Clubs:
                            return (itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon) ? "Clubs" : "Sledges";
                        case Skills.SkillType.Axes:
                            return (itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon) ? "Axes" : "TwoHandAxes";
                        case Skills.SkillType.Knives:
                            return "Knives";
                        case Skills.SkillType.Unarmed:
                            return "Fists";
                        case Skills.SkillType.ElementalMagic:
                        case Skills.SkillType.BloodMagic:
                            return "Staffs";
                        case Skills.SkillType.Polearms:
                            return "Polearms";
                        case Skills.SkillType.Pickaxes:
                            return "Pickaxes";
                        case Skills.SkillType.Sneak:
                            return "Torches";
                        // A modded bow/crossbow that declares itself a two-handed weapon rather than
                        // ItemType.Bow still names its skill, so trust it before the animation tier.
                        case Skills.SkillType.Bows:
                        case Skills.SkillType.Crossbows:
                            return "Bows";
                    }
                    break;
                case ItemDrop.ItemData.ItemType.Shield:
                    if (item.m_shared.m_timedBlockBonus > 0)
                    {
                        return (item.m_shared.m_timedBlockBonus >= 2.5f) ? "Bucklers" : "RoundShields";
                    }
                    else
                    {
                        return "TowerShields";
                    }
                case ItemDrop.ItemData.ItemType.Bow:
                    return "Bows";
                case ItemDrop.ItemData.ItemType.Helmet:
                    return "HeadArmor";
                case ItemDrop.ItemData.ItemType.Chest:
                    return "ChestArmor";
                case ItemDrop.ItemData.ItemType.Legs:
                    return "LegsArmor";
                case ItemDrop.ItemData.ItemType.Shoulder:
                    return "ShouldersArmor";
                case ItemDrop.ItemData.ItemType.Torch:
                    return "Torches";
                case ItemDrop.ItemData.ItemType.Tool:
                    return "Tools";
                case ItemDrop.ItemData.ItemType.Trinket:
                    return "Trinket";
                case ItemDrop.ItemData.ItemType.Utility:
                case ItemDrop.ItemData.ItemType.Misc:
                    return "Utility";
            }

            // It is possible that the item is not a known skill type
            // This happens with weapons that use mod skills eg: scythes
            switch (item.m_shared.m_animationState)
            {
                // Its either an axe or a sword, currently this is only therzies throwing axes which get to this point
                case ItemDrop.ItemData.AnimationState.OneHanded:
                    return "Axes";
                case ItemDrop.ItemData.AnimationState.DualAxes:
                    return "TwoHandAxes";
                case ItemDrop.ItemData.AnimationState.Unarmed:
                    if (item.m_shared.m_skillType == Skills.SkillType.None)
                    {
                        // This is likely a throwable bomb, make sure it remains unknown
                        break;
                    }
                    return "Fists";
                case ItemDrop.ItemData.AnimationState.MagicItem:
                    return "Staffs";
                case ItemDrop.ItemData.AnimationState.Scythe:
                case ItemDrop.ItemData.AnimationState.Atgeir:
                    return "Polearms";
                case ItemDrop.ItemData.AnimationState.Bow:
                case ItemDrop.ItemData.AnimationState.Crossbow:
                    return "Bows";
                case ItemDrop.ItemData.AnimationState.Feaster:
                case ItemDrop.ItemData.AnimationState.FishingRod:
                    return "Tools";
                case ItemDrop.ItemData.AnimationState.Torch:
                case ItemDrop.ItemData.AnimationState.LeftTorch:
                    return "Torches";
                case ItemDrop.ItemData.AnimationState.Greatsword:
                    return "Swords";
                case ItemDrop.ItemData.AnimationState.TwoHandedClub:
                    return "Sledges";
            }

            EpicLoot.Log($"Unknown item type for item {item.m_shared.m_name}: {itemType}");
            return Unknown;
        }
    }
}
