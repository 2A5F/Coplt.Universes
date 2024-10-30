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

        var unmanaged_size = set.Where(static t => t is { IsTag: false, IsManaged: false }).Sum(static t => t.Size);
        var stride = 0;
        var chunk_size = ArcheConstants.ChunkSize;
        if (unmanaged_size > 0)
        {
            if (unmanaged_size < ArcheConstants.ChunkSize - ArcheConstants.CacheLineSize)
            {
                stride = (ArcheConstants.ChunkSize - ArcheConstants.CacheLineSize) / unmanaged_size;
            }
            if (stride < ArcheConstants.MinChunkCapacity)
            {
                var (q, r) = int.DivRem(
                    unmanaged_size * ArcheConstants.MinChunkCapacity + ArcheConstants.CacheLineSize,
                    ArcheConstants.ChunkSize
                );
                chunk_size = (r == 0 ? q : q + 1) * ArcheConstants.ChunkSize;
                stride = ArcheConstants.MinChunkCapacity;
            }
            Debug.Assert(chunk_size - ArcheConstants.CacheLineSize >= unmanaged_size * stride);
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

        var chunk_typ =
            arche_typ.DefineNestedType($"Chunk`{set.Count}", TypeAttributes.NestedPublic, typeof(ArcheType.Chunk));
        var chunk_generics = chunk_typ.DefineGenericParameters(
            set.Where(static t => !t.IsTag).Select(static t => $"T{t.Id.Id}").ToArray()
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

        {
            var ctor = chunk_typ.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, []);
            var ilg = ctor.GetILGenerator();

            if (unmanaged_memory_handle != null)
            {
                // unmanaged_memory_handle = UnmanagedAllocator.Alloc(
                //     chunk_size - ArcheConstants.CacheLineSize,
                //     ArcheConstants.CacheLineSize
                // );
                // unmanaged_array_field = unmanaged_memory_handle.Pointer;
                
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Ldc_I4, chunk_size - ArcheConstants.CacheLineSize);
                ilg.Emit(OpCodes.Ldc_I4, ArcheConstants.CacheLineSize);
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
        // todo
        return inst;

        #endregion
    }
}
