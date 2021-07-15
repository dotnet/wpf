// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Contains the Canvas class.
//              Spec at Canvas.xml
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

using MS.Internal.PresentationFramework;

namespace System.Windows.Controls
{
    /// <summary>
    /// Canvas is used to place child UIElements at arbitrary positions or to draw children in multiple
    /// layers.
    /// 
    /// Child positions are computed from the Left, Top properties.  These properties do
    /// not contribute to the size of the Canvas.  To position children in a way that affects the Canvas' size,
    /// use the Margin properties.
    /// 
    /// The order that children are drawn (z-order) is determined exclusively by child order.
    /// </summary>
    public class Canvas : Panel
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        static Canvas()
        {
            ControlsTraceLogger.AddControl(TelemetryControls.Canvas);
        }

        /// <summary>
        ///     Default DependencyObject constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public Canvas() : base()
        {
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Reads the attached property Left from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the Left attached property.</param>
        /// <returns>The property's value.</returns>
        /// <seealso cref="Canvas.LeftProperty" />
        [TypeConverter("System.Windows.LengthConverter, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        [AttachedPropertyBrowsableForChildren()]
        public static double GetLeft(UIElement element)
        {
            if (element == null) { throw new ArgumentNullException(nameof(element)); }
            return (double)element.GetValue(LeftProperty);
        }

        /// <summary>
        /// Writes the attached property Left to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the Left attached property.</param>
        /// <param name="length">The length to set</param>
        /// <seealso cref="Canvas.LeftProperty" />
        public static void SetLeft(UIElement element, double length)
        {
            if (element == null) { throw new ArgumentNullException(nameof(element)); }
            element.SetValue(LeftProperty, length);
        }

        /// <summary>
        /// Reads the attached property Top from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the Top attached property.</param>
        /// <returns>The property's value.</returns>
        /// <seealso cref="Canvas.TopProperty" />
        [TypeConverter("System.Windows.LengthConverter, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        [AttachedPropertyBrowsableForChildren()]
        public static double GetTop(UIElement element)
        {
            if (element == null) { throw new ArgumentNullException(nameof(element)); }
            return (double)element.GetValue(TopProperty);
        }

        /// <summary>
        /// Writes the attached property Top to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the Top attached property.</param>
        /// <param name="length">The length to set</param>
        /// <seealso cref="Canvas.TopProperty" />
        public static void SetTop(UIElement element, double length)
        {
            if (element == null) { throw new ArgumentNullException(nameof(element)); }
            element.SetValue(TopProperty, length);
        }

        /// <summary>
        /// Reads the attached property Right from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the Right attached property.</param>
        /// <returns>The property's Length value.</returns>
        /// <seealso cref="Canvas.RightProperty" />
        [TypeConverter("System.Windows.LengthConverter, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        [AttachedPropertyBrowsableForChildren()]
        public static double GetRight(UIElement element)
        {
            if (element == null) { throw new ArgumentNullException(nameof(element)); }
            return (double)element.GetValue(RightProperty);
        }

        /// <summary>
        /// Writes the attached property Right to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the Right attached property.</param>
        /// <param name="length">The Length to set</param>
        /// <seealso cref="Canvas.RightProperty" />
        public static void SetRight(UIElement element, double length)
        {
            if (element == null) { throw new ArgumentNullException(nameof(element)); }
            element.SetValue(RightProperty, length);
        }

        /// <summary>
        /// Reads the attached property Bottom from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the Bottom attached property.</param>
        /// <returns>The property's Length value.</returns>
        /// <seealso cref="Canvas.BottomProperty" />
        [TypeConverter("System.Windows.LengthConverter, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        [AttachedPropertyBrowsableForChildren()]
        public static double GetBottom(UIElement element)
        {
            if (element == null) { throw new ArgumentNullException(nameof(element)); }
            return (double)element.GetValue(BottomProperty);
        }

        /// <summary>
        /// Writes the attached property Bottom to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the Bottom attached property.</param>
        /// <param name="length">The Length to set</param>
        /// <seealso cref="Canvas.BottomProperty" />
        public static void SetBottom(UIElement element, double length)
        {
            if (element == null) { throw new ArgumentNullException(nameof(element)); }
            element.SetValue(BottomProperty, length);
        }



        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties + Dependency Properties's
        //
        //-------------------------------------------------------------------
      
        #region Public Properties

        //having this invalidate callback allows to host UIElements in Canvas and still
        //receive invalidations when Left/Top/Bottom/Right properties change - 
        //registering the attached properties with AffectsParentArrange flag would be a mistake
        //because those flags only work for FrameworkElements
        private static void OnPositioningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = d as UIElement;
            if(uie != null)
            {
                Canvas p = VisualTreeHelper.GetParent(uie) as Canvas;
                if(p != null)
                    p.InvalidateArrange();
            }
         }

        /// <summary>
        /// This is the dependency property registered for the Canvas' Left attached property.
        /// 
        /// The Left property is read by a Canvas on its children to determine where to position them.
        /// The child's offset from this property does not have an effect on the Canvas' own size.
        /// Conflict between the Left and Right properties is resolved in favor of Left.
        /// </summary>
        public static readonly DependencyProperty LeftProperty
            = DependencyProperty.RegisterAttached("Left", typeof(double), typeof(Canvas),
                    new FrameworkPropertyMetadata(Double.NaN, new PropertyChangedCallback(OnPositioningChanged)),
                    new ValidateValueCallback(System.Windows.Shapes.Shape.IsDoubleFiniteOrNaN));

        /// <summary>
        /// This is the dependency property registered for the Canvas' Top attached property.
        /// 
        /// The Top property is read by a Canvas on its children to determine where to position them.
        /// The child's offset from this property does not have an effect on the Canvas' own size.
        /// </summary>
        public static readonly DependencyProperty TopProperty
            = DependencyProperty.RegisterAttached("Top", typeof(double), typeof(Canvas),
                    new FrameworkPropertyMetadata(Double.NaN, new PropertyChangedCallback(OnPositioningChanged)),
                    new ValidateValueCallback(System.Windows.Shapes.Shape.IsDoubleFiniteOrNaN));

        /// <summary>
        /// This is the dependency property registered for the Canvas' Right attached property.
        /// 
        /// The Right property is read by a Canvas on its children to determine where to position them.
        /// The child's offset from this property does not have an effect on the Canvas' own size.
        /// Conflict between the Left and Right properties is resolved in favor of Left.
        /// </summary>
        public static readonly DependencyProperty RightProperty
            = DependencyProperty.RegisterAttached("Right", typeof(double), typeof(Canvas),
                    new FrameworkPropertyMetadata(Double.NaN, new PropertyChangedCallback(OnPositioningChanged)),
                    new ValidateValueCallback(System.Windows.Shapes.Shape.IsDoubleFiniteOrNaN));

        /// <summary>
        /// This is the dependency property registered for the Canvas' Bottom attached property.
        /// 
        /// The Bottom property is read by a Canvas on its children to determine where to position them.
        /// The child's offset from this property does not have an effect on the Canvas' own size.
        /// </summary>
        public static readonly DependencyProperty BottomProperty
            = DependencyProperty.RegisterAttached("Bottom", typeof(double), typeof(Canvas),
                    new FrameworkPropertyMetadata(Double.NaN, new PropertyChangedCallback(OnPositioningChanged)),
                    new ValidateValueCallback(System.Windows.Shapes.Shape.IsDoubleFiniteOrNaN));

        #endregion

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Updates DesiredSize of the Canvas.  Called by parent UIElement.  This is the first pass of layout.
        /// </summary>
        /// <param name="constraint">Constraint size is an "upper limit" that Canvas should not exceed.</param>
        /// <returns>Canvas' desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            Size childConstraint = new Size(Double.PositiveInfinity, Double.PositiveInfinity);
            
            foreach (UIElement child in InternalChildren)
            {
                if (child == null) { continue; }
                child.Measure(childConstraint);
            }

            return new Size();
        }

        /// <summary>
        /// Canvas computes a position for each of its children taking into account their margin and
        /// attached Canvas properties: Top, Left.  
        /// 
        /// Canvas will also arrange each of its children.
        /// </summary>
        /// <param name="arrangeSize">Size that Canvas will assume to position children.</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            //Canvas arranges children at their DesiredSize.
            //This means that Margin on children is actually respected and added
            //to the size of layout partition for a child. 
            //Therefore, is Margin is 10 and Left is 20, the child's ink will start at 30.
            
            foreach (UIElement child in InternalChildren)
            {
                if (child == null) { continue; }

                double x = 0;
                double y = 0;
                

                //Compute offset of the child:
                //If Left is specified, then Right is ignored
                //If Left is not specified, then Right is used
                //If both are not there, then 0
                double left = GetLeft(child);
                if(!DoubleUtil.IsNaN(left)) 
                {
                    x = left; 
                }
                else
                {
                    double right = GetRight(child);

                    if(!DoubleUtil.IsNaN(right)) 
                    {
                        x = arrangeSize.Width - child.DesiredSize.Width - right;
                    }
                }
                
                double top = GetTop(child);
                if(!DoubleUtil.IsNaN(top)) 
                {
                    y = top; 
                }
                else
                {
                    double bottom = GetBottom(child);

                    if(!DoubleUtil.IsNaN(bottom)) 
                    {
                        y = arrangeSize.Height - child.DesiredSize.Height - bottom;
                    }
                }
                
                child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
            }
            return arrangeSize;
        }

        /// <summary>
        /// Override of <seealso cref="UIElement.GetLayoutClip"/>.
        /// </summary>
        /// <returns>Geometry to use as additional clip if LayoutConstrained=true</returns>
        protected override Geometry GetLayoutClip(Size layoutSlotSize)
        {
            //Canvas only clips to bounds if ClipToBounds is set, 
            //  no automatic clipping
            if(ClipToBounds)
                return new RectangleGeometry(new Rect(RenderSize));
            else
                return null;
        }
        
        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 9; }
        }

        #endregion Protected Methods
    }
}

