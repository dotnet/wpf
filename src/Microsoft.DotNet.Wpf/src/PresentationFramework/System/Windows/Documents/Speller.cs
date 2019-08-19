// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Spell checking component for the TextEditor.
//

namespace System.Windows.Documents
{
    using MS.Internal;
    using System.Threading;
    using System.Windows.Threading;
    using System.Globalization;
    using System.Collections;
    using System.Collections.Generic;
    using System.Security;
    using System.Runtime.InteropServices;
    using MS.Win32;
    using System.Windows.Controls;
    using System.Windows.Markup; // XmlLanguage
    using System.Windows.Input;
    using System.IO;
    using System.Windows.Navigation;

    // Spell checking component for the TextEditor.
    // Class is marked as partial to allow for definition of TextMapOffsetLogger in a separate
    // source file. When TextMapOffsetLogger is removed, the partial declaration can
    // be removed. See doc comments in TextMapOffsetErrorLogger for more details.
    internal partial class Speller
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates a new instance.  We have at most one Speller instance
        // per TextEditor.
        internal Speller(TextEditor textEditor)
        {
            _textEditor = textEditor;

            _textEditor.TextContainer.Change += new TextContainerChangeEventHandler(OnTextContainerChange);

            // Schedule some idle time to start examining the document.
            if (_textEditor.TextContainer.SymbolCount > 0)
            {
                ScheduleIdleCallback();
            }

            _defaultCulture = InputLanguageManager.Current != null ? InputLanguageManager.Current.CurrentInputLanguage :
                                                                     Thread.CurrentThread.CurrentCulture;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Called by TextEditor to disable spelling.
        internal void Detach()
        {
            Invariant.Assert(_textEditor != null);

            _textEditor.TextContainer.Change -= new TextContainerChangeEventHandler(OnTextContainerChange);

            if (_pendingCaretMovedCallback)
            {
                _textEditor.Selection.Changed -= new EventHandler(OnCaretMoved);
                _textEditor.UiScope.LostFocus -= new RoutedEventHandler(OnLostFocus);
                _pendingCaretMovedCallback = false;
            }

            // Shutdown the highlight layer.
            if (_highlightLayer != null)
            {
                _textEditor.TextContainer.Highlights.RemoveLayer(_highlightLayer);
                _highlightLayer = null;
            }

            // Shutdown the status table.
            _statusTable = null;

            // Release our nl6 objects.
            if (_spellerInterop != null)
            {
                _spellerInterop.Dispose();
                _spellerInterop = null;
            }

            // Clear the TextEditor.  (Used as a sentinel to track Detachedness
            // from pending idle callback.)
            _textEditor = null;
        }

        // Returns an object holding state about an error at the specified
        // position, or null if no error is present.
        //
        // If forceEvaluation is set true, the speller will analyze any dirty region
        // covered by the position.  Otherwise dirty regions will be treated as
        // non-errors.
        internal SpellingError GetError(ITextPointer position, LogicalDirection direction, bool forceEvaluation)
        {
            ITextPointer start;
            ITextPointer end;
            SpellingError error;

            // Evaluate any pending dirty region.
            if (forceEvaluation &&
                EnsureInitialized() &&
                _statusTable.IsRunType(position.CreateStaticPointer(), direction, SpellerStatusTable.RunType.Dirty))
            {
                ScanPosition(position, direction);
            }

            // Get the error result.
            if (_statusTable != null &&
                _statusTable.GetError(position.CreateStaticPointer(), direction, out start, out end))
            {
                error = new SpellingError(this, start, end);
            }
            else
            {
                error = null;
            }

            return error;
        }

        // Worker for TextBox/RichTextBox.GetNextSpellingErrorPosition.
        // Returns the start position of the next error, or null if no error exists.
        //
        // NB: this method will force an evaluation of any dirty regions between
        // position and the next error, which in the worst case is the rest of
        // the document.
        internal ITextPointer GetNextSpellingErrorPosition(ITextPointer position, LogicalDirection direction)
        {
            if (!EnsureInitialized())
                return null;

            StaticTextPointer endPosition;
            SpellerStatusTable.RunType runType;

            while (_statusTable.GetRun(position.CreateStaticPointer(), direction, out runType, out endPosition))
            {
                if (runType == SpellerStatusTable.RunType.Error)
                    break;

                if (runType == SpellerStatusTable.RunType.Dirty)
                {
                    ScanPosition(position, direction);

                    _statusTable.GetRun(position.CreateStaticPointer(), direction, out runType, out endPosition);
                    Invariant.Assert(runType != SpellerStatusTable.RunType.Dirty);

                    if (runType == SpellerStatusTable.RunType.Error)
                        break;
                }

                position = endPosition.CreateDynamicTextPointer(direction);
            }

            SpellingError spellingError = GetError(position, direction, false /* forceEvaluation */);
            return spellingError == null ? null : spellingError.Start;
        }

        // Called by SpellingError to retreive a list of suggestions
        // for an error range.
        // This method actually runs the speller on the specified text,
        // re-evaluating the error from scratch.
        internal IList GetSuggestionsForError(SpellingError error)
        {
            ITextPointer contextStart;
            ITextPointer contextEnd;
            ITextPointer contentStart;
            ITextPointer contentEnd;
            TextMap textMap;
            ArrayList suggestions;

            suggestions = new ArrayList(1);

            //
            // IMPORTANT!!
            //
            // This logic here must match ScanRange, or else we might not
            // calculate the exact same error.  Keep the two methods in sync!
            //

            XmlLanguage language;
            CultureInfo culture = GetCurrentCultureAndLanguage(error.Start, out language);
            if (culture == null || !_spellerInterop.CanSpellCheck(culture))
            {
                // Return an empty list.
            }
            else
            {
                ExpandToWordBreakAndContext(error.Start, LogicalDirection.Backward, language, out contentStart, out contextStart);
                ExpandToWordBreakAndContext(error.End, LogicalDirection.Forward, language, out contentEnd, out contextEnd);

                textMap = new TextMap(contextStart, contextEnd, contentStart, contentEnd);

                SetCulture(culture);

                _spellerInterop.Mode = SpellerInteropBase.SpellerMode.SpellingErrorsWithSuggestions;

                _spellerInterop.EnumTextSegments(textMap.Text, textMap.TextLength, null,
                    new SpellerInteropBase.EnumTextSegmentsCallback(ScanErrorTextSegment), new TextMapCallbackData(textMap, suggestions));
            }

            return suggestions;
        }

        // Worker for context menu's "Ignore All" item.
        // Adds a word to the ignore list, and clears any matching errors.
        //
        // implement this as a process-wide list.
        internal void IgnoreAll(string word)
        {
            if (_ignoredWordsList == null)
            {
                _ignoredWordsList = new ArrayList(1);
            }

            int index = _ignoredWordsList.BinarySearch(word, new CaseInsensitiveComparer(_defaultCulture));

            if (index < 0)
            {
                // This is a new word to ignore.

                // Add it the list so we don't flag it later.
                _ignoredWordsList.Insert(~index, word);

                // Then search through the error list, clearing any matching
                // errors.

                if (_statusTable != null)
                {
                    StaticTextPointer pointer = _textEditor.TextContainer.CreateStaticPointerAtOffset(0);
                    ITextPointer errorStart;
                    ITextPointer errorEnd;
                    Char[] charArray = null;

                    while (!pointer.IsNull)
                    {
                        if (_statusTable.GetError(pointer, LogicalDirection.Forward, out errorStart, out errorEnd))
                        {
                            string error = TextRangeBase.GetTextInternal(errorStart, errorEnd, ref charArray);

                            if (String.Compare(word, error, true /* ignoreCase */, _defaultCulture) == 0)
                            {
                                _statusTable.MarkCleanRange(errorStart, errorEnd);
                            }
                        }

                        pointer = _statusTable.GetNextErrorTransition(pointer, LogicalDirection.Forward);
                    }
                }
            }
        }

        // Sets the speller engine spelling reform option.
        internal void SetSpellingReform(SpellingReform spellingReform)
        {
            if (_spellingReform != spellingReform)
            {
                _spellingReform = spellingReform;

                // Invalidate the whole document.
                ResetErrors();
            }
        }

        /// <summary>
        /// Loads/unloads custom dictionaries based on value of <paramref name="add"/>.
        /// </summary>
        /// <param name="dictionaryLocations"></param>
        /// <param name="add"></param>
        internal void SetCustomDictionaries(CustomDictionarySources dictionaryLocations, bool add)
        {
            if (!EnsureInitialized())
            {
                return;
            }
            if (add)
            {
                foreach (Uri item in dictionaryLocations)
                {
                    OnDictionaryUriAdded(item);
                }
            }
            else
            {
                OnDictionaryUriCollectionCleared();
            }
        }

        // Called when a global state change invalidates all cached errors.
        internal void ResetErrors()
        {
            if (_statusTable != null)
            {
                _statusTable.MarkDirtyRange(_textEditor.TextContainer.Start, _textEditor.TextContainer.End);

                if (_textEditor.TextContainer.SymbolCount > 0)
                {
                    ScheduleIdleCallback();
                }
            }
        }

        // Returns true if the specified property affects speller evaluation.
        internal static bool IsSpellerAffectingProperty(DependencyProperty property)
        {
            return property == FrameworkElement.LanguageProperty ||
                   property == SpellCheck.SpellingReformProperty;
        }

        /// <summary>
        /// Loads custom Dictionary.
        /// </summary>
        /// <param name="customLexiconPath"></param>
        internal void OnDictionaryUriAdded(Uri uri)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            //
            // Re-adding same dictionary URI first requires to unload previously loaded one with same URI
            //
            if (UriMap.ContainsKey(uri))
            {
                OnDictionaryUriRemoved(uri);
            }

            Uri pathUri;
            if (!uri.IsAbsoluteUri || uri.IsFile)
            {
                pathUri = ResolvePathUri(uri);
                object lexicon = _spellerInterop.LoadDictionary(pathUri.LocalPath);
                UriMap.Add(uri, new DictionaryInfo(pathUri, lexicon));
            }
            else
            {
                LoadDictionaryFromPackUri(uri);
            }
            ResetErrors();
        }

        /// <summary>
        /// Removes specified custom dictionary from the list of loaded dictionaries.
        /// </summary>
        /// <param name="uri"></param>
        internal void OnDictionaryUriRemoved(Uri uri)
        {
            if (!EnsureInitialized())
            {
                return;
            }
            if (!UriMap.ContainsKey(uri))
            {
                return;
            }
            DictionaryInfo info = UriMap[uri];
            try
            {
                _spellerInterop.UnloadDictionary(info.Lexicon);
            }
            catch(Exception e)
            {
                System.Diagnostics.Trace.Write(string.Format(CultureInfo.InvariantCulture, "Unloading dictionary failed. Original Uri:{0}, file Uri:{1}, exception:{2}", uri.ToString(), info.PathUri.ToString(), e.ToString()));
                throw;
            }
            UriMap.Remove(uri);
            ResetErrors();
        }

        /// <summary>
        /// Removes all custom dictionaries.
        /// </summary>
        internal void OnDictionaryUriCollectionCleared()
        {
            if (!EnsureInitialized())
            {
                return;
            }
            // Unload all files
            _spellerInterop.ReleaseAllLexicons();
            UriMap.Clear();
            ResetErrors();
        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // A run-length array tracking speller status of all text in the document.
        internal SpellerStatusTable StatusTable
        {
            get
            {
                return _statusTable;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// A map between the original location specified by a user and actual path + reference to loaded custom dicitonary.
        /// </summary>
        private Dictionary<Uri, DictionaryInfo> UriMap
        {
            get
            {
                if (_uriMap == null)
                {
                    _uriMap = new Dictionary<Uri, DictionaryInfo>();
                }
                return _uriMap;
            }
        }
        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Initializes state for the Speller.
        // Delayed until the first text change event, or first idle callback.
        private bool EnsureInitialized()
        {
            if (_spellerInterop != null)
                return true;

            if (_failedToInit)
                return false;

            Invariant.Assert(_highlightLayer == null);
            Invariant.Assert(_statusTable == null);

            _spellerInterop = SpellerInteropBase.CreateInstance();

            _failedToInit = (_spellerInterop == null);

            if (_failedToInit)
                return false;

            _highlightLayer = new SpellerHighlightLayer(this);

            _statusTable = new SpellerStatusTable(_textEditor.TextContainer.Start, _highlightLayer);

            _textEditor.TextContainer.Highlights.AddLayer(_highlightLayer);

            _spellingReform = (SpellingReform)_textEditor.UiScope.GetValue(SpellCheck.SpellingReformProperty);

            return true;
        }

        // Posts a background priority operation to the dispatcher queue.
        // All scanning takes place during idle-time callbacks.
        private void ScheduleIdleCallback()
        {
            if (!_pendingIdleCallback)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new DispatcherOperationCallback(OnIdle), null);
                _pendingIdleCallback = true;
            }
        }

        // Enables the TextSelection.Changed listener.
        // We call this method when an otherwise clean document has text
        // covered by the caret or an IME composition that must be analyzed
        // when the selection moves away.
        private void ScheduleCaretMovedCallback()
        {
            if (!_pendingCaretMovedCallback)
            {
                _textEditor.Selection.Changed += new EventHandler(OnCaretMoved);
                _textEditor.UiScope.LostFocus += new RoutedEventHandler(OnLostFocus);
                _pendingCaretMovedCallback = true;
            }
        }

        // Callback for document changes.
        // Marks appropriate sections of the document as dirty then posts
        // an idle request for future analysis.
        private void OnTextContainerChange(object sender, TextContainerChangeEventArgs e)
        {
            Invariant.Assert(sender == _textEditor.TextContainer);

            if (e.Count == 0 ||
                (e.TextChange == TextChangeType.PropertyModified && !IsSpellerAffectingProperty(e.Property)))
            {
                // Speller doesn't care about most property changes.
                return;
            }

            if (_failedToInit)
            {
                // Speller engine is not available.
                return;
            }

            if (_statusTable != null)
            {
                _statusTable.OnTextChange(e);
            }

            ScheduleIdleCallback();
        }

        // Runs the speller idle callback.
        // During this callback, we scan dirty portions of the document
        // until all text is examined, or we exceed a set time limit.
        // If we run out of time with more work to do, we post a new idle
        // callback request, yielding to any pending high-priority work
        // (such as user input).
        //  this is no good.  We need a single idle callback for
        // all Speller instances, since we can't have 1000 TextBoxes/Spellers
        // each eating a 20 ms timeslice.
        private object OnIdle(object unused)
        {
            Invariant.Assert(_pendingIdleCallback);

            // Reset _pendingIdleCallback.
            _pendingIdleCallback = false;

            // _textEditor will be null if we've been detached since requesting the callback.
            if (_textEditor != null &&
                EnsureInitialized())
            {
                ITextPointer start;
                ITextPointer end;
                long timeLimit;
                ScanStatus status;

                timeLimit = DateTime.Now.Ticks + MaxIdleTimeSliceNs;

                end = null;
                status = null;

                // Iterate over chunks of dirty text until we run out of time
                // or finish with the entire document.
                do
                {
                    if (!GetNextScanRange(end, out start, out end))
                        break;

                    status = ScanRange(start, end, timeLimit);
                }
                while (!status.HasExceededTimeLimit);

                // Schedule any pending work before we yield.
                if (status != null)
                {
                    if (status.HasExceededTimeLimit)
                    {
                        ScheduleIdleCallback();
                    }
                }
            }

            return null;
        }

        // Callback for TextSelection.Changed event.
        private void OnCaretMoved(object sender, EventArgs e)
        {
            OnCaretMovedWorker();
        }

        // Callback for UiScope.LostFocus event.
        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            OnCaretMovedWorker();
        }

        // Callback for the TextSelection.Changed or UiScope.LostFocus events.
        // We enter this method when an otherwise clean document has text
        // covered by the caret or an IME composition that must be analyzed
        // when the selection moves away.
        private void OnCaretMovedWorker()
        {
            if (!_pendingCaretMovedCallback || _textEditor == null)
            {
                // Because the event route caches the callback, we can get a
                // callback even after removing the handler.
                // Just ignore the spurious callback.
                return;
            }

            _textEditor.Selection.Changed -= new EventHandler(OnCaretMoved);
            _textEditor.UiScope.LostFocus -= new RoutedEventHandler(OnLostFocus);
            _pendingCaretMovedCallback = false;

            // Now that the caret's out of the way, analyze the text it
            // used to cover the next time the app goes idle.
            ScheduleIdleCallback();
        }

        // Calculates the next run of text to feed to the speller.
        // If there's nothing left to analyze, returns false and start/end will
        // be null on exit.
        private bool GetNextScanRange(ITextPointer searchStart, out ITextPointer start, out ITextPointer end)
        {
            ITextPointer rawStart;
            ITextPointer rawEnd;

            start = null;
            end = null;

            // Consider prioritizing visible region.

            // First iteration of the scan loop, searchStart == null.
            if (searchStart == null)
            {
                searchStart = _textEditor.TextContainer.Start;
            }

            // Grab the first dirty range.
            GetNextScanRangeRaw(searchStart, out rawStart, out rawEnd);

            if (rawStart != null)
            {
                // Skip over the caret and/or IME composition.
                AdjustScanRangeAroundComposition(rawStart, rawEnd, out start, out end);
            }

            return start != null;
        }

        // Finds the next dirty range following searchStart, without considering
        // the current caret or IME composition.
        private void GetNextScanRangeRaw(ITextPointer searchStart, out ITextPointer start, out ITextPointer end)
        {
            Invariant.Assert(searchStart != null);

            start = null;
            end = null;

            // Grab the first dirty range.
            _statusTable.GetFirstDirtyRange(searchStart, out start, out end);

            if (start != null)
            {
                Invariant.Assert(start.CompareTo(end) < 0);

                // Cap the block size by a constant.
                if (start.GetOffsetToPosition(end) > MaxScanBlockSize)
                {
                    end = start.CreatePointer(MaxScanBlockSize);
                }

                // Ensure the block has constant language.
                XmlLanguage language = GetCurrentLanguage(start);
                end = GetNextLanguageTransition(start, LogicalDirection.Forward, language, end);
                Invariant.Assert(start.CompareTo(end) < 0);
            }
        }

        // Truncates a range of text if it overlaps the word containing the
        // caret, or an IME composition.
        private void AdjustScanRangeAroundComposition(ITextPointer rawStart, ITextPointer rawEnd,
            out ITextPointer start, out ITextPointer end)
        {
            start = rawStart;
            end = rawEnd;

            if (!_textEditor.Selection.IsEmpty)
            {
                // No caret to adjust around.
                return;
            }

            if (!_textEditor.UiScope.IsKeyboardFocused)
            {
                // Document isn't focused, no caret rendered.
                return;
            }

            // Get the word surrounding the caret.

            ITextPointer wordBreakLeft;
            ITextPointer wordBreakRight;
            ITextPointer caretPosition;
            TextMap textMap;
            ArrayList segments;

            caretPosition = _textEditor.Selection.Start;

            // Disable spell checking functionality since we're only
            // interested in word breaks here.  This greatly cuts down
            // the engine's workload.
            _spellerInterop.Mode = SpellerInteropBase.SpellerMode.WordBreaking;

            XmlLanguage language = GetCurrentLanguage(caretPosition);
            wordBreakLeft = SearchForWordBreaks(caretPosition, LogicalDirection.Backward, language, 1, false /* stopOnError */);
            wordBreakRight = SearchForWordBreaks(caretPosition, LogicalDirection.Forward, language, 1, false /* stopOnError */);

            textMap = new TextMap(wordBreakLeft, wordBreakRight, caretPosition, caretPosition);
            segments = new ArrayList(2);
            _spellerInterop.EnumTextSegments(textMap.Text, textMap.TextLength, null,
                new SpellerInteropBase.EnumTextSegmentsCallback(ExpandToWordBreakCallback), segments);

            // We will have no segments when position is surrounded by
            // nothing but white space.
            if (segments.Count != 0)
            {
                int leftBreakOffset;
                int rightBreakOffset;

                // Figure out where caretPosition lives in the segment list.
                FindPositionInSegmentList(textMap, LogicalDirection.Backward, segments, out leftBreakOffset, out rightBreakOffset);

                wordBreakLeft = textMap.MapOffsetToPosition(leftBreakOffset);
                wordBreakRight = textMap.MapOffsetToPosition(rightBreakOffset);
            }

            // Overlap?
            if (wordBreakLeft.CompareTo(rawEnd) < 0 &&
                wordBreakRight.CompareTo(rawStart) > 0)
            {
                if (wordBreakLeft.CompareTo(rawStart) > 0)
                {
                    // Truncate the right half of the input range.
                    end = wordBreakLeft;
                }
                else if (wordBreakRight.CompareTo(rawEnd) < 0)
                {
                    // Truncate the left half of the input range.
                    start = wordBreakRight;
                }
                else
                {
                    // The entire dirty range is covered by the caret word.
                    // Try to find a following dirty range.
                    GetNextScanRangeRaw(wordBreakRight, out start, out end);
                }

                // Schedule a future callback to deal with the skipped
                // overlapping section.
                ScheduleCaretMovedCallback();
            }
        }

        // Analyzes a run of text.  The scan may be interrupted if we run out
        // of time along the way, in which case some subset of the contained
        // words will be left dirty.
        private ScanStatus ScanRange(ITextPointer start, ITextPointer end, long timeLimit)
        {
            ITextPointer contextStart;
            ITextPointer contextEnd;
            ITextPointer contentStart;
            ITextPointer contentEnd;
            TextMap textMap;
            ScanStatus status;

            //
            // IMPORTANT: the scan logic here (word break expansion, TextMap creation, etc.)
            // must match GetSuggestionForError exactly.  Keep the methods in sync!
            //

            //
            // Expand the content to include whole words.
            // Also get pointers to sufficient surrounding text to analyze
            // multi-word errors correctly.
            //

            status = new ScanStatus(timeLimit, start);

            XmlLanguage language;
            CultureInfo culture = GetCurrentCultureAndLanguage(start, out language);

            if (culture == null)
            {
                // Someone set a bogus language on the run -- ignore it.
                _statusTable.MarkCleanRange(start, end);
            }
            else
            {
                SetCulture(culture);

                ExpandToWordBreakAndContext(start, LogicalDirection.Backward, language, out contentStart, out contextStart);
                ExpandToWordBreakAndContext(end, LogicalDirection.Forward, language, out contentEnd, out contextEnd);

                Invariant.Assert(contentStart.CompareTo(contentEnd) < 0);
                Invariant.Assert(contextStart.CompareTo(contextEnd) < 0);
                Invariant.Assert(contentStart.CompareTo(contextStart) >= 0);
                Invariant.Assert(contentEnd.CompareTo(contextEnd) <= 0);

                //
                // Mark the range clean, before we scan for errors.
                //
                _statusTable.MarkCleanRange(contentStart, contentEnd);

                //
                // Read the text.
                //

                // Check for a compatible language.
                if (_spellerInterop.CanSpellCheck(culture))
                {
                    // Find spelling errors, but we don't need suggestions
                    _spellerInterop.Mode = SpellerInteropBase.SpellerMode.SpellingErrors;

                    textMap = new TextMap(contextStart, contextEnd, contentStart, contentEnd);

                    //
                    // Iterate over sentences and segments.
                    //

                    _spellerInterop.EnumTextSegments(textMap.Text, textMap.TextLength, new SpellerInteropBase.EnumSentencesCallback(ScanRangeCheckTimeLimitCallback),
                        new SpellerInteropBase.EnumTextSegmentsCallback(ScanTextSegment), new TextMapCallbackData(textMap, status));

                    if (status.TimeoutPosition != null)
                    {
                        if (status.TimeoutPosition.CompareTo(end) < 0)
                        {
                            // We ran out of time before analyzing the whole block.
                            // Reset the dirty status of the remainder.
                            _statusTable.MarkDirtyRange(status.TimeoutPosition, end);
                            // We should always make some forward progress, even just one word,
                            // otherwise we'll never finish checking the document.
                            if (status.TimeoutPosition.CompareTo(start) <= 0)
                            {
                                // Diagnostic info for bug 1577085.
                                string debugMessage = "Speller is not advancing! \n" +
                                                      "Culture = " + culture + "\n" +
                                                      "Start offset = " + start.Offset + " parent = " + start.ParentType.Name + "\n" +
                                                      "ContextStart offset = " + contextStart.Offset + " parent = " + contextStart.ParentType.Name + "\n" +
                                                      "ContentStart offset = " + contentStart.Offset + " parent = " + contentStart.ParentType.Name + "\n" +
                                                      "ContentEnd offset = " + contentEnd.Offset + " parent = " + contentEnd.ParentType.Name + "\n" +
                                                      "ContextEnd offset = " + contextEnd.Offset + " parent = " + contextEnd.ParentType.Name + "\n" +
                                                      "Timeout offset = " + status.TimeoutPosition.Offset + " parent = " + status.TimeoutPosition.ParentType.Name + "\n" +
                                                      "textMap TextLength = " + textMap.TextLength + " text = " + new string(textMap.Text) + "\n" +
                                                      "Document = " + start.TextContainer.Parent.GetType().Name + "\n";

                                if (start is TextPointer)
                                {
                                    debugMessage += "Xml = " + new TextRange((TextPointer)start.TextContainer.Start, (TextPointer)start.TextContainer.End).Xml;
                                }

                                Invariant.Assert(false, debugMessage);
                            }
                        }
                        else
                        {
                            // We ran of time but finished the whole block.
                            // TimeoutPosition should never be past contentEnd.
                            // It might be less than contentEnd if the dirty run ends
                            // with an element edge, in which case TimeoutPosition
                            // will preceed the final element edge(s).
                            Invariant.Assert(status.TimeoutPosition.CompareTo(contentEnd) <= 0);
                        }
                    }
                }
            }

            return status;
        }

        // Callback for the error segment scanned during error lookup.
        // Returns a list of correction suggestions.
        private bool ScanErrorTextSegment(SpellerInteropBase.ISpellerSegment textSegment, object o)
        {
            TextMapCallbackData data = (TextMapCallbackData)o;
            SpellerInteropBase.ITextRange sTextRange = textSegment.TextRange;

            // Check if this segment falls outside the content range.
            // The region before/after the content is only for context --
            // to handle multi-word errors correctly.  We never want to mark it.
            if (sTextRange.Start + sTextRange.Length <= data.TextMap.ContentStartOffset)
            {
                // Preceeding context, skip this segment and keep going.
                return true;
            }

            if (sTextRange.Start >= data.TextMap.ContentEndOffset)
            {
                // Following context, skip this segment and stop iterating any remainder.
                return false;
            }

            if (sTextRange.Length > 1) // Ignore single letter errors
            {
                if (textSegment.SubSegments.Count == 0)
                {
                    ArrayList suggestions = (ArrayList)data.Data;
                    if(textSegment.Suggestions.Count > 0)
                    {
                        foreach(string suggestion in textSegment.Suggestions)
                        {
                            suggestions.Add(suggestion);
                        }
                    }
                }
                else
                {
                    textSegment.EnumSubSegments(new SpellerInteropBase.EnumTextSegmentsCallback(ScanErrorTextSegment), data);
                }
            }

            // We only expect one error segment for this callback, so skip any
            // following context segments.
            return false;
        }

        // Scans a single segment (the engine's formal notion of a "word").
        // Called indirectly by ScanRange.
        // Returns true to continue the segment enumeration, false to
        // break out of the iteration.
        private bool ScanTextSegment(SpellerInteropBase.ISpellerSegment textSegment, object o)
        {
            TextMapCallbackData data = (TextMapCallbackData)o;
            SpellerInteropBase.ITextRange sTextRange = textSegment.TextRange;
            char[] word;

            // Check if this segment falls outside the content range.
            // The region before/after the content is only for context --
            // to handle multi-word errors correctly.  We never want to mark it.
            if (sTextRange.Start + sTextRange.Length <= data.TextMap.ContentStartOffset)
            {
                // Preceeding context, skip this segment and keep going.
                return true;
            }
            if (sTextRange.Start >= data.TextMap.ContentEndOffset)
            {
                // Following context, skip this segment and stop iterating any remainder.
                return false;
            }

            if (sTextRange.Length > 1) // Ignore single letter errors.
            {
                // Check if the segment has been marked "ignore" by the user.
                word = new char[sTextRange.Length];
                Array.Copy(data.TextMap.Text, sTextRange.Start, word, 0, sTextRange.Length);

                if (!IsIgnoredWord(word))
                {
                    if(!textSegment.IsClean)
                    {
                        if (textSegment.SubSegments.Count == 0)
                        {
                            // We have an error.
                            MarkErrorRange(data.TextMap, sTextRange);
                        }
                        else
                        {
                            // We have a subsegment with an error.
                            textSegment.EnumSubSegments(new SpellerInteropBase.EnumTextSegmentsCallback(ScanTextSegment), data);
                        }
                    }
                }
            }

            return true;
        }

        // Called after we've scanned all the segments within a sentence from inside ScanRange.
        // This method returns false to the enumerator to end the scan if we've run out of time.
        //
        // NB: we terminate the segment enumeration run by ScanRange only at sentence boundaries,
        // not at more finely grained segment boundaries.  This is because the vast amount of
        // work done takes place when preparing to iterate segments, so the incremental cost
        // of actually walking the segments once calculated is ignorable, but halting the scan
        // after looking at a single segment would mean repeating the overhead on all segments
        // in the sentence.
        private bool ScanRangeCheckTimeLimitCallback(SpellerInteropBase.ISpellerSentence sentence, object o)
        {
            TextMapCallbackData data = (TextMapCallbackData)o;
            ScanStatus status = (ScanStatus)data.Data;

            // Stop iterating if we exceed our time budget.
            // In which case, take note of where we left off.
            if (status.HasExceededTimeLimit)
            {
                Invariant.Assert(status.TimeoutPosition == null); // We should only set this once....

                int sentenceEndOffset = sentence.EndOffset;

                if (sentenceEndOffset >= 0)
                {
                    // The end of this segment may extend past textMap.ContentEndOffset,
                    // in the case of multi-word errors.  So truncate.
                    // Need to handle MWEs extending past content end consistently.
                    int timeOutOffset = Math.Min(data.TextMap.ContentEndOffset, sentenceEndOffset);

                    // Be careful not to stop the iteration if we haven't reached
                    // the content start yet.  It's possible that the context text
                    // will extend backwards into another sentence.  We must always
                    // make forward progress, even if doing so exceeds the timeout
                    // limit, otherwise we'll get stuck infinitely eating idle time.
                    //
                    //  we could remove this check if we
                    // change the ExpandToWordBreakAndContext logic to never extend
                    // outside the original content sentence.  We never need
                    // context outside the start/end sentence, it can't be part of
                    // a multi-word error.
                    if (timeOutOffset > data.TextMap.ContentStartOffset)
                    {
                        ITextPointer timeoutPosition = data.TextMap.MapOffsetToPosition(timeOutOffset);

                        // even if the offset has advanced past the content-start,
                        // the text position may not have advanced past the original
                        // starting position.  (This has been observed when resuming
                        // a scan after a timeout near a Hyperlink.  The second scan
                        // effectively repeats the work of the first, after backing
                        // up the context, and times out in the same place as the
                        // first, thus making no progress.)
                        // Ignore the time limit if no progress has been made.
                        if (timeoutPosition.CompareTo(status.StartPosition) > 0)
                        {
                            status.TimeoutPosition = timeoutPosition;
                        }
                    }
                }
            }

            return (status.TimeoutPosition == null);
        }

        // Flags a run of text with an error.
        // In two exceptional circumstances we schedule an idle-time callback
        // to re-analyze the run instead of marking it:
        // - when the caret is within the error text.
        // - when an IME composition covers the text.
        private void MarkErrorRange(TextMap textMap, SpellerInteropBase.ITextRange sTextRange)
        {
            ITextPointer errorStart;
            ITextPointer errorEnd;

            if (sTextRange.Start + sTextRange.Length > textMap.ContentEndOffset)
            {
                // We found an error that starts in the content but extends into
                // the context.  This must be a multi-word error.
                // For now, ignore it.
                // Need to handle MWEs that cross into context.
                return;
            }

            errorStart = textMap.MapOffsetToPosition(sTextRange.Start);
            errorEnd = textMap.MapOffsetToPosition(sTextRange.Start + sTextRange.Length);

            if (sTextRange.Start < textMap.ContentStartOffset)
            {
                Invariant.Assert(sTextRange.Start + sTextRange.Length > textMap.ContentStartOffset);

                // We've found an error that start in the context and extends into
                // the content.  This can happen as more text is revealed to the
                // speller engine as the caret moves forward.
                // E.g., while scanning "avalon's" we flag an error over "avalon",
                // ignoring the "'s" because the caret is positioned within that segment.
                // Then, the user hits space and now we analyze "'s" along with its
                // preceding context "avalon".  In this final scan, "avalon's" as a while
                // is flagged as an error and we enter this if statement.

                // We must mark the range clean before we can mark it dirty.
                // _statusTable.MarkErrorRange can only handle clean runs.
                _statusTable.MarkCleanRange(errorStart, errorEnd);
            }

            _statusTable.MarkErrorRange(errorStart, errorEnd);
        }

        // Examines a position and returns to two relative positions:
        //
        // contentPosition -> position moved inward to the nearest word
        // break (opposite direction param, toward the content).
        //
        // contextPosition -> position moved outward away from content
        // to a word break that includes sufficient text to handle multi-
        // word errors correctly.
        private void ExpandToWordBreakAndContext(ITextPointer position, LogicalDirection direction, XmlLanguage language,
            out ITextPointer contentPosition, out ITextPointer contextPosition)
        {
            ITextPointer start;
            ITextPointer end;
            ITextPointer outwardPosition;
            ITextPointer inwardPosition;
            TextMap textMap;
            ArrayList segments;
            SpellerInteropBase.ITextRange sTextRange;
            LogicalDirection inwardDirection;
            int i;

            contentPosition = position;
            contextPosition = position;

            if (position.GetPointerContext(direction) == TextPointerContext.None)
            {
                // There is no following context, we're at document start/end.
                return;
            }

            // Disable spell checking functionality since we're only
            // interested in word breaks here.  This greatly cuts down
            // the engine's workload.
            _spellerInterop.Mode = SpellerInteropBase.SpellerMode.WordBreaking;

            //
            // Build an array of wordbreak offsets surrounding the position.
            //

            // 1. Search outward, into surrounding text.  We need MinWordBreaksForContext
            // word breaks to handle multi-word errors.
            outwardPosition = SearchForWordBreaks(position, direction, language, MinWordBreaksForContext, true /* stopOnError */);

            // 2. Search inward, towards content.  We just need one word break inward.
            inwardDirection = direction == LogicalDirection.Forward ? LogicalDirection.Backward : LogicalDirection.Forward;
            inwardPosition = SearchForWordBreaks(position, inwardDirection, language, 1, false /* stopOnError */);

            // Get combined word breaks.  This may not be the same as we calculated
            // in two parts above, since we don't know yet whether or not position is
            // on a word break.
            if (direction == LogicalDirection.Backward)
            {
                start = outwardPosition;
                end = inwardPosition;
            }
            else
            {
                start = inwardPosition;
                end = outwardPosition;
            }
            textMap = new TextMap(start, end, position, position);
            segments = new ArrayList(MinWordBreaksForContext + 1);
            _spellerInterop.EnumTextSegments(textMap.Text, textMap.TextLength, null,
                new SpellerInteropBase.EnumTextSegmentsCallback(ExpandToWordBreakCallback), segments);

            //
            // Use our table of word breaks to calculate context and content positions.
            //
            if (segments.Count == 0)
            {
                // No segments.  This can happen if position is surrounded by
                // nothing but white space.  We've already initialized contentPosition
                // and contextPosition so there's nothing to do.
            }
            else
            {
                int leftWordBreak;
                int rightWordBreak;
                int contentOffset;
                int contextOffset;

                // Figure out where position lives in the segment list.
                i = FindPositionInSegmentList(textMap, direction, segments, out leftWordBreak, out rightWordBreak);

                // contentPosition should be an edge on the segment we found.
                if (direction == LogicalDirection.Backward)
                {
                    contentOffset = textMap.ContentStartOffset == rightWordBreak ? rightWordBreak : leftWordBreak;
                }
                else
                {
                    contentOffset = textMap.ContentStartOffset == leftWordBreak ? leftWordBreak : rightWordBreak;
                }

                // See <summary> section in the doc comments of TextMapOffsetErrorLogger for details on
                // what is being logged and why.
                var errorLogger = new TextMapOffsetErrorLogger(direction, textMap, segments, i, leftWordBreak, rightWordBreak, contentOffset);
                errorLogger.LogDebugInfo();

                contentPosition = textMap.MapOffsetToPosition(contentOffset);

                // contextPosition should be MinWordBreaksForContext - 1 words away.
                if (direction == LogicalDirection.Backward)
                {
                    i -= (MinWordBreaksForContext - 1);
                    sTextRange = (SpellerInteropBase.ITextRange)segments[Math.Max(i, 0)];
                    // We might actually follow contentOffset if we're at the document edge.
                    // Don't let that happen.
                    contextOffset = Math.Min(sTextRange.Start, contentOffset);
                }
                else
                {
                    i += MinWordBreaksForContext;
                    sTextRange = (SpellerInteropBase.ITextRange)segments[Math.Min(i, segments.Count-1)];
                    // We might actually preceed contentOffset if we're at the document edge.
                    // Don't let that happen.
                    contextOffset = Math.Max(sTextRange.Start + sTextRange.Length, contentOffset);
                }

                errorLogger.ContextOffset = contextOffset;
                errorLogger.LogDebugInfo();

                contextPosition = textMap.MapOffsetToPosition(contextOffset);
            }

            // Final fixup: if the dirty range covers only formatting (which is not passed
            // to the speller engine) then we might actually "expand" in the wrong
            // direction, since the TextMap will jump over formatting.
            // Backup if necessary.
            if (direction == LogicalDirection.Backward)
            {
                if (position.CompareTo(contentPosition) < 0)
                {
                    contentPosition = position;
                }
                if (position.CompareTo(contextPosition) < 0)
                {
                    contextPosition = position;
                }
            }
            else
            {
                if (position.CompareTo(contentPosition) > 0)
                {
                    contentPosition = position;
                }
                if (position.CompareTo(contextPosition) > 0)
                {
                    contextPosition = position;
                }
            }
        }

        // Helper for ExpandToWordBreakAndContext -- returns the index of a segment
        // containing or bordering a specified position (textMap.ContentStartOffset).
        // Also returns the offset, within the TextMap, of the two word breaks surrounding
        // TextMap.ContentStartOffset.  The word breaks may be segment edges, or
        // the extent of a run of whitespace between two segments.
        private int FindPositionInSegmentList(TextMap textMap, LogicalDirection direction, ArrayList segments,
            out int leftWordBreak, out int rightWordBreak)
        {
            SpellerInteropBase.ITextRange sTextRange;
            int index;

            // Make the compiler happy by initializing the out's to bogus values.
            leftWordBreak = Int32.MaxValue;
            rightWordBreak = -1;

            // Check before the first segment, which start at the first
            // non-whitespace char.
            sTextRange = (SpellerInteropBase.ITextRange)segments[0];
            if (textMap.ContentStartOffset < sTextRange.Start)
            {
                leftWordBreak = 0;
                rightWordBreak = sTextRange.Start;
                index = -1;
            }
            else
            {
                // Check after the last segment, which does not include final whitespace.
                sTextRange = (SpellerInteropBase.ITextRange)segments[segments.Count - 1];
                if (textMap.ContentStartOffset > sTextRange.Start + sTextRange.Length)
                {
                    leftWordBreak = sTextRange.Start + sTextRange.Length;
                    rightWordBreak = textMap.TextLength;
                    index = segments.Count;
                }
                else
                {
                    // Walk the segment list, checking each segment and space in between.
                    for (index = 0; index < segments.Count; index++)
                    {
                        sTextRange = (SpellerInteropBase.ITextRange)segments[index];

                        leftWordBreak = sTextRange.Start;
                        rightWordBreak = sTextRange.Start + sTextRange.Length;

                        // Check if we're inside this segment.
                        if (leftWordBreak <= textMap.ContentStartOffset &&
                            rightWordBreak >= textMap.ContentStartOffset)
                        {
                            break;
                        }
                        // Or if we're between this segment and the next one --
                        // segments do not include white space.
                        if (index < segments.Count - 1 &&
                            rightWordBreak < textMap.ContentStartOffset)
                        {
                            sTextRange = (SpellerInteropBase.ITextRange)segments[index + 1];
                            leftWordBreak = rightWordBreak;
                            rightWordBreak = sTextRange.Start;

                            if (rightWordBreak > textMap.ContentStartOffset)
                            {
                                // position is between segments[i] and segments[i+1].
                                // Adjust i so that adding MinWordBreaksForContext below
                                // doesn't include an extra word.
                                if (direction == LogicalDirection.Backward)
                                {
                                    index++;
                                }
                                break;
                            }
                        }
                    }
                }
            }

            Invariant.Assert(leftWordBreak <= textMap.ContentStartOffset && textMap.ContentStartOffset <= rightWordBreak);

            return index;
        }

        // Helper for ExpandToWordBreakAndContext -- returns the position
        // of the nth word break in the specified direction.
        // If stopOnError is true, the search will halt if an error run is
        // encountered along the way.  The search is also halted if text in
        // a new language is encountered.
        private ITextPointer SearchForWordBreaks(ITextPointer position, LogicalDirection direction, XmlLanguage language, int minWordCount, bool stopOnError)
        {
            ITextPointer closestErrorPosition;
            ITextPointer searchPosition;
            ITextPointer start;
            ITextPointer end;
            StaticTextPointer nextErrorTransition;
            int segmentCount;
            TextMap textMap;

            searchPosition = position.CreatePointer();

            closestErrorPosition = null;
            if (stopOnError)
            {
                nextErrorTransition = _statusTable.GetNextErrorTransition(position.CreateStaticPointer(), direction);
                if (!nextErrorTransition.IsNull)
                {
                    closestErrorPosition = nextErrorTransition.CreateDynamicTextPointer(LogicalDirection.Forward);
                }
            }

            bool hitBreakPoint = false;

            do
            {
                searchPosition.MoveByOffset(direction == LogicalDirection.Backward ? -ContextBlockSize : +ContextBlockSize);

                // Don't go past closestErrorPosition.
                if (closestErrorPosition != null)
                {
                    if (direction == LogicalDirection.Backward && closestErrorPosition.CompareTo(searchPosition) > 0 ||
                        direction == LogicalDirection.Forward && closestErrorPosition.CompareTo(searchPosition) < 0)
                    {
                        searchPosition.MoveToPosition(closestErrorPosition);
                        hitBreakPoint = true;
                    }
                }

                // Don't venture into text in another language.
                ITextPointer closestLanguageTransition = GetNextLanguageTransition(position, direction, language, searchPosition);

                if (direction == LogicalDirection.Backward && closestLanguageTransition.CompareTo(searchPosition) > 0 ||
                    direction == LogicalDirection.Forward && closestLanguageTransition.CompareTo(searchPosition) < 0)
                {
                    searchPosition.MoveToPosition(closestLanguageTransition);
                    hitBreakPoint = true;
                }

                if (direction == LogicalDirection.Backward)
                {
                    start = searchPosition;
                    end = position;
                }
                else
                {
                    start = position;
                    end = searchPosition;
                }

                textMap = new TextMap(start, end, start, end);
                segmentCount = _spellerInterop.EnumTextSegments(textMap.Text, textMap.TextLength, null, null, null);
            }
            while (!hitBreakPoint &&
                   segmentCount < minWordCount + 1 &&
                   searchPosition.GetPointerContext(direction) != TextPointerContext.None);

            return searchPosition;
        }

        // Returns the closest of either a halting position or the position preceding text
        // tagged with a differing XmlLanguage from a start position.
        private ITextPointer GetNextLanguageTransition(ITextPointer position, LogicalDirection direction, XmlLanguage language, ITextPointer haltPosition)
        {
            ITextPointer navigator = position.CreatePointer();

            while ((direction == LogicalDirection.Forward && navigator.CompareTo(haltPosition) < 0) ||
                   (direction == LogicalDirection.Backward && navigator.CompareTo(haltPosition) > 0))
            {
                if (GetCurrentLanguage(navigator) != language)
                    break;

                navigator.MoveToNextContextPosition(direction);
            }

            // If we moved past haltPosition on the final MoveToNextContextPosition, move back.
            if ((direction == LogicalDirection.Forward && navigator.CompareTo(haltPosition) > 0) ||
                (direction == LogicalDirection.Backward && navigator.CompareTo(haltPosition) < 0))
            {
                navigator.MoveToPosition(haltPosition);
            }

            return navigator;
        }

        // Called indirectly by ExpandToWordBreakAndContext while iterating segments.
        // Builds up an array of segment offsets while iterating.
        private bool ExpandToWordBreakCallback(SpellerInteropBase.ISpellerSegment textSegment, object o)
        {
            ArrayList segments = (ArrayList)o;

            segments.Add(textSegment.TextRange);

            return true;
        }

        // Returns true if a user has tagged the specified word with "Ignore All".
        private bool IsIgnoredWord(char[] word)
        {
            bool isIgnoredWord = false;

            if (_ignoredWordsList != null)
            {
                isIgnoredWord = _ignoredWordsList.BinarySearch(new string(word), new CaseInsensitiveComparer(_defaultCulture)) >= 0;
            }

            return isIgnoredWord;
        }

        // Returns true if we have an engine capable of proofing the specified
        // language.
        private static bool CanSpellCheck(CultureInfo culture)
        {
            bool canSpellCheck;

            switch (culture.TwoLetterISOLanguageName)
            {
                case "en":
                case "de":
                case "fr":
                case "es":
                    canSpellCheck = true;
                    break;

                default:
                    canSpellCheck = false;
                    break;
            }

            return canSpellCheck;
        }

        // Sets the speller engine language and spelling reform options.
        private void SetCulture(CultureInfo culture)
        {
            //
            // Set the language.
            //

            _spellerInterop.SetLocale(culture);

            //
            // Set spelling reform, if necessary.
            //

            _spellerInterop.SetReformMode(culture, _spellingReform);
        }

        // Scans the word containing a specified character.
        private void ScanPosition(ITextPointer position, LogicalDirection direction)
        {
            ITextPointer start;
            ITextPointer end;

            if (direction == LogicalDirection.Forward)
            {
                start = position;
                end = position.CreatePointer(+1);
            }
            else
            {
                start = position.CreatePointer(-1);
                end = position;
            }

            ScanRange(start, end, Int64.MaxValue /* timeLimit */);
        }

        // Returns the XmlLanguage of the parent element at position.
        private XmlLanguage GetCurrentLanguage(ITextPointer position)
        {
            XmlLanguage language;

            GetCurrentCultureAndLanguage(position, out language);

            return language;
        }

        // Returns the CultureInfo of the content at a position.
        // Returns null if there is no CultureInfo matching the current XmlLanguage.
        private CultureInfo GetCurrentCultureAndLanguage(ITextPointer position, out XmlLanguage language)
        {
            CultureInfo cultureInfo;
            bool hasModifiers;

            // TextBox takes the input language iff no local LanguageProperty is set.
            if (!_textEditor.AcceptsRichContent &&
                _textEditor.UiScope.GetValueSource(FrameworkElement.LanguageProperty, null, out hasModifiers) == BaseValueSourceInternal.Default)
            {
                cultureInfo = _defaultCulture;
                language = XmlLanguage.GetLanguage(cultureInfo.IetfLanguageTag);
            }
            else
            {
                language = (XmlLanguage)position.GetValue(FrameworkElement.LanguageProperty);

                if (language == null)
                {
                    cultureInfo = null;
                }
                else
                {
                    try
                    {
                        cultureInfo = language.GetSpecificCulture();
                    }
                    catch (InvalidOperationException)
                    {
                        // Someone set a bogus language on the run.
                        cultureInfo = null;
                    }
                }
            }

            return cultureInfo;
        }

        /// <summary>
        /// If give Uri is relative, creates full path by appending current directory.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static Uri ResolvePathUri(Uri uri)
        {
            Uri fileUri;
            if (!uri.IsAbsoluteUri)
            {
                fileUri = new Uri(new Uri(Directory.GetCurrentDirectory() + "/"), uri);
            }
            else
            {
                fileUri = uri;
            }

            return fileUri;
        }

        /// <summary>
        /// Loads dictionary specified by a pack URI.
        /// Creates a temprorary file, copies dictionary data referenced by pack URI
        /// into a temp file and loads the temp file as a dictionary.
        /// </summary>
        /// <param name="item"></param>
        private void LoadDictionaryFromPackUri(Uri item)
        {
            string tempFolder;
            Uri tempLocationUri;

            tempLocationUri = LoadPackFile(item);
            tempFolder = System.IO.Path.GetTempPath();

            try
            {
                object lexicon = _spellerInterop.LoadDictionary(tempLocationUri, tempFolder);
                UriMap.Add(item, new DictionaryInfo(tempLocationUri, lexicon));
            }
            finally
            {
                CleanupDictionaryTempFile(tempLocationUri);
            }
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="tempLocationUri"></param>
        private void CleanupDictionaryTempFile(Uri tempLocationUri)
        {
            if (tempLocationUri != null)
            {
                try
                {
                    System.IO.File.Delete(tempLocationUri.LocalPath);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.Write(string.Format(CultureInfo.InvariantCulture, "Failure to delete temporary file with custom dictionary data. file Uri:{0},exception:{1}", tempLocationUri.ToString(), e.ToString()));
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates a temp file and copies content of dictionary file referenced by the input
        /// <paramref name="uri"/> to the temp file.
        /// The caller is responsible for deleting the file.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns>Returns Uri corresponding to the newly created temp file.
        /// </returns>
        private static Uri LoadPackFile(Uri uri)
        {
            string tmpFilePath;
            Invariant.Assert(MS.Internal.IO.Packaging.PackUriHelper.IsPackUri(uri));
            Uri resolvedUri = MS.Internal.Utility.BindUriHelper.GetResolvedUri(BaseUriHelper.PackAppBaseUri, uri);
            using (Stream sourceStream = WpfWebRequestHelper.CreateRequestAndGetResponseStream(resolvedUri))
            {
                using (FileStream outputStream = FileHelper.CreateAndOpenTemporaryFile(out tmpFilePath, FileAccess.ReadWrite))
                {
                    sourceStream.CopyTo(outputStream);
                }
            }
            return new Uri(tmpFilePath);
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // Holds a run of text intended for the speller engine.
        // Because the engine only understands plain text, this class coverts
        // arbitrary runs of document text to speller-suitable plain text
        // and keeps a table that allows it to efficiently map back from plain
        // text offsets (used by the engine) to ITextPointers.
        private class TextMap
        {
            // Creates a new instance.
            // contextStart/End refer to the whole run of text.
            // contentStart/End are a subset of the text, which is what
            // the engine will actually tag with errors.
            // The space between context and content is used by the engine
            // to correctly analyze multiple word phrase like "Los Angeles"
            // that could otherwise be truncated and incorrectly tagged.
            internal TextMap(ITextPointer contextStart, ITextPointer contextEnd,
                ITextPointer contentStart, ITextPointer contentEnd)
            {
                ITextPointer position;
                int maxChars;
                int inlineCount;
                int runCount;
                int i;
                int distance;

                Invariant.Assert(contextStart.CompareTo(contentStart) <= 0);
                Invariant.Assert(contextEnd.CompareTo(contentEnd) >= 0);

                _basePosition = contextStart.GetFrozenPointer(LogicalDirection.Backward);

                position = contextStart.CreatePointer();
                maxChars = contextStart.GetOffsetToPosition(contextEnd);

                _text = new char[maxChars];
                _positionMap = new int[maxChars+1];

                _textLength = 0;
                inlineCount = 0;

                _contentStartOffset = 0;
                _contentEndOffset = 0;

                // Iterate over the run, building up a matching plain text buffer
                // and a table that tells us how to map back to the original text.
                while (position.CompareTo(contextEnd) < 0)
                {
                    if (position.CompareTo(contentStart) == 0)
                    {
                        _contentStartOffset = _textLength;
                    }
                    if (position.CompareTo(contentEnd) == 0)
                    {
                        _contentEndOffset = _textLength;
                    }

                    switch (position.GetPointerContext(LogicalDirection.Forward))
                    {
                        case TextPointerContext.Text:
                            runCount = position.GetTextRunLength(LogicalDirection.Forward);
                            runCount = Math.Min(runCount, _text.Length - _textLength);
                            runCount = Math.Min(runCount, position.GetOffsetToPosition(contextEnd));

                            position.GetTextInRun(LogicalDirection.Forward, _text, _textLength, runCount);

                            for (i = _textLength; i < _textLength + runCount; i++)
                            {
                                _positionMap[i] = i + inlineCount;
                            }

                            distance = position.GetOffsetToPosition(contentStart);
                            if (distance >= 0 && distance <= runCount)
                            {
                                _contentStartOffset = _textLength + position.GetOffsetToPosition(contentStart);
                            }
                            distance = position.GetOffsetToPosition(contentEnd);
                            if (distance >= 0 && distance <= runCount)
                            {
                                _contentEndOffset = _textLength + position.GetOffsetToPosition(contentEnd);
                            }

                            position.MoveByOffset(runCount);
                            _textLength += runCount;
                            break;

                        case TextPointerContext.ElementStart:
                        case TextPointerContext.ElementEnd:
                            if (IsAdjacentToFormatElement(position))
                            {
                                // Filter out formatting tags from the plain text.
                                inlineCount++;
                            }
                            else
                            {
                                // Stick in a word break to account for the block element.
                                _text[_textLength] = ' ';
                                _positionMap[_textLength] = _textLength + inlineCount;
                                _textLength++;
                            }
                            position.MoveToNextContextPosition(LogicalDirection.Forward);
                            break;

                        case TextPointerContext.EmbeddedElement:
                            _text[_textLength] = '\xf8ff'; // Unicode private use.
                            _positionMap[_textLength] = _textLength + inlineCount;
                            _textLength++;

                            position.MoveToNextContextPosition(LogicalDirection.Forward);
                            break;
                    }
                }

                if (position.CompareTo(contentEnd) == 0)
                {
                    _contentEndOffset = _textLength;
                }

                if (_textLength > 0)
                {
                    _positionMap[_textLength] = _positionMap[_textLength - 1] + 1;
                }
                else
                {
                    _positionMap[0] = 0;
                }

                Invariant.Assert(_contentStartOffset <= _contentEndOffset);
            }

            // Returns an ITextPointer in the document with position matching
            // an offset within the plain text.
            internal ITextPointer MapOffsetToPosition(int offset)
            {
                Invariant.Assert(offset >= 0 && offset <= _textLength);

                return _basePosition.CreatePointer(_positionMap[offset]);
            }

            // Offset in the plain text of the content start.
            internal int ContentStartOffset
            {
                get { return _contentStartOffset; }
            }

            // Offset in the plain text of the content end.
            internal int ContentEndOffset
            {
                get { return _contentEndOffset; }
            }

            // Plain text representation of the document run.
            // Do not use Text.Length!  The actual content size may be smaller,
            // use the TextLength property instead.
            internal char[] Text
            {
                get { return _text; }
            }

            // Length of the plain text.  This may be less than Text.Length --
            // we allocate a maximum value for the array which is not always used
            // by the final content.
            internal int TextLength
            {
                get { return _textLength; }
            }

            // Returns true if pointer preceeds an Inline start or end edge.
            private bool IsAdjacentToFormatElement(ITextPointer pointer)
            {
                TextPointerContext context;
                bool isAdjacentToFormatElement;

                isAdjacentToFormatElement = false;

                context = pointer.GetPointerContext(LogicalDirection.Forward);

                if (context == TextPointerContext.ElementStart &&
                    TextSchema.IsFormattingType(pointer.GetElementType(LogicalDirection.Forward)))
                {
                    isAdjacentToFormatElement = true;
                }
                else if (context == TextPointerContext.ElementEnd &&
                         TextSchema.IsFormattingType(pointer.ParentType))
                {
                    isAdjacentToFormatElement = true;
                }

                return isAdjacentToFormatElement;
            }

            // Position of the plain text block within the document.
            private readonly ITextPointer _basePosition;

            // Plain text version of the document run.
            private readonly char[] _text;

            // Map of plain text offsets to document symbol offsets relative
            // to _basePosition.
            private readonly int[] _positionMap;

            // Size of the content within _text.
            private readonly int _textLength;

            // Plain text offset of the content start.
            private readonly int _contentStartOffset;

            // Plain text offset of the content end.
            private readonly int _contentEndOffset;
        }

        // Holds state tracking the progress of speller scan,
        // used during idle time document analysis.
        private class ScanStatus
        {
            // Creates a new instance.  timeLimit is the maximum value of
            // DateTime.Now.Ticks at which the scan should end.
            internal ScanStatus(long timeLimit, ITextPointer startPosition)
            {
                _timeLimit = timeLimit;
                _startPosition = startPosition;
            }

            // Returns true if the scan has exceeded its time budget.
            internal bool HasExceededTimeLimit
            {
                get
                {
                    long nowTicks = DateTime.Now.Ticks;

#if DEBUG
                    // Track how far over budget we are the first time we check.
                    if (nowTicks >= _timeLimit && _debugMsOverTimeLimit == 0)
                    {
                        _debugMsOverTimeLimit = (int)(((double)(nowTicks - _timeLimit)) / 10000);
                    }
#endif // DEBUG

                    return nowTicks >= _timeLimit;
                }
            }

            // If we've timed out, holds the position we left off -- the remainder
            // of the text run yet to be analyzed.
            internal ITextPointer TimeoutPosition
            {
                get { return _timeoutPosition; }
                set { _timeoutPosition = value; }
            }

            // starting text position - scan must advance past this
            internal ITextPointer StartPosition
            {
                get { return _startPosition; }
            }

            // Budget for this scan, in 100 nanosecond intervals.
            private readonly long _timeLimit;

            // starting text position - scan must advance past this
            private readonly ITextPointer _startPosition;

            // If we've timed out, holds the position we left off -- the remainder
            // of the text run yet to be analyzed.
            private ITextPointer _timeoutPosition;

#if DEBUG
            // Number of milliseconds we've exceeded our time limit by.
            private int _debugMsOverTimeLimit;
#endif // DEBUG
        }

        // Container used to hold state for SpellerInterop callbacks.
        private class TextMapCallbackData
        {
            internal TextMapCallbackData(TextMap textmap, object data)
            {
                _textmap = textmap;
                _data = data;
            }

            internal TextMap TextMap { get { return _textmap; } }

            internal object Data { get { return _data; } }

            private readonly TextMap _textmap;
            private readonly object _data;
        }

        /// <summary>
        /// Holds custom dicitonary related data.
        /// </summary>
        private class DictionaryInfo
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="pathUri"></param>
            /// <param name="lexicon"></param>
            internal DictionaryInfo(Uri pathUri, object lexicon)
            {
                _pathUri = pathUri;
                _lexicon = lexicon;
            }

            internal Uri PathUri
            {
                get
                {
                    return _pathUri;
                }
            }

            internal object Lexicon
            {
                get
                {
                    return _lexicon;
                }
            }

            /// <summary>
            /// </summary>
            private readonly object _lexicon;

            /// <summary>
            /// File location where custom dictionary is loaded from .
            /// </summary>
            private readonly Uri _pathUri;
        }

        #endregion Private Types

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Max time slice for speller background proofing, in milliseconds.
        // Larger numbers mean we can scan entire documents more quickly,
        // but with less responsiveness to user interruptions.
        private const int MaxIdleTimeSliceMs = 20;
        // Max time slice for speller background proofing, in 100 nanosecond intervals.
        private const long MaxIdleTimeSliceNs = MaxIdleTimeSliceMs*10000;

        // Max number of characters to pass to engine in a single call.
        // Increasing this number will decrease the time to scan an entire
        // document, at the cost of more time spent in individual Idle callbacks
        // (app is less respsonsive).
        // NLG devs have warned us not to set this value below 32, to avoid
        // cases where they don't have enough context to identify errors
        // correctly.
        private const int MaxScanBlockSize = 64;

        // Number of characters to advance on each iteration while searching
        // for context.  We need at least three words on either side of a text
        // run to give the speller enough context to identify multi-word
        // errors (or non-errors, like Los Angeles).
        //
        // Larger numbers here will mean fewer text scans, but a smaller
        // minimum scan.
        private const int ContextBlockSize = 32;

        // The minimum number of word breaks we need to have sufficient
        // context for detecting multi-word errors.  This number is
        // a constant -- it is not something to adjust for perf.
        private const int MinWordBreaksForContext = 4;

        // TextEditor that owns this Speller.
        private TextEditor _textEditor;

        // A run-length array tracking speller status of all text in the document.
        private SpellerStatusTable _statusTable;

        // HighlightLayer used to display error squiggles.
        private SpellerHighlightLayer _highlightLayer;

        // Engine object used to analyze runs of text.
        // kepowell from the nlg team suggests that we cache a single
        // ITextChunk/ITextContext for the thread and reuse it across
        // Spellers.  FE TextChunks in particular are expensive, because
        // they cache large amounts of data per instance, on the order
        // of 10k's of data.
        private SpellerInteropBase _spellerInterop;

        // Current spelling reform setting.
        private SpellingReform _spellingReform;

        // true if we've already posted but not yet received a background queue item.
        private bool _pendingIdleCallback;

        // true if we have an active TextSelection.Changed listener.
        private bool _pendingCaretMovedCallback;

        // List of words tagged by the user as non-errors.
        private ArrayList _ignoredWordsList;

        // The CultureInfo associated with this speller.
        // Used for ignored words comparison, and plain text controls (TextBox).
        private readonly CultureInfo _defaultCulture;

        // Set true if the nl6 library is unavailable.
        private bool _failedToInit;

        /// <summary>
        /// Holds mapping between original Uri passed in by user and COM reference to the loaded dictionary.
        /// This dictionary MUST NOT contain any null items.
        /// </summary>
        private Dictionary<Uri, DictionaryInfo> _uriMap;

        #endregion Private Fields
    }
}

