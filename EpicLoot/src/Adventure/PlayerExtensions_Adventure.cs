using HarmonyLib;

namespace EpicLoot.Adventure
{
    public static class PlayerExtensions_Adventure
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
            var worldId = ZNet.m_world?.m_uid ?? 0;
            var adventureComponent = GetAdventureComponent(player);
            var saveData = adventureComponent.SaveData.AllSaveData.Find(x => (int)worldId == x.WorldID);
            if (saveData == null)
            {
                saveData = new AdventureSaveData() { WorldID = (int)worldId };
                adventureComponent.SaveData.AllSaveData.Add(saveData);
            }

            return saveData;
        }

        public static void SaveAdventureSaveData(this Player player)
        {
            GetAdventureComponent(player).Save();
        }

        /// <summary>
        /// Writes the local player's adventure state into Player.m_customData right now, instead of
        /// waiting for the next Player.Save (up to 20 minutes). Server-authoritative character mods
        /// (ValheimEnforcer) snapshot m_customData on their own schedule and replay it on the next
        /// login, which would roll completed bounties back.
        /// </summary>
        public static void PersistLocalPlayerAdventureData()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            player.SaveAdventureSaveData();
        }

        /// <summary>
        /// Adds the adventure data to the player custom data before a player save.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.Save))]
        public static class Player_Save_Patch
        {
            public static void Prefix(Player __instance)
            {
                __instance.SaveAdventureSaveData();
            }
        }
    }
}
