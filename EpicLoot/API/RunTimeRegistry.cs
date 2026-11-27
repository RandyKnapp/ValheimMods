using System.Collections.Generic;

namespace EpicLoot;

public static partial class API
{
    // The keys the Add* calls return, which the matching Update* calls take to find what was added.
    private static class RuntimeRegistry
    {
        private static readonly Dictionary<string, object> registry = new();
        private static int counter;
        
        public static string Register(object obj)
        {
            string typeName = obj.GetType().Name;
            string key = $"{typeName}_obj_{++counter}";
            registry[key] = obj;
            return key;
        }

        public static bool TryGetValue<T>(string key, out T value) where T : class
        {
            if (key != null && registry.TryGetValue(key, out object obj) && obj is T result)
            {
                value = result;
                return true;
            }

            value = null!;
            return false;
        }
    }
}