using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public class WeightedRandomCollection<T>
    {
        private readonly Random _random;
        private List<T> _list;
        private Func<T, float> _weightSelector;
        private bool _removeOnSelect;

        public float TotalWeight { get; private set; }

        public WeightedRandomCollection(Random random)
        {
            _random = random;
        }

        public WeightedRandomCollection(Random random, IEnumerable<T> collection, Func<T, float> weightSelector, bool removeOnSelect = false)
        {
            _random = random ?? new Random();
            Setup(collection, weightSelector, removeOnSelect);
        }

        public void Setup(IEnumerable<T> collection, Func<T, float> weightSelector, bool removeOnSelect = false)
        {
            _list = collection.ToList();
            _weightSelector = weightSelector;
            _removeOnSelect = removeOnSelect;
            TotalWeight = _list.Sum(_weightSelector);
        }

        public T Roll()
        {
            if (_list == null)
            {
                return default(T);
            }

            float itemWeightIndex = (float)_random.NextDouble() * TotalWeight;
            float currentWeightIndex = 0;

            T result = default(T);
            foreach (var item in from weightedItem in _list select new { Value = weightedItem, Weight = _weightSelector(weightedItem) })
            {
                currentWeightIndex += item.Weight;

                if (currentWeightIndex >= itemWeightIndex)
                {
                    result = item.Value;
                    break;
                }
            }

            if (_removeOnSelect)
            {
                _list.Remove(result);
                TotalWeight = _list.Sum(_weightSelector);
            }

            return result;
        }

        public List<T> Roll(int numberOfRolls)
        {
            if (_list == null)
            {
                return null;
            }
            var results = new List<T>();
            for (int i = 0; i < numberOfRolls; i++)
            {
                T result = Roll();
                if (!EqualityComparer<T>.Default.Equals(result, default(T)))
                {
                    results.Add(Roll());
                }
            }
            return results;
        }

        public void Reset()
        {
            _list = null;
            _weightSelector = null;
            _removeOnSelect = false;
            TotalWeight = 0;
        }
    }
}
