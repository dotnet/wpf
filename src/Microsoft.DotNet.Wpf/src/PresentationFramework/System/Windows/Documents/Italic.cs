// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Italic element. 
//    Xaml markup helper for indicating italicized content.
//    Equivalent to a Span with FontStyle property set to FontStyles.Italic.
//    Can contain other inline elements.
//

namespace System.Windows.Documents 
{
    /// <summary>
    /// Italic element - markup helper for indicating italicized content.
    /// Equivalent to a Span with FontStyle property set to FontStyles.Italic.
    /// Can contain other inline elements.
    /// </summary>
    public class Italic : Span
    {
        //-------------------------------------------------------------------
        //
        // Connstructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Static ctor.  Initializes property metadata.
        /// </summary>
        static Italic()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Italic), new FrameworkPropertyMetadata(typeof(Italic)));
        }

        /// <summary>
        /// Initilizes a new instance of a Italic element
        /// </summary>
        /// <remarks>
        /// To become fully functional this element requires at least one other Inline element
        /// as its child, typically Run with some text.
        /// In Xaml markup the Italic element may appear without Run child,
        /// but please note that such Run was implicitly inserted by parser.
        /// </remarks>
        public Italic() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of Italic element and adds a given Inline element as its first child.
        /// </summary>
        /// <param name="childInline">
        /// Inline element added as an initial child to this Italic element
        /// </param>
        public Italic(Inline childInline) : base(childInline)
        {
        }

        /// <summary>
        /// Creates a new Italic instance.
        /// </summary>
        /// <param name="childInline">
        /// Optional child Inline for the new Italic.  May be null.
        /// </param>
        /// <param name="insertionPosition">
        /// Optional position at which to insert the new Italic.  May be null.
        /// </param>
        public Italic(Inline childInline, TextPointer insertionPosition) : base(childInline, insertionPosition)
        {
        }

        /// <summary>
        /// Creates a new Italic instance covering existing content.
        /// </summary>
        /// <param name="start">
        /// Start position of the new Italic.
        /// </param>
        /// <param name="end">
        /// End position of the new Italic.
        /// </param>
        /// <remarks>
        /// start and end must both be parented by the same Paragraph, otherwise
        /// the method will raise an ArgumentException.
        /// </remarks>
        public Italic(TextPointer start, TextPointer end) : base(start, end)
        {
        }

        #endregion Constructors
    }
}

