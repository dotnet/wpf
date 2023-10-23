// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.IO;

namespace System.Windows
{
    internal readonly ref struct BinaryFormatWriterScope
    {
        private readonly BinaryWriter _writer;

        public BinaryFormatWriterScope(Stream stream)
        {
            _writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            SerializationHeader.Default.Write(_writer);
        }

        public static implicit operator BinaryWriter(in BinaryFormatWriterScope scope) => scope._writer;

        public void Dispose()
        {
            MessageEnd.Instance.Write(_writer);
            _writer.Dispose();
        }
    }
}

