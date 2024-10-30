using System.Collections;
using System.Runtime.CompilerServices;

namespace Coplt.Universes.Core;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly unsafe struct ChunkView<T>(T* ptr, int length) : IReadOnlyList<T>
{
    #region Getter

    public T* Ptr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ptr;
    }
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => length;
    }
    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
            return ref *UncheckedGet(index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T* UncheckedGet(int index) => (T*)((byte*)Ptr + TypeMeta.Of<T>().AlignedSize * index);

    #endregion

    #region Enumerator

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new EnumeratorClass(this);
    IEnumerator IEnumerable.GetEnumerator() => new EnumeratorClass(this);

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public struct Enumerator(ChunkView<T> self)
    {
        private int m_index = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => m_index = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var index = m_index + 1;
            if (index >= self.Length) return false;
            m_index = index;
            return true;
        }

        public ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref *self.UncheckedGet(m_index);
        }
    }

    private sealed class EnumeratorClass(ChunkView<T> self) : IEnumerator<T>
    {
        private int m_index = -1;
        public void Reset() => m_index = -1;
        public bool MoveNext()
        {
            var index = m_index + 1;
            if (index >= self.Length) return false;
            m_index = index;
            return true;
        }
        public T Current => *self.UncheckedGet(m_index);
        object? IEnumerator.Current => Current;
        public void Dispose() { }
    }

    #endregion

    #region Interface

    int IReadOnlyCollection<T>.Count => Length;

    T IReadOnlyList<T>.this[int index] => this[index];

    #endregion
}
