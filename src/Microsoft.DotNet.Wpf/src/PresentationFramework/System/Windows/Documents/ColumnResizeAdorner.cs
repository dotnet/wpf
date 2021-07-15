// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
//
// Description: Adorner for column resize.
// 

using System;
using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MS.Win32;
using MS.Internal;

namespace System.Windows.Documents.Internal
{
    internal class ColumnResizeAdorner : Adorner
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        
        /// <summary>
        /// C'tor for adorner
        /// </summary>
        /// <param name="scope">
        /// FramwerokElement with TextView to which this element is attached
        /// as adorner.
        /// </param>
        internal ColumnResizeAdorner(UIElement scope) : base(scope)
        {
            Debug.Assert(scope != null);

            // position
            _pen = new Pen(new SolidColorBrush(Colors.LightSlateGray), 2.0);

            _x = Double.NaN;
            _top = Double.NaN;
            _height = Double.NaN;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        /// Add a transform so that the adorner is in right spot.
        /// </summary>
        /// <param name="transform">
        /// The transform applied to the object the adorner adorns
        /// </param>
        /// <returns>
        /// Transform to apply to the adorner
        /// </returns>
        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            GeneralTransformGroup group;            
            TranslateTransform translation;

            group = new GeneralTransformGroup();            
            translation = new TranslateTransform(_x, _top);

            group.Children.Add(translation);

            if (transform != null)
            {
                group.Children.Add(transform);
            }

            return group;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
 
        #region Protected Methods


        protected override void OnRender(DrawingContext drawingContext)
        {
            // Render as a 2 pixel wide rect, one pixel in each bordering char bounding box.

            drawingContext.DrawLine(_pen, new Point(0, 0), 
                                          new Point(0, _height));
        }

        #endregion Protected Events

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Updates position for adorner.
        /// </summary>
        /// <param name="newX">
        /// </param>
        internal void Update(double newX)
        {
            if(_x != newX)
            {
                _x = newX;
                AdornerLayer adornerLayer;

                adornerLayer = VisualTreeHelper.GetParent(this) as AdornerLayer;
                if (adornerLayer != null)
                {
                    // It may be null when TextBox is detached from a tree
                    adornerLayer.Update(AdornedElement);
                    adornerLayer.InvalidateVisual();
                }
            }
        }

        internal void Initialize(UIElement renderScope, double xPos, double yPos, double height)
        {
            Debug.Assert(_adornerLayer == null, "Attempt to overwrite existing AdornerLayer!");

            _adornerLayer = AdornerLayer.GetAdornerLayer(renderScope);

            if (_adornerLayer != null)
            {
                _adornerLayer.Add(this);
            }

            _x = xPos;
            _top = yPos;
            _height = height;
        }

        internal void Uninitialize()
        {
            if (_adornerLayer != null)
            {
                _adornerLayer.Remove(this);
                _adornerLayer = null;
            }
        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // x position
        private double _x;

        // top position
        private double _top;

        // height
        private double _height;

        private Pen _pen;

        // Cached adornerlayer
        private AdornerLayer _adornerLayer;

        #endregion Private Fields
    }
}


