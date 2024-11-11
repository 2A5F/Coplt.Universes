namespace Coplt.Universes.Collections;

public interface IDenseHashSearchCtrl<out R, C>
    where R : allows ref struct
    where C : allows ref struct
{
    /// <summary>
    /// must be O(1)
    /// </summary>
    uint Size { get; }
    /// <summary>
    /// must size += 1
    /// </summary>
    C Add();
    C At(uint index);
    bool Eq(C ctx);
    ulong Hash(C ctx);
    R Get(C ctx);
    R None();
    R RemoveSwapLast(C last, uint index);
    R RemoveLast();
}
