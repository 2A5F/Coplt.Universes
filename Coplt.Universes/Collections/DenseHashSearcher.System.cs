using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Coplt.Universes.Utilities;

namespace Coplt.Universes.Collections;

public static partial class DenseHashSearcher
{
    [StructLayout(LayoutKind.Auto)]
    public struct SysAlg() : IDenseHashSearcher<SysAlg>
    {
        #region Create

        public static SysAlg Create() => new();

        #endregion

        #region Static

        static SysAlg()
        {
            if (!BitConverter.IsLittleEndian)
                throw new NotSupportedException("Only Little endian system arrays are supported");
        }

        #endregion

        #region Fields

        private struct Slot
        {
            public uint hash;
            public uint next;

            public override string ToString() => $"{{ Hash = {hash}, Next = {(int)next} }}";
        }

        private uint[] m_buckets = new uint[4];
        private Slot[] m_slots = new Slot[4];
        private nuint m_size_m1 = 3;

        #endregion

        #region GetBucket

        private readonly ref uint GetBucket(uint hash) => ref m_buckets.GetUnchecked(hash & m_size_m1);

        private readonly ref Slot SlotAt(nuint index) => ref m_slots.GetUnchecked(index);

        #endregion

        #region Grow

        private void Grow(uint count)
        {
            var new_size = (uint)m_buckets.Length * 2;
            var new_buckets = new uint[new_size];
            var new_slots = new Slot[new_size];
            Array.Copy(m_slots, new_slots, m_slots.Length);
            m_buckets = new_buckets;
            m_slots = new_slots;
            m_size_m1 = new_size - 1;

            for (var i = 0u; i < count; i++)
            {
                ref var slot = ref SlotAt(i);
                if ((int)slot.next >= -1)
                {
                    ref var bucket = ref GetBucket(slot.hash);
                    slot.next = bucket - 1;
                    bucket = i + 1;
                }
            }
        }

        #endregion

        #region TryEmplace

        public R UnsafeTryEmplace<S, R, C>(S search, ulong hash, out bool is_new)
            where S : IDenseHashSearchCtrl<R, C>, allows ref struct
            where R : allows ref struct
            where C : allows ref struct
        {
            var collision_count = 0u;
            ref var bucket = ref GetBucket((uint)hash);
            var i = bucket - 1;
            while (i < (uint)m_buckets.Length)
            {
                ref var slot = ref SlotAt(i);
                var ctx = search.At(i);
                if (slot.hash == (uint)hash && search.Eq(ctx))
                {
                    is_new = false;
                    return search.Get(ctx);
                }

                i = slot.next;

                collision_count++;
                if (collision_count > m_buckets.Length)
                    throw new NotSupportedException("Concurrent operations are not supported");
            }

            var index = search.Size;
            if (index == m_buckets.Length)
            {
                Grow(index);
                bucket = ref GetBucket((uint)hash);
            }

            {
                ref var slot = ref SlotAt(index);
                slot.hash = (uint)hash;
                slot.next = bucket - 1;
                bucket = index + 1;
                is_new = true;
                var ctx = search.Add();
                return search.Get(ctx);
            }
        }

        #endregion

        #region TryFind

        public readonly R UnsafeTryFind<S, R, C>(S search, ulong hash)
            where S : IDenseHashSearchCtrl<R, C>, allows ref struct
            where R : allows ref struct
            where C : allows ref struct
        {
            if (search.Size == 0) return search.None();
            var collision_count = 0u;
            var i = GetBucket((uint)hash) - 1;
            do
            {
                if (i >= (uint)m_buckets.Length)
                    return search.None();

                ref var slot = ref SlotAt(i);
                var ctx = search.At(i);
                if (slot.hash == (uint)hash && search.Eq(ctx))
                    return search.Get(ctx);

                i = slot.next;

                collision_count++;
            } while (collision_count <= (uint)m_buckets.Length);

            throw new NotSupportedException("Concurrent operations are not supported");
        }

        #endregion

        #region Remove

        public R UnsafeRemove<S, R, C>(S search, ulong hash)
            where S : IDenseHashSearchCtrl<R, C>, allows ref struct
            where R : allows ref struct
            where C : allows ref struct
        {
            if (search.Size == 0) return search.None();
            var count_m1 = search.Size - 1;
            uint collision_count = 0;
            ref var bucket = ref GetBucket((uint)hash);
            var prev = uint.MaxValue;
            var i = bucket - 1; // Value in buckets is 1-based
            while ((int)i >= 0)
            {
                ref var slot = ref SlotAt(i);
                var ctx = search.At(i);
                if (slot.hash == (uint)hash && search.Eq(ctx))
                {
                    if (i == count_m1)
                    {
                        if (prev != uint.MaxValue)
                        {
                            ref var prev_slot = ref SlotAt(prev);
                            prev_slot.next = slot.next;
                        }
                        else
                        {
                            bucket = slot.next + 1;
                        }

                        return search.RemoveLast();
                    }

                    ref var last_slot = ref SlotAt(count_m1);
                    var last_ctx = search.At(count_m1);
                    var last_prev = uint.MaxValue;
                    ref var last_bucket = ref GetBucket(last_slot.hash);
                    var last_i = last_bucket - 1;
                    uint last_collision_count = 0;
                    while ((int)last_i >= 0)
                    {
                        if (Unsafe.AreSame(ref SlotAt(last_i), ref last_slot))
                        {
                            if (prev != uint.MaxValue)
                            {
                                ref var prev_slot = ref SlotAt(prev);
                                prev_slot.next = slot.next;
                            }
                            else
                            {
                                bucket = slot.next + 1;
                            }
                            if (last_prev != uint.MaxValue)
                            {
                                ref var prev_slot = ref SlotAt(prev);
                                prev_slot.next = i;
                            }
                            else
                            {
                                last_bucket = i + 1;
                            }
                            slot = last_slot;
                            return search.RemoveSwapLast(last_ctx, i);
                        }

                        last_prev = last_i;
                        last_i = last_slot.next;

                        last_collision_count++;
                        if (last_collision_count > (uint)m_buckets.Length)
                            throw new NotSupportedException("Concurrent operations are not supported");
                    }
                    throw new NotSupportedException("Concurrent operations are not supported");
                }

                prev = i;
                i = slot.next;

                collision_count++;
                if (collision_count > (uint)m_buckets.Length)
                    throw new NotSupportedException("Concurrent operations are not supported");
            }
            return search.None();
        }

        #endregion

        #region Clear

        public void Clear()
        {
            Array.Clear(m_buckets);
            Array.Clear(m_slots);
        }

        #endregion
    }
}
