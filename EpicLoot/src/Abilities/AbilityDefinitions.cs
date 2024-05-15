using System.Collections.Generic;

namespace EpicLoot.Abilities
{
    public static class AbilityDefinitions
    {
        public static AbilityConfig Config;
        public static readonly Dictionary<string, AbilityDefinition> Abilities = new Dictionary<string, AbilityDefinition>();

        public static void Initialize(AbilityConfig config)
        {
            Config = config;

            Abilities.Clear();
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

        public static bool TryGetAbilityDef(string abilityID, out AbilityDefinition abilityDef)
        {
            return Abilities.TryGetValue(abilityID, out abilityDef);
        }
    }
}
