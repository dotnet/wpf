// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Visual representing line of text. 
//


using System;
using System.Windows.Media;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // Visual representing line of text.
    // ----------------------------------------------------------------------
    internal sealed class LineVisual : DrawingVisual
    {
        // ------------------------------------------------------------------
        // Open drawing context.
        // ------------------------------------------------------------------
        internal DrawingContext Open()
        {
            return RenderOpen();
        }
        
        internal double WidthIncludingTrailingWhitespace;
    }
}
