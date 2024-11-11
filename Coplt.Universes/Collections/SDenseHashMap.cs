using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Coplt.Universes.Utilities;

namespace Coplt.Universes.Collections;

[StructLayout(LayoutKind.Auto)]
public struct SDenseHashMap<TKey, TValue, HashSearcher, HashWrapper>() : IDictionary<TKey, TValue>
    where HashSearcher : struct, IDenseHashSearcher<HashSearcher>
    where HashWrapper : IHashWrapper
{
    #region Fields

    internal SVector<TKey> m_keys = new();
    internal SVector<TValue> m_values = new();
    internal HashSearcher m_hash_searcher = HashSearcher.Create();

    #endregion

    #region Ctrl

    internal readonly ref struct Ctx(ref TKey key, ref TValue value)
    {
        public readonly ref TKey key = ref key;
        public readonly ref TValue value = ref value;
    }

    internal readonly ref struct Ctrl(ref SDenseHashMap<TKey, TValue, HashSearcher, HashWrapper> self, ref TKey key)
        : IDenseHashSearchCtrl<RefBox<TValue>, Ctx>
    {
        private readonly ref SDenseHashMap<TKey, TValue, HashSearcher, HashWrapper> self = ref self;
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
        public unsafe RefBox<TValue> RemoveSwapLast(Ctx last, uint index)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe RefBox<TValue> RemoveLast()
        {
            self.m_keys.RemoveLastUnchecked();
            self.m_values.RemoveLastUnchecked();
            return new(ref Unsafe.AsRef<TValue>((void*)nuint.MaxValue));
        }
    }

    internal readonly ref struct CtrlRemoveGetValue(
        ref SDenseHashMap<TKey, TValue, HashSearcher, HashWrapper> self,
        ref TKey key,
        ref TValue value
    )
        : IDenseHashSearchCtrl<RefBox<TValue>, Ctx>
    {
        private readonly ref SDenseHashMap<TKey, TValue, HashSearcher, HashWrapper> self = ref self;
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
        public RefBox<TValue> RemoveSwapLast(Ctx last, uint index)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefBox<TValue> RemoveLast()
        {
            value = self.m_values.GetUnchecked(self.m_values.Count - 1);
            self.m_keys.RemoveLastUnchecked();
            self.m_values.RemoveLastUnchecked();
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
        var r = Unsafe.AsRef(in m_hash_searcher).UnsafeTryFind<Ctrl, RefBox<TValue>, Ctx>(ctrl, hash);
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
        var r = Unsafe.AsRef(in m_hash_searcher).UnsafeTryFind<Ctrl, RefBox<TValue>, Ctx>(ctrl, hash);
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
        var r = Unsafe.AsRef(in m_hash_searcher).UnsafeTryFind<Ctrl, RefBox<TValue>, Ctx>(ctrl, hash);
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

    public bool TryAdd(TKey key, Func<TValue> f)
    {
        ref var slot = ref UnsafeTryEmplace(key, out var is_new);
        if (is_new) slot = f();
        return is_new;
    }

    public bool TryAdd<A>(TKey key, A arg, Func<A, TValue> f)
    {
        ref var slot = ref UnsafeTryEmplace(key, out var is_new);
        if (is_new) slot = f(arg);
        return is_new;
    }

    /// <returns>ture if add, false if replace</returns>
    public bool AddOrReplace(TKey key, TValue value)
    {
        ref var slot = ref UnsafeTryEmplace(key, out var is_new);
        slot = value;
        return is_new;
    }

    public TValue GetOrAdd(TKey key, Func<TValue> add) => UnsafeGetOrAdd(key, add);

    public ref TValue UnsafeGetOrAdd(TKey key, Func<TValue> add)
    {
        ref var slot = ref UnsafeTryEmplace(key, out var is_new);
        if (is_new) slot = add();
#pragma warning disable CS8619
        return ref slot;
#pragma warning restore CS8619
    }

    public TValue GetOrAdd<A>(TKey key, A arg, Func<A, TValue> add) => UnsafeGetOrAdd(key, arg, add);

    public ref TValue UnsafeGetOrAdd<A>(TKey key, A arg, Func<A, TValue> add)
    {
        ref var slot = ref UnsafeTryEmplace(key, out var is_new);
        if (is_new) slot = add(arg);
#pragma warning disable CS8619
        return ref slot;
#pragma warning restore CS8619
    }

    /// <returns>ture if add, false if update</returns>
    public bool AddOrUpdate(TKey key, Func<TValue, TValue> update, Func<TValue> add)
    {
        ref var slot = ref UnsafeTryEmplace(key, out var is_new);
        slot = is_new ? add() : update(slot);
        return is_new;
    }

    /// <returns>ture if add, false if update</returns>
    public bool AddOrUpdate<A>(TKey key, A arg, Func<A, TValue, TValue> update, Func<A, TValue> add)
    {
        ref var slot = ref UnsafeTryEmplace(key, out var is_new);
        slot = is_new ? add(arg) : update(arg, slot);
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

    public struct Enumerator(scoped in SDenseHashMap<TKey, TValue, HashSearcher, HashWrapper> self)
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

    public readonly struct KeyView(SDenseHashMap<TKey, TValue, HashSearcher, HashWrapper> self) : ICollection<TKey>
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

    public readonly struct ValueView(SDenseHashMap<TKey, TValue, HashSearcher, HashWrapper> self) : ICollection<TValue>
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
