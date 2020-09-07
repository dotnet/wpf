// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: BufferCache class implementation.
//

using System;
using System.Threading;
using MS.Internal.Text.TextInterface;

namespace MS.Internal.FontCache
{
    /// <summary>
    /// A static, thread safe array cache used to minimize heap allocations.
    /// </summary>
    /// <remarks>
    /// Cached arrays are not zero initialized, and they may be larger than
    /// the requested number of elements.
    /// </remarks>
    internal static class BufferCache
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Attempts to release all allocated memory.  Has no effect if the cache
        /// is locked by another thread.
        /// </summary>
        internal static void Reset()
        {
            if (Interlocked.Increment(ref _mutex) == 1)
            {
                _buffers = null;
            }
            Interlocked.Decrement(ref _mutex);
        }

        /// <summary>
        /// Returns a GlyphMetrics[].
        /// </summary>
        /// <param name="length">
        /// Minimum number of elements in the array.
        /// </param>
        internal static GlyphMetrics[] GetGlyphMetrics(int length)
        {
            GlyphMetrics[] glyphMetrics = (GlyphMetrics[])GetBuffer(length, GlyphMetricsIndex);

            if (glyphMetrics == null)
            {
                glyphMetrics = new GlyphMetrics[length];
            }

            return glyphMetrics;
        }

        /// <summary>
        /// Releases a previously allocated GlyphMetrics[], possibly adding it
        /// to the cache.
        /// </summary>
        /// <remarks>
        /// It is not strictly necessary to call this method after receiving an
        /// array.  The penalty is the performance hit of doing a heap allocation
        /// on the next request if this method is not called.
        /// </remarks>
        internal static void ReleaseGlyphMetrics(GlyphMetrics[] glyphMetrics)
        {
            ReleaseBuffer(glyphMetrics, GlyphMetricsIndex);
        }

        /// <summary>
        /// Returns a ushort[].
        /// </summary>
        /// <param name="length">
        /// Minimum number of elements in the array.
        /// </param>
        internal static ushort[] GetUShorts(int length)
        {
            ushort[] ushorts = (ushort[])GetBuffer(length, UShortsIndex);

            if (ushorts == null)
            {
                ushorts = new ushort[length];
            }

            return ushorts;
        }

        /// <summary>
        /// Releases a previously allocated ushort[], possibly adding it
        /// to the cache.
        /// </summary>
        /// <remarks>
        /// It is not strictly necessary to call this method after receiving an
        /// array.  The penalty is the performance hit of doing a heap allocation
        /// on the next request if this method is not called.
        /// </remarks>
        internal static void ReleaseUShorts(ushort[] ushorts)
        {
            ReleaseBuffer(ushorts, UShortsIndex);
        }

        /// <summary>
        /// Returns a uint[].
        /// </summary>
        /// <param name="length">
        /// Minimum number of elements in the array.
        /// </param>
        internal static uint[] GetUInts(int length)
        {
            uint[] uints = (uint[])GetBuffer(length, UIntsIndex);

            if (uints == null)
            {
                uints = new uint[length];
            }

            return uints;
        }

        /// <summary>
        /// Releases a previously allocated uint[], possibly adding it
        /// to the cache.
        /// </summary>
        /// <remarks>
        /// It is not strictly necessary to call this method after receiving an
        /// array.  The penalty is the performance hit of doing a heap allocation
        /// on the next request if this method is not called.
        /// </remarks>
        internal static void ReleaseUInts(uint[] uints)
        {
            ReleaseBuffer(uints, UIntsIndex);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Searches for an array in the cache.
        /// </summary>
        /// <param name="length">
        /// Minimum number of elements in the array.
        /// </param>
        /// <param name="index">
        /// Specifies the type of array.
        /// </param>
        /// <returns>
        /// A matching array if present, otherwise null.
        /// </returns>
        private static Array GetBuffer(int length, int index)
        {
            Array buffer = null;

            if (Interlocked.Increment(ref _mutex) == 1)
            {
                if (_buffers != null &&
                    _buffers[index] != null &&
                    length <= _buffers[index].Length)
                {
                    buffer = _buffers[index];
                    _buffers[index] = null;
                }
            }
            Interlocked.Decrement(ref _mutex);

            return buffer;
        }

        /// <summary>
        /// Takes ownership of an array.
        /// </summary>
        /// <param name="buffer">
        /// The array.  May be null.
        /// </param>
        /// <param name="index">
        /// Specifies the type of array.
        /// </param>
        private static void ReleaseBuffer(Array buffer, int index)
        {
            if (buffer != null)
            {
                if (Interlocked.Increment(ref _mutex) == 1)
                {
                    if (_buffers == null)
                    {
                        _buffers = new Array[BuffersLength];
                    }

                    if (_buffers[index] == null ||
                        (_buffers[index].Length < buffer.Length && buffer.Length <= MaxBufferLength))
                    {
                        _buffers[index] = buffer;
                    }
                }
                Interlocked.Decrement(ref _mutex);
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Max number of elements in any cached array.  If a request if made for a larger array
        // it will always be allocated from the heap.
        private const int MaxBufferLength = 1024;

        // Indices in _buffers for each supported type.
        private const int GlyphMetricsIndex         = 0;
        private const int UIntsIndex                = 1;
        private const int UShortsIndex              = 2;
        private const int BuffersLength             = 3;

        // Guards access to _buffers.
        static private long _mutex;

        // Array of cached arrays, one bucker per supported type.
        // Currently, we cache just one array per type.  A more general cache would hold N byte arrays.
        // However, we don't currently have any scenarios that hold more than one array of the same type
        // or more than two arrays of different types at the same time, so it is difficult to justify
        // making the implementation more complex.  ComputeTypographyAvailabilities could benefit from
        // a more general cache (UnicodeRange.GetFullRange could use a cached array), but the savings
        // in profiled scenarios are small, ~16k for MSNBaml.exe.  If we find a more compelling
        // scenario a change might be worthwhile.
        static private Array[] _buffers;

        #endregion Private Fields
    }
}
