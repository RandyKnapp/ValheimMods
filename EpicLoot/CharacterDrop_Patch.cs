using HarmonyLib;

namespace EpicLoot
{
    //public void OnDeath()
    [HarmonyPatch(typeof(CharacterDrop), "OnDeath")]
    public static class CharacterDrop_OnDeath_Patch
    {
        public static void Postfix(CharacterDrop __instance)
        {
            EpicLoot.OnCharacterDeath(__instance);
        }
    }
}
