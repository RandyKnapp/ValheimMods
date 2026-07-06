using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace Jam
{
    public class UpdateJams
    {
        public static void UpdateJamConfigruations()
        {
            foreach (string jam in Jam.Jams)
            {
                string recipeString = string.Empty;
                bool enabled = true;
                float health = 0f;
                float stamina = 0f;
                float eitr = 0f;
                float regen = 0f;
                float duration = 0f;

                // TODO finish switch statement
                switch (jam)
                {
                    case "RaspberryJam":
                        enabled = Jam.RaspberryJamEnabled.Value;
                        recipeString = Jam.RaspberryJamRecipe.Value;
                        duration = Jam.RaspberryJamDuration.Value;
                        health = Jam.RaspberryJamHealth.Value;
                        stamina = Jam.RaspberryJamStamina.Value;
                        eitr = Jam.RaspberryJamEtir.Value;
                        regen = Jam.RaspberryJamRegen.Value;
                        break;
                    case "HoneyRaspberryJam":
                        enabled = Jam.HoneyRaspberryJamEnabled.Value;
                        recipeString = Jam.HoneyRaspberryJamRecipe.Value;
                        duration = Jam.HoneyRaspberryJamDuration.Value;
                        health = Jam.HoneyRaspberryJamHealth.Value;
                        stamina = Jam.HoneyRaspberryJamStamina.Value;
                        eitr = Jam.HoneyRaspberryJamEtir.Value;
                        regen = Jam.HoneyRaspberryJamRegen.Value;
                        break;
                    case "BlueberryJam":
                        enabled = Jam.BlueberryJamEnabled.Value;
                        recipeString = Jam.BlueberryJamRecipe.Value;
                        duration = Jam.BlueberryJamDuration.Value;
                        health = Jam.BlueberryJamHealth.Value;
                        stamina = Jam.BlueberryJamStamina.Value;
                        eitr = Jam.BlueberryJamEtir.Value;
                        regen = Jam.BlueberryJamRegen.Value;
                        break;
                    case "HoneyBlueberryJam":
                        enabled = Jam.HoneyBlueberryJamEnabled.Value;
                        recipeString = Jam.HoneyBlueberryJamRecipe.Value;
                        duration = Jam.HoneyBlueberryJamDuration.Value;
                        health = Jam.HoneyBlueberryJamHealth.Value;
                        stamina = Jam.HoneyBlueberryJamStamina.Value;
                        eitr = Jam.HoneyBlueberryJamEtir.Value;
                        regen = Jam.HoneyBlueberryJamRegen.Value;
                        break;
                    case "CloudberryJam":
                        enabled = Jam.CloudberryJamEnabled.Value;
                        recipeString = Jam.CloudberryJamRecipe.Value;
                        duration = Jam.CloudberryJamDuration.Value;
                        health = Jam.CloudberryJamHealth.Value;
                        stamina = Jam.CloudberryJamStamina.Value;
                        eitr = Jam.CloudberryJamEtir.Value;
                        regen = Jam.CloudberryJamRegen.Value;
                        break;
                    case "HoneyCloudberryJam":
                        enabled = Jam.HoneyCloudberryJamEnabled.Value;
                        recipeString = Jam.HoneyCloudberryJamRecipe.Value;
                        duration = Jam.HoneyCloudberryJamDuration.Value;
                        health = Jam.HoneyCloudberryJamHealth.Value;
                        stamina = Jam.HoneyCloudberryJamStamina.Value;
                        eitr = Jam.HoneyCloudberryJamEtir.Value;
                        regen = Jam.HoneyCloudberryJamRegen.Value;
                        break;
                    case "KingsJam":
                        enabled = Jam.KingsJamEnabled.Value;
                        recipeString = Jam.KingsJamRecipe.Value;
                        duration = Jam.KingsJamDuration.Value;
                        health = Jam.KingsJamHealth.Value;
                        stamina = Jam.KingsJamStamina.Value;
                        eitr = Jam.KingsJamEtir.Value;
                        regen = Jam.KingsJamRegen.Value;
                        break;
                    case "NordicJam":
                        enabled = Jam.NordicJamEnabled.Value;
                        recipeString = Jam.NordicJamRecipe.Value;
                        duration = Jam.NordicJamDuration.Value;
                        health = Jam.NordicJamHealth.Value;
                        stamina = Jam.NordicJamStamina.Value;
                        eitr = Jam.NordicJamEtir.Value;
                        regen = Jam.NordicJamRegen.Value;
                        break;
                    case "MushroomJam":
                        enabled = Jam.MushroomJamEnabled.Value;
                        recipeString = Jam.MushroomJamRecipe.Value;
                        duration = Jam.MushroomJamDuration.Value;
                        health = Jam.MushroomJamHealth.Value;
                        stamina = Jam.MushroomJamStamina.Value;
                        eitr = Jam.MushroomJamEtir.Value;
                        regen = Jam.MushroomJamRegen.Value;
                        break;
                    case "AshlandsJam":
                        enabled = Jam.AshlandsJamEnabled.Value;
                        recipeString = Jam.AshlandsJamRecipe.Value;
                        duration = Jam.AshlandsJamDuration.Value;
                        health = Jam.AshlandsJamHealth.Value;
                        stamina = Jam.AshlandsJamStamina.Value;
                        eitr = Jam.AshlandsJamEtir.Value;
                        regen = Jam.AshlandsJamRegen.Value;
                        break;
                    default:
                        Jam.JamLogger.LogError($"Could not update {jam} configurations. Not a configurable jam.");
                        continue;
                }

                UpdateJamConfigruation(jam, enabled, recipeString, duration, health, stamina, eitr, regen);
            }
        }

        public static void UpdateJamConfigruation(string name, bool enabled,
            string recipe, float duration, float health, float stamina, float etir, float regen)
        {
            // Update Recipe
            Piece.Requirement[] reqs = RecipesHelper.MakeRequirementFromConfig(name, recipe).ToArray();
            Recipe gameRecipe = ObjectDB.instance.m_recipes.FirstOrDefault(x =>
                x.m_item != null &&
                x.m_item.m_itemData.m_dropPrefab != null &&
                x.m_item.m_itemData.m_dropPrefab.name == name);

            if (gameRecipe == null)
            {
                Jam.JamLogger.LogError($"Could not find recipe for item {name}");
            }
            else
            {
                gameRecipe.m_enabled = enabled;
                gameRecipe.m_resources = reqs;
            }

            // Update Item
            GameObject item = PrefabManager.Instance.GetPrefab(name);

            if (item == null)
            {
                Jam.JamLogger.LogError($"Could not find prefab for item {name}");
                return;
            }

            ItemDrop itemDrop = item.GetComponent<ItemDrop>();

            if (itemDrop == null)
            {
                Jam.JamLogger.LogError($"Could not find itemDrop for item {name}");
                return;
            }

            itemDrop.m_itemData.m_shared.m_foodBurnTime = duration;
            itemDrop.m_itemData.m_shared.m_food = health;
            itemDrop.m_itemData.m_shared.m_foodStamina = stamina;
            itemDrop.m_itemData.m_shared.m_foodEitr = etir;
            itemDrop.m_itemData.m_shared.m_foodRegen = regen;

            // Add to Serving Tray
            PieceTable pieceTable = null;
            GameObject trayPrefab = PrefabManager.Instance.GetPrefab("Feaster");

            if (trayPrefab == null || !trayPrefab.TryGetComponent<ItemDrop>(out ItemDrop itemdrop))
            {
                Jam.JamLogger.LogWarning($"Serving Tray not found, will not add build piece.");
                return;
            }

            pieceTable = itemdrop.m_itemData.m_shared.m_buildPieces;
            GameObject pieceTablePiece = pieceTable.m_pieces.Find(x => x.name == Utils.GetPrefabName(item.name));

            if (!enabled && pieceTablePiece != null)
            {
                // Remove existing
                pieceTable.m_pieces.Remove(pieceTablePiece);
            }
            else if (enabled && pieceTablePiece == null)
            {
                // Add new
                pieceTable.m_pieces.Add(item);
            }
        }
    }
}
