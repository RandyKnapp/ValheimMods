using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string HelsHarvest = nameof(HelsHarvest);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // HelsHarvest (4-piece Hel's Covenant set bonus, NoRoll). While the set staff is in hand, a creature killed by the
    // wearer or by one of the wearer's summons rises at its corpse as a thrall: a friendly skeleton that follows and
    // fights for Lifetime seconds, at the star level of the creature it rose from and stronger with the wearer's Blood
    // Magic skill (as Dead Raiser's skeletons are). At most the effect value are up at once; a new one makes the oldest
    // crumble. A thrall's own kills raise more.
    //
    // Who made the kill is only known where the victim dies: OnDeath runs on the victim's owner, which may be another
    // client or the dedicated server, and m_lastHit is only set there. So a Character.OnDeath prefix on the owner works
    // out the credited player -- the attacker itself, or the player whose summon it was -- and sends that player's own
    // client an RPC with the corpse's place and level, which raises the thrall there, so the thralls are always owned by
    // the player they follow. A player
    // whose client has the full set says so in a ZDO flag, so kills by anyone else cost no RPC.
    //
    // A summon's owner is recorded on its ZDO ("el-sumby") when a staff spawns it (SpawnAbility.SetupAoe, called once
    // per spawned creature), since the Trollstav troll has no other link back to its summoner; Dead Raiser skeletons
    // also carry their summoner's name in s_follow, which is the fallback.
    //
    // Thralls are EL_HelThrall, a ZNetScene-registered copy of Skeleton_Friendly under another name, so Dead Raiser's
    // own summon cap and its same-name unsummon never count them. They are non-persistent, so they are never saved and
    // vanish with their owner, carry no drops, and die on a timer.
    public static class HelsHarvest
    {
        // Both tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultLifetime = 30f;
        public const float DefaultSkillFactorPerLevel = 0.02f;  // Dead Raiser's m_copySkillToRandomFactor

        private const string LifetimeKey = "Lifetime";
        private const string SkillFactorPerLevelKey = "SkillFactorPerLevel";

        private static float Lifetime => EffectConfig.GetClamped(MagicEffectType.HelsHarvest, LifetimeKey, DefaultLifetime, 5f, 300f);
        private static float SkillFactorPerLevel => EffectConfig.GetClamped(MagicEffectType.HelsHarvest,
            SkillFactorPerLevelKey, DefaultSkillFactorPerLevel, 0f, 0.1f);

        private const string SourcePrefab = "Skeleton_Friendly";
        public const string ThrallPrefab = "EL_HelThrall";
        private static readonly int ThrallHash = ThrallPrefab.GetStableHashCode();
        private const string SpawnFxPrefab = "fx_summon_skeleton_spawn";
        private const float MaxWaterDepth = 1f;     // deeper water over the corpse than this, and nothing rises
        private const int GroundCastMargin = 2;

        // On a summon's ZDO: the summoner's player ID. On a player's ZDO: wearing the full set.
        internal static readonly int SummonedByHash = "el-sumby".GetStableHashCode();
        private static readonly int HelFlagHash = "el-hel".GetStableHashCode();
        private const string RaiseRpc = "el-helraise";

        // StarLevelSystem's own keys (common/DataObjects.cs), written as bools before Instantiate, as SpiritAnimal does:
        // they keep its spawn multiplication from cloning a thrall and its levelling from changing the level it rose with.
        private static readonly int SlsSpawnMultHash = "SLS_MULT".GetStableHashCode();
        private static readonly int SlsSpawnManagedHash = "SLS_EXTMGD".GetStableHashCode();
        private static readonly int SlsNoSleepHash = "SLS_NOSLEEP".GetStableHashCode();

        private static GameObject _container;       // disabled parent that keeps the template from Awaking
        private static GameObject _template;
        private static readonly HashSet<string> MissingLogged = new HashSet<string>();

        // State for the local player only; SyncOwner drops it when the local player changes.
        private static Player _owner;
        private static bool _flagPublished;
        private static readonly List<Character> Thralls = new List<Character>();

        // Kills credited faster than thralls rise (an area attack, a chain): raised one at a time, and only the newest
        // that fit under the cap, so a multi-kill does not spawn skeletons only to crumble them in the same frame.
        private const float RaiseInterval = 0.2f;
        private const float RaiseMaxAge = 5f;       // a kill this old raises nothing; the fight has moved on
        private struct RaiseRequest
        {
            public Vector3 Position;
            public float Yaw;
            public int Level;
            public float Time;
        }
        private static readonly List<RaiseRequest> PendingRaises = new List<RaiseRequest>();
        private static float _nextRaiseAt;

        // Tooltip: "... rise as thralls that fight for you for {1} seconds ... Up to {0} at once ..."
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.HelsHarvest, value => new object[] { value, Lifetime });
        }

        private static void SyncOwner(Player player)
        {
            if (_owner == player)
            {
                return;
            }

            _owner = player;
            _flagPublished = false;
            Thralls.Clear();
            PendingRaises.Clear();
        }

        // Written only on change, so the ZDO revision moves once per change rather than every frame.
        private static void PublishFlag(Player player)
        {
            bool hasSet = player.HasActiveMagicEffect(MagicEffectType.HelsHarvest, out float _);
            if (hasSet == _flagPublished)
            {
                return;
            }

            ZDO zdo = player.m_nview != null ? player.m_nview.GetZDO() : null;
            if (zdo != null)
            {
                _flagPublished = hasSet;
                zdo.Set(HelFlagHash, hasSet);
            }
        }

        // Victim's owner. Who gets the kill: a player, or the player who summoned the killer.
        private static void OnCreatureDeath(Character victim)
        {
            if (victim is Player || victim.IsTamed() || victim.m_nview == null || !victim.m_nview.IsValid() ||
                !victim.m_nview.IsOwner() || victim.m_nview.GetZDO().GetPrefab() == ThrallHash)
            {
                return;
            }

            Character killer = victim.m_lastHit?.GetAttacker();
            Player credited = killer as Player ?? SummonerOf(killer);
            if (credited == null || credited.m_nview == null || !credited.m_nview.IsValid() ||
                !credited.m_nview.GetZDO().GetBool(HelFlagHash))
            {
                return;
            }

            Vector3 position = victim.transform.position;
            float yaw = victim.transform.rotation.eulerAngles.y;
            credited.m_nview.InvokeRPC(RaiseRpc, position, yaw, victim.GetLevel());
        }

        private static Player SummonerOf(Character summon)
        {
            if (summon == null || summon.m_nview == null || !summon.m_nview.IsValid())
            {
                return null;
            }

            ZDO zdo = summon.m_nview.GetZDO();
            long summoner = zdo.GetLong(SummonedByHash);
            if (summoner != 0L)
            {
                return Player.GetPlayer(summoner);
            }

            // A staff summon from before the stamp existed: Dead Raiser's skeletons follow their summoner by name.
            if (!summon.IsTamed() || !summon.TryGetComponent(out Tameable tameable) ||
                tameable.m_levelUpOwnerSkill != Skills.SkillType.BloodMagic &&
                tameable.m_levelUpOwnerSkill != Skills.SkillType.ElementalMagic)
            {
                return null;
            }

            string follow = zdo.GetString(ZDOVars.s_follow);
            if (string.IsNullOrEmpty(follow))
            {
                return null;
            }

            foreach (Player player in Player.GetAllPlayers())
            {
                if (player.GetPlayerName() == follow)
                {
                    return player;
                }
            }
            return null;
        }

        // The credited player's own client: queued, and raised from Player.Update (ProcessRaises).
        private static void OnRaiseRpc(Player player, Vector3 position, float yaw, int level)
        {
            if (player != Player.m_localPlayer || player.IsDead() ||
                !player.HasActiveMagicEffect(MagicEffectType.HelsHarvest, out float value))
            {
                return;
            }

            SyncOwner(player);
            PendingRaises.Add(new RaiseRequest { Position = position, Yaw = yaw, Level = level, Time = Time.time });
            int cap = Mathf.Max(1, Mathf.RoundToInt(value));
            if (PendingRaises.Count > cap)
            {
                PendingRaises.RemoveRange(0, PendingRaises.Count - cap);
            }
        }

        private static void ProcessRaises(Player player)
        {
            if (PendingRaises.Count == 0 || Time.time < _nextRaiseAt)
            {
                return;
            }

            RaiseRequest request = PendingRaises[0];
            PendingRaises.RemoveAt(0);
            if (Time.time - request.Time > RaiseMaxAge || player.IsDead() ||
                !player.HasActiveMagicEffect(MagicEffectType.HelsHarvest, out float value))
            {
                return;
            }

            _nextRaiseAt = Time.time + RaiseInterval;
            Raise(player, request.Position, request.Yaw, request.Level, value);
        }

        private static void Raise(Player player, Vector3 position, float yaw, int level, float value)
        {
            Thralls.RemoveAll(thrall => thrall == null || thrall.IsDead());
            int cap = Mathf.Max(1, Mathf.RoundToInt(value));
            while (Thralls.Count >= cap)
            {
                Character oldest = Thralls[0];
                Thralls.RemoveAt(0);
                Crumble(oldest);
            }

            // Cast from just above the corpse, not from the sky: under a roof or in a dungeon the roof is not the ground.
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetSolidHeight(position, out float height, GroundCastMargin))
            {
                position.y = height;
            }
            if (Floating.GetLiquidLevel(position) - position.y > MaxWaterDepth)
            {
                return;
            }

            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(ThrallHash) : null;
            if (prefab == null)
            {
                if (MissingLogged.Add(ThrallPrefab))
                {
                    EpicLoot.LogWarning($"[HelsHarvest] the '{ThrallPrefab}' prefab is not registered; no thralls will rise.");
                }
                return;
            }

            Character thrall = null;
            try
            {
                thrall = Spawn(prefab, position, Quaternion.Euler(0f, yaw, 0f), player, level);
            }
            catch (Exception e)
            {
                EpicLoot.LogError($"[HelsHarvest] raising a thrall failed: {e}");
            }

            if (thrall == null)
            {
                return;
            }

            Thralls.Add(thrall);
            var fx = ZNetScene.instance.GetPrefab(SpawnFxPrefab);
            if (fx != null)
            {
                Object.Instantiate(fx, position, Quaternion.identity);
            }
            EpicLoot.Log($"[HelsHarvest] A level {thrall.GetLevel()} thrall rises ({Thralls.Count}/{cap}).");
        }

        // A thrall is always owned by the client that raised it, so it is killed here, as its own timer would.
        private static void Crumble(Character thrall)
        {
            if (thrall == null || thrall.IsDead() || thrall.m_nview == null || !thrall.m_nview.IsValid() ||
                !thrall.m_nview.IsOwner())
            {
                return;
            }

            thrall.ApplyDamage(new HitData
            {
                m_damage = { m_damage = 99999f },
                m_point = thrall.transform.position
            }, showDamageText: false, triggerEffects: true);
        }

        // ZNetView.Awake's new-object branch, with the data written before Instantiate as ZNetScene.CreateObject does.
        // The level is read in Character.Awake, so health, damage and the star visuals all follow it.
        private static Character Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Player player, int level)
        {
            var prefabView = prefab.GetComponent<ZNetView>();
            int hash = prefab.name.GetStableHashCode();

            ZDO zdo = ZDOMan.instance.CreateNewZDO(position, hash);   // owned by this peer
            zdo.Persistent = false;
            zdo.Type = prefabView.m_type;
            zdo.Distant = prefabView.m_distant;
            zdo.SetPrefab(hash);          // before Instantiate: an unset prefab reads as an invalid ZDO
            zdo.SetRotation(rotation);

            zdo.Set(ZDOVars.s_level, Mathf.Max(1, level));
            zdo.Set(ZDOVars.s_tamed, true);
            zdo.Set(ZDOVars.s_sleeping, false);
            zdo.Set(ZDOVars.s_despawnInDay, false);
            zdo.Set(ZDOVars.s_eventCreature, false);
            zdo.Set(ZDOVars.s_follow, player.GetPlayerName());
            // Character.GetSkillLevel is a gameplay read, so EpicLoot's own skill bonuses count, as they do for Dead
            // Raiser's skeletons.
            zdo.Set(ZDOVars.s_randomSkillFactor, 1f + player.GetSkillLevel(Skills.SkillType.BloodMagic) * SkillFactorPerLevel);
            zdo.Set(SummonedByHash, player.GetPlayerID());
            zdo.Set(SlsSpawnMultHash, true);
            zdo.Set(SlsSpawnManagedHash, true);
            zdo.Set(SlsNoSleepHash, true);

            GameObject instance = null;
            ZNetView.m_useInitZDO = true;
            ZNetView.m_initZDO = zdo;
            try
            {
                instance = Object.Instantiate(prefab, position, rotation);
            }
            finally
            {
                // A throwing Awake must not leave our ZDO for the next networked object spawned anywhere to adopt.
                if (ZNetView.m_initZDO != null)
                {
                    ZNetView.m_initZDO = null;
                    ZDOMan.instance.DestroyZDO(zdo);
                    if (instance != null)
                    {
                        Object.Destroy(instance);
                        instance = null;
                    }
                }
                ZNetView.m_useInitZDO = false;
            }

            var character = instance != null ? instance.GetComponent<Character>() : null;
            if (character == null)
            {
                if (instance != null)
                {
                    ZNetScene.instance.Destroy(instance);
                }
                return null;
            }

            if (character.GetBaseAI() is MonsterAI ai)
            {
                ai.m_fallAsleepDistance = 0f;
                ai.SetFollowTarget(player.gameObject);
                ai.Alert();
            }
            instance.GetComponent<ZSyncAnimation>()?.SetBool("wakeup", true);
            instance.GetComponent<CharacterTimedDestruction>()?.Trigger(Lifetime);
            return character;
        }

        // Hooked to PrefabManager.OnPrefabsRegistered (a ZNetScene.Awake postfix on every client and the server, each
        // world load). The template is built once and kept across worlds; later loads only re-inject it.
        public static void RegisterPrefabs()
        {
            ZNetScene zns = ZNetScene.instance;
            if (zns == null)
            {
                return;
            }

            if (_template == null)
            {
                _template = BuildTemplate(zns);
            }
            if (_template == null)
            {
                return;
            }

            if (!zns.m_prefabs.Contains(_template))
            {
                zns.m_prefabs.Add(_template);
            }
            zns.m_namedPrefabs[ThrallHash] = _template;
        }

        private static GameObject BuildTemplate(ZNetScene zns)
        {
            GameObject source = zns.GetPrefab(SourcePrefab);
            if (source == null)
            {
                if (MissingLogged.Add(SourcePrefab))
                {
                    EpicLoot.LogWarning($"[HelsHarvest] could not find the '{SourcePrefab}' prefab; no thralls will rise.");
                }
                return null;
            }

            if (_container == null)
            {
                _container = new GameObject("EL_HelThrallContainer");
                _container.SetActive(false);
                Object.DontDestroyOnLoad(_container);
            }

            // Under the disabled container nothing on the copy Awakes, while activeSelf stays true: remote clients
            // build it from this template and never call SetActive on it.
            GameObject template = Object.Instantiate(source, _container.transform);
            template.name = ThrallPrefab;

            var character = template.GetComponent<Character>();
            if (character != null)
            {
                character.m_name = "$mod_epicloot_hel_thrall";
            }

            var tameable = template.GetComponent<Tameable>();
            if (tameable != null)
            {
                tameable.m_randomStartingName = new List<string>();
            }

            var view = template.GetComponent<ZNetView>();
            if (view != null)
            {
                view.m_persistent = false;
            }

            foreach (var drop in template.GetComponentsInChildren<CharacterDrop>(true))
            {
                Object.DestroyImmediate(drop);
            }

            var timed = template.GetComponent<CharacterTimedDestruction>() ?? template.AddComponent<CharacterTimedDestruction>();
            timed.m_triggerOnAwake = false;
            return template;
        }

        // Staff summons record their summoner: SetupAoe runs once for each creature a SpawnAbility spawns, with that
        // creature, on the caster's client, right after it was created there.
        [HarmonyPatch(typeof(SpawnAbility), nameof(SpawnAbility.SetupAoe))]
        private static class SpawnAbility_SetupAoe_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(SpawnAbility __instance, Character owner)
            {
                if (owner == null || !(__instance.m_owner is Player player) || player != Player.m_localPlayer ||
                    owner.m_nview == null || !owner.m_nview.IsValid() || !owner.m_nview.IsOwner())
                {
                    return;
                }

                owner.m_nview.GetZDO().Set(SummonedByHash, player.GetPlayerID());
            }
        }

        // OnDeath also runs from the death animation's event on every client; only the owner's call counts.
        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        private static class Character_OnDeath_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Character __instance)
            {
                try
                {
                    OnCreatureDeath(__instance);
                }
                catch (Exception e)
                {
                    EpicLoot.LogError($"[HelsHarvest] {e}");
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static class Player_Update_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                if (__instance == Player.m_localPlayer)
                {
                    SyncOwner(__instance);
                    PublishFlag(__instance);
                    ProcessRaises(__instance);
                }
            }
        }

        // Every player carries the RPC; only the local player's copy acts on it.
        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static class Player_Awake_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                __instance.m_nview?.Register<Vector3, float, int>(RaiseRpc,
                    (sender, position, yaw, level) => OnRaiseRpc(__instance, position, yaw, level));
            }
        }
    }
}
