// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace System.Windows
{
    /// <summary>
    ///  Single dimensional array of objects.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/982b2f50-6367-402a-aaf2-44ee96e2a5e0">
    ///    [MS-NRBF] 2.4.3.2
    ///   </see>
    ///  </para>
    /// </remarks>
    internal sealed class ArraySingleObject : ArrayRecord, IRecord<ArraySingleObject>
    {
        public static RecordType RecordType => RecordType.ArraySingleObject;

        public ArraySingleObject(ArrayInfo arrayInfo, IReadOnlyList<object> arrayObjects)
            : base(arrayInfo, arrayObjects)
        { }

        static ArraySingleObject IBinaryFormatParseable<ArraySingleObject>.Parse(
            BinaryReader reader,
            RecordMap recordMap)
        {
            ArraySingleObject record = new(
                ArrayInfo.Parse(reader, out Count length),
                ReadRecords(reader, recordMap, length));

            recordMap[record.ObjectId] = record;
            return record;
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write((byte)RecordType);
            ArrayInfo.Write(writer);
            WriteRecords(writer, ArrayObjects);
        }
    }
}
