// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Contains the IScrollInfo interface.
//              Spec:  Virtualization in Layout.doc
//

using MS.Internal;
using MS.Utility;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Media;
using System;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// An IScrollInfo serves as the main scrolling region inside a <see cref="System.Windows.Controls.ScrollViewer" />
    /// or derived class.  It exposes scrolling properties, methods for logical scrolling, computing
    /// which children are visible, and measuring/drawing/offsetting/clipping content.    
    /// </summary>
    public interface IScrollInfo
    {
        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Scroll content by one line to the top.
        /// </summary>
        void LineUp();

        /// <summary>
        /// Scroll content by one line to the bottom.
        /// </summary>
        void LineDown();

        /// <summary>
        /// Scroll content by one line to the left.
        /// </summary>
        void LineLeft();

        /// <summary>
        /// Scroll content by one line to the right.
        /// </summary>
        void LineRight();


        /// <summary>
        /// Scroll content by one page to the top.
        /// </summary>
        void PageUp();

        /// <summary>
        /// Scroll content by one page to the bottom.
        /// </summary>
        void PageDown();

        /// <summary>
        /// Scroll content by one page to the left.
        /// </summary>
        void PageLeft();

        /// <summary>
        /// Scroll content by one page to the right.
        /// </summary>
        void PageRight();


        /// <summary>
        /// Scroll content by one page to the top.
        /// </summary>
        void MouseWheelUp();

        /// <summary>
        /// Scroll content by one page to the bottom.
        /// </summary>
        void MouseWheelDown();

        /// <summary>
        /// Scroll content by one page to the left.
        /// </summary>
        void MouseWheelLeft();

        /// <summary>
        /// Scroll content by one page to the right.
        /// </summary>
        void MouseWheelRight();

        /// <summary>
        /// Set the HorizontalOffset to the passed value.  
        /// An implementation may coerce this value into a valid range, typically inclusively between 0 and <see cref="ExtentWidth" /> less <see cref="ViewportWidth" />.
        /// </summary>
        void SetHorizontalOffset(double offset);

        /// <summary>
        /// Set the VerticalOffset to the passed value.  
        /// An implementation may coerce this value into a valid range, typically inclusively between 0 and <see cref="ExtentHeight" /> less <see cref="ViewportHeight" />.
        /// </summary>
        void SetVerticalOffset(double offset);

        /// <summary>
        /// This scrolls to make the rectangle in the Visual's coordinate space visible.
        /// </summary>
        /// <param name="visual">The Visual that should become visible</param>
        /// <param name="rectangle">A rectangle representing in the visual's coordinate space to make visible.</param>
        /// <returns>
        /// A rectangle in the IScrollInfo's coordinate space that has been made visible.  
        /// Other ancestors to in turn make this new rectangle visible.
        /// The rectangle should generally be a transformed version of the input rectangle.  In some cases, like
        /// when the input rectangle cannot entirely fit in the viewport, the return value might be smaller.
        /// </returns>
        Rect MakeVisible(Visual visual, Rect rectangle);

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// This property indicates to the IScrollInfo whether or not it can scroll in the vertical given dimension.
        /// </summary>
        bool CanVerticallyScroll { get; set; }

        /// <summary>
        /// This property indicates to the IScrollInfo whether or not it can scroll in the horizontal given dimension.
        /// </summary>
        bool CanHorizontallyScroll { get; set; }

        /// <summary>
        /// ExtentWidth contains the full horizontal range of the scrolled content.
        /// </summary>
        double ExtentWidth { get; }

        /// <summary>
        /// ExtentHeight contains the full vertical range of the scrolled content.
        /// </summary>
        double ExtentHeight { get; }

        /// <summary>
        /// ViewportWidth contains the currently visible horizontal range of the scrolled content.
        /// </summary>
        double ViewportWidth { get; }

        /// <summary>
        /// ViewportHeight contains the currently visible vertical range of the scrolled content.
        /// </summary>
        double ViewportHeight { get; }

        /// <summary>
        /// HorizontalOffset is the horizontal offset into the scrolled content that represents the first unit visible.
        /// </summary>
        double HorizontalOffset { get; }

        /// <summary>
        /// VerticalOffset is the vertical offset into the scrolled content that represents the first unit visible.
        /// </summary>
        double VerticalOffset { get; }

        /// <summary>
        /// ScrollOwner is the container that controls any scrollbars, headers, etc... that are dependant
        /// on this IScrollInfo's properties.  Implementers of IScrollInfo should call InvalidateScrollInfo()
        /// on this object when properties change.
        /// </summary>
        ScrollViewer ScrollOwner { get; set; }

        #endregion
    }
}
