// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Caret scroll-into-view enum.
// 

namespace System.Windows.Documents
{
    // Caret scroll-into-view enum.
    internal enum CaretScrollMethod
    {
        // No value.
        Unset = 0,

        // Scroll the caret rect into view with no extra hueristics.
        Simple,
        
        // Scroll the caret rect into view after a navigation,
        // offset such that a porportion of the viewport is scrolled
        // into view as well.
        Navigation,

        // Do not scroll the caret into view.
        None,
    }
}
