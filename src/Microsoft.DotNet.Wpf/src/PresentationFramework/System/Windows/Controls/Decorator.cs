// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Contains the Decorator class.
//              Spec at Decorator.xml
//

using MS.Internal;
using MS.Internal.Controls;
using MS.Utility;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Markup; // IAddChild, ContentPropertyAttribute

namespace System.Windows.Controls
{
    /// <summary>
    /// Decorator is a base class for elements that apply effects onto or around a single child.
    /// </summary>
    [Localizability(LocalizationCategory.Ignore, Readability = Readability.Unreadable)]
    [ContentProperty("Child")]
    public class Decorator : FrameworkElement, IAddChild
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Default DependencyObject constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public Decorator() : base()
        {
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        ///<summary>
        /// This method is called to Add the object as a child of the Decorator.  This method is used primarily
        /// by the parser; a more direct way of adding a child to a Decorator is to use the <see cref="Child" />
        /// property.
        ///</summary>
        ///<param name="value">
        /// The object to add as a child; it must be a UIElement.
        ///</param>
        void IAddChild.AddChild (Object value)
        {
            if (!(value is UIElement))
            {
                throw new ArgumentException (SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(UIElement)), "value");
            }

            if (this.Child != null)
            {
                throw new ArgumentException(SR.Get(SRID.CanOnlyHaveOneChild, this.GetType(), value.GetType()));
            }

            this.Child = (UIElement)value;
        }

        ///<summary>
        /// This method is called by the parser when text appears under the tag in markup.
        /// As Decorators do not support text, calling this method has no effect if the text
        /// is all whitespace.  For non-whitespace text, throw an exception.
        ///</summary>
        ///<param name="text">
        /// Text to add as a child.
        ///</param> 
        void IAddChild.AddText (string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }
        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The single child of a <see cref="System.Windows.Controls.Decorator" />
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
                if(_child != value)
                {
                    // notify the visual layer that the old child has been removed.
                    RemoveVisualChild(_child);                    

                    //need to remove old element from logical tree
                    RemoveLogicalChild(_child);

                    _child = value;

                    AddLogicalChild(value);
                    // notify the visual layer about the new child.
                    AddVisualChild(value);

                    InvalidateMeasure();
                }
            }
        }

        /// <summary> 
        /// Returns enumerator to logical children.
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                if (_child == null)
                {
                    return EmptyEnumerator.Instance;
                }
                
                return new SingleChildEnumerator(_child);
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods


        /// <summary>
        /// Returns the Visual children count.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get { return (_child == null) ? 0 : 1; }
        }

        /// <summary>
        /// Returns the child at the specified index.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (    (_child == null)
                ||  (index != 0))
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }
            
            return _child;
        }

        /// <summary>
        /// Updates DesiredSize of the Decorator.  Called by parent UIElement.  This is the first pass of layout.
        /// </summary>
        /// <remarks>
        /// Decorator determines a desired size it needs from the child's sizing properties, margin, and requested size.
        /// </remarks>
        /// <param name="constraint">Constraint size is an "upper limit" that the return value should not exceed.</param>
        /// <returns>The Decorator's desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            UIElement child = Child;
            if (child != null)
            {
                child.Measure(constraint);
                return (child.DesiredSize);
            }
            return (new Size());
        }
        
        /// <summary>
        /// Decorator computes the position of its single child inside child's Margin and calls Arrange
        /// on the child.
        /// </summary>
        /// <param name="arrangeSize">Size the Decorator will assume.</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            UIElement child = Child;
            if (child != null)
            {
                child.Arrange(new Rect(arrangeSize));
            }
            return (arrangeSize);
        }

        #endregion

        #region Internal Properties
        internal UIElement IntChild
        {
            get { return _child; }
            set { _child = value; }
        }
        #endregion

        #region Private Members

        UIElement _child;
        #endregion Private Members
    }
}



