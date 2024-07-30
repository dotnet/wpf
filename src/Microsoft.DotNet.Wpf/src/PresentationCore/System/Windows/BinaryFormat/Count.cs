// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
namespace System.Windows
{
    /// <summary>
    ///  Positive <see langword="int"/> enforcing count of items.
    /// </summary>
    /// <devdoc>
    ///  Idea here is that doing this makes it less likely we'll slip through cases where
    ///  we don't check for negative numbers. And also not confuse counts with ids.
    /// </devdoc>
    internal readonly struct Count : IEquatable<Count>
    {
        private readonly int _count;

        private Count(int count) => _count = count.OrThrowIfNegative();

        public static Count Zero { get; } = 0;
        public static Count One { get; } = 1;

        public static implicit operator int(Count value) => value._count;
        public static implicit operator Count(int value) => new(value);

        public override bool Equals([NotNullWhen(true)] object? obj)
            => (obj is Count count && Equals(count)) || (obj is int value && value == _count);

        public bool Equals(Count other) => _count == other._count;

        public override readonly int GetHashCode() => _count.GetHashCode();
        public override readonly string ToString() => _count.ToString();

        public static bool operator ==(Count left, Count right) => left._count == right._count;
        public static bool operator !=(Count left, Count right) => !(left == right);
    }
}
