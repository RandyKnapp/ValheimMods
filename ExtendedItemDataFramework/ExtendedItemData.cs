using System;
using System.Collections.Generic;
using LitJson;
using UnityEngine;

namespace ExtendedItemDataFramework
{
    [Serializable]
    public class BaseExtendedItemComponent
    {
        public string TypeName;
        [NonSerialized]
        public ItemDrop.ItemData ItemData;

        protected BaseExtendedItemComponent(string typeName, ItemDrop.ItemData parent)
        {
            TypeName = typeName;
            ItemData = parent;
        }

        public string Serialize()
        {
            Type type = Type.GetType(TypeName);
            return JsonMapper.ToJson(Convert.ChangeType(this, type));
        }
    }

    public class ExtendedItemData : ItemDrop.ItemData
    {
        public readonly List<BaseExtendedItemComponent> Components = new List<BaseExtendedItemComponent>();

        public ExtendedItemData(ItemDrop.ItemData from)
        {
            m_stack = from.m_stack;
            m_durability = from.m_durability;
            m_quality = from.m_quality;
            m_variant = from.m_variant;
            m_shared = from.m_shared;
            m_crafterID = from.m_crafterID;
            m_crafterName = from.m_crafterName;
            m_gridPos = from.m_gridPos;
            m_equiped = from.m_equiped;
            m_dropPrefab = from.m_dropPrefab;
            m_lastAttackTime = from.m_lastAttackTime;
            m_lastProjectile = from.m_lastProjectile;

            if (from is ExtendedItemData fromExtendedItemData)
            {
                Debug.LogWarning("Copying ExtendedItemData");
                Components.AddRange(fromExtendedItemData.Components);
            }
            else
            {
                Debug.LogWarning("Converting old ItemData to new ExtendedItemData");
                Components.Add(new CrafterNameItemDataComponent(this, from.m_crafterName));
            }

            Save();
        }

        public T GetComponent<T>() where T : class
        {
            foreach (var component in Components)
            {
                var componentT = component as T;
                if (componentT != null)
                {
                    return componentT;
                }
            }

            return null;
        }

        public void Save()
        {
            m_crafterName = JsonMapper.ToJson(Components);
            Debug.LogWarning("Saved Extended Item");
            Debug.Log(m_crafterName);
        }

        public void Load()
        {
            /*Debug.LogWarning("Loading Extended Item");
            var state = "Start";
            JsonReader reader = new JsonReader(m_crafterName);
            while (reader.Read())
            {
                var token = reader.Value.GetType().ToString();
                switch (state)
                {
                    case "Start":
                        if (token == "ArrayStart")
                        {

                        }

                        break;
                }
            }*/
        }
    }
}
