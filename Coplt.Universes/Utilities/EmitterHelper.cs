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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf_TypeUtils_CreateChunkSlice()
    {
        Ldtoken(new MethodRef(typeof(TypeUtils), nameof(TypeUtils.CreateChunkSlice)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();
        return IL.Return<MethodInfo>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf_TypeUtils_CreateChunkSpan()
    {
        Ldtoken(new MethodRef(typeof(TypeUtils), nameof(TypeUtils.CreateChunkSpan)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();
        return IL.Return<MethodInfo>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf_TypeUtils_CreateChunkSpanByArray()
    {
        Ldtoken(new MethodRef(typeof(TypeUtils), nameof(TypeUtils.CreateChunkSpanByArray)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();
        return IL.Return<MethodInfo>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf_TypeUtils_CreateChunkView()
    {
        Ldtoken(new MethodRef(typeof(TypeUtils), nameof(TypeUtils.CreateChunkView)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();
        return IL.Return<MethodInfo>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf_TypeUtils_CreateChunkViewByArray()
    {
        Ldtoken(new MethodRef(typeof(TypeUtils), nameof(TypeUtils.CreateChunkViewByArray)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();
        return IL.Return<MethodInfo>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf_TypeUtils_CreateSpanByArray()
    {
        Ldtoken(new MethodRef(typeof(TypeUtils), nameof(TypeUtils.CreateSpanByArray)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();
        return IL.Return<MethodInfo>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf__ArgumentOutOfRangeException_ThrowIfGreaterThanOrEqual()
    {
        Ldtoken(new MethodRef(typeof(ArgumentOutOfRangeException),
            nameof(ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();
        return IL.Return<MethodInfo>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf__ArcheType_CreateChunk()
    {
        Ldtoken(new MethodRef(typeof(ArcheType), nameof(ArcheType.CreateChunk)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();
        return IL.Return<MethodInfo>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf__ArcheType_Chunk_set_ArcheType()
    {
        Ldtoken(MethodRef.PropertySet(typeof(ArcheType.Chunk), nameof(ArcheType.Chunk.ArcheType)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();
        return IL.Return<MethodInfo>();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf__ArcheType_Chunk_get_Count()
    {
        Ldtoken(MethodRef.PropertyGet(typeof(ArcheType.Chunk), nameof(ArcheType.Chunk.Count)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();
        return IL.Return<MethodInfo>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf__ArcheType_Chunk_TryGetAt()
    {
        Ldtoken(new MethodRef(typeof(ArcheType.Chunk), nameof(ArcheType.Chunk.TryGetAt)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();
        return IL.Return<MethodInfo>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf__ArcheType_TryGetAt()
    {
        Ldtoken(new MethodRef(typeof(ArcheType), nameof(ArcheType.TryGetAt)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();
        return IL.Return<MethodInfo>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf__ArcheType_Chunk_TryGetAtUnchecked()
    {
        Ldtoken(new MethodRef(typeof(ArcheType.Chunk), nameof(ArcheType.Chunk.TryGetAtUnchecked)));
        Call(new MethodRef(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));
        Castclass<MethodInfo>();
        return IL.Return<MethodInfo>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo MethodOf__ArcheType_TryGetAtUnchecked()
    {
        Ldtoken(new MethodRef(typeof(ArcheType), nameof(ArcheType.TryGetAtUnchecked)));
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
