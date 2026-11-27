using System;
using HarmonyLib;

namespace EpicLoot.LegendarySystem
{
    // Stowing and drawing (Humanoid.HideHandItems/ShowHandItems) move one hand at a time, and each step's unequip or
    // equip would publish a loadout with that item neither in the hand nor on the back yet. That breaks the set for a
    // moment, which restarts its effects, so publishing waits until the move is done.
    public static class SetLooks
    {
        private static int _held;

        public static void Publish(Player player)
        {
            if (_held > 0)
            {
                return;
            }

            SetAura.Publish(player);
            SetArmorTint.Publish(player);
        }

        // Writes the ID of the first full set worn (stowed hand items count) that `shows` accepts, or "" for none, to
        // the local player's ZDO. False when there is no local player ZDO to write to.
        internal static bool PublishFullSet(Player player, int zdoHash, Func<LegendarySetInfo, bool> shows)
        {
            if (player == null || player != Player.m_localPlayer || player.m_nview == null ||
                !(player.m_nview.GetZDO() is ZDO zdo))
            {
                return false;
            }

            string setID = "";
            foreach (LegendarySetProgress progress in SetBonusEvaluator.GetVisibleSetProgress(player))
            {
                if (progress.IsFull && shows(progress.Set))
                {
                    setID = progress.Set.ID;
                    break;
                }
            }

            if (zdo.GetString(zdoHash) != setID)
            {
                zdo.Set(zdoHash, setID);
            }

            return true;
        }

        private static void Hold(Humanoid humanoid)
        {
            if (humanoid != null && humanoid == Player.m_localPlayer)
            {
                _held++;
            }
        }

        private static void Release(Humanoid humanoid)
        {
            if (humanoid != null && humanoid == Player.m_localPlayer && _held > 0 && --_held == 0)
            {
                Publish((Player)humanoid);
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.HideHandItems))]
        private static class Humanoid_HideHandItems_Patch
        {
            private static void Prefix(Humanoid __instance) => Hold(__instance);

            private static void Finalizer(Humanoid __instance) => Release(__instance);
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.ShowHandItems))]
        private static class Humanoid_ShowHandItems_Patch
        {
            private static void Prefix(Humanoid __instance) => Hold(__instance);

            private static void Finalizer(Humanoid __instance) => Release(__instance);
        }
    }
}
