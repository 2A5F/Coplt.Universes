using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Coplt.Universes.Utilities;

namespace Coplt.Universes.Collections;

public interface IHashSearchCtrl<out R, C>
    where R : allows ref struct
    where C : allows ref struct
{
    uint Size { get; }
    /// <summary>
    /// must size += 1
    /// </summary>
    C Add();
    C At(uint index);
    bool Eq(C ctx);
    ulong Hash(C ctx);
    R Get(C ctx);
}

/// <summary>
/// https://github.com/martinus/unordered_dense
/// </summary>
[StructLayout(LayoutKind.Auto)]
public struct SHashSearcher
{
    /// <summary>
    /// 2^(64-m_shift) number of buckets
    /// </summary>
    public const byte initial_shifts = 64 - 2;
    public const float default_max_load_factor = 0.8F;

    public const uint max_buckets = 1u << (sizeof(uint) * 8 - 1);

    public struct Bucket(uint mDistAndFingerprint, uint mValueIdx)
    {
        /// <summary>
        /// skip 1 byte fingerprint
        /// </summary>
        public const uint dist_inc = 1u << 8;
        /// <summary>
        /// mask for 1 byte of fingerprint
        /// </summary>
        public const uint fingerprint_mask = dist_inc - 1;

        /// <summary>
        /// upper 3 byte: distance to original bucket. lower byte: fingerprint from hash
        /// </summary>
        public uint m_dist_and_fingerprint = mDistAndFingerprint;
        public uint m_value_idx = mValueIdx;

        public override string ToString() => $"{m_dist_and_fingerprint}, {m_value_idx}";
    }

    private Bucket[] m_buckets = [];
    private uint m_num_buckets;
    private uint m_max_bucket_capacity;
    private float m_max_load_factor = default_max_load_factor;
    private byte m_shifts = initial_shifts;

    public SHashSearcher()
    {
        CreateBucketFromShift();
    }

    #region Misc

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint DistAndFingerprintFromHash(ulong hash) =>
        (uint)(Bucket.dist_inc | (hash & Bucket.fingerprint_mask));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint GetBucketIndex(ulong hash) => (uint)(hash >> m_shifts);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint NextBucketIndex(uint index)
    {
        var next = index + 1;
        return next >= m_num_buckets ? 0 : next;
    }

    #endregion

    #region Grow

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint CalcNumBuckets(uint shifts) => uint.Min(1u << (int)(64u - shifts), max_buckets);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CreateBucketFromShift()
    {
        m_num_buckets = CalcNumBuckets(m_shifts);
        m_buckets = new Bucket[m_num_buckets];
        m_max_bucket_capacity = m_num_buckets != max_buckets
            ? (uint)(m_num_buckets * m_max_load_factor)
            : max_buckets;
    }

    private void Grow<S, R, C>(S search)
        where S : IHashSearchCtrl<R, C>, allows ref struct
        where R : allows ref struct
        where C : allows ref struct
    {
        if (m_max_bucket_capacity == max_buckets) throw new OverflowException();
        --m_shifts;
        CreateBucketFromShift();
        ReCalcBuckets<S, R, C>(search);
    }

    #endregion

    #region CalcBucket

    private void ReCalcBuckets<S, R, C>(S search)
        where S : IHashSearchCtrl<R, C>, allows ref struct
        where R : allows ref struct
        where C : allows ref struct
    {
        for (uint value_idx = 0, end_idx = search.Size; value_idx < end_idx; ++value_idx)
        {
            var ctx = search.At(value_idx);
            var (dist_and_fingerprint, bucket) = NextWhileLess<S, R, C>(search, ctx);

            // we know for certain that key has not yet been inserted, so no need to check it.
            PlaceAndShiftUp(new(dist_and_fingerprint, value_idx), bucket);
        }
    }

    private (uint dist_and_fingerprint, uint bucket) NextWhileLess<S, R, C>(S search, C ctx)
        where S : IHashSearchCtrl<R, C>, allows ref struct
        where R : allows ref struct
        where C : allows ref struct
    {
        var hash = search.Hash(ctx);
        var dist_and_fingerprint = DistAndFingerprintFromHash(hash);
        var bucket_index = GetBucketIndex(hash);

        while (dist_and_fingerprint < m_buckets.GetUnchecked(bucket_index).m_dist_and_fingerprint)
        {
            dist_and_fingerprint += Bucket.dist_inc;
            bucket_index = NextBucketIndex(bucket_index);
        }
        return (dist_and_fingerprint, bucket_index);
    }

    private void PlaceAndShiftUp(Bucket bucket, uint place)
    {
        ref var slot = ref m_buckets.GetUnchecked(place);
        while (0 != slot.m_dist_and_fingerprint)
        {
            (bucket, slot) = (slot, bucket);
            place = NextBucketIndex(place);
            bucket.m_dist_and_fingerprint += Bucket.dist_inc;
            slot = ref m_buckets.GetUnchecked(place);
        }
        slot = bucket;
    }

    #endregion

    #region TryEmplace

    public R UnsafeTryEmplace<S, R, C>(S search, ulong hash, out bool is_new)
        where S : IHashSearchCtrl<R, C>, allows ref struct
        where R : allows ref struct
        where C : allows ref struct
    {
        var dist_and_fingerprint = DistAndFingerprintFromHash(hash);
        var bucket_index = GetBucketIndex(hash);

        for (;;)
        {
            ref var bucket = ref m_buckets.GetUnchecked(bucket_index);
            if (dist_and_fingerprint == bucket.m_dist_and_fingerprint)
            {
                var ctx = search.At(bucket.m_value_idx);
                if (search.Eq(ctx))
                {
                    is_new = false;
                    return search.Get(ctx);
                }
            }
            else if (dist_and_fingerprint > bucket.m_dist_and_fingerprint)
            {
                return UnsafePlaceAt<S, R, C>(search, dist_and_fingerprint, bucket_index, out is_new);
            }
            dist_and_fingerprint += Bucket.dist_inc;
            bucket_index = NextBucketIndex(bucket_index);
        }
    }

    private R UnsafePlaceAt<S, R, C>(S search, uint dist_and_fingerprint, uint bucket_idx, out bool is_new)
        where S : IHashSearchCtrl<R, C>, allows ref struct
        where R : allows ref struct
        where C : allows ref struct
    {
        var size = search.Size;
        if (size > m_max_bucket_capacity) Grow<S, R, C>(search);

        var bucket = new Bucket { m_dist_and_fingerprint = dist_and_fingerprint, m_value_idx = size };
        PlaceAndShiftUp(bucket, bucket_idx);

        var ctx = search.Add();
        is_new = true;
        return search.Get(ctx);
    }

    #endregion
}

[StructLayout(LayoutKind.Auto)]
public struct SHashSet<T, HashWrapper>() where HashWrapper : IHashWrapper
{
    internal SVector<T> m_items = new();
    internal SHashSearcher m_hash_searcher = new();

    public int Count => m_items.Count;

    internal readonly ref struct Ctrl(ref SHashSet<T, HashWrapper> self, ref T item)
        : IHashSearchCtrl<RefBox<T>, RefBox<T>>
    {
        private readonly ref SHashSet<T, HashWrapper> self = ref self;
        private readonly ref T item = ref item;
        public uint Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (uint)self.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefBox<T> Add()
        {
            ref var r = ref self.m_items.UnsafeAdd();
            r = item;
            return new(ref r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefBox<T> At(uint index) => new(ref self.m_items.GetUnchecked(index));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Eq(RefBox<T> ctx) => EqualityComparer<T>.Default.Equals(item, ctx.Ref);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Hash(RefBox<T> ctx) => Hash(ctx.Ref);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Hash(T item) => HashWrapper.Hash(EqualityComparer<T>.Default.GetHashCode(item!));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Hash() => Hash(item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefBox<T> Get(RefBox<T> ctx) => ctx;
    }

    public bool TryInsert(T item)
    {
        var ctrl = new Ctrl(ref this, ref item);
        var hash = ctrl.Hash();
        m_hash_searcher.UnsafeTryEmplace<Ctrl, RefBox<T>, RefBox<T>>(ctrl, hash, out var is_new);
        return is_new;
    }

    public SVector<T>.Enumerator GetEnumerator() => m_items.GetEnumerator();
}

[StructLayout(LayoutKind.Auto)]
public struct SHashMap<TKey, TValue, HashWrapper>() where HashWrapper : IHashWrapper
{
    internal SVector<TKey> m_keys = new();
    internal SVector<TValue> m_values = new();
    internal SHashSearcher m_hash_searcher = new();

    public int Count => m_keys.Count;

    internal readonly ref struct Ctx(ref TKey key, ref TValue value)
    {
        public readonly ref TKey key = ref key;
        public readonly ref TValue value = ref value;
    }

    internal readonly ref struct Ctrl(ref SHashMap<TKey, TValue, HashWrapper> self, ref TKey key)
        : IHashSearchCtrl<RefBox<TValue>, Ctx>
    {
        private readonly ref SHashMap<TKey, TValue, HashWrapper> self = ref self;
        private readonly ref TKey key = ref key;
        public uint Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (uint)self.Count;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Ctx Add()
        {
            var ctx = new Ctx(ref self.m_keys.UnsafeAdd(), ref self.m_values.UnsafeAdd());
            ctx.key = key;
            return ctx;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Ctx At(uint index) => new(ref self.m_keys.GetUnchecked(index), ref self.m_values.GetUnchecked(index));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Eq(Ctx ctx) => EqualityComparer<TKey>.Default.Equals(key, ctx.key);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Hash(Ctx ctx) => Hash(ctx.key);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Hash(TKey item) => HashWrapper.Hash(EqualityComparer<TKey>.Default.GetHashCode(item!));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Hash() => Hash(key);
        public RefBox<TValue> Get(Ctx ctx) => new(ref ctx.value);
    }

    public bool TryInsert(TKey key, TValue value)
    {
        ref var slot = ref UnsafeTryEmplace(key, out var is_new);
        if (is_new) slot = value;
        return is_new;
    }

    public ref TValue UnsafeTryEmplace(TKey key, out bool is_new)
    {
        var ctrl = new Ctrl(ref this, ref key);
        var hash = ctrl.Hash();
        var r = m_hash_searcher.UnsafeTryEmplace<Ctrl, RefBox<TValue>, Ctx>(ctrl, hash, out is_new);
        return ref Unsafe.AsRef(in r.Ref);
    }
}
