using System.Runtime.CompilerServices;

namespace Coplt.Universes.Core;

public abstract class UnmanagedAllocator
{
    public static UnmanagedAllocator Instance { get; set; } = new DefaultUnmanagedAllocator();

    public static MemoryHandle Alloc(int size, int align) => Instance.Allocate(size, align);

    public abstract MemoryHandle Allocate(int size, int align);
}

public abstract unsafe class MemoryHandle : IDisposable
{
    public abstract void* Pointer { get; }

    #region Dispose

    protected abstract void Dispose(bool disposing);
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    ~MemoryHandle() => Dispose(false);

    #endregion
}

public sealed unsafe class DefaultUnmanagedAllocator : UnmanagedAllocator
{
    public override MemoryHandle Allocate(int size, int align)
    {
        var arr = GC.AllocateUninitializedArray<byte>(size + align, true);
        var ptr = Unsafe.AsPointer(ref arr[0]);
        // todo align
        return new ArrayMemoryHandle(arr, ptr);
    }

    // ReSharper disable once InconsistentNaming
    private sealed class ArrayMemoryHandle(byte[] arr, void* ptr) : MemoryHandle
    {
        private byte[] arr = arr;
        
        public override void* Pointer => ptr;
        protected override void Dispose(bool disposing) { }
    }
}
