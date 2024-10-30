namespace Coplt.Universes.Core;

/// <summary>
/// Specifies the alignment requirements of a structure, unmanaged structures only, and within Ecs only
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class AlignAttribute(uint Alignment) : Attribute
{
    public uint Alignment { get; } = Alignment;
}
