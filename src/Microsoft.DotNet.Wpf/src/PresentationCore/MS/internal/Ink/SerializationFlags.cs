// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Primary root namespace for TabletPC/Ink/Handwriting/Recognition in .NET

namespace System.Windows.Ink
{
    /// <summary>
    /// Specifies how to persist the ink.
    /// </summary>
    internal enum PersistenceFormat
    {
        /// <summary>
        /// Native ink serialization format.
        /// </summary>
        InkSerializedFormat = 0,

        /// <summary>
        /// GIF serialization format.
        /// </summary>
        Gif = 1,
    }

    /// <summary>
    /// Specifies how to compress the ink.
    /// </summary>
    internal enum CompressionMode
    {
        /// <summary>
        /// Compressed
        /// </summary>
        Compressed = 0,


        /// <summary>
        /// NoCompression
        /// </summary>
        NoCompression = 2
    }
}
