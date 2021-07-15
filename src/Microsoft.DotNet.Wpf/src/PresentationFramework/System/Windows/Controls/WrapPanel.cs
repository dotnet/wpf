// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Contains the WrapPanel class.
//              Spec at WrapPanel.xml
//

using MS.Internal;
using MS.Internal.Telemetry.PresentationFramework;
using MS.Utility;
using System.ComponentModel;

using System.Diagnostics;
using System.Reflection;
using System.Windows.Threading;

using System.Windows.Media;


using System;

namespace System.Windows.Controls
{
    /// <summary>
    /// WrapPanel is used to place child UIElements at sequential positions from left to the right
    /// and then "wrap" the lines of children from top to the bottom.
    /// 
    /// All children get the layout partition of size ItemWidth x ItemHeight.
    /// 
    /// </summary>
    public class WrapPanel : Panel
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors
        static WrapPanel()
        {
            ControlsTraceLogger.AddControl(TelemetryControls.WrapPanel);
        }

        /// <summary>
        ///     Default constructor
        /// </summary>
        public WrapPanel() : base()
        {
            _orientation = (Orientation) OrientationProperty.GetDefaultValue(DependencyObjectType);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods


        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties + Dependency Properties's
        //
        //-------------------------------------------------------------------
      
        #region Public Properties

        private static bool IsWidthHeightValid(object value)
        {
            double v = (double)value;
            return (DoubleUtil.IsNaN(v)) || (v >= 0.0d && !Double.IsPositiveInfinity(v));
        }

        /// <summary>
        /// DependencyProperty for <see cref="ItemWidth" /> property.
        /// </summary>
        public static readonly DependencyProperty ItemWidthProperty =
                DependencyProperty.Register(
                        "ItemWidth", 
                        typeof(double), 
                        typeof(WrapPanel),
                        new FrameworkPropertyMetadata(
                                Double.NaN, 
                                FrameworkPropertyMetadataOptions.AffectsMeasure),
                        new ValidateValueCallback(IsWidthHeightValid));

        /// <summary>
        /// The ItemWidth and ItemHeight properties specify the size of all items in the WrapPanel. 
        /// Note that children of 
        /// WrapPanel may have their own Width/Height properties set - the ItemWidth/ItemHeight 
        /// specifies the size of "layout partition" reserved by WrapPanel for the child.
        /// If this property is not set (or set to "Auto" in markup or Double.NaN in code) - the size of layout
        /// partition is equal to DesiredSize of the child element.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double ItemWidth
        {
            get { return (double) GetValue(ItemWidthProperty); }
            set { SetValue(ItemWidthProperty, value); }
        }


        /// <summary>
        /// DependencyProperty for <see cref="ItemHeight" /> property.
        /// </summary>
        public static readonly DependencyProperty ItemHeightProperty =
                DependencyProperty.Register(
                        "ItemHeight", 
                        typeof(double), 
                        typeof(WrapPanel),
                        new FrameworkPropertyMetadata(
                                Double.NaN, 
                                FrameworkPropertyMetadataOptions.AffectsMeasure),
                        new ValidateValueCallback(IsWidthHeightValid));


        /// <summary>
        /// The ItemWidth and ItemHeight properties specify the size of all items in the WrapPanel. 
        /// Note that children of 
        /// WrapPanel may have their own Width/Height properties set - the ItemWidth/ItemHeight 
        /// specifies the size of "layout partition" reserved by WrapPanel for the child.
        /// If this property is not set (or set to "Auto" in markup or Double.NaN in code) - the size of layout
        /// partition is equal to DesiredSize of the child element.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double ItemHeight
        {
            get { return (double) GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Orientation" /> property.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
                StackPanel.OrientationProperty.AddOwner(
                        typeof(WrapPanel),
                        new FrameworkPropertyMetadata(
                                Orientation.Horizontal,
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnOrientationChanged)));

        /// <summary>
        /// Specifies dimension of children positioning in absence of wrapping.
        /// Wrapping occurs in orthogonal direction. For example, if Orientation is Horizontal, 
        /// the items try to form horizontal rows first and if needed are wrapped and form vertical stack of rows.
        /// If Orientation is Vertical, items first positioned in a vertical column, and if there is
        /// not enough space - wrapping creates additional columns in horizontal dimension.
        /// </summary>
        public Orientation Orientation
        {
            get { return _orientation; }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// <see cref="PropertyMetadata.PropertyChangedCallback"/>
        /// </summary>
        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            WrapPanel p = (WrapPanel)d;
            p._orientation = (Orientation) e.NewValue;
        }

        private Orientation _orientation;

        #endregion

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        private struct UVSize
        {
            internal UVSize (Orientation orientation, double width, double height)
            {
                U = V = 0d;
                _orientation = orientation;
                Width = width;
                Height = height;
            }

            internal UVSize (Orientation orientation)
            {
                U = V = 0d;
                _orientation = orientation;
            }
            
            internal double U;
            internal double V;
            private Orientation _orientation;

            internal double Width
            {
                get { return (_orientation == Orientation.Horizontal ? U : V); }
                set { if(_orientation == Orientation.Horizontal) U = value; else V = value; }
            }
            internal double Height
            {
                get { return (_orientation == Orientation.Horizontal ? V : U); }
                set { if(_orientation == Orientation.Horizontal) V = value; else U = value; }
            }
        }


        /// <summary>
        /// <see cref="FrameworkElement.MeasureOverride"/>
        /// </summary>
        protected override Size MeasureOverride(Size constraint)
        {
            UVSize curLineSize = new UVSize(Orientation);
            UVSize panelSize = new UVSize(Orientation);
            UVSize uvConstraint = new UVSize(Orientation, constraint.Width, constraint.Height);
            double itemWidth = ItemWidth;
            double itemHeight = ItemHeight;
            bool itemWidthSet = !DoubleUtil.IsNaN(itemWidth);
            bool itemHeightSet = !DoubleUtil.IsNaN(itemHeight);
            
            Size childConstraint = new Size(
                (itemWidthSet ?  itemWidth  : constraint.Width),
                (itemHeightSet ? itemHeight : constraint.Height));

            UIElementCollection children = InternalChildren;

            for(int i=0, count = children.Count; i<count; i++)
            {
                UIElement child = children[i] as UIElement;
                if(child == null) continue;

                //Flow passes its own constrint to children
                child.Measure(childConstraint);

                //this is the size of the child in UV space
                UVSize sz = new UVSize(
                    Orientation,
                    (itemWidthSet ?  itemWidth  : child.DesiredSize.Width),
                    (itemHeightSet ? itemHeight : child.DesiredSize.Height));

                if (DoubleUtil.GreaterThan(curLineSize.U + sz.U, uvConstraint.U)) //need to switch to another line
                {
                    panelSize.U = Math.Max(curLineSize.U, panelSize.U);
                    panelSize.V += curLineSize.V;
                    curLineSize = sz;

                    if(DoubleUtil.GreaterThan(sz.U, uvConstraint.U)) //the element is wider then the constrint - give it a separate line                    
                    {
                        panelSize.U = Math.Max(sz.U, panelSize.U);
                        panelSize.V += sz.V;
                        curLineSize = new UVSize(Orientation);
                    }
                }
                else //continue to accumulate a line
                {
                    curLineSize.U += sz.U;
                    curLineSize.V = Math.Max(sz.V, curLineSize.V);
                }
            }

            //the last line size, if any should be added
            panelSize.U = Math.Max(curLineSize.U, panelSize.U);
            panelSize.V += curLineSize.V;

            //go from UV space to W/H space
            return new Size(panelSize.Width, panelSize.Height);
        }

        /// <summary>
        /// <see cref="FrameworkElement.ArrangeOverride"/>
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            int firstInLine = 0;
            double itemWidth = ItemWidth;
            double itemHeight = ItemHeight;
            double accumulatedV = 0;
            double itemU = (Orientation == Orientation.Horizontal ? itemWidth : itemHeight);
            UVSize curLineSize = new UVSize(Orientation);
            UVSize uvFinalSize = new UVSize(Orientation, finalSize.Width, finalSize.Height);
            bool itemWidthSet = !DoubleUtil.IsNaN(itemWidth);
            bool itemHeightSet = !DoubleUtil.IsNaN(itemHeight);
            bool useItemU = (Orientation == Orientation.Horizontal ? itemWidthSet : itemHeightSet);

            UIElementCollection children = InternalChildren;

            for(int i=0, count = children.Count; i<count; i++)
            {
                UIElement child = children[i] as UIElement;
                if(child == null) continue;
                
                UVSize sz = new UVSize(
                    Orientation,
                    (itemWidthSet ?  itemWidth  : child.DesiredSize.Width),
                    (itemHeightSet ? itemHeight : child.DesiredSize.Height));

                if (DoubleUtil.GreaterThan(curLineSize.U + sz.U, uvFinalSize.U)) //need to switch to another line
                {
                    arrangeLine(accumulatedV, curLineSize.V, firstInLine, i, useItemU, itemU);

                    accumulatedV += curLineSize.V;
                    curLineSize = sz;

                    if(DoubleUtil.GreaterThan(sz.U, uvFinalSize.U)) //the element is wider then the constraint - give it a separate line                    
                    {
                        //switch to next line which only contain one element
                        arrangeLine(accumulatedV, sz.V, i, ++i, useItemU, itemU);

                        accumulatedV += sz.V;
                        curLineSize = new UVSize(Orientation);
                    }
                    firstInLine= i;
                }
                else //continue to accumulate a line
                {
                    curLineSize.U += sz.U;
                    curLineSize.V = Math.Max(sz.V, curLineSize.V);
                }
            }

            //arrange the last line, if any
            if(firstInLine < children.Count)
            {
                arrangeLine(accumulatedV, curLineSize.V, firstInLine, children.Count, useItemU, itemU);
            }

            return finalSize;
        }

        private void arrangeLine(double v, double lineV, int start, int end, bool useItemU, double itemU)
        {   
            double u = 0;
            bool isHorizontal = (Orientation == Orientation.Horizontal);
            
            UIElementCollection children = InternalChildren;
            for(int i = start; i < end; i++)
            {
                UIElement child = children[i] as UIElement;
                if(child != null)
                {
                    UVSize childSize = new UVSize(Orientation, child.DesiredSize.Width, child.DesiredSize.Height);
                    double layoutSlotU = (useItemU ? itemU : childSize.U);
                    child.Arrange(new Rect(
                        (isHorizontal ? u : v), 
                        (isHorizontal ? v : u), 
                        (isHorizontal ? layoutSlotU : lineV), 
                        (isHorizontal ? lineV : layoutSlotU)));
                    u += layoutSlotU;                
                }
            }
        }

        #endregion Protected Methods
    }
}

