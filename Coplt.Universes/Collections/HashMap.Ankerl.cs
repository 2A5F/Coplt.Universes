using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Coplt.Universes.Utilities;

namespace Coplt.Universes.Collections;

// https://github.com/martinus/unordered_dense
// https://github.com/martinus/unordered_dense/tree/f30ed41b58af8c79788e8581fe57a6faf856258e

/// <summary>
/// A hash searcher using Ankerl's algorithm, slow insertion and remove,
/// random query is comparable to dictionary,
/// and sequential traversal is extremely fast
/// </summary>
[StructLayout(LayoutKind.Auto)]
public struct AnkerlHashSearcher : IHashSearcher<AnkerlHashSearcher>
{
    #region Create

    public static AnkerlHashSearcher Create() => new();

    #endregion

    #region Fields

    /// <summary>
    /// 2^(64-m_shift) number of buckets
    /// </summary>
    private const byte initial_shifts = 64 - 2;
    private const float default_max_load_factor = 0.8F;

    private const uint max_buckets = 1u << (sizeof(uint) * 8 - 1);

    private struct Bucket(uint mDistAndFingerprint, uint mValueIdx)
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
    private readonly float m_max_load_factor = default_max_load_factor;
    private byte m_shifts = initial_shifts;

    #endregion

    #region Ctor

    public AnkerlHashSearcher()
    {
        CreateBucketFromShift();
    }

    #endregion

    #region Misc

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint DistAndFingerprintFromHash(ulong hash) =>
        (uint)(Bucket.dist_inc | (hash & Bucket.fingerprint_mask));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly uint GetBucketIndex(ulong hash) => (uint)(hash >> m_shifts);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly uint NextBucketIndex(uint index)
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
        where S : IDenseHashSearchCtrl<R, C>, allows ref struct
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
        where S : IDenseHashSearchCtrl<R, C>, allows ref struct
        where R : allows ref struct
        where C : allows ref struct
    {
        for (uint value_idx = 0, end_idx = search.Size; value_idx < end_idx; ++value_idx)
        {
            var ctx = search.At(value_idx);
            var hash = search.Hash(ctx);
            var (dist_and_fingerprint, bucket) = NextWhileLess(hash);

            // we know for certain that key has not yet been inserted, so no need to check it.
            PlaceAndShiftUp(new(dist_and_fingerprint, value_idx), bucket);
        }
    }

    private (uint dist_and_fingerprint, uint bucket) NextWhileLess(ulong hash)
    {
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
        where S : IDenseHashSearchCtrl<R, C>, allows ref struct
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
        where S : IDenseHashSearchCtrl<R, C>, allows ref struct
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

    #region TryFind

    public readonly R UnsafeTryFind<S, R, C>(S search, ulong hash)
        where S : IDenseHashSearchCtrl<R, C>, allows ref struct
        where R : allows ref struct
        where C : allows ref struct
    {
        if (search.Size == 0) return search.None();
        var dist_and_fingerprint = DistAndFingerprintFromHash(hash);
        var bucket_index = GetBucketIndex(hash);

        ref var bucket = ref m_buckets.GetUnchecked(bucket_index);

        // unrolled loop. *Always* check a few directly, then enter the loop. This is faster.

        if (dist_and_fingerprint == bucket.m_dist_and_fingerprint)
        {
            var ctx = search.At(bucket.m_value_idx);
            if (search.Eq(ctx))
            {
                return search.Get(ctx);
            }
        }
        dist_and_fingerprint += Bucket.dist_inc;
        bucket_index = NextBucketIndex(bucket_index);
        bucket = ref m_buckets.GetUnchecked(bucket_index);

        if (dist_and_fingerprint == bucket.m_dist_and_fingerprint)
        {
            var ctx = search.At(bucket.m_value_idx);
            if (search.Eq(ctx))
            {
                return search.Get(ctx);
            }
        }
        dist_and_fingerprint += Bucket.dist_inc;
        bucket_index = NextBucketIndex(bucket_index);
        bucket = ref m_buckets.GetUnchecked(bucket_index);

        for (;;)
        {
            if (dist_and_fingerprint == bucket.m_dist_and_fingerprint)
            {
                var ctx = search.At(bucket.m_value_idx);
                if (search.Eq(ctx))
                {
                    return search.Get(ctx);
                }
            }
            else if (dist_and_fingerprint > bucket.m_dist_and_fingerprint)
            {
                return search.None();
            }
            dist_and_fingerprint += Bucket.dist_inc;
            bucket_index = NextBucketIndex(bucket_index);
            bucket = ref m_buckets.GetUnchecked(bucket_index);
        }
    }

    #endregion

    #region Remove

    public R UnsafeRemove<S, R, C>(S search, ulong hash)
        where S : IDenseHashSearchCtrl<R, C>, allows ref struct
        where R : allows ref struct
        where C : allows ref struct
    {
        if (search.Size == 0) return search.None();

        var (dist_and_fingerprint, bucket_idx) = NextWhileLess(hash);

        ref var bucket = ref m_buckets.GetUnchecked(bucket_idx);
        re:
        if (dist_and_fingerprint == bucket.m_dist_and_fingerprint)
        {
            var ctx = search.At(bucket.m_value_idx);
            if (!search.Eq(ctx))
            {
                dist_and_fingerprint += Bucket.dist_inc;
                bucket_idx = NextBucketIndex(bucket_idx);
                goto re;
            }
        }

        if (dist_and_fingerprint != bucket.m_dist_and_fingerprint) return search.None();

        return DoRemove<S, R, C>(search, ref bucket, bucket_idx);
    }

    private R DoRemove<S, R, C>(S search, ref Bucket bucket, uint bucket_index)
        where S : IDenseHashSearchCtrl<R, C>, allows ref struct
        where R : allows ref struct
        where C : allows ref struct
    {
        var value_idx_to_remove = bucket.m_value_idx;

        // shift down until either empty or an element with correct spot is found
        var next_bucket_index = NextBucketIndex(bucket_index);
        re:
        ref var next_bucket = ref m_buckets.GetUnchecked(next_bucket_index);
        if (next_bucket.m_dist_and_fingerprint >= Bucket.dist_inc * 2)
        {
            bucket = new(next_bucket.m_dist_and_fingerprint - Bucket.dist_inc, next_bucket.m_value_idx);
            bucket_index = next_bucket_index;
            next_bucket_index = NextBucketIndex(next_bucket_index);
            bucket = ref m_buckets.GetUnchecked(bucket_index);
            goto re;
        }
        bucket = default;

        // swap value to end
        var last_idx = search.Size - 1;
        var last = search.At(last_idx);
        if (value_idx_to_remove != last_idx)
        {
            var hash = search.Hash(last);
            bucket_index = GetBucketIndex(hash);

            ref var target = ref m_buckets.GetUnchecked(bucket_index);
            while (target.m_value_idx != last_idx)
            {
                bucket_index = NextBucketIndex(bucket_index);
                target = ref m_buckets.GetUnchecked(bucket_index);
            }
            target.m_value_idx = value_idx_to_remove;
        }

        return search.RemoveSwapLast(last, value_idx_to_remove);
    }

    #endregion

    #region Clear

    public void Clear()
    {
        Array.Clear(m_buckets);
    }

    #endregion
}