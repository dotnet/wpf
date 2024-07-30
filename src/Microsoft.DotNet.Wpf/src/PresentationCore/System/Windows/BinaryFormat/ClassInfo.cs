// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace System.Windows
{
    /// <summary>
    ///  Class info.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/0a192be0-58a1-41d0-8a54-9c91db0ab7bf">
    ///    [MS-NRBF] 2.3.1.1
    ///   </see>
    ///  </para>
    /// </remarks>
    internal class ClassInfo : IBinaryWriteable
    {
        public Id ObjectId { get; }
        public string Name { get; }
        public IReadOnlyList<string> MemberNames { get; }

        public ClassInfo(Id objectId, string name, IReadOnlyList<string> memberNames)
        {
            ObjectId = objectId;
            Name = name;
            MemberNames = memberNames;
        }

        public static ClassInfo Parse(BinaryReader reader, out Count memberCount)
        {
            Id objectId = reader.ReadInt32();
            string name = reader.ReadString();
            memberCount = reader.ReadInt32();
            List<string> memberNames = new(Math.Min(BinaryFormattedObject.MaxNewCollectionSize, memberCount));
            for (int i = 0; i < memberCount; i++)
            {
                memberNames.Add(reader.ReadString());
            }

            return new(objectId, name, memberNames);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(ObjectId);
            writer.Write(Name);
            writer.Write(MemberNames.Count);

            foreach (string name in MemberNames)
            {
                writer.Write(name);
            }
        }
    }
}
