using System.Collections.Concurrent;

namespace Coplt.Universes.Core;

public readonly record struct TypeId(ulong Id)
{
    #region Static

    private static ulong s_inc;

    private static readonly ConcurrentDictionary<ulong, Type> s_id_to_type = new();

    private static class Static<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly ulong s_id;

        static Static()
        {
            s_id = Interlocked.Increment(ref s_inc);
            s_id_to_type[s_id] = typeof(T);
        }
    }

    #endregion

    #region Of

    public static TypeId Of<T>() => new(Static<T>.s_id);

    #endregion

    #region Type

    public Type Type => s_id_to_type[Id];

    #endregion

    #region ToString

    public override string ToString() => $"TypeId({Id}) -> {Type}";

    #endregion
}
