// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: ITextStoreACP implementation.
//


using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Threading;
using System.Threading;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using MS.Internal;
using System.Windows.Controls;
using System.Windows.Markup;        // for XmlLanguage
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Documents;
using MS.Internal.Documents;
using System.Security;
using MS.Win32;

using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace System.Windows.Documents
{
    // The TextStore class is a managed implementation of a Text Services
    // Framework ITextStoreACP.  TextStores represent documents for TSF,
    // which enables things like IME input, speech dictation, or ink-to-text
    // handwriting.
    //
    // The TextEditor class instantiates TextStore's when it detects available
    // Text Services on the desktop.
    internal class TextStore : UnsafeNativeMethods.ITextStoreACP,
                               UnsafeNativeMethods.ITfThreadFocusSink,
                               UnsafeNativeMethods.ITfContextOwnerCompositionSink,
                               UnsafeNativeMethods.ITfTextEditSink,
                               UnsafeNativeMethods.ITfTransitoryExtensionSink,
                               UnsafeNativeMethods.ITfMouseTrackerACP
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates a new TextStore instance.
        // The interesting initialization is in Attach/Detach.
        internal TextStore(TextEditor textEditor)
        {
            // We have only weak reference to TextEditor so it is free to be GCed.
            _weakTextEditor = new ScopeWeakReference(textEditor);

            // initialize Cookies.
            _threadFocusCookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
            _editSinkCookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
            _editCookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
            _transitoryExtensionSinkCookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Methods - ITextStoreACP
        //
        //------------------------------------------------------

        #region ITextStoreACP

        // See msdn's ITextStoreACP documentation for a full description.
        public void AdviseSink(ref Guid riid, object obj, UnsafeNativeMethods.AdviseFlags flags)
        {
            UnsafeNativeMethods.ITextStoreACPSink sink;

            if (riid != UnsafeNativeMethods.IID_ITextStoreACPSink)
            {
                throw new COMException(SR.Get(SRID.TextStore_CONNECT_E_CANNOTCONNECT), unchecked((int)0x80040202));
            }

            sink = obj as UnsafeNativeMethods.ITextStoreACPSink;
            if (sink == null)
            {
                throw new COMException(SR.Get(SRID.TextStore_E_NOINTERFACE), unchecked((int)0x80004002));
            }

            // It's legal to replace existing sink.
            if (HasSink)
            {
                Marshal.ReleaseComObject(_sink);
            }
            else
            {
                // Start tracking window movement for _sink.
                _textservicesHost.RegisterWinEventSink(this);
            }

            _sink = sink;
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void UnadviseSink(object obj)
        {
            if (obj != _sink)
            {
                throw new COMException(SR.Get(SRID.TextStore_CONNECT_E_NOCONNECTION), unchecked((int)0x80040200));
            }

            Marshal.ReleaseComObject(_sink);
            _sink = null;

            // We don't need to track window movement for this textstore any more.
            // _sink was the only consumer.
            _textservicesHost.UnregisterWinEventSink(this);
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void RequestLock(UnsafeNativeMethods.LockFlags flags, out int hrSession)
        {
            if (!HasSink)
                throw new COMException(SR.Get(SRID.TextStore_NoSink));

            if (flags == 0)
                throw new COMException(SR.Get(SRID.TextStore_BadLockFlags));

            if (_lockFlags != 0)
            {
                // Normally, we disallow reentrant lock requests.
                // However, there is one legal case.  If the caller already
                // holds a read lock, and is asking for a write lock, then
                // we will grant that asynchronously as soon as they walk
                // back up the stack to the original RequestLock call.
                if (((_lockFlags & UnsafeNativeMethods.LockFlags.TS_LF_WRITE) == UnsafeNativeMethods.LockFlags.TS_LF_WRITE) ||
                    ((flags & UnsafeNativeMethods.LockFlags.TS_LF_WRITE) == 0) ||
                    ((flags & UnsafeNativeMethods.LockFlags.TS_LF_SYNC) == UnsafeNativeMethods.LockFlags.TS_LF_SYNC))
                {
                    throw new COMException(SR.Get(SRID.TextStore_ReentrantRequestLock));
                }

                _pendingWriteReq = true;
                hrSession = UnsafeNativeMethods.TS_S_ASYNC;
            }
            else
            {
                if (_textChangeReentrencyCount == 0)
                {
                    // We can grant a synchronous lock.
                    hrSession = GrantLockWorker(flags);
                }
                else
                {
                    // We can't grant a synchornous lock -- we're inside a OnTextChanged notification.
                    // We don't want to allow even read-only locks, because that might
                    // trigger a layout update.
                    if ((flags & UnsafeNativeMethods.LockFlags.TS_LF_SYNC) == 0)
                    {
                        if (_pendingAsyncLockFlags == 0)
                        {
                            // No pending lock item in the queue, post one.
                            _pendingAsyncLockFlags = flags;
                            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(GrantLockHandler), null);
                        }
                        else if (((flags & UnsafeNativeMethods.LockFlags.TS_LF_READWRITE) & _pendingAsyncLockFlags) !=
                                 (flags & UnsafeNativeMethods.LockFlags.TS_LF_READWRITE))
                        {
                            // There's a pending item in the queue, but we need to bump up
                            // the privilege from read-only to write.
                            _pendingAsyncLockFlags = flags;
                        }
                        else
                        {
                            // There's already a pending queue item of sufficient privilege --
                            // nothing to do.
                        }
                        hrSession = UnsafeNativeMethods.TS_S_ASYNC;
                    }
                    else
                    {
                        // Caller insists on a sync lock -- give up.
                        hrSession = UnsafeNativeMethods.TS_E_SYNCHRONOUS;
                    }
                }
            }
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void GetStatus(out UnsafeNativeMethods.TS_STATUS status)
        {
            if (IsTextEditorValid && IsReadOnly)
            {
                // ITfContext::GetStatus() does not take edit cookie. So this could be called
                // out of EditSession. We need to get an access to Dispatcher to check ReadOnly.
                status.dynamicFlags = UnsafeNativeMethods.DynamicStatusFlags.TS_SD_READONLY;
            }
            else
            {
                status.dynamicFlags = 0;
            }

            // This textstore supports Regions.
            status.staticFlags = UnsafeNativeMethods.StaticStatusFlags.TS_SS_REGIONS;
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void QueryInsert(int startIndex, int endIndex, int cch, out int startResultIndex, out int endResultIndex)
        {
            // For now, always ok to insert.
            startResultIndex = startIndex;
            endResultIndex = endIndex;
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void GetSelection(int index, int count, UnsafeNativeMethods.TS_SELECTION_ACP[] selection, out int fetched)
        {
            fetched = 0;

            if (count > 0 && (index == 0 || index == UnsafeNativeMethods.TS_DEFAULT_SELECTION))
            {
                selection[0].start = this.TextSelection.Start.CharOffset;
                selection[0].end = this.TextSelection.End.CharOffset;
                selection[0].style.ase = (this.TextSelection.MovingPosition.CompareTo(this.TextSelection.Start) == 0) ? UnsafeNativeMethods.TsActiveSelEnd.TS_AE_START : UnsafeNativeMethods.TsActiveSelEnd.TS_AE_END;
                selection[0].style.interimChar = _interimSelection;
                fetched = 1;
            }
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void SetSelection(int count, UnsafeNativeMethods.TS_SELECTION_ACP[] selection)
        {
            ITextPointer start;
            ITextPointer end;

            if (count == 1)
            {
                GetNormalizedRange(selection[0].start, selection[0].end, out start, out end);

                if (selection[0].start == selection[0].end)
                {
                    // Setting a caret.  Make sure we set Backward direction to
                    // keep the caret tight with the composition text.
                    this.TextSelection.SetCaretToPosition(start, LogicalDirection.Backward, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/true);
                }
                else if (selection[0].style.ase == UnsafeNativeMethods.TsActiveSelEnd.TS_AE_START)
                {
                    this.TextSelection.Select(end, start);
                }
                else
                {
                    this.TextSelection.Select(start, end);
                }

                // Update the selection style of InterimSelection.
                bool previousInterimSelection = _interimSelection;
                _interimSelection = selection[0].style.interimChar;

                if (previousInterimSelection != _interimSelection)
                {
                    // Call TextSelection to start/stop the block caret.
                    this.TextSelection.OnInterimSelectionChanged(_interimSelection);
                }
            }
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void GetText(int startIndex, int endIndex, char[] text, int cchReq, out int charsCopied,
            UnsafeNativeMethods.TS_RUNINFO[] runInfo, int cRunInfoReq, out int cRunInfoRcv, out int nextIndex)
        {
            ITextPointer navigator;
            ITextPointer limit;
            bool hitLimit;

            charsCopied = 0;
            cRunInfoRcv = 0;
            nextIndex = startIndex;
            if (cchReq == 0 && cRunInfoReq == 0)
                return;

            if (startIndex == endIndex)
                return;

            navigator = CreatePointerAtCharOffset(startIndex, LogicalDirection.Forward);
            limit = (endIndex >= 0) ? CreatePointerAtCharOffset(endIndex, LogicalDirection.Forward) : null;
            hitLimit = false;

            // Loop until we hit something that blocks the get, or until we run
            // out of buffer space.
            while (!hitLimit && (cchReq == 0 || cchReq > charsCopied) && (cRunInfoReq == 0 || cRunInfoReq > cRunInfoRcv))
            {
                TextPointerContext context = navigator.GetPointerContext(LogicalDirection.Forward);

                switch (context)
                {
                    case TextPointerContext.Text:
                        hitLimit = WalkTextRun(navigator, limit, text, cchReq, ref charsCopied, runInfo, cRunInfoReq, ref cRunInfoRcv);
                        break;

                    case TextPointerContext.EmbeddedElement:
                        hitLimit = WalkObjectRun(navigator, limit, text, cchReq, ref charsCopied, runInfo, cRunInfoReq, ref cRunInfoRcv);
                        break;

                    case TextPointerContext.ElementStart:
                        Invariant.Assert(navigator is TextPointer);
                        TextElement element = (TextElement)((TextPointer)navigator).GetAdjacentElement(LogicalDirection.Forward);

                        if (element.IMELeftEdgeCharCount > 0)
                        {
                            Invariant.Assert(element.IMELeftEdgeCharCount == 1);
                            hitLimit = WalkRegionBoundary(navigator, limit, text, cchReq, ref charsCopied, runInfo, cRunInfoReq, ref cRunInfoRcv);
                        }
                        else
                        {
                            navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                            hitLimit = (limit != null && navigator.CompareTo(limit) >= 0);
                        }
                        break;

                    case TextPointerContext.ElementEnd:
                        navigator.MoveToNextContextPosition(LogicalDirection.Forward);
                        hitLimit = (limit != null && navigator.CompareTo(limit) >= 0);
                        break;

                    case TextPointerContext.None:
                        // Hit the begin/end-of-doc.
                        hitLimit = true;
                        break;

                    default:
                        Invariant.Assert(false, "Bogus TextPointerContext!");
                        break;
                }
            }

            nextIndex = navigator.CharOffset;
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void SetText(UnsafeNativeMethods.SetTextFlags flags, int startIndex, int endIndex, char[] text, int cch, out UnsafeNativeMethods.TS_TEXTCHANGE change)
        {
            if (this.IsReadOnly)
            {
                throw new COMException(SR.Get(SRID.TextStore_TS_E_READONLY), UnsafeNativeMethods.TS_E_READONLY);
            }

            ITextPointer start;
            ITextPointer end;

            GetNormalizedRange(startIndex, endIndex, out start, out end);

            while (start != null && TextPointerBase.IsBeforeFirstTable(start))
            {
                start = start.GetNextInsertionPosition(LogicalDirection.Forward);
            }

            if (start == null)
            {
                throw new COMException(SR.Get(SRID.TextStore_CompositionRejected), NativeMethods.E_FAIL);
            }

            if (start.CompareTo(end) > 0)
            {
                end = start;
            }

            string filteredText = FilterCompositionString(new string(text), start.GetOffsetToPosition(end)); // does NOT filter MaxLength.
            if (filteredText == null)
            {
                throw new COMException(SR.Get(SRID.TextStore_CompositionRejected), NativeMethods.E_FAIL);
            }

            // Openes a composition undo unit for the composition undo.
            CompositionParentUndoUnit unit = OpenCompositionUndoUnit();
            UndoCloseAction undoCloseAction = UndoCloseAction.Rollback;

            try
            {
                ITextRange range = new TextRange(start, end, true /* ignoreTextUnitBoundaries */);

                this.TextEditor.SetText(range, filteredText, InputLanguageManager.Current.CurrentInputLanguage);

                change.start = startIndex;
                change.oldEnd = endIndex;
                change.newEnd = endIndex + text.Length - (endIndex - startIndex);

                ValidateChange(change);
                VerifyTextStoreConsistency();

                undoCloseAction = UndoCloseAction.Commit;
            }
            finally
            {
                // Closes compsotion undo unit with commit to add the composition undo unit into the undo stack.
                CloseTextParentUndoUnit(unit, undoCloseAction);
            }
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void GetFormattedText(int startIndex, int endIndex, out object obj)
        {
            obj = null;
            throw new COMException(SR.Get(SRID.TextStore_E_NOTIMPL), unchecked((int)0x80004001));
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void GetEmbedded(int index, ref Guid guidService, ref Guid riid, out object obj)
        {
            obj = null;

#if ENABLE_INK_EMBEDDING
            ITextPointer textPosition;

            if (index < this.TextContainer.IMECharCount)
            {
                // Create a position just following the index and look backward.
                // CreatePointerAtCharOffset always returns a pointer adjacent
                // to the lowest symbol offset matching a given char offset.
                textPosition = CreatePointerAtCharOffset(index + 1, LogicalDirection.Forward);

                if (textPosition.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.EmbeddedElement)
                {
                    object rawobj = textPosition.GetAdjacentElement(LogicalDirection.Forward);
                    InkInteropObject inkobject = rawobj as InkInteropObject;
                    if (inkobject != null)
                    {
                        obj = inkobject.OleDataObject;
                    }
                }
            }
#endif
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void QueryInsertEmbedded(ref Guid guidService, int formatEtc, out bool insertable)
        {
#if true
            //
            // Disable embedded object temporarily because...
            // -  There is no persistency supported including cut and past (Bug 985589).
            // -  It is GDI metadata that is rendered so there is no relation with Avalon ink editing at all.
            // -  This was one of major feature in Cicero on Office XP timeframe however the latest Tablet
            //    Input Panel does not have this feature anymore. (Does it?)
            //
            insertable = false;
#else
            if (TextEditor.AcceptsRichContent)
            {
                // check the guidService or formatEtc before returning true!
                insertable = this.TextSelection.HasConcreteTextContainer;
            }
            else
            {
                insertable = false;
            }
#endif
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void InsertEmbedded(UnsafeNativeMethods.InsertEmbeddedFlags flags, int startIndex, int endIndex, object obj, out UnsafeNativeMethods.TS_TEXTCHANGE change)
        {
            if (IsReadOnly)
            {
                throw new COMException(SR.Get(SRID.TextStore_TS_E_READONLY), UnsafeNativeMethods.TS_E_READONLY);
            }

            // Disable embedded object temporarily.
#if ENABLE_INK_EMBEDDING
            if (!TextSelection.HasConcreteTextContainer)
            {
                throw new COMException(SR.Get(SRID.TextStore_TS_E_FORMAT), UnsafeNativeMethods.TS_E_FORMAT);
            }

            TextContainer container;
            TextPointer startPosition;
            TextPointer endPosition;
            IComDataObject data;

            // The object must have IOldDataObject internface.
            // The obj param of InsertEmbedded is IDataObject in Win32 definition.
            data = obj as IComDataObject;
            if (data == null)
            {
                throw new COMException(SR.Get(SRID.TextStore_BadObject), NativeMethods.E_INVALIDARG);
            }

            container = (TextContainer)this.TextContainer;

            startPosition = container.CreatePointerAtOffset(startIndex, LogicalDirection.Backward);
            endPosition = container.CreatePointerAtOffset(endIndex, LogicalDirection.Forward);
             
            InsertEmbeddedAtRange(startPosition, endPosition, data, out change);
#else
            throw new COMException(SR.Get(SRID.TextStore_TS_E_FORMAT), UnsafeNativeMethods.TS_E_FORMAT);
#endif
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void InsertTextAtSelection(UnsafeNativeMethods.InsertAtSelectionFlags flags, char[] text, int cch, out int startIndex, out int endIndex, out UnsafeNativeMethods.TS_TEXTCHANGE change)
        {
            ITextPointer startNavigator;
            ITextPointer endNavigator;
            int selectionStartIndex;
            int selectionEndIndex;

            startIndex = -1;
            endIndex = -1;

            change.start = 0;
            change.oldEnd = 0;
            change.newEnd = 0;

            if (IsReadOnly)
            {
                throw new COMException(SR.Get(SRID.TextStore_TS_E_READONLY), UnsafeNativeMethods.TS_E_READONLY);
            }

            //
            //
            // The code here that uses ApplyTypingHeuristics and GetAdjustedSelection is
            // fragile in the sense that it is not common with the non-IME code path.
            // We need to refactor the code to change that.
            //

            ITextRange range = new TextRange(this.TextSelection.AnchorPosition, this.TextSelection.MovingPosition);
            range.ApplyTypingHeuristics(false /* overType */);

            ITextPointer start;
            ITextPointer end;

            GetAdjustedSelection(range.Start, range.End, out start, out end);

            // Someone might change the default selection gravity, so use our
            // own TextPositions to track the insert.
            startNavigator = start.CreatePointer();
            startNavigator.SetLogicalDirection(LogicalDirection.Backward);
            endNavigator = end.CreatePointer();
            endNavigator.SetLogicalDirection(LogicalDirection.Forward);

            selectionStartIndex = startNavigator.CharOffset;
            selectionEndIndex = endNavigator.CharOffset;

            // Do the insert.
            if ((flags & UnsafeNativeMethods.InsertAtSelectionFlags.TS_IAS_QUERYONLY) == 0)
            {
                // Opene a composition undo unit for the composition undo.
                CompositionParentUndoUnit unit = OpenCompositionUndoUnit();
                UndoCloseAction undoCloseAction = UndoCloseAction.Rollback;

                try
                {
                    VerifyTextStoreConsistency();

                    change.oldEnd = selectionEndIndex;

                    string filteredText = FilterCompositionString(new string(text), range.Start.GetOffsetToPosition(range.End)); // does NOT filter MaxLength.
                    if (filteredText == null)
                    {
                        throw new COMException(SR.Get(SRID.TextStore_CompositionRejected), NativeMethods.E_FAIL);
                    }

                    // We still need to call ApplyTypingHeuristics, even though
                    // we already did the work above, because it might need
                    // to spring load formatting.
                    this.TextSelection.ApplyTypingHeuristics(false /* overType */);

                    //Invariant.Assert(this.TextSelection.Start.CompareTo(range.Start) == 0 && this.TextSelection.End.CompareTo(range.End) == 0);
                    // We cannot make this Assertion because TextRange will normalize
                    // differently around Floater/Inline edges.  This is probably
                    // not desired behavior.  To repro,
                    //
                    // <StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:sys="clr-namespace:System;assembly=mscorlib" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" >
                    //  <RichTextBox FontSize="24" Height="150">
                    //      <FlowDocument>
                    //          <Paragraph>
                    //              <Run>para</Run>
                    //              <Floater HorizontalAlignment="Right" Width="100" Background="#FFFF0000">
                    //                  <Paragraph><Run>Floater</Run></Paragraph>
                    //              </Floater>
                    //              <Run> </Run>
                    //          </Paragraph>
                    //      </FlowDocument>
                    //  </RichTextBox>
                    // </StackPanel>
                    //
                    // 1. Put the caret before the Floater.
                    // 2. Shift-right to select the entire Floater.
                    // 3. Activate the chinese pinyin IME, and press 'a'.

                    // Avoid calling Select when the selection doesn't need a
                    // final reposition to preserve any spring loaded formatting
                    // from ApplyTypingHeuristics.
                    if (start.CompareTo(this.TextSelection.Start) != 0 ||
                        end.CompareTo(this.TextSelection.End) != 0)
                    {
                        this.TextSelection.Select(start, end);
                    }

                    if (!_isComposing && _previousCompositionStartOffset == -1)
                    {
                        // IMEs have the option (TF_IAS_NO_DEFAULT_COMPOSITION)
                        // of inserting text (via this method only) without first
                        // starting a composition.  If that happens, we need
                        // to remember where the composition started, from the
                        // point of view of the application listening to events
                        // we will raise in the future.
                        _previousCompositionStartOffset = this.TextSelection.Start.Offset;
                        _previousCompositionEndOffset = this.TextSelection.End.Offset;
                    }

                    this.TextEditor.SetSelectedText(filteredText, InputLanguageManager.Current.CurrentInputLanguage);

                    change.start = startNavigator.CharOffset;
                    change.newEnd = endNavigator.CharOffset;

                    ValidateChange(change);
                    VerifyTextStoreConsistency();

                    undoCloseAction = UndoCloseAction.Commit;
                }
                finally
                {
                    // Close a composition undo unit with commit to add the composition undo unit into the undo stack.
                    CloseTextParentUndoUnit(unit, undoCloseAction);
                }
            }

            // Report the location of the new text.
            if ((flags & UnsafeNativeMethods.InsertAtSelectionFlags.TS_IAS_NOQUERY) == 0)
            {
                startIndex = selectionStartIndex;
                endIndex = endNavigator.CharOffset;
            }
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void InsertEmbeddedAtSelection(UnsafeNativeMethods.InsertAtSelectionFlags flags, object obj, out int startIndex, out int endIndex, out UnsafeNativeMethods.TS_TEXTCHANGE change)
        {
            startIndex = -1;
            endIndex = -1;

            change.start = 0;
            change.oldEnd = 0;
            change.newEnd = 0;

            if (IsReadOnly)
            {
                throw new COMException(SR.Get(SRID.TextStore_TS_E_READONLY), UnsafeNativeMethods.TS_E_READONLY);
            }

#if ENABLE_INK_EMBEDDING
            IComDataObject data;

            if (IsReadOnly)
            {
                throw new COMException(SR.Get(SRID.TextStore_TS_E_READONLY), UnsafeNativeMethods.TS_E_READONLY);
            }

            if (!TextSelection.HasConcreteTextContainer)
            {
                throw new COMException(SR.Get(SRID.TextStore_TS_E_FORMAT), UnsafeNativeMethods.TS_E_FORMAT);
            }

            // The object must have IOldDataObject internface.
            // The obj param of InsertEmbedded is IDataObject in Win32 definition.
            data = obj as IComDataObject;
            if (data == null)
            {
                throw new COMException(SR.Get(SRID.TextStore_BadObject), NativeMethods.E_INVALIDARG);
            }

            // Do the insert.
            if ((flags & UnsafeNativeMethods.InsertAtSelectionFlags.TS_IAS_QUERYONLY) == 0)
            {
                InsertEmbeddedAtRange((TextPointer)this.TextSelection.Start, (TextPointer)this.TextSelection.End, data, out change);
            }

            if ((flags & UnsafeNativeMethods.InsertAtSelectionFlags.TS_IAS_NOQUERY) == 0)
            {
                startIndex = this.TextSelection.Start.Offset;
                endIndex = this.TextSelection.End.Offset;
            }
#else
            throw new COMException(SR.Get(SRID.TextStore_TS_E_FORMAT), UnsafeNativeMethods.TS_E_FORMAT);
#endif
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public int RequestSupportedAttrs(UnsafeNativeMethods.AttributeFlags flags, int count, Guid[] filterAttributes)
        {
            // return the default app property value, which target is Scope.
            PrepareAttributes((InputScope)UiScope.GetValue(InputMethod.InputScopeProperty),
                              (double)UiScope.GetValue(TextElement.FontSizeProperty),
                              (FontFamily)UiScope.GetValue(TextElement.FontFamilyProperty),
                              (XmlLanguage)UiScope.GetValue(FrameworkContentElement.LanguageProperty),
                              UiScope as Visual,
                              count, filterAttributes);

            if (_preparedattributes.Count == 0)
                return NativeMethods.S_FALSE;

            return NativeMethods.S_OK;
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public int RequestAttrsAtPosition(int index, int count, Guid[] filterAttributes, UnsafeNativeMethods.AttributeFlags flags)
        {
            ITextPointer position;

            position = CreatePointerAtCharOffset(index, LogicalDirection.Forward);

            PrepareAttributes((InputScope)position.GetValue(InputMethod.InputScopeProperty),
                              (double)position.GetValue(TextElement.FontSizeProperty),
                              (FontFamily)position.GetValue(TextElement.FontFamilyProperty),
                              (XmlLanguage)position.GetValue(FrameworkContentElement.LanguageProperty),
                              null,
                              count, filterAttributes);

            if (_preparedattributes.Count == 0)
                return NativeMethods.S_FALSE;

            return NativeMethods.S_OK;
        }


        // See msdn's ITextStoreACP documentation for a full description.
        public void RequestAttrsTransitioningAtPosition(int position, int count, Guid[] filterAttributes, UnsafeNativeMethods.AttributeFlags flags)
        {
            throw new COMException(SR.Get(SRID.TextStore_E_NOTIMPL), unchecked((int)0x80004001));
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void FindNextAttrTransition(int startIndex, int haltIndex, int count, Guid[] filterAttributes, UnsafeNativeMethods.AttributeFlags flags, out int acpNext, out bool found, out int foundOffset)
        {
            acpNext = 0;
            found = false;
            foundOffset = 0;
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void RetrieveRequestedAttrs(int count, UnsafeNativeMethods.TS_ATTRVAL[] attributeVals, out int fetched)
        {
            fetched = 0;
            int i;

            for (i = 0; i < count; i++)
            {
                if (i >= _preparedattributes.Count)
                    break;

                attributeVals[i] = ((UnsafeNativeMethods.TS_ATTRVAL)_preparedattributes[i]);
                fetched++;
            }

            // clear _preparedattributes now so we can keep the ref count of val if it is VT_UNKNOWN.
            _preparedattributes.Clear();
            _preparedattributes = null;
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void GetEnd(out int end)
        {
            end = this.TextContainer.IMECharCount;
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void GetActiveView(out int viewCookie)
        {
            viewCookie = _viewCookie;
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void GetACPFromPoint(int viewCookie, ref UnsafeNativeMethods.POINT tsfPoint, UnsafeNativeMethods.GetPositionFromPointFlags flags, out int positionCP)
        {

            PresentationSource source;
            IWin32Window win32Window;
            CompositionTarget compositionTarget;
            ITextView view;
            Point milPoint;
            ITextPointer position;
            NativeMethods.POINT point;

            point = new NativeMethods.POINT(tsfPoint.x, tsfPoint.y);
            GetVisualInfo(out source, out win32Window, out view);
            compositionTarget = source.CompositionTarget;

            // Convert to client coordinates.
            SafeNativeMethods.ScreenToClient(new HandleRef(null,win32Window.Handle), point);

            // Convert to mil measure units.
            milPoint = new Point(point.x, point.y);
            milPoint = compositionTarget.TransformFromDevice.Transform(milPoint);

            // Convert to local coordinates.
            GeneralTransform transform = compositionTarget.RootVisual.TransformToDescendant(RenderScope);
            if (transform != null)
            {
                // REVIEW: should we throw if the point could not be transformed?
                transform.TryTransform(milPoint, out milPoint);
            }

            // Validate layout information on TextView
            if (!view.Validate(milPoint))
            {
                throw new COMException(SR.Get(SRID.TextStore_TS_E_NOLAYOUT), UnsafeNativeMethods.TS_E_NOLAYOUT);
            }

            // Do the hittest.
            position = view.GetTextPositionFromPoint(milPoint, (flags & UnsafeNativeMethods.GetPositionFromPointFlags.GXFPF_NEAREST) != 0 /* snapToText */);
            if (position == null)
            {
                // GXFPF_ROUND_NEAREST was clear and we didn't hit a char.
                throw new COMException(SR.Get(SRID.TextStore_TS_E_INVALIDPOINT), UnsafeNativeMethods.TS_E_INVALIDPOINT);
            }

            positionCP = position.CharOffset;
            if ((flags & UnsafeNativeMethods.GetPositionFromPointFlags.GXFPF_ROUND_NEAREST) == 0)
            {
                // Check if the point is on the backward position of the TextPosition.
                Rect rectCur;
                Rect rectPrev;
                Point milPointTopLeft;
                Point milPointBottomRight;

                ITextPointer positionCur = position.CreatePointer(LogicalDirection.Backward);
                ITextPointer positionPrev = position.CreatePointer(LogicalDirection.Forward);
                positionPrev.MoveToNextInsertionPosition(LogicalDirection.Backward);

                rectCur = view.GetRectangleFromTextPosition(positionCur);
                rectPrev = view.GetRectangleFromTextPosition(positionPrev);

                // Take the "extended" union of the previous char's bounding box.
                milPointTopLeft = new Point(Math.Min(rectPrev.Left, rectCur.Left), Math.Min(rectPrev.Top, rectCur.Top));
                milPointBottomRight = new Point(Math.Max(rectPrev.Left, rectCur.Left), Math.Max(rectPrev.Bottom, rectCur.Bottom));

                // The rect of the previous char.
                Rect rectTest = new Rect(milPointTopLeft, milPointBottomRight);
                if (rectTest.Contains(milPoint))
                    positionCP--;
            }
        }

        // See msdn's ITextStoreACP documentation for a full description.
        void UnsafeNativeMethods.ITextStoreACP.GetTextExt(int viewCookie, int startIndex, int endIndex, out UnsafeNativeMethods.RECT rect, out bool clipped)
        {
            PresentationSource source;
            IWin32Window win32Window;
            CompositionTarget compositionTarget;
            ITextView view;
            ITextPointer startPointer;
            ITextPointer endPointer;
            GeneralTransform transform;
            Point milPointTopLeft;
            Point milPointBottomRight;

            // We need to update the layout before getting rect. It could be dirty by SetText call of TIP.
            _isInUpdateLayout = true;
            UiScope.UpdateLayout();
            _isInUpdateLayout = false;

            // if UpdateLayout caused a text change, startIndex
            // and endIndex are no longer valid.  Handling this correctly is quite
            // difficult - Cicero assumes that the text can't change while it
            // owns the lock.  Instead, we artificially reset the char count (to
            // keep VerifyTextStoreConsistency happy), and return TS_R_NOLAYOUT
            // to the caller; this seems to be good enough (i.e. avoids crashes)
            // in practice.
            if (_hasTextChangedInUpdateLayout)
            {
                _netCharCount = this.TextContainer.IMECharCount;
                throw new COMException(SR.Get(SRID.TextStore_TS_E_NOLAYOUT), UnsafeNativeMethods.TS_E_NOLAYOUT);
            }

            rect = new UnsafeNativeMethods.RECT();
            clipped = false;
            GetVisualInfo(out source, out win32Window, out view);
            compositionTarget = source.CompositionTarget;

            // We use local coordinates.
            startPointer = CreatePointerAtCharOffset(startIndex, LogicalDirection.Forward);
            startPointer.MoveToInsertionPosition(LogicalDirection.Forward);

            if (!this.TextView.IsValid)
            {
                // We can not get the visual. Return TS_R_NOLAYOUT to the caller.
                throw new COMException(SR.Get(SRID.TextStore_TS_E_NOLAYOUT), UnsafeNativeMethods.TS_E_NOLAYOUT);
            }

            if (startIndex == endIndex)
            {
                Rect rectStart = startPointer.GetCharacterRect(LogicalDirection.Forward);
                milPointTopLeft = rectStart.TopLeft;
                milPointBottomRight = rectStart.BottomRight;
            }
            else
            {
                Rect rectBound = new Rect(Size.Empty);
                ITextPointer navigator = startPointer.CreatePointer();
                endPointer = CreatePointerAtCharOffset(endIndex, LogicalDirection.Backward);
                endPointer.MoveToInsertionPosition(LogicalDirection.Backward);
                bool moved;

                do
                {
                    // Compute the textSegment bounds line by line.
                    TextSegment lineRange = this.TextView.GetLineRange(navigator);
                    ITextPointer end;
                    Rect lineRect;

                    // Skip any BlockUIContainer or any other content that is not treated as a line by TextView.
                    if (!lineRange.IsNull)
                    {
                        ITextPointer start = (lineRange.Start.CompareTo(startPointer) <= 0) ? startPointer : lineRange.Start;
                        end = (lineRange.End.CompareTo(endPointer) >= 0) ? endPointer : lineRange.End;

                        lineRect = GetLineBounds(start, end);
                        moved = (navigator.MoveToLineBoundary(1) != 0) ? true : false;
                    }
                    else
                    {
                        lineRect = navigator.GetCharacterRect(LogicalDirection.Forward);
                        moved = navigator.MoveToNextInsertionPosition(LogicalDirection.Forward);
                        end = navigator;
                    }

                    if (lineRect.IsEmpty == false)
                    {
                        rectBound.Union(lineRect);
                    }

                    if (end.CompareTo(endPointer) == 0)
                    {
                        break;
                    }
                }
                while (moved);

                // Invariant.Assert(rectBound.IsEmpty == false);

                milPointTopLeft = rectBound.TopLeft;
                milPointBottomRight = rectBound.BottomRight;
            }

            // Transform to root visual coordinates.
            transform = UiScope.TransformToAncestor(compositionTarget.RootVisual);

            // REVIEW: should we use TransformBounds here?
            transform.TryTransform(milPointTopLeft, out milPointTopLeft);
            transform.TryTransform(milPointBottomRight, out milPointBottomRight);

            rect = TransformRootRectToScreenCoordinates(milPointTopLeft, milPointBottomRight, win32Window, source);
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void GetScreenExt(int viewCookie, out UnsafeNativeMethods.RECT rect)
        {
            PresentationSource source;
            IWin32Window win32Window;
            ITextView view;
            CompositionTarget compositionTarget;
            Rect rectUi;
            Rect rectDescendant;
            Point milPointTopLeft;
            Point milPointBottomRight;
            GeneralTransform transform;

            rectUi = UiScope.VisualContentBounds;
            rectDescendant = UiScope.VisualDescendantBounds;
            rectUi.Union(rectDescendant);

            //
            // Do we need to check cliping rgn?
            //

            GetVisualInfo(out source, out win32Window, out view);
            compositionTarget = source.CompositionTarget;

            // Take the points of the renderScope.
            milPointTopLeft = new Point(rectUi.Left, rectUi.Top);
            milPointBottomRight = new Point(rectUi.Right, rectUi.Bottom);

            // Transform to root visual coordinates.
            transform = UiScope.TransformToAncestor(compositionTarget.RootVisual);

            // REVIEW: should we use TransformBounds here?
            transform.TryTransform(milPointTopLeft, out milPointTopLeft);
            transform.TryTransform(milPointBottomRight, out milPointBottomRight);
            rect = TransformRootRectToScreenCoordinates(milPointTopLeft, milPointBottomRight, win32Window, source);
        }

        // See msdn's ITextStoreACP documentation for a full description.
        void UnsafeNativeMethods.ITextStoreACP.GetWnd(int viewCookie, out IntPtr hwnd)
        {
            hwnd = IntPtr.Zero;
            hwnd = CriticalSourceWnd;
        }

        #endregion ITextStoreACP

        //------------------------------------------------------
        //
        //  Methods - ITfThreadFocusSink
        //
        //------------------------------------------------------

        #region ITfThreadFocusSink

        // See msdn's ITextStoreACP documentation for a full description.
        void UnsafeNativeMethods.ITfThreadFocusSink.OnSetThreadFocus()
        {
            if (!IsTextEditorValid)
            {
                return;
            }

            // Reset the focus, cicero won't do it for us.
            if (Keyboard.FocusedElement == UiScope)
            {
                OnGotFocus();
            }
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void OnKillThreadFocus()
        {
        }

        #endregion ITfThreadFocusSink

        //------------------------------------------------------
        //
        //  Methods - ITfContextOwnerCompositionSink
        //
        //------------------------------------------------------

        #region ITfContextOwnerCompositionSink

        // See msdn's ITextStoreACP documentation for a full description.
        public void OnStartComposition(UnsafeNativeMethods.ITfCompositionView view, out bool ok)
        {
            // Disallow multiple compositions.
            if (_isComposing)
            {
                ok = false;
                return;
            }

            ITextPointer start;
            ITextPointer end;

            GetCompositionPositions(view, out start, out end);

            // The call to MarkCultureProperty or SetText (which calls MarkCultureProperty)
            // modifies the start and end TextPointers in the case of a multiple characters being replaced by 
            // input from the IMEPad in a langugage different than that of the current text.
            // startOffsetBefore, endOffsetBefore and _lastCompositionText are stored in a 
            // CompositionEventRecord to be later replayed in RaiseCompositionEvents (after releasing the lock).
            // Store these variables based off of the original start and end TextPointers.
            int startOffsetBefore = start.Offset;
            int endOffsetBefore = end.Offset;
            _lastCompositionText = TextRangeBase.GetTextInternal(start, end);
            
            if (_previousCompositionStartOffset != -1)
            {
                startOffsetBefore = _previousCompositionStartOffset;
                endOffsetBefore = _previousCompositionEndOffset;
            }
            else
            {
               if (this.TextEditor.AcceptsRichContent && start.CompareTo(end) != 0)
                {
                    TextElement startElement = (TextElement)((TextPointer)start).Parent;
                    TextElement endElement = (TextElement)((TextPointer)end).Parent;
                    TextElement commonAncestor = TextElement.GetCommonAncestor(startElement, endElement);
                    
                    int originalIMECharCount = this.TextContainer.IMECharCount;
                    TextRange range = new TextRange(start, end);
                    string unmergedText = range.Text;
                    
                    if (commonAncestor is Run)
                    {
                        // A single Run needs to be handled differently from the cases below since the
                        // serialized text for the range can include extra characters for things like
                        // ListItems, which could cause us to increase the number of characters visible
                        // to the IME in the document.
                        this.TextEditor.MarkCultureProperty(range, InputLanguageManager.Current.CurrentInputLanguage);
                    }
                    else if (commonAncestor is Paragraph || commonAncestor is Span)
                    {
                        // Check if the IME is jump-starting a composition over existing content.
                        // This is problematic if the existing content spans multiple
                        // Inlines or the language of the existing content differs from the
                        // current input language.
                        // The IME will likely edit just a subset of the composition range.
                        // But later, in UpdateCompositionText, we will update a larger range
                        // (the whole composition) which could merge Runs.  And once we
                        // merge Runs the IME did not originally merge, our recorded character
                        // offsets are out of synch and very bad things will happen.
                        // Force any merges now by replacing the content with a single
                        // Run, before we start caching character offsets.

                        this.TextEditor.SetText(range, unmergedText, InputLanguageManager.Current.CurrentInputLanguage);                      
                    }
                    // It is crucial that from the point of view of the IME the document
                    // has not changed.  That means the plain text of the content we just
                    // replaced must not have changed.
                    Invariant.Assert(range.Text == unmergedText);
                    Invariant.Assert(originalIMECharCount == this.TextContainer.IMECharCount);
                }
            }
            
            // Add the composition message into the composition message list.
            // This composition message list will be handled all together after we release the lock.
            this.CompositionEventList.Add(new CompositionEventRecord(CompositionStage.StartComposition, startOffsetBefore, endOffsetBefore, _lastCompositionText));

            _previousCompositionStartOffset = start.Offset;
            _previousCompositionEndOffset = end.Offset;

            _isComposing = true;

            // Composition event is completed, so new composition undo unit will be opened.
            BreakTypingSequence(end);

            ok = true;
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void OnUpdateComposition(UnsafeNativeMethods.ITfCompositionView view, UnsafeNativeMethods.ITfRange rangeNew)
        {
            // If UiScope has a ToolTip and it is open, any keyboard/mouse activity should close the tooltip.
            this.TextEditor.CloseToolTip();

            Invariant.Assert(_isComposing);
            Invariant.Assert(_previousCompositionStartOffset != -1);

            ITextPointer oldStart;
            ITextPointer oldEnd;

            GetCompositionPositions(view, out oldStart, out oldEnd);

            ITextPointer newStart = null;
            ITextPointer newEnd = null;

            bool compositionRangeShifted = false;

            if (rangeNew != null)
            {
                TextPositionsFromITfRange(rangeNew, out newStart, out newEnd);
                compositionRangeShifted = (newStart.Offset != oldStart.Offset || newEnd.Offset != oldEnd.Offset);
            }

            string compositionText = TextRangeBase.GetTextInternal(oldStart, oldEnd);

            if (compositionRangeShifted)
            {
                // Add internal shift record to process it later when we raise events in RaiseCompositionEvents.
                CompositionEventRecord record = new CompositionEventRecord(CompositionStage.UpdateComposition, _previousCompositionStartOffset, _previousCompositionEndOffset, compositionText, true);
                this.CompositionEventList.Add(record);

                _previousCompositionStartOffset = newStart.Offset;
                _previousCompositionEndOffset = newEnd.Offset;

                _lastCompositionText = null;
            }
            else
            {
                // Add the composition message into the composition message list.
                // This composition message list will be handled all together after release the lock.

                CompositionEventRecord record = new CompositionEventRecord(CompositionStage.UpdateComposition, _previousCompositionStartOffset, _previousCompositionEndOffset, compositionText);
                CompositionEventRecord previousRecord = (this.CompositionEventList.Count == 0) ? null : this.CompositionEventList[this.CompositionEventList.Count - 1];

                if (_lastCompositionText == null ||
                    String.CompareOrdinal(compositionText, _lastCompositionText) != 0)
                {
                    // Add the new update event.
                    this.CompositionEventList.Add(record);
                }

                _previousCompositionStartOffset = oldStart.Offset;
                _previousCompositionEndOffset = oldEnd.Offset;
                _lastCompositionText = compositionText;
            }

            // Composition event is completed, so new composition undo unit will be opened.
            BreakTypingSequence(oldEnd);
        }

        // See msdn's ITextStoreACP documentation for a full description.
        public void OnEndComposition(UnsafeNativeMethods.ITfCompositionView view)
        {
            Invariant.Assert(_isComposing);
            Invariant.Assert(_previousCompositionStartOffset != -1);

            ITextPointer start;
            ITextPointer end;

            GetCompositionPositions(view, out start, out end);

            // If we're called from inside the scope of HandleCompositionEvents
            // we won't be raising any events.
            if (_compositionEventState == CompositionEventState.NotRaisingEvents)
            {
                // Add the composition message into the composition message list.
                // This composition message list will be handled all together after release the lock.
                this.CompositionEventList.Add(new CompositionEventRecord(CompositionStage.EndComposition, start.Offset, end.Offset, TextRangeBase.GetTextInternal(start, end)));

                // Composition event is completed, so new composition undo unit will be opened.
                CompositionParentUndoUnit unit = PeekCompositionParentUndoUnit();
                if (unit != null)
                {
                    unit.IsLastCompositionUnit = true;
                }
            }

            _nextUndoUnitIsFirstCompositionUnit = true;
            _isComposing = false;
            _previousCompositionStartOffset = -1;
            _previousCompositionEndOffset = -1;

            // The composition no longer exist. We should stop the interim block caret.
            if (_interimSelection)
            {
                _interimSelection = false;
                TextSelection.OnInterimSelectionChanged(_interimSelection);
            }

            BreakTypingSequence(end);
        }

        #endregion ITfContextOwnerCompositionSink

        //------------------------------------------------------
        //
        //  Methods - ITfTextEditSink
        //
        //------------------------------------------------------

        #region ITfTextEditSink

        // See msdn's ITextStoreACP documentation for a full description.
        void UnsafeNativeMethods.ITfTextEditSink.OnEndEdit(UnsafeNativeMethods.ITfContext context, int ecReadOnly, UnsafeNativeMethods.ITfEditRecord editRecord)
        {
            // Call text service's property OnEndEdit.
            _textservicesproperty.OnEndEdit(context, ecReadOnly, editRecord);

            // Release editRecord so Finalizer won't do Release() to Cicero's object in GC thread.
            Marshal.ReleaseComObject(editRecord);
        }

        #endregion ITfTextEditSink

        //------------------------------------------------------
        //
        //  Public Methods - ITfTransitoryExtensionSink
        //
        //------------------------------------------------------

        #region ITfTransitoryExtensionSink

        // Transitory Document has been updated.
        // This is the notification of the changes of the result string and the composition string.
        public void OnTransitoryExtensionUpdated(UnsafeNativeMethods.ITfContext context, int ecReadOnly, UnsafeNativeMethods.ITfRange rangeResult, UnsafeNativeMethods.ITfRange rangeComposition, out bool fDeleteResultRange)
        {
            fDeleteResultRange = true;

            if (rangeResult != null)
            {
                string result = StringFromITfRange(rangeResult, ecReadOnly);
                if (result.Length > 0)
                {
                    if (TextEditor.AllowOvertype && TextEditor._OvertypeMode && TextSelection.IsEmpty)
                    {
                        // Extend selection forward to innclude next character within this paragraph
                        ITextPointer navigator;

                        navigator = TextSelection.End.CreatePointer();
                        navigator.MoveToInsertionPosition(LogicalDirection.Forward);

                        // There is a bug here: when textData contains more than 1 character
                        // we need to eat that number of symbols in TextContainer.
                        // Currently we eat only one always.
                        if (navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                        {
                            Char[] nextChars;
                            nextChars = new Char[2];

                            // Check if the caret stands before newline - we should not eat it in overtype mode.
                            navigator.GetTextInRun(LogicalDirection.Forward, nextChars, 0, nextChars.Length);
                            if (!(nextChars[0] == Environment.NewLine[0] && nextChars[1] == Environment.NewLine[1]))
                            {
                                int cnt = result.Length;
                                while (cnt-- > 0)
                                {
                                    TextSelection.ExtendToNextInsertionPosition(LogicalDirection.Forward);
                                }
                            }
                        }
                    }

                    string filteredText = FilterCompositionString(result, TextSelection.Start.GetOffsetToPosition(TextSelection.End)); // does NOT filter MaxLength.
                    if (filteredText == null)
                    {
                        throw new COMException(SR.Get(SRID.TextStore_CompositionRejected), NativeMethods.E_FAIL);
                    }

                    this.TextEditor.SetText(TextSelection, filteredText, InputLanguageManager.Current.CurrentInputLanguage);
                    TextSelection.Select(TextSelection.End, TextSelection.End);
                }
            }
        }

        #endregion ITfTransitoryExtensionSink

        //------------------------------------------------------
        //
        //  Public Methods - ITfMouseTrackerACP
        //
        //------------------------------------------------------

        #region ITfMouseTrackerACP

        // new mouse sink is registered.
        public int AdviceMouseSink(UnsafeNativeMethods.ITfRangeACP range, UnsafeNativeMethods.ITfMouseSink sink, out int dwCookie)
        {
            if (_mouseSinks == null)
            {
                _mouseSinks = new ArrayList(1);
            }

            // Find sinks.
            _mouseSinks.Sort();
            for (dwCookie = 0; dwCookie < _mouseSinks.Count; dwCookie++)
            {
                if (((MouseSink)_mouseSinks[dwCookie]).Cookie != dwCookie)
                {
                    break;
                }
            }

            // -1 is an invalid cookie value. This should not happen.
            Invariant.Assert(dwCookie != UnsafeNativeMethods.TF_INVALID_COOKIE);

            _mouseSinks.Add(new MouseSink(range, sink, dwCookie));


            if (_mouseSinks.Count == 1)
            {
                // If this is the first sink, start listening mouse event for MSIME mouse operation.
                UiScope.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(OnMouseButtonEvent);
                UiScope.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(OnMouseButtonEvent);
                UiScope.PreviewMouseRightButtonDown += new MouseButtonEventHandler(OnMouseButtonEvent);
                UiScope.PreviewMouseRightButtonUp += new MouseButtonEventHandler(OnMouseButtonEvent);
                UiScope.PreviewMouseMove += new MouseEventHandler(OnMouseEvent);
            }
            return NativeMethods.S_OK;
        }

        // existing mouse sink is unadviced.
        public int UnadviceMouseSink(int dwCookie)
        {
            int ret = NativeMethods.E_INVALIDARG;
            int i;
            for (i = 0; i < _mouseSinks.Count; i++)
            {
                MouseSink mSink = (MouseSink)_mouseSinks[i];
                if (mSink.Cookie == dwCookie)
                {
                    _mouseSinks.RemoveAt(i);

                    if (_mouseSinks.Count == 0)
                    {
                        // If there is no registerd sink, stop listening mouse event for MSIME mouse operation.
                        UiScope.PreviewMouseLeftButtonDown -= new MouseButtonEventHandler(OnMouseButtonEvent);
                        UiScope.PreviewMouseLeftButtonUp -= new MouseButtonEventHandler(OnMouseButtonEvent);
                        UiScope.PreviewMouseRightButtonDown -= new MouseButtonEventHandler(OnMouseButtonEvent);
                        UiScope.PreviewMouseRightButtonUp -= new MouseButtonEventHandler(OnMouseButtonEvent);
                        UiScope.PreviewMouseMove -= new MouseEventHandler(OnMouseEvent);
                    }

                    // Dispose sink and range.
                    if (mSink.Locked)
                    {
                        mSink.PendingDispose = true;
                    }
                    else
                    {
                        mSink.Dispose();
                    }
                    ret = NativeMethods.S_OK;
                    break;
                }
            }

            return ret;
        }

        #endregion ITfMouseTrackerACP

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Called by the TextEditor when the document should go live.
        internal void OnAttach()
        {
            _netCharCount = this.TextContainer.IMECharCount;

            // keep _textservicesHost because we may not be in Dispatcher when GC Finalizer calls OnDetach().
            _textservicesHost = TextServicesHost.Current;

            _textservicesHost.RegisterTextStore(this);

            this.TextContainer.Change += new TextContainerChangeEventHandler(OnTextContainerChange);

            _textservicesproperty = new TextServicesProperty(this);
        }

        // Called by the TextEditor when the document should shut down.
        internal void OnDetach(bool finalizer)
        {
            // TextEditor could be GCed before.
            if (this.IsTextEditorValid)
            {
                this.TextContainer.Change -= new TextContainerChangeEventHandler(OnTextContainerChange);
            }

            // Relase the naitive resources. Unregister ThreadFocusSink and EditSink and release DocumentManager.
            _textservicesHost.UnregisterTextStore(this, finalizer);

            _textservicesproperty = null;
        }

        // Called when our TextEditor.TextContainer gets keyboard focus.
        internal void OnGotFocus()
        {
            //Re-enable this assert once known conditions are clearer.
            //Invariant.Assert(Scope.IsKeyboardFocused);

            // We don't set focus to the DocumentManager if InputMethod is disabled in this element.
            // InputMethod already called ThreadMgr.SetFocus(EmptyDIM) if IsInputMethodEnabledProperty is false.
            if ((bool)UiScope.GetValue(InputMethod.IsInputMethodEnabledProperty))
            {
                // ThreadMgr Method call will be marshalled to the dispatcher thread since Ciecro is STA.
                _textservicesHost.ThreadManager.SetFocus(DocumentManager);
            }

            if (_makeLayoutChangeOnGotFocus)
            {
                OnLayoutUpdated();
                _makeLayoutChangeOnGotFocus = false;
            }
        }

        // Called when losing keyboard focus.
        // Finalizes the current composition.
        internal void OnLostFocus()
        {
            CompleteComposition();
        }

        // Called when the layout of the rendered TextContainer changes.
        // Called explicitly by the TextEditor.
        internal void OnLayoutUpdated()
        {
            if (HasSink)
            {
                // Sink Method call will be marshalled to the dispatcher thread since Ciecro is STA.
                _sink.OnLayoutChange(UnsafeNativeMethods.TsLayoutCode.TS_LC_CHANGE, _viewCookie);
            }

            if (_textservicesproperty != null)
            {
                _textservicesproperty.OnLayoutUpdated();
            }
        }

        // Called as the selection changes.
        // We can't modify document state here in any way.
        internal void OnSelectionChange()
        {
            _compositionModifiedByEventListener = true;
        }

        // Called when the selection changes position.
        // Called explicitly by the TextEditor.
        /// <summary>
        /// Critical - calls unmanaged code (_sink)
        /// TreatAsSafe - notifies of selection change, no potential data leak, this is safe
        /// </summary>
        internal void OnSelectionChanged()
        {
            if (_compositionEventState == CompositionEventState.RaisingEvents)
            {
                return;
            }

            if (_ignoreNextSelectionChange)
            {
                // If this change originated from a TIP, ignore it.
                // Note if there's a reentrant 2nd selection change
                // inside the current change block notification, we won't
                // ignore the selection change event, which is what we want
                // (and why we have to clear the flag right now).
                _ignoreNextSelectionChange = false;
            }
            else if (HasSink)
            {
                // Sink Method call will be marshalled to the dispatcher thread since Ciecro is STA.
                _sink.OnSelectionChange();
            }
        }

        // Query or do reconvert for the current selection.
        internal bool QueryRangeOrReconvertSelection(bool fDoReconvert)
        {
            // If there is a composition that covers the current selection,
            // we can return it is reconvertable.
            // Some TIP may finalize and cancel the current candidate list (Bug 1291712).
            if (_isComposing && !fDoReconvert)
            {
                ITextPointer compositionStart;
                ITextPointer compositionEnd;

                GetCompositionPositions(out compositionStart, out compositionEnd);

                if ((compositionStart != null) &&
                    (compositionEnd != null))
                {
                    if ((compositionStart.CompareTo(TextSelection.Start) <= 0) &&
                        (compositionStart.CompareTo(TextSelection.End) <= 0) &&
                        (compositionEnd.CompareTo(TextSelection.Start) >= 0) &&
                        (compositionEnd.CompareTo(TextSelection.End) >= 0))
                    {
                        return true;
                    }
                }
            }

            bool fReconvertable = false;
            UnsafeNativeMethods.ITfFnReconversion funcReconv;
            UnsafeNativeMethods.ITfRange range;

            fReconvertable = GetFnReconv(TextSelection.Start, TextSelection.End, out funcReconv, out range);

            if (funcReconv != null)
            {
                if (fDoReconvert)
                    funcReconv.Reconvert(range);

                Marshal.ReleaseComObject(funcReconv);
            }

            if (range != null)
            {
                Marshal.ReleaseComObject(range);
            }

            return fReconvertable;
        }

        // Query or do reconvert for the current selection.
        internal UnsafeNativeMethods.ITfCandidateList GetReconversionCandidateList()
        {
            bool fReconvertable = false;
            UnsafeNativeMethods.ITfFnReconversion funcReconv;
            UnsafeNativeMethods.ITfRange range;
            UnsafeNativeMethods.ITfCandidateList candidateList = null;
            fReconvertable = GetFnReconv(TextSelection.Start, TextSelection.End, out funcReconv, out range);

            if (funcReconv != null)
            {
                funcReconv.GetReconversion(range, out candidateList);
                Marshal.ReleaseComObject(funcReconv);
            }

            if (range != null)
            {
                Marshal.ReleaseComObject(range);
            }

            return candidateList;
        }

        // Query or do reconvert for the current selection.
        private bool GetFnReconv(ITextPointer textStart, ITextPointer textEnd, out UnsafeNativeMethods.ITfFnReconversion funcReconv, out UnsafeNativeMethods.ITfRange rangeNew)
        {
            UnsafeNativeMethods.ITfContext context;
            UnsafeNativeMethods.ITfRange range;
            UnsafeNativeMethods.ITfRangeACP rangeACP;
            bool fReconvertable = false;

            funcReconv = null;
            rangeNew = null;

            // Create ITfRangeACP for the current selection.
            //  1. Get the context from the document manager and call GetStart to get the instance of
            //     ITfRange.
            //  2. QI the ITfRange to get ITfRangeACP.
            //  3. Get start and end of the current selection from TextContainer.
            //  4. Set extent of the ITfRangeACP.
            DocumentManager.GetBase(out context);
            context.GetStart(EditCookie, out range);
            rangeACP = range as UnsafeNativeMethods.ITfRangeACP;
            int start = textStart.CharOffset;
            int end = textEnd.CharOffset;
            rangeACP.SetExtent(start, end - start);

            // Readonly fields can not be passed ref to the interface methods.
            // Create pads for them.
            Guid guidSysFunc = UnsafeNativeMethods.GUID_SYSTEM_FUNCTIONPROVIDER;
            Guid guidNull = UnsafeNativeMethods.Guid_Null;
            Guid iidFnReconv = UnsafeNativeMethods.IID_ITfFnReconversion;

            UnsafeNativeMethods.ITfFunctionProvider functionPrv;

            // ThreadMgr Method call will be marshalled to the dispatcher thread since Ciecro is STA.
            _textservicesHost.ThreadManager.GetFunctionProvider(ref guidSysFunc, out functionPrv);

            object obj;

            // ITfFnReconversion is always available in SystemFunctionProvider.
            functionPrv.GetFunction(ref guidNull, ref iidFnReconv, out obj);
            funcReconv = obj as UnsafeNativeMethods.ITfFnReconversion;
            funcReconv.QueryRange(range, out rangeNew, out fReconvertable);


            // release objects.
            Marshal.ReleaseComObject(functionPrv);

            if (!fReconvertable)
            {
                Marshal.ReleaseComObject(funcReconv);
                funcReconv = null;
            }

            Marshal.ReleaseComObject(range);
            Marshal.ReleaseComObject(context);

            return fReconvertable;
        }

        // Completes the current composition, if any, asynchronously.
        internal void CompleteCompositionAsync()
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(CompleteCompositionHandler), null);
        }

        // Completes the current composition, if any.
        internal void CompleteComposition()
        {
            if (_isComposing)
            {
                FrameworkTextComposition.CompleteCurrentComposition(this.DocumentManager);
            }

            _previousCompositionStartOffset = -1;
            _previousCompositionEndOffset = -1;

            _previousCompositionStart = null;
            _previousCompositionEnd = null;
        }

        // Creates an ITextPointer at a specific character offset.
        internal ITextPointer CreatePointerAtCharOffset(int charOffset, LogicalDirection direction)
        {
            ValidateCharOffset(charOffset);

            ITextPointer pointer = this.TextContainer.CreatePointerAtCharOffset(charOffset, direction);

            if (pointer == null)
            {
                // A null pointer means that the ITextContainer has no character offsets.
                // This happens in an empty TextBox, or in a mal-formed RichTextBox.
                // In either case, use the selection start.
                pointer = this.TextSelection.Start.CreatePointer(direction);
            }

            return pointer;
        }

        internal void MakeLayoutChangeOnGotFocus()
        {
            if (_isComposing)
            {
                _makeLayoutChangeOnGotFocus = true;
            }
        }
        /// <summary>
        /// Inserts composition text into the document.
        /// Raises public text, selection changed events.
        /// Called by default editor TextInputEvent handler.
        /// </summary>
        /// <param name="composition"></param>
        internal void UpdateCompositionText(FrameworkTextComposition composition)
        {
            if (_compositionModifiedByEventListener)
            {
                // If the app has modified the document since this event was raised
                // (by hooking a TextInput event), then we don't know what to do,
                // so do nothing.
                return;
            }

            _handledByTextStoreListener = true;

            bool isMaxLengthExceeded = false;
            string text;
            ITextRange range;

            if (composition._ResultStart != null)
            {
                //
                // If we're here it means composition is being finalized
                //
                range = new TextRange(composition._ResultStart, composition._ResultEnd, true /* ignoreTextUnitBoundaries */);
                text = this.TextEditor._FilterText(composition.Text, range);

                if (text.Length != composition.Text.Length)
                {
                    isMaxLengthExceeded = true;
                }
            }
            else
            {
                range = new TextRange(composition._CompositionStart, composition._CompositionEnd, true /* ignoreTextUnitBoundaries */);
                text = this.TextEditor._FilterText(composition.CompositionText, range, /*filterMaxLength:*/false);

                // A change in length should not happen other than for MaxLength filtering during finalization since we cover those
                // cases and reject input if necessary when the IME edits the document in the first place.
                Invariant.Assert(text.Length == composition.CompositionText.Length);
            }

            //
            // Preparing to create new Composition undo unit and
            // set it as the last composition unit.
            // this is important for further call to MergeCompositionUndoUnits.
            //
            _nextUndoUnitIsFirstCompositionUnit = false;
            CompositionParentUndoUnit topUndoUnit = PeekCompositionParentUndoUnit();
            if (null != topUndoUnit)
            {
                topUndoUnit.IsLastCompositionUnit = false;
            }

            CompositionParentUndoUnit compositionUndoUnit = OpenCompositionUndoUnit(range.Start, range.End);
            UndoCloseAction undoCloseAction = UndoCloseAction.Rollback;

            if (composition._ResultStart != null)
            {
                // If the composition is being finalized, this will be the last undo unit in this group.
                _nextUndoUnitIsFirstCompositionUnit = true;
                compositionUndoUnit.IsLastCompositionUnit = true;
            }

            this.TextSelection.BeginChange();
            try
            {
                this.TextEditor.SetText(range, text, InputLanguageManager.Current.CurrentInputLanguage);

                // shouldn't we record the selection position from the original event instead?
                if (_interimSelection)
                {
                    this.TextSelection.Select(range.Start, range.End);
                }
                else
                {
                    this.TextSelection.SetCaretToPosition(range.End, LogicalDirection.Backward, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/true);
                }

                compositionUndoUnit.RecordRedoSelectionState(range.End, range.End);
                undoCloseAction = UndoCloseAction.Commit;
            }
            finally
            {
                // We're about to raise the public event.
                // Set a flag so we can detect app changes.
                _compositionModifiedByEventListener = isMaxLengthExceeded;

                // PUBLIC EVENT:
                this.TextSelection.EndChange();

                CloseTextParentUndoUnit(compositionUndoUnit, undoCloseAction);
            }
        }
        internal static FrameworkTextComposition CreateComposition(TextEditor editor, object owner)
        {
            FrameworkTextComposition composition;

            // FrameworkRichTextComposition should be used for RichContent so TextRange is exposed for the application
            // to track the composition range.
            // FrameworkTextComposition should be used for non-RichContent and TextRange is not exposed.
            if (editor.AcceptsRichContent)
            {
                composition = new FrameworkRichTextComposition(InputManager.UnsecureCurrent, editor.UiScope, owner);
            }
            else
            {
                composition = new FrameworkTextComposition(InputManager.Current, editor.UiScope, owner);
            }

            return composition;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal UIElement RenderScope
        {
            get
            {
                 if (this.TextEditor == null)
                     return null;

                 if (this.TextEditor.TextView == null)
                     return null;

                 return this.TextEditor.TextView.RenderScope;
            }
        }

        internal FrameworkElement UiScope
        {
            get { return this.TextEditor.UiScope; }
        }

        internal ITextContainer TextContainer
        {
            get { return this.TextEditor.TextContainer; }
        }

        internal ITextView TextView
        {
            get { return TextEditor.TextView; }
        }

        // The pointer to ITfDocumentMgr.
        internal UnsafeNativeMethods.ITfDocumentMgr DocumentManager
        {
            get
            {
                if (_documentmanager == null)
                {
                    return null;
                }

                return _documentmanager.Value;
            }

            set { _documentmanager = new SecurityCriticalDataClass<UnsafeNativeMethods.ITfDocumentMgr>(value); }
        }

        // Cookie for ITfThreadFocusSink.
        internal int ThreadFocusCookie
        {
            get { return _threadFocusCookie; }
            set { _threadFocusCookie = value; }
        }

        // Cookie for ITfTextEditSink.
        internal int EditSinkCookie
        {
            get { return _editSinkCookie; }
            set { _editSinkCookie = value; }
        }

        // Cookie for ITfContext.
        internal int EditCookie
        {
            get { return _editCookie; }
            set { _editCookie = value; }
        }

        // True if the current selection is for interim character.
        internal bool IsInterimSelection
        {
            get { return _interimSelection; }
        }

        // true if we're in the middle of an ongoing composition.
        internal bool IsComposing
        {
            get { return _isComposing; }
        }

        // true if we're in the middle of an ongoing composition,
        // with exception described in RaiseCompositionEvents.
        internal bool IsEffectivelyComposing
        {
            get { return _isComposing && !_isEffectivelyNotComposing; }
        }

        internal int TransitoryExtensionSinkCookie
        {
            get { return _transitoryExtensionSinkCookie; }
            set { _transitoryExtensionSinkCookie = value; }
        }

        internal IntPtr CriticalSourceWnd
        {
            get
            {
                bool callerIsTrusted = true;
                return( GetSourceWnd(callerIsTrusted) );
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Tree change listener.  We need to forward any tree change events
        // to TSF.  But we must never forward any events that occur while
        // TSF holds a document lock.
        private void OnTextContainerChange(object sender, TextContainerChangeEventArgs args)
        {
            if (args.IMECharCount > 0 && (args.TextChange == TextChangeType.ContentAdded || args.TextChange == TextChangeType.ContentRemoved))
            {
                _compositionModifiedByEventListener = true;
            }

            if (_compositionEventState == CompositionEventState.RaisingEvents)
            {
                return;
            }

            Invariant.Assert(sender == this.TextContainer);

#if ENABLE_INK_EMBEDDING
            // Record the offset of the first symbol in the document
            // affected by this edit.
            // Used by RemoveContent to track the effects of an edit.
            if (args.TextChange == TextChangeType.ContentRemoved &&
                _minSymbolsRemovedIndex > args.TextPosition.Offset)
            {
                _minSymbolsRemovedIndex = args.TextPosition.Offset;
            }
#endif

            // Don't send TSF events that it initiated (ie, while it holds a lock).
            if (_lockFlags == 0 && HasSink)
            {
                int charsAdded = 0;
                int charsRemoved = 0;

                if (args.TextChange == TextChangeType.ContentAdded)
                {
                    charsAdded = args.IMECharCount;
                }
                else if (args.TextChange == TextChangeType.ContentRemoved)
                {
                    charsRemoved = args.IMECharCount;
                }
                else
                {
                    // This is a TextChange.ContentAffected change, which we
                    // don't want to pass on to cicero.  Cicero doesn't care
                    // about DependencyProperty values, and we don't want it
                    // to invalidate cicero properties unless symbols were
                    // added or removed.
                }

                if (charsAdded > 0 || charsRemoved > 0)
                {
                    UnsafeNativeMethods.TS_TEXTCHANGE change;

                    change.start = args.ITextPosition.CharOffset;
                    change.oldEnd = change.start + charsRemoved;
                    change.newEnd = change.start + charsAdded;

                    ValidateChange(change);
                    // We can't call VerifyTextStoreConsistency() yet because more changes may be pending.

                    // Eventually we may want to support TS_TC_CORRECTION flag.
                    // This would let IMEs know not to throw out metadata on autocorrects.  It's optional.
                    try
                    {
                        _textChangeReentrencyCount++;
                        _sink.OnTextChange(0 /* flags */, ref change);
                    }
                    finally
                    {
                        _textChangeReentrencyCount--;
                    }
                }
            }
            else if (_isInUpdateLayout)
            {
                _hasTextChangedInUpdateLayout = true;
            }
        }

        // DispatcherOperationCallback callback.  Async lock requests are dequeued to
        // this callback, which grants the pending lock.
        private object GrantLockHandler(object o)
        {
            // _textservicesHost or _sink may have been released (set null) if cicero already shut down
            // before we got this callback.  In which case, there's no one
            // to talk to.
            if ((_textservicesHost != null) && (HasSink))
            {
                GrantLockWorker(_pendingAsyncLockFlags);
            }
            _pendingAsyncLockFlags = 0;
            return null;
        }

        private bool HasSink
        {
            get { return _sink != null; }
        }

        // Makes an OnLockGranted callback to cicero.
        private int GrantLockWorker(UnsafeNativeMethods.LockFlags flags)
        {
            int hrSession;

            TextEditor textEditor = this.TextEditor;

            if (textEditor == null)
            {
                // The app shutdown before we got an async callback.
                hrSession = NativeMethods.E_FAIL;
            }
            else
            {
                _lockFlags = flags;
                _hasTextChangedInUpdateLayout = false;
                UndoManager undoManager = UndoManager.GetUndoManager(textEditor.TextContainer.Parent);
                int initialUndoCount = 0;
                bool wasImeSupportModeEnabled = false;

                // undoManager will be null for readonly documents like FlowDocumentReader.
                // why expose readonly documents to IMEs?
                if (undoManager != null)
                {
                    initialUndoCount = undoManager.UndoCount;
                    wasImeSupportModeEnabled = undoManager.IsImeSupportModeEnabled;
                    undoManager.IsImeSupportModeEnabled = true;
                }

                // Reset the composition offsets.  Sometimes an IME will
                // allow the editor handle a keystroke during an active composition.
                // See bug 118934.  When this happens, we need to update the composition
                // here.  Where the IME holds a lock, no one else can modify
                // the text, and int offsets allow us to use the undo stack internally.
                _previousCompositionStartOffset = (_previousCompositionStart == null) ? -1 : _previousCompositionStart.Offset;
                _previousCompositionEndOffset = (_previousCompositionEnd == null) ? -1 : _previousCompositionEnd.Offset;

                try
                {
                    textEditor.Selection.BeginChangeNoUndo();
                    try
                    {
                        hrSession = GrantLock();
                        if (_pendingWriteReq)
                        {
                            _lockFlags = UnsafeNativeMethods.LockFlags.TS_LF_READWRITE;
                            GrantLock();
                        }
                    }
                    finally
                    {
                        _pendingWriteReq = false;
                        _lockFlags = 0;

                        // Set a flag to ignore the first selection change event during
                        // this change block -- we must not report any changes made to
                        // the selection by the IME that just released the cicero lock.
                        _ignoreNextSelectionChange = textEditor.Selection._IsChanged;
                        try
                        {
                            // Skip the public events for the changing of the composition
                            // by Cicero, but the below HandleCompositionEvents will raise
                            // the public events about the composition and text change.
                            textEditor.Selection.EndChange(false /* disableScroll */, true /* skipEvents */);
                        }
                        finally
                        {
                            // Note we also clear the flag in our OnSelectionChanged listener,
                            // but we have to clear it here in case the change block we just
                            // closed wasn't the outermost change block.
                            _ignoreNextSelectionChange = false;

                            // The next call to HandleCompositionEvents involves firing events
                            // that could result in a reentrancy. By initializing these TextPointers
                            // we are being prepared for such an eventuality. 
                            _previousCompositionStart = (_previousCompositionStartOffset == -1) ? null : textEditor.TextContainer.CreatePointerAtOffset(_previousCompositionStartOffset, LogicalDirection.Backward);
                            _previousCompositionEnd = (_previousCompositionEndOffset == -1) ? null : textEditor.TextContainer.CreatePointerAtOffset(_previousCompositionEndOffset, LogicalDirection.Forward);
                        }
                    }

                    if (undoManager != null)
                    {
                        // Finally raise the recorded composition events publicly.
                        HandleCompositionEvents(initialUndoCount);
                    }
                }
                finally
                {
                    if (undoManager != null)
                    {
                        undoManager.IsImeSupportModeEnabled = wasImeSupportModeEnabled;
                    }

                    // The TextContainer will have changed when playing back the recorded events and thus we need to refresh the TextPointers.
                    _previousCompositionStart = (_previousCompositionStartOffset == -1) ? null : textEditor.TextContainer.CreatePointerAtOffset(_previousCompositionStartOffset, LogicalDirection.Backward);
                    _previousCompositionEnd = (_previousCompositionEndOffset == -1) ? null : textEditor.TextContainer.CreatePointerAtOffset(_previousCompositionEndOffset, LogicalDirection.Forward);
                }
            }

            return hrSession;
        }

        // Grant cicero a lock, and do any house keeping around it.
        // Note cicero won't get tree change events from within the scope of this method.
        private int GrantLock()
        {
            int hr;

            // GrantLock should be called from only RequestLock. So it must be in the dispatcher thread.
            Invariant.Assert(Thread.CurrentThread == _textservicesHost.Dispatcher.Thread, "GrantLock called on bad thread!");

            VerifyTextStoreConsistency();

            hr = _sink.OnLockGranted(_lockFlags);

            VerifyTextStoreConsistency();

            return hr;
        }

        // GetText handler for text runs.
        private static bool WalkTextRun(ITextPointer navigator, ITextPointer limit, char[] text, int cchReq, ref int charsCopied, UnsafeNativeMethods.TS_RUNINFO[] runInfo, int cRunInfoReq, ref int cRunInfoRcv)
        {
            int runCount;
            int offset;
            bool hitLimit;

            Invariant.Assert(navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text);
            Invariant.Assert(limit == null || navigator.CompareTo(limit) <= 0);

            hitLimit = false;

            if (cchReq > 0)
            {
                runCount = TextPointerBase.GetTextWithLimit(navigator, LogicalDirection.Forward, text, charsCopied, Math.Min(cchReq, text.Length - charsCopied), limit);
                navigator.MoveByOffset(runCount);
                charsCopied += runCount;
                hitLimit = (text.Length == charsCopied) || (limit != null && navigator.CompareTo(limit) == 0);
            }
            else
            {
                // Caller doesn't want text, just run info.
                // Advance the navigator.
                runCount = navigator.GetTextRunLength(LogicalDirection.Forward);
                navigator.MoveToNextContextPosition(LogicalDirection.Forward);

                // If the caller passed in a non-null limit, backup to the limit if
                // we've passed it.
                if (limit != null)
                {
                    if (navigator.CompareTo(limit) >= 0)
                    {
                        offset = limit.GetOffsetToPosition(navigator);
                        Invariant.Assert(offset >= 0 && offset <= runCount, "Bogus offset -- extends past run!");
                        runCount -= offset;
                        navigator.MoveToPosition(limit);
                        hitLimit = true;
                    }
                }
            }

            if (cRunInfoReq > 0 && runCount > 0)
            {
                // Be sure to merge this text run with the previous run, if they are both text runs.
                // (A good robustness fix would be to make cicero handle this, if we ever get the chance.)
                if (cRunInfoRcv > 0 && runInfo[cRunInfoRcv - 1].type == UnsafeNativeMethods.TsRunType.TS_RT_PLAIN)
                {
                    runInfo[cRunInfoRcv - 1].count += runCount;
                }
                else
                {
                    runInfo[cRunInfoRcv].count = runCount;
                    runInfo[cRunInfoRcv].type = UnsafeNativeMethods.TsRunType.TS_RT_PLAIN;
                    cRunInfoRcv++;
                }
            }

            return hitLimit;
        }


        // GetText handler for object runs.
        private static bool WalkObjectRun(ITextPointer navigator, ITextPointer limit, char[] text, int cchReq, ref int charsCopied, UnsafeNativeMethods.TS_RUNINFO[] runInfo, int cRunInfoReq, ref int cRunInfoRcv)
        {
            bool hitLimit;

            Invariant.Assert(navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.EmbeddedElement);
            Invariant.Assert(limit == null || navigator.CompareTo(limit) <= 0);

            if (limit != null && navigator.CompareTo(limit) == 0)
            {
                return true;
            }

            hitLimit = false;

            navigator.MoveToNextContextPosition(LogicalDirection.Forward);

            if (cchReq >= 1)
            {
                text[charsCopied] = UnsafeNativeMethods.TS_CHAR_EMBEDDED;
                charsCopied++;
            }

            if (cRunInfoReq > 0)
            {
                // Be sure to merge this text run with the previous run, if they are both text runs.
                // (A good robustness fix would be to make cicero handle this, if we ever get the chance.)
                if (cRunInfoRcv > 0 && runInfo[cRunInfoRcv - 1].type == UnsafeNativeMethods.TsRunType.TS_RT_PLAIN)
                {
                    runInfo[cRunInfoRcv - 1].count++;
                }
                else
                {
                    runInfo[cRunInfoRcv].count = 1;
                    runInfo[cRunInfoRcv].type = UnsafeNativeMethods.TsRunType.TS_RT_PLAIN;
                    cRunInfoRcv++;
                }
            }

            return hitLimit;
        }

        // GetText handler for Blocks and TableCell to add '\n' or TS_CHAR_REGION.
        private static bool WalkRegionBoundary(ITextPointer navigator, ITextPointer limit, char[] text, int cchReq, ref int charsCopied, UnsafeNativeMethods.TS_RUNINFO[] runInfo, int cRunInfoReq, ref int cRunInfoRcv)
        {
            bool hitLimit;

            Invariant.Assert(navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart || navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd);
            Invariant.Assert(limit == null || navigator.CompareTo(limit) <= 0);

            // If the caller passed in a non-null limit, we don't do anything and just return true.
            // we've passed it.
            if (limit != null)
            {
                if (navigator.CompareTo(limit) >= 0)
                {
                    return true;
                }
            }

            hitLimit = false;

            if (cchReq > 0)
            {
                // Add one TS_CHAR_REGION (TableCell) or '\n' (everything else) char.
                char ch = (navigator.GetAdjacentElement(LogicalDirection.Forward) is TableCell) ? UnsafeNativeMethods.TS_CHAR_REGION : '\n';
                text[charsCopied] = ch;
                navigator.MoveByOffset(1);
                charsCopied += 1;
                hitLimit = (text.Length == charsCopied) || (limit != null && navigator.CompareTo(limit) == 0);
            }
            else
            {
                // Caller doesn't want text, just run info.
                // Advance the navigator.
                // Add one TS_CHAR_REGION char.
                navigator.MoveByOffset(1);
            }

            if (cRunInfoReq > 0)
            {
                // Be sure to merge this text run with the previous run, if they are both text runs.
                // (A good robustness fix would be to make cicero handle this, if we ever get the chance.)
                if (cRunInfoRcv > 0 && runInfo[cRunInfoRcv - 1].type == UnsafeNativeMethods.TsRunType.TS_RT_PLAIN)
                {
                    runInfo[cRunInfoRcv - 1].count += 1;
                }
                else
                {
                    runInfo[cRunInfoRcv].count = 1;
                    runInfo[cRunInfoRcv].type = UnsafeNativeMethods.TsRunType.TS_RT_PLAIN;
                    cRunInfoRcv++;
                }
            }

            return hitLimit;
        }

        // Returns objects useful for talking to the underlying HWND.
        // Throws TS_E_NOLAYOUT if they are not available.
        private void GetVisualInfo(out PresentationSource source, out IWin32Window win32Window, out ITextView view)
        {
            source = PresentationSource.CriticalFromVisual(RenderScope);
            win32Window = source as IWin32Window;

            if (win32Window == null)
            {
                throw new COMException(SR.Get(SRID.TextStore_TS_E_NOLAYOUT), UnsafeNativeMethods.TS_E_NOLAYOUT);
            }

            view = this.TextView;
        }

        // Transforms mil measure unit points to screen pixels.
        private static UnsafeNativeMethods.RECT TransformRootRectToScreenCoordinates(Point milPointTopLeft, Point milPointBottomRight, IWin32Window win32Window, PresentationSource source)
        {
            UnsafeNativeMethods.RECT rect;
            NativeMethods.POINT clientPoint;
            CompositionTarget compositionTarget;

            rect = new UnsafeNativeMethods.RECT();

            // Transform to device units.
            compositionTarget = source.CompositionTarget;
            milPointTopLeft = compositionTarget.TransformToDevice.Transform(milPointTopLeft);
            milPointBottomRight = compositionTarget.TransformToDevice.Transform(milPointBottomRight);

            IntPtr hwnd = IntPtr.Zero;
            hwnd = win32Window.Handle;

            // Transform to screen coords.
            clientPoint = new NativeMethods.POINT();
            UnsafeNativeMethods.ClientToScreen(new HandleRef(null, hwnd), /* ref by interop */ clientPoint);

            rect.left = (int)(clientPoint.x + milPointTopLeft.X);
            rect.right = (int)(clientPoint.x + milPointBottomRight.X);
            rect.top = (int)(clientPoint.y + milPointTopLeft.Y);
            rect.bottom = (int)(clientPoint.y + milPointBottomRight.Y);
            return rect;
        }

#if ENABLE_INK_EMBEDDING
        // Insert InkInteropObject at the position.
        private void InsertEmbeddedAtPosition(TextPointer position, IComDataObject data, out UnsafeNativeMethods.TS_TEXTCHANGE change)
        {

            ITextContainer container;
            // Get enhanced metafile handle from IOleDataObject.
            FORMATETC formatetc = new FORMATETC();
            STGMEDIUM stgmedium = new STGMEDIUM();
            formatetc.cfFormat = NativeMethods.CF_ENHMETAFILE;
            formatetc.ptd = IntPtr.Zero;
            formatetc.dwAspect = DVASPECT.DVASPECT_CONTENT;
            formatetc.lindex = -1;
            formatetc.tymed = TYMED.TYMED_ENHMF;
            stgmedium.tymed = TYMED.TYMED_ENHMF;

            data.GetData(ref formatetc, out stgmedium);

            if (stgmedium.unionmember == IntPtr.Zero)
            {
                throw new COMException(SR.Get(SRID.TextStore_BadObjectData), NativeMethods.E_INVALIDARG);
            }

            IntPtr hbitmap = SystemDrawingHelper.ConvertMetafileToHBitmap(stgmedium.unionmember);

            // create a InkInteropObject framework element.
            InkInteropObject inkobject = new InkInteropObject(data);

            inkobject.Source = Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero, Int32Rect.Empty, null);

            position = InsertInkAtPosition(position, inkobject, out change);

            // Move the selection.
            container = this.TextContainer;
            TextSelection.SetCaretToPosition(position, LogicalDirection.Backward, /*allowStopAtLineEnd:*/false, /*allowStopNearSpace:*/false);
        }

        // Inserts an InkInteropObject at a specified position.
        private TextPointer InsertInkAtPosition(TextPointer insertionPosition, InkInteropObject inkobject, out UnsafeNativeMethods.TS_TEXTCHANGE change)
        {
            int symbolsAddedBefore = 0;
            int symbolsAddedAfter = 0;

            // Prepare an insertion position for InlineUIContainer.
            // As an optimization, shift outside of any formatting tags to avoid
            // splitting tags below.
            while (insertionPosition.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &&
                TextSchema.IsFormattingType(insertionPosition.Parent.GetType()))
            {
                insertionPosition = insertionPosition.GetNextContextPosition(LogicalDirection.Backward);
            }
            while (insertionPosition.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd &&
                TextSchema.IsFormattingType(insertionPosition.Parent.GetType()))
            {
                insertionPosition = insertionPosition.GetNextContextPosition(LogicalDirection.Forward);
            }

            // If we need to, split the current parent TextElement and prepare
            // a suitable home for an InlineUIContainer.
            if (!TextSchema.IsValidParent(insertionPosition.Parent.GetType(), typeof(InlineUIContainer)))
            {
                insertionPosition = TextRangeEditTables.EnsureInsertionPosition(insertionPosition, out symbolsAddedBefore, out symbolsAddedAfter);
                Invariant.Assert(insertionPosition.Parent is Run, "position must be in Run scope");

                insertionPosition = TextRangeEdit.SplitElement(insertionPosition);
                // We need to remember how many symbols were added into addition
                // to the InlineUIContainer itself.
                // Account for the two element edges just added.
                symbolsAddedBefore += 1;
                symbolsAddedAfter += 1;
            }

            // Create an InlineUIContainer.
            InlineUIContainer inlineUIContainer = new InlineUIContainer(inkobject);

            change.start = ((ITextPointer)insertionPosition).Offset - symbolsAddedBefore;
            change.oldEnd = change.start;

            // Insert it into the insertionPosition.  This adds 3 symbols.
            insertionPosition.InsertTextElement(inlineUIContainer);

            change.newEnd = change.start + symbolsAddedBefore + inlineUIContainer.SymbolCount + symbolsAddedAfter;

            // Return a position after the inserted object.
            return inlineUIContainer.ElementEnd.GetInsertionPosition(LogicalDirection.Forward);
        }
#endif // ENABLE_INK_EMBEDDING

        // determine a family name from a FontFamily and XmlLanguage
        private static string GetFontFamilyName(FontFamily fontFamily, XmlLanguage language)
        {
            if (fontFamily == null)
                return null;

            // If the font family was constructed from a font name or URI, return that value.
            if (fontFamily.Source != null)
                return fontFamily.Source;

            // Use the dictionary of names provided by the font.
            LanguageSpecificStringDictionary names = fontFamily.FamilyNames;
            if (names == null)
                return null;

            // try every matching language to most-specific to least specific, including ""
            foreach (XmlLanguage matchingLanguage in language.MatchingLanguages)
            {
                string name = names[matchingLanguage];
                if (name != null)
                    return name;
            }

            // give up!
            return null;
        }

        // Prepare the app property values and store them into _preparedatribute.
        private void PrepareAttributes(InputScope inputScope, double fontSize, FontFamily fontFamily, XmlLanguage language, Visual visual, int count, Guid[] filterAttributes)
        {
            if (_preparedattributes == null)
            {
                _preparedattributes = new ArrayList(count);
            }
            else
            {
                _preparedattributes.Clear();
            }

            int i;
            for (i = 0; i < _supportingattributes.Length; i++)
            {
                if (count != 0)
                {
                    int j;
                    bool found = false;
                    for (j = 0; j < count; j++)
                    {
                        if (_supportingattributes[i].Guid.Equals(filterAttributes[j]))
                            found = true;
                    }

                    if (!found)
                        continue;
                }

                UnsafeNativeMethods.TS_ATTRVAL attrval = new UnsafeNativeMethods.TS_ATTRVAL();
                attrval.attributeId = _supportingattributes[i].Guid;
                attrval.overlappedId = (int)_supportingattributes[i].Style;
                attrval.val = new NativeMethods.VARIANT();

                // This VARIANT is returned to the caller, which supposed to call VariantClear().
                // GC does not have to clear it.
                attrval.val.SuppressFinalize();

                switch (_supportingattributes[i].Style)
                {
                    case AttributeStyle.InputScope:
                        object obj = new InputScopeAttribute(inputScope);
                        attrval.val.vt = (short)NativeMethods.tagVT.VT_UNKNOWN;
                        attrval.val.data1.Value = Marshal.GetIUnknownForObject(obj);
                        break;

                    case AttributeStyle.Font_Style_Height:
                        // We always evaluate the font size and returns a value.
                        attrval.val.vt = (short)NativeMethods.tagVT.VT_I4;
                        attrval.val.data1.Value = (IntPtr)(int)fontSize;
                        break;

                    case AttributeStyle.Font_FaceName:
                        {
                            string familyName = GetFontFamilyName(fontFamily, language);
                            if (familyName != null)
                            {
                                attrval.val.vt = (short)NativeMethods.tagVT.VT_BSTR;
                                attrval.val.data1.Value = Marshal.StringToBSTR(familyName);
                            }
                        }
                        break;

                    case AttributeStyle.Font_SizePts:
                        attrval.val.vt = (short)NativeMethods.tagVT.VT_I4;
                        attrval.val.data1.Value = (IntPtr)(int)(fontSize / 96.0 * 72.0);
                        break;

                    case AttributeStyle.Text_ReadOnly:
                        attrval.val.vt = (short)NativeMethods.tagVT.VT_BOOL;
                        attrval.val.data1.Value = IsReadOnly ? (IntPtr)1 : (IntPtr)0;
                        break;

                    case AttributeStyle.Text_Orientation:
                        attrval.val.vt = (short)NativeMethods.tagVT.VT_I4;
                        attrval.val.data1.Value = (IntPtr)0;

                        // Get the transformation that is relative from source.
                        PresentationSource source = null;

                        source = PresentationSource.CriticalFromVisual((Visual)RenderScope);
                        if (source != null)
                        {
                            Visual root = source.RootVisual;
                            if ((root !=  null) && (visual != null))
                            {
                                //
                                // Calc radian from Matirix. This is approximate calculation from the first row.
                                // If tf.M12 is 0, angle will be 0. So we don't have to calc it.
                                //
                                GeneralTransform transform = visual.TransformToAncestor(root);
                                Transform t = transform.AffineTransform;
                                // REVIEW: if the transformation is not affine, this is not going to work
                                if (t != null)
                                {
                                    Matrix tf = t.Value;
                                    if ((tf.M11 != 0) || (tf.M12 != 0))
                                    {
                                        double radSin = Math.Asin(tf.M12 / Math.Sqrt((tf.M11 * tf.M11) + (tf.M12 * tf.M12)));
                                        double radCos = Math.Acos(tf.M11 / Math.Sqrt((tf.M11 * tf.M11) + (tf.M12 * tf.M12)));
                                        // double angleSin = Math.Round((radSin * 180) / Math.PI, 0);
                                        double angleCos = Math.Round((radCos * 180) / Math.PI, 0);
                                        double angle;

                                        // determine angle from the sign of radSin;
                                        if (radSin <= 0)
                                            angle = angleCos;
                                        else
                                            angle = 360 - angleCos;

                                        attrval.val.data1.Value = (IntPtr)((int)angle * 10);
                                    }
                                }
                            }
                        }
                        break;

                    case AttributeStyle.Text_VerticalWriting:
                        //
                        //     the vertical writing is not supported yet
                        //
                        attrval.val.vt = (short)NativeMethods.tagVT.VT_BOOL;
                        attrval.val.data1.Value = (IntPtr)0;
                        break;
                }

                _preparedattributes.Add(attrval);
            }
        }

        // retrieve the TextPositions from ITfRange.
        private void TextPositionsFromITfRange(UnsafeNativeMethods.ITfRange range, out ITextPointer start, out ITextPointer end)
        {
            UnsafeNativeMethods.ITfRangeACP rangeACP;
            int startIndex;
            int length;

            rangeACP = range as UnsafeNativeMethods.ITfRangeACP;
            rangeACP.GetExtent(out startIndex, out length);

            start = CreatePointerAtCharOffset(startIndex, LogicalDirection.Backward);
            end = CreatePointerAtCharOffset(startIndex + length, LogicalDirection.Forward);

            while (start.CompareTo(end) < 0 && start.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.Text)
            {
                start.MoveToNextContextPosition(LogicalDirection.Forward);
            }
        }

        // Returns the start and end positions of the current composition, or
        // null if there is no current composition.
        private void GetCompositionPositions(out ITextPointer start, out ITextPointer end)
        {
            start = null;
            end = null;

            if (_isComposing)
            {
                UnsafeNativeMethods.ITfCompositionView view = FrameworkTextComposition.GetCurrentCompositionView(this.DocumentManager);

                if (view != null)
                {
                    GetCompositionPositions(view, out start, out end);
                }
            }
        }

        private void GetCompositionPositions(UnsafeNativeMethods.ITfCompositionView view, out ITextPointer start, out ITextPointer end)
        {
            UnsafeNativeMethods.ITfRange range;
            view.GetRange(out range);

            TextPositionsFromITfRange(range, out start, out end);

            Marshal.ReleaseComObject(range);
            Marshal.ReleaseComObject(view);
        }

        // get the text from ITfRange.
        private static string StringFromITfRange(UnsafeNativeMethods.ITfRange range, int ecReadOnly)
        {
            // Transitory Document uses ther TextStore, which is ACP base.
            UnsafeNativeMethods.ITfRangeACP rangeacp = (UnsafeNativeMethods.ITfRangeACP)range;
            int start;
            int count;
            int countRet;
            rangeacp.GetExtent(out start, out count);
            char[] text = new char[count];
            rangeacp.GetText(ecReadOnly, 0, text, count, out countRet);
            return new string(text);
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
               btnState |= NativeMethods.MK_LBUTTON;
            }
            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
               btnState |= NativeMethods.MK_RBUTTON;
            }
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
               btnState |= NativeMethods.MK_SHIFT;
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
               btnState |= NativeMethods.MK_CONTROL;
            }

            Point point = Mouse.GetPosition(RenderScope);
            ITextView view;
            ITextPointer positionCurrent;
            ITextPointer positionNext;
            Rect rectCurrent;
            Rect rectNext;

            view = TextView;
            // Check if view is available.
            if (view == null)
            {
                return false;
            }

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
            edge = positionCurrent.CharOffset;

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

            int i;
            bool eaten = false;
            for (i = 0; (i < _mouseSinks.Count) && (eaten == false); i++)
            {
                MouseSink mSink = (MouseSink)_mouseSinks[i];

                //
                // TIPs care about only the range.
                // If the quadrant is outside of the range, we don't do SendMessage.
                //

                int start;
                int count;
                mSink.Range.GetExtent(out start, out count);

                if (edge < start)
                   continue;

                if (edge > start + count)
                   continue;

                if ((edge == start) && (quadrant <= 1))
                   continue;

                if ((edge == start + count) && (quadrant >= 2))
                   continue;

                mSink.Locked = true;
                try
                {
                    mSink.Sink.OnMouseEvent(edge - start, quadrant, btnState, out eaten);
                }
                finally
                {
                    mSink.Locked = false;
                }
            }

            return eaten;
        }

        /// <summary>
        /// This overload assumes that at the time of opening new
        /// CompositionParentUndoUnit the composition is still active.
        /// </summary>
        /// <returns></returns>
        private CompositionParentUndoUnit OpenCompositionUndoUnit()
        {
            return OpenCompositionUndoUnit(null, null);
        }

        // Opens the composition undo unit if it exists on the top
        // of the stack. Otherwise, create a new composition undo unit
        // and add it to the undo stack.
        private CompositionParentUndoUnit OpenCompositionUndoUnit(ITextPointer compositionStart, ITextPointer compositionEnd)
        {
            UndoManager undoManager = UndoManager.GetUndoManager(this.TextContainer.Parent);

            if (undoManager == null || !undoManager.IsEnabled)
            {
                return null;
            }

            // The start position is where we'll put the caret if this composition is later
            // undone by a user.
            //
            // At this point some IMEs will not have updated the selection to a
            // position within the composition, suggesting that we always want to
            // use selection start.  However, some IMEs will expand the composition backward on input
            // so the composition covers unmodified text.  (E.g.: chinese prc pinyin IME
            // will expand to cover previously finalized text on <space> input.)
            //
            // So we use a hueristic: take the rightmost of the selection start or composition
            // start.
            ITextPointer start;

            if (compositionStart == null)
            {
                Invariant.Assert(compositionEnd == null);

                GetCompositionPositions(out compositionStart, out compositionEnd);
            }

            if (compositionStart != null && compositionStart.CompareTo(this.TextSelection.Start) > 0)
            {
                start = compositionStart;
            }
            else
            {
                start = this.TextSelection.Start;
            }

            CompositionParentUndoUnit unit = new CompositionParentUndoUnit(this.TextSelection, start, start, _nextUndoUnitIsFirstCompositionUnit);
            _nextUndoUnitIsFirstCompositionUnit = false;

            // Add the given composition undo unit to the undo manager and making it
            // as the opened undo unit.
            undoManager.Open(unit);

            return unit;
        }

        /// <summary>
        /// Computes the bounds for a given text segment, provided that the entire segment
        /// is located on a single text line.
        /// </summary>
        private static Rect GetLineBounds(ITextPointer start, ITextPointer end)
        {
            // Get the line range.
            if (!start.HasValidLayout || !end.HasValidLayout)
            {
                return Rect.Empty;
            }

            // Get the left and the width of the range bounds.
            Rect lineBounds = start.GetCharacterRect(LogicalDirection.Forward);
            lineBounds.Union(end.GetCharacterRect(LogicalDirection.Backward));

            // Scan the line range and compute the top and the height of the bounding rectangle.
            ITextPointer navigator = start.CreatePointer(LogicalDirection.Forward);
            while (navigator.MoveToNextContextPosition(LogicalDirection.Forward) == true && navigator.CompareTo(end) < 0)
            {
                TextPointerContext context = navigator.GetPointerContext(LogicalDirection.Backward);
                switch (context)
                {
                    case TextPointerContext.ElementStart:
                        lineBounds.Union(navigator.GetCharacterRect(LogicalDirection.Backward));
                        navigator.MoveToElementEdge(ElementEdge.AfterEnd);
                        break;
                    case TextPointerContext.ElementEnd:
                    case TextPointerContext.EmbeddedElement:
                        lineBounds.Union(navigator.GetCharacterRect(LogicalDirection.Backward));
                        break;
                    case TextPointerContext.Text:
                        break;
                    default:
                        // Unexpected
                        Invariant.Assert(context != TextPointerContext.None);
                        break;
                }
            }

            return lineBounds;
        }

#if ENABLE_INK_EMBEDDING
        // Inserts an embedded object into the document, replacing a range of text.
        private void InsertEmbeddedAtRange(TextPointer startPosition, TextPointer endPosition, IComDataObject data, out UnsafeNativeMethods.TS_TEXTCHANGE change)
        {
            int symbolsRemoved;
            int removeStartIndex;
            int startIndex;

            // Remove the existing range content.
            // See the comments on RemoveContent for an explanation of the
            // out params.
            startIndex = startPosition.Offset;
            RemoveContent(startPosition, endPosition, out symbolsRemoved, out removeStartIndex);
            Invariant.Assert(startIndex >= removeStartIndex);

            // Remember where we're actually going to do the insert.
            startIndex = startPosition.Offset;
            Invariant.Assert(startIndex >= removeStartIndex);

            // Do the insert.
            // The TS_TEXTCHANGE reflects on the insert, we have to update it
            // for any content we may have removed above.
            InsertEmbeddedAtPosition(startPosition, data, out change);

            // Update change for the remove content step above.
            change.start = removeStartIndex;
            change.oldEnd += symbolsRemoved;
        }

        // Deletes a specified run of content.
        //
        // On exit,
        //   symbolsRemoved <== count of symbols actually removed.
        //   removeStartIndex <== offset of first symbol affected by the edit.
        //
        // removeStartIndex is always <= endPosition.Offset, but it does not necessarily
        // match the position of the logically removed content.  In some rare cases
        // a scoping element may be removed, meaning we have two or more runs of
        // removed content, and removeStartIndex + symbolsRemoved < the offset of
        // the last position affected by the operation.
        private void RemoveContent(ITextPointer startPosition, ITextPointer endPosition, out int symbolsRemoved, out int removeStartIndex)
        {
            symbolsRemoved = 0;
            removeStartIndex = startPosition.Offset;

            if (startPosition.CompareTo(endPosition) == 0)
                return;

            TextContainer container = (TextContainer)startPosition.TextContainer;

            symbolsRemoved = container.SymbolCount;

            if (startPosition is TextPointer)
            {
                _minSymbolsRemovedIndex = Int32.MaxValue;
            }

            startPosition.DeleteContentToPosition(endPosition);

            if (startPosition is TextPointer)
            {
                removeStartIndex = _minSymbolsRemovedIndex;
            }

            symbolsRemoved = symbolsRemoved - container.SymbolCount;
        }
#endif // ENABLE_INK_EMBEDDING

        // Filter the composition string during IME edits to the document. This method does NOT
        // filter MaxLength.
        private string FilterCompositionString(string text, int charsToReplaceCount)
        {
            string newText = this.TextEditor._FilterText(text, charsToReplaceCount, /*filterMaxLength:*/false);

            // if the length has been changed, there is no way to recover and we finalize the composition string.
            if (newText.Length != text.Length)
            {
                CompleteCompositionAsync();
                return null;
            }

            return newText;
        }

        // Handler to complete the composition string.
        private object CompleteCompositionHandler(object o)
        {
            CompleteComposition();
            return null;
        }

        private IntPtr GetSourceWnd(bool callerIsTrusted)
        {
            IntPtr hwnd = IntPtr.Zero;
            if (RenderScope != null)
            {
                IWin32Window win32Window;

                if (callerIsTrusted)
                {
                    win32Window = PresentationSource.CriticalFromVisual(RenderScope) as IWin32Window;
                }
                else
                {
                    win32Window = PresentationSource.FromVisual(RenderScope) as IWin32Window;
                }

                if (win32Window != null)
                {
                    hwnd = win32Window.Handle;
                }
            }
            return hwnd;
        }

        // Detects errors in the change notifications we send TSF.
        private void ValidateChange(UnsafeNativeMethods.TS_TEXTCHANGE change)
        {
            Invariant.Assert(change.start >= 0, "Bad StartIndex");
            Invariant.Assert(change.start <= change.oldEnd, "Bad oldEnd index");
            Invariant.Assert(change.start <= change.newEnd, "Bad newEnd index");

            _netCharCount += (change.newEnd - change.oldEnd);
            Invariant.Assert(_netCharCount >= 0, "Negative _netCharCount!");
        }

        // Asserts that this TextStore is sending TS_TEXTCHANGE structs
        // in sync with the actual TextContainer.
        private void VerifyTextStoreConsistency()
        {
            if (_netCharCount != this.TextContainer.IMECharCount)
            {
                Invariant.Assert(false, "TextContainer/TextStore have inconsistent char counts!");
            }
        }

        // Validates the character offset supplied by cicero.
        // See bug 1395082.  Sometimes cicero gives us offsets that are
        // too large for the document.
        private void ValidateCharOffset(int offset)
        {
            if (offset < 0 || offset > this.TextContainer.IMECharCount)
            {
                throw new ArgumentException(SR.Get(SRID.TextStore_BadIMECharOffset, offset, this.TextContainer.IMECharCount));
            }
        }

        /// Discards previous composition undo unit, to prevent
        /// from merging it with the subsequent typing.
        private void BreakTypingSequence(ITextPointer caretPosition)
        {
            CompositionParentUndoUnit unit = PeekCompositionParentUndoUnit();

            if (unit != null)
            {
                // We also put the caret at the end of the composition after
                // redoing a composition undo.  So update the end position now.
                unit.RecordRedoSelectionState(caretPosition, caretPosition);
            }
        }

        // Repositions an ITextRange to comply with limitations on IME input.
        // We cannot modify Table structure, or insert content
        // before or after Tables or BlockUIContainers while maintaing our
        // contract with the cicero interfaces (without major refactoring of
        // our code).
        private static void GetAdjustedSelection(ITextPointer startIn, ITextPointer endIn, out ITextPointer startOut, out ITextPointer endOut)
        {
            startOut = startIn;
            endOut = endIn;

            TextPointer start = startOut as TextPointer;

            // Tables and BlockUIContainers only exist in TextContainers, if
            // we're in some other kind of document no adjustments are needed.
            if (start == null)
            {
                return;
            }

            TextPointer end = (TextPointer)endOut;

            if (startIn.CompareTo(endIn) != 0)
            {
                bool scopingBlockUIContainer = TextPointerBase.IsInBlockUIContainer(start) || TextPointerBase.IsInBlockUIContainer(end);
                TableCell startCell = TextRangeEditTables.GetTableCellFromPosition(start);
                TableCell endCell = TextRangeEditTables.GetTableCellFromPosition(end);
                bool singleScopingTableCell = (startCell != null && startCell == endCell);
                bool scopingTable = TextRangeEditTables.GetTableFromPosition(start) != null || TextRangeEditTables.GetTableFromPosition(end) != null;

                // With a non-empty selection, if neither end of the selection is inside a Table or BlockUIContainer,
                // there's nothing to adjust.
                if (!scopingBlockUIContainer &&
                    (singleScopingTableCell || !scopingTable))
                {
                    return;
                }
            }

            // From this point forward, we know selection will collapse to
            // a single insertion point, so we ignore end.

            if (start.IsAtRowEnd)
            {
                TextPointer previousPosition = start.GetNextInsertionPosition(LogicalDirection.Backward);
                Table currentTable = TextRangeEditTables.GetTableFromPosition(start);
                start = TextRangeEditTables.GetAdjustedRowEndPosition(currentTable, start);

                if (!start.IsAtInsertionPosition)
                {
                    // The document ends with a Table, and position is just past that.
                    // Back up to the previous TableCell proceding.
                    start = previousPosition;
                }
            }
            else if (TextPointerBase.IsInBlockUIContainer(start))
            {
                if (start.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart)
                {
                    start = start.GetNextInsertionPosition(LogicalDirection.Backward);
                }
                else
                {
                    start = start.GetNextInsertionPosition(LogicalDirection.Forward);
                }
            }

            while (start != null && TextPointerBase.IsBeforeFirstTable(start))
            {
                // Note that the symmetrical case, AfterLastTable, is handled by
                // the IsAtRowEnd test above.
                start = start.GetNextInsertionPosition(LogicalDirection.Forward);
            }

            // If we have non-canonical format, give up.
            if (start == null || start.IsAtRowEnd || TextPointerBase.IsInBlockUIContainer(start))
            {
                throw new COMException(SR.Get(SRID.TextStore_CompositionRejected), NativeMethods.E_FAIL);
            }

            startOut = start;
            endOut = start;
        }

        // Normalizes a range:
        //
        // -The start position is advanced over all element edges not visible
        //  to the IMEs.
        // -Start and end positions are moved to insertion positions.
        private void GetNormalizedRange(int startCharOffset, int endCharOffset, out ITextPointer start, out ITextPointer end)
        {
            start = CreatePointerAtCharOffset(startCharOffset, LogicalDirection.Forward);
            end = (startCharOffset == endCharOffset) ? start : CreatePointerAtCharOffset(endCharOffset, LogicalDirection.Backward);

            // Skip over hidden element edges.
            while (start.CompareTo(end) < 0)
            {
                TextPointerContext forwardContext = start.GetPointerContext(LogicalDirection.Forward);

                if (forwardContext == TextPointerContext.ElementStart)
                {
                    TextElement element = start.GetAdjacentElement(LogicalDirection.Forward) as TextElement;

                    if (element == null)
                        break;
                    if (element.IMELeftEdgeCharCount != 0)
                        break;
                }
                else if (forwardContext != TextPointerContext.ElementEnd)
                {
                    break;
                }

                start.MoveToNextContextPosition(LogicalDirection.Forward);
            }

            // Move to insertion positions.
            // If the positions are already adjacent to text, we must respect
            // the IME's decision in regards to exact placement.
            // MoveToInsertionPosition will skip over surrogates and combining
            // marks, but the IME needs fine-grained control over these positions.

            if (start.CompareTo(end) == 0)
            {
                start = start.GetFormatNormalizedPosition(LogicalDirection.Backward);
                end = start;
            }
            else
            {
                start = start.GetFormatNormalizedPosition(LogicalDirection.Backward);
                end = end.GetFormatNormalizedPosition(LogicalDirection.Backward);
            }
        }

        // Raises public events corresponding to internal callbacks from
        // Cicero.  Specifically, here we raise
        // TextInputStart, TextInputUpdate, and TextInput events.
        //
        // The Cicero contract disallows any reentrancy while calling IMEs hold
        // the document lock.  By this point, the lock has just been released,
        // and we are free to "play back" the record we stored in _compositionEventList.
        //
        // However, we may have several events queued up.  We use the undo stack
        // to roll document state back before the first event was received and
        // then restore state forward incrementally with each public event.
        private void HandleCompositionEvents(int previousUndoCount)
        {
            if (this.CompositionEventList.Count == 0 ||
                _compositionEventState != CompositionEventState.NotRaisingEvents)
            {
                // No work to do.
                return;
            }

            // Set a flag that informs the event listeners that they must hide
            // events from the IMEs.  We don't want the IMEs to know about
            // the view of the document we're about to present the application.
            _compositionEventState = CompositionEventState.RaisingEvents;

            try
            {
                // Remember our original selection positions.
                int imeSelectionAnchorOffset = this.TextSelection.AnchorPosition.Offset;
                int imeSelectionMovingOffset = this.TextSelection.MovingPosition.Offset;

                UndoManager undoManager = UndoManager.GetUndoManager(this.TextContainer.Parent);

                //
                // Undo the last set of IME changes, saving the current state
                // on the undo stack.
                //

                undoManager.SetRedoStack(null); // Clear the redo stack in case undoManager.UndoCount - previousUndoCount == 0.
                UndoQuietly(undoManager.UndoCount - previousUndoCount);
                Stack imeChangeStack = undoManager.SetRedoStack(null);

                int initialUndoCount = undoManager.UndoCount;

                //
                // Play back IME changes, raising public events as we go along.
                //

                int appSelectionAnchorOffset;
                int appSelectionMovingOffset;

                RaiseCompositionEvents(out appSelectionAnchorOffset, out appSelectionMovingOffset);

                //
                // Restore text composition with undo or redo
                //

                SetFinalDocumentState(undoManager, imeChangeStack, undoManager.UndoCount - initialUndoCount, imeSelectionAnchorOffset, imeSelectionMovingOffset, appSelectionAnchorOffset, appSelectionMovingOffset);
            }
            finally
            {
                // Clear the composition message list
                this.CompositionEventList.Clear();

                // Reset the rasising composition events flag
                _compositionEventState = CompositionEventState.NotRaisingEvents;
            }
        }

        // Open the text parent undo unit
        private TextParentUndoUnit OpenTextParentUndoUnit()
        {
            UndoManager undoManager = UndoManager.GetUndoManager(this.TextContainer.Parent);

            // Create the text parent undo unit
            TextParentUndoUnit textParentUndoUnit = new TextParentUndoUnit(this.TextSelection, this.TextSelection.Start, this.TextSelection.Start);

            // Open the text parent undo unit
            undoManager.Open(textParentUndoUnit);

            return textParentUndoUnit;
        }

        // Close the text parent undo unit
        private void CloseTextParentUndoUnit(TextParentUndoUnit textParentUndoUnit, UndoCloseAction undoCloseAction)
        {
            if (textParentUndoUnit != null)
            {
                UndoManager undoManager = UndoManager.GetUndoManager(this.TextContainer.Parent);

                // Close the text parent undo unit
                undoManager.Close(textParentUndoUnit, undoCloseAction);
            }
        }

        // Raise the each composition events(Start, Update and End).
        // At this point, hte document has been "rolled back" to its original
        // state before the last IME edit.  We'll play back each IME edit
        // (StartComposition/UpdateComposition/EndComposition) now, raising
        // public events at each iteration.
        private void RaiseCompositionEvents(out int appSelectionAnchorOffset, out int appSelectionMovingOffset)
        {
            appSelectionAnchorOffset = -1;
            appSelectionMovingOffset = -1;

            UndoManager undoManager = UndoManager.GetUndoManager(this.TextContainer.Parent);

            // Raise the each composition events
            for (int i = 0; i < this.CompositionEventList.Count; i++)
            {
                CompositionEventRecord record = this.CompositionEventList[i];
                FrameworkTextComposition composition = CreateComposition(this.TextEditor, this);

                ITextPointer start = this.TextContainer.CreatePointerAtOffset(record.StartOffsetBefore, LogicalDirection.Backward);
                ITextPointer end = this.TextContainer.CreatePointerAtOffset(record.EndOffsetBefore, LogicalDirection.Forward);

                bool handled = false;
                _handledByTextStoreListener = false;
                _compositionModifiedByEventListener = false;

                switch (record.Stage)
                {
                    case CompositionStage.StartComposition:
                        composition.Stage = TextCompositionStage.None;
                        composition.SetCompositionPositions(start, end, record.Text);

                        undoManager.MinUndoStackCount = undoManager.UndoCount;
                        try
                        {
                            // PUBLIC event:
                            handled = TextCompositionManager.StartComposition(composition);
                        }
                        finally
                        {
                            undoManager.MinUndoStackCount = 0;
                        }
                        break;

                    case CompositionStage.UpdateComposition:
                        composition.Stage = TextCompositionStage.Started;
                        composition.SetCompositionPositions(start, end, record.Text);

                        // At its discretion, an IME may implicitly convert the leading edge of the composition, for example the Pinyin IME
                        // finalizes implicitly the composition chunk preceding the unrecognized character and
                        // creates a new composition. For example if composition string is 'c1''c2'';''c3'
                        // what will happen is that 'c1''c2' will be finalized and new composition starting from ';'
                        // will be created. Also as a result of this the composition start/end gets shifted.
                        // This behavior can violate the MaxLength property.
                        //
                        // If the composition was shifted we will finalize the chunk before the shift. The actual filtering will occur when
                        // UpdateCompositionText gets called when we handle the TextInput event caused by CompleteComposition.
                        //

                        undoManager.MinUndoStackCount = undoManager.UndoCount;
                        try
                        {
                            if (IsCompositionRecordShifted(record) && IsMaxLengthExceeded(composition.CompositionText, (record.EndOffsetBefore - record.StartOffsetBefore)))
                            {
                                composition.SetResultPositions(start, end, record.Text);

                                // PUBLIC event:
                                handled = TextCompositionManager.CompleteComposition(composition);

                                _compositionModifiedByEventListener = true;// this will cause the for() loop to break;
                            }
                            else if (!record.IsShiftUpdate)
                            {
                                // PUBLIC event:
                                handled = TextCompositionManager.UpdateComposition(composition);
                            }
                        }
                        finally
                        {
                            undoManager.MinUndoStackCount = 0;
                        }

                        break;

                    case CompositionStage.EndComposition:
                        composition.Stage = TextCompositionStage.Started;
                        composition.SetResultPositions(start, end, record.Text);

                        undoManager.MinUndoStackCount = undoManager.UndoCount;
                        try
                        {
                            _isEffectivelyNotComposing = true;
                            // PUBLIC event:
                            handled = TextCompositionManager.CompleteComposition(composition);
                        }
                        finally
                        {
                            undoManager.MinUndoStackCount = 0;
                            _isEffectivelyNotComposing = false;
                        }

                        break;

                    default:
                        Invariant.Assert(false, "Invalid composition stage!");
                        break;
                }

                // If composition events is handled by application, we immediately complete the
                // composition and keep the application's change.
                if ((record.Stage == CompositionStage.EndComposition && !_handledByTextStoreListener) ||
                    (record.Stage != CompositionStage.EndComposition && handled) ||
                    composition.PendingComplete)
                {
                    _compositionModifiedByEventListener = true;
                }

                if (_compositionModifiedByEventListener)
                {
                    // Stop rasing the composition by application's text change or handled events
                    appSelectionAnchorOffset = this.TextSelection.AnchorPosition.Offset;
                    appSelectionMovingOffset = this.TextSelection.MovingPosition.Offset;
                    break;
                }

                // We're clear to update the composition.
                // For EndComposition, this has already happened in the default
                // TextEditor TextInputEvent listener.  (We don't have default
                // control/editor listeners for TextInputStart or TextInputUpdate
                // event because there are no public virtuals for those events
                // on UIElement.)
                if (record.Stage != CompositionStage.EndComposition && !record.IsShiftUpdate)
                {
                    // UpdateCompositionText raises a PUBLIC EVENT....
                    UpdateCompositionText(composition);
                }

                if (record.Stage == CompositionStage.EndComposition)
                {
                    // Move the start position of the next complete composition text
                    start = end.GetFrozenPointer(LogicalDirection.Backward);
                }

                // Because we just raised an event, we may need to complete the composition.
                if (_compositionModifiedByEventListener)
                {
                    // Stop rasing the composition by application's text change or handled events
                    appSelectionAnchorOffset = this.TextSelection.AnchorPosition.Offset;
                    appSelectionMovingOffset = this.TextSelection.MovingPosition.Offset;
                    break;
                }
            }
        }

        // Does this composition text breach the MaxLength constraint?
        private bool IsMaxLengthExceeded(string textData, int charsToReplaceCount)
        {
            // We only filter text for plain text content

            if (!this.TextEditor.AcceptsRichContent && this.TextEditor.MaxLength > 0)
            {
                ITextContainer textContainer = this.TextContainer;
                int currentLength = textContainer.SymbolCount - charsToReplaceCount;

                int extraCharsAllowed = Math.Max(0, this.TextEditor.MaxLength - currentLength);

                // Does textData length exceed allowed char length?
                if (textData.Length > extraCharsAllowed)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsCompositionRecordShifted(CompositionEventRecord record)
        {
            //
            // _previousCompositionStartOffset is set in OnUpdateComposition IME callback.
            // At this point it reflects the current offsets of the composition text.
            //

            if ((((0 <= record.StartOffsetBefore) && (0 <= _previousCompositionStartOffset))
                && (record.StartOffsetBefore < _previousCompositionStartOffset)) ||
                record.IsShiftUpdate)
            {
                return true;
            }

            return false;
        }

        // Called after raising public events.
        //
        // If the application interrupted events, this method will temporarily rollback
        // the application changes to safely finalize the composition, then restore
        // the application changes.
        //
        // If the application did not interrupt events, restores the original view
        // of the document last seen by IMEs.
        private void SetFinalDocumentState(UndoManager undoManager, Stack imeChangeStack, int appChangeCount,
            int imeSelectionAnchorOffset, int imeSelectionMovingOffset, int appSelectionAnchorOffset, int appSelectionMovingOffset)
        {
            this.TextSelection.BeginChangeNoUndo();

            try
            {
                bool textChanged = _compositionModifiedByEventListener;

                //
                // Undo app changes.
                //

                UndoQuietly(appChangeCount);

                //
                // Redo IME changes.
                //

                Stack appRedoStack = undoManager.SetRedoStack(imeChangeStack);
                int imeChangeCount = imeChangeStack.Count;
                RedoQuietly(imeChangeCount);

                // At this point the document should be exactly where the IME left it.
                Invariant.Assert(_netCharCount == this.TextContainer.IMECharCount);

                if (textChanged)
                {
                    //
                    // We need to complete the composition before continuing.
                    //

                    int completeUnitCount = undoManager.UndoCount;

                    if (_isComposing)
                    {
                        TextParentUndoUnit completeUndoUnit = OpenTextParentUndoUnit();
                        try
                        {
                            CompleteComposition();
                        }
                        finally
                        {
                            CloseTextParentUndoUnit(completeUndoUnit, (completeUndoUnit.LastUnit != null) ? UndoCloseAction.Commit : UndoCloseAction.Discard);
                        }
                    }

                    completeUnitCount = (undoManager.UndoCount - completeUnitCount);

                    // Set a flag that informs the event listeners they need
                    // to pass along change notifications to the IMEs.
                    _compositionEventState = CompositionEventState.ApplyingApplicationChange;
                    try
                    {
                        //
                        // Undo the composition complete, if any.
                        //

                        UndoQuietly(completeUnitCount);

                        //
                        // Undo the remaining IME changes.
                        //

                        UndoQuietly(imeChangeCount);

                        //
                        // Restore application changes.
                        //

                        undoManager.SetRedoStack(appRedoStack);
                        RedoQuietly(appChangeCount);

                        // The IME should have received the app change events from preceeding RedoQuietly.
                        Invariant.Assert(_netCharCount == this.TextContainer.IMECharCount);

                        // we can't rely on Redo fixing up the selection.
                        // If the app only modified the selection appChangeCount == 0.
                        ITextPointer anchor = this.TextContainer.CreatePointerAtOffset(appSelectionAnchorOffset, LogicalDirection.Forward);
                        ITextPointer moving = this.TextContainer.CreatePointerAtOffset(appSelectionMovingOffset, LogicalDirection.Forward);

                        this.TextSelection.Select(anchor, moving);

                        //
                        // We may have a filtering related composition undo unit on the top
                        // of the stack and if that's the case we want to merge it with all
                        // other composition undo units (if present).
                        //
                        MergeCompositionUndoUnits();
                    }
                    finally
                    {
                        // Reset CompositionEventState after Redo operation
                        _compositionEventState = CompositionEventState.RaisingEvents;
                    }
                }
                else
                {
                    // Restore the selection.
                    ITextPointer anchor = this.TextContainer.CreatePointerAtOffset(imeSelectionAnchorOffset, LogicalDirection.Backward);
                    ITextPointer moving = this.TextContainer.CreatePointerAtOffset(imeSelectionMovingOffset, LogicalDirection.Backward);

                    this.TextSelection.Select(anchor, moving);

                    // Since we just had a composition accepted, we need to merge all
                    // of its individual units now.
                    MergeCompositionUndoUnits();
                }
            }
            finally
            {
                this.TextSelection.EndChange(false /* disableScroll */, true /* skipEvents */);
            }
        }

        // Pops a unit off the undo stack without raising any public events.
        private void UndoQuietly(int count)
        {
            if (count > 0)
            {
                UndoManager undoManager = UndoManager.GetUndoManager(this.TextContainer.Parent);

                this.TextSelection.BeginChangeNoUndo();
                try
                {
                    undoManager.Undo(count);
                }
                finally
                {
                    this.TextSelection.EndChange(false /* disableScroll */, true /* skipEvents */);
                }
            }
        }

        // Pops a unit off the redo stack without raising any public events.
        private void RedoQuietly(int count)
        {
            if (count > 0)
            {
                UndoManager undoManager = UndoManager.GetUndoManager(this.TextContainer.Parent);

                this.TextSelection.BeginChangeNoUndo();
                try
                {
                    undoManager.Redo(count);
                }
                finally
                {
                    this.TextSelection.EndChange(false /* disableScroll */, true /* skipEvents */);
                }
            }
        }

        // Merges individual undo units that are part of a single composition into
        // single undo units.
        private void MergeCompositionUndoUnits()
        {
            UndoManager undoManager = UndoManager.GetUndoManager(this.TextContainer.Parent);

            if (undoManager == null || !undoManager.IsEnabled)
            {
                return;
            }

            // Walk backwards through the undo stack, looking for units originating
            // from a single composition to merge.
            int i = undoManager.UndoCount - 1;
            int j = undoManager.UndoCount - 1;

            while (i >= 0)
            {
                CompositionParentUndoUnit unit = undoManager.GetUndoUnit(i) as CompositionParentUndoUnit;

                if (unit == null || (unit.IsFirstCompositionUnit && unit.IsLastCompositionUnit)) // what if first/last by chance? Miss preceeding...
                    break;

                if (!unit.IsFirstCompositionUnit)
                {
                    i--;
                    continue;
                }

                // We're ready to merge.
                int mergeCount = j - i;

                for (int k = i+1; k <= i + mergeCount; k++)
                {
                    CompositionParentUndoUnit mergeSource = (CompositionParentUndoUnit)undoManager.GetUndoUnit(k);
                    unit.MergeCompositionUnit(mergeSource);
                }

                undoManager.RemoveUndoRange(i + 1, mergeCount);

                i--;
                j = i;
            }
        }


        // Returns the top CompositionParentUndoUnit on the undo stack,
        // if any.  Does not actually pop the unit.
        private CompositionParentUndoUnit PeekCompositionParentUndoUnit()
        {
            UndoManager undoManager = UndoManager.GetUndoManager(this.TextContainer.Parent);

            if (undoManager == null || !undoManager.IsEnabled)
                return null;

            return undoManager.PeekUndoStack() as CompositionParentUndoUnit;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        private bool IsTextEditorValid
        {
            get { return _weakTextEditor.IsValid; }
        }

        private TextEditor TextEditor
        {
            get { return _weakTextEditor.TextEditor; }
        }

        private ITextSelection TextSelection
        {
            get { return this.TextEditor.Selection; }
        }

        private bool IsReadOnly
        {
            get
            {
                return ((bool)this.UiScope.GetValue(TextEditor.IsReadOnlyProperty) || TextEditor.IsReadOnly);
            }
        }

        // Lazy allocated _compositionEventList accessor.
        private List<CompositionEventRecord> CompositionEventList
        {
            get
            {
                if (_compositionEventList == null)
                {
                    _compositionEventList = new List<CompositionEventRecord>();
                }

                return _compositionEventList;
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // This is an enumrator of AppProperty types.
        private enum AttributeStyle
        {
            InputScope = 0,
            Font_Style_Height = 1,
            Font_FaceName = 2,
            Font_SizePts = 3,
            Text_ReadOnly = 4,
            Text_Orientation = 5,
            Text_VerticalWriting = 6,
        }

        // This structure maps TS_ATTR (GUID) to AttributeStyle.
        private struct TextServicesAttribute
        {
            internal TextServicesAttribute(Guid guid, AttributeStyle style)
            {
                _guid = guid;
                _style = style;
            }

            internal Guid Guid
            {
                get { return _guid; }
            }

            internal AttributeStyle Style
            {
                get { return _style; }
            }

            private Guid _guid;

            private AttributeStyle _style;
        }

        // Scope WeakReference wrapper to detect whether the target object is invalid.
        private class ScopeWeakReference : WeakReference
        {
            internal ScopeWeakReference(object obj) : base(obj)
            {
            }

            internal bool IsValid
            {
                get
                {
                    try
                    {
                        return IsAlive;
                    }
                    catch (InvalidOperationException)
                    {
                        return false;
                    }
                }
            }

            internal TextEditor TextEditor
            {
                get
                {
                    try
                    {
                        return (TextEditor)this.Target;
                    }
                    catch (InvalidOperationException)
                    {
                        return null;
                    }
                }
            }
        }

        // This structure maps TS_ATTR (GUID) to AttributeStyle.
        private class MouseSink : IDisposable, IComparer
        {
            internal MouseSink(UnsafeNativeMethods.ITfRangeACP range, UnsafeNativeMethods.ITfMouseSink sink, int cookie)
            {
                _range = new SecurityCriticalDataClass<UnsafeNativeMethods.ITfRangeACP>(range);
                _sink = new SecurityCriticalDataClass<UnsafeNativeMethods.ITfMouseSink>(sink);
                _cookie = cookie;
            }

            public void Dispose()
            {
                Invariant.Assert(!_locked);

                // In case Dispose comes twice.
                if (_range != null)
                {
                    Marshal.ReleaseComObject(_range.Value);
                    _range = null;
                }
                if (_sink != null)
                {
                    Marshal.ReleaseComObject(_sink.Value);
                    _sink = null;
                }
                _cookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
                GC.SuppressFinalize(this);
            }

            public int Compare( Object x, Object y )
            {
                return (((MouseSink)x)._cookie - ((MouseSink)y)._cookie);
            }

            // While Locked == false, UnadviseSink will not immediately
            // dispose of this object.  Locked is a poor man's AddRef/Release,
            // necessary because (1) we can't let the gc Dispose this object on
            // the finalizer thread, and (2) cicero will sometimes unadvise
            // this object from within a callback.
            internal bool Locked
            {
                get
                {
                    return _locked;
                }

                set
                {
                    _locked = value;

                    if (!_locked && _pendingDispose)
                    {
                        Dispose();
                    }
                }
            }

            // Set true when this object has been released via UnadviseSink.
            internal bool PendingDispose
            {
                set
                {
                    _pendingDispose = value;
                }
            }

            internal UnsafeNativeMethods.ITfRangeACP Range
            {
                get {return _range.Value;}
            }

            internal UnsafeNativeMethods.ITfMouseSink Sink
            {
                get {return _sink.Value;}
            }

            internal int Cookie {get{return _cookie;}}

            private SecurityCriticalDataClass<UnsafeNativeMethods.ITfRangeACP> _range;

            private SecurityCriticalDataClass<UnsafeNativeMethods.ITfMouseSink> _sink;

            private int _cookie;

            // Set true during a sink callback.
            private bool _locked;

            // Set true when this object has been released via UnadviseSink.
            private bool _pendingDispose;
        }

        // Custom parent undo unit used to hold composition updates.
        private class CompositionParentUndoUnit : TextParentUndoUnit
        {
            internal CompositionParentUndoUnit(ITextSelection selection, ITextPointer anchorPosition, ITextPointer movingPosition, bool isFirstCompositionUnit)
                : base(selection, anchorPosition, movingPosition)
            {
                _isFirstCompositionUnit = isFirstCompositionUnit;
            }

            // Creates a redo unit from an undo unit.
            private CompositionParentUndoUnit(CompositionParentUndoUnit undoUnit)
                : base(undoUnit)
            {
                _isFirstCompositionUnit = undoUnit._isFirstCompositionUnit;
                _isLastCompositionUnit = undoUnit._isLastCompositionUnit;
            }

            protected override TextParentUndoUnit CreateRedoUnit()
            {
                return new CompositionParentUndoUnit(this);
            }

            // Merges another unit into this unit.
            internal void MergeCompositionUnit(CompositionParentUndoUnit unit)
            {
                object[] units = unit.CopyUnits();

                Invariant.Assert(this.Locked); // If this fails, then the Locked = true below is invalid.
                this.Locked = false;

                for (int i = units.Length - 1; i >= 0; i--)
                {
                    Add((IUndoUnit)units[i]);
                }

                this.Locked = true;

                MergeRedoSelectionState(unit);

                _isLastCompositionUnit |= unit.IsLastCompositionUnit;
            }

            // True if this unit is the first unit of a composition.
            internal bool IsFirstCompositionUnit
            {
                get
                {
                    return _isFirstCompositionUnit;
                }
            }

            // True if this unit is the last unit of a composition.
            internal bool IsLastCompositionUnit
            {
                get
                {
                    return _isLastCompositionUnit;
                }

                set
                {
                    _isLastCompositionUnit = value;
                }
            }

            // Returns a shallow copy of this units children.
            private object[] CopyUnits()
            {
                return this.Units.ToArray();
            }

            private readonly bool _isFirstCompositionUnit;

            private bool _isLastCompositionUnit;
        }

        // Tristate used to filter TextContainer change events.
        private enum CompositionEventState
        {
            // Not currently raising composition events.
            // Events received in this state are application changes.
            NotRaisingEvents = 0,

            // Raising public events.  Events should be hidden from IMEs.
            RaisingEvents = 1,

            // Raising public event, but events should not be hidden from IMEs.
            ApplyingApplicationChange = 2,
        }

        // Context associated with a FrameworkTextComposition.
        private enum CompositionStage
        {
            /// <summary>
            /// The StartComposition has set.
            /// </summary>
            StartComposition = 1,

            /// <summary>
            /// The UpdateComposition has set.
            /// </summary>
            UpdateComposition = 2,

            /// <summary>
            /// The EndComposition has set.
            /// </summary>
            EndComposition = 3,
        }

        // Package for state saved during a composition start/update/end event.
        private class CompositionEventRecord
        {
            internal CompositionEventRecord(CompositionStage stage, int startOffsetBefore, int endOffsetBefore, string text):
                this(stage, startOffsetBefore, endOffsetBefore, text, false)
            {
            }
            internal CompositionEventRecord(CompositionStage stage, int startOffsetBefore, int endOffsetBefore, string text, bool isShiftUpdate)
            {
                _stage = stage;
                _startOffsetBefore = startOffsetBefore;
                _endOffsetBefore = endOffsetBefore;
                _text = text;
                _isShiftUpdate = isShiftUpdate;
            }


            internal CompositionStage Stage
            {
                get { return _stage; }
            }

            internal int StartOffsetBefore
            {
                get { return _startOffsetBefore; }
            }

            internal int EndOffsetBefore
            {
                get { return _endOffsetBefore; }
            }

            internal string Text
            {
                get { return _text; }
            }
            internal bool IsShiftUpdate
            {
                get { return _isShiftUpdate; }
            }

            private readonly CompositionStage _stage;

            private readonly int _startOffsetBefore;

            private readonly int _endOffsetBefore;

            private readonly string _text;

            /// <summary>
            /// Indicates if current record is for update event which
            /// caused also a shift of composition positions.
            /// This can happen in OnUpdateComposition
            /// </summary>
            private readonly bool _isShiftUpdate;
        }

        #endregion Private Types

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // An element to which the TextEditor instance is attached
        private readonly ScopeWeakReference _weakTextEditor;

        private TextServicesHost _textservicesHost;

        // A TSF sink used to notify TSF after selection, document, or layout changes.
        private UnsafeNativeMethods.ITextStoreACPSink _sink;

        // true if TSF has a pending lock upgrade request.
        private bool _pendingWriteReq;

        // The current document lock status.
        private UnsafeNativeMethods.LockFlags _lockFlags;

        // If we're waiting for an async lock request to be dispatched,
        // this field will be non-zero with the requested lock privilege level.
        private UnsafeNativeMethods.LockFlags _pendingAsyncLockFlags;

        // Counter set non-zero during OnTextChange callbacks.
        // Used to prevent reentrant locks.
        private int _textChangeReentrencyCount;

        // true if we're in the middle of an ongoing composition.
        private bool _isComposing;

        // true while raising an event that should be treated as "not composing"
        // by the data-binding engine
        private bool _isEffectivelyNotComposing;

        private int _previousCompositionStartOffset = -1;

        private int _previousCompositionEndOffset = -1;

        // Position of the composition start as of the last update.
        // We can't simply store int offsets because under some circumstances
        // IMEs will ignore text input during a composition, letting the editor
        // handle the event, in which case we need live pointers to react
        // to the changes.  See bug 118934.
        private ITextPointer _previousCompositionStart;

        // Position of the composition end as of the last update.
        private ITextPointer _previousCompositionEnd;

        // Manages display attributes for active compositions.
        private TextServicesProperty _textservicesproperty;

        // We only ever expose one view, so we can use a constant identifier
        // when talking to TSF.
        private const int _viewCookie = 0;

        // The TSF document object.  This is a native resource.
        private SecurityCriticalDataClass<UnsafeNativeMethods.ITfDocumentMgr> _documentmanager;

        // The ITfThreadFocusSink cookie.
        private int _threadFocusCookie;

        // The ITfEditSink cookie.
        private int _editSinkCookie;

        // The readonly edit cookie TSF returns from CreateContext.
        private int _editCookie;

        // The transitory extension sink cookie.
        private int _transitoryExtensionSinkCookie;

        // This is the temp array for TS_ATTRVAL for RetrieveRequestedAttr.
        private ArrayList _preparedattributes;

        // This is the array for mouse sinks.
        private ArrayList _mouseSinks;

        // We keep the mapping data from TS_ATTR (GUID) to AttributeStyle.
        private static readonly TextServicesAttribute[] _supportingattributes = new TextServicesAttribute[] {
            new TextServicesAttribute(UnsafeNativeMethods.GUID_PROP_INPUTSCOPE, AttributeStyle.InputScope),
                          new TextServicesAttribute(UnsafeNativeMethods.TSATTRID_Font_Style_Height, AttributeStyle.Font_Style_Height),
                          new TextServicesAttribute(UnsafeNativeMethods.TSATTRID_Font_FaceName, AttributeStyle.Font_FaceName),
                          new TextServicesAttribute(UnsafeNativeMethods.TSATTRID_Font_SizePts, AttributeStyle.Font_SizePts),
                          new TextServicesAttribute(UnsafeNativeMethods.TSATTRID_Text_ReadOnly, AttributeStyle.Text_ReadOnly),
                          new TextServicesAttribute(UnsafeNativeMethods.TSATTRID_Text_Orientation, AttributeStyle.Text_Orientation),
                          new TextServicesAttribute(UnsafeNativeMethods.TSATTRID_Text_VerticalWriting, AttributeStyle.Text_VerticalWriting),
        };

        // This is true if the current selection is for an interim character. Koream Interim Support.
        private bool _interimSelection;

#if ENABLE_INK_EMBEDDING
        // Buffer used by RemoveContent/OnTextContainerChangeAdded to record
        // the first symbol offset affected by a content remove edit.
        private int _minSymbolsRemovedIndex;
#endif // #if ENABLE_INK_EMBEDDING

        // Set true if a TIP modifies the selection.
        // Used to avoid reporting non-external selection changes when the
        // OnLockGranted change block closes.
        private bool _ignoreNextSelectionChange;

        // The sum of all character added/removed events.
        // Should never be negative.
        // Should always equal this.TextContainer.IMECharCount.
        // This field is only used for reliabilty reasons, in calls to Invariant.Assert.
        private int _netCharCount;

        // The element might be disconnected from tree when the parent window is moved.
        // We need to make a LayoutChange notification when it gets focus back.
        private bool _makeLayoutChangeOnGotFocus;

        // Flag that indicate the rasising composition events
        private CompositionEventState _compositionEventState;

        // Flag that indicate the composition text changed by
        // someone other than the editor or an IME (ie, during
        // a public event).
        // NOTE: This flag can internally be set by us when the composition text
        // is being changed as a result of filtering to enforce the MaxLenght property.
        private bool _compositionModifiedByEventListener;

        // Composition event list.
        private List<CompositionEventRecord> _compositionEventList;

        // Used to identify the start of a new composition on the undo stack.
        private bool _nextUndoUnitIsFirstCompositionUnit = true;

        // A record of the last text inserted by a composition update.
        private string _lastCompositionText;

        // Set true when TextEditor.OnTextInput handles a TextInput event (no app override).
        private bool _handledByTextStoreListener;

        // Two bools used to detect when text changes occur during UpdateLayout
        // while Cicero holds a lock
        private bool _isInUpdateLayout;
        private bool _hasTextChangedInUpdateLayout;

        #endregion Private Fields
    }
}

