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
        /// waiting for the next Player.Save.
        ///
        /// Every adventure state change -- a bounty target slain, a reward claimed, a map bought --
        /// used to live only in the in-memory AdventureComponent until vanilla got around to saving
        /// the profile, which is up to Game.m_saveInterval (20 minutes) later. For that whole window
        /// m_customData described a past that no longer existed, and anything else reading it saw
        /// that past: server-authoritative character mods (ValheimEnforcer) snapshot the dictionary
        /// on their own schedule and replay their snapshot back onto the player on the next login,
        /// which silently rolled completed bounties back to InProgress -- permanently, since their
        /// targets are already dead. The kill ledger makes a loss worse still: the server deletes a
        /// player's queued kill logs the moment it sends them, so a replayed offline kill that is
        /// not written down is gone for good.
        ///
        /// Serializing the blob is a few kilobytes and these events are rare, so the cheap fix is to
        /// keep m_customData continuously true rather than to guess who else is reading it. The
        /// Player.Save prefix below stays as the backstop.
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
