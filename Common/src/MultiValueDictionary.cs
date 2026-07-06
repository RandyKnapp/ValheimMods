using System.Collections.Generic;

namespace Common
{
    public class MultiValueDictionary<TKey, TValue> : Dictionary<TKey, List<TValue>>
    {
        public void Add(TKey key, TValue value)
        {
            if (!TryGetValue(key, out var container))
            {
                container = new List<TValue>();
                base.Add(key, container);
            }
            container.Add(value);
        }

        public bool ContainsValue(TKey key, TValue value)
        {
            if (TryGetValue(key, out var values))
            {
                return values.Contains(value);
            }
            return false;
        }

        public void Remove(TKey key, TValue value)
        {
            if (TryGetValue(key, out var container))
            {
                container.Remove(value);
                if (container.Count <= 0)
                {
                    Remove(key);
                }
            }
        }

        public void Merge(MultiValueDictionary<TKey, TValue> toMergeWith)
        {
            if (toMergeWith == null)
            {
                return;
            }

            foreach (var pair in toMergeWith)
            {
                foreach (var value in pair.Value)
                {
                    Add(pair.Key, value);
                }
            }
        }

        public List<TValue> GetValues(TKey key, bool returnEmptySet = false)
        {
            if (!base.TryGetValue(key, out var toReturn) && returnEmptySet)
            {
                toReturn = new List<TValue>();
            }
            return toReturn;
        }
    }
}
