// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      TemplatedAdorner applies the style provided in the ctor to a
//      control and provides a transform via GetDesiredTransform that
//      will cause the AdornedElementPlaceholder to be positioned directly
//      over the AdornedElement.
//
// See specs at Specs/Validation.mht
//

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Documents;
using MS.Utility;

namespace MS.Internal.Controls
{
    /// <summary>
    /// This class is sealed because it calls OnVisualChildrenChanged virtual in the
    /// constructor and it does not override it, but derived classes could.
    /// </summary>
    internal sealed class TemplatedAdorner : Adorner
    {
        private Control _child;

        /// <summary>
        /// The clear the single child of a TemplatedAdorner
        /// </summary>
        public void ClearChild()
        {
            this.RemoveVisualChild(_child);
            _child = null;
        }

        public TemplatedAdorner(UIElement adornedElement, ControlTemplate adornerTemplate) : base(adornedElement)
        {
            Debug.Assert(adornedElement != null, "adornedElement should not be null");
            Debug.Assert(adornerTemplate != null, "adornerTemplate should not be null");

            Control control = new Control();

            control.DataContext = Validation.GetErrors(adornedElement);
            //control.IsEnabled = false; // Hittest should not work on visual subtree
            control.IsTabStop = false;      // Tab should not get into adorner layer
            control.Template = adornerTemplate;
            _child = control;
            this.AddVisualChild(_child);
        }

        /// <summary>
        /// Adorners don't always want to be transformed in the same way as the elements they
        /// adorn.  Adorners which adorn points, such as resize handles, want to be translated
        /// and rotated but not scaled.  Adorners adorning an object, like a marquee, may want
        /// all transforms.  This method is called by AdornerLayer to allow the adorner to
        /// filter out the transforms it doesn't want and return a new transform with just the
        /// transforms it wants applied.  An adorner can also add an additional translation
        /// transform at this time, allowing it to be positioned somewhere other than the upper
        /// left corner of its adorned element.
        /// </summary>
        /// <param name="transform">The transform applied to the object the adorner adorns</param>
        /// <returns>Transform to apply to the adorner</returns>
        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            if (ReferenceElement == null)
                return transform;

            GeneralTransformGroup group = new GeneralTransformGroup();
            group.Children.Add(transform);

            GeneralTransform t = this.TransformToDescendant(ReferenceElement);
            if (t != null)
            {
                group.Children.Add(t);
            }
            return group;
        }

        public FrameworkElement ReferenceElement
        {
            get
            {
                return _referenceElement;
            }
            set
            {
                _referenceElement = value;
            }
        }

        /// <summary>
        ///   Derived class must implement to support Visual children. The method must return
        ///    the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark:
        ///       During this virtual call it is not valid to modify the Visual tree.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (_child == null || index != 0)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }

            return _child;
        }

        /// <summary>
        ///  Derived classes override this property to enable the Visual code to enumerate
        ///  the Visual children. Derived classes need to return the number of children
        ///  from this method.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark: During this virtual method the Visual tree must not be modified.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get { return _child != null ? 1 : 0; }
        }

        /// <summary>
        /// Measure adorner.
        /// </summary>
        protected override Size MeasureOverride(Size constraint)
        {
            Debug.Assert(_child != null, "_child should not be null");

            if (ReferenceElement != null && AdornedElement != null &&
                AdornedElement.IsMeasureValid &&
                !DoubleUtil.AreClose(ReferenceElement.DesiredSize, AdornedElement.DesiredSize)
                )
            {
                ReferenceElement.InvalidateMeasure();
            }

            (_child).Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

            return (_child).DesiredSize;
        }



        /// <summary>
        ///     Default control arrangement is to only arrange
        ///     the first visual child. No transforms will be applied.
        /// </summary>
        protected override Size ArrangeOverride(Size size)
        {
            Size finalSize;

            finalSize = base.ArrangeOverride(size);

            if (_child != null)
            {
                _child.Arrange(new Rect(new Point(), finalSize));
            }
            return finalSize;
        }


        internal override bool NeedsUpdate(Size oldSize)
        {
            bool result = base.NeedsUpdate(oldSize);
            Visibility desired = AdornedElement.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            if (desired != this.Visibility)
            {
                this.Visibility = desired;
                result = true;
            }
            return result;
        }

        private FrameworkElement _referenceElement;
    }
}




