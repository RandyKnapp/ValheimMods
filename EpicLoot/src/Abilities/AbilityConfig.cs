using System;
using System.Collections.Generic;

namespace EpicLoot.Abilities
{
    [Serializable]
    public enum AbilityActivationMode
    {
        Passive,
        Triggerable,
        Activated
    }

    [Serializable]
    public enum AbilityAction
    {
        Custom,
        StatusEffect
    }

    [Serializable]
    public class AbilityDefinition
    {
        public string ID;
        public string IconAsset;
        public AbilityActivationMode ActivationMode;
        public float Cooldown;
        public AbilityAction Action;
        public List<string> ActionParams = new List<string>();
    }

    [Serializable]
    public class AbilityConfig
    {
        public List<AbilityDefinition> Abilities;
    }
}
