// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: HWND-based RichEdit Proxy

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Text;
using Accessibility;
using MS.Win32;
using NativeMethodsSetLastError = MS.Internal.UIAutomationClientSideProviders.NativeMethodsSetLastError;

namespace MS.Internal.AutomationProxies
{
    class WindowsRichEdit : ProxyHwnd, IValueProvider, ITextProvider
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        WindowsRichEdit (IntPtr hwnd, ProxyFragment parent, int style)
            : base( hwnd, parent, style )
        {
            _type = WindowsEditBox.GetEditboxtype(hwnd);
            if (IsMultiline)
            {
                _cControlType = ControlType.Document;
            }
            else
            {
                _cControlType = ControlType.Edit;
            }

            _fIsKeyboardFocusable = true;

            // support for events
            _createOnEvent = new WinEventTracker.ProxyRaiseEvents (RaiseEvents);
        }

        #endregion Initialization

        #region Proxy Create

        // Static Create method called by UIAutomation to create this proxy.
        // returns null if unsuccessful
        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild, int idObject)
        {
            return Create(hwnd, idChild);
        }

        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild)
        {
            // Something is wrong if idChild is not zero
            if (idChild != 0)
            {
                Debug.Assert (idChild == 0, "Invalid Child Id, idChild != 0");
                throw new ArgumentOutOfRangeException("idChild", idChild, SR.Get(SRID.ShouldBeZero));
            }

            return new WindowsRichEdit(hwnd, null, 0);
        }

        // Static Create method called by the event tracker system
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                WindowsRichEdit wtv = new WindowsRichEdit (hwnd, null, 0);

                // If this event means the selection may have changed, raise that event
                // here instead of relying on the generic DispatchEvents mechanism below.
                // The RichTextEdit provider needs to handle this event specially.
                if (eventId == NativeMethods.EventObjectLocationChange
                    && idObject == NativeMethods.OBJID_CARET)
                {
                    wtv.RaiseTextSelectionEvent(eventId, idProp, idObject, idChild);
                }
                else
                {
                    wtv.DispatchEvents(eventId, idProp, idObject, idChild);
                }
            }
        }

        #endregion Proxy Create

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider(AutomationPattern iid)
        {
            try
            {
                if (iid == ValuePattern.Pattern && !IsMultiline)
                {
                    if (IsDocument())
                    {
                        EnsureTextDocument();
                    }
                    return this;
                }

                // text pattern is supported for non-password boxes
                if (iid == TextPattern.Pattern && _type != WindowsEditBox.EditboxType.Password && IsDocument())
                {
                    EnsureTextDocument();
                    return this;
                }

                return null;
            }
                //If tom.dll is missing
            catch (System.IO.FileNotFoundException)
            {
                return null;
            }
        }

        // Process all the Logical and Raw Element Properties
        internal override object GetElementProperty(AutomationProperty idProp)
        {
            if (idProp == AutomationElement.IsPasswordProperty)
            {
                return _type == WindowsEditBox.EditboxType.Password;
            }
            else if (idProp == AutomationElement.NameProperty)
            {
                // Per ControlType.Edit spec if there is no static text
                // label for an Edit then it must NEVER return the contents
                // of the edit as the Name property. This relies on the
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
        #endregion ProxySimple Interface

        #region Value Pattern

        // Sets the text of the edit.
        void IValueProvider.SetValue(string str)
        {
            // Check if the window is disabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            if (Misc.IsBitSet(WindowStyle, NativeMethods.ES_READONLY))
            {
                throw new InvalidOperationException(SR.Get(SRID.ValueReadonly));
            }

            // check if control only accepts numbers
            if (Misc.IsBitSet(WindowStyle, NativeMethods.ES_NUMBER) && !WindowsFormsHelper.IsWindowsFormsControl(_hwnd))
            {
                // check if string contains any non-numeric characters.
                foreach (char ch in str)
                {
                    if (char.IsLetter(ch))
                    {
                        throw new ArgumentException(SR.Get(SRID.NotAValidValue, str), "val");
                    }
                }
            }

            // Text/edit box should not enter more characters than what is allowed through keyboard.
            // Determine the max number of chars this editbox accepts

            int result = Misc.ProxySendMessageInt(_hwnd, NativeMethods.EM_GETLIMITTEXT, IntPtr.Zero, IntPtr.Zero);
            if (result < str.Length)
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
            // we must have called EnsureTextDocument() before arriving here.
            Debug.Assert(_document != null);

            // clone a range from the documents selection
            ITextRange range = null;
            ITextSelection selection = _document.Selection;
            if (selection != null)
            {
                // duplicate the selection range since we don't want their modifications to affect the selection
                range = selection.GetDuplicate();

                // for future reference: active endpoint is
                // ((selection.Flags & TomSelectionFlags.tomSelStartActive) == TomSelectionFlags.tomSelStartActive) ? TextPatternRangeEndpoint.Start : TextPatternRangeEndpoint.End;
            }

            if (range == null)
                return new ITextRangeProvider[] { };
            else
                return new ITextRangeProvider[] { new WindowsRichEditRange(range, this) };
        }

        ITextRangeProvider [] ITextProvider.GetVisibleRanges()
        {
            ITextRange range = GetVisibleRange();

            if (range == null)
                return new ITextRangeProvider[] { };
            else
                return new ITextRangeProvider[] { new WindowsRichEditRange(range, this) };
        }

        ITextRangeProvider ITextProvider.RangeFromChild(IRawElementProviderSimple childElement)
        {
            // we don't have any children so this call must be in error.
            // if we implement children for hyperlinks and embedded objects then we'll need to change this.
            throw new InvalidOperationException(SR.Get(SRID.RichEditTextPatternHasNoChildren,GetType().FullName));
        }

        ITextRangeProvider ITextProvider.RangeFromPoint(Point screenLocation)
        {
            // we must have called EnsureTextDocument() before arriving here.
            Debug.Assert(_document != null);

            // TextPattern has verified that the point is inside our client area so we don't need to check for that.

            // get the degenerate range at the point
            // we're assuming ITextDocument::RangeFromPoint always returns a degenerate range
            ITextRange range = _document.RangeFromPoint((int)screenLocation.X, (int)screenLocation.Y);
            Debug.Assert(range.Start == range.End);

            // if you wanted to get the character under the point instead of the degenerate range nearest
            // the point, then you would add:

            //// if the point is within the character to the right then expand the degenerate range to
            //// include the character.
            //Rect rect;
            //range.MoveEnd(TomUnit.tomCharacter, 1);
            //rect = WindowsRichEditRange.GetSingleLineRangeRectangle(range, BoundingRectangle);
            //if (!rect.Contains(screenLocation))
            //{
            //    // if the point is within the character to the left then expand the degenerate range
            //    // to include the character.
            //    range.Collapse(TomStartEnd.tomStart);
            //    range.MoveStart(TomUnit.tomCharacter, -1);
            //    rect = WindowsRichEditRange.GetSingleLineRangeRectangle(range, BoundingRectangle);
            //    if (!rect.Contains(screenLocation))
            //    {
            //        // the point is not in an adjacent character cell so leave it degenerate.
            //        range.Collapse(TomStartEnd.tomEnd);
            //    }
            //}

            return range != null ? new WindowsRichEditRange(range, this) : null;
        }

        #endregion TextPattern Methods

        #region TextPattern Properties

        ITextRangeProvider ITextProvider.DocumentRange
        {
            get
            {
                // we must have called EnsureTextDocument() before arriving here.
                Debug.Assert(_document != null);

                // create a text range that covers the entire main story.
                ITextRange range = _document.Range(0, 0);
                range.SetRange(0, range.StoryLength);
                return new WindowsRichEditRange(range, this);
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
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // returns the range that is visible in the RichEdit window
        internal ITextRange GetVisibleRange()
        {
            // get a range from the center point of the client rectangle
            Rect rect = BoundingRectangle;
            int x = ((int)rect.Left + (int)rect.Right) / 2;
            int y = ((int)rect.Top + (int)rect.Bottom) / 2;
            ITextRange range = _document.RangeFromPoint(x, y);

            // expand it to fill the window.
            range.Expand(TomUnit.tomWindow);

            // There is a bug with RichEdit 3.0.  The expand to tomWindow may gets 0 as the range's cpBegin (Start).
            // So need to trim off what is outside of the window.
            int start = range.Start;
            // The ITextRange::SetRange method sets this range's Start = min(cp1, cp2) and End = max(cp1, cp2).
            // If the range is a nondegenerate selection, cp2 is the active end; if it's a degenerate selection,
            // the ambiguous cp is displayed at the start of the line (rather than at the end of the previous line).
            // Set the end to the start and the start to the end to create an ambiguous cp.
            range.SetRange(range.End, range.Start);
            bool gotPoint = WindowsRichEditRange.RangeGetPoint(range, TomGetPoint.tomStart, out x, out y);
            while (!gotPoint || !Misc.PtInRect(ref rect, x, y))
            {
                range.MoveStart(TomUnit.tomWord, 1);
                gotPoint = WindowsRichEditRange.RangeGetPoint(range, TomGetPoint.tomStart, out x, out y);
            }

            if (start != range.Start)
            {
                // The trimming was done based on the left edge of the range.  The last visiable partial
                // character/word has been also added back into the range, need to remove it.  Do the comparing
                // against the characters right edge and the window rectangle.
                ITextRange rangeAdjust = _document.Range(0, range.Start - 1);
                gotPoint = WindowsRichEditRange.RangeGetPoint(rangeAdjust, TomGetPoint.TA_BOTTOM | TomGetPoint.TA_RIGHT, out x, out y);

                while (gotPoint && Misc.PtInRect(ref rect, x, y) && rangeAdjust.Start != rangeAdjust.End)
                {
                    rangeAdjust.MoveEnd(TomUnit.tomCharacter, -1);
                    range.MoveStart(TomUnit.tomCharacter, -1);
                    gotPoint = WindowsRichEditRange.RangeGetPoint(rangeAdjust, TomGetPoint.TA_BOTTOM | TomGetPoint.TA_RIGHT, out x, out y);
                }
            }

            // There is a bug with RichEdit 3.0.  The expand to tomWindow gets the last cp of the bottom
            // line in the window as the range's cpLim (End).  The cpLim may be passed the right side of
            // the window.
            // So need to trim off what is on the right side of the window.
            int end = range.End;
            gotPoint = WindowsRichEditRange.RangeGetPoint(range, TomGetPoint.TA_RIGHT, out x, out y);
            while (!gotPoint || !Misc.PtInRect(ref rect, x, y))
            {
                range.MoveEnd(TomUnit.tomWord, -1);
                gotPoint = WindowsRichEditRange.RangeGetPoint(range, TomGetPoint.TA_RIGHT, out x, out y);
            }

            if (end != range.End)
            {
                // The trimming was done based on the right edge of the range.  The last visiable partial
                // character/word has been also trimmed so add it back to the range.  Do the comparing
                // against the characters left edge and the window rectangle.
                ITextRange rangeAdjust = _document.Range(range.End, end);
                do
                {
                    if (range.MoveEnd(TomUnit.tomCharacter, 1) == 0)
                    {
                        break;
                    }
                    rangeAdjust.MoveStart(TomUnit.tomCharacter, 1);
                    gotPoint = WindowsRichEditRange.RangeGetPoint(rangeAdjust, TomGetPoint.tomStart, out x, out y);
                } while (gotPoint && Misc.PtInRect(ref rect, x, y));
            }

            return range;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal ITextDocument Document
        {
            get
            {
                return _document;
            }
        }

        internal bool IsMultiline
        {
            get
            {
                return _type == WindowsEditBox.EditboxType.Multiline;
            }
        }

        internal bool ReadOnly
        {
            get
            {
                return (WindowStyle & NativeMethods.ES_READONLY) == NativeMethods.ES_READONLY;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // return true if the user can modify the text in the edit control.
        internal bool IsReadOnly()
        {
            return (!SafeNativeMethods.IsWindowEnabled(WindowHandle) ||
                    Misc.IsBitSet(WindowStyle, NativeMethods.ES_READONLY));
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // call this method before allowing access to any TextPattern properties or methods.
        // it ensures that we have an ITextDocument pointer to the richedit control.
        private void EnsureTextDocument()
        {
            // if we don't have a document pointer yet then get one by sending richedit a WM_GETOBJECT message
            // with object id asking for the native OM.
            if (_document == null)
            {
                object obj = null;

                if (UnsafeNativeMethods.AccessibleObjectFromWindow(WindowHandle, NativeMethods.OBJID_NATIVEOM, ref UnsafeNativeMethods.IID_IDispatch, ref obj) != NativeMethods.S_OK)
                {
                    throw new System.NotImplementedException(SR.Get(SRID.NoITextDocumentFromRichEdit));
                }

                // This is temp solution which will prevent exception in the case
                // when we cannot obtain the ITextDocument from a RichEdit control
                // The direct cast does not work for RichEdit20w controls used in MS Office
                _document = obj as ITextDocument;
                if (_document == null)
                {
                    throw new System.NotImplementedException(SR.Get(SRID.NoITextDocumentFromRichEdit));
                }
            }
        }

        private string GetValue()
        {
            if (_type == WindowsEditBox.EditboxType.Password)
            {
                // Trying to retrieve the text (through WM_GETTEXT) from an edit box that has
                // the ES_PASSWORD style throws an UnauthorizedAccessException.
                throw new UnauthorizedAccessException();
            }

            if (IsDocument())
            {
                ITextRange range = _document.Range(0, 0);
                int start = 0;
                int end = range.StoryLength;

                range.SetRange(start, end);

                string text = range.Text;
                // Empty edits contain a degenerate/empty range, and will return null
                // for their text - treat this as "", not null, since we do want to expose
                // a non-null value.
                if (string.IsNullOrEmpty(text))
                {
                    return "";
                }

                int embeddedObjectOffset = text.IndexOf((char)0xFFFC);
                if (embeddedObjectOffset != -1)
                {
                    StringBuilder sbText = new StringBuilder();
                    object embeddedObject;
                    while (start < end && embeddedObjectOffset != -1)
                    {
                        sbText.Append(text.Substring(start, embeddedObjectOffset - start));
                        range.SetRange(embeddedObjectOffset, end);
                        if (range.GetEmbeddedObject(out embeddedObject) == NativeMethods.S_OK && embeddedObject != null)
                        {
                            GetEmbeddedObjectText(embeddedObject, sbText);
                        }
                        else
                        {
                            // If there is some kind of error, just append a space to the text.  In this way
                            // we will be no worse of then before implementing the embedded object get text.
                            sbText.Append(" ");
                        }
                        start = embeddedObjectOffset + 1;
                        embeddedObjectOffset = text.IndexOf((char)0xFFFC, start);
                    }

                    if (start < end)
                    {
                        sbText.Append(text.Substring(start, end - start));
                    }

                    text = sbText.ToString();
                }

                return text;
            }
            else
            {
                return Misc.ProxyGetText(_hwnd);
            }
        }

        private void GetEmbeddedObjectText(object embeddedObject, StringBuilder sbText)
        {
            string text;

            IAccessible acc = embeddedObject as IAccessible;
            if (acc != null)
            {
                text = acc.get_accName(NativeMethods.CHILD_SELF);
                if (!string.IsNullOrEmpty(text))
                {
                    sbText.Append(text);
                    return;
                }
            }

            // Didn't get IAccessible (or didn't get a name from it).
            // Try the IDataObject technique instead...

            int hr = NativeMethods.S_FALSE;
            IDataObject dataObject = null;
            IOleObject oleObject = embeddedObject as IOleObject;
            if (oleObject != null)
            {
                // Try IOleObject::GetClipboardData (which returns an IDataObject) first...
                hr = oleObject.GetClipboardData(0, out dataObject);
            }

            // If that didn't work, try the embeddedObject as a IDataObject instead...
            if (hr != NativeMethods.S_OK)
            {
                dataObject = embeddedObject as IDataObject;
            }

            if (dataObject == null)
            {
                return;
            }

            // Got the IDataObject. Now query it for text formats. Try Unicode first...

            bool fGotUnicode = true;

            UnsafeNativeMethods.FORMATETC fetc = new UnsafeNativeMethods.FORMATETC();
            fetc.cfFormat = DataObjectConstants.CF_UNICODETEXT;
            fetc.ptd = IntPtr.Zero;
            fetc.dwAspect = DataObjectConstants.DVASPECT_CONTENT;
            fetc.lindex = -1;
            fetc.tymed = DataObjectConstants.TYMED_HGLOBAL;

            UnsafeNativeMethods.STGMEDIUM med = new UnsafeNativeMethods.STGMEDIUM();
            med.tymed = DataObjectConstants.TYMED_HGLOBAL;
            med.pUnkForRelease = IntPtr.Zero;
            med.hGlobal = IntPtr.Zero;

            hr = dataObject.GetData(ref fetc, ref med);

            if (hr != NativeMethods.S_OK || med.hGlobal == IntPtr.Zero)
            {
                // If we didn't get Unicode, try for ANSI instead...
                fGotUnicode = false;
                fetc.cfFormat = DataObjectConstants.CF_TEXT;

                hr = dataObject.GetData(ref fetc, ref med);
            }

            // Did we get anything?
            if (hr != NativeMethods.S_OK || med.hGlobal == IntPtr.Zero)
            {
                return;
            }

            //lock the memory, so data can be copied into
            IntPtr globalMem = UnsafeNativeMethods.GlobalLock(med.hGlobal);

            try
            {
                //error check for the memory pointer
                if (globalMem == IntPtr.Zero)
                {
                    return;
                }

                unsafe
                {
                    //get the string
                    if (fGotUnicode)
                    {
                        text = new string((char*)globalMem);
                    }
                    else
                    {
                        text = new string((sbyte*)globalMem);
                    }
                }

                sbText.Append(text);
            }
            finally
            {
                //unlock the memory
                UnsafeNativeMethods.GlobalUnlock(med.hGlobal);
                UnsafeNativeMethods.ReleaseStgMedium(ref med);
            }
        }

        private bool IsDocument()
        {
            return !OnCommandBar();
        }

        private bool OnCommandBar()
        {
            IntPtr hwndParent = NativeMethodsSetLastError.GetAncestor(_hwnd, NativeMethods.GA_PARENT);

            if (hwndParent != IntPtr.Zero)
            {
                return Misc.GetClassName(hwndParent).Equals("MsoCommandBar");
            }
            return false;
        }

        // Raise the TextSelectionChanged event, if appropriate.
        private void RaiseTextSelectionEvent(int eventId, object idProp, int idObject, int idChild)
        {
            // We cannot use the generic DispatchEvents mechanism to raise
            // the TextSelection event, since DispatchEvents() will eventually call
            // EventManager.HandleTextSelectionChangedEvent(),
            // which will use logic inappropriate for the RichTextEdit control:
            // namely, that wtv._document.Selection.GetSelection() returns
            // an ITextRange which can *change* when e.g. backspace is pressed
            // in the control.  To reliably detect an actual change in selection,
            // we have to perform a specific test of the old and new selection
            // endpoints and raise the event ourselves if appropriate.

            bool raiseTextSelectionEvent = false;

            EnsureTextDocument();
            ITextSelection textSelection = _document.Selection;
            if (textSelection != null)
            {
                raiseTextSelectionEvent =
                    (_raiseEventsOldSelectionStart != textSelection.Start
                        || _raiseEventsOldSelectionEnd != textSelection.End);

                _raiseEventsOldSelectionStart = textSelection.Start;
                _raiseEventsOldSelectionEnd = textSelection.End;
            }
            else
            {
                raiseTextSelectionEvent =
                    (_raiseEventsOldSelectionStart != _NO_ENDPOINT
                        || _raiseEventsOldSelectionEnd != _NO_ENDPOINT);

                _raiseEventsOldSelectionStart = _NO_ENDPOINT;
                _raiseEventsOldSelectionEnd = _NO_ENDPOINT;
            }

            if (raiseTextSelectionEvent)
            {
                AutomationInteropProvider.RaiseAutomationEvent(
                    TextPattern.TextSelectionChangedEvent,
                    this, new AutomationEventArgs(TextPattern.TextSelectionChangedEvent));
            }
        }

        #endregion private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private ITextDocument _document;
        private WindowsEditBox.EditboxType _type;

        // Used in RaiseEvents() to track changes in the selection endpoints.
        static private int _raiseEventsOldSelectionStart;
        static private int _raiseEventsOldSelectionEnd;
        private const int _NO_ENDPOINT = -1;

        #endregion private Fields
    }
}
