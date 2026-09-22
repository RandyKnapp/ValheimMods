using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.Adventure
{
    /// <summary>
    /// Makes the server write adventure objects a connected player created - a bounty or treasure map
    /// spawner, and the creatures or chest it places - to disk on its next world save.
    ///
    /// Since the chunked save format, a world save rewrites only the chunks ZDOMan has marked dirty,
    /// and only local writes mark one: ZDO.IncreaseDataRevision, ZDO.IncreaseOwnerRevision, and a
    /// persistent ZDO moving between sectors. ZDOMan.RPC_ZDOData, which files the ZDOs a client sends,
    /// does none of them - a new ZDO is added to its sector while Persistent is still false, and
    /// Deserialize sets the flag and the data directly. Vanilla's ownership handoff, the usual source
    /// of dirty chunks, only runs near players, and a spawner is dropped at a map circle far from
    /// everyone. A server restart before anyone reached the circle lost the spawner, and the bounty or
    /// map stayed in progress with nothing ever appearing there. What a client places is exposed the
    /// same way until its chunk is marked by something else.
    ///
    /// The server's own spawners and placements are local writes and were never affected. The Valheim
    /// Community Patch fixes this for every object ("Fix Unsaved Client Changes"); this covers the
    /// adventure objects on servers without it, and marking the same chunk twice is harmless.
    /// </summary>
    internal static class AdventureSpawnSaveMarker
    {
        private const string PlacedRpc = "el.AdventureSpawnPlaced";

        /// <summary>
        /// How long the server waits for a placed object named by the RPC to arrive. The RPC is sent at
        /// once and the object with the next ZDO sync, so it is normally a fraction of a second; an
        /// object killed or destroyed before it synced never arrives at all.
        /// </summary>
        private const float PendingSeconds = 60f;

        /// <summary>Server only: placed objects the RPC named before their ZDO arrived, and when.</summary>
        private static readonly Dictionary<ZDOID, float> Pending = new();
        private static readonly List<ZDOID> ExpiredScratch = new();

        internal static void RegisterRPC(ZRoutedRpc routedRpc)
        {
            if (Common.Utils.IsServer())
            {
                routedRpc.Register<ZPackage>(PlacedRpc, RPC_Placed);
            }
        }

        /// <summary>
        /// Marks the chunks of objects the spawner just placed for the next world save. Only the server
        /// can mark one, so a client names the objects to the server instead.
        /// </summary>
        internal static void MarkPlaced(IReadOnlyList<ZDO> placed)
        {
            if (placed.Count == 0 || ZNet.instance == null || ZDOMan.instance == null)
            {
                return;
            }

            if (ZNet.instance.IsServer())
            {
                foreach (ZDO zdo in placed)
                {
                    ZDOMan.instance.SetDirtySector(zdo);
                }

                return;
            }

            var pkg = new ZPackage();
            pkg.Write(placed.Count);
            foreach (ZDO zdo in placed)
            {
                pkg.Write(zdo.m_uid);
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(PlacedRpc, pkg);
        }

        private static void RPC_Placed(long sender, ZPackage pkg)
        {
            if (ZDOMan.instance == null)
            {
                return;
            }

            PruneExpired();

            int count = pkg.ReadInt();
            int waiting = 0;
            for (int i = 0; i < count; i++)
            {
                ZDOID id = pkg.ReadZDOID();
                ZDO zdo = ZDOMan.instance.GetZDO(id);
                if (zdo != null)
                {
                    ZDOMan.instance.SetDirtySector(zdo);
                }
                else
                {
                    // Usually the case: the RPC went out immediately, the objects wait for the next sync.
                    Pending[id] = Time.time;
                    waiting++;
                }
            }

            EpicLoot.Log($"Marked {count - waiting} placed adventure object(s) for the next world save; " +
                $"{waiting} will be marked when they arrive.");
        }

        private static void PruneExpired()
        {
            if (Pending.Count == 0)
            {
                return;
            }

            ExpiredScratch.Clear();
            foreach (KeyValuePair<ZDOID, float> entry in Pending)
            {
                if (Time.time - entry.Value > PendingSeconds)
                {
                    ExpiredScratch.Add(entry.Key);
                }
            }

            foreach (ZDOID id in ExpiredScratch)
            {
                Pending.Remove(id);
            }

            ExpiredScratch.Clear();
        }

        /// <summary>
        /// ZDO.Deserialize's only caller is ZDOMan.RPC_ZDOData, so this sees every ZDO a peer sends:
        /// spawners, and the placed objects the RPC is still waiting for.
        /// </summary>
        [HarmonyPatch(typeof(ZDO), nameof(ZDO.Deserialize))]
        private static class ZDO_Deserialize_Patch
        {
            [UsedImplicitly]
            private static void Postfix(ZDO __instance)
            {
                // Runs for every ZDO received, so the cheap tests come first. Pending is only ever
                // filled on the server.
                if (__instance.GetPrefab() != AdventureSpawnController.PrefabHash &&
                    (Pending.Count == 0 || !Pending.Remove(__instance.m_uid)))
                {
                    return;
                }

                if (ZNet.instance == null || !ZNet.instance.IsServer() || ZDOMan.instance == null)
                {
                    return;
                }

                // RPC_ZDOData sets the position before calling Deserialize, so this is the sector the
                // object was filed in. SetDirtySector is the call a local write makes.
                ZDOMan.instance.SetDirtySector(__instance);
            }
        }
    }
}
