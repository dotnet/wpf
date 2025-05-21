// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Windows
{
    /// <summary>
    ///  Binary format header.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/a7e578d3-400a-4249-9424-7529d10d1b3c">
    ///    [MS-NRBF] 2.6.1
    ///   </see>
    ///  </para>
    /// </remarks>
    internal sealed class SerializationHeader : IRecord
    {
        /// <summary>
        ///  The id of the root object record.
        /// </summary>
        public Id RootId;

        /// <summary>
        ///  Ignored. BinaryFormatter puts out -1.
        /// </summary>
        public int HeaderId;

        /// <summary>
        ///  Must be 1.
        /// </summary>
        public Id MajorVersion;

        /// <summary>
        ///  Must be 0.
        /// </summary>
        public Id MinorVersion;

        public static RecordType RecordType => RecordType.SerializedStreamHeader;

        public static SerializationHeader Default => new()
        {
            MajorVersion = 1,
            RootId = 1,
            HeaderId = -1,
        };

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)RecordType);
            writer.Write(RootId);
            writer.Write(HeaderId);
            writer.Write(MajorVersion);
            writer.Write(MinorVersion);
        }
    }
}
