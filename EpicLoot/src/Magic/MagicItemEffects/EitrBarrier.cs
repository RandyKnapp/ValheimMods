using System.Collections.Generic;
using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string EitrBarrier = nameof(EitrBarrier);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // EitrBarrier (4-piece Mimir's Well set bonus, NoRoll). While the barrier is up, the effect value in percent of every
    // hit is taken from eitr instead, at DamagePerEitr damage per point of eitr, and the rest goes on to the block,
    // resistances, armor and health as usual. When eitr runs dry, from a hit or from casting, the barrier collapses for
    // Cooldown seconds and comes back once there is MinEitr again. Not the Cyan shard's EitrShield, which spends eitr
    // on what is left of a hit after armor and has no barrier.
    //
    // The barrier being up IS the EL_EitrBarrier status effect on the local player, and being broken is the
    // EL_EitrBarrierBroken cooldown, so no static state can go stale on death or a world change. A Player.Update
    // postfix raises, breaks and drops it. The absorb is the status effect's own OnDamaged, as vanilla's Staff of
    // Protection bubble absorbs: RPC_Damage calls it on the victim's owner once the hit is known to land and the
    // world's difficulty scaling is in, before the block, resistances and armor, so the barrier takes its share of the
    // raw hit. The eitr is taken from m_eitr directly, as Frostwalker does, because UseEitr's EpicLoot patches would
    // treat every hit as a spell cast (HealthOnEitrUse would heal on it, EitrUseGivesAdrenaline would pay out).
    //
    // The purple bubble is the status effect's start effect, like vanilla's Staff of Protection: a recoloured copy of
    // vfx_StaffShield. The copy keeps its ZNetView and ZSyncTransform, so other players see it follow the wearer, and
    // vanilla removes it with the status effect. The break fx is a networked copy too. All three copies are registered
    // in ZNetScene on every client each world load, so a ZDO for one never arrives unrecognised. The hit fx, up to
    // five a second under damage over time, is an RPC instead: each client plays a local copy, and no hit creates a
    // ZDO.
    public static class EitrBarrier
    {
        // All tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultCooldown = 5f;    // seconds the barrier stays down after collapsing
        public const float DefaultMinEitr = 5f;     // eitr needed to raise it again, so it does not flicker during regen
        public const float DefaultDamagePerEitr = 1f;

        private const string CooldownKey = "Cooldown";
        private const string MinEitrKey = "MinEitr";
        private const string DamagePerEitrKey = "DamagePerEitr";

        private const float EmptyEitr = 0.5f;       // less than this counts as run dry
        private const float HitFxInterval = 0.2f;   // damage over time and flurries would otherwise spawn fx every tick
        private const float BarrierHue = 0.76f;     // purple

        private const string BubbleSource = "vfx_StaffShield";
        private const string HitFxSource = "fx_StaffShield_Hit";
        private const string BreakFxSource = "fx_StaffShield_Break";
        public const string BubblePrefab = "vfx_EL_EitrBarrier";
        public const string HitFxPrefab = "fx_EL_EitrBarrier_Hit";
        public const string BreakFxPrefab = "fx_EL_EitrBarrier_Break";

        private const string IconItem = "Eitr";
        private const string FallbackIconStatusEffect = "Staff_shield";

        private const string BarrierName = "EL_EitrBarrier";
        private static readonly int BarrierHash = BarrierName.GetStableHashCode();
        private const string BrokenName = "EL_EitrBarrierBroken";
        private static readonly int BrokenHash = BrokenName.GetStableHashCode();

        private static GameObject _container;       // disabled parent that keeps the templates from Awaking
        private static GameObject _bubble;
        private static GameObject _hitFx;
        private static GameObject _breakFx;
        private static SE_EitrBarrier _barrier;
        private static StatusEffect _broken;
        private static bool _iconMissingLogged;
        private static readonly HashSet<string> MissingLogged = new HashSet<string>();
        private static float _nextHitFx;
        private const string HitFxRpc = "el-ebhit";

        // The HUD text, rebuilt only when the number changes (the HUD asks every frame).
        private static int _iconTextValue = -1;
        private static string _iconText = "";

        // Player-editable, so each is kept in a sane range. A zero cooldown would give the broken status effect a zero
        // ttl, which vanilla treats as "never expires".
        private static float Cooldown => EffectConfig.GetClamped(MagicEffectType.EitrBarrier,
            CooldownKey, DefaultCooldown, 0.1f, 600f);
        private static float MinEitr => EffectConfig.GetClamped(MagicEffectType.EitrBarrier,
            MinEitrKey, DefaultMinEitr, EmptyEitr, 1000f);
        private static float DamagePerEitr => EffectConfig.GetClamped(MagicEffectType.EitrBarrier,
            DamagePerEitrKey, DefaultDamagePerEitr, 0.1f, 100f);

        // The world's eitr cost modifier applies to the barrier as it does to spells.
        private static float EitrRate => Game.m_eitrRate > 0f ? Game.m_eitrRate : 1f;

        // Tooltip: "... takes {0}% of every hit from your Eitr ..., {2} damage per Eitr. It collapses for {1}s ..."
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.EitrBarrier, value => new object[] { value, Cooldown, DamagePerEitr });
        }

        // The damage the barrier can still take at the local player's current eitr: the HUD icon's text.
        public static string GetIconText()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return "";
            }

            if (player.GetTotalActiveMagicEffectValue(MagicEffectType.EitrBarrier) <= 0f)
            {
                return "";
            }

            int value = Mathf.FloorToInt(player.m_eitr * DamagePerEitr / EitrRate);
            if (value != _iconTextValue)
            {
                _iconTextValue = value;
                _iconText = value.ToString();
            }
            return _iconText;
        }

        // SE_EitrBarrier.OnDamaged, on the local player while the barrier is up. Vanilla is walking the status effect list
        // at this point, so a hit that drains the last eitr leaves the collapse to the next Tick, a frame later.
        internal static void Absorb(Player player, HitData hit, Character attacker)
        {
            if (hit == null || player == null || player != Player.m_localPlayer ||
                SharedCharacterRpcDamagePatch.IsSelfInflicted(player, hit, attacker))
            {
                return;
            }

            float percent = Mathf.Clamp(player.GetTotalActiveMagicEffectValue(MagicEffectType.EitrBarrier), 0f, 100f);
            if (percent <= 0f)
            {
                return;
            }

            float total = hit.m_damage.EpicLootGetTotalDamageAgainstPlayer();
            if (total <= 0f)
            {
                return;
            }

            float perEitr = DamagePerEitr;
            float rate = EitrRate;
            float absorbed = Mathf.Min(total * percent / 100f, player.m_eitr * perEitr / rate);
            if (absorbed <= 0f)
            {
                return;
            }

            player.m_eitr = Mathf.Max(0f, player.m_eitr - absorbed * rate / perEitr);
            // Holds natural regen off the way a spell cast does (RPC_UseEitr restarts the same timer).
            player.m_eitrRegenTimer = Mathf.Max(player.m_eitrRegenTimer, player.m_eitrRegenDelay);

            // Every damage type shrinks alike, fire and poison included (vanilla splits them into damage over time
            // later). At 100% a fully absorbed hit shows no damage number and adds no stagger: ApplyDamage skips it.
            hit.m_damage.Modify(1f - absorbed / total);
            PlayHitFx(player, hit);
        }

        private static void Tick(Player player)
        {
            SEMan seMan = player.GetSEMan();
            bool up = seMan.HaveStatusEffect(BarrierHash);

            if (player.IsDead() || !player.HasActiveMagicEffect(MagicEffectType.EitrBarrier, out float _))
            {
                if (up)
                {
                    seMan.RemoveStatusEffect(BarrierHash, true);
                }
                return;
            }

            if (up)
            {
                // Run dry by anything, a spell included.
                if (player.m_eitr < EmptyEitr)
                {
                    Break(player);
                }
                return;
            }

            if (seMan.HaveStatusEffect(BrokenHash) || player.m_eitr < MinEitr)
            {
                return;
            }

            StatusEffect barrier = GetOrCreateBarrier();
            if (barrier != null)
            {
                seMan.AddStatusEffect(barrier);
            }
        }

        // Removing the status effect takes the bubble with it. The cooldown's ttl is stamped on the shared prototype
        // before adding, so a retuned Cooldown takes hold on the next collapse.
        private static void Break(Player player)
        {
            SEMan seMan = player.GetSEMan();
            seMan.RemoveStatusEffect(BarrierHash, true);

            StatusEffect broken = GetOrCreateBroken();
            if (broken != null)
            {
                broken.m_ttl = Cooldown;
                seMan.AddStatusEffect(broken, true);
            }

            if (_breakFx != null)
            {
                Object.Instantiate(_breakFx, player.GetCenterPoint(), player.transform.rotation);
            }
        }

        private static void PlayHitFx(Player player, HitData hit)
        {
            if (_hitFx == null || Time.time < _nextHitFx)
            {
                return;
            }
            _nextHitFx = Time.time + HitFxInterval;

            // Damage over time carries no point or direction.
            Vector3 point = hit.m_point != Vector3.zero ? hit.m_point : player.GetCenterPoint();
            Vector3 facing = hit.m_dir.sqrMagnitude > 0.001f ? -hit.m_dir : Vector3.zero;
            if (player.m_nview != null && player.m_nview.IsValid())
            {
                player.m_nview.InvokeRPC(ZNetView.Everybody, HitFxRpc, point, facing);
            }
        }

        private static void OnHitFxRpc(Vector3 point, Vector3 facing)
        {
            if (_hitFx == null || (ZNet.instance != null && ZNet.instance.IsDedicated()))
            {
                return;
            }

            Quaternion rotation = facing.sqrMagnitude > 0.001f ? Quaternion.LookRotation(facing) : Quaternion.identity;
            LocalFx.Spawn(_hitFx, point, rotation);
        }

        // World load (SetEffectWarmup), after RegisterPrefabs: the local copy the hit fx plays from.
        internal static void WarmupAtLoad(bool drawn)
        {
            if (drawn)
            {
                LocalFx.Prewarm(_hitFx, scaled: false);
            }
        }

        // The same flags as vanilla's Staff_shield start effect: attached to the player, scaled to its radius.
        private static StatusEffect GetOrCreateBarrier()
        {
            if (_barrier != null)
            {
                return _barrier;
            }

            Sprite icon = GetIcon();
            if (icon == null)
            {
                return null;
            }

            SE_EitrBarrier se = ScriptableObject.CreateInstance<SE_EitrBarrier>();
            se.name = BarrierName;
            se.m_name = "$mod_epicloot_se_eitrbarrier";
            se.m_icon = icon;
            se.m_ttl = 0f;      // never expires on its own; Tick and Break remove it
            if (_bubble != null)
            {
                se.m_startEffects = new EffectList
                {
                    m_effectPrefabs = new[]
                    {
                        new EffectList.EffectData { m_prefab = _bubble, m_enabled = true, m_attach = true, m_scale = true }
                    }
                };
            }
            _barrier = se;
            return _barrier;
        }

        private static StatusEffect GetOrCreateBroken()
        {
            if (_broken != null)
            {
                return _broken;
            }

            Sprite icon = GetIcon();
            if (icon == null)
            {
                return null;
            }

            StatusEffect se = ScriptableObject.CreateInstance<StatusEffect>();
            se.name = BrokenName;
            se.m_name = "$mod_epicloot_se_eitrbarrier_broken";
            se.m_icon = icon;
            se.m_ttl = Cooldown;    // restamped on every collapse by Break
            se.m_cooldownIcon = true;
            _broken = se;
            return _broken;
        }

        // A null icon would render as an invisible HUD entry, so with neither icon found the barrier stays down. Not
        // looked for again once it was missing: Tick asks every frame while the barrier could rise.
        private static Sprite GetIcon()
        {
            ObjectDB objectDB = ObjectDB.instance;
            if (objectDB == null || _iconMissingLogged)
            {
                return null;
            }

            Sprite icon = objectDB.GetItemPrefab(IconItem)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null)
            {
                icon = objectDB.GetStatusEffect(FallbackIconStatusEffect.GetStableHashCode())?.m_icon;
            }

            if (icon == null && !_iconMissingLogged)
            {
                _iconMissingLogged = true;
                EpicLoot.LogWarning($"[EitrBarrier] could not find the '{IconItem}' or '{FallbackIconStatusEffect}' icon; the barrier will not rise.");
            }
            return icon;
        }

        // Hooked to PrefabManager.OnPrefabsRegistered (a ZNetScene.Awake postfix on every client and the server, each
        // world load). The templates are built once and kept across worlds; later loads only re-inject them.
        public static void RegisterPrefabs()
        {
            ZNetScene zns = ZNetScene.instance;
            if (zns == null)
            {
                return;
            }

            if (_bubble == null)
            {
                _bubble = BuildTemplate(zns, BubbleSource, BubblePrefab);
                // A barrier prototype built before the bubble existed would never show it.
                _barrier = null;
            }
            if (_hitFx == null)
            {
                _hitFx = BuildTemplate(zns, HitFxSource, HitFxPrefab);
            }
            if (_breakFx == null)
            {
                _breakFx = BuildTemplate(zns, BreakFxSource, BreakFxPrefab);
            }

            Register(zns, _bubble, BubblePrefab);
            Register(zns, _hitFx, HitFxPrefab);
            Register(zns, _breakFx, BreakFxPrefab);
        }

        private static void Register(ZNetScene zns, GameObject template, string name)
        {
            if (template == null)
            {
                return;
            }

            if (!zns.m_prefabs.Contains(template))
            {
                zns.m_prefabs.Add(template);
            }
            zns.m_namedPrefabs[name.GetStableHashCode()] = template;
        }

        private static GameObject BuildTemplate(ZNetScene zns, string sourceName, string name)
        {
            GameObject source = zns.GetPrefab(sourceName);
            if (source == null)
            {
                if (MissingLogged.Add(sourceName))
                {
                    EpicLoot.LogWarning($"[EitrBarrier] could not find the '{sourceName}' prefab; that part of the barrier will not show.");
                }
                return null;
            }

            if (_container == null)
            {
                _container = new GameObject("EL_EitrBarrierContainer");
                _container.SetActive(false);
                Object.DontDestroyOnLoad(_container);
            }

            // Under the disabled container nothing on the copy Awakes, while activeSelf stays true: remote clients
            // build it from this template and never call SetActive on it.
            GameObject template = Object.Instantiate(source, _container.transform);
            template.name = name;
            Recolor(template);
            return template;
        }

        // Turns the vanilla red to purple: material tints, lights and particle colours, each keeping its own
        // saturation, brightness (HDR included) and alpha. Copies of the materials, so the Staff of Protection keeps
        // its red; a material several renderers share is copied once.
        private static void Recolor(GameObject root)
        {
            var copies = new Dictionary<Material, Material>();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material original = materials[i];
                    if (original == null)
                    {
                        continue;
                    }

                    if (!copies.TryGetValue(original, out Material copy))
                    {
                        copy = new Material(original) { name = original.name + "_eitrbarrier" };
                        RecolorProperty(copy, "_Color");
                        RecolorProperty(copy, "_TintColor");
                        RecolorProperty(copy, "_EmissionColor");
                        copies[original] = copy;
                    }
                    materials[i] = copy;
                }
                renderer.sharedMaterials = materials;
            }

            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                light.color = Shift(light.color);
            }

            foreach (ParticleSystem particles in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particles.main;
                main.startColor = Shift(main.startColor);

                ParticleSystem.ColorOverLifetimeModule overLifetime = particles.colorOverLifetime;
                if (overLifetime.enabled)
                {
                    overLifetime.color = Shift(overLifetime.color);
                }
            }
        }

        private static void RecolorProperty(Material material, string property)
        {
            if (material.HasProperty(property))
            {
                material.SetColor(property, Shift(material.GetColor(property)));
            }
        }

        // Greys and whites (smoke, sparks) are left as they are.
        private static Color Shift(Color color)
        {
            Color.RGBToHSV(color, out float _, out float saturation, out float value);
            if (saturation < 0.05f)
            {
                return color;
            }

            Color shifted = Color.HSVToRGB(BarrierHue, saturation, value, true);
            shifted.a = color.a;
            return shifted;
        }

        private static Gradient Shift(Gradient gradient)
        {
            if (gradient == null)
            {
                return null;
            }

            GradientColorKey[] keys = gradient.colorKeys;
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].color = Shift(keys[i].color);
            }

            var shifted = new Gradient { mode = gradient.mode };
            shifted.SetKeys(keys, gradient.alphaKeys);
            return shifted;
        }

        private static ParticleSystem.MinMaxGradient Shift(ParticleSystem.MinMaxGradient gradient)
        {
            switch (gradient.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(Shift(gradient.color));
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(Shift(gradient.colorMin), Shift(gradient.colorMax));
                case ParticleSystemGradientMode.Gradient:
                    return new ParticleSystem.MinMaxGradient(Shift(gradient.gradient));
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(Shift(gradient.gradientMin), Shift(gradient.gradientMax));
                case ParticleSystemGradientMode.RandomColor:
                    return new ParticleSystem.MinMaxGradient(Shift(gradient.gradient)) { mode = ParticleSystemGradientMode.RandomColor };
                default:
                    return gradient;
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
                    Tick(__instance);
                }
            }
        }

        // Every player carries the hit fx RPC; every client plays it.
        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static class Player_Awake_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                __instance.m_nview?.Register<Vector3, Vector3>(HitFxRpc, (sender, point, facing) => OnHitFxRpc(point, facing));
            }
        }
    }

    // HUD entry while the barrier is up; its icon text is the damage it can still take. The purple bubble is its start
    // effect, and it takes its share of each hit itself (EitrBarrier.Absorb).
    public class SE_EitrBarrier : StatusEffect
    {
        public override string GetIconText()
        {
            return EitrBarrier.GetIconText();
        }

        public override void OnDamaged(HitData hit, Character attacker)
        {
            EitrBarrier.Absorb(m_character as Player, hit, attacker);
        }
    }
}
