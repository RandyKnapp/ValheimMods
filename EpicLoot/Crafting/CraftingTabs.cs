namespace EpicLoot.Crafting
{
    public enum CraftingTabType
    {
        Crafting,
        Upgrade,
        Disenchant,
        Enchant,
        Augment
    }

    public enum CraftingTabStyle
    {
        Horizontal,
        HorizontalSquish,
        Vertical,
        Angled
    }

    public static class CraftingTabs
    {
        public static bool HaveRequirementsHelper(Player player, Piece.Requirement[] requirements, int qualityLevel)
        {
            foreach (var resource in requirements)
            {
                if (resource.m_resItem)
                {
                    var amount = resource.GetAmount(qualityLevel);
                    var num = player.m_inventory.CountItems(resource.m_resItem.m_itemData.m_shared.m_name);
                    if (num < amount)
                        return false;
                }
            }
            return true;
        }
    }
}
