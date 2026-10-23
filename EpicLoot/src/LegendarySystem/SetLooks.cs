using HarmonyLib;

namespace EpicLoot.LegendarySystem
{
    // What the wearer publishes of the full set it shows, for every client to draw: SetAura's ID (its effects and
    // SetVisuals') and SetArmorTint's. Both count the hand items stowed on the back as worn
    // (SetBonusEvaluator.GetVisibleSetProgress), so putting a set weapon away keeps the look but not the bonuses.
    //
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
