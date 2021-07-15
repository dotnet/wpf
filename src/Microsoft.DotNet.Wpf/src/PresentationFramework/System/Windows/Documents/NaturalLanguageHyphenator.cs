// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   Implementation of TextLexicalService abstract class used by TextFormatter for
//   document layout. This implementation is based on the hyphenation service in
//   NaturalLanguage6.dll - the component owned by the Natural Language Team.
//

using System.Security;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.TextFormatting;
using MS.Win32;
using MS.Internal;
using DllImport=MS.Internal.PresentationFramework.DllImport;

namespace System.Windows.Documents
{
    /// <summary>
    /// The NLG hyphenation-based implementation of TextLexicalService used by TextFormatter
    /// for line-breaking purpose.
    /// </summary>
    internal class NaturalLanguageHyphenator : TextLexicalService, IDisposable
    {
        private IntPtr  _hyphenatorResource;
        private bool    _disposed;


        /// <summary>
        /// Construct an NLG-based hyphenator
        /// </summary>
        internal NaturalLanguageHyphenator()
        {
            try
            {
                _hyphenatorResource = UnsafeNativeMethods.NlCreateHyphenator();
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }


        /// <summary>
        /// Finalize hyphenator's unmanaged resource
        /// </summary>
        ~NaturalLanguageHyphenator()
        {
            CleanupInternal(true);
        }


        /// <summary>
        /// Dispose hyphenator's unmanaged resource
        /// </summary>
        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
            CleanupInternal(false);
        }


        /// <summary>
        /// Internal clean-up routine
        /// </summary>
        private void CleanupInternal(bool finalizing)
        {
            if (!_disposed && _hyphenatorResource != IntPtr.Zero)
            {
                UnsafeNativeMethods.NlDestroyHyphenator(ref _hyphenatorResource);
                _disposed = true;
            }
        }


        /// <summary>
        /// TextFormatter to query whether the lexical services component could provides
        /// analysis for the specified culture.
        /// </summary>
        /// <param name="culture">Culture whose text is to be analyzed</param>
        /// <returns>Boolean value indicates whether the specified culture is supported</returns>
        public override bool IsCultureSupported(CultureInfo culture)
        {
            // Accept all cultures for the time being. Ideally NL6 should provide a way for the client
            // to test supported culture.
            return true;
        }


        /// <summary>
        /// TextFormatter to get the lexical breaks of the specified raw text
        /// </summary>
        /// <param name="characterSource">character array</param>
        /// <param name="length">number of character in the character array to analyze</param>
        /// <param name="textCulture">culture of the specified character source</param>
        /// <returns>lexical breaks of the text</returns>
        public override TextLexicalBreaks AnalyzeText(
            char[]          characterSource,
            int             length,
            CultureInfo     textCulture
            )
        {
            Invariant.Assert(
                    characterSource != null
                &&  characterSource.Length > 0
                &&  length > 0
                &&  length <= characterSource.Length
                );

            if (_hyphenatorResource == IntPtr.Zero)
            {
                // No hyphenator available, no service delivered
                return null;
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(SR.Get(SRID.HyphenatorDisposed));
            }

            byte[] isHyphenPositions = new byte[(length + 7) / 8];

            UnsafeNativeMethods.NlHyphenate(
                _hyphenatorResource,
                characterSource,
                length,
                ((textCulture != null && textCulture != CultureInfo.InvariantCulture) ? textCulture.LCID : 0),
                isHyphenPositions,
                isHyphenPositions.Length
                );

            return new HyphenBreaks(isHyphenPositions, length);
        }

        /// <summary>
        /// Private implementation of TextLexicalBreaks that encapsulates hyphen opportunities within
        /// a character string.
        /// </summary>
        private class HyphenBreaks : TextLexicalBreaks
        {
            private byte[]  _isHyphenPositions;
            private int     _numPositions;


            internal HyphenBreaks(byte[] isHyphenPositions, int numPositions)
            {
                _isHyphenPositions = isHyphenPositions;
                _numPositions = numPositions;
            }


            /// <summary>
            /// Indexer for the value at the nth break index (bit nth of the logical bit array)
            /// </summary>
            private bool this[int index]
            {
                get { return (_isHyphenPositions[index / 8] & (1 << index % 8)) != 0; }
            }


            public override int Length
            {
                get
                {
                    return _numPositions;
                }
            }


            public override int GetNextBreak(int currentIndex)
            {
                if (_isHyphenPositions != null && currentIndex >= 0)
                {
                    int ich = currentIndex + 1;
                    while (ich < _numPositions && !this[ich])
                        ich++;

                    if (ich < _numPositions)
                        return ich;
                }
                // return negative value when break is not found.
                return -1;
            }


            public override int GetPreviousBreak(int currentIndex)
            {
                if (_isHyphenPositions != null && currentIndex < _numPositions)
                {
                    int ich = currentIndex;
                    while (ich > 0 && !this[ich])
                        ich--;

                    if (ich > 0)
                        return ich;
                }
                // return negative value when break is not found.
                return -1;
            }
        }


        private static class UnsafeNativeMethods
        {
            [DllImport(DllImport.PresentationNative, PreserveSig = false)]
            internal static extern IntPtr NlCreateHyphenator();

            [DllImport(DllImport.PresentationNative, PreserveSig = true)]
            internal static extern void NlDestroyHyphenator(ref IntPtr hyphenator);

            [DllImport(DllImport.PresentationNative, PreserveSig = false)]
            internal static extern void NlHyphenate(
                IntPtr          hyphenator,
                [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeParamIndex = 2)]
                char[]          inputText,
                int             textLength,
                int             localeID,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)]
                byte[]          hyphenBreaks,
                int             numPositions
                );
        }
    }
}


