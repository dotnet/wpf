// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Encapsulates ISpellChecker services exposed by
//              MsSpellCheckLib.RCW and provides
//              resilience against out-of-proc COM server failures.
//

using System.ComponentModel;
using System.Runtime.InteropServices;
using ISpellChecker = System.Windows.Documents.MsSpellCheckLib.RCW.ISpellChecker;
using IEnumString = System.Windows.Documents.MsSpellCheckLib.RCW.IEnumString;
using ISpellCheckerChangedEventHandler = System.Windows.Documents.MsSpellCheckLib.RCW.ISpellCheckerChangedEventHandler;
using IOptionDescription = System.Windows.Documents.MsSpellCheckLib.RCW.IOptionDescription;
using IEnumSpellingError = System.Windows.Documents.MsSpellCheckLib.RCW.IEnumSpellingError;

namespace System.Windows.Documents
{
    namespace MsSpellCheckLib
    {
        /// <summary>
        /// This type encapsulates services provided by RCW.ISpellChecker interface and provides
        /// a resilient (to out-of-proc COM server failures) interface to callers.
        /// </summary>
        /// <remarks>
        /// The ISpellCheckerFactory and IUserDictionareisRegistrar methods are implemented using the following pattern.
        /// For a method Foo(), we see the following entries:
        ///
        ///     1. The most basic implementation of the method.
        ///         private FooImpl();
        ///
        ///     2. Some resilience added to the basic implementation. This calls into FooImpl repeatedly.
        ///         private FooImplWithRetries(bool shouldSuppressCOMExceptions);
        ///
        ///     3. Finally, the version that is exposed to callers.
        ///         public Foo(bool shouldSuppressCOMExceptions = true);
        /// </remarks>
        internal partial class SpellChecker : IDisposable
        {
            #region Constructor and Initialization

            public SpellChecker(string languageTag)
            {
                _speller = new ChangeNotifyWrapper<ISpellChecker>();
                _languageTag = languageTag;
                _spellCheckerChangedEventHandler = new SpellCheckerChangedEventHandler(this);

                if (Init(shouldSuppressCOMExceptions: false))
                {
                    _speller.PropertyChanged += SpellerInstanceChanged;
                }
            }

            private bool Init(bool shouldSuppressCOMExceptions = true)
            {
                _speller.Value = SpellCheckerFactory.CreateSpellChecker(_languageTag, shouldSuppressCOMExceptions);

                return (_speller.Value != null);
            }

            #endregion // Constructor and Initialization

            #region ISpellChecker services


            #region GetLanguageTage

            /// <remarks>
            ///     We really don't need to call into COM to get this
            ///     value since we cache it.
            /// </remarks>
            public string GetLanguageTag()
            {
                return _disposed ? null : _languageTag;
            }

            #endregion // GetLanguageTag


            #region Suggest

            public List<string> SuggestImpl(string word)
            {
                IEnumString suggestions = _speller.Value.Suggest(word);

                return
                    suggestions?.ToList(shouldSuppressCOMExceptions:false, shouldReleaseCOMObject:true);
            }

            public List<string> SuggestImplWithRetries(string word, bool shouldSuppressCOMExceptions = true)
            {
                List<string> result = null;
                bool callSucceeded =
                    RetryHelper.TryExecuteFunction(
                        func: () => { return SuggestImpl(word); },
                        result: out result,
                        preamble:  () => Init(shouldSuppressCOMExceptions),
                        ignoredExceptions: SuppressedExceptions[shouldSuppressCOMExceptions]);

                return callSucceeded ? result : null;
            }

            public List<string> Suggest(string word, bool shouldSuppressCOMExceptions = true)
            {
                return _disposed ? null : SuggestImplWithRetries(word, shouldSuppressCOMExceptions);
            }

            #endregion // Suggest

            #region Add

            private void AddImpl(string word)
            {
                _speller.Value.Add(word);
            }

            private void AddImplWithRetries(string word, bool shouldSuppressCOMExceptions = true)
            {
                // AddImpl and Init are SecuritySafeCritical, so it is okay to
                // create an anon. lambdas that calls into them, and pass
                // those lambdas below.
                RetryHelper.TryCallAction(
                    action: () => AddImpl(word),
                    preamble: () => Init(shouldSuppressCOMExceptions),
                    ignoredExceptions: SuppressedExceptions[shouldSuppressCOMExceptions]);
            }

            public void Add(string word, bool shouldSuppressCOMExceptions = true)
            {
                if (_disposed) return;

                AddImplWithRetries(word, shouldSuppressCOMExceptions);
            }

            #endregion // Add

            #region Ignore

            private void IgnoreImpl(string word)
            {
                _speller.Value.Ignore(word);
            }

            public void IgnoreImplWithRetries(string word, bool shouldSuppressCOMExceptions = true)
            {
                // IgnoreImpl and Init are SecuritySafeCritical, so it is okay to
                // create anon. lambdas that calls into them, and pass
                // those lambdas below.
                RetryHelper.TryCallAction(
                    action: () => IgnoreImpl(word),
                    preamble: () => Init(shouldSuppressCOMExceptions),
                    ignoredExceptions: SuppressedExceptions[shouldSuppressCOMExceptions]);
            }
            public void Ignore(string word, bool shouldSuppressCOMExceptions = true)
            {
                if (_disposed) return;

                IgnoreImplWithRetries(word, shouldSuppressCOMExceptions);
            }

            #endregion // Ignore

            #region AutoCorrect

            private void AutoCorrectImpl(string from, string to)
            {
                _speller.Value.AutoCorrect(from, to);
            }

            private void AutoCorrectImplWithRetries(string from, string to, bool suppressCOMExceptions = true)
            {
                RetryHelper.TryCallAction(
                    action: () => AutoCorrectImpl(from, to),
                    preamble: () => Init(suppressCOMExceptions),
                    ignoredExceptions: SuppressedExceptions[suppressCOMExceptions]);
            }

            public void AutoCorrect(string from, string to, bool suppressCOMExceptions = true)
            {
                AutoCorrectImplWithRetries(from, to, suppressCOMExceptions);
            }

            #endregion

            #region GetOptionValue

            private byte GetOptionValueImpl(string optionId)
            {
                return _speller.Value.GetOptionValue(optionId);
            }

            private byte GetOptionValueImplWithRetries(string optionId, bool suppressCOMExceptions = true)
            {
                byte optionValue;
                bool callSucceeded =
                    RetryHelper.TryExecuteFunction(
                        func: () => GetOptionValueImpl(optionId),
                        result: out optionValue,
                        preamble: () => Init(suppressCOMExceptions),
                        ignoredExceptions: SuppressedExceptions[suppressCOMExceptions]);

                return callSucceeded ? optionValue : (byte)0;
            }

            public byte GetOptionValue(string optionId, bool suppressCOMExceptions = true)
            {
                return GetOptionValueImplWithRetries(optionId, suppressCOMExceptions);
            }

            #endregion // GetOptionValue

            #region GetOptionIds

            private List<string> GetOptionIdsImpl()
            {
                IEnumString optionIds = _speller.Value.OptionIds;
                return optionIds?.ToList(false, true);
            }

            private List<string> GetOptionIdsImplWithRetries(bool suppressCOMExceptions)
            {
                List<string> optionIds = null;
                bool callSucceeded =
                    RetryHelper.TryExecuteFunction(
                        func: GetOptionIdsImpl,
                        result: out optionIds,
                        preamble: () => Init(suppressCOMExceptions),
                        ignoredExceptions: SuppressedExceptions[suppressCOMExceptions]);

                return callSucceeded ? optionIds : null;
            }

            public List<string> GetOptionIds(bool suppressCOMExceptions = true)
            {
                return _disposed ? null : GetOptionIdsImplWithRetries(suppressCOMExceptions);
            }

            #endregion

            #region GetId

            private string GetIdImpl()
            {
                return _speller.Value.Id;
            }

            private string GetIdImplWithRetries(bool suppressCOMExceptions)
            {
                string id = null;
                bool callSucceeded =
                    RetryHelper.TryExecuteFunction(
                        func: GetIdImpl,
                        result: out id,
                        preamble: () => Init(suppressCOMExceptions),
                        ignoredExceptions: SuppressedExceptions[suppressCOMExceptions]);

                return callSucceeded ? id : null;
            }

            private string GetId(bool suppressCOMExceptions = true)
            {
                return _disposed ? null : GetIdImplWithRetries(suppressCOMExceptions);
            }

            #endregion // GetId

            #region GetLocalizedName

            private string GetLocalizedNameImpl()
            {
                return _speller.Value.LocalizedName;
            }

            private string GetLocalizedNameImplWithRetries(bool suppressCOMExceptions)
            {
                string localizedName = null;
                bool callSucceeded =
                    RetryHelper.TryExecuteFunction(
                        func: GetLocalizedNameImpl,
                        result: out localizedName,
                        preamble: () => Init(suppressCOMExceptions),
                        ignoredExceptions: SuppressedExceptions[suppressCOMExceptions]);

                return callSucceeded ? localizedName : null;
            }

            public string GetLocalizedName(bool suppressCOMExceptions = true)
            {
                return _disposed ? null : GetLocalizedNameImplWithRetries(suppressCOMExceptions);
            }

            #endregion // GetLocalizedName

            #region GetOptionDescription

            private OptionDescription GetOptionDescriptionImpl(string optionId)
            {
                IOptionDescription iod = _speller.Value.GetOptionDescription(optionId);
                return (iod != null) ? OptionDescription.Create(iod, false, true) : null;
            }

            private OptionDescription GetOptionDescriptionImplWithRetries(string optionId, bool suppressCOMExceptions)
            {
                OptionDescription optionDescription = null;
                bool callSucceeded =
                    RetryHelper.TryExecuteFunction(
                        func: () => GetOptionDescriptionImpl(optionId),
                        result: out optionDescription,
                        preamble: () => Init(suppressCOMExceptions),
                        ignoredExceptions: SuppressedExceptions[suppressCOMExceptions]);

                return callSucceeded ? optionDescription : null;
            }

            public OptionDescription GetOptionDescription(string optionId, bool suppressCOMExceptions = true)
            {
                return _disposed ? null : GetOptionDescriptionImplWithRetries(optionId, suppressCOMExceptions);
            }

            #endregion // GetOptionDescription

            #region Check

            private List<SpellingError> CheckImpl(string text)
            {
                IEnumSpellingError errors = _speller.Value.Check(text);
                return errors?.ToList(this, text, false, true);
            }

            private List<SpellingError> CheckImplWithRetries(string text, bool suppressCOMExceptions)
            {
                List<SpellingError> errors = null;
                bool callSucceeded =
                    RetryHelper.TryExecuteFunction(
                        func: () => CheckImpl(text),
                        result: out errors,
                        preamble: () => Init(suppressCOMExceptions),
                        ignoredExceptions: SuppressedExceptions[suppressCOMExceptions]);

                return callSucceeded ? errors : null;
            }

            public List<SpellingError> Check(string text, bool suppressCOMExceptions = true)
            {
                return _disposed ? null : CheckImplWithRetries(text, suppressCOMExceptions);
            }

            #endregion // Check

            #region ComprehensiveCheck

            public List<SpellingError> ComprehensiveCheckImpl(string text)
            {
                IEnumSpellingError errors = _speller.Value.ComprehensiveCheck(text);
                return errors?.ToList(this, text, false, true);
            }

            public List<SpellingError> ComprehensiveCheckImplWithRetries(string text, bool shouldSuppressCOMExceptions = true)
            {
                List<SpellingError> errors = null;
                bool callSucceeded =
                    RetryHelper.TryExecuteFunction(
                        func: () => ComprehensiveCheckImpl(text),
                        result: out errors,
                        preamble: () => Init(shouldSuppressCOMExceptions),
                        ignoredExceptions: SuppressedExceptions[shouldSuppressCOMExceptions]);

                return callSucceeded ? errors : null;
            }

            public List<SpellingError> ComprehensiveCheck(string text, bool shouldSuppressCOMExceptions = true)
            {
                return _disposed ? null : ComprehensiveCheckImplWithRetries(text, shouldSuppressCOMExceptions);
            }

            #endregion // ComprehensiveCheck

            #region HasErrors

            // This returns true if the given text has any spelling errors.
            // It is a shortcut for
            //      ComprehensiveCheck(text)?.Count != 0
            // that avoids the (expensive) creation of the managed list of errors and
            // their suggested corrections.
            public bool HasErrorsImpl(string text)
            {
                IEnumSpellingError errors = _speller.Value.ComprehensiveCheck(text);
                return (errors != null) ? errors.HasErrors(false, true) : false;
            }

            public bool HasErrorsImplWithRetries(string text, bool shouldSuppressCOMExceptions = true)
            {
                bool hasErrors = false;
                bool callSucceeded =
                    RetryHelper.TryExecuteFunction(
                        func: () => HasErrorsImpl(text),
                        result: out hasErrors,
                        preamble: () => Init(shouldSuppressCOMExceptions),
                        ignoredExceptions: SuppressedExceptions[shouldSuppressCOMExceptions]);

                return callSucceeded ? hasErrors : false;
            }

            public bool HasErrors(string text, bool shouldSuppressCOMExceptions = true)
            {
                if (_disposed || String.IsNullOrWhiteSpace(text))
                    return false;

                // In practice, this method is called many times on the same few
                // words in the vicinity of the insertion caret. The calls to the
                // native spell-checker can be expensive (more so for misspelled
                // that have many nearby corrections), enough to cause lags in
                // response time.  To mitigate this, we keep a cache of the most
                // recent queries and answer from the cache when possible, avoiding
                // the expensive native calls about 80% of the time.

                // The _hasErrorsCache member can be set to null by another thread
                // when the native spell-checker changes.  To avoid NREs, use a local
                // reference here.  If the cache is nulled out while we're in
                // this method, the worst that happens is that a new entry we add
                // to the old cache won't be visible to the next query, causing one
                // extra "avoidable" native query.  It's not worth the effort and
                // synchronization overhead to "solve" this very infrequent case.
                List<HasErrorsResult> hasErrorsCache = _hasErrorsCache;

                // search the MRU cache for the text
                int cacheSize = (hasErrorsCache != null) ? hasErrorsCache.Count : 0;
                int index;
                for (index = 0; index < cacheSize; ++index)
                {
                    if (text == hasErrorsCache[index].Text)
                        break;
                }

                HasErrorsResult result;
                if (index < cacheSize)
                {
                    // if found, use the cached result
                    result = hasErrorsCache[index];
                }
                else
                {
                    // otherwise, get the result from the native spell checker
                    result = new HasErrorsResult(text, HasErrorsImplWithRetries(text, shouldSuppressCOMExceptions));

                    // add it to the cache, initializing as needed
                    if (hasErrorsCache == null)
                    {
                        hasErrorsCache = new List<HasErrorsResult>(HasErrorsCacheCapacity);
                        _hasErrorsCache = hasErrorsCache;
                    }

                    if (cacheSize < HasErrorsCacheCapacity)
                    {
                        // add an entry at index cacheSize.  It will get overwritten
                        // in the first iteration of the move-to-front loop,
                        // but we have to add something so that the reference
                        // to cache[index] doesn't hit an out-of-range exception.
                        hasErrorsCache.Add(result);
                    }
                    else
                    {
                        index = HasErrorsCacheCapacity - 1;
                    }
                }

                // move the entry to the front of the cache (to preserve MRU),
                // and return the result
                for (; index > 0; --index)
                {
                    hasErrorsCache[index] = hasErrorsCache[index-1];
                }
                hasErrorsCache[0] = result;

                return result.HasErrors;
            }

            #endregion HasErrors

            #region Add/Remove SpellCheckerChanged support

            private uint? add_SpellCheckerChangedImpl(ISpellCheckerChangedEventHandler handler)
            {
                return (handler != null) ? (uint?)null : _speller.Value.add_SpellCheckerChanged(handler);
            }

            private uint? addSpellCheckerChangedImplWithRetries(ISpellCheckerChangedEventHandler handler, bool suppressCOMExceptions)
            {
                uint? eventCookie;
                bool callSucceeded =
                    RetryHelper.TryExecuteFunction(
                        func: () => add_SpellCheckerChangedImpl(handler),
                        result: out eventCookie,
                        preamble: () => Init(suppressCOMExceptions),
                        ignoredExceptions: SuppressedExceptions[suppressCOMExceptions]);

                return callSucceeded ? eventCookie : null;
            }

            private uint? add_SpellCheckerChanged(ISpellCheckerChangedEventHandler handler, bool suppressCOMExceptions = true)
            {
                return _disposed ? null : addSpellCheckerChangedImplWithRetries(handler, suppressCOMExceptions);
            }

            private void remove_SpellCheckerChangedImpl(uint eventCookie)
            {
                _speller.Value.remove_SpellCheckerChanged(eventCookie);
            }

            private void remove_SpellCheckerChangedImplWithRetries(uint eventCookie, bool suppressCOMExceptions = true)
            {
                RetryHelper.TryCallAction(
                    action: () => remove_SpellCheckerChangedImpl(eventCookie),
                    preamble: () => Init(suppressCOMExceptions),
                    ignoredExceptions: SuppressedExceptions[suppressCOMExceptions]);
            }

            private void remove_SpellCheckerChanged(uint eventCookie, bool suppressCOMExceptions = true)
            {
                if (_disposed) return;
                remove_SpellCheckerChangedImplWithRetries(eventCookie, suppressCOMExceptions);
            }

            /// <summary>
            /// This is called when the ISpellChecker instance stored in <see cref="_speller"/>.Value
            /// changes (likely due to a COM failure and reinitialization). When this happens,
            /// we will re-register with add_SpellCheckerChanged if appropriate and update
            /// the eventCookie. Thsi will in-turn permit users of the SpellChecker type
            /// to listen to SpellChecker.Changed event when the underlying ISpellChecker
            /// instance indicates a change.
            /// </summary>
            private void SpellerInstanceChanged(object sender, PropertyChangedEventArgs args)
            {
                _hasErrorsCache = null;     // cached HasErrors results are no longer valid

                // Re-register callbacks with ISpellChecker
                if (_changed != null)
                {
                    lock (_changed)
                    {
                        if (_changed != null)
                        {
                            _eventCookie = add_SpellCheckerChanged(_spellCheckerChangedEventHandler);
                        }
                    }
                }
            }

            /// <summary>
            /// Called when ISpellChecker instance calls into _spellCheckerChangedEventHandler.Invoke
            /// to indicate a change. Invoke in turn calls OnChanged.
            /// </summary>
            internal virtual void OnChanged(SpellCheckerChangedEventArgs e)
            {
                _hasErrorsCache = null;     // cached HasErrors results are no longer valid

                _changed?.Invoke(this, e);
            }

            #region Events

            /// <summary>
            /// Event used to receive notifications when the underlying ISpellChecker
            /// instance indicates a change.
            /// </summary>
            public event EventHandler<SpellCheckerChangedEventArgs> Changed
            {
                add
                {
                    lock (_changed)
                    {
                        if (_changed == null)
                        {
                            _eventCookie = add_SpellCheckerChanged(_spellCheckerChangedEventHandler);
                        }

                        _changed += value;
                    }
                }

                remove
                {
                    lock (_changed)
                    {
                        _changed -= value;
                        if ((_changed == null) && (_eventCookie.HasValue))
                        {
                            remove_SpellCheckerChanged(_eventCookie.Value);
                            _eventCookie = null;
                        }
                    }
                }
            }

            #endregion // Events

            #endregion // Add/Remove SpellCheckerChanged support

            #endregion // ISpellChecker


            #region IDisposable Support

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            protected virtual void Dispose(bool disposing)
            {
                if (_disposed) return;

                _disposed = true;
                if (_speller?.Value != null)
                {
                    try
                    {
                        Marshal.ReleaseComObject(_speller.Value);
                    }
                    catch
                    {
                        // do nothing
                    }
                    _speller = null;
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            ~SpellChecker()
            {
                Dispose(false);
            }

            #endregion // IDisposable Support

            #region Fields

            private static readonly Dictionary<bool, List<Type>> SuppressedExceptions = new Dictionary<bool, List<Type>>
            {
                {false, new List<Type>{ /*empty*/ }},
                {true, new List<Type> { typeof(COMException), typeof(UnauthorizedAccessException)}}
            };

            private ChangeNotifyWrapper<ISpellChecker> _speller;
            private string _languageTag;

            // Change notification related fields
            private SpellCheckerChangedEventHandler _spellCheckerChangedEventHandler;
            private uint? _eventCookie = null;
            private event EventHandler<SpellCheckerChangedEventArgs> _changed;

            // caching HasErrors results
            private class HasErrorsResult : Tuple<string, bool>
            {
                public HasErrorsResult(string text, bool hasErrors) : base(text, hasErrors) {}
                public string Text { get { return Item1; } }
                public bool HasErrors { get { return Item2; } }
            }
            private List<HasErrorsResult> _hasErrorsCache;
            private const int HasErrorsCacheCapacity = 10;      // cache the most recent 10 results

            private bool _disposed = false;

            #endregion // Fields
        }
    }
}
