using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Coplt.Universes.Core;

#region ITypeSet

public interface ITypeSet
{
    public static abstract ImmutableHashSet<TypeMeta> Types { get; }
}

#endregion

#region TypeSetId

public readonly record struct TypeSetId(ulong Id)
{
    public TypeSet Set => TypeSet.TryFromId(Id)!;

    #region ToString

    public override string ToString() => Set == null! ? $"TypeSet {{ Id = {Id} }}" : $"{Set}";

    #endregion
}

#endregion

#region TypeSet

public abstract partial class TypeSet : IEnumerable<TypeMeta>, IEquatable<TypeSet>
{
    #region Fields

    private readonly ulong m_id;
    private readonly SortedSet m_sorted_set;

    private protected TypeSet(ulong id, SortedSet set)
    {
        m_id = id;
        m_sorted_set = set;
    }

    public int Count => m_sorted_set.Types.Count;

    public TypeMeta this[int index] => m_sorted_set.Types[index];

    public ReadOnlySpan<TypeMeta> Span => CollectionsMarshal.AsSpan(m_sorted_set.Types);

    public ImmutableHashSet<TypeMeta> Set => m_sorted_set.RawSet;

    public TypeSetId Id => new(m_id);

    #endregion

    #region Inst

    private sealed class Inst<TS>(ulong id, SortedSet set)
        : TypeSet(id, set)
    {
        #region Contains

        public override bool Contains<TV>() => ContainsValue<TS, TV>.Value;

        #endregion

        #region IndexOf

        public override int IndexOf<TV>() => IndexOfValue<TS, TV>.Value;

        #endregion

        #region IsOverlap

        public override bool IsOverlap(TypeSet other) => other.IsOverlap<TS>();

        private protected override bool IsOverlap<TO>() => IsOverlapValue<TO, TS>.Value;

        #endregion

        #region IsSubsetOf

        public override bool IsSubsetOf(TypeSet other) => other.IsSubsetOf<TS>();

        private protected override bool IsSubsetOf<TO>() => IsSubsetOfValue<TO, TS>.Value;

        #endregion

        #region IsSupersetOf

        public override bool IsSupersetOf(TypeSet other) => other.IsSupersetOf<TS>();
        private protected override bool IsSupersetOf<TO>() => IsSupersetOfValue<TO, TS>.Value;

        #endregion

        #region ArcheType

        public override ArcheType ArcheType() => ArcheTypeValue<TS>.Value;

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
        public static readonly bool Value = SetOf<TA>().Remove(TypeMeta.Of<Entity>())
            .Overlaps(SetOf<TB>().Remove(TypeMeta.Of<Entity>()));
    }

    #endregion

    #region IsSubsetOf

    public abstract bool IsSubsetOf(TypeSet other);

    private protected abstract bool IsSubsetOf<TO>();

    private static class IsSubsetOfValue<TA, TB>
    {
        public static readonly bool Value = SetOf<TA>().Remove(TypeMeta.Of<Entity>())
            .IsSubsetOf(SetOf<TB>().Remove(TypeMeta.Of<Entity>()));
    }

    #endregion

    #region IsSupersetOf

    public abstract bool IsSupersetOf(TypeSet other);

    private protected abstract bool IsSupersetOf<TO>();

    private static class IsSupersetOfValue<TA, TB>
    {
        public static readonly bool Value = SetOf<TA>().Remove(TypeMeta.Of<Entity>())
            .IsSupersetOf(SetOf<TB>().Remove(TypeMeta.Of<Entity>()));
    }

    #endregion

    #endregion

    #region Static

    private static ulong s_set_id_inc;

    private static readonly ConcurrentDictionary<SortedSet, TypeSet> s_set_cache = new();
    private static readonly ConcurrentDictionary<ulong, TypeSet> s_id_to_set = new();
    private static readonly ConcurrentDictionary<Type, TypeSet> s_unique_to_set = new();

    // ReSharper disable once InconsistentNaming
    internal readonly struct SortedSet : IEquatable<SortedSet>
    {
        public static SortedSet Create(ImmutableHashSet<TypeMeta> RawSet)
        {
            var set = RawSet;
            set = set.Add(TypeMeta.Of<Entity>());
            return new(SortType(set), set);
        }
        private SortedSet(List<TypeMeta> types, ImmutableHashSet<TypeMeta> rawSet)
        {
            Types = types;
            RawSet = rawSet;
        }

        public readonly List<TypeMeta> Types;
        public readonly ImmutableHashSet<TypeMeta> RawSet;
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

    private static class Static<TSet> where TSet : ITypeSet
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly TypeSet s_set = Get(TSet.Types);
    }

    public static TypeSet Get(ImmutableHashSet<TypeMeta> types) => s_set_cache.GetOrAdd(SortedSet.Create(types),
        static set =>
        {
            var unique = UniqueTypeEmitter.Emit();
            var id = Interlocked.Increment(ref s_set_id_inc);
            var type = typeof(Inst<>).MakeGenericType(unique);
            var inst = (TypeSet)Activator.CreateInstance(type, id, set)!;
            s_id_to_set[id] = inst;
            s_unique_to_set[unique] = inst;
            return inst;
        });

    public static TypeSet Get<TSet>() where TSet : ITypeSet => Static<TSet>.s_set;

    internal static TypeSet? TryFromId(ulong id) => s_id_to_set.GetValueOrDefault(id);
    internal static TypeSet TypeSetOf<TS>() => s_unique_to_set[typeof(TS)];

    internal static ImmutableHashSet<TypeMeta> SetOf<TS>() => s_unique_to_set[typeof(TS)].Set;

    internal static List<TypeMeta> ListOf<TS>() => s_unique_to_set[typeof(TS)].m_sorted_set.Types;

    #endregion

    #region ArcheType

    public abstract ArcheType ArcheType();

    private static class ArcheTypeValue<TS>
    {
        public static readonly ArcheType Value = ArcheEmitter.Get(TypeSetOf<TS>());
    }

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

    public List<TypeMeta>.Enumerator GetEnumerator() => m_sorted_set.Types.GetEnumerator();
    IEnumerator<TypeMeta> IEnumerable<TypeMeta>.GetEnumerator() => m_sorted_set.Types.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => m_sorted_set.Types.GetEnumerator();

    #endregion

    #region ToString

    public override string ToString() =>
        $"TypeSet {{ Id = {m_id}, Types = {string.Join(", ", m_sorted_set.Types.Select(static t => t.Type))} }}";

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

    #region Of

    public static TypeSet Of() => Get<EmptyTypeSet>();

    #endregion

    #region Builder

    public static TypeSetBuilder Builder() => new(ImmutableHashSet<TypeMeta>.Empty);

    #endregion
}

#endregion

#region EmptyTypeSet

public readonly struct EmptyTypeSet : ITypeSet
{
    public static ImmutableHashSet<TypeMeta> Types => ImmutableHashSet<TypeMeta>.Empty;
}

#endregion

#region TypeSetBuilder

public readonly partial record struct TypeSetBuilder(ImmutableHashSet<TypeMeta> Types)
{
    public TypeSet Build() => TypeSet.Get(Types);
}

#endregion
