// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Defines InkCanvasEditingMode for InkCanvas
//

using System;

namespace System.Windows.Controls
{    
    /// <summary>
    /// Defines the InkCanvasEditingMode for the InkEditor
    /// </summary>
    public enum InkCanvasEditingMode
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// Ink
        /// </summary>
        Ink,
        /// <summary>
        /// GestureOnly
        /// </summary>
        GestureOnly,
        /// <summary>
        /// InkAndGesture
        /// </summary>
        InkAndGesture,
        /// <summary>
        /// Select
        /// </summary>
        Select,
        /// <summary>
        /// EraseByPoint
        /// </summary>
        EraseByPoint,
        /// <summary>
        /// EraseByStroke
        /// </summary>
        EraseByStroke,
    }

    // NOTICE-2004/10/13-WAYNEZEN,
    // Whenever the InkCanvasEditingMode is modified, please update this EditingModeHelper.IsDefined.
    internal static class EditingModeHelper
    {
        // Helper like Enum.IsDefined,  for InkCanvasEditingMode.
        internal static bool IsDefined(InkCanvasEditingMode InkCanvasEditingMode)
        {
            return (InkCanvasEditingMode >= InkCanvasEditingMode.None && InkCanvasEditingMode <= InkCanvasEditingMode.EraseByStroke);
        }
    }
}
