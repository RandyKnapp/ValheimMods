using UnityEngine;

namespace EpicLoot.Adventure
{
    /// <summary>
    /// The single definition of "this adventure spawn point is too close to a player's ward".
    ///
    /// Checked once, at placement time, from
    /// <see cref="AdventureSpawnController.DeterminespawnPoint"/> - the only point in the flow where
    /// it can mean anything. Picking the world point is seed-only and loads nothing, so
    /// <see cref="PrivateArea.m_allAreas"/> holds no ward from a remote zone and a check there would
    /// have approved every location regardless. The placement search is the one that runs with the
    /// area genuinely loaded, and it expands its search band when a ward vetoes a candidate.
    /// </summary>
    internal static class AdventureWardCheck
    {
        /// <summary>
        /// True when an <b>enabled</b> ward covers <paramref name="location"/> once its radius is
        /// grown by <paramref name="buffer"/>. The <see cref="PrivateArea.IsEnabled"/> test matters:
        /// a deactivated guard stone protects nothing in vanilla, so it must not veto a spawn either.
        /// Every vanilla call site pairs the two the same way (PrivateArea.CheckAccess, OnObjectDamaged,
        /// GetNearbyAreas).
        /// </summary>
        internal static bool TryFindNearbyWard(Vector3 location, float buffer, out PrivateArea ward)
        {
            foreach (PrivateArea privateArea in PrivateArea.m_allAreas)
            {
                if (privateArea != null && privateArea.IsEnabled() && privateArea.IsInside(location, buffer))
                {
                    ward = privateArea;
                    return true;
                }
            }

            ward = null;
            return false;
        }

        /// <summary>
        /// Describes <paramref name="ward"/> for a log line. Null-safe so callers can log a miss.
        /// </summary>
        internal static string DescribeWard(PrivateArea ward)
        {
            if (ward == null)
            {
                return "none";
            }

            Vector3 position = ward.transform.position;
            return $"({position.x:0.##}, {position.y:0.##}, {position.z:0.##}) radius={ward.m_radius:0.##}";
        }
    }
}
