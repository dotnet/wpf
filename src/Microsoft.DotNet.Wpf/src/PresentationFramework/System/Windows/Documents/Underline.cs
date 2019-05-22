// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Underline element. 
//    Xaml markup helper for indicating superscript content.
//    Equivalent to a Span with TextDecorations property set to TextDecorations.Underlined.
//    Can contain other inline elements.
//

namespace System.Windows.Documents 
{
    /// <summary>
    /// Underline element - markup helper for indicating superscript content.
    /// Equivalent to a Span with TextDecorations property set to TextDecorations.Underlined.
    /// Can contain other inline elements.
    /// </summary>
    public class Underline : Span
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
        static Underline()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Underline), new FrameworkPropertyMetadata(typeof(Underline)));
        }

        /// <summary>
        /// Initilizes a new instance of a Underline element
        /// </summary>
        /// <remarks>
        /// To become fully functional this element requires at least one other Inline element
        /// as its child, typically Run with some text.
        /// In Xaml markup the UNderline element may appear without Run child,
        /// but please note that such Run was implicitly inserted by parser.
        /// </remarks>
        public Underline() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of Underline element and adds a given Subscript element as its first child.
        /// </summary>
        /// <param name="childInline">
        /// Inline element added as an initial child to this Underline element
        /// </param>
        public Underline(Inline childInline) : base(childInline)
        {
        }

        /// <summary>
        /// Creates a new Underline instance.
        /// </summary>
        /// <param name="childInline">
        /// Optional child Inline for the new Underline.  May be null.
        /// </param>
        /// <param name="insertionPosition">
        /// Optional position at which to insert the new Underline.  May be null.
        /// </param>
        public Underline(Inline childInline, TextPointer insertionPosition) : base(childInline, insertionPosition)
        {
        }

        /// <summary>
        /// Creates a new Underline instance covering existing content.
        /// </summary>
        /// <param name="start">
        /// Start position of the new Underline.
        /// </param>
        /// <param name="end">
        /// End position of the new Underline.
        /// </param>
        /// <remarks>
        /// start and end must both be parented by the same Paragraph, otherwise
        /// the method will raise an ArgumentException.
        /// </remarks>
        public Underline(TextPointer start, TextPointer end) : base(start, end)
        {
        }

        #endregion Constructors
    }
}
