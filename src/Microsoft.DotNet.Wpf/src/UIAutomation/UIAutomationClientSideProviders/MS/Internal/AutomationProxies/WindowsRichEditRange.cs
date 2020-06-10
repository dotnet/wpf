// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
// Common code to support the ITextPointerProvider interface

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Text;
using System.ComponentModel;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    internal class WindowsRichEditRange : ITextRangeProvider
    {
        internal WindowsRichEditRange(ITextRange range, WindowsRichEdit pattern)
        {
            Debug.Assert(range != null);
            Debug.Assert(pattern != null);

            _range = range;
            _pattern = pattern;
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        ITextRangeProvider ITextRangeProvider.Clone()
        {
            // Use ITextRange::GetDuplicate to duplicate the ITextRange.
            ITextRange range = _range.GetDuplicate();
            return range!=null ? new WindowsRichEditRange(range, _pattern) : null;
        }

        bool ITextRangeProvider.Compare(ITextRangeProvider range)
        {
            // Use ITextRange::IsEqual to compare ITextRanges.
            WindowsRichEditRange otherRange = (WindowsRichEditRange)range;
            return _range.IsEqual(otherRange._range)==TomBool.tomTrue;
        }

        int ITextRangeProvider.CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            // Get the endpoint character positions using ITextRange::GetStart && ITextRange::GetEnd.
            // Subtract the character positions to get the return value.
            WindowsRichEditRange otherRange = (WindowsRichEditRange)targetRange;
            int e1 = (endpoint == TextPatternRangeEndpoint.Start) ? _range.Start : _range.End;
            int e2 = (targetEndpoint == TextPatternRangeEndpoint.Start) ? otherRange._range.Start : otherRange._range.End;
            return e1 - e2;
        }

        void ITextRangeProvider.ExpandToEnclosingUnit(TextUnit unit)
        {
            Misc.SetFocus(_pattern._hwnd);

            switch (unit)
            {
                case TextUnit.Format:
                    {
                        // take the minimum of expanding by character and paragraph formatting.
                        ITextRange charRange = _range.GetDuplicate();
                        charRange.Expand(TomUnit.tomCharFormat);

                        ITextRange paraRange = _range.GetDuplicate();
                        paraRange.Expand(TomUnit.tomParaFormat);

                        _range.SetRange(Math.Max(charRange.Start, paraRange.Start), Math.Min(charRange.End, paraRange.End));
                    }
                    break;

                default:
                    _range.Expand(TomUnitFromTextUnit(unit, "unit"));
                    break;
            }
        }

        ITextRangeProvider ITextRangeProvider.FindAttribute(int attributeId, object val, bool backwards)
        {
            AutomationTextAttribute attribute = AutomationTextAttribute.LookupById(attributeId);
            // for paragraph-level attributes (ITextPara) search by units of uniform paragraph formatting.
            // for character-level attributes (ITextFont) search by units of uniform character formatting.
            if (attribute == TextPattern.BulletStyleAttribute
                || attribute == TextPattern.HorizontalTextAlignmentAttribute
                || attribute == TextPattern.IndentationFirstLineAttribute
                || attribute == TextPattern.IndentationLeadingAttribute
                || attribute == TextPattern.IndentationTrailingAttribute
                || attribute == TextPattern.TabsAttribute)
            {
                if (backwards)
                {
                    return FindAttributeBackwards(attribute, val, TomUnit.tomParaFormat);
                }
                else
                {
                    return FindAttributeForwards(attribute, val, TomUnit.tomParaFormat);
                }
            }
            else
            {
                if (backwards)
                {
                    return FindAttributeBackwards(attribute, val, TomUnit.tomCharFormat);
                }
                else
                {
                    return FindAttributeForwards(attribute, val, TomUnit.tomCharFormat);
                }
            }
        }

        ITextRangeProvider ITextRangeProvider.FindText(string text, bool backwards, bool ignoreCase)
        {
            // PerSharp/PreFast will flag this as warning 6507/56507: Prefer 'string.IsNullOrEmpty(text)' over checks for null and/or emptiness.
            // A null string is not should throw an ArgumentNullException while an empty string should throw an ArgumentException.
            // Therefore we can not use IsNullOrEmpty() here, suppress the warning.
#pragma warning suppress 6507
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }
#pragma warning suppress 6507
            if (text.Length == 0)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidParameter));
            }

            // copy the our range and search from the start point or end point depending on 
            // whether we are searching backwards.
            ITextRange range = _range.GetDuplicate();
            TomMatch flags = ignoreCase ? 0 : TomMatch.tomMatchCase;
            int max = range.End - range.Start;
            int length;
            if (!backwards)
            {
                length = range.FindTextStart(text, max, flags);
                range.End = range.Start + length;
            }
            else
            {
                length = range.FindTextEnd(text, -max, flags);
                range.Start = range.End - length;
            }

            // return a new range if we found something or null otherwise.
            return length > 0 ? new WindowsRichEditRange(range, _pattern) : null;
        }

        object ITextRangeProvider.GetAttributeValue(int attributeId)
        {
            AutomationTextAttribute attribute = AutomationTextAttribute.LookupById(attributeId);

            // note regarding UnderlineColorAttribute: richedit does not support colored underlines.  all underlines
            // are the same color as the text so richedit does not support UnderlineColorAttribute.

            return GetAttributeValueForRange(_range, attribute);
        }

        double[] ITextRangeProvider.GetBoundingRectangles()
        {
            // if the range is entirely off-screen then return an empty array of rectangles
            ITextRange visibleRange = _pattern.GetVisibleRange();
            int start = Math.Max(_range.Start, visibleRange.Start);
            int end = Math.Min(_range.End, visibleRange.End);
            if (start > end)
            {
                return Array.Empty<double>();
            }

            // get the client area in screen coordinates.
            // we'll use it to "clip" ranges that are partially scrolled out of view.
            NativeMethods.Win32Rect w32rect = new NativeMethods.Win32Rect();
            Misc.GetClientRectInScreenCoordinates(_pattern.WindowHandle, ref w32rect);
            Rect clientRect = new Rect(w32rect.left, w32rect.top, w32rect.right - w32rect.left, w32rect.bottom - w32rect.top);

            // for each line except the last add a bounding rectangle
            // that spans from the start of the line (or start of the original
            // range in the case of the first line) to the end of the line.
            ArrayList rects = new ArrayList();
            Rect rect;
            ITextRange range = _pattern.Document.Range(start, start);
            range.EndOf(TomUnit.tomLine, TomExtend.tomExtend);
            while (range.End < end)
            {
                rect = CalculateOneLineRangeRectangle(range, clientRect);
                if (rect.Width > 0 && rect.Height > 0)
                {
                    rects.Add(rect);
                }

                // move to the start of the next line and extend it to the end.
                range.Move(TomUnit.tomLine, 1);
                range.EndOf(TomUnit.tomLine, TomExtend.tomExtend);
            }

            // add the bounding rectangle for last (and possibly only) line.
            range.End = end;
            rect = CalculateOneLineRangeRectangle(range, clientRect);
            if (rect.Width > 0 && rect.Height > 0)
            {
                rects.Add(rect);
            }

            // convert our list of rectangles into an array and return it.
            Rect[] rectArray = new Rect[rects.Count];
            rects.CopyTo(rectArray);

            return Misc.RectArrayToDoubleArray(rectArray);
        }

        IRawElementProviderSimple ITextRangeProvider.GetEnclosingElement()
        {
            // note: if we have hyperlink children we'll need something more sophisticated.
            return _pattern;
        }

        string ITextRangeProvider.GetText(int maxLength)
        {
            // if no maximum length is given then return the text of the entire range.
            string text;
            if (maxLength < 0)
            {
                text = _range.Text;
            }
            else
            {
                // if the maximum length is greater than the length of the range then
                // return the text of the entire range
                int start = _range.Start;
                int end = _range.End;
                if (end - start <= maxLength)
                {
                    text = _range.Text;
                }
                else
                {
                    // the range is greater than the maximum length so get the text from a
                    // cloned, truncated range.
                    ITextRange range = _range.GetDuplicate();
                    range.End = range.Start + maxLength;
                    text = range.Text;
                    // PerSharp/PreFast will flag this as a warning 6507/56507: Prefer 'string.IsNullOrEmpty(text)' over checks for null and/or emptiness.
                    // An empty strings is desirable, not a null string.  Cannot use IsNullOrEmpty().
                    // Suppress the warning.
#pragma warning suppress 6507
                    Debug.Assert(text == null || text.Length == maxLength);
                }
            }

            // RichEdit returns null for an empty range rather than an empty string.
            return string.IsNullOrEmpty(text) ? "" : text;
        }

        int ITextRangeProvider.Move(TextUnit unit, int count)
        {
            Misc.SetFocus(_pattern._hwnd);

            int moved;
            switch (unit)
            {
                case TextUnit.Format:
                    moved = MoveFormatUnit(count);
                    break;

                default:
                    moved = _range.Move(TomUnitFromTextUnit(unit, "unit"), count);
                    break;
            }
            return moved;
        }

        int ITextRangeProvider.MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
        {
            Misc.SetFocus(_pattern._hwnd);

            int moved;
            switch (unit)
            {
                case TextUnit.Format:
                    if (endpoint == TextPatternRangeEndpoint.Start)
                    {
                        moved = MoveStartFormatUnit(count);
                    }
                    else
                    {
                        moved = MoveEndFormatUnit(count);
                    }
                    break;

                default:
                    if (endpoint == TextPatternRangeEndpoint.Start)
                    {
                        moved = _range.MoveStart(TomUnitFromTextUnit(unit, "unit"), count);
                    }
                    else
                    {
                        moved = _range.MoveEnd(TomUnitFromTextUnit(unit, "unit"), count);
                    }
                    break;
            }
            return moved;
        }

        void ITextRangeProvider.MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            Misc.SetFocus(_pattern._hwnd);

            // our our character position to the target character position
            ITextRange range = ((WindowsRichEditRange)targetRange)._range;
            int cp = (targetEndpoint == TextPatternRangeEndpoint.Start) ? range.Start : range.End;
            if (endpoint == TextPatternRangeEndpoint.Start)
            {
                // TOM has an idiosyncracy that you can't set Start to after the final '\r'.
                // If you attempt to do so then it will change End instead. Yuk!
                // So if they attempt to set the start of the range to the very end of the
                // document we'll place it immediately before the end of the document instead.
                int storyLength = _range.StoryLength;
                _range.Start = cp<storyLength ? cp : storyLength-1;
            }
            else
            {
                _range.End = cp;
            }
        }

        void ITextRangeProvider.Select()
        {
            Misc.SetFocus(_pattern._hwnd);

            _range.Select();

            // for future reference: ITextRange::Select sets the active end to End.
            // If the client wanted the active endpoint to be the start then fix it up.
            // if (activeEndpoint == TextPatternRangeEndpoint.Start)
            //{
            //    _pattern.Document.Selection.Flags = _pattern.Document.Selection.Flags | TomSelectionFlags.tomSelStartActive;
            //}
        }

        void ITextRangeProvider.AddToSelection()
        {
            throw new InvalidOperationException();
        }

        void ITextRangeProvider.RemoveFromSelection()
        {
            throw new InvalidOperationException();
        }

        void ITextRangeProvider.ScrollIntoView(bool alignToTop)
        {
            Misc.SetFocus(_pattern._hwnd);

            if (alignToTop)
            {
                _range.ScrollIntoView(TomStartEnd.tomStart);
            }
            else
            {
                // ITextRange.ScrollIntoView with tomEnd results in the last line only partially visible.
                // to ensure that the last line is fully-visible we scroll the *next* line to the bottom.
                ITextRange range = _range.GetDuplicate();
                range.MoveEnd(TomUnit.tomLine, 1);
                range.ScrollIntoView(TomStartEnd.tomEnd);
            }
        }
     
        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        IRawElementProviderSimple[] ITextRangeProvider.GetChildren()
        {
            // if we implement hyperlink, etc. children then this becomes more involved.
            return Array.Empty<IRawElementProviderSimple>();
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        // this is an extension of object.Equals that compares arrays by value instead of by reference.
        private static bool AttributeValuesAreEqual(object v1, object v2)
        {
            // check if the objects are equal
            if (object.Equals(v1, v2))
            {
                return true;
            }
            Array a1 = v1 as Array;
            Array a2 = v2 as Array;

            if( a1 != null && a2 != null)
            {
                // System.Array has reference equality not value equality so we manually check
                // whether two arrays have the same contents.

                // verify they have the same number of dimensions and total elements
                if (a1.Length != a2.Length || a1.Rank != a2.Rank)
                {
                    return false;
                }

                // verify each dimension has the same upper and lower bounds
                for (int i = 0; i < a1.Rank; i++)
                {
                    if (a1.GetLowerBound(i) != a2.GetLowerBound(i)
                        || a1.GetUpperBound(i) != a2.GetUpperBound(i))
                    {
                        return false;
                    }
                }

                // compare each element
                IEnumerator e1 = a1.GetEnumerator();
                IEnumerator e2 = a2.GetEnumerator();
                while (e1.MoveNext() && e2.MoveNext())
                {
                    // (we recursively use CompareAttributeValues to handle the case of nested arrays.)
                    if (!AttributeValuesAreEqual(e1.Current, e2.Current))
                    {
                        return false;
                    }
                }

                // all checks passed.  the arrays are value equal.
                return true;
            }
            // To workaround the conversion that Marshaling of COM-interop did.
            else if (v2 is Enum)
            {
                return object.Equals(v1, (int)v2);
            }
            else
            {
                return false;
            }
        }

        private ITextRangeProvider FindAttributeForwards(AutomationTextAttribute attribute, object val, TomUnit unit)
        {
            // we accumulate the resulting subrange with these two endpoints:
            const int NoMatchYet = -1;
            int start = NoMatchYet; // set to a character position when we find the beginning of the match.
            int end = NoMatchYet; // a character position that is extended each time we find another matching subrange.

            // examine each subrange of uniform formatting until we reach the end of our range.
            // if we complete a match within the range we will break out of the middle of the loop.
            int limit = _range.End; // cache the limit of our search range.
            ITextRange subrange = FirstUnit(_range); 
            while (NextUnit(limit, subrange, unit))
            {
                // if this subrange of values has a matching attribute then add it to
                // our resulting subrange.
                object subrangeVal = GetAttributeValueForRange(subrange, attribute);
                if (AttributeValuesAreEqual(val, subrangeVal))
                {
                    // set the start pointer if this is the first matching subrange.
                    if (start == NoMatchYet)
                    {
                        start = subrange.Start;
                    }

                    // update the end of the matching subrange to include the current one.
                    end = subrange.End;
                }
                else
                {
                    // no match.

                    // if we have found a matching subrange then we're done.
                    if (start != NoMatchYet)
                    {
                        break;
                    }
                }
            }

            // if we have a matching subrange then return it, otherwise return null.
            if (start != NoMatchYet)
            {
                subrange.SetRange(start, end);
                return new WindowsRichEditRange(subrange, _pattern);
            }
            else
            {
                return null;
            }
        }

        private ITextRangeProvider FindAttributeBackwards(AutomationTextAttribute attribute, object val, TomUnit unit)
        {
            // this works just like FindAttributeForwards except we work our way backward through the range.

            // we accumulate the resulting subrange with these two endpoints:
            const int NoMatchYet = -1;
            int start = NoMatchYet; // a character position that is extended each time we find another matching subrange.
            int end = NoMatchYet; // set to a character position when we find the beginning of the match.

            // examine each subrange of uniform formatting until we reach the end of our range.
            // if we complete a match within the range we will break out of the middle of the loop.
            int limit = _range.Start; // cache the limit of our search range.
            ITextRange subrange = LastUnit(_range);
            while (PreviousUnit(limit, subrange, unit))
            {
                // if this subrange of values has a matching attribute then add it to
                // our resulting subrange.
                object subrangeVal = GetAttributeValueForRange(subrange, attribute);
                if (AttributeValuesAreEqual(val, subrangeVal))
                {
                    // set the start pointer if this is the first matching subrange.
                    if (end == NoMatchYet)
                    {
                        end = subrange.End;
                    }

                    // update the start of the matching subrange to include the current one.
                    start = subrange.Start;
                }
                else
                {
                    // no match.

                    // if we have found a matching subrange then we're done.
                    if (end != NoMatchYet)
                    {
                        break;
                    }
                }
            }

            // if we have a matching subrange then return it, otherwise return null.
            if (end != NoMatchYet)
            {
                subrange.SetRange(start, end);
                return new WindowsRichEditRange(subrange, _pattern);
            }
            else
            {
                return null;
            }
        }

        private object GetAttributeValueForRange(ITextRange range, AutomationTextAttribute attribute)
        {
            // conditional attributes that we aren't implementing this version are commented out.

            object rval;
            if (attribute == TextPattern.AnimationStyleAttribute) { rval = GetAnimationStyle(range.Font); }
            else if (attribute == TextPattern.BackgroundColorAttribute) { rval = GetBackgroundColor(range.Font); }
            else if (attribute == TextPattern.BulletStyleAttribute) { rval = GetBulletStyle(range.Para); }
            else if (attribute == TextPattern.CapStyleAttribute) { rval = GetCapStyle(range.Font); }
//            else if (attribute == TextPattern.CompositionStateAttribute) { rval = GetCompositionState(range.Font, range.Para); }
//            else if (attribute == TextPattern.CultureAttribute) { rval = GetCulture(range.Font, range.Para); }
            else if (attribute == TextPattern.FontNameAttribute) { rval = GetFontName(range); }
            else if (attribute == TextPattern.FontSizeAttribute) { rval = GetFontSize(range.Font); }
            else if (attribute == TextPattern.FontWeightAttribute) { rval = GetFontWeight(range.Font); }
            else if (attribute == TextPattern.ForegroundColorAttribute) { rval = GetForegroundColor(range.Font); }
//            else if (attribute == TextPattern.HeadingLevelAttribute) { rval = GetHeadingLevel(range.Font, range.Para); }
            else if (attribute == TextPattern.HorizontalTextAlignmentAttribute) { rval = GetHorizontalTextAlignment(range.Para); }
            else if (attribute == TextPattern.IndentationFirstLineAttribute) { rval = GetIndentationFirstLine(range.Para); }
            else if (attribute == TextPattern.IndentationLeadingAttribute) { rval = GetIndentationLeading(range.Para); }
            else if (attribute == TextPattern.IndentationTrailingAttribute) { rval = GetIndentationTrailing(range.Para); }
            else if (attribute == TextPattern.IsHiddenAttribute) { rval = GetHidden(range.Font); }
            else if (attribute == TextPattern.IsItalicAttribute) { rval = GetItalic(range.Font); }
            else if (attribute == TextPattern.IsReadOnlyAttribute) { rval = GetReadOnly(range.Font); }
            else if (attribute == TextPattern.IsSubscriptAttribute) { rval = GetSubscript(range.Font); }
            else if (attribute == TextPattern.IsSuperscriptAttribute) { rval = GetSuperscript(range.Font); }
//            else if (attribute == TextPattern.MarginBottomAttribute) { rval = GetMarginBottom(range.Font, range.Para); }
//            else if (attribute == TextPattern.MarginLeadingAttribute) { rval = GetMarginLeading(range.Font, range.Para); }
//            else if (attribute == TextPattern.MarginTopAttribute) { rval = GetMarginTop(range.Font, range.Para); }
//            else if (attribute == TextPattern.MarginTrailingAttribute) { rval = GetMarginTrailing(range.Font, range.Para); }
//            else if (attribute == TextPattern.MarkedAutoCorrectedAttribute) { rval = GetMarkedAutoCorrected(range.Font, range.Para); }
//            else if (attribute == TextPattern.MarkedGrammaticallyWrongAttribute) { rval = GetMarkedGrammaticallyWrong(range.Font, range.Para); }
//            else if (attribute == TextPattern.MarkedMisspelledAttribute) { rval = GetMarkedMisspelled(range.Font, range.Para); }
//            else if (attribute == TextPattern.MarkedSmartTagAttribute) { rval = GetMarkedSmartTag(range.Font, range.Para); }
//            else if (attribute == TextPattern.OrderedListStringAttribute) { rval = GetOrderedListString(range.Font, range.Para); }
            else if (attribute == TextPattern.OutlineStylesAttribute) { rval = GetOutlineStyles(range.Font); }
//            else if (attribute == TextPattern.OverlineColorAttribute) { rval = GetOverlineColor(range.Font); }
//            else if (attribute == TextPattern.OverlineStyleAttribute) { rval = GetOverlineStyle(range.Font); }
//            else if (attribute == TextPattern.PageHeightAttribute) { rval = GetPageHeight(range.Font, range.Para); }
//            else if (attribute == TextPattern.PageNumberAttribute) { rval = GetPageNumber(range.Font, range.Para); }
//            else if (attribute == TextPattern.PageWidthAttribute) { rval = GetPageWidth(range.Font, range.Para); }
//            else if (attribute == TextPattern.PortraitAttribute) { rval = GetPortrait(range.Font, range.Para); }
//            else if (attribute == TextPattern.StrikethroughColorAttribute) { rval = GetStrikethroughColor(range.Font); }
            else if (attribute == TextPattern.StrikethroughStyleAttribute) { rval = GetStrikethroughStyle(range.Font); }
            else if (attribute == TextPattern.TabsAttribute) { rval = GetTabs(range.Para); }
//            else if (attribute == TextPattern.TextFlowDirectionAttribute) { rval = GetTextFlowDirection(range.Font, range.Para); }
//            else if (attribute == TextPattern.UnderlineColorAttribute) { rval = GetUnderlineColor(range.Font); }
            else if (attribute == TextPattern.UnderlineStyleAttribute) { rval = GetUnderlineStyle(range.Font); }
//            else if (attribute == TextPattern.VerticalTextAlignmentAttribute) { rval = GetVerticalTextAlignment(range.Font, range.Para); }
            else
            {
                rval = AutomationElement.NotSupported;
            }
            return rval;
        }


        private static object GetAnimationStyle(ITextFont font)
        {
            TomAnimation anim = font.Animation;
            if (anim == TomAnimation.tomUndefined)
            {
                return TextPattern.MixedAttributeValue;
            }
            else
            {
                // the AnimationStyle enum matches the TomAnimation enum
                return (AnimationStyle)(int)anim;
            }
        }

        private static object GetBackgroundColor(ITextFont font)
        {
            int color = font.BackColor;
            switch (color)
            {
                case (int)TomConst.tomAutocolor:
                    // tomAutocolor means richedit is using the default system background color.
                    // review: if RichEdit sends a WM_CTLCOLOR message to it's parent then the 
                    // background color can depend on whatever background brush the parent supplies.
                    return SafeNativeMethods.GetSysColor(NativeMethods.COLOR_WINDOW);

                case (int)TomConst.tomUndefined:
                    return TextPattern.MixedAttributeValue;

                default:
                    // if the high-byte is zero then we have a COLORREF
                    if ((color & 0xff000000) == 0)
                    {
                        return color;
                    }
                    else
                    {
                        // we have a PALETTEINDEX color
                        return AutomationElement.NotSupported;
                    }
            }
        }

        private static object GetBulletStyle(ITextPara para) 
        {
            // look at the ListType field of the paragraph style.
            TomListType t = para.ListType;
            if (t == TomListType.tomUndefined)
            {
                return TextPattern.MixedAttributeValue;
            }
            else
            {
                switch (para.ListType & TomListType.tomListTypeMask)
                {
                    case TomListType.tomListBullet:
                        return BulletStyle.FilledRoundBullet;

                    default:
                        return BulletStyle.None;
                }
            }
        }

        private static object GetCapStyle(ITextFont font) 
        {
            TomBool allCaps = font.AllCaps;
            TomBool smallCaps = font.SmallCaps;

            if (allCaps == TomBool.tomUndefined || smallCaps == TomBool.tomUndefined)
            {
                return TextPattern.MixedAttributeValue;
            }
            else
            {
                // note: AllCaps and SmallCaps are mutually exclusive.
                if (font.AllCaps == TomBool.tomTrue)
                {
                    return CapStyle.AllCap;
                }
                else if (font.SmallCaps == TomBool.tomTrue)
                {
                    return CapStyle.SmallCap;
                }
                else
                {
                    return CapStyle.None;
                }
            }
        }

        private static object GetFontName(ITextRange range)
        {
            // most ITextFont properties return tomUndefined if the value varies over the range.
            // for the font name, though, ITextFont just returns the name of the font at one end
            // of the range.  So we have to go through the range in tomCharFormat units and examine
            // the font name in each one to see if it is uniform or not.

            // if it is a degenerate range then return the font name for that degenerate range.
            string name = null;
            int start = range.Start;
            int end = range.End; // cache the limit
            if (start >= end)
            {
                name = range.Font.Name;
            }
            else
            {
                // iterate over blocks of uniformly-formatted text
                for (ITextRange unitRange = FirstUnit(range); NextUnit(end, unitRange, TomUnit.tomCharFormat); )
                {
                    // on the first iteration remember the font name.
                    if (string.IsNullOrEmpty(name))
                    {
                        name = unitRange.Font.Name;
                    }
                    else
                    {
                        // on subsequent iterations compare if the font name is the same.
                        if (string.Compare(name, unitRange.Font.Name, StringComparison.Ordinal) != 0)
                        {
                            return TextPattern.MixedAttributeValue;
                        }
                    }
                }
            }
           
            return name;
        }

        private static object GetFontSize(ITextFont font)
        {
            float size = font.Size;
            if ((TomConst)size == TomConst.tomUndefined)
            {
                return TextPattern.MixedAttributeValue;
            }
            else
            {
                return (double)size;
            }
        }

        private static object GetFontWeight(ITextFont font) 
        {
            int weight = font.Weight;
            if (weight == (int)TomConst.tomUndefined)
            {
                return TextPattern.MixedAttributeValue;
            }
            else
            {
                return weight;
            }
        }

        private static object GetForegroundColor(ITextFont font)
        {
            int color = font.ForeColor;
            switch (color)
            {
                case (int)TomConst.tomAutocolor:
                    // tomAutocolor means richedit is using the default system foreground color.
                    // review: if RichEdit sends a WM_CTLCOLOR message to it's parent then the 
                    // text color can depend on whatever foreground color the parent supplies.
                    return SafeNativeMethods.GetSysColor(NativeMethods.COLOR_WINDOWTEXT);

                case (int)TomConst.tomUndefined:
                    return TextPattern.MixedAttributeValue;

                default:
                    // if the high-byte is zero then we have a COLORREF
                    if ((color & 0xff000000) == 0)
                    {
                        return color;
                    }
                    else
                    {
                        // we have a PALETTEINDEX color
                        return AutomationElement.NotSupported;
                    }
            }
        }

        private static object GetHorizontalTextAlignment(ITextPara para) 
        { 
            // review: ITextPara::GetListAlignment?
            TomAlignment alignment = para.Alignment;
            if (alignment == TomAlignment.tomUndefined)
            {
                return TextPattern.MixedAttributeValue;
            }
            else
            {
                // HorizontalTextAlignment enum matcheds TomAlignment enum
                return (HorizontalTextAlignment)(int)para.Alignment;
            }
        }

        private static object GetIndentationFirstLine(ITextPara para) 
        {
            float indent = para.FirstLineIndent;
            if ((TomConst)indent == TomConst.tomUndefined)
            {
                return TextPattern.MixedAttributeValue;
            }
            else
            {
                return (double)indent;
            }
        }

        private static object GetIndentationLeading(ITextPara para) 
        {
            float indent = para.LeftIndent;
            if ((TomConst)indent == TomConst.tomUndefined)
            {
                return TextPattern.MixedAttributeValue;
            }
            else
            {
                return (double)indent;
            }
        }

        private static object GetIndentationTrailing(ITextPara para) 
        { 
            float indent = para.RightIndent;
            if ((TomConst)indent == TomConst.tomUndefined)
            {
                return TextPattern.MixedAttributeValue;
            }
            else
            {
                return (double)indent;
            }
        }

        private static object GetHidden(ITextFont font)
        {
            TomBool hidden = font.Hidden;
            if (hidden == TomBool.tomUndefined)
            {
                return TextPattern.MixedAttributeValue;
            }
            else
            {
                return hidden == TomBool.tomTrue;
            }
        }

        private static object GetItalic(ITextFont font) 
        {
            TomBool italic = font.Italic;
            if (italic == TomBool.tomUndefined)
            {
                return TextPattern.MixedAttributeValue;
            }
            else
            {
                return italic == TomBool.tomTrue;
            }
        }

        private static object GetOutlineStyles(ITextFont font) 
        {
            TomBool outline = font.Outline;
            TomBool shadow = font.Shadow;
            TomBool emboss = font.Emboss;
            TomBool engrave = font.Engrave;

            if (outline == TomBool.tomUndefined || shadow == TomBool.tomUndefined || emboss == TomBool.tomUndefined || engrave == TomBool.tomUndefined) 
            { 
                return TextPattern.MixedAttributeValue; 
            }
            else
            {
                OutlineStyles style = 0;
                style |= (outline == TomBool.tomTrue) ? OutlineStyles.Outline : 0;
                style |= (shadow == TomBool.tomTrue) ? OutlineStyles.Shadow : 0;
                style |= (emboss == TomBool.tomTrue) ? OutlineStyles.Embossed : 0;
                style |= (engrave == TomBool.tomTrue) ? OutlineStyles.Engraved : 0;
                return style;
            }
        }

        private object GetReadOnly(ITextFont font)
        {
            // if the entire pattern is read-only then every range within it is also
            // read only.
            if (_pattern.ReadOnly)
            {
                return true;
            }

            // check if the "Protected" font style is turned on.
            TomBool protect = font.Protected;
            if (protect == TomBool.tomUndefined)
            {
                return TextPattern.MixedAttributeValue;
            }
            else
            {
                return protect == TomBool.tomTrue;
            }
        }

        private static object GetStrikethroughStyle(ITextFont font) 
        {
            TomBool strike = font.StrikeThrough;
            if (strike == TomBool.tomUndefined)
            {
                return TextPattern.MixedAttributeValue;
            }
            else
            {
                return strike == TomBool.tomTrue ? TextDecorationLineStyle.Single : TextDecorationLineStyle.None;
            }
        }

        private static object GetSubscript(ITextFont font) 
        {
            TomBool sub = font.Subscript;
            if (sub == TomBool.tomUndefined)
            {
                return TextPattern.MixedAttributeValue;
            }
            else
            {
                return sub == TomBool.tomTrue;
            }
        }

        private static object GetSuperscript(ITextFont font) 
        {
            TomBool super = font.Superscript;
            if (super == TomBool.tomUndefined)
            {
                return TextPattern.MixedAttributeValue;
            }
            else
            {
                return super == TomBool.tomTrue;
            }
        }

        private static object GetTabs(ITextPara para) 
        {
            int count = para.TabCount;
            if (count == (int)TomConst.tomUndefined)
            {
                return TextPattern.MixedAttributeValue;
            }
            else
            {
                double[] tabs = new double[count];
                for (int i = 0; i < count; i++)
                {
                    float tbPos;
                    TomAlignment tbAlign;
                    TomLeader tbLeader;
                    para.GetTab(i, out tbPos, out tbAlign, out tbLeader);
                    tabs[i] = tbPos;
                }
                return tabs;
            }
        }

        private static object GetUnderlineStyle(ITextFont font) 
        {
            // note: if a range spans different underline styles then it won't return tomUndefined.  instead it appears
            // to return the underline style at the endpoint.  if a range spans underlined and non-underlined text then
            // it returns tomUndefined properly.

            TomUnderline underline = font.Underline;
            if (underline == TomUnderline.tomUndefined)
            {
                return TextPattern.MixedAttributeValue;
            }
            else
            {
                switch (underline)
                {
                    case TomUnderline.tomTrue:
                        return TextDecorationLineStyle.Single;

                    default:
                        // TextDecorationLineStyle enum matches TomUnderline enum
                        return (TextDecorationLineStyle)(int)underline;
                }
            }
        }

        private int MoveFormatUnit(int count)
        {
            int moved = 0;

            if (count > 0)
            {
                // if it is a non-degenerate range then collapse it.
                if (_range.Start < _range.End)
                {
                    _range.Collapse(TomStartEnd.tomEnd);
                } 
                
                // move the start point forward the number of units.  the end point will get pushed along.
                moved += MoveStartFormatUnit(count);
            }
            else if (count < 0)
            {
                // if it is a non-degenerate range then collapse it.
                if (_range.Start < _range.End)
                {
                    _range.Collapse(TomStartEnd.tomStart);
                }

                // move the end point backward the number of units.  the start point will get pushed along.
                moved += MoveEndFormatUnit(count);
            }

            return moved;
        }

        private int MoveStartFormatUnit(int count)
        {
            // This is identical to MoveEndFormatUnit except we are calling MoveStartOneXXX instead of the MoveEndOneXXX versions.
            int moved = 0;

            if (count > 0)
            {
                // loop until we have moved the requested number of units
                // or we can't move anymore and break out of the loop.
                while (moved < count)
                {
                    if (MoveStartOneFormatUnitForward())
                    {
                        moved++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else if (count < 0)
            {
                // loop until we have moved the requested number of units
                // or we can't move anymore and break out of the loop.
                while (moved > count)
                {
                    if (MoveStartOneFormatUnitBackward())
                    {
                        moved--;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return moved;
        }

        private int MoveEndFormatUnit(int count)
        {
            // This is identical to MoveStartFormatUnit except we are calling MoveEndOneXXX instead of the MoveStartOneXXX versions.
            int moved = 0;

            if (count > 0)
            {
                // loop until we have moved the requested number of units
                // or we can't move anymore and break out of the loop.
                while (moved < count)
                {
                    if (MoveEndOneFormatUnitForward())
                    {
                        moved++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else if (count < 0)
            {
                // loop until we have moved the requested number of units
                // or we can't move anymore and break out of the loop.
                while (moved > count)
                {
                    if (MoveEndOneFormatUnitBackward())
                    {
                        moved--;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return moved;
        }

        private bool MoveStartOneFormatUnitForward()
        {
            // try moving the endpoint one char-format unit and one para-format unit
            // and remember where each one ended up.
            ITextRange charRange = _range.GetDuplicate();
            int charMoved = charRange.MoveStart(TomUnit.tomCharFormat, 1);
            Debug.Assert(charMoved == 0 || charMoved == 1);
            ITextRange paraRange = _range.GetDuplicate();
            int paraMoved = paraRange.MoveStart(TomUnit.tomParaFormat, 1);
            Debug.Assert(paraMoved == 0 || paraMoved == 1);

            // ensure the endpoint is set to whichever moved the *least*
            // and return true iff we moved successfully.
            if (charMoved == 1 && (paraMoved == 0 || charRange.Start <= paraRange.Start))
            {
                _range = charRange;
                return true;
            }
            else if (paraMoved == 1)
            {
                _range = paraRange;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool MoveStartOneFormatUnitBackward()
        {
            // try moving the endpoint one char-format unit and one para-format unit
            // and remember where each one ended up.
            ITextRange charRange = _range.GetDuplicate();
            int charMoved = charRange.MoveStart(TomUnit.tomCharFormat, -1);
            Debug.Assert(charMoved == 0 || charMoved == -1);
            ITextRange paraRange = _range.GetDuplicate();
            int paraMoved = paraRange.MoveStart(TomUnit.tomParaFormat, -1);
            Debug.Assert(paraMoved == 0 || paraMoved == -1);

            // ensure the endpoint is set to whichever moved the *least*
            // and return true iff we moved successfully.
            if (charMoved == -1 && (paraMoved == 0 || charRange.Start >= paraRange.Start))
            {
                _range = charRange;
                return true;
            }
            else if (paraMoved == -1)
            {
                _range = paraRange;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool MoveEndOneFormatUnitForward()
        {
            // try moving the endpoint one char-format unit and one para-format unit
            // and remember where each one ended up.
            ITextRange charRange = _range.GetDuplicate();
            int charMoved = charRange.MoveEnd(TomUnit.tomCharFormat, 1);
            Debug.Assert(charMoved == 0 || charMoved == 1);
            ITextRange paraRange = _range.GetDuplicate();
            int paraMoved = paraRange.MoveEnd(TomUnit.tomParaFormat, 1);
            Debug.Assert(paraMoved == 0 || paraMoved == 1);

            // ensure the endpoint is set to whichever moved the *least*
            // and return true iff we moved successfully.
            if (charMoved == 1 && (paraMoved == 0 || charRange.End <= paraRange.End))
            {
                _range = charRange;
                return true;
            }
            else if (paraMoved == 1)
            {
                _range = paraRange;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool MoveEndOneFormatUnitBackward()
        {
            // try moving the endpoint one char-format unit and one para-format unit
            // and remember where each one ended up.
            ITextRange charRange = _range.GetDuplicate();
            int charMoved = charRange.MoveEnd(TomUnit.tomCharFormat, -1);
            Debug.Assert(charMoved == 0 || charMoved == -1);
            ITextRange paraRange = _range.GetDuplicate();
            int paraMoved = paraRange.MoveEnd(TomUnit.tomParaFormat, -1);
            Debug.Assert(paraMoved == 0 || paraMoved == -1);

            // ensure the endpoint is set to whichever moved the *least*
            // and return true iff we moved successfully.
            if (charMoved == -1 && (paraMoved == 0 || charRange.End >= paraRange.End))
            {
                _range = charRange;
                return true;
            }
            else if (paraMoved == -1)
            {
                _range = paraRange;
                return true;
            }
            else
            {
                return false;
            }
        }

        // this wrapper around ITextRange.GetPoint returns true if GetPoint returned S_OK, false if GetPoint returned S_FALSE,
        // or throws an exception for an error hresult.
        internal static bool RangeGetPoint(ITextRange range, TomGetPoint type, out int x, out int y)
        {
            int hr = range.GetPoint(type, out x, out y);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            return hr == 0;
        }

        // converts one of our TextUnits to the corresponding TomUnit.
        // if there isn't a corresponding one it throws an ArgumentException for the specified name.
        private static TomUnit TomUnitFromTextUnit(TextUnit unit, string name)
        {
            switch (unit)
            {
                case TextUnit.Character:
                    return TomUnit.tomCharacter;

                case TextUnit.Word:
                    return TomUnit.tomWord;

                case TextUnit.Line:
                    return TomUnit.tomLine;

                case TextUnit.Paragraph:
                    return TomUnit.tomParagraph;

                case TextUnit.Page:
                case TextUnit.Document:
                    return TomUnit.tomStory;

                default:
                    throw new ArgumentException(name);
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Static Methods
        //
        //------------------------------------------------------

        #region Static Methods

        // these helper functions make it convenient to walk through a range by unit as follows:
        //   int end = range.End;
        //   for (ITextRange subrange=FirstUnit(range); NextUnit(end, subrange, tomUnit.tomCharFormat);)...
        // and
        //   int start = range.Start;
        //   for (ITextRange subrange=LastUnit(range); PreviousUnit(start, subrange, tomUnit.tomParaFormat);)...

        private static ITextRange FirstUnit(ITextRange range)
        {
            // get a degenerate subrange positioned at the beginning of the range
            ITextRange subrange = range.GetDuplicate();
            subrange.Collapse(TomStartEnd.tomStart);
            return subrange;
        }

        private static ITextRange LastUnit(ITextRange range)
        {
            // get a degenerate subrange positioned at the end of the range
            ITextRange subrange = range.GetDuplicate();
            subrange.Collapse(TomStartEnd.tomEnd);
            return subrange;
        }

        private static bool NextUnit(int end, ITextRange subrange, TomUnit unit)
        {
            if (subrange.End >= end)
            {
                return false;
            }
            else
            {
                // collapse the range to the end and then extend it another unit.
                subrange.Collapse(TomStartEnd.tomEnd);
                subrange.MoveEnd(unit, 1);

                // truncate if necessary to ensure it fits inside the range
                if (subrange.End > end)
                {
                    subrange.End = end;
                }

                return true;
            }
        }

        private static bool PreviousUnit(int start, ITextRange subrange, TomUnit unit)
        {
            if (subrange.Start <= start)
            {
                return false;
            }
            else
            {
                // collapse the range to the end and then extend it another unit.
                subrange.Collapse(TomStartEnd.tomStart);
                subrange.MoveStart(unit, -1);

                // truncate if necessary to ensure it fits inside the range
                if (subrange.Start < start)
                {
                    subrange.Start = start;
                }

                return true;
            }
        }

        // this function gets the bounding rectangle for a range that does not wrap lines.
        private static Rect CalculateOneLineRangeRectangle(ITextRange lineRange, Rect clientRect)
        {
            int start = lineRange.Start;
            int end = lineRange.End;
            if (start < end)
            {
                // make a working copy of the range. shrink it by one character since 
                // ITextRange.GetPoint returns the  coordinates of the character *after* the end of the 
                // range rather than the one immediately *before* the end of the range.
                ITextRange range = lineRange.GetDuplicate();
                range.MoveEnd(TomUnit.tomCharacter, -1);
                end--;

                // The ITextRange::SetRange method sets this range's Start = min(cp1, cp2) and End = max(cp1, cp2).
                // If the range is a nondegenerate selection, cp2 is the active end; if it's a degenerate selection,
                // the ambiguous cp is displayed at the start of the line (rather than at the end of the previous line).
                // Set the end to the start and the start to the end to create an ambiguous cp.
                range.SetRange(range.End, range.Start);

                Rect rect = new Rect(clientRect.Location, clientRect.Size);
                bool trimmed = TrimRectangleByRangeCorners(range, ref rect);
                if (!trimmed)
                {
                    while (!trimmed && start < end)
                    {
                        // shrink the range
                        range.MoveStart(TomUnit.tomCharacter, 1);
                        // If the character just moved over was a '\r' the start will be out of sink with range.Start
                        start++;
                        if (start < end)
                        {
                            range.MoveEnd(TomUnit.tomCharacter, -1);
                            // If the character just moved over was a '\r' the end will be out of sink with range.End
                            end--;
                        }
                        //Debug.Assert(start == range.Start);
                        //Debug.Assert(end == range.End);

                        trimmed = TrimRectangleByRangeCorners(range, ref rect);
                    }

                    if (trimmed)
                    {
                        rect.X = clientRect.X;
                        rect.Width = clientRect.Width;
                    }
                }

                if (!trimmed)
                {
                    rect = Rect.Empty;
                }
                return rect;
            }
            else
            {
                // degenerate range.  return empty rectangle.

                // it might be nice to return a zero-width rectangle with the appropriate height and location.
                // however, when the degenerate range is at a line wrapping point then it is ambiguous as to
                // whether the rectangle should be at the end of one line or the beginning of the next.  clients
                // can always extend a degenerate range by one character in either direction before getting a bounding
                // rectangle if they want.

                return Rect.Empty;
            }
        }

        // pass in a rectangle of the entire client area and this function will trim it based on visible corners
        // of the range plus the character following the range. we get the extra character because
        // ITextRange.GetPoint(tomEnd,...) refers to the character following the endpoint rather than immediately 
        // preceding the endpoint.  callers have to take this into account and adjust their ranges accordingly.
        // it will trim the rectangle as much as possible based on the visible corners and leave the rest
        // of the rectangle unchanged.  it returns true if it found at least one visible corner.
        // note: there are some obscure cases that this won't handle.  if all four corners of a character cell
        // are outside the client area but some part of the middle of the character is visible then this routine will
        // think that the character is entirely invisible.  this limitation is due to the fact that you can only
        // get coordinates from ITextRange for a specific point on an endpoint character and if that specific 
        // point is offscreen then ITextRange::GetPoint returns S_FALSE.
        private static bool TrimRectangleByRangeCorners(ITextRange range, ref Rect rect)
        {
            // it's easier to work with the rectangle components separately..
            double left = rect.Left;
            double top = rect.Top;
            double right = rect.Right;
            double bottom = rect.Bottom;

            // if the top-left corner is visible then trim off anything above that corner.
            // if we are on our first iteration then also trim anything to the left of it.
            int x, y;
            bool GotTopLeft, GotTopRight, GotBottomLeft, GotBottomRight;
            GotTopRight = false; GotBottomLeft = false;
            if (GotTopLeft = RangeGetPoint(range, TomGetPoint.tomStart/*|TOP|LEFT*/, out x, out y))
            {
                left = x;
                top = y;
            }

            // if the bottom-right corner is visible then trim off anything below that corner.
            // if we are on our first iteration then also trim anything to the right of it.
            if (GotBottomRight = RangeGetPoint(range, /*tomEnd|*/TomGetPoint.TA_BOTTOM | TomGetPoint.TA_RIGHT, out x, out y))
            {
                right = x;
                bottom = y;
            }

            // (if diagonal corners are visible then we are done since we have trimmed all four sides.)

            // if only one or neither diagonal corner is visible...
            if (!GotTopLeft || !GotBottomRight)
            {
                // if the top-right corner is visible then trim off anything above that corner.
                // if we are on our first iteration then also trim anything to the right of it.
                if (GotTopRight = RangeGetPoint(range, /*tomEnd|TOP|*/TomGetPoint.TA_RIGHT, out x, out y))
                {
                    right = x;
                    top = y;
                }
                else
                {
                    // if the bottom-left corner is visible then trim off anything below that corner.
                    // if we are on our first iteration then also trim anything to the left of it.
                    if (GotBottomLeft = RangeGetPoint(range, TomGetPoint.tomStart | TomGetPoint.TA_BOTTOM /*|LEFT*/, out x, out y))
                    {
                        left = x;
                        bottom = y;
                    }
                }
            }

            // update the rectangle
            rect.X = left;
            rect.Y = top;
            rect.Width = right - left;
            rect.Height = bottom - top;

            return GotTopLeft || GotTopRight || GotBottomLeft || GotBottomRight;
        }

        #endregion Static Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private ITextRange _range;  // alert: this can point to different ITextRange objects over the lifetime of this WindowsRichEditRange.
        private WindowsRichEdit _pattern;

        #endregion private Fields
    }
}
