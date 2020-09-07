// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;

namespace System.Windows.Controls
{
    /// <summary>
    /// This delegate is used by handlers of the ScrollChangedEvent event.
    /// </summary>
    /// <param name="sender">The current element along the event's route.</param>
    /// <param name="e">The event arguments containing additional information about the event.</param>
    /// <returns>Nothing.</returns>
    public delegate void ScrollChangedEventHandler(object sender, ScrollChangedEventArgs e);

    /// <summary>
    /// The ScrollChangedEventsArgs describe a change in scrolling state.
    /// </summary>
    public class ScrollChangedEventArgs: RoutedEventArgs
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        internal ScrollChangedEventArgs(Vector offset, Vector offsetChange, Size extent, Vector extentChange, Size viewport, Vector viewportChange)
        {
            _offset = offset;
            _offsetChange = offsetChange;
            _extent = extent;
            _extentChange = extentChange;
            _viewport = viewport;
            _viewportChange = viewportChange;
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Properties (CLR + Avalon)
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Updated HorizontalPosition of the scrolled content
        /// <seealso cref="System.Windows.Controls.ScrollViewer.HorizontalOffset"/>
        /// </summary>
        public double HorizontalOffset
        { 
            get { return _offset.X; } 
        }
        /// <summary>
        /// Updated VerticalPosition of the scrolled content
        /// <seealso cref="System.Windows.Controls.ScrollViewer.VerticalOffset"/>
        /// </summary>
        public double VerticalOffset
        {
            get { return _offset.Y; }
        }
        /// <summary>
        /// Change in horizontal offset of the scrolled content
        /// <seealso cref="System.Windows.Controls.ScrollViewer.HorizontalOffset"/>
        /// </summary>
        public double HorizontalChange
        {
            get { return _offsetChange.X; }
        }
        /// <summary>
        /// Change in vertical offset of the scrolled content
        /// <seealso cref="System.Windows.Controls.ScrollViewer.VerticalOffset"/>
        /// </summary>
        public double VerticalChange
        {
            get { return _offsetChange.Y; }
        }

        /// <summary>
        /// Updated horizontal size of the viewing window of the ScrollViewer
        /// <seealso cref="System.Windows.Controls.ScrollViewer.ViewportWidth"/>
        /// </summary>
        public double ViewportWidth
        {
            get { return _viewport.Width; }
        }
        /// <summary>
        /// Updated vertical size of the viewing window of the ScrollViewer.
        /// <seealso cref="System.Windows.Controls.ScrollViewer.ViewportHeight"/>
        /// </summary>
        public double ViewportHeight
        {
            get { return _viewport.Height; }
        }
        /// <summary>
        /// Change in the horizontal size of the viewing window of the ScrollViewer
        /// <seealso cref="System.Windows.Controls.ScrollViewer.ViewportWidth"/>
        /// </summary>
        public double ViewportWidthChange
        {
            get { return _viewportChange.X; }
        }
        /// <summary>
        /// Change in the vertical size of the viewing window of the ScrollViewer.
        /// <seealso cref="System.Windows.Controls.ScrollViewer.ViewportHeight"/>
        /// </summary>
        public double ViewportHeightChange
        {
            get { return _viewportChange.Y; }
        }

        /// <summary>
        /// Updated horizontal size of the scrollable content.
        /// <seealso cref="System.Windows.Controls.ScrollViewer.ExtentWidth"/>
        /// </summary>
        public double ExtentWidth
        {
            get { return _extent.Width; }
        }
        /// <summary>
        /// Updated vertical size of the scrollable content.
        /// <seealso cref="System.Windows.Controls.ScrollViewer.ExtentHeight"/>
        /// </summary>
        public double ExtentHeight
        {
            get { return _extent.Height; }
        }
        /// <summary>
        /// Change in the horizontal size of the scrollable content.
        /// <seealso cref="System.Windows.Controls.ScrollViewer.ExtentWidth"/>
        /// </summary>
        public double ExtentWidthChange
        {
            get { return _extentChange.X; }
        }
        /// <summary>
        /// Change in the vertical size of the scrollable content.
        /// <seealso cref="System.Windows.Controls.ScrollViewer.ExtentHeight"/>
        /// </summary>
        public double ExtentHeightChange
        {
            get { return _extentChange.Y; }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Protected Methods
            //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// This method is used to perform the proper type casting in order to
        /// call the type-safe ScrollChangedEventHandler delegate for the ScrollChangedEvent event.
        /// </summary>
        /// <param name="genericHandler">The handler to invoke.</param>
        /// <param name="genericTarget">The current object along the event's route.</param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            ScrollChangedEventHandler handler = (ScrollChangedEventHandler)genericHandler;
            handler(genericTarget, this);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields
        
        // Current scroll data
        private Vector _offset;
        private Vector _offsetChange;
        private Size   _extent;
        private Vector _extentChange;
        private Size   _viewport;
        private Vector _viewportChange;

        #endregion
    }
}
