// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Transform pattern provider interface

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Expose an element's ability to change its on-screen position, size or orientation
    /// </summary>
    [ComVisible(true)]
    [Guid("6829ddc4-4f91-4ffa-b86f-bd3e2987cb4c")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface ITransformProvider
#else
    public interface ITransformProvider
#endif
    {
        /// <summary>
        /// Used to adjust an element's current location. The x, and y parameters represent the  
        /// absolute on-screen position of the top-left corner in pixels, not the delta between the 
        /// desired location and the window's current location. 
        /// </summary>
        /// 
        /// <param name="x">absolute on-screen position of the top left corner</param>
        /// <param name="y">absolute on-screen position of the top left corner</param>
        void Move( double x, double y );

        /// <summary>
        /// Used to modify element's on-screen dimensions (affects the 
        /// BoundingRectangle and BoundingGeometry properties)
        /// When called on a split pane, it may have the side-effect of resizing
        /// other surrounding panes.
        /// </summary>
        /// <param name="width">The requested width of the window.</param>
        /// <param name="height">The requested height of the window.</param>
        void Resize( double width, double height );

        /// <summary>
        /// Rotate the element the specified number of degrees.
        /// </summary>
        /// <param name="degrees">The requested degrees to rotate the element.  A positive number rotates clockwise
        /// a negative number rotates counter clockwise</param>
        void Rotate( double degrees );

        /// <summary>Returns true if the element can be moved otherwise returns false.</summary>
        bool CanMove 
        {
            [return: MarshalAs(UnmanagedType.Bool)] // Without this, only lower SHORT of BOOL*pRetVal param is updated.
            get;
        }
        
        /// <summary>Returns true if the element can be resized otherwise returns false.</summary>
        bool CanResize
        {
            [return: MarshalAs(UnmanagedType.Bool)] // Without this, only lower SHORT of BOOL*pRetVal param is updated.
            get;
        }
        
        /// <summary>Returns true if the element can be rotated otherwise returns false.</summary>
        bool CanRotate
        {
            [return: MarshalAs(UnmanagedType.Bool)] // Without this, only lower SHORT of BOOL*pRetVal param is updated.
            get;
        }
    }
}
