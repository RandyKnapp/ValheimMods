using System;
using System.Collections.Generic;

namespace EpicLoot.Abilities
{
    public static class AbilityFactory
    {
        public static readonly Dictionary<string, Type> AbilityClassTypes = new Dictionary<string, Type>();

        public static void Register(string abilityID, Type abilityClassType)
        {
            if (!AbilityClassTypes.ContainsKey(abilityID))
            {
                AbilityClassTypes.Add(abilityID, abilityClassType);
            }
            else
            {
                EpicLoot.LogWarning($"Duplicate entry found for AbilityClassTypes: {abilityID}.");
            }
        }

        public static Ability Create(string abilityID)
        {
            if (AbilityClassTypes.TryGetValue(abilityID, out var abilityClassType))
            {
                return (Ability)Activator.CreateInstance(abilityClassType);
            }

            return new Ability();
        }
    }
}
