// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace System.Windows
{
    /// <summary>
    ///  System class information.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/f5bd730f-d944-42ab-b6b3-013099559a4b">
    ///    [MS-NRBF] 2.3.2.4
    ///   </see>
    ///  </para>
    /// </remarks>
    internal sealed class SystemClassWithMembers : ClassRecord, IRecord<SystemClassWithMembers>
    {
        public SystemClassWithMembers(ClassInfo classInfo, IReadOnlyList<object> memberValues)
            : base(classInfo, memberValues) { }

        public static RecordType RecordType => RecordType.SystemClassWithMembers;

        static SystemClassWithMembers IBinaryFormatParseable<SystemClassWithMembers>.Parse(
            BinaryReader reader,
            RecordMap recordMap)
        {
            ClassInfo classInfo = ClassInfo.Parse(reader, out _);
            SystemClassWithMembers record = new(
                classInfo,
                ReadDataFromClassInfo(reader, recordMap, classInfo));

            // Index this record by the id of the embedded ClassInfo's object id.
            recordMap[record.ClassInfo.ObjectId] = record;
            return record;
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write((byte)RecordType);
            ClassInfo.Write(writer);
        }
    }
}
