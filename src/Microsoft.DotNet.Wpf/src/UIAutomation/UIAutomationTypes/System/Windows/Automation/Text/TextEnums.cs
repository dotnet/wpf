// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for text range.
//
//  
//


// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MS.Internal.Automation;

namespace System.Windows.Automation.Text
{
    // The TextPatternRange class comes after the following enums related to text patterns and ranges.

    /// <summary>
    /// Values for AnimationStyleAttribute
    ///</summary>
    // NOTE: the values match those returned from ITextFont::GetAnimation.
    [ComVisible(true)]
    [Guid("B6C08F15-AA5E-4754-9E4C-AA279D3F36D4")]
#if (INTERNAL_COMPILE)
    internal enum AnimationStyle
#else
    public enum AnimationStyle
#endif
    {
        /// <summary>None</summary>
        None = 0,
        /// <summary>LasVegasLights</summary>
        LasVegasLights = 1,
        /// <summary>BlinkingBackground</summary>
        BlinkingBackground = 2,
        /// <summary>SparkleText</summary>
        SparkleText = 3,
        /// <summary>MarchingBlackAnts</summary>
        MarchingBlackAnts = 4,
        /// <summary>MarchingRedAnts</summary>
        MarchingRedAnts = 5,
        /// <summary>Shimmer</summary>
        Shimmer = 6,
        /// <summary>Other</summary>
        Other = -1,
    }

    /// <summary>
    /// Values for BulletStyleAttribute
    ///</summary>
    [ComVisible(true)]
    [Guid("814FAC6C-F8DE-4682-AF5F-37C4F720990C")]
#if (INTERNAL_COMPILE)
    internal enum BulletStyle
#else
    public enum BulletStyle
#endif
    {
        /// <summary>None</summary>
        None = 0, 
        /// <summary>HollowRoundBullet</summary>
        HollowRoundBullet = 1, 
        /// <summary>FilledRoundBullet</summary>
        FilledRoundBullet = 2, 
        /// <summary>HollowSquareBullet</summary>
        HollowSquareBullet = 3,
        /// <summary>FilledSquareBullet</summary>
        FilledSquareBullet = 4, 
        /// <summary>DashBullet</summary>
        DashBullet = 5, 
        /// <summary>Other</summary>
        Other = -1,
    }


    /// <summary>
    /// Values for CapStyleAttribute
    ///</summary>
    [ComVisible(true)]
    [Guid("4E33C74B-7848-4f1e-B819-A0D866C2EA1F")]
#if (INTERNAL_COMPILE)
    internal enum CapStyle
#else
    public enum CapStyle
#endif
    {
        /// <summary>None</summary>
        None = 0,
        /// <summary>SmallCap</summary>
        SmallCap = 1,
        /// <summary>AllCap</summary>
        AllCap = 2,
        /// <summary></summary>
        AllPetiteCaps = 3,
        /// <summary></summary>
        PetiteCaps = 4,
        /// <summary></summary>
        Unicase = 5,
        /// <summary></summary>
        Titling = 6,
        /// <summary>Other</summary>
        Other = -1,
    }

    /// <summary>
    /// TextFlowDirectionAttribute is some combination of these attributes.
    /// For example, a value of zero means text flows in horizontal lines,
    /// left-to-right, from the top of the page to the bottom of the page.
    /// A value of RightToLeft|BottomToTop|Vertical means text flows 
    /// in vertical line from the bottom to the top, with successive lines
    /// going right-to-left on the page
    ///</summary>
    [Flags]
    [ComVisible(true)]
    [Guid("2E22CC6B-7C34-4002-91AA-E103A09D1027")]
#if (INTERNAL_COMPILE)
    internal enum FlowDirections
#else
    public enum FlowDirections
#endif
    {
        /// <summary>None</summary>
        Default = 0,
        /// <summary>RightToLeft.</summary>
        RightToLeft = 1,
        /// <summary>BottomToTop.</summary>
        BottomToTop = 2,
        /// <summary>Vertical.</summary>
        Vertical = 4,
    }

    /// <summary>
    /// Values for HorizontalTextAlignmentAttribute
    ///</summary>
    // Note: the values match those returned from ITextPara::GetAlignment
    [ComVisible(true)]
    [Guid("1FBE7021-A1E4-4e9b-BE94-2C7DFA59D5DD")]
#if (INTERNAL_COMPILE)
    internal enum HorizontalTextAlignment
#else
    public enum HorizontalTextAlignment
#endif
    {
        /// <summary>Left</summary>
        Left = 0,
        /// <summary>Centered</summary>
        Centered = 1,
        /// <summary>Right</summary>
        Right = 2,
        /// <summary>Justified</summary>
        Justified = 3,
    }

    /// <summary>
    /// Values for OutlineStylesAttribute
    ///</summary>
    [Flags]
    [ComVisible(true)]
    [Guid("1F57B37D-CB59-43f4-95E0-7C9E40DB427E")]
#if (INTERNAL_COMPILE)
    internal enum OutlineStyles
#else
    public enum OutlineStyles
#endif
    {
        /// <summary>None</summary>
        None = 0,
        /// <summary>Outline</summary>
        Outline = 1,
        /// <summary>Shadow</summary>
        Shadow = 2,
        /// <summary>Engraved</summary>
        Engraved = 4,
        /// <summary>Embossed</summary>
        Embossed = 8,
    }

    /// <summary>
    /// Each TextPatternRange has two endpoints.  This enumeration allows 
    /// the endpoints to be identified when calling member functions of
    /// TextPatternRange.
    ///</summary>
    [ComVisible(true)]
    [Guid("62242CAC-9CD0-4364-813D-4F0A36DD842D")]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal enum TextPatternRangeEndpoint
#else
    public enum TextPatternRangeEndpoint
#endif
    {
        /// <summary>The starting point of the range.</summary>
        Start = 0,
        /// <summary>The ending point of the range.</summary>
        End = 1
    }

    /// <summary>
    /// Units of text for the purposes of navigation.
    ///</summary>
    [ComVisible(true)]
    [Guid("A044E5C8-FC20-4747-8CC8-1487F9CBB680")]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal enum TextUnit
#else
    public enum TextUnit
#endif
    {
        // if you add a unit less than Character or greater than Document
        // then you need to update ValidateUnitArgument.

        /// <summary>Character</summary>
        Character = 0,
        /// <summary>Format</summary>
        Format = 1,
        /// <summary>Word</summary>
        Word = 2,
        /// <summary>Line</summary>
        Line = 3,
        /// <summary>Paragraph</summary>
        Paragraph = 4,
        /// <summary>Page</summary>
        Page = 5,
        /// <summary>Document</summary>
        Document = 6,
    }

    /// <summary>
    /// Values for UnderlineStyleAttribute
    ///</summary>
    // Note: the values match those returned by ITextFont::GetUnderline
    [ComVisible(true)]
    [Guid("909D8633-2941-428e-A549-C752E2FC078C")]
#if (INTERNAL_COMPILE)
    internal enum TextDecorationLineStyle
#else
    public enum TextDecorationLineStyle
#endif
    {
        /// <summary>None</summary>
        None = 0,
        /// <summary>Single</summary>
        Single = 1,
        /// <summary>WordsOnly</summary>
        WordsOnly = 2,
        /// <summary>Double</summary>
        Double = 3,
        /// <summary>Dot</summary>
        Dot = 4,
        /// <summary>Dash</summary>
        Dash = 5,
        /// <summary>Dash Dot</summary>
        DashDot = 6,
        /// <summary>Dash Dot Dot</summary>
        DashDotDot = 7,
        /// <summary>Wavy</summary>
        Wavy = 8,
        /// <summary>ThickSingle</summary>
        ThickSingle = 9,
        /// <summary>DoubleWavy</summary>
        DoubleWavy = 11,
        /// <summary>ThickWavy</summary>
        ThickWavy = 12,
        /// <summary>LongDash</summary>
        LongDash = 13,
        /// <summary>ThickDash</summary>
        ThickDash = 14,
        /// <summary>Thick Dash Dot</summary>
        ThickDashDot = 15,
        /// <summary>Thick Dash Dot Dot</summary>
        ThickDashDotDot = 16,
        /// <summary>ThickDot</summary>
        ThickDot = 17,
        /// <summary>ThickLongDash</summary>
        ThickLongDash = 18,
        /// <summary>Other</summary>
        Other = -1,
    }
}
