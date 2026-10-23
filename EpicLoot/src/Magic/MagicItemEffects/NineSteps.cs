using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string NineSteps = nameof(NineSteps);
    }
}

namespace EpicLoot.MagicItemEffects
{
    public static class NineSteps
    {
        public const int DefaultSteps = 9;
        public const float DefaultStepInterval = 0.5f;
        public const float DefaultSlow = 40f;

        private const string StepsKey = "Steps";
        private const string StepIntervalKey = "StepInterval";
        private const string SlowKey = "Slow";

        private const float MinThreshold = 10f;
        private const float MaxThreshold = 1000f;

        private const string DoomedToken = "$mod_epicloot_ninesteps_doomed";
        private const string TextColor = "#9BE22D";
        internal const string DoomFx = "sfx_Poison_Start";
        internal const string StepFx = "vfx_poisonarrow_hit";

        private static bool _missingFxLogged;

        internal static int Steps => EffectConfig.GetIntAtLeast(MagicEffectType.NineSteps, StepsKey, DefaultSteps, 1);
        internal static float StepInterval => Mathf.Max(0.1f, EffectConfig.Get(MagicEffectType.NineSteps, StepIntervalKey, DefaultStepInterval));
        internal static float SlowMultiplier => 1f - EffectConfig.GetClamped(MagicEffectType.NineSteps, SlowKey, DefaultSlow, 0f, 90f) / 100f;

        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.NineSteps,
                value => new object[] { value, Steps, Steps * StepInterval });
        }

        // The threshold comes off the wire, so it is bounded on receipt: a zero or negative one would doom on
        // the first drop of venom.
        internal static float ClampThreshold(float threshold)
        {
            return threshold > 0f ? Mathf.Clamp(threshold, MinThreshold, MaxThreshold) : 0f;
        }

        internal static void Evaluate(Character victim, CoiledVenom venom, bool resisted)
        {
            if (venom.Doomed || victim.IsDead())
            {
                return;
            }

            float pool = victim.GetSEMan().GetStatusEffect(SEMan.s_statusEffectPoison) is SE_Poison poison
                ? Mathf.Max(0f, poison.m_damageLeft)
                : 0f;

            // The scaling Character.ApplyDamage puts on every poison tick.
            float venomDamage = pool * Game.m_playerDamageRate *
                Game.instance.GetDifficultyDamageScaleEnemy(victim.transform.position);
            float health = victim.GetHealth();
            float needed = health * venom.DoomThreshold / 100f;

            string name = Utils.GetPrefabName(victim.gameObject);
            string added = resisted
                ? "poison fully resisted"
                : $"+{venom.Added:0.##} venom on {venom.Carried:0.##} carried";
            string verdict = venom.DoomThreshold > 0f
                ? $"dooms at {needed:0.#} ({venom.DoomThreshold:0.#}% of health)"
                : "Nine Steps not active";
            EpicLoot.Log($"[NineSteps] {name}: {added}, venom {venomDamage:0.##} vs health {health:0.#}/{victim.GetMaxHealth():0.#}, {verdict}");

            if (venom.DoomThreshold <= 0f || venomDamage <= 0f || health <= 0f || venomDamage < needed)
            {
                return;
            }

            EpicLoot.Log($"[NineSteps] {name} doomed: {Steps} steps, {StepInterval:0.##}s apart");
            venom.Doom = victim.gameObject.AddComponent<NineStepsDoom>();
            venom.Doom.Begin(victim, venom.Attacker);
        }

        // DamageText relays the text to every client, and each localizes it for itself.
        internal static void ShowText(Character character, DamageText.TextType type, string text)
        {
            if (DamageText.instance != null)
            {
                DamageText.instance.ShowText(type, character.GetTopPoint(), $"<color={TextColor}>{text}</color>");
            }
        }

        internal static void ShowDoomed(Character character)
        {
            ShowText(character, DamageText.TextType.Bonus, DoomedToken);
        }

        // Vanilla's floating text template has rich text switched off, and the doom text's colour tag needs it.
        // Every client draws the text from its own template, so each one switches it on for itself.
        [HarmonyPatch(typeof(DamageText), nameof(DamageText.Awake))]
        private static class DamageText_Awake_Patch
        {
            [UsedImplicitly]
            private static void Postfix(DamageText __instance)
            {
                if (__instance.m_worldTextBase != null && __instance.m_worldTextBase.TryGetComponent(out TMP_Text text))
                {
                    text.richText = true;
                }
            }
        }

        internal static void PlayFx(string name, Character character)
        {
            GameObject prefab = ZNetScene.instance?.GetPrefab(name);
            if (prefab == null)
            {
                if (!_missingFxLogged)
                {
                    _missingFxLogged = true;
                    EpicLoot.LogWarning($"[NineSteps] could not find the '{name}' prefab; the doom plays without it.");
                }
                return;
            }

            Object.Instantiate(prefab, character.GetCenterPoint(), Quaternion.identity);
        }
    }

    internal sealed class NineStepsDoom : MonoBehaviour
    {
        private Character _character;
        private ZDOID _attacker;
        private int _stepsLeft;

        internal void Begin(Character character, ZDOID attacker)
        {
            _character = character;
            _attacker = attacker;
            _stepsLeft = NineSteps.Steps;

            // A poison tick carries no attacker, so a creature it finished would die with nobody credited for
            // the kill. The venom is taken out here and the last step deals it instead.
            character.GetSEMan().RemoveStatusEffect(SEMan.s_statusEffectPoison, true);

            NineSteps.ShowDoomed(character);
            NineSteps.PlayFx(NineSteps.DoomFx, character);
            ApplySlow();

            float interval = NineSteps.StepInterval;
            InvokeRepeating(nameof(Step), interval, interval);
        }

        [UsedImplicitly]
        private void Step()
        {
            if (_character == null || _character.IsDead() || _character.m_nview == null ||
                !_character.m_nview.IsValid() || !_character.m_nview.IsOwner())
            {
                EpicLoot.Log("[NineSteps] Doom dropped: the creature died early or changed owner");
                Destroy(this);
                return;
            }

            _stepsLeft--;
            NineSteps.PlayFx(NineSteps.StepFx, _character);
            if (_stepsLeft > 0)
            {
                NineSteps.ShowText(_character, DamageText.TextType.Normal, _stepsLeft.ToString());
                ApplySlow();
                return;
            }

            Kill();
            Destroy(this);
        }

        private void ApplySlow()
        {
            float multiplier = Slow.ClampMultiplier(NineSteps.SlowMultiplier);
            if (multiplier < 1f && !_character.IsBoss())
            {
                _character.m_nview.InvokeRPC(ZRoutedRpc.Everybody, Slow.RPCKey, multiplier);
            }
        }

        private void Kill()
        {
            var hit = new HitData
            {
                m_point = _character.GetCenterPoint(),
                m_hitType = HitData.HitType.Poisoned,
            };
            hit.m_damage.m_damage = _character.GetMaxHealth() * 100f;

            GameObject attacker = ZNetScene.instance?.FindInstance(_attacker);
            bool credited = false;
            if (attacker != null && attacker.TryGetComponent(out Character attackerCharacter))
            {
                hit.SetAttacker(attackerCharacter);
                credited = true;
            }

            _character.ApplyDamage(hit, false, false);
            EpicLoot.Log($"[NineSteps] {Utils.GetPrefabName(_character.gameObject)} took its last step: health now {_character.GetHealth():0.#}, " +
                (credited ? $"kill credited to {attacker.name}" : "attacker not found, kill uncredited"));
        }
    }
}
