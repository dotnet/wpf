// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.IO;

namespace System.Windows
{
    /// <summary>
    ///  String record.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/eb503ca5-e1f6-4271-a7ee-c4ca38d07996">
    ///    [MS-NRBF] 2.5.7
    ///   </see>
    ///  </para>
    /// </remarks>
    internal sealed class BinaryObjectString : IRecord<BinaryObjectString>
    {
        public Id ObjectId { get; }
        public string Value { get; }

        public static RecordType RecordType => RecordType.BinaryObjectString;

        public BinaryObjectString(Id objectId, string value)
        {
            ObjectId = objectId;
            Value = value;
        }

        static BinaryObjectString IBinaryFormatParseable<BinaryObjectString>.Parse(
            BinaryReader reader,
            RecordMap recordMap)
        {
            BinaryObjectString record = new(reader.ReadInt32(), reader.ReadString());

            recordMap[record.ObjectId] = record;
            return record;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)RecordType);
            writer.Write(ObjectId);
            writer.Write(Value);
        }

        public override bool Equals(object? obj)
            => ReferenceEquals(this, obj)
                || (obj is BinaryObjectString bos && bos.ObjectId == ObjectId && bos.Value == Value);

        public override int GetHashCode() => HashCode.Combine(ObjectId, Value);
        public override string ToString() => Value;
    }
}

