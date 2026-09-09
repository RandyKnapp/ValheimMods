namespace EpicLoot
{
    /// <summary>
    /// Which currently-worn items a call to <see cref="Humanoid.EquipItem"/> is about to unequip.
    ///
    /// Vanilla does its displacement *inside* EquipItem, after every prefix has run, so a prefix that
    /// needs to reason about the resulting loadout has to predict it. This mirrors the item-type chain
    /// in Humanoid.EquipItem (assembly_valheim, Humanoid.cs 763-897) branch for branch -- keep it in
    /// sync if that chain changes.
    ///
    /// Deliberately conservative in two ways, because callers read "not displaced" as "still worn
    /// afterwards" and a wrong yes is the dangerous answer:
    ///   * only the exact ItemData references vanilla holds in its own slot fields are ever reported,
    ///     so an item living in a slot some other mod owns is never assumed displaced;
    ///   * an item type this does not recognise displaces nothing. That covers genuinely unequippable
    ///     types and, importantly, a type another mod has temporarily borrowed to reroute the equip --
    ///     EquipmentAndQuickSlots' MultiUtility stamps a sentinel type on m_shared for exactly that.
    ///
    /// m_hiddenLeftItem/m_hiddenRightItem are deliberately ignored: HideHandItems unequips those items
    /// first, so they are already absent from Inventory.GetEquippedItems() and the nulling EquipItem
    /// does to those two fields changes no worn state.
    /// </summary>
    public static class EquipDisplacement
    {
        /// <summary>
        /// The worn items one equip displaces -- at most two, the two hands. A struct with reference
        /// comparisons rather than a collection: this is built inside a Humanoid.EquipItem prefix.
        /// </summary>
        public readonly struct Displaced
        {
            public readonly ItemDrop.ItemData First;
            public readonly ItemDrop.ItemData Second;

            public Displaced(ItemDrop.ItemData first, ItemDrop.ItemData second = null)
            {
                First = first;
                Second = second;
            }

            public bool Contains(ItemDrop.ItemData item)
            {
                return item != null && (ReferenceEquals(item, First) || ReferenceEquals(item, Second));
            }
        }

        public static Displaced Predict(Humanoid humanoid, ItemDrop.ItemData incoming)
        {
            // Vanilla's own first early-out: re-equipping what is already worn returns false and
            // touches nothing, so there is no displacement to speak of.
            if (humanoid == null || incoming == null || incoming.m_shared == null ||
                humanoid.IsItemEquiped(incoming))
            {
                return default;
            }

            ItemDrop.ItemData right = humanoid.m_rightItem;
            ItemDrop.ItemData left = humanoid.m_leftItem;

            switch (incoming.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.Tool:
                    return new Displaced(right, left);

                case ItemDrop.ItemData.ItemType.Torch:
                    // A torch drawn alongside a one-handed weapon fills the empty left hand instead of
                    // replacing anything.
                    if (right != null && left == null && IsType(right, ItemDrop.ItemData.ItemType.OneHandedWeapon))
                    {
                        return default;
                    }
                    return new Displaced(right, IsType(left, ItemDrop.ItemData.ItemType.Shield) ? null : left);

                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                    // A torch already in the right hand is *moved* to the empty left hand, not dropped.
                    if (right != null && left == null && IsType(right, ItemDrop.ItemData.ItemType.Torch))
                    {
                        return default;
                    }
                    return new Displaced(right,
                        IsType(left, ItemDrop.ItemData.ItemType.Shield) || IsType(left, ItemDrop.ItemData.ItemType.Torch)
                            ? null
                            : left);

                case ItemDrop.ItemData.ItemType.Shield:
                    return new Displaced(left,
                        IsType(right, ItemDrop.ItemData.ItemType.OneHandedWeapon) || IsType(right, ItemDrop.ItemData.ItemType.Torch)
                            ? null
                            : right);

                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                    return new Displaced(left, right);

                case ItemDrop.ItemData.ItemType.Chest:
                    return new Displaced(humanoid.m_chestItem);
                case ItemDrop.ItemData.ItemType.Legs:
                    return new Displaced(humanoid.m_legItem);
                case ItemDrop.ItemData.ItemType.Helmet:
                    return new Displaced(humanoid.m_helmetItem);
                case ItemDrop.ItemData.ItemType.Shoulder:
                    return new Displaced(humanoid.m_shoulderItem);
                case ItemDrop.ItemData.ItemType.Utility:
                    return new Displaced(humanoid.m_utilityItem);
                case ItemDrop.ItemData.ItemType.Trinket:
                    return new Displaced(humanoid.m_trinketItem);

                // Both ammo types share the one ammo slot.
                case ItemDrop.ItemData.ItemType.Ammo:
                case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
                    return new Displaced(humanoid.m_ammoItem);

                default:
                    return default;
            }
        }

        private static bool IsType(ItemDrop.ItemData item, ItemDrop.ItemData.ItemType type)
        {
            return item != null && item.m_shared != null && item.m_shared.m_itemType == type;
        }
    }
}
