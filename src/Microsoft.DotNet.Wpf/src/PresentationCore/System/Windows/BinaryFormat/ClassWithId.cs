// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Collections.Generic;
using System.IO;

namespace System.Windows
{
    /// <summary>
    ///  Class information that references another class record's metadata.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/2d168388-37f4-408a-b5e0-e48dbce73e26">
    ///    [MS-NRBF] 2.3.2.5
    ///   </see>
    ///  </para>
    /// </remarks>
    internal sealed class ClassWithId : ClassRecord
    {
        private readonly ClassRecord _metadataClass;

        public override Id ObjectId { get; }

        /// <summary>
        ///  The ObjectId of a prior <see cref="SystemClassWithMembersAndTypes"/>,
        ///  or <see cref="ClassWithMembersAndTypes"/>.
        /// </summary>
        public Id MetadataId { get; }

        public ClassWithId(Id id, ClassRecord metadataClass, IReadOnlyList<object> memberValues)
            : base(metadataClass.ClassInfo, memberValues)
        {
            ObjectId = id;
            MetadataId = metadataClass.ObjectId;
            _metadataClass = metadataClass;
        }

        public static RecordType RecordType => RecordType.ClassWithId;

        public override void Write(BinaryWriter writer)
        {
            writer.Write((byte)RecordType);
            writer.Write(ObjectId);
            writer.Write(MetadataId);

            switch (_metadataClass)
            {
                case ClassWithMembersAndTypes classWithMembersAndTypes:
                    WriteValuesFromMemberTypeInfo(writer, classWithMembersAndTypes.MemberTypeInfo, MemberValues);
                    break;
                case SystemClassWithMembersAndTypes systemClassWithMembersAndTypes:
                    WriteValuesFromMemberTypeInfo(writer, systemClassWithMembersAndTypes.MemberTypeInfo, MemberValues);
                    break;
                default:
                    throw new SerializationException();
            }
        }

        // The following implicit conversion is to facilitate lookup of related records
        // using the correct identifier.

        public static implicit operator Id(ClassWithId value) => value.MetadataId;
    }
}
