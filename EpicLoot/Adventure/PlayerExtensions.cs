using HarmonyLib;

namespace EpicLoot.Adventure
{
    public static class PlayerExtensions
    {
        private static AdventureComponent GetAdventureComponent(Player player)
        {
            var c = player.GetComponent<AdventureComponent>();
            if (c == null)
            {
                c = player.gameObject.AddComponent<AdventureComponent>();
            }

            return c;
        }

        public static AdventureSaveData GetAdventureSaveData(this Player player)
        {
            return GetAdventureComponent(player).SaveData;
        }

        public static void SaveAdventureSaveData(this Player player)
        {
            GetAdventureComponent(player).Save();
        }
    }
}
