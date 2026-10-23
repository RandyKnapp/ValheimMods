// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 描画対象の管理情報
    /// レンダラーまたはボーンの描画反映を行う
    /// </summary>
    public class RenderData : IDisposable, ITransform
    {
        /// <summary>
        /// 参照カウント。０になると破棄される
        /// </summary>
        public int ReferenceCount { get; private set; }

        /// <summary>
        /// 利用中のプロセス（＝利用カウント）
        /// </summary>
        HashSet<ClothProcess> useProcessSet = new HashSet<ClothProcess>();

        /// <summary>
        /// Meshへの書き込み停止フラグ
        /// </summary>
        bool isSkipWriting;

        //=========================================================================================
        // セットアップデータ
        internal RenderSetupData setupData;
        internal RenderSetupData.UniqueSerializationData preBuildUniqueSerializeData;

        internal string Name => setupData?.name ?? "(empty)";

        internal bool HasSkinnedMesh => setupData?.hasSkinnedMesh ?? false;
        internal bool HasBoneWeight => setupData?.hasBoneWeight ?? false;

        //=========================================================================================
        // オリジナル情報
        internal Mesh originalMesh { get; private set; }
        private Renderer renderer;
        private SkinnedMeshRenderer skinnedMeshRendere;
        private MeshFilter meshFilter;
        internal List<Transform> transformList { get; private set; }
        internal Mesh customMesh { get; private set; }

        // RenderDataWorkバッファへのインデックス(RenderManagerが管理)
        internal int renderDataWorkIndex { get; private set; } = -1;

        //=========================================================================================
        public void Dispose()
        {
            // オリジナルメッシュに戻す
            SwapOriginalMesh(null);

            setupData?.Dispose();
            preBuildUniqueSerializeData = null;

            MagicaManager.Render.RemoveRenderDataWork(renderDataWorkIndex);

            if (customMesh)
                GameObject.Destroy(customMesh);
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
            setupData?.GetUsedTransform(transformSet);
        }

        public void ReplaceTransform(Dictionary<MagicaObjectId, Transform> replaceDict)
        {
            setupData?.ReplaceTransform(replaceDict);
        }

        /// <summary>
        /// 初期化（メインスレッドのみ）
        /// この処理はスレッド化できないので少し負荷がかかるが即時実行する
        /// </summary>
        /// <param name="ren"></param>
        internal void Initialize(
            Renderer ren,
            RenderSetupData referenceSetupData,
            RenderSetupData.UniqueSerializationData referencePreBuildUniqueSetupData,
            RenderSetupSerializeData referenceInitSetupData
            )
        {
            Debug.Assert(ren);

            // セットアップデータ作成
            // PreBuildでは外部から受け渡される
            if (referenceSetupData != null && referencePreBuildUniqueSetupData != null)
            {
                setupData = referenceSetupData;
                preBuildUniqueSerializeData = referencePreBuildUniqueSetupData;

                originalMesh = preBuildUniqueSerializeData.originalMesh;
                renderer = preBuildUniqueSerializeData.renderer;
                skinnedMeshRendere = preBuildUniqueSerializeData.skinRenderer;
                meshFilter = preBuildUniqueSerializeData.meshFilter;
                transformList = preBuildUniqueSerializeData.transformList;
            }
            else
            {
                setupData = new RenderSetupData(referenceInitSetupData, ren);
                preBuildUniqueSerializeData = null;

                originalMesh = setupData.originalMesh;
                renderer = setupData.renderer;
                skinnedMeshRendere = setupData.skinRenderer;
                meshFilter = setupData.meshFilter;
                transformList = setupData.transformList;
            }

            // レンダーデータワークを確保
            renderDataWorkIndex = MagicaManager.Render.AddRenderDataWork(this);
        }

        internal ResultCode Result => setupData?.result ?? ResultCode.None;

        //=========================================================================================
        internal int AddReferenceCount()
        {
            ReferenceCount++;
            return ReferenceCount;
        }

        internal int RemoveReferenceCount()
        {
            ReferenceCount--;
            return ReferenceCount;
        }

        //=========================================================================================
        void SwapCustomMesh(ClothProcess process)
        {
            Debug.Assert(setupData != null);

            if (setupData.IsFaild())
                return;
            if (originalMesh == null)
                return;
            if (MagicaManager.Render.IsSetRenderDataWorkFlag(renderDataWorkIndex, RenderManager.RenderDataFlag_UseCustomMesh))
                return;

            // カスタムメッシュの作成
            if (customMesh == null)
            {
                //Debug.Assert(setupData.originalMesh);
                // クローン作成
                customMesh = GameObject.Instantiate(originalMesh);
                customMesh.MarkDynamic();

                // bind pose
                if (HasBoneWeight)
                {
                    int transformCount = preBuildUniqueSerializeData != null ? preBuildUniqueSerializeData.transformList.Count : setupData.TransformCount;
                    var bindPoseList = new List<Matrix4x4>(transformCount);
                    bindPoseList.AddRange(setupData.bindPoseList);
                    // rootBone/skinning bones
                    while (bindPoseList.Count < transformCount)
                        bindPoseList.Add(Matrix4x4.identity);
                    customMesh.bindposes = bindPoseList.ToArray();

                    // スキニング用ボーンを書き換える
                    // このリストにはオリジナルのスキニングボーン＋レンダラーのトランスフォームが含まれている
                    skinnedMeshRendere.bones = transformList.ToArray();
                }
            }

            // 作業バッファリセット
            ResetCustomMeshWorkData();

            // カスタムメッシュに表示切り替え
            SetMesh(customMesh);
            MagicaManager.Render.SetBitsRenderDataWorkFlag(renderDataWorkIndex, RenderManager.RenderDataFlag_UseCustomMesh, true);

            // Event
            if (process != null && process.cloth)
                process.cloth.OnRendererMeshChange?.Invoke(process.cloth, renderer, true);
        }

        void ResetCustomMeshWorkData()
        {
            var rm = MagicaManager.Render;
            ref var wdata = ref rm.GetRenderDataWorkRef(renderDataWorkIndex);
            int vcnt = setupData.vertexCount;

            // オリジナルデータをコピーする
            if (setupData.HasMeshDataArray)
            {
                var meshData = setupData.meshDataArray[0];
                using var localPositions = new NativeArray<Vector3>(vcnt, Allocator.TempJob);
                using var localNormals = new NativeArray<Vector3>(vcnt, Allocator.TempJob);
                meshData.GetVertices(localPositions);
                meshData.GetNormals(localNormals);
                rm.renderMeshPositions.CopyFrom(localPositions, wdata.renderMeshPositionAndNormalChunk.startIndex, vcnt);
                rm.renderMeshNormals.CopyFrom(localNormals, wdata.renderMeshPositionAndNormalChunk.startIndex, vcnt);
                if (wdata.HasMeshTangent)
                {
                    using var localTangents = new NativeArray<Vector4>(vcnt, Allocator.TempJob);
                    meshData.GetTangents(localTangents);
                    rm.renderMeshTangents.CopyFrom(localTangents, wdata.renderMeshTangentChunk.startIndex, vcnt);
                    wdata.flag.SetBits(RenderManager.RenderDataFlag_HasTangent, true); // 最終的な接線あり
                }
            }
            else
            {
                rm.renderMeshPositions.CopyFrom(setupData.localPositions, wdata.renderMeshPositionAndNormalChunk.startIndex, vcnt);
                rm.renderMeshNormals.CopyFrom(setupData.localNormals, wdata.renderMeshPositionAndNormalChunk.startIndex, vcnt);
                if (wdata.HasMeshTangent && setupData.HasTangent)
                {
                    rm.renderMeshTangents.CopyFrom(setupData.localTangents, wdata.renderMeshTangentChunk.startIndex, vcnt);
                    wdata.flag.SetBits(RenderManager.RenderDataFlag_HasTangent, true); // 最終的な接線あり
                }
            }
            if (HasBoneWeight && wdata.HasBoneWeight)
            {
                using var boneWeights = new NativeArray<BoneWeight>(vcnt, Allocator.TempJob);
                setupData.GetBoneWeightsRun(boneWeights);
                rm.renderMeshBoneWeights.CopyFrom(boneWeights, wdata.renderMeshBoneWeightChunk.startIndex, vcnt);
            }
        }

        /// <summary>
        /// オリジナルメッシュに戻す
        /// </summary>
        void SwapOriginalMesh(ClothProcess process)
        {
            var rm = MagicaManager.Render;

            if (rm.IsSetRenderDataWorkFlag(renderDataWorkIndex, RenderManager.RenderDataFlag_UseCustomMesh) && setupData != null)
            {
                SetMesh(originalMesh);

                if (skinnedMeshRendere != null)
                {
                    skinnedMeshRendere.bones = transformList.ToArray();
                }
            }
            rm.SetBitsRenderDataWorkFlag(renderDataWorkIndex, RenderManager.RenderDataFlag_UseCustomMesh, false);

            // Event
            if (process != null && process.cloth)
                process.cloth.OnRendererMeshChange?.Invoke(process.cloth, renderer, false);
        }

        /// <summary>
        /// レンダラーにメッシュを設定する
        /// </summary>
        /// <param name="mesh"></param>
        void SetMesh(Mesh mesh)
        {
            if (mesh == null)
                return;

            if (setupData != null)
            {
                if (meshFilter != null)
                {
                    meshFilter.mesh = mesh;
                }
                else if (skinnedMeshRendere != null)
                {
                    skinnedMeshRendere.sharedMesh = mesh;
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// 利用の開始
        /// 利用するということはメッシュに頂点を書き込むことを意味する
        /// 通常コンポーネントがEnableになったときに行う
        /// </summary>
        public void StartUse(ClothProcess cprocess)
        {
            UpdateUse(cprocess, 1);
        }

        /// <summary>
        /// 利用の停止
        /// 停止するということはメッシュに頂点を書き込まないことを意味する
        /// 通常コンポーネントがDisableになったときに行う
        /// </summary>
        public void EndUse(ClothProcess cprocess)
        {
            //Debug.Assert(useProcessSet.Count > 0);
            UpdateUse(cprocess, -1);
        }

        internal void UpdateUse(ClothProcess cprocess, int add)
        {
            if (add > 0)
            {
                useProcessSet.Add(cprocess);
            }
            else if (add < 0)
            {
                //Debug.Assert(useProcessSet.Count > 0);
                if (useProcessSet.Contains(cprocess))
                    useProcessSet.Remove(cprocess);
                else
                    return;
            }

            // Invisible状態
            bool invisible = useProcessSet.Any(x => (x.IsCameraCullingInvisible() && x.IsCameraCullingKeep() == false) || x.IsDistanceCullingInvisible());

            // 状態変更
            bool modifyBoneWeight = false;
            if (invisible || useProcessSet.Count == 0)
            {
                // 利用停止
                // オリジナルメッシュに切り替え
                SwapOriginalMesh(cprocess);
            }
            else if (add == 0 && useProcessSet.Count > 0)
            {
                // カリング復帰
                // カスタムメッシュに切り替え、および作業バッファ作成
                // すでにカスタムメッシュが存在する場合は作業バッファのみ再初期化する
                SwapCustomMesh(cprocess);
                modifyBoneWeight = true;
            }
            else if (add > 0 && useProcessSet.Count == 1)
            {
                // 利用開始
                // カスタムメッシュに切り替え、および作業バッファ作成
                // すでにカスタムメッシュが存在する場合は作業バッファのみ再初期化する
                SwapCustomMesh(cprocess);
                modifyBoneWeight = true;
            }
            else if (add != 0)
            {
                // 複数から利用されている状態で１つが停止した。
                // バッファを最初期化する
                ResetCustomMeshWorkData();
                modifyBoneWeight = true;
            }

            // BoneWeight変更を連動するマッピングに指示する
            if (modifyBoneWeight)
            {
                ref var wdata = ref MagicaManager.Render.GetRenderDataWorkRef(renderDataWorkIndex);
                int mcnt = wdata.mappingDataIndexList.Length;
                for (int i = 0; i < mcnt; i++)
                {
                    int mindex = wdata.mappingDataIndexList[i];
                    ref var mdata = ref MagicaManager.Team.GetMappingDataRef(mindex);
                    mdata.flag.SetBits(TeamManager.MappingDataFlag_ModifyBoneWeight, true);
                }
            }

            //Debug.Log($"add:{add}, invisible:{invisible}, useCount:{useProcessSet.Count}, ModifyBoneWeight = {flag.IsSet(Flag_ModifyBoneWeight)}");
        }

        //=========================================================================================
        /// <summary>
        /// Meshへの書き込みフラグを更新する
        /// </summary>
        internal void UpdateSkipWriting()
        {
            isSkipWriting = false;
            foreach (var cprocess in useProcessSet)
            {
                if (cprocess.IsSkipWriting())
                    isSkipWriting = true;
            }
        }

        //=========================================================================================
        internal void WriteMesh()
        {
            var rm = MagicaManager.Render;
            ref var wdata = ref rm.GetRenderDataWorkRef(renderDataWorkIndex);

            if (wdata.UseCustomMesh == false || useProcessSet.Count == 0)
                return;

            // 書き込み停止中ならスキップ
            if (isSkipWriting)
                return;

            //Debug.Log($"WriteMesh [{Name}] ChangePositionNormal:{flag.IsSet(Flag_ChangePositionNormal)}, ChangeBoneWeight:{flag.IsSet(Flag_ChangeBoneWeight)}");

            // メッシュに反映
            if (wdata.flag.IsSet(RenderManager.RenderDataFlag_WritePositionNormal))
            {
                customMesh.SetVertices(rm.renderMeshPositions.GetNativeArray(), wdata.renderMeshPositionAndNormalChunk.startIndex, wdata.renderMeshPositionAndNormalChunk.dataLength);
                customMesh.SetNormals(rm.renderMeshNormals.GetNativeArray(), wdata.renderMeshPositionAndNormalChunk.startIndex, wdata.renderMeshPositionAndNormalChunk.dataLength);
                wdata.flag.SetBits(RenderManager.RenderDataFlag_WritePositionNormal, false);
                //Debug.Log($"[{customMesh.name}] Write Position+Normal");
            }
            if (wdata.flag.IsSet(RenderManager.RenderDataFlag_WriteTangent))
            {
                customMesh.SetTangents(rm.renderMeshTangents.GetNativeArray(), wdata.renderMeshTangentChunk.startIndex, wdata.renderMeshTangentChunk.dataLength);
                wdata.flag.SetBits(RenderManager.RenderDataFlag_WriteTangent, false);
                //Debug.Log($"[{customMesh.name}] Write Tangent");
            }
            if (wdata.flag.IsSet(RenderManager.RenderDataFlag_WriteBoneWeight))
            {
                // BoneWeightはNativeArrayの区間指定ができない
                var boneWeightsSlice = new NativeSlice<BoneWeight>(rm.renderMeshBoneWeights.GetNativeArray(), wdata.renderMeshBoneWeightChunk.startIndex, wdata.renderMeshBoneWeightChunk.dataLength);
                customMesh.boneWeights = boneWeightsSlice.ToArray();
                wdata.flag.SetBits(RenderManager.RenderDataFlag_WriteBoneWeight, false);
                //Debug.Log($"[{customMesh.name}] Write BoneWeight");
            }
        }

        //=========================================================================================
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($">>> [{Name}] ref:{ReferenceCount}, useProcess:{useProcessSet.Count}, HasSkinnedMesh:{HasSkinnedMesh}, HasBoneWeight:{HasBoneWeight}");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
