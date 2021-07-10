// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Contains the Viewbox Decorator class.
//              Spec at Viewbox.xml
//

using MS.Internal;
using MS.Utility;
using MS.Internal.Controls;
using MS.Internal.Telemetry.PresentationFramework;
using System.Diagnostics;
using System.Collections;
using System.Windows.Threading;

using System.Windows.Media;
using System.Windows.Documents;

using System;

namespace System.Windows.Controls
{
    #region StretchDirection enum type

    /// <summary>
    /// StretchDirection - Enum which describes when scaling should be used on the content of a Viewbox. This
    /// enum restricts the scaling factors along various axes.
    /// </summary>
    /// <seealso cref="Viewbox" />
    public enum StretchDirection
    {
        /// <summary>
        /// Only scales the content upwards when the content is smaller than the Viewbox.
        /// If the content is larger, no scaling downwards is done.
        /// </summary>
        UpOnly,

        /// <summary>
        /// Only scales the content downwards when the content is larger than the Viewbox.
        /// If the content is smaller, no scaling upwards is done.
        /// </summary>
        DownOnly,

        /// <summary>
        /// Always stretches to fit the Viewbox according to the stretch mode.
        /// </summary>
        Both
    } 

    #endregion

    /// <summary>
    /// </summary>
    public class Viewbox : Decorator 
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        static Viewbox()
        {
            ControlsTraceLogger.AddControl(TelemetryControls.ViewBox);
        }

        /// <summary>
        ///     Default DependencyObject constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public Viewbox() : base()
        {
        }
        
        #endregion


        //-------------------------------------------------------------------
        //
        //  Public Fields
        //
        //-------------------------------------------------------------------

        #region Public Fields

        /// <summary>
            /// This is the DependencyProperty for the Viewbox's Stretch property.
            ///
            /// Default:  Stretch.Uniform
        /// <seealso cref="Viewbox.Stretch" />
        /// </summary>
        public static readonly DependencyProperty StretchProperty
            = DependencyProperty.Register(
                "Stretch",          // Property name
                typeof(Stretch),    // Property type
                typeof(Viewbox),    // Property owner
                new FrameworkPropertyMetadata(Stretch.Uniform, FrameworkPropertyMetadataOptions.AffectsMeasure),
                new ValidateValueCallback(ValidateStretchValue));                           

        private static bool ValidateStretchValue(object value)
        {
            Stretch s = (Stretch)value;
            return (    s == Stretch.Uniform
                    ||  s == Stretch.None
                    ||  s == Stretch.Fill
                    ||  s == Stretch.UniformToFill);
        }

        /// <summary>
        /// This is the DependencyProperty for the Viewbox's StretchDirection property.
        /// Default:  StretchDirection.Both
        /// <seealso cref="Viewbox.StretchDirection" />
        /// </summary>
        public static readonly DependencyProperty StretchDirectionProperty
            = DependencyProperty.Register(
                "StretchDirection",         // Property name
                typeof(StretchDirection),   // Property type
                typeof(Viewbox),            // Property owner
                new FrameworkPropertyMetadata(StretchDirection.Both, FrameworkPropertyMetadataOptions.AffectsMeasure),
                new ValidateValueCallback(ValidateStretchDirectionValue));                           

        private static bool ValidateStretchDirectionValue(object value)
        {
            StretchDirection sd = (StretchDirection)value;
            return (    sd == StretchDirection.Both
                    ||  sd == StretchDirection.DownOnly
                    ||  sd == StretchDirection.UpOnly);
        }

        #endregion


        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        private ContainerVisual InternalVisual
        {
            get
            {
                if(_internalVisual == null) 
                {
                    _internalVisual = new ContainerVisual();
                    AddVisualChild(_internalVisual);
                }
                return _internalVisual;
            }
        }

        private UIElement InternalChild
        {
            get
            {
                VisualCollection vc = InternalVisual.Children;
                if (vc.Count != 0) return vc[0] as UIElement;
                else               return null;
            }
            set
            {
                VisualCollection vc = InternalVisual.Children;
                if (vc.Count != 0) vc.Clear();
                vc.Add(value);
            }
        }   

        private Transform InternalTransform
        {
            get
            {
                return InternalVisual.Transform;
            }
            set
            {
                InternalVisual.Transform = value;
            }
        }                

        /// <summary>
        /// The single child of a <see cref="Viewbox" />
        /// </summary>
        public override UIElement Child
        {
            //everything is the same as on Decorator, the only difference is to insert intermediate Visual to
            //specify scaling transform
            get
            {
                return InternalChild;
            }
            
            set
            {
                UIElement old = InternalChild;

                if(old != value)
                {
                    //need to remove old element from logical tree
                    RemoveLogicalChild(old);

                    if(value != null)
                    {
                        AddLogicalChild(value);
                    }

                    InternalChild = value;
                    
                    InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// Returns the Visual children count.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get { return 1; /* Always have internal container visual */ }
        }

        /// <summary>
        /// Returns the child at the specified index.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }
            return InternalVisual;
        }
        
        /// <summary> 
        /// Returns enumerator to logical children.
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                if (InternalChild == null)
                {
                    return EmptyEnumerator.Instance;
                }
                
                return new SingleChildEnumerator(InternalChild);
            }
        }

        /// <summary>
        /// Gets/Sets the Stretch mode of the Viewbox, which determines how the content will be
        /// fit into the Viewbox's space.
        ///
        /// </summary>
        /// <seealso cref="Viewbox.StretchProperty" />
        /// <seealso cref="Stretch" />
        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        /// <summary>
        /// Gets/Sets the stretch direction of the Viewbox, which determines the restrictions on
        /// scaling that are applied to the content inside the Viewbox.  For instance, this property
        /// can be used to prevent the content from being smaller than its native size or larger than
        /// its native size.
        /// </summary>
        /// <seealso cref="Viewbox.StretchDirectionProperty" />
        public StretchDirection StretchDirection
        {
            get  {  return (StretchDirection)GetValue(StretchDirectionProperty);  }
            set  {  SetValue(StretchDirectionProperty, value);  }
        }
        
        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Updates DesiredSize of the Viewbox.  Called by parent UIElement.  This is the first pass of layout.
        /// </summary>
        /// <remarks>
        /// Viewbox measures it's child at an infinite constraint; it allows the child to be however large it so desires.
        /// The child's returned size will be used as it's natural size for scaling to Viewbox's size during Arrange.
        /// </remarks>
        /// <param name="constraint">Constraint size is an "upper limit" that the return value should not exceed.</param>
        /// <returns>The Decorator's desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
                UIElement child = InternalChild;
                Size parentSize = new Size();

                if (child != null)
                {
                    // Initialize child constraint to infinity.  We need to get a "natural" size for the child in absence of constraint.
                    // Note that an author *can* impose a constraint on a child by using Height/Width, &c... properties 
                    Size infinteConstraint = new Size(Double.PositiveInfinity, Double.PositiveInfinity);

                    child.Measure(infinteConstraint);
                    Size childSize = child.DesiredSize;

                    Size scalefac = ComputeScaleFactor(constraint, childSize, this.Stretch, this.StretchDirection);

                    parentSize.Width = scalefac.Width * childSize.Width;
                    parentSize.Height = scalefac.Height * childSize.Height;
                }

                return parentSize;
        }



        /// <summary>
        /// Viewbox always sets the child to its desired size.  It then computes and applies a transformation
        /// from that size to the space available: Viewbox's own input size less child margin.
        /// 
        /// Viewbox also calls arrange on its child.
        /// </summary>
        /// <param name="arrangeSize">Size in which Border will draw the borders/background and children.</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
                UIElement child = InternalChild;
                if (child != null)
                {
                    Size childSize = child.DesiredSize;

                    // Compute scaling factors from arrange size and the measured child content size
                    Size scalefac = ComputeScaleFactor(arrangeSize, childSize, this.Stretch, this.StretchDirection);

                    InternalTransform = new ScaleTransform(scalefac.Width, scalefac.Height);

                    // Arrange the child to the desired size 
                    child.Arrange(new Rect(new Point(), child.DesiredSize));

                    //return the size oocupied by scaled child
                    arrangeSize.Width = scalefac.Width * childSize.Width;
                    arrangeSize.Height = scalefac.Height * childSize.Height;
                }
                return arrangeSize;
        }



        /// <summary>
        /// This is a helper function that computes scale factors depending on a target size and a content size
        /// </summary>
        /// <param name="availableSize">Size into which the content is being fitted.</param>
        /// <param name="contentSize">Size of the content, measured natively (unconstrained).</param>
        /// <param name="stretch">Value of the Stretch property on the element.</param>
        /// <param name="stretchDirection">Value of the StretchDirection property on the element.</param>
        internal static Size ComputeScaleFactor(Size availableSize, 
                                                Size contentSize, 
                                                Stretch stretch, 
                                                StretchDirection stretchDirection)
        {
            // Compute scaling factors to use for axes
            double scaleX = 1.0;
            double scaleY = 1.0;

            bool isConstrainedWidth = !Double.IsPositiveInfinity(availableSize.Width);
            bool isConstrainedHeight = !Double.IsPositiveInfinity(availableSize.Height);

           if (     (stretch == Stretch.Uniform || stretch == Stretch.UniformToFill || stretch == Stretch.Fill)
                &&  (isConstrainedWidth || isConstrainedHeight) )
            {
                // Compute scaling factors for both axes
                scaleX = (DoubleUtil.IsZero(contentSize.Width)) ? 0.0 : availableSize.Width / contentSize.Width;
                scaleY = (DoubleUtil.IsZero(contentSize.Height)) ? 0.0 : availableSize.Height / contentSize.Height;

                if (!isConstrainedWidth)        scaleX = scaleY;
                else if (!isConstrainedHeight)  scaleY = scaleX;
                else 
                {
                    // If not preserving aspect ratio, then just apply transform to fit
                    switch (stretch) 
                    {
                        case Stretch.Uniform:       //Find minimum scale that we use for both axes
                            double minscale = Math.Min(scaleX, scaleY);
                            scaleX = scaleY = minscale;
                            break;

                        case Stretch.UniformToFill: //Find maximum scale that we use for both axes
                            double maxscale = Math.Max(scaleX, scaleY);
                            scaleX = scaleY = maxscale;
                            break;

                        case Stretch.Fill:          //We already computed the fill scale factors above, so just use them
                            break;
                    }
                }

                //Apply stretch direction by bounding scales.
                //In the uniform case, scaleX=scaleY, so this sort of clamping will maintain aspect ratio
                //In the uniform fill case, we have the same result too.
                //In the fill case, note that we change aspect ratio, but that is okay
                switch(stretchDirection)
                {
                    case StretchDirection.UpOnly:
                        if (scaleX < 1.0) scaleX = 1.0;
                        if (scaleY < 1.0) scaleY = 1.0;
                        break;

                    case StretchDirection.DownOnly:
                        if (scaleX > 1.0) scaleX = 1.0;
                        if (scaleY > 1.0) scaleY = 1.0;
                        break;

                    case StretchDirection.Both:
                        break;

                    default:
                        break;
                }
            }
            //Return this as a size now
            return new Size(scaleX, scaleY);
        }
    
        #endregion Protected Methods



        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private ContainerVisual _internalVisual;

        #endregion
    }
}


