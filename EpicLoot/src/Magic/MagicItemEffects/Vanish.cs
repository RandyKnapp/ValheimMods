using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string Vanish = nameof(Vanish);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // Vanish (3-piece Huldufólk set bonus, NoRoll). A dodge roll with an alerted enemy within Radius, off cooldown,
    // drops a cloud of smoke: enemies within Radius lose track of the wearer, who stays unseen and unheard for the
    // effect value in seconds. The wearer's next hit from hiding, with any weapon, is a sneak attack. Cooldown seconds
    // must pass before the next, counted from the vanish.
    //
    // The cooldown is the EL_VanishCooldown status effect: Vanish is ready exactly when that is gone, so whatever ends it
    // (its timer, death, a cleanse) readies Vanish too, and the HUD never disagrees. Only when its icon is missing, and so
    // there is no status effect, does a plain timer stand in.
    //
    // Hiding uses vanilla's own senses. AI on every machine reads a player's stealth and noise from the player's ZDO,
    // so the wearer's client holds both at 0 (Player.UpdateStealth and Character.RPC_AddNoise are held off) and no
    // creature can see or hear them, wherever it is simulated. Creatures already chasing need telling: one RPC to every
    // client has each one that owns a nearby monster hunting the wearer drop its target and stand down. Raiders and
    // bosses keep their alert (a boss's alert counts towards the active-boss key), but lose the target all the same.
    //
    // The sneak attack is applied on the attacker's side, from SharedCharacterDamagePatch's prefix, using the hit's
    // own backstab bonus (which already carries ModifyBackstab, for melee and projectiles alike). The hit's bonus is
    // then set to 1, so vanilla's backstab on the victim's owner (which has a 300 s cooldown per creature and needs the
    // creature un-alerted by the time the hit arrives) and Opportunist cannot apply it a second time.
    public static class Vanish
    {
        // Both tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultCooldown = 20f;
        public const float DefaultRadius = 15f;

        private const string CooldownKey = "Cooldown";
        private const string RadiusKey = "Radius";

        // Player-editable; a zero cooldown would give the cooldown status effect a zero ttl, which vanilla treats as
        // "never expires".
        private static float Cooldown => EffectConfig.GetClamped(MagicEffectType.Vanish, CooldownKey, DefaultCooldown, 1f, 600f);
        private static float Radius => EffectConfig.GetClamped(MagicEffectType.Vanish, RadiusKey, DefaultRadius, 1f, 50f);

        private const string VanishRpc = "el-vanish";

        // Networked vanilla fx, spawned once by the wearer so everyone sees the smoke: Odin's vanishing cloud and the
        // Jötunn witch's dodge.
        private static readonly string[] SmokeFxPrefabs = { "vfx_odin_despawn", "sfx_jotunwitch_dodge" };

        private const string HiddenName = "EL_Vanished";
        private const string CooldownName = "EL_VanishCooldown";
        private static readonly int HiddenHash = HiddenName.GetStableHashCode();
        private static readonly int CooldownHash = CooldownName.GetStableHashCode();
        private const string IconStatusEffect = "SetEffect_TrollArmor";
        private const string FallbackIconItem = "HelmetTrollLeather";

        private static StatusEffect _hiddenPrototype;
        private static StatusEffect _cooldownPrototype;
        private static bool _iconMissingLogged;
        private static readonly HashSet<string> MissingFxLogged = new HashSet<string>();

        // State for the local player only; SyncOwner drops it when the local player changes.
        private static Player _owner;
        private static float _readyAt;             // only for when the cooldown status effect could not be made
        private static float _hiddenUntil;
        private static bool _sneakReady;
        private static bool _restoreStealth;

        // Tooltip: "... enemies within {1}m lose sight of you for {0} seconds ... Cooldown: {2} seconds."
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.Vanish, value => new object[] { value, Radius, Cooldown });
        }

        public static bool IsHidden => _owner != null && _owner == Player.m_localPlayer && Time.time < _hiddenUntil;

        private static void SyncOwner(Player player)
        {
            if (_owner == player)
            {
                return;
            }

            _owner = player;
            _readyAt = 0f;
            _hiddenUntil = 0f;
            _sneakReady = false;
            _restoreStealth = false;
        }

        private static void OnDodge(Player player)
        {
            SyncOwner(player);
            float now = Time.time;
            if (IsCoolingDown(player, now) || !player.HasActiveMagicEffect(MagicEffectType.Vanish, out float seconds) || seconds <= 0f)
            {
                return;
            }

            float radius = Radius;
            if (!AlertedEnemyNear(player, radius))
            {
                return;
            }

            StartHiding(player, seconds, radius);
        }

        private static bool IsCoolingDown(Player player, float now)
        {
            return GetOrCreateCooldown() != null ? player.GetSEMan().HaveStatusEffect(CooldownHash) : now < _readyAt;
        }

        // Alerted is synced to every client through the creature's ZDO; whom it is hunting is not, so an alerted enemy
        // close by is the test.
        private static bool AlertedEnemyNear(Player player, float radius)
        {
            Vector3 position = player.transform.position;
            float radiusSqr = radius * radius;
            foreach (BaseAI ai in BaseAI.BaseAIInstances)
            {
                Character character = ai != null ? ai.m_character : null;
                if (character == null || character.IsDead() || !ai.IsAlerted() || !BaseAI.IsEnemy(player, character))
                {
                    continue;
                }

                if ((character.transform.position - position).sqrMagnitude <= radiusSqr)
                {
                    return true;
                }
            }
            return false;
        }

        private static void StartHiding(Player player, float seconds, float radius)
        {
            float now = Time.time;
            _hiddenUntil = now + seconds;
            _sneakReady = true;
            _restoreStealth = true;
            float cooldown = Cooldown;
            _readyAt = now + cooldown;

            ZDO zdo = player.m_nview != null ? player.m_nview.GetZDO() : null;
            player.m_stealthFactor = 0f;
            player.m_stealthFactorTarget = 0f;
            player.m_noiseRange = 0f;
            if (zdo != null)
            {
                zdo.Set(ZDOVars.s_stealth, 0f);
                zdo.Set(ZDOVars.s_noise, 0f);
            }

            Vector3 position = player.transform.position;
            foreach (string name in SmokeFxPrefabs)
            {
                var prefab = FindFxPrefab(name);
                if (prefab != null)
                {
                    Object.Instantiate(prefab, position, player.transform.rotation);
                }
            }

            if (player.m_nview != null && player.m_nview.IsValid())
            {
                player.m_nview.InvokeRPC(ZNetView.Everybody, VanishRpc, position, radius);
            }

            SEMan seMan = player.GetSEMan();
            StatusEffect hidden = GetOrCreateHidden();
            if (hidden != null)
            {
                hidden.m_ttl = seconds;
                seMan.AddStatusEffect(hidden, resetTime: true);
            }
            StatusEffect cooling = GetOrCreateCooldown();
            if (cooling != null)
            {
                cooling.m_ttl = cooldown;
                seMan.AddStatusEffect(cooling, resetTime: true);
            }

            EpicLoot.Log($"[Vanish] Hidden for {seconds:0.#} s within {radius:0.#} m; ready again in {cooldown:0.#} s.");
        }

        private static void EndHiding(Player player)
        {
            _hiddenUntil = 0f;
            _sneakReady = false;
            player.GetSEMan().RemoveStatusEffect(HiddenHash, true);
        }

        // Every client, the wearer's included: monsters this client simulates that are hunting the wearer, within the
        // radius, lose their target.
        private static void OnVanishRpc(Player vanished, Vector3 position, float radius)
        {
            float radiusSqr = radius * radius;
            foreach (BaseAI ai in BaseAI.BaseAIInstances)
            {
                if (!(ai is MonsterAI monster) || monster.m_targetCreature != vanished || ai.m_nview == null ||
                    !ai.m_nview.IsValid() || !ai.m_nview.IsOwner() ||
                    (ai.transform.position - position).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                monster.m_targetCreature = null;
                Character character = ai.m_character;
                if (!monster.HuntPlayer() && (character == null || !character.IsBoss()))
                {
                    monster.SetAlerted(false);
                }
            }
        }

        // Called by SharedCharacterDamagePatch's prefix for weapon strikes, before Opportunist.
        public static void ModifyOutgoingHit(Character target, HitData hit, Character attacker)
        {
            if (!_sneakReady || attacker == null || attacker != Player.m_localPlayer || target == null)
            {
                return;
            }

            var player = (Player)attacker;
            if (!IsHidden)
            {
                _sneakReady = false;
                return;
            }

            EndHiding(player);
            if (target.GetBaseAI() == null)
            {
                return;
            }

            if (hit.m_backstabBonus > 1f)
            {
                hit.ApplyModifier(hit.m_backstabBonus);
                target.m_backstabHitEffects.Create(hit.m_point, Quaternion.identity, target.transform, 1f, -1,
                    attacker.GetZDOID());
                EpicLoot.Log($"[Vanish] Sneak attack on {target.m_name}: x{hit.m_backstabBonus:0.##}.");
            }
            hit.m_backstabBonus = 1f;
        }

        private static GameObject FindFxPrefab(string name)
        {
            var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            if (prefab == null && MissingFxLogged.Add(name))
            {
                EpicLoot.LogWarning($"[Vanish] could not find the '{name}' prefab; playing without it.");
            }
            return prefab;
        }

        private static StatusEffect GetOrCreateHidden()
        {
            if (_hiddenPrototype == null)
            {
                _hiddenPrototype = CreateStatusEffect(HiddenName, "$mod_epicloot_se_vanish", cooldown: false);
            }
            return _hiddenPrototype;
        }

        private static StatusEffect GetOrCreateCooldown()
        {
            if (_cooldownPrototype == null)
            {
                _cooldownPrototype = CreateStatusEffect(CooldownName, "$mod_epicloot_se_vanish_cooldown", cooldown: true);
            }
            return _cooldownPrototype;
        }

        // A null icon would render as an invisible HUD entry, so a missing icon logs once and shows nothing; hiding
        // and the cooldown still work without it. The ttl is stamped on the prototype before each add.
        private static StatusEffect CreateStatusEffect(string name, string token, bool cooldown)
        {
            ObjectDB objectDB = ObjectDB.instance;
            Sprite icon = objectDB?.GetStatusEffect(IconStatusEffect.GetStableHashCode())?.m_icon ??
                objectDB?.GetItemPrefab(FallbackIconItem)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null)
            {
                if (!_iconMissingLogged)
                {
                    _iconMissingLogged = true;
                    EpicLoot.LogWarning($"[Vanish] could not find the '{IconStatusEffect}' or '{FallbackIconItem}' icon; Vanish shows no HUD entry.");
                }
                return null;
            }

            StatusEffect se = ScriptableObject.CreateInstance<StatusEffect>();
            se.name = name;
            se.m_name = token;
            se.m_icon = icon;
            se.m_ttl = 1f;
            se.m_cooldownIcon = cooldown;
            return se;
        }

        // The roll commits inside UpdateDodge, which zeroes the queued-dodge timer when it starts one; Player.Dodge only
        // queues it.
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateDodge))]
        private static class Player_UpdateDodge_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance, out float __state)
            {
                __state = __instance == Player.m_localPlayer ? __instance.m_queuedDodgeTimer : 0f;
            }

            [HarmonyPostfix]
            private static void Postfix(Player __instance, float dt, float __state)
            {
                if (__state - dt > 0f && __instance.m_queuedDodgeTimer == 0f && __instance == Player.m_localPlayer)
                {
                    OnDodge(__instance);
                }
            }
        }

        // Holds stealth at 0 while hidden, writing the ZDO only when it differs. When hiding ends, a player who is not
        // crouching is seen again at once rather than over vanilla's four-second ramp.
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateStealth))]
        private static class Player_UpdateStealth_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Player __instance)
            {
                if (!ReferenceEquals(__instance, _owner) || __instance != Player.m_localPlayer)
                {
                    return true;
                }

                ZDO zdo = __instance.m_nview != null ? __instance.m_nview.GetZDO() : null;
                if (IsHidden)
                {
                    if (__instance.m_stealthFactor != 0f)
                    {
                        __instance.m_stealthFactor = 0f;
                        zdo?.Set(ZDOVars.s_stealth, 0f);
                    }
                    return false;
                }

                if (_restoreStealth)
                {
                    _restoreStealth = false;
                    _sneakReady = false;
                    if (!__instance.IsCrouching())
                    {
                        __instance.m_stealthFactor = 1f;
                        __instance.m_stealthFactorTarget = 1f;
                        zdo?.Set(ZDOVars.s_stealth, 1f);
                    }
                }
                return true;
            }
        }

        // No noise while hidden: the dodge, footsteps and swings would otherwise call every creature nearby.
        [HarmonyPatch(typeof(Character), nameof(Character.RPC_AddNoise))]
        private static class Character_RPC_AddNoise_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Character __instance)
            {
                return !(IsHidden && ReferenceEquals(__instance, _owner));
            }
        }

        // Every player carries the RPC; every client runs it for the monsters it owns.
        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static class Player_Awake_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                __instance.m_nview?.Register<Vector3, float>(VanishRpc,
                    (sender, position, radius) => OnVanishRpc(__instance, position, radius));
            }
        }
    }
}
