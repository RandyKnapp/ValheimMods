using EpicLoot.Biomes;
using HarmonyLib;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EpicLoot.Adventure
{
    public class TreasureMapChest : MonoBehaviour
    {
        public Heightmap.Biome Biome;
        public int Interval;

        public string LootTableName => $"TreasureMapChest_{BiomeDataManager.GetName(Biome)}";

        public void Setup(long playerID, Heightmap.Biome biome, int treasureMapInterval)
        {
            Reinitialize(biome, treasureMapInterval, false, playerID);

            var container = GetComponent<Container>();
            if (container == null || container.m_nview == null)
            {
                EpicLoot.LogError($"Trying to set up TreasureMapChest ({biome} {treasureMapInterval}) but there was no Container component!");
                return;
            }

            var zdo = container.m_nview.GetZDO();
            if (zdo != null && zdo.IsValid())
            {
                container.GetInventory().RemoveAll();

                zdo.Set("TreasureMapChest.Interval", Interval);
                zdo.Set("TreasureMapChest.Biome", Biome.ToString());
                zdo.Set("creator", playerID);

                var items = LootRoller.RollLootTable(LootTableName, 1, LootTableName, transform.position);
                var biomeConfig = AdventureDataManager.Config.TreasureMap.BiomeInfo.Find(x => x.GetBiome() == biome);

                // The token and coin stacks below still need a slot each once Prosperity's bonus rolls
                // have filled the chest.
                var rewardStacks = (biomeConfig?.ForestTokens > 0 ? 1 : 0) + (biomeConfig?.IronTokens > 0 ? 1 : 0) +
                    (biomeConfig?.GoldTokens > 0 ? 1 : 0) + (biomeConfig?.Coins > 0 ? 1 : 0);
                ContainerCapacity.AddItems(container, items, rewardStacks);

                if (biomeConfig?.ForestTokens > 0)
                    container.m_inventory.AddItem("ForestToken", biomeConfig.ForestTokens, 1, 0, 0, string.Empty, cheated: false);

                if (biomeConfig?.IronTokens > 0)
                    container.m_inventory.AddItem("IronBountyToken", biomeConfig.IronTokens, 1, 0, 0, string.Empty, cheated: false);

                if (biomeConfig?.GoldTokens > 0)
                    container.m_inventory.AddItem("GoldBountyToken", biomeConfig.GoldTokens, 1, 0, 0, string.Empty, cheated: false);

                if (biomeConfig?.Coins > 0)
                    container.m_inventory.AddItem("Coins", biomeConfig.Coins, 1, 0, 0, string.Empty, cheated: false);

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
                var label = Localization.instance.Localize("$mod_epicloot_treasurechest_name",
                    BiomeDataManager.GetLocalizationToken(Biome), (treasureMapInterval + 1).ToString());
                container.m_name = Localization.instance.Localize(label);
                container.m_privacy = hasBeenFound ? Container.PrivacySetting.Public : Container.PrivacySetting.Private;
                container.m_autoDestroyEmpty = true;

                // The chest can end up inside another player's ward (it spawns at a random point,
                // or a ward can be raised over it after the fact) that the buyer isn't permitted on.
                // Disable the guard-stone check so the buyer can always reach their purchased
                // treasure; access is still restricted to them by the Private privacy setting above.
                container.m_checkGuardStone = false;
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

            Destroy(gameObject.GetComponent<WearNTear>());
        }

        /// <summary>
        /// Whether <paramref name="container"/> is a treasure map chest its buyer has not found yet, and
        /// <paramref name="playerID"/> is not that buyer. Private privacy alone does not keep others out:
        /// Server Devcommands lets admins open any private chest (on by default), and other mods share chests.
        /// </summary>
        public static bool IsLockedFor(Container container, long playerID)
        {
            var zdo = GetChestZdo(container);
            if (zdo == null || zdo.GetBool("TreasureMapChest.HasBeenFound"))
            {
                return false;
            }

            var owner = zdo.GetLong(ZDOVars.s_creator);
            return owner != 0L && owner != playerID;
        }

        public static bool IsBuyer(Container container, long playerID)
        {
            var zdo = GetChestZdo(container);
            return zdo != null && playerID != 0L && zdo.GetLong(ZDOVars.s_creator) == playerID;
        }

        private static ZDO GetChestZdo(Container container)
        {
            if (!container.TryGetComponent(out TreasureMapChest _))
            {
                return null;
            }

            var zdo = container.m_nview == null ? null : container.m_nview.GetZDO();
            return zdo != null && zdo.IsValid() ? zdo : null;
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
    public static class Container_Awake_Patch
    {
        public static void Postfix(Container __instance)
        {
            var zdo = __instance.m_nview == null ? null : __instance.m_nview.GetZDO();
            if (zdo != null)
            {
                var biomeString = zdo.GetString($"{nameof(TreasureMapChest)}.{nameof(TreasureMapChest.Biome)}");
                if (!string.IsNullOrEmpty(biomeString))
                {
                    // The ZDO holds Biome.ToString(): an enum name for vanilla biomes, the number for a
                    // custom one. The registry reads both.
                    if (BiomeDataManager.TryResolve(biomeString, out Heightmap.Biome biome))
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

    /// <summary>
    /// Keeps an unfound treasure map chest closed to everyone but its buyer (<see cref="TreasureMapChest.IsLockedFor"/>).
    /// Every open, stack and take-all request is checked here, on the requester's machine and again on the chest
    /// owner's. Last, so it overrides Server Devcommands' postfix, which grants admins every private chest.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.CheckAccess))]
    public static class Container_CheckAccess_TreasureMapChest_Patch
    {
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Container __instance, long playerID, ref bool __result)
        {
            if (__result && TreasureMapChest.IsLockedFor(__instance, playerID))
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// Server Devcommands also turns every refused open into a granted one on the admin's machine, which would show
    /// the chest anyway. Last, so it runs after that prefix and refuses the open as vanilla does a private chest.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.RPC_OpenResponse))]
    public static class Container_RPC_OpenResponse_TreasureMapChest_Patch
    {
        [HarmonyPriority(Priority.Last)]
        public static bool Prefix(Container __instance, bool granted)
        {
            var player = Player.m_localPlayer;
            if (!granted || player == null || !TreasureMapChest.IsLockedFor(__instance, player.GetPlayerID()))
            {
                return true;
            }

            player.Message(MessageHud.MessageType.Center, "$msg_cantopen");
            return false;
        }
    }

    /// <summary>
    /// Lets a chest's buyer open it inside a ward they are not permitted on. Vanilla already does (the chest's
    /// <see cref="Container.m_checkGuardStone"/> is off), but ward mods (Protective Wards, WardIsLove) refuse every chest
    /// in a ward from their own prefix on <see cref="Container.Interact"/>, and HarmonyX runs every prefix even after one
    /// has skipped the method, so no prefix there can overrule them. The buyer's press is taken here instead, one call
    /// earlier, and handed to vanilla's Container.Interact without anyone's patches. The open request itself still goes
    /// through <see cref="Container.CheckAccess"/> on the chest's owner, as any open does.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.Interact))]
    public static class Player_Interact_TreasureMapChest_Patch
    {
        public static bool Prefix(Player __instance, GameObject go, bool hold, bool alt)
        {
            if (go == null || go.GetComponentInParent<Interactable>() is not Container container ||
                !TreasureMapChest.IsBuyer(container, __instance.GetPlayerID()))
            {
                return true;
            }

            // The rest of vanilla's Player.Interact, which wraps the Container.Interact call.
            if (__instance.InAttack() || __instance.InDodge() || (hold && Time.time - __instance.m_lastHoverInteractTime < 0.2f))
            {
                return false;
            }

            __instance.m_lastHoverInteractTime = Time.time;
            if (VanillaInteract(container, __instance, hold, alt))
            {
                __instance.DoInteractAnimation(container.gameObject);
            }

            return false;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
        public static bool VanillaInteract(Container instance, Humanoid character, bool hold, bool alt) =>
            throw new NotImplementedException("Harmony reverse patch stub");
    }

    /// <summary>
    /// WardIsLove labels every chest in a ward "no access". The buyer, who can open theirs
    /// (<see cref="Player_Interact_TreasureMapChest_Patch"/>), gets vanilla's text for it.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    public static class Container_GetHoverText_TreasureMapChest_Patch
    {
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Container __instance, ref string __result)
        {
            var player = Player.m_localPlayer;
            if (player != null && TreasureMapChest.IsBuyer(__instance, player.GetPlayerID()))
            {
                __result = VanillaGetHoverText(__instance);
            }
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
        public static string VanillaGetHoverText(Container instance) =>
            throw new NotImplementedException("Harmony reverse patch stub");
    }

    [HarmonyPatch(typeof(Container), nameof(Container.RPC_OpenResponse))]
    public static class Container_RPC_OpenRespons_Patch
    {
        public static void Postfix(Container __instance, long uid, bool granted)
        {
            var zdo = __instance.m_nview == null ? null : __instance.m_nview.GetZDO();
            var player = Player.m_localPlayer;
            var treasureMapChest = __instance.GetComponent<TreasureMapChest>();
            if (!granted || player == null || treasureMapChest == null || zdo == null || !zdo.IsValid())
            {
                return;
            }

            // Only the buyer finds their chest. Anyone else is refused an unfound one (the patches above), and
            // opening one already found credits them nothing.
            var owner = zdo.GetLong(ZDOVars.s_creator);
            if (owner != 0L && owner != player.GetPlayerID())
            {
                return;
            }

            var saveData = player.GetAdventureSaveData();
            if (zdo.GetBool("TreasureMapChest.HasBeenFound"))
            {
                // A chest another player opened first in an older build is flagged found while the buyer's map
                // is still open; the buyer finishes the map by opening it.
                if (saveData.FoundTreasureChest(treasureMapChest.Interval, treasureMapChest.Biome))
                {
                    player.SaveAdventureSaveData();
                    MessageHud.instance.ShowBiomeFoundMsg("Treasure Found!", true);
                }

                return;
            }

            EpicLoot.Log($"Player is opening treasure map chest ({treasureMapChest.Biome}, {treasureMapChest.Interval})!");
            saveData.FoundTreasureChest(treasureMapChest.Interval, treasureMapChest.Biome);
            player.SaveAdventureSaveData();

            zdo.Set("TreasureMapChest.HasBeenFound", true);

            __instance.m_privacy = Container.PrivacySetting.Public;

            MessageHud.instance.ShowBiomeFoundMsg("Treasure Found!", true);

            Object.Destroy(treasureMapChest.GetComponent<Beacon>());
            Object.Destroy(treasureMapChest.GetComponent<Rigidbody>());
        }
    }
}
