using System.Reflection;
using System.Runtime.CompilerServices;
using Coplt.Universes.Utilities;
using InlineIL;
using static InlineIL.IL.Emit;

namespace Coplt.Universes.Core;

public readonly record struct TypeMeta(
    Type Type,
    TypeId Id,
    uint Size,
    uint Align,
    uint AlignedSize,
    bool IsManaged,
    bool IsTag
)
{
    #region Of

    private static class TypeMetaOf<T>
    {
        public static readonly TypeMeta Value = new(
            typeof(T), TypeId.Of<T>(), (uint)Unsafe.SizeOf<T>(), TypeUtils.AlignOf<T>(), TypeUtils.AlignedSizeOf<T>(),
            RuntimeHelpers.IsReferenceOrContainsReferences<T>(),
            TypeUtils.IsTag<T>()
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypeMeta Of<T>() => TypeMetaOf<T>.Value;

    public static TypeMeta Of(Type type)
    {
        Ldtoken(new MethodRef(typeof(TypeMeta), nameof(Of), 1));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();

        Ldc_I4_1();
        Newarr<Type>();
        Dup();
        Ldc_I4_0();
        Ldarg_0();
        Stelem_Ref();
        Callvirt(new MethodRef(typeof(MethodInfo), nameof(MethodInfo.MakeGenericMethod)));

        Ldnull();
        Call(new MethodRef(typeof(Array), nameof(Array.Empty)).MakeGenericMethod(typeof(object)));
        Callvirt(new MethodRef(typeof(MethodBase), nameof(MethodBase.Invoke), typeof(object), typeof(object[])));

        Unbox_Any<TypeMeta>();
        return IL.Return<TypeMeta>();
    }

    #endregion

    #region Equals

    public bool Equals(TypeMeta other) => Id.Equals(other.Id);

    public override int GetHashCode() => Id.GetHashCode();

    #endregion
}

public readonly record struct FieldMeta(FieldInfo Field, TypeMeta FieldType, TypeMeta Type, int Index)
{
    #region Equals

    public bool Equals(FieldMeta other) => Field.Equals(other.Field);
    public override int GetHashCode() => Field.GetHashCode();

    #endregion
}
