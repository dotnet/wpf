// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Visual representing a PTS page.
//

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // Visual representing a PTS page.
    // ----------------------------------------------------------------------
    internal class PageVisual : DrawingVisual, IContentHost
    {
        // ------------------------------------------------------------------
        // Create a visual representing a PTS page.
        // ------------------------------------------------------------------
        internal PageVisual(FlowDocumentPage owner)
        {
            _owner = new WeakReference(owner);
        }

        // ------------------------------------------------------------------
        // Set information about background that is necessary for rendering
        // process.
        //
        //      backgroundBrush - The background brush used for background.
        //      renderBounds - Render bounds of the visual.
        // ------------------------------------------------------------------
        internal void DrawBackground(Brush backgroundBrush, Rect renderBounds)
        {
            if (_backgroundBrush != backgroundBrush || _renderBounds != renderBounds)
            {
                _backgroundBrush = backgroundBrush;
                _renderBounds = renderBounds;

                // Open DrawingContext and draw background.
                // If background is not set, Open will clean the render data, but it
                // will preserve visual children.
                using (DrawingContext dc = RenderOpen())
                {
                    if (_backgroundBrush != null)
                    {
                        dc.DrawRectangle(_backgroundBrush, null, _renderBounds);
                    }
                    else
                    {
                        dc.DrawRectangle(Brushes.Transparent, null, _renderBounds);
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        // Get/Set visual child.
        // ------------------------------------------------------------------
        internal Visual Child
        {
            get
            {
                VisualCollection vc = this.Children;
                Debug.Assert(vc.Count <= 1);
                return (vc.Count == 0) ? null : vc[0];
            }
            set
            {
                VisualCollection vc = this.Children;
                Debug.Assert(vc.Count <= 1);
                if (vc.Count == 0)
                {
                    vc.Add(value);
                }
                else if (vc[0] != value)
                {
                    vc[0] = value;
                }
                // else Visual child is the same as already stored; do nothing.
            }
        }

        // ------------------------------------------------------------------
        // Clear its DrawingContext
        // Opening and closing a DrawingContext, clears it.
        // ------------------------------------------------------------------
        internal void ClearDrawingContext()
        {
            DrawingContext ctx = this.RenderOpen();
            if(ctx != null)
                ctx.Close();               
        }
        
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
            IContentHost host = _owner.Target as IContentHost;
            if (host != null)
            {
                return host.InputHitTest(point);
            }
            return null;
        }

        /// <summary>
        /// <see cref="IContentHost.GetRectangles"/>
        /// </summary>
        ReadOnlyCollection<Rect> IContentHost.GetRectangles(ContentElement child)
        {
            IContentHost host = _owner.Target as IContentHost;
            if (host != null)
            {
                return host.GetRectangles(child);
            }
            return new ReadOnlyCollection<Rect>(new List<Rect>(0));
        }

        /// <summary>
        /// <see cref="IContentHost.HostedElements"/>
        /// </summary>
        IEnumerator<IInputElement> IContentHost.HostedElements
        {
            get
            {
                IContentHost host = _owner.Target as IContentHost;
                if (host != null)
                {
                    return host.HostedElements;
                }
                return null;
            }
        }

        /// <summary>
        /// <see cref="IContentHost.OnChildDesiredSizeChanged"/>
        /// </summary>
        void IContentHost.OnChildDesiredSizeChanged(UIElement child)
        {
            IContentHost host = _owner.Target as IContentHost;
            if (host != null)
            {
                host.OnChildDesiredSizeChanged(child);
            }
        }

        #endregion IContentHost Members

        // ------------------------------------------------------------------
        // Reference to DocumentPage that owns this visual.
        // ------------------------------------------------------------------
        private readonly WeakReference _owner;

        // ------------------------------------------------------------------
        // Brush used for background rendering.
        // ------------------------------------------------------------------

        private Brush _backgroundBrush;

        // ------------------------------------------------------------------
        // Render bounds.
        // ------------------------------------------------------------------
        private Rect _renderBounds;
    }
}
