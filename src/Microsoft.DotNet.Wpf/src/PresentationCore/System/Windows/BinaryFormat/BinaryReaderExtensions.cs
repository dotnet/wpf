// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.IO;

namespace System.Windows
{
    internal static class BinaryReaderExtensions
    {
        /// <summary>
    ///  Reads a binary formatted <see cref="DateTime"/> from the given <paramref name="reader"/>.
    /// </summary>
    /// <exception cref="SerializationException">The data was invalid.</exception>
    public static unsafe DateTime ReadDateTime(this BinaryReader reader)
        => CreateDateTimeFromData(reader.ReadInt64());

    /// <summary>
    ///  Creates a <see cref="DateTime"/> object from raw data with validation.
    /// </summary>
    /// <exception cref="SerializationException"><paramref name="data"/> was invalid.</exception>
    private static DateTime CreateDateTimeFromData(long data)
    {
        // Copied from System.Runtime.Serialization.Formatters.Binary.BinaryParser

        // Use DateTime's public constructor to validate the input, but we
        // can't return that result as it strips off the kind. To address
        // that, store the value directly into a DateTime via an unsafe cast.
        // See BinaryFormatterWriter.WriteDateTime for details.

        try
        {
            const long TicksMask = 0x3FFFFFFFFFFFFFFF;
            _ = new DateTime(data & TicksMask);
        }
        catch (ArgumentException ex)
        {
            // Bad data
            throw new SerializationException(ex.Message, ex);
        }

        return Unsafe.As<long, DateTime>(ref data);
    }

    /// <summary>
    ///  Returns the remaining amount of bytes in the given <paramref name="reader"/>.
    /// </summary>
    public static long Remaining(this BinaryReader reader)
    {
        Stream stream = reader.BaseStream;
        return stream.Length - stream.Position;
    }
    }
}

