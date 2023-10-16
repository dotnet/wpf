// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.IO;

namespace System.Windows
{
    /// <summary>
    ///  Null object record.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/fe51522c-23d1-48dd-9913-c84894abc127">
    ///    [MS-NRBF] 2.5.4
    ///   </see>
    ///  </para>
    /// </remarks>
    internal sealed class ObjectNull : NullRecord, IRecord<ObjectNull>
    {
        public static ObjectNull Instance { get; } = new();

        private ObjectNull() { }

        public override Count NullCount => Count.One;

        public static RecordType RecordType => RecordType.ObjectNull;

        static ObjectNull IBinaryFormatParseable<ObjectNull>.Parse(
            BinaryReader reader,
            RecordMap recordMap) => Instance;

        public void Write(BinaryWriter writer) => writer.Write((byte)RecordType);

        public override bool Equals(object? obj) => obj is ObjectNull;

        public override int GetHashCode() => Instance.GetHashCode();
    }
}
