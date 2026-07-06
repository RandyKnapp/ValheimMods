using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

public abstract class IncreaseDrop
{
    public string MagicEffect { get; set; }
    public string ZDOVar { get; set; }

    public void DoPrefix(HitData hit)
    {
        if (hit != null)
        {
            Player player = Player.m_localPlayer;
            if (player != null &&
                MagicEffectsHelper.HasActiveMagicEffectOnWeapon(player, player.GetCurrentWeapon(),
                    MagicEffect, out float effectValue) &&
                player.m_nview.GetZDO().GetInt(ZDOVar) != (int)effectValue)
            {
                player.m_nview.GetZDO().Set(ZDOVar, (int)effectValue);
            }
        }
    }

    public void TryDropExtraItems(Character character, DropTable dropTable, Vector3 objPosition)
    {
        if (character is Player)
        {
            int effectValue = (character as Player).m_nview.GetZDO().GetInt(ZDOVar);

            if (effectValue > 0)
            {
                DropExtraItems(dropTable.GetDropList(effectValue), objPosition);
            }
        }
    }

    public void DropExtraItems(List<GameObject> dropList, Vector3 objPosition)
    {
        Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.5f;
        Vector3 position = objPosition + Vector3.up + new Vector3(vector.x, 0, vector.y);
        Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);

        for (int i = 0; i < dropList.Count; i++)
        {
            GameObject drop = dropList[i];
            ItemDrop.OnCreateNew(UnityEngine.Object.Instantiate(drop, position, rotation));
        }
    }
}
