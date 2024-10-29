using System.Reflection;
using System.Reflection.Emit;

namespace Coplt.Universes.Core;

public static class UniqueTypeEmitter
{
    public static Type Emit()
    {
        var guid = Guid.NewGuid();
        var asm_name = $"{nameof(Coplt)}.{nameof(Universes)}.{nameof(Core)}.Unique.{guid:N}";
        var asm = AssemblyBuilder.DefineDynamicAssembly(new(asm_name), AssemblyBuilderAccess.RunAndCollect);
        var mod = asm.DefineDynamicModule(asm_name);
        
        var typ_name = $"{guid:N}";
        var typ = mod.DefineType(typ_name, TypeAttributes.Public | TypeAttributes.Sealed, typeof(ValueType));
        
        return typ.CreateType();
    }
}
