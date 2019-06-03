// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: HWND-based Edit Box proxy

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Text;
using MS.Win32;
using NativeMethodsSetLastError = MS.Internal.UIAutomationClientSideProviders.NativeMethodsSetLastError;

namespace MS.Internal.AutomationProxies
{
    // TERMINOLOGY: Win32 Edit controls use the term "index" to mean a character position.
    // For example the EM_LINEINDEX message converts a line number to it's starting character position.
    // Perhaps not the best choice but we use it to be consistent.

    class WindowsEditBox : ProxyHwnd, IValueProvider, ITextProvider
    {
        // ------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // This is the default contructor that calls the base class constructor.
        // This function determines the type of the edit control and then calls the default constructor.
        internal WindowsEditBox (IntPtr hwnd, ProxyFragment parent, int item)
            : base( hwnd, parent, item)
        {
            _type = GetEditboxtype (hwnd);
            if (IsMultiline)
            {
                _cControlType = ControlType.Document;
            }
            else
            {
                _cControlType = ControlType.Edit;
            }

            // When embedded inside of the combobox, hide the edit portion in the content element tree
            _fIsContent = !IsInsideOfCombo();
            _fIsKeyboardFocusable = true;

            if (IsInsideOfListView(hwnd))
            {
                _sAutomationId = "edit";
            }

            // support for events
            _createOnEvent = new WinEventTracker.ProxyRaiseEvents (RaiseEvents);
        }

        #endregion

        #region Proxy Create

        // Static Create method called by UIAutomation to create this proxy.
        // <returns null if unsuccessful
        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild, int idObject)
        {
            return Create(hwnd, idChild);
        }

        private static IRawElementProviderSimple Create(IntPtr hwnd, int idChild)
        {
            // If edit is part of the combo we would have to create it
            // Right now in WCP there is no ability to simply "skip" the element
            // Simply returning combo for edit that is embedded inside of
            // combo will not work, and will in fact create infinite loop
            // User will not see edit in the LogicalTree, since we will implement
            // AutomationElement.LogicalMapping, BUT user still can get "edit"
            // from Hwnd
            // Something is wrong if idChild is not zero
            if (idChild != 0)
            {
                System.Diagnostics.Debug.Assert (idChild == 0, "Invalid Child Id, idChild != 0");
                throw new ArgumentOutOfRangeException("idChild", idChild, SR.Get(SRID.ShouldBeZero));
            }

            return new WindowsEditBox(hwnd, null, 0);
        }

        // Static Create method called by the event tracker system
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                ProxySimple el;
                if (IsInsideOfIPAddress(hwnd))
                {
                    el = new ByteEditBoxOverride(hwnd, idChild);
                }
                else
                {
                    el = new WindowsEditBox(hwnd, null, 0);

                    // If this is an Edit control inside of a Winforms Spinner, need to treat the property
                    // changes for as property changes on the whole Winforms Spinner, not on the element
                    // of the Winforms Spinner.  Winforms Spinner raise WinEvents on the Edit portion of
                    // the spinner and not the UpDown portion like the Win32 Spinner.
                    IntPtr hwndParent = NativeMethodsSetLastError.GetAncestor(hwnd, NativeMethods.GA_PARENT);
                    if (hwndParent != IntPtr.Zero)
                    {
                        // Test for spinner - Create checks if the element is a spinner
                        ProxySimple spinner = (ProxySimple)WinformsSpinner.Create(hwndParent, 0);
                        if (spinner != null)
                        {
                            el = spinner;
                        }
                    }
                }

                el.DispatchEvents (eventId, idProp, idObject, idChild);
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        internal override string GetAccessKey()
        {
            string accessKey = base.GetAccessKey();
            if ((bool)GetElementProperty(AutomationElement.IsKeyboardFocusableProperty))
            {
                // Walk up the parent hwnd chain to find the associated label,
                // e.g. for the case of an edit box in a writable combo box.
                // The hwnd tree in this case may look like:
                //    - Dialog
                //      |- ComboBoxEx32  (optional)
                //         |- ComboBox
                //            |- Edit
                // So we may need to walk up several levels.
                // Although this loop isn't really confined to a window hierarchy
                // of exactly the type shown above, in practice there are no realistic
                // situations where this will do any harm.   If this is shown to be
                // a problem at some point, we will have to constrain the loop to
                // parent hwnd's of type ComboBox or ComboBoxEx32.
                for (IntPtr hwnd = _hwnd;
                            hwnd != IntPtr.Zero && string.IsNullOrEmpty(accessKey);
                            hwnd = Misc.GetParent(hwnd))
                {
                    accessKey = GetLabelAccessKey(hwnd);
                }
            }
            return accessKey;
        }

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider(AutomationPattern iid)
        {
            if (IsInsideOfIPAddress(_hwnd))
            {
                // ByteEditBoxOverride will service this call.
                return null;
            }
            else if (iid == ValuePattern.Pattern && !IsMultiline)
            {
                return this;
            }
            // text pattern is supported for non-password boxes
            else if (iid == TextPattern.Pattern && _type != EditboxType.Password)
            {
                return this;
            }

            return null;
        }

        // Process all the Logical and Raw Element Properties
        internal override object GetElementProperty(AutomationProperty idProp)
        {
            if (idProp == AutomationElement.IsControlElementProperty)
            {
                // When embedded inside of the spinner, hide edit portion in the logical tree
                if (IsInsideOfSpinner())
                {
                    return false;
                }
            }
            else if (idProp == AutomationElement.IsPasswordProperty)
            {
                return _type == EditboxType.Password;
            }
            else if (idProp == AutomationElement.NameProperty)
            {
                // Per ControlType.Edit spec if there is no static text
                // label for an Edit then it must NEVER return the contents
                // of the edit as the Name property.  This relies on the
                // default ProxyHwnd impl looking for a label for Name and
                // not using WM_GETTEXT on the edit hwnd.
                string name = base.GetElementProperty(idProp) as string;
                if (string.IsNullOrEmpty(name))
                {
                    // Stop UIA from asking other providers
                    return AutomationElement.NotSupported;
                }

                return name;
            }

            return base.GetElementProperty(idProp);
        }

        internal override ProxySimple ElementProviderFromPoint(int x, int y)
        {
            IntPtr hwndUpDown = WindowsSpinner.GetUpDownFromEdit(_hwnd);
            if (hwndUpDown != IntPtr.Zero)
            {
                return new WindowsSpinner(hwndUpDown, _hwnd, _parent, _item);
            }

            return base.ElementProviderFromPoint(x, y);
        }

        internal override ProxySimple GetFocus()
        {
            IntPtr hwndUpDown = WindowsSpinner.GetUpDownFromEdit(_hwnd);
            if (hwndUpDown != IntPtr.Zero)
            {
                return new WindowsSpinner(hwndUpDown, _hwnd, _parent, _item);
            }

            return base.GetFocus();
        }

        internal override ProxySimple GetParent()
        {
            IntPtr hwndUpDown = WindowsSpinner.GetUpDownFromEdit(_hwnd);
            if (hwndUpDown != IntPtr.Zero)
            {
                return new WindowsSpinner(hwndUpDown, _hwnd, _parent, _item);
            }

            return base.GetParent();
        }

        #endregion ProxySimple Interface

        #region ProxyHwnd Overrides

        // Builds a list of Win32 WinEvents to process a UIAutomation Event.
        protected override WinEventTracker.EvtIdProperty[] EventToWinEvent(AutomationEvent idEvent, out int cEvent)
        {
            return base.EventToWinEvent(idEvent, out cEvent);
        }

        #endregion

        #region Value Pattern

        // Sets the text of the edit.
        void IValueProvider.SetValue (string str)
        {
            // Ensure that the edit box and all its parents are enabled.
            Misc.CheckEnabled(_hwnd);

            int styles = WindowStyle;

            if (Misc.IsBitSet(styles, NativeMethods.ES_READONLY))
            {
                throw new InvalidOperationException(SR.Get(SRID.ValueReadonly));
            }

            // check if control only accepts numbers
            if (Misc.IsBitSet(styles, NativeMethods.ES_NUMBER))
            {
                // check if string contains any non-numeric characters.
                foreach (char ch in str)
                {
                    if (char.IsLetter (ch))
                    {
                        throw new ArgumentException(SR.Get(SRID.NotAValidValue, str), "val");
                    }
                }
            }

            // Text/edit box should not enter more characters than what is allowed through keyboard.
            // Determine the max number of chars this editbox accepts
            int result = Misc.ProxySendMessageInt(_hwnd, NativeMethods.EM_GETLIMITTEXT, IntPtr.Zero, IntPtr.Zero);
            // A result of -1 means that no limit is set.
            if (result != -1 && result < str.Length)
            {
                throw new InvalidOperationException (SR.Get(SRID.OperationCannotBePerformed));
            }

            // Send the message...
            result = Misc.ProxySendMessageInt(_hwnd, NativeMethods.WM_SETTEXT, IntPtr.Zero, new StringBuilder(str));
            if (result != 1)
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }
        }

        // Request to get the value that this UI element is representing as a string
        string IValueProvider.Value
        {
            get
            {
                return GetValue();
            }
        }

        bool IValueProvider.IsReadOnly
        {
            get
            {
                return IsReadOnly();
            }
        }

        #endregion

        #region Text Pattern Methods

        ITextRangeProvider [] ITextProvider.GetSelection()
        {
            int start, end;
            GetSel(out start, out end);
            return new ITextRangeProvider[] { new WindowsEditBoxRange(this, start, end) };
        }

        ITextRangeProvider [] ITextProvider.GetVisibleRanges()
        {
            int start, end;
            GetVisibleRangePoints(out start, out end);

            return new ITextRangeProvider[] { new WindowsEditBoxRange(this, start, end) };
        }

        ITextRangeProvider ITextProvider.RangeFromChild(IRawElementProviderSimple childElement)
        {
            // we don't have any children so this call must be in error.
            throw new InvalidOperationException(SR.Get(SRID.EditControlsHaveNoChildren,GetType().FullName));
        }

        ITextRangeProvider ITextProvider.RangeFromPoint(Point screenLocation)
        {
            // convert screen to client coordinates.
            // (Essentially ScreenToClient but MapWindowPoints accounts for window mirroring using WS_EX_LAYOUTRTL.)
            NativeMethods.Win32Point clientLocation = (NativeMethods.Win32Point)screenLocation;
            if (!Misc.MapWindowPoints(IntPtr.Zero, WindowHandle, ref clientLocation, 1))
            {
                return null;
            }

            // we have to deal with the possibility that the coordinate is inside the window rect
            // but outside the client rect. in that case we just scoot it over so it is at the nearest
            // point in the client rect.
            NativeMethods.Win32Rect clientRect = new NativeMethods.Win32Rect();
            if (!Misc.GetClientRect(WindowHandle, ref clientRect))
            {
                return null;
            }
            clientLocation.x = Math.Max(clientLocation.x, clientRect.left);
            clientLocation.x = Math.Min(clientLocation.x, clientRect.right);
            clientLocation.y = Math.Max(clientLocation.y, clientRect.top);
            clientLocation.y = Math.Min(clientLocation.y, clientRect.bottom);

            // get the character at those client coordinates
            int start = CharFromPosEx(clientLocation);
            return new WindowsEditBoxRange(this, start, start);
        }

        #endregion TextPattern Methods

        #region TextPattern Properties

        ITextRangeProvider ITextProvider.DocumentRange
        {
            get
            {
                return new WindowsEditBoxRange(this, 0, GetTextLength());
            }
        }

        SupportedTextSelection ITextProvider.SupportedTextSelection
        {
            get
            {
                return SupportedTextSelection.Single;
            }
        }

        #endregion Text Properties

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal bool IsMultiline
        {
            get
            {
                return _type == EditboxType.Multiline;
            }
        }

        internal bool IsScrollable
        {
            get
            {
                return _type == EditboxType.Scrollable;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // send an EM_CHARPOS message with an (x,y) coordinate in lParam and
        // report the returned character index and line number.
        // IMPORTANT: the character index and line number are each 16-bit (half of the lResult).
        // the high-order word is zero.
        internal void CharFromPos(NativeMethods.Win32Point point, out ushort indexLowWord, out ushort lineLowWord)
        {
            Debug.Assert(point.x >= 0 && point.x < 65536, "WindowsEditBox.CharFromPos() x coordinate out of range.");
            Debug.Assert(point.y >= 0 && point.y < 65536, "WindowsEditBox.CharFromPos() y coordinate out of range.");

            // ask edit control for line number and character offset at client coordinates.
            // low order word of return value is the character position.
            // high order word is the zero-based line index.
            IntPtr lParam = NativeMethods.Util.MAKELPARAM(point.x, point.y);
            int result = Misc.ProxySendMessageInt(WindowHandle, NativeMethods.EM_CHARFROMPOS, IntPtr.Zero, lParam);
            indexLowWord = unchecked((ushort)(NativeMethods.Util.LOWORD(result)));
            lineLowWord = unchecked((ushort)(NativeMethods.Util.HIWORD(result)));
        }

        // an improvement on EM_CHARFROMPOS that handles edit controls with content greater than 65535 characters.
        internal int CharFromPosEx(NativeMethods.Win32Point point)
        {
            // get the low words of the character position and line number at the coordinate.
            ushort indexLowWord, lineLowWord;
            CharFromPos(point, out indexLowWord, out lineLowWord);

            // we handle multi-line edit controls differently than single line edit controls.
            int index;
            if (IsMultiline)
            {
                // note: an optimization would be to call GetTextLength and do the following only if the length is
                // greater than 65535.

                // to make the line number accurate in the case there are more than 65535 lines we get a 32-bit line number
                // for the first visible line then use it to figure out the high-order word of our line number.
                // this assumes that there aren't more than 65535 visible lines in the window, which is a
                // pretty safe assumption.
                int line = IntFromLowWord(lineLowWord, GetFirstVisibleLine());

                // to make the character position accurate in case there are more than 65535 characters we use our line number to
                // get the character position of the beginning of that line and use it to figure out the high-order word
                // of our character position.
                // this assumes that there aren't more than 65535 characters on the line before our target character,
                // which is a pretty safe assumption.
                index = IntFromLowWord(indexLowWord, LineIndex(line));
            }
            else
            {
                // single-line edit control

                // to make the character position accurate in case there are more than 65535 characters we get a 32-bit
                // character position from the first visible character, then use it to figure out the high-order word
                // of our character position.
                // this assumes that there aren't more than 65535 visible characters in the window,
                // which is a pretty safe assumption.
                index = IntFromLowWord(indexLowWord, GetFirstVisibleChar());
            }
            return index;
        }

        // Retrive edit box type
        internal static EditboxType GetEditboxtype (IntPtr hwnd)
        {
            int style = Misc.GetWindowStyle(hwnd);
            EditboxType type = EditboxType.Editbox;

            if (Misc.IsBitSet(style, NativeMethods.ES_PASSWORD))
            {
                type = EditboxType.Password;
            }
            else if (Misc.IsBitSet(style, NativeMethods.ES_MULTILINE))
            {
                type = EditboxType.Multiline;
            }
            else if (Misc.IsBitSet(style, NativeMethods.ES_AUTOHSCROLL))
            {
                type = EditboxType.Scrollable;
            }

            return type;
        }

        // send an EM_GETFIRSTVISIBLELINE message to a single-line edit box to get the first visible character.
        internal int GetFirstVisibleChar()
        {
            Debug.Assert(!IsMultiline);
            return Misc.ProxySendMessageInt(WindowHandle, NativeMethods.EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);
        }

        // send an EM_GETFIRSTVISIBLELINE message to a multi-line edit box to get the first visible line.
        internal int GetFirstVisibleLine()
        {
            Debug.Assert(IsMultiline);
            return Misc.ProxySendMessageInt(WindowHandle, NativeMethods.EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);
        }

        // send a WM_GETFONT message to find out what font the edit control is using.
        internal IntPtr GetFont()
        {
            IntPtr result = Misc.ProxySendMessage(WindowHandle, NativeMethods.WM_GETFONT, IntPtr.Zero, IntPtr.Zero);

            //
            // A null result is within normal bounds, as per the WM_GETFONT documentation:
            // The return value is a handle to the font used by the control, or NULL if the control is using the system font.
            //
            if (result == IntPtr.Zero)
            {
                result = UnsafeNativeMethods.GetStockObject(NativeMethods.SYSTEM_FONT);
            }

            return result;
        }

        // send an EM_GETLINECOUNT message to find out how many lines are in the edit box.
        internal int GetLineCount()
        {
            return Misc.ProxySendMessageInt(WindowHandle, NativeMethods.EM_GETLINECOUNT, IntPtr.Zero, IntPtr.Zero);
        }

        internal NativeMethods.LOGFONT GetLogfont()
        {
            IntPtr hfont = GetFont();
            Debug.Assert(hfont != IntPtr.Zero, "WindowsEditBox.GetLogfont got null HFONT");
            NativeMethods.LOGFONT logfont = new NativeMethods.LOGFONT();
            int cb = Marshal.SizeOf(typeof(NativeMethods.LOGFONT));
            if (Misc.GetObjectW(hfont, cb, ref logfont) != cb)
            {
                Debug.Assert(false, "WindowsEditBox.GetObject unexpected return value");
            }
            return logfont;
        }

        // send an EM_GETRECT message to find out the bounding rectangle
        internal Rect GetRect()
        {
            NativeMethods.Win32Rect rect = new NativeMethods.Win32Rect();
            Misc.ProxySendMessage(WindowHandle, NativeMethods.EM_GETRECT, IntPtr.Zero, ref rect);
            return new Rect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
        }

        // send an EM_GETSEL message to get the starting and ending
        // character positions of the selection.
        internal void GetSel(out int start, out int end)
        {
            Misc.ProxySendMessage(WindowHandle, NativeMethods.EM_GETSEL, out start, out end);
        }

        // send a WM_GETTEXT message to get the text of the edit box.
        internal string GetText()
        {
            return Misc.ProxyGetText(_hwnd, GetTextLength());
        }

        // send a WM_GETTEXTLENGTH to find out how long the text is in the edit box.
        internal int GetTextLength()
        {
            return Misc.ProxySendMessageInt(WindowHandle, NativeMethods.WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
        }

        internal void GetVisibleRangePoints(out int start, out int end)
        {
            start = 0;
            end = 0;

            NativeMethods.Win32Rect rect = new NativeMethods.Win32Rect();

            if (Misc.GetClientRect(_hwnd, ref rect) && !rect.IsEmpty)
            {
                NativeMethods.SIZE size;
                string s = new string('E', 1);
                GetTextExtentPoint32(s, out size);

                NativeMethods.Win32Point ptStart = new NativeMethods.Win32Point((int)(rect.left + size.cx / 4), (int)(rect.top + size.cy / 4));
                NativeMethods.Win32Point ptEnd = new NativeMethods.Win32Point((int)(rect.right - size.cx / 8), (int)(rect.bottom - size.cy / 4));

                start = CharFromPosEx(ptStart);
                end = CharFromPosEx(ptEnd);

                if (start > 0)
                {
                    Point pt = PosFromChar(start);
                    if (pt.X < rect.left)
                    {
                        start++;
                    }
                }
            }
            else
            {
                // multi-line edit controls are handled differently than single-line edit controls.

                if (IsMultiline)
                {
                    // get the line number of the first visible line and start the range at
                    // the beginning of that line.
                    int firstLine = GetFirstVisibleLine();
                    start = LineIndex(firstLine);

                    // calculate the line number of the first line scrolled off the bottom and
                    // end the range at the beginning of that line.
                    end = LineIndex(firstLine + LinesPerPage());
                }
                else
                {
                    // single-line edit control

                    // the problem is that using a variable-width font the number of characters visible
                    // depends on the text that is in the edit control.  so we can't just divide the
                    // width of the edit control by the width of a character.

                    // so instead we do a binary search of the characters from the first visible character
                    // to the end of the text to find the visibility boundary.
                    Rect r = GetRect();
                    int limit = GetTextLength();
                    start = GetFirstVisibleChar();

                    int lo = start; // known visible
                    int hi = limit; // known non-visible
                    while (lo + 1 < hi)
                    {
                        int mid = (lo + hi) / 2;

                        Point pt = PosFromChar(mid);
                        if (pt.X >= r.Left && pt.X < r.Right)
                        {
                            lo = mid;
                        }
                        else
                        {
                            hi = mid;
                        }
                    }

                    // trim off one character unless the range is empty or reaches the end.
                    end = hi > start && hi < limit ? hi - 1 : hi;
                }
            }
        }

        // return true iff the user can modify the text in the edit control.
        internal bool IsReadOnly()
        {
            return (!SafeNativeMethods.IsWindowEnabled(WindowHandle) || Misc.IsBitSet(WindowStyle, NativeMethods.ES_READONLY));
        }

        // send an EM_LINEFROMCHAR message to find out what line contains a character position.
        internal int LineFromChar(int index)
        {
            Debug.Assert(index >= 0, "WindowsEditBox.LineFromChar negative index.");
            Debug.Assert(index <= GetTextLength(), "WindowsEditBox.LineFromChar index out of range.");
            // The return of EM_LINEFROMCHAR is the zero-based line number of the line containing the character index.
            return Misc.ProxySendMessageInt(WindowHandle, NativeMethods.EM_LINEFROMCHAR, (IntPtr)index, IntPtr.Zero);
        }

        // send an EM_LINEINDEX message to get the character position at the start of a line.
        internal int LineIndex(int line)
        {
            int index = Misc.ProxySendMessageInt(WindowHandle, NativeMethods.EM_LINEINDEX, (IntPtr)(line), IntPtr.Zero);
            return index >=0 ? index : GetTextLength();
        }

        // send an EM_LINESCROLL message to scroll it horizontally and/or vertically
        internal bool LineScroll(int charactersHorizontal, int linesVertical)
        {
            return 0 != Misc.ProxySendMessageInt(WindowHandle, NativeMethods.EM_LINESCROLL, (IntPtr)charactersHorizontal, (IntPtr)linesVertical);
        }

        // determine the number of lines that are visible in the Edit control.
        // if there is no scrollbar then this is the actual number of lines displayed.
        internal int LinesPerPage()
        {
            int linePerPage = 0;
            if (Misc.IsBitSet(WindowStyle, NativeMethods.WS_VSCROLL))
            {
                // we call GetScrollInfo and return the size of the "page"
                NativeMethods.ScrollInfo si = new NativeMethods.ScrollInfo();
                si.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativeMethods.ScrollInfo));
                si.fMask = NativeMethods.SIF_ALL;
                bool ok = Misc.GetScrollInfo(WindowHandle, NativeMethods.SB_VERT, ref si);
                linePerPage = ok ? si.nPage : 0;
                if (IsMultiline && linePerPage <= 0)
                {
                    linePerPage = 1;
                }
            }
            else
            {
                NativeMethods.Win32Rect rect = new NativeMethods.Win32Rect();

                if (Misc.GetClientRect(_hwnd, ref rect) && !rect.IsEmpty)
                {
                    NativeMethods.SIZE size;
                    string s = new string('E', 1);
                    GetTextExtentPoint32(s, out size);

                    if (size.cy != 0)
                    {
                        linePerPage = (rect.bottom - rect.top) / size.cy;
                    }
                }
            }
            return linePerPage;
        }

        // send an EM_POSFROMCHAR message to find out the (x,y) position of a character.
        // note: this assumes that the x and y coordinates will not be greater than 65535.
        internal Point PosFromChar(int index)
        {
            Debug.Assert(index >= 0, "WindowsEditBox.PosFromChar negative index.");
            Debug.Assert(index < GetTextLength(), "WindowsEditBox.PosFromChar index out of range.");

            int result = Misc.ProxySendMessageInt(WindowHandle, NativeMethods.EM_POSFROMCHAR, (IntPtr)index, IntPtr.Zero);
            Debug.Assert(result!=-1, "WindowsEditBox.PosFromChar index out of bounds.");

            // A returned coordinate can be a negative value if the specified character is not displayed in the
            // edit control's client area.  So do not loose the sign.
            int x = (int)((short)NativeMethods.Util.LOWORD(result));
            int y = (int)((short)NativeMethods.Util.HIWORD(result));
            return new Point(x, y);
        }

        // a variation on EM_POSFROMCHAR that returns the upper-right corner instead of upper-left.
        internal Point PosFromCharUR(int index, string text)
        {
            // get the upper-left of the character
            Point pt;
            char ch = text[index];
            switch(ch)
            {
                case '\n':
                case '\r':
                    // get the UL corner of the character and return it since these characters have no width.
                    pt = PosFromChar(index);
                    break;

                case '\t':
                    {
                        // for tabs the calculated width of the character is no help so we use the
                        // UL corner of the following character if it is on the same line.
                        bool useNext = index<GetTextLength()-1 && LineFromChar(index+1)==LineFromChar(index);
                        pt = PosFromChar(useNext ? index+1 : index);
                    }
                    break;

                default:
                    {
                        // get the UL corner of the character
                        pt = PosFromChar(index);

                        // add the width of the character at that position.
                        NativeMethods.SIZE size;
                        string s = new string(ch, 1);
                        if( GetTextExtentPoint32(s, out size) == 0)
                        {
                            break;
                        }

                        pt.X += size.cx;
                    }
                    break;
            }
            return pt;
        }

        // send an EM_SETSEL message to set the selection.
        internal void SetSel(int start, int end)
        {
            Debug.Assert(start >= 0, "WindowsEditBox.SetSel negative start.");
            Debug.Assert(start <= GetTextLength(), "WindowsEditBox.SetSel start out of range.");
            Debug.Assert(end >= 0, "WindowsEditBox.SetSel negative end.");
            Debug.Assert(end <= GetTextLength(), "WindowsEditBox.SetSel end out of range.");

            Misc.ProxySendMessage(WindowHandle, NativeMethods.EM_SETSEL, (IntPtr)start, (IntPtr)end);
        }

        // Retrieves text associated with EditBox control.
        // this differs from GetText(), above, in that it limits the text to 2000 characters.
        // it is used by the Value pattern methods, not the Text pattern methods.
        internal static string Text(IntPtr hwnd)
        {
            return Misc.ProxyGetText(hwnd);
        }

        #endregion

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        // Different styles of edit boxes.
        internal enum EditboxType
        {
            Editbox,
            Multiline,
            Password,
            Readonly,
            Scrollable
        };

        #endregion

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------

        #region Private methods

        private int GetTextExtentPoint32(string text, out NativeMethods.SIZE size)
        {
            size.cx = 0;
            size.cy = 0;

            // add the width of the character at that position.
            // note: if any of these can throw an exception then we should use a finally clause
            // to ensure the DC is released.
            IntPtr hdc = Misc.GetDC(_hwnd);
            if (hdc == IntPtr.Zero)
            {
                return 0;
            }

            IntPtr oldFont = IntPtr.Zero;
            try
            {
                IntPtr hfont = GetFont();
                oldFont = Misc.SelectObject(hdc, hfont);
                return Misc.GetTextExtentPoint32(hdc, text, text.Length, out size);
            }
            finally
            {
                if (oldFont != IntPtr.Zero)
                {
                    Misc.SelectObject(hdc, oldFont);
                }

                Misc.ReleaseDC(_hwnd, hdc);
            }
        }

        private string GetValue()
        {
            if (_type == EditboxType.Password)
            {
                // Trying to retrieve the text (through WM_GETTEXT) from an edit box that has
                // the ES_PASSWORD style throws an InvalidOperationException.
                throw new InvalidOperationException();
            }

            return Text(_hwnd);
        }

        private bool IsInsideOfCombo()
        {
            IntPtr hwndParent = NativeMethodsSetLastError.GetAncestor(_hwnd, NativeMethods.GA_PARENT);

            if (hwndParent == IntPtr.Zero)
            {
                return false;
            }

            // Test for combo
            NativeMethods.COMBOBOXINFO cbInfo = new NativeMethods.COMBOBOXINFO(NativeMethods.comboboxInfoSize);

            if (WindowsComboBox.GetComboInfo(hwndParent, ref cbInfo) && cbInfo.hwndItem == _hwnd)
            {
                return true;
            }

            return false;
        }

        private bool IsInsideOfSpinner()
        {
            IntPtr hwndParent = NativeMethodsSetLastError.GetAncestor(_hwnd, NativeMethods.GA_PARENT);

            if (hwndParent != IntPtr.Zero)
            {
                // Test for spinner - Create checks if the element is a spinner
                if (WinformsSpinner.Create(hwndParent, 0) != null)
                {
                    return true;
                }
            }

            return WindowsSpinner.IsSpinnerEdit(_hwnd);
        }

        private static bool IsInsideOfIPAddress(IntPtr hwnd)
        {
            IntPtr hwndParent = NativeMethodsSetLastError.GetAncestor(hwnd, NativeMethods.GA_PARENT);

            if (hwndParent != IntPtr.Zero)
            {
                string classname = Misc.GetClassName(hwndParent);
                if (classname.Equals("SysIPAddress32"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInsideOfListView(IntPtr hwnd)
        {
            IntPtr hwndParent = NativeMethodsSetLastError.GetAncestor(hwnd, NativeMethods.GA_PARENT);

            if (hwndParent != IntPtr.Zero)
            {
                string classname = Misc.GetClassName(hwndParent);
                if (classname.Equals("SysListView32"))
                {
                    return true;
                }
            }

            return false;
        }

        // given the low-order word of a non-negative integer that is at most 0xffff greater than some floor integer
        // we calculate what the original integer was.
        private static int IntFromLowWord(ushort lowWord, int floor)
        {
            // get the high-word from our floor integer
            Debug.Assert(floor >= 0);
            short hiWord = (short)(floor >> 16);

            // if our unknown integer has cross a 64k boundary from our floor integer
            // then we need to increment the high-word
            ushort floorLowWord = (ushort)(floor & 0xffff);
            if (lowWord < floorLowWord)
                hiWord++;

            // combine the adjusted floor high-word with the known low-word to get our integer
            int i = NativeMethods.Util.MAKELONG((int)lowWord, (int)hiWord);
            Debug.Assert(i >= floor);

            return i;
        }

        #endregion Private methods

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------

        #region Private Fields

        private EditboxType _type;

        #endregion
    }
}
