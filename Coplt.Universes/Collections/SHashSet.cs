using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Coplt.Universes.Collections;

[StructLayout(LayoutKind.Auto)]
public struct SHashSet<T, HashSearcher, HashWrapper>() : ISet<T>
    where HashSearcher : struct, IHashSearcher<HashSearcher>
    where HashWrapper : struct, IHashWrapper
{
    #region Fields

    internal SVector<T> m_items = new();
    internal HashSearcher m_hash_searcher = HashSearcher.Create();

    #endregion

    #region Ctrl

    internal readonly ref struct Ctrl(ref SHashSet<T, HashSearcher, HashWrapper> self, ref T item)
        : IDenseHashSearchCtrl<RefBox<T>, RefBox<T>>
    {
        private readonly ref SHashSet<T, HashSearcher, HashWrapper> self = ref self;
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
        public unsafe RefBox<T> RemoveSwapLast(RefBox<T> last, uint index)
        {
            ref var slot = ref self.m_items.GetUnchecked(index);
            slot = last.Ref;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) last.Ref = default!;
            self.m_items.RemoveLastUncheckedSkipReset();
            return new(ref Unsafe.AsRef<T>((void*)nuint.MaxValue));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe RefBox<T> RemoveLast()
        {
            self.m_items.RemoveAndGetLastUnchecked();
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
        var r = Unsafe.AsRef(in m_hash_searcher).UnsafeTryFind<Ctrl, RefBox<T>, RefBox<T>>(ctrl, hash);
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
