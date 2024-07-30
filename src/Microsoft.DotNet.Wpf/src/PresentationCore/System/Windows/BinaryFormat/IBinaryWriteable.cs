// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Windows
{
    /// <summary>
    ///  Expresses that the object can be written with a <see cref="BinaryWriter"/>
    /// </summary>
    internal interface IBinaryWriteable
    {
        /// <summary>
        ///  Writes the current object to the given <paramref name="writer"/>.
        /// </summary>
        void Write(BinaryWriter writer);
    }
}

