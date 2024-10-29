using System.Reflection;
using System.Runtime.CompilerServices;
using InlineIL;
using static InlineIL.IL.Emit;

namespace Coplt.Universes.Utilities;

#pragma warning disable CS0169

public static class TypeUtils
{
    #region AlignOf

    private struct AlignOfHelper<T>
    {
        private byte m_dummy;
        private T m_data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int AlignOf<T>() => sizeof(AlignOfHelper<T>) - sizeof(T);
    
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
            !typeof(T).IsPrimitive && Unsafe.SizeOf<T>() == 1 &&
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
}
