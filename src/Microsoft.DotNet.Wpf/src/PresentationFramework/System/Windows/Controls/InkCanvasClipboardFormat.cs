// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Defines an Enum type which is used by InkCanvas' Clipboard supports.
//
// Features:
//

namespace System.Windows.Controls
{
    /// <summary>
    /// InkCanvasClipboardFormat
    /// </summary>
    public enum InkCanvasClipboardFormat
    {
        /// <summary>
        /// Ink Serialized Format
        /// </summary>
        InkSerializedFormat = 0,
        /// <summary>
        /// Text Format
        /// </summary>
        Text,
        /// <summary>
        /// Xaml Format
        /// </summary>
        Xaml,
    }
}
