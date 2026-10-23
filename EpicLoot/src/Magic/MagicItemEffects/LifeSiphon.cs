using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using System;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string LifeSiphon = nameof(LifeSiphon);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // LifeSiphon (4-piece Hel's Court set bonus, NoRoll). A hit from the wearer siphons the target for Duration seconds,
    // and each further hit restarts that. While siphoned, the target cannot be healed by anything -- natural regen,
    // meads, healing status effects, healers, and in PvP food and lifesteal -- and every player whose hit lands on it,
    // wearer or not, heals for the effect value as a percentage of the health that hit took. Siphons from wearers of
    // different rarities keep the higher percentage while they overlap.
    //
    // Everything that decides happens on the target's owner, which may be another client or the dedicated server. A
    // remote player's magic effects are not replicated, so each player's client mirrors its own value into its ZDO
    // (Multiplayer_Player_Patch.UpdateRichesAndLuck), and the owner reads it off the attacker as the hit lands
    // (SharedCharacterRpcDamagePatch, before vanilla applies the damage, so the wearer's first hit heals too). The
    // siphon itself lives in the target's ZDO, not in a status effect: a monster's status effects exist only on its
    // owner and are lost when ownership moves. Vanilla routes every heal to the owner's RPC_Heal, which is refused
    // there; creature regen advances its own clock before healing, so nothing is healed back once the siphon ends.
    // The heal to the attacker goes through vanilla Heal, which reaches the attacker's own client.
    //
    // Only hits with a player attacker heal: summons and tames heal no one, and vanilla gives burning, poison and
    // spirit ticks no attacker at all. The owner cannot tell a weapon strike from damage an effect deals on its own
    // (novas, chain lightning), so those heal too.
    //
    // The aura is a local copy of vanilla's tar drip (vfx_Tared) tinted dark red on every client, announced by an RPC
    // from the owner when a siphon starts and every AuraResendSeconds while it is kept up, for players who came into
    // range since. Each copy then follows the target's ZDO and fades when the siphon ends. Of the drip, only its falling
    // tar drops are kept (KeepOnlyDrops).
    public static class LifeSiphon
    {
        // Tunable in this effect's Config block in magiceffects.json, under this key name.
        public const float DefaultDuration = 30f;
        private const string DurationKey = "Duration";

        private static float Duration => EffectConfig.GetClamped(MagicEffectType.LifeSiphon, DurationKey, DefaultDuration, 1f, 300f);

        // Player-ZDO key holding the local player's Life Siphon value, mirrored by
        // Multiplayer_Player_Patch.UpdateRichesAndLuck on equip/unequip.
        public const string ZdoValueKey = "el-lsiph";

        // On a siphoned target's ZDO, written by its owner: until when (network time, in ticks), the heal percentage,
        // and when the aura was last announced. Network time is shared to within a couple of seconds.
        internal static readonly int UntilHash = "el-lsuntil".GetStableHashCode();
        private static readonly int PercentHash = "el-lspct".GetStableHashCode();
        private static readonly int AuraSentHash = "el-lsfx".GetStableHashCode();
        private const string AuraRpc = "el-lsiphfx";

        private const float AuraResendSeconds = 10f;
        private static readonly long AuraResendTicks = TimeSpan.FromSeconds(AuraResendSeconds).Ticks;

        private const string AuraPrefab = "vfx_Tared";
        private const string DropSystem = "tardrop";    // the one part of the drip the aura keeps
        private static readonly Color AuraTint = new Color(0.4f, 0.02f, 0.03f);
        private static bool _auraMissingLogged;
        private static bool _dropsMissingLogged;

        // Tooltip: "Your hits drain a foe for {1} seconds ... heals for {0}% of the damage dealt."
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.LifeSiphon, value => new object[] { value, Duration });
        }

        internal static long NowTicks => ZNet.instance != null ? ZNet.instance.GetTime().Ticks : 0L;

        // Readable on every client; the owner's copy is the one that counts.
        private static bool TryGetSiphon(Character target, out float percent)
        {
            percent = 0f;
            ZDO zdo = target != null && target.m_nview != null ? target.m_nview.GetZDO() : null;
            if (zdo == null)
            {
                return false;
            }

            long until = zdo.GetLong(UntilHash);
            if (until == 0L || until <= NowTicks)
            {
                return false;
            }

            percent = zdo.GetFloat(PercentHash);
            return percent > 0f;
        }

        // Target's owner, from SharedCharacterRpcDamagePatch's prefix: a hit that will land, not self-inflicted.
        public static void OnHitLanding(Character target, HitData hit, Character attacker)
        {
            if (!(attacker is Player player) || player.m_nview == null || !player.m_nview.IsValid() ||
                target.m_nview == null || !target.m_nview.IsValid())
            {
                return;
            }

            float percent = player.m_nview.GetZDO().GetFloat(ZdoValueKey);
            long now = NowTicks;
            if (percent <= 0f || now == 0L)
            {
                return;
            }

            ZDO zdo = target.m_nview.GetZDO();
            bool active = zdo.GetLong(UntilHash) > now;
            if (active)
            {
                percent = Mathf.Max(percent, zdo.GetFloat(PercentHash));
            }

            long until = now + TimeSpan.FromSeconds(Duration).Ticks;
            zdo.Set(UntilHash, until);
            zdo.Set(PercentHash, percent);

            if (!active || now - zdo.GetLong(AuraSentHash) >= AuraResendTicks)
            {
                zdo.Set(AuraSentHash, now);
                target.m_nview.InvokeRPC(ZRoutedRpc.Everybody, AuraRpc, until);
            }
        }

        // The owner applies the final damage here, after resistances, armor and difficulty scaling. The prefix notes
        // the health before it only for a siphoned target hit by a player, so every other hit costs two ZDO reads.
        [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
        private static class Character_ApplyDamage_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Character __instance, HitData hit, out float __state)
            {
                __state = -1f;
                if (hit == null || __instance.m_nview == null || !__instance.m_nview.IsOwner() ||
                    !TryGetSiphon(__instance, out _) || !(hit.GetAttacker() is Player attacker) || attacker == __instance)
                {
                    return;
                }

                __state = __instance.GetHealth();
            }

            [HarmonyPostfix]
            private static void Postfix(Character __instance, HitData hit, float __state)
            {
                if (__state <= 0f || !TryGetSiphon(__instance, out float percent) ||
                    !(hit.GetAttacker() is Player attacker))
                {
                    return;
                }

                // Health taken, not the hit's total: an overkill hit only drains what was left.
                float dealt = __state - Mathf.Max(__instance.GetHealth(), 0f);
                if (dealt > 0f)
                {
                    attacker.Heal(dealt * percent / 100f);
                }
            }
        }

        // Every heal ends here, on the character's owner, whoever asked for it (vanilla Heal routes remote calls here).
        [HarmonyPatch(typeof(Character), "RPC_Heal")]
        private static class Character_RPC_Heal_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Character __instance)
            {
                return __instance.m_nview == null || !__instance.m_nview.IsOwner() || !TryGetSiphon(__instance, out _);
            }
        }

        // Every character carries the aura RPC, so the owner's broadcast reaches every client that has it loaded.
        [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
        private static class Character_Awake_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Character __instance)
            {
                __instance.m_nview?.Register<long>(AuraRpc, (sender, until) => OnAuraRpc(__instance, until));
            }
        }

        private static void OnAuraRpc(Character target, long until)
        {
            if (target == null || target.IsDead() || (ZNet.instance != null && ZNet.instance.IsDedicated()))
            {
                return;
            }

            if (LifeSiphonAura.TryGet(target, out LifeSiphonAura existing))
            {
                existing.Extend(until);
                return;
            }

            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(AuraPrefab) : null;
            if (prefab == null)
            {
                if (!_auraMissingLogged)
                {
                    _auraMissingLogged = true;
                    EpicLoot.LogWarning($"[LifeSiphon] Missing prefab {AuraPrefab}; siphoned targets will show no aura.");
                }
                return;
            }

            // Placed, parented and scaled the way vanilla attaches a status effect's visuals to a character.
            GameObject fx = LocalFx.Spawn(prefab, target.GetCenterPoint(), target.transform.rotation,
                target.GetRadius() * 2f, target.transform, lifetime: 0f);
            if (fx == null)
            {
                return;
            }

            KeepOnlyDrops(fx);
            foreach (ParticleSystem particles in fx.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particles.main;
                Color tint = AuraTint;
                tint.a = main.startColor.colorMax.a;
                main.startColor = tint;
            }

            fx.AddComponent<LifeSiphonAura>().Begin(target, until);
        }

        // Only the tar drops are kept, to keep the screen quiet in a fight: the hit spray, the cloud, the splashes and
        // the flow (with the splats it stamps on the ground) all go, at once so the tint loop never sees them. The
        // root's own system emits nothing and holds the rest, so it stays. Should an update rename the drops, nothing
        // but the splats is taken, rather than leaving no aura at all.
        private static void KeepOnlyDrops(GameObject fx)
        {
            Transform drops = fx.transform.Find(DropSystem);
            if (drops == null || drops.GetComponent<ParticleSystem>() == null)
            {
                if (!_dropsMissingLogged)
                {
                    _dropsMissingLogged = true;
                    EpicLoot.LogWarning($"[LifeSiphon] {AuraPrefab} has no '{DropSystem}'; siphoned targets show its full drip.");
                }
                RemoveSplats(fx);
                return;
            }

            for (int i = fx.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = fx.transform.GetChild(i);
                if (child != drops && child.GetComponentInChildren<ParticleSystem>(true) != null)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        // vfx_Tared's drops stamp a flat splat wherever they land: a ParticleDecal on the dripping system emits into a
        // world-aligned decal system. Both go, the splat system at once so the tint loop never sees it.
        private static void RemoveSplats(GameObject fx)
        {
            foreach (ParticleDecal decal in fx.GetComponentsInChildren<ParticleDecal>(true))
            {
                if (decal.m_decalSystem != null && decal.m_decalSystem.transform.IsChildOf(fx.transform))
                {
                    UnityEngine.Object.DestroyImmediate(decal.m_decalSystem.gameObject);
                }
                UnityEngine.Object.DestroyImmediate(decal);
            }
        }
    }

    // One client's aura on a siphoned character. It holds the later of what the owner announced and what the target's
    // ZDO says now, so a refresh that came without an announcement still keeps it up.
    public class LifeSiphonAura : MonoBehaviour
    {
        private const float CheckInterval = 0.5f;
        private const float FadeTime = 2f;

        // Each siphoned character's aura on this client, so a refresh finds it without searching the character.
        private static readonly System.Collections.Generic.Dictionary<Character, LifeSiphonAura> Auras =
            new System.Collections.Generic.Dictionary<Character, LifeSiphonAura>();

        private Character _target;
        private long _until;
        private float _destroyAt = -1f;

        internal static bool TryGet(Character target, out LifeSiphonAura aura)
        {
            return Auras.TryGetValue(target, out aura) && aura != null;
        }

        public void Begin(Character target, long until)
        {
            _target = target;
            _until = until;
            Auras[target] = this;
            InvokeRepeating(nameof(Check), CheckInterval, CheckInterval);
        }

        private void OnDestroy()
        {
            if (_target != null && Auras.TryGetValue(_target, out LifeSiphonAura aura) && aura == this)
            {
                Auras.Remove(_target);
            }
            else
            {
                // The character went first: drop whatever no longer has one.
                foreach (var pair in new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<Character, LifeSiphonAura>>(Auras))
                {
                    if (pair.Key == null || pair.Value == this)
                    {
                        Auras.Remove(pair.Key);
                    }
                }
            }
        }

        public void Extend(long until)
        {
            _until = Math.Max(_until, until);
            if (_destroyAt >= 0f)
            {
                _destroyAt = -1f;
                foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>())
                {
                    particles.Play();
                }
            }
        }

        private void Check()
        {
            if (_target == null || _target.IsDead())
            {
                Destroy(gameObject);
                return;
            }

            if (_destroyAt >= 0f)
            {
                if (Time.time >= _destroyAt)
                {
                    Destroy(gameObject);
                }
                return;
            }

            ZDO zdo = _target.m_nview != null ? _target.m_nview.GetZDO() : null;
            if (zdo != null)
            {
                _until = Math.Max(_until, zdo.GetLong(LifeSiphon.UntilHash));
            }

            if (_until > LifeSiphon.NowTicks)
            {
                return;
            }

            foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>())
            {
                particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
            _destroyAt = Time.time + FadeTime;
        }
    }
}
