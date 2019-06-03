// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose:  Subclass BinaryWriter.
*
\***************************************************************************/

using System;
using System.IO;
using System.Text;

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    internal class BamlBinaryWriter: BinaryWriter
    {
        public BamlBinaryWriter(Stream stream, Encoding code)
            :base(stream, code)
        {
        }

        public new void Write7BitEncodedInt(int value)
        {
            base.Write7BitEncodedInt(value);
        }

        public static int SizeOf7bitEncodedSize(int size)
        {
            const int _7bits = 0x7F;
            const int _14bits = ( _7bits << 7) | _7bits;
            const int _21bits = (_14bits << 7) | _7bits;
            const int _28bits = (_21bits << 7) | _7bits;
            
            if (0 == (size & ~_7bits))
                return 1;
            if (0 == (size & ~_14bits))
                return 2;
            if (0 == (size & ~_21bits))
                return 3;
            if (0 == (size & ~_28bits))
                return 4;
            return 5;
        }
    }
}
