using System.Reflection;
using System.Reflection.Emit;

namespace Coplt.Universes.Core;

public static class UniqueTypeEmitter
{
    private const string asm_name = $"{nameof(Coplt)}.{nameof(Universes)}.{nameof(Core)}.UniqueTypes";
    private static readonly AssemblyBuilder asm =
        AssemblyBuilder.DefineDynamicAssembly(new(asm_name), AssemblyBuilderAccess.RunAndCollect);
    private static readonly ModuleBuilder mod = asm.DefineDynamicModule(asm_name);
    
    public static Type Emit()
    {
        var guid = Guid.NewGuid();
        var typ_name = $"{guid:N}";
        var typ = mod.DefineType(typ_name, TypeAttributes.Public | TypeAttributes.Sealed, typeof(ValueType));

        return typ.CreateType();
    }
}
