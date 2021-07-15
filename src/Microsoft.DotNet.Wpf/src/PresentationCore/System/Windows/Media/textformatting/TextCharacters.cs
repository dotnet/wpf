// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Implementation of text symbol for characters
//
//  Spec:      Text Formatting API.doc
//
//


using System;
using System.Diagnostics;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using System.Windows;
using MS.Internal;
using MS.Internal.Shaping;
using MS.Internal.TextFormatting;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// A specialized TextSymbols implemented by TextFormatter to produces 
    /// a collection of TextCharacterShape – each represents a collection of 
    /// character glyphs from distinct physical typeface.
    /// </summary>
    public class TextCharacters : TextRun, ITextSymbols, IShapeableTextCollector
    {
        private CharacterBufferReference    _characterBufferReference;
        private int                         _length;
        private TextRunProperties           _textRunProperties;

        #region Constructors

        /// <summary>
        /// Construct a run of text content from character array
        /// </summary>
        public TextCharacters(
            char[]                      characterArray,
            int                         offsetToFirstChar,
            int                         length,
            TextRunProperties           textRunProperties
            ) : 
            this(
                new CharacterBufferReference(characterArray, offsetToFirstChar),
                length,
                textRunProperties
                )
        {}


        /// <summary>
        /// Construct a run for text content from string 
        /// </summary>
        public TextCharacters(
            string                      characterString,
            TextRunProperties           textRunProperties
            ) : 
            this(
                characterString,
                0,  // offserToFirstChar
                (characterString == null) ? 0 : characterString.Length,
                textRunProperties
                )
        {}


        /// <summary>
        /// Construct a run for text content from string
        /// </summary>
        public TextCharacters(
            string                      characterString,
            int                         offsetToFirstChar,
            int                         length,
            TextRunProperties           textRunProperties
            ) : 
            this(
                new CharacterBufferReference(characterString, offsetToFirstChar),
                length,
                textRunProperties
                )
        {}


        /// <summary>
        /// Construct a run for text content from unsafe character string
        /// </summary>
        [CLSCompliant(false)]
        public unsafe TextCharacters(
            char*                       unsafeCharacterString,
            int                         length,
            TextRunProperties           textRunProperties
            ) : 
            this(
                new CharacterBufferReference(unsafeCharacterString, length),
                length,
                textRunProperties
                )
        {}


        /// <summary>
        /// Internal constructor of TextContent
        /// </summary>
        private TextCharacters(
            CharacterBufferReference    characterBufferReference,
            int                         length,
            TextRunProperties           textRunProperties
            )
        {        
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException("length", SR.Get(SRID.ParameterMustBeGreaterThanZero));
            }

            if (textRunProperties == null)
            {
                throw new ArgumentNullException("textRunProperties");
            }

            if (textRunProperties.Typeface == null)
            {
                throw new ArgumentNullException("textRunProperties.Typeface");
            }

            if (textRunProperties.CultureInfo == null)
            {
                throw new ArgumentNullException("textRunProperties.CultureInfo");
            }

            if (textRunProperties.FontRenderingEmSize <= 0)
            {
                throw new ArgumentOutOfRangeException("textRunProperties.FontRenderingEmSize", SR.Get(SRID.ParameterMustBeGreaterThanZero));
            }

            _characterBufferReference = characterBufferReference;
            _length = length;
            _textRunProperties = textRunProperties;
        }

        #endregion


        #region TextRun implementation
        
        /// <summary>
        /// Character buffer
        /// </summary>
        public sealed override CharacterBufferReference CharacterBufferReference 
        { 
            get { return _characterBufferReference; }
        }

        
        /// <summary>
        /// Character length of the run
        /// </summary>
        /// <value></value>
        public sealed override int Length 
        { 
            get { return _length; }
        }


        /// <summary>
        /// A set of properties shared by every characters in the run
        /// </summary>
        public sealed override TextRunProperties Properties
        {
            get { return _textRunProperties; }
        }

        #endregion


        #region ITextSymbols implementation
        
        /// <summary>
        /// Break a run of text into individually shape items.
        /// Shape items are delimited by 
        ///     Change of writing system
        ///     Change of glyph typeface
        /// </summary>
        IList<TextShapeableSymbols> ITextSymbols.GetTextShapeableSymbols(
            GlyphingCache               glyphingCache,
            CharacterBufferReference    characterBufferReference,
            int                         length,
            bool                        rightToLeft,
            bool                        isRightToLeftParagraph,
            CultureInfo                 digitCulture,
            TextModifierScope           textModifierScope,
            TextFormattingMode          textFormattingMode,
            bool                        isSideways
            )
        {
            if (characterBufferReference.CharacterBuffer == null)
            {
                throw new ArgumentNullException("characterBufferReference.CharacterBuffer");
            }
            
            int offsetToFirstChar = characterBufferReference.OffsetToFirstChar - _characterBufferReference.OffsetToFirstChar;

            Debug.Assert(characterBufferReference.CharacterBuffer == _characterBufferReference.CharacterBuffer);
            Debug.Assert(offsetToFirstChar >= 0 && offsetToFirstChar < _length);

            if (    length < 0 
                ||  offsetToFirstChar + length > _length)
            {
                length = _length - offsetToFirstChar;
            }

            // Get the actual text run properties in effect, after invoking any
            // text modifiers that may be in scope.
            TextRunProperties textRunProperties = _textRunProperties;
            if (textModifierScope != null)
            {
                textRunProperties = textModifierScope.ModifyProperties(textRunProperties);
            }

            if (!rightToLeft)
            {
                // Fast loop early out for run with all non-complex characters
                // which can be optimized by not going thru shaping engine.

                int nominalLength;

                if (textRunProperties.Typeface.CheckFastPathNominalGlyphs(
                    new CharacterBufferRange(characterBufferReference, length),
                    textRunProperties.FontRenderingEmSize,
                    (float)textRunProperties.PixelsPerDip,
                    1.0,
                    double.MaxValue,    // widthMax
                    true,               // keepAWord
                    digitCulture != null,
                    CultureMapper.GetSpecificCulture(textRunProperties.CultureInfo),
                    textFormattingMode,
                    isSideways,
                    false, //breakOnTabs
                    out nominalLength
                    ) && length == nominalLength)
                {
                    return new TextShapeableCharacters[]
                    {
                        new TextShapeableCharacters(
                            new CharacterBufferRange(characterBufferReference, nominalLength),
                            textRunProperties,
                            textRunProperties.FontRenderingEmSize,                        
                            new MS.Internal.Text.TextInterface.ItemProps(),
                            null,   // shapeTypeface (no shaping required)
                            false,   // nullShape,
                            textFormattingMode,
                            isSideways
                            )
                    };
                }
            }

            IList<TextShapeableSymbols> shapeables = new List<TextShapeableSymbols>(2);

            glyphingCache.GetShapeableText(
                textRunProperties.Typeface,
                characterBufferReference,
                length,
                textRunProperties,
                digitCulture,
                isRightToLeftParagraph,
                shapeables,
                this as IShapeableTextCollector,
                textFormattingMode
                );

            return shapeables;
        }

        #endregion


        #region IShapeableTextCollector implementation

        /// <summary>
        /// Add shapeable text object to the list
        /// </summary>
        void IShapeableTextCollector.Add(
            IList<TextShapeableSymbols>  shapeables,
            CharacterBufferRange         characterBufferRange,
            TextRunProperties            textRunProperties,
            MS.Internal.Text.TextInterface.ItemProps textItem,
            ShapeTypeface                shapeTypeface,
            double                       emScale,
            bool                         nullShape,
            TextFormattingMode               textFormattingMode
            )
        {
            Debug.Assert(shapeables != null);

            shapeables.Add(
                new TextShapeableCharacters(
                    characterBufferRange,
                    textRunProperties,
                    textRunProperties.FontRenderingEmSize * emScale,
                    textItem,
                    shapeTypeface,
                    nullShape,
                    textFormattingMode,
                    false
                    )
                );
        }

        #endregion
    }
}
