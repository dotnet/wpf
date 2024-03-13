// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Runtime.Serialization;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace System.Windows
{
    /// <summary>
    ///  Object model for the binary format put out by BinaryFormatter. It parses and creates a model but does not
    ///  instantiate any reference types outside of string.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   This is useful for explicitly controlling the rehydration of binary formatted data. BinaryFormatter is
    ///   depreciated for security concerns (it has no way to constrain what it hydrates from an incoming stream).
    ///  </para>
    ///  <para>
    ///   NOTE: Multidimensional and jagged arrays are not yet implemented.
    ///  </para>
    /// </remarks>
    internal sealed class BinaryFormattedObject
    {
        // Don't reserve space in collections based on read lengths for more than this size to defend against corrupted lengths.
#if DEBUG
    internal const int MaxNewCollectionSize = 1024 * 10;
#else
    internal const int MaxNewCollectionSize = 10;
#endif
        private readonly List<IRecord> _records = new();
        private readonly RecordMap _recordMap = new();
        /// <summary>
        ///  Creates <see cref="BinaryFormattedObject"/> by parsing <paramref name="stream"/>.
        /// </summary>
        public BinaryFormattedObject(Stream stream, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(stream);
            using BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen: leaveOpen);
            IRecord? currentRecord;
            do
            {
                try
                {
                    currentRecord = Record.ReadBinaryFormatRecord(reader, _recordMap);
                }
                catch (SerializationException)
                {
                    throw;
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidCastException or ArithmeticException or IOException)
                {
                    // Make the exception easier to catch, but retain the original stack trace.
                    throw;
                }

                _records.Add(currentRecord);
            }
            while (currentRecord is not MessageEnd);
        }

        /// <summary>
        ///  Total count of top-level records.
        /// </summary>
        public int RecordCount => _records.Count;

        /// <summary>
        ///  Gets a record by it's index.
        /// </summary>
        public IRecord this[int index] => _records[index];

        /// <summary>
        ///  Gets a record by it's identfier. Not all records have identifiers, only ones that
        ///  can be referenced by other records.
        /// </summary>
        public IRecord this[Id id] => _recordMap[id];
    }
}

