// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
// ITextRangeProvider interface for WindowsEditBox

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Text;
using System.ComponentModel;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    // TERMINOLOGY: Win32 Edit controls use the term "index" to mean a character position.
    // For example the EM_LINEINDEX message converts a line number to it's starting character position.
    // Perhaps not the best choice but we use it to be consistent.

    internal class WindowsEditBoxRange : ITextRangeProvider
    {
        //------------------------------------------------------
        //
        //  Constructor
        //
        //------------------------------------------------------
 
        internal WindowsEditBoxRange(WindowsEditBox provider, int start, int end)
        {
            if (start < 0 || end < start)
            {
                // i'm throwing an invalid operation exception rather than an argument exception because 
                // clients never call this constructor directly.  it always happens as a result of some 
                // other operation, e.g. cloning an existing TextPatternRange.
                throw new InvalidOperationException(SR.Get(SRID.InvalidTextRangeOffset,GetType().FullName));
            }
            Debug.Assert(provider != null);

            _provider = provider;
            _start = start;
            _end = end;
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        ITextRangeProvider ITextRangeProvider.Clone()
        {
            return new WindowsEditBoxRange(_provider, Start, End);
        }

        bool ITextRangeProvider.Compare(ITextRangeProvider range)
        {
            // TextPatternRange already verifies the other range comes from the same element before forwarding so we only need to worry about
            // whether the endpoints are identical.
            WindowsEditBoxRange editRange = (WindowsEditBoxRange)range;
            return editRange.Start == Start && editRange.End == End;
        }

        int ITextRangeProvider.CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            // TextPatternRange already verifies the other range comes from the same element before forwarding so we only need to worry about
            // comparing the endpoints.
            WindowsEditBoxRange editRange = (WindowsEditBoxRange)targetRange;
            int e1 = (endpoint == TextPatternRangeEndpoint.Start) ? Start : End;
            int e2 = (targetEndpoint == TextPatternRangeEndpoint.Start) ? editRange.Start : editRange.End;
            return e1 - e2;
        }

        void ITextRangeProvider.ExpandToEnclosingUnit(TextUnit unit)
        {
            Misc.SetFocus(_provider._hwnd);

            switch (unit)
            {
                case TextUnit.Character:
                    // if it is a degenerate range then expand it to be one character.
                    // otherwise, leave it as it is.
                    if (Start == End)
                    {
                        int moved;
                        End = MoveEndpointForward(End, TextUnit.Character, 1, out moved);
                    }
                    break;

                case TextUnit.Word:
                    {
                        // this works same as paragraph except we look for word boundaries instead of paragraph boundaries.

                        // get the text so we can figure out where the boundaries are
                        string text = _provider.GetText();
                        ValidateEndpoints();

#if WCP_NLS_ENABLED
                        // use the same word breaker that Avalon Text uses.
                        WordBreaker breaker = new WordBreaker();
                        TextContainer container = new TextContainer(text);
                        // if the starting point of the range is not already at a word break
                        // then move it backwards to the nearest word break.
                        TextNavigator startNavigator = new TextNavigator(Start, container);
                        if (!breaker.IsAtWordBreak(startNavigator))
                        {
                            breaker.MoveToPreviousWordBreak(startNavigator);
                            Start = startNavigator.Position;
                        }

                        // if the range is degenerate or the ending point of the range is not already at a word break 
                        // then move it forwards to the nearest word break.
                        TextNavigator endNavigator = new TextNavigator(End, container);
                        if (Start==End || !breaker.IsAtWordBreak(endNavigator))
                        {
                            breaker.MoveToNextWordBreak(endNavigator);
                            End = endNavigator.Position;
                        }
#else
                        // move start left until we reach a word boundary.
                        for (; !AtWordBoundary(text, Start); Start--) ;

                        // move end right until we reach word boundary (different from Start).
                        End = Math.Min(Math.Max(End, Start + 1), text.Length);
                        for (; !AtWordBoundary(text, End); End++) ;
#endif
                    }
                    break;

                case TextUnit.Line:
                    {
                        if (_provider.GetLineCount() != 1)
                        {
                            int startLine = _provider.LineFromChar(Start);
                            int endLine = _provider.LineFromChar(End);

                            MoveTo(_provider.LineIndex(startLine), _provider.LineIndex(endLine + 1));
                        }
                        else
                        {
                            MoveTo(0, _provider.GetTextLength());
                        }
                    }
                    break;

                case TextUnit.Paragraph:
                    { 
                        // this works same as paragraph except we look for word boundaries instead of paragraph boundaries.

                        // get the text so we can figure out where the boundaries are
                        string text = _provider.GetText();
                        ValidateEndpoints();

                        // move start left until we reach a paragraph boundary.
                        for (; !AtParagraphBoundary(text, Start); Start--);

                        // move end right until we reach a paragraph boundary (different from Start).
                        End = Math.Min(Math.Max(End, Start + 1), text.Length);
                        for (; !AtParagraphBoundary(text, End); End++);
                    } 
                    break;

                case TextUnit.Format:
                case TextUnit.Page:
                case TextUnit.Document:
                    MoveTo(0, _provider.GetTextLength());
                    break;

                //break;
                default:
                    throw new System.ComponentModel.InvalidEnumArgumentException("unit", (int)unit, typeof(TextUnit));
            }
        }

        ITextRangeProvider ITextRangeProvider.FindAttribute(int attributeId, object val, bool backwards)
        {
            AutomationTextAttribute attribute = AutomationTextAttribute.LookupById(attributeId);
            // generic controls are plain text so if the attribute matches then it matches over the whole range.

            // To workaround the conversion that Marshaling of COM-interop did.
            object targetAttribute = GetAttributeValue(attribute);
            if (targetAttribute is Enum)
            {
                targetAttribute = (int)targetAttribute;
            }

            return val.Equals(targetAttribute) ? new WindowsEditBoxRange(_provider, Start, End) : null;
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

            // get the text of the range
            string rangeText = _provider.GetText();
            ValidateEndpoints();
            rangeText = rangeText.Substring(Start, Length);

            // if we are ignoring case then convert everything to lowercase
            if (ignoreCase)
            {
                rangeText = rangeText.ToLower(System.Globalization.CultureInfo.InvariantCulture);
                text = text.ToLower(System.Globalization.CultureInfo.InvariantCulture);
            }

            // do a case-sensitive search for the text inside the range.
            int i = backwards ? rangeText.LastIndexOf(text, StringComparison.Ordinal) : rangeText.IndexOf(text, StringComparison.Ordinal);

            // if the text was found then create a new range covering the found text.
            return i >= 0 ? new WindowsEditBoxRange(_provider, Start + i, Start + i + text.Length) : null;
        }

        object ITextRangeProvider.GetAttributeValue(int attributeId)
        {
            AutomationTextAttribute attribute = AutomationTextAttribute.LookupById(attributeId);
            return GetAttributeValue(attribute);
        }

        double[] ITextRangeProvider.GetBoundingRectangles()
        {
            // Return zero rectangles for a degenerate range.   We don't return an empty, 
            // but properly positioned, rectangle for degenerate ranges because
            // there is ambiguity at line breaks and some international scenarios.
            if (IsDegenerate)
            {
                return new double[0];
            }

            // we'll need to have the text eventually (so we can measure characters) so get it up
            // front so we can check the endpoints before proceeding.
            string text = _provider.GetText();
            ValidateEndpoints();

            // get the mapping from client coordinates to screen coordinates
            NativeMethods.Win32Point w32point;
            w32point.x = 0;
            w32point.y = 0;
            if (!Misc.MapWindowPoints(_provider.WindowHandle, IntPtr.Zero, ref w32point, 1))
            {
                return new double[0];
            }
            Point mapClientToScreen = new Point(w32point.x, w32point.y);

            // clip the rectangles to the edit control's formatting rectangle
            Rect clippingRectangle = _provider.GetRect();

            // we accumulate rectangles onto a list
            ArrayList rects;

            if (_provider.IsMultiline)
            {
                rects = GetMultilineBoundingRectangles(text, mapClientToScreen, clippingRectangle);
            }
            else
            {
                // single line edit control

                rects = new ArrayList(1);

                // figure out the rectangle for this one line
                Point startPoint = _provider.PosFromChar(Start);
                Point endPoint = _provider.PosFromCharUR(End - 1, text);
                Rect rect = new Rect(startPoint.X, startPoint.Y, endPoint.X - startPoint.X, clippingRectangle.Height);
                rect.Intersect(clippingRectangle);

                // use the rectangle if it is non-empty.
                if (rect.Width > 0 && rect.Height > 0)  // r.Empty is true only if both width & height are zero.  Duh!
                {
                    rect.Offset(mapClientToScreen.X, mapClientToScreen.Y);
                    rects.Add(rect);
                }
            }

            // convert the list of rectangles into an array for returning
            Rect[] rectArray = new Rect[rects.Count];
            rects.CopyTo(rectArray);

            return Misc.RectArrayToDoubleArray(rectArray);
        }

        IRawElementProviderSimple ITextRangeProvider.GetEnclosingElement()
        {
            return _provider;
        }

        string ITextRangeProvider.GetText(int maxLength)
        {
            if (maxLength < 0)
                maxLength = End;
            string text = _provider.GetText();
            ValidateEndpoints();
            return text.Substring(Start, maxLength >= 0 ? Math.Min(Length, maxLength) : Length);
        }

        int ITextRangeProvider.Move(TextUnit unit, int count)
        {
            Misc.SetFocus(_provider._hwnd);

            // positive count means move forward.  negative count means move backwards.
            int moved = 0;
            if (count > 0)
            {
                // If the range is non-degenerate then we need to collapse the range.
                // (See the discussion of Count for ITextRange::Move)
                if (!IsDegenerate)
                {
                    // If the count is greater than zero, collapse the range at its end point
                    Start = End;
                }

                // move the degenerate range forward by the number of units
                int m;
                int start = Start;
                Start = MoveEndpointForward(Start, unit, count, out m);
                // if the start did not change then no move was done.
                if (start != Start)
                {
                    moved = m;
                }
            }
            else if (count < 0)
            {
                // If the range is non-degenerate then we need to collapse the range.
                if (!IsDegenerate)
                {
                    // If the count is less than zero, collapse the range at the starting point
                    End = Start;
                }

                // move the degenerate range backward by the number of units
                int m;
                int end = End;
                End = MoveEndpointBackward(End, unit, count, out m);
                // if the end did not change then no move was done.
                if (end != End)
                {
                    moved = m;
                }
            }
            else
            {
                // moving zero of any unit has no effect.
                moved = 0;
            }

            return moved;
        }

        int ITextRangeProvider.MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
        {
            Misc.SetFocus(_provider._hwnd);

            // positive count means move forward.  negative count means move backwards.
            int moved = 0;
            bool moveStart = endpoint == TextPatternRangeEndpoint.Start;
            int start = Start;
            int end = End;
            if (count > 0)
            {
                if (moveStart)
                {
                    Start = MoveEndpointForward(Start, unit, count, out moved);

                    // if the start did not change then no move was done.
                    if (start == Start)
                    {
                        moved = 0;
                    }
                }
                else
                {
                    End = MoveEndpointForward(End, unit, count, out moved);

                    // if the end did not change then no move was done.
                    if (end == End)
                    {
                        moved = 0;
                    }
                }
            }
            else if (count < 0)
            {
                if (moveStart)
                {
                    Start = MoveEndpointBackward(Start, unit, count, out moved);

                    // if the start did not change then no move was done.
                    if (start == Start)
                    {
                        moved = 0;
                    }
                }
                else
                {
                    End = MoveEndpointBackward(End, unit, count, out moved);

                    // if the end did not change then no move was done.
                    if (end == End)
                    {
                        moved = 0;
                    }
                }
            }
            else
            {
                // moving zero of any unit has no effect.
                moved = 0;
            }

            return moved;
        }

        void ITextRangeProvider.MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            Misc.SetFocus(_provider._hwnd);

            // TextPatternRange already verifies the other range comes from the same element before forwarding so we only need to worry
            // about the endpoints.
            WindowsEditBoxRange editRange = (WindowsEditBoxRange)targetRange;
            int e = (targetEndpoint == TextPatternRangeEndpoint.Start) ? editRange.Start : editRange.End;

            if (endpoint == TextPatternRangeEndpoint.Start)
            {
                Start = e;
            }
            else
            {
                End = e;
            }
        }

        void ITextRangeProvider.Select()
        {
            Misc.SetFocus(_provider._hwnd);

            _provider.SetSel(Start, End);
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
            Misc.SetFocus(_provider._hwnd);

            // Scroll into view is handled differently depending on whether
            // it is a multi-line control or not.
            if (_provider.IsMultiline)
            {
                int newFirstLine;

                if (alignToTop)
                {
                    newFirstLine = _provider.LineFromChar(Start);
                }
                else
                {
                    newFirstLine =
                        Math.Max(0, _provider.LineFromChar(End) - _provider.LinesPerPage() + 1);
                }

                _provider.LineScroll(Start, newFirstLine - _provider.GetFirstVisibleLine());

            }
            else if (_provider.IsScrollable)
            {
                Misc.SetFocus(_provider._hwnd);

                int visibleStart;
                int visibleEnd;
                _provider.GetVisibleRangePoints(out visibleStart, out visibleEnd);

                if (Misc.IsReadingRTL(_provider._hwnd))
                {
                    short key = UnsafeNativeMethods.VK_LEFT;

                    if (Start > visibleStart)
                    {
                        key = UnsafeNativeMethods.VK_RIGHT;
                    }

                    while (Start > visibleStart || Start < visibleEnd)
                    {
                        Input.SendKeyboardInputVK(key, true);
                        _provider.GetVisibleRangePoints(out visibleStart, out visibleEnd);
                    }
                }
                else
                {
                    short key = UnsafeNativeMethods.VK_RIGHT;

                    if (Start < visibleStart)
                    {
                        key = UnsafeNativeMethods.VK_LEFT;
                    }

                    while (Start < visibleStart || Start > visibleEnd)
                    {
                        Input.SendKeyboardInputVK(key, true);
                        _provider.GetVisibleRangePoints(out visibleStart, out visibleEnd);
                    }
                }
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
            // we don't have any children so return an empty array
            return new IRawElementProviderSimple[0];
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        // returns true iff index identifies a paragraph boundary within text.
        private static bool AtParagraphBoundary(string text, int index)
        {
            return index <= 0 || index >= text.Length || (text[index - 1]=='\n') && (text[index]!='\n');
        }

#if !WCP_NLS_ENABLED
        // returns true iff index identifies a word boundary within text.
        // following richedit & word precedent the boundaries are at the leading edge of the word
        // so the span of a word includes trailing whitespace.
        private static bool AtWordBoundary(string text, int index)
        {
            // NOTE: this is a heuristic word break detector that matches RichEdit behavior pretty well for
            // English prose.  It is a placeholder until we put in something with real wordbreaking
            // intelligence based on the System.NaturalLanguage DLL.

            // we are at a word boundary if we are at the beginning or end of the text
            if (index <= 0 || index >= text.Length)
            {
                return true;
            }

            if( AtParagraphBoundary(text, index))
            {
                return true;
            }

            char ch1 = text[index - 1];
            char ch2 = text[index];

            // an apostrophe does *not* break a word if it follows or precedes characters
            if ((char.IsLetterOrDigit(ch1) && IsApostrophe(ch2))
                || (IsApostrophe(ch1) && char.IsLetterOrDigit(ch2) && index >= 2 && char.IsLetterOrDigit(text[index - 2])))
            {
                return false;
            }

            // the following transitions mark boundaries.
            // note: these are constructed to include trailing whitespace.
            return (char.IsWhiteSpace(ch1) && !char.IsWhiteSpace(ch2))
                || (char.IsLetterOrDigit(ch1) && !char.IsLetterOrDigit(ch2))
                || (!char.IsLetterOrDigit(ch1) && char.IsLetterOrDigit(ch2))
                || (char.IsPunctuation(ch1) && char.IsWhiteSpace(ch2));
        }

        private static bool IsApostrophe(char ch)
        {
            return ch == '\'' || 
                   ch == (char)0x2019; // Unicode Right Single Quote Mark
        }

#endif

        // a big pseudo-switch statement based on the attribute
        private object GetAttributeValue(AutomationTextAttribute attribute)
        {
            object rval;
            if (attribute == TextPattern.BackgroundColorAttribute)
            {
                rval = GetBackgroundColor();
            }
            else if (attribute == TextPattern.CapStyleAttribute)
            {
                rval = GetCapStyle(_provider.WindowStyle);
            }
            else if (attribute == TextPattern.FontNameAttribute)
            {
                rval = GetFontName(_provider.GetLogfont());
            }
            else if (attribute == TextPattern.FontSizeAttribute)
            {
                rval = GetFontSize(_provider.GetLogfont());
            }
            else if (attribute == TextPattern.FontWeightAttribute)
            {
                rval = GetFontWeight(_provider.GetLogfont());
            }
            else if (attribute == TextPattern.ForegroundColorAttribute)
            {
                rval = GetForegroundColor();
            }
            else if (attribute == TextPattern.HorizontalTextAlignmentAttribute)
            {
                rval = GetHorizontalTextAlignment(_provider.WindowStyle);
            }
            else if (attribute == TextPattern.IsItalicAttribute)
            {
                rval = GetItalic(_provider.GetLogfont());
            }
            else if (attribute == TextPattern.IsReadOnlyAttribute)
            {
                rval = GetReadOnly();
            }
            else if (attribute == TextPattern.StrikethroughStyleAttribute)
            {
                rval = GetStrikethroughStyle(_provider.GetLogfont());
            }
            else if (attribute == TextPattern.UnderlineStyleAttribute)
            {
                rval = GetUnderlineStyle(_provider.GetLogfont());
            }
            else
            {
                rval = AutomationElement.NotSupported;
            }
            return rval;
        }

        // helper function to accumulate a list of bounding rectangles for a potentially mult-line range
        private ArrayList GetMultilineBoundingRectangles(string text, Point mapClientToScreen, Rect clippingRectangle)
        {
            // remember the line height
            int height = Math.Abs(_provider.GetLogfont().lfHeight);;

            // get the starting and ending lines for the range.
            int start = Start;
            int end = End;

            int startLine = _provider.LineFromChar(start);
            int endLine = _provider.LineFromChar(end - 1);

            // adjust the start based on the first visible line

            int firstVisibleLine = _provider.GetFirstVisibleLine();
            if (firstVisibleLine > startLine)
            {
                startLine = firstVisibleLine;
                start = _provider.LineIndex(startLine);
            }

            // adjust the end based on the last visible line
            int lastVisibleLine = firstVisibleLine + _provider.LinesPerPage() - 1;
            if (lastVisibleLine < endLine)
            {
                endLine = lastVisibleLine;
                end = _provider.LineIndex(endLine) - 1;
            }

            // adding a rectangle for each line
            ArrayList rects = new ArrayList(Math.Max(endLine - startLine + 1, 0));
            int nextLineIndex = _provider.LineIndex(startLine);
            for (int i = startLine; i <= endLine; i++)
            {
                // determine the starting coordinate on this line
                Point startPoint;
                if (i == startLine)
                {
                    startPoint = _provider.PosFromChar(start);
                }
                else
                {
                    startPoint = _provider.PosFromChar(nextLineIndex);
                }

                // determine the ending coordinate on this line
                Point endPoint;
                if (i == endLine)
                {
                    endPoint = _provider.PosFromCharUR(end-1, text); 
                }
                else
                {
                    nextLineIndex = _provider.LineIndex(i + 1);
                    endPoint = _provider.PosFromChar(nextLineIndex - 1);
                }

                // add a bounding rectangle for this line if it is nonempty
                Rect rect = new Rect(startPoint.X, startPoint.Y, endPoint.X - startPoint.X, height);
                rect.Intersect(clippingRectangle);
                if (rect.Width > 0 && rect.Height > 0)  // r.Empty is true only if both width & height are zero.  Duh!
                {
                    rect.Offset(mapClientToScreen.X, mapClientToScreen.Y);
                    rects.Add(rect);
                }
            }

            return rects;
        }

        // returns the value of the corresponding text attribute
        private static object GetHorizontalTextAlignment(int style)
        {
            if (Misc.IsBitSet(style, NativeMethods.ES_CENTER))
            {
                return HorizontalTextAlignment.Centered;
            }
            else if (Misc.IsBitSet(style, NativeMethods.ES_RIGHT))
            {
                return HorizontalTextAlignment.Right;
            }
            else
            {
                return HorizontalTextAlignment.Left;
            }
        }

        // returns the value of the corresponding text attribute
        private static object GetCapStyle(int style)
        {
            return Misc.IsBitSet(style, NativeMethods.ES_UPPERCASE) ? CapStyle.AllCap : CapStyle.None;
        }

        // returns the value of the corresponding text attribute
        private object GetReadOnly()
        {
            return _provider.IsReadOnly();
        }

        // returns the value of the corresponding text attribute
        private static object GetBackgroundColor()
        {
            // NOTE! it is possible for parents of edit controls to change the background color by responding
            // to WM_CTLCOLOREDIT however we have decided not to handle that case.
            return SafeNativeMethods.GetSysColor(NativeMethods.COLOR_WINDOW);
        }

        // returns the value of the corresponding text attribute
        private static object GetFontName(NativeMethods.LOGFONT logfont)
        {
            return logfont.lfFaceName;
        }

        // returns the value of the corresponding text attribute
        private static object GetFontSize(NativeMethods.LOGFONT logfont)
        {
            // note: this assumes integral point sizes. violating this assumption would confuse the user
            // because they set something to 7 point but reports that it is, say 7.2 point, due to the rounding.
            IntPtr hdc = Misc.GetDC(IntPtr.Zero);
            if (hdc == IntPtr.Zero)
            {
                return null;
            }
            int lpy = UnsafeNativeMethods.GetDeviceCaps(hdc, NativeMethods.LOGPIXELSY);
            Misc.ReleaseDC(IntPtr.Zero, hdc);
            return Math.Round((double)(-logfont.lfHeight) * 72 / lpy);
        }

        // returns the value of the corresponding text attribute
        private static object GetFontWeight(NativeMethods.LOGFONT logfont)
        {
            return logfont.lfWeight;
        }

        // returns the value of the corresponding text attribute
        private static object GetForegroundColor()
        {
            // NOTE! it is possible for parents of edit controls to change the text color by responding
            // to WM_CTLCOLOREDIT however we have decided not to handle that case.
            return SafeNativeMethods.GetSysColor(NativeMethods.COLOR_WINDOWTEXT);
        }

        // returns the value of the corresponding text attribute
        private static object GetItalic(NativeMethods.LOGFONT logfont)
        {
            return logfont.lfItalic != 0;
        }

        // returns the value of the corresponding text attribute
        private static object GetStrikethroughStyle(NativeMethods.LOGFONT logfont)
        {
            return logfont.lfStrikeOut != 0 ? TextDecorationLineStyle.Single : TextDecorationLineStyle.None;
        }

        // returns the value of the corresponding text attribute
        private static object GetUnderlineStyle(NativeMethods.LOGFONT logfont)
        {
            return logfont.lfUnderline != 0 ? TextDecorationLineStyle.Single : TextDecorationLineStyle.None;
        }

        // moves an endpoint forward a certain number of units.
        // the endpoint is just an index into the text so it could represent either
        // the endpoint.
        private int MoveEndpointForward(int index, TextUnit unit, int count, out int moved)
        {
            switch (unit)
            {
                case TextUnit.Character:
                    {
                        int limit = _provider.GetTextLength() ;
                        ValidateEndpoints();

                        moved = Math.Min(count, limit - index);
                        index = index + moved;

                        index = index > limit ? limit : index;
                    }
                    break;

                case TextUnit.Word:
                    {
                        string text = _provider.GetText();
                        ValidateEndpoints();

#if WCP_NLS_ENABLED
                    // use the same word breaker as Avalon Text.
                    WordBreaker breaker = new WordBreaker();
                    TextContainer container = new TextContainer(text);
                    TextNavigator navigator = new TextNavigator(index, container);

                    // move forward one word break for each count
                    for (moved = 0; moved < count && index < text.Length; moved++)
                    {
                        if (!breaker.MoveToNextWordBreak(navigator))
                            break;
                    }

                    index = navigator.Position;
#else
                        for (moved = 0; moved < count && index < text.Length; moved++)
                        {
                            for (index++; !AtWordBoundary(text, index); index++) ;
                        }
#endif
                    }
                    break;

                case TextUnit.Line:
                    {
                        // figure out what line we are on.  if we are in the middle of a line and
                        // are moving left then we'll round up to the next line so that we move
                        // to the beginning of the current line.
                        int line = _provider.LineFromChar(index);

                        // limit the number of lines moved to the number of lines available to move
                        // Note lineMax is always >= 1.
                        int lineMax = _provider.GetLineCount();
                        moved = Math.Min(count, lineMax - line - 1);

                        if (moved > 0)
                        {
                            // move the endpoint to the beginning of the destination line.
                            index = _provider.LineIndex(line + moved);
                        }
                        else if (moved == 0 && lineMax == 1)
                        {
                            // There is only one line so get the text length as endpoint
                            index = _provider.GetTextLength();
                            moved = 1;
                        }
                    }
                    break;

                case TextUnit.Paragraph:
                    {
                        // just like moving words but we look for paragraph boundaries instead of 
                        // word boundaries.
                        string text = _provider.GetText();
                        ValidateEndpoints();

                        for (moved = 0; moved < count && index < text.Length; moved++)
                        {
                            for (index++; !AtParagraphBoundary(text, index); index++) ;
                        }
                    }
                    break;

                case TextUnit.Format:
                case TextUnit.Page:
                case TextUnit.Document:
                    {
                        // since edit controls are plain text moving one uniform format unit will
                        // take us all the way to the end of the document, just like
                        // "pages" and document.
                        int limit = _provider.GetTextLength();
                        ValidateEndpoints();

                        // we'll move 1 format unit if we aren't already at the end of the
                        // document.  Otherwise, we won't move at all.
                        moved = index < limit ? 1 : 0;
                        index = limit;
                    }
                    break;

                default:
                    throw new System.ComponentModel.InvalidEnumArgumentException("unit", (int)unit, typeof(TextUnit));
            }

            return index;
        }

        // moves an endpoint backward a certain number of units.
        // the endpoint is just an index into the text so it could represent either
        // the endpoint.
        private int MoveEndpointBackward(int index, TextUnit unit, int count, out int moved)
        {
            switch (unit)
            {
                case TextUnit.Character:
                    {
                        int limit = _provider.GetTextLength();
                        ValidateEndpoints();

                        int oneBasedIndex = index + 1;

                        moved = Math.Max(count, -oneBasedIndex);
                        index = index + moved;

                        index = index < 0 ? 0 : index;
                    }
                    break;

                case TextUnit.Word:
                    {
                        string text = _provider.GetText();
                        ValidateEndpoints();

#if WCP_NLS_ENABLED
                    // use the same word breaker as Avalon Text.
                    WordBreaker breaker = new WordBreaker();
                    TextContainer container = new TextContainer(text);
                    TextNavigator navigator = new TextNavigator(index, container);

                    // move backward one word break for each count
                    for (moved = 0; moved > count && index > 0; moved--)
                    {
                        if (!breaker.MoveToPreviousWordBreak(navigator))
                            break;
                    }

                    index = navigator.Position;
#else
                        for (moved = 0; moved > count && index > 0; moved--)
                        {
                            for (index--; !AtWordBoundary(text, index); index--) ;
                        }
#endif
                    }
                    break;

                case TextUnit.Line:
                    {
                        // Note count < 0.

                        // Get 1-based line.
                        int line = _provider.LineFromChar(index) + 1;

                        int lineMax = _provider.GetLineCount();

                        // Truncate the count to the number of available lines.
                        int actualCount = Math.Max(count, -line);

                        moved = actualCount;

                        if (actualCount == -line)
                        {
                            // We are moving by the maximum number of possible lines,
                            // so we know the resulting index will be 0.
                            index = 0;

                            // If a line other than the first consists of only "\r\n",
                            // you can move backwards past this line and the position changes,
                            // hence this is counted.  The first line is special, though:
                            // if it is empty, and you move say from the second line back up
                            // to the first, you cannot move further; however if the first line
                            // is nonempty, you can move from the end of the first line to its
                            // beginning!  This latter move is counted, but if the first line
                            // is empty, it is not counted.

                            // Recalculate the value of "moved".
                            // The first line is empty if it consists only of
                            // a line separator sequence.
                            bool firstLineEmpty =
                                ((lineMax > 1 && _provider.LineIndex(1) == _lineSeparator.Length)
                                    || lineMax == 0);
                                
                            if (moved < 0 && firstLineEmpty)
                            {
                                ++moved;
                            }
                        }
                        else // actualCount > -line
                        {
                            // Move the endpoint to the beginning of the following line,
                            // then back by the line separator length to get to the end
                            // of the previous line, since the Edit control has
                            // no method to get the character index of the end
                            // of a line directly.
                            index = _provider.LineIndex(line + actualCount) - _lineSeparator.Length;
                        }
                    }
                    break;

                case TextUnit.Paragraph:
                    {
                        // just like moving words but we look for paragraph boundaries instead of 
                        // word boundaries.
                        string text = _provider.GetText();
                        ValidateEndpoints();

                        for (moved = 0; moved > count && index > 0; moved--)
                        {
                            for (index--; !AtParagraphBoundary(text, index); index--) ;
                        }
                    }
                    break;

                case TextUnit.Format:
                case TextUnit.Page:
                case TextUnit.Document:
                    {
                        // since edit controls are plain text moving one uniform format unit will
                        // take us all the way to the beginning of the document, just like
                        // "pages" and document.

                        // we'll move 1 format unit if we aren't already at the beginning of the
                        // document.  Otherwise, we won't move at all.
                        moved = index > 0 ? -1 : 0;
                        index = 0;
                    }
                    break;

                default:
                    throw new System.ComponentModel.InvalidEnumArgumentException("unit", (int)unit, typeof(TextUnit));
            }

            return index;
        }

        // method to set both endpoints simultaneously
        private void MoveTo(int start, int end)
        {
            if (start < 0 || end < start)
            {
                throw new InvalidOperationException(SR.Get(SRID.InvalidTextRangeOffset,GetType().FullName));
            }

            _start = start;
            _end = end;
        }

        private void ValidateEndpoints()
        {
            int limit = _provider.GetTextLength();
            if (Start > limit || End > limit)
            {
                throw new InvalidOperationException(SR.Get(SRID.InvalidRangeEndpoint,GetType().FullName));
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        private bool IsDegenerate
        {
            get
            {
                // strictly only needs to be == since never should _start>_end.
                return _start >= _end;
            }
        }

        private int End
        {
            get
            {
                return _end;
            }
            set
            {
                // ensure that we never accidentally get a negative index
                if (value < 0)
                {
                    throw new InvalidOperationException(SR.Get(SRID.InvalidTextRangeOffset,GetType().FullName));
                }

                // ensure that end never moves before start
                if (value < _start)
                {
                    _start = value;
                }
                _end = value;
            }
        }

        private int Length
        {
            get
            {
                return _end - _start;
            }
        }

        private int Start
        {
            get
            {
                return _start;
            }
            set
            {
                // ensure that we never accidentally get a negative index
                if (value < 0)
                {
                    throw new InvalidOperationException(SR.Get(SRID.InvalidTextRangeOffset,GetType().FullName));
                }

                // ensure that start never moves after end
                if (value > _end)
                {
                    _end = value;
                }
                _start = value;
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Static Methods
        //
        //------------------------------------------------------

        #region Static Methods

        #endregion Static Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private WindowsEditBox _provider;
        private int _start;
        private int _end;

        // Edit controls always use "\r\n" as the line separator, not "\n".
        private const string _lineSeparator = "\r\n";  // This string is a non-localizable string

        #endregion Private Fields
    }
}
