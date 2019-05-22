// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Provides an adapter class describing WinRT type
//                  Windows.Data.Text.WordSegment
//
//              WordSegment represents a word from your provided text. Words in this class
//                  do not include trailing whitespace or punctuation. This class can also expose
//                  alternate forms of words, and normalized numbers, currencies, dates and times. 
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MS.Internal.WindowsRuntime
{
    namespace Windows.Data.Text
    {
        internal class WordSegment
        {
            static WordSegment()
            {
                try
                {
                    s_WinRTType = Type.GetType(s_TypeName);
                }
                catch
                {
                    s_WinRTType = null;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="wordSegment"></param>
            public WordSegment(object wordSegment)
            {
                if (s_WinRTType == null) throw new PlatformNotSupportedException();
                if (wordSegment.GetType() != s_WinRTType) throw new ArgumentException();

                _wordSegment = wordSegment;
            }

            public IReadOnlyList<AlternateWordForm> AlternateForms
            {
                get
                {
                    if (_alternateForms == null)
                    {
                        object alternates = _wordSegment.ReflectionGetProperty("AlternateForms");

                        List<AlternateWordForm> result = new List<AlternateWordForm>();
                        foreach (var item in (IEnumerable)alternates)
                        {
                            result.Add(new AlternateWordForm(item));
                        }

                        _alternateForms = result.AsReadOnly();
                    }


                    return _alternateForms;
                }
            }

            public TextSegment SourceTextSegment
            {
                get
                {
                    if (_sourceTextSegment == null)
                    {
                        _sourceTextSegment = new TextSegment(_wordSegment.ReflectionGetProperty("SourceTextSegment"));
                    }

                    return _sourceTextSegment;
                }
            }

            public string Text
            {
                get
                {
                    if (_text == null)
                    {
                        _text = _wordSegment.ReflectionGetProperty<string>("Text");
                    }

                    return _text;
                }
            }

            public static Type WinRTType
            {
                get
                {
                    return s_WinRTType; ;
                }
            }

            private object _wordSegment;
            private IReadOnlyList<AlternateWordForm> _alternateForms = null;
            private TextSegment _sourceTextSegment = null;
            private string _text = null;

            private static string s_TypeName = "Windows.Data.Text.WordSegment, Windows, ContentType=WindowsRuntime";
            private static Type s_WinRTType = null;
        }
}
}