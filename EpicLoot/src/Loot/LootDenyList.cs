using System;
using System.Collections.Generic;

namespace EpicLoot
{
    public static class LootDenyList
    {
        private static readonly HashSet<string> DeniedPrefabs = new HashSet<string>(StringComparer.Ordinal)
        {
            // Cheat Items
            "SledgeCheat",
            "SwordCheat",

            // Unused Items
            "ShieldKnight",

            // Enemy Items
            "DvergerArbalest_shootDeepNorth",
            "DvergerArbalest",

            // FW_ -- Fallen Warrior (25)
            "FW_ArmorBronzeChest",
            "FW_ArmorBronzeLegs",
            "FW_ArmorFenringChest",
            "FW_ArmorFenringLegs",
            "FW_ArmorMageChest",
            "FW_ArmorMageChest_Ashlands",
            "FW_ArmorMageLegs",
            "FW_ArmorMageLegs_Ashlands",
            "FW_ArmorPaddedCuirass",
            "FW_ArmorPaddedGreaves",
            "FW_ArmorTrollLeatherChest",
            "FW_ArmorTrollLeatherLegs",
            "FW_AxeBronze",
            "FW_BattleaxeCrystal",
            "FW_BowDraugrFang",
            "FW_CapeLinen",
            "FW_CapeTrollHide",
            "FW_CapeWolf",
            "FW_HelmetBronze",
            "FW_KnifeSilver",
            "FW_KnifeSkollAndHati",
            "FW_ShieldBlackmetalTower",
            "FW_StaffFireball",
            "FW_StaffLightning",
            "FW_SwordBlackmetal",

            // SP_ -- self-killing primary attack on its weapons (28)
            "SP_ArmorBronzeChest",
            "SP_ArmorBronzeLegs",
            "SP_ArmorDress1",
            "SP_ArmorFenringChest",
            "SP_ArmorFenringLegs",
            "SP_ArmorLeatherLegs",
            "SP_ArmorMageChest",
            "SP_ArmorMageChest_Ashlands",
            "SP_ArmorMageLegs",
            "SP_ArmorMageLegs_Ashlands",
            "SP_ArmorPaddedCuirass",
            "SP_ArmorPaddedGreaves",
            "SP_ArmorTrollLeatherChest",
            "SP_ArmorTrollLeatherLegs",
            "SP_ArmorTunic5",
            "SP_AxeBronze",
            "SP_BattleaxeCrystal",
            "SP_BowDraugrFang",
            "SP_CapeLinen",
            "SP_CapeTrollHide",
            "SP_CapeWolf",
            "SP_HelmetBronze",
            "SP_KnifeSilver",
            "SP_KnifeSkollAndHati",
            "SP_ShieldBlackmetalTower",
            "SP_StaffFireball",
            "SP_StaffLightning",
            "SP_SwordBlackmetal",
        };

        /// <summary>True when <paramref name="prefabName"/> is a prop that must never drop as loot.</summary>
        public static bool IsDenied(string prefabName)
        {
            return !string.IsNullOrEmpty(prefabName) && DeniedPrefabs.Contains(prefabName);
        }
    }
}
