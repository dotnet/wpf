// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//  This class handles IMM32 IME's composition string and support level 3 input to TextBox and RichTextBox.
//

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Security;
using System.Text;
using MS.Win32;
using MS.Internal.Documents;
using MS.Internal.PresentationFramework;
using MS.Internal;
using MS.Internal.Interop;

// Enable presharp pragma warning suppress directives.
#pragma warning disable 1634, 1691

namespace System.Windows.Documents
{
    //------------------------------------------------------
    //
    //  ImmComposition class
    //
    //------------------------------------------------------

    //
    // This class handles IMM32 IME's composition string and
    // support level 3 input to TextBox and RichTextBox.
    //
    internal class ImmComposition
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        //
        // Creates a new ImmComposition instance.
        //
        static ImmComposition()
        {
        }

        //
        // Creates a new ImmComposition instance.
        //
        internal ImmComposition(HwndSource source)
        {
            UpdateSource(null, source);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Method
        //
        //------------------------------------------------------

        //
        // Create an instance of ImmComposition per source window.
        //
        internal static ImmComposition GetImmComposition(FrameworkElement scope)
        {
            HwndSource source = PresentationSource.CriticalFromVisual(scope) as HwndSource;

            ImmComposition immComposition = null;

            if (source != null)
            {
                lock (_list)
                {
                    immComposition = (ImmComposition)_list[source];

                    if (immComposition == null)
                    {
                        immComposition = new ImmComposition(source);
                        _list[source] = immComposition;
                    }
                }
            }

            return immComposition;
        }

        //
        // This is called when TextEditor is detached.
        // We need to remove event handlers.
        //
        internal void OnDetach(TextEditor editor)
        {
            if (editor != _editor)
            {
                // ignore calls from editors that aren't the one we're attached to
                return;
            }

            if (_editor != null)
            {
                PresentationSource.RemoveSourceChangedHandler(UiScope, new SourceChangedEventHandler(OnSourceChanged));
                _editor.TextContainer.Change -= new TextContainerChangeEventHandler(OnTextContainerChange);
            }

            _editor = null;
        }

        //
        // Callback from TextEditor when it gets focus.
        //
        internal void OnGotFocus(TextEditor editor)
        {
            if (editor == _editor)
            {
                // If an event listener does a reentrant SetFocus, we can get
                // here without a matching OnLostFocus.  Early out so
                // that we don't attach too many handlers.
                return;
            }

            // remove source changed handler for previous editor.
            if (_editor != null)
            {
                PresentationSource.RemoveSourceChangedHandler(UiScope, new SourceChangedEventHandler(OnSourceChanged));
                _editor.TextContainer.Change -= new TextContainerChangeEventHandler(OnTextContainerChange);
            }

            // Update the current focus TextEditor, RenderScope and UiScope.
            _editor = editor;

            // we need to track the source change.
            PresentationSource.AddSourceChangedHandler(UiScope, new SourceChangedEventHandler(OnSourceChanged));

            _editor.TextContainer.Change += new TextContainerChangeEventHandler(OnTextContainerChange);

            // Update the current composition window position.
            UpdateNearCaretCompositionWindow();
        }

        //
        // Callback from TextEditor when it lost focus.
        //
        internal void OnLostFocus()
        {
            if (_editor == null)
                return;

            _losingFocus = true;
            try
            {
                // complete the composition string when it lost focus.
                CompleteComposition();
            }
            finally
            {
                _losingFocus = false;
            }
        }

        //
        // Callback from TextEditor when the layout is updated
        //
        internal void OnLayoutUpdated()
        {
            if (_updateCompWndPosAtNextLayoutUpdate && IsReadingWindowIme())
            {
                UpdateNearCaretCompositionWindow();
            }
            _updateCompWndPosAtNextLayoutUpdate = false;
        }

        //
        // complete the composition string by calling ImmNotifyIME.
        //
        internal void CompleteComposition()
        {
            UnregisterMouseListeners();

            if (_source == null)
            {
                // Do nothing if HwndSource is already gone(disposed) or disconnected.
                return;
            }

            _compositionModifiedByEventListener = true;

            IntPtr hwnd = IntPtr.Zero;

            hwnd = ((IWin32Window)_source).Handle;

            IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));
            if (himc != IntPtr.Zero)
            {
                UnsafeNativeMethods.ImmNotifyIME(new HandleRef(this, himc), NativeMethods.NI_COMPOSITIONSTR, NativeMethods.CPS_COMPLETE, 0);

                UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));
            }

            if (_compositionAdorner != null)
            {
                _compositionAdorner.Uninitialize();
                _compositionAdorner = null;
            }

            _startComposition = null;
            _endComposition = null;
        }

        // Called as the selection changes.
        // We can't modify document state here in any way.
        internal void OnSelectionChange()
        {
            _compositionModifiedByEventListener = true;
        }

        // Callback for TextSelection.Changed event.
        internal void OnSelectionChanged()
        {
            if (!this.IsInKeyboardFocus)
            {
                return;
            }

            // Update the current composition window position.
            UpdateNearCaretCompositionWindow();
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        //
        // Returns true if we're in the middle of an ongoing composition.
        //
        internal bool IsComposition
        {
            get
            {
                return _startComposition != null;
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        // SourceChanged callback
        //
        private void OnSourceChanged(object sender, SourceChangedEventArgs e)
        {
            HwndSource newSource = null;
            HwndSource oldSource = null;

            newSource = e.NewSource as HwndSource;
            oldSource = e.OldSource as HwndSource;

            UpdateSource(oldSource, newSource);

            // Clean up the old source changed event handler that was connected with UiScope.
            if (oldSource != null && UiScope != null)
            {
                // Remove the source changed event handler here.
                // Ohterwise, we'll get the leak of the SourceChangedEventHandler.
                // New source changed event handler will be added by getting OnGotFocus on new UiScope.
                PresentationSource.RemoveSourceChangedHandler(UiScope, new SourceChangedEventHandler(OnSourceChanged));
            }
        }

        //
        // Update _list and _source with new source.
        //
        private void UpdateSource(HwndSource oldSource, HwndSource newSource)
        {
            // If this object is moving directly from one source to another
            // without passing through null, problems could arise.  This object
            // becomes the ImmComposition for the new source (_list[newSource] = this),
            // but the new source may already have an ImmComposition.  This can
            // lead to confusion when handling composition messages or when
            // shutting down the source.
            // Fortunately this can't happen - the visual tree doesn't support re-parenting
            // in one step (you have to detach child, then reattach it to a new parent).
            // If that should change, this method will need some rethinking.
            Debug.Assert(oldSource == null || newSource == null,
                        "ImmComposition doesn't support changing source directly");

            // Detach the TextEditor.  This avoids leaks and crashes (DevDiv2 1162020, 1201925).
            OnDetach(_editor);

            if (_source != null)
            {
                Debug.Assert((oldSource == null) || (oldSource == _source));

                _source.RemoveHook(new HwndSourceHook(ImmCompositionFilterMessage));

                _source.Disposed -= new EventHandler(OnHwndDisposed);

                // Remove HwndSource from the list.
                _list.Remove(_source);
                _source = null;
            }

            if (newSource != null)
            {
                _list[newSource] = this;
                _source = newSource;
                _source.AddHook(new HwndSourceHook(ImmCompositionFilterMessage));
                _source.Disposed += new EventHandler(OnHwndDisposed);
            }

            // _source should always be a newSource.
            Debug.Assert(newSource == _source);
        }

        //
        // Window Hook to track WM_IME_ messages.
        //
        private IntPtr ImmCompositionFilterMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            IntPtr lret = IntPtr.Zero;
            switch ((WindowMessage)msg)
            {
                case WindowMessage.WM_IME_CHAR:
                    OnWmImeChar(wParam, ref handled);
                    break;

                case WindowMessage.WM_IME_NOTIFY:
                    // we don't have to update handled.
                    OnWmImeNotify(hwnd, wParam);
                    break;

                case WindowMessage.WM_IME_STARTCOMPOSITION:
                case WindowMessage.WM_IME_ENDCOMPOSITION:
                    if (IsInKeyboardFocus && !IsReadOnly)
                    {
                        // Do Level 2 for legacy Chinese IMM32 IMEs.
                        if (!IsReadingWindowIme())
                        {
                            handled = true;
                        }
                    }

                    break;

                case WindowMessage.WM_IME_COMPOSITION:
                    OnWmImeComposition(hwnd, lParam, ref handled);
                    break;

                case WindowMessage.WM_IME_REQUEST:
                    lret = OnWmImeRequest(wParam, lParam, ref handled);
                    break;

                case WindowMessage.WM_INPUTLANGCHANGE:
                    // Set the composition window position (reading window position) for
                    // legacy Chinese IMM32 IMEs.
                    if (IsReadingWindowIme())
                    {
                        UpdateNearCaretCompositionWindow();
                    }
                    break;
            }

            return lret;
        }

        //
        // WM_IME_COMPOSITION handler
        //
        private void OnWmImeComposition(IntPtr hwnd, IntPtr lParam, ref bool handled)
        {
            IntPtr himc;
            int size;
            int cursorPos = 0;
            int deltaStart = 0;
            char[] result = null;
            char[] composition = null;
            byte[] attributes = null;

            if (IsReadingWindowIme())
            {
                // Don't handle WM_IME_COMPOSITION for Chinese Legacy IMEs.
                return;
            }

            if (!IsInKeyboardFocus && !_losingFocus)
            {
                // Don't handle WM_IME_COMPOSITION if we don't have a focus.
                return;
            }

            if (IsReadOnly)
            {
                // Don't handle WM_IME_COMPOSITION if it is readonly.
                return;
            }


            himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));
            if (himc == IntPtr.Zero)
            {
                // we don't do anything with NULL-HIMC.
                return;
            }

            //
            // Get the result string from hIMC.
            //
            if (((int)lParam & NativeMethods.GCS_RESULTSTR) != 0)
            {
                size = UnsafeNativeMethods.ImmGetCompositionString(new HandleRef(this, himc), NativeMethods.GCS_RESULTSTR, IntPtr.Zero, 0);
                if (size > 0)
                {
                    result = new char[size / Marshal.SizeOf(typeof(short))];

                    // 3rd param is out and contains actual result of this call.
                    // suppress Presharp 6031.
#pragma warning suppress 6031
                    UnsafeNativeMethods.ImmGetCompositionString(new HandleRef(this, himc), NativeMethods.GCS_RESULTSTR, result, size);
                }
            }

            //
            // Get the composition string from hIMC.
            //
            if (((int)lParam & NativeMethods.GCS_COMPSTR) != 0)
            {
                size = UnsafeNativeMethods.ImmGetCompositionString(new HandleRef(this, himc), NativeMethods.GCS_COMPSTR, IntPtr.Zero, 0);
                if (size > 0)
                {
                    composition = new char[size / Marshal.SizeOf(typeof(short))];
                    // 3rd param is out and contains actual result of this call.
                    // suppress Presharp 6031.
#pragma warning suppress 6031
                    UnsafeNativeMethods.ImmGetCompositionString(new HandleRef(this, himc), NativeMethods.GCS_COMPSTR, composition, size);

                    //
                    // Get the caret position from hIMC.
                    //
                    if (((int)lParam & NativeMethods.GCS_CURSORPOS) != 0)
                    {
                        cursorPos = UnsafeNativeMethods.ImmGetCompositionString(new HandleRef(this, himc), NativeMethods.GCS_CURSORPOS, IntPtr.Zero, 0);
                    }

                    //
                    // Get the delta start position from hIMC.
                    //
                    if (((int)lParam & NativeMethods.GCS_DELTASTART) != 0)
                    {
                        deltaStart = UnsafeNativeMethods.ImmGetCompositionString(new HandleRef(this, himc), NativeMethods.GCS_DELTASTART, IntPtr.Zero, 0);
                    }

                    //
                    // Get the attribute information from hIMC.
                    //
                    if (((int)lParam & NativeMethods.GCS_COMPATTR) != 0)
                    {
                        size = UnsafeNativeMethods.ImmGetCompositionString(new HandleRef(this, himc), NativeMethods.GCS_COMPATTR, IntPtr.Zero, 0);
                        if (size > 0)
                        {
                            attributes = new byte[size / Marshal.SizeOf(typeof(byte))];
                            // 3rd param is out and contains actual result of this call.
                            // suppress Presharp 6031.
#pragma warning suppress 6031
                            UnsafeNativeMethods.ImmGetCompositionString(new HandleRef(this, himc), NativeMethods.GCS_COMPATTR, attributes, size);
                        }
                    }
                }
            }

            UpdateCompositionString(result, composition, cursorPos, deltaStart, attributes);

            UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));
            handled = true;
        }

        //
        // WM_IME_CHAR handler
        //
        private void OnWmImeChar(IntPtr wParam, ref bool handled)
        {
            if (!IsInKeyboardFocus && !_losingFocus)
            {
                // Don't handle WM_IME_CAHR if we don't have a focus.
                return;
            }

            if (IsReadOnly)
            {
                // Don't handle WM_IME_CAHR if it is readonly.
                return;
            }

            if (_handlingImeMessage)
            {
                // We will be called reentrantly while completing compositions
                // in response to application listeners.  In that case, don't
                // propegate events to listeners.
                return;
            }

            _handlingImeMessage = true;
            try
            {
                int resultLength;
                string compositionString = BuildCompositionString(null, new char[] { (char)wParam }, out resultLength);

                if (compositionString == null)
                {
                    CompleteComposition();
                }
                else
                {
                    FrameworkTextComposition composition = TextStore.CreateComposition(_editor, this);
                    _compositionModifiedByEventListener = false;
                    _caretOffset = 1;

                    //
                    // Raise TextInputStart.
                    //
                    bool handledbyApp = RaiseTextInputStartEvent(composition, resultLength, compositionString);

                    if (handledbyApp)
                    {
                        CompleteComposition();
                    }
                    else
                    {
                        //
                        // Raise TextInput.
                        //
                        bool handledByApp = RaiseTextInputEvent(composition, compositionString);

                        if (handledByApp)
                        {
                            CompleteComposition();
                            goto Exit;
                        }
                    }
                }
            }
            finally
            {
                _handlingImeMessage = false;
            }

            // the string has been finalized. Update the reading window position for
            // legacy Chinese IMEs.
            if (IsReadingWindowIme())
            {
                UpdateNearCaretCompositionWindow();
            }

        Exit:
            handled = true;
        }

        //
        // WM_IME_NOTIFY handler
        //
        private void OnWmImeNotify(IntPtr hwnd, IntPtr wParam)
        {
            IntPtr himc;

            // we don't have to do anything if _editor is null.
            if (!IsInKeyboardFocus)
            {
                return;
            }

            if ((int)wParam == NativeMethods.IMN_OPENCANDIDATE)
            {
                himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));
                if (himc != IntPtr.Zero)
                {
                    NativeMethods.CANDIDATEFORM candform = new NativeMethods.CANDIDATEFORM();
                    //
                    // At IMN_OPENCANDIDATE, we need to set the candidate window location to hIMC.
                    //
                    if (IsReadingWindowIme())
                    {
                        // Level 2 for Chinese legacy IMEs.
                        // We have already set the composition form. The candidate window will follow it.
                        candform.dwIndex = 0;
                        candform.dwStyle = NativeMethods.CFS_DEFAULT;
                        candform.rcArea.left = 0;
                        candform.rcArea.right = 0;
                        candform.rcArea.top = 0;
                        candform.rcArea.bottom = 0;
                        candform.ptCurrentPos = new NativeMethods.POINT(0, 0);
                    }
                    else
                    {
                        ITextView view;
                        ITextPointer startNavigator;
                        ITextPointer endNavigator;
                        ITextPointer caretNavigator;
                        GeneralTransform transform;
                        Point milPointTopLeft;
                        Point milPointBottomRight;
                        Point milPointCaret;
                        Rect rectStart;
                        Rect rectEnd;
                        Rect rectCaret;
                        CompositionTarget compositionTarget;

                        compositionTarget = _source.CompositionTarget;

                        if (_startComposition != null)
                        {
                            startNavigator = _startComposition.CreatePointer();
                        }
                        else
                        {
                            startNavigator = _editor.Selection.Start.CreatePointer();
                        }

                        if (_endComposition != null)
                        {
                            endNavigator = _endComposition.CreatePointer();
                        }
                        else
                        {
                            endNavigator = _editor.Selection.End.CreatePointer();
                        }

                        if (_startComposition != null)
                        {
                            caretNavigator = _caretOffset > 0 ? _startComposition.CreatePointer(_caretOffset, LogicalDirection.Forward) : _endComposition;
                        }
                        else
                        {
                            caretNavigator = _editor.Selection.End.CreatePointer();
                        }

                        ITextPointer startPosition = startNavigator.CreatePointer(LogicalDirection.Forward);
                        ITextPointer endPosition = endNavigator.CreatePointer(LogicalDirection.Backward);
                        ITextPointer caretPosition = caretNavigator.CreatePointer(LogicalDirection.Forward);

                        // We need to update the layout before getting rect. It could be dirty.
                        if (!startPosition.ValidateLayout() ||
                            !endPosition.ValidateLayout() ||
                            !caretPosition.ValidateLayout())
                        {
                            return;
                        }

                        view = TextEditor.GetTextView(RenderScope);

                        rectStart = view.GetRectangleFromTextPosition(startPosition);
                        rectEnd = view.GetRectangleFromTextPosition(endPosition);
                        rectCaret = view.GetRectangleFromTextPosition(caretPosition);

                        // Take the "extended" union of the first and last char's bounding box.
                        milPointTopLeft = new Point(Math.Min(rectStart.Left, rectEnd.Left), Math.Min(rectStart.Top, rectEnd.Top));
                        milPointBottomRight = new Point(Math.Max(rectStart.Left, rectEnd.Left), Math.Max(rectStart.Bottom, rectEnd.Bottom));
                        milPointCaret = new Point(rectCaret.Left, rectCaret.Bottom);

                        // Transform to root visual coordinates.
                        transform = RenderScope.TransformToAncestor(compositionTarget.RootVisual);
                        transform.TryTransform(milPointTopLeft, out milPointTopLeft);
                        transform.TryTransform(milPointBottomRight, out milPointBottomRight);
                        transform.TryTransform(milPointCaret, out milPointCaret);

                        // Transform to device units.
                        milPointTopLeft = compositionTarget.TransformToDevice.Transform(milPointTopLeft);
                        milPointBottomRight = compositionTarget.TransformToDevice.Transform(milPointBottomRight);
                        milPointCaret = compositionTarget.TransformToDevice.Transform(milPointCaret);

                        // Build CANDIDATEFORM. CANDIDATEFORM is window coodidate.
                        candform.dwIndex = 0;
                        candform.dwStyle = NativeMethods.CFS_EXCLUDE;
                        candform.rcArea.left = ConvertToInt32(milPointTopLeft.X);
                        candform.rcArea.right = ConvertToInt32(milPointBottomRight.X);
                        candform.rcArea.top = ConvertToInt32(milPointTopLeft.Y);
                        candform.rcArea.bottom = ConvertToInt32(milPointBottomRight.Y);
                        candform.ptCurrentPos = new NativeMethods.POINT(ConvertToInt32(milPointCaret.X), ConvertToInt32(milPointCaret.Y));
                    }
                    // Call IMM32 to set new candidate position to hIMC.
                    // ImmSetCandidateWindow fails when
                    //  - candform.dwIndex is invalid (over 4).
                    //  - himc belongs to other threads.
                    //  - fail to lock IMC.
                    // Those cases are ignorable for us.
                    // In addition, it does not set win32 last error and we have no clue to handle error.
#pragma warning suppress 6031
                    UnsafeNativeMethods.ImmSetCandidateWindow(new HandleRef(this, himc), ref candform);
                    UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));
                }

                // We want to pass this message to DefWindowProc.
                // We don't update "handled".
            }
        }

        //
        // Use Level 2 for Chinese IME
        //
        private void UpdateNearCaretCompositionWindow()
        {
            ITextView view;
            Rect rectUi;
            GeneralTransform transform;
            Point milPointTopLeft;
            Point milPointBottomRight;
            Point milPointCaret;
            Rect rectCaret;
            CompositionTarget compositionTarget;
            IntPtr hwnd;

            if (!IsInKeyboardFocus)
            {
                return;
            }

            if (_source == null)
            {
                return;
            }

            hwnd = ((IWin32Window)_source).Handle;

            rectUi = UiScope.VisualContentBounds;
            view = _editor.TextView;

            //
            //   We need to update Layout to calculate the correct position of the composition window.
            //   We can wait until LayoutChanged but we need to know if Layout is being updated or not.
            //
            // RenderScope.UpdateLayout();

            // During incremental layout update, the region of the view covered by
            // the selection may not be ready yet.
            if (!_editor.Selection.End.HasValidLayout)
            {
                _updateCompWndPosAtNextLayoutUpdate = true;
                return;
            }

            compositionTarget = _source.CompositionTarget;

            // HwndSource.CompositionTarget may return null if the target hwnd is being destroyed and disposed.
            if (compositionTarget == null || compositionTarget.RootVisual == null)
            {
                return;
            }

            // If the mouse click happens before rendering, the seleciton move notification is generated.
            // However the visual tree is not completely connected yet. We need to check it.
            if (!compositionTarget.RootVisual.IsAncestorOf(RenderScope))
            {
                return;
            }

            IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));
            if (himc != IntPtr.Zero)
            {
                rectCaret = view.GetRectangleFromTextPosition(_editor.Selection.End.CreatePointer(LogicalDirection.Backward));

                // Take the points of the renderScope.
                milPointTopLeft = new Point(rectUi.Left, rectUi.Top);
                milPointBottomRight = new Point(rectUi.Right, rectUi.Bottom);

                // Take the "extended" union of the first and last char's bounding box.
                // milPointCaret = new Point(rectCaret.Left, rectCaret.Top);
                milPointCaret = new Point(rectCaret.Left, rectCaret.Bottom);

                // Transform to root visual coordinates.
                transform = RenderScope.TransformToAncestor(compositionTarget.RootVisual);
                transform.TryTransform(milPointTopLeft, out milPointTopLeft);
                transform.TryTransform(milPointBottomRight, out milPointBottomRight);
                transform.TryTransform(milPointCaret, out milPointCaret);

                // Transform to device units.
                milPointTopLeft = compositionTarget.TransformToDevice.Transform(milPointTopLeft);
                milPointBottomRight = compositionTarget.TransformToDevice.Transform(milPointBottomRight);
                milPointCaret = compositionTarget.TransformToDevice.Transform(milPointCaret);

                // Build COMPOSITIONFORM. COMPOSITIONFORM is window coodidate.
                NativeMethods.COMPOSITIONFORM compform = new NativeMethods.COMPOSITIONFORM();
                compform.dwStyle = NativeMethods.CFS_RECT;
                compform.rcArea.left = ConvertToInt32(milPointTopLeft.X);
                compform.rcArea.right = ConvertToInt32(milPointBottomRight.X);
                compform.rcArea.top = ConvertToInt32(milPointTopLeft.Y);
                compform.rcArea.bottom = ConvertToInt32(milPointBottomRight.Y);
                compform.ptCurrentPos = new NativeMethods.POINT(ConvertToInt32(milPointCaret.X), ConvertToInt32(milPointCaret.Y));

                // Call IMM32 to set new candidate position to hIMC.
                // ImmSetCompositionWindow fails when
                //  - himc belongs to other threads.
                //  - fail to lock IMC.
                // Those cases are ignorable for us.
                // In addition, it does not set win32 last error and we have no clue to handle error.
                UnsafeNativeMethods.ImmSetCompositionWindow(new HandleRef(this, himc), ref compform);
                UnsafeNativeMethods.ImmReleaseContext(new HandleRef(this, hwnd), new HandleRef(this, himc));
            }
        }

        //
        // Hwnd disposed callback.
        //
        private void OnHwndDisposed(object sender, EventArgs args)
        {
            UpdateSource(_source, null);
        }

        //
        // update the composition string on the scope
        //
        private void UpdateCompositionString(char[] resultChars, char[] compositionChars, int caretOffset, int deltaStart, byte[] attributes)
        {
            if (_handlingImeMessage)
            {
                // We will be called reentrantly while completing compositions
                // in response to application listeners.  In that case, don't
                // propegate events to listeners.
                return;
            }

            _handlingImeMessage = true;
            try
            {
                //
                // Remove any existing composition adorner for display attribute.
                //
                if (_compositionAdorner != null)
                {
                    _compositionAdorner.Uninitialize();
                    _compositionAdorner = null;
                }

                //
                // Build up an array of resultChars + compositionChars -- the complete span of changing text.
                //
                int resultLength;
                string compositionString = BuildCompositionString(resultChars, compositionChars, out resultLength);

                if (compositionString == null)
                {
                    CompleteComposition();
                    return;
                }

                //
                // Remember where the IME placed the caret.
                //
                RecordCaretOffset(caretOffset, attributes, compositionString.Length);

                FrameworkTextComposition composition = TextStore.CreateComposition(_editor, this);
                _compositionModifiedByEventListener = false;

                if (_startComposition == null)
                {
                    Invariant.Assert(_endComposition == null);

                    //
                    // Raise TextInputStart.
                    //
                    bool handledbyApp = RaiseTextInputStartEvent(composition, resultLength, compositionString);

                    if (handledbyApp)
                    {
                        CompleteComposition();
                        return;
                    }
                }
                else if (compositionChars != null)
                {
                    //
                    // Raise TextInputUpdate.
                    //
                    bool handledByApp = RaiseTextInputUpdateEvent(composition, resultLength, compositionString);

                    if (handledByApp)
                    {
                        CompleteComposition();
                        return;
                    }
                }

                if (compositionChars == null)
                {
                    //
                    // Raise TextInput.
                    //
                    bool handledByApp = RaiseTextInputEvent(composition, compositionString);

                    if (handledByApp)
                    {
                        CompleteComposition();
                        return;
                    }
                }

                if (_startComposition != null)
                {
                    SetCompositionAdorner(attributes);
                }
            }
            finally
            {
                _handlingImeMessage = false;
            }
        }


        /// <summary>
        /// Attempts to build a string containing zero or more result chars followed by zero or more composition chars.
        /// </summary>
        /// <param name="resultChars"></param>
        /// <param name="compositionChars"></param>
        /// <param name="resultLength"></param>
        /// <returns></returns>
        private string BuildCompositionString(char[] resultChars, char[] compositionChars, out int resultLength)
        {
            int compositionLength = compositionChars == null ? 0 : compositionChars.Length;
            resultLength = resultChars == null ? 0 : resultChars.Length;
            char[] compositionText;

            if (resultChars == null)
            {
                compositionText = compositionChars;
            }
            else if (compositionChars == null)
            {
                compositionText = resultChars;
            }
            else
            {
                compositionText = new char[resultLength + compositionLength];
                Array.Copy(resultChars, 0, compositionText, 0, resultLength);
                Array.Copy(compositionChars, 0, compositionText, resultLength, compositionLength);
            }

            string compositionString = new string(compositionText);

            int originalLength = (compositionText == null) ? 0 : compositionText.Length;
            return (compositionString.Length == originalLength) ? compositionString : null;
        }

        // Caches the IME specified caret offset.
        // Value is the offset in unicode code points from the composition start.
        private void RecordCaretOffset(int caretOffset, byte[] attributes, int compositionLength)
        {
            // Use the suggested value if it is on ATTR_INPUT, otherwise set the caret at the end of
            // composition string. So it always stays where the new char is inserted.
            if ((attributes != null) &&
                // If the next char of the cursorPos is INPUTATTR.
                (((caretOffset >= 0) &&
                  (caretOffset < attributes.Length) &&
                  (attributes[caretOffset] == NativeMethods.ATTR_INPUT)) ||
                // If the prev char os the cursorPos is INPUTATTR.
                 ((caretOffset > 0) &&
                  ((caretOffset - 1) < attributes.Length) &&
                  (attributes[caretOffset - 1] == NativeMethods.ATTR_INPUT))))
            {
                _caretOffset = caretOffset;
            }
            else
            {
                _caretOffset = -1;
            }
        }

        // Raises a public TextInputStart event.
        // Returns true if a listener handles the event or modifies document state.
        private bool RaiseTextInputStartEvent(FrameworkTextComposition composition, int resultLength, string compositionString)
        {
            composition.Stage = TextCompositionStage.None;
            composition.SetCompositionPositions(_editor.Selection.Start, _editor.Selection.End, compositionString);

            // PUBLIC event:
            bool handled = TextCompositionManager.StartComposition(composition);

            if (handled ||
                composition.PendingComplete ||
                _compositionModifiedByEventListener)
            {
                return true;
            }

            // UpdateCompositionText raises a PUBLIC EVENT....
            UpdateCompositionText(composition, resultLength, true /* includeResultText */, out _startComposition, out _endComposition);

            if (_compositionModifiedByEventListener)
            {
                return true;
            }

            RegisterMouseListeners();

            return false;
        }

        // Raises a public TextInputUpdate event.
        // Returns true if a listener handles the event or modifies document state.
        private bool RaiseTextInputUpdateEvent(FrameworkTextComposition composition, int resultLength, string compositionString)
        {
            composition.Stage = TextCompositionStage.Started;
            composition.SetCompositionPositions(_startComposition, _endComposition, compositionString);

            // PUBLIC event:
            bool handled = TextCompositionManager.UpdateComposition(composition);

            if (handled ||
                composition.PendingComplete ||
                _compositionModifiedByEventListener)
            {
                return true;
            }

            // UpdateCompositionText raises a PUBLIC EVENT....
            UpdateCompositionText(composition, resultLength, false /* includeResultText */, out _startComposition, out _endComposition);

            if (_compositionModifiedByEventListener)
            {
                return true;
            }

            return false;
        }

        // Raises a public TextInput event.
        // Returns true if a listener handles the event or modifies document state.
        private bool RaiseTextInputEvent(FrameworkTextComposition composition, string compositionString)
        {
            composition.Stage = TextCompositionStage.Started;
            composition.SetResultPositions(_startComposition, _endComposition, compositionString);

            _startComposition = null;
            _endComposition = null;

            UnregisterMouseListeners();

            _handledByEditorListener = false;

            // PUBLIC event:
            TextCompositionManager.CompleteComposition(composition);

            _compositionUndoUnit = null;

            return (!_handledByEditorListener || composition.PendingComplete || _compositionModifiedByEventListener);
        }

        // Inserts composition text into the document.
        // Raises public text, selection changed events.
        // Called by default editor TextInputEvent handler.
        internal void UpdateCompositionText(FrameworkTextComposition composition)
        {
            ITextPointer start;
            ITextPointer end;

            UpdateCompositionText(composition, 0, true /* includeResultText */
                                                                              , out start, out end);
        }

        // Inserts composition text into the document.
        // Raises public text, selection changed events.
        // Returns the position of the inserted text.  If includeResultText is
        // true, start/end will cover all the inserted text.  Otherwise, text
        // from offset 0 to resultLength is omitted from start/end.
        internal void UpdateCompositionText(FrameworkTextComposition composition, int resultLength, bool includeResultText, out ITextPointer start, out ITextPointer end)
        {
            start = null;
            end = null;

            if (_compositionModifiedByEventListener)
            {
                // If the app has modified the document since this event was raised
                // (by hooking a TextInput event), then we don't know what to do,
                // so do nothing.
                return;
            }

            _handledByEditorListener = true;

            bool isTextFiltered = false;

            UndoCloseAction undoCloseAction = UndoCloseAction.Rollback;
            OpenCompositionUndoUnit();

            try
            {
                _editor.Selection.BeginChange();
                try
                {
                    // this code duplicated in TextStore.UpdateCompositionText

                    ITextRange range;
                    string text;

                    // DevDiv.1106868 We need to set ignoreTextUnitBoundaries to true in the TextRange constructor in case we are dealing with
                    // a supplementary character (a pair of surrogate characters that together form a single character), otherwise TextRange
                    // will break us out of the compound sequence
                    if (composition._ResultStart != null)
                    {
                        //
                        // If we're here it means composition is being finalized
                        //
                        range = new TextRange(composition._ResultStart, composition._ResultEnd, true /* ignoreTextUnitBoundaries */);
                        text = this._editor._FilterText(composition.Text, range);
                        isTextFiltered = (text != composition.Text);
                        if (isTextFiltered)
                        {
                            // If text was filtered we need to update the caret to point
                            // past the updated text (_caretOffset == text.Length), but we should
                            // also keep in mind that IMM's are free to have put caret in
                            // any position so we're chosing minimum of both.
                            _caretOffset = Math.Min(_caretOffset, text.Length);
                        }
                    }
                    else
                    {
                        range = new TextRange(composition._CompositionStart, composition._CompositionEnd, true /* ignoreTextUnitBoundaries */);
                        text = composition.CompositionText;
                    }

                    _editor.SetText(range, text, InputLanguageManager.Current.CurrentInputLanguage);

                    if (includeResultText)
                    {
                        start = range.Start;
                    }
                    else
                    {
                        start = range.Start.CreatePointer(resultLength, LogicalDirection.Forward);
                    }
                    end = range.End;

                    ITextPointer caretPosition = _caretOffset >= 0 ? start.CreatePointer(_caretOffset, LogicalDirection.Forward) : end;
                    _editor.Selection.Select(caretPosition, caretPosition);
                }
                finally
                {
                    // We're about to raise the public event.
                    // Set a flag so we can detect app changes.
                    _compositionModifiedByEventListener = false;

                    _editor.Selection.EndChange();
                    if (isTextFiltered)
                    {
                        _compositionModifiedByEventListener = true;
                    }
                }

                undoCloseAction = UndoCloseAction.Commit;
            }
            finally
            {
                CloseCompositionUndoUnit(undoCloseAction, end);
            }
        }

        // Decorates the composition with IME specified underlining.
        private void SetCompositionAdorner(byte[] attributes)
        {
            if (attributes != null)
            {
                int startOffset = 0;
                for (int i = 0; i < attributes.Length; i++)
                {
                    if ((i + 1) < attributes.Length)
                    {
                        if (attributes[i] == attributes[i + 1])
                        {
                            continue;
                        }
                    }
                    ITextPointer startAttribute = _startComposition.CreatePointer(startOffset, LogicalDirection.Backward);
                    ITextPointer endAttribute = _startComposition.CreatePointer(i + 1, LogicalDirection.Forward);

                    if (_compositionAdorner == null)
                    {
                        _compositionAdorner = new CompositionAdorner(_editor.TextView);
                        _compositionAdorner.Initialize(_editor.TextView);
                    }

                    //
                    // Need a design of the composition rendering.
                    //

                    UnsafeNativeMethods.TF_DISPLAYATTRIBUTE displayAttribute = new UnsafeNativeMethods.TF_DISPLAYATTRIBUTE();

                    displayAttribute.crLine.type = UnsafeNativeMethods.TF_DA_COLORTYPE.TF_CT_COLORREF;
                    displayAttribute.crLine.indexOrColorRef = 0;
                    displayAttribute.lsStyle = UnsafeNativeMethods.TF_DA_LINESTYLE.TF_LS_NONE;
                    displayAttribute.fBoldLine = false;

                    switch (attributes[i])
                    {
                        case NativeMethods.ATTR_INPUT:
                            displayAttribute.lsStyle = UnsafeNativeMethods.TF_DA_LINESTYLE.TF_LS_DOT;
                            break;

                        case NativeMethods.ATTR_TARGET_CONVERTED:
                            displayAttribute.lsStyle = UnsafeNativeMethods.TF_DA_LINESTYLE.TF_LS_SOLID;
                            displayAttribute.fBoldLine = true;
                            break;

                        case NativeMethods.ATTR_CONVERTED:
                            displayAttribute.lsStyle = UnsafeNativeMethods.TF_DA_LINESTYLE.TF_LS_DOT;
                            break;

                        case NativeMethods.ATTR_TARGET_NOTCONVERTED:
                            displayAttribute.lsStyle = UnsafeNativeMethods.TF_DA_LINESTYLE.TF_LS_SOLID;
                            break;

                        case NativeMethods.ATTR_INPUT_ERROR:
                            break;

                        case NativeMethods.ATTR_FIXEDCONVERTED:
                            break;
                    }

#if UNUSED_IME_HIGHLIGHT_LAYER
                                // Demand create the highlight layer.
                                if (_highlightLayer == null)
                                {
                                    _highlightLayer = new DisplayAttributeHighlightLayer();
                                }

                                // Need to pass the foreground and background color of the composition
                                _highlightLayer.Add(startClause, endClause, /*TextDecorationCollection:*/null);
#endif

                    TextServicesDisplayAttribute textServiceDisplayAttribute = new TextServicesDisplayAttribute(displayAttribute);

                    // Add the attribute range into CompositionAdorner.
                    _compositionAdorner.AddAttributeRange(startAttribute, endAttribute, textServiceDisplayAttribute);
                    startOffset = i + 1;
                }

#if UNUSED_IME_HIGHLIGHT_LAYER
                            if (_highlightLayer != null)
                            {
                                _editor.TextContainer.Highlights.AddLayer(_highlightLayer);
                            }
#endif

                if (_compositionAdorner != null)
                {
                    // Update the layout to get the acurated rectangle from calling GetRectangleFromTextPosition
                    _editor.TextView.RenderScope.UpdateLayout();

                    // Invalidate the composition adorner to apply the composition attribute ranges.
                    _compositionAdorner.InvalidateAdorner();
                }
            }
        }

        // Start listening mouse event for MSIME mouse operation.
        private void RegisterMouseListeners()
        {
            UiScope.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(OnMouseButtonEvent);
            UiScope.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(OnMouseButtonEvent);
            UiScope.PreviewMouseRightButtonDown += new MouseButtonEventHandler(OnMouseButtonEvent);
            UiScope.PreviewMouseRightButtonUp += new MouseButtonEventHandler(OnMouseButtonEvent);
            UiScope.PreviewMouseMove += new MouseEventHandler(OnMouseEvent);
        }

        // Stop listening mouse event for MSIME mouse operation.
        private void UnregisterMouseListeners()
        {
            if (this.UiScope != null)
            {
                UiScope.PreviewMouseLeftButtonDown -= new MouseButtonEventHandler(OnMouseButtonEvent);
                UiScope.PreviewMouseLeftButtonUp -= new MouseButtonEventHandler(OnMouseButtonEvent);
                UiScope.PreviewMouseRightButtonDown -= new MouseButtonEventHandler(OnMouseButtonEvent);
                UiScope.PreviewMouseRightButtonUp -= new MouseButtonEventHandler(OnMouseButtonEvent);
                UiScope.PreviewMouseMove -= new MouseEventHandler(OnMouseEvent);
            }
        }

        //
        // WM_IME_REQUEST handler
        //
        private IntPtr OnWmImeRequest(IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            IntPtr lret = IntPtr.Zero;

            switch ((int)wParam)
            {
                case NativeMethods.IMR_RECONVERTSTRING:
                    lret = OnWmImeRequest_ReconvertString(lParam, ref handled, false);
                    break;

                case NativeMethods.IMR_CONFIRMRECONVERTSTRING:
                    lret = OnWmImeRequest_ConfirmReconvertString(lParam, ref handled);
                    break;

                case NativeMethods.IMR_QUERYCHARPOSITION:
                    break;

                case NativeMethods.IMR_DOCUMENTFEED:
                    lret = OnWmImeRequest_ReconvertString(lParam, ref handled, true);
                    break;
            }

            return lret;
        }

        //
        // WM_IME_REQUEST/IMR_RECONVERTSTRING handler
        //
        private IntPtr OnWmImeRequest_ReconvertString(IntPtr lParam, ref bool handled, bool fDocFeed)
        {
            if (!fDocFeed)
            {
                _isReconvReady = false;
            }

            if (!IsInKeyboardFocus)
            {
                return IntPtr.Zero;
            }

            ITextRange range;

            //
            // If there is the composition string, we use it. Otherwise we use the current selection.
            //
            if (fDocFeed && (_startComposition != null) && (_endComposition != null))
            {
                range = new TextRange(_startComposition, _endComposition);
            }
            else
            {
                range = _editor.Selection;
            }

            string target = range.Text;

            int requestSize = Marshal.SizeOf(typeof(NativeMethods.RECONVERTSTRING)) + (target.Length * Marshal.SizeOf(typeof(short))) + ((_maxSrounding + 1) * Marshal.SizeOf(typeof(short)) * 2);
            IntPtr lret = new IntPtr(requestSize);

            if (lParam != IntPtr.Zero)
            {
                int offsetStart;
                string surrounding = GetSurroundingText(range, out offsetStart);

                // Create RECONVERTSTRING structure from lParam.
                NativeMethods.RECONVERTSTRING reconv = (NativeMethods.RECONVERTSTRING)Marshal.PtrToStructure(lParam, typeof(NativeMethods.RECONVERTSTRING));

                reconv.dwSize = requestSize;
                reconv.dwVersion = 0;                                                         // must be 0
                reconv.dwStrLen = surrounding.Length;                                         // in char count
                reconv.dwStrOffset = Marshal.SizeOf(typeof(NativeMethods.RECONVERTSTRING));   // in byte count
                reconv.dwCompStrLen = target.Length;                                          // in char count
                reconv.dwCompStrOffset = offsetStart * Marshal.SizeOf(typeof(short));         // in byte count
                reconv.dwTargetStrLen = target.Length;                                        // in char count
                reconv.dwTargetStrOffset = offsetStart * Marshal.SizeOf(typeof(short));       // in byte count

                if (!fDocFeed)
                {
                    //
                    // If this is IMR_RECONVERTSTRING, we cache it. So we can refer it later when we get
                    // IMR_CONFIRMRECONVERTSTRING message.
                    //
                    _reconv = reconv;
                    _isReconvReady = true;
                }

                // Copy the strucuture back to lParam.
                Marshal.StructureToPtr(reconv, lParam, true);

                StoreSurroundingText(lParam, surrounding);
            }

            handled = true;
            return lret;
        }

        private unsafe static void StoreSurroundingText(IntPtr reconv, string surrounding)
        {
            // Copy the string to the pointer right after the structure.
            byte* p = (byte*)reconv.ToPointer();
            p += Marshal.SizeOf(typeof(NativeMethods.RECONVERTSTRING));
            Marshal.Copy(surrounding.ToCharArray(), 0, new IntPtr((void*)p), surrounding.Length);
        }

        //
        // Get the surrounding text of the given range.
        // The offsetStart is out param to return the offset of the given range in the returned surrounding text.
        //
        private static string GetSurroundingText(ITextRange range, out int offsetStart)
        {
            ITextPointer navigator;
            bool done;
            string surrounding = "";
            int bufLength;

            //
            // Get the previous text of the given range.
            //
            navigator = range.Start.CreatePointer();
            done = false;
            bufLength = _maxSrounding;
            while (!done && (bufLength > 0))
            {
                switch (navigator.GetPointerContext(LogicalDirection.Backward))
                {
                    case TextPointerContext.Text:
                        char[] buffer = new char[bufLength];
                        int copied = navigator.GetTextInRun(LogicalDirection.Backward, buffer, 0, buffer.Length);
                        Invariant.Assert(copied != 0);
                        navigator.MoveByOffset(0 - copied);
                        bufLength -= copied;
                        surrounding = surrounding.Insert(0, new string(buffer, 0, copied));
                        break;

                    case TextPointerContext.EmbeddedElement:
                        done = true;
                        break;

                    case TextPointerContext.ElementStart:
                    case TextPointerContext.ElementEnd:
                        // ignore the inline element.
                        if (!navigator.GetElementType(LogicalDirection.Backward).IsSubclassOf(typeof(Inline)))
                        {
                            done = true;
                        }
                        navigator.MoveToNextContextPosition(LogicalDirection.Backward);
                        break;


                    case TextPointerContext.None:
                        done = true;
                        break;

                    default:
                        navigator.MoveToNextContextPosition(LogicalDirection.Backward);
                        break;
                }
            }

            // offsetStart is the amount of the current surroundingText.
            offsetStart = surrounding.Length;

            //
            // add the text in the given range.
            //
            surrounding += range.Text;

            //
            // Get the following text of the given range.
            //
            navigator = range.End.CreatePointer();
            done = false;
            bufLength = _maxSrounding;
            while (!done && (bufLength > 0))
            {
                switch (navigator.GetPointerContext(LogicalDirection.Forward))
                {
                    case TextPointerContext.Text:
                        char[] buffer = new char[bufLength];
                        int copied = navigator.GetTextInRun(LogicalDirection.Forward, buffer, 0, buffer.Length);
                        navigator.MoveByOffset(copied);
                        bufLength -= copied;
                        surrounding += new string(buffer, 0, copied);
                        break;

                    case TextPointerContext.EmbeddedElement:
                        done = true;
                        break;

                    case TextPointerContext.ElementStart:
                    case TextPointerContext.ElementEnd:
                        // ignore the inline element.
                        if (!navigator.GetElementType(LogicalDirection.Forward).IsSubclassOf(typeof(Inline)))
                        {
                            done = true;
                        }
                        navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                        break;

                    case TextPointerContext.None:
                        done = true;
                        break;

                    default:
                        navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                        break;
                }
            }

            return surrounding;
        }

        //
        // WM_IME_REQUEST/IMR_CONFIRMRECONVERTSTRING handler
        //
        private IntPtr OnWmImeRequest_ConfirmReconvertString(IntPtr lParam, ref bool handled)
        {
            if (!IsInKeyboardFocus)
            {
                return IntPtr.Zero;
            }

            if (!_isReconvReady)
            {
                return IntPtr.Zero;
            }

            NativeMethods.RECONVERTSTRING reconv = (NativeMethods.RECONVERTSTRING)Marshal.PtrToStructure(lParam, typeof(NativeMethods.RECONVERTSTRING));

            // If the entire string in RECONVERTSTRING has been changed, we don't handle it.
            if (_reconv.dwStrLen != reconv.dwStrLen)
            {
                handled = true;
                return IntPtr.Zero;
            }

            //
            // If the new CompStr was suggested by IME, we need to adjust the selection with it.
            //
            if ((_reconv.dwCompStrLen != reconv.dwCompStrLen) ||
                (_reconv.dwCompStrOffset != reconv.dwCompStrOffset))
            {
                ITextRange range = _editor.Selection;

                //
                // Create the start point from the selection
                //
                ITextPointer start = range.Start.CreatePointer(LogicalDirection.Backward);

                // Move the start point to new  dwCompStrOffset.
                start = MoveToNextCharPos(start, (reconv.dwCompStrOffset - _reconv.dwCompStrOffset) / Marshal.SizeOf(typeof(short)));
                // Create the end position and move this as dwCompStrLen.
                ITextPointer end = start.CreatePointer(LogicalDirection.Forward);
                end = MoveToNextCharPos(end, reconv.dwCompStrLen);

                // Update the selection with new start and end.
                _editor.Selection.Select(start, end);
            }

            _isReconvReady = false;
            handled = true;
            return new IntPtr(1);
        }

        //
        // Move the TextPointer by offset in char count.
        //
        private static ITextPointer MoveToNextCharPos(ITextPointer position, int offset)
        {
            bool done = false;
            if (offset < 0)
            {
                while ((offset < 0) && !done)
                {
                    switch (position.GetPointerContext(LogicalDirection.Backward))
                    {
                        case TextPointerContext.Text:
                            offset++;
                            break;
                        case TextPointerContext.None:
                            done = true;
                            break;
                    }
                    position.MoveByOffset(-1);
                }
            }
            else if (offset > 0)
            {
                while ((offset > 0) && !done)
                {
                    switch (position.GetPointerContext(LogicalDirection.Forward))
                    {
                        case TextPointerContext.Text:
                            offset--;
                            break;
                        case TextPointerContext.None:
                            done = true;
                            break;
                    }
                    position.MoveByOffset(1);
                }
            }

            return position;
        }

        //
        // Move the TextPointer by offset in char count.
        //
        private bool IsReadingWindowIme()
        {
            int prop = UnsafeNativeMethods.ImmGetProperty(new HandleRef(this, SafeNativeMethods.GetKeyboardLayout(0)), NativeMethods.IGP_PROPERTY);
            return (((prop & NativeMethods.IME_PROP_AT_CARET) == 0) || ((prop & NativeMethods.IME_PROP_SPECIAL_UI) != 0));
        }

        //
        // Mouse Button state was changed.
        //
        private void OnMouseButtonEvent(object sender, MouseButtonEventArgs e)
        {
            e.Handled = InternalMouseEventHandler();
        }

        //
        // Mouse was moved.
        //
        private void OnMouseEvent(object sender, MouseEventArgs e)
        {
            e.Handled = InternalMouseEventHandler();
        }

        //
        // The mouse event handler to generate MSIME message to IME listeners.
        //
        private bool InternalMouseEventHandler()
        {
            int btnState = 0;
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                btnState = 1; // IMEMOUSE_LDOWN
            }
            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                btnState = 2; // IMEMOUSE_RDOWN
            }

            Point point = Mouse.GetPosition(RenderScope);
            ITextView view;
            ITextPointer positionCurrent;
            ITextPointer positionNext;
            Rect rectCurrent;
            Rect rectNext;

            view = TextEditor.GetTextView(RenderScope);
            // Validate layout information on TextView
            if (!view.Validate(point))
            {
                return false;
            }

            // Do the hittest.
            positionCurrent = view.GetTextPositionFromPoint(point, false);
            if (positionCurrent == null)
            {
                return false;
            }

            rectCurrent = view.GetRectangleFromTextPosition(positionCurrent);

            positionNext = positionCurrent.CreatePointer();
            if (positionNext == null)
            {
                return false;
            }

            if (point.X - rectCurrent.Left >= 0)
            {
                positionNext.MoveToNextInsertionPosition(LogicalDirection.Forward);
            }
            else
            {
                positionNext.MoveToNextInsertionPosition(LogicalDirection.Backward);
            }

            rectNext = view.GetRectangleFromTextPosition(positionNext);

            int edge;
            int quadrant;
            edge = _editor.TextContainer.Start.GetOffsetToPosition(positionCurrent);
            int startComposition = _editor.TextContainer.Start.GetOffsetToPosition(_startComposition);
            int endComposition = _editor.TextContainer.Start.GetOffsetToPosition(_endComposition);

            //
            // IMEs care about only the composition string range.
            //

            if (edge < startComposition)
            {
                return false;
            }

            if (edge > endComposition)
            {
                return false;
            }

            if (rectNext.Left == rectCurrent.Left)
            {
                // if rectNext.Left == rectCurrent.Left, the width of char is 0 and mouse click points there.
                // there is no quadrent. So we alwasys make it 0.
                quadrant = 0;
            }
            else
            {
                if (point.X - rectCurrent.Left >= 0)
                {
                    if ((((point.X - rectCurrent.Left) * 4) / (rectNext.Left - rectCurrent.Left)) <= 1)
                        quadrant = 2;
                    else
                        quadrant = 3;
                }
                else
                {
                    if (((point.X - rectNext.Left) * 4) / (rectCurrent.Left - rectNext.Left) <= 3)
                        quadrant = 0;
                    else
                        quadrant = 1;
                }
            }

            //
            // IMEs care about only the composition string range.
            // If the quadrant is outside of the range, we don't do SendMessage.
            //

            if ((edge == startComposition) && (quadrant <= 1))
            {
                return false;
            }

            if ((edge == endComposition) && (quadrant >= 2))
            {
                return false;
            }

            //
            // The edge must be relative to the composition string.
            //
            edge -= startComposition;

            int wParam = (edge << 16) + (quadrant << 8) + btnState;

            IntPtr hwnd = IntPtr.Zero;

            hwnd = ((IWin32Window)_source).Handle;

            IntPtr himc = UnsafeNativeMethods.ImmGetContext(new HandleRef(this, hwnd));

            IntPtr lret = IntPtr.Zero;
            if (himc != IntPtr.Zero)
            {
                IntPtr hwndDefIme = IntPtr.Zero;
                hwndDefIme = UnsafeNativeMethods.ImmGetDefaultIMEWnd(new HandleRef(this, hwnd));
                lret = UnsafeNativeMethods.SendMessage(hwndDefIme, s_MsImeMouseMessage, new IntPtr(wParam), himc);
            }

            // We eat this event if IME handled.
            return (lret != IntPtr.Zero) ? true : false;
        }

        // Opens a composition undo unit. Opens the compsed composition undo unit if it exist on the top
        // of the stack. Otherwise, create new composition undo unit and add it to the undo manager and
        // making it as the opened undo unit.
        private void OpenCompositionUndoUnit()
        {
            UndoManager undoManager;
            DependencyObject parent;

            parent = _editor.TextContainer.Parent;
            undoManager = UndoManager.GetUndoManager(parent);

            if (undoManager != null && undoManager.IsEnabled && undoManager.OpenedUnit == null)
            {
                if (_compositionUndoUnit != null && _compositionUndoUnit == undoManager.LastUnit && !_compositionUndoUnit.Locked)
                {
                    // Opens a closed composition undo unit on the top of the stack.
                    undoManager.Reopen(_compositionUndoUnit);
                }
                else
                {
                    _compositionUndoUnit = new TextParentUndoUnit(_editor.Selection);

                    // Add the given composition undo unit to the undo manager and making it
                    // as the opened undo unit.
                    undoManager.Open(_compositionUndoUnit);
                }
            }
            else
            {
                _compositionUndoUnit = null;
            }
        }

        // Closes an opened composition unit and adding it to the containing unit's undo stack.
        private void CloseCompositionUndoUnit(UndoCloseAction undoCloseAction, ITextPointer compositionEnd)
        {
            UndoManager undoManager;
            DependencyObject parent;

            parent = _editor.TextContainer.Parent;
            undoManager = UndoManager.GetUndoManager(parent);

            if (undoManager != null && undoManager.IsEnabled && undoManager.OpenedUnit != null)
            {
                if (_compositionUndoUnit != null)
                {
                    // Closes an opened composition unit and commit it to add the composition
                    // undo unit into the containing unit's undo stack.
                    if (undoCloseAction == UndoCloseAction.Commit)
                    {
                        _compositionUndoUnit.RecordRedoSelectionState(compositionEnd, compositionEnd);
                    }
                    undoManager.Close(_compositionUndoUnit, undoCloseAction);
                }
            }
            else
            {
                _compositionUndoUnit = null;
            }
        }

        // Converts a double into a 32 bit integer, truncating values that
        // exceed Int32.MinValue or Int32.MaxValue.
        private int ConvertToInt32(double value)
        {
            int i;

            if (Double.IsNaN(value))
            {
                // (int)value is 0x80000000. So we should assign Int32.MinValue.
                i = Int32.MinValue;
            }
            else if (value < Int32.MinValue)
            {
                i = Int32.MinValue;
            }
            else if (value > Int32.MaxValue)
            {
                i = Int32.MaxValue;
            }
            else
            {
                i = Convert.ToInt32(value);
            }

            return i;
        }

        private void OnTextContainerChange(object sender, TextContainerChangeEventArgs args)
        {
            if (args.IMECharCount > 0 && (args.TextChange == TextChangeType.ContentAdded || args.TextChange == TextChangeType.ContentRemoved))
            {
                _compositionModifiedByEventListener = true;
            }
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        private UIElement RenderScope
        {
            get { return _editor.TextView == null ? null : _editor.TextView.RenderScope; }
        }

        private FrameworkElement UiScope
        {
            get { return (_editor == null) ? null : _editor.UiScope; }
        }

        private bool IsReadOnly
        {
            get
            {
                return ((bool)UiScope.GetValue(TextEditor.IsReadOnlyProperty) || _editor.IsReadOnly);
            }
        }

        private bool IsInKeyboardFocus
        {
            get
            {
                if (_editor == null)
                {
                    return false;
                }

                if (UiScope == null)
                {
                    return false;
                }

                if (!UiScope.IsKeyboardFocused)
                {
                    return false;
                }
                return true;
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        //
        // HwndSource of this instance of ImmComposition.
        //
        private HwndSource _source;

        //
        // TextEditor of the current focus element.
        //
        private TextEditor _editor;

        //
        // The current start position of the compositon string.
        // This is null if the composition string does not exist.
        //
        private ITextPointer _startComposition;

        //
        // The current end position of the compositon string.
        // This is null if the composition string does not exist.
        //
        private ITextPointer _endComposition;

        //
        // The offset in chars from the start of the composition to the IME caret.
        //
        private int _caretOffset;

#if UNUSED_IME_HIGHLIGHT_LAYER
        //
        // Highlight layer forLevel3 composition drawing.
        //
        private DisplayAttributeHighlightLayer _highlightLayer;
#endif

        //
        // CompositionAdorner for displaying the composition attributes.
        //
        private CompositionAdorner _compositionAdorner;

        //
        // List of ImmComposition instances.
        //
        private static Hashtable _list = new Hashtable(1);

        //
        // Dash length of the compositon string underline.
        //
        private const double _dashLength = 2.0;

        //
        // Max surrounding char count for RECONVERTSTRING/DOCFEED.
        //
        private const int _maxSrounding = 0x20;

        //
        //  Cached RECONVERTSTRING structure for IMR_CONFIRMRECONVERTSTRING message handling.
        //
        private NativeMethods.RECONVERTSTRING _reconv;

        //
        //  True if the cached RECONVERTSTRING structure is ready.
        //
        private bool _isReconvReady;

        //
        //  MSIME mouse operation message.
        //
        private static WindowMessage s_MsImeMouseMessage = UnsafeNativeMethods.RegisterWindowMessage("MSIMEMouseOperation");

        // This is the composition undo unit.
        private TextParentUndoUnit _compositionUndoUnit;

        // Reentry flag, set true while handling WM_IME_UPDATE, WM_IME_CHAR.
        private bool _handlingImeMessage;

        // If this is true, call UpdateNearCaretCompositionWindow() at the next layout update.
        private bool _updateCompWndPosAtNextLayoutUpdate;

        // Set true if an application listener modified the document content
        // or selection inside a TextInput* event.
        private bool _compositionModifiedByEventListener;

        // Flag set true when TextInput events are handled by the default
        // TextEditor listener -- not intercepted by an application listener.
        private bool _handledByEditorListener;

        // Set true while completing a composition from OnLostFocus.
        private bool _losingFocus;

        #endregion Private Fields
    }
}
