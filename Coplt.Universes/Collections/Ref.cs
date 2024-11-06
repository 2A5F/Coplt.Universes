using System.Runtime.CompilerServices;

namespace Coplt.Universes.Collections;

public readonly ref struct RefBox<T>(ref T Ref)
{
    public readonly ref T Ref = ref Ref;

    public ref T GetPinnableReference() => ref Ref;

    public bool IsNull => Unsafe.IsNullRef(in Ref);

    public override string ToString() => IsNull ? "null" : $"{Ref}";
}

public readonly ref struct ReadOnlyRefBox<T>(ref readonly T Ref)
{
    public readonly ref readonly T Ref = ref Ref;

    public ref readonly T GetPinnableReference() => ref Ref;

    public bool IsNull => Unsafe.IsNullRef(in Ref);

    public override string ToString() => IsNull ? "null" : $"{Ref}";
}
