using System;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EpicLoot.Adventure
{
    public class TreasureMapChest : MonoBehaviour
    {
        public Heightmap.Biome Biome;
        public int Interval;

        public string LootTableName => $"TreasureMapChest_{Biome}";

        public void Setup(Player player, Heightmap.Biome biome, int treasureMapInterval)
        {
            Reinitialize(biome, treasureMapInterval, false, player.GetPlayerID());

            var container = GetComponent<Container>();
            if (container == null || container.m_nview == null)
            {
                EpicLoot.LogError($"Trying to set up TreasureMapChest ({biome} {treasureMapInterval}) but there was no Container component!");
            }

            var zdo = container.m_nview.GetZDO();
            if (zdo != null && zdo.IsValid())
            {
                container.GetInventory().RemoveAll();

                zdo.Set("TreasureMapChest.Interval", Interval);
                zdo.Set("TreasureMapChest.Biome", Biome.ToString());
                zdo.Set("creator", player.GetPlayerID());

                var items = LootRoller.RollLootTable(LootTableName, 1, LootTableName, transform.position);
                items.ForEach(item => container.m_inventory.AddItem(item));

                var biomeConfig = AdventureDataManager.Config.TreasureMap.BiomeInfo.Find(x => x.Biome == biome);
                if (biomeConfig?.ForestTokens > 0)
                    container.m_inventory.AddItem("ForestToken", biomeConfig.ForestTokens, 1, 0, 0, string.Empty);

                if (biomeConfig?.IronTokens > 0)
                    container.m_inventory.AddItem("IronBountyToken", biomeConfig.IronTokens, 1, 0, 0, string.Empty);

                if (biomeConfig?.GoldTokens > 0)
                    container.m_inventory.AddItem("GoldBountyToken", biomeConfig.GoldTokens, 1, 0, 0, string.Empty);

                if (biomeConfig?.Coins > 0)
                    container.m_inventory.AddItem("Coins", biomeConfig.Coins, 1, 0, 0, string.Empty);

                container.Save();
            }
            else
            {
                EpicLoot.LogError($"Trying to set up TreasureMapChest ({biome} {treasureMapInterval}) but ZDO was not valid!");
            }
        }

        public void Reinitialize(Heightmap.Biome biome, int treasureMapInterval, bool hasBeenFound, long ownerPlayerId)
        {
            Biome = biome;
            Interval = treasureMapInterval;
            gameObject.layer = 12;

            var container = GetComponent<Container>();
            if (container != null)
            {
                if (hasBeenFound && container.m_inventory.NrOfItems() == 0)
                {
                    // Remove treasure chests that have already been found
                    var zdo = container.m_nview.GetZDO();
                    if (zdo != null && zdo.IsValid())
                    {
                        zdo.SetOwner(ZDOMan.GetSessionID());
                        container.m_nview.Destroy();
                        return;
                    }
                }

                var label = Localization.instance.Localize("$mod_epicloot_treasurechest_name", $"$biome_{Biome.ToString().ToLower()}", (treasureMapInterval + 1).ToString());
                container.m_name = Localization.instance.Localize(label);
                container.m_privacy = hasBeenFound ? Container.PrivacySetting.Public : Container.PrivacySetting.Private;
            }

            var piece = GetComponent<Piece>();
            if (piece != null)
            {
                piece.m_creator = ownerPlayerId;
            }

            if (!hasBeenFound)
            {
                var beacon = gameObject.AddComponent<Beacon>();
                beacon.m_range = EpicLoot.GetAndvaranautRange();
            }

            // TODO Figure out why this damn thing won't float
            // Idea: It's probably because of the snap to ground chests have? Or gravity physics?
            /*var rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.constraints =
                RigidbodyConstraints.FreezePositionX
                | RigidbodyConstraints.FreezePositionZ
                | RigidbodyConstraints.FreezeRotation;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

            var floating = gameObject.AddComponent<Floating>();
            floating.m_waterLevelOffset = 0.3f;
            floating.TerrainCheck();*/

            Destroy(gameObject.GetComponent<WearNTear>());
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
    public static class Container_Awake_Patch
    {
        public static void Postfix(Container __instance)
        {
            var zdo = __instance.m_nview.GetZDO();
            if (zdo != null)
            {
                var biomeString = zdo.GetString($"{nameof(TreasureMapChest)}.{nameof(TreasureMapChest.Biome)}");
                if (!string.IsNullOrEmpty(biomeString))
                {
                    if (Enum.TryParse(biomeString, out Heightmap.Biome biome))
                    {
                        var interval = zdo.GetInt("TreasureMapChest.Interval");
                        var hasBeenFound = zdo.GetBool("TreasureMapChest.HasBeenFound");
                        var owner = zdo.GetLong("creator");
                        var treasureMapChest = __instance.gameObject.AddComponent<TreasureMapChest>();
                        treasureMapChest.Reinitialize(biome, interval, hasBeenFound, owner);
                    }
                    else
                    {
                        EpicLoot.LogError($"[EpicLoot.Adventure.Container_Awake] Unknown biome: {biomeString}");
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.RPC_OpenRespons))]
    public static class Container_RPC_OpenRespons_Patch
    {
        public static void Postfix(Container __instance, long uid, bool granted)
        {
            var zdo = __instance.m_nview.GetZDO();
            if (zdo == null || !zdo.IsValid() || zdo.GetBool("TreasureMapChest.HasBeenFound"))
            {
                return;
            }

            var player = Player.m_localPlayer;
            if (granted && player != null)
            {
                var treasureMapChest = __instance.GetComponent<TreasureMapChest>();
                if (treasureMapChest != null)
                {
                    EpicLoot.Log($"Player is opening treasure map chest ({treasureMapChest.Biome}, {treasureMapChest.Interval})!");
                    var saveData = player.GetAdventureSaveData();
                    saveData.FoundTreasureChest(treasureMapChest.Interval, treasureMapChest.Biome);

                    zdo.Set("TreasureMapChest.HasBeenFound", true);

                    __instance.m_privacy = Container.PrivacySetting.Public;

                    MessageHud.instance.ShowBiomeFoundMsg("Treasure Found!", true);

                    Object.Destroy(treasureMapChest.GetComponent<Beacon>());
                    Object.Destroy(treasureMapChest.GetComponent<Rigidbody>());
                }
            }
        }
    }
}
