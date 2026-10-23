// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// コライダーの管理
    /// MC1と違いコライダーはパーティクルとは別に管理される
    /// コライダーはチームごとに分けて管理される。
    /// 同じコライダーをチームAとチームBが共有していたとしてもそれぞれ別のコライダーとして登録される。
    /// </summary>
    public class ColliderManager : IManager, IValid
    {
        /// <summary>
        /// チームID
        /// </summary>
        public ExNativeArray<short> teamIdArray;

        /// <summary>
        /// コライダー種類(最大15まで)
        /// </summary>
        public enum ColliderType : byte
        {
            None = 0,
            Sphere = 1,
            CapsuleX_Center = 2,
            CapsuleY_Center = 3,
            CapsuleZ_Center = 4,
            CapsuleX_Start = 5,
            CapsuleY_Start = 6,
            CapsuleZ_Start = 7,
            Plane = 8,
            Box = 9,
        }

        /// <summary>
        /// シンメトリータイプ(最大15まで)
        /// </summary>
        public enum SymmetryType : byte
        {
            None = 0,
            X_Symmetry = 1,
            Y_Symmetry = 2,
            Z_Symmetry = 3,
            XYZ_Symmetry = 4,
        }

        /// <summary>
        /// フラグ(16bit)
        /// (0~3:4bit)コライダー種類
        /// (4~7:4bit)シンメトリータイプ
        /// (8~15:8bit)フラグ
        /// </summary>
        public const ushort Flag_Valid = 0x0100;    // データの有無
        public const ushort Flag_Enable = 0x0200;   // 有効状態
        public const ushort Flag_Reset = 0x0400;    // 位置リセット
        public const ushort Flag_Reverse = 0x0800;  // 方向逆転
        public const ushort Flag_Symmetry = 0x1000; // シンメトリー
        public const ushort Flag_SymmetryReverse = 0x2000;  // シンメトリーによる方向フリップ
        public const ushort Flag_ScaleSuspend = 0x4000; // 極小スケールによる機能停止
        public ExNativeArray<ExBitFlag16> flagArray;

        /// <summary>
        /// トランスフォームからの中心ローカルオフセット位置
        /// </summary>
        public ExNativeArray<float3> centerArray;

        /// <summary>
        /// コライダーのサイズ情報
        /// Sphere(x:半径)
        /// Capsule(x:始点半径, y:終点半径, z:長さ)
        /// Box(x:サイズX, y:サイズY, z:サイズZ)
        /// </summary>
        public ExNativeArray<float3> sizeArray;

        /// <summary>
        /// 現フレーム姿勢
        /// トランスフォームからスナップされた姿勢（ワールド）
        /// センターオフセットも計算される
        /// </summary>
        public ExNativeArray<float3> framePositions;
        public ExNativeArray<quaternion> frameRotations;
        public ExNativeArray<float3> frameScales;

        /// <summary>
        /// １つ前のフレーム姿勢（ワールド）
        /// </summary>
        public ExNativeArray<float3> oldFramePositions;
        public ExNativeArray<quaternion> oldFrameRotations;

        /// <summary>
        /// 現ステップでの姿勢（ワールド）
        /// </summary>
        public ExNativeArray<float3> nowPositions;
        public ExNativeArray<quaternion> nowRotations;

        /// <summary>
        /// 前ステップでの姿勢（ワールド）
        /// </summary>
        public ExNativeArray<float3> oldPositions;
        public ExNativeArray<quaternion> oldRotations;

        /// <summary>
        /// シンメトリー用のメインコライダーへのローカルインデックス配列
        /// </summary>
        public ExNativeArray<int> mainColliderIndices;

        /// <summary>
        /// 登録コライダーコンポーネント
        /// 現在はデバッグ用
        /// </summary>
        HashSet<ColliderComponent> colliderSet = new HashSet<ColliderComponent>();

        bool isValid = false;

        //=========================================================================================
        /// <summary>
        /// ステップごとの作業データ
        /// </summary>
        internal struct WorkData
        {
            public AABB aabb;
            public float2 radius;
            public float3x2 oldPos;
            public float3x2 nextPos;
            public quaternion inverseOldRot;
            public quaternion rot;
        }

        internal ExNativeArray<WorkData> workDataArray;

        //=========================================================================================
        public void Dispose()
        {
            isValid = false;

            teamIdArray?.Dispose();
            flagArray?.Dispose();
            centerArray?.Dispose();
            sizeArray?.Dispose();
            framePositions?.Dispose();
            frameRotations?.Dispose();
            frameScales?.Dispose();
            nowPositions?.Dispose();
            nowRotations?.Dispose();
            oldFramePositions?.Dispose();
            oldFrameRotations?.Dispose();
            oldPositions?.Dispose();
            oldRotations?.Dispose();
            workDataArray?.Dispose();
            mainColliderIndices?.Dispose();

            teamIdArray = null;
            flagArray = null;
            sizeArray = null;
            framePositions = null;
            frameRotations = null;
            frameScales = null;
            nowPositions = null;
            nowRotations = null;
            oldFramePositions = null;
            oldFrameRotations = null;
            oldPositions = null;
            oldRotations = null;
            workDataArray = null;
            mainColliderIndices = null;

            colliderSet.Clear();
        }

        public void EnterdEditMode()
        {
            Dispose();
        }

        public void Initialize()
        {
            Dispose();

            const int capacity = 256;
            teamIdArray = new ExNativeArray<short>(capacity);
            flagArray = new ExNativeArray<ExBitFlag16>(capacity);
            centerArray = new ExNativeArray<float3>(capacity);
            sizeArray = new ExNativeArray<float3>(capacity);
            framePositions = new ExNativeArray<float3>(capacity);
            frameRotations = new ExNativeArray<quaternion>(capacity);
            frameScales = new ExNativeArray<float3>(capacity);
            nowPositions = new ExNativeArray<float3>(capacity);
            nowRotations = new ExNativeArray<quaternion>(capacity);
            oldFramePositions = new ExNativeArray<float3>(capacity);
            oldFrameRotations = new ExNativeArray<quaternion>(capacity);
            oldPositions = new ExNativeArray<float3>(capacity);
            oldRotations = new ExNativeArray<quaternion>(capacity);
            workDataArray = new ExNativeArray<WorkData>(capacity);
            mainColliderIndices = new ExNativeArray<int>(capacity);

            isValid = true;
        }

        public bool IsValid()
        {
            return isValid;
        }

        //=========================================================================================
        /// <summary>
        /// チームにコライダー領域を登録する
        /// 最初から最大コライダー数で領域を初期化しておく
        /// </summary>
        /// <param name="cprocess"></param>
        public void Register(ClothProcess cprocess)
        {
            if (isValid == false)
                return;

            // コライダー数
            int cnt = cprocess.cloth.SerializeData.colliderCollisionConstraint.ColliderLength;
            if (cnt > 0)
            {
                // シンメトリーも考慮した初期コライダーの数を算出する
                var clist = cprocess.cloth.SerializeData.colliderCollisionConstraint.colliderList;
                int activeCount = 0;
                clist.ForEach(x =>
                {
                    if (x)
                        activeCount += x.symmetryMode != ColliderSymmetryMode.None ? 2 : 1;
                });
                //Debug.Log($"[{cprocess.cloth.name}] ActiveColliderCount:{activeCount}");

                // 初期コライダーの領域確保
                int teamId = cprocess.TeamId;
                ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
                tdata.colliderChunk = teamIdArray.AddRange(activeCount, (short)teamId);
                flagArray.AddRange(activeCount, default);
                centerArray.AddRange(activeCount);
                sizeArray.AddRange(activeCount);
                framePositions.AddRange(activeCount);
                frameRotations.AddRange(activeCount);
                frameScales.AddRange(activeCount);
                nowPositions.AddRange(activeCount);
                nowRotations.AddRange(activeCount);
                oldFramePositions.AddRange(activeCount);
                oldFrameRotations.AddRange(activeCount);
                oldPositions.AddRange(activeCount);
                oldRotations.AddRange(activeCount);
                workDataArray.AddRange(activeCount);
                mainColliderIndices.AddRange(activeCount);
                Transform t = cprocess.cloth.ClothTransform;
                tdata.colliderTransformChunk = MagicaManager.Bone.AddTransform(activeCount, teamId, t); // 領域のみ
                tdata.colliderCount = 0;

                // 初期コライダー登録
                for (int i = 0; i < clist.Count; i++)
                {
                    var col = clist[i];
                    if (col && cprocess.colliderDict.ContainsKey(col) == false)
                        AddCollider(cprocess, col);
                }
            }
        }

        /// <summary>
        /// チームからコライダー領域を解除する
        /// </summary>
        /// <param name="cprocess"></param>
        public void Exit(ClothProcess cprocess)
        {
            if (isValid == false)
                return;

            int teamId = cprocess.TeamId;

            // コライダー解除
            foreach (var col in cprocess.colliderDict.Keys)
            {
                if (col)
                {
                    if (col.Exit(teamId))
                    {
                        // 利用者０
                        colliderSet.Remove(col);
                    }
                }
            }
            cprocess.colliderDict.Clear();

            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);

            var c = tdata.colliderChunk;
            teamIdArray.RemoveAndFill(c); // 0クリア
            flagArray.RemoveAndFill(c); // 0クリア
            centerArray.Remove(c);
            sizeArray.Remove(c);
            framePositions.Remove(c);
            frameRotations.Remove(c);
            frameScales.Remove(c);
            nowPositions.Remove(c);
            nowRotations.Remove(c);
            oldFramePositions.Remove(c);
            oldFrameRotations.Remove(c);
            oldPositions.Remove(c);
            oldRotations.Remove(c);
            workDataArray.Remove(c);
            mainColliderIndices.Remove(c);

            tdata.colliderChunk.Clear();
            tdata.colliderCount = 0;

            // コライダートランスフォーム解除
            MagicaManager.Bone.RemoveTransform(tdata.colliderTransformChunk);
            tdata.colliderTransformChunk.Clear();
        }

        //=========================================================================================
        /// <summary>
        /// コライダーのリスト内容を更新する
        /// これはMagicaClothコンポーネント側のコライダーリストが変更された場合
        /// つまりパラメータの変更ではない
        /// </summary>
        /// <param name="cprocess"></param>
        internal void UpdateColliders(ClothProcess cprocess)
        {
            if (isValid == false)
                return;

            // ここではコライダーリストへのコンポーネント追加削除だけで、シンメトリーなどの切り替えは考慮しなくていい
            var clist = cprocess.cloth.SerializeData.colliderCollisionConstraint.colliderList;

            // 現在の登録コライダーと比較し不要なコライダーを削除する
            var keys = cprocess.colliderDict.Keys.ToList();
            foreach (ColliderComponent col in keys)
            {
                if (col && clist.Contains(col) == false)
                {
                    // コライダー削除
                    RemoveCollider(col, cprocess.TeamId);
                }
            }

            // 現在の登録コライダーと比較し必要なコライダーを追加する
            foreach (var col in clist)
            {
                if (col && cprocess.colliderDict.ContainsKey(col) == false)
                {
                    AddCollider(cprocess, col);
                }
            }
        }

        /// <summary>
        /// コライダーの個別登録
        /// </summary>
        /// <param name="cprocess"></param>
        /// <param name="col"></param>
        void AddCollider(ClothProcess cprocess, ColliderComponent col)
        {
            if (isValid == false)
                return;
            if (col == null)
                return;
            if (cprocess.colliderDict.ContainsKey(col))
                return; // すでに追加済み

            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(cprocess.TeamId);
            Debug.Assert(tdata.IsValid);

            // Main
            AddColliderInternal(ref tdata, cprocess, col, false);
            colliderSet.Add(col);

            // Symmetry
            col.SetActiveSymmetryMode(firstOnly: true); // シンメトリー情報更新
            AddColliderInternal(ref tdata, cprocess, col, true);

            // コライダーコンポーネント側にも登録する（紐づけ）
            col.Register(cprocess.TeamId);
        }

        void AddColliderInternal(ref TeamManager.TeamData tdata, ClothProcess cprocess, ColliderComponent col, bool isSymmetry)
        {
            // シンメトリーの有効性
            if (isSymmetry)
            {
                if (col.ActiveSymmetryMode == ColliderSymmetryMode.None)
                    return;
                if (col.ActiveSymmetryTarget == null)
                    return;
            }

            int teamId = cprocess.TeamId;

            // 領域拡張
            if (tdata.colliderChunk.IsValid == false)
            {
                // 新規確保
                int newCount = Define.System.ExpandedColliderCount;
                tdata.colliderChunk = teamIdArray.AddRange(newCount, (short)cprocess.TeamId);
                flagArray.AddRange(newCount, default);
                centerArray.AddRange(newCount);
                sizeArray.AddRange(newCount);
                framePositions.AddRange(newCount);
                frameRotations.AddRange(newCount);
                frameScales.AddRange(newCount);
                nowPositions.AddRange(newCount);
                nowRotations.AddRange(newCount);
                oldFramePositions.AddRange(newCount);
                oldFrameRotations.AddRange(newCount);
                oldPositions.AddRange(newCount);
                oldRotations.AddRange(newCount);
                workDataArray.AddRange(newCount);
                mainColliderIndices.AddRange(newCount);
                Transform t = cprocess.cloth.ClothTransform;
                tdata.colliderTransformChunk = MagicaManager.Bone.AddTransform(newCount, cprocess.TeamId, t); // 領域のみ
                tdata.colliderCount = 0;
            }
            else if (tdata.UseColliderCount == tdata.colliderChunk.dataLength)
            {
                // コライダー配列のキャパシティ上限なら拡張する
                // 拡張
                int newCount = tdata.colliderChunk.dataLength + Define.System.ExpandedColliderCount;
                var oldColliderChunk = tdata.colliderChunk;
                tdata.colliderChunk = teamIdArray.Expand(oldColliderChunk, newCount);
                flagArray.ExpandAndFill(oldColliderChunk, newCount); // 旧領域のフラグはクリアする必要あり
                centerArray.Expand(oldColliderChunk, newCount);
                sizeArray.Expand(oldColliderChunk, newCount);
                framePositions.Expand(oldColliderChunk, newCount);
                frameRotations.Expand(oldColliderChunk, newCount);
                frameScales.Expand(oldColliderChunk, newCount);
                nowPositions.Expand(oldColliderChunk, newCount);
                nowRotations.Expand(oldColliderChunk, newCount);
                oldFramePositions.Expand(oldColliderChunk, newCount);
                oldFrameRotations.Expand(oldColliderChunk, newCount);
                oldPositions.Expand(oldColliderChunk, newCount);
                oldRotations.Expand(oldColliderChunk, newCount);
                workDataArray.Expand(oldColliderChunk, newCount);
                mainColliderIndices.Expand(oldColliderChunk, newCount);

                // コライダートランスフォーム拡張
                var oldColliderTransformChunk = tdata.colliderTransformChunk;
                tdata.colliderTransformChunk = MagicaManager.Bone.Expand(oldColliderTransformChunk, newCount);
            }

            // 追加インデックス
            int localIndex = tdata.UseColliderCount;
            int arrayIndex = tdata.colliderChunk.startIndex + localIndex;
            int transformIndex = tdata.colliderTransformChunk.startIndex + localIndex;

            // フラグ
            var flag = new ExBitFlag16();
            flag = DataUtility.SetColliderType(flag, col.GetColliderType());
            flag.SetFlag(Flag_Valid, true);
            flag.SetFlag(Flag_Enable, col.isActiveAndEnabled);
            flag.SetFlag(Flag_Reset, true);
            flag.SetFlag(Flag_Reverse, col.IsReverseDirection());
            // ワールド姿勢
            var ct = col.transform;
            float3 pos = float3.zero;
            quaternion rot = quaternion.identity;
            float3 scl = 1;
            float3 center = col.center;
            // 登録トランスフォーム
            Transform target = ct;
            if (isSymmetry)
            {
                // シンメトリー
                // ここでは姿勢計算は不要。方向性のみでよい。
                SymmetryType symType;
                switch (col.ActiveSymmetryMode)
                {
                    case ColliderSymmetryMode.X_Symmetry:
                        symType = SymmetryType.X_Symmetry;
                        break;
                    case ColliderSymmetryMode.Y_Symmetry:
                        symType = SymmetryType.Y_Symmetry;
                        break;
                    case ColliderSymmetryMode.Z_Symmetry:
                        symType = SymmetryType.Z_Symmetry;
                        break;
                    case ColliderSymmetryMode.XYZ_Symmetry:
                        symType = SymmetryType.XYZ_Symmetry;
                        break;
                    default:
                        Develop.LogError("Unknown active symmetry mode.");
                        return;
                }
                flag = DataUtility.SetSymmetryType(flag, symType);

                // 方向性
                float direction = 1.0f;
                if (col is MagicaCapsuleCollider)
                {
                    var ccol = col as MagicaCapsuleCollider;
                    if (col.ActiveSymmetryMode == ColliderSymmetryMode.X_Symmetry && ccol.direction == MagicaCapsuleCollider.Direction.X)
                        direction = -1.0f;
                    else if (col.ActiveSymmetryMode == ColliderSymmetryMode.Y_Symmetry && ccol.direction == MagicaCapsuleCollider.Direction.Y)
                        direction = -1.0f;
                    else if (col.ActiveSymmetryMode == ColliderSymmetryMode.Z_Symmetry && ccol.direction == MagicaCapsuleCollider.Direction.Z)
                        direction = -1.0f;
                    else if (col.ActiveSymmetryMode == ColliderSymmetryMode.XYZ_Symmetry)
                        direction = -1.0f;
                }
                else if (col is MagicaPlaneCollider)
                {
                    switch (col.ActiveSymmetryMode)
                    {
                        case ColliderSymmetryMode.Y_Symmetry:
                        case ColliderSymmetryMode.XYZ_Symmetry:
                            direction = -1.0f;
                            break;
                    }
                }

                // フラグ
                flag.SetFlag(Flag_Symmetry, true);
                flag.SetFlag(Flag_SymmetryReverse, direction < 0.0f);

                // 登録トランスフォーム
                target = col.ActiveSymmetryTarget;
            }

            // マネージャへ登録
            teamIdArray[arrayIndex] = (short)teamId;
            flagArray[arrayIndex] = flag;
            centerArray[arrayIndex] = center;
            sizeArray[arrayIndex] = math.max(col.GetSize(), 0.0001f); // 念のため
            framePositions[arrayIndex] = pos;
            frameRotations[arrayIndex] = rot;
            frameScales[arrayIndex] = scl;
            nowPositions[arrayIndex] = pos;
            nowRotations[arrayIndex] = rot;
            oldFramePositions[arrayIndex] = pos;
            oldFrameRotations[arrayIndex] = rot;
            oldPositions[arrayIndex] = pos;
            oldRotations[arrayIndex] = rot;
            mainColliderIndices[arrayIndex] = 0;

            // ClothProcessにコライダーコンポーネントを登録
            if (cprocess.colliderDict.ContainsKey(col))
            {
                int2 data = cprocess.colliderDict[col];
                if (isSymmetry)
                {
                    data.y = localIndex;
                    // メインコライダーローカルインデックスを記録
                    mainColliderIndices[arrayIndex] = data.x;
                }
                else
                    data.x = localIndex;
                cprocess.colliderDict[col] = data;
            }
            else
            {
                Debug.Assert(isSymmetry == false);
                cprocess.colliderDict.Add(col, new int2(localIndex, 0));
            }

            // トランスフォーム登録
            bool t_enable = cprocess.IsEnable && flag.IsSet(Flag_Enable);
            var tflag = new ExBitFlag8(TransformManager.Flag_Read);
            tflag.SetFlag(TransformManager.Flag_Enable, t_enable);
            MagicaManager.Bone.SetTransform(target, tflag, transformIndex, teamId);

            tdata.colliderCount++;
        }


        /// <summary>
        /// コライダーを削除する
        /// 削除領域は生存する最後尾のデータと入れ替えられる(SwapBack)
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="localIndex"></param>
        internal void RemoveCollider(ColliderComponent col, int teamId)
        {
            if (isValid == false)
                return;
            if (col == null || teamId == 0)
                return;

            // Symmetry
            RemoveColliderInternal(col, teamId, true);

            // Main
            RemoveColliderInternal(col, teamId, false);

            // コライダーコンポーネント側からも削除登録する（紐づけ解除）
            if (col.Exit(teamId))
            {
                // 利用者０
                colliderSet.Remove(col);
            }
        }

        void RemoveColliderInternal(ColliderComponent col, int teamId, bool isSymmetry)
        {
            if (isValid == false)
                return;
            if (col == null || teamId == 0)
                return;
            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
            int ccnt = tdata.colliderCount;
            if (ccnt == 0)
                return;
            var cprocess = MagicaManager.Team.GetClothProcess(teamId);
            if (cprocess == null)
                return;
            if (cprocess.colliderDict.ContainsKey(col) == false)
                return;
            int2 data = cprocess.colliderDict[col];
            int localIndex = data.x;
            if (isSymmetry)
            {
                if (data.y == 0)
                    return; // シンメトリーコライダーなし
                localIndex = data.y;
            }

            int arrayIndex = tdata.colliderChunk.startIndex + localIndex;
            int transformIndex = tdata.colliderTransformChunk.startIndex + localIndex;

            int swapLocalIndex = ccnt - 1;
            int swapArrayIndex = tdata.colliderChunk.startIndex + swapLocalIndex;
            int swapTransformIndex = tdata.colliderTransformChunk.startIndex + swapLocalIndex;

            if (arrayIndex < swapArrayIndex)
            {
                // remove swap back
                flagArray[arrayIndex] = flagArray[swapArrayIndex];
                teamIdArray[arrayIndex] = teamIdArray[swapArrayIndex];
                centerArray[arrayIndex] = centerArray[swapArrayIndex];
                sizeArray[arrayIndex] = sizeArray[swapArrayIndex];
                framePositions[arrayIndex] = framePositions[swapArrayIndex];
                frameRotations[arrayIndex] = frameRotations[swapArrayIndex];
                frameScales[arrayIndex] = frameScales[swapArrayIndex];
                nowPositions[arrayIndex] = nowPositions[swapArrayIndex];
                nowRotations[arrayIndex] = nowRotations[swapArrayIndex];
                oldFramePositions[arrayIndex] = oldFramePositions[swapArrayIndex];
                oldFrameRotations[arrayIndex] = oldFrameRotations[swapArrayIndex];
                oldPositions[arrayIndex] = oldPositions[swapArrayIndex];
                oldRotations[arrayIndex] = oldRotations[swapArrayIndex];
                mainColliderIndices[arrayIndex] = mainColliderIndices[swapArrayIndex];

                flagArray[swapArrayIndex] = default;
                teamIdArray[swapArrayIndex] = 0;

                // transform
                MagicaManager.Bone.CopyTransform(swapTransformIndex, transformIndex);
                MagicaManager.Bone.SetTransform(null, default, swapTransformIndex, 0);

                // cprocess
                var ckeys = cprocess.colliderDict.Keys.ToList();
                foreach (ColliderComponent kcol in ckeys)
                {
                    Debug.Assert(cprocess.colliderDict.ContainsKey(kcol));
                    int2 data2 = cprocess.colliderDict[kcol];
                    if (data2.x == swapLocalIndex)
                    {
                        data2.x = localIndex;
                        cprocess.colliderDict[kcol] = data2;
                        break;
                    }
                    if (data2.y == swapLocalIndex)
                    {
                        data2.y = localIndex;
                        cprocess.colliderDict[kcol] = data2;
                        break;
                    }
                }
            }
            else
            {
                // remove
                flagArray[arrayIndex] = default;
                teamIdArray[arrayIndex] = 0;

                // transform
                MagicaManager.Bone.SetTransform(null, default, transformIndex, 0);
            }
            if (isSymmetry)
                data.y = 0;
            else
                data.x = 0;
            cprocess.colliderDict[col] = data;
            if (data.x == 0 && data.y == 0 && isSymmetry == false)
                cprocess.colliderDict.Remove(col); // コライダー削除

            tdata.colliderCount--;
        }

        /// <summary>
        /// 有効状態の変更
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="index"></param>
        /// <param name="sw"></param>
        internal void EnableCollider(ColliderComponent col, int teamId, bool sw)
        {
            if (IsValid() == false)
                return;
            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
            if (tdata.IsValid == false)
                return;
            var cprocess = MagicaManager.Team.GetClothProcess(teamId);

            if (cprocess.colliderDict.ContainsKey(col) == false)
                return;

            // メイン、シンメトリーの２つをチェック
            int2 data = cprocess.colliderDict[col];
            for (int i = 0; i < 2; i++)
            {
                int localIndex = data[i];
                if (i == 1 && localIndex == 0)
                    continue; // シンメトリーのindex0はデータなし

                int arrayIndex = tdata.colliderChunk.startIndex + localIndex;
                var flag = flagArray[arrayIndex];
                flag.SetFlag(Flag_Enable, sw);
                flag.SetFlag(Flag_Reset, true); // Enable/Disableどちらでもリセット
                flagArray[arrayIndex] = flag;

                // トランスフォーム有効状態
                int transformIndex = tdata.colliderTransformChunk.startIndex + localIndex;
                bool t_enable = cprocess.IsEnable && flag.IsSet(Flag_Enable);
                MagicaManager.Bone.EnableTransform(transformIndex, t_enable);
            }
        }

        /// <summary>
        /// チーム有効状態変更に伴うコライダー状態の変更
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="sw"></param>
        internal void EnableTeamCollider(int teamId)
        {
            if (IsValid() == false)
                return;
            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
            if (tdata.IsValid == false)
                return;
            if (tdata.UseColliderCount == 0)
                return;

            bool teamEnable = tdata.IsEnable;
            var c = tdata.colliderTransformChunk;
            for (int i = 0; i < c.dataLength; i++)
            {
                int arrayIndex = tdata.colliderChunk.startIndex + i;
                int transformIndex = c.startIndex + i;

                // フラグ
                var flag = flagArray[arrayIndex];
                //flag.SetFlag(Flag_Enable, sw); // コライダーのEnableフラグはコライダー固有のものなので変更する必要はない
                flag.SetFlag(Flag_Reset, true); // Enable/Disableどちらでもリセット
                flagArray[arrayIndex] = flag;

                // 有効状態
                bool t_enable = teamEnable && flag.IsSet(Flag_Enable);
                MagicaManager.Bone.EnableTransform(transformIndex, t_enable);
            }
        }

        /// <summary>
        /// コライダーコンポーネントのパラメータ変更を反映する
        /// シンメトリーの変更なども反映させる
        /// </summary>
        /// <param name="col"></param>
        /// <param name="teamId"></param>
        internal void UpdateParameters(ColliderComponent col, int teamId, bool changeSymmetry)
        {
            if (IsValid() == false)
                return;

            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
            if (tdata.IsValid == false)
                return;
            var cprocess = MagicaManager.Team.GetClothProcess(teamId);
            if (cprocess.colliderDict.ContainsKey(col) == false)
                return;
            int2 data = cprocess.colliderDict[col];

            // Main
            // メインコライダーは変更のみ考える
            // メインは削除されることはない。Transformの変更もない。
            int localIndex = data.x;
            int arrayIndex = tdata.colliderChunk.startIndex + localIndex;
            var flag = flagArray[arrayIndex];
            flag = DataUtility.SetColliderType(flag, col.GetColliderType());
            flag.SetFlag(Flag_Reverse, col.IsReverseDirection());
            flagArray[arrayIndex] = flag;
            centerArray[arrayIndex] = col.center;
            sizeArray[arrayIndex] = math.max(col.GetSize(), 0.0001f); // 念のため

            // Symmetry
            if (col.ActiveSymmetryMode != ColliderSymmetryMode.None && col.ActiveSymmetryTarget != null)
            {
                // シンメトリーあり
                if (changeSymmetry)
                {
                    // シンメトリーのモードまたはターゲットの変更
                    // 一旦削除して再度追加する
                    RemoveColliderInternal(col, teamId, true);
                    AddColliderInternal(ref tdata, cprocess, col, true);
                    //Debug.Log($"remove and add symmetry.");
                }
                else if (data.y > 0)
                {
                    // シンメトリーのモードおよびターゲットは変更されていない
                    // パラメータの変更のみ
                    localIndex = data.y;
                    arrayIndex = tdata.colliderChunk.startIndex + localIndex;
                    flag = flagArray[arrayIndex];
                    flag = DataUtility.SetColliderType(flag, col.GetColliderType());
                    flag.SetFlag(Flag_Reverse, col.IsReverseDirection());
                    flagArray[arrayIndex] = flag;
                    centerArray[arrayIndex] = col.center;
                    sizeArray[arrayIndex] = math.max(col.GetSize(), 0.0001f); // 念のため
                    //Debug.Log($"modify symmetry parameter only.");
                }
            }
            else
            {
                // シンメトリーなし
                // 既存のシンメトリーコライダーが存在する場合は削除する
                RemoveColliderInternal(col, teamId, true);
                //Debug.Log($"remove symmetry.");
            }
        }

        //=========================================================================================
        // Simulation
        //=========================================================================================
        /// <summary>
        /// シミュレーション更新前処理
        /// コライダー姿勢の読み取り
        /// </summary>
        internal static void SimulationPreUpdate(
            DataChunk chunk,
            // team
            ref TeamManager.TeamData tdata,
            ref InertiaConstraint.CenterData cdata,
            // collider
            ref NativeArray<ExBitFlag16> flagArray,
            ref NativeArray<float3> centerArray,
            ref NativeArray<float3> framePositions,
            ref NativeArray<quaternion> frameRotations,
            ref NativeArray<float3> frameScales,
            ref NativeArray<float3> oldFramePositions,
            ref NativeArray<quaternion> oldFrameRotations,
            ref NativeArray<float3> nowPositions,
            ref NativeArray<quaternion> nowRotations,
            ref NativeArray<float3> oldPositions,
            ref NativeArray<quaternion> oldRotations,
            ref NativeArray<int> mainColliderIndices,
            // transform
            ref NativeArray<float3> transformPositionArray,
            ref NativeArray<quaternion> transformRotationArray,
            ref NativeArray<float3> transformScaleArray,
            ref NativeArray<float3> transformLocalPositionArray,
            ref NativeArray<quaternion> transformLocalRotationArray,
            ref NativeArray<float3> transformLocalScaleArray
            )
        {
            // コライダーごと
            //int index = tdata.colliderChunk.startIndex;
            int index = tdata.colliderChunk.startIndex + chunk.startIndex;
            //for (int i = 0; i < tdata.colliderChunk.dataLength; i++, index++)
            for (int i = 0; i < chunk.dataLength; i++, index++)
            {
                var flag = flagArray[index];
                if (flag.IsSet(Flag_Valid) == false || flag.IsSet(Flag_Enable) == false)
                    continue;

                var center = centerArray[index];
                int l_index = index - tdata.colliderChunk.startIndex;
                int t_index = tdata.colliderTransformChunk.startIndex + l_index;

                // ほぼ０スケールは極小スケールに変換し無効化させる
                bool isZeroScale = false;
                float3 pscl = transformScaleArray[t_index];
                for (int j = 0; j < 3; j++)
                {
                    float s = pscl[j];
                    if (s < 1e-06f && s > -1e-06f)
                    {
                        pscl[j] = 1e-06f;
                        isZeroScale = true;
                    }
                }
                flag.SetFlag(Flag_ScaleSuspend, isZeroScale);

                // コライダー姿勢（ワールド）
                float3 wpos;
                quaternion wrot;
                float3 wscl;
                if (flag.IsSet(Flag_Symmetry))
                {
                    // Symmetry
                    int mainLocalIndex = mainColliderIndices[index];
                    int mainTransformIndex = tdata.colliderTransformChunk.startIndex + mainLocalIndex;
                    float3 lpos = transformLocalPositionArray[mainTransformIndex];
                    float3 lerot = MathUtility.ToEuler(transformLocalRotationArray[mainTransformIndex]);
                    float3 lscl = transformLocalScaleArray[mainTransformIndex];

                    var symmetryType = DataUtility.GetSymmetryType(flag);
                    switch (symmetryType)
                    {
                        case SymmetryType.X_Symmetry:
                            lpos.x = -lpos.x;
                            center.x = -center.x;
                            lerot.y = -lerot.y;
                            lerot.z = -lerot.z;
                            break;
                        case SymmetryType.Y_Symmetry:
                            lpos.y = -lpos.y;
                            center.y = -center.y;
                            lerot.x = -lerot.x;
                            lerot.z = -lerot.z;
                            break;
                        case SymmetryType.Z_Symmetry:
                            lpos.z = -lpos.z;
                            center.z = -center.z;
                            lerot.x = -lerot.x;
                            lerot.y = -lerot.y;
                            break;
                        case SymmetryType.XYZ_Symmetry:
                            lpos = -lpos;
                            center = -center;
                            break;
                    }

                    // シンメトリー先の親
                    float3 ppos = transformPositionArray[t_index];
                    quaternion prot = transformRotationArray[t_index];
                    //float3 pscl = transformScaleArray[t_index];

                    // マイナススケール
                    float3 sclSign = math.sign(pscl);
                    float3 sclEulerSign = 1;
                    if (pscl.x < 0 || pscl.y < 0 || pscl.z < 0)
                        sclEulerSign = sclSign * -1;

                    // シンメトリーコライダーの姿勢
                    wpos = MathUtility.TransformPoint(lpos, ppos, prot, pscl);
                    wrot = math.mul(prot, quaternion.Euler(math.radians(lerot * sclEulerSign)));
                    wscl = pscl * lscl;

                    // オフセット
                    wpos += math.mul(wrot, center * sclSign) * wscl * sclSign;
                }
                else
                {
                    // Main
                    wpos = transformPositionArray[t_index];
                    wrot = transformRotationArray[t_index];
                    //wscl = transformScaleArray[t_index];
                    wscl = pscl;

                    // マイナススケール
                    float3 sclSign = math.sign(wscl);

                    // オフセット
                    wpos += math.mul(wrot, center * sclSign) * wscl * sclSign;
                }

                // 格納
                framePositions[index] = wpos;
                frameRotations[index] = wrot;
                frameScales[index] = wscl;

                // リセット処理
                if (tdata.IsReset || flag.IsSet(Flag_Reset) || isZeroScale)
                {
                    oldFramePositions[index] = wpos;
                    oldFrameRotations[index] = wrot;
                    nowPositions[index] = wpos;
                    nowRotations[index] = wrot;
                    oldPositions[index] = wpos;
                    oldRotations[index] = wrot;

                    flag.SetFlag(Flag_Reset, false);
                }
                else if (tdata.IsInertiaShift || tdata.IsNegativeScaleTeleport)
                {
                    // 慣性全体シフト
                    var oldFramePosition = oldFramePositions[index];
                    var oldFrameRotation = oldFrameRotations[index];
                    var nowPosition = nowPositions[index];
                    var nowRotation = nowRotations[index];
                    var oldPosition = oldPositions[index];
                    var oldRotation = oldRotations[index];

                    // マイナススケール
                    if (tdata.IsNegativeScaleTeleport)
                    {
                        // 本体のスケール反転に合わせてシミュレーションに影響が出ないように必要な座標系を同様に軸反転させる
                        // コライダーはセンター空間で反転させる
                        // 回転に関してはパーティクルとは異なり法線接線をスケール方向により反転させて組み直す

                        // センター空間軸反転用マトリックス
                        float4x4 negativeM = cdata.negativeScaleMatrix;

                        oldFramePosition = MathUtility.TransformPoint(oldFramePosition, negativeM);
                        oldFrameRotation = MathUtility.TransformRotation(oldFrameRotation, negativeM, tdata.negativeScaleChange);

                        nowPosition = MathUtility.TransformPoint(nowPosition, negativeM);
                        nowRotation = MathUtility.TransformRotation(nowRotation, negativeM, tdata.negativeScaleChange);

                        oldPosition = MathUtility.TransformPoint(oldPosition, negativeM);
                        oldRotation = MathUtility.TransformRotation(oldRotation, negativeM, tdata.negativeScaleChange);
                    }

                    if (tdata.IsInertiaShift)
                    {
                        // cdata.frameComponentShiftVector : 全体シフトベクトル
                        // cdata.frameComponentShiftRotation : 全体シフト回転
                        // cdata.oldComponentWorldPosition : フレーム移動前のコンポーネント中心位置

                        float3 prevFrameWorldPosition = cdata.oldComponentWorldPosition;

                        oldFramePosition = MathUtility.ShiftPosition(oldFramePosition, prevFrameWorldPosition, cdata.frameComponentShiftVector, cdata.frameComponentShiftRotation);
                        oldFrameRotation = math.mul(cdata.frameComponentShiftRotation, oldFrameRotation);

                        nowPosition = MathUtility.ShiftPosition(nowPosition, prevFrameWorldPosition, cdata.frameComponentShiftVector, cdata.frameComponentShiftRotation);
                        nowRotation = math.mul(cdata.frameComponentShiftRotation, nowRotation);

                        oldPosition = MathUtility.ShiftPosition(oldPosition, prevFrameWorldPosition, cdata.frameComponentShiftVector, cdata.frameComponentShiftRotation);
                        oldRotation = math.mul(cdata.frameComponentShiftRotation, oldRotation);
                    }

                    oldFramePositions[index] = oldFramePosition;
                    oldFrameRotations[index] = oldFrameRotation;
                    nowPositions[index] = nowPosition;
                    nowRotations[index] = nowRotation;
                    oldPositions[index] = oldPosition;
                    oldRotations[index] = oldRotation;
                }

                // フラグ書き戻し
                flagArray[index] = flag;
            }
        }

        internal static void SimulationStartStep(
            // team
            ref TeamManager.TeamData tdata,
            ref InertiaConstraint.CenterData cdata,
            // collider
            ref NativeArray<ExBitFlag16> flagArray,
            ref NativeArray<float3> sizeArray,
            ref NativeArray<float3> framePositions,
            ref NativeArray<quaternion> frameRotations,
            ref NativeArray<float3> frameScales,
            ref NativeArray<float3> oldFramePositions,
            ref NativeArray<quaternion> oldFrameRotations,
            ref NativeArray<float3> nowPositions,
            ref NativeArray<quaternion> nowRotations,
            ref NativeArray<float3> oldPositions,
            ref NativeArray<quaternion> oldRotations,
            ref NativeArray<WorkData> workDataArray
            )
        {
            // コライダーごと
            int cindex = tdata.colliderChunk.startIndex;
            for (int i = 0; i < tdata.colliderChunk.dataLength; i++, cindex++)
            {
                var flag = flagArray[cindex];
                if (flag.IsSet(Flag_Valid) == false || flag.IsSet(Flag_Enable) == false || flag.IsSet(Flag_ScaleSuspend))
                    continue;

                // 今回のシミュレーションステップでの姿勢を求める
                float3 pos = math.lerp(oldFramePositions[cindex], framePositions[cindex], tdata.frameInterpolation);
                quaternion rot = math.slerp(oldFrameRotations[cindex], frameRotations[cindex], tdata.frameInterpolation);
                rot = math.normalize(rot); // 必要
                nowPositions[cindex] = pos;
                nowRotations[cindex] = rot;
                //Debug.Log($"cpos:{pos}, coldpos:{oldPos}");

                // コライダー慣性シフト
                // old姿勢をシフトさせる
                var oldpos = oldPositions[cindex];
                var oldrot = oldRotations[cindex];

                // ローカル慣性シフト
                oldpos = math.lerp(oldpos, pos, cdata.stepMoveInertiaRatio);
                oldrot = math.slerp(oldrot, rot, cdata.stepRotationInertiaRatio);
                oldPositions[cindex] = oldpos;
                oldRotations[cindex] = math.normalize(oldrot);

                // ステップ作業データの構築
                var type = DataUtility.GetColliderType(flag);
                var work = new WorkData();
                var csize = sizeArray[cindex];
                var cscl = frameScales[cindex];
                work.inverseOldRot = math.inverse(oldrot);
                work.rot = rot;
                if (type == ColliderType.Sphere)
                {
                    // radius
                    float radius = csize.x * math.abs(cscl.x); // X軸のみを見る
                    work.radius = radius;

                    // aabb
                    var aabb = new AABB(math.min(oldpos, pos), math.max(oldpos, pos));
                    aabb.Expand(radius);
                    work.aabb = aabb;

                    // oldpos
                    work.oldPos.c0 = oldpos;

                    // nextpos
                    work.nextPos.c0 = pos;
                }
                else if (type >= ColliderType.CapsuleX_Center && type <= ColliderType.CapsuleZ_Start)
                {
                    // 中央揃え
                    bool alignedCenter = type >= ColliderType.CapsuleX_Center && type <= ColliderType.CapsuleZ_Center;

                    // 方向性
                    float3 dir = (type == ColliderType.CapsuleX_Center || type == ColliderType.CapsuleX_Start) ? math.right()
                        : (type == ColliderType.CapsuleY_Center || type == ColliderType.CapsuleY_Start) ? math.up()
                        : math.forward();

                    // スケール
                    //float scl = math.dot(math.abs(cscl), dir); // dirの軸のスケールを使用する

                    // マイナススケール
                    float scl0 = math.dot(cscl, dir); // dirの軸のスケールを使用する
                    dir *= math.sign(scl0); // 方向反転
                    float scl = math.abs(scl0);

                    // 逆方向
                    if (flag.IsSet(Flag_Reverse))
                        dir = -dir;

                    // シンメトリーによる方向性
                    if (flag.IsSet(Flag_Symmetry) && flag.IsSet(Flag_SymmetryReverse))
                        dir = -dir;

                    // x = 始点半径
                    // y = 終点半径
                    // z = 長さ
                    csize *= scl;

                    float sr = csize.x;
                    float er = csize.y;
                    float length = csize.z;

                    // 長さ
                    float slen = alignedCenter ? length * 0.5f : 0.0f;
                    float elen = alignedCenter ? length * 0.5f : (length - sr);
                    slen = math.max(slen - sr, 0.0f);
                    elen = math.max(elen - er, 0.0f);

                    // 移動前カプセル始点と終点
                    float3 soldpos = oldpos + math.mul(oldrot, dir * slen);
                    float3 eoldpos = oldpos - math.mul(oldrot, dir * elen);

                    // 移動後カプセル始点と終点
                    float3 spos = pos + math.mul(rot, dir * slen);
                    float3 epos = pos - math.mul(rot, dir * elen);

                    // AABB
                    var aabbC = new AABB(math.min(soldpos, spos) - sr, math.max(soldpos, spos) + sr);
                    var aabbC1 = new AABB(math.min(eoldpos, epos) - er, math.max(eoldpos, epos) + er);
                    aabbC.Encapsulate(aabbC1);

                    // 格納
                    work.aabb = aabbC;
                    work.radius = new float2(sr, er);
                    work.oldPos = new float3x2(soldpos, eoldpos);
                    work.nextPos = new float3x2(spos, epos);
                }
                else if (type == ColliderType.Plane)
                {
                    // 押し出し法線方向をoldposに格納する
                    // マイナススケール
                    float3 dir = math.up();
                    dir *= math.sign(cscl.y); // Y反転時は逆にする

                    // シンメトリーによる方向性
                    if (flag.IsSet(Flag_Symmetry) && flag.IsSet(Flag_SymmetryReverse))
                        dir = -dir;

                    float3 n = math.mul(rot, dir);
                    work.oldPos.c0 = n;
                    work.nextPos.c0 = pos;
                }

                workDataArray[cindex] = work;
            }
        }

        /// <summary>
        /// シミュレーションステップ後処理
        /// old姿勢の格納
        /// </summary>
        internal static void SimulationEndStep(
            DataChunk chunk,
            // team
            ref TeamManager.TeamData tdata,
            // collider
            ref NativeArray<float3> nowPositions,
            ref NativeArray<quaternion> nowRotations,
            ref NativeArray<float3> oldPositions,
            ref NativeArray<quaternion> oldRotations
            )
        {
            //int cindex = tdata.colliderChunk.startIndex;
            int cindex = tdata.colliderChunk.startIndex + chunk.startIndex;
            //for (int i = 0; i < tdata.colliderChunk.dataLength; i++, cindex++)
            for (int i = 0; i < chunk.dataLength; i++, cindex++)
            {
                oldPositions[cindex] = nowPositions[cindex];
                oldRotations[cindex] = nowRotations[cindex];
            }
        }

        /// <summary>
        /// シミュレーション更新後処理
        /// </summary>
        internal static void SimulationPostUpdate(
            // team
            ref TeamManager.TeamData tdata,
            // collider
            ref NativeArray<float3> framePositions,
            ref NativeArray<quaternion> frameRotations,
            ref NativeArray<float3> oldFramePositions,
            ref NativeArray<quaternion> oldFrameRotations
            )
        {
            if (tdata.colliderCount == 0)
                return;
            if (tdata.IsRunning == false)
                return;

            int cindex = tdata.colliderChunk.startIndex;
            for (int k = 0; k < tdata.colliderChunk.dataLength; k++, cindex++)
            {
                // コライダー履歴更新
                oldFramePositions[cindex] = framePositions[cindex];
                oldFrameRotations[cindex] = frameRotations[cindex];

            }
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"========== Collider Manager ==========");
            if (IsValid() == false)
            {
                sb.AppendLine($"Collider Manager. Invalid.");
            }
            else
            {
                int cnt = teamIdArray?.Count ?? 0;

                sb.AppendLine($"Use:{cnt}");
                sb.AppendLine($"  -flagArray:{flagArray.ToSummary()}");
                sb.AppendLine($"  -centerArray:{centerArray.ToSummary()}");
                sb.AppendLine($"  -sizeArray:{sizeArray.ToSummary()}");
                sb.AppendLine($"  -framePositions:{framePositions.ToSummary()}");
                sb.AppendLine($"  -frameRotations:{frameRotations.ToSummary()}");
                sb.AppendLine($"  -frameScales:{frameScales.ToSummary()}");
                sb.AppendLine($"  -oldFramePositions:{oldFramePositions.ToSummary()}");
                sb.AppendLine($"  -oldFrameRotations:{oldFrameRotations.ToSummary()}");
                sb.AppendLine($"  -nowPositions:{nowPositions.ToSummary()}");
                sb.AppendLine($"  -nowRotations:{nowRotations.ToSummary()}");
                sb.AppendLine($"  -oldPositions:{oldPositions.ToSummary()}");
                sb.AppendLine($"  -oldRotations:{oldRotations.ToSummary()}");
                sb.AppendLine($"  -mainColliderIndices:{mainColliderIndices.ToSummary()}");

                sb.AppendLine($"[Colliders]");
                int useCnt = 0;
                for (int i = 0; i < cnt; i++)
                {
                    var flag = flagArray[i];
                    if (flag.IsSet(Flag_Valid) == false)
                        continue;
                    useCnt++;
                    var ctype = DataUtility.GetColliderType(flag);
                    string sym = flag.IsSet(Flag_Symmetry) ? "Symmetry" : "";
                    sb.AppendLine($"  [{i}] tid:{teamIdArray[i]}, flag:0x{flag.Value:X}, type:{ctype}, size:{sizeArray[i]}, cen:{centerArray[i]}, {sym}");
                }
                sb.AppendLine($"  ActiveCount:{useCnt}");

                sb.AppendLine($"[Collider Components:{colliderSet.Count}]");
                foreach (var col in colliderSet)
                {
                    if (col)
                    {
                        sb.Append($"({col.UseTeamCount})  {col.name}");
                        if (col.ActiveSymmetryMode != ColliderSymmetryMode.None)
                        {
                            sb.Append($"  Symmetry:{col.ActiveSymmetryMode}");
                        }
                        sb.AppendLine();
                    }
                    else
                        sb.AppendLine($"  (null!)");
                }
            }
            sb.AppendLine();
            Debug.Log(sb.ToString());
            allsb.Append(sb);
        }
    }
}
