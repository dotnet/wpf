// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Public types for FlowDocument properties. 
//
//

using System;
using System.Windows;
using System.Windows.Media;

namespace System.Windows
{
    /// <summary>
    /// This property describes mechanism by which a line box is determined for each line.
    /// </summary>
    public enum LineStackingStrategy
    {        
        /// <summary>
        /// The stack-height is determined by the block element 'line-height' property value.
        /// </summary>
        BlockLineHeight,   

        /// <summary>
        /// The stack-height is the smallest value that contains the extended block progression 
        /// dimension of all the inline elements on that line when those elements are properly aligned.
        /// The 'line-height' property value is taken into account only for the block elements.
        /// </summary>
        MaxHeight,

        ///// <summary>
        ///// The stack-height is the smallest value that contains the extended block progression 
        ///// dimension of all the inline elements on that line when those elements are properly aligned.
        ///// </summary>
        //InlineLineHeight,        

        ///// <summary>
        ///// The stack-height is the smallest multiple of the block element 'line-height' computed value 
        ///// that can contain the block progression of all the inline elements on that line when those 
        ///// elements are properly aligned.
        ///// </summary>
        //GridHeight,
    }

    /// <summary>
    /// Describes how to distribute space in the case where the Column Content 
    /// Width is smaller than the content width of the element.
    /// </summary>
    public enum ColumnSpaceDistribution
    {
        /// <summary>
        /// The space is placed before the first column.
        /// </summary>
        Left    = 0,

        /// <summary>
        /// The space is placed after the last column.
        /// </summary>
        /// 
        Right   = 1,

        /// <summary>
        /// The space is distrubted equally between all columns.
        /// </summary>
        Between = 2,
    }

    /// <summary>
    /// Specifies point of reference for a figure in vertical direction.
    /// </summary>
    // NOTE: Do not change values - they have to be in sync with PTS enums: FSKREF & FSKALIGNFIG
    public enum FigureVerticalAnchor
    {
        /// <summary>
        /// Anchor the figure to the top of the page area.
        /// </summary>
        PageTop         = 0,

        /// <summary>
        /// Anchor the figure to the center of the page area.
        /// </summary>
        PageCenter      = 1,

        /// <summary>
        /// Anchor the figure to the bottom of the page area.
        /// </summary>
        PageBottom      = 2,

        /// <summary>
        /// Anchor the figure to the top of the page content area.
        /// </summary>
        ContentTop      = 3,

        /// <summary>
        /// Anchor the figure to the center of the page content area.
        /// </summary>
        ContentCenter   = 4,

        /// <summary>
        /// Anchor the figure to the bottom of the page content area.
        /// </summary>
        ContentBottom   = 5,


        /// <summary>
        /// Anchor the figure to the top of the current paragraph.
        /// </summary>
        ParagraphTop    = 6,

        //ParagraphCenter = 7,      Not supported by PTS
        //ParagraphBottom = 8,      Not supported by PTS

        // Disabled
        // Anchor the figure to the top of the current character position.
        //CharacterTop    = 9,
        // Anchor the figure to the center of the current character position.
        //CharacterCenter = 10,
        // Anchor the figure to the bottom of the current character position.
        //CharacterBottom = 11,
    }

    /// <summary>
    /// Specifies point of reference for a figure in horizontal direction.
    /// </summary>
    // NOTE: Do not change values - they have to be in sync with PTS enums: FSKREF & FSKALIGNFIG
    public enum FigureHorizontalAnchor
    {
        /// <summary>
        /// Anchor the figure to the left of the page area.
        /// </summary>
        PageLeft        = 0,

        /// <summary>
        /// Anchor the figure to the center of the page area.
        /// </summary>
        PageCenter      = 1,

        /// <summary>
        /// Anchor the figure to the right of the page area.
        /// </summary>
        PageRight       = 2,

        /// <summary>
        /// Anchor the figure to the left of the page content area.
        /// </summary>
        ContentLeft     = 3,

        /// <summary>
        /// Anchor the figure to the center of the page content area.
        /// </summary>
        ContentCenter   = 4,

        /// <summary>
        /// Anchor the figure to the right of the page content area.
        /// </summary>
        ContentRight    = 5,

        /// <summary>
        /// Anchor the figure to the left of the current column
        /// </summary>
        ColumnLeft   = 6,

        /// <summary>
        /// Anchor the figure to the center of the current column
        /// </summary>
        ColumnCenter = 7,

        /// <summary>
        /// Anchor the figure to the right of the current column
        /// </summary>
        ColumnRight  = 8,

        // Disabled
        // Anchor the figure to the left of the current character position.
        //CharacterLeft   = 9,
        // Anchor the figure to the center of the current character position.
        //CharacterCenter = 10,
        // Anchor the figure to the right of the current character position.
        //CharacterRight  = 11,
    }

    /// <summary>
    /// Specifies which side content can wrap around an object.
    /// </summary>
    // NOTE: Do not change values - they have to be in sync with PTS enums: FSKWRAP
    public enum WrapDirection
    {
        /// <summary>
        /// Content does not flow around the object.
        /// </summary>
        None    = 0,

        /// <summary>
        /// Content flows around the left side of the object only; no content is displayed to the right.
        /// </summary>
        Left    = 1,

        /// <summary>
        /// Content flows around the right side of the object only; no content is displayed to the left.
        /// </summary>
        Right   = 2,

        /// <summary>
        /// Content flows around both sides of the object.
        /// </summary>
        Both    = 3,
    }
}
