// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***************************************************************************\
*
*
* Purpose:  Subclass BinaryReader.
*
*
\***************************************************************************/
using System.IO;
using System.Text;

namespace System.Windows.Markup
{
    internal class BamlBinaryReader: BinaryReader
    {
        public BamlBinaryReader(Stream stream, Encoding code)
            :base(stream, code)
        {
        }

        public new int Read7BitEncodedInt()
        {
            return base.Read7BitEncodedInt();
        }
    }
}
