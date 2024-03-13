// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Collections.Generic;
using System.IO;

namespace System.Windows
{
    /// <summary>
    ///  Array of objects.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/9c62c928-db4e-43ca-aeba-146256ef67c2">
    ///    [MS-NRBF] 2.4.3.1
    ///   </see>
    ///  </para>
    /// </remarks>
    internal sealed class BinaryArray : ArrayRecord, IRecord<BinaryArray>
    {
        public Count Rank { get; }
        public BinaryArrayType Type { get; }
        public MemberTypeInfo TypeInfo { get; }

        private BinaryArray(
            Count rank,
            BinaryArrayType type,
            ArrayInfo arrayInfo,
            MemberTypeInfo typeInfo,
            IReadOnlyList<object> arrayObjects)
            : base(arrayInfo, arrayObjects)
        {
            Rank = rank;
            Type = type;
            TypeInfo = typeInfo;
        }

        public static RecordType RecordType => RecordType.BinaryArray;

        static BinaryArray IBinaryFormatParseable<BinaryArray>.Parse(BinaryReader reader, RecordMap recordMap)
        {
            Id objectId = reader.ReadInt32();
            BinaryArrayType arrayType = (BinaryArrayType)reader.ReadByte();
            Count rank = reader.ReadInt32();
            Count length = reader.ReadInt32();

            if (arrayType != BinaryArrayType.Single || rank != 1)
            {
                throw new NotSupportedException("Only single dimensional arrays are currently supported.");
            }

            MemberTypeInfo memberTypeInfo = MemberTypeInfo.Parse(reader, 1);
            List<object> arrayObjects = new(Math.Min(BinaryFormattedObject.MaxNewCollectionSize, length));
            (BinaryType type, object? typeInfo) = memberTypeInfo[0];
            for (int i = 0; i < length; i++)
            {
                arrayObjects.Add(ReadValue(reader, recordMap, type, typeInfo));
            }

            return new(rank, arrayType, new ArrayInfo(objectId, length), memberTypeInfo, arrayObjects);
        }

        public override void Write(BinaryWriter writer)
        {
            throw new NotSupportedException();
        }
    }
}
