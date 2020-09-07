// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
//
// Description: 
//              AdornerDecorator class.
//              See spec at: AdornerLayer Spec.htm
// 

using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace System.Windows.Documents
{
    /// <summary>
    /// This AdornerDecorator does not hookup its child in the logical tree. It's being
    /// used by PopupRoot and FixedDocument.
    /// </summary>
    internal class NonLogicalAdornerDecorator : AdornerDecorator
    {
        public override UIElement Child
        {
            get
            {
                return IntChild;
            }
            set
            {
                if (IntChild != value)
                {
                    this.RemoveVisualChild(IntChild);
                    this.RemoveVisualChild(AdornerLayer);                    
                    IntChild = value;
                    if(value != null)
                    {
                        this.AddVisualChild(value);
                        this.AddVisualChild(AdornerLayer);                        
                    }

                    InvalidateMeasure();
                }
            }
        }
    }


    /// <summary>
    /// Object which allows elements beneath it in the visual tree to be adorned.
    /// AdornerDecorator has two children.
    /// The first child is the parent of the rest of the visual tree below the AdornerDecorator.
    /// The second child is the AdornerLayer on which adorners are rendered.
    /// 
    /// AdornerDecorator is intended to be used as part of an object's Style.
    /// </summary>
    public class AdornerDecorator : Decorator
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public AdornerDecorator() : base()
        {
            _adornerLayer = new AdornerLayer();
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// AdornerLayer on which adorners are rendered.
        /// </summary>
        public AdornerLayer AdornerLayer
        {
            get
            {
                return _adornerLayer;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Measurement override. Implement your size-to-content logic here.
        /// </summary>
        /// <param name="constraint">
        /// Sizing constraint.
        /// </param>
        protected override Size MeasureOverride(Size constraint)
        {
                Size desiredSize = base.MeasureOverride(constraint);
                if (VisualTreeHelper.GetParent(_adornerLayer) != null)
                {
                    // We don't really care about the size of the AdornerLayer-- we'll
                    // always just make the AdornerDecorator the full desiredSize.  But
                    // we need to measure it anyway, to make sure Adorners render.
                    _adornerLayer.Measure(constraint);
                }
                return desiredSize;
        }

        /// <summary>
        /// Override for <seealso cref="FrameworkElement.ArrangeOverride" />  
        /// </summary>
        /// <param name="finalSize">The size reserved for this element by the parent</param>
        /// <returns>The actual ink area of the element, typically the same as finalSize</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
                Size inkSize = base.ArrangeOverride(finalSize);

                if (VisualTreeHelper.GetParent(_adornerLayer) != null)
                {
                    _adornerLayer.Arrange(new Rect(finalSize));
                }

                return (inkSize);
        }


        /// <summary>
        /// Gets or sets the child of the AdornerDecorator.
        /// </summary>
        public override UIElement Child
        {
            get
            {
                return base.Child;
            }
            set
            {
                Visual old = base.Child;

                if (old == value)
                {
                    return;
                }
                
                if (value == null)
                {
                    base.Child = null;
                    RemoveVisualChild(_adornerLayer);
                }
                else
                {
                    base.Child = value;
                    if (null == old)
                    {
                        AddVisualChild(_adornerLayer);
                    }
                }
            }  
        }                                

        /// <summary>
        /// Returns the Visual children count.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get 
            { 
                if (base.Child != null)
                {
                    return 2; // One for the child and one for the adorner layer.
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Returns the child at the specified index.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (base.Child == null)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }
            else
            {
                switch (index)
                {
                    case 0: 
                        return base.Child;

                    case 1:
                        return _adornerLayer;

                    default:
                        throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
                }
            }
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------

        #region Private Members

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 6; }
        }
        
        readonly AdornerLayer _adornerLayer;

        #endregion Private Members
    }
}




