// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Collections.Generic;

namespace System.Windows
{
    internal abstract partial class NullRecord
    {
        /// <summary>
        ///  Multiple null object record.
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/f4abb5dd-aab7-4e0a-9d77-1d6c99f5779e">
        ///    [MS-NRBF] 2.5.5
        ///   </see>
        ///  </para>
        /// </remarks>
        internal sealed class ObjectNullMultiple : NullRecord, IRecord<ObjectNullMultiple>
        {
            public static RecordType RecordType => RecordType.ObjectNullMultiple;

            public ObjectNullMultiple(Count count) => NullCount = count;

            static ObjectNullMultiple IBinaryFormatParseable<ObjectNullMultiple>.Parse(
                BinaryReader reader,
                RecordMap recordMap)
                => new(reader.ReadInt32());

            public void Write(BinaryWriter writer)
            {
                writer.Write((byte)RecordType);
                writer.Write(NullCount);
            }
        }
    }
}
