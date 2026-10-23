// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Mathematics;

namespace MagicaCloth2
{
    /// <summary>
    /// Nativeバッファへのインターロック書き込み制御関連
    /// </summary>
    public static class InterlockUtility
    {
        /// <summary>
        /// 固定小数点への変換倍率
        /// </summary>
        internal const int ToFixed = 1000000;

        /// <summary>
        /// 少数への復元倍率
        /// </summary>
        internal const float ToFloat = 0.000001f;

        //=========================================================================================
        /// <summary>
        /// 集計バッファの指定インデックスにfloat3を固定小数点として加算しカウンタをインクリメントする
        /// </summary>
        /// <param name="index"></param>
        /// <param name="add"></param>
        /// <param name="cntPt"></param>
        /// <param name="sumPt"></param>
        unsafe internal static void AddFloat3(int index, float3 add, int* cntPt, int* sumPt)
        {
            Interlocked.Increment(ref cntPt[index]);
            int3 iadd = (int3)(add * ToFixed);
            //Debug.Log($"InterlockAdd [{index}]:{iadd}");
            index *= 3;
            for (int i = 0; i < 3; i++, index++)
            {
                if (iadd[i] != 0)
                    Interlocked.Add(ref sumPt[index], iadd[i]);
            }
        }

        /// <summary>
        /// 集計バッファの指定インデックスにfloat3を固定小数点として加算する（カウントは操作しない）
        /// </summary>
        /// <param name="index"></param>
        /// <param name="add"></param>
        /// <param name="sumPt"></param>
        unsafe internal static void AddFloat3(int index, float3 add, int* sumPt)
        {
            int3 iadd = (int3)(add * ToFixed);
            index *= 3;
            for (int i = 0; i < 3; i++, index++)
            {
                if (iadd[i] != 0)
                    Interlocked.Add(ref sumPt[index], iadd[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static void Max(int index, float value, int* pt)
        {
            int ival = (int)value * ToFixed;
            int now = pt[index];
            int oldNow = now + 1;

            while (ival > now && now != oldNow)
            {
                oldNow = now;
                now = Interlocked.CompareExchange(ref pt[index], ival, now);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static float3 ReadAverageFloat3(int index, int* cntPt, int* sumPt)
        {
            int count = cntPt[index];
            if (count == 0)
                return 0;

            int dataIndex = index * 3;

            // 集計
            float3 add = new float3(sumPt[dataIndex], sumPt[dataIndex + 1], sumPt[dataIndex + 2]);
            add /= count;

            // データは固定小数点なので戻す
            add *= ToFloat;

            return add;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static float3 ReadFloat3(int index, int* vecPt)
        {
            int dataIndex = index * 3;
            float3 v = new float3(vecPt[dataIndex], vecPt[dataIndex + 1], vecPt[dataIndex + 2]);

            // データは固定小数点なので戻す
            v *= ToFloat;

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static float ReadFloat(int index, int* floatPt)
        {
            return floatPt[index] * ToFloat;
        }
    }
}
