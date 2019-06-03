// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Implementation of text paragraph break control 
//
//  Spec:      Text Formatting API.doc
//
//


using System;
using System.Collections;
using System.Windows;


namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// Specialized line break control used to mark the end of a paragraph
    /// </summary>
    public class TextEndOfParagraph : TextEndOfLine
    {
        #region Constructors

        /// <summary>
        /// Construct a paragraph break run
        /// </summary>
        /// <param name="length">number of characters</param>
        public TextEndOfParagraph(int length) : base(length)
        {}


        /// <summary>
        /// Construct a paragraph break run
        /// </summary>
        /// <param name="length">number of characters</param>
        /// <param name="textRunProperties">linebreak text run properties</param>
        public TextEndOfParagraph(
            int                 length, 
            TextRunProperties   textRunProperties
            )
            : base(
                length, 
                textRunProperties
                )
        {}

        #endregion
    }
}

