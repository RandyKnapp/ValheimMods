using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.Adventure
{
    public class SE_CustomFinder : SE_Finder
    {
        public List<Type> RequiredComponentTypes;
        public List<Type> DisallowedComponentTypes;

        public override void UpdateStatusEffect(float dt)
        {
            m_updateBeaconTimer += dt;
            if (m_updateBeaconTimer > 1.0)
            {
                m_updateBeaconTimer = 0.0f;
                var closestBeaconInRange = FindClosestBeaconInRange(m_character.transform.position);
                if (closestBeaconInRange != m_beacon)
                {
                    m_beacon = closestBeaconInRange;
                    if (m_beacon != null)
                    {
                        m_lastDistance = Utils.DistanceXZ(m_character.transform.position, m_beacon.transform.position);
                        m_pingTimer = 0.0f;
                    }
                }
            }

            if (m_beacon == null)
            {
                return;
            }

            var distanceToBeacon = Utils.DistanceXZ(m_character.transform.position, m_beacon.transform.position);
            var t = Mathf.Clamp01(distanceToBeacon / m_beacon.m_range);
            var beaconPingFrequency = Mathf.Lerp(m_closeFrequency, m_distantFrequency, t);

            m_pingTimer += dt;
            if (m_pingTimer <= beaconPingFrequency)
            {
                return;
            }

            m_pingTimer = 0.0f;
            if (t < 0.2f)
            {
                m_pingEffectNear.Create(m_character.transform.position, m_character.transform.rotation, m_character.transform);
            }
            else if (t < 0.6f)
            {
                m_pingEffectMed.Create(m_character.transform.position, m_character.transform.rotation, m_character.transform);
            }
            else
            {
                m_pingEffectFar.Create(m_character.transform.position, m_character.transform.rotation, m_character.transform);
            }
            m_lastDistance = distanceToBeacon;
        }

        public Beacon FindClosestBeaconInRange(Vector3 point)
        {
            Beacon beacon = null;
            var closestDistance = float.MaxValue;
            foreach (var instance in Beacon.m_instances)
            {
                var allowed = true;

                if (RequiredComponentTypes != null)
                {
                    foreach (var allowedComponentType in RequiredComponentTypes)
                    {
                        var c = instance.GetComponent(allowedComponentType);
                        if (c == null)
                        {
                            allowed = false;
                            break;
                        }
                    }
                }

                if (DisallowedComponentTypes != null)
                {
                    foreach (var disallowedComponentType in DisallowedComponentTypes)
                    {
                        var c = instance.GetComponent(disallowedComponentType);
                        if (c != null)
                        {
                            allowed = false;
                            break;
                        }
                    }
                }

                if (!allowed)
                {
                    continue;
                }

                var num2 = Vector3.Distance(point, instance.transform.position);
                if (num2 < instance.m_range && (beacon == null || num2 < closestDistance))
                {
                    beacon = instance;
                    closestDistance = num2;
                }
            }
            return beacon;
        }
    }
}
