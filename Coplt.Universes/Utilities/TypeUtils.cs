using System.Reflection;
using System.Runtime.CompilerServices;
using Coplt.Universes.Core;
using InlineIL;
using static InlineIL.IL.Emit;

namespace Coplt.Universes.Utilities;

#pragma warning disable CS0169

public static unsafe class TypeUtils
{
    #region AlignUp

    public static uint AlignUp(uint value, uint alignment) => (value + alignment - 1) & ~(alignment - 1);

    public static ulong AlignUp(ulong value, ulong alignment) => (value + alignment - 1) & ~(alignment - 1);

    public static nuint AlignUp(nuint value, nuint alignment) => (value + alignment - 1) & ~(alignment - 1);

    #endregion

    #region AlignOf

    private struct AlignOfHelper<T>
    {
        private byte m_dummy;
        private T m_data;
    }

    private static unsafe class AlignOfValue<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly uint Value;

        static AlignOfValue()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) Value = 1;
            else if (typeof(T).GetCustomAttribute<AlignAttribute>() is { Alignment: var alignment })
                Value = alignment;
            else Value = (uint)sizeof(AlignOfHelper<T>) - (uint)sizeof(T);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint AlignOf<T>() => AlignOfValue<T>.Value;

    public static int AlignOf(Type type)
    {
        Ldtoken(new MethodRef(typeof(TypeUtils), nameof(AlignOf), 1));
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

        Unbox_Any<int>();
        return IL.Return<int>();
    }

    #endregion

    #region IsTag

    private static class IsTagOf<T>
    {
        public static readonly bool Value =
            !typeof(T).IsPrimitive && typeof(T).IsValueType && Unsafe.SizeOf<T>() == 1 &&
            typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic).Length is 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTag<T>() => IsTagOf<T>.Value;

    public static bool IsTag(Type type)
    {
        Ldtoken(new MethodRef(typeof(TypeUtils), nameof(IsTag), 1));
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

        Unbox_Any<bool>();
        return IL.Return<bool>();
    }

    #endregion

    #region Create ChunkView

    public static ChunkView<T> CreateChunkView<T>(T* ptr, int length) => new(ptr, length);

    #endregion

    #region Create Span By Array

    public static Span<T> CreateSpanByArray<T>(T[] array) => new(array);

    #endregion
}
