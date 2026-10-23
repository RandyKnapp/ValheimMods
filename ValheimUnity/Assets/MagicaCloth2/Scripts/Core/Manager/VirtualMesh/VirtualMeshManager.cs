// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// VirtualMeshの管理マネージャ
    /// </summary>
    public class VirtualMeshManager : IManager, IValid
    {
        //=========================================================================================
        // ■ProxyMesh
        //=========================================================================================
        // ■共通
        /// <summary>
        /// 対応するチームID
        /// </summary>
        public ExNativeArray<short> teamIds;

        /// <summary>
        /// 頂点属性
        /// </summary>
        public ExNativeArray<VertexAttribute> attributes;

        /// <summary>
        /// 頂点ごとの接続トライアングルインデックスと法線接線フリップフラグ（最大７つ）
        /// これは法線を再計算するために用いられるもので７つあれば十分であると判断したもの。
        /// そのため正確なトライアングル接続を表していない。
        /// データは12-20bitのuintでパックされている
        /// 12(hi) = 法線接線のフリップフラグ(法線:0x1,接線:0x2)。ONの場合フリップ。
        /// 20(low) = トライアングルインデックス。
        /// </summary>
        public ExNativeArray<FixedList32Bytes<uint>> vertexToTriangles;

        /// <summary>
        /// 頂点ごとの接続頂点インデックス
        /// ※現在未使用
        /// </summary>
        //public ExNativeArray<uint> vertexToVertexIndexArray;
        //public ExNativeArray<ushort> vertexToVertexDataArray;

        /// <summary>
        /// 頂点ごとのバインドポーズ
        /// 頂点バインドにはスケール値は不要
        /// </summary>
        public ExNativeArray<float3> vertexBindPosePositions;
        public ExNativeArray<quaternion> vertexBindPoseRotations;

        /// <summary>
        /// 各頂点の深さ(0.0-1.0)
        /// </summary>
        public ExNativeArray<float> vertexDepths;

        /// <summary>
        /// 各頂点のルートインデックス(-1=なし)
        /// </summary>
        public ExNativeArray<int> vertexRootIndices;

        /// <summary>
        /// 各頂点の親からの基準ローカル座標
        /// </summary>
        public ExNativeArray<float3> vertexLocalPositions;

        /// <summary>
        /// 各頂点の親からの基準ローカル回転
        /// </summary>
        public ExNativeArray<quaternion> vertexLocalRotations;

        /// <summary>
        /// 各頂点の親頂点インデックス(-1=なし)
        /// </summary>
        public ExNativeArray<int> vertexParentIndices;

        /// <summary>
        /// 各頂点の子頂点インデックスリスト
        /// </summary>
        public ExNativeArray<uint> vertexChildIndexArray;
        public ExNativeArray<ushort> vertexChildDataArray;

        /// <summary>
        /// 法線調整用回転
        /// </summary>
        public ExNativeArray<quaternion> normalAdjustmentRotations;

        /// <summary>
        /// 各頂点の角度計算用のローカル回転
        /// pitch/yaw個別制限はv1.0では実装しないので一旦停止させる
        /// </summary>
        //public ExNativeArray<quaternion> vertexAngleCalcLocalRotations;

        /// <summary>
        /// UV
        /// VirtualMeshのUVはTangent計算用でありテクスチャマッピング用ではないので注意！
        /// </summary>
        public ExNativeArray<float2> uv;


        public int ProxyVertexCount => teamIds?.Count ?? 0;

        // ■トライアングル -----------------------------------------------------
        public ExNativeArray<short> triangleTeamIdArray;

        /// <summary>
        /// トライアングル頂点インデックス
        /// </summary>
        public ExNativeArray<int3> triangles;

        /// <summary>
        /// トライアングル法線
        /// </summary>
        public ExNativeArray<float3> triangleNormals;

        /// <summary>
        /// トライアングル接線
        /// </summary>
        public ExNativeArray<float3> triangleTangents;

        public int ProxyTriangleCount => triangles?.Count ?? 0;

        // ■エッジ -------------------------------------------------------------
        public ExNativeArray<short> edgeTeamIdArray;

        /// <summary>
        /// エッジ頂点インデックス
        /// </summary>
        public ExNativeArray<int2> edges;

        /// <summary>
        /// エッジ固有フラグ(VirtualMesh.EdgeFlag_~)
        /// </summary>
        public ExNativeArray<ExBitFlag8> edgeFlags;

        public int ProxyEdgeCount => edges?.Count ?? 0;

        // ■ベースライン -------------------------------------------------------
        /// <summary>
        /// ベースラインごとのフラグ
        /// </summary>
        public ExNativeArray<ExBitFlag8> baseLineFlags;

        /// <summary>
        /// ベースラインごとのチームID
        /// </summary>
        public ExNativeArray<short> baseLineTeamIds;

        /// <summary>
        /// ベースラインごとのデータ開始インデックス
        /// </summary>
        public ExNativeArray<ushort> baseLineStartDataIndices;

        /// <summary>
        /// ベースラインごとのデータ数
        /// </summary>
        public ExNativeArray<ushort> baseLineDataCounts;

        /// <summary>
        /// ベースラインデータ（頂点インデックス）
        /// </summary>
        public ExNativeArray<ushort> baseLineData;

        public int ProxyBaseLineCount => baseLineFlags?.Count ?? 0;

        // ■メッシュ基本(共通) -------------------------------------------------
        public ExNativeArray<float3> localPositions;
        public ExNativeArray<float3> localNormals;
        public ExNativeArray<float3> localTangents;
        public ExNativeArray<VirtualMeshBoneWeight> boneWeights;
        public ExNativeArray<int> skinBoneTransformIndices;
        public ExNativeArray<float4x4> skinBoneBindPoses;

        public int ProxyLocalPositionCount => localPositions?.Count ?? 0;

        // ■BoneClothのみ -----------------------------------------------------
        public ExNativeArray<quaternion> vertexToTransformRotations;

        // ■最終頂点姿勢
        public ExNativeArray<float3> positions;
        public ExNativeArray<quaternion> rotations;

        //=========================================================================================
        // ■MappingMesh
        //=========================================================================================
        public ExNativeArray<short> mappingIdArray; // (+1)されているので注意！
        public ExNativeArray<int> mappingReferenceIndices;
        public ExNativeArray<VertexAttribute> mappingAttributes;
        public ExNativeArray<float3> mappingLocalPositins;
        public ExNativeArray<float3> mappingLocalNormals;
        public ExNativeArray<float3> mappingLocalTangents;
        public ExNativeArray<VirtualMeshBoneWeight> mappingBoneWeights;
#if MC2_DEBUG
        public ExNativeArray<float3> mappingPositions;
        //public ExNativeArray<float3> mappingNormals;
        //public ExNativeArray<float3> mappingTangents;
#endif

        public int MappingVertexCount => mappingIdArray?.Count ?? 0;

        //=========================================================================================
        bool isValid = false;

        //=========================================================================================
        public void Dispose()
        {
            isValid = false;

            // 作業バッファ
            teamIds?.Dispose();
            attributes?.Dispose();
            vertexToTriangles?.Dispose();
            //vertexToVertexIndexArray?.Dispose();
            //vertexToVertexDataArray?.Dispose();
            vertexBindPosePositions?.Dispose();
            vertexBindPoseRotations?.Dispose();
            vertexDepths?.Dispose();
            vertexRootIndices?.Dispose();
            vertexLocalPositions?.Dispose();
            vertexLocalRotations?.Dispose();
            vertexParentIndices?.Dispose();
            vertexChildIndexArray?.Dispose();
            vertexChildDataArray?.Dispose();
            normalAdjustmentRotations?.Dispose();
            //vertexAngleCalcLocalRotations?.Dispose();
            uv?.Dispose();
            teamIds = null;
            attributes = null;
            vertexToTriangles = null;
            //vertexToVertexIndexArray = null;
            //vertexToVertexDataArray = null;
            vertexBindPosePositions = null;
            vertexBindPoseRotations = null;
            vertexDepths = null;
            vertexRootIndices = null;
            vertexLocalPositions = null;
            vertexLocalRotations = null;
            vertexParentIndices = null;
            vertexChildIndexArray = null;
            vertexChildDataArray = null;
            normalAdjustmentRotations = null;
            //vertexAngleCalcLocalRotations = null;
            uv = null;

            triangleTeamIdArray?.Dispose();
            triangles?.Dispose();
            triangleNormals?.Dispose();
            triangleTangents?.Dispose();
            triangleTeamIdArray = null;
            triangles = null;
            triangleNormals = null;
            triangleTangents = null;

            edgeTeamIdArray?.Dispose();
            edges?.Dispose();
            edgeFlags?.Dispose();
            edgeTeamIdArray = null;
            edges = null;
            edgeFlags = null;

            positions?.Dispose();
            rotations?.Dispose();
            positions = null;
            rotations = null;

            baseLineFlags?.Dispose();
            baseLineTeamIds?.Dispose();
            baseLineStartDataIndices?.Dispose();
            baseLineDataCounts?.Dispose();
            baseLineData?.Dispose();
            baseLineFlags = null;
            baseLineTeamIds = null;
            baseLineStartDataIndices = null;
            baseLineDataCounts = null;
            baseLineData = null;

            localPositions?.Dispose();
            localNormals?.Dispose();
            localTangents?.Dispose();
            boneWeights?.Dispose();
            skinBoneTransformIndices?.Dispose();
            skinBoneBindPoses?.Dispose();
            localPositions = null;
            localNormals = null;
            localTangents = null;
            boneWeights = null;
            skinBoneTransformIndices = null;
            skinBoneBindPoses = null;

            vertexToTransformRotations?.Dispose();
            vertexToTransformRotations = null;

            mappingIdArray?.Dispose();
            mappingReferenceIndices?.Dispose();
            mappingAttributes?.Dispose();
            mappingLocalPositins?.Dispose();
            mappingLocalNormals?.Dispose();
            mappingLocalTangents?.Dispose();
            mappingBoneWeights?.Dispose();
#if MC2_DEBUG
            mappingPositions?.Dispose();
            //mappingNormals?.Dispose();
            //mappingTangents?.Dispose();
#endif
            mappingIdArray = null;
            mappingReferenceIndices = null;
            mappingAttributes = null;
            mappingLocalPositins = null;
            mappingLocalNormals = null;
            mappingLocalTangents = null;
            mappingBoneWeights = null;
        }

        public void EnterdEditMode()
        {
            Dispose();
        }

        public void Initialize()
        {
            Dispose();

            // 作業バッファ
            const int capacity = 0;
            const bool create = true;
            teamIds = new ExNativeArray<short>(capacity, create);
            attributes = new ExNativeArray<VertexAttribute>(capacity, create);
            vertexToTriangles = new ExNativeArray<FixedList32Bytes<uint>>(capacity, create);
            //vertexToVertexIndexArray = new ExNativeArray<uint>(capacity, create);
            //vertexToVertexDataArray = new ExNativeArray<ushort>(capacity, create);
            vertexBindPosePositions = new ExNativeArray<float3>(capacity, create);
            vertexBindPoseRotations = new ExNativeArray<quaternion>(capacity, create);
            vertexDepths = new ExNativeArray<float>(capacity, create);
            vertexRootIndices = new ExNativeArray<int>(capacity, create);
            vertexLocalPositions = new ExNativeArray<float3>(capacity, create);
            vertexLocalRotations = new ExNativeArray<quaternion>(capacity, create);
            vertexParentIndices = new ExNativeArray<int>(capacity, create);
            vertexChildIndexArray = new ExNativeArray<uint>(capacity, create);
            vertexChildDataArray = new ExNativeArray<ushort>(capacity, create);
            normalAdjustmentRotations = new ExNativeArray<quaternion>(capacity, create);
            //vertexAngleCalcLocalRotations = new ExNativeArray<quaternion>(capacity, create);
            uv = new ExNativeArray<float2>(capacity, create);

            triangleTeamIdArray = new ExNativeArray<short>(capacity, create);
            triangles = new ExNativeArray<int3>(capacity, create);
            triangleNormals = new ExNativeArray<float3>(capacity, create);
            triangleTangents = new ExNativeArray<float3>(capacity, create);

            edgeTeamIdArray = new ExNativeArray<short>(capacity, create);
            edges = new ExNativeArray<int2>(capacity, create);
            edgeFlags = new ExNativeArray<ExBitFlag8>(capacity, create);

            positions = new ExNativeArray<float3>(capacity, create);
            rotations = new ExNativeArray<quaternion>(capacity, create);

            baseLineFlags = new ExNativeArray<ExBitFlag8>(capacity, create);
            baseLineTeamIds = new ExNativeArray<short>(capacity, create);
            baseLineStartDataIndices = new ExNativeArray<ushort>(capacity, create);
            baseLineDataCounts = new ExNativeArray<ushort>(capacity, create);
            baseLineData = new ExNativeArray<ushort>(capacity, create);

            localPositions = new ExNativeArray<float3>(capacity, create);
            localNormals = new ExNativeArray<float3>(capacity, create);
            localTangents = new ExNativeArray<float3>(capacity, create);
            boneWeights = new ExNativeArray<VirtualMeshBoneWeight>(capacity, create);
            skinBoneTransformIndices = new ExNativeArray<int>(capacity, create);
            skinBoneBindPoses = new ExNativeArray<float4x4>(capacity, create);

            vertexToTransformRotations = new ExNativeArray<quaternion>(capacity, create);

            mappingIdArray = new ExNativeArray<short>(capacity, create);
            mappingReferenceIndices = new ExNativeArray<int>(capacity, create);
            mappingAttributes = new ExNativeArray<VertexAttribute>(capacity, create);
            mappingLocalPositins = new ExNativeArray<float3>(capacity, create);
            mappingLocalNormals = new ExNativeArray<float3>(capacity, create);
            mappingLocalTangents = new ExNativeArray<float3>(capacity, create);
            mappingBoneWeights = new ExNativeArray<VirtualMeshBoneWeight>(capacity, create);
#if MC2_DEBUG
            mappingPositions = new ExNativeArray<float3>(capacity, create);
            //mappingNormals = new ExNativeArray<float3>(capacity, create);
            //mappingTangents = new ExNativeArray<float3>(capacity, create);
#endif

            isValid = true;
        }

        public bool IsValid()
        {
            return isValid;
        }

        //=========================================================================================
        /// <summary>
        /// プロキシメッシュをマネージャに登録する
        /// </summary>
        public void RegisterProxyMesh(int teamId, VirtualMeshContainer proxyMeshContainer)
        {
            if (isValid == false)
                return;

            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
            var proxyMesh = proxyMeshContainer.shareVirtualMesh;

            // mesh type
            tdata.proxyMeshType = proxyMesh.meshType;

            // Transform
            tdata.proxyTransformChunk = MagicaManager.Bone.AddTransform(proxyMeshContainer, teamId);

            // center transform
            tdata.centerTransformIndex = proxyMesh.centerTransformIndex + tdata.proxyTransformChunk.startIndex;

            // 作業用バッファ
            // 共通
            int vcnt = proxyMesh.VertexCount;
            tdata.proxyCommonChunk = teamIds.AddRange(vcnt, (short)teamId);
            attributes.AddRange(proxyMesh.attributes);
            vertexToTriangles.AddRange(proxyMesh.vertexToTriangles);
            //vertexToVertexIndexArray.AddRange(proxyMesh.vertexToVertexIndexArray);
            vertexBindPosePositions.AddRange(proxyMesh.vertexBindPosePositions);
            vertexBindPoseRotations.AddRange(proxyMesh.vertexBindPoseRotations);
            vertexDepths.AddRange(proxyMesh.vertexDepths);
            vertexRootIndices.AddRange(proxyMesh.vertexRootIndices);
            vertexLocalPositions.AddRange(proxyMesh.vertexLocalPositions);
            vertexLocalRotations.AddRange(proxyMesh.vertexLocalRotations);
            vertexParentIndices.AddRange(proxyMesh.vertexParentIndices);
            vertexChildIndexArray.AddRange(proxyMesh.vertexChildIndexArray);
            normalAdjustmentRotations.AddRange(proxyMesh.normalAdjustmentRotations);
            //vertexAngleCalcLocalRotations.AddRange(proxyMesh.vertexAngleCalcLocalRotations);
            uv.AddRange(proxyMesh.uv);
            positions.AddRange(vcnt);
            rotations.AddRange(vcnt);

            // 頂点接続データ
            //tdata.proxyVertexToVertexDataChunk = vertexToVertexDataArray.AddRange(proxyMesh.vertexToVertexDataArray);

            // 子頂点データ
            tdata.proxyVertexChildDataChunk = vertexChildDataArray.AddRange(proxyMesh.vertexChildDataArray);

            // トライアングル
            if (proxyMesh.TriangleCount > 0)
            {
                tdata.proxyTriangleChunk = triangleTeamIdArray.AddRange(proxyMesh.TriangleCount, (short)teamId);
                triangles.AddRange(proxyMesh.triangles);
                triangleNormals.AddRange(proxyMesh.TriangleCount);
                triangleTangents.AddRange(proxyMesh.TriangleCount);
            }

            // エッジ（エッジは利用時のみ）
            if (proxyMesh.EdgeCount > 0)
            {
                tdata.proxyEdgeChunk = edgeTeamIdArray.AddRange(proxyMesh.EdgeCount, (short)teamId);
                edges.AddRange(proxyMesh.edges);
                edgeFlags.AddRange(proxyMesh.edgeFlags);
            }

            // ベースライン
            if (proxyMesh.BaseLineCount > 0)
            {
                tdata.baseLineChunk = baseLineFlags.AddRange(proxyMesh.baseLineFlags);
                baseLineStartDataIndices.AddRange(proxyMesh.baseLineStartDataIndices);
                baseLineDataCounts.AddRange(proxyMesh.baseLineDataCounts);
                baseLineTeamIds.AddRange(proxyMesh.BaseLineCount, (short)teamId);

                tdata.baseLineDataChunk = baseLineData.AddRange(proxyMesh.baseLineData);
            }

            // メッシュ基本
            tdata.proxyMeshChunk = localPositions.AddRange(proxyMesh.localPositions);
            localNormals.AddRange(proxyMesh.localNormals);
            localTangents.AddRange(proxyMesh.localTangents);
            boneWeights.AddRange(proxyMesh.boneWeights);

            // スキニング
            tdata.proxySkinBoneChunk = skinBoneTransformIndices.AddRange(proxyMesh.skinBoneTransformIndices);
            skinBoneBindPoses.AddRange(proxyMesh.skinBoneBindPoses);

            // BoneClothのみ
            if (proxyMesh.meshType == VirtualMesh.MeshType.ProxyBoneMesh)
            {
                tdata.proxyBoneChunk = vertexToTransformRotations.AddRange(proxyMesh.vertexToTransformRotations);
            }

            // Lineデータの有無を記録
            tdata.flag.SetBits(TeamManager.Flag_ProxyMeshLine, proxyMesh.LineCount > 0);
            //Develop.DebugLog($"Proxy[{proxyMesh.name}] Line:{proxyMesh.LineCount > 0}");

            // 頂点間の最大距離
            //tdata.maxVertexDistance = proxyMesh.maxVertexDistance.Value;
            //Debug.Log($"maxVertexDistance:{proxyMesh.maxVertexDistance.Value}");
            //Debug.Log($"averageVertexDistance:{proxyMesh.averageVertexDistance.Value}");
        }

        /// <summary>
        /// プロキシメッシュをマネージャから解除する
        /// </summary>
        public void ExitProxyMesh(int teamId)
        {
            if (isValid == false)
                return;

            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);

            // Transform
            MagicaManager.Bone.RemoveTransform(tdata.proxyTransformChunk);
            tdata.proxyTransformChunk.Clear();

            // 作業用バッファ
            teamIds.RemoveAndFill(tdata.proxyCommonChunk); // 0で埋める
            attributes.RemoveAndFill(tdata.proxyCommonChunk); // 0で埋める
            vertexToTriangles.Remove(tdata.proxyCommonChunk);
            //vertexToVertexIndexArray.Remove(tdata.proxyCommonChunk);
            vertexBindPosePositions.Remove(tdata.proxyCommonChunk);
            vertexBindPoseRotations.Remove(tdata.proxyCommonChunk);
            vertexDepths.Remove(tdata.proxyCommonChunk);
            vertexRootIndices.Remove(tdata.proxyCommonChunk);
            vertexLocalPositions.Remove(tdata.proxyCommonChunk);
            vertexLocalRotations.Remove(tdata.proxyCommonChunk);
            vertexParentIndices.Remove(tdata.proxyCommonChunk);
            vertexChildIndexArray.Remove(tdata.proxyCommonChunk);
            normalAdjustmentRotations.Remove(tdata.proxyCommonChunk);
            //vertexAngleCalcLocalRotations.Remove(tdata.proxyCommonChunk);
            uv.Remove(tdata.proxyCommonChunk);
            positions.Remove(tdata.proxyCommonChunk);
            rotations.Remove(tdata.proxyCommonChunk);
            tdata.proxyCommonChunk.Clear();

            //vertexToVertexDataArray.Remove(tdata.proxyVertexToVertexDataChunk);
            //tdata.proxyVertexToVertexDataChunk.Clear();

            vertexChildDataArray.Remove(tdata.proxyVertexChildDataChunk);
            tdata.proxyVertexChildDataChunk.Clear();

            triangleTeamIdArray.RemoveAndFill(tdata.proxyTriangleChunk, 0); // 0で埋める
            triangles.Remove(tdata.proxyTriangleChunk);
            triangleNormals.Remove(tdata.proxyTriangleChunk);
            triangleTangents.Remove(tdata.proxyTriangleChunk);
            tdata.proxyTriangleChunk.Clear();

            edgeTeamIdArray.RemoveAndFill(tdata.proxyEdgeChunk, 0); // 0で埋める
            edges.Remove(tdata.proxyEdgeChunk);
            edgeFlags.Remove(tdata.proxyEdgeChunk);

            baseLineFlags.RemoveAndFill(tdata.baseLineChunk); // 0で埋める
            baseLineTeamIds.Remove(tdata.baseLineChunk);
            baseLineStartDataIndices.Remove(tdata.baseLineChunk);
            baseLineDataCounts.Remove(tdata.baseLineChunk);
            tdata.baseLineChunk.Clear();

            baseLineData.Remove(tdata.baseLineDataChunk);
            tdata.baseLineDataChunk.Clear();

            localPositions.Remove(tdata.proxyMeshChunk);
            localNormals.Remove(tdata.proxyMeshChunk);
            localTangents.Remove(tdata.proxyMeshChunk);
            boneWeights.Remove(tdata.proxyMeshChunk);
            tdata.proxyMeshChunk.Clear();

            skinBoneTransformIndices.Remove(tdata.proxySkinBoneChunk);
            skinBoneBindPoses.Remove(tdata.proxySkinBoneChunk);
            tdata.proxySkinBoneChunk.Clear();

            vertexToTransformRotations.Remove(tdata.proxyBoneChunk);
            tdata.proxyBoneChunk.Clear();

            // 同時に連動するマッピングメッシュも解放する
            var mappingList = MagicaManager.Team.teamMappingIndexArray[teamId];
            int mcnt = mappingList.Length;
            for (int i = 0; i < mcnt; i++)
            {
                int mappingIndex = mappingList[i];
                ExitMappingMesh(teamId, mappingIndex);
            }
        }

        //=========================================================================================
        /// <summary>
        /// マッピングメッシュをマネージャに登録する（チームにも登録される）
        /// </summary>
        /// <param name="cbase"></param>
        /// <param name="mappingMesh"></param>
        /// <returns></returns>
        public DataChunk RegisterMappingMesh(
            int teamId,
            VirtualMeshContainer mappingMeshContainer,
            int renderDataWorkIndex
            )
        {
            if (isValid == false)
                return DataChunk.Empty;

            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
            ref var mappingList = ref MagicaManager.Team.GetTeamMappingRef(teamId);

            var mdata = new TeamManager.MappingData();
            mdata.teamId = teamId;

            var mappingMesh = mappingMeshContainer.shareVirtualMesh;

            // transform
            var ct = mappingMeshContainer.GetCenterTransform();
            var c = MagicaManager.Bone.AddTransform(ct, new ExBitFlag8(TransformManager.Flag_Read | TransformManager.Flag_Enable), teamId);
            mdata.centerTransformIndex = c.startIndex;

            // プロキシメッシュへの変換
            mdata.toProxyMatrix = mappingMesh.toProxyMatrix;
            mdata.toProxyRotation = mappingMesh.toProxyRotation;

            // 登録インデックスが必要なので先に登録する
            c = MagicaManager.Team.mappingDataArray.Add(mdata);
            int mappingIndex = c.startIndex;

            // 基本データ
            int vcnt = mappingMesh.VertexCount;
            mdata.mappingCommonChunk = mappingIdArray.AddRange(vcnt, (short)(mappingIndex + 1)); // (+1)されるので注意！

            mappingReferenceIndices.AddRange(mappingMesh.referenceIndices);
            mappingAttributes.AddRange(mappingMesh.attributes);
            mappingLocalPositins.AddRange(mappingMesh.localPositions);
            mappingLocalNormals.AddRange(mappingMesh.localNormals);
            mappingLocalTangents.AddRange(mappingMesh.localTangents);
            mappingBoneWeights.AddRange(mappingMesh.boneWeights);
#if MC2_DEBUG
            mappingPositions.AddRange(vcnt);
            //mappingNormals.AddRange(vcnt);
            //mappingTangents.AddRange(vcnt);
#endif

            // RenderMeshWorkデータと紐づけ
            mdata.renderDataWorkIndex = renderDataWorkIndex;
            ref var wdata = ref MagicaManager.Render.GetRenderDataWorkRef(renderDataWorkIndex);
            wdata.AddMappingIndex(mappingIndex);

            // 再登録
            MagicaManager.Team.mappingDataArray[mappingIndex] = mdata;
            mappingList.MC2Set((short)mappingIndex);

            // vmeshにも記録する
            mappingMesh.mappingId = mappingIndex;

            Develop.DebugLog($"RegisterMappingMesh. team:{teamId}, mappingIndex:{mappingIndex}");

            // マッピングメッシュの登録チャンクを返す
            return mdata.mappingCommonChunk;
        }

        public void ExitMappingMesh(int teamId, int mappingIndex)
        {
            if (isValid == false)
                return;

            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
            ref var mdata = ref MagicaManager.Team.mappingDataArray.GetRef(mappingIndex);
            ref var mappingList = ref MagicaManager.Team.GetTeamMappingRef(teamId);

            // Transform解放
            MagicaManager.Bone.RemoveTransform(new DataChunk(mdata.centerTransformIndex, 1));

            // 作業バッファ解放
            mappingIdArray.RemoveAndFill(mdata.mappingCommonChunk, 0);
            mappingReferenceIndices.Remove(mdata.mappingCommonChunk);
            mappingAttributes.Remove(mdata.mappingCommonChunk);
            mappingLocalPositins.Remove(mdata.mappingCommonChunk);
            mappingLocalNormals.Remove(mdata.mappingCommonChunk);
            mappingLocalTangents.Remove(mdata.mappingCommonChunk);
            mappingBoneWeights.Remove(mdata.mappingCommonChunk);
#if MC2_DEBUG
            mappingPositions.Remove(mdata.mappingCommonChunk);
            //mappingNormals.Remove(mdata.mappingCommonChunk);
            //mappingTangents.Remove(mdata.mappingCommonChunk);
#endif

            // RenderMeshWorkデータとの紐づけ解除
            ref var wdata = ref MagicaManager.Render.GetRenderDataWorkRef(mdata.renderDataWorkIndex);
            wdata.RemoveMappingIndex(mappingIndex);

            // チームから削除する
            mappingList.MC2RemoveItemAtSwapBack((short)mappingIndex);

            MagicaManager.Team.mappingDataArray.RemoveAndFill(new DataChunk(mappingIndex, 1));

            Develop.DebugLog($"ExitMappingMesh. team:{teamId}, mappingIndex:{mappingIndex}");
        }

        //=========================================================================================
        // Simulation
        //=========================================================================================
        /// <summary>
        /// プロキシメッシュの頂点スキニングを行い座標・法線・接線を求める
        /// [BoneCloth][MeshCloth]兼用
        /// 姿勢はワールド座標で格納される
        /// </summary>
        internal static void SimulationPreProxyMeshUpdate(
            DataChunk chunk,
            // team
            int teamId,
            ref TeamManager.TeamData tdata,

            // vmesh
            in NativeArray<VertexAttribute> attributes,
            in NativeArray<float3> localPositions,
            in NativeArray<float3> localNormals,
            in NativeArray<float3> localTangents,
            in NativeArray<VirtualMeshBoneWeight> boneWeights,
            in NativeArray<int> skinBoneTransformIndices,
            in NativeArray<float4x4> skinBoneBindPoses,
            ref NativeArray<float3> positions,
            ref NativeArray<quaternion> rotations,

            // transform
            in NativeArray<float4x4> transformLocalToWorldMatrixArray
            )
        {
            var pc = tdata.proxyCommonChunk;
            if (pc.dataLength == 0)
                return;

            // ProxyMeshをスキニングして頂点姿勢を求める
            //int vindex = pc.startIndex;
            int vindex = pc.startIndex + chunk.startIndex;
            //int mvindex = tdata.proxyMeshChunk.startIndex;
            int mvindex = tdata.proxyMeshChunk.startIndex + chunk.startIndex;
            int sb_start = tdata.proxySkinBoneChunk.startIndex;
            int t_start = tdata.proxyTransformChunk.startIndex;
            float4x3 wpose = 0;
            float4x3 _pose = 0;
            float4x3 _lpose = 0;
            //for (int k = 0; k < pc.dataLength; k++, vindex++, mvindex++)
            for (int k = 0; k < chunk.dataLength; k++, vindex++, mvindex++)
            {
                var bw = boneWeights[mvindex];
                int wcnt = bw.Count;

                wpose = 0;
                _pose = 0;

                _lpose.c0 = new float4(localPositions[mvindex], 1);
                _lpose.c1 = new float4(localNormals[mvindex], 0);
                _lpose.c2 = new float4(localTangents[mvindex], 0);

                for (int i = 0; i < wcnt; i++)
                {
                    float w = bw.weights[i];

                    _pose = _lpose;
                    //Debug.Log($"[{mvindex}] lpos:{lpos}, lnor:{lnor}, ltan:{ltan}");

                    // ボーンローカル空間に変換
                    int l_boneIndex = bw.boneIndices[i];
                    float4x4 bp = skinBoneBindPoses[sb_start + l_boneIndex];
                    _pose = math.mul(bp, _pose);

                    // 現在のワールド空間に変換
                    int tindex = skinBoneTransformIndices[sb_start + l_boneIndex] + t_start;
                    var lw = transformLocalToWorldMatrixArray[tindex];
                    _pose = math.mul(lw, _pose);

                    // ウエイト
                    wpose += _pose * w;
                }

                float3 wpos = wpose.c0.xyz;
                float3 wnor = wpose.c1.xyz;
                float3 wtan = wpose.c2.xyz;

                // バインドポーズにスケールが入るので単位化する必要がある
#if MC2_DEBUG
                Develop.Assert(math.length(wnor) > 0.0f);
                Develop.Assert(math.length(wtan) > 0.0f);
#endif
                wnor = math.normalize(wnor);
                wtan = math.normalize(wtan);
                var wrot = MathUtility.ToRotation(wnor, wtan);

                positions[vindex] = wpos;
                rotations[vindex] = wrot;

                //Debug.Log($"[{teamId}] pv:{vindex}, wpos:{wpos}");
            }
        }

        /// <summary>
        /// クロスシミュレーションの結果をProxyMeshへ反映させる
        /// ラインがある場合はベースラインごとに姿勢を整える
        /// </summary>
        internal static void SimulationPostProxyMeshUpdateLine(
            DataChunk chunk,
            // team
            ref TeamManager.TeamData tdata,
            ref ClothParameters param,
            // vmesh
            ref NativeArray<VertexAttribute> attributes,
            ref NativeArray<float3> positions,
            ref NativeArray<quaternion> rotations,
            ref NativeArray<float3> vertexLocalPositions,
            ref NativeArray<quaternion> vertexLocalRotations,
            ref NativeArray<uint> vertexChildIndexArray,
            ref NativeArray<ushort> vertexChildDataArray,
            ref NativeArray<ExBitFlag8> baseLineFlags,
            ref NativeArray<ushort> baseLineStartIndices,
            ref NativeArray<ushort> baseLineDataCounts,
            ref NativeArray<ushort> baseLineData,
            // temp
            ref NativeArray<float3> tempVectorBufferA,
            ref NativeArray<quaternion> tempRotationBufferA
            )
        {
            // ラインがある場合はベースラインごとに姿勢を整える
            // ただしLineが存在しないメッシュはスキップする。トライアングルベースの回転制御にすべて上書きされるため。
            if (tdata.baseLineChunk.IsValid && tdata.flag.IsSet(TeamManager.Flag_ProxyMeshLine))
            {
                // parameter
                float averageRate = param.rotationalInterpolation; // 回転平均化割合
                float rootInterpolation = param.rootRotation;
                float animeRatio = tdata.animationPoseRatio;
                float blendWeight = tdata.blendWeight;

                int s_vindex = tdata.proxyCommonChunk.startIndex;
                int s_pindex = tdata.particleChunk.startIndex;
                int s_dataIndex = tdata.baseLineDataChunk.startIndex;
                int s_childDataIndex = tdata.proxyVertexChildDataChunk.startIndex;

                int bindex = tdata.baseLineChunk.startIndex + chunk.startIndex;
                for (int k = 0; k < chunk.dataLength; k++, bindex++)
                {
                    // ラインを含む場合のみ実行する
                    var bflag = baseLineFlags[bindex];
                    if (bflag.IsSet(VirtualMesh.BaseLineFlag_IncludeLine) == false)
                        continue;

                    // ベースラインをルートから走査する
                    int dataIndex = baseLineStartIndices[bindex] + s_dataIndex;
                    int dataCnt = baseLineDataCounts[bindex];
                    for (int i = 0; i < dataCnt; i++, dataIndex++)
                    {
                        // 自身を親とする
                        int lindex = baseLineData[dataIndex];
                        int vindex = lindex + s_vindex;
                        int pindex = lindex + s_pindex;
                        var pos = positions[vindex];
                        var rot = rotations[vindex];
                        var attr = attributes[vindex];

                        //Debug.Log($"p:[{vindex}] rot:[{rot}] wn:{MathUtility.ToNormal(rot)}, wt:{MathUtility.ToTangent(rot)}");

                        // 子の情報
                        var pack = vertexChildIndexArray[vindex];
                        int cstart = DataUtility.Unpack12_20Low(pack);
                        int ccnt = DataUtility.Unpack12_20Hi(pack);

                        // ★Ver2.17.0より変更。以下の問題を修正
                        // BoneClothの制御Transformがアニメーションにより姿勢制御されている場合に最終回転が全体的におかしくなる
                        // これは回転計算の元をアニメーション姿勢ではなく初期姿勢に固定しているため
                        // そのためBlendWeight=0にしてもオリジナルの姿勢に戻らなくなる
                        // またベクトルのゼロ距離判定を強化

                        // 現在のベース姿勢
                        var basePos = tempVectorBufferA[pindex];
                        var baseRot = tempRotationBufferA[pindex];
                        var baseInvRot = math.inverse(baseRot);

                        if (ccnt > 0 && attr.IsInvalid() == false)
                        {
                            // 子への平均ベクトル
                            float3 ctv = 0;
                            float3 cv = 0;

                            // 自身を基準に子の回転を求める、また子への平均ベクトルを加算する
                            for (int j = 0; j < ccnt; j++)
                            {
                                int clindex = vertexChildDataArray[s_childDataIndex + cstart + j];
                                int cvindex = clindex + s_vindex;
                                int cpindex = clindex + s_pindex;

                                // 子の属性
                                var cattr = attributes[cvindex];

                                // このゼロ距離状態
                                bool isC0 = cattr.IsSet(VertexAttribute.Flag_ZeroDistance);

                                // 子の座標
                                var cpos = positions[cvindex];

                                // 子の現在のベース姿勢とローカル姿勢
                                var cbasePos = tempVectorBufferA[cpindex];
                                var cbaseRot = tempRotationBufferA[cpindex];
                                float3 cbaseLocalPos = math.mul(baseInvRot, cbasePos - basePos);
                                quaternion cbaseLocalRot = math.mul(baseInvRot, cbaseRot);

                                // 計算基準のローカル位置とローカル回転
                                // AnimationPoseRatioにより補間
                                float3 lpos = math.lerp(vertexLocalPositions[cvindex] * tdata.negativeScaleDirection, cbaseLocalPos, animeRatio);
                                quaternion lrot = math.slerp(vertexLocalRotations[cvindex].value * tdata.negativeScaleQuaternionValue, cbaseLocalRot, animeRatio);

                                // 子の本来のベクトル
                                float3 tv = isC0 ? 0.0f : math.mul(rot, lpos);

                                ctv += tv;

                                // 子の現在ベクトル
                                if (cattr.IsMove())
                                {
                                    float3 v = cpos - pos;
                                    cv += v;

                                    bool isV0 = MathUtility.IsZeroDistance(v);

                                    // 子の回転を決定
                                    // この時点で子は親の方向(1.0)の回転となる
                                    var crot = math.mul(rot, lrot);

                                    // 現在のベクトル変位による補正
                                    if(isC0 == false)
                                    {
                                        var q = MathUtility.FromToRotation(tv, v);
                                        crot = math.mul(q, crot);
                                    }

                                    rotations[cvindex] = crot;
                                }
                                else
                                {
                                    cv += tv;
                                }
                            }

                            // 子の方向への回転調整
                            float t = attr.IsMove() ? averageRate : rootInterpolation;
                            bool isCtv0 = MathUtility.IsZeroDistance(ctv);
                            bool isCv0 = MathUtility.IsZeroDistance(cv);
                            var cq = (isCtv0 || isCv0) ? quaternion.identity : MathUtility.FromToRotation(ctv, cv, t);
                            rot = math.mul(cq, rot);
                        }

                        // ブレンドウエイト適用
                        rot = math.slerp(baseRot, rot, blendWeight);

                        // 自身の姿勢を確定させる
                        rotations[vindex] = rot;
                    }
                }
            }
        }

        /// <summary>
        /// クロスシミュレーションの結果をProxyMeshへ反映させる
        /// トライアングルの法線と接線を求める
        /// </summary>
        internal static void SimulationPostProxyMeshUpdateTriangle(
            DataChunk chunk,
            // team
            ref TeamManager.TeamData tdata,
            // vmesh
            ref NativeArray<float3> positions,
            ref NativeArray<int3> triangles,
            ref NativeArray<float3> triangleNormals,
            ref NativeArray<float3> triangleTangents,
            ref NativeArray<float2> uvs
            )
        {

            // トライアングルがある場合はトライアングル接続情報から最終的な姿勢を求める
            if (tdata.TriangleCount > 0)
            {
                // トライアングルの法線と接線を求める
                // 座標系の変換は行わない
                //int tindex = tdata.proxyTriangleChunk.startIndex;
                int tindex = tdata.proxyTriangleChunk.startIndex + chunk.startIndex;
                //for (int k = 0; k < tdata.proxyTriangleChunk.dataLength; k++, tindex++)
                for (int k = 0; k < chunk.dataLength; k++, tindex++)
                {
                    int3 tri = triangles[tindex];

                    // トライアングル法線を求める
                    int start = tdata.proxyCommonChunk.startIndex;
                    var pos1 = positions[start + tri.x];
                    var pos2 = positions[start + tri.y];
                    var pos3 = positions[start + tri.z];
                    float3 cross = math.cross(pos2 - pos1, pos3 - pos1);
                    float len = math.length(cross);
                    if (len > Define.System.Epsilon)
                    {
                        float3 nor = cross / len;

                        // マイナススケール
                        nor *= tdata.negativeScaleTriangleSign.x;

                        triangleNormals[tindex] = nor;
                    }
#if MC2_DEBUG
                    else
                        Debug.LogWarning("CalcTriangleNormalTangentJob.normal = 0!");
#endif

                    // トライアングル接線を求める
                    var uv1 = uvs[start + tri.x];
                    var uv2 = uvs[start + tri.y];
                    var uv3 = uvs[start + tri.z];
                    var tan = MathUtility.TriangleTangent(pos1, pos2, pos3, uv1, uv2, uv3);
                    if (math.lengthsq(tan) > 0.0f)
                    {
                        // マイナススケール
                        tan *= tdata.negativeScaleTriangleSign.y;

                        triangleTangents[tindex] = tan;
                    }
#if MC2_DEBUG
                    else
                        Debug.LogWarning("CalcTriangleNormalTangentJob.tangent = 0!");
#endif
                }
            }
        }

        /// <summary>
        /// クロスシミュレーションの結果をProxyMeshへ反映させる
        /// トライアングルの法線接線から頂点法線接線を平均化して求める
        /// </summary>
        internal static void SimulationPostProxyMeshUpdateTriangleSum(
            DataChunk chunk,
            // team
            ref TeamManager.TeamData tdata,
            // vmesh
            ref NativeArray<quaternion> rotations,
            ref NativeArray<float3> triangleNormals,
            ref NativeArray<float3> triangleTangents,
            ref NativeArray<FixedList32Bytes<uint>> vertexToTriangles,
            ref NativeArray<quaternion> normalAdjustmentRotations
            )
        {
            // トライアングルがある場合はトライアングル接続情報から最終的な姿勢を求める
            if (tdata.TriangleCount > 0)
            {
                // トライアングルの法線接線から頂点法線接線を平均化して求める
                // （ワールド座標空間）
                //int vindex = tdata.proxyCommonChunk.startIndex;
                int vindex = tdata.proxyCommonChunk.startIndex + chunk.startIndex;
                //for (int k = 0; k < tdata.proxyCommonChunk.dataLength; k++, vindex++)
                for (int k = 0; k < chunk.dataLength; k++, vindex++)
                {
                    var tlist = vertexToTriangles[vindex];
                    if (tlist.Length > 0)
                    {
                        float3 nor = 0;
                        float3 tan = 0;
                        for (int i = 0; i < tlist.Length; i++)
                        {
                            // 12-20bitのパックで格納されている
                            // 12(hi) = 法線と接線のフリップフラグ
                            // 20(low) = トライアングルインデックス
                            uint data = tlist[i];
                            int flipFlag = DataUtility.Unpack12_20Hi(data);
                            int tindex = DataUtility.Unpack12_20Low(data);

                            tindex += tdata.proxyTriangleChunk.startIndex;
                            nor += triangleNormals[tindex] * ((flipFlag & 0x1) == 0 ? 1 : -1);
                            tan += triangleTangents[tindex] * ((flipFlag & 0x2) == 0 ? 1 : -1);
                        }
                        //Debug.Log($"Vertex:{vindex} nor:{nor}, tan:{tan}");

                        // 法線０を考慮する。法線を０にするとポリゴンが欠けるため
                        float ln = math.length(nor);
                        float lt = math.length(tan);
                        if (ln > 1e-06f && lt > 1e-06f)
                        {
                            nor = nor / ln;
                            tan = tan / lt;

                            float dot = math.dot(nor, tan);
                            if (dot != 1.0f && dot != -1.0f)
                            {
                                // トライアングル回転は従法線から算出するように変更(v2.1.7)
                                float3 binor = math.normalize(math.cross(nor, tan));
                                var rot = quaternion.LookRotation(binor, nor);

                                // 法線調整用回転を乗算する（不要な場合は単位回転が入っている）
                                // マイナススケール
                                rot = math.mul(rot, normalAdjustmentRotations[vindex].value * tdata.negativeScaleQuaternionValue);
                                //rot = math.mul(rot, normalAdjustmentRotations[vindex]); // オリジナル

                                rotations[vindex] = rot;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// クロスシミュレーションの結果をProxyMeshへ反映させる
        /// BoneClothの場合は頂点姿勢から連動するトランスフォームのワールド姿勢を計算する
        /// </summary>
        internal static void SimulationPostProxyMeshUpdateWorldTransform(
            DataChunk chunk,
            // team
            ref TeamManager.TeamData tdata,
            // vmesh
            ref NativeArray<float3> positions,
            ref NativeArray<quaternion> rotations,
            ref NativeArray<quaternion> vertexToTransformRotations,
            // transform
            ref NativeArray<float3> transformPositionArray,
            ref NativeArray<quaternion> transformRotationArray
            )
        {
            // Transformパーティクル
            if (tdata.proxyMeshType == VirtualMesh.MeshType.ProxyBoneMesh)
            {
                // Transformパーティクルの場合はvertexToTransform回転を乗算してTransformDataに情報を書き戻す
                //int vindex = tdata.proxyCommonChunk.startIndex;
                int vindex = tdata.proxyCommonChunk.startIndex + chunk.startIndex;
                //for (int k = 0; k < tdata.proxyCommonChunk.dataLength; k++, vindex++)
                for (int k = 0; k < chunk.dataLength; k++, vindex++)
                {
                    // pos/rotはワールド空間
                    var pos = positions[vindex];
                    var rot = rotations[vindex];

                    // 本来のTransformの姿勢を求める回転を掛ける
                    //int boneIndex = tdata.proxyBoneChunk.startIndex + k;
                    int boneIndex = tdata.proxyBoneChunk.startIndex + chunk.startIndex + k;
                    quaternion v2t = vertexToTransformRotations[boneIndex];

                    // マイナススケール
                    rot = math.mul(rot, v2t.value * tdata.negativeScaleQuaternionValue);
                    //rot = math.mul(rot, v2t); // オリジナル

                    // ワールド姿勢
                    //int tindex = tdata.proxyTransformChunk.startIndex + k;
                    int tindex = tdata.proxyTransformChunk.startIndex + chunk.startIndex + k;
                    transformPositionArray[tindex] = pos;
                    transformRotationArray[tindex] = rot;
                }
            }
        }

        /// <summary>
        /// BoneClothの場合はTransformのローカル姿勢を計算する
        /// </summary>
        internal static void SimulationPostProxyMeshUpdateLocalTransform(
            // team
            ref TeamManager.TeamData tdata,
            // vmesh
            ref NativeArray<VertexAttribute> attributes,
            ref NativeArray<int> parentIndices,
            // transform
            ref NativeArray<float3> transformPositionArray,
            ref NativeArray<quaternion> transformRotationArray,
            ref NativeArray<float3> transformScaleArray,
            ref NativeArray<float3> transformLocalPositionArray,
            ref NativeArray<quaternion> transformLocalRotationArray
            )
        {
            // Transformパーティクル
            if (tdata.proxyMeshType == VirtualMesh.MeshType.ProxyBoneMesh)
            {
                // Transformパーティクルは親からのローカル姿勢を計算してTransformData情報に書き込む
                int vindex = tdata.proxyCommonChunk.startIndex;
                //vindex = tdata.proxyCommonChunk.startIndex + chunk.startIndex;
                for (int k = 0; k < tdata.proxyCommonChunk.dataLength; k++, vindex++)
                //for (int k = 0; k < chunk.dataLength; k++, vindex++)
                {
                    int parentIndex = parentIndices[vindex];
                    if (parentIndex < 0)
                        continue;

                    var attr = attributes[vindex];
                    if (attr.IsMove() == false)
                        continue;

                    // 親からのローカル姿勢を計算しトランスフォーム情報に書き込む
                    int tindex = tdata.proxyTransformChunk.startIndex + k;
                    //int tindex = tdata.proxyTransformChunk.startIndex + chunk.startIndex + k;
                    int ptindex = tdata.proxyTransformChunk.startIndex + parentIndex;
                    var ppos = transformPositionArray[ptindex];
                    var prot = transformRotationArray[ptindex];
                    var pscl = transformScaleArray[ptindex];
                    var pos = transformPositionArray[tindex];
                    var rot = transformRotationArray[tindex];

#if true
                    var iprot = math.inverse(prot);
                    var v = pos - ppos;
                    var lpos = math.mul(iprot, v);

                    //Develop.Assert(pscl.x > 0.0f && pscl.y > 0.0f && pscl.z > 0.0f);
                    lpos /= pscl;
                    var lrot = math.mul(iprot, rot);
#endif

                    // マイナススケール
                    lrot = lrot.value * tdata.negativeScaleQuaternionValue;

                    transformLocalPositionArray[tindex] = lpos;
                    transformLocalRotationArray[tindex] = lrot;
                }
            }
        }

        /// <summary>
        /// マッピングメッシュの頂点姿勢を連動するプロキシメッシュからスキニングして求める
        /// </summary>
        internal JobHandle PostMappingMeshUpdateBatchSchedule(JobHandle jobHandle, int workerCount)
        {
            if (MagicaManager.Team.MappingCount == 0)
                return jobHandle;

            var tm = MagicaManager.Team;
            var bm = MagicaManager.Bone;
            var rm = MagicaManager.Render;

            // マッピングメッシュの変換マトリックスを求める
            var calcMeshConvert_A_Job = new CalcMeshConvert_A_Job()
            {
                // team
                teamDataArray = tm.teamDataArray.GetNativeArray(),
                // transform
                transformPositionArray = bm.positionArray.GetNativeArray(),
                transformRotationArray = bm.rotationArray.GetNativeArray(),
                transformScaleArray = bm.scaleArray.GetNativeArray(),
                // mapping
                mappingDataArray = tm.mappingDataArray.GetNativeArray(),
                // render mesh
                renderDataWorkArray = rm.renderDataWorkArray.GetNativeArray(),
            };
            jobHandle = calcMeshConvert_A_Job.Schedule(tm.MappingCount, 1, jobHandle);

            // マッピングメッシュの頂点姿勢をプロキシメッシュから逆スキニングして求める
            // マッピングメッシュの頂点姿勢を書き込み用バッファに書き込む
            // 必要があればボーンウエイトも書き込む
            var calcMeshConvert_B_Job = new CalcMeshConvert_B_Job()
            {
                workerCount = workerCount,
                // team
                teamDataArray = tm.teamDataArray.GetNativeArray(),
                // mapping
                mappingDataArray = tm.mappingDataArray.GetNativeArray(),
                //mappingIdArray = mappingIdArray.GetNativeArray(),
                mappingAttributes = mappingAttributes.GetNativeArray(),
                mappingLocalPositions = mappingLocalPositins.GetNativeArray(),
                mappingLocalNormals = mappingLocalNormals.GetNativeArray(),
                mappingLocalTangents = mappingLocalTangents.GetNativeArray(),
                mappingBoneWeights = mappingBoneWeights.GetNativeArray(),
#if MC2_DEBUG
                mappingPositions = mappingPositions.GetNativeArray(),
                //mappingNormals = mappingNormals.GetNativeArray(),
                //mappingTangents = mappingTangents.GetNativeArray(),
#endif
                mappingReferenceIndices = mappingReferenceIndices.GetNativeArray(),
                // proxy
                proxyPositions = positions.GetNativeArray(),
                proxyRotations = rotations.GetNativeArray(),
                proxyVertexBindPosePositions = vertexBindPosePositions.GetNativeArray(),
                proxyVertexBindPoseRotations = vertexBindPoseRotations.GetNativeArray(),
                // render mesh
                renderDataWorkArray = rm.renderDataWorkArray.GetNativeArray(),
                renderMeshPositions = rm.renderMeshPositions.GetNativeArray(),
                renderMeshNormals = rm.renderMeshNormals.GetNativeArray(),
                renderMeshTangents = rm.renderMeshTangents.GetNativeArray(),
                renderMeshBoneWeights = rm.renderMeshBoneWeights.GetNativeArray(),
            };
            jobHandle = calcMeshConvert_B_Job.Schedule(tm.MappingCount * workerCount, 1, jobHandle);

            // レンダーメッシュデータの後処理
            var postRenderDataJob = new PostRenderMeshWorkDataBatchJob()
            {
                // render mesh
                renderDataWorkArray = rm.renderDataWorkArray.GetNativeArray(),
                // mapping data
                mappingDataArray = tm.mappingDataArray.GetNativeArray(),
            };
            jobHandle = postRenderDataJob.Schedule(rm.RenderDataWorkCount, 8, jobHandle);

            return jobHandle;
        }

        /// <summary>
        /// プロキシメッシュからマッピングメッシュへの変換マトリックスを求める
        /// </summary>
        [BurstCompile]
        struct CalcMeshConvert_A_Job : IJobParallelFor
        {
            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // transform
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScaleArray;

            // mapping
            public NativeArray<TeamManager.MappingData> mappingDataArray;

            // render mesh
            [Unity.Collections.ReadOnly]
            public NativeArray<RenderManager.RenderDataWork> renderDataWorkArray;

            // マッピングメッシュごと
            public void Execute(int index)
            {
                var mdata = mappingDataArray[index];
                if (mdata.IsValid() == false)
                    return;

                var tdata = teamDataArray[mdata.teamId];
                if (tdata.IsProcess == false)
                    return;

                // RenderMeshWorkData
                var wdata = renderDataWorkArray[mdata.renderDataWorkIndex];
                if (wdata.UseCustomMesh == false)
                    return;

                //=======================================================================
                // ■マッピングメッシュとプロキシメッシュの座標変換マトリックスを求める
                {
                    // mapping
                    var pos = transformPositionArray[mdata.centerTransformIndex];
                    var rot = transformRotationArray[mdata.centerTransformIndex];
                    var scl = transformScaleArray[mdata.centerTransformIndex];
                    var irot = math.inverse(rot);

                    // proxy
                    var ppos = transformPositionArray[tdata.centerTransformIndex];
                    var prot = transformRotationArray[tdata.centerTransformIndex];
                    var pscl = transformScaleArray[tdata.centerTransformIndex];

                    // プロキシメッシュとマッピングメッシュの座標空間が等しいか判定
                    bool sameSpace = MathUtility.CompareTransform(pos, rot, scl, ppos, prot, pscl);
                    mdata.sameSpace = sameSpace;
                    //Debug.Log($"sameSpace:{sameSpace}, scl:{scl}, pscl:{pscl}");

                    // ワールド空間からマッピングメッシュへの座標空間変換
                    mdata.toMappingMatrix = math.inverse(MathUtility.LocalToWorldMatrix(pos, rot, scl));
                    mdata.toMappingRotation = irot;

                    // マッピングメッシュ用のスケール比率
                    // チームのステップ実行とは無関係に毎フレーム適用する必要があるためチームスケール比率と分離する
                    var initScaleLength = math.length(tdata.initScale);
                    Develop.Assert(initScaleLength > 0.0f);
                    mdata.scaleRatio = math.length(pscl) / initScaleLength;
                }

                //=======================================================================
                // ■マッピングメッシュメッシュ頂点姿勢をプロキシメッシュから逆スキニングして求める
                // 接線の有無
                bool useTangent = tdata.IsTangent;
                // ボーンウエイトの書き込み
                bool modifyBoneWeight = wdata.HasBoneWeight && mdata.flag.IsSet(TeamManager.MappingDataFlag_ModifyBoneWeight);

                //=======================================================================
                // ■結果格納
                mdata.flag.SetBits(TeamManager.MappingDataFlag_ChangePositionNormal, true);
                mdata.flag.SetBits(TeamManager.MappingDataFlag_ChangeTangent, useTangent);
                mdata.flag.SetBits(TeamManager.MappingDataFlag_ChangeBoneWeight, modifyBoneWeight);
                mappingDataArray[index] = mdata;
            }
        }

        /// <summary>
        /// マッピングメッシュの頂点姿勢をプロキシメッシュから逆スキニングして求める
        /// マッピングメッシュの頂点姿勢を書き込み用バッファに書き込む
        /// 必要があればボーンウエイトも書き込む
        /// </summary>
        [BurstCompile]
        struct CalcMeshConvert_B_Job : IJobParallelFor
        {
            public int workerCount;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // mapping
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.MappingData> mappingDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> mappingAttributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> mappingLocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> mappingLocalNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> mappingLocalTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> mappingBoneWeights;
#if MC2_DEBUG
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> mappingPositions;
#endif
            [Unity.Collections.ReadOnly]
            public NativeArray<int> mappingReferenceIndices;

            // proxy mesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> proxyPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> proxyRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> proxyVertexBindPosePositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> proxyVertexBindPoseRotations;

            // render mesh
            [Unity.Collections.ReadOnly]
            public NativeArray<RenderManager.RenderDataWork> renderDataWorkArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> renderMeshPositions;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> renderMeshNormals;
            [NativeDisableParallelForRestriction]
            public NativeArray<float4> renderMeshTangents;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<BoneWeight> renderMeshBoneWeights;

            // マッピングメッシュごと
            public void Execute(int dataIndex)
            {
                // チームIDとワーカーID
                int localIndex = dataIndex / workerCount;
                int workerIndex = dataIndex % workerCount;

                var mdata = mappingDataArray[localIndex];
                if (mdata.IsValid() == false)
                    return;

                var tdata = teamDataArray[mdata.teamId];
                if (tdata.IsProcess == false)
                    return;

                // RenderMeshWorkData
                var wdata = renderDataWorkArray[mdata.renderDataWorkIndex];
                if (wdata.UseCustomMesh == false)
                    return;

                // 範囲
                var chunk = MathUtility.GetWorkerChunk(mdata.mappingCommonChunk.dataLength, workerCount, workerIndex);
                if (chunk.IsValid == false)
                    return;

                //=======================================================================
                // ■マッピングメッシュメッシュ頂点姿勢をプロキシメッシュから逆スキニングして求める
                // 接線の有無
                bool useTangent = tdata.IsTangent;
                // マイナススケール
                float4 negativeScl = new float4(tdata.negativeScaleDirection, 1);
                // ProxyMeshスケール
                float3 proxyScl = tdata.initScale * mdata.scaleRatio; // 初期スケール x 現在のスケール比率
                // ボーンウエイトの書き込み
                bool modifyBoneWeight = wdata.HasBoneWeight && mdata.flag.IsSet(TeamManager.MappingDataFlag_ModifyBoneWeight);

                //int mvindex = mdata.mappingCommonChunk.startIndex;
                int mvindex = mdata.mappingCommonChunk.startIndex + chunk.startIndex;
                //for (int k = 0; k < mdata.mappingCommonChunk.dataLength; k++, mvindex++)
                for (int k = 0; k < chunk.dataLength; k++, mvindex++)
                {
                    // 無効頂点は無視する
                    var attr = mappingAttributes[mvindex];
                    if (attr.IsInvalid())
                        continue;

                    // 固定も書き込まない
                    if (attr.IsFixed())
                        continue;

                    // マッピングメッシュ姿勢
                    float4x3 _pose = 0;
                    _pose.c0 = new float4(mappingLocalPositions[mvindex], 1);
                    _pose.c1 = new float4(mappingLocalNormals[mvindex], 0);
                    _pose.c2 = math.select(0, new float4(mappingLocalTangents[mvindex], 0), useTangent);

                    // プロキシメッシュの座標空間に変換する
                    if (mdata.sameSpace == false)
                    {
                        // 現在の姿勢ではなくマッピング時の姿勢で変換を行う
                        _pose = math.mul(mdata.toProxyMatrix, _pose);
                    }

                    // マイナススケール
                    _pose *= new float4x3(negativeScl, negativeScl, negativeScl);

                    // 以降計算はすべてプロキシメッシュのローカル空間で行う
                    var bw = mappingBoneWeights[mvindex];
                    int wcnt = bw.Count;
                    float4x3 _opose = 0;
                    // ProxyMeshスケール
                    for (int i = 0; i < wcnt; i++)
                    {
                        float w = bw.weights[i];

                        int tvindex = bw.boneIndices[i] + tdata.proxyCommonChunk.startIndex;

                        // バインドポーズの逆座標と逆回転
                        float3 bipos = proxyVertexBindPosePositions[tvindex];
                        quaternion birot = proxyVertexBindPoseRotations[tvindex];

                        // マイナススケール
                        bipos *= tdata.negativeScaleDirection;
                        birot = birot.value * tdata.negativeScaleQuaternionValue;

                        // quaternionからmatrixへの変換が重いのでここはそのまま
                        float3 pos = math.mul(birot, _pose.c0.xyz + bipos);
                        float3 nor = math.mul(birot, _pose.c1.xyz);
                        float3 tan = math.mul(birot, _pose.c2.xyz);

                        float3 ppos = proxyPositions[tvindex];
                        quaternion prot = proxyRotations[tvindex];

                        // ワールド変換
                        // quaternionからmatrixへの変換が重いのでここはそのまま
                        pos = math.mul(prot, pos * proxyScl) + ppos;
                        nor = math.mul(prot, nor);
                        tan = math.mul(prot, tan);

                        // ウエイト
                        _opose.c0.xyz += pos * w;
                        _opose.c1.xyz += nor * w;
                        _opose.c2.xyz += tan * w;
                    }

                    // ここまでのopos/orotはワールド空間
                    // マッピングメッシュのローカル空間に変換する
                    _opose.c0.w = 1;
                    _opose = math.mul(mdata.toMappingMatrix, _opose);

#if MC2_DEBUG

                    // 結果格納
                    mappingPositions[mvindex] = _opose.c0.xyz;
                    //mappingNormals[mvindex] = math.normalize(_opose.c1.xyz);
                    //if (useTangent)
                    //    mappingTangents[mvindex] = math.normalize(_opose.c2.xyz);
#endif
#if true
                    // ■結果格納
                    // 書き込む頂点インデックス
                    int buffIndex = mappingReferenceIndices[mvindex];

                    // positoins / normals
                    int windex = wdata.renderMeshPositionAndNormalChunk.startIndex + buffIndex;
                    renderMeshPositions[windex] = _opose.c0.xyz;
                    renderMeshNormals[windex] = math.normalize(_opose.c1.xyz);

                    // tangents
                    if (useTangent)
                    {
                        windex = wdata.renderMeshTangentChunk.startIndex + buffIndex;
                        float4 tan = renderMeshTangents[windex];
                        renderMeshTangents[windex] = new float4(math.normalize(_opose.c2.xyz), tan.w);
                    }
#endif
                    // bone weights
                    if (modifyBoneWeight)
                    {
                        // 使用頂点のウエイトはcenterTransform100%で書き込む
                        windex = wdata.renderMeshBoneWeightChunk.startIndex + buffIndex;
                        renderMeshBoneWeights[windex] = wdata.centerBoneWeight;
                    }
                }
                //Debug.Log($"[{index}] renderMeshPositions:start={mdata.renderMeshPositionAndNormalChunk.startIndex}, length={mdata.renderMeshPositionAndNormalChunk.dataLength}");
            }
        }

        /// <summary>
        /// レンダーメッシュデータの後処理
        /// </summary>
        [BurstCompile]
        struct PostRenderMeshWorkDataBatchJob : IJobParallelFor
        {
            // render mesh
            public NativeArray<RenderManager.RenderDataWork> renderDataWorkArray;

            // mapping
            [NativeDisableParallelForRestriction]
            public NativeArray<TeamManager.MappingData> mappingDataArray;

            // RenderDataWorkごと
            public void Execute(int windex)
            {
                var wdata = renderDataWorkArray[windex];
                if (wdata.IsValid() == false)
                    return;
                if (wdata.UseCustomMesh == false)
                    return;

                // 各種書き込みフラグを設定する
                bool changePositionNormal = false;
                bool changeTangent = false;
                bool changeBoneWeight = false;
                int mcnt = wdata.mappingDataIndexList.Length;
                for (int k = 0; k < mcnt; k++)
                {
                    int mindex = wdata.mappingDataIndexList[k];
                    var mdata = mappingDataArray[mindex];

                    if (mdata.flag.IsSet(TeamManager.MappingDataFlag_ChangePositionNormal))
                    {
                        changePositionNormal = true;
                        mdata.flag.SetBits(TeamManager.MappingDataFlag_ChangePositionNormal, false);
                    }
                    if (mdata.flag.IsSet(TeamManager.MappingDataFlag_ChangeTangent))
                    {
                        changeTangent = true;
                        mdata.flag.SetBits(TeamManager.MappingDataFlag_ChangeTangent, false);
                    }
                    if (mdata.flag.IsSet(TeamManager.MappingDataFlag_ChangeBoneWeight))
                    {
                        changeBoneWeight = true;
                        mdata.flag.SetBits(TeamManager.MappingDataFlag_ChangeBoneWeight, false);
                        mdata.flag.SetBits(TeamManager.MappingDataFlag_ModifyBoneWeight, false); // Modifyフラグも消す
                    }

                    mappingDataArray[mindex] = mdata;
                }

                // 書き込みフラグ設定
                wdata.flag.SetBits(RenderManager.RenderDataFlag_WritePositionNormal, changePositionNormal);
                wdata.flag.SetBits(RenderManager.RenderDataFlag_WriteTangent, changeTangent);
                wdata.flag.SetBits(RenderManager.RenderDataFlag_WriteBoneWeight, changeBoneWeight);

                renderDataWorkArray[windex] = wdata;
            }
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"========== VMesh Manager ==========");
            if (IsValid() == false)
            {
                sb.AppendLine($"VirtualMesh Manager. Invalid.");
            }
            else
            {
                sb.AppendLine($"VirtualMesh Manager.");
                sb.AppendLine($"  -ProxyVertexCount:{ProxyVertexCount}");
                sb.AppendLine($"  -ProxyEdgeCount:{ProxyEdgeCount}");
                sb.AppendLine($"  -ProxyTriangleCount:{ProxyTriangleCount}");
                sb.AppendLine($"  -ProxyBaseLineCount:{ProxyBaseLineCount}");
                sb.AppendLine($"  -ProxyLocalPositionCount:{ProxyLocalPositionCount}");

                sb.AppendLine($"  [ProxyMesh]");
                sb.AppendLine($"    -teamIds:{teamIds.ToSummary()}");
                sb.AppendLine($"    -attributes:{attributes.ToSummary()}");
                sb.AppendLine($"    -vertexToTriangles:{vertexToTriangles.ToSummary()}");
                sb.AppendLine($"    -vertexBindPosePositions:{vertexBindPosePositions.ToSummary()}");
                sb.AppendLine($"    -vertexBindPoseRotations:{vertexBindPoseRotations.ToSummary()}");
                sb.AppendLine($"    -vertexDepths:{vertexDepths.ToSummary()}");
                sb.AppendLine($"    -vertexRootIndices:{vertexRootIndices.ToSummary()}");
                sb.AppendLine($"    -vertexLocalPositions:{vertexLocalPositions.ToSummary()}");
                sb.AppendLine($"    -vertexLocalRotations:{vertexLocalRotations.ToSummary()}");
                sb.AppendLine($"    -vertexParentIndices:{vertexParentIndices.ToSummary()}");
                sb.AppendLine($"    -vertexChildIndexArray:{vertexChildIndexArray.ToSummary()}");
                sb.AppendLine($"    -vertexChildDataArray:{vertexChildDataArray.ToSummary()}");
                sb.AppendLine($"    -normalAdjustmentRotations:{normalAdjustmentRotations.ToSummary()}");
                sb.AppendLine($"    -uv:{uv.ToSummary()}");

                sb.AppendLine($"    -triangleTeamIdArray:{triangleTeamIdArray.ToSummary()}");
                sb.AppendLine($"    -triangles:{triangles.ToSummary()}");
                sb.AppendLine($"    -triangleNormals:{triangleNormals.ToSummary()}");
                sb.AppendLine($"    -triangleTangents:{triangleTangents.ToSummary()}");

                sb.AppendLine($"    -edgeTeamIdArray:{edgeTeamIdArray.ToSummary()}");
                sb.AppendLine($"    -edges:{edges.ToSummary()}");
                sb.AppendLine($"    -edgeFlags:{edgeFlags.ToSummary()}");

                sb.AppendLine($"    -baseLineFlags:{baseLineFlags.ToSummary()}");
                sb.AppendLine($"    -baseLineTeamIds:{baseLineTeamIds.ToSummary()}");
                sb.AppendLine($"    -baseLineStartDataIndices:{baseLineStartDataIndices.ToSummary()}");
                sb.AppendLine($"    -baseLineDataCounts:{baseLineDataCounts.ToSummary()}");
                sb.AppendLine($"    -baseLineData:{baseLineData.ToSummary()}");

                sb.AppendLine($"  [Mesh Common]");
                sb.AppendLine($"    -localPositions:{localPositions.ToSummary()}");
                sb.AppendLine($"    -localNormals:{localNormals.ToSummary()}");
                sb.AppendLine($"    -localTangents:{localTangents.ToSummary()}");
                sb.AppendLine($"    -boneWeights:{boneWeights.ToSummary()}");
                sb.AppendLine($"    -skinBoneTransformIndices:{skinBoneTransformIndices.ToSummary()}");
                sb.AppendLine($"    -skinBoneBindPoses:{skinBoneBindPoses.ToSummary()}");

                sb.AppendLine($"  [Mesh Other]");
                sb.AppendLine($"    -vertexToTransformRotations:{vertexToTransformRotations.ToSummary()}");
                sb.AppendLine($"    -positions:{positions.ToSummary()}");
                sb.AppendLine($"    -rotations:{rotations.ToSummary()}");

                sb.AppendLine($"  [Mapping]");
                sb.AppendLine($"    -MappingVertexCount:{MappingVertexCount}");
                sb.AppendLine($"    -mappingReferenceIndices:{mappingReferenceIndices.ToSummary()}");
                sb.AppendLine($"    -mappingAttributes:{mappingAttributes.ToSummary()}");
                sb.AppendLine($"    -mappingLocalPositins:{mappingLocalPositins.ToSummary()}");
                sb.AppendLine($"    -mappingLocalNormals:{mappingLocalNormals.ToSummary()}");
                sb.AppendLine($"    -mappingBoneWeights:{mappingBoneWeights.ToSummary()}");
                //sb.AppendLine($"    -mappingPositions:{mappingPositions.ToSummary()}");
                //sb.AppendLine($"    -mappingNormals:{mappingNormals.ToSummary()}");

                //sb.AppendLine($"  [RenderMeshBuffer]");
                //sb.AppendLine($"    -renderMeshPositions:{renderMeshPositions.ToSummary()}");
                //sb.AppendLine($"    -renderMeshNormals:{renderMeshNormals.ToSummary()}");
                //sb.AppendLine($"    -renderMeshTangents:{renderMeshTangents.ToSummary()}");
            }
            sb.AppendLine();
            Debug.Log(sb.ToString());
            allsb.Append(sb);
        }
    }
}
