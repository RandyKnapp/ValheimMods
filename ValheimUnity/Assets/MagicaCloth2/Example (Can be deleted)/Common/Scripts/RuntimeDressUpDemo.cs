// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Dress-up sample.
    /// </summary>
    public class RuntimeDressUpDemo : MonoBehaviour
    {
        /// <summary>
        /// Avatar to change clothes.
        /// </summary>
        public GameObject targetAvatar;

        /// <summary>
        /// Hair prefab with MagicaCloth set in advance.
        /// </summary>
        public GameObject hariEqupPrefab;

        /// <summary>
        /// Clothes prefab with MagicaCloth set in advance.
        /// </summary>
        public GameObject bodyEquipPrefab;

        //=========================================================================================
        /// <summary>
        /// Bones dictionary of avatars to dress up.
        /// </summary>
        Dictionary<string, Transform> targetAvatarBoneMap = new Dictionary<string, Transform>();

        /// <summary>
        /// Information class for canceling dress-up.
        /// </summary>
        class EquipInfo
        {
            public GameObject equipObject;
            public List<ColliderComponent> colliderList;

            public bool IsValid() => equipObject != null;
        }
        EquipInfo hairEquipInfo = new EquipInfo();
        EquipInfo bodyEquipInfo = new EquipInfo();

        //=========================================================================================
        protected void Start()
        {
            Init();
        }

        //=========================================================================================
        public void OnHairEquipButton()
        {
            if (hairEquipInfo.IsValid())
                Remove(hairEquipInfo);
            else
                Equip(hariEqupPrefab, hairEquipInfo);
        }

        public void OnBodyEquipButton()
        {
            if (bodyEquipInfo.IsValid())
                Remove(bodyEquipInfo);
            else
                Equip(bodyEquipPrefab, bodyEquipInfo);
        }

        //=========================================================================================
        /// <summary>
        /// Create an avatar bone dictionary in advance.
        /// </summary>
        void Init()
        {
            Debug.Assert(targetAvatar);

            // Create all bone maps for the target avatar
            foreach (Transform bone in targetAvatar.GetComponentsInChildren<Transform>())
            {
                if (targetAvatarBoneMap.ContainsKey(bone.name) == false)
                {
                    targetAvatarBoneMap.Add(bone.name, bone);
                }
                else
                {
                    Debug.Log($"Duplicate bone name :{bone.name}");
                }
            }
        }

        /// <summary>
        /// Equip clothes.
        /// </summary>
        /// <param name="equipPrefab"></param>
        /// <param name="einfo"></param>
        void Equip(GameObject equipPrefab, EquipInfo einfo)
        {
            Debug.Assert(equipPrefab);

            // Generate a prefab with cloth set up.
            var gobj = Instantiate(equipPrefab, targetAvatar.transform);

            // All cloth components included in the prefab.
            var clothList = new List<MagicaCloth>(gobj.GetComponentsInChildren<MagicaCloth>());

            // All collider components included in the prefab.
            var colliderList = new List<ColliderComponent>(gobj.GetComponentsInChildren<ColliderComponent>());

            //　All renderers included in the prefab.
            var skinList = new List<SkinnedMeshRenderer>(gobj.GetComponentsInChildren<SkinnedMeshRenderer>());

            // First stop the automatic build that is executed with Start().
            // And just in case, it does some initialization called Awake().
            foreach (var cloth in clothList)
            {
                // Normally it is called with Awake(), but if the component is disabled, it will not be executed, so call it manually.
                // Ignored if already run with Awake().
                cloth.Initialize();

                // Turn off auto-build on Start().
                cloth.DisableAutoBuild();
            }

            // Swap the bones of the SkinnedMeshRenderer.
            // This process is a general dress-up process for SkinnedMeshRenderer.
            // Comment out this series of processes when performing this process with functions such as other assets.
            foreach (var sren in skinList)
            {
                var bones = sren.bones;
                Transform[] newBones = new Transform[bones.Length];

                for (int i = 0; i < bones.Length; ++i)
                {
                    Transform bone = bones[i];
                    if (!targetAvatarBoneMap.TryGetValue(bone.name, out newBones[i]))
                    {
                        // Is the bone the renderer itself?
                        if (bone.name == sren.name)
                        {
                            newBones[i] = sren.transform;
                        }
                        else
                        {
                            // bone not found
                            Debug.Log($"[SkinnedMeshRenderer({sren.name})] Unable to map bone [{bone.name}] to target skeleton.");
                        }
                    }
                }
                sren.bones = newBones;

                // root bone
                if (targetAvatarBoneMap.ContainsKey(sren.rootBone != null ? sren.rootBone.name : null))
                {
                    sren.rootBone = targetAvatarBoneMap[sren.rootBone.name];
                }
            }

            // Here, replace the bones used by the MagicaCloth component.
            foreach (var cloth in clothList)
            {
                // Replaces a component's transform.
                cloth.ReplaceTransform(targetAvatarBoneMap);
            }

            // Move all colliders to the new avatar.
            foreach (var collider in colliderList)
            {
                Transform parent = collider.transform.parent;
                if (parent && targetAvatarBoneMap.ContainsKey(parent.name))
                {
                    Transform newParent = targetAvatarBoneMap[parent.name];

                    // After changing the parent, you need to write back the local posture and align it.
                    collider.transform.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
                    collider.transform.SetParent(newParent);
                    collider.transform.SetLocalPositionAndRotation(localPosition, localRotation);
                }
            }

            // Finally let's start building the cloth component.
            foreach (var cloth in clothList)
            {
                // I disabled the automatic build, so I build it manually.
                cloth.BuildAndRun();
            }

            // Record information for release.
            einfo.equipObject = gobj;
            einfo.colliderList = colliderList;
        }

        /// <summary>
        /// Removes equipped clothing.
        /// </summary>
        /// <param name="einfo"></param>
        void Remove(EquipInfo einfo)
        {
            Destroy(einfo.equipObject);
            foreach (var c in einfo.colliderList)
            {
                Destroy(c.gameObject);
            }

            einfo.equipObject = null;
            einfo.colliderList.Clear();
        }
    }
}
