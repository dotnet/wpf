// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Floater element. 
//

using System.ComponentModel;
using MS.Internal;
using MS.Internal.PtsHost.UnsafeNativeMethods;      // PTS restrictions

namespace System.Windows.Documents
{
    /// <summary>
    /// Floater element 
    /// </summary>
    public class Floater : AnchoredBlock
    {
        //-------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Static ctor.  Initializes property metadata.
        /// </summary>
        static Floater()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Floater), new FrameworkPropertyMetadata(typeof(Floater)));
        }

        /// <summary>
        /// Initialized the new instance of a Floater
        /// </summary>
        public Floater() : this(null, null)
        {
        }

        /// <summary>
        /// Initialized the new instance of a Floater specifying a Block added
        /// to a Floater as its first child.
        /// </summary>
        /// <param name="childBlock">
        /// Block added as a first initial child of the Floater.
        /// </param>
        public Floater(Block childBlock) : this(childBlock, null)
        {
        }

        /// <summary>
        /// Creates a new Floater instance.
        /// </summary>
        /// <param name="childBlock">
        /// Optional child of the new Floater, may be null.
        /// </param>
        /// <param name="insertionPosition">
        /// Optional position at which to insert the new Floater. May
        /// be null.
        /// </param>
        public Floater(Block childBlock, TextPointer insertionPosition) : base(childBlock, insertionPosition)
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        // Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// DependencyProperty for <see cref="HorizontalAlignment" /> property.
        /// </summary>
        public static readonly DependencyProperty HorizontalAlignmentProperty = 
                FrameworkElement.HorizontalAlignmentProperty.AddOwner(
                        typeof(Floater),
                        new FrameworkPropertyMetadata(
                                HorizontalAlignment.Stretch,
                                FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// 
        /// </summary>
        public HorizontalAlignment HorizontalAlignment
        {
            get { return (HorizontalAlignment)GetValue(HorizontalAlignmentProperty); }
            set { SetValue(HorizontalAlignmentProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Width" /> property.
        /// </summary>
        public static readonly DependencyProperty WidthProperty =
                DependencyProperty.Register(
                        "Width",
                        typeof(double),
                        typeof(Floater),
                        new FrameworkPropertyMetadata(
                                Double.NaN,
                                FrameworkPropertyMetadataOptions.AffectsMeasure),
                        new ValidateValueCallback(IsValidWidth));

        /// <summary>
        /// The Width property specifies the width of the element.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double Width
        {
            get { return (double)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private static bool IsValidWidth(object o)
        {
            double width = (double)o;
            double maxWidth = Math.Min(1000000, PTS.MaxPageSize);
            if (Double.IsNaN(width))
            {
                // Default value of width is NaN
                return true;
            }
            if (width < 0 || width > maxWidth)
            {
                return false;
            }
            return true;
        }

        #endregion Private Methods
    }
}
