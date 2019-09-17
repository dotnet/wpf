// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Media;

using System;

namespace System.Windows.Input 
{
    /// <summary>
    ///     An interface for controlling the mouse input provider.
    /// </summary>
    internal interface IMouseInputProvider : IInputProvider
    {
        /// <summary>
        ///     Changes the appearance of the mouse cursor.
        /// </summary>
        /// <param name="cursor">
        ///     The kind of cursor to change to.
        /// </param>
        /// <returns>
        ///     Whether or not cursor was successfully changed.
        /// </returns>
        bool SetCursor(Cursor cursor);
        
        /// <summary>
        ///     Requests that the mouse input be captured.
        /// </summary>
        /// <returns>
        ///     Whether or not the mouse input was successfully captured.
        /// </returns>
        bool CaptureMouse();
        
        /// <summary>
        ///     Releases the mouse capture.
        /// </summary>
        void ReleaseMouseCapture();

        /// <summary>
        /// GetIntermediaePoints
        /// </summary>
        /// <param name="relativeTo">Points will be returned relative to this element</param>
        /// <param name="points">Intermediate Points to return</param>
        /// <returns>Count of points if succeeded, else return -1</returns>
        int GetIntermediatePoints(IInputElement relativeTo, Point[] points);
    }
}


