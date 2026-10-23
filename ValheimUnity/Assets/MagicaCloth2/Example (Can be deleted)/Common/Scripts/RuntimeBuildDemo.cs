// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// A sample that builds MagicaCloth at runtime.
    /// </summary>
    public class RuntimeBuildDemo : MonoBehaviour
    {
        [SerializeField]
        private GameObject characterPrefab;
        [SerializeField]
        private MagicaCloth frontHairSource;
        [SerializeField]
        private string ribbonPresetName;
        [SerializeField]
        private string skirtName;
        [SerializeField]
        private Texture2D skirtPaintMap;

        GameObject character;
        GameObjectContainer gameObjectContainer;

        protected void Start()
        {
            character = null;
            gameObjectContainer = null;
        }

        public void OnCreateButton()
        {
            if (character)
                return;

            // Generate a character from a prefab.
            GenerateCharacter();

            // BoneCloth construction example (1).
            SetupHairTail_BoneCloth();

            // BoneCloth construction example (2).
            SetupFrontHair_BoneCloth();

            // BoneCloth construction example (3).
            SetupRibbon_BoneCloth();

            // MeshCloth construction example (1).
            SetupSkirt_MeshCloth();
        }

        public void OnRemoveButton()
        {
            if (character)
            {
                Destroy(character);
                character = null;
                gameObjectContainer = null;
            }
        }

        /// <summary>
        /// Generate a character from a prefab.
        /// A character already contains a<GameObjectContainer> to reference a GameObject.
        /// This component is optional.
        /// It's just there to help with data construction.
        /// </summary>
        void GenerateCharacter()
        {
            if (characterPrefab)
            {
                character = Instantiate(characterPrefab, transform);
                gameObjectContainer = character.GetComponent<GameObjectContainer>();
            }
        }

        /// <summary>
        /// BoneCloth construction example (1).
        /// Set all parameters from a script.
        /// </summary>
        void SetupHairTail_BoneCloth()
        {
            if (character == null)
                return;

            var obj = new GameObject("HairTail_BoneCloth");
            obj.transform.SetParent(character.transform, false);

            // add Magica Cloth
            var cloth = obj.AddComponent<MagicaCloth>();
            var sdata = cloth.SerializeData;

            // bone cloth
            sdata.clothType = ClothProcess.ClothType.BoneCloth;
            sdata.rootBones.Add(gameObjectContainer.GetGameObject("J_L_HairTail_00_B").transform);
            sdata.rootBones.Add(gameObjectContainer.GetGameObject("J_R_HairTail_00_B").transform);

            // setup parameters
            sdata.gravity = 3.0f;
            sdata.damping.SetValue(0.05f);
            sdata.angleRestorationConstraint.stiffness.SetValue(0.15f, 1.0f, 0.15f, true);
            sdata.angleRestorationConstraint.velocityAttenuation = 0.6f;
            sdata.tetherConstraint.distanceCompression = 0.5f;
            sdata.inertiaConstraint.particleSpeedLimit.SetValue(true, 3.0f);
            sdata.colliderCollisionConstraint.mode = ColliderCollisionConstraint.Mode.None;

            // start build
            cloth.BuildAndRun();
        }

        /// <summary>
        /// BoneCloth construction example (2).
        /// Copy parameters from an existing component.
        /// </summary>
        void SetupFrontHair_BoneCloth()
        {
            if (character == null || frontHairSource == null)
                return;

            var obj = new GameObject("HairFront_BoneCloth");
            obj.transform.SetParent(character.transform, false);

            // add Magica Cloth
            var cloth = obj.AddComponent<MagicaCloth>();
            var sdata = cloth.SerializeData;

            // bone cloth
            sdata.clothType = ClothProcess.ClothType.BoneCloth;
            sdata.rootBones.Add(gameObjectContainer.GetGameObject("J_L_HairFront_00_B").transform);
            sdata.rootBones.Add(gameObjectContainer.GetGameObject("J_L_HairSide2_00_B").transform);
            sdata.rootBones.Add(gameObjectContainer.GetGameObject("J_L_HairSide_00_B").transform);
            sdata.rootBones.Add(gameObjectContainer.GetGameObject("J_R_HairFront_00_B").transform);
            sdata.rootBones.Add(gameObjectContainer.GetGameObject("J_R_HairSide2_00_B").transform);
            sdata.rootBones.Add(gameObjectContainer.GetGameObject("J_R_HairSide_00_B").transform);

            // Normal direction setting for backstop
            sdata.normalAlignmentSetting.alignmentMode = NormalAlignmentSettings.AlignmentMode.Transform;
            sdata.normalAlignmentSetting.adjustmentTransform = gameObjectContainer.GetGameObject("HeadCenter").transform;

            // setup parameters
            // Copy from source settings
            sdata.Import(frontHairSource, false);

            // start build
            cloth.BuildAndRun();
        }

        /// <summary>
        /// BoneCloth construction example (3).
        /// Load parameters from saved presets.
        /// </summary>
        void SetupRibbon_BoneCloth()
        {
            if (character == null || string.IsNullOrEmpty(ribbonPresetName))
                return;

            var obj = new GameObject("Ribbon_BoneCloth");
            obj.transform.SetParent(character.transform, false);

            // add Magica Cloth
            var cloth = obj.AddComponent<MagicaCloth>();
            var sdata = cloth.SerializeData;

            // bone cloth
            sdata.clothType = ClothProcess.ClothType.BoneCloth;
            sdata.rootBones.Add(gameObjectContainer.GetGameObject("J_L_HeadRibbon_00_B").transform);
            sdata.rootBones.Add(gameObjectContainer.GetGameObject("J_R_HeadRibbon_00_B").transform);

            // setup parameters
            // Load presets from the Resource folder.
            // Since presets are in TextAssets format, they can also be used as asset bundles.
            var presetText = Resources.Load<TextAsset>(ribbonPresetName);
            sdata.ImportJson(presetText.text);

            // start build
            cloth.BuildAndRun();
        }

        /// <summary>
        /// MeshCloth construction example (1).
        /// Reads vertex attributes from a paintmap.
        /// </summary>
        void SetupSkirt_MeshCloth()
        {
            if (character == null || skirtPaintMap == null)
                return;

            // skirt renderer
            var sobj = gameObjectContainer.GetGameObject(skirtName);
            if (sobj == null)
                return;
            if (!sobj.TryGetComponent<Renderer>(out var skirtRenderer))
                return;

            // add Magica Cloth
            var obj = new GameObject("Skirt_MeshCloth");
            obj.transform.SetParent(character.transform, false);
            var cloth = obj.AddComponent<MagicaCloth>();
            var sdata = cloth.SerializeData;

            // mesh cloth
            sdata.clothType = ClothProcess.ClothType.MeshCloth;
            sdata.sourceRenderers.Add(skirtRenderer);

            // reduction settings
            sdata.reductionSetting.simpleDistance = 0.0212f;
            sdata.reductionSetting.shapeDistance = 0.0244f;

            // paint map settings
            // *** Paintmaps must have Read/Write attributes enabled! ***
            sdata.paintMode = ClothSerializeData.PaintMode.Texture_Fixed_Move;
            sdata.paintMaps.Add(skirtPaintMap);

            // setup parameters
            sdata.gravity = 1.0f;
            sdata.damping.SetValue(0.03f);
            sdata.angleRestorationConstraint.stiffness.SetValue(0.05f, 1.0f, 0.5f, true);
            sdata.angleRestorationConstraint.velocityAttenuation = 0.5f;
            sdata.angleLimitConstraint.useAngleLimit = true;
            sdata.angleLimitConstraint.limitAngle.SetValue(45.0f, 0.0f, 1.0f, true);
            sdata.distanceConstraint.stiffness.SetValue(0.5f, 1.0f, 0.5f, true);
            sdata.tetherConstraint.distanceCompression = 0.9f;
            sdata.inertiaConstraint.depthInertia = 0.7f;
            sdata.inertiaConstraint.movementSpeedLimit.SetValue(true, 3.0f);
            sdata.inertiaConstraint.particleSpeedLimit.SetValue(true, 3.0f);
            sdata.colliderCollisionConstraint.mode = ColliderCollisionConstraint.Mode.Point;

            // setup collider
            // UpperLeg L
            var lobj = new GameObject("CapsuleCollider_L");
            lobj.transform.SetParent(gameObjectContainer.GetGameObject("Character1_LeftUpLeg").transform);
            lobj.transform.localPosition = new Vector3(0.0049f, 0.0f, -0.0832f);
            lobj.transform.localEulerAngles = new Vector3(0.23f, 16.376f, -0.028f);
            var colliderL = lobj.AddComponent<MagicaCapsuleCollider>();
            colliderL.direction = MagicaCapsuleCollider.Direction.Z;
            colliderL.SetSize(0.082f, 0.094f, 0.3f);
            // UpperLeg R (Symmetry)
            colliderL.symmetryMode = ColliderSymmetryMode.AutomaticHumanBody;
            colliderL.UpdateParameters(); // Required when changing parameters.
            sdata.colliderCollisionConstraint.colliderList.Add(colliderL);

            // start build
            cloth.BuildAndRun();
        }
    }
}
