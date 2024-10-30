using System.Diagnostics;
using System.Runtime.CompilerServices;
using Coplt.Universes.Utilities;

namespace Coplt.Universes.Core;

public abstract class UnmanagedAllocator
{
    public static UnmanagedAllocator Instance { get; set; } = new DefaultUnmanagedAllocator();

    public static MemoryHandle Alloc(nuint size, nuint align) => Instance.Allocate(size, align);

    public abstract MemoryHandle Allocate(nuint size, nuint align);
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
    public override MemoryHandle Allocate(nuint size, nuint align)
    {
        if (size >= int.MaxValue) throw new ArgumentException($"Size too large, must < {int.MaxValue}", nameof(size));
        if (!nuint.IsPow2(align)) throw new ArgumentException("Align must be power of 2", nameof(align));
        var arr = GC.AllocateUninitializedArray<byte>((int)(size + align - 1), true);
        var ptr = (nuint)Unsafe.AsPointer(ref arr[0]);
        var new_ptr = TypeUtils.AlignUp(ptr, align);
        Debug.Assert(new_ptr + size <= ptr + size + align - 1);
        return new ArrayMemoryHandle(arr, (void*)new_ptr);
    }

    // ReSharper disable once InconsistentNaming
    private sealed class ArrayMemoryHandle(byte[] arr, void* ptr) : MemoryHandle
    {
        private byte[] arr = arr;

        public override void* Pointer => ptr;
        protected override void Dispose(bool disposing)
        {
            if (disposing) arr = null!;
        }
    }
}
