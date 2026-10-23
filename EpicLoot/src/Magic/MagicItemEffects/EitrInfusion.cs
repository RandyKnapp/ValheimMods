using EpicLoot.Config;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string EitrInfusion = nameof(EitrInfusion);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // EitrInfusion (4-piece Einherjar set bonus, NoRoll). A hotkey pours all of the wearer's eitr into the weapon: every
    // EitrPerSecond eitr becomes a second of infusion, and MinEitr is needed to start one. While infused, each weapon hit
    // adds the effect value as a percent of the weapon's highest damage type, as that same type, and natural eitr regen
    // is held off. Meads, Eitr Weaving parries and Eitr Leech still add eitr, and pressing the key again pours what has
    // come back into more time. The element is locked on the first press: Lightning, Frost or Burning on the wearer, or
    // a nearby fire, turns the bonus into that element for the whole infusion. Poison never counts.
    //
    // The timer and the regen hold run whatever the wearer does; the damage needs the 4-piece, i.e. the set sword in
    // hand. Spellsword's eitr cost is waived while infused (Spellsword checks IsInfused), since the eitr is already
    // spent.
    //
    // The bonus is added in ModifyDamage's GetDamage postfix, among the other added damage and before the multipliers,
    // so Modify Elemental/Physical Damage and the all-damage effects scale it, the weapon tooltip shows it, and it
    // reaches every weapon hit (EpicLoot's own bonus hits never read GetDamage). It is computed on the attacker and
    // travels inside the hit.
    //
    // The look follows Leslie's Infusion (EpicLootLeslieAlphaTest). Every pour plays the Dundr staff's recharge, eitr
    // channelled into the weapon, in full: no attack, block or roll until it ends, and a press made while it cannot
    // start waits for it. A press without enough eitr shrugs. The HUD icon is the matching vanilla elemental sword's. While infused with the set sword in hand, the sword carries an aura on
    // every client (EitrInfusionAura): a glow in the element's colour, the element's own particles and a light. The
    // owner mirrors only the element to its player ZDO; each client draws the aura and plays its ignite sound itself.
    public static class EitrInfusion
    {
        public enum Element
        {
            None,
            Fire,
            Frost,
            Lightning
        }

        // All tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultMinEitr = 20f;
        public const float DefaultEitrPerSecond = 4f;
        public const float DefaultMaxSeconds = 0f;      // 0 = no cap

        private const string MinEitrKey = "MinEitr";
        private const string EitrPerSecondKey = "EitrPerSecond";
        private const string MaxSecondsKey = "MaxSeconds";

        // Player-editable, so each is kept in a sane range; EitrPerSecond divides.
        private static float MinEitr => EffectConfig.GetClamped(MagicEffectType.EitrInfusion, MinEitrKey, DefaultMinEitr, 1f, 1000f);
        private static float EitrPerSecond => EffectConfig.GetClamped(MagicEffectType.EitrInfusion, EitrPerSecondKey,
            DefaultEitrPerSecond, 0.1f, 100f);
        private static float MaxSeconds => EffectConfig.GetClamped(MagicEffectType.EitrInfusion, MaxSecondsKey,
            DefaultMaxSeconds, 0f, 3600f);

        // The world's eitr cost modifier applies to the infusion as it does to spells.
        private static float EitrRate => Game.m_eitrRate > 0f ? Game.m_eitrRate : 1f;

        private const float MinTopUp = 0.5f;            // less eitr than this is not worth a press

        private const string IndicatorName = "EL_EitrInfusion";
        private static readonly int IndicatorHash = IndicatorName.GetStableHashCode();
        private const string IconItem = "Eitr";
        private const string FallbackIconItem = "MeadEitrMinor";
        private const string TopUpSfx = "sfx_Potion_eitr_minor";

        // The element each client draws on a player's sword, mirrored to the player ZDO: 0 for none, else the element
        // + 1. Written only on change.
        internal static readonly int AuraZdoHash = "el-einf".GetStableHashCode();
        internal const int ElementCount = 4;

        // The Dundr staff's recharge. Both of its states are minor actions, which refuse attacks and blocks.
        private const string CastAnimation = "recharge_lightningstaff";
        private const string CastDoneTrigger = CastAnimation + "_done";
        private const float CastSeconds = 1f;
        private const float PressBufferSeconds = 0.5f;  // how long a press waits to be castable, as vanilla queues attacks
        private const string ShrugEmote = "shrug";

        // Indexed by Element: the HUD icon's item, and the sound each client plays as the aura appears.
        private static readonly string[] ElementIconItems = { null, "SwordDyrnwyn", "SwordMistwalker", "SwordNiedhoggLightning" };
        private static readonly string[] IgniteSfx = { null, "sfx_FireAddFuel", "sfx_Frost_Start", "sfx_weapons_lightning_enable" };
        private static readonly Sprite[] ElementIcons = new Sprite[ElementCount];

        private static StatusEffect _indicator;
        private static bool _iconMissingLogged;
        private static readonly HashSet<string> MissingFxLogged = new HashSet<string>();

        // Tooltip: "{0}% of the weapon's highest damage ... at least {1} Eitr ... a second for every {2} Eitr ..."
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.EitrInfusion, value => new object[]
            {
                value, MinEitr, EitrPerSecond
            });
        }

        // "Eitr Infusion [T]" in the combat key hints, while a press would do something.
        private static readonly SetAbilityKeyHint InfuseHint = new SetAbilityKeyHint("EL_EitrInfusion",
            "$mod_epicloot_eitrinfusion_hint", () => CanShowHint(Player.m_localPlayer),
            () => ELConfig.EitrInfusionKey, () => ELConfig.EitrInfusionGamepadButton);

        // State for the local player only; SyncOwner drops it when the local player changes (respawn, logout).
        private static Player _owner;
        private static bool _hasSet;
        private static float _infusedUntil;
        private static Element _element;
        private static int _publishedAura = -1;
        private static float _castUntil;                // 0 while not casting
        private static float _pressedUntil;             // 0 when no press is waiting

        public static bool IsInfused => _owner != null && _owner == Player.m_localPlayer && Time.time < _infusedUntil;

        public static float SecondsLeft => IsInfused ? _infusedUntil - Time.time : 0f;

        public static Element LockedElement => _element;

        private static void SyncOwner(Player player)
        {
            if (_owner == player)
            {
                return;
            }

            _owner = player;
            _hasSet = false;
            _infusedUntil = 0f;
            _element = Element.None;
            _publishedAura = -1;
            _castUntil = 0f;
            _pressedUntil = 0f;
        }

        private static bool CanShowHint(Player player)
        {
            if (player == null || player != _owner || !_hasSet || player.IsDead())
            {
                return false;
            }

            return IsInfused ? player.GetEitr() >= MinTopUp : player.GetEitr() >= MinEitr;
        }

        private static KeyCode Bound(BepInEx.Configuration.ConfigEntry<KeyCode> entry) => entry?.Value ?? KeyCode.None;

        private static bool KeyDown(KeyCode key) => key != KeyCode.None && ZInput.GetKeyDown(key, logWarning: false);

        private static bool KeyHeld(KeyCode key) => key != KeyCode.None && ZInput.GetKey(key, logWarning: false);

        // Called before the body of Player.Update, i.e. before vanilla reads this frame's buttons (T is OpenEmote,
        // read by HandleRadialInput). With the full set worn the key belongs to the infusion.
        private static void OnLocalPlayerUpdate(Player player)
        {
            SyncOwner(player);
            _hasSet = player.HasActiveMagicEffect(MagicEffectType.EitrInfusion, out float _);

            HandleKey(player);
            RunPendingPress(player);
            UpdateCast(player);
            PublishAura(player);
        }

        private static void HandleKey(Player player)
        {
            var key = Bound(ELConfig.EitrInfusionKey);
            var button = Bound(ELConfig.EitrInfusionGamepadButton);
            bool keyDown = KeyDown(key);
            bool buttonDown = SetAbilityKeys.GamepadButtonDown(player, button);
            if (!keyDown && !buttonDown || !_hasSet || !player.TakeInput() || Hud.InRadial() || player.IsDead())
            {
                return;
            }

            if (keyDown)
            {
                OverwhelmingLaunch.ConsumeKey(key, refresh: true);
            }
            if (buttonDown)
            {
                OverwhelmingLaunch.ConsumeKey(button, refresh: true);
            }

            _pressedUntil = Time.time + PressBufferSeconds;
        }

        // Every pour comes with the full cast, so a press made mid-swing, mid-roll or mid-stagger waits for the wearer
        // to be free, and is dropped if that takes longer than the buffer.
        private static void RunPendingPress(Player player)
        {
            if (_pressedUntil <= 0f)
            {
                return;
            }

            if (Time.time > _pressedUntil || !_hasSet || player.IsDead())
            {
                _pressedUntil = 0f;
                return;
            }

            if (!CanCast(player))
            {
                return;
            }

            _pressedUntil = 0f;
            TryInfuse(player);
        }

        // A block does not stop it: the cast drops the guard, as the staff's own recharge does. A cast already
        // running takes a further pour, which lengthens it.
        private static bool CanCast(Player player)
        {
            if (_castUntil > 0f)
            {
                return true;
            }

            return player.m_zanim != null && !player.InAttack() && !player.InDodge() && !player.IsStaggering() &&
                !player.InMinorAction() && !player.IsAttached() && !player.IsSwimming();
        }

        // Keys whose vanilla action is read in the fixed step (attack, block, jump -- mostly a concern for the gamepad
        // button) have already been read by the time Player.Update runs, so they are held down here too.
        private static void OnLocalPlayerFixedUpdate(Player player)
        {
            var key = Bound(ELConfig.EitrInfusionKey);
            var button = Bound(ELConfig.EitrInfusionGamepadButton);
            bool keyHeld = KeyHeld(key);
            bool buttonHeld = SetAbilityKeys.GamepadButtonHeld(player, button);
            if (!keyHeld && !buttonHeld || !_hasSet || player != _owner || !player.TakeInput())
            {
                return;
            }

            if (keyHeld)
            {
                OverwhelmingLaunch.ConsumeKey(key, refresh: false);
            }
            if (buttonHeld)
            {
                OverwhelmingLaunch.ConsumeKey(button, refresh: false);
            }
        }

        private static void TryInfuse(Player player)
        {
            if (!player.HasActiveMagicEffect(MagicEffectType.EitrInfusion, out float _))
            {
                return;
            }

            bool infused = IsInfused;
            float eitr = player.GetEitr();
            if (!infused && eitr < MinEitr || infused && eitr < MinTopUp)
            {
                Hud.instance?.EitrBarEmptyFlash();
                if (!infused)
                {
                    player.Message(MessageHud.MessageType.Center,
                        Localization.instance.Localize("$mod_epicloot_eitrinfusion_noeitr", MinEitr.ToString("0")));

                    // Standing still only: vanilla stops an emote as soon as you move.
                    if (player.m_moveDir == Vector3.zero)
                    {
                        player.StartEmote(ShrugEmote);
                    }
                }
                return;
            }

            float now = Time.time;
            if (!infused)
            {
                _element = LockElement(player);
                _infusedUntil = now;
            }

            float perSecond = EitrPerSecond * EitrRate;
            float seconds = eitr / perSecond;
            float cap = MaxSeconds;
            if (cap > 0f)
            {
                // Only what fits under the cap is poured; the rest stays in the pool.
                seconds = Mathf.Clamp(now + cap - _infusedUntil, 0f, seconds);
                if (infused && seconds <= 0f)
                {
                    return;     // already full
                }
            }
            _infusedUntil += seconds;

            // Taken from m_eitr directly, as Frostwalker and the Eitr Barrier do: UseEitr's EpicLoot patches would treat
            // the pour as a spell cast (HealthOnEitrUse, EitrUseGivesAdrenaline).
            player.m_eitr = Mathf.Max(0f, eitr - seconds * perSecond);
            PauseEitrRegen(player);

            if (infused)
            {
                SpawnNetworkedFx(TopUpSfx, player.transform.position, player.transform.rotation);
            }
            // Also on a top-up, in case something cleared the indicator while the infusion ran on.
            ShowIndicator(player);
            StartCast(player);

            EpicLoot.Log($"[EitrInfusion] {(infused ? "Topped up" : "Started")} with {seconds * perSecond:0.#} eitr: +{seconds:0.#} s " +
                $"({SecondsLeft:0.#} s left), element {_element}.");
        }

        // The rare, deliberate statuses outrank the ambient campfire; Poison never counts.
        private static Element LockElement(Player player)
        {
            var seMan = player.GetSEMan();
            if (seMan.HaveStatusEffect(SEMan.s_statusEffectLightning))
            {
                return Element.Lightning;
            }
            if (seMan.HaveStatusEffect(SEMan.s_statusEffectFrost))
            {
                return Element.Frost;
            }
            if (seMan.HaveStatusEffect(SEMan.s_statusEffectBurning) || seMan.HaveStatusEffect(SEMan.s_statusEffectCampFire))
            {
                return Element.Fire;
            }
            return Element.None;
        }

        // Holds natural regen off the way a spell cast does (RPC_UseEitr restarts the same timer), without spending any.
        private static void PauseEitrRegen(Player player)
        {
            player.m_eitrRegenTimer = Mathf.Max(player.m_eitrRegenTimer, player.m_eitrRegenDelay);
        }

        // Only reached through CanCast. ZSyncAnimation carries it to every client. The wearer is committed: nothing
        // ends it early, and while it plays vanilla refuses attacks and blocks, and Player_UpdateDodge_Patch holds back
        // rolls. Walking and jumping stay free, as in the staff's own recharge.
        private static void StartCast(Player player)
        {
            if (_castUntil > 0f)
            {
                _castUntil = Time.time + CastSeconds;
                return;
            }

            if (player.InEmote())
            {
                player.StopEmote();
            }

            player.m_zanim.SetBool(CastAnimation, true);
            _castUntil = Time.time + CastSeconds;
        }

        // Ends the cast on time, with vanilla's finishing pose. An attack or roll pressed in its last half second is
        // still in vanilla's queue and comes out after it.
        private static void UpdateCast(Player player)
        {
            if (_castUntil <= 0f || Time.time < _castUntil)
            {
                return;
            }

            _castUntil = 0f;
            player.m_zanim.SetBool(CastAnimation, false);
            if (!player.IsDead())
            {
                player.m_zanim.SetTrigger(CastDoneTrigger);
            }
        }

        // Written only on change, so the ZDO revision moves once per change rather than every frame.
        private static void PublishAura(Player player)
        {
            int aura = IsInfused && _hasSet ? (int)_element + 1 : 0;
            if (aura == _publishedAura)
            {
                return;
            }

            ZDO zdo = player.m_nview != null && player.m_nview.IsValid() ? player.m_nview.GetZDO() : null;
            if (zdo == null)
            {
                return;
            }

            _publishedAura = aura;
            zdo.Set(AuraZdoHash, aura);
        }

        // Each client's own copy (LocalFx), as the aura appears there; some of these have no ZNetView to share.
        internal static void PlayIgnite(Element element, Vector3 position)
        {
            string name = IgniteSfx[(int)element];
            GameObject prefab = name != null ? FindFxPrefab(name) : null;
            if (prefab != null)
            {
                LocalFx.Spawn(prefab, position, Quaternion.identity);
            }
        }

        // World load (SetEffectWarmup): the aura templates and the local copies of the ignite sounds.
        internal static void WarmupAtLoad(bool drawn)
        {
            if (!drawn)
            {
                return;
            }

            EitrInfusionAura.BuildTemplates();
            foreach (string name in IgniteSfx)
            {
                if (name != null)
                {
                    LocalFx.Prewarm(FindFxPrefab(name), scaled: false);
                }
            }
        }

        // Called from ModifyDamage.ApplyMagicDamageModifiers, after the added damage and before the multipliers.
        public static void AddInfusedDamage(Player player, ItemDrop.ItemData item, ref HitData.DamageTypes damages)
        {
            if (player == null || !IsInfused || player != _owner || item == null || item != player.GetCurrentWeapon() ||
                !player.HasActiveMagicEffect(MagicEffectType.EitrInfusion, out float value, 0.01f))
            {
                return;
            }

            // The highest of the damage types a hit can carry (chop and pickaxe are tool damage); ties go to the
            // first in this order. Vanilla's GetMajorityDamageType never looks at blunt, so it is not used.
            float best = damages.m_blunt;
            int kind = 0;
            Consider(damages.m_slash, 1, ref best, ref kind);
            Consider(damages.m_pierce, 2, ref best, ref kind);
            Consider(damages.m_fire, 3, ref best, ref kind);
            Consider(damages.m_frost, 4, ref best, ref kind);
            Consider(damages.m_lightning, 5, ref best, ref kind);
            Consider(damages.m_poison, 6, ref best, ref kind);
            Consider(damages.m_spirit, 7, ref best, ref kind);
            if (best <= 0f)
            {
                return;
            }

            float bonus = best * value;
            switch (_element)
            {
                case Element.Fire:
                    damages.m_fire += bonus;
                    return;
                case Element.Frost:
                    damages.m_frost += bonus;
                    return;
                case Element.Lightning:
                    damages.m_lightning += bonus;
                    return;
            }

            switch (kind)
            {
                case 0: damages.m_blunt += bonus; break;
                case 1: damages.m_slash += bonus; break;
                case 2: damages.m_pierce += bonus; break;
                case 3: damages.m_fire += bonus; break;
                case 4: damages.m_frost += bonus; break;
                case 5: damages.m_lightning += bonus; break;
                case 6: damages.m_poison += bonus; break;
                case 7: damages.m_spirit += bonus; break;
            }
        }

        private static void Consider(float amount, int index, ref float best, ref int kind)
        {
            if (amount > best)
            {
                best = amount;
                kind = index;
            }
        }

        // AddStatusEffect clones the prototype; the copy's name and icon carry the locked element for the HUD.
        private static void ShowIndicator(Player player)
        {
            SEMan seMan = player.GetSEMan();
            if (seMan.HaveStatusEffect(IndicatorHash))
            {
                return;
            }

            StatusEffect indicator = GetOrCreateIndicator();
            if (indicator == null)
            {
                return;
            }

            StatusEffect added = seMan.AddStatusEffect(indicator);
            if (added != null)
            {
                added.m_name = IndicatorToken(_element);
                Sprite icon = GetElementIcon(_element);
                if (icon != null)
                {
                    added.m_icon = icon;
                }
            }
        }

        // The matching vanilla elemental sword's icon; null for plain eitr, which keeps the indicator's own.
        private static Sprite GetElementIcon(Element element)
        {
            int index = (int)element;
            if (ElementIcons[index] == null && ElementIconItems[index] != null)
            {
                ElementIcons[index] = ObjectDB.instance?.GetItemPrefab(ElementIconItems[index])?.GetComponent<ItemDrop>()?
                    .m_itemData.GetIcon();
            }
            return ElementIcons[index];
        }

        private static string IndicatorToken(Element element)
        {
            switch (element)
            {
                case Element.Fire: return "$mod_epicloot_se_eitrinfusion_fire";
                case Element.Frost: return "$mod_epicloot_se_eitrinfusion_frost";
                case Element.Lightning: return "$mod_epicloot_se_eitrinfusion_lightning";
                default: return "$mod_epicloot_se_eitrinfusion";
            }
        }

        // A null icon would render as an invisible HUD entry, so a missing icon logs once and shows nothing. The eitr
        // mead's start sound (networked) plays on the wearer when it is added. Its potion swirl is left out: the cast
        // and the blade aura already show the infusion.
        private static StatusEffect GetOrCreateIndicator()
        {
            if (_indicator != null)
            {
                return _indicator;
            }

            ObjectDB objectDB = ObjectDB.instance;
            Sprite icon = objectDB?.GetItemPrefab(IconItem)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon() ??
                objectDB?.GetItemPrefab(FallbackIconItem)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null)
            {
                if (!_iconMissingLogged)
                {
                    _iconMissingLogged = true;
                    EpicLoot.LogWarning($"[EitrInfusion] could not find the '{IconItem}' or '{FallbackIconItem}' icon; the HUD indicator will not display.");
                }
                return null;
            }

            SE_EitrInfusion se = ScriptableObject.CreateInstance<SE_EitrInfusion>();
            se.name = IndicatorName;
            se.m_name = "$mod_epicloot_se_eitrinfusion";
            se.m_icon = icon;
            se.m_ttl = 0f;      // never expires on its own; IsDone removes it

            var startEffects = new List<EffectList.EffectData>();
            AddEffect(startEffects, "sfx_Potion_eitr_minor", attach: false);
            if (startEffects.Count > 0)
            {
                se.m_startEffects = new EffectList { m_effectPrefabs = startEffects.ToArray() };
            }

            _indicator = se;
            return _indicator;
        }

        private static void AddEffect(List<EffectList.EffectData> effects, string name, bool attach)
        {
            var prefab = FindFxPrefab(name);
            if (prefab != null)
            {
                effects.Add(new EffectList.EffectData { m_prefab = prefab, m_enabled = true, m_attach = attach });
            }
        }

        private static void SpawnNetworkedFx(string name, Vector3 position, Quaternion rotation)
        {
            var prefab = FindFxPrefab(name);
            if (prefab != null)
            {
                Object.Instantiate(prefab, position, rotation);
            }
        }

        private static GameObject FindFxPrefab(string name)
        {
            var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            if (prefab == null && MissingFxLogged.Add(name))
            {
                EpicLoot.LogWarning($"[EitrInfusion] could not find the '{name}' prefab; playing without it.");
            }
            return prefab;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static class Player_Update_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance)
            {
                if (__instance == Player.m_localPlayer)
                {
                    OnLocalPlayerUpdate(__instance);
                }
            }
        }

        // Every player on every client carries the aura; a headless server draws nothing.
        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static class Player_Awake_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    __instance.gameObject.AddComponent<EitrInfusionAura>();
                }
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.FixedUpdate))]
        private static class PlayerController_FixedUpdate_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(PlayerController __instance)
            {
                var player = __instance.m_character;
                if (player != null && player == Player.m_localPlayer)
                {
                    OnLocalPlayerFixedUpdate(player);
                }
            }
        }

        // No roll starts during the cast. Vanilla starts a queued roll only while its timer is above zero, so the timer is
        // hidden from it for the step and then given back, still counting down: the rest of UpdateDodge (dodge
        // invincibility, the end of a roll) runs as usual, and a roll pressed late in the cast comes out after it.
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateDodge))]
        private static class Player_UpdateDodge_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance, out float __state)
            {
                __state = float.NaN;
                if (_castUntil > 0f && ReferenceEquals(__instance, _owner))
                {
                    __state = __instance.m_queuedDodgeTimer;
                    __instance.m_queuedDodgeTimer = 0f;
                }
            }

            [HarmonyPostfix]
            private static void Postfix(Player __instance, float dt, float __state)
            {
                if (!float.IsNaN(__state))
                {
                    __instance.m_queuedDodgeTimer = __state - dt;
                }
            }
        }

        // Just before vanilla's regen step, every update while infused.
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateStats), typeof(float))]
        private static class Player_UpdateStats_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance)
            {
                if (IsInfused && ReferenceEquals(__instance, _owner))
                {
                    PauseEitrRegen(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(KeyHints), nameof(KeyHints.UpdateHints))]
        private static class KeyHints_UpdateHints_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(KeyHints __instance) => InfuseHint.Refresh(__instance);
        }
    }

    // HUD entry while infused: the seconds left as its icon text, and the locked element in its name.
    public class SE_EitrInfusion : StatusEffect
    {
        public override string GetIconText()
        {
            return StatusEffect.GetTimeString(EitrInfusion.SecondsLeft);
        }

        public override bool IsDone()
        {
            return !EitrInfusion.IsInfused;
        }
    }

    // The infused sword as every client sees it, on every player: from the element the owner mirrors to its ZDO, an
    // aura on the hand model of the weapon in hand. A re-equip makes a new hand model, so the aura is rebuilt on it, and
    // when the infusion ends it fades (AuraFade). The ignite sound plays as the aura appears, except for an infusion
    // already running when this client first sees the player.
    //
    // An aura is a glow in the element's colour, the element's own particles and a light, copied from vanilla prefabs
    // into one inactive template per element at load and fitted to the weapon by WeaponAuraKit.
    public class EitrInfusionAura : MonoBehaviour
    {
        private const float FadeSeconds = 3f;          // the longest particle lifetime here, so a fade cuts none off
        private const float QuietSeconds = 1f;         // an infusion seen this soon after the component starts is old

        // Indexed by EitrInfusion.Element.
        private static readonly Color[] Colors =
        {
            new Color(0.55f, 0.4f, 1f),                // eitr
            new Color(1f, 0.5f, 0.15f),                // fire
            new Color(0.45f, 0.85f, 1f),               // frost, Mistwalker's blue
            new Color(0.6f, 0.7f, 1f),                 // lightning
        };
        private static readonly float[] LightIntensities = { 0.8f, 1.2f, 0.8f, 1f };
        private const float LightRange = 3f;

        // The glow: the upgrade glow's particles, recoloured and sized like Mistwalker's frost.
        private const float GlowMinSize = 0.1f;
        private const float GlowMaxSize = 0.28f;
        private const float GlowAlpha = 0.3f;
        private const float GlowRate = 40f;
        private const float EitrGlowRate = 70f;        // plain eitr has no other particles, so its glow is denser

        // The element's own particles, from Leslie's sources: the particle systems under Branch in Prefab (all but the
        // prefab root's own when Branch is null), sized by Scale. The first source found is used. Fire prefers
        // Dyrnwyn's blade flames, which are the torch's flames laid along a sword.
        private struct AccentSource
        {
            public readonly string Prefab;
            public readonly string Branch;
            public readonly float Scale;

            public AccentSource(string prefab, string branch, float scale)
            {
                Prefab = prefab;
                Branch = branch;
                Scale = scale;
            }
        }

        private static readonly AccentSource[][] Accents =
        {
            new AccentSource[0],
            new[] { new AccentSource("SwordDyrnwyn", "Burny vfx", 1f), new AccentSource("Torch", "fx_Torch_Carried", 0.6f) },
            new[] { new AccentSource("vfx_Freezing", null, 0.3f) },
            new[] { new AccentSource("fx_Lightning", null, 0.3f) },
        };

        private static GameObject _templateRoot;
        private static readonly GameObject[] Templates = new GameObject[EitrInfusion.ElementCount];

        private Player _player;
        private int _shown;
        private GameObject _weapon;
        private GameObject _aura;
        private float _startTime;

        private void Awake()
        {
            _player = GetComponent<Player>();
            _startTime = Time.time;
        }

        private void LateUpdate()
        {
            int state = GetState();
            Transform hand = null;
            GameObject weapon = state > 0 ? WeaponAuraKit.GetWeaponModel(_player.m_visEquipment, out hand) : null;
            if (state == _shown && weapon == _weapon)
            {
                return;
            }

            var element = (EitrInfusion.Element)(state - 1);
            bool ignite = state > 0 && state != _shown && Time.time - _startTime >= QuietSeconds;
            _shown = state;
            _weapon = weapon;

            if (_aura != null)
            {
                _aura.AddComponent<AuraFade>().Begin(FadeSeconds);
            }
            _aura = weapon != null ? Attach(weapon, hand, element) : null;

            if (ignite)
            {
                EitrInfusion.PlayIgnite(element, weapon != null ? weapon.transform.position : _player.GetCenterPoint());
            }
        }

        // The main-menu preview player has no ZDO, so it never shows an aura.
        private int GetState()
        {
            ZDO zdo = _player != null && _player.m_nview != null ? _player.m_nview.GetZDO() : null;
            if (zdo == null || _player.IsDead())
            {
                return 0;
            }

            int state = zdo.GetInt(EitrInfusion.AuraZdoHash);
            return state > 0 && state <= EitrInfusion.ElementCount ? state : 0;
        }

        // World load (SetEffectWarmup), or the first aura should that have failed. Vanilla prefabs don't change, so the
        // templates are kept for the session.
        internal static void BuildTemplates()
        {
            if (_templateRoot != null || ZNetScene.instance == null)
            {
                return;
            }

            _templateRoot = new GameObject("EL_EitrInfusionAuras");
            _templateRoot.SetActive(false);
            DontDestroyOnLoad(_templateRoot);

            ParticleSystem glow = WeaponAuraKit.FindGlowSource();
            if (glow == null)
            {
                EpicLoot.LogWarning("[EitrInfusion] found no weapon with an upgrade glow to copy; auras show without one.");
            }

            for (int i = 0; i < Templates.Length; i++)
            {
                Templates[i] = BuildTemplate((EitrInfusion.Element)i, glow);
            }
        }

        // Root, then a frame placed where the weapon's emitter is, holding the particle systems and the light. Inactive,
        // so a copy is fitted to its weapon before any of it plays.
        private static GameObject BuildTemplate(EitrInfusion.Element element, ParticleSystem glowSource)
        {
            int index = (int)element;
            Transform frame = WeaponAuraKit.NewTemplate(_templateRoot.transform, $"EL_EitrInfusionAura_{element}", out GameObject root);

            if (glowSource != null)
            {
                WeaponAuraKit.AddGlow(glowSource, frame, Colors[index], GlowAlpha, GlowMinSize, GlowMaxSize,
                    element == EitrInfusion.Element.None ? EitrGlowRate : GlowRate);
            }

            bool accented = Accents[index].Length == 0;
            foreach (AccentSource source in Accents[index])
            {
                if (CopyAccent(source, frame))
                {
                    accented = true;
                    break;
                }
            }
            if (!accented)
            {
                EpicLoot.LogWarning($"[EitrInfusion] found none of the {element} aura's particles; it shows the glow only.");
            }

            WeaponAuraKit.AddLight(frame, Colors[index], LightIntensities[index], LightRange);

            root.SetActive(false);
            return root;
        }

        // Lifetimes are kept as they are: Dyrnwyn's flare is one long-lived particle, which AuraFade clears instead.
        private static bool CopyAccent(AccentSource source, Transform frame)
        {
            GameObject prefab = WeaponAuraKit.FindPrefab(source.Prefab);
            Transform branch = prefab == null ? null : source.Branch == null ? prefab.transform : WeaponAuraKit.FindDeep(prefab.transform, source.Branch);
            return WeaponAuraKit.CopyBranch(branch, frame, source.Scale, includeRoot: false, clampLifetime: false).Count > 0;
        }

        private static GameObject Attach(GameObject weapon, Transform hand, EitrInfusion.Element element)
        {
            BuildTemplates();
            GameObject template = Templates[(int)element];
            return template != null ? WeaponAuraKit.AttachFitted(template, weapon, hand) : null;
        }
    }
}
