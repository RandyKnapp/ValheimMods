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
                object result = Activator.CreateInstance(abilityClassType);
                if (result is API.AbilityProxy proxyAbility && !proxyAbility.InjectCallbacks(abilityID))
                {
                    return new Ability();
                }

                return (Ability)result;
            }

            return new Ability();
        }
    }
}
