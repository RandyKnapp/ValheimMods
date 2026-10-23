// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 描画の管理とメッシュ更新マネージャ
    /// </summary>
    public class RenderManager : IManager, IValid
    {
        /// <summary>
        /// 描画データをint型ハンドルで管理する
        /// </summary>
        Dictionary<MagicaObjectId, RenderData> renderDataDict = new Dictionary<MagicaObjectId, RenderData>();

        //=========================================================================================
        // ■RenderData
        //=========================================================================================
        /// <summary>
        /// RenderDataの状態フラグ(32bit)
        /// </summary>
        public const int RenderDataFlag_UseCustomMesh = 0; // カスタムメッシュの利用
        public const int RenderDataFlag_WritePositionNormal = 1; // 座標および法線の書き込み
        public const int RenderDataFlag_WriteBoneWeight = 2; // ボーンウエイトの書き込み
        //public const int RenderDataFlag_ModifyBoneWeight = 3; // ボーンウエイトの変更
        public const int RenderDataFlag_HasMeshTangent = 4; // オリジナルメッシュが接線を持っているかどうか
        public const int RenderDataFlag_HasTangent = 5; // 最終的に接線情報を持っているかどうか
        public const int RenderDataFlag_WriteTangent = 6; // 接線の書き込み
        public const int RenderDataFlag_HasSkinnedMesh = 7;
        public const int RenderDataFlag_HasBoneWeight = 8;

        public struct RenderDataWork : IValid
        {
            /// <summary>
            /// RenderData状態フラグ
            /// </summary>
            public BitField32 flag;

            /// <summary>
            /// バッファへの格納先情報
            /// </summary>
            public DataChunk renderMeshPositionAndNormalChunk;
            public DataChunk renderMeshTangentChunk;
            public DataChunk renderMeshBoneWeightChunk;

            /// <summary>
            /// 制御頂点に設定するボーンウエイト
            /// </summary>
            public BoneWeight centerBoneWeight;

            /// <summary>
            /// 紐づけられているマッピングメッシュデータへのインデックスリスト
            /// </summary>
            public FixedList32Bytes<short> mappingDataIndexList;

            public bool IsValid()
            {
                return renderMeshPositionAndNormalChunk.IsValid;
            }

            public bool UseCustomMesh => flag.IsSet(RenderDataFlag_UseCustomMesh);
            public bool HasMeshTangent => flag.IsSet(RenderDataFlag_HasMeshTangent);
            public bool HasTangent => flag.IsSet(RenderDataFlag_HasTangent);
            public bool HasBoneWeight => flag.IsSet(RenderDataFlag_HasBoneWeight);

            public void AddMappingIndex(int mindex)
            {
                mappingDataIndexList.Add((short)mindex);
            }

            public void RemoveMappingIndex(int mindex)
            {
                mappingDataIndexList.MC2RemoveItemAtSwapBack((short)mindex);
            }
        }
        public ExNativeArray<RenderDataWork> renderDataWorkArray;

        public int RenderDataWorkCount => renderDataWorkArray?.Count ?? 0;

        //=========================================================================================
        // ■RenderMesh
        //=========================================================================================
        public ExNativeArray<float3> renderMeshPositions;
        public ExNativeArray<float3> renderMeshNormals;
        public ExNativeArray<float4> renderMeshTangents;
        public ExNativeArray<BoneWeight> renderMeshBoneWeights;

        bool isValid = false;

        //=========================================================================================
        public void Initialize()
        {
            Dispose();

            // 作業バッファ
            const int capacity = 0;
            const bool create = true;
            renderDataWorkArray = new ExNativeArray<RenderDataWork>(capacity, create);
            renderMeshPositions = new ExNativeArray<float3>(capacity, create);
            renderMeshNormals = new ExNativeArray<float3>(capacity, create);
            renderMeshTangents = new ExNativeArray<float4>(capacity, create);
            renderMeshBoneWeights = new ExNativeArray<BoneWeight>(capacity, create);

            // 更新処理
            MagicaManager.afterDelayedDelegate += PreRenderingUpdate;

            isValid = true;
        }

        public void EnterdEditMode()
        {
            Dispose();
        }

        public void Dispose()
        {
            isValid = false;

            lock (renderDataDict)
            {
                foreach (var rinfo in renderDataDict.Values)
                {
                    rinfo?.Dispose();
                }
            }
            renderDataDict.Clear();

            // 作業バッファ
            renderDataWorkArray?.Dispose();
            renderMeshPositions?.Dispose();
            renderMeshNormals?.Dispose();
            renderMeshTangents?.Dispose();
            renderMeshBoneWeights?.Dispose();
            renderDataWorkArray = null;
            renderMeshPositions = null;
            renderMeshNormals = null;
            renderMeshBoneWeights = null;
            renderMeshTangents = null;

            // 更新処理
            MagicaManager.afterDelayedDelegate -= PreRenderingUpdate;
        }

        public bool IsValid()
        {
            return isValid;
        }

        //=========================================================================================
        /// <summary>
        /// 管理するレンダラーの追加（メインスレッドのみ）
        /// </summary>
        /// <param name="ren"></param>
        /// <returns></returns>
        public MagicaObjectId AddRenderer(
            Renderer ren,
            RenderSetupData referenceSetupData,
            RenderSetupData.UniqueSerializationData referenceUniqueSetupData,
            RenderSetupSerializeData referenceInitSetupData
            )
        {
            if (isValid == false)
                return MagicaObjectId.Invalid;
            Debug.Assert(ren);

            // 制御ハンドル
            MagicaObjectId handle = ren.GetMagicaId();

            lock (renderDataDict)
            {
                if (renderDataDict.ContainsKey(handle) == false)
                {
                    // 新規
                    var rdata = new RenderData();
                    rdata.Initialize(ren, referenceSetupData, referenceUniqueSetupData, referenceInitSetupData);
                    renderDataDict.Add(handle, rdata);
                }

                // 参照カウント+
                renderDataDict[handle].AddReferenceCount();
            }

            return handle;
        }

        public bool RemoveRenderer(MagicaObjectId handle)
        {
            if (isValid == false)
                return false;

            bool delete = false;

            //Debug.Log($"RemoveRenderer:{handle}");
            //Debug.Assert(ren);
            Debug.Assert(renderDataDict.ContainsKey(handle));

            lock (renderDataDict)
            {
                if (renderDataDict.ContainsKey(handle))
                {
                    var rdata = renderDataDict[handle];
                    if (rdata.RemoveReferenceCount() == 0)
                    {
                        // 破棄する
                        rdata.Dispose();

                        renderDataDict.Remove(handle);

                        delete = true;
                    }
                }
            }

            return delete;
        }

        public RenderData GetRendererData(MagicaObjectId handle)
        {
            if (isValid == false)
                return null;

            lock (renderDataDict)
            {
                if (renderDataDict.ContainsKey(handle))
                    return renderDataDict[handle];
                else
                    return null;

            }
        }

        //=========================================================================================
        public int AddRenderDataWork(RenderData rdata)
        {
            if (isValid == false)
                return -1;

            var wdata = new RenderDataWork();

            // オリジナルメッシュの接線情報を確認
            wdata.flag.SetBits(RenderDataFlag_HasMeshTangent, rdata.originalMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent));
            //Debug.Log($"OriginalMesh[{originalMesh.name}] hasTangent:{originalMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent)}");

            // Flag
            wdata.flag.SetBits(RenderDataFlag_HasSkinnedMesh, rdata.HasSkinnedMesh);
            wdata.flag.SetBits(RenderDataFlag_HasBoneWeight, rdata.HasBoneWeight);

            // センタートランスフォーム用ボーンウエイト
            var centerBoneWeight = new BoneWeight();
            centerBoneWeight.boneIndex0 = rdata.setupData.renderTransformIndex;
            centerBoneWeight.weight0 = 1.0f;
            wdata.centerBoneWeight = centerBoneWeight;

            // レンダーメッシュ用バッファ確保
            int vcnt = rdata.originalMesh.vertexCount;
            wdata.renderMeshPositionAndNormalChunk = renderMeshPositions.AddRange(vcnt);
            renderMeshNormals.AddRange(vcnt);
            if (wdata.HasMeshTangent)
                wdata.renderMeshTangentChunk = renderMeshTangents.AddRange(vcnt);
            if (wdata.HasBoneWeight)
            {
                wdata.renderMeshBoneWeightChunk = renderMeshBoneWeights.AddRange(vcnt);
            }

            // 格納
            var c = renderDataWorkArray.Add(wdata);
            return c.startIndex;
        }

        public void RemoveRenderDataWork(int index)
        {
            if (isValid == false)
                return;
            if (index < 0)
                return;

            // バッファ削除
            ref var wdata = ref GetRenderDataWorkRef(index);
            if (wdata.renderMeshPositionAndNormalChunk.IsValid)
            {
                renderMeshPositions.Remove(wdata.renderMeshPositionAndNormalChunk);
                renderMeshNormals.Remove(wdata.renderMeshPositionAndNormalChunk);
            }
            if (wdata.renderMeshTangentChunk.IsValid)
            {
                renderMeshTangents.Remove(wdata.renderMeshTangentChunk);
            }
            if (wdata.renderMeshBoneWeightChunk.IsValid)
            {
                renderMeshBoneWeights.Remove(wdata.renderMeshBoneWeightChunk);
            }
            wdata.renderMeshPositionAndNormalChunk.Clear();
            wdata.renderMeshTangentChunk.Clear();
            wdata.renderMeshBoneWeightChunk.Clear();
            wdata.flag.Clear();

            // 削除
            renderDataWorkArray.RemoveAndFill(new DataChunk(index));
        }

        public ref RenderDataWork GetRenderDataWorkRef(int index)
        {
            return ref renderDataWorkArray.GetRef(index);
        }

        public bool IsSetRenderDataWorkFlag(int index, int flag)
        {
            ref var wdata = ref GetRenderDataWorkRef(index);
            return wdata.flag.IsSet(flag);
        }

        public void SetBitsRenderDataWorkFlag(int index, int flag, bool sw)
        {
            ref var wdata = ref GetRenderDataWorkRef(index);
            wdata.flag.SetBits(flag, sw);
        }

        //=========================================================================================
        /// <summary>
        /// 有効化
        /// </summary>
        /// <param name="handle"></param>
        public void StartUse(ClothProcess cprocess, MagicaObjectId handle)
        {
            GetRendererData(handle)?.StartUse(cprocess);
        }

        /// <summary>
        /// 無効化
        /// </summary>
        /// <param name="handle"></param>
        public void EndUse(ClothProcess cprocess, MagicaObjectId handle)
        {
            GetRendererData(handle)?.EndUse(cprocess);
        }

        //=========================================================================================
        static readonly ProfilerMarker writeMeshTimeProfiler = new ProfilerMarker("WriteMesh");

        /// <summary>
        /// レンダリング前更新
        /// </summary>
        void PreRenderingUpdate()
        {
            if (renderDataDict.Count == 0)
                return;

            // メッシュへの反映
            writeMeshTimeProfiler.Begin();
            foreach (var rdata in renderDataDict.Values)
                rdata?.WriteMesh();
            writeMeshTimeProfiler.End();
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"========== Render Manager ==========");
            if (IsValid() == false)
            {
                sb.AppendLine($"Render Manager. Invalid.");
            }
            else
            {
                sb.AppendLine($"Render Manager. Count({renderDataDict.Count})");
                sb.AppendLine($"  [RenderMeshBuffer]");
                sb.AppendLine($"    -renderMeshPositions:{renderMeshPositions.ToSummary()}");
                sb.AppendLine($"    -renderMeshNormals:{renderMeshNormals.ToSummary()}");
                sb.AppendLine($"    -renderMeshTangents:{renderMeshTangents.ToSummary()}");

                sb.AppendLine($"  [RenderDataWork]");
                sb.AppendLine($"    -Count:{renderDataWorkArray.Count}");
                if (renderDataWorkArray.Count > 0)
                {
                    for (int j = 0; j < renderDataWorkArray.Count; j++)
                    {
                        var wdata = renderDataWorkArray[j];
                        if (wdata.IsValid() == false)
                            continue;
                        sb.AppendLine($"    [{j}] MappingListCount:{wdata.mappingDataIndexList.Length}");
                    }
                }

                sb.AppendLine($"  [RenderData]");
                foreach (var kv in renderDataDict)
                {
                    sb.Append(kv.Value.ToString());
                }
            }
            sb.AppendLine();
            Debug.Log(sb.ToString());
            allsb.Append(sb);
        }
    }
}
