namespace Coplt.Universes;

public readonly record struct Entity(ulong value)
{
    public int Id => (int)value;
    public uint Ver => (uint)(value >> 32);

    public bool IsNull => Ver == 0;

    public bool Equals(Entity other) => value == other.value;
    public override int GetHashCode() => value.GetHashCode();

    public override string ToString() => $"Entity {{ {Id} : {Ver} }}";
}
