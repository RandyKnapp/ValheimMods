// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Collections;

namespace MagicaCloth2
{
    /// <summary>
    /// NativeParallelHashMapの拡張メソッド
    /// </summary>
    public static class NativeParallelHashMap
    {
        /// <summary>
        /// NativeParallelMultiHashMapが確保されている場合のみDispose()する
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="map"></param>
#if MC2_COLLECTIONS_200
        public static void MC2DisposeSafe<TKey, TValue>(ref this NativeParallelHashMap<TKey, TValue> map) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
#else
        public static void MC2DisposeSafe<TKey, TValue>(ref this NativeParallelHashMap<TKey, TValue> map) where TKey : struct, IEquatable<TKey> where TValue : struct, IEquatable<TValue>
#endif
        {
            if (map.IsCreated)
                map.Dispose();
        }
    }
}
