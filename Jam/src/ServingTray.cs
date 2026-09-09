using Common;
using Jotunn.Managers;
using UnityEngine;

namespace Jam
{
    /// <summary>
    /// Keeps the Serving Tray (Feaster) piece table in step with each jam's craftable setting.
    ///
    /// This lives in Jam rather than in <see cref="ItemBatchLoader"/> because the loader has no piece
    /// table concept and Feaster is a specific piece, not a general item property.
    /// </summary>
    public static class ServingTray
    {
        public static void Register()
        {
            // A dedicated server never registers the item prefabs (ItemBatchLoader.BatchSetup skips
            // BatchAddItems there), so there is no Feaster piece table to keep in step and every
            // lookup below would fail. Same headless check the loader uses.
            if (Common.Utils.IsServer()) { return; }

            // Run once the items exist, and again whenever the server pushes its config down.
            ItemManager.OnItemsRegistered += SyncAll;
            SynchronizationManager.OnConfigurationSynchronized += (_, __) => SyncAll();

            foreach (ItemDefinition jam in JamItems.Definitions)
            {
                ItemDefinition itemdef = jam;
                itemdef.CraftableCfg.SettingChanged += (_, __) =>
                {
                    ConfigChangeDebouncer.Schedule(itemdef.CraftableCfg, () => Sync(itemdef));
                };
            }
        }

        public static void SyncAll()
        {
            foreach (ItemDefinition jam in JamItems.Definitions)
            {
                Sync(jam);
            }
        }

        private static void Sync(ItemDefinition itemdef)
        {
            GameObject item = PrefabManager.Instance.GetPrefab(itemdef.Prefab);

            if (item == null)
            {
                Jam.JamLogger.LogError($"Could not find prefab for item {itemdef.Prefab}");
                return;
            }

            GameObject trayPrefab = PrefabManager.Instance.GetPrefab("Feaster");

            if (trayPrefab == null || !trayPrefab.TryGetComponent(out ItemDrop trayItemDrop))
            {
                Jam.JamLogger.LogWarning("Serving Tray not found, will not add build piece.");
                return;
            }

            PieceTable pieceTable = trayItemDrop.m_itemData.m_shared.m_buildPieces;
            // m_pieces holds prefab references, so their names carry no "(Clone)" suffix.
            GameObject pieceTablePiece = pieceTable.m_pieces.Find(x => x != null && x.name == itemdef.Prefab);
            bool enabled = itemdef.CraftableCfg.Value;

            if (!enabled && pieceTablePiece != null)
            {
                pieceTable.m_pieces.Remove(pieceTablePiece);
            }
            else if (enabled && pieceTablePiece == null)
            {
                pieceTable.m_pieces.Add(item);
            }
        }
    }
}
