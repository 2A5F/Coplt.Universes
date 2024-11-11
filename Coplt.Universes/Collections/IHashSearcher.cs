namespace Coplt.Universes.Collections;

public interface IHashSearcher<out Self>
{
    public static abstract Self Create();

    public R UnsafeTryEmplace<S, R, C>(S search, ulong hash, out bool is_new)
        where S : IDenseHashSearchCtrl<R, C>, allows ref struct
        where R : allows ref struct
        where C : allows ref struct;

    public R UnsafeTryFind<S, R, C>(S search, ulong hash)
        where S : IDenseHashSearchCtrl<R, C>, allows ref struct
        where R : allows ref struct
        where C : allows ref struct;

    public R UnsafeRemove<S, R, C>(S search, ulong hash)
        where S : IDenseHashSearchCtrl<R, C>, allows ref struct
        where R : allows ref struct
        where C : allows ref struct;

    public void Clear();
}
