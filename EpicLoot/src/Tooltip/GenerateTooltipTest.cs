using BepInEx;
using System.IO;

namespace EpicLoot;

internal class GenerateTooltipTest
{
    public static void GenerateInventoryTooltips(bool magicTooltipDisable)
    {
        if (Player.m_localPlayer == null)
        {
            return;
        }

        Inventory inventory = Player.m_localPlayer.GetInventory();

        if (inventory == null)
        {
            return;
        }

        string directory = GetTooltipDirectoryPath();

        foreach (var item in inventory.GetAllItems())
        {
            MagicTooltipPatches.TooltipDisable = magicTooltipDisable;
            string tooltip = item.GetTooltip();
            MagicTooltipPatches.TooltipDisable = false;
            string fileName = Path.Combine(directory, $"{item.m_shared.m_name}.txt");

            File.WriteAllText(fileName, tooltip);
        }
    }

    private static string GetTooltipDirectoryPath()
    {
        string folderPath = Path.Combine(Paths.ConfigPath, "EpicLoot", "TooltipTest");

        DirectoryInfo dirInfo = Directory.CreateDirectory(folderPath);

        return dirInfo.FullName;
    }
}
