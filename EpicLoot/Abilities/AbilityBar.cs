using System;
using System.Collections.Generic;
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
            if (_index < 0)
            {
                return;
            }

            if (Icon.sprite == null || Icon.sprite.name != ability.AbilityDef.IconAsset)
            {
                Icon.sprite = EpicLoot.LoadAsset<Sprite>(ability.AbilityDef.IconAsset);
            }

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

    public class AbilityBar : MonoBehaviour
    {
        public List<AbilityIcon> AbilityIcons = new List<AbilityIcon>();

        protected RectTransform _parent;
        protected RectTransform _rt;
        protected HorizontalLayoutGroup _horizontalLayoutGroup;
        protected TextAnchor _currentAnchor;
        protected Player _player;
        protected AbilityController _abilityController;

        public virtual void Awake()
        {
            _parent = (RectTransform)transform.parent;
            _rt = (RectTransform)transform;
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

        public virtual void Update()
        {
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

            EnsureCorrectPosition();

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

        private void EnsureCorrectPosition()
        {
            if (_currentAnchor == EpicLoot.AbilityBarAnchor.Value 
                && _rt.anchoredPosition == EpicLoot.AbilityBarPosition.Value
                && _horizontalLayoutGroup.childAlignment == EpicLoot.AbilityBarLayoutAlignment.Value
                && _horizontalLayoutGroup.spacing == EpicLoot.AbilityBarIconSpacing.Value)
            {
                return;
            }

            _horizontalLayoutGroup.childAlignment = EpicLoot.AbilityBarLayoutAlignment.Value;
            _horizontalLayoutGroup.spacing = EpicLoot.AbilityBarIconSpacing.Value;

            _currentAnchor = EpicLoot.AbilityBarAnchor.Value;
            switch (_currentAnchor)
            {
                case TextAnchor.UpperLeft:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(0, 1);
                    break;
                case TextAnchor.UpperCenter:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 1);
                    break;
                case TextAnchor.UpperRight:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(1, 1);
                    break;
                case TextAnchor.MiddleLeft:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(0, 0.5f);
                    break;
                case TextAnchor.MiddleCenter:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
                    break;
                case TextAnchor.MiddleRight:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(1, 0.5f);
                    break;
                case TextAnchor.LowerLeft:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(0, 0);
                    break;
                case TextAnchor.LowerCenter:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0);
                    break;
                case TextAnchor.LowerRight:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(1, 0);
                    break;
            }

            _rt.anchoredPosition = EpicLoot.AbilityBarPosition.Value;
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
