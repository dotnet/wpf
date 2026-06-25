// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description: Extension of DrawingVisual for state that TextBoxView needs.
//

using System.Windows.Media;

namespace System.Windows.Controls
{
    /// <summary>
    /// Extension of DrawingVisual for state that TextBoxView needs.
    /// </summary>
    internal class TextBoxLineDrawingVisual : DrawingVisual
    {
        /// <summary>
        /// Whether this line visual should be removed from the visual tree on Arrange.
        /// </summary>
        internal bool DiscardOnArrange { get; set; }
    }
}
