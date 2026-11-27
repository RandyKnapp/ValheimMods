using EpicLoot.General;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot
{
    public enum StaffRole
    {
        Damage,
        Summon,
        Support
    }

    public static class StaffRoles
    {
        private const int MaxSpawnDepth = 4;

        private sealed class Profile
        {
            public readonly HashSet<StaffRole> Roles = new HashSet<StaffRole>();
            public HitData.DamageTypes Dealt;
        }

        private static readonly Dictionary<ItemDrop.ItemData.SharedData, Profile> Cache =
            new Dictionary<ItemDrop.ItemData.SharedData, Profile>();

        // Each ObjectDB brings its own SharedData objects, so the last world's entries would never be hit again.
        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class ObjectDB_Awake_Patch
        {
            private static void Prefix()
            {
                Cache.Clear();
            }
        }

        public static bool IsStaff(ItemDrop.ItemData item)
        {
            Skills.SkillType skill = item.m_shared.m_skillType;
            return skill == Skills.SkillType.BloodMagic || skill == Skills.SkillType.ElementalMagic;
        }

        public static bool HasAnyRole(ItemDrop.ItemData item, List<StaffRole> roles)
        {
            HashSet<StaffRole> itemRoles = GetRoles(item);
            foreach (StaffRole role in roles)
            {
                if (itemRoles.Contains(role))
                {
                    return true;
                }
            }

            return false;
        }

        public static HashSet<StaffRole> GetRoles(ItemDrop.ItemData item)
        {
            return GetProfile(item).Roles;
        }

        // The staff's own damage plus what the objects it spawns carry themselves: an Orb of Ahri or a Staff of Frost
        // Orbs has none of its own, and its orbs' damage is what the imbues scale (ModifyDamage.ApplyToSpawnedDamage).
        public static HitData.DamageTypes GetDealtDamage(ItemDrop.ItemData item)
        {
            return GetProfile(item).Dealt;
        }

        private static Profile GetProfile(ItemDrop.ItemData item)
        {
            if (!Cache.TryGetValue(item.m_shared, out Profile profile))
            {
                profile = Classify(item.m_shared);
                Cache[item.m_shared] = profile;
            }

            return profile;
        }

        // Vanilla sets up a spawn-on-hit projectile or Aoe without hit data, so it keeps its prefab damage
        // instead of the weapon's and never sees the weapon's effects.
        public static bool CarriesOwnDamage(Character owner, HitData hitData, ItemDrop.ItemData item)
        {
            return hitData == null && item?.m_shared != null && owner != null && owner == Player.m_localPlayer &&
                IsStaff(item);
        }

        private static Profile Classify(ItemDrop.ItemData.SharedData shared)
        {
            var profile = new Profile { Dealt = shared.m_damages };
            if (shared.m_damages.EpicLootGetTotalDamage() > 0f)
            {
                profile.Roles.Add(StaffRole.Damage);
            }

            var visited = new HashSet<GameObject>();
            Inspect(shared.m_attack?.m_attackProjectile, false, true, 0, visited, profile);
            Inspect(shared.m_secondaryAttack?.m_attackProjectile, false, true, 0, visited, profile);

            if (profile.Roles.Count == 0)
            {
                profile.Roles.Add(StaffRole.Support);
            }

            return profile;
        }

        // Mirrors how vanilla finds the object to set up: the attack's own IProjectile, or for a spawn-on-hit
        // object the first IProjectile anywhere in its children.
        private static void Inspect(GameObject prefab, bool spawned, bool receivesHitData, int depth,
            HashSet<GameObject> visited, Profile profile)
        {
            if (prefab == null || depth > MaxSpawnDepth || !visited.Add(prefab))
            {
                return;
            }

            IProjectile target = spawned ? prefab.GetComponentInChildren<IProjectile>() : prefab.GetComponent<IProjectile>();
            switch (target)
            {
                case SpawnAbility:
                    profile.Roles.Add(StaffRole.Summon);
                    break;
                case Aoe aoe:
                    if (!receivesHitData && aoe.m_hitEnemy && aoe.m_damage.EpicLootGetTotalDamage() > 0f)
                    {
                        profile.Roles.Add(StaffRole.Damage);
                        profile.Dealt.Add(aoe.m_damage);
                    }
                    break;
                case Projectile projectile:
                    if (!receivesHitData && projectile.m_damage.EpicLootGetTotalDamage() > 0f)
                    {
                        profile.Roles.Add(StaffRole.Damage);
                        profile.Dealt.Add(projectile.m_damage);
                    }
                    Inspect(projectile.m_spawnOnHit, true, projectile.m_projectilesInheritHitData, depth + 1, visited, profile);
                    break;
            }
        }
    }

    // Reads a role list leniently: an unknown name is skipped with a warning, so one typo in a config file or an API
    // definition drops that role instead of failing the whole file or effect. Writing is left to Json.NET.
    public class StaffRoleListConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<StaffRole>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var roles = new List<StaffRole>();
            if (reader.TokenType == JsonToken.Null)
            {
                return roles;
            }

            if (reader.TokenType != JsonToken.StartArray)
            {
                throw new JsonSerializationException($"AllowedStaffRoles must be a list, not {reader.TokenType}.");
            }

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                // Server-to-client sync writes the roles as numbers, config files and the API as names.
                object value = reader.Value;
                bool known = reader.TokenType switch
                {
                    JsonToken.Integer => Enum.IsDefined(typeof(StaffRole), Convert.ToInt32(value)),
                    JsonToken.String => Enum.TryParse((string)value, true, out StaffRole parsed) &&
                        Enum.IsDefined(typeof(StaffRole), parsed),
                    _ => false
                };
                if (known)
                {
                    roles.Add(reader.TokenType == JsonToken.Integer
                        ? (StaffRole)Convert.ToInt32(value)
                        : (StaffRole)Enum.Parse(typeof(StaffRole), (string)value, true));
                    continue;
                }

                EpicLoot.LogWarningForce($"Unknown staff role '{value ?? reader.TokenType}' in AllowedStaffRoles was " +
                    $"ignored. Roles are {string.Join(", ", Enum.GetNames(typeof(StaffRole)))}.");
                reader.Skip();
            }

            return roles;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
