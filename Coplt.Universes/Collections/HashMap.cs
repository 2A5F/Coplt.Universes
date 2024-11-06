using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Coplt.Universes.Utilities;

namespace Coplt.Universes.Collections;

public interface IHashSearchCtrl<out R, C>
    where R : allows ref struct
    where C : allows ref struct
{
    /// <summary>
    /// must be O(1)
    /// </summary>
    uint Size { get; }
    /// <summary>
    /// must size += 1
    /// </summary>
    C Add();
    C At(uint index);
    bool Eq(C ctx);
    ulong Hash(C ctx);
    R Get(C ctx);
    R None();
    /// <summary>
    ///  Must swap to end
    /// </summary>
    R Remove(C last, uint index);
}

/// <summary>
/// https://github.com/martinus/unordered_dense
/// </summary>
[StructLayout(LayoutKind.Auto)]
public struct SHashSearcher
{
    #region Fields

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

    #endregion

    #region Ctor

    public SHashSearcher()
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

    #region TryFind

    public readonly R UnsafeTryFind<S, R, C>(S search, ulong hash)
        where S : IHashSearchCtrl<R, C>, allows ref struct
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
        where S : IHashSearchCtrl<R, C>, allows ref struct
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
        where S : IHashSearchCtrl<R, C>, allows ref struct
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

        return search.Remove(last, value_idx_to_remove);
    }

    #endregion

    #region Clear

    public void Clear()
    {
        Array.Clear(m_buckets);
    }

    #endregion
}

[StructLayout(LayoutKind.Auto)]
public struct SHashSet<T, HashWrapper>() : ISet<T>
    where HashWrapper : IHashWrapper
{
    #region Fields

    internal SVector<T> m_items = new();
    internal SHashSearcher m_hash_searcher = new();

    #endregion

    #region Ctrl

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefBox<T> None() => default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe RefBox<T> Remove(RefBox<T> last, uint index)
        {
            ref var slot = ref self.m_items.GetUnchecked(index);
            slot = last.Ref;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) last.Ref = default!;
            self.m_items.RemoveLastUncheckedSkipReset();
            return new(ref Unsafe.AsRef<T>((void*)nuint.MaxValue));
        }
    }

    #endregion

    #region Count

    public readonly int Count => m_items.Count;

    #endregion

    #region IsReadOnly

    public readonly bool IsReadOnly => false;

    #endregion

    #region Contains

    public readonly bool Contains(T item)
    {
        var ctrl = new Ctrl(ref Unsafe.AsRef(in this), ref item);
        var hash = ctrl.Hash();
        var r = m_hash_searcher.UnsafeTryFind<Ctrl, RefBox<T>, RefBox<T>>(ctrl, hash);
        return !r.IsNull;
    }

    #endregion

    #region Add

    void ICollection<T>.Add(T item) => TryAdd(item);
    bool ISet<T>.Add(T item) => TryAdd(item);

    public bool TryAdd(T item)
    {
        var ctrl = new Ctrl(ref this, ref item);
        var hash = ctrl.Hash();
        m_hash_searcher.UnsafeTryEmplace<Ctrl, RefBox<T>, RefBox<T>>(ctrl, hash, out var is_new);
        return is_new;
    }

    public ref T UnsafeTryAdd(T item, out bool is_new)
    {
        var ctrl = new Ctrl(ref this, ref item);
        var hash = ctrl.Hash();
        var r = m_hash_searcher.UnsafeTryEmplace<Ctrl, RefBox<T>, RefBox<T>>(ctrl, hash, out is_new);
        return ref Unsafe.AsRef(in r.Ref); // https://github.com/dotnet/csharplang/discussions/8556
    }

    #endregion

    #region Remove

    public bool Remove(T item)
    {
        var ctrl = new Ctrl(ref this, ref item);
        var hash = ctrl.Hash();
        var r = m_hash_searcher.UnsafeRemove<Ctrl, RefBox<T>, RefBox<T>>(ctrl, hash);
        return !r.IsNull;
    }

    #endregion

    #region Clear

    public void Clear()
    {
        m_hash_searcher.Clear();
        m_items.Clear();
    }

    #endregion

    #region CopyTo

    public readonly void CopyTo(T[] array, int arrayIndex) => m_items.CopyTo(array, arrayIndex);
    public readonly void CopyTo(Span<T> span) => m_items.CopyTo(span);

    #endregion

    #region Set Query

    void ISet<T>.ExceptWith(IEnumerable<T> other) => throw new NotSupportedException();
    void ISet<T>.SymmetricExceptWith(IEnumerable<T> other) => throw new NotSupportedException();
    void ISet<T>.IntersectWith(IEnumerable<T> other) => throw new NotSupportedException();
    bool ISet<T>.IsProperSubsetOf(IEnumerable<T> other) => throw new NotSupportedException();
    bool ISet<T>.IsProperSupersetOf(IEnumerable<T> other) => throw new NotSupportedException();
    bool ISet<T>.IsSubsetOf(IEnumerable<T> other) => throw new NotSupportedException();
    bool ISet<T>.IsSupersetOf(IEnumerable<T> other) => throw new NotSupportedException();
    bool ISet<T>.Overlaps(IEnumerable<T> other) => throw new NotSupportedException();
    bool ISet<T>.SetEquals(IEnumerable<T> other) => throw new NotSupportedException();
    void ISet<T>.UnionWith(IEnumerable<T> other) => throw new NotSupportedException();

    #endregion

    #region GetEnumerator

    public readonly SVector<T>.Enumerator GetEnumerator() => m_items.GetEnumerator();
    readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => new SVector<T>.EnumeratorClass(in m_items);
    readonly IEnumerator IEnumerable.GetEnumerator() => new SVector<T>.EnumeratorClass(in m_items);

    #endregion
}

[StructLayout(LayoutKind.Auto)]
public struct SHashMap<TKey, TValue, HashWrapper>() : IDictionary<TKey, TValue>
    where HashWrapper : IHashWrapper
{
    #region Fields

    internal SVector<TKey> m_keys = new();
    internal SVector<TValue> m_values = new();
    internal SHashSearcher m_hash_searcher = new();

    #endregion

    #region Ctrl

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefBox<TValue> None() => default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe RefBox<TValue> Remove(Ctx last, uint index)
        {
            var ctx = At(index);
            ctx.key = last.key;
            ctx.value = last.value;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>()) last.key = default!;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>()) last.value = default!;
            self.m_keys.RemoveLastUncheckedSkipReset();
            self.m_values.RemoveLastUncheckedSkipReset();
            return new(ref Unsafe.AsRef<TValue>((void*)nuint.MaxValue));
        }
    }

    internal readonly ref struct CtrlRemoveGetValue(
        ref SHashMap<TKey, TValue, HashWrapper> self,
        ref TKey key,
        ref TValue value
    )
        : IHashSearchCtrl<RefBox<TValue>, Ctx>
    {
        private readonly ref SHashMap<TKey, TValue, HashWrapper> self = ref self;
        private readonly ref TKey key = ref key;
        private readonly ref TValue value = ref value;
        public uint Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (uint)self.Count;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Ctx Add() => throw new NotSupportedException();
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefBox<TValue> None() => default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefBox<TValue> Remove(Ctx last, uint index)
        {
            var ctx = At(index);
            value = ctx.value;
            ctx.key = last.key;
            ctx.value = last.value;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>()) last.key = default!;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>()) last.value = default!;
            self.m_keys.RemoveLastUncheckedSkipReset();
            self.m_values.RemoveLastUncheckedSkipReset();
            return new(ref value);
        }
    }

    #endregion

    #region Count

    public readonly int Count => m_keys.Count;

    #endregion

    #region IsReadOnly

    public readonly bool IsReadOnly => false;

    #endregion

    #region Contains

    public readonly bool ContainsKey(TKey key)
    {
        var ctrl = new Ctrl(ref Unsafe.AsRef(in this), ref key);
        var hash = ctrl.Hash();
        var r = m_hash_searcher.UnsafeTryFind<Ctrl, RefBox<TValue>, Ctx>(ctrl, hash);
        return !r.IsNull;
    }

    readonly bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) =>
        TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);

    #endregion

    #region Index

    public TValue this[TKey key]
    {
        readonly get => TryGetValue(key, out var v) ? v : throw new KeyNotFoundException();
        set => AddOrReplace(key, value);
    }

    #endregion

    #region Get

    public readonly bool TryGetValueRef(TKey key, out RefBox<TValue> value)
    {
        var ctrl = new Ctrl(ref Unsafe.AsRef(in this), ref key);
        var hash = ctrl.Hash();
        var r = m_hash_searcher.UnsafeTryFind<Ctrl, RefBox<TValue>, Ctx>(ctrl, hash);
        value = new(ref Unsafe.AsRef(in r.Ref)); // https://github.com/dotnet/csharplang/discussions/8556
        return !r.IsNull;
    }

    public readonly bool TryGetValue(TKey key, out TValue value)
    {
        if (TryGetValueRef(key, out RefBox<TValue> r))
        {
            value = r.Ref;
            return true;
        }
        else
        {
            value = default!;
            return false;
        }
    }

    public readonly ref TValue UnsafeTryGetValue(TKey key)
    {
        var ctrl = new Ctrl(ref Unsafe.AsRef(in this), ref key);
        var hash = ctrl.Hash();
        var r = m_hash_searcher.UnsafeTryFind<Ctrl, RefBox<TValue>, Ctx>(ctrl, hash);
        return ref Unsafe.AsRef(in r.Ref); // https://github.com/dotnet/csharplang/discussions/8556
    }

    #endregion

    #region Add

    public bool TryAdd(TKey key, TValue value)
    {
        ref var slot = ref UnsafeTryEmplace(key, out var is_new);
        if (is_new) slot = value;
        return is_new;
    }

    /// <returns>ture if add, false if replace</returns>
    public bool AddOrReplace(TKey key, TValue value)
    {
        ref var slot = ref UnsafeTryEmplace(key, out var is_new);
        slot = value;
        return is_new;
    }

    void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => TryAdd(key, value);
    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => TryAdd(item.Key, item.Value);

    public ref TValue UnsafeTryEmplace(TKey key, out bool is_new)
    {
        var ctrl = new Ctrl(ref this, ref key);
        var hash = ctrl.Hash();
        var r = m_hash_searcher.UnsafeTryEmplace<Ctrl, RefBox<TValue>, Ctx>(ctrl, hash, out is_new);
        return ref Unsafe.AsRef(in r.Ref); // https://github.com/dotnet/csharplang/discussions/8556
    }

    #endregion

    #region Remove

    public bool Remove(TKey key)
    {
        var ctrl = new Ctrl(ref this, ref key);
        var hash = ctrl.Hash();
        var r = m_hash_searcher.UnsafeRemove<Ctrl, RefBox<TValue>, Ctx>(ctrl, hash);
        return !r.IsNull;
    }

    public bool TryRemove(TKey key, out TValue value)
    {
        Unsafe.SkipInit(out value);
        var ctrl = new CtrlRemoveGetValue(ref this, ref key, ref value);
        var hash = ctrl.Hash();
        var r = m_hash_searcher.UnsafeRemove<CtrlRemoveGetValue, RefBox<TValue>, Ctx>(ctrl, hash);
        return !r.IsNull;
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

    #endregion

    #region Clear

    public void Clear()
    {
        m_hash_searcher.Clear();
        m_keys.Clear();
        m_values.Clear();
    }

    #endregion

    #region CopyTo

    readonly void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (arrayIndex >= array.Length) return;
        foreach (var kv in this)
        {
            array[arrayIndex] = kv;
            arrayIndex++;
            if (arrayIndex >= array.Length) return;
        }
    }

    #endregion

    #region GetEnumerator

    public readonly Enumerator GetEnumerator() => new(this);
    readonly IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() =>
        GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator(scoped in SHashMap<TKey, TValue, HashWrapper> self)
        : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private int m_index = -1;
        private readonly int m_count = self.Count;
        private readonly TKey[] m_keys = self.m_keys.UnsafeInternalArray;
        private readonly TValue[] m_values = self.m_values.UnsafeInternalArray;

        public void Reset() => m_index = -1;
        public bool MoveNext()
        {
            var index = m_index + 1;
            if (index >= m_count) return false;
            m_index = index;
            return true;
        }
        public KeyValuePair<TKey, TValue> Current => new(m_keys.GetUnchecked(m_index), m_values.GetUnchecked(m_index));
        object IEnumerator.Current => Current;
        public void Dispose() { }
    }

    #endregion

    #region Views

    [UnscopedRef]
    public readonly KeyView Keys => new(this);
    [UnscopedRef]
    public readonly ValueView Values => new(this);

    public readonly struct KeyView(SHashMap<TKey, TValue, HashWrapper> self) : ICollection<TKey>
    {
        public int Count => self.Count;

        public bool IsReadOnly => true;
        public Span<TKey> Span => self.m_keys.Span;
        public bool Contains(TKey item) => self.ContainsKey(item);

        public SVector<TKey>.Enumerator GetEnumerator() => new(in self.m_keys);

        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() =>
            new SVector<TKey>.EnumeratorClass(in self.m_keys);
        IEnumerator IEnumerable.GetEnumerator() => new SVector<TKey>.EnumeratorClass(in self.m_keys);

        void ICollection<TKey>.Add(TKey item) => throw new NotSupportedException();
        void ICollection<TKey>.Clear() => throw new NotSupportedException();
        bool ICollection<TKey>.Remove(TKey item) => throw new NotSupportedException();

        public void CopyTo(TKey[] array, int arrayIndex) => self.m_keys.CopyTo(array, arrayIndex);
        public void CopyTo(Span<TKey> span) => self.m_keys.CopyTo(span);
    }

    public readonly struct ValueView(SHashMap<TKey, TValue, HashWrapper> self) : ICollection<TValue>
    {
        public int Count => self.Count;

        public bool IsReadOnly => true;

        public Span<TValue> Span => self.m_values.Span;
        bool ICollection<TValue>.Contains(TValue item) => self.m_values.Contains(item);

        public SVector<TValue>.Enumerator GetEnumerator() => new(in self.m_values);

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() =>
            new SVector<TValue>.EnumeratorClass(in self.m_values);
        IEnumerator IEnumerable.GetEnumerator() => new SVector<TValue>.EnumeratorClass(in self.m_values);

        void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();
        void ICollection<TValue>.Clear() => throw new NotSupportedException();
        bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException();

        public void CopyTo(TValue[] array, int arrayIndex) => self.m_values.CopyTo(array, arrayIndex);
        public void CopyTo(Span<TValue> span) => self.m_values.CopyTo(span);
    }

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => throw new NotSupportedException();
    ICollection<TValue> IDictionary<TKey, TValue>.Values => throw new NotSupportedException();

    #endregion
}
