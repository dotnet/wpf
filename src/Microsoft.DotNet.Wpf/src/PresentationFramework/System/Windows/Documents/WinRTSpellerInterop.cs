// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Custom COM marshalling code and interfaces for interaction
//                  with the WinRT wordbreaker API and ISpellChecker
//                  spell-checker API
//

namespace System.Windows.Documents
{
    using MS.Internal;
    using MS.Internal.WindowsRuntime.Windows.Data.Text;

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Security;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents.Tracing;
    using System.Windows.Input;
    using System.Windows.Threading;

    using System.Windows.Documents.MsSpellCheckLib;

    internal partial class WinRTSpellerInterop: SpellerInteropBase
    {
        #region Constructors

        /// <exception cref="PlatformNotSupportedException">
        /// The OS platform is not supported
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The OS platform is supportable, but spellchecking services are currently unavailable
        /// </exception>
        internal WinRTSpellerInterop()
        {
            try
            {
                SpellCheckerFactory.Create(shouldSuppressCOMExceptions: false);
            }
            catch (Exception ex)
                // Sometimes, InvalidCastException is thrown when SpellCheckerFactory fails to instantiate correctly
                when (ex is InvalidCastException || ex is COMException )
            {
                Dispose();
                throw new PlatformNotSupportedException(string.Empty, ex);
            }

            _spellCheckers = new Dictionary<CultureInfo, Tuple<WordsSegmenter, SpellChecker>>();
            _customDictionaryFiles = new Dictionary<string, List<string>>();

            _defaultCulture = InputLanguageManager.Current?.CurrentInputLanguage ?? Thread.CurrentThread.CurrentCulture;
            _culture = null;

            try
            {
                EnsureWordBreakerAndSpellCheckerForCulture(_defaultCulture, throwOnError: true);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PlatformNotSupportedException)
            {
                _spellCheckers = null;
                Dispose();

                if ((ex is PlatformNotSupportedException) || (ex is NotSupportedException))
                {
                    throw;
                }
                else
                {
                    throw new NotSupportedException(string.Empty, ex);
                }
            }

            // This class is only instantiated from the UI thread, so it is safe to
            // obtain the Dispatcher this way.
            _dispatcher = new WeakReference<Dispatcher>(Dispatcher.CurrentDispatcher);

            WeakEventManager<AppDomain, UnhandledExceptionEventArgs>
                .AddHandler(AppDomain.CurrentDomain, "UnhandledException", ProcessUnhandledException);
        }

        ~WinRTSpellerInterop()
        {
            Dispose(false);
        }

        #endregion Constructors

        #region IDispose

        public override void  Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Internal interop resource cleanup
        /// </summary>
        /// <param name="disposing">
        ///     False when called from the Finalizer
        ///     True when called explicitly from Dispose()
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(SR.Get(SRID.TextEditorSpellerInteropHasBeenDisposed));
            }

            try
            {
                // Ensure that Dispose is called from the UI thread
                // If it is not, then make it so
                if (BeginInvokeOnUIThread((Action<bool>)Dispose, DispatcherPriority.Normal, disposing) == null)
                {
                    // Already on UI thread
                    // Continue with core Dispose logic

                    ReleaseAllResources(disposing);
                    _isDisposed = true;
                }
            }
            catch (InvalidOperationException)
            {
                // We have no way determining whether or not this is running
                // on the UI thread.
            }
        }

        #endregion

        #region Internal Methods

        internal override void SetLocale(CultureInfo culture)
        {
            Culture = culture;
        }

        /// <summary>
        /// Sets the mode in which the spell-checker operates
        /// We care about 3 different modes here:
        ///
        /// 1. Shallow spellchecking - i.e., wordbreaking +      spellchecking + NOT (suggestions)
        /// 2. Deep spellchecking    - i.e., wordbreaking +      spellchecking +      suggestions
        /// 3. Wordbreaking only     - i.e., wordbreaking + NOT (spellchcking) + NOT (suggestions)
        /// </summary>
        internal override SpellerMode Mode
        {
            set
            {
                _mode = value;
            }
        }

        /// <summary>
        /// If true, multi-word spelling errors would be detected
        /// This flag is ignored by WinRTSpellerInterop
        /// </summary>
        internal override bool MultiWordMode
        {
            set
            {
                // do nothing - multi-word mode specification is not supported
                // _multiWordMode = value;
            }
        }

        /// <summary>
        /// Sets spelling reform mode
        /// WinRTSpellerInterop doesn't support spelling reform
        /// </summary>
        /// <param name="culture"></param>
        /// <param name="spellingReform"></param>
        internal override void SetReformMode(CultureInfo culture, SpellingReform spellingReform)
        {
            // Do nothing - spelling reform is not supported
            // _spellingReformInfos[culture] =  spellingReform;
        }

        /// <summary>
        /// Returns true if we have an engine capable of proofing the specified language.
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        internal override bool CanSpellCheck(CultureInfo culture)
        {
            return !_isDisposed && EnsureWordBreakerAndSpellCheckerForCulture(culture);
        }


        #region Dictionary Methods

        /// <summary>
        /// Unloads a given custom dictionary
        /// </summary>
        /// <param name="token"></param>
        internal override void UnloadDictionary(object token)
        {
            if (_isDisposed) return;

            var data = (Tuple<string, string>)token;
            string ietfLanguageTag = data.Item1;
            string filePath = data.Item2;

            using (new SpellerCOMActionTraceLogger(this, SpellerCOMActionTraceLogger.Actions.UnregisterUserDictionary))
            {
                SpellCheckerFactory.UnregisterUserDictionary(filePath, ietfLanguageTag);
            }

            FileHelper.DeleteTemporaryFile(filePath);
        }

        /// <summary>
        /// Loads a custom dictionary
        /// </summary>
        /// <param name="lexiconFilePath"></param>
        /// <returns></returns>
        internal override object LoadDictionary(string lexiconFilePath)
        {
            return _isDisposed ? null : LoadDictionaryImpl(lexiconFilePath);
        }

        /// <summary>
        /// Loads a custom dictionary
        /// </summary>
        /// <param name="item"></param>
        /// <param name="trustedFolder"></param>
        /// <param name="dictionaryLoadedCallback"></param>
        /// <returns></returns>
        internal override object LoadDictionary(Uri item, string trustedFolder)
        {
            if (_isDisposed)
            {
                return null;
            }

            return LoadDictionaryImpl(item.LocalPath);
        }

        /// <summary>
        /// Releases all currently loaded custom dictionaries
        /// </summary>
        internal override void ReleaseAllLexicons()
        {
            if (!_isDisposed)
            {
                ClearDictionaries();
            }
        }

        #endregion

        #endregion Internal Methods


        #region Private Methods

        /// <summary>
        /// </summary>
        /// <param name="culture"></param>
        /// <param name="throwOnError"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private bool EnsureWordBreakerAndSpellCheckerForCulture(CultureInfo culture, bool throwOnError = false)
        {
            if (_isDisposed || (culture == null))
            {
                return false;
            }

            if(!_spellCheckers.ContainsKey(culture))
            {
                WordsSegmenter wordBreaker = null;

                try
                {
                    // Generally, we want to use the neutral language segmenter. This will ensure that the
                    // WordsSegmenter instance will not inadvertently de-compound words into stems. For e.g.,
                    // the dedicated segmenter for German will break down words like Hausnummer into {Haus, nummer},
                    // whereas the nuetral segmenter will not do so.
                    wordBreaker = WordsSegmenter.Create(culture.Name, shouldPreferNeutralSegmenter:true);
                }
                catch when (!throwOnError)
                {
                    // ArgumentException: culture name is malformed - unlikely given we use culture.Name
                    // PlatformNotSupportedException: OS is not supported
                    // NotSupportedException: culture name is likely well-formed, but not available currently for wordbreaking
                    wordBreaker = null;
                }

                // Even if wordBreaker.ResolvedLanguage == WordsSegmenter.Undetermined, we will use it
                // as an appropriate fallback wordbreaker as long as a corresponding ISpellChecker is found.
                if (wordBreaker == null)
                {
                    _spellCheckers[culture] = null;
                    return false;
                }

                SpellChecker spellChecker = null;

                try
                {
                    using (new SpellerCOMActionTraceLogger(this, SpellerCOMActionTraceLogger.Actions.SpellCheckerCreation))
                    {
                        spellChecker = new SpellChecker(culture.Name);
                    }
                }
                catch (Exception ex)
                {
                    spellChecker = null;

                    // ArgumentException:
                    // Either the language name is malformed (unlikely given we use culture.Name)
                    //   or this language is not supported. It might be supported if the appropriate
                    //   input language is added by the user, but it is not available at this time.

                    if (throwOnError && ex is ArgumentException)
                    {
                        throw new NotSupportedException(string.Empty, ex);
                    }
                }

                if (spellChecker == null)
                {
                    _spellCheckers[culture] = null;
                }
                else
                {
                    _spellCheckers[culture] = new Tuple<WordsSegmenter, SpellChecker>(wordBreaker, spellChecker);
                }
            }

            return (_spellCheckers[culture] == null ? false : true);
        }

        /// <summary>
        /// foreach(sentence in text.sentences)
        ///      foreach(segment in sentence)
        ///          continueIteration = segmentCallback(segment, data)
        ///      endfor
        ///
        ///      if (sentenceCallback != null)
        ///          continueIteration = sentenceCallback(sentence, data)
        ///      endif
        ///
        ///      if (!continueIteration)
        ///          break
        ///      endif
        ///  endfor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="count"></param>
        /// <param name="sentenceCallback"></param>
        /// <param name="segmentCallback"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        internal override int EnumTextSegments(char[] text, int count,
            EnumSentencesCallback sentenceCallback, EnumTextSegmentsCallback segmentCallback, object data)
        {
            if (_isDisposed)
            {
                return 0;
            }

            var wordBreaker = CurrentWordBreaker ?? DefaultCultureWordBreaker;
            var spellChecker = CurrentSpellChecker;

            bool spellCheckerNeeded = _mode.HasFlag(SpellerMode.SpellingErrors) || _mode.HasFlag(SpellerMode.Suggestions);
            if ((wordBreaker == null) || (spellCheckerNeeded && spellChecker == null)) return 0;

            int segmentCount = 0;
            bool continueIteration = true;

            // WinRT WordsSegmenter doesn't have the ability to break down text into segments (sentences).
            // Treat the whole text as a single segment for now.
            foreach(string strSentence in new string[]{string.Join(string.Empty, text)})
            {
                SpellerSentence sentence = new SpellerSentence(strSentence, wordBreaker, CurrentSpellChecker, this);
                segmentCount += sentence.Segments.Count;

                if (segmentCallback != null)
                {
                    for (int i = 0; continueIteration && (i < sentence.Segments.Count); i++)
                    {
                        continueIteration = segmentCallback(sentence.Segments[i], data);
                    }
                }

                if (sentenceCallback != null)
                {
                    continueIteration = sentenceCallback(sentence, data);
                }

                if (!continueIteration) break;
            }

            return segmentCount;
        }

        /// <summary>
        ///     Actual implementation of loading a dictionary
        /// </summary>
        /// <param name="lexiconFilePath"></param>
        /// <param name="dictionaryLoadedCallback"></param>
        /// <param name="callbackParam"></param>
        /// <returns>
        ///     A tuple of cultureinfo detected from <paramref name="lexiconFilePath"/> and
        ///     a temp file path which holds a copy of <paramref name="lexiconFilePath"/>
        ///
        ///     If no culture is specified in the first line of <paramref name="lexiconFilePath"/>
        ///     in the format #LID nnnn (where nnnn = decimal LCID of the culture), then invariant
        ///     culture is returned.
        /// </returns>
        /// <remarks>
        ///     At the end of this method, we guarantee that <paramref name="lexiconFilePath"/>
        ///     can be reclaimed (i.e., potentially deleted) by the caller.
        /// </remarks>
        private Tuple<string, string> LoadDictionaryImpl(string lexiconFilePath)
        {
            if (_isDisposed)
            {
                return new Tuple<string, string>(null, null);
            }

            if (!File.Exists(lexiconFilePath))
            {
                throw new ArgumentException(SR.Get(SRID.CustomDictionaryFailedToLoadDictionaryUri, lexiconFilePath));
            }

            bool fileCopied = false;
            string lexiconPrivateCopyPath = null;

            try
            {
                CultureInfo culture = null;

                // Read the first line of the file and detect culture, if specified
                using (FileStream stream = new FileStream(lexiconFilePath, FileMode.Open, FileAccess.Read))
                {
                    string line = null;
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        line = reader.ReadLine();
                        culture = WinRTSpellerInterop.TryParseLexiconCulture(line);
                    }
                }

                string ietfLanguageTag = culture.IetfLanguageTag;

                // Make a temp file and copy the original file over.
                // Ensure that the copy has Unicode (UTF16-LE) encoding
                using (FileStream lexiconPrivateCopyStream = FileHelper.CreateAndOpenTemporaryFile(out lexiconPrivateCopyPath, extension: "dic"))
                {
                    WinRTSpellerInterop.CopyToUnicodeFile(lexiconFilePath, lexiconPrivateCopyStream);
                    fileCopied = true;
                }

                // Add the temp file (with .dic extension) just created to a cache,
                // then pass it along to IUserDictionariesRegistrar

                if (!_customDictionaryFiles.ContainsKey(ietfLanguageTag))
                {
                    _customDictionaryFiles[ietfLanguageTag] = new List<string>();
                }

                _customDictionaryFiles[ietfLanguageTag].Add(lexiconPrivateCopyPath);

                using (new SpellerCOMActionTraceLogger(this, SpellerCOMActionTraceLogger.Actions.RegisterUserDictionary))
                {
                    SpellCheckerFactory.RegisterUserDictionary(lexiconPrivateCopyPath, ietfLanguageTag);
                }

                return new Tuple<string, string>(ietfLanguageTag, lexiconPrivateCopyPath);
            }
            catch (Exception e) when ((e is ArgumentException) || !fileCopied)
            {
                // IUserDictionariesRegistrar.RegisterUserDictionary can
                // throw ArgumentException on failure. Cleanup the temp file if
                // we successfully created one.
                if (lexiconPrivateCopyPath != null)
                {
                    FileHelper.DeleteTemporaryFile(lexiconPrivateCopyPath);
                }

                throw new ArgumentException(SR.Get(SRID.CustomDictionaryFailedToLoadDictionaryUri, lexiconFilePath), e);
            }
        }

        /// <summary>
        ///     Actual implementation of clearing all dictionaries
        /// </summary>
        /// <remarks>
        ///     ClearDictionaries() can be called from the following methods
        ///         Dispose(bool):              UI thread
        ///         ReleaseAllLexicons:         UI thread
        ///         ProcessUnhandledException:  UI thread
        ///
        ///     Even when Dispose or ProcessUnhandledException runs in a different thread,
        ///     for e.g., the Finalizer thread or an arbitrary exception handling thread
        ///     respectively, we ensure that ClearDictionaries is always called in the UI
        ///     thread by invoking it with help from the cached <see cref="Dispatcher"/>
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void ClearDictionaries(bool disposing = false)
        {
            if (_isDisposed)
            {
                // Likely this platform is not supported - do not process further.
                return;
            }

            if (_customDictionaryFiles != null)
            {
                foreach (KeyValuePair<string, List<string>> items in _customDictionaryFiles)
                {
                    string ietfLanguageTag = items.Key;
                    foreach (string filePath in items.Value)
                        try
                        {
                            using (new SpellerCOMActionTraceLogger(this, SpellerCOMActionTraceLogger.Actions.UnregisterUserDictionary))
                            {
                                SpellCheckerFactory.UnregisterUserDictionary(filePath, ietfLanguageTag);
                            }

                            FileHelper.DeleteTemporaryFile(filePath);
                        }
                        catch
                        {
                            // Do nothing - Continue to make a best effort
                            // attempt at unregistering custom dictionaries
                        }
                }

                _customDictionaryFiles.Clear();
            }

            if (disposing)
            {
                _customDictionaryFiles = null;
            }
        }

        /// <summary>
        ///     Detect whether the <paramref name="line"/> is of the form #LID nnnn,
        ///     and if it is, try to instantiate a CultureInfo object with LCID nnnn.
        /// </summary>
        /// <param name="line"></param>
        /// <returns>
        ///     The CultureInfo object corresponding to the LCID specified in the <paramref name="line"/>
        /// </returns>
        private static CultureInfo TryParseLexiconCulture(string line)
        {
            const string regexPattern = @"\s*\#LID\s+(\d+)\s*";
            RegexOptions regexOptions = RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled;

            CultureInfo result = CultureInfo.InvariantCulture;

            if (line == null)
            {
                return result;
            }

            string[] matches = Regex.Split(line.Trim(), regexPattern, regexOptions);

            // We expect 1 exact match, which implies matches.Length == 3 (before, match, after)
            if (matches.Length != 3)
            {
                return result;
            }

            string before = matches[0];
            string match  = matches[1];
            string after  = matches[2];

            // We expect 1 exact match, which implies the following:
            //      before == after == string.Emtpy
            //      match is parsable into an integer
            int lcid;
            if ((before != string.Empty) || (after != string.Empty) || (!Int32.TryParse(match, out lcid)))
            {
                return result;
            }

            try
            {
                result = new CultureInfo(lcid);
            }
            catch (CultureNotFoundException)
            {
                result = CultureInfo.InvariantCulture;
            }

            return result;
        }

        /// <summary>
        ///     Copies <paramref name="sourcePath"/> to <paramref name="targetPath"/>. During the copy, it transcodes
        ///     <paramref name="sourcePath"/> to Unicode (UTL16-LE) if necessary and ensures that <paramref name="targetPath"/>
        ///     has the right BOM (Byte Order Mark) for UTF16-LE (FF FE)
        /// </summary>
        /// <see cref = "// See http://www.unicode.org/faq/utf_bom.html" />
        /// <param name="sourcePath"></param>
        /// <param name="targetPath"></param>
        private static void CopyToUnicodeFile(string sourcePath, FileStream targetStream)
        {
            bool utf16LEEncoding = false;
            using (FileStream sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
            {
                // Check that the first two bytes indicate the BOM for UTF16-LE
                // If found, we can directly copy the file over without additional transcoding.
                utf16LEEncoding = ((sourceStream.ReadByte() == 0xFF) && (sourceStream.ReadByte() == 0xFE));
                sourceStream.Seek(0, SeekOrigin.Begin);

                if (utf16LEEncoding)
                {
                    sourceStream.CopyTo(targetStream);
                }
                else
                {
                    using (StreamReader reader = new StreamReader(sourceStream))
                    {
                        // Create the StreamWriter with encoding = Unicode to ensure that the new file
                        // contains the BOM for UTF16-LE, and also ensures that the file contents are
                        // encoded correctly
                        using (StreamWriter writer = new StreamWriter(targetStream, Text.Encoding.Unicode))
                        {
                            string line = null;
                            while ((line = reader.ReadLine()) != null)
                            {
                                writer.WriteLine(line);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to unregister all custom dictionaries if an unhandled exception is raised
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var disposing = false;

            try
            {
                if (BeginInvokeOnUIThread((Action<bool>)ClearDictionaries, DispatcherPriority.Normal, disposing) == null)
                {
                    // Already on UI thread
                    ClearDictionaries(disposing);
                }
            }
            catch(InvalidOperationException)
            {
                // We have no way determining whether or not this is running
                // on the UI thread. Safest to avoid calling into thread-sensitive
                // operations like ClearDictionaries
            }
        }


        /// <summary>
        /// Releases all resources by:
        /// <list type="bullet">
        /// <item>disposing all IDisposables, which would in turn
        /// free up any associated respective native resources</item>
        /// <item>Unregistering all custom dictionaries</item>
        /// </list>
        /// </summary>
        /// <param name="disposing">True when called from <see cref="Dispose"/>, False when called from the Finalizer</param>
        private void ReleaseAllResources(bool disposing)
        {
            if (_spellCheckers != null)
            {
                foreach (Tuple<WordsSegmenter, SpellChecker> item in _spellCheckers.Values)
                {
                    SpellChecker spellChecker = item?.Item2;
                    if (spellChecker != null)
                    {
                        spellChecker.Dispose();
                    }
                }

                _spellCheckers = null;
            }

            ClearDictionaries(disposing);
        }

        /// <summary>
        /// Executes the specified delegate asynchronously on the UI thread using the <see cref="Dispatcher"/>
        /// stored in the <see cref="WeakReference{T}"/> <see cref="_dispatcher"/>
        ///
        /// If the current thread is already the UI thread, then the delegate is is not executed and the method
        /// returns null.
        /// </summary>
        /// <param name="method">The delegate to a method that takes parameters specified in <paramref name="args"/>,
        /// which is pushed onto the <see cref="Dispatcher"/> queue.</param>
        /// <param name="priority">The priority, relative to the other pending operations in the <see cref="Dispatcher"/>
        /// event queue, the specified method is invoked.</param>
        /// <param name="args">An array of objects to pass as arguments to the given method. Can be <code>null</code></param>
        /// <returns>
        /// An object, which is returned immediately after <see cref="Dispatcher.BeginInvoke"/> is called,
        /// that can be used to interact with the delegate as it is pending execution in the event queue.
        ///
        /// If the current thread is already the same thread as the one on which the cached <see cref="Dispatcher"/>
        /// is running, then it returns null.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the cached <see cref="Dispatcher"/> can not be obtained
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown by <see cref="Dispatcher.BeginInvoke"/> when <paramref name="method"/> is <code>null
        /// </code></exception>
        /// <remarks>
        ///     The usual pattern for methods that deal with delegates is to mark them with
        ///     the <see cref="SecuritySafeCriticalAttribute"/>. This would ensure that the caller
        ///     is responsible for managing its CAS attribute appropriately, but the method
        ///     that receives the delegate and orchestrates its execution can do so successfully
        ///     irrespective of whether the delegate is Critical or Transparent.
        ///
        ///     In this instance, marking this method with <see cref="SecuritySafeCriticalAttribute"/> is
        ///     unnecessary because <see cref="Dispatcher.BeginInvoke"/> is really what is used to
        ///     orchestrate the execution of <paramref name="method"/>, and in turn, <see cref="Dispatcher.BeginInvoke"/>
        ///     takes care of marking its own CAS attribute appropriately.
        /// </remarks>
        private DispatcherOperation BeginInvokeOnUIThread(Delegate method, DispatcherPriority priority, params object[] args)
        {
            Dispatcher dispatcher = null;

            if (_dispatcher == null ||
                !_dispatcher.TryGetTarget(out dispatcher) ||
                dispatcher == null)
            {
                throw new InvalidOperationException();
            }

            if (!dispatcher.CheckAccess())
            {
                return dispatcher.BeginInvoke(method, priority, args);
            }

            return null;
        }

        #endregion

        #region Private Properties

        private CultureInfo Culture
        {
            get
            {
                return _culture;
            }

            set
            {
                _culture = value;
                EnsureWordBreakerAndSpellCheckerForCulture(_culture);
            }
        }

        private WordsSegmenter CurrentWordBreaker
        {
            get
            {
                if (Culture == null)
                {
                    return null;
                }
                else
                {
                    EnsureWordBreakerAndSpellCheckerForCulture(Culture);
                    return _spellCheckers[Culture]?.Item1;
                }
            }
        }

        private WordsSegmenter DefaultCultureWordBreaker
        {
            get
            {
                if (_defaultCulture == null)
                {
                    return null;
                }
                else
                {
                    return _spellCheckers[_defaultCulture]?.Item1;
                }
            }
        }

        private SpellChecker CurrentSpellChecker
        {
            get
            {
                if (Culture == null)
                {
                    return null;
                }
                else
                {
                    EnsureWordBreakerAndSpellCheckerForCulture(Culture);
                    return _spellCheckers[Culture]?.Item2;
                }
            }
        }

        #endregion

        #region Private Fields

        private bool _isDisposed = false;
        private SpellerMode _mode = SpellerMode.None;

        // Cache of word-breakers and spellcheckers
        private Dictionary<CultureInfo, Tuple<WordsSegmenter, SpellChecker>> _spellCheckers;

        private CultureInfo _defaultCulture;
        private CultureInfo _culture;

        /// <summary>
        /// Cache of private dictionaries
        /// Key: ietfLanguageTag
        /// Values: List of file names that have been registered for <i>ietfLanguageTag</i>
        /// </summary>
        private Dictionary<string, List<string>> _customDictionaryFiles;

        /// <summary>
        /// <see cref="Dispatcher"/> associated with the thread on which this instance
        /// of <see cref="WinRTSpellerInterop"/> was created. This <see cref="Dispatcher"/>
        /// is used to ensure that all COM calls are delegated to the UI thread
        /// by the finalizer.
        /// </summary>
        private readonly WeakReference<Dispatcher> _dispatcher;

        #endregion Private Fields

        #region Private Types

        private struct TextRange: SpellerInteropBase.ITextRange
        {
            public TextRange(MS.Internal.WindowsRuntime.Windows.Data.Text.TextSegment textSegment)
            {
                _length = (int)textSegment.Length;
                _start = (int)textSegment.StartPosition;
            }

            public static explicit operator TextRange(MS.Internal.WindowsRuntime.Windows.Data.Text.TextSegment textSegment)
            {
                return new TextRange(textSegment);
            }

            #region SpellerInteropBase.ITextRange

            public int Start
            {
                get { return _start;  }
            }

            public int Length
            {
                get { return _length; }
            }

            #endregion

            private readonly int _start;
            private readonly int _length;
        }

        [DebuggerDisplay("SubSegments.Count = {SubSegments.Count} TextRange = {TextRange.Start},{TextRange.Length}")]
        private class SpellerSegment: ISpellerSegment
        {
            #region Constructor

            public SpellerSegment(WordSegment segment, SpellChecker spellChecker, WinRTSpellerInterop owner)
            {
                _segment = segment;
                _spellChecker = spellChecker;
                _suggestions = null;
                _owner = owner;
            }

            static SpellerSegment()
            {
                _empty = new List<ISpellerSegment>().AsReadOnly();
            }

            #endregion

            #region Private Methods

            private void EnumerateSuggestions()
            {
                List<string> result = new List<string>();
                _isClean = true;

                if (_spellChecker == null)
                {
                    _suggestions = result.AsReadOnly();
                    return;
                }

                List<SpellChecker.SpellingError> spellingErrors = null;

                using (new SpellerCOMActionTraceLogger(_owner, SpellerCOMActionTraceLogger.Actions.ComprehensiveCheck))
                {
                    spellingErrors = _spellChecker.ComprehensiveCheck(_segment.Text);
                }

                if (spellingErrors == null)
                {
                    _suggestions = result.AsReadOnly();
                    return;
                }

                foreach (var spellingError in spellingErrors)
                {
                    result.AddRange(spellingError.Suggestions);
                    if (spellingError.CorrectiveAction != SpellChecker.CorrectiveAction.None)
                    {
                        _isClean = false;
                    }
                }

                _suggestions = result.AsReadOnly();
            }

            #endregion

            #region SpellerInteropBase.ISpellerSegment

            /// <summary>
            /// Returns a read-only list of sub-segments of this segment
            /// WinRT word-segmenter doesn't really support sub-segments,
            ///   so we always return an empty list
            /// </summary>
            public IReadOnlyList<ISpellerSegment> SubSegments
            {
                get
                {
                    return SpellerSegment._empty;
                }
            }

            public ITextRange TextRange
            {
                get
                {
                    return new TextRange(_segment.SourceTextSegment);
                }
            }

            public IReadOnlyList<string> Suggestions
            {
                get
                {
                    if (_suggestions == null)
                    {
                        EnumerateSuggestions();
                    }

                    return _suggestions;
                }
            }

            public bool IsClean
            {
                get
                {
                    if (_isClean == null)
                    {
                        EnumerateSuggestions();
                    }

                    return _isClean.Value;
                }
            }

            public void EnumSubSegments(EnumTextSegmentsCallback segmentCallback, object data)
            {
                bool result = true;

                for (int i = 0; result && (i < SubSegments.Count); i++)
                {
                    result = segmentCallback(SubSegments[i], data);
                }
            }

            #endregion SpellerInteropBase.ISpellerSegment

            #region Private Fields

            private WordSegment _segment;

            SpellChecker _spellChecker;
            private IReadOnlyList<string> _suggestions;
            private bool? _isClean = null;

            private static readonly IReadOnlyList<ISpellerSegment> _empty;

            /// <remarks>
            /// This field is used only to support TraceLogging telemetry
            /// logged using <see cref="SpellerCOMActionTraceLogger"/>. It
            /// has no other functional use.
            /// </remarks>
            private WinRTSpellerInterop _owner;

            #endregion Private Fields
        }

        [DebuggerDisplay("Sentence = {_sentence}")]
        private class SpellerSentence: ISpellerSentence
        {
            public SpellerSentence(string sentence, WordsSegmenter wordBreaker, SpellChecker spellChecker, WinRTSpellerInterop owner)
            {
                _sentence = sentence;
                _wordBreaker = wordBreaker;
                _spellChecker = spellChecker;
                _segments = null;
                _owner = owner;
            }

            #region SpellerInteropBase.ISpellerSentence

            public IReadOnlyList<ISpellerSegment> Segments
            {
                get
                {
                    if (_segments == null)
                    {
                        List<SpellerSegment> segments = new List<SpellerSegment>();

                        foreach (var wordSegment in _wordBreaker.GetTokens(_sentence))
                        {
                            segments.Add(new SpellerSegment(wordSegment, _spellChecker, _owner));
                        }

                        _segments = segments.AsReadOnly();
                    }

                    return _segments;
                }
            }

            public int EndOffset
            {
                get
                {
                    int endOffset = -1;

                    if (Segments.Count > 0)
                    {
                        ITextRange textRange = Segments[Segments.Count - 1].TextRange;
                        endOffset = textRange.Start + textRange.Length;
                    }

                    return endOffset;
                }
            }

            #endregion

            private string _sentence;
            private WordsSegmenter _wordBreaker;
            private SpellChecker  _spellChecker;
            private IReadOnlyList<SpellerSegment> _segments;

            /// <remarks>
            /// This field is used only to support TraceLogging telemetry
            /// logged using <see cref="SpellerCOMActionTraceLogger"/>. It
            /// has no other functional use.
            /// </remarks>
            private WinRTSpellerInterop _owner;
        }

        #endregion Private Types

        #region Private Interfaces

        #endregion Private Interfaces
    }
}
