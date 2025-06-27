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
    /// Extends the <see cref="ITransformProvider"/> interface to enable Microsoft UI Automation providers to expose API to support the viewport zooming functionality of a control.
    /// </summary>
    [ComVisible(true)]
    [Guid("4758742f-7ac2-460c-bc48-09fc09308a93")]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface ITransformProvider2: ITransformProvider
#else
    public interface ITransformProvider2 : ITransformProvider
#endif
    {
        /// <summary>
        /// Zooms the viewport of the control.
        /// </summary>
        /// 
        /// <param name="zoom">The amount to zoom the viewport, specified as a percentage. The provider should zoom the viewport to the nearest supported value.</param>
        void Zoom( double zoom );

        /// <summary>
        /// Zooms the viewport of the control by the specified logical unit.
        /// </summary>
        /// <param name="zoomUnit">The logical unit by which to increase or decrease the zoom of the viewport.</param>
        void ZoomByUnit( ZoomUnit zoomUnit );

        /// <summary>Gets a value that indicates whether the control supports zooming of its viewport.</summary>
        /// <value><c>true</c> if the viewport can be zoomed; otherwise, <c>false</c>.</value>
        bool CanZoom
        {
            [return: MarshalAs(UnmanagedType.Bool)] // Without this, only lower SHORT of BOOL*pRetVal param is updated.
            get;
        }

        /// <summary>Gets the zoom level of the control's viewport.</summary>
        /// <value>The zoom level, specified as a percentage. The provider should zoom the viewport to the nearest supported value.</remarks>
        double ZoomLevel
        {
            get;
        }

        /// <summary>Gets the minimum zoom level of the element.</summary>
        /// <value>The minimum zoom level, as a percentage.</remarks>
        double ZoomMinimum
        {
            get;
        }

        /// <summary>Gets the maximum zoom level of the element.</summary>
        /// <value>The maximum zoom level, as a percentage.</value>
        double ZoomMaximum
        {
            get;
        }
    }
}
