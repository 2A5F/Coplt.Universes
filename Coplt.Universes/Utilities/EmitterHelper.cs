using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Coplt.Universes.Core;
using InlineIL;
using static InlineIL.IL.Emit;

namespace Coplt.Universes.Utilities;

public static class EmitterHelper
{
    #region LdToken

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf__MemoryMarshal_CreateSpan()
    {
        Ldtoken(new MethodRef(typeof(MemoryMarshal), nameof(MemoryMarshal.CreateSpan)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();
        return IL.Return<MethodInfo>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConstructorInfo ConstructorOf__IndexOutOfRangeException()
    {
        Ldtoken(MethodRef.Constructor(typeof(IndexOutOfRangeException)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<ConstructorInfo>();
        return IL.Return<ConstructorInfo>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf__Unsafe_Add__ref_int()
    {
        Ldtoken(new MethodRef(typeof(Unsafe), nameof(Unsafe.Add), TypeRef.MethodGenericParameters[0].MakeByRefType(),
            typeof(int)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();
        return IL.Return<MethodInfo>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf__UnmanagedAllocator_Alloc()
    {
        Ldtoken(new MethodRef(typeof(UnmanagedAllocator), nameof(UnmanagedAllocator.Alloc)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();
        return IL.Return<MethodInfo>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf__MemoryHandle_get_Pointer()
    {
        Ldtoken(MethodRef.PropertyGet(typeof(MemoryHandle), nameof(MemoryHandle.Pointer)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();
        return IL.Return<MethodInfo>();
    }

    #endregion

    #region EmitLdInd

    public static void EmitLdInd(this ILGenerator ilg, Type type)
    {
        if (type == typeof(int))
        {
            ilg.Emit(OpCodes.Ldind_I4);
            return;
        }
        if (type == typeof(uint))
        {
            ilg.Emit(OpCodes.Ldind_U4);
            return;
        }
        if (type == typeof(long) || type == typeof(ulong))
        {
            ilg.Emit(OpCodes.Ldind_I8);
            return;
        }
        if (type == typeof(short))
        {
            ilg.Emit(OpCodes.Ldind_I2);
            return;
        }
        if (type == typeof(ushort))
        {
            ilg.Emit(OpCodes.Ldind_U2);
            return;
        }
        if (type == typeof(byte))
        {
            ilg.Emit(OpCodes.Ldind_U1);
            return;
        }
        if (type == typeof(sbyte))
        {
            ilg.Emit(OpCodes.Ldind_I1);
            return;
        }
        if (type == typeof(float))
        {
            ilg.Emit(OpCodes.Ldind_R4);
            return;
        }
        if (type == typeof(double))
        {
            ilg.Emit(OpCodes.Ldind_R8);
            return;
        }
        if (type == typeof(nint) || type == typeof(nuint) || type.IsByRef || type.IsPointer)
        {
            ilg.Emit(OpCodes.Ldind_I);
            return;
        }
        if (type.IsValueType)
        {
            ilg.Emit(OpCodes.Ldobj, type);
            return;
        }
        else
        {
            ilg.Emit(OpCodes.Ldind_Ref);
            return;
        }
    }

    #endregion
}
