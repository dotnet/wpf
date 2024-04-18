// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace System.Windows
{
    /// <summary>
    ///  System class information with type info.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/ecb47445-831f-4ef5-9c9b-afd4d06e3657">
    ///    [MS-NRBF] 2.3.2.3
    ///   </see>
    ///  </para>
    /// </remarks>
    internal sealed class SystemClassWithMembersAndTypes : ClassRecord, IRecord<SystemClassWithMembersAndTypes>
    {
        public MemberTypeInfo MemberTypeInfo { get; }

        public SystemClassWithMembersAndTypes(
            ClassInfo classInfo,
            MemberTypeInfo memberTypeInfo,
            IReadOnlyList<object> memberValues)
            : base(classInfo, memberValues)
        {
            MemberTypeInfo = memberTypeInfo;
        }

        public SystemClassWithMembersAndTypes(
            ClassInfo classInfo,
            MemberTypeInfo memberTypeInfo,
            params object[] memberValues)
            : this(classInfo, memberTypeInfo, (IReadOnlyList<object>)memberValues)
        {
        }

        public static RecordType RecordType => RecordType.SystemClassWithMembersAndTypes;

        static SystemClassWithMembersAndTypes IBinaryFormatParseable<SystemClassWithMembersAndTypes>.Parse(
            BinaryReader reader,
            RecordMap recordMap)
        {
            ClassInfo classInfo = ClassInfo.Parse(reader, out Count memberCount);
            MemberTypeInfo memberTypeInfo = MemberTypeInfo.Parse(reader, memberCount);

            SystemClassWithMembersAndTypes record = new(
                classInfo,
                memberTypeInfo,
                ReadValuesFromMemberTypeInfo(reader, recordMap, memberTypeInfo));

            // Index this record by the id of the embedded ClassInfo's object id.
            recordMap[record.ClassInfo.ObjectId] = record;
            return record;
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write((byte)RecordType);
            ClassInfo.Write(writer);
            MemberTypeInfo.Write(writer);
            WriteValuesFromMemberTypeInfo(writer, MemberTypeInfo, MemberValues);
        }
    }
}
