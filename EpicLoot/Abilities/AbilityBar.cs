using System;
using System.Collections.Generic;
using Common;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.Abilities
{
    public class AbilityIcon : MonoBehaviour
    {
        public Image Icon;
        public Image CooldownProgress;
        public Text Binding;
        public Text CooldownText;

        protected int _index = -1;

        public virtual void Awake()
        {
            Icon = transform.Find("Icon").GetComponent<Image>();
            CooldownProgress = transform.Find("CooldownProgress").GetComponent<Image>();
            Binding = transform.Find("Binding").GetComponent<Text>();
            CooldownText = transform.Find("CooldownText").GetComponent<Text>();

            var iconMaterial = Hud.instance.m_pieceIconPrefab.transform.Find("icon").GetComponent<Image>().material;
            Icon.material = iconMaterial;
        }

        public void SetIndex(int index)
        {
            _index = index;
        }

        public virtual void Refresh(Ability ability)
        {
            if (_index < 0 || ability == null || ability.AbilityDef == null)
            {
                return;
            }

            if (Icon.sprite == null || Icon.sprite.name != ability.AbilityDef.IconAsset)
            {
                Icon.sprite = EpicLoot.LoadAsset<Sprite>(ability.AbilityDef.IconAsset);
            }

            Binding.enabled = ability.AbilityDef.ActivationMode == AbilityActivationMode.Activated;
            var bindingText = EpicLoot.AbilityKeyCodes[_index].Value.ToUpperInvariant();
            if (Binding.text != bindingText)
            {
                Binding.text = bindingText;
            }

            var cooldownSecondsRemaining = ability.TimeUntilCooldownEnds();
            var cooldownPercentComplete = ability.PercentCooldownComplete();
            var onCooldown = ability.IsOnCooldown();

            CooldownText.enabled = onCooldown;
            CooldownProgress.enabled = onCooldown;

            if (onCooldown)
            {
                var cooldownTime = TimeSpan.FromSeconds(cooldownSecondsRemaining);
                CooldownText.text = cooldownTime.ToString(@"m\:ss");
                CooldownProgress.fillAmount = 1 - cooldownPercentComplete;
            }

            Icon.color = onCooldown ? new Color(1, 0, 1, 0) : Color.white;
        }
    }

    public class AbilityBar : ConfigPositionedElement
    {
        public List<AbilityIcon> AbilityIcons = new List<AbilityIcon>();

        protected HorizontalLayoutGroup _horizontalLayoutGroup;
        protected Player _player;
        protected AbilityController _abilityController;

        public override void Awake()
        {
            AnchorConfig = EpicLoot.AbilityBarAnchor;
            PositionConfig = EpicLoot.AbilityBarPosition;
            base.Awake();

            _horizontalLayoutGroup = GetComponent<HorizontalLayoutGroup>();
            EnsureCorrectPosition();

            for (var i = 0; i < 3; i++)
            {
                var child = transform.GetChild(i);
                var abilityIcon = child.gameObject.AddComponent<AbilityIcon>();
                AbilityIcons.Add(abilityIcon);
                abilityIcon.SetIndex(i);
                abilityIcon.gameObject.SetActive(false);
            }
        }

        public override void Update()
        {
            base.Update();

            if (_player == null)
            {
                _player = Player.m_localPlayer;
                if (_player != null)
                {
                    _abilityController = _player.GetComponent<AbilityController>();
                }
            }

            if (_player == null || _abilityController == null)
            {
                return;
            }

            for (var index = 0; index < AbilityIcons.Count; index++)
            {
                var abilityIcon = AbilityIcons[index];
                var ability = _abilityController.GetCurrentAbility(index);
                abilityIcon.gameObject.SetActive(ability != null);
                if (ability != null)
                {
                    abilityIcon.Refresh(ability);
                }
            }
        }

        public override void EnsureCorrectPosition()
        {
            base.EnsureCorrectPosition();

            if (_horizontalLayoutGroup.childAlignment == EpicLoot.AbilityBarLayoutAlignment.Value
                && Mathf.Approximately(_horizontalLayoutGroup.spacing, EpicLoot.AbilityBarIconSpacing.Value))
            {
                return;
            }

            _horizontalLayoutGroup.childAlignment = EpicLoot.AbilityBarLayoutAlignment.Value;
            _horizontalLayoutGroup.spacing = EpicLoot.AbilityBarIconSpacing.Value;
        }
    }

    [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
    public static class Hud_Awake_Patch
    {
        public static void Postfix(Hud __instance)
        {
            var abilityBar = Object.Instantiate(EpicLoot.Assets.AbilityBar, __instance.m_rootObject.transform, false);
            abilityBar.AddComponent<AbilityBar>();
        }
    }
}
