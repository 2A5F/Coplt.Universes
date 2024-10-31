using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Coplt.Universes.Utilities;

namespace Coplt.Universes.Core;

public static class ArcheEmitter
{
    private static readonly ConcurrentDictionary<TypeSet, ArcheType> s_cache = new();

    public static ArcheType Get(TypeSet set) => s_cache.GetOrAdd(set, Emit);

    private static ArcheType Emit(TypeSet set)
    {
        #region Calc Chunk Size, Stride

        var unmanaged_size = (uint)set.Where(static t => t is { IsTag: false, IsManaged: false })
            .Sum(static t => t.AlignedSize);
        var max_align = Math.Max(set.Where(static t => t is { IsTag: false, IsManaged: false })
            .Max(static t => t.Align), ArcheConstants.CacheLineSize);
        var stride = 0u;
        var chunk_size = ArcheConstants.ChunkSize;
        if (unmanaged_size > 0)
        {
            if (unmanaged_size < ArcheConstants.ChunkSize - max_align)
            {
                stride = (ArcheConstants.ChunkSize - max_align) / unmanaged_size;
            }
            if (stride < ArcheConstants.MinChunkCapacity)
            {
                var (q, r) = uint.DivRem(
                    unmanaged_size * ArcheConstants.MinChunkCapacity + max_align,
                    ArcheConstants.ChunkSize
                );
                chunk_size = (r == 0 ? q : q + 1) * ArcheConstants.ChunkSize;
                stride = ArcheConstants.MinChunkCapacity;
            }
            Debug.Assert(chunk_size - max_align >= unmanaged_size * stride);
        }
        else
        {
            stride = ArcheConstants.MinChunkCapacity;
            chunk_size = 0;
        }

        #endregion

        #region Define Type

        var asm_name = $"{nameof(Coplt)}.{nameof(Universes)}.{nameof(Core)}.Arche{set.Id.Id}";
        var asm = AssemblyBuilder.DefineDynamicAssembly(new(asm_name), AssemblyBuilderAccess.RunAndCollect);
        var mod = asm.DefineDynamicModule(asm_name);

        var arche_typ_name = $"{nameof(Coplt)}.{nameof(Universes)}.{nameof(Core)}.Arche{set.Id.Id}";
        var arche_typ = mod.DefineType(arche_typ_name,
            TypeAttributes.Public | TypeAttributes.Sealed, typeof(ArcheType));

        var no_tag_count = set.Where(static t => !t.IsTag).Count();

        var chunk_typ =
            arche_typ.DefineNestedType($"Chunk`{no_tag_count}", TypeAttributes.NestedPublic, typeof(ArcheType.Chunk));
        var chunk_generics = chunk_typ.DefineGenericParameters(
            set.Where(static t => !t.IsTag).Select(static t => $"T{t.Id.Id}").ToArray()
        );

        var chunk_typ_inst = chunk_typ.MakeGenericType(
            set.Where(static t => !t.IsTag).Select(static t => t.Type).ToArray()
        );

        #endregion

        #region Define Chunk Fields

        const string unmanaged_memory_handle_name = "m_unmanaged_memory_handle";
        const string unmanaged_array_name = "m_unmanaged_array";
        const string managed_array_name = "m_managed_array";

        FieldInfo? unmanaged_array_field = null;
        FieldInfo? unmanaged_memory_handle = null;
        if (unmanaged_size > 0)
        {
            unmanaged_array_field = chunk_typ.DefineField(unmanaged_array_name, typeof(byte*), FieldAttributes.Public);
            unmanaged_memory_handle = chunk_typ.DefineField(unmanaged_memory_handle_name,
                typeof(MemoryHandle), FieldAttributes.Public);
        }
        var managed_array_fields = new Dictionary<int, FieldInfo>();
        foreach (
            var (_, i) in set
                .Select(static (a, b) => (a, b))
                .Where(static a => a.a is { IsTag: false, IsManaged: true })
        )
        {
            managed_array_fields.Add(
                i, chunk_typ.DefineField($"{managed_array_name}{i}", chunk_generics[i].MakeArrayType(),
                    FieldAttributes.Public)
            );
        }

        #endregion

        #region Define Chunk Ctor

        ConstructorBuilder chunk_ctor;
        {
            chunk_ctor = chunk_typ.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, [arche_typ]);
            var ilg = chunk_ctor.GetILGenerator();

            {
                // this.ArcheType = arg1
                
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Ldarg_1);
                ilg.Emit(OpCodes.Callvirt, EmitterHelper.MethodOf__ArcheType_Chunk_set_ArcheType());
            }

            if (unmanaged_memory_handle != null)
            {
                // unmanaged_memory_handle = UnmanagedAllocator.Alloc(
                //     chunk_size,
                //     max_align
                // );
                // unmanaged_array_field = unmanaged_memory_handle.Pointer;

                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Ldc_I4, chunk_size);
                ilg.Emit(OpCodes.Conv_U);
                ilg.Emit(OpCodes.Ldc_I4, max_align);
                ilg.Emit(OpCodes.Conv_U);
                ilg.Emit(OpCodes.Call, EmitterHelper.MethodOf__UnmanagedAllocator_Alloc());
                ilg.Emit(OpCodes.Stfld, unmanaged_memory_handle);

                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Dup);
                ilg.Emit(OpCodes.Ldfld, unmanaged_memory_handle);
                ilg.Emit(OpCodes.Callvirt, EmitterHelper.MethodOf__MemoryHandle_get_Pointer());
                ilg.Emit(OpCodes.Stfld, unmanaged_array_field!);
            }

            foreach (var (i, field) in managed_array_fields)
            {
                // field = new T[stride];

                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Ldc_I4, stride);
                ilg.Emit(OpCodes.Newarr, chunk_generics[i]);
                ilg.Emit(OpCodes.Stfld, field);
            }

            ilg.Emit(OpCodes.Ret);
        }

        #endregion

        #region Define Chunk View/Span

        var cur_offset = 0u;
        var unmanaged_offsets = new Dictionary<int, uint>();

        foreach (
            var (meta, i) in set.Select(static (a, b) => (a, b))
                .Where(static a => a.a is { IsTag: false, IsManaged: false })
        )
        {
            cur_offset = TypeUtils.AlignUp(cur_offset, meta.Align);
            unmanaged_offsets[i] = cur_offset;

            var generic = chunk_generics[i];
            // SysSpan
            if (meta.Size == meta.AlignedSize)
            {
                var ret_type = typeof(Span<>).MakeGenericType(generic);
                var create = EmitterHelper.MethodOf__MemoryMarshal_CreateSpan().MakeGenericMethod(generic);
                var prop = chunk_typ.DefineProperty($"Span{i}", PropertyAttributes.None, ret_type, []);
                var get = chunk_typ.DefineMethod($"get_Span{i}", MethodAttributes.Public, ret_type, []);
                prop.SetGetMethod(get);
                var ilg = get.GetILGenerator();
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Ldfld, unmanaged_array_field!);
                if (cur_offset != 0)
                {
                    ilg.Emit(OpCodes.Ldc_I4, cur_offset);
                    ilg.Emit(OpCodes.Conv_U);
                    ilg.Emit(OpCodes.Add);
                }
                ilg.Emit(OpCodes.Ldc_I4, stride);
                ilg.Emit(OpCodes.Call, create);
                ilg.Emit(OpCodes.Ret);
            }
            // SpanView
            {
                var ret_type = typeof(ChunkSpan<>).MakeGenericType(generic);
                var create = EmitterHelper.MethodOf_TypeUtils_CreateChunkSpan().MakeGenericMethod(generic);
                var prop = chunk_typ.DefineProperty($"SpanView{i}", PropertyAttributes.None, ret_type, []);
                var get = chunk_typ.DefineMethod($"get_SpanView{i}", MethodAttributes.Public, ret_type, []);
                prop.SetGetMethod(get);
                var ilg = get.GetILGenerator();
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Ldfld, unmanaged_array_field!);
                if (cur_offset != 0)
                {
                    ilg.Emit(OpCodes.Ldc_I4, cur_offset);
                    ilg.Emit(OpCodes.Conv_U);
                    ilg.Emit(OpCodes.Add);
                }
                ilg.Emit(OpCodes.Ldc_I4, stride);
                ilg.Emit(OpCodes.Call, create);
                ilg.Emit(OpCodes.Ret);
            }
            // SliceView
            {
                var ret_type = typeof(ChunkSlice<>).MakeGenericType(generic);
                var create = EmitterHelper.MethodOf_TypeUtils_CreateChunkSlice().MakeGenericMethod(generic);
                var prop = chunk_typ.DefineProperty($"SliceView{i}", PropertyAttributes.None, ret_type, []);
                var get = chunk_typ.DefineMethod($"get_SliceView{i}", MethodAttributes.Public, ret_type, []);
                prop.SetGetMethod(get);
                var ilg = get.GetILGenerator();
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Ldfld, unmanaged_array_field!);
                if (cur_offset != 0)
                {
                    ilg.Emit(OpCodes.Ldc_I4, cur_offset);
                    ilg.Emit(OpCodes.Conv_U);
                    ilg.Emit(OpCodes.Add);
                }
                ilg.Emit(OpCodes.Ldc_I4, stride);
                ilg.Emit(OpCodes.Call, create);
                ilg.Emit(OpCodes.Ret);
            }
            // View
            {
                var ret_type = typeof(ChunkView<>).MakeGenericType(generic);
                var create = EmitterHelper.MethodOf_TypeUtils_CreateChunkView().MakeGenericMethod(generic);
                var prop = chunk_typ.DefineProperty($"View{i}", PropertyAttributes.None, ret_type, []);
                var get = chunk_typ.DefineMethod($"get_View{i}", MethodAttributes.Public, ret_type, []);
                prop.SetGetMethod(get);
                var ilg = get.GetILGenerator();
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Dup);
                ilg.Emit(OpCodes.Ldfld, unmanaged_array_field!);
                if (cur_offset != 0)
                {
                    ilg.Emit(OpCodes.Ldc_I4, cur_offset);
                    ilg.Emit(OpCodes.Conv_U);
                    ilg.Emit(OpCodes.Add);
                }
                ilg.Emit(OpCodes.Call, create);
                ilg.Emit(OpCodes.Ret);
            }

            cur_offset += meta.AlignedSize * (stride - 1) + meta.Size;
        }

        Debug.Assert(cur_offset < chunk_size);

        foreach (var (_, i) in set.Select(static (a, b) => (a, b))
                     .Where(static a => a.a is { IsTag: false, IsManaged: true }))
        {
            var generic = chunk_generics[i];
            // SysSpan
            {
                var ret_type = typeof(Span<>).MakeGenericType(generic);
                var create = EmitterHelper.MethodOf_TypeUtils_CreateSpanByArray().MakeGenericMethod(generic);
                var prop = chunk_typ.DefineProperty($"Span{i}", PropertyAttributes.None, ret_type, []);
                var get = chunk_typ.DefineMethod($"get_Span{i}", MethodAttributes.Public, ret_type, []);
                prop.SetGetMethod(get);
                var ilg = get.GetILGenerator();
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Ldfld, managed_array_fields[i]);
                ilg.Emit(OpCodes.Call, create);
                ilg.Emit(OpCodes.Ret);
            }
            // SpanView
            {
                var ret_type = typeof(ChunkSpan<>).MakeGenericType(generic);
                var create = EmitterHelper.MethodOf_TypeUtils_CreateChunkSpanByArray().MakeGenericMethod(generic);
                var prop = chunk_typ.DefineProperty($"SpanView{i}", PropertyAttributes.None, ret_type, []);
                var get = chunk_typ.DefineMethod($"get_SpanView{i}", MethodAttributes.Public, ret_type, []);
                prop.SetGetMethod(get);
                var ilg = get.GetILGenerator();
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Ldfld, managed_array_fields[i]);
                ilg.Emit(OpCodes.Call, create);
                ilg.Emit(OpCodes.Ret);
            }
            // View
            {
                var ret_type = typeof(ChunkView<>).MakeGenericType(generic);
                var create = EmitterHelper.MethodOf_TypeUtils_CreateChunkViewByArray().MakeGenericMethod(generic);
                var prop = chunk_typ.DefineProperty($"View{i}", PropertyAttributes.None, ret_type, []);
                var get = chunk_typ.DefineMethod($"get_View{i}", MethodAttributes.Public, ret_type, []);
                prop.SetGetMethod(get);
                var ilg = get.GetILGenerator();
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Ldfld, managed_array_fields[i]);
                ilg.Emit(OpCodes.Call, create);
                ilg.Emit(OpCodes.Ret);
            }
        }

        #endregion

        #region Define Count

        {
            var get = chunk_typ.DefineMethod("get_Count", MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(int), []);
            chunk_typ.DefineMethodOverride(get, EmitterHelper.MethodOf__ArcheType_Chunk_get_Count());
            var ilg = get.GetILGenerator();
            ilg.Emit(OpCodes.Ldc_I4, stride);
            ilg.Emit(OpCodes.Ret);
        }

        #endregion

        #region Define Create Chunk

        {
            var method = arche_typ.DefineMethod(nameof(ArcheType.CreateChunk),
                MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(ArcheType.Chunk), []);
            arche_typ.DefineMethodOverride(method, EmitterHelper.MethodOf__ArcheType_CreateChunk());
            var ctor = TypeBuilder.GetConstructor(chunk_typ_inst, chunk_ctor);
            var ilg = method.GetILGenerator();
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Newobj, ctor);
            ilg.Emit(OpCodes.Ret);
        }

        #endregion

        #region Create Type

        var arche_type = arche_typ.CreateType();

        var chunk_type = chunk_typ.CreateType().MakeGenericType(
            set.Where(static t => !t.IsTag).Select(static t => t.Type).ToArray()
        );

        #endregion

        #region Create ArcheType Instance

        var inst = (ArcheType)Activator.CreateInstance(arche_type)!;
        inst.TypeSet = set;
        inst.Type = arche_type;
        inst.ChunkType = chunk_type;
        inst.ChunkSize = chunk_size;
        inst.Stride = stride;
        inst.UnmanagedArrayField = chunk_type.GetField(unmanaged_array_name);
        inst.ManagedArrayField =
            managed_array_fields.ToFrozenDictionary(static a => a.Key,
                a => chunk_type.GetField($"{managed_array_name}{a.Key}")!);
        inst.UnmanagedOffsets = unmanaged_offsets.ToFrozenDictionary();
        // todo
        return inst;

        #endregion
    }
}
