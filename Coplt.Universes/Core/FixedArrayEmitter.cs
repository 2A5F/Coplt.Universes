using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Coplt.Universes.Utilities;

namespace Coplt.Universes.Core;

public static class FixedArrayEmitter
{
    private static readonly ConcurrentDictionary<int, Type> s_cache = new();

    public static Type Get(int len)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(len);
        return s_cache.GetOrAdd(len, Emit);
    }

    private static Type Emit(int len)
    {
        var asm_name = $"{nameof(Coplt)}.{nameof(Universes)}.{nameof(Core)}.FixedArray{len}";
        var asm = AssemblyBuilder.DefineDynamicAssembly(new(asm_name), AssemblyBuilderAccess.RunAndCollect);
        var mod = asm.DefineDynamicModule(asm_name);

        var typ_name = $"{asm_name}.FixedArray`1";
        var typ = mod.DefineType(typ_name, TypeAttributes.Public | TypeAttributes.Sealed, typeof(ValueType));
        var generic = typ.DefineGenericParameters("T")[0];
        typ.SetCustomAttribute(
            new CustomAttributeBuilder(typeof(InlineArrayAttribute).GetConstructor([typeof(int)])!, [len]));
        typ.DefineField($"_", generic, FieldAttributes.Private);

        var inter = typeof(IFixedArray<>).MakeGenericType(generic);
        typ.AddInterfaceImplementation(typeof(IFixedArray<>).MakeGenericType(generic));

        {
            var span_type = typeof(Span<>).MakeGenericType(generic);
            var as_span_prop = typ.DefineProperty($"Span", PropertyAttributes.None, CallingConventions.Standard,
                span_type, []);
            as_span_prop.SetCustomAttribute(typeof(UnscopedRefAttribute).GetConstructor([])!, []);
            var as_span_get = typ.DefineMethod("get_Span", MethodAttributes.Public | MethodAttributes.Virtual,
                CallingConventions.Standard,
                span_type, []);
            as_span_prop.SetGetMethod(as_span_get);
            typ.DefineMethodOverride(as_span_get,
                TypeBuilder.GetMethod(inter, typeof(IFixedArray<>).GetMethod("get_Span")!));
            var ilg = as_span_get.GetILGenerator();
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldc_I4, len);
            ilg.Emit(OpCodes.Call, EmitterHelper.MethodOf__MemoryMarshal_CreateSpan().MakeGenericMethod(generic));
            ilg.Emit(OpCodes.Ret);
        }

        {
            var item_prop = typ.DefineProperty($"Item", PropertyAttributes.None, CallingConventions.Standard,
                generic.MakeByRefType(), [typeof(int)]);
            var item_get = typ.DefineMethod("get_Item", MethodAttributes.Public | MethodAttributes.Virtual,
                CallingConventions.Standard,
                generic.MakeByRefType(), [typeof(int)]);
            item_prop.SetGetMethod(item_get);
            typ.DefineMethodOverride(item_get,
                TypeBuilder.GetMethod(inter, typeof(IFixedArray<>).GetMethod("get_Item")!));
            var ilg = item_get.GetILGenerator();
            var err = ilg.DefineLabel();

            // if (index < 0) goto err;
            ilg.Emit(OpCodes.Ldarg_1);
            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Blt_S, err);

            // if (index >= len) goto err;
            ilg.Emit(OpCodes.Ldarg_1);
            ilg.Emit(OpCodes.Ldc_I4, len);
            ilg.Emit(OpCodes.Bge_S, err);

            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldarg_1);
            ilg.Emit(OpCodes.Call, EmitterHelper.MethodOf__Unsafe_Add__ref_int().MakeGenericMethod(generic));
            ilg.Emit(OpCodes.Ret);

            ilg.MarkLabel(err);
            ilg.Emit(OpCodes.Newobj, EmitterHelper.ConstructorOf__IndexOutOfRangeException());
            ilg.Emit(OpCodes.Throw);
        }

        {
            var len_prop = typ.DefineProperty($"Length", PropertyAttributes.None, CallingConventions.Standard,
                typeof(int), []);
            var len_get = typ.DefineMethod("get_Length", MethodAttributes.Public | MethodAttributes.Virtual,
                CallingConventions.Standard,
                typeof(int), []);
            len_prop.SetGetMethod(len_get);
            typ.DefineMethodOverride(len_get,
                TypeBuilder.GetMethod(inter, typeof(IFixedArray<>).GetMethod("get_Length")!));
            var ilg = len_get.GetILGenerator();
            ilg.Emit(OpCodes.Ldc_I4, len);
            ilg.Emit(OpCodes.Ret);
        }

        {
            var size_prop = typ.DefineProperty($"Size", PropertyAttributes.None, CallingConventions.Standard,
                typeof(int), []);
            var size_get = typ.DefineMethod("get_Size", MethodAttributes.Public | MethodAttributes.Virtual,
                CallingConventions.Standard,
                typeof(int), []);
            size_prop.SetGetMethod(size_get);
            typ.DefineMethodOverride(size_get,
                TypeBuilder.GetMethod(inter, typeof(IFixedArray<>).GetMethod("get_Size")!));
            var ilg = size_get.GetILGenerator();
            ilg.Emit(OpCodes.Sizeof, typ);
            ilg.Emit(OpCodes.Ret);
        }

        return typ.CreateType();
    }
}
