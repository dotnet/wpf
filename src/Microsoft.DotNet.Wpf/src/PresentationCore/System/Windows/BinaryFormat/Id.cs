// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace System.Windows
{
    /// <summary>
    ///  Identifier struct.
    /// </summary>
    internal readonly struct Id : IEquatable<Id>
    {
        private readonly int _id;

        // It is possible that the id may be negative with value types. See BinaryObjectWriter.InternalGetId.
        private Id(int id) => _id = id;

        public static implicit operator int(Id value) => value._id;
        public static implicit operator Id(int value) => new(value);

        public override bool Equals([NotNullWhen(true)] object? obj)
            => (obj is Id id && Equals(id)) || (obj is int value && value == _id);

        public bool Equals(Id other) => _id == other._id || -1 * _id == other._id;

        public override readonly int GetHashCode() => _id < 0 ? (-1 * _id).GetHashCode() : _id.GetHashCode();
        public override readonly string ToString() => _id.ToString();

        public static bool operator ==(Id left, Id right) => left.Equals(right);

        public static bool operator !=(Id left, Id right) => !(left == right);
    }
}
