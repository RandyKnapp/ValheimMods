using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;

namespace EpicLoot;

public static partial class API
{
    /// <summary>
    /// Simple registry for external assets, useful to keep track of objects,
    /// Keys are generated and returned to external API,
    /// to store and use to update specific objects
    /// </summary>
    /// <remarks>
    /// 1. External plugin invokes to 'add', on success, returns unique key
    /// 2. Key stored in registry with associated object
    /// 3. External plugin invokes to 'update' using unique key to target object
    /// </remarks>
    private static class RuntimeRegistry
    {
        private static readonly Dictionary<string, object> registry = new();
        private static int counter;
        
        /// <param name="obj"></param>
        /// <returns>unique identifier</returns>
        public static string Register(object obj)
        {
            string typeName = obj.GetType().Name;
            string key = $"{typeName}_obj_{++counter}";
            registry[key] = obj;
            return key;
        }

        /// <param name="key">unique key <see cref="string"/></param>
        /// <param name="value">object as class type <see cref="T"/></param>
        /// <typeparam name="T">class type <see cref="T"/></typeparam>
        /// <returns>True if object found matching key</returns>
        public static bool TryGetValue<T>(string key, out T value) where T : class
        {
            if (registry.TryGetValue(key, out object obj) && obj is T result)
            {
                value = result;
                return true;
            }

            value = null!;
            return false;
        }

        [PublicAPI]
        public static int GetCount() => counter;

        [PublicAPI]
        public static List<string> GetRegisteredKeys() => registry.Keys.ToList();
    }
}