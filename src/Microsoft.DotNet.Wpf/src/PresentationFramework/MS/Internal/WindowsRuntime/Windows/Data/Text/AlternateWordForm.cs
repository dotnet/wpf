// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Provides an adapter class for the WinRT type Windows.Data.Text.AlternateWordForm.
//                  AlternateWordForm identifies an alternate form of the word represented by a WordSegment
//                  object. For example, this may contain a number in a normalized format.
//

using System;
using System.Runtime.CompilerServices;

namespace MS.Internal.WindowsRuntime
{
    namespace Windows.Data.Text
    {
        internal enum AlternateNormalizationFormat
        {
            NotNormalized = 0,
            Number = 1,
            Currency = 2,
            Date = 3,
            Time = 4,
        };

        internal class AlternateWordForm
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            static AlternateWordForm()
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

            public AlternateWordForm(object alternateWordForm)
            {
                if (s_WinRTType == null) throw new PlatformNotSupportedException();
                if (alternateWordForm.GetType() != s_WinRTType) throw new ArgumentException();

                _alternateWordForm = alternateWordForm;
            }

            public string AlternateText
            {
                get
                {
                    if (_alternateText == null)
                    {
                        _alternateText = _alternateWordForm.ReflectionGetProperty<string>("AlternateText");
                    }

                    return _alternateText;
                }
            }

            public AlternateNormalizationFormat NormalizationFormat
            {
                get
                {
                    if (_alternateNormalizationFormat == null)
                    {
                        _alternateNormalizationFormat = 
                            _alternateWordForm.ReflectionGetProperty<AlternateNormalizationFormat>("NormalizationFormat");
                    }

                    return _alternateNormalizationFormat.Value;
                }
            }

            public TextSegment SourceTextSegment
            {
                get
                {
                    if (_sourceTextSegment == null)
                    {
                        _sourceTextSegment = new TextSegment(_alternateWordForm.ReflectionGetProperty("SourceTextSegment"));
                    }

                    return _sourceTextSegment;
                }
            }

            public static Type WinRTType
            {
                get
                {
                    return s_WinRTType;
                }
            }


            private static Type s_WinRTType = null;
            private static string s_TypeName = "Windows.Data.Text.AlternateWordForm, Windows, ContentType=WindowsRuntime";

            private object _alternateWordForm = null;

            private TextSegment _sourceTextSegment = null;
            private AlternateNormalizationFormat? _alternateNormalizationFormat = null;
            private string _alternateText = null;
        }
}
}