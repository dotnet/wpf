// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Custom COM marshalling code and interfaces for interaction
//              with the Natural Language Group's nl6 proofing engine.
//

namespace System.Windows.Documents
{
    using System.Collections;
    using System.Runtime.InteropServices;
    using MS.Internal;
    using MS.Win32;
    using System.Globalization;
    using System.Security;
    using System.IO;
    using System.Collections.Generic;
    using System.Windows.Controls;
    using MS.Internal.PresentationFramework;

    // Custom COM marshalling code and interfaces for interaction
    // with the Natural Language Group's nl6 proofing engine.
    internal class NLGSpellerInterop : SpellerInteropBase
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Construct an NLG-based speller interop layer
        /// </summary>
        internal NLGSpellerInterop()
        {
            // Start the lifetime of Natural Language library
            UnsafeNlMethods.NlLoad();

            bool exceptionThrown = true;
            try
            {
                //
                // Allocate the TextChunk.
                //

                _textChunk = CreateTextChunk();

                //
                // Allocate the TextContext.
                //

                ITextContext textContext = CreateTextContext();
                try
                {
                    _textChunk.put_Context(textContext);
                }
                finally
                {
                    Marshal.ReleaseComObject(textContext);
                }

                //
                // Set nl properties.
                //
                _textChunk.put_ReuseObjects(true);
                Mode = SpellerMode.None;

                //  reenable MWE checking when perf is acceptable.
                // We're disabling multi-word error checking for the short term
                // because it is so expensive, 30-50% extra elapsed time.
                MultiWordMode = false;

                exceptionThrown = false;
            }
            finally
            {
                if (exceptionThrown)
                {
                    if (_textChunk != null)
                    {
                        Marshal.ReleaseComObject(_textChunk);
                        _textChunk = null;
                    }

                    UnsafeNlMethods.NlUnload();
                }
            }
        }

        ~NLGSpellerInterop()
        {
            Dispose(false);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  IDispose Methods
        //
        //------------------------------------------------------

        #region IDispose Methods

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Internal interop resource cleanup
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(SR.Get(SRID.TextEditorSpellerInteropHasBeenDisposed));

            if (_textChunk != null)
            {
                Marshal.ReleaseComObject(_textChunk);
                _textChunk = null;
            }

            // Stop the lifetime of Natural Language library
            UnsafeNlMethods.NlUnload();

            _isDisposed = true;
        }

        #endregion IDispose Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal override void SetLocale(CultureInfo culture)
        {
            _textChunk.put_Locale(culture.LCID);
        }

        // Sets an indexed option on the speller's TextContext.
        private void SetContextOption(string option, object value)
        {
            ITextContext textContext;

            _textChunk.get_Context(out textContext);

            if (textContext != null)
            {
                try
                {
                    IProcessingOptions options;

                    textContext.get_Options(out options);
                    if (options != null)
                    {
                        try
                        {
                            options.put_Item(option, value);
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(options);
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(textContext);
                }
            }
        }

        // Helper for methods that need to iterate over segments within a text run.
        // Returns the total number of segments encountered.
        internal override int EnumTextSegments(char[] text, int count,
            EnumSentencesCallback sentenceCallback, EnumTextSegmentsCallback segmentCallback, object data)
        {
            int segmentCount = 0;

            // Unintuively, the speller engine will grab and store the pointer
            // we pass into ITextChunk.SetInputArray.  So it's not safe to merely
            // pinvoke text directly.  We need to allocate a chunk of memory
            // and keep it fixed for the duration of this method call.
            IntPtr inputArray = Marshal.AllocHGlobal(count * 2);

            try
            {
                // Give the TextChunk its next block of text.
                Marshal.Copy(text, 0, inputArray, count);
                _textChunk.SetInputArray(inputArray, count);

                //
                // Iterate over sentences.
                //

                UnsafeNativeMethods.IEnumVariant sentenceEnumerator;

                // Note because we're in the engine's ReuseObjects mode, we may
                // not use ITextChunk.get_Sentences.  We must use the enumerator.
                _textChunk.GetEnumerator(out sentenceEnumerator);
                try
                {
                    NativeMethods.VARIANT variant = new NativeMethods.VARIANT();
                    int[] fetched = new int[1];
                    bool continueIteration = true;

                    sentenceEnumerator.Reset();

                    do
                    {
                        int result;

                        variant.Clear();

                        result = EnumVariantNext(sentenceEnumerator, variant, fetched);

                        if ((result != NativeMethods.S_OK) || (fetched[0] == 0))
                        {
                            break;
                        }

                        using (SpellerSentence sentence = new SpellerSentence((NLGSpellerInterop.ISentence)variant.ToObject()))
                        {
                            segmentCount += sentence.Segments.Count;

                            if (segmentCallback != null)
                            {
                                // Iterate over segments.
                                for (int i = 0; continueIteration && (i < sentence.Segments.Count); i++ )
                                {
                                    continueIteration = segmentCallback(sentence.Segments[i], data);
                                }
                            }

                            // Make another callback when we're done with the entire sentence.
                            if (sentenceCallback != null)
                            {
                                continueIteration = sentenceCallback(sentence, data);
                            }
                        }
                    }
                    while (continueIteration);

                    variant.Clear();
                }
                finally
                {
                    Marshal.ReleaseComObject(sentenceEnumerator);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(inputArray);
            }

            return segmentCount;
        }

        /// <summary>
        /// Unloads given custom dictionary
        /// </summary>
        /// <param name="lexicon"></param>
        internal override void UnloadDictionary(object dictionary)
        {
            ILexicon lexicon = dictionary as ILexicon;
            Invariant.Assert(lexicon != null);

            ITextContext textContext = null;
            try
            {
                _textChunk.get_Context(out textContext);
                textContext.RemoveLexicon(lexicon);
            }
            finally
            {
                Marshal.ReleaseComObject(lexicon);

                if (textContext != null)
                {
                    Marshal.ReleaseComObject(textContext);
                }
            }
        }

        /// <summary>
        /// Loads custom dictionary
        /// </summary>
        /// <param name="lexiconFilePath"></param>
        /// <returns></returns>
        internal override object LoadDictionary(string lexiconFilePath)
        {
            return AddLexicon(lexiconFilePath);
        }


        /// <summary>
        /// Loads custom dictionary.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="trustedFolder"></param>
        /// <returns></returns>
        /// <remarks>
        /// There are 2 kinds of files we're trying to load here: Files specified by user directly, and files
        /// which we created and filled with data from pack Uri locations specified by user.
        /// These 'trusted' files are placed under <paramref name="trustedFolder"/>.
        ///
        /// Files specified in <paramref name="trustedFolder"/> are wrapped in FileIOPermission.Assert(),
        /// providing read access to trusted files under <paramref name="trustedFolder"/>, i.e. additionally
        /// we're making sure that specified trusted locations are under the trusted Folder.
        ///
        /// This is needed to differentiate a case when user passes in a local path location which just happens to be under
        /// trusted folder. We still want to fail in this case, since we want to trust only files that we've created.
        /// </remarks>
        internal override object LoadDictionary(Uri item, string trustedFolder)
        {
            return LoadDictionary(item.LocalPath);
        }

        /// <summary>
        /// Releases all currently loaded lexicons.
        /// </summary>
        internal override void ReleaseAllLexicons()
        {
            ITextContext textContext = null;
            try
            {
                _textChunk.get_Context(out textContext);
                Int32 lexiconCount = 0;
                textContext.get_LexiconCount(out lexiconCount);
                while (lexiconCount > 0)
                {
                    ILexicon lexicon = null;
                    textContext.get_Lexicon(0, out lexicon);
                    textContext.RemoveLexicon(lexicon);
                    Marshal.ReleaseComObject(lexicon);
                    lexiconCount--;
                }
            }
            finally
            {
                if (textContext != null)
                {
                    Marshal.ReleaseComObject(textContext);
                }
            }
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

                if (_mode.HasFlag(SpellerMode.SpellingErrors))
                {
                    SetContextOption("IsSpellChecking", true);

                    if (_mode.HasFlag(SpellerMode.Suggestions))
                    {
                        SetContextOption("IsSpellVerifyOnly", false);
                    }
                    else
                    {
                        SetContextOption("IsSpellVerifyOnly", true);
                    }
                }
                else if (_mode.HasFlag(SpellerMode.WordBreaking))
                {
                    SetContextOption("IsSpellChecking", false);
                }
            }
        }

        /// <summary>
        /// If true, multi-word spelling errors would be detected
        /// </summary>
        internal override bool MultiWordMode
        {
            set
            {
                _multiWordMode = value;
                SetContextOption("IsSpellSuggestingMWEs", _multiWordMode);
            }
        }

        /// <summary>
        /// Sets spelling reform mode
        /// </summary>
        /// <param name="culture"></param>
        /// <param name="spellingReform"></param>
        internal override void SetReformMode(CultureInfo culture, SpellingReform spellingReform)
        {
            const int
                BothPreAndPost = 0,
                Prereform      = 1,
                Postreform     = 2;

            string option;

            switch (culture.TwoLetterISOLanguageName)
            {
                case "de":
                    option = "GermanReform";
                    break;

                case "fr":
                    option = "FrenchReform";
                    break;

                default:
                    option = null;
                    break;
            }

            if (option != null)
            {
                switch (spellingReform)
                {
                    case SpellingReform.Prereform:
                        SetContextOption(option, Prereform);
                        break;

                    case SpellingReform.Postreform:
                        SetContextOption(option, Postreform);
                        break;

                    case SpellingReform.PreAndPostreform:
                        if (option == "GermanReform")
                        {
                            // BothPreAndPost is disallowed for german -- the engine has undefined results.
                            SetContextOption(option, Postreform);
                        }
                        else
                        {
                            SetContextOption(option, BothPreAndPost);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Returns true if we have an engine capable of proofing the specified language.
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        internal override bool CanSpellCheck(CultureInfo culture)
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

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        /// <summary>
        /// ITextRange implementation compatible with NLG API's
        ///  typedef struct STextRange
        /// {
        ///     long Start;
        ///     long Length;
        /// };
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct STextRange : SpellerInteropBase.ITextRange
        {
            #region SpellerInteropBase.ITextRange

            public int Start
            {
                get { return _start; }
            }

            public int Length
            {
                get { return _length; }
            }

            #endregion SpellerInteropBase.ITextRange

            private readonly int _start;
            private readonly int _length;
        }

        /// <summary>
        /// RangeRole enum defined by NLG API's
        /// </summary>
        private enum RangeRole
        {
            ecrrSimpleSegment = 0,
            ecrrAlternativeForm = 1,
            ecrrIncorrect = 2,
            ecrrAutoReplaceForm = 3,
            ecrrCorrectForm = 4,
            ecrrPreferredForm = 5,
            ecrrNormalizedForm = 6,
            ecrrCompoundSegment = 7,
            ecrrPhraseSegment = 8,
            ecrrNamedEntity = 9,
            ecrrCompoundWord = 10,
            ecrrPhrase = 11,
            ecrrUnknownWord = 12,
            ecrrContraction = 13,
            ecrrHyphenatedWord = 14,
            ecrrContractionSegment = 15,
            ecrrHyphenatedSegment = 16,
            ecrrCapitalization = 17,
            ecrrAccent = 18,
            ecrrRepeated = 19,
            ecrrDefinition = 20,
            ecrrOutOfContext = 21,
        };

        /// <summary>
        /// Implementation of ISpellerSegment that manages the lifetime of 
        /// an ITextSegment (NLG COM interface) object
        /// </summary>
        private class SpellerSegment : ISpellerSegment, IDisposable
        {
            #region Constructor 

            public SpellerSegment(ITextSegment textSegment)
            {
                _textSegment = textSegment;
            }

            #endregion Constructor

            #region Private Methods

            /// <summary>
            /// Enumerates spelling suggestions for this segment
            /// </summary>
            private void EnumerateSuggestions()
            {
                List<string> suggestions = new List<string>();

                UnsafeNativeMethods.IEnumVariant variantEnumerator;

                _textSegment.get_Suggestions(out variantEnumerator);

                if (variantEnumerator == null)
                {
                    // nl6 will return null enum instead of an empty enum.
                    _suggestions = suggestions.AsReadOnly();
                    return;
                }

                try
                {
                    NativeMethods.VARIANT variant = new NativeMethods.VARIANT();
                    int[] fetched = new int[1];

                    while (true)
                    {
                        int result;

                        variant.Clear();
                        result = EnumVariantNext(variantEnumerator, variant, fetched);

                        if ((result != NativeMethods.S_OK) || (fetched[0] == 0))
                        {
                            break;
                        }

                        // Convert the VARIANT to string, and add it to our list.
                        // There's some special magic here.  The VARIANT is VT_UI2/ByRef.
                        // But under the hood it's really a raw WCHAR *.
                        suggestions.Add(Marshal.PtrToStringUni(variant.data1.Value));
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(variantEnumerator);
                }

                _suggestions = suggestions.AsReadOnly();
                return;
            }

            /// <summary>
            /// Enumerates sub-segments of this segment
            /// </summary>
            private void EnumerateSubSegments()
            {
                _textSegment.get_Count(out _subSegmentCount);

                List<ISpellerSegment> subSegments = new List<ISpellerSegment>();

                for (int i = 0; i < _subSegmentCount; i++)
                {
                    ITextSegment subSegment;
                    _textSegment.get_Item(i, out subSegment);

                    // subSegment COM object will get released by SpellerSegment's finalizer
                    subSegments.Add(new SpellerSegment(subSegment));
                }

                _subSegments = subSegments.AsReadOnly();
            }

            #endregion

            #region SpellerInteropBase.ISpellerSegment

            /// <summary>
            /// Returns a read-only list of sub-segments of this segment
            /// </summary>
            public IReadOnlyList<ISpellerSegment> SubSegments
            {
                get
                {
                    if (_subSegments == null)
                    {
                        EnumerateSubSegments();
                    }

                    return _subSegments;
                }
            }

            /// <summary>
            /// Identifies, by position, this segment in it's source sentence
            /// </summary>
            public ITextRange TextRange
            {
                get
                {
                    if (_sTextRange == null)
                    {
                        STextRange sTextRange;
                        _textSegment.get_Range(out sTextRange);

                        _sTextRange = sTextRange;
                    }

                    return _sTextRange.Value;
                }
            }

            /// <summary>
            /// Generates spelling suggestions for this segment
            /// If the segment has no suggestions (usually because it is not misspelled,
            /// but also possible for errors the engine cannot make sense of, or that are
            /// contained in sub-segments), this method returns an empty list
            /// </summary>
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

            /// <summary>
            /// Checks whether this segment is free of spelling errors
            /// </summary>
            public bool IsClean 
            {
                get
                {
                    return (RangeRole != RangeRole.ecrrIncorrect);
                }
            }

            /// <summary>
            /// Enumerates a segment's subsegments, making a callback on each iteration.
            /// </summary>
            /// <param name="segmentCallback"></param>
            /// <param name="data"></param>
            public void EnumSubSegments(EnumTextSegmentsCallback segmentCallback, object data)
            {
                bool result = true;

                // Walk the subsegments, the error's in there somewhere.
                for (int i = 0; result && (i < SubSegments.Count); i++)
                {
                    result = segmentCallback(SubSegments[i], data);
                }
            }


            #endregion SpellerInteropBase.ISpellerSegment

            #region IDisposable

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("NLGSpellerInterop.SpellerSegment");
                }

                if (_subSegments != null)
                {
                    foreach (SpellerSegment subSegment in _subSegments)
                    {
                        // Don't call Dispose(disposing) here. That will 
                        // fail to suppress finalization of subsegment objects.
                        subSegment.Dispose();
                    }
                    _subSegments = null;
                }

                if (_textSegment != null)
                {
                    Marshal.ReleaseComObject(_textSegment);
                    _textSegment = null;
                }

                _disposed = true;
            }

            ~SpellerSegment()
            {
                Dispose(false);
            }

            #endregion

            #region Public Properties

            public RangeRole RangeRole
            {
                get
                {
                    if (_rangeRole == null)
                    {
                        RangeRole role;
                        _textSegment.get_Role(out role);

                        _rangeRole = role;
                    }

                    return _rangeRole.Value;
                }
            }

            #endregion Public Properties

            #region Private Fields

            // SpellerInteropBase fields
            private STextRange? _sTextRange = null;
            private int _subSegmentCount;
            private IReadOnlyList<ISpellerSegment> _subSegments = null;
            private IReadOnlyList<string> _suggestions = null;

            // SpellerSegment specific fields
            private RangeRole? _rangeRole = null;
            private ITextSegment _textSegment;

            // IDisposable management
            private bool _disposed = false;

            #endregion Private Fields
        }

        /// <summary>
        /// Implementation of ISpellerSentence that manages the lifetime of
        /// an ISentence (NLG COM interface) object
        /// </summary>
        private class SpellerSentence : ISpellerSentence, IDisposable
        {
            /// <summary>
            /// Constructs a SpellerSentence object 
            /// </summary>
            /// <param name="sentence"></param>
            public SpellerSentence(ISentence sentence)
            {
                _disposed = false;

                try
                {
                    int sentenceSegmentCount;
                    sentence.get_Count(out sentenceSegmentCount);

                    // Iterate over segments.
                    List<ISpellerSegment> segments = new List<ISpellerSegment>();

                    for (int i = 0; i < sentenceSegmentCount; i++)
                    {
                        NLGSpellerInterop.ITextSegment textSegment;
                        sentence.get_Item(i, out textSegment);

                        // SpellerSegment finalizer will take care of releasing the COM object
                        segments.Add(new SpellerSegment(textSegment));
                    }

                    _segments = segments.AsReadOnly();

                    Invariant.Assert(_segments.Count == sentenceSegmentCount);
                }
                finally
                {
                    Marshal.ReleaseComObject(sentence);
                }
            }

            #region SpellerInteropBase.ISpellerSentence

            /// <summary>
            /// Segments that this sentence is comprised of
            /// </summary>
            public IReadOnlyList<ISpellerSegment> Segments
            {
                get
                {
                    return _segments;
                }
            }

            /// <summary>
            /// Final symbol offset of a sentence.
            /// </summary>
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

            #endregion SpellerInteropBase.ISpellerSentence

            #region IDisposable

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("NLGSpellerInterop.SpellerSentence");
                }

                if (_segments != null)
                {
                    foreach (SpellerSegment segment in _segments)
                    {
                        segment.Dispose();
                    }

                    _segments = null;
                }

                _disposed = true;                
            }

            ~SpellerSentence()
            {
                Dispose(false);
            }

            #endregion

            #region Private Fields

            private IReadOnlyList<ISpellerSegment> _segments;
            private bool _disposed;

            #endregion Private Fields
        }

        #endregion Private Types

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods



        //------------------------------------------------------
        //
        //  ILexicon management methods
        //
        //------------------------------------------------------
        #region ILexicon management methods

        /// <summary>
        /// Adds new custom dictionary to the spell engine.
        /// </summary>
        /// <param name="lexiconFilePath"></param>
        /// <returns>Reference to new ILexicon</returns>
        ///
        private ILexicon AddLexicon(string lexiconFilePath)
        {
            ITextContext textContext = null;
            ILexicon lexicon = null;
            bool exception = true;
            bool hasDemand = false;

            try
            {
                hasDemand = true;

                lexicon = NLGSpellerInterop.CreateLexicon();
                lexicon.ReadFrom(lexiconFilePath);
                _textChunk.get_Context(out textContext);
                textContext.AddLexicon(lexicon);
                exception = false;
            }
            catch (Exception e)
            {
                // We'll provide details of exception only if Demand to access lexiconFilePath was satisfied.
                // Otherwise it's a security concern to disclose this data.
                if (hasDemand)
                {
                    throw new ArgumentException(SR.Get(SRID.CustomDictionaryFailedToLoadDictionaryUri, lexiconFilePath), e);
                }
                else
                {
                    throw;// Demand has failed so we're rethrowing security exception.
                }
            }
            finally
            {
                if ((exception) &&(lexicon != null))
                {
                    Marshal.ReleaseComObject(lexicon);
                }
                if (null != textContext)
                {
                    Marshal.ReleaseComObject(textContext);
                }
            }
            return lexicon;
        }

        #endregion ILexicon management methods


        // Returns an object exported from NaturalLanguage6.dll's class factory.
        private static object CreateInstance(Guid clsid, Guid iid)
        {
            object classObject;
            UnsafeNlMethods.NlGetClassObject(ref clsid, ref iid, out classObject);
            return classObject;
        }

        // Creates a new ITextContext instance.
        private static ITextContext CreateTextContext()
        {
            return (ITextContext)CreateInstance(CLSID_ITextContext, IID_ITextContext);
        }

        // Creates a new ITextChunk instance.
        private static ITextChunk CreateTextChunk()
        {
            return (ITextChunk)CreateInstance(CLSID_ITextChunk, IID_ITextChunk);
        }

        // Creates a new ILexicon instance.
        private static ILexicon CreateLexicon()
        {
            return (ILexicon)CreateInstance(CLSID_Lexicon, IID_ILexicon);
        }



        // Helper for IEnumVariant.Next call -- the debugger isn't displaying
        // variables in any method with the call.
        private static int EnumVariantNext(UnsafeNativeMethods.IEnumVariant variantEnumerator, NativeMethods.VARIANT variant, int[] fetched)
        {
            int result;

            unsafe
            {
                fixed (void* pVariant = &variant.vt)
                {
                    result = variantEnumerator.Next(1, (IntPtr)pVariant, fetched);
                }
            }

            return result;
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Interfaces
        //
        //------------------------------------------------------

        #region Private Interfaces

        private static class UnsafeNlMethods
        {
            [DllImport(DllImport.PresentationNative, PreserveSig = false)]
            internal static extern void NlLoad();

            [DllImport(DllImport.PresentationNative, PreserveSig = true)]
            internal static extern void NlUnload();

            [DllImport(DllImport.PresentationNative, PreserveSig = false)]
            internal static extern void NlGetClassObject(ref Guid clsid, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object classObject);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("004CD7E2-8B63-4ef9-8D46-080CDBBE47AF")]
        internal interface ILexicon
        {
            //[
            //]
            //HRESULT ReadFrom ([in] BSTR filename);
            void ReadFrom ([MarshalAs( UnmanagedType.BStr )]string fileName);

            //[
            //]
            //HRESULT WriteTo ([in] BSTR filename);
            void stub_WriteTo ();

            //[
            //]
            //HRESULT GetEnumerator ([retval,out] ILexiconEntryEnumerator **enumerator);
            void stub_GetEnumerator ();

            //[
            //]
            //HRESULT IndexOf (
            //             [in] BSTR word,
            //             [out,retval] long *index);
            void stub_IndexOf();

            //[
            //]
            //HRESULT TagFor (
            //             [in] BSTR word,
            //             [in] long tagIndex,
            //             [out,retval] long *index);
            void stub_TagFor ();

            //[
            //]
            //HRESULT ContainsPrefix (
            //             [in] BSTR prefix,
            //             [out,retval] VARIANT_BOOL *containsPrefix);
            void stub_ContainsPrefix();

            //[
            //]
            //HRESULT Add ([in] BSTR entry);
            void stub_Add();

            //[
            //]
            //HRESULT Remove ([in] BSTR entry);
        	void stub_Remove();
            //[
            //    propget
            //]
            //HRESULT Version ([out, retval, ref] BSTR *pval);
            void stub_Version();


            //[
            //    helpstring("The number of elements in this collection."),
            //    propget
            //]
            //HRESULT Count ([out, retval, ref] long *pval);
            void stub_Count();


            //[
            //    helpstring("Get an enumerator of elements in this collection."),
            //    restricted,
            //    propget
            //]
            //HRESULT _NewEnum ([out, retval, ref] IEnumVARIANT **pval);
            void stub__NewEnum();


            //[
            //    propget
            //]
            //HRESULT Item (
            //             [in] long key,
            //    [out, retval, ref] ILexiconEntry **pval);
            void stub_get_Item();

            //[
            //    propput
            //]
            //HRESULT Item (
            //             [in] long key,
            //    [in] ILexiconEntry *val);
            void stub_set_Item();

            //[
            //    propget
            //]
            //HRESULT ItemByName (
            //             [in] BSTR key,
            //    [out, retval, ref] ILexiconEntry **pval);
            void stub_get_ItemByName();

            //[
            //    propput
            //]
            //HRESULT ItemByName (
            //             [in] BSTR key,
            //    [in] ILexiconEntry *val);
            void stub_set_ItemByName();

            //[
            //    propget
            //]
            //HRESULT PropertyCount ([out, retval, ref] long *pval);
            void stub_get0_PropertyCount();


            //[
            //    helpstring("The keys for this dictionary are the names of the properties, the value are VARIANTS."),
            //    propget
            //]
            //HRESULT Property (
            //             [in] VARIANT index,
            //    [out, retval, ref] VARIANT *pval);
            void stub_get1_Property();

            //[
            //    helpstring("The keys for this dictionary are the names of the properties, the value are VARIANTS."),
            //    propput
            //]
            //HRESULT Property (
            //             [in] VARIANT index,
            //    [in] VARIANT val);
            void stub_set_Property();

            //[
            //    propget
            //]
            //HRESULT IsSealed ([out, retval, ref] VARIANT_BOOL *pval);
            void stub_get_IsSealed();


            //[
            //    propget
            //]
            //HRESULT IsReadOnly ([out, retval, ref] VARIANT_BOOL *pval);
            void stub_get_IsReadOnly();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("B6797CC0-11AE-4047-A438-26C0C916EB8D")]
        private interface ITextContext
        {
            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_PropertyCount )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_PropertyCount();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Property )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT index,
            //     /* [ref][retval][out] */ VARIANT *pval);
            void stub_get_Property();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_Property )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT index,
            //     /* [in] */ VARIANT val);
            void stub_put_Property();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_DefaultDialectCount )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_DefaultDialectCount();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_DefaultDialect )(
            //     ITextContext * This,
            //     /* [in] */ long index,
            //     /* [ref][retval][out] */ LCID *pval);
            void stub_get_DefaultDialect();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *AddDefaultDialect )(
            //     ITextContext * This,
            //     /* [in] */ LCID dicalect);
            void stub_AddDefaultDialect();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *RemoveDefaultDialect )(
            //     ITextContext * This,
            //     /* [in] */ LCID dicalect);
            void stub_RemoveDefaultDialect();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_LexiconCount )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ long *pval);
            void get_LexiconCount([MarshalAs(UnmanagedType.I4)] out Int32 lexiconCount);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Lexicon )(
            //     ITextContext * This,
            //     /* [in] */ long index,
            //     /* [ref][retval][out] */ ILexicon **pval);
            void get_Lexicon(Int32 index, [MarshalAs(UnmanagedType.Interface)] out ILexicon lexicon);

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *AddLexicon )(
            //     ITextContext * This,
            //     /* [in] */ ILexicon *pLexicon);
            void AddLexicon([In, MarshalAs(UnmanagedType.Interface)] ILexicon lexicon);

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *RemoveLexicon )(
            //     ITextContext * This,
            //     /* [in] */ ILexicon *pLexicon);
            void RemoveLexicon([In, MarshalAs(UnmanagedType.Interface)] ILexicon lexicon);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Version )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ BSTR *pval);
            void stub_get_Version();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_ResourceLoader )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ ILoadResources **pval);
            void stub_get_ResourceLoader();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_ResourceLoader )(
            //     ITextContext * This,
            //     /* [in] */ ILoadResources *val);
            void stub_put_ResourceLoader();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Options )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ IProcessingOptions **pval);
            void get_Options([MarshalAs(UnmanagedType.Interface)] out IProcessingOptions val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Capabilities )(
            //     ITextContext * This,
            //     /* [in] */ LCID locale,
            //     /* [ref][retval][out] */ IProcessingOptions **pval);
            void get_Capabilities(Int32 locale, [MarshalAs(UnmanagedType.Interface)] out IProcessingOptions val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Lexicons )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_Lexicons();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_Lexicons )(
            //     ITextContext * This,
            //     /* [in] */ IEnumVARIANT *val);
            void stub_put_Lexicons();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_MaxSentences )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_MaxSentences();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_MaxSentences )(
            //     ITextContext * This,
            //     /* [in] */ long val);
            void stub_put_MaxSentences();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsSingleLanguage )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsSingleLanguage();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsSingleLanguage )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsSingleLanguage();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsSimpleWordBreaking )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsSimpleWordBreaking();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsSimpleWordBreaking )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsSimpleWordBreaking();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_UseRelativeTimes )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_UseRelativeTimes();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_UseRelativeTimes )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_UseRelativeTimes();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IgnorePunctuation )(
            // ITextContext * This,
            // /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IgnorePunctuation();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IgnorePunctuation )(
            // ITextContext * This,
            // /* [in] */ VARIANT_BOOL val);
            void stub_put_IgnorePunctuation();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsCaching )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsCaching();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsCaching )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsCaching();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsShowingGaps )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsShowingGaps();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsShowingGaps )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsShowingGaps();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsShowingCharacterNormalizations )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsShowingCharacterNormalizations();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsShowingCharacterNormalizations )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsShowingCharacterNormalizations();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsShowingWordNormalizations )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsShowingWordNormalizations();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsShowingWordNormalizations )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsShowingWordNormalizations();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsComputingCompounds )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsComputingCompounds();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsComputingCompounds )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsComputingCompounds();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsComputingInflections )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsComputingInflections();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsComputingInflections )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsComputingInflections();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsComputingLemmas )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsComputingLemmas();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsComputingLemmas )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsComputingLemmas();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsComputingExpansions )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsComputingExpansions();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsComputingExpansions )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsComputingExpansions();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsComputingBases )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsComputingBases();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsComputingBases )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsComputingBases();

            // /* [propget][helpstring] */ HRESULT STDMETHODCALLTYPE get_IsComputingPartOfSpeechTags(
            // /* [ref][retval][out] */ VARIANT_BOOL *pval) = 0;
            void stub_get_IsComputingPartOfSpeechTags();

            // /* [propput][helpstring] */ HRESULT STDMETHODCALLTYPE put_IsComputingPartOfSpeechTags(
            // /* [in] */ VARIANT_BOOL val) = 0;
            void stub_put_IsComputingPartOfSpeechTags();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsFindingDefinitions )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsFindingDefinitions();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsFindingDefinitions )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsFindingDefinitions();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsFindingDateTimeMeasures )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsFindingDateTimeMeasures();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsFindingDateTimeMeasures )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsFindingDateTimeMeasures();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsFindingPersons )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsFindingPersons();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsFindingPersons )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsFindingPersons();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsFindingLocations )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsFindingLocations();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsFindingLocations )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsFindingLocations();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsFindingOrganizations )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsFindingOrganizations();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsFindingOrganizations )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsFindingOrganizations();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsFindingPhrases )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsFindingPhrases();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsFindingPhrases )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsFindingPhrases();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("549F997E-0EC3-43d4-B443-2BF8021010CF")]
        private interface ITextChunk
        {
            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_InputText )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ BSTR *pval);
            void stub_get_InputText();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_InputText )(
            //     ITextChunk * This,
            //     /* [in] */ BSTR val);
            void stub_put_InputText();

            // /* [restricted][helpstring] */ HRESULT ( STDMETHODCALLTYPE *SetInputArray )(
            //     ITextChunk * This,
            //     /* [string][in] */ LPCWSTR str,
            //     /* [in] */ long size);
            void SetInputArray([In] IntPtr inputArray, Int32 size);

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *RegisterEngine )(
            //     ITextChunk * This,
            //     /* [in] */ GUID *guid,
            //     /* [in] */ BSTR dllName);
            void stub_RegisterEngine();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *UnregisterEngine )(
            //     ITextChunk * This,
            //     /* [in] */ GUID *guid);
            void stub_UnregisterEngine();

            // /* [propget][restricted][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_InputArray )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ LPCWSTR *pval);
            void stub_get_InputArray();

            // /* [propget][restricted][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_InputArrayRange )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ STextRange *pval);
            void stub_get_InputArrayRange();

            // /* [propput][restricted][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_InputArrayRange )(
            //     ITextChunk * This,
            //     /* [in] */ STextRange val);
            void stub_put_InputArrayRange();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Count )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ long *pval);
            void get_Count(out Int32 val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Item )(
            //     ITextChunk * This,
            //     /* [in] */ long index,
            //     /* [ref][retval][out] */ ISentence **pval);
            void get_Item(Int32 index, [MarshalAs(UnmanagedType.Interface)] out ISentence val);

            // /* [propget][restricted][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get__NewEnum )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get__NewEnum();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Sentences )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void get_Sentences([MarshalAs(UnmanagedType.Interface)] out MS.Win32.UnsafeNativeMethods.IEnumVariant val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_PropertyCount )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_PropertyCount();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Property )(
            //     ITextChunk * This,
            //     /* [in] */ VARIANT index,
            //     /* [ref][retval][out] */ VARIANT *pval);
            void stub_get_Property();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_Property )(
            //     ITextChunk * This,
            //     /* [in] */ VARIANT index,
            //     /* [in] */ VARIANT val);
            void stub_put_Property();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Context )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ ITextContext **pval);
            void get_Context([MarshalAs(UnmanagedType.Interface)] out ITextContext val);

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_Context )(
            //     ITextChunk * This,
            //     /* [in] */ ITextContext *val);
            void put_Context([MarshalAs(UnmanagedType.Interface)] ITextContext val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Locale )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ LCID *pval);
            void stub_get_Locale();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_Locale )(
            //     ITextChunk * This,
            //     /* [in] */ LCID val);
            void put_Locale(Int32 val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsLocaleReliable )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsLocaleReliable();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsLocaleReliable )(
            //     ITextChunk * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsLocaleReliable();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsEndOfDocument )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsEndOfDocument();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsEndOfDocument )(
            //     ITextChunk * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsEndOfDocument();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetEnumerator )(
            //     ITextChunk * This,
            //     /* [retval][out] */ IEnumVARIANT **ppSent);
            void GetEnumerator([MarshalAs(UnmanagedType.Interface)] out MS.Win32.UnsafeNativeMethods.IEnumVariant val);

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ToString )(
            //     ITextChunk * This,
            //     /* [retval][out] */ BSTR *pstr);
            void stub_ToString();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ProcessStream )(
            //     ITextChunk * This,
            //     /* [in] */ IRangedTextSource *input,
            //     /* [out][in] */ IRangedTextSink *output);
            void stub_ProcessStream();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_ReuseObjects )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void get_ReuseObjects(out bool val);

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_ReuseObjects )(
            //     ITextChunk * This,
            //     /* [in] */ VARIANT_BOOL val);
            void put_ReuseObjects(bool val);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("F0C13A7A-199B-44be-8492-F91EAA50F943")]
        private interface ISentence
        {
            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_PropertyCount )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_PropertyCount();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Property )(
            //     ISentence * This,
            //     /* [in] */ VARIANT index,
            //     /* [ref][retval][out] */ VARIANT *pval);
            void stub_get_Property();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_Property )(
            //     ISentence * This,
            //     /* [in] */ VARIANT index,
            //     /* [in] */ VARIANT val);
            void stub_put_Property();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Count )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ long *pval);
            void get_Count(out Int32 val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Parent )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ ITextChunk **pval);
            void stub_get_Parent();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Item )(
            //     ISentence * This,
            //     /* [in] */ long index,
            //     /* [ref][retval][out] */ ITextSegment **pval);
            void get_Item(Int32 index, [MarshalAs(UnmanagedType.Interface)] out ITextSegment val);

            // /* [propget][restricted][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get__NewEnum )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get__NewEnum();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Segments )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_Segments();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetEnumerator )(
            //     ISentence * This,
            //     /* [retval][out] */ IEnumVARIANT **string);
            void stub_GetEnumerator();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsEndOfParagraph )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsEndOfParagraph();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsUnfinished )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsUnfinished();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsUnfinishedAtEnd )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsUnfinishedAtEnd();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Locale )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ LCID *pval);
            void stub_get_Locale();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsLocaleReliable )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsLocaleReliable();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Range )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ STextRange *pval);
            void stub_get_Range();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_RequiresNormalization )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_RequiresNormalization();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ToString )(
            //     ISentence * This,
            //     /* [retval][out] */ BSTR *string);
            void stub_ToString();

            // /* [helpstring][restricted] */ HRESULT ( STDMETHODCALLTYPE *CopyToString )(
            //     ISentence * This,
            //     /* [in][string] */ LPWSTR pStr,
            //     /* [in][out] */ long* pcch,
            //     /* [in] */ VARIANT_BOOL fAlwaysCopy);
            void stub_CopyToString();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("AF4656B8-5E5E-4fb2-A2D8-1E977E549A56")]
        private interface ITextSegment
        {
            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsSurfaceString )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsSurfaceString();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Range )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ STextRange *pval);
            void get_Range([MarshalAs(UnmanagedType.Struct)] out STextRange val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Identifier )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_Identifier();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Unit )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ MeasureUnit *pval);
            void stub_get_Unit();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Count )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ long *pval);
            void get_Count(out Int32 val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Item )(
            //     ITextSegment * This,
            //     /* [in] */ long index,
            //     /* [ref][retval][out] */ ITextSegment **pval);
            void get_Item(Int32 index, [MarshalAs(UnmanagedType.Interface)] out ITextSegment val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Expansions )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_Expansions();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Bases )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_Bases();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_SuggestionScores )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_SuggestionScores();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_PropertyCount )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_PropertyCount();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Property )(
            //     ITextSegment * This,
            //     /* [in] */ VARIANT index,
            //     /* [ref][retval][out] */ VARIANT *pval);
            void stub_get_Property();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_Property )(
            //     ITextSegment * This,
            //     /* [in] */ VARIANT index,
            //     /* [in] */ VARIANT val);
            void stub_put_Property();

            // /* [helpstring][restricted] */ HRESULT ( STDMETHODCALLTYPE *CopyToString )(
            //     ISentence * This,
            //     /* [in][string] */ LPWSTR pStr,
            //     /* [in][out] */ long* pcch,
            //     /* [in] */ VARIANT_BOOL fAlwaysCopy);
            void stub_CopyToString();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Role )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ RangeRole *pval);
            void get_Role(out RangeRole val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_PrimaryType )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ PrimaryRangeType *pval);
            void stub_get_PrimaryType();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_SecondaryType )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ SecondaryRangeType *pval);
            void stub_get_SecondaryType();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_SpellingVariations )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_SpellingVariations();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_CharacterNormalizations )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_CharacterNormalizations();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Representations )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_Representations();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Inflections )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_Inflections();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Suggestions )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void get_Suggestions([MarshalAs(UnmanagedType.Interface)] out MS.Win32.UnsafeNativeMethods.IEnumVariant val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Lemmas )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_Lemmas();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_SubSegments )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_SubSegments();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Alternatives )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_Alternatives();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ToString )(
            //     ITextSegment * This,
            //     /* [retval][out] */ BSTR *string);
            void stub_ToString();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsPossiblePhraseStart )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsPossiblePhraseStart();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_SpellingScore )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_SpellingScore();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsPunctuation )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsPunctuation();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsEndPunctuation )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsEndPunctuation();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsSpace )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsSpace();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsAbbreviation )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsAbbreviation();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsSmiley )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsSmiley();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("C090356B-A6A5-442a-A204-CFD5415B5902")]
        private interface IProcessingOptions
        {
            // /* [propget][restricted][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get__NewEnum )(
            //     IProcessingOptions * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get__NewEnum();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetEnumerator )(
            //     IProcessingOptions * This,
            //     /* [retval][out] */ IEnumVARIANT **ppSent);
            void stub_GetEnumerator();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Locale )(
            //     IProcessingOptions * This,
            //     /* [ref][retval][out] */ LCID *pval);
            void stub_get_Locale();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Count )(
            //     IProcessingOptions * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_Count();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Name )(
            //     IProcessingOptions * This,
            //     /* [in] */ long index,
            //     /* [ref][retval][out] */ BSTR *pval);
            void stub_get_Name();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Item )(
            //     IProcessingOptions * This,
            //     /* [in] */ VARIANT index,
            //     /* [ref][retval][out] */ VARIANT *pval);
            void stub_get_Item();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_Item )(
            //     IProcessingOptions * This,
            //     /* [in] */ VARIANT index,
            //     /* [in] */ VARIANT val);
            void put_Item(object index, object val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsReadOnly )(
            //     IProcessingOptions * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsReadOnly();
        }

        #endregion Private Interfaces

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private ITextChunk _textChunk;

        // True after this object has been disposed.
        private bool _isDisposed;

        // Speller mode 
        private SpellerMode _mode;

        // Multi-word error checking mode
        private bool _multiWordMode;

        // 333E6924-4353-4934-A7BE-5FB5BDDDB2D6
        private static readonly Guid CLSID_ITextContext = new Guid(0x333E6924, 0x4353, 0x4934, 0xA7, 0xBE, 0x5F, 0xB5, 0xBD, 0xDD, 0xB2, 0xD6);

        // B6797CC0-11AE-4047-A438-26C0C916EB8D
        private static readonly Guid IID_ITextContext = new Guid(0xB6797CC0, 0x11AE, 0x4047, 0xA4, 0x38, 0x26, 0xC0, 0xC9, 0x16, 0xEB, 0x8D);

        // 89EA5B5A-D01C-4560-A874-9FC92AFB0EFA
        private static readonly Guid CLSID_ITextChunk = new Guid(0x89EA5B5A, 0xD01C, 0x4560, 0xA8, 0x74, 0x9F, 0xC9, 0x2A, 0xFB, 0x0E, 0xFA);

        // 549F997E-0EC3-43d4-B443-2BF8021010CF
        private static readonly Guid IID_ITextChunk = new Guid(0x549F997E, 0x0EC3, 0x43d4, 0xB4, 0x43, 0x2B, 0xF8, 0x02, 0x10, 0x10, 0xCF);

        private static readonly Guid CLSID_Lexicon = new Guid("D385FDAD-D394-4812-9CEC-C6575C0B2B38");
        private static readonly Guid IID_ILexicon = new Guid("004CD7E2-8B63-4ef9-8D46-080CDBBE47AF");

        #endregion Private Fields
    }
}

