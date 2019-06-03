// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System.IO;

namespace System.Windows.Baml2006
{
    class BamlBinaryReader : BinaryReader
    {
        public BamlBinaryReader(Stream stream)
            : base(stream)
        {
        }

        public new int Read7BitEncodedInt()
        {
            return base.Read7BitEncodedInt();
        }
    }
}
