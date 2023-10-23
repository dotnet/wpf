// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace System.Windows
{
    /// <summary>
    ///  Single dimensional array of strings.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/3d98fd60-d2b4-448a-ac0b-3cd8dea41f9d">
    ///    [MS-NRBF] 2.4.3.4
    ///   </see>
    ///  </para>
    /// </remarks>
    internal sealed class ArraySingleString : ArrayRecord, IRecord<ArraySingleString>
    {
        public static RecordType RecordType => RecordType.ArraySingleString;

        public ArraySingleString(ArrayInfo arrayInfo, IReadOnlyList<object> arrayObjects)
            : base(arrayInfo, arrayObjects)
        { }

        static ArraySingleString IBinaryFormatParseable<ArraySingleString>.Parse(
            BinaryReader reader,
            RecordMap recordMap)
        {
            ArraySingleString record = new(
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
