namespace Coplt.Universes;

public readonly unsafe struct Ref<T>
{
    private readonly T[]? m_array;
    private readonly T* m_ptr_or_index;

    public Ref(T* ptr)
    {
        m_ptr_or_index = ptr;
    }

    public Ref(T[] array, nuint index)
    {
        m_array = array;
        m_ptr_or_index = (T*)index;
    }

    public ref readonly T V
    {
        get
        {
            if (m_array == null) return ref *m_ptr_or_index;
            return ref m_array![(nuint)m_ptr_or_index];
        }
    }

    public ref readonly T GetPinnableReference() => ref V;

    public override string ToString() => $"{V}";
}

public readonly unsafe struct Mut<T>
{
    private readonly T[]? m_array;
    private readonly T* m_ptr_or_index;

    public Mut(T* ptr)
    {
        m_ptr_or_index = ptr;
    }

    public Mut(T[] array, nuint index)
    {
        m_array = array;
        m_ptr_or_index = (T*)index;
    }

    public ref T V
    {
        get
        {
            if (m_array == null) return ref *m_ptr_or_index;
            return ref m_array![(nuint)m_ptr_or_index];
        }
    }

    public ref T GetPinnableReference() => ref V;

    public override string ToString() => $"{V}";
}
