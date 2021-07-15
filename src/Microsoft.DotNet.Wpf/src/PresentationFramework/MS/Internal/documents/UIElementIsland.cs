// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: UIElement layout island.
//

using System;
using System.Collections.Generic;       // List<T>
using System.Collections.ObjectModel;   // ReadOnlyCollection<T>
using System.Windows;                   // UIElement
using System.Windows.Media;             // Visual

namespace MS.Internal.Documents
{
    /// <summary> 
    /// UIElement layout island.
    /// </summary>
    internal class UIElementIsland : ContainerVisual, IContentHost, IDisposable
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary> 
        /// Create an instance of a UIElementIsland.
        /// </summary>
        internal UIElementIsland(UIElement child)
        {
            SetFlags(true, VisualFlags.IsLayoutIslandRoot);
            _child = child;

            if (_child != null)
            {
                // Disconnect visual from its old parent, if necessary.
                Visual currentParent = VisualTreeHelper.GetParent(_child) as Visual;
                if (currentParent != null)
                {
                    Invariant.Assert(currentParent is UIElementIsland, "Parent should always be a UIElementIsland.");
                    ((UIElementIsland)currentParent).Dispose();
                }

                // Notify the Visual layer that a new child appeared.
                Children.Add(_child);
            }
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Do layout of entire UIElement Island.
        /// </summary>
        /// <param name="availableSize">Avalilable slot size of the UIElement Island.</param>
        /// <param name="horizontalAutoSize">Whether horizontal autosizing is enabled.</param>
        /// <param name="verticalAutoSize">Whether vertical autosizing is enabled.</param>
        /// <returns>The size of the UIElement Island.</returns>
        internal Size DoLayout(Size availableSize, bool horizontalAutoSize, bool verticalAutoSize)
        {
            Size islandSize = new Size();
            if (_child != null)
            {
                // Get FlowDirection from logical parent and set it on UIElemntIsland
                // to get layout mirroring provided by the layout system.
                if (_child is FrameworkElement && ((FrameworkElement)_child).Parent != null)
                {
                    SetValue(FrameworkElement.FlowDirectionProperty, ((FrameworkElement)_child).Parent.GetValue(FrameworkElement.FlowDirectionProperty));
                }

                try
                {
                    _layoutInProgress = true;

                    // Measure UIElement
                    _child.Measure(availableSize);

                    // Arrange UIElement
                    islandSize.Width = horizontalAutoSize ? _child.DesiredSize.Width : availableSize.Width;
                    islandSize.Height = verticalAutoSize ? _child.DesiredSize.Height : availableSize.Height;
                    _child.Arrange(new Rect(islandSize));
                }
                finally
                {
                    _layoutInProgress = false;
                }
            }
            return islandSize;
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Root of UIElement island.
        /// </summary>
        internal UIElement Root
        {
            get
            {
                return _child;
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Internal Events
        //
        //-------------------------------------------------------------------

        #region Internal Events

        /// <summary>
        /// Fired after DesiredSize for child UIElement has been changed.
        /// </summary>
        internal event DesiredSizeChangedEventHandler DesiredSizeChanged;

        #endregion Internal Events

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private UIElement _child;       // Hosted UIElement root.
        private bool _layoutInProgress; // Whether layout is in progress.

        #endregion Private Fields

        //-------------------------------------------------------------------
        //
        //  IDisposable Members
        //
        //-------------------------------------------------------------------

        #region IDisposable Members

        /// <summary>
        /// <see cref="IDisposable.Dispose"/>
        /// </summary>
        public void Dispose()
        {
            if (_child != null)
            {
                Children.Clear();
                _child = null;
            }
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Members

        //-------------------------------------------------------------------
        //
        //  IContentHost Members
        //
        //-------------------------------------------------------------------

        #region IContentHost Members

        /// <summary>
        /// <see cref="IContentHost.InputHitTest"/>
        /// </summary>
        IInputElement IContentHost.InputHitTest(Point point)
        {
            // UIElementIsland only hosts UIElements, which can be found by the
            // normal hit-testing logic, so we don't need to provide our own
            // hit-testing implementation.
            return null;
        }

        /// <summary>
        /// <see cref="IContentHost.GetRectangles"/>
        /// </summary>
        ReadOnlyCollection<Rect> IContentHost.GetRectangles(ContentElement child)
        {
            return new ReadOnlyCollection<Rect>(new List<Rect>());
        }

        /// <summary>
        /// <see cref="IContentHost.HostedElements"/>
        /// </summary>
        IEnumerator<IInputElement> IContentHost.HostedElements
        {
            get
            {
                List<IInputElement> hostedElements = new List<IInputElement>();
                if (_child != null)
                {
                    hostedElements.Add(_child);
                }
                return hostedElements.GetEnumerator();
            }
        }

        /// <summary>
        /// <see cref="IContentHost.OnChildDesiredSizeChanged"/>
        /// </summary>
        void IContentHost.OnChildDesiredSizeChanged(UIElement child)
        {
            Invariant.Assert(child == _child);
            if (!_layoutInProgress && DesiredSizeChanged != null)
            {
                DesiredSizeChanged(this, new DesiredSizeChangedEventArgs(child));
            }
        }

        #endregion IContentHost Members
    }

    /// <summary>
    /// DesiredSizeChanged event handler.
    /// </summary>
    internal delegate void DesiredSizeChangedEventHandler(object sender, DesiredSizeChangedEventArgs e);

    /// <summary>
    /// Event arguments for the DesiredSizeChanged event.
    /// </summary>
    internal class DesiredSizeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="child">UIElement for which DesiredSize has been changed.</param>
        internal DesiredSizeChangedEventArgs(UIElement child)
        {
            _child = child;
        }

        /// <summary>
        /// UIElement for which DesiredSize has been changed.
        /// </summary>
        internal UIElement Child
        {
            get { return _child; }
        }

        /// <summary>
        /// UIElement for which DesiredSize has been changed.
        /// </summary>
        private readonly UIElement _child;
    }
}
