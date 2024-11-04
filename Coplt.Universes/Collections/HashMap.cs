using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Coplt.Universes.Utilities;

namespace Coplt.Universes.Collections;

public interface IHashSearch
{
    bool Eq(uint index);
}

[StructLayout(LayoutKind.Auto)]
public struct SHashSearcher()
{
    /// <summary>
    /// 2^(64-m_shift) number of buckets
    /// </summary>
    public const byte initial_shifts = 64 - 2;
    public const float default_max_load_factor = 0.8F;

    public struct Bucket
    {
        /// <summary>
        /// skip 1 byte fingerprint
        /// </summary>
        public const uint dist_inc = 1U << 8;
        /// <summary>
        /// mask for 1 byte of fingerprint
        /// </summary>
        public const uint fingerprint_mask = dist_inc - 1;

        /// <summary>
        /// upper 3 byte: distance to original bucket. lower byte: fingerprint from hash
        /// </summary>
        public uint m_dist_and_fingerprint;
        public uint m_value_idx;
    }

    private Bucket[] m_buckets = [];
    private int m_num_buckets;
    private int m_max_bucket_capacity;
    private float m_max_load_factor = default_max_load_factor;
    private byte m_shifts = initial_shifts;

    #region Misc

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint DistAndFingerprintFromHash(uint hash) => Bucket.dist_inc | (hash & Bucket.fingerprint_mask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint GetBucketIndex(uint hash) => hash >> m_shifts;

    #endregion

    #region TryEmplace

    public bool UnsafeTryEmplace<S>(S search, uint hash) where S : IHashSearch, allows ref struct
    {
        var dist_and_fingerprint = DistAndFingerprintFromHash(hash);
        var bucket_index = GetBucketIndex(hash);

        for (;;)
        {
            ref var bucket = ref m_buckets.GetUnchecked(bucket_index);
            if (dist_and_fingerprint == bucket.m_dist_and_fingerprint)
            {
                if (search.Eq(bucket.m_value_idx)) { }
            }
        }

        // todo
        return default;
    }

    #endregion
}
