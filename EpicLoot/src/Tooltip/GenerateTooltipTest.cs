using BepInEx;
using System.IO;
using Newtonsoft.Json;

namespace EpicLoot;

internal class GenerateTooltipTest
{
    // Writes one file per inventory item: the tooltip text, and with the inventory grid's hints, so a refactor
    // of the tooltip can be checked by diffing two runs on the same character. details forces the Shift view.
    public static string GenerateInventoryTooltips(bool magicTooltipDisable, bool details)
    {
        if (Player.m_localPlayer == null)
        {
            return null;
        }

        Inventory inventory = Player.m_localPlayer.GetInventory();

        if (inventory == null)
        {
            return null;
        }

        string directory = GetTooltipDirectoryPath((magicTooltipDisable ? "vanilla" : "magic") + (details ? "_details" : ""));

        MagicItem.ForceEffectDetails = details;
        try
        {
            foreach (var item in inventory.GetAllItems())
            {
                ItemTooltip.Disabled = magicTooltipDisable;
                string tooltip = item.GetTooltip();
                ItemTooltip.Disabled = false;

                string fileName = Path.Combine(directory, GetFileName(item));
                File.WriteAllText(fileName + ".txt", tooltip);

                if (!magicTooltipDisable)
                {
                    File.WriteAllText(fileName + ".inventory.txt", ItemTooltip.GetInventoryText(item, out _));
                    ItemTooltipData data = ItemTooltip.GetData(item, item.m_quality, details);
                    File.WriteAllText(fileName + ".json", JsonConvert.SerializeObject(data, Formatting.Indented,
                        ItemTooltip.JsonSettings));
                }
            }
        }
        finally
        {
            ItemTooltip.Disabled = false;
            MagicItem.ForceEffectDetails = false;
        }

        return directory;
    }

    // By grid position: two items with the same base name would otherwise overwrite each other.
    private static string GetFileName(ItemDrop.ItemData item)
    {
        string name = item.m_shared.m_name;
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        return $"{item.m_gridPos.y}_{item.m_gridPos.x}_{name}";
    }

    private static string GetTooltipDirectoryPath(string mode)
    {
        string folderPath = Path.Combine(Paths.ConfigPath, "EpicLoot", "TooltipTest", mode);

        DirectoryInfo dirInfo = Directory.CreateDirectory(folderPath);

        return dirInfo.FullName;
    }
}
