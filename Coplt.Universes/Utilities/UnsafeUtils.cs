using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Coplt.Universes.Utilities;

public static class UnsafeUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetUnchecked<T>(this T[] array, int index) =>
        ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetUnchecked<T>(this T[] array, uint index) =>
        ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetUnchecked<T>(this T[] array, nint index) =>
        ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetUnchecked<T>(this T[] array, nuint index) =>
        ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
}
