using EpicLoot.Biomes;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EpicLoot.GatedItemType
{
    /// <summary>
    /// Item Drop Limits CurrentBiomeGradualUnlock. A tier (the biome whose boss an item needs) opens with its boss,
    /// then drops its gear with a chance of the player's kills in that biome over its UnlockKills (biomedata.json).
    /// A tier that misses is passed over, so the pick falls to the same item type a tier lower, which rolls its own
    /// chance. Kills are counted only while this mode is set, so a server using another mode sends no kill RPCs.
    /// </summary>
    public static class GradualUnlock
    {
        public const string SaveKey = EpicLoot.PluginId + "+BiomeKills";
        private const string KillRpc = "el.BiomeKill";
        private static readonly int[] MilestonePercents = { 25, 50, 75, 100 };

        internal enum View
        {
            // Outside a loot roll (the merchant's seeded stock): only tiers at 100%.
            FullyUnlocked,
            // Inside one drop: each tier rolls once, so every candidate item of a tier gets the same answer.
            // Rolling per candidate would almost always let one of a dozen through.
            Rolling,
            // Previews of what could drop: any tier above 0%.
            Possible
        }

        private static View _view = View.FullyUnlocked;
        private static int _exemptDepth;
        private static readonly Dictionary<Heightmap.Biome, bool> _rolledTiers = new Dictionary<Heightmap.Biome, bool>();

        private static Player _loadedFor;
        private static readonly Dictionary<long, Dictionary<string, int>> _killsByWorld = new Dictionary<long, Dictionary<string, int>>();

        // Every drop reads a tier's kill total, so the biome-to-tier map and the totals are cached. BiomeDataManager
        // builds a new BiomesInOrder list on every rebuild, including the legacy-bosses one that fires no event.
        private static IReadOnlyList<BiomeDefinition> _tierSource;
        private static readonly Dictionary<Heightmap.Biome, Heightmap.Biome> _tierOf = new Dictionary<Heightmap.Biome, Heightmap.Biome>();
        private static readonly Dictionary<Heightmap.Biome, int> _tierKills = new Dictionary<Heightmap.Biome, int>();
        private static bool _totalsValid;
        private static Player _totalsFor;
        private static long _totalsWorld;

        public readonly struct Scope : IDisposable
        {
            private readonly bool _exempt;
            private readonly bool _restoreView;
            private readonly View _previous;

            internal Scope(bool exempt, bool restoreView, View previous)
            {
                _exempt = exempt;
                _restoreView = restoreView;
                _previous = previous;
            }

            public void Dispose()
            {
                if (_exempt)
                {
                    _exemptDepth--;
                }

                if (_restoreView)
                {
                    _view = _previous;
                }
            }
        }

        /// <summary>One drop being resolved: each tier's chance is rolled at most once inside it.</summary>
        internal static Scope RollFor(GatedItemTypeMode mode)
        {
            if (mode != GatedItemTypeMode.CurrentBiomeGradualUnlock)
            {
                return default;
            }

            Scope scope = new Scope(false, true, _view);
            _view = View.Rolling;
            _rolledTiers.Clear();
            return scope;
        }

        internal static Scope PossibleFor(GatedItemTypeMode mode)
        {
            if (mode != GatedItemTypeMode.CurrentBiomeGradualUnlock)
            {
                return default;
            }

            Scope scope = new Scope(false, true, _view);
            _view = View.Possible;
            return scope;
        }

        /// <summary>A boss's own drops keep its full tier, so the kill that opens a biome is never a letdown.</summary>
        public static Scope ExemptIfBoss(string characterName)
        {
            if (EpicLoot.GetGatedItemTypeMode() != GatedItemTypeMode.CurrentBiomeGradualUnlock ||
                string.IsNullOrEmpty(characterName) || ZNetScene.instance == null)
            {
                return default;
            }

            GameObject prefab = ZNetScene.instance.GetPrefab(characterName);
            Character character = prefab != null ? prefab.GetComponent<Character>() : null;
            if (character == null || !character.IsBoss())
            {
                return default;
            }

            _exemptDepth++;
            return new Scope(true, false, _view);
        }

        /// <summary>
        /// The tier an item needs is open in the current view. The boss check is separate (CheckGateBossKill); this
        /// is the kill ramp on top of it.
        /// </summary>
        internal static bool IsItemTierOpen(GatedItemDetails details)
        {
            if (details?.RequiredBosses == null)
            {
                return true;
            }

            foreach (string key in details.RequiredBosses)
            {
                Heightmap.Biome tier = BiomeDataManager.GetFirstBiomeForBossKey(key);
                if (tier != Heightmap.Biome.None && !IsTierOpen(tier))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsTierOpen(Heightmap.Biome tier)
        {
            if (_exemptDepth > 0)
            {
                return true;
            }

            if (_view == View.Rolling && _rolledTiers.TryGetValue(tier, out bool rolled))
            {
                return rolled;
            }

            float chance = GetUnlockChance(tier);
            switch (_view)
            {
                case View.Rolling:
                    bool open = chance >= 1f || UnityEngine.Random.value < chance;
                    _rolledTiers[tier] = open;
                    return open;
                case View.Possible:
                    return chance > 0f;
                default:
                    return chance >= 1f;
            }
        }

        /// <summary>
        /// The local player's chance for a tier's gear, from kills alone; whether its boss is dead is not part of it.
        /// </summary>
        public static float GetUnlockChance(Heightmap.Biome tier)
        {
            int required = BiomeDataManager.GetUnlockKills(tier);
            if (required <= 0)
            {
                return 1f;
            }

            return Mathf.Clamp01((float)GetTierKills(tier) / required);
        }

        /// <summary>
        /// The biome whose UnlockKills and gear a biome's kills count toward: the earliest biome sharing one of its
        /// boss keys (Ocean shares the Elder's key with the Black Forest), or the biome itself.
        /// </summary>
        public static Heightmap.Biome GetTier(Heightmap.Biome biome)
        {
            EnsureTiers();
            return _tierOf.TryGetValue(biome, out Heightmap.Biome tier) ? tier : biome;
        }

        private static void EnsureTiers()
        {
            IReadOnlyList<BiomeDefinition> biomes = BiomeDataManager.BiomesInOrder;
            if (ReferenceEquals(biomes, _tierSource))
            {
                return;
            }

            _tierSource = biomes;
            _tierOf.Clear();
            for (int i = 0; i < biomes.Count; i++)
            {
                _tierOf[biomes[i].Biome] = FindTier(biomes[i].Biome);
            }

            _totalsValid = false;
        }

        private static Heightmap.Biome FindTier(Heightmap.Biome biome)
        {
            Heightmap.Biome tier = biome;
            int tierOrder = BiomeDataManager.GetOrder(biome);
            foreach (string key in BiomeDataManager.GetBossKeys(biome))
            {
                Heightmap.Biome first = BiomeDataManager.GetFirstBiomeForBossKey(key);
                int order = BiomeDataManager.GetOrder(first);
                if (first != Heightmap.Biome.None && order < tierOrder)
                {
                    tier = first;
                    tierOrder = order;
                }
            }

            return tier;
        }

        public static int GetTierKills(Heightmap.Biome tier)
        {
            EnsureTotals();
            return _tierKills.TryGetValue(tier, out int total) ? total : 0;
        }

        private static void EnsureTotals()
        {
            EnsureTiers();
            Player player = Player.m_localPlayer;
            long world = ZNet.m_world?.m_uid ?? 0;
            if (_totalsValid && ReferenceEquals(player, _totalsFor) && world == _totalsWorld)
            {
                return;
            }

            _tierKills.Clear();
            Dictionary<string, int> kills = CurrentWorldKills(false);
            _totalsFor = player;
            _totalsWorld = world;
            _totalsValid = true;
            if (kills == null)
            {
                return;
            }

            IReadOnlyList<BiomeDefinition> biomes = BiomeDataManager.BiomesInOrder;
            for (int i = 0; i < biomes.Count; i++)
            {
                if (kills.TryGetValue(biomes[i].Name, out int count))
                {
                    Heightmap.Biome tier = GetTier(biomes[i].Biome);
                    _tierKills.TryGetValue(tier, out int total);
                    _tierKills[tier] = total + count;
                }
            }
        }

        public static int GetKills(Heightmap.Biome biome)
        {
            Dictionary<string, int> kills = CurrentWorldKills(false);
            return kills != null && kills.TryGetValue(BiomeDataManager.GetName(biome), out int count) ? count : 0;
        }

        public static bool SetKills(Heightmap.Biome biome, int count)
        {
            Player player = Player.m_localPlayer;
            Dictionary<string, int> kills = CurrentWorldKills(true);
            if (player == null || kills == null)
            {
                return false;
            }

            string name = BiomeDataManager.GetName(biome);
            if (count > 0)
            {
                kills[name] = count;
            }
            else
            {
                kills.Remove(name);
            }

            _totalsValid = false;
            Save(player);
            return true;
        }

        private static bool IsCounting()
        {
            return EpicLoot.GetGatedItemTypeMode() == GatedItemTypeMode.CurrentBiomeGradualUnlock;
        }

        public static void RegisterRPC(ZRoutedRpc routedRpc)
        {
            routedRpc.Register<int>(KillRpc, (sender, biome) => Credit((Heightmap.Biome)biome));
        }

        private static void CreditAttackers(Character character)
        {
            if (!IsCounting() || character == null || character.IsPlayer() || character.IsTamed() ||
                ZNet.instance == null || WorldGenerator.instance == null)
            {
                return;
            }

            // Vanilla's OnDeath runs on every client from the death animation and returns early everywhere but the
            // owner, which is the one machine whose ZDO still lists the attackers.
            ZNetView nview = character.m_nview;
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            Heightmap.Biome biome = WorldGenerator.instance.GetBiome(character.transform.position);
            if (!BiomeDataManager.IsKnown(biome))
            {
                return;
            }

            ZDO zdo = nview.GetZDO();
            foreach (ZNet.PlayerInfo info in ZNet.instance.GetPlayerList())
            {
                // The flag vanilla sets for every player who hit the creature, read the way its OnDeath reads it.
                if (!zdo.GetBool(ZDOVars.s_attackers + info.m_name))
                {
                    continue;
                }

                // A player on the respawn screen or still loading in has no character, and its UserID of 0 would
                // route the credit to everybody.
                if (info.m_characterID.IsNone())
                {
                    continue;
                }

                if (info.m_characterID == ZNet.instance.LocalPlayerCharacterID && Player.m_localPlayer != null)
                {
                    Credit(biome);
                }
                else
                {
                    ZRoutedRpc.instance?.InvokeRoutedRPC(info.m_characterID.UserID, KillRpc, (int)biome);
                }
            }
        }

        private static void Credit(Heightmap.Biome biome)
        {
            Player player = Player.m_localPlayer;
            if (player == null || !IsCounting() || !BiomeDataManager.IsKnown(biome))
            {
                return;
            }

            Dictionary<string, int> kills = CurrentWorldKills(true);
            Heightmap.Biome tier = GetTier(biome);
            int before = GetTierKills(tier);
            string name = BiomeDataManager.GetName(biome);
            kills.TryGetValue(name, out int count);
            kills[name] = count + 1;
            _totalsValid = false;
            Save(player);
            ShowMilestone(player, tier, before, before + 1);
        }

        private static void ShowMilestone(Player player, Heightmap.Biome tier, int before, int after)
        {
            long required = BiomeDataManager.GetUnlockKills(tier);
            if (required <= 0 || !BiomeDataManager.HasAllBossKeys(tier))
            {
                return;
            }

            int crossed = -1;
            foreach (int percent in MilestonePercents)
            {
                long threshold = percent * required;
                if (before * 100L < threshold && after * 100L >= threshold)
                {
                    crossed = percent;
                }
            }

            if (crossed < 0)
            {
                return;
            }

            string biomeName = Localization.instance.Localize(BiomeDataManager.GetLocalizationToken(tier));
            player.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize(
                "$mod_epicloot_gradualunlock_milestone", biomeName, crossed.ToString(CultureInfo.InvariantCulture)), log: true);
        }

        public static IEnumerable<string> Describe()
        {
            yield return $"Item Drop Limits: {EpicLoot.GetGatedItemTypeMode()}, world {ZNet.m_world?.m_uid ?? 0}";
            foreach (BiomeDefinition definition in BiomeDataManager.BiomesInOrder)
            {
                Heightmap.Biome tier = GetTier(definition.Biome);
                int kills = GetKills(definition.Biome);
                if (tier != definition.Biome)
                {
                    yield return $"{definition.Name}: {kills} kills, counted toward {BiomeDataManager.GetName(tier)}";
                    continue;
                }

                int required = definition.UnlockKills;
                int tierKills = GetTierKills(tier);
                string boss = definition.BossDefeatedKeys.Count == 0 ? "ungated" :
                    BiomeDataManager.HasAllBossKeys(tier) ? "boss defeated" : "boss alive";
                string progress = required <= 0 ? "no kills needed" :
                    $"{tierKills}/{required} kills, {GetUnlockChance(tier):0%}";
                yield return $"{definition.Name}: {progress}, {boss}";
            }
        }

        private static Dictionary<string, int> CurrentWorldKills(bool create)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return null;
            }

            if (_loadedFor != player)
            {
                Load(player);
            }

            long world = ZNet.m_world?.m_uid ?? 0;
            if (!_killsByWorld.TryGetValue(world, out Dictionary<string, int> kills) && create)
            {
                kills = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                _killsByWorld[world] = kills;
            }

            return kills;
        }

        // "worldUid:Meadows=120,BlackForest=40;otherWorldUid:...". Biome names are letters and digits only.
        private static void Load(Player player)
        {
            _killsByWorld.Clear();
            _loadedFor = player;
            if (!player.m_customData.TryGetValue(SaveKey, out string text) || string.IsNullOrEmpty(text))
            {
                return;
            }

            foreach (string worldText in text.Split(';'))
            {
                int colon = worldText.IndexOf(':');
                if (colon <= 0 || !long.TryParse(worldText.Substring(0, colon), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out long world))
                {
                    continue;
                }

                var kills = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (string entry in worldText.Substring(colon + 1).Split(','))
                {
                    int equals = entry.IndexOf('=');
                    if (equals > 0 && int.TryParse(entry.Substring(equals + 1), NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out int count) && count > 0)
                    {
                        kills[entry.Substring(0, equals)] = count;
                    }
                }

                _killsByWorld[world] = kills;
            }
        }

        // Written on every change rather than at Player.Save, so a mod that snapshots m_customData on its own
        // schedule (ValheimEnforcer) never replays an older count.
        private static void Save(Player player)
        {
            var sb = new StringBuilder();
            foreach (KeyValuePair<long, Dictionary<string, int>> world in _killsByWorld.Where(w => w.Value.Count > 0))
            {
                if (sb.Length > 0)
                {
                    sb.Append(';');
                }

                sb.Append(world.Key.ToString(CultureInfo.InvariantCulture)).Append(':');
                sb.Append(string.Join(",", world.Value.Select(e => e.Key + "=" + e.Value.ToString(CultureInfo.InvariantCulture))));
            }

            player.m_customData[SaveKey] = sb.ToString();
        }

        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        private static class Character_OnDeath_Patch
        {
            // A prefix: vanilla's OnDeath ends by destroying the creature, and its ZDO with the attacker flags.
            [HarmonyPrefix]
            private static void Prefix(Character __instance)
            {
                try
                {
                    CreditAttackers(__instance);
                }
                catch (Exception e)
                {
                    EpicLoot.LogError($"[GradualUnlock] {e}");
                }
            }
        }
    }
}
