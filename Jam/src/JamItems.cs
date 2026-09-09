using Common;
using Jotunn.Configs;
using System.Collections.Generic;
using UnityEngine;

namespace Jam
{
    /// <summary>
    /// Declares every jam and hands it to the shared <see cref="ItemBatchLoader"/>, which binds the
    /// server-synced config entries (recipe, crafting station, station level, craft amount and the
    /// five food stats), registers the items with Jotunn, and keeps them reconciled to the config for
    /// the rest of the session - across live edits, server config syncs and ObjectDB rebuilds.
    /// </summary>
    public class JamItems
    {
        // Ranges for the per-jam config sliders. ItemStatConfig defaults to a 0-400 range, which is
        // far too tight for a 20-minute buff duration and looser than useful for the rest.
        private const float MaxFoodValue = 200f;
        private const float MaxRegen = 20f;
        private const float MaxDuration = 6000f;

        private static readonly List<ItemDefinition> Jams = new List<ItemDefinition>();

        /// <summary>Every registered jam, in declaration order. Read by <see cref="ServingTray"/>.</summary>
        public static IReadOnlyList<ItemDefinition> Definitions => Jams;

        private readonly ItemBatchLoader _loader = new ItemBatchLoader();

        public JamItems(AssetBundle assetBundle)
        {
            // jamassets is a flat bundle keyed by bare prefab name, and every jam prefab carries its
            // own icon, so the loader's "Assets/Custom/..." path conventions do not apply here.
            ItemBatchLoader.PrefabPathFormat = null;
            ItemBatchLoader.IconPathFormat = null;

            //     prefab                display name                 lvl  hp  stam eitr regen duration  recipe
            AddJam("RaspberryJam", "Raspberry Jam", 1, 14f, 30f, 0f, 1f, 1200f,
                Ingredient("Raspberry", 14));
            AddJam("HoneyRaspberryJam", "Sweetened Raspberry Jam", 1, 18f, 35f, 0f, 2f, 1200f,
                Ingredient("Raspberry", 8), Ingredient("Honey", 4));
            AddJam("BlueberryJam", "Blueberry Jam", 1, 14f, 35f, 0f, 1f, 1200f,
                Ingredient("Blueberries", 14));
            AddJam("HoneyBlueberryJam", "Sweetened Blueberry Jam", 1, 18f, 40f, 0f, 2f, 1200f,
                Ingredient("Blueberries", 8), Ingredient("Honey", 4));
            AddJam("CloudberryJam", "Cloudberry Jam", 3, 26f, 45f, 0f, 1f, 1200f,
                Ingredient("Cloudberry", 14));
            AddJam("HoneyCloudberryJam", "Sweetened Cloudberry Jam", 3, 30f, 50f, 0f, 2f, 1200f,
                Ingredient("Cloudberry", 8), Ingredient("Honey", 4));
            AddJam("KingsJam", "Kings Jam", 3, 26f, 50f, 0f, 2f, 1200f,
                Ingredient("Raspberry", 8), Ingredient("Cloudberry", 6));
            AddJam("NordicJam", "Nordic Jam", 3, 26f, 55f, 0f, 2f, 1200f,
                Ingredient("Blueberries", 8), Ingredient("Cloudberry", 6));
            AddJam("MushroomJam", "Savory Eitrian Jam", 4, 35f, 55f, 30f, 3f, 1500f,
                Ingredient("MushroomMagecap", 4), Ingredient("MushroomJotunPuffs", 4),
                Ingredient("Onion", 2), Ingredient("Honey", 2));
            AddJam("AshlandsJam", "Rich Eitrian Jam", 5, 40f, 60f, 35f, 4f, 1500f,
                Ingredient("Vineberry", 4), Ingredient("MushroomSmokePuff", 4),
                Ingredient("Onion", 2), Ingredient("Honey", 2));

            _loader.BatchSetup(assetBundle);
        }

        /// <summary>
        /// Every jam has the same shape - a cauldron food crafted four at a time, differing only in
        /// station level, its five food stats and its recipe - so they are declared through one helper
        /// rather than ten near-identical <see cref="ItemDefinition"/> blocks.
        /// </summary>
        private void AddJam(string prefab, string name, int stationLevel,
            float health, float stamina, float eitr, float regen, float duration,
            params RecipeIngredient[] recipe)
        {
            ItemDefinition jam = new ItemDefinition
            {
                Name = name,
                Prefab = prefab,
                Category = ItemCategory.Food,
                CraftedAt = CraftingStations.Cauldron,
                ReqStationlevel = stationLevel,
                CraftAmount = 4,
                ModifableStats = new Dictionary<ItemStat, ItemStatConfig>
                {
                    { ItemStat.food_health, Stat(health, MaxFoodValue) },
                    { ItemStat.food_stamina, Stat(stamina, MaxFoodValue) },
                    { ItemStat.food_eitr, Stat(eitr, MaxFoodValue) },
                    { ItemStat.food_regen, Stat(regen, MaxRegen) },
                    { ItemStat.food_duration, Stat(duration, MaxDuration) }
                },
                Recipe = new RecipeDefinition { RecipeItems = new List<RecipeIngredient>(recipe) }
            };

            Jams.Add(jam);
            _loader.AddDefinition(jam);
        }

        private static ItemStatConfig Stat(float value, float max)
        {
            return new ItemStatConfig { Default_value = value, Min = 0f, Max = max };
        }

        private static RecipeIngredient Ingredient(string prefab, int amount)
        {
            return new RecipeIngredient { Prefab = prefab, Amount = amount };
        }
    }
}
