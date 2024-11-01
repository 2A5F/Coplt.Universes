using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Coplt.Universes.Utilities;

namespace Coplt.Universes.Core;

public abstract class ChunkGetAtEmitter<C, T> where C : ArcheType.Chunk
{
    public static ChunkGetAtEmitter<C, T> Instance { get; } = Create();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract ref T TryGetAt(C chunk, int index);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract ref T TryGetAtUnchecked(C chunk, int index);

    #region Impls

    private sealed class NullImpl : ChunkGetAtEmitter<C, T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ref T TryGetAt(C chunk, int index) => ref Unsafe.NullRef<T>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ref T TryGetAtUnchecked(C chunk, int index) => ref Unsafe.NullRef<T>();
    }

    private sealed class DefaultImpl : ChunkGetAtEmitter<C, T>
    {
        private static T s_default = default!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ref T TryGetAt(C chunk, int index)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, chunk.Count, nameof(index));
            return ref s_default;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ref T TryGetAtUnchecked(C chunk, int index) => ref s_default;
    }

    #endregion

    private static ChunkGetAtEmitter<C, T> Create()
    {
        var arche = ArcheType.FromChunk<C>()!;
        if (!arche.TypeSet.Contains<T>()) return new NullImpl();
        if (TypeUtils.IsTag<T>()) return new DefaultImpl();

        var type_index = arche.TypeSet.IndexOf<T>();
        var get_span_view = typeof(C).GetProperty($"SpanView{type_index}")!.GetMethod!;

        var mod = arche.Module;
        var guid = Guid.NewGuid();
        var typ = mod.DefineType($"ChunkGetAt.Impl[{guid:N}]", TypeAttributes.Public, typeof(ChunkGetAtEmitter<C, T>));

        {
            var try_get_at = typ.DefineMethod(nameof(TryGetAt), MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(T).MakeByRefType(), [typeof(C), typeof(int)]);
            typ.DefineMethodOverride(try_get_at,
                typeof(ChunkGetAtEmitter<C, T>).GetMethod(nameof(TryGetAt))!);
            var ilg = try_get_at.GetILGenerator();

            ilg.Emit(OpCodes.Ldarg_2);
            ilg.Emit(OpCodes.Conv_U4);
            ilg.Emit(OpCodes.Ldarg_1);
            ilg.Emit(OpCodes.Callvirt, EmitterHelper.MethodOf__ArcheType_Chunk_get_Count());
            ilg.Emit(OpCodes.Ldstr, "index");
            ilg.Emit(OpCodes.Call,
                EmitterHelper.MethodOf__ArgumentOutOfRangeException_ThrowIfGreaterThanOrEqual()
                    .MakeGenericMethod(typeof(int)));

            var tmp = ilg.DeclareLocal(typeof(ChunkSpan<T>));
            ilg.Emit(OpCodes.Ldarg_1);
            ilg.Emit(OpCodes.Callvirt, get_span_view);
            ilg.Emit(OpCodes.Stloc, tmp);
            ilg.Emit(OpCodes.Ldloca, tmp);
            ilg.Emit(OpCodes.Ldarg_2);
            ilg.Emit(OpCodes.Call, typeof(ChunkSpan<T>).GetMethod(nameof(ChunkSpan<T>.UncheckedGet))!);
            ilg.Emit(OpCodes.Ret);
        }
        
        {
            var try_get_at = typ.DefineMethod(nameof(TryGetAtUnchecked), MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(T).MakeByRefType(), [typeof(C), typeof(int)]);
            typ.DefineMethodOverride(try_get_at,
                typeof(ChunkGetAtEmitter<C, T>).GetMethod(nameof(TryGetAtUnchecked))!);
            var ilg = try_get_at.GetILGenerator();
            
            var tmp = ilg.DeclareLocal(typeof(ChunkSpan<T>));
            ilg.Emit(OpCodes.Ldarg_1);
            ilg.Emit(OpCodes.Callvirt, get_span_view);
            ilg.Emit(OpCodes.Stloc, tmp);
            ilg.Emit(OpCodes.Ldloca, tmp);
            ilg.Emit(OpCodes.Ldarg_2);
            ilg.Emit(OpCodes.Call, typeof(ChunkSpan<T>).GetMethod(nameof(ChunkSpan<T>.UncheckedGet))!);
            ilg.Emit(OpCodes.Ret);
        }
        
        var type = typ.CreateType();
        var inst = (ChunkGetAtEmitter<C, T>)Activator.CreateInstance(type)!;
        return inst;
    }
}
