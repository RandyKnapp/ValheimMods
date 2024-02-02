using HarmonyLib;

namespace EquipmentAndQuickSlots
{

    [HarmonyPatch(typeof(Skills), "OnDeath")]
    public class DontLoseSkillsOnDeath_Patch
    {
        public static bool Prefix()
        {
            if (EquipmentAndQuickSlots.DontLoseSkillsOnDeat.Value == false)
            {
                return false;
            }
            return true;
        }
    }

}