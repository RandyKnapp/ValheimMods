using System;
using System.Collections.Generic;

namespace EpicLoot.Abilities
{
    public static class AbilityDefinitions
    {
        public static AbilityConfig Config;
        public static readonly Dictionary<string, AbilityDefinition> Abilities = new Dictionary<string, AbilityDefinition>();
        public static event Action OnSetupAbilityDefinitions;

        public static void Initialize(AbilityConfig config)
        {
            Config = config;
            OnSetupAbilityDefinitions?.Invoke();

            Abilities.Clear();
            if (Config?.Abilities == null)
            {
                // Malformed/empty abilities.json (or a null server payload): keep the ability list
                // empty instead of NRE-ing out of config load.
                EpicLoot.LogWarning("abilities.json produced no ability list; no abilities are registered.");
                return;
            }
            foreach (var def in Config.Abilities)
            {
                if (!Abilities.ContainsKey(def.ID))
                {
                    Abilities.Add(def.ID, def);
                }
                else
                {
                    EpicLoot.LogWarning($"Duplicate entry found for Abilities: {def.ID}. " +
                        $"Please fix your configuration.");
                }
            }
        }

        public static AbilityConfig GetCFG()
        {
            return Config;
        }

        public static bool TryGetAbilityDef(string abilityID, out AbilityDefinition abilityDef)
        {
            return Abilities.TryGetValue(abilityID, out abilityDef);
        }
    }
}
