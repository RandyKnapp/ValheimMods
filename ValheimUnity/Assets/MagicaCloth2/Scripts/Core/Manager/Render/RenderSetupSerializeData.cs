// Magica Cloth 2.
// Copyright (c) 2025 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    [System.Serializable]
    public class RenderSetupSerializeData : ITransform
    {
        public RenderSetupData.SetupType setupType;

        public int vertexCount;
        public bool hasSkinnedMesh;
        public bool hasBoneWeight;
        public int skinRootBoneIndex;
        public int renderTransformIndex;
        public int skinBoneCount;
        public int transformCount;
        public int useTransformCount;

        public int[] useTransformIndexArray;

        public Transform[] transformArray;
        public float3[] transformPositions;
        public quaternion[] transformRotations;
        public float3[] transformLocalPositions;
        public quaternion[] transformLocalRotations;
        public float3[] transformScales;

        public float4x4 initRenderLocalToWorld; // 初期化時の基準マトリックス(LtoW)
        public float4x4 initRenderWorldtoLocal; // 初期化時の基準マトリックス(WtoL)
        public quaternion initRenderRotation; // 初期化時の基準回転
        public float3 initRenderScale; // 初期化時の基準スケール

        public Mesh originalMesh; // 変更前のオリジナル共有メッシュ(SkinnedMeshRender利用時のみ)

        public bool DataValidateMeshCloth(Renderer ren)
        {
            if (setupType != RenderSetupData.SetupType.MeshCloth)
                return false;
            if (ren == null)
                return false;

            if (useTransformIndexArray == null || useTransformIndexArray.Length == 0)
                return false;

            if (ren is SkinnedMeshRenderer)
            {
                if (hasSkinnedMesh == false)
                    return false;

                var sren = ren as SkinnedMeshRenderer;
                var smesh = sren.sharedMesh;
                if (smesh == null)
                    return false;

                // 重いか？
                //int bcnt = smesh.bindposes?.Length ?? 0;
                //if (skinBoneCount != bcnt)
                //    return false;
            }
            else
            {
                if (hasSkinnedMesh)
                    return false;

                if (!ren.TryGetComponent<MeshFilter>(out var filter))
                    return false;

                var smesh = filter.sharedMesh;
                if (smesh == null)
                    return false;
            }

            if (DataValidateTransform() == false)
                return false;

            return true;
        }

        public bool DataValidateBoneCloth(ClothSerializeData sdata, RenderSetupData.SetupType clothType)
        {
            if (setupType != clothType)
                return false;
            if (hasSkinnedMesh)
                return false;
            if (hasBoneWeight)
                return false;

            if (DataValidateTransform() == false)
                return false;

            // root bone
            foreach (var rootBone in sdata.rootBones)
            {
                if (rootBone == null || Array.FindIndex(transformArray, x => x == rootBone) < 0)
                    return false;
            }

            return true;
        }

        public bool DataValidateTransform()
        {
            if (transformCount == 0)
                return false;
            if (useTransformCount == 0)
                return false;

            int ucnt = useTransformCount;

            if (ucnt != (useTransformIndexArray?.Length ?? 0))
                return false;
            if (ucnt != (transformArray?.Length ?? 0))
                return false;
            if (Array.FindIndex(transformArray, x => x == null) >= 0)
                return false;

            if (ucnt != (transformPositions?.Length ?? 0))
                return false;
            if (ucnt != (transformRotations?.Length ?? 0))
                return false;
            if (ucnt != (transformLocalPositions?.Length ?? 0))
                return false;
            if (ucnt != (transformLocalRotations?.Length ?? 0))
                return false;
            if (ucnt != (transformScales?.Length ?? 0))
                return false;

            return true;
        }

        public bool Serialize(RenderSetupData sd)
        {
            Debug.Assert(sd != null);
            Debug.Assert(sd.TransformCount > 0);

            setupType = sd.setupType;
            vertexCount = sd.vertexCount;
            hasSkinnedMesh = sd.hasSkinnedMesh;
            hasBoneWeight = sd.hasBoneWeight;
            skinRootBoneIndex = sd.skinRootBoneIndex;
            renderTransformIndex = sd.renderTransformIndex;
            skinBoneCount = sd.skinBoneCount;
            transformCount = sd.TransformCount;
            useTransformCount = 0;
            originalMesh = null;

            if (sd.TransformCount > 0)
            {
                int tcnt = sd.TransformCount;

                using var useTransformIndexList = new NativeList<int>(tcnt, Allocator.TempJob);
                if (setupType == RenderSetupData.SetupType.MeshCloth)
                {
                    // MeshClothではウエイトとして利用されているボーンのみパックする
                    if (hasBoneWeight)
                    {
                        var job = new CalcUseBoneArrayJob2()
                        {
                            boneCount = skinBoneCount,
                            boneWeightArray = sd.boneWeightArray,
                            useBoneIndexList = useTransformIndexList,
                        };
                        job.Run();
                        if (skinRootBoneIndex >= skinBoneCount)
                            useTransformIndexList.Add(skinRootBoneIndex);
                        useTransformIndexList.Add(renderTransformIndex);
                    }
                    else
                    {
                        // 通常メッシュはそのまま
                        for (int i = 0; i < tcnt; i++)
                            useTransformIndexList.Add(i);
                    }

                    // オリジナルメッシュを記録
                    if (sd.originalMesh && sd.originalMesh.name.Contains("(Clone)") == false)
                    {
                        originalMesh = sd.originalMesh;
                    }
                }
                else
                {
                    // BoneCloth系はそのまま
                    for (int i = 0; i < tcnt; i++)
                        useTransformIndexList.Add(i);
                }

                using var tempArray = useTransformIndexList.ToArray(Allocator.TempJob);
                useTransformIndexArray = tempArray.ToArray();

                int ucnt = useTransformIndexArray.Length;

                useTransformCount = ucnt;

                transformArray = new Transform[ucnt];
                transformPositions = new float3[ucnt];
                transformRotations = new quaternion[ucnt];
                transformLocalPositions = new float3[ucnt];
                transformLocalRotations = new quaternion[ucnt];
                transformScales = new float3[ucnt];

                for (int i = 0; i < ucnt; i++)
                {
                    int tindex = useTransformIndexArray[i];

                    transformArray[i] = sd.transformList[tindex];
                    transformPositions[i] = sd.transformPositions[tindex];
                    transformRotations[i] = sd.transformRotations[tindex];
                    transformLocalPositions[i] = sd.transformLocalPositions[tindex];
                    transformLocalRotations[i] = sd.transformLocalRotations[tindex];
                    transformScales[i] = sd.transformScales[tindex];
                }
            }

            initRenderLocalToWorld = sd.initRenderLocalToWorld;
            initRenderWorldtoLocal = sd.initRenderWorldtoLocal;
            initRenderRotation = sd.initRenderRotation;
            initRenderScale = sd.initRenderScale;

            return true;
        }

        [BurstCompile]
        struct CalcUseBoneArrayJob2 : IJob
        {
            public int boneCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<BoneWeight1> boneWeightArray;
            public NativeList<int> useBoneIndexList;

            public void Execute()
            {
                var useBoneArray = new NativeArray<byte>(boneCount, Allocator.Temp);
                int cnt = boneWeightArray.Length;
                int packCnt = 0;
                for (int i = 0; i < cnt; i++)
                {
                    var bw1 = boneWeightArray[i];
                    if (bw1.weight > 0.0f)
                    {
                        byte oldFlag = useBoneArray[bw1.boneIndex];
                        if (oldFlag == 0)
                            packCnt++;
                        useBoneArray[bw1.boneIndex] = 1;
                    }
                }

                // 利用Boneのインデックスをパックする
                for (int i = 0; i < boneCount; i++)
                {
                    byte flag = useBoneArray[i];
                    if (flag != 0)
                    {
                        useBoneIndexList.Add(i);
                    }
                }
                useBoneArray.Dispose();
            }
        }

        public int GetLocalHash()
        {
            int hash = 0;

            hash += (int)setupType * 100;
            hash += vertexCount;
            hash += hasSkinnedMesh ? 1 : 0;
            hash += hasBoneWeight ? 1 : 0;
            hash += skinRootBoneIndex;
            hash += renderTransformIndex;
            hash += skinBoneCount;
            hash += transformCount;
            hash += useTransformCount;

            hash += useTransformIndexArray?.Length ?? 0;
            hash += transformArray?.Length ?? 0;
            hash += transformPositions?.Length ?? 0;
            hash += transformRotations?.Length ?? 0;
            hash += transformLocalPositions?.Length ?? 0;
            hash += transformLocalRotations?.Length ?? 0;
            hash += transformScales?.Length ?? 0;

            if (transformArray != null)
                foreach (var t in transformArray)
                    hash += t != null ? (456 + t.childCount * 789) : 0;

            if (originalMesh != null)
                hash += originalMesh.vertexCount;

            return hash;
        }

        public int GetGlobalHash()
        {
            int hash = 0;

            if (transformArray != null)
            {
                foreach (var t in transformArray)
                {
                    if (t)
                    {
                        hash += t.localPosition.GetHashCode();
                        hash += t.localRotation.GetHashCode();
                        hash += t.localScale.GetHashCode();
                    }
                }
            }

            // レンダーワールドスケールのみ監視する
            // ワールドスケールはちょっとした移動や回転などで浮動小数点誤差が出るので少数３桁まででハッシュ化する
            int3 intScale = (int3)math.round(initRenderScale * 1000);
            //Debug.Log($"★initScale:{intScale}");
            hash += intScale.GetHashCode();

            return hash;
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
            if (transformArray != null)
            {
                foreach (var t in transformArray)
                {
                    if (t)
                        transformSet.Add(t);
                }
            }
        }

        public void ReplaceTransform(Dictionary<MagicaObjectId, Transform> replaceDict)
        {
            if (transformArray == null)
                return;
            for (int i = 0; i < transformArray.Length; i++)
            {
                var t = transformArray[i];
                if (t && replaceDict.ContainsKey(t.GetMagicaId()))
                {
                    transformArray[i] = replaceDict[t.GetMagicaId()];
                }
            }
        }
    }
}
