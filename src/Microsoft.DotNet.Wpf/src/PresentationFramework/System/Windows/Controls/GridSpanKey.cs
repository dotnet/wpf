// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Controls;

public partial class Grid
{
    /// <summary>
    /// Helper struct for representing a key for a span in <see cref="Dictionary{TKey, TValue}"/>.
    /// </summary>
    private readonly struct GridSpanKey : IEquatable<GridSpanKey>
    {
        /// <summary>
        /// Returns start index of the span.
        /// </summary>
        internal int Start { get; }

        /// <summary>
        /// Returns span count.
        /// </summary>
        internal int Count { get; }

        /// <summary>
        /// Returns <see langword="true"/> if this is a column span.
        /// <see langword="false"/> if this is a row span.
        /// </summary>
        internal bool U { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="start">Starting index of the span.</param>
        /// <param name="count">Span count.</param>
        /// <param name="u"><see langword="true"/> for columns; <see langword="false"/> for rows.</param>
        internal GridSpanKey(int start, int count, bool u)
        {
            Start = start;
            Count = count;
            U = u;
        }

        /// <summary>
        /// <see cref="object.GetHashCode"/>
        /// </summary>
        public override int GetHashCode()
        {
            int hash = Start ^ (Count << 2);

            return U ? hash &= 0x7ffffff : hash |= 0x8000000;
        }

        /// <summary>
        /// <see cref="object.Equals(object)"/>
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is GridSpanKey other && Equals(other);
        }

        /// <summary>
        /// The <see cref="IEquatable{SpanKey}"/> implementation.
        /// </summary>
        public bool Equals(GridSpanKey other)
        {
            return other.Start == Start && other.Count == Count && other.U == U;
        }
    }
}
