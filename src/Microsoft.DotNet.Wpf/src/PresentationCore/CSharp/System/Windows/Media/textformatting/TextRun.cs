// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Definition of text run
//
//  Spec:      Text Formatting API.doc
//
//


using System;
using System.Collections;
using System.Windows;
using System.Windows.Media;
using MS.Internal.TextFormatting;


namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// Root of the hierarchy. TextRun provides definition for a group of characters 
    /// sharing the same set of character properties.
    /// </summary>
    /// <remarks>
    /// 
    ///     == TEXTRUN hierarchy ==
    /// 
    ///      ()  denotes abstract class
    ///      []  denotes concrete class
    ///     ____ denotes "is-a" relationship
    ///     o--> denotes "contain" relationship
    /// 
    /// 
    ///     (TextRun)_____(TextEmbeddedObject)_______[MyInlineObject]
    ///                |
    ///                |____[TextCharacters]
    ///                |           |
    ///                |           o--->(TextShapeableSymbols)____[TextShapeableCharacters]
    ///                |
    ///                |__(TextShapeableSymbols)_______[TabletShapeableInk]
    ///                |
    ///                |__[TextEndOfLine]
    ///                |        |
    ///                |        |__[TextEndOfParagraph]
    ///                |
    ///                |__(TextModifier)______[TextDecorationsModifier]
    ///                |
    ///                |__[TextEndOfSegment]
    /// 
    /// 
    ///     Public abstract classes:
    /// 
    ///     TextRun is the root abstraction where all kinds of run derive. 
    ///     TextEmbeddedObject is object within text flow which is measured, drawn and hittest'd as a whole.
    ///     TextShapeableSymbols is collection of characters which are measured, drawn and hittest'd as a series of individual glyphs.
    ///     TextModifier is a text run that modifies properties of subsequent text runs in its scope. 
    /// 
    /// 
    ///     Public built-in concrete classes:
    /// 
    ///     TextCharacters is a specialized TextRun implemented by TextFormatter, containing a collection of TextShapeableSymbols.
    ///     TextShapeableCharacters is a specialized TextShapeableSymbols implemented by TextFormatter, characters are formatted thru specified typeface.
    ///     TextEndOfLine is a specialized TextRun implemented by TextFormatter to mark the end of line.
    ///     TextEndOfParagraph is a specialized TextLineBreak implemented by TextFormatter to mark the end of paragraph.
    ///     TextDefaultModifier is a specialized TextModifier implemented by TextFormatter.
    ///     TextEndOfSegment is a specialized TextRun implemented by TextFormatter that ends the scope of a TextModifier.
    /// 
    /// 
    ///     Client-implemented concrete classes:
    /// 
    ///     MyInlineObject is a specialized TextEmbeddedObject implemented by TextFormatter's client for e.g. inline image, button etc.
    ///     TabletInkShape is a specialized TextShapeableSymbols implemented by Tablet team, characters are formatted thru Tablet's inking engine.
    /// 
    /// </remarks>
    public abstract class TextRun
    {
        /// <summary>
        /// Reference to character buffer
        /// </summary>
        public abstract CharacterBufferReference CharacterBufferReference 
        { get; }

        
        /// <summary>
        /// Character length
        /// </summary>
        public abstract int Length 
        { get; }


        /// <summary>
        /// A set of properties shared by every characters in the run
        /// </summary>
        public abstract TextRunProperties Properties 
        { get; }
    }
}

