// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: TextRange provider adaptor for Text Object Model based objects
//

using System;                               // Exception, ...
using System.Collections;                   // Hashtable
using System.Collections.Generic;           // List<L>
using System.Collections.ObjectModel;       // ReadOnlyCollection<T>
using System.Globalization;                 // CultureInfo
using System.Windows;                       // TextDecorationCollection
using System.Windows.Automation;            // TextPatternIdentifiers
using System.Windows.Automation.Peers;      // AutomationPeer
using System.Windows.Automation.Provider;   // ITextRangeProvider
using System.Windows.Automation.Text;       // TextUnit
using System.Windows.Markup;                // XmlLanguage
using System.Windows.Media;                 // FontFamily, Brush
using System.Windows.Documents;             // ITextPointer
using MS.Internal.Documents;                // TextContainerHelper

namespace MS.Internal.Automation
{
    /// <summary>
    /// Implements the UIA's ITextRangeProvider interface for WCP text providing controls
    /// </summary>
    internal class TextRangeAdaptor : ITextRangeProvider
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Static initialization.
        /// </summary>
        static TextRangeAdaptor()
        {
            _textPatternAttributes = new Hashtable();

            // AnimationStyle
            _textPatternAttributes.Add(
                TextPatternIdentifiers.AnimationStyleAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        TextEffectCollection effects = tp.GetValue(TextElement.TextEffectsProperty) as TextEffectCollection;
                        return (effects != null && effects.Count > 0) ? AnimationStyle.Other : AnimationStyle.None;
                    },
                    delegate(object val1, object val2) { return (AnimationStyle)val1 == (AnimationStyle)val2; })
                );
            // BackgroundColor
            _textPatternAttributes.Add(
                TextPatternIdentifiers.BackgroundColorAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        return ColorFromBrush(tp.GetValue(TextElement.BackgroundProperty));
                    },
                    delegate(object val1, object val2) { return (int)val1 == (int)val2; })
                );
            // BulletStyle
            _textPatternAttributes.Add(
                TextPatternIdentifiers.BulletStyleAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        object val = tp.GetValue(List.MarkerStyleProperty);
                        if (val is TextMarkerStyle)
                        {
                            switch ((TextMarkerStyle)val)
                            {
                                case TextMarkerStyle.None:
                                    val = BulletStyle.None;
                                    break;
                                case TextMarkerStyle.Disc:
                                    val = BulletStyle.FilledRoundBullet;
                                    break;
                                case TextMarkerStyle.Circle:
                                    val = BulletStyle.HollowRoundBullet;
                                    break;
                                case TextMarkerStyle.Square:
                                    val = BulletStyle.HollowSquareBullet;
                                    break;
                                case TextMarkerStyle.Box:
                                    val = BulletStyle.FilledSquareBullet;
                                    break;
                                default:
                                    val = BulletStyle.Other;
                                    break;
                            }
                        }
                        else
                        {
                            val = BulletStyle.None;
                        }
                        return val;
                    },
                    delegate(object val1, object val2) { return (BulletStyle)val1 == (BulletStyle)val2; })
                );
            // CapStyle
            _textPatternAttributes.Add(
                TextPatternIdentifiers.CapStyleAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        FontCapitals capsWCP = (FontCapitals)tp.GetValue(Typography.CapitalsProperty);
                        CapStyle capsUIA;
                        switch (capsWCP)
                        {
                            case FontCapitals.Normal:
                                capsUIA = CapStyle.None;
                                break;
                            case FontCapitals.AllSmallCaps:
                                capsUIA = CapStyle.AllCap;
                                break;
                            case FontCapitals.SmallCaps:
                                capsUIA = CapStyle.SmallCap;
                                break;
                            case FontCapitals.AllPetiteCaps:
                                capsUIA = CapStyle.AllPetiteCaps;
                                break;
                            case FontCapitals.PetiteCaps:
                                capsUIA = CapStyle.PetiteCaps;
                                break;
                            case FontCapitals.Unicase:
                                capsUIA = CapStyle.Unicase;
                                break;
                            case FontCapitals.Titling:
                                capsUIA = CapStyle.Titling;
                                break;
                            default:
                                capsUIA = CapStyle.Other;
                                break;
                        }
                        return capsUIA;
                    },
                    delegate(object val1, object val2) { return (CapStyle)val1 == (CapStyle)val2; })
                );
            // Culture
            _textPatternAttributes.Add(
                TextPatternIdentifiers.CultureAttribute,
                new TextAttributeHelper(
                // UIAutomation expects an LCID on the provider side
                    delegate(ITextPointer tp)
                    {
                        object val = tp.GetValue(FrameworkElement.LanguageProperty);
                        return (val is XmlLanguage) ? ((XmlLanguage)val).GetEquivalentCulture().LCID : CultureInfo.InvariantCulture.LCID;
                    },
                    delegate(object val1, object val2) { return (int)val1 == (int)val2; })
                );
            // FontName
            _textPatternAttributes.Add(
                TextPatternIdentifiers.FontNameAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        return GetFontFamilyName((FontFamily)tp.GetValue(TextElement.FontFamilyProperty), tp);
                    },
                    delegate(object val1, object val2) { return (val1 as string) == (val2 as string); })
                );
            // FontSize
            _textPatternAttributes.Add(
                TextPatternIdentifiers.FontSizeAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        return NativeObjectLengthToPoints((double)tp.GetValue(TextElement.FontSizeProperty));
                    },
                    delegate(object val1, object val2) { return (double)val1 == (double)val2; })
                );
            // FontWeight
            _textPatternAttributes.Add(
                TextPatternIdentifiers.FontWeightAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        FontWeight fontWeight = (FontWeight)tp.GetValue(TextElement.FontWeightProperty);
                        return fontWeight.ToOpenTypeWeight();
                    },
                    delegate(object val1, object val2) { return (int)val1 == (int)val2; })
                );
            // ForegroundColor
            _textPatternAttributes.Add(
                TextPatternIdentifiers.ForegroundColorAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        return ColorFromBrush(tp.GetValue(TextElement.ForegroundProperty));
                    },
                    delegate(object val1, object val2) { return (int)val1 == (int)val2; })
                );
            // HorizontalTextAlignment
            _textPatternAttributes.Add(
                TextPatternIdentifiers.HorizontalTextAlignmentAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        TextAlignment alignmentWCP = (TextAlignment)tp.GetValue(Block.TextAlignmentProperty);
                        HorizontalTextAlignment alignmentUIA;
                        switch (alignmentWCP)
                        {
                            case TextAlignment.Left:
                            default:
                                alignmentUIA = HorizontalTextAlignment.Left;
                                break;
                            case TextAlignment.Right:
                                alignmentUIA = HorizontalTextAlignment.Right;
                                break;
                            case TextAlignment.Center:
                                alignmentUIA = HorizontalTextAlignment.Centered;
                                break;
                            case TextAlignment.Justify:
                                alignmentUIA = HorizontalTextAlignment.Justified;
                                break;
                        }
                        return alignmentUIA;
                    },
                    delegate(object val1, object val2) { return (HorizontalTextAlignment)val1 == (HorizontalTextAlignment)val2; })
                );
            // IndentationFirstLine
            _textPatternAttributes.Add(
                TextPatternIdentifiers.IndentationFirstLineAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        return NativeObjectLengthToPoints((double)tp.GetValue(Paragraph.TextIndentProperty));
                    },
                    delegate(object val1, object val2) { return (double)val1 == (double)val2; })
                );
            // IndentationLeading
            _textPatternAttributes.Add(
                TextPatternIdentifiers.IndentationLeadingAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        Thickness padding = (Thickness)tp.GetValue(Block.PaddingProperty);
                        return padding.IsValid(true, false, false, false) ? NativeObjectLengthToPoints(padding.Left) : 0;
                    },
                    delegate(object val1, object val2) { return (double)val1 == (double)val2; })
                );
            // IndentationTrailing
            _textPatternAttributes.Add(
                TextPatternIdentifiers.IndentationTrailingAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        Thickness padding = (Thickness)tp.GetValue(Block.PaddingProperty);
                        return padding.IsValid(true, false, false, false) ? NativeObjectLengthToPoints(padding.Right) : 0;
                    },
                    delegate(object val1, object val2) { return (double)val1 == (double)val2; })
                );
            // IsHidden
            _textPatternAttributes.Add(
                TextPatternIdentifiers.IsHiddenAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp) { return false; },
                    delegate(object val1, object val2) { return (bool)val1 == (bool)val2; })
                );
            // IsItalic
            _textPatternAttributes.Add(
                TextPatternIdentifiers.IsItalicAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        FontStyle style = (FontStyle)tp.GetValue(TextElement.FontStyleProperty);
                        // FontStyles.Oblique is assumed a sort of Italic (#1053181).
                        return (style == FontStyles.Italic || style == FontStyles.Oblique);
                    },
                    delegate(object val1, object val2) { return (bool)val1 == (bool)val2; })
                );
            // IsReadOnly
            _textPatternAttributes.Add(
                TextPatternIdentifiers.IsReadOnlyAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        bool readOnly = false;
                        if (tp.TextContainer.TextSelection != null)
                        {
                            readOnly = tp.TextContainer.TextSelection.TextEditor.IsReadOnly;
                        }
                        return readOnly;
                    },
                    delegate(object val1, object val2) { return (bool)val1 == (bool)val2; })
                );
            // IsSubscript
            _textPatternAttributes.Add(
                TextPatternIdentifiers.IsSubscriptAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        FontVariants fontVariants = (FontVariants)tp.GetValue(Typography.VariantsProperty);
                        return (fontVariants == FontVariants.Subscript);
                    },
                    delegate(object val1, object val2) { return (bool)val1 == (bool)val2; })
                );
            // IsSuperscript
            _textPatternAttributes.Add(
                TextPatternIdentifiers.IsSuperscriptAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        FontVariants fontVariants = (FontVariants)tp.GetValue(Typography.VariantsProperty);
                        return (fontVariants == FontVariants.Superscript);
                    },
                    delegate(object val1, object val2) { return (bool)val1 == (bool)val2; })
                );
            // MarginBottom
            _textPatternAttributes.Add(
                TextPatternIdentifiers.MarginBottomAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        Thickness margin = (Thickness)tp.GetValue(FrameworkElement.MarginProperty);
                        return margin.IsValid(true, false, false, false) ? NativeObjectLengthToPoints(margin.Bottom) : 0;
                    },
                    delegate(object val1, object val2) { return (double)val1 == (double)val2; })
                );
            // MarginLeading
            _textPatternAttributes.Add(
                TextPatternIdentifiers.MarginLeadingAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        Thickness margin = (Thickness)tp.GetValue(FrameworkElement.MarginProperty);
                        return margin.IsValid(true, false, false, false) ? NativeObjectLengthToPoints(margin.Left) : 0;
                    },
                    delegate(object val1, object val2) { return (double)val1 == (double)val2; })
                );
            // MarginTop
            _textPatternAttributes.Add(
                TextPatternIdentifiers.MarginTopAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        Thickness margin = (Thickness)tp.GetValue(FrameworkElement.MarginProperty);
                        return margin.IsValid(true, false, false, false) ? NativeObjectLengthToPoints(margin.Top) : 0;
                    },
                    delegate(object val1, object val2) { return (double)val1 == (double)val2; })
                );
            // MarginTrailing
            _textPatternAttributes.Add(
                TextPatternIdentifiers.MarginTrailingAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        Thickness margin = (Thickness)tp.GetValue(FrameworkElement.MarginProperty);
                        return margin.IsValid(true, false, false, false) ? NativeObjectLengthToPoints(margin.Right) : 0;
                    },
                    delegate(object val1, object val2) { return (double)val1 == (double)val2; })
                );
            // OutlineStyles 
            _textPatternAttributes.Add(
                TextPatternIdentifiers.OutlineStylesAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp) { return OutlineStyles.None; },
                    delegate(object val1, object val2) { return (OutlineStyles)val1 == (OutlineStyles)val2; })
                );
            // OverlineColor
            _textPatternAttributes.Add(
                TextPatternIdentifiers.OverlineColorAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        object decors = tp.GetValue(Inline.TextDecorationsProperty);
                        return GetTextDecorationColor(decors as TextDecorationCollection, TextDecorationLocation.OverLine);
                    },
                    delegate(object val1, object val2) { return (int)val1 == (int)val2; })
                );
            // OverlineStyle
            _textPatternAttributes.Add(
                TextPatternIdentifiers.OverlineStyleAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        object decors = tp.GetValue(Inline.TextDecorationsProperty);
                        return GetTextDecorationLineStyle(decors as TextDecorationCollection, TextDecorationLocation.OverLine);
                    },
                    delegate(object val1, object val2) { return (TextDecorationLineStyle)val1 == (TextDecorationLineStyle)val2; })
                );
            // StrikeThroughColor
            _textPatternAttributes.Add(
                TextPatternIdentifiers.StrikethroughColorAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        object decors = tp.GetValue(Inline.TextDecorationsProperty);
                        return GetTextDecorationColor(decors as TextDecorationCollection, TextDecorationLocation.Strikethrough);
                    },
                    delegate(object val1, object val2) { return (int)val1 == (int)val2; })
                );
            // StrikeThroughStyle
            _textPatternAttributes.Add(
                TextPatternIdentifiers.StrikethroughStyleAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        object decors = tp.GetValue(Inline.TextDecorationsProperty);
                        return GetTextDecorationLineStyle(decors as TextDecorationCollection, TextDecorationLocation.Strikethrough);
                    },
                    delegate(object val1, object val2) { return (TextDecorationLineStyle)val1 == (TextDecorationLineStyle)val2; })
                );
            // TextFlowDirections
            _textPatternAttributes.Add(
                TextPatternIdentifiers.TextFlowDirectionsAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        FlowDirection flowWCP = (FlowDirection)tp.GetValue(FrameworkElement.FlowDirectionProperty);
                        FlowDirections flowUIA;
                        switch (flowWCP)
                        {
                            case FlowDirection.LeftToRight:
                            default:
                                flowUIA = FlowDirections.Default;
                                break;
                            case FlowDirection.RightToLeft:
                                flowUIA = FlowDirections.RightToLeft;
                                break;
                        }
                        return flowUIA;
                    },
                    delegate(object val1, object val2) { return (FlowDirections)val1 == (FlowDirections)val2; })
                );
            // UnderlineColor
            _textPatternAttributes.Add(
                TextPatternIdentifiers.UnderlineColorAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        object decors = tp.GetValue(Inline.TextDecorationsProperty);
                        return GetTextDecorationColor(decors as TextDecorationCollection, TextDecorationLocation.Underline);
                    },
                    delegate(object val1, object val2) { return (int)val1 == (int)val2; })
                );
            // UnderlineStyle
            _textPatternAttributes.Add(
                TextPatternIdentifiers.UnderlineStyleAttribute,
                new TextAttributeHelper(
                    delegate(ITextPointer tp)
                    {
                        object decors = tp.GetValue(Inline.TextDecorationsProperty);
                        return GetTextDecorationLineStyle(decors as TextDecorationCollection, TextDecorationLocation.Underline);
                    },
                    delegate(object val1, object val2) { return (TextDecorationLineStyle)val1 == (TextDecorationLineStyle)val2; })
                );
            // TextPatternIdentifiers.TabsAttribute
        }

        /// <summary>
        /// Constructor
        /// </summary>
        internal TextRangeAdaptor(TextAdaptor textAdaptor, ITextPointer start, ITextPointer end, AutomationPeer textPeer)
        {
            Invariant.Assert(textAdaptor != null, "Invalid textAdaptor.");
            Invariant.Assert(textPeer != null, "Invalid textPeer.");
            Invariant.Assert(start != null && end != null, "Invalid range.");
            Invariant.Assert(start.CompareTo(end) <= 0, "Invalid range, end < start.");

            _textAdaptor = textAdaptor;
            _start = start.CreatePointer();
            _end = end.CreatePointer();
            _textPeer = textPeer;
        }
        #endregion Constructors        

        #region Internal methods
        
        /// <summary>
        /// This wrapper is to cover up a shortcoming in MoveToInsertionPosition code. 
        /// If the position is inside a Hyperlink, it is being moved out of the hyperlink and to the previous
        /// insertion position which can be on the previous line (or unit) and this is creating problems
        /// This is a temporary solution until we find a better solution to handle the case
        /// There is also a related bug in MoveToLineBoundary: PS#1742102
        /// </summary>
        internal static bool MoveToInsertionPosition(ITextPointer position, LogicalDirection direction)
        {
            if (!position.TextContainer.IsReadOnly ||
                (!TextPointerBase.IsAtNonMergeableInlineStart(position) && !TextPointerBase.IsAtNonMergeableInlineEnd(position)))
            {
                return position.MoveToInsertionPosition(direction);
            }
            return false;
        }
        #endregion Internal methods
        



        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Verifies that the given range points to the same text container as this one.
        /// </summary>
        /// <returns>The validated range casted to TextRangeAdaptor</returns>
        private TextRangeAdaptor ValidateAndThrow(ITextRangeProvider range)
        {
            TextRangeAdaptor rangeAdaptor = range as TextRangeAdaptor;
            if (rangeAdaptor == null || rangeAdaptor._start.TextContainer != _start.TextContainer)
            {
                throw new ArgumentException(SR.Get(SRID.TextRangeProvider_WrongTextRange));
            }
            return rangeAdaptor;
        }

        /// <summary>
        /// Expands the range to an integral number of enclosing units.  If the range is already an
        /// integral number of the specified units then it remains unchanged.
        /// </summary>
        private void ExpandToEnclosingUnit(TextUnit unit, bool expandStart, bool expandEnd)
        {
            ITextView textView;
            switch (unit)
            {
                case TextUnit.Character:
                    if (expandStart && !TextPointerBase.IsAtInsertionPosition(_start))
                    {
                        TextPointerBase.MoveToNextInsertionPosition(_start, LogicalDirection.Backward);
                    }
                    if (expandEnd && !TextPointerBase.IsAtInsertionPosition(_end))
                    {
                        TextPointerBase.MoveToNextInsertionPosition(_end, LogicalDirection.Forward);
                    }
                    break;

                case TextUnit.Word:
                    if (expandStart && !IsAtWordBoundary(_start))
                    {
                        MoveToNextWordBoundary(_start, LogicalDirection.Backward);
                    }
                    if (expandEnd && !IsAtWordBoundary(_end))
                    {
                        MoveToNextWordBoundary(_end, LogicalDirection.Forward);
                    }
                    break;

                case TextUnit.Format:
                    // Formatting changes can be introduced by elements. Hence it is fair to 
                    // assume that formatting boundaries are defined by non-text context.

                    if (expandStart)
                    {
                        TextPointerContext forwardContext = _start.GetPointerContext(LogicalDirection.Forward);
                        while (true)
                        {
                            TextPointerContext backwardContext = _start.GetPointerContext(LogicalDirection.Backward);

                            if (backwardContext == TextPointerContext.None)
                                break;
                            if (forwardContext == TextPointerContext.Text && backwardContext != TextPointerContext.Text)
                                break;

                            forwardContext = backwardContext;
                            _start.MoveToNextContextPosition(LogicalDirection.Backward);
                        }
                    }

                    if (expandEnd)
                    {
                        TextPointerContext backwardContext = _end.GetPointerContext(LogicalDirection.Backward);
                        while (true)
                        {
                            TextPointerContext forwardContext = _end.GetPointerContext(LogicalDirection.Forward);

                            if (forwardContext == TextPointerContext.None)
                                break;
                            if (forwardContext == TextPointerContext.Text && backwardContext != TextPointerContext.Text)
                                break;

                            backwardContext = forwardContext;
                            _end.MoveToNextContextPosition(LogicalDirection.Forward);
                        }
                    }

                    // Set LogicalDirection to prevent end points from crossing a formatting
                    // boundary when normalized.
                    _start.SetLogicalDirection(LogicalDirection.Forward);
                    _end.SetLogicalDirection(LogicalDirection.Forward);
                    break;

                case TextUnit.Line:
                    // Positions are snapped to closest line boundaries. But since line information
                    // is based on the layout, positions are not changed, if:
                    // a) they are not currently in the view, or
                    // b) containing line cannot be found.
                    textView = _textAdaptor.GetUpdatedTextView();
                    if (textView != null && textView.IsValid)
                    {
                        bool snapEndPosition = true;
                        if (expandStart && textView.Contains(_start))
                        {
                            TextSegment lineRange = textView.GetLineRange(_start);
                            if (!lineRange.IsNull)
                            {
                                // Move start position to the beginning of containing line.
                                if (_start.CompareTo(lineRange.Start) != 0)
                                {
                                    _start = lineRange.Start.CreatePointer();
                                }
                                // If this line contains also end position, move it to the
                                // end of this line.
                                if (lineRange.Contains(_end))
                                {
                                    snapEndPosition = false;
                                    if (_end.CompareTo(lineRange.End) != 0)
                                    {
                                        _end = lineRange.End.CreatePointer();
                                    }
                                }
                            }
                        }
                        if (expandEnd && snapEndPosition && textView.Contains(_end))
                        {
                            TextSegment lineRange = textView.GetLineRange(_end);
                            if (!lineRange.IsNull)
                            {
                                // Move end position to the end of containing line.
                                if (_end.CompareTo(lineRange.End) != 0)
                                {
                                    _end = lineRange.End.CreatePointer();
                                }
                            }
                        }
                    }
                    break;

                case TextUnit.Paragraph:
                    // Utilize TextRange logic to determine paragraph boundaries.
                    ITextRange textRange = new TextRange(_start, _end);
                    TextRangeBase.SelectParagraph(textRange, _start);
                    if (expandStart && _start.CompareTo(textRange.Start) != 0)
                    {
                        _start = textRange.Start.CreatePointer();
                    }
                    if (expandEnd)
                    {
                        if (!textRange.Contains(_end))
                        {
                            TextRangeBase.SelectParagraph(textRange, _end);
                        }
                        if (_end.CompareTo(textRange.End) != 0)
                        {
                            _end = textRange.End.CreatePointer();
                        }
                    }
                    break;

                case TextUnit.Page:
                    // Positions are snapped to nearest page boundaries. But since page information
                    // is based on the layout, positions are not changed, if they are not currently in the view.
                    // We need to consider 2 types of scenarios: single page and multi-page.
                    // In case of multi-page scenario, first need to find a page associated with the position.
                    // If page is found, move the start position to the beginning of the first range of that page
                    // and move the end position to the end of the last range of that page.
                    textView = _textAdaptor.GetUpdatedTextView();
                    if (textView != null && textView.IsValid)
                    {
                        if (expandStart && textView.Contains(_start))
                        {
                            ITextView pageTextView = textView;
                            if (textView is MultiPageTextView)
                            {
                                // This is "multi page" case. Find page associated with the start position.
                                pageTextView = ((MultiPageTextView)textView).GetPageTextViewFromPosition(_start);
                            }
                            ReadOnlyCollection<TextSegment> textSegments = pageTextView.TextSegments;
                            if (textSegments != null && textSegments.Count > 0)
                            {
                                if (_start.CompareTo(textSegments[0].Start) != 0)
                                {
                                    _start = textSegments[0].Start.CreatePointer();
                                }
                            }
                        }
                        if (expandEnd && textView.Contains(_end))
                        {
                            ITextView pageTextView = textView;
                            if (textView is MultiPageTextView)
                            {
                                // This is "multi page" case. Find page associated with the start position.
                                pageTextView = ((MultiPageTextView)textView).GetPageTextViewFromPosition(_end);
                            }
                            ReadOnlyCollection<TextSegment> textSegments = pageTextView.TextSegments;
                            if (textSegments != null && textSegments.Count > 0)
                            {
                                if (_end.CompareTo(textSegments[textSegments.Count - 1].End) != 0)
                                {
                                    _end = textSegments[textSegments.Count - 1].End.CreatePointer();
                                }
                            }
                        }
                    }
                    break;

                case TextUnit.Document:
                    if (expandStart && _start.CompareTo(_start.TextContainer.Start) != 0)
                    {
                        _start = _start.TextContainer.Start.CreatePointer();
                    }
                    if (expandEnd && _end.CompareTo(_start.TextContainer.End) != 0)
                    {
                        _end = _start.TextContainer.End.CreatePointer();
                    }
                    break;

                default:
                    // Unknown unit
                    break;
            }
        }

        /// <summary>
        /// Moves the position to the closes unit boundary.
        /// </summary>
        private bool MoveToUnitBoundary(ITextPointer position, bool isStart, LogicalDirection direction, TextUnit unit)
        {
            bool moved = false;
            ITextView textView;
            switch (unit)
            {
                case TextUnit.Character:
                    if (!TextPointerBase.IsAtInsertionPosition(position))
                    {
                        if (TextPointerBase.MoveToNextInsertionPosition(position, direction))
                        {
                            moved = true;
                        }
                    }
                    break;

                case TextUnit.Word:
                    if (!IsAtWordBoundary(position))
                    {
                        if (MoveToNextWordBoundary(position, direction))
                        {
                            moved = true;
                        }
                    }
                    break;

                case TextUnit.Format:
                    // Formatting changes can be introduced by elements. Hence it is fair to 
                    // assume that formatting boundaries are defined by non-text context.
                    while (position.GetPointerContext(direction) == TextPointerContext.Text)
                    {
                        if (position.MoveToNextContextPosition(direction))
                        {
                            moved = true;
                        }
                    }
                    // Make sure we end with text on the right, so that later ExpandToEnclosingUnit calls
                    // do the right thing.
                    if (moved && direction == LogicalDirection.Forward)
                    {
                        while (true)
                        {
                            TextPointerContext context = position.GetPointerContext(LogicalDirection.Forward);

                            if (context != TextPointerContext.ElementStart && context != TextPointerContext.ElementEnd)
                                break;

                            position.MoveToNextContextPosition(LogicalDirection.Forward);
                        }
                    }
                    break;

                case TextUnit.Line:
                    // Positions are snapped to closest line boundaries. But since line information
                    // is based on the layout, positions are not changed, if:
                    // a) they are not currently in the view, or
                    // b) containing line cannot be found.
                    textView = _textAdaptor.GetUpdatedTextView();
                    if (textView != null && textView.IsValid && textView.Contains(position))
                    {
                        TextSegment lineRange = textView.GetLineRange(position);
                        if (!lineRange.IsNull)
                        {
                            double newSuggestedX;
                            int linesMoved = 0;

                            if (direction == LogicalDirection.Forward)
                            {
                                ITextPointer nextLineStart = null;

                                if (isStart)
                                {
                                    nextLineStart = textView.GetPositionAtNextLine(lineRange.End, Double.NaN, 1, out newSuggestedX, out linesMoved);
                                }

                                if (linesMoved != 0)
                                {
                                    lineRange = textView.GetLineRange(nextLineStart);
                                    nextLineStart = lineRange.Start;
                                }
                                else
                                {
                                    nextLineStart = lineRange.End;
                                }
                                nextLineStart = GetInsertionPosition(nextLineStart, LogicalDirection.Forward);

                                if (position.CompareTo(nextLineStart) != 0)
                                {
                                    position.MoveToPosition(nextLineStart);
                                    position.SetLogicalDirection(isStart ? LogicalDirection.Forward : LogicalDirection.Backward);
                                    moved = true;
                                }
                            }
                            else
                            {
                                ITextPointer previousLineEnd = null;

                                if (!isStart)
                                {
                                    previousLineEnd = textView.GetPositionAtNextLine(lineRange.Start, Double.NaN, -1, out newSuggestedX, out linesMoved);
                                }

                                if (linesMoved != 0)
                                {
                                    lineRange = textView.GetLineRange(previousLineEnd);
                                    previousLineEnd = lineRange.End;
                                }
                                else
                                {
                                    previousLineEnd = lineRange.Start;
                                }
                                previousLineEnd = GetInsertionPosition(previousLineEnd, LogicalDirection.Backward);

                                if (position.CompareTo(previousLineEnd) != 0)
                                {
                                    position.MoveToPosition(previousLineEnd);
                                    position.SetLogicalDirection(isStart ? LogicalDirection.Forward : LogicalDirection.Backward);
                                    moved = true;
                                }
                            }
                        }
                    }
                    break;

                case TextUnit.Paragraph:
                    // Utilize TextRange logic to determine paragraph boundaries.
                    ITextRange textRange = new TextRange(position, position);
                    TextRangeBase.SelectParagraph(textRange, position);

                    if (direction == LogicalDirection.Forward)
                    {
                        ITextPointer nextParagraphStart = textRange.End;

                        if (isStart)
                        {
                            nextParagraphStart = nextParagraphStart.CreatePointer();
                            if (nextParagraphStart.MoveToNextInsertionPosition(LogicalDirection.Forward))
                            {
                                TextRangeBase.SelectParagraph(textRange, nextParagraphStart);
                                nextParagraphStart = textRange.Start;
                            }
                        }

                        if (position.CompareTo(nextParagraphStart) != 0)
                        {
                            position.MoveToPosition(nextParagraphStart);
                            position.SetLogicalDirection(isStart ? LogicalDirection.Forward : LogicalDirection.Backward);
                            moved = true;
                        }
                    }
                    else
                    {
                        ITextPointer previousParagraphEnd = textRange.Start;

                        if (!isStart)
                        {
                            previousParagraphEnd = previousParagraphEnd.CreatePointer();
                            if (previousParagraphEnd.MoveToNextInsertionPosition(LogicalDirection.Backward))
                            {
                                TextRangeBase.SelectParagraph(textRange, previousParagraphEnd);
                                previousParagraphEnd = textRange.End;
                            }
                        }

                        if (position.CompareTo(previousParagraphEnd) != 0)
                        {
                            position.MoveToPosition(previousParagraphEnd);
                            position.SetLogicalDirection(isStart ? LogicalDirection.Forward : LogicalDirection.Backward);
                            moved = true;
                        }
                    }
                    break;

                case TextUnit.Page:
                    // Positions are snapped to nearest page boundaries. But since page information
                    // is based on the layout, positions are not changed, if they are not currently in the view.
                    // We need to consider 2 types of scenarios: single page and multi-page.
                    // In case of multi-page scenario, first need to find a page associated with the position.
                    // If page is found, move the start position to the beginning of the first range of that page
                    // and move the end position to the end of the last range of that page.
                    textView = _textAdaptor.GetUpdatedTextView();
                    if (textView != null && textView.IsValid && textView.Contains(position))
                    {
                        ITextView pageTextView = textView;
                        if (textView is MultiPageTextView)
                        {
                            // This is "multi page" case. Find page associated with the start position.
                            pageTextView = ((MultiPageTextView)textView).GetPageTextViewFromPosition(position);
                        }
                        ReadOnlyCollection<TextSegment> textSegments = pageTextView.TextSegments;

                        if (textSegments != null && textSegments.Count > 0)
                        {
                            //When comparing, we need to take into account if the pointer is not right at 
                            //the end of the page (or beginning) because of normalization
                            
                            if (direction == LogicalDirection.Forward)
                            {
                                while (position.CompareTo(textSegments[textSegments.Count - 1].End) != 0)
                                {
                                    if (position.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.ElementEnd)
                                    {
                                        position.MoveToPosition(textSegments[textSegments.Count - 1].End);
                                        moved = true;
                                        break;
                                    }
                                    Invariant.Assert(position.MoveToNextContextPosition(LogicalDirection.Forward));
                                }
                                MoveToInsertionPosition(position, LogicalDirection.Forward);
                            }
                            else
                            {
                                while (position.CompareTo(textSegments[0].Start) != 0)
                                {
                                    if (position.GetPointerContext(LogicalDirection.Backward) != TextPointerContext.ElementStart)
                                    {
                                        position.MoveToPosition(textSegments[0].Start);
                                        moved = true;
                                        break;
                                    }
                                    Invariant.Assert(position.MoveToNextContextPosition(LogicalDirection.Backward));
                                }
                                MoveToInsertionPosition(position, LogicalDirection.Backward);
                            }
                        }
                    }
                    break;

                case TextUnit.Document:
                    if (direction == LogicalDirection.Forward)
                    {
                        if (position.CompareTo(GetInsertionPosition(position.TextContainer.End, LogicalDirection.Backward)) != 0)
                        {
                            position.MoveToPosition(position.TextContainer.End);
                            moved = true;
                        }
                    }
                    else
                    {
                        if (position.CompareTo(GetInsertionPosition(position.TextContainer.Start, LogicalDirection.Forward)) != 0)
                        {
                            position.MoveToPosition(position.TextContainer.Start);
                            moved = true;
                        }
                    }
                    break;

                default:
                    // Unknown unit
                    break;
            }
            return moved;
        }

        /// <summary>
        /// Re-positions the given position by an integral number of text units, but it does
        /// not guarantee that position is snapped to TextUnit boundary.
        /// This method assumes that input position is already snapped to appropriate TextUnit boundary.
        /// </summary>
        /// <param name="position">The position to move</param>
        /// <param name="unit">Text units to step by</param>
        /// <param name="count">Number of units to step over. Also specifies the direction of moving: 
        /// forward if positive, backward otherwise</param>
        /// <returns>The actual number of units the position was moved over</returns>
        private int MovePositionByUnits(ITextPointer position, TextUnit unit, int count)
        {
            ITextView textView;
            int moved = 0;
            int absCount = (count == int.MinValue) ? int.MaxValue : Math.Abs(count);
            LogicalDirection direction = (count > 0) ? LogicalDirection.Forward : LogicalDirection.Backward;

            // This method assumes that position is already snapped to appropriate TextUnit.

            switch (unit)
            {
                case TextUnit.Character:
                    while (moved < absCount)
                    {
                        if (!TextPointerBase.MoveToNextInsertionPosition(position, direction))
                        {
                            break;
                        }
                        moved++;
                    }
                    break;

                case TextUnit.Word:
                    while (moved < absCount)
                    {
                        if (!MoveToNextWordBoundary(position, direction))
                        {
                            break;
                        }
                        moved++;
                    }
                    break;

                case TextUnit.Format:
                    // Formatting changes can be introduced by elements. Hence it is fair to 
                    // assume that formatting boundaries are defined by non-text context.
                    while (moved < absCount)
                    {
                        ITextPointer positionOrig = position.CreatePointer();

                        // First skip all text in given direction.
                        while (position.GetPointerContext(direction) == TextPointerContext.Text)
                        {
                            if (!position.MoveToNextContextPosition(direction))
                            {
                                break;
                            }
                        }
                        // Move to next context
                        if (!position.MoveToNextContextPosition(direction))
                        {
                            break;
                        }
                        // Skip all formatting elements and position the pointer next to text.
                        while (position.GetPointerContext(direction) != TextPointerContext.Text)
                        {
                            if (!position.MoveToNextContextPosition(direction))
                            {
                                break;
                            }
                        }
                        // If moving backwards, position the pointer at the beginning of formatting range.
                        if (direction == LogicalDirection.Backward)
                        {
                            while (position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.Text)
                            {
                                if (!position.MoveToNextContextPosition(LogicalDirection.Backward))
                                {
                                    break;
                                }
                            }
                        }
                        if (position.GetPointerContext(direction) != TextPointerContext.None)
                        {
                            moved++;
                        }
                        else
                        {
                            position.MoveToPosition(positionOrig);
                            break;
                        }
                    }

                    // Adjust logical direction to point to the following text (forward or backward movement).
                    // If we don't do this, we'll normalize in the wrong direction and get stuck in a loop
                    // if caller tries to advance again.
                    position.SetLogicalDirection(LogicalDirection.Forward);

                    break;

                case TextUnit.Line:
                    // Position is snapped to nearest line boundary. But since line information
                    // is based on the layout, position is not changed, if:
                    // a) it is not currently in the view, or
                    // b) containing line cannot be found.
                    textView = _textAdaptor.GetUpdatedTextView();
                    if (textView != null && textView.IsValid && textView.Contains(position))
                    {
                        // ITextPointer.MoveToLineBoundary can't handle Table row end positions.
                        // Mimic TextEditor's caret navigation code and move into the preceding
                        // TableCell.
                        if (TextPointerBase.IsAtRowEnd(position))
                        {
                            position.MoveToNextInsertionPosition(LogicalDirection.Backward);
                        }

                        moved = position.MoveToLineBoundary(count);
                        
                        MoveToInsertionPosition(position, LogicalDirection.Forward);

                        if (moved < 0)
                        {
                            moved = -moved; // Will be reversed below.
                        }
                    }
                    break; 

                case TextUnit.Paragraph:
                    // Utilize TextRange logic to determine paragraph boundaries.
                    ITextRange paragraphRange = new TextRange(position, position);
                    paragraphRange.SelectParagraph(position);
                    while (moved < absCount)
                    {
                        position.MoveToPosition(direction == LogicalDirection.Forward ? paragraphRange.End : paragraphRange.Start);
                        if (!position.MoveToNextInsertionPosition(direction))
                        {
                            break;
                        }
                        moved++;
                        paragraphRange.SelectParagraph(position);
                        position.MoveToPosition(paragraphRange.Start); // Position it always at the beginning of the paragraph.
                    }
                    break;

                case TextUnit.Page:
                    // But since page information is based on the layout, position is not changed, if:
                    // a) it is not currently in the view, or
                    // b) containing page cannot be found.
                    // Page movement is possible only in multi-page scenario.
                    textView = _textAdaptor.GetUpdatedTextView();
                    if (textView != null && textView.IsValid && textView.Contains(position))
                    {
                        if (textView is MultiPageTextView)
                        {
                            // Get embedded page ITextView for given position.
                            ITextView pageTextView = ((MultiPageTextView)textView).GetPageTextViewFromPosition(position);
                            ReadOnlyCollection<TextSegment> textSegments = pageTextView.TextSegments;
                            while (moved < absCount)
                            {
                                if (textSegments == null || textSegments.Count == 0)
                                {
                                    break;
                                }
                                // Move the position to appropriate edge.
                                if (direction == LogicalDirection.Backward)
                                {
                                    position.MoveToPosition(textSegments[0].Start);
                                    MoveToInsertionPosition(position, LogicalDirection.Backward);
                                }
                                else
                                {
                                    position.MoveToPosition(textSegments[textSegments.Count - 1].End);
                                    MoveToInsertionPosition(position, LogicalDirection.Forward);
                                }
                                // Try to move the position to the next page.
                                ITextPointer positionTemp = position.CreatePointer();
                                if (!positionTemp.MoveToNextInsertionPosition(direction))
                                {
                                    break;
                                }
                                else
                                {
                                    // MoveToNextInsertionPosition may return 'true' and move the position
                                    // in oposite direction.
                                    if (direction == LogicalDirection.Forward)
                                    {
                                        if (positionTemp.CompareTo(position) <= 0)
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if (positionTemp.CompareTo(position) >= 0)
                                        {
                                            break;
                                        }
                                    }
                                }
                                // Get embedded page ITextView for given position.
                                if (!textView.Contains(positionTemp))
                                {
                                    break;
                                }
                                pageTextView = ((MultiPageTextView)textView).GetPageTextViewFromPosition(positionTemp);
                                textSegments = pageTextView.TextSegments;
                                moved++;
                            }
                        }
                    }
                    break;

                case TextUnit.Document:
                    // This method assumes that position is already snapped to appropriate TextUnit.
                    break;
            }

            return (direction == LogicalDirection.Forward) ? moved : -moved;
        }

        /// <summary>
        /// Finds the value of a given attribute within the range.
        /// </summary>
        private object GetAttributeValue(TextAttributeHelper attr)
        {
            ITextPointer start = _start.CreatePointer();
            ITextPointer end = _end.CreatePointer();

            if (start.CompareTo(end) < 0)
            {
                // If we get plunked just outside an element we need to move ourselves inside
                // before we check our start and end values.
                while (IsElementBoundary(start.GetPointerContext(LogicalDirection.Forward)))
                {
                    if (!start.MoveToNextContextPosition(LogicalDirection.Forward) || start.CompareTo(end) >= 0)
                    {
                        break;
                    }
                }
                while (IsElementBoundary(end.GetPointerContext(LogicalDirection.Backward)))
                {
                    if (!end.MoveToNextContextPosition(LogicalDirection.Backward) || start.CompareTo(end) >= 0)
                    {
                        break;
                    }
                }
                if (start.CompareTo(end) > 0)
                {
                    return AutomationElementIdentifiers.NotSupported;
                }
            }

            // Get value at range end and check if it ever changes through the range.
            object valueAtEndPos = attr.GetValueAt(end);
            while (start.CompareTo(end) < 0 && attr.AreEqual(valueAtEndPos, attr.GetValueAt(start)))
            {
                if (!start.MoveToNextContextPosition(LogicalDirection.Forward) || start.CompareTo(end) > 0)
                {
                    break;
                }
            }

            return (start.CompareTo(end) >= 0) ? valueAtEndPos : TextPatternIdentifiers.MixedAttributeValue;
        }

        /// <summary>
        /// Checks whether symbol type refers to element boundary.
        /// </summary>
        private bool IsElementBoundary(TextPointerContext symbolType)
        {
            return ((symbolType == TextPointerContext.ElementStart) || (symbolType == TextPointerContext.ElementEnd));
        }

        /// <summary>
        /// Converts Brush to Color
        /// </summary>
        private static int ColorFromBrush(object brush)
        {
            SolidColorBrush solidBrush = brush as SolidColorBrush;
            Color color = (solidBrush != null) ? solidBrush.Color : Colors.Black;
            return (0 + (color.R << 16) + (color.G << 8) + color.B);
        }

        /// <summary>
        /// Retrieves FontFamily name.
        /// </summary>
        private static string GetFontFamilyName(FontFamily fontFamily, ITextPointer context)
        {
            if (fontFamily != null)
            {
                // Typical case: return the family name/URI used to construct the FontFamily.
                if (fontFamily.Source != null)
                    return fontFamily.Source;

                // Use the target font specified by the first family map with a compatible language.
                if (fontFamily.FamilyMaps != null)
                {
                    XmlLanguage textLanguage = (context != null) ?
                        (XmlLanguage)context.GetValue(FrameworkElement.LanguageProperty) :
                        null;

                    foreach (FontFamilyMap familyMap in fontFamily.FamilyMaps)
                    {
                        // A language-neutral family map matches any text language.
                        if (familyMap.Language == null)
                            return familyMap.Target;

                        // Does the language match the text culture or a parent culture?
                        if (textLanguage != null && familyMap.Language.RangeIncludes(textLanguage))
                            return familyMap.Target;
                    }
                }
            }

            // Worst case: we have to return something so just return a default family name.
            return _defaultFamilyName;
        }

        /// <summary>
        /// Retrieves TextDecoration color.
        /// </summary>
        private static int GetTextDecorationColor(TextDecorationCollection decorations, TextDecorationLocation location)
        {
            if (decorations == null)
            {
                return 0;
            }

            int color = 0;
            foreach (TextDecoration decor in decorations)
            {
                if (decor.Location == location)
                {
                    if (decor.Pen != null)
                    {
                        color = ColorFromBrush(decor.Pen.Brush);
                        // Ignore other decorations and their coloring if there're more at the same location.
                        break;
                    }
                }
            }
            return color;
        }

        /// <summary>
        /// Retrieves TextDecoration style.
        /// </summary>
        private static TextDecorationLineStyle GetTextDecorationLineStyle(TextDecorationCollection decorations, TextDecorationLocation location)
        {
            if (decorations == null)
            {
                return TextDecorationLineStyle.None;
            }

            TextDecorationLineStyle lineStyle = TextDecorationLineStyle.None;
            foreach (TextDecoration decor in decorations)
            {
                if (decor.Location == location)
                {
                    if (lineStyle == TextDecorationLineStyle.None)
                    {
                        // There's a whole bunch of all kinds of custom styles defined in TextDecorationLineStyle 
                        // including WordsOnly, Double, Wavy, DoubleWavy, ThickWavy, which we can not determine from 
                        // TextDecoration anyway. Hence, it seems would be too much bang for a buck if we try 
                        // to guess out the other dozen by analyzing TextDecoration.Pen. Let's keep it simple 
                        // and make difference only between solid and dashed lines. 
                        if (decor.Pen != null)
                        {
                            lineStyle = (decor.Pen.DashStyle.Dashes.Count > 1) ? TextDecorationLineStyle.Dash : TextDecorationLineStyle.Single;
                        }
                        else
                        {
                            lineStyle = TextDecorationLineStyle.Single;
                        }
                    }
                    else
                    {
                        lineStyle = TextDecorationLineStyle.Other;
                        break;
                    }
                }
            }
            return lineStyle;
        }

        /// <summary>
        /// Converts Avalon units to points
        /// </summary>
        private static double NativeObjectLengthToPoints(double length)
        {
            return (DoubleUtil.IsNaN(length) ? 0d : (length * 72.0 / 96.0));
        }

        /// <summary>
        /// Retrieves AutomationPeer enclosing entire range.
        /// Also makes sure automation tree is properly created.
        /// </summary>
        /// <returns></returns>
        private AutomationPeer GetEnclosingAutomationPeer(ITextPointer start, ITextPointer end)
        {
            // Retrieve element enclosing the entire range.
            ITextPointer elementStart, elementEnd;
            AutomationPeer peer = TextContainerHelper.GetEnclosingAutomationPeer(start, end, out elementStart, out elementEnd);

            // If no AutomationPeer is found, assume the owner of TextPattern.
            // Otherwise make sure that the AutomationPeer is properly connected
            // through automation tree.
            if (peer == null)
            {
                peer = _textPeer;
            }
            else
            {
                Invariant.Assert(elementStart != null && elementEnd != null);
                AutomationPeer peerParent = GetEnclosingAutomationPeer(elementStart, elementEnd);
                GetAutomationPeersFromRange(peerParent, elementStart, elementEnd);
            }
            return peer;
        }

        /// <summary>
        /// Retrieves automation provider from AutomationPeer.
        /// </summary>
        private IRawElementProviderSimple ProviderFromPeer(AutomationPeer peer)
        {
            IRawElementProviderSimple provider;
            if (_textPeer is TextAutomationPeer)
            {
                provider = ((TextAutomationPeer)_textPeer).ProviderFromPeer(peer);
            }
            else
            {
                provider = ((ContentTextAutomationPeer)_textPeer).ProviderFromPeer(peer);
            }
            return provider;
        }

        /// <summary>
        /// Retrieves AutomationPeers from specified range.
        /// </summary>
        private List<AutomationPeer> GetAutomationPeersFromRange(AutomationPeer peer, ITextPointer start, ITextPointer end)
        {
            List<AutomationPeer> peers;
            Invariant.Assert(peer is TextAutomationPeer || peer is ContentTextAutomationPeer);
            if (peer is TextAutomationPeer)
            {
                peers = ((TextAutomationPeer)peer).GetAutomationPeersFromRange(start, end);
            }
            else
            {
                peers = ((ContentTextAutomationPeer)peer).GetAutomationPeersFromRange(start, end);
            }
            return peers;
        }

        /// <summary>
        /// Helper function to check if given position is at word boundary. TextPointerBase.IsAtWordBoundary 
        /// cannot be used for not normalized positions, because it will always return TRUE, but in fact such 
        /// position is not in world boundary.
        /// </summary>
        private static bool IsAtWordBoundary(ITextPointer position)
        {
            if (!TextPointerBase.IsAtInsertionPosition(position))
            {
                return false;
            }
            // Note that we always use Forward direction for word orientation.
            return TextPointerBase.IsAtWordBoundary(position, LogicalDirection.Forward);
        }

        /// <summary>
        /// Helper function to move given position to word boundary. TextPointerBase.MoveToNextWordBoundary 
        /// cannot be used directly, because it does not modify LogicalDirection. Because of that, IsAtWordBoundary
        /// for just moved positions may return FALSE.
        /// </summary>
        private static bool MoveToNextWordBoundary(ITextPointer position, LogicalDirection direction)
        {
            int moveCounter = 0;
            ITextPointer startPosition = position.CreatePointer();

            // Move the position in the given direction until word boundary is reached.
            while (position.MoveToNextInsertionPosition(direction))
            {
                moveCounter++;
                if (IsAtWordBoundary(position))
                {
                    break;
                }
                // Need to break the loop for weird case when there is no word break in text content.
                // When the word looks too long, consider end of textRun as a word break.
                if (moveCounter > 128) // 128 was taken as a random number. Probably not big enough though...
                {
                    position.MoveToPosition(startPosition);
                    position.MoveToNextContextPosition(direction);
                    break;
                }
            }

            // Note that we always use Forward direction for word orientation.
            if (moveCounter > 0)
            {
                position.SetLogicalDirection(LogicalDirection.Forward);
            }
            return moveCounter > 0;
        }

        /// <summary>
        /// Ensures that the start and end points of this range are at valid insertion (caret)
        /// positions.
        /// </summary>
        private void Normalize()
        {
            MoveToInsertionPosition(_start, _start.LogicalDirection);
            MoveToInsertionPosition(_end, _end.LogicalDirection);
            
            // If start passes end, move the entire range to the start position.
            if (_start.CompareTo(_end) > 0)
            {
                _end.MoveToPosition(_start);
            }
        }

        /// <summary>
        /// Returns a normalized copy of a position -- the position at the closest legal
        /// caret position in a given direction.
        /// </summary>
        private ITextPointer GetInsertionPosition(ITextPointer position, LogicalDirection direction)
        {
            position = position.CreatePointer();
            MoveToInsertionPosition(position, direction);

            return position;
        }


        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private ITextPointer _start;
        private ITextPointer _end;
        private TextAdaptor _textAdaptor;
        private AutomationPeer _textPeer;

        private static Hashtable _textPatternAttributes;
        private const string _defaultFamilyName = "Global User Interface";

        #endregion Private Fields

        //-------------------------------------------------------------------
        //
        //  Private Types
        //
        //-------------------------------------------------------------------

        #region Private Types

        /// <summary>
        /// 
        /// </summary>
        private class TextAttributeHelper
        {
            internal delegate object GetValueAtDelegate(ITextPointer textPointer);
            internal delegate bool AreEqualDelegate(object val1, object val2);

            internal TextAttributeHelper(GetValueAtDelegate getValueDelegate, AreEqualDelegate areEqualDelegate)
            {
                _getValueDelegate = getValueDelegate;
                _areEqualDelegate = areEqualDelegate;
            }

            internal GetValueAtDelegate GetValueAt { get { return _getValueDelegate; } }
            internal AreEqualDelegate AreEqual { get { return _areEqualDelegate; } }

            private GetValueAtDelegate _getValueDelegate;
            private AreEqualDelegate _areEqualDelegate;
        }

        #endregion Private Types

        //-------------------------------------------------------------------
        //
        //  ITextRangeProvider
        //
        //-------------------------------------------------------------------

        #region ITextRangeProvider

        /// <summary>
        /// Retrieves a new range covering an identical span of text. The new range can be manipulated independently from the original.
        /// </summary>
        /// <returns>The new range.</returns>
        ITextRangeProvider ITextRangeProvider.Clone()
        {
            return new TextRangeAdaptor(_textAdaptor, _start, _end, _textPeer);
        }

        /// <summary>
        /// Compares this range with another range.
        /// </summary>
        /// <param name="range">A range to compare. 
        /// The range must have come from the same text provider or an InvalidArgumentException will be thrown.</param>
        /// <returns>true if both ranges span the same text.</returns>
        bool ITextRangeProvider.Compare(ITextRangeProvider range)
        {
            if (range == null)
            {
                throw new ArgumentNullException("range");
            }

            TextRangeAdaptor rangeAdaptor = ValidateAndThrow(range);

            Normalize();
            rangeAdaptor.Normalize();

            return (rangeAdaptor._start.CompareTo(_start) == 0 && rangeAdaptor._end.CompareTo(_end) == 0);
        }

        /// <summary>
        /// Compares the endpoint of this range with the endpoint of another range.
        /// </summary>
        /// <param name="endpoint">The endpoint of this range to compare.</param>
        /// <param name="targetRange">The range with the other endpoint to compare.
        /// The range must have come from the same text provider or an InvalidArgumentException will be thrown.</param>
        /// <param name="targetEndpoint">The endpoint on the other range to compare.</param>
        /// <returns>Returns &lt;0 if this endpoint occurs earlier in the text than the target endpoint. 
        /// Returns 0 if this endpoint is at the same location as the target endpoint. 
        /// Returns &gt;0 if this endpoint occurs later in the text than the target endpoint.</returns>
        int ITextRangeProvider.CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            if (targetRange == null)
            {
                throw new ArgumentNullException("targetRange");
            }

            TextRangeAdaptor rangeAdaptor = ValidateAndThrow(targetRange);

            Normalize();
            rangeAdaptor.Normalize();

            ITextPointer position = (endpoint == TextPatternRangeEndpoint.Start) ? _start : _end;
            ITextPointer targetPosition = (targetEndpoint == TextPatternRangeEndpoint.Start) ? rangeAdaptor._start : rangeAdaptor._end;
            return position.CompareTo(targetPosition);
        }

        /// <summary>
        /// Expands the range to an integral number of enclosing units.  this could be used, for example,
        /// to guarantee that a range endpoint is not in the middle of a word.  If the range is already an
        /// integral number of the specified units then it remains unchanged.
        /// </summary>
        /// <param name="unit">The textual unit.</param>
        void ITextRangeProvider.ExpandToEnclosingUnit(TextUnit unit)
        {
            Normalize();

            // TextPattern spec update: End EndPoint moves to the end of the same TextUnit that 
            // Start EndPoint is within (whether it was within that unit originally or not).
            // To support this scenario always move _end to the next position after _start.
            _end.MoveToPosition(_start);
            _end.MoveToNextInsertionPosition(LogicalDirection.Forward);

            ExpandToEnclosingUnit(unit, true, true);
        }

        /// <summary>
        /// Searches for a subrange of text that has the specified attribute.  To search the entire document use the text provider's
        /// document range.
        /// </summary>
        /// <param name="attributeId">The attribute to search for.</param>
        /// <param name="value">The value of the specified attribute to search for.</param>
        /// <param name="backward">true if the last occurring range should be returned instead of the first.</param>
        /// <returns>A subrange with the specified attribute, or null if no such subrange exists.</returns>
        ITextRangeProvider ITextRangeProvider.FindAttribute(int attributeId, object value, bool backward)
        {
            AutomationTextAttribute attribute = AutomationTextAttribute.LookupById(attributeId);
            if (attribute == null)
            {
                throw new ArgumentNullException("attributeId");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (!_textPatternAttributes.ContainsKey(attribute))
            {
                return null;
            }

            Normalize();

            ITextRangeProvider resultRange = null;
            ITextPointer attrStart = null;
            ITextPointer attrEnd = null;
            TextAttributeHelper attr = (TextAttributeHelper)_textPatternAttributes[attribute];
            if (backward)
            {
                ITextPointer stop = _start;
                ITextPointer position = _end.CreatePointer(LogicalDirection.Backward);

                // Go backward from the range end position until we find a position that 
                // has our attribute or we hit the start position of the search range.
                attrStart = stop;
                while (position.CompareTo(stop) > 0)
                {
                    if (attr.AreEqual(value, attr.GetValueAt(position)))
                    {
                        if (attrEnd == null)
                        {
                            attrEnd = position.CreatePointer(LogicalDirection.Backward);
                        }
                    }
                    else
                    {
                        if (attrEnd != null)
                        {
                            attrStart = position.CreatePointer(LogicalDirection.Forward);
                            break;
                        }
                    }
                    if (!position.MoveToNextContextPosition(LogicalDirection.Backward))
                    {
                        break;
                    }
                }
            }
            else
            {
                ITextPointer stop = _end;
                ITextPointer position = _start.CreatePointer(LogicalDirection.Forward);

                // Go backward from the range end position until we find a position that 
                // has our attribute or we hit the start position of the search range.
                attrEnd = stop;
                while (position.CompareTo(stop) < 0)
                {
                    if (attr.AreEqual(value, attr.GetValueAt(position)))
                    {
                        if (attrStart == null)
                        {
                            attrStart = position.CreatePointer(LogicalDirection.Forward);
                        }
                    }
                    else
                    {
                        if (attrStart != null)
                        {
                            attrEnd = position.CreatePointer(LogicalDirection.Backward);
                            break;
                        }
                    }
                    if (!position.MoveToNextContextPosition(LogicalDirection.Forward))
                    {
                        break;
                    }
                }
            }
            if (attrStart != null && attrEnd != null)
            {
                resultRange = new TextRangeAdaptor(_textAdaptor, attrStart, attrEnd, _textPeer);
            }

            return resultRange;
        }

        /// <summary>
        /// Searches for an occurrence of text within the range.
        /// </summary>
        /// <param name="text">The text to search for.</param>
        /// <param name="backward">true if the last occurring range should be returned instead of the first.</param>
        /// <param name="ignoreCase">true if case should be ignored for the purposes of comparison.</param>
        /// <returns>A subrange with the specified text, or null if no such subrange exists.</returns>
        ITextRangeProvider ITextRangeProvider.FindText(string text, bool backward, bool ignoreCase)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }
            if (text.Length == 0)
            {
                throw new ArgumentException(SR.Get(SRID.TextRangeProvider_EmptyStringParameter, "text"));
            }

            Normalize();

            if (_start.CompareTo(_end) == 0)
            {
                return null;
            }

            TextRangeAdaptor range = null;
            FindFlags findFlags = FindFlags.None;
            if (!ignoreCase)
            {
                findFlags |= FindFlags.MatchCase;
            }
            if (backward)
            {
                findFlags |= FindFlags.FindInReverse;
            }
            ITextRange findResult = TextFindEngine.Find(_start, _end, text, findFlags, CultureInfo.CurrentCulture);
            if (findResult != null && !findResult.IsEmpty)
            {
                range = new TextRangeAdaptor(_textAdaptor, findResult.Start, findResult.End, _textPeer);
            }

            return range;
        }

        /// <summary>
        /// Retrieves the value of a text attribute over the entire range.
        /// </summary>
        /// <param name="attributeId">The text attribute.</param>
        /// <returns>The value of the attribute across the range. 
        /// If the attribute's value varies over the range then the value is TextPattern.MixedAttributeValue</returns>
        object ITextRangeProvider.GetAttributeValue(int attributeId)
        {
            AutomationTextAttribute attribute = AutomationTextAttribute.LookupById(attributeId);

            
            // In Windows 8, a new text attribute was introduced that WPF does not have any reference for
            // this caused WPF to throw an ArgumentException.  This code path was strange as we already can
            // return NotSupported, which is a valid response.  So change this to no longer throw.
            if (attribute == null || !_textPatternAttributes.ContainsKey(attribute))
            {
                return AutomationElementIdentifiers.NotSupported;
            }

            Normalize();

            return GetAttributeValue((TextAttributeHelper)_textPatternAttributes[attribute]);
        }

        /// <summary>
        /// Retrieves the bounding rectangles for viewable lines of the range.
        /// </summary>
        /// <returns>An array of bounding rectangles for each line or portion of a line within the client area of the text provider.
        /// No bounding rectangles will be returned for lines that are empty or scrolled out of view.  Note that even though a
        /// bounding rectangle is returned the corresponding text may not be visible due to overlapping windows.
        /// This will not return null, but may return an empty array.</returns>
        double[] ITextRangeProvider.GetBoundingRectangles()
        {
            Normalize();

            Rect[] rects = _textAdaptor.GetBoundingRectangles(_start, _end, true, true);
            double[] asDoubles = new double[rects.Length * 4];
            for (int i = 0; i < rects.Length; i++)
            {
                asDoubles[4 * i] = rects[i].X;
                asDoubles[4 * i + 1] = rects[i].Y;
                asDoubles[4 * i + 2] = rects[i].Width;
                asDoubles[4 * i + 3] = rects[i].Height;
            }
            return asDoubles;
        }

        /// <summary>
        /// Retrieves the innermost element that encloses this range.
        /// </summary>
        /// <returns>An element.  Usually this element will be the one that supplied this range.
        /// However, if the text provider supports child elements such as tables or hyperlinks, then the
        /// enclosing element could be a descendant element of the text provider.
        /// </returns>
        IRawElementProviderSimple ITextRangeProvider.GetEnclosingElement()
        {
            Normalize();

            AutomationPeer peer = GetEnclosingAutomationPeer(_start, _end);
            Invariant.Assert(peer != null);
            IRawElementProviderSimple provider = ProviderFromPeer(peer);
            Invariant.Assert(provider != null);
            return provider;
        }

        /// <summary>
        /// Retrieves the text of the range.
        /// </summary>
        /// <param name="maxLength">Specifies the maximum length of the string to return or -1 if no limit is requested.</param>
        /// <returns>The text of the range possibly truncated to the specified limit.</returns>
        string ITextRangeProvider.GetText(int maxLength)
        {
            if (maxLength < 0 && maxLength != -1)
            {
                throw new ArgumentException(SR.Get(SRID.TextRangeProvider_InvalidParameterValue, maxLength, "maxLength"));
            }

            Normalize();

            string text = TextRangeBase.GetTextInternal(_start, _end);
            return (text.Length <= maxLength || maxLength == -1) ? text : text.Substring(0, maxLength);
        }

        /// <summary>
        /// Moves the range the specified number of units in the text.  Note that the text is not altered.  Instead the
        /// range spans a different part of the text.
        /// If the range is degenerate, this method tries to move the insertion point count units.  If the range is nondegenerate 
        /// and count is greater than zero, this method collapses the range at its end point, moves the resulting range forward 
        /// to a unit boundary (if it is not already at one), and then tries to move count - 1 units forward. If the range is 
        /// nondegenerate and count is less than zero, this method collapses the range at the starting point, moves the resulting 
        /// range backward to a unit boundary (if it isn't already at one), and then tries to move |count| - 1 units backward. 
        /// Thus, in both cases, collapsing a nondegenerate range, whether or not moving to the start or end of the unit following 
        /// the collapse, counts as a unit.
        /// </summary>
        /// <param name="unit">The textual unit for moving.</param>
        /// <param name="count">The number of units to move.  A positive count moves the range forward.  
        /// A negative count moves backward. A count of 0 has no effect.</param>
        /// <returns>The number of units actually moved, which can be less than the number requested if 
        /// moving the range runs into the beginning or end of the document.</returns>
        int ITextRangeProvider.Move(TextUnit unit, int count)
        {
            Normalize();

            int movedCount = 0;
            // Do not expand range for Paragraphs, because TextRange.SelectParagraph will take care of it.
            if (unit != TextUnit.Paragraph)
            {
                ExpandToEnclosingUnit(unit, true, true);
            }
            if (count != 0)
            {
                // Move start position by number of units.
                ITextPointer position = _start.CreatePointer();
                movedCount = MovePositionByUnits(position, unit, count);

                // If endpoint has been moved at least by one unit or its direction has changed, snap it to TextUnit boundary,
                // because movement done by MovePositionByUnits does not guarantee position snapping.
                if ((position.CompareTo(_start)==0 && position.LogicalDirection != _start.LogicalDirection) ||
                    (count > 0 && position.CompareTo(_start) > 0) ||
                    (count < 0 && position.CompareTo(_start) < 0))
                {
                    _start = position;

                    // Move end position by 1 offset forward, so it does not point to _start.
                    // Later ExpandToEnclosingUnit will position it at appropriate unit boundary.
                    _end = position.CreatePointer();
                    if (unit != TextUnit.Page)
                    {
                        _end.MoveToNextInsertionPosition(LogicalDirection.Forward);
                    }
                    
                    ExpandToEnclosingUnit(unit, true, true);
                    // If endpoint has been moved, but 'movedCount' is 0, it means that we snapped to neariest
                    // unit boundary. Treat this situation as actual move.
                    if (movedCount == 0)
                    {
                        movedCount = (count > 0) ? 1 : -1;
                    }
                }
            }
            return movedCount;
        }

        /// <summary>
        /// Moves one endpoint of the range the specified number of units in the text.
        /// If the endpoint being moved crosses the other endpoint then the other endpoint
        /// is moved along too resulting in a degenerate range and ensuring the correct ordering
        /// of the endpoints. (i.e. always Start&lt;=End)
        /// </summary>
        /// <param name="endpoint">The endpoint to move.</param>
        /// <param name="unit">The textual unit for moving.</param>
        /// <param name="count">The number of units to move.  A positive count moves the endpoint forward.  
        /// A negative count moves backward. A count of 0 has no effect.</param>
        /// <returns>The number of units actually moved, which can be less than the number requested if 
        /// moving the endpoint runs into the beginning or end of the document.</returns>
        int ITextRangeProvider.MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
        {
            Normalize();

            int movedCount = 0;
            if (count != 0)
            {
                // Move endpoint by number of units.
                bool start = (endpoint == TextPatternRangeEndpoint.Start);
                ITextPointer positionRef = start ? _start : _end;
                ITextPointer position = positionRef.CreatePointer();
                if (MoveToUnitBoundary(position, start, count < 0 ? LogicalDirection.Backward : LogicalDirection.Forward, unit))
                {
                    movedCount = (count > 0) ? 1 : -1;
                }
                if (count != movedCount)
                {
                    movedCount += MovePositionByUnits(position, unit, count - movedCount);
                }

                // If endpoint has been moved at least by one unit, snap it to TextUnit boundary,
                // because movement done by MovePositionByUnits does not guarantee position snapping.
                if ((count > 0 && position.CompareTo(positionRef) > 0) || 
                    (count < 0 && position.CompareTo(positionRef) < 0) ||
                    (position.CompareTo(positionRef) == 0 && position.LogicalDirection != positionRef.LogicalDirection))
                {
                    if (start)
                    {
                        _start = position;
                    }
                    else
                    {
                        _end = position;
                    }
                    if (unit != TextUnit.Page)
                    {
                        ExpandToEnclosingUnit(unit, start, !start);
                    }
                    // If endpoint has been moved, but 'movedCount' is 0, it means that we snapped to neariest
                    // unit boundary. Treat this situation as actual move.
                    if (movedCount == 0)
                    {
                        movedCount = (count > 0) ? 1 : -1;
                    }
                }
                // Ensure the correct ordering of the endpoint.
                if (_start.CompareTo(_end) > 0)
                {
                    if (start)
                    {
                        _end = _start.CreatePointer();
                    }
                    else
                    {
                        _start = _end.CreatePointer();
                    }
                }
            }
            return movedCount;
        }

        /// <summary>
        /// Moves an endpoint of this range to coincide with the endpoint of another range.
        /// </summary>
        /// <param name="endpoint">The endpoint to move.</param>
        /// <param name="targetRange">Another range from the same text provider.</param>
        /// <param name="targetEndpoint">An endpoint on the other range.</param>
        void ITextRangeProvider.MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            if (targetRange == null)
            {
                throw new ArgumentNullException("targetRange");
            }
            TextRangeAdaptor rangeAdaptor = ValidateAndThrow(targetRange);
            ITextPointer targetPointer = (targetEndpoint == TextPatternRangeEndpoint.Start) ? rangeAdaptor._start : rangeAdaptor._end;
            if (endpoint == TextPatternRangeEndpoint.Start)
            {
                _start = targetPointer.CreatePointer();
                // Ensure the correct ordering of the endpoint.
                if (_start.CompareTo(_end) > 0)
                {
                    _end = _start.CreatePointer();
                }
            }
            else
            {
                _end = targetPointer.CreatePointer();
                // Ensure the correct ordering of the endpoint.
                if (_start.CompareTo(_end) > 0)
                {
                    _start = _end.CreatePointer();
                }
            }
        }

        /// <summary>
        /// Selects the text of the range within the provider.  If the provider does not have a concept of selection then
        /// it should return false for ITextProvider.SupportsTextSelection property and throw an InvalidOperation 
        /// exception for this method.
        /// </summary>
        void ITextRangeProvider.Select()
        {
            if (((ITextProvider)_textAdaptor).SupportedTextSelection == SupportedTextSelection.None)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextProvider_TextSelectionNotSupported));
            }

            Normalize();

            _textAdaptor.Select(_start, _end);
        }

        /// <summary>
        /// Adds the text of the range to the current selection.  If the provider does not have a concept of selection
        /// or does not support multiple disjoint selection then it throw an InvalidOperation 
        /// exception for this method.
        /// </summary>
        void ITextRangeProvider.AddToSelection()
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Removes the text of the range from the current selection.  If the provider does not have a concept of selection
        /// or does not support multiple disjoint selection then it throw an InvalidOperation 
        /// exception for this method.
        /// </summary>
        void ITextRangeProvider.RemoveFromSelection()
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Scrolls the text in the provider so the range is within the viewport.
        /// </summary>
        /// <param name="alignToTop">true if the provider should be scrolled so the range is flush with the top of the viewport.
        /// false if the provider should be scrolled so the range is flush with the bottom.</param>
        void ITextRangeProvider.ScrollIntoView(bool alignToTop)
        {
            Normalize();

            _textAdaptor.ScrollIntoView(_start, _end, alignToTop);
        }

        /// <summary>
        /// Retrieves a collection of all of the children that fall within the range.
        /// </summary>
        /// <returns>An enumeration of all children that fall within the range.  Children
        /// that overlap with the range but are not entirely enclosed by it will
        /// also be included in the collection.  If there are no children then
        /// this can return either null or an empty enumeration.</returns>
        IRawElementProviderSimple[] ITextRangeProvider.GetChildren()
        {
            Normalize();

            IRawElementProviderSimple[] elements = null;
            AutomationPeer peer = GetEnclosingAutomationPeer(_start, _end);
            Invariant.Assert(peer != null);
            List<AutomationPeer> peers = GetAutomationPeersFromRange(peer, _start, _end);
            if (peers.Count > 0)
            {
                elements = new IRawElementProviderSimple[peers.Count];
                for (int i = 0; i < peers.Count; i++)
                {
                    elements[i] = ProviderFromPeer(peers[i]);
                }
            }
            return elements;
        }

        #endregion ITextRangeProvider
    }
}
