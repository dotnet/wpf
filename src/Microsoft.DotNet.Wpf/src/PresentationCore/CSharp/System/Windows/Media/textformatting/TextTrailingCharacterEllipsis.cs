// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Implementation of text collapsing properties for whole line trailing character ellipsis
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
    /// a collapsing properties to collapse whole line toward the end
    /// at character granularity and with ellipsis being the collapsing symbol
    /// </summary>
    public class TextTrailingCharacterEllipsis : TextCollapsingProperties
    {
        private double      _width;
        private TextRun     _ellipsis;

        private const string StringHorizontalEllipsis = "\x2026";


        #region Constructor

        /// <summary>
        /// Construct a text trailing character ellipsis collapsing properties
        /// </summary>
        /// <param name="width">width in which collapsing is constrained to</param>
        /// <param name="textRunProperties">text run properties of ellispis symbol</param>
        public TextTrailingCharacterEllipsis(
            double              width,
            TextRunProperties   textRunProperties
            )
        {
            _width = width;
            _ellipsis = new TextCharacters(StringHorizontalEllipsis, textRunProperties);
        }

        #endregion


        /// <summary>
        /// TextFormatter to get width in which specified collapsible range constrained to
        /// </summary>
        public sealed override double Width
        {
            get { return _width; }
        }


        /// <summary>
        /// TextFormatter to get text run used as collapsing symbol
        /// </summary>
        public sealed override TextRun Symbol
        {
            get { return _ellipsis; }
        }


        /// <summary>
        /// TextFormatter to get style of collapsing
        /// </summary>
        public sealed override TextCollapsingStyle Style
        {
            get { return TextCollapsingStyle.TrailingCharacter; }
        }
    }
}

