using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Coplt.Universes.Core;

public interface ITypeSet
{
    public ImmutableHashSet<TypeMeta> GetTypes();
}

public readonly record struct TypeSetId(ulong Id)
{
    public TypeSet Set => TypeSet.TryFromId(Id)!;

    #region ToString

    public override string ToString() => Set == null! ? $"TypeSet {{ Id = {Id} }}" : $"{Set}";

    #endregion
}

public abstract partial class TypeSet : IEnumerable<TypeMeta>, IEquatable<TypeSet>
{
    #region Fields

    private readonly ulong m_id;
    private readonly List<TypeMeta> m_types;
    private readonly ImmutableHashSet<TypeMeta> m_type_set;

    private protected TypeSet(ulong id, List<TypeMeta> types, ImmutableHashSet<TypeMeta> type_set)
    {
        m_id = id;
        m_types = types;
        m_type_set = type_set;
    }

    public int Count => m_types.Count;

    public TypeMeta this[int index] => m_types[index];

    public ReadOnlySpan<TypeMeta> Span => CollectionsMarshal.AsSpan(m_types);

    public ImmutableHashSet<TypeMeta> Set => m_type_set;

    public TypeSetId Id => new(m_id);

    #endregion

    #region Inst

    private sealed class Inst<T>(ulong id, List<TypeMeta> types, ImmutableHashSet<TypeMeta> type_set)
        : TypeSet(id, types, type_set)
    {
        #region Contains

        public override bool Contains<TV>() => ContainsValue<T, TV>.Value;

        #endregion

        #region IndexOf

        public override int IndexOf<TV>() => IndexOfValue<T, TV>.Value;

        #endregion

        #region IsOverlap

        public override bool IsOverlap(TypeSet other) => other.IsOverlap<T>();

        private protected override bool IsOverlap<TO>() => IsOverlapValue<TO, T>.Value;

        #endregion

        #region IsSubsetOf

        public override bool IsSubsetOf(TypeSet other) => other.IsSubsetOf<T>();

        private protected override bool IsSubsetOf<TO>() => IsSubsetOfValue<TO, T>.Value;

        #endregion

        #region IsSupersetOf

        public override bool IsSupersetOf(TypeSet other) => other.IsSupersetOf<T>();
        private protected override bool IsSupersetOf<TO>() => IsSupersetOfValue<TO, T>.Value;

        #endregion
    }

    #endregion

    #region Query

    #region Contains

    public abstract bool Contains<T>();

    private static class ContainsValue<TS, TV>
    {
        public static readonly bool Value = IndexOfValue<TS, TV>.Value >= 0;
    }

    #endregion

    #region IndexOf

    public abstract int IndexOf<T>();

    private static class IndexOfValue<TS, TV>
    {
        public static readonly int Value = ListOf<TS>().FindIndex(static t => t.Id == TypeId.Of<TV>());
    }

    #endregion

    #region IsOverlap

    public abstract bool IsOverlap(TypeSet other);

    private protected abstract bool IsOverlap<TO>();

    private static class IsOverlapValue<TA, TB>
    {
        public static readonly bool Value = SetOf<TA>().Overlaps(SetOf<TB>());
    }

    #endregion

    #region IsSubsetOf

    public abstract bool IsSubsetOf(TypeSet other);

    private protected abstract bool IsSubsetOf<TO>();

    private static class IsSubsetOfValue<TA, TB>
    {
        public static readonly bool Value = SetOf<TA>().IsSubsetOf(SetOf<TB>());
    }

    #endregion

    #region IsSupersetOf

    public abstract bool IsSupersetOf(TypeSet other);

    private protected abstract bool IsSupersetOf<TO>();

    private static class IsSupersetOfValue<TA, TB>
    {
        public static readonly bool Value = SetOf<TA>().IsSupersetOf(SetOf<TB>());
    }

    #endregion

    #endregion

    #region Static

    private static ulong s_set_id_inc;

    private static readonly ConcurrentDictionary<SortedSet, TypeSet> s_set_cache = new();
    private static readonly ConcurrentDictionary<ulong, TypeSet> s_id_to_set = new();
    private static readonly ConcurrentDictionary<Type, TypeSet> s_unique_to_set = new();

    // ReSharper disable once InconsistentNaming
    private readonly struct SortedSet(List<TypeMeta> Types) : IEquatable<SortedSet>
    {
        public readonly List<TypeMeta> Types = Types;
        public bool Equals(SortedSet other) => Types.SequenceEqual(other.Types);
        public override bool Equals(object? obj) => obj is SortedSet other && Equals(other);
        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var type in Types)
            {
                hash.Add(type);
            }
            return hash.ToHashCode();
        }
        public static bool operator ==(SortedSet left, SortedSet right) => left.Equals(right);
        public static bool operator !=(SortedSet left, SortedSet right) => !left.Equals(right);
    }

    private static class Static<TSet> where TSet : struct, ITypeSet
    {
        public static readonly TypeSet s_set = s_set_cache.GetOrAdd(new(SortType(default(TSet).GetTypes())),
            static set =>
            {
                var unique = UniqueTypeEmitter.Emit();
                var id = Interlocked.Increment(ref s_set_id_inc);
                var type = typeof(Inst<>).MakeGenericType(unique);
                var inst = (TypeSet)Activator.CreateInstance(type, id, set.Types, default(TSet).GetTypes())!;
                s_id_to_set[id] = inst;
                s_unique_to_set[unique] = inst;
                return inst;
            });
    }

    public static TypeSet Get<TSet>() where TSet : struct, ITypeSet => Static<TSet>.s_set;

    internal static TypeSet? TryFromId(ulong id) => s_id_to_set.GetValueOrDefault(id);

    internal static ImmutableHashSet<TypeMeta> SetOf<T>() => s_unique_to_set[typeof(T)].Set;

    internal static List<TypeMeta> ListOf<T>() => s_unique_to_set[typeof(T)].m_types;

    #endregion

    #region Equals

    public bool Equals(TypeSet? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return m_id == other.m_id;
    }

    public override bool Equals(object? obj) => obj is TypeSet other && Equals(other);

    public override int GetHashCode() => m_id.GetHashCode();

    #endregion

    #region GetEnumerator

    public List<TypeMeta>.Enumerator GetEnumerator() => m_types.GetEnumerator();
    IEnumerator<TypeMeta> IEnumerable<TypeMeta>.GetEnumerator() => m_types.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => m_types.GetEnumerator();

    #endregion

    #region ToString

    public override string ToString() =>
        $"TypeSet {{ Id = {m_id}, Types = {string.Join(", ", m_types.Select(static t => t.Type))} }}";

    #endregion

    #region SortType

    public static List<TypeMeta> SortType(IEnumerable<TypeMeta> types) => types
        .Where(static a => !a.IsTag)
        .OrderBy(static a => a.IsManaged)
        .ThenByDescending(static a => a.Size)
        .ThenByDescending(static a => a.Align)
        .ThenBy(static a => a.Type.Name)
        .ThenBy(static a => a.Type.GetHashCode())
        .ToList();

    #endregion

    #region S

    // internal readonly struct DynS<T> : ITypeSet
    // {
    //     // ReSharper disable once StaticMemberInGenericType
    //     public static ImmutableHashSet<TypeMeta> Types
    //     {
    //         [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //         get;
    //         set;
    //     } = [];
    //     public ImmutableHashSet<TypeMeta> GetTypes() => Types;
    // }

    public readonly struct S<T> : ITypeSet
    {
        public ImmutableHashSet<TypeMeta> GetTypes() => Types;
        public S<S<T>, TA> A<TA>() => default;

        private static ImmutableHashSet<TypeMeta> Types
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        } = ImmutableHashSet.Create(TypeMeta.Of<Entity>(), TypeMeta.Of<T>());

        public TypeSet Build() => Get<S<T>>();
    }

    public readonly struct S<TB, T> : ITypeSet
        where TB : struct, ITypeSet
    {
        public ImmutableHashSet<TypeMeta> GetTypes() => Types;
        public S<S<TB, T>, TA> A<TA>() => default;

        private static ImmutableHashSet<TypeMeta> Types
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        } = default(TB).GetTypes().Add(TypeMeta.Of<T>());

        public TypeSet Build() => Get<S<TB, T>>();
    }

    #endregion
}
