// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace System.Windows
{
    /// <summary>
    ///  Class information with the source library.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/ebbdad88-91fe-48ae-a985-661f9cc7e0de">
    ///    [MS-NRBF] 2.3.2.2
    ///   </see>
    ///  </para>
    /// </remarks>
    internal sealed class ClassWithMembers : ClassRecord, IRecord<ClassWithMembers>
    {
        public Id LibraryId { get; }

        public ClassWithMembers(ClassInfo classInfo, Id libraryId, IReadOnlyList<object> memberValues)
            : base(classInfo, memberValues)
        {
            LibraryId = libraryId;
        }

        public static RecordType RecordType => RecordType.ClassWithMembers;

        static ClassWithMembers IBinaryFormatParseable<ClassWithMembers>.Parse(
            BinaryReader reader,
            RecordMap recordMap)
        {
            ClassInfo classInfo = ClassInfo.Parse(reader, out _);
            ClassWithMembers record = new(
                classInfo,
                reader.ReadInt32(),
                ReadDataFromClassInfo(reader, recordMap, classInfo));

            // Index this record by the id of the embedded ClassInfo's object id.
            recordMap[classInfo.ObjectId] = record;
            return record;
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write((byte)RecordType);
            ClassInfo.Write(writer);
            writer.Write(LibraryId);
        }
    }
}
