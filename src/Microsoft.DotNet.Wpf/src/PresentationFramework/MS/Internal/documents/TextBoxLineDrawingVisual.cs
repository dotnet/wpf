// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Extension of DrawingVisual for state that TextBoxView needs.
//

using System;
using System.Windows;
using System.Windows.Controls;
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
