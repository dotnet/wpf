// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Provides a class describing the WinRT type 
//                  Windows.Data.Text.WordsSegmenter. In addition to this, it also 
//                  defines a related delegate type used to support methods in this class.
//
//              WordsSegmenter is a class able to segment provided text into words.  
//              
//              The language supplied when these objects are constructed is matched against the 
//                  languages with word breakers on the system, and the best word segmentation rules 
//                  available are used. The language need not be one of the appliation's supported 
//                  languages. If there are no supported language rules available specifically for 
//                  that language, the language-neutral rules are used (an implementation of Unicode 
//                  Standard Annex #29 Unicode Text Segmentation), and the ResolvedLanguage property is 
//                  set to "und" (undetermined language).
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using MS.Internal.PresentationFramework.Interop;


namespace MS.Internal.WindowsRuntime
{
    namespace Windows.Data.Text
    {
        internal partial class WordsSegmenter
        {
            /// <summary>
            /// </summary>
            /// <param name="language"></param>
            /// <returns></returns>
            /// <exception cref="System.ArgumentException"><paramref name="language"/> is not a well-formed language identifier</exception>
            /// <exception cref="System.NotSupportedException"><paramref name="language"/> is not supported</exception>
            /// <exception cref="PlatformNotSupportedException">The OS platform is not supported</exception>
            public static WordsSegmenter Create(string language, bool shouldPreferNeutralSegmenter = false)
            {
                if (!OSVersionHelper.IsOsWindows8Point1OrGreater)
                {
                    throw new PlatformNotSupportedException();
                }
                if (shouldPreferNeutralSegmenter)
                {
                    if (!ShouldUseDedicatedSegmenter(language))
                    {
                        language = Undetermined;
                    }
                }

                return new WordsSegmenter(language);
            }

            private static bool ShouldUseDedicatedSegmenter(string languageTag)
            {
                bool result = true;

                try
                {
                    var language = new Globalization.Language(languageTag);
                    string script = language.Script;

                    if (ScriptCodesRequiringDedicatedSegmenter.FindIndex(s => s.Equals(script, StringComparison.InvariantCultureIgnoreCase)) == -1)
                    {
                        result = false;
                    }
                }
                catch (Exception e) 
                    when ((e is NotSupportedException)      || 
                          (e is ArgumentException)          || 
                          (e is TargetInvocationException)  || 
                          (e is MissingMemberException))
                {
                    // Do nothing - return true as the default result
                }

                return result;
            }

            /// <summary>
            /// List of ISO-15924 script codes of langugues for which we must use 
            /// a dedicated word-breaker. These languages are broken using rules 
            /// different than those for "spaced" languages. 
            /// </summary>
            private static List<string> ScriptCodesRequiringDedicatedSegmenter { get; } = new List<string>
            {
                "Bopo", // Bopomofo
                "Brah", // Brahmi
                "Egyp", // Egyptian hieroglyphs
                "Goth", // Gothic
                "Hang", // Hangul (Hangul, Hangeul)
                "Hani", // Han (Hanzi, Kanji, Hanja)
                "Ital", // Old Italic (Etruscan, Oscan, etc.)
                "Java", // Javanese
                "Kana", // Katakana
                "Khar", // Kharoshthi
                "Laoo", // Lao
                "Lisu", // Lisu (Fraser)
                "Mymr", // Myanmar (Burmese)
                "Talu", // New Tai Lue
                "Thai", // Thai
                "Tibt", // Tibetan
                "Xsux", // Cuneiform, Sumero-Akkadian
                "Yiii", // Yi
            };


            public static readonly string Undetermined = "und";
        }
    }
}
