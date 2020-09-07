// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      AdornedElementPlaceholder is an element used in a Binding.ErrorTemplate.
//      Its purpose is to mimic the height and width of the AdornedElement so that
//      other elements in Template can be arranged around or within it.
//
// See specs at Validation.mht
//

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using MS.Internal.Controls;
using MS.Utility;


namespace System.Windows.Controls
{
    /// <summary>
    ///     The base class for all controls.
    /// </summary>
    [ContentProperty("Child")]
    public class AdornedElementPlaceholder : FrameworkElement, IAddChild
    {
        #region Constructors

        /// <summary>
        ///     Default Control constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public AdornedElementPlaceholder() : base()
        {
        }

        #endregion Constructors


        ///<summary>
        /// This method is called to Add the object as a child.  This method is used primarily
        /// by the parser; a more direct way of adding a child is to use the <see cref="Child" />
        /// property.
        ///</summary>
        ///<param name="value">
        /// The object to add as a child; it must be a UIElement.
        ///</param>
        void IAddChild.AddChild (Object value)
        {
            // keeping consistent with other elements:  adding null is a no-op.
            if (value == null)
                return;

            if (!(value is UIElement))
                throw new ArgumentException (SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(UIElement)), "value");

            if (this.Child != null)
                throw new ArgumentException(SR.Get(SRID.CanOnlyHaveOneChild, this.GetType(), value.GetType()));

            this.Child = (UIElement)value;
        }

        ///<summary>
        /// This method is called by the parser when text appears under the tag in markup.
        /// Calling this method has no effect if text is just whitespace.  If text is not
        /// just whitespace, throw an exception.
        ///</summary>
        ///<param name="text">
        /// Text to add as a child.
        ///</param>
        void IAddChild.AddText (string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }


        ///<summary>
        /// Element for which the AdornedElementPlaceholder is reserving space.
        ///</summary>
        public UIElement AdornedElement
        {
            get
            {
                TemplatedAdorner adorner = TemplatedAdorner;
                return adorner == null ? null : TemplatedAdorner.AdornedElement;
            }
        }


        /// <summary>
        /// The single child of an <see cref="AdornedElementPlaceholder" />
        /// </summary>
        [DefaultValue(null)]
        public virtual UIElement Child
        {
            get
            {
                return _child;
            }

            set
            {
                UIElement old = _child;

                if (old != value)
                {
                    RemoveVisualChild(old);
                    //need to remove old element from logical tree
                    RemoveLogicalChild(old);
                    _child = value;
                    
                    AddVisualChild(_child);
                    AddLogicalChild(value);

                    InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// Gets the Visual children count.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                return (_child == null) ? 0 : 1;
            }
        }

        /// <summary>
        /// Gets the Visual child at the specified index.
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
        /// Returns enumerator to logical children.
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                // Could optimize this code by returning EmptyEnumerator.Instance if _child == null.
                return new SingleChildEnumerator(_child);
            }
        }

        /// <summary>
        ///     This virtual method in called when IsInitialized is set to true and it raises an Initialized event
        /// </summary>
        protected override void OnInitialized(EventArgs e)
        {
            if (TemplatedParent == null)
                throw new InvalidOperationException(SR.Get(SRID.AdornedElementPlaceholderMustBeInTemplate));

            base.OnInitialized(e);
        }


        /// <summary>
        ///     AdornedElementPlaceholder measure behavior is to measure
        ///     only the first visual child.  Note that the return value
        ///     of Measure on this child is ignored as the purpose of this
        ///     class is to match the size of the element for which this
        ///     is a placeholder.
        /// </summary>
        /// <param name="constraint">The measurement constraints.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            if (TemplatedParent == null)
                throw new InvalidOperationException(SR.Get(SRID.AdornedElementPlaceholderMustBeInTemplate));

            if (AdornedElement == null)
                return new Size(0,0);

            Size desiredSize = AdornedElement.RenderSize;
            UIElement child = Child;

            if (child != null)
                child.Measure(desiredSize);

            return desiredSize;
        }

        /// <summary>
        ///     Default AdornedElementPlaceholder arrangement is to only arrange
        ///     the first visual child. No transforms will be applied.
        /// </summary>
        /// <param name="arrangeBounds">The computed size.</param>
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            UIElement child = Child;

            if (child != null)
                child.Arrange(new Rect(arrangeBounds));

            return arrangeBounds;
        }


        private TemplatedAdorner TemplatedAdorner
        {
            get
            {
                if (_templatedAdorner == null)
                {
                    // find the TemplatedAdorner
                    FrameworkElement templateParent = this.TemplatedParent as FrameworkElement;

                    if (templateParent != null)
                    {
                        _templatedAdorner = VisualTreeHelper.GetParent(templateParent) as TemplatedAdorner;

                        if (_templatedAdorner != null && _templatedAdorner.ReferenceElement == null)
                        {
                            _templatedAdorner.ReferenceElement = this;
                        }
                    }
                }
                return _templatedAdorner;
            }
        }

        private UIElement _child;
        private TemplatedAdorner _templatedAdorner;
    }
}




