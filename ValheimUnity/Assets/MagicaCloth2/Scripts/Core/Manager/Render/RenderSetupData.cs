// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Jobs;

namespace MagicaCloth2
{
    /// <summary>
    /// 描画対象の基本情報
    /// レンダラーまたはボーン構成情報
    /// この情報をもとに仮想メッシュなどを作成する
    /// またこの情報はキャラクターが動き出す前に取得しておく必要がある
    /// そのためAwake()などで実行する
    /// </summary>
    public partial class RenderSetupData : IDisposable, ITransform
    {
        public ResultCode result;
        public string name = string.Empty;
        public bool isManaged; // pre-build DeserializeManager管理

        // タイプ
        public enum SetupType
        {
            MeshCloth = 0,
            BoneCloth = 1,
            BoneSpring = 2,
        }
        public SetupType setupType;

        // Mesh ---------------------------------------------------------------
        // レンダラーとメッシュ情報
        public Renderer renderer;
        public SkinnedMeshRenderer skinRenderer;
        public MeshFilter meshFilter;
        public Mesh originalMesh;
        public int vertexCount;
        public bool hasSkinnedMesh; // SkinnedMeshRendererを利用しているかどうか
        public bool hasBoneWeight; // SkinnedMeshRendererでもボーンウエイトを持っていないケースあり！
        public Mesh.MeshDataArray meshDataArray;　// Jobで利用するためのMeshData
        public int skinRootBoneIndex;
        public int skinBoneCount;

        // MeshDataでは取得できないメッシュ情報
        public List<Matrix4x4> bindPoseList;
        public NativeArray<byte> bonesPerVertexArray;
        public NativeArray<BoneWeight1> boneWeightArray;

        // PreBuild時のみ保持する情報.逆にmeshDataArrayは持たない
        public NativeArray<Vector3> localPositions;
        public NativeArray<Vector3> localNormals;
        public NativeArray<Vector4> localTangents;

        // Bone ---------------------------------------------------------------
        public List<MagicaObjectId> rootTransformIdList;
        public enum BoneConnectionMode
        {
            // line only.
            // ラインのみ
            Line = 0,

            //Automatically mesh connection according to the interval of Transform.
            // Transformの間隔に従い自動でメッシュ接続
            AutomaticMesh = 1,

            //Generate meshes in the order of Transforms registered in RootList and connect the beginning and end in a loop.
            // RootListに登録されたTransformの順にメッシュを生成し、最初と最後をループ状に繋げる
            SequentialLoopMesh = 2,

            // Generate meshes in the order of Transforms registered in RootList, but do not connect the beginning and end.
            // RootListに登録されたTransformの順にメッシュを生成するが最初と最後を繋げない
            SequentialNonLoopMesh = 3,
        }
        public BoneConnectionMode boneConnectionMode = BoneConnectionMode.Line;
        public List<int> collisionBoneIndexList; // BoneSpringのコリジョン有効Transformインデックスリスト

        // Common -------------------------------------------------------------
        // Transform情報
        // 通常メッシュはrenderTransorm100%のスキニングとして扱われる
        public List<Transform> transformList; // skin bonesは[0]～skinBoneCountまで
        public List<MagicaObjectId> transformIdList;
        public List<MagicaObjectId> transformParentIdList; // 親ID(0=なし)
        public List<FixedList512Bytes<MagicaObjectId>> transformChildIdList; // 子IDリスト
        public NativeArray<float3> transformPositions;
        public NativeArray<quaternion> transformRotations;
        public NativeArray<float3> transformLocalPositions;
        public NativeArray<quaternion> transformLocalRotations;
        public NativeArray<float3> transformScales;
        public NativeArray<quaternion> transformInverseRotations;
        public int renderTransformIndex; // 描画基準トランスフォーム
        public float4x4 initRenderLocalToWorld; // 初期化時の基準マトリックス(LtoW)
        public float4x4 initRenderWorldtoLocal; // 初期化時の基準マトリックス(WtoL)
        public quaternion initRenderRotation; // 初期化時の基準回転
        public float3 initRenderScale; // 初期化時の基準スケール

        public bool IsSuccess() => result.IsSuccess();
        public bool IsFaild() => result.IsFaild();
        public int TransformCount => transformList?.Count ?? 0;
        public bool HasMeshDataArray => meshDataArray.Length > 0;
        public bool HasLocalPositions => localPositions.IsCreated;
        public bool HasTangent => localTangents.IsCreated && localTangents.Length > 0;

        static readonly ProfilerMarker readTransformProfiler = new ProfilerMarker("readTransform");

        //=========================================================================================
        public RenderSetupData() { }

        /// <summary>
        /// レンダラーから基本情報を作成する（メインスレッドのみ）
        /// タイプはMeshになる
        /// </summary>
        /// <param name="ren"></param>
        public RenderSetupData(RenderSetupSerializeData referenceInitSetupData, Renderer ren)
        {
            //using (initProfiler.Auto())
            {
                result.Clear();
                // Meshタイプに設定
                setupType = SetupType.MeshCloth;

                if (ren == null)
                {
                    Develop.LogWarning("Renderer is null!");
                    result.SetError(Define.Result.RenderSetup_InvalidSource);
                    return;
                }

                // 初期化データの有無
                bool useInitData = referenceInitSetupData != null;

                name = ren.name;

                var sren = ren as SkinnedMeshRenderer;

                hasSkinnedMesh = sren ? true : false;
                hasBoneWeight = false;
                skinRenderer = sren;
                Mesh mesh;

                // 必要なトランスフォーム情報
                // トランスフォームのクラスとインスタンスIDを収集する
                // 描画の基準トランスフォーム
                var renderTransform = ren.transform;

                if (useInitData)
                {
                    // 初期化データがある場合はコピーして終わり
                    skinBoneCount = referenceInitSetupData.skinBoneCount;
                    skinRootBoneIndex = referenceInitSetupData.skinRootBoneIndex;
                    renderTransformIndex = referenceInitSetupData.renderTransformIndex;
                    hasBoneWeight = referenceInitSetupData.hasBoneWeight;

                    // transformList復元
                    transformList = new List<Transform>(new Transform[referenceInitSetupData.transformCount]);
                    int ucnt = referenceInitSetupData.useTransformCount;
                    for (int i = 0; i < ucnt; i++)
                    {
                        int tindex = referenceInitSetupData.useTransformIndexArray[i];
                        transformList[tindex] = referenceInitSetupData.transformArray[i];
                    }

                    // SkinnedMeshRendererかつオリジナルメッシュが存在する場合はSkinnedMeshRenererを復元する
                    // 実行時キャラクターコピーへの対応
                    if (sren && referenceInitSetupData.originalMesh && sren.sharedMesh != referenceInitSetupData.originalMesh && skinBoneCount > 0)
                    {
                        sren.sharedMesh = referenceInitSetupData.originalMesh;
                        var newBones = new Transform[skinBoneCount];
                        transformList.CopyTo(0, newBones, 0, skinBoneCount);
                        sren.bones = newBones;
                        sren.rootBone = transformList[skinRootBoneIndex];
                        //Debug.Log($"★SkinnedMeshRenderer再構成");
                    }
                }
                else if (sren)
                {
                    // bones
                    // このスキニングボーンの取得が特に重くメモリアロケーションも頻発する問題児
                    // しかし回避方法がないため現状やむなし
                    var bones = sren.bones;
                    if (bones == null || bones.Length == 0)
                    {
                        // ブレンドシェイプではボーンや頂点ウエイトが無くてもSkinnedMeshRendererが利用される
                        // ブレンドシェイプの機能がSkinnedMeshRendererにしか無いため
                        // そのため、このようなケースではSkinnedMeshRendererであるが通常メッシュとして扱うことにする
                        // bones
                        skinBoneCount = 1;
                        transformList = new List<Transform>(skinBoneCount);
                        transformList.Add(renderTransform);

                        // rootBone
                        skinRootBoneIndex = 0;

                        // render
                        renderTransformIndex = 0;
                    }
                    else
                    {
                        skinBoneCount = bones.Length;
                        transformList = new List<Transform>(skinBoneCount + 2);
                        transformList.AddRange(bones);

                        // rootBone
                        var rootBone = sren.rootBone ? sren.rootBone : renderTransform;
                        skinRootBoneIndex = transformList.Count;
                        transformList.Add(rootBone);

                        // render
                        renderTransformIndex = transformList.Count;
                        transformList.Add(renderTransform);

                        hasBoneWeight = true;
                    }
                }
                else
                {
                    // bones
                    skinBoneCount = 1;
                    transformList = new List<Transform>(skinBoneCount);
                    transformList.Add(renderTransform);

                    // rootBone
                    skinRootBoneIndex = 0;

                    // render
                    renderTransformIndex = 0;
                }

                // トランスフォーム情報の読み取り
                ReadTransformInformation(includeChilds: false, referenceInitSetupData, ren.transform);

                // bindpose / weights
                if (sren)
                {
                    mesh = sren.sharedMesh;
                    if (mesh == null)
                    {
                        Develop.LogWarning("SkinnedMeshRenderer.sharedMesh is null!");
                        result.SetError(Define.Result.RenderSetup_NoMeshOnRenderer);
                        return;
                    }

                    if (hasBoneWeight)
                    {
                        bindPoseList = new List<Matrix4x4>(mesh.bindposes);

                        // どうもコピーを作らないとダメらしい..
                        // ※具体的にはメッシュのクローンを作成したときに壊れる
                        using var weightArray = mesh.GetAllBoneWeights();
                        using var perVertexArray = mesh.GetBonesPerVertex();
                        boneWeightArray = new NativeArray<BoneWeight1>(weightArray, Allocator.Persistent);
                        bonesPerVertexArray = new NativeArray<byte>(perVertexArray, Allocator.Persistent);

#if UNITY_EDITOR
                        // ５ボーン以上を利用する頂点ウエイトは警告とする。一応無効となるだけで動くのでエラーにはしない。
                        // なおこの検証はビルド環境では行わない
                        int vcnt = mesh.vertexCount;
                        using var bonesPerVertexResult = new NativeReference<Define.Result>(Allocator.TempJob);
                        var job = new VertexWeight5BoneCheckJob()
                        {
                            vcnt = vcnt,
                            bonesPerVertexArray = bonesPerVertexArray,
                            result = bonesPerVertexResult,
                        };
                        job.Run();
                        result.SetWarning(bonesPerVertexResult.Value);
#endif
                    }
                    else
                    {
                        bindPoseList = new List<Matrix4x4>(1);
                        bindPoseList.Add(Matrix4x4.identity);
                    }
                }
                else
                {
                    if (!ren.TryGetComponent<MeshFilter>(out var filter))
                    {
                        result.SetError(Define.Result.RenderSetup_InvalidSource);
                        return;
                    }
                    mesh = filter.sharedMesh;
                    if (mesh == null)
                    {
                        Develop.LogWarning("MeshFilter.sharedMesh is null!");
                        result.SetError(Define.Result.RenderSetup_NoMeshOnRenderer);
                        return;
                    }

                    bindPoseList = new List<Matrix4x4>(1);
                    bindPoseList.Add(Matrix4x4.identity);
                    meshFilter = filter;
                }

                if (mesh.isReadable == false)
                {
                    result.SetError(Define.Result.RenderSetup_Unreadable);
                    return;
                }
                if (mesh.vertexCount > 65535)
                {
                    result.SetError(Define.Result.RenderSetup_Over65535vertices);
                    return;
                }

                // スレッドでメッシュデータを構築するためにオリジナルのMeshDataを取得する
                // mesh data array
                meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);

                // rendererとmeshを記録
                renderer = ren;
                originalMesh = mesh;
                vertexCount = mesh.vertexCount;

                // 完了
                result.SetSuccess();
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// ５ウエイト以上の検出
        /// </summary>
        [BurstCompile]
        struct VertexWeight5BoneCheckJob : IJob
        {
            public int vcnt;

            [Unity.Collections.ReadOnly]
            public NativeArray<byte> bonesPerVertexArray;

            [Unity.Collections.WriteOnly]
            public NativeReference<Define.Result> result;

            public void Execute()
            {
                result.Value = Define.Result.None;

                for (int i = 0; i < vcnt; i++)
                {
                    if (bonesPerVertexArray[i] >= 5)
                    {
                        result.Value = Define.Result.RenderMesh_VertexWeightIs5BonesOrMore;
                        break;
                    }
                }
            }
        }
#endif

        /// <summary>
        /// ルートボーンリストから基本情報を作成する（メインスレッドのみ）
        /// タイプはBoneになる
        /// </summary>
        /// <param name="renderTransform"></param>
        /// <param name="rootTransforms"></param>
        public RenderSetupData(
            RenderSetupSerializeData referenceInitSetupData,
            SetupType setType,
            Transform renderTransform,
            List<Transform> rootTransforms,
            List<Transform> collisionBones,
            BoneConnectionMode connectionMode = BoneConnectionMode.Line,
            string name = "(no name)"
            )
        {
            result.Clear();

            //using (initProfiler.Auto())
            try
            {
                bool useInitData = referenceInitSetupData != null;

                // Boneタイプに設定
                setupType = setType;

                // 接続モード
                boneConnectionMode = connectionMode;

                if (renderTransform == null)
                {
                    //Debug.LogWarning("render transform is null!");
                    result.SetError(Define.Result.RenderSetup_InvalidSource);
                    return;
                }
                if (rootTransforms == null || rootTransforms.Count == 0)
                {
                    //Debug.LogWarning("rootBones is empty!");
                    result.SetError(Define.Result.RenderSetup_InvalidSource);
                    return;
                }

                this.name = name;

                // 必要なトランスフォーム情報
                if (useInitData)
                {
                    // 初期化データがある場合はコピーして終わり
                    transformList = new List<Transform>(referenceInitSetupData.transformArray);
                    skinBoneCount = referenceInitSetupData.skinBoneCount;
                    renderTransformIndex = referenceInitSetupData.renderTransformIndex;
                }
                else
                {
                    var indexDict = new Dictionary<Transform, int>(256);
                    transformList = new List<Transform>(256);

                    // root以下をすべて登録する
                    var stack = new Stack<Transform>(256);
                    foreach (var t in rootTransforms)
                        stack.Push(t);
                    while (stack.Count > 0)
                    {
                        var t = stack.Pop();
                        if (indexDict.ContainsKey(t))
                            continue;

                        // 登録
                        int index = transformList.Count;
                        transformList.Add(t);
                        indexDict.Add(t, index);

                        // child
                        int cnt = t.childCount;
                        for (int i = 0; i < cnt; i++)
                        {
                            stack.Push(t.GetChild(i));
                        }
                    }

                    // スキニングボーン数
                    skinBoneCount = transformList.Count;

                    // レンダートランスフォームを最後に追加
                    renderTransformIndex = transformList.Count;
                    transformList.Add(renderTransform);
                }

                // root transform id
                rootTransformIdList = new List<MagicaObjectId>(rootTransforms.Count);
                foreach (var t in rootTransforms)
                {
                    rootTransformIdList.Add(t.GetMagicaId());
                }

                // collision transform (use BoneSpring)
                if (collisionBones != null)
                {
                    collisionBoneIndexList = new List<int>(collisionBones.Count);
                    foreach (var t in collisionBones)
                    {
                        if (t)
                        {
                            int index = transformList.IndexOf(t);
                            collisionBoneIndexList.Add(index);
                            //Debug.Log($"collision bones:{t.name}, index:{index}");
                        }
                    }
                }

                // トランスフォーム情報の読み取り
                ReadTransformInformation(includeChilds: true, referenceInitSetupData, renderTransform);

                // 完了
                result.SetSuccess();
            }
            catch (MagicaClothProcessingException)
            {
                if (result.IsError() == false)
                    result.SetError(Define.Result.RenderSetup_UnknownError);
                result.DebugLog();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                result.SetError(Define.Result.RenderSetup_Exception);
            }
        }

        /// <summary>
        /// トランスフォーム情報の読み取り（メインスレッドのみ）
        /// この情報だけはキャラクターが動く前に取得する必要がある
        /// </summary>
        void ReadTransformInformation(bool includeChilds, RenderSetupSerializeData referenceInitSetupData, Transform rendererTransform)
        {
            readTransformProfiler.Begin();

            int tcnt = transformList.Count;
            bool useInitData = referenceInitSetupData != null;

            // バッファ作成
            transformPositions = new NativeArray<float3>(tcnt, Allocator.Persistent);
            transformRotations = new NativeArray<quaternion>(tcnt, Allocator.Persistent);
            transformLocalPositions = new NativeArray<float3>(tcnt, Allocator.Persistent);
            transformLocalRotations = new NativeArray<quaternion>(tcnt, Allocator.Persistent);
            transformScales = new NativeArray<float3>(tcnt, Allocator.Persistent);
            transformInverseRotations = new NativeArray<quaternion>(tcnt, Allocator.Persistent);

            // 読み取り
            if (useInitData)
            {
                Debug.Assert(tcnt == referenceInitSetupData.transformCount);

                // 初期化データがある場合はコピーして終わり
                int ucnt = referenceInitSetupData.useTransformCount;

                if (ucnt == tcnt)
                {
                    // 全体コピー
                    NativeArray<float3>.Copy(referenceInitSetupData.transformPositions, 0, transformPositions, 0, ucnt);
                    NativeArray<quaternion>.Copy(referenceInitSetupData.transformRotations, 0, transformRotations, 0, ucnt);
                    NativeArray<float3>.Copy(referenceInitSetupData.transformLocalPositions, 0, transformLocalPositions, 0, ucnt);
                    NativeArray<quaternion>.Copy(referenceInitSetupData.transformLocalRotations, 0, transformLocalRotations, 0, ucnt);
                    NativeArray<float3>.Copy(referenceInitSetupData.transformScales, 0, transformScales, 0, ucnt);
                }
                else
                {
                    // 差分
                    for (int i = 0; i < ucnt; i++)
                    {
                        int tindex = referenceInitSetupData.useTransformIndexArray[i];
                        transformPositions[tindex] = referenceInitSetupData.transformPositions[i];
                        transformRotations[tindex] = referenceInitSetupData.transformRotations[i];
                        transformLocalPositions[tindex] = referenceInitSetupData.transformLocalPositions[i];
                        transformLocalRotations[tindex] = referenceInitSetupData.transformLocalRotations[i];
                        transformScales[tindex] = referenceInitSetupData.transformScales[i];
                    }
                }

                // 逆回転のみ計算で求める
                var job = new CalcInverseRotationJob()
                {
                    rotations = transformRotations,
                    inverseRotations = transformInverseRotations,
                };
                job.Run(tcnt);

                // 初期センタートランスフォームを別途コピーしておく
                initRenderLocalToWorld = referenceInitSetupData.initRenderLocalToWorld;
                initRenderWorldtoLocal = referenceInitSetupData.initRenderWorldtoLocal;
                initRenderRotation = referenceInitSetupData.initRenderRotation;
                initRenderScale = referenceInitSetupData.initRenderScale;
            }
            else
            {
                //using var transformArray = new TransformAccessArray(transformList.ToArray());
                // Unity6.1対応
                using var transformArray = new TransformAccessArray(transformList.Count);
                int cnt = transformList.Count;
                for (int i = 0; i < cnt; i++)
                {
                    var t = transformList[i];
                    if (t == null)
                        t = rendererTransform;
                    //transformArray[i] = t;
                    transformArray.Add(t);
                }


                var job = new ReadTransformJob()
                {
                    positions = transformPositions,
                    rotations = transformRotations,
                    scales = transformScales,
                    localPositions = transformLocalPositions,
                    localRotations = transformLocalRotations,
                    inverseRotations = transformInverseRotations
                };
                // シミュレーション以外でワーカーを消費したくないのでRun()版にしておく
                job.RunReadOnly(transformArray);

                // 初期センタートランスフォームを別途コピーしておく
                initRenderLocalToWorld = GetRendeerLocalToWorldMatrix();
                initRenderWorldtoLocal = math.inverse(initRenderLocalToWorld);
                initRenderRotation = transformRotations[renderTransformIndex];
                initRenderScale = transformScales[renderTransformIndex];
            }

            // id / parent id
            transformIdList = new List<MagicaObjectId>(tcnt);
            transformParentIdList = new List<MagicaObjectId>(tcnt);
            for (int i = 0; i < tcnt; i++)
            {
                //int id = 0, pid = 0;
                MagicaObjectId id = MagicaObjectId.Invalid;
                MagicaObjectId pid = MagicaObjectId.Invalid;

                var t = transformList[i];
                if (t)
                {
                    id = t.GetMagicaId();
                    if (includeChilds && t.parent)
                        pid = t.parent.GetMagicaId();
                }
                transformIdList.Add(id);
                transformParentIdList.Add(pid);
            }

            // child id
            if (includeChilds)
            {
                transformChildIdList = new List<FixedList512Bytes<MagicaObjectId>>(tcnt);
                for (int i = 0; i < tcnt; i++)
                {
                    var t = transformList[i];
                    var clist = new FixedList512Bytes<MagicaObjectId>();
                    if (t && t.childCount > 0)
                    {
                        for (int j = 0; j < t.childCount; j++)
                        {
                            var ct = t.GetChild(j);
                            clist.Add(ct.GetMagicaId());
                        }
                    }
                    transformChildIdList.Add(clist);
                }
            }

            readTransformProfiler.End();
        }

        /// <summary>
        /// 逆回転を計算で求める
        /// </summary>
        [BurstCompile]
        struct CalcInverseRotationJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotations;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> inverseRotations;

            public void Execute(int index)
            {
                var rot = rotations[index];
                if (math.any(rot.value))
                {
                    // どれか１つでも != 0 なら実行する
                    var irot = math.inverse(rot);
                    inverseRotations[index] = irot;
                }
            }
        }

        /// <summary>
        /// 最低限のTransform情報を収集する
        /// </summary>
        [BurstCompile]
        struct ReadTransformJob : IJobParallelForTransform
        {
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> positions;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> rotations;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> scales;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> localPositions;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> localRotations;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> inverseRotations;

            public void Execute(int index, TransformAccess transform)
            {
                if (transform.isValid == false)
                    return;

                transform.GetPositionAndRotation(out var pos, out var rot);
                float4x4 LtoW = transform.localToWorldMatrix;

                positions[index] = pos;
                rotations[index] = rot;
                localPositions[index] = transform.localPosition;
                localRotations[index] = transform.localRotation;

                // マトリックスから正確なスケール値を算出する（これはTransform.lossyScaleと等価）
                var irot = math.inverse(rot);
                var m2 = math.mul(new float4x4(irot, float3.zero), LtoW);
                var scl = new float3(m2.c0.x, m2.c1.y, m2.c2.z);
                scales[index] = scl;

                // 逆回転
                inverseRotations[index] = irot;
            }
        }

        public void Dispose()
        {
            // Pre-Build DeserializeManager管理中は破棄させない
            if (isManaged)
                return;

            bonesPerVertexArray.MC2DisposeSafe();
            boneWeightArray.MC2DisposeSafe();
            localPositions.MC2DisposeSafe();
            localNormals.MC2DisposeSafe();
            localTangents.MC2DisposeSafe();

            transformPositions.MC2DisposeSafe();
            transformRotations.MC2DisposeSafe();
            transformLocalPositions.MC2DisposeSafe();
            transformLocalRotations.MC2DisposeSafe();
            transformScales.MC2DisposeSafe();
            transformInverseRotations.MC2DisposeSafe();

            // MeshDataArrayはメインスレッドのみDispose()可能
            if (setupType == SetupType.MeshCloth)
            {
                if (meshDataArray.Length > 0)
                    meshDataArray.Dispose();
            }
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
            if (transformList != null)
            {
                foreach (var t in transformList)
                    if (t)
                        transformSet.Add(t);
            }
        }

        public void ReplaceTransform(Dictionary<MagicaObjectId, Transform> replaceDict)
        {
            if (rootTransformIdList != null)
            {
                for (int i = 0; i < rootTransformIdList.Count; i++)
                {
                    MagicaObjectId id = rootTransformIdList[i];
                    if (replaceDict.ContainsKey(id))
                    {
                        rootTransformIdList[i] = replaceDict[id].GetMagicaId();
                    }
                }
            }
            for (int i = 0; i < TransformCount; i++)
            {
                MagicaObjectId id = transformIdList[i];
                if (replaceDict.ContainsKey(id))
                {
                    var t = replaceDict[id];
                    transformIdList[i] = t.GetMagicaId();
                    transformList[i] = t;
                }
                MagicaObjectId pid = transformParentIdList[i];
                if (replaceDict.ContainsKey(pid))
                {
                    var t = replaceDict[pid];
                    transformParentIdList[i] = t.GetMagicaId();
                }

                if (transformChildIdList != null)
                {
                    var cidlist = transformChildIdList[i];
                    for (int j = 0; j < cidlist.Length; j++)
                    {
                        MagicaObjectId cid = cidlist[j];
                        if (replaceDict.ContainsKey(cid))
                        {
                            var t = replaceDict[cid];
                            cidlist[j] = t.GetMagicaId();
                        }
                    }
                    transformChildIdList[i] = cidlist;
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// 描画基準トランスフォームを取得する
        /// </summary>
        /// <returns></returns>
        public Transform GetRendeerTransform()
        {
            return transformList[renderTransformIndex];
        }

        public MagicaObjectId GetRenderTransformId()
        {
            return transformIdList[renderTransformIndex];
        }

        public float4x4 GetRendeerLocalToWorldMatrix()
        {
            int index = renderTransformIndex;
            var pos = transformPositions[index];
            var rot = transformRotations[index];
            var scl = transformScales[index];
            return float4x4.TRS(pos, rot, scl);
        }

        /// <summary>
        /// スキンレンダラーのルートトランスフォームを取得する
        /// </summary>
        /// <returns></returns>
        public Transform GetSkinRootTransform()
        {
            return transformList[skinRootBoneIndex];
        }

        public MagicaObjectId GetSkinRootTransformId()
        {
            return transformIdList[skinRootBoneIndex];
        }

        public int GetTransformIndexFromId(MagicaObjectId id)
        {
            return transformIdList.IndexOf(id);
        }

        /// <summary>
        /// 指定indexの親トランスフォームのインデックスを返す(-1=なし)
        /// </summary>
        /// <param name="index"></param>
        /// <param name="centerExcluded">true=センタートランスフォームは除外する</param>
        /// <returns>なし(-1)</returns>
        public int GetParentTransformIndex(int index, bool centerExcluded)
        {
            MagicaObjectId pid = transformParentIdList[index];
            int i = transformIdList.IndexOf(pid);
            if (centerExcluded && i == renderTransformIndex)
                i = -1;
            return i;
        }

        //=========================================================================================
        /// <summary>
        /// オリジナルメッシュのボーンウエイトをBoneWeight構造体のNativeArrayで取得する
        /// </summary>
        /// <param name="weights"></param>
        public void GetBoneWeightsRun(NativeArray<BoneWeight> weights)
        {
            var job = new GetBoneWeightJos()
            {
                vcnt = vertexCount,
                bonesPerVertexArray = bonesPerVertexArray,
                boneWeightArray = boneWeightArray,
                boneWeights = weights,
            };
            job.Run();
        }

        [BurstCompile]
        struct GetBoneWeightJos : IJob
        {
            public int vcnt;

            [Unity.Collections.ReadOnly]
            public NativeArray<byte> bonesPerVertexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<BoneWeight1> boneWeightArray;

            [Unity.Collections.WriteOnly]
            public NativeArray<BoneWeight> boneWeights;

            public void Execute()
            {
                int index = 0;
                for (int i = 0; i < vcnt; i++)
                {
                    var bw = new BoneWeight();

                    var cnt = bonesPerVertexArray[i];
                    for (int j = 0; j < cnt; j++)
                    {
                        var bw1 = boneWeightArray[index];
                        index++;

                        switch (j)
                        {
                            case 0:
                                bw.weight0 = bw1.weight;
                                bw.boneIndex0 = bw1.boneIndex;
                                break;
                            case 1:
                                bw.weight1 = bw1.weight;
                                bw.boneIndex1 = bw1.boneIndex;
                                break;
                            case 2:
                                bw.weight2 = bw1.weight;
                                bw.boneIndex2 = bw1.boneIndex;
                                break;
                            case 3:
                                bw.weight3 = bw1.weight;
                                bw.boneIndex3 = bw1.boneIndex;
                                break;
                        }
                    }

                    boneWeights[i] = bw;
                }
            }
        }
    }
}
