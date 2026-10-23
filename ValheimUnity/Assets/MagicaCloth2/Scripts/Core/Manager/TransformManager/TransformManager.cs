// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace MagicaCloth2
{
    public class TransformManager : IManager, IValid
    {
        // フラグ
        internal const byte Flag_Read = 0x01;
        internal const byte Flag_WorldRotWrite = 0x02; // ワールド回転のみ書き込み
        internal const byte Flag_LocalPosRotWrite = 0x04; // ローカル座標・回転書き込み
        internal const byte Flag_Restore = 0x08; // 復元する
        internal const byte Flag_Enable = 0x10; // 有効状態
        internal ExNativeArray<ExBitFlag8> flagArray;

        /// <summary>
        /// 初期localPosition
        /// </summary>
        internal ExNativeArray<float3> initLocalPositionArray;

        /// <summary>
        /// 初期localRotation
        /// </summary>
        internal ExNativeArray<quaternion> initLocalRotationArray;

        /// <summary>
        /// シミュレーションの基準となるlocalPosition
        /// </summary>
        internal ExNativeArray<float3> baseLocalPositionArray;

        /// <summary>
        /// シミュレーションの基準となるlocalRotation
        /// </summary>
        internal ExNativeArray<quaternion> baseLocalRotationArray;

        /// <summary>
        /// ワールド座標
        /// </summary>
        internal ExNativeArray<float3> positionArray;

        /// <summary>
        /// ワールド回転
        /// </summary>
        internal ExNativeArray<quaternion> rotationArray;

        /// <summary>
        /// ワールド逆回転
        /// </summary>
        //internal ExNativeArray<quaternion> inverseRotationArray;

        /// <summary>
        /// ワールドスケール
        /// Transform.lossyScaleと等価
        /// </summary>
        internal ExNativeArray<float3> scaleArray;

        /// <summary>
        /// ローカル座標
        /// </summary>
        internal ExNativeArray<float3> localPositionArray;

        /// <summary>
        /// ローカル回転
        /// </summary>
        internal ExNativeArray<quaternion> localRotationArray;

        /// <summary>
        /// ローカルスケール
        /// </summary>
        internal ExNativeArray<float3> localScaleArray;

        /// <summary>
        /// ワールド変換マトリックス
        /// </summary>
        internal ExNativeArray<float4x4> localToWorldMatrixArray;

        /// <summary>
        /// 接続チームID(0=なし)
        /// </summary>
        internal ExNativeArray<short> teamIdArray;

        /// <summary>
        /// 読み込み用トランスフォームアクセス配列
        /// この配列は上記の配列グループとインデックが同期している
        /// </summary>
        internal TransformAccessArray transformAccessArray;


        internal int Count => flagArray?.Count ?? 0;

        //=========================================================================================
        /// <summary>
        /// コンポーネント用ワールド姿勢
        /// </summary>
        internal ExNativeArray<float3> componentPositionArray;
        internal ExNativeArray<float> componentMinScaleArray; // このスケール値はXYZの最小の絶対値（用途は０スケール判定）

        /// <summary>
        /// コンポーネント用トランスフォーム
        /// </summary>
        internal TransformAccessArray componentTransformAccessArray;

        internal NativeReference<bool> existFixedTeam;

        bool isValid;

        //=========================================================================================
        public void Dispose()
        {
            isValid = false;

            flagArray?.Dispose();
            initLocalPositionArray?.Dispose();
            initLocalRotationArray?.Dispose();
            baseLocalPositionArray?.Dispose();
            baseLocalRotationArray?.Dispose();
            positionArray?.Dispose();
            rotationArray?.Dispose();
            //inverseRotationArray?.Dispose();
            scaleArray?.Dispose();
            localPositionArray?.Dispose();
            localRotationArray?.Dispose();
            localScaleArray?.Dispose();
            localToWorldMatrixArray?.Dispose();
            teamIdArray?.Dispose();
            //writeIndexArray?.Dispose();

            flagArray = null;
            initLocalPositionArray = null;
            initLocalRotationArray = null;
            baseLocalPositionArray = null;
            baseLocalRotationArray = null;
            positionArray = null;
            rotationArray = null;
            //inverseRotationArray = null;
            scaleArray = null;
            localPositionArray = null;
            localRotationArray = null;
            localScaleArray = null;
            localToWorldMatrixArray = null;
            teamIdArray = null;
            //writeIndexArray = null;

            if (transformAccessArray.isCreated)
                transformAccessArray.Dispose();
            //if (writeTransformAccessArray.isCreated)
            //    writeTransformAccessArray.Dispose();

            componentPositionArray?.Dispose();
            componentMinScaleArray?.Dispose();
            if (componentTransformAccessArray.isCreated)
                componentTransformAccessArray.Dispose();

            if (existFixedTeam.IsCreated)
                existFixedTeam.Dispose();
        }

        public void EnterdEditMode()
        {
            Dispose();
        }

        public void Initialize()
        {
            Dispose();

            const int capacity = 256;
            flagArray = new ExNativeArray<ExBitFlag8>(capacity);
            initLocalPositionArray = new ExNativeArray<float3>(capacity);
            initLocalRotationArray = new ExNativeArray<quaternion>(capacity);
            baseLocalPositionArray = new ExNativeArray<float3>(capacity);
            baseLocalRotationArray = new ExNativeArray<quaternion>(capacity);
            positionArray = new ExNativeArray<float3>(capacity);
            rotationArray = new ExNativeArray<quaternion>(capacity);
            //inverseRotationArray = new ExNativeArray<quaternion>(capacity);
            scaleArray = new ExNativeArray<float3>(capacity);
            localPositionArray = new ExNativeArray<float3>(capacity);
            localRotationArray = new ExNativeArray<quaternion>(capacity);
            localScaleArray = new ExNativeArray<float3>(capacity);
            localToWorldMatrixArray = new ExNativeArray<float4x4>(capacity);
            teamIdArray = new ExNativeArray<short>(capacity);

            transformAccessArray = new TransformAccessArray(capacity);

            componentPositionArray = new ExNativeArray<float3>(capacity);
            componentMinScaleArray = new ExNativeArray<float>(capacity);
            componentTransformAccessArray = new TransformAccessArray(capacity);

            existFixedTeam = new NativeReference<bool>(Allocator.Persistent);

            isValid = true;
        }

        public bool IsValid()
        {
            return isValid;
        }

        //=========================================================================================
        /// <summary>
        /// VirtualMeshのTransformDataを追加する
        /// </summary>
        /// <param name="tdata"></param>
        /// <returns></returns>
        internal DataChunk AddTransform(VirtualMeshContainer cmesh, int teamId)
        {
            if (isValid == false)
                return default;

            Debug.Assert(cmesh != null);
            int cnt = cmesh.GetTransformCount();

            // データコピー追加
            var c = flagArray.AddRange(cmesh.shareVirtualMesh.transformData.flagArray);
            initLocalPositionArray.AddRange(cmesh.shareVirtualMesh.transformData.initLocalPositionArray);
            initLocalRotationArray.AddRange(cmesh.shareVirtualMesh.transformData.initLocalRotationArray);
            baseLocalPositionArray.AddRange(cmesh.shareVirtualMesh.transformData.initLocalPositionArray);
            baseLocalRotationArray.AddRange(cmesh.shareVirtualMesh.transformData.initLocalRotationArray);

            // 領域のみ追加
            positionArray.AddRange(cnt);
            rotationArray.AddRange(cnt);
            //inverseRotationArray.AddRange(cnt);
            scaleArray.AddRange(cnt);
            localPositionArray.AddRange(cnt);
            localRotationArray.AddRange(cnt);
            localScaleArray.AddRange(cnt);
            localToWorldMatrixArray.AddRange(cnt);

            // チームID
            teamIdArray.AddRange(cnt, (short)teamId);

            // トランスフォーム
            int nowcnt = transformAccessArray.length;

            // データチャンクの開始まで埋める
            var meshT = cmesh.GetCenterTransform();
            int start = c.startIndex;
            while (nowcnt < start)
            {
                transformAccessArray.Add(meshT);
                nowcnt++;
            }

            for (int i = 0; i < cnt; i++)
            {
                Transform t = cmesh.GetTransformFromIndex(i);
                if (t == null)
                    t = meshT;
                int index = c.startIndex + i;
                if (index < nowcnt)
                    transformAccessArray[index] = t;
                else
                    transformAccessArray.Add(t);
            }

            return c;
        }

        /// <summary>
        /// Transformの領域のみ追加する
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        internal DataChunk AddTransform(int count, int teamId, Transform t)
        {
            if (isValid == false)
                return default;

            // 領域のみ追加する
            var c = flagArray.AddRange(count);
            initLocalPositionArray.AddRange(count);
            initLocalRotationArray.AddRange(count);
            baseLocalPositionArray.AddRange(count);
            baseLocalRotationArray.AddRange(count);
            positionArray.AddRange(count);
            rotationArray.AddRange(count);
            //inverseRotationArray.AddRange(count);
            scaleArray.AddRange(count);
            localPositionArray.AddRange(count);
            localRotationArray.AddRange(count);
            localScaleArray.AddRange(count);
            localToWorldMatrixArray.AddRange(count);

            // チームID
            teamIdArray.AddRange(count, (short)teamId);

            // トランスフォームはすべてnullで登録する(Unity6.1でnull登録はNGになった！)
            int nowcnt = transformAccessArray.length;

            // データチャンクの開始まで埋める
            int start = c.startIndex;
            while (nowcnt < start)
            {
                transformAccessArray.Add(t);
                nowcnt++;
            }

            for (int i = 0; i < count; i++)
            {
                //Transform t = null;
                int index = c.startIndex + i;
                if (index < nowcnt)
                    transformAccessArray[index] = t;
                else
                    transformAccessArray.Add(t);
            }

            return c;
        }

        /// <summary>
        /// Transform１つを追加する
        /// </summary>
        /// <param name="t"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        internal DataChunk AddTransform(Transform t, ExBitFlag8 flag, int teamId)
        {
            if (isValid == false)
                return default;

            // データコピー追加
            var c = flagArray.Add(flag);
            initLocalPositionArray.Add(t.localPosition);
            initLocalRotationArray.Add(t.localRotation);
            baseLocalPositionArray.Add(t.localPosition);
            baseLocalRotationArray.Add(t.localRotation);
            positionArray.Add(t.position);
            rotationArray.Add(t.rotation);
            //inverseRotationArray.Add(math.inverse(t.rotation));
            scaleArray.Add(t.lossyScale);
            localPositionArray.Add(t.localPosition);
            localRotationArray.Add(t.localRotation);
            localScaleArray.Add(t.localScale);
            localToWorldMatrixArray.Add(float4x4.identity); // ここは単位行列

            // チームID
            teamIdArray.Add((short)teamId);

            // トランスフォーム
            int nowcnt = transformAccessArray.length;
            int index = c.startIndex;
            if (index < nowcnt)
                transformAccessArray[index] = t;
            else
                transformAccessArray.Add(t);

            return c;
        }

        /// <summary>
        /// Transform情報を書き換える
        /// </summary>
        /// <param name="t"></param>
        /// <param name="flag"></param>
        /// <param name="index"></param>
        internal void SetTransform(Transform t, ExBitFlag8 flag, int index, int teamId)
        {
            if (isValid == false)
                return;

            if (t != null)
            {
                // データ設定
                flagArray[index] = flag;
                initLocalPositionArray[index] = t.localPosition;
                initLocalRotationArray[index] = t.localRotation;
                baseLocalPositionArray[index] = t.localPosition;
                baseLocalRotationArray[index] = t.localRotation;
                positionArray[index] = t.position;
                rotationArray[index] = t.rotation;
                //inverseRotationArray[index] = math.inverse(t.rotation);
                scaleArray[index] = t.lossyScale;
                localPositionArray[index] = t.localPosition;
                localRotationArray[index] = t.localRotation;
                localScaleArray[index] = t.localScale;
                //localToWorldMatrix[index] = t.localToWorldMatrix; // ここは不要
                teamIdArray[index] = (short)teamId;
                transformAccessArray[index] = t;
            }
            else
            {
                // データクリア（無効化）
                flagArray[index] = default;
                transformAccessArray[index] = null;
                teamIdArray[index] = 0;
            }
        }

        /// <summary>
        /// Transform情報をコピーする
        /// </summary>
        /// <param name="fromIndex"></param>
        /// <param name="toIndex"></param>
        internal void CopyTransform(int fromIndex, int toIndex)
        {
            if (isValid == false)
                return;

            flagArray[toIndex] = flagArray[fromIndex];
            initLocalPositionArray[toIndex] = initLocalPositionArray[fromIndex];
            initLocalRotationArray[toIndex] = initLocalRotationArray[fromIndex];
            baseLocalPositionArray[toIndex] = baseLocalPositionArray[fromIndex];
            baseLocalRotationArray[toIndex] = baseLocalRotationArray[fromIndex];
            positionArray[toIndex] = positionArray[fromIndex];
            rotationArray[toIndex] = rotationArray[fromIndex];
            //inverseRotationArray[toIndex] = inverseRotationArray[fromIndex];
            scaleArray[toIndex] = scaleArray[fromIndex];
            localPositionArray[toIndex] = localPositionArray[fromIndex];
            localRotationArray[toIndex] = localRotationArray[fromIndex];
            localScaleArray[toIndex] = localScaleArray[fromIndex];
            //localToWorldMatrix[toIndex] = localToWorldMatrix[fromIndex]; // ここは不要
            transformAccessArray[toIndex] = transformAccessArray[fromIndex];
            teamIdArray[toIndex] = teamIdArray[fromIndex];
        }

        /// <summary>
        /// トランスフォームを削除する
        /// </summary>
        /// <param name="c"></param>
        internal void RemoveTransform(DataChunk c)
        {
            if (isValid == false)
                return;
            if (c.IsValid == false)
                return;

            flagArray.RemoveAndFill(c);
            initLocalPositionArray.Remove(c);
            initLocalRotationArray.Remove(c);
            baseLocalPositionArray.Remove(c);
            baseLocalRotationArray.Remove(c);
            positionArray.Remove(c);
            rotationArray.Remove(c);
            //inverseRotationArray.Remove(c);
            scaleArray.Remove(c);
            localPositionArray.Remove(c);
            localRotationArray.Remove(c);
            localScaleArray.Remove(c);
            localToWorldMatrixArray.Remove(c);
            teamIdArray.RemoveAndFill(c, 0);

            // トランスフォーム削除
            for (int i = 0; i < c.dataLength; i++)
            {
                int index = c.startIndex + i;
                transformAccessArray[index] = null;
            }
        }

        /// <summary>
        /// トランスフォームの有効状態を切り替える
        /// </summary>
        /// <param name="c"></param>
        /// <param name="sw">true=有効, false=無効</param>
        internal void EnableTransform(DataChunk c, bool sw)
        {
            if (isValid == false)
                return;
            if (c.IsValid == false)
                return;

            var job = new EnableTransformJob()
            {
                chunk = c,
                sw = sw,
                flagList = flagArray.GetNativeArray(),
            };
            job.Run();
        }

        [BurstCompile]
        struct EnableTransformJob : IJob
        {
            public DataChunk chunk;
            public bool sw;
            public NativeArray<ExBitFlag8> flagList;

            public void Execute()
            {
                for (int i = 0; i < chunk.dataLength; i++)
                {
                    int index = chunk.startIndex + i;

                    var flag = flagList[index];
                    if (flag.Value == 0)
                        continue;

                    flag.SetFlag(Flag_Enable, sw);
                    flagList[index] = flag;
                }
            }
        }

        /// <summary>
        /// トランスフォームの有効状態を切り替える
        /// </summary>
        /// <param name="index"></param>
        /// <param name="sw">true=有効, false=無効</param>
        internal void EnableTransform(int index, bool sw)
        {
            if (isValid == false)
                return;
            if (index < 0)
                return;

            var flag = flagArray[index];
            if (flag.Value == 0)
                return;
            flag.SetFlag(Flag_Enable, sw);
            flagArray[index] = flag;
        }

        internal DataChunk Expand(DataChunk c, int newLength)
        {
            if (isValid == false)
                return default;

            // 領域
            var nc = flagArray.Expand(c, newLength);
            initLocalPositionArray.Expand(c, newLength);
            initLocalRotationArray.Expand(c, newLength);
            baseLocalPositionArray.Expand(c, newLength);
            baseLocalRotationArray.Expand(c, newLength);
            positionArray.Expand(c, newLength);
            rotationArray.Expand(c, newLength);
            //inverseRotationArray.Expand(c, newLength);
            scaleArray.Expand(c, newLength);
            localPositionArray.Expand(c, newLength);
            localRotationArray.Expand(c, newLength);
            localScaleArray.Expand(c, newLength);
            localToWorldMatrixArray.Expand(c, newLength);

            // チームID
            teamIdArray.Expand(c, newLength);

            // トランスフォームアクセス配列の拡張
            if (c.startIndex != nc.startIndex)
            {
                // 旧領域の先頭Transform
                Transform frontT = transformAccessArray[c.startIndex];

                while (transformAccessArray.length < (nc.startIndex + nc.dataLength))
                    transformAccessArray.Add(frontT);

                for (int i = 0; i < c.dataLength; i++)
                {
                    Transform t = transformAccessArray[c.startIndex + i];
                    transformAccessArray[nc.startIndex + i] = t;
                    transformAccessArray[c.startIndex + i] = null;
                }
            }

            return nc;
        }

        //=========================================================================================
        /// <summary>
        /// Transformを初期姿勢で復元させるジョブを発行する
        /// </summary>
        /// <param name="count"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public JobHandle RestoreTransform(JobHandle jobHandle)
        {
            existFixedTeam.Value = false;
            if (Count > 0)
            {
                //Debug.Log("RestoreTransform");
                var job = new RestoreTransformJob()
                {
                    existFixedTeam = existFixedTeam,

                    flagList = flagArray.GetNativeArray(),
                    localPositionArray = initLocalPositionArray.GetNativeArray(),
                    localRotationArray = initLocalRotationArray.GetNativeArray(),
                    teamIdArray = teamIdArray.GetNativeArray(),

                    teamDataArray = MagicaManager.Team.teamDataArray.GetNativeArray(),
                };
                jobHandle = job.Schedule(transformAccessArray, jobHandle);
            }

            return jobHandle;
        }

        [BurstCompile]
        struct RestoreTransformJob : IJobParallelForTransform
        {
            [NativeDisableParallelForRestriction]
            public NativeReference<bool> existFixedTeam;

            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> flagList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> localRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;


            public void Execute(int index, TransformAccess transform)
            {
                if (transform.isValid == false)
                    return;
                var flag = flagList[index];
                if (flag.IsSet(Flag_Restore) == false)
                    return;

                int teamId = teamIdArray[index];
                var tdata = teamDataArray[teamId];

                // 一度のみ復元フラグが立っている場合は実行する
                if (flag.IsSet(Flag_Enable) == false && tdata.flag.IsSet(TeamManager.Flag_RestoreTransformOnlyOnec) == false)
                    return;

                // Keepカリング時はスキップする
                if ((tdata.IsCameraCullingInvisible && tdata.IsCameraCullingKeep) || tdata.IsDistanceCullingInvisible)
                    return;

                transform.SetLocalPositionAndRotation(localPositionArray[index], localRotationArray[index]);

                // 物理更新チームの存在
                if (tdata.IsFixedUpdate)
                    existFixedTeam.Value = true;

                //Debug.Log($"RestoreTransform [{index}] lpos:{localPositionArray[index]}, lrot:{localRotationArray[index]}");
            }
        }

        //=========================================================================================
        /// <summary>
        /// UnityPhysicsチームかつFixedUpdateが実行されなかったフレームは退避させておいたベース姿勢で再度復元させる
        /// </summary>
        /// <param name="count"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public JobHandle RestoreBaseTransform(JobHandle jobHandle)
        {
            if (Count > 0)
            {
                //Debug.Log("RestoreTransform");
                var job = new RestoreBaseTransformJob()
                {
                    flagList = flagArray.GetNativeArray(),
                    baseLocalPositionArray = baseLocalPositionArray.GetNativeArray(),
                    baseLocalRotationArray = baseLocalRotationArray.GetNativeArray(),
                    teamIdArray = teamIdArray.GetNativeArray(),

                    teamDataArray = MagicaManager.Team.teamDataArray.GetNativeArray(),
                };
                jobHandle = job.Schedule(transformAccessArray, jobHandle);
            }

            return jobHandle;
        }

        [BurstCompile]
        struct RestoreBaseTransformJob : IJobParallelForTransform
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> flagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> baseLocalPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> baseLocalRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;


            public void Execute(int index, TransformAccess transform)
            {
                if (transform.isValid == false)
                    return;
                var flag = flagList[index];
                if (flag.IsSet(Flag_Restore) == false)
                    return;

                int teamId = teamIdArray[index];
                var tdata = teamDataArray[teamId];

                // Fixed更新チームのみ
                if (tdata.IsFixedUpdate == false)
                    return;

                // 一度のみ復元フラグが立っている場合は実行する
                if (flag.IsSet(Flag_Enable) == false && tdata.flag.IsSet(TeamManager.Flag_RestoreTransformOnlyOnec) == false)
                    return;

                // Keepカリング時はスキップする
                if ((tdata.IsCameraCullingInvisible && tdata.IsCameraCullingKeep) || tdata.IsDistanceCullingInvisible)
                    return;

                transform.SetLocalPositionAndRotation(baseLocalPositionArray[index], baseLocalRotationArray[index]);

                //Debug.Log($"RestoreTransform [{index}] lpos:{baseLocalPositionArray[index]}, lrot:{baseLocalRotationArray[index].value}");
            }
        }

        //=========================================================================================
        /// <summary>
        /// トランスフォームを読み込むジョブを発行する
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public JobHandle ReadTransformSchedule(JobHandle jobHandle)
        {
            if (Count > 0)
            {
                var job = new ReadTransformJob()
                {
                    fixedUpdateCount = MagicaManager.Time.FixedUpdateCount,

                    flagList = flagArray.GetNativeArray(),
                    positionArray = positionArray.GetNativeArray(),
                    rotationArray = rotationArray.GetNativeArray(),
                    scaleList = scaleArray.GetNativeArray(),
                    localPositionArray = localPositionArray.GetNativeArray(),
                    localRotationArray = localRotationArray.GetNativeArray(),
                    localScaleArray = localScaleArray.GetNativeArray(),
                    //inverseRotationArray = inverseRotationArray.GetNativeArray(),
                    localToWorldMatrixArray = localToWorldMatrixArray.GetNativeArray(),
                    baseLocalPositionArray = baseLocalPositionArray.GetNativeArray(),
                    baseLocalRotationArray = baseLocalRotationArray.GetNativeArray(),

                    teamIdArray = teamIdArray.GetNativeArray(),

                    teamDataArray = MagicaManager.Team.teamDataArray.GetNativeArray(),
                };
                jobHandle = job.ScheduleReadOnly(transformAccessArray, 8, jobHandle);
            }

            return jobHandle;
        }

        [BurstCompile]
        struct ReadTransformJob : IJobParallelForTransform
        {
            public int fixedUpdateCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> flagList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> positionArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> rotationArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> scaleList;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> localPositionArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> localRotationArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> localScaleArray;
            //[Unity.Collections.WriteOnly]
            //public NativeArray<quaternion> inverseRotationArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float4x4> localToWorldMatrixArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> baseLocalPositionArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> baseLocalRotationArray;

            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            public void Execute(int index, TransformAccess transform)
            {
                if (transform.isValid == false)
                    return;
                var flag = flagList[index];
                if (flag.IsSet(Flag_Enable) == false)
                    return;
                if (flag.IsSet(Flag_Read) == false)
                    return;

                // カリング時は読み込まない
                int teamId = teamIdArray[index];
                var tdata = teamDataArray[teamId];
                if (tdata.IsCullingInvisible)
                    return;

                transform.GetPositionAndRotation(out var pos, out var rot);
                float4x4 LtoW = transform.localToWorldMatrix;

                positionArray[index] = pos;
                rotationArray[index] = rot;
                localPositionArray[index] = transform.localPosition;
                localRotationArray[index] = transform.localRotation;
                localScaleArray[index] = transform.localScale;

                // マトリックスから正確なスケール値を算出する（これはTransform.lossyScaleと等価）
                var irot = math.inverse(rot);
                var m2 = math.mul(new float4x4(irot, float3.zero), LtoW);
                var scl = new float3(m2.c0.x, m2.c1.y, m2.c2.z);
                scaleList[index] = scl;

                //Debug.Log($"ReadTransform [{index}] pos:{pos}, rot:{rot}, scl:{scl}");
                //Debug.Log($"LtoW:\n{LtoW}");

                // ワールド->ローカル変換用の逆クォータニオン
                //inverseRotationArray[index] = math.inverse(rot);

                // ワールド変換マトリックス
                localToWorldMatrixArray[index] = LtoW;

                // 今回の姿勢を基本姿勢として退避させる
                // Fixed更新チームかつFixedUpdateが実行されている場合、そもそもFixed更新出ない場合は毎回
                if ((tdata.IsFixedUpdate && fixedUpdateCount > 0) || tdata.IsFixedUpdate == false)
                {
                    baseLocalPositionArray[index] = transform.localPosition;
                    baseLocalRotationArray[index] = transform.localRotation;
                }

                //Debug.Log($"ReadTransform [{index}] pos:{pos}, rot:{rot}");
            }
        }

        //=========================================================================================
        /// <summary>
        /// Transformを書き込むジョブを発行する
        /// </summary>
        /// <param name="count"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public JobHandle WriteTransformSchedule(JobHandle jobHandle)
        {
            var job = new WriteTransformJob()
            {
                flagList = flagArray.GetNativeArray(),
                worldPositions = positionArray.GetNativeArray(),
                worldRotations = rotationArray.GetNativeArray(),
                localPositions = localPositionArray.GetNativeArray(),
                localRotations = localRotationArray.GetNativeArray(),

                teamIdArray = teamIdArray.GetNativeArray(),

                teamDataArray = MagicaManager.Team.teamDataArray.GetNativeArray(),
            };
            jobHandle = job.Schedule(transformAccessArray, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct WriteTransformJob : IJobParallelForTransform
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> flagList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> worldPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> worldRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> localRotations;

            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            public void Execute(int index, TransformAccess transform)
            {
                if (transform.isValid == false)
                    return;
                var flag = flagList[index];
                if (flag.IsSet(Flag_Enable) == false)
                    return;

                int teamId = teamIdArray[index];
                var tdata = teamDataArray[teamId];

                // カリング時は書き込まない
                if (tdata.IsCullingInvisible)
                    return;

                // 極小スケール時は書き込まない
                if (tdata.IsScaleSuspend)
                    return;

                // 書き込み停止中ならスキップ
                if (tdata.flag.IsSet(TeamManager.Flag_SkipWriting))
                    return;

                if (flag.IsSet(Flag_WorldRotWrite))
                {
                    // ワールド回転
                    transform.rotation = worldRotations[index];
                    //Debug.Log($"WriteTransform [{index}] (World!) rot:{worldRotations[index]}");

                    // BoneSpringのみワールド座標を書き込む
                    if (tdata.IsSpring)
                    {
                        transform.position = worldPositions[index];
                    }
                }
                else if (flag.IsSet(Flag_LocalPosRotWrite))
                {
                    // ローカル座標・回転を書き込む
                    transform.SetLocalPositionAndRotation(localPositions[index], localRotations[index]);

                    //Debug.Log($"WriteTransform [{index}] (local!) lpos:{localPositions[index]}, lrot:{localRotations[index]}");
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// コンポーネント用トランスフォームの登録
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        internal int AddComponentTransform(Transform t)
        {
            if (isValid == false)
                return -1;
            Debug.Assert(t);

            int index = componentPositionArray.Add(float3.zero).startIndex;
            componentMinScaleArray.Add(1);

            // トランスフォーム
            int nowcnt = componentTransformAccessArray.length;
            if (index < nowcnt)
                componentTransformAccessArray[index] = t;
            else
                componentTransformAccessArray.Add(t);

            return index;
        }

        /// <summary>
        /// コンポーネント用トランスフォームの削除
        /// </summary>
        /// <param name="index"></param>
        internal void RemoveComponentTransform(int index)
        {
            if (isValid == false)
                return;
            if (index < 0)
                return;

            componentPositionArray.Remove(index);
            componentMinScaleArray.Remove(index);

            // トランスフォーム削除
            componentTransformAccessArray[index] = null;
        }

        /// <summary>
        /// トランスフォームを読み込むジョブを発行する
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle ReadComponentTransform(JobHandle jobHandle)
        {
            if (componentPositionArray.Count > 0)
            {
                var job = new ReadComponentTransformJob()
                {
                    positionArray = componentPositionArray.GetNativeArray(),
                    minScaleArray = componentMinScaleArray.GetNativeArray(),
                };
                jobHandle = job.ScheduleReadOnly(componentTransformAccessArray, 16, jobHandle);
            }

            return jobHandle;
        }

        [BurstCompile]
        struct ReadComponentTransformJob : IJobParallelForTransform
        {
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> positionArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float> minScaleArray;

            public void Execute(int index, TransformAccess transform)
            {
                if (transform.isValid == false)
                    return;

                positionArray[index] = transform.position;

                // スケールXYZの最小の絶対値
                // コンポーネントの０スケール判定に使用
                float3 scl = transform.localToWorldMatrix.lossyScale;
                float minScale = math.cmin(math.abs(scl));
                minScaleArray[index] = minScale;

                //Debug.Log(scl);
                //Debug.Log(minScale);
            }
        }


        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"========== Transform Manager ==========");
            if (IsValid() == false)
            {
                sb.AppendLine($"Transform Manager. Invalid.");
            }
            else
            {
                // transform
                int tcnt = transformAccessArray.isCreated ? transformAccessArray.length : 0;
                sb.AppendLine($"Transform Manager. Length:{tcnt}");
                sb.AppendLine($"  -flagArray:{flagArray.ToSummary()}");
                sb.AppendLine($"  -initLocalPositionArray:{initLocalPositionArray.ToSummary()}");
                sb.AppendLine($"  -initLocalRotationArray:{initLocalRotationArray.ToSummary()}");
                sb.AppendLine($"  -baseLocalPositionArray:{baseLocalPositionArray.ToSummary()}");
                sb.AppendLine($"  -baseLocalRotationArray:{baseLocalRotationArray.ToSummary()}");
                sb.AppendLine($"  -positionArray:{positionArray.ToSummary()}");
                sb.AppendLine($"  -rotationArray:{rotationArray.ToSummary()}");
                //sb.AppendLine($"  -inverseRotationArray:{inverseRotationArray.ToSummary()}");
                sb.AppendLine($"  -scaleArray:{scaleArray.ToSummary()}");
                sb.AppendLine($"  -localPositionArray:{localPositionArray.ToSummary()}");
                sb.AppendLine($"  -localRotationArray:{localRotationArray.ToSummary()}");
                sb.AppendLine($"  -localScaleArray:{localScaleArray.ToSummary()}");
                sb.AppendLine($"  -localToWorldMatirxArray:{localToWorldMatrixArray.ToSummary()}");
                sb.AppendLine($"  -teamIdArray:{teamIdArray.ToSummary()}");

                if (transformAccessArray.isCreated)
                {
                    for (int i = 0; i < tcnt; i++)
                    {
                        var t = transformAccessArray[i];
                        var flag = flagArray[i];
                        var teamId = teamIdArray[i];
                        sb.Append($"  [{i}] team:{teamId} (");
                        sb.Append(flag.IsSet(Flag_Enable) ? "E" : "");
                        sb.Append(flag.IsSet(Flag_Restore) ? "R" : "");
                        sb.Append(flag.IsSet(Flag_Read) ? "r" : "");
                        sb.Append(flag.IsSet(Flag_WorldRotWrite) ? "W" : "");
                        sb.Append(flag.IsSet(Flag_LocalPosRotWrite) ? "w" : "");
                        if (t)
                            sb.AppendLine($") {t.name}");
                        else
                            sb.AppendLine($") (null)");
                    }
                }

                // component
                tcnt = componentTransformAccessArray.isCreated ? componentTransformAccessArray.length : 0;
                sb.AppendLine($"Component Transform Manager. Length:{tcnt}");
                sb.AppendLine($"  -componentPositionArray:{componentPositionArray.ToSummary()}");
                sb.AppendLine($"  -componentMinScaleArray:{componentMinScaleArray.ToSummary()}");
                if (componentTransformAccessArray.isCreated)
                {
                    for (int i = 0; i < tcnt; i++)
                    {
                        var t = componentTransformAccessArray[i];
                        var flag = flagArray[i];
                        var teamId = teamIdArray[i];
                        sb.Append($"  [{i}] team:{teamId} (");
                        sb.Append(flag.IsSet(Flag_Enable) ? "E" : "");
                        sb.Append(flag.IsSet(Flag_Restore) ? "R" : "");
                        sb.Append(flag.IsSet(Flag_Read) ? "r" : "");
                        sb.Append(flag.IsSet(Flag_WorldRotWrite) ? "W" : "");
                        sb.Append(flag.IsSet(Flag_LocalPosRotWrite) ? "w" : "");
                        if (t)
                            sb.AppendLine($") {t.name}");
                        else
                            sb.AppendLine($") (null)");
                    }
                }
            }
            sb.AppendLine();
            Debug.Log(sb.ToString());
            allsb.Append(sb);
        }
    }
}
