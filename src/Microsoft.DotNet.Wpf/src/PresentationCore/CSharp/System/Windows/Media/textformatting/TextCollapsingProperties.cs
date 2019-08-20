// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Definition of text collapsing properties and related types
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
    /// Properties of text collapsing
    /// </summary>
    public abstract class TextCollapsingProperties
    {
        /// <summary>
        /// TextFormatter to get width in which specified collapsible range constrained to
        /// </summary>
        public abstract double Width
        { get; }


        /// <summary>
        /// TextFormatter to get text run used as collapsing symbol
        /// </summary>
        public abstract TextRun Symbol
        { get; }


        /// <summary>
        /// TextFormatter to get style of collapsing
        /// </summary>
        public abstract TextCollapsingStyle Style
        { get; }
    }


    /// <summary>
    /// Range of characters and its width measurement where collapsing has happened within a line
    /// </summary>
    public sealed class TextCollapsedRange
    {
        private int         _cp;
        private int         _length;
        private double      _width;


        /// <summary>
        /// Construct a collapsed range
        /// </summary>
        /// <param name="cp">first character collapsed</param>
        /// <param name="length">number of characters collapsed</param>
        /// <param name="width">total width of collapsed characters</param>
        internal TextCollapsedRange(
            int         cp,
            int         length,
            double      width
            )
        {
            _cp = cp;
            _length = length;
            _width = width;
        }


        /// <summary>
        /// text source character index to the first character in range that is collapsed
        /// </summary>
        public int TextSourceCharacterIndex
        {
            get { return _cp; }
        }


        /// <summary>
        /// number of characters collapsed
        /// </summary>
        public int Length
        {
            get { return _length; }
        }


        /// <summary>
        /// total width of collapsed character range
        /// </summary>
        public double Width
        {
            get { return _width; }
        }
    }


    /// <summary>
    /// Text collapsing style
    /// </summary>
    public enum TextCollapsingStyle
    {
        /// <summary>
        /// Collapse trailing characters
        /// </summary>
        TrailingCharacter,

        /// <summary>
        /// Collapse trailing words
        /// </summary>
        TrailingWord,
    }
}

