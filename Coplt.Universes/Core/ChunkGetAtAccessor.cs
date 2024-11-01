using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Coplt.Universes.Utilities;

namespace Coplt.Universes.Core;

public abstract class ChunkGetAtAccessor<C, T> where C : ArcheType.Chunk
{
    public static ChunkGetAtAccessor<C, T> Instance { get; } = Create();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract ref T TryGetAt(C chunk, int index);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract Ref<T> TryGetImmAt(C chunk, int index);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract Mut<T> TryGetMutAt(C chunk, int index);

    #region Impls

    private sealed class NullImpl : ChunkGetAtAccessor<C, T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ref T TryGetAt(C chunk, int index) => ref Unsafe.NullRef<T>();
        public override Ref<T> TryGetImmAt(C chunk, int index) => default;
        public override Mut<T> TryGetMutAt(C chunk, int index) => default;
    }

    private sealed class DefaultImpl : ChunkGetAtAccessor<C, T>
    {
        private static readonly T[] s_default = [default!];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ref T TryGetAt(C chunk, int index) => ref s_default[0];
        public override Ref<T> TryGetImmAt(C chunk, int index) => new(s_default, 0);
        public override Mut<T> TryGetMutAt(C chunk, int index) => new(s_default, 0);
    }

    #endregion

    private static ChunkGetAtAccessor<C, T> Create()
    {
        var arche = ArcheType.FromChunk<C>()!;
        if (!arche.TypeSet.Contains<T>()) return new NullImpl();
        if (TypeUtils.IsTag<T>()) return new DefaultImpl();

        var type_index = arche.TypeSet.IndexOf<T>();
        var meta = arche.TypeSet[type_index];
        var get_span_view = typeof(C).GetProperty($"SpanView{type_index}")!.GetMethod!;
        var get_slice_view = typeof(C).GetProperty($"SliceView{type_index}")?.GetMethod!;

        var guid = Guid.NewGuid();
        var asm_name = $"{nameof(Coplt)}.{nameof(Universes)}.{nameof(Core)}.ArcheAccessor{arche.TypeSet.Id.Id}.{guid:N}";
        var asm = AssemblyBuilder.DefineDynamicAssembly(new(asm_name), AssemblyBuilderAccess.RunAndCollect);
        var mod = asm.DefineDynamicModule(asm_name);
        var typ = mod.DefineType($"ChunkGetAtAccessor[{arche.TypeSet.Id.Id}]", TypeAttributes.Public | TypeAttributes.Sealed,
            typeof(ChunkGetAtAccessor<C, T>));

        {
            var try_get_at = typ.DefineMethod(nameof(TryGetAt), MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(T).MakeByRefType(), [typeof(C), typeof(int)]);
            typ.DefineMethodOverride(try_get_at,
                typeof(ChunkGetAtAccessor<C, T>).GetMethod(nameof(TryGetAt))!);
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

        if (meta.IsManaged)
        {
            throw new NotImplementedException("todo");
        }
        else
        {
            {
                var try_get_at = typ.DefineMethod(nameof(TryGetImmAt), MethodAttributes.Public | MethodAttributes.Virtual,
                    typeof(Ref<T>), [typeof(C), typeof(int)]);
                typ.DefineMethodOverride(try_get_at,
                    typeof(ChunkGetAtAccessor<C, T>).GetMethod(nameof(TryGetImmAt))!);
                var ilg = try_get_at.GetILGenerator();

                var tmp = ilg.DeclareLocal(typeof(ChunkSlice<T>));
                ilg.Emit(OpCodes.Ldarg_1);
                ilg.Emit(OpCodes.Callvirt, get_slice_view);
                ilg.Emit(OpCodes.Stloc, tmp);
                ilg.Emit(OpCodes.Ldloca, tmp);
                ilg.Emit(OpCodes.Ldarg_2);
                ilg.Emit(OpCodes.Call, typeof(ChunkSlice<T>).GetMethod(nameof(ChunkSlice<T>.ImmRefAtUnchecked))!);
                ilg.Emit(OpCodes.Ret);
            }
            
            {
                var try_get_at = typ.DefineMethod(nameof(TryGetMutAt), MethodAttributes.Public | MethodAttributes.Virtual,
                    typeof(Mut<T>), [typeof(C), typeof(int)]);
                typ.DefineMethodOverride(try_get_at,
                    typeof(ChunkGetAtAccessor<C, T>).GetMethod(nameof(TryGetMutAt))!);
                var ilg = try_get_at.GetILGenerator();

                var tmp = ilg.DeclareLocal(typeof(ChunkSlice<T>));
                ilg.Emit(OpCodes.Ldarg_1);
                ilg.Emit(OpCodes.Callvirt, get_slice_view);
                ilg.Emit(OpCodes.Stloc, tmp);
                ilg.Emit(OpCodes.Ldloca, tmp);
                ilg.Emit(OpCodes.Ldarg_2);
                ilg.Emit(OpCodes.Call, typeof(ChunkSlice<T>).GetMethod(nameof(ChunkSlice<T>.MutRefAtUnchecked))!);
                ilg.Emit(OpCodes.Ret);
            }
        }

        var type = typ.CreateType();
        var inst = (ChunkGetAtAccessor<C, T>)Activator.CreateInstance(type)!;
        return inst;
    }
}
