using UnityEngine;

namespace EpicLoot.Adventure
{
    /// <summary>How close an adventure spawn point is to a player's ward, nearest first.</summary>
    internal enum WardProximity
    {
        /// <summary>No enabled ward within its radius plus the buffer.</summary>
        Clear,

        /// <summary>Within a ward's radius plus the buffer, but outside the area the ward protects.</summary>
        NearWard,

        /// <summary>Inside the area a ward protects.</summary>
        InsideWard
    }

    /// <summary>
    /// The single definition of "this adventure spawn point is too close to a player's ward".
    ///
    /// Checked once, at placement time, from
    /// <see cref="AdventureSpawnController.DeterminespawnPoint"/> - the only point in the flow where
    /// it can mean anything. Picking the world point is seed-only and loads nothing, so
    /// <see cref="PrivateArea.m_allAreas"/> holds no ward from a remote zone and a check there would
    /// have approved every location regardless. The placement search is the one that runs with the
    /// area genuinely loaded. It prefers spots clear of wards but never leaves the map circle to find
    /// one, so a ward only decides where inside the circle a spawn lands.
    /// </summary>
    internal static class AdventureWardCheck
    {
        /// <summary>
        /// How close <paramref name="location"/> is to an <b>enabled</b> ward, where "near" means inside
        /// the ward's radius grown by <paramref name="buffer"/>. The <see cref="PrivateArea.IsEnabled"/>
        /// test matters: a deactivated guard stone protects nothing in vanilla, so it must not steer a
        /// spawn either. Every vanilla call site pairs the two the same way (PrivateArea.CheckAccess,
        /// OnObjectDamaged, GetNearbyAreas). <paramref name="ward"/> is the ward responsible, or null.
        /// </summary>
        internal static WardProximity GetWardProximity(Vector3 location, float buffer, out PrivateArea ward)
        {
            ward = null;
            WardProximity proximity = WardProximity.Clear;

            foreach (PrivateArea privateArea in PrivateArea.m_allAreas)
            {
                if (privateArea == null || !privateArea.IsEnabled())
                {
                    continue;
                }

                if (privateArea.IsInside(location, 0f))
                {
                    ward = privateArea;
                    return WardProximity.InsideWard;
                }

                if (proximity == WardProximity.Clear && privateArea.IsInside(location, buffer))
                {
                    ward = privateArea;
                    proximity = WardProximity.NearWard;
                }
            }

            return proximity;
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
