using UnityEngine;

namespace EpicLoot;

public class TemperRequirement(string prefab, int amount)
{
    public bool isValid => item != null;
    private ItemDrop _item;
    public ItemDrop item
    {
        get
        {
            if (_item != null) return _item;
            if (ObjectDB.instance)
            {
                GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(prefab);
                if (itemPrefab == null)
                {
                    return null;
                }
                if (!itemPrefab.TryGetComponent(out ItemDrop i))
                {
                    return null;
                }
                _item = i;
                return _item;
            }

            return null;
        }   
    }
    public readonly int amount = amount;

    public Piece.Requirement ToPieceRequirement() => new Piece.Requirement()
    {
        m_resItem = item,
        m_amount = amount,
    };
}