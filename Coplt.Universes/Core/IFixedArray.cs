namespace Coplt.Universes.Core;

public interface IFixedArray<T>
{
    /// <summary>
    /// How many items are included
    /// </summary>
    public int Length { get; }
    /// <summary>
    /// Get item ref at index
    /// </summary>
    public ref T this[int index] { get; }
    /// <summary>
    /// Get the <see cref="Span{T}"/>
    /// </summary>
    public Span<T> Span { get; }
    /// <summary>
    /// Size of <see cref="IFixedArray{T}"/>, not <see cref="Length"/>
    /// </summary>
    public int Size { get; }
}
