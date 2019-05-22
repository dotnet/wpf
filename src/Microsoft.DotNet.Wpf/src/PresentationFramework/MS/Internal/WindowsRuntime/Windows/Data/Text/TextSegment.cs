// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Provides an implementation of the WinRT structure 
//                  Windows.Data.Text.TextSegment, which identifies a sub-string of a 
//                  source text string. 
//

using System;

namespace MS.Internal.WindowsRuntime
{
    namespace Windows.Data.Text
    {
        internal class TextSegment
        {
            static TextSegment()
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

            public TextSegment(object textSegment)
            {
                if (s_WinRTType == null) throw new PlatformNotSupportedException();
                if (textSegment.GetType() != s_WinRTType) throw new ArgumentException();

                _textSegment = textSegment;
            }

            public uint Length
            {
                get
                {
                    if (_length == null)
                    {
                        _length = _textSegment.ReflectionGetField<uint>("Length");
                    }

                    return _length.Value;
                }
            }

            public uint StartPosition
            {
                get
                {
                    if (_startPosition == null)
                    {
                        _startPosition = _textSegment.ReflectionGetField<uint>("StartPosition");
                    }

                    return _startPosition.Value;
                }
            }

            public static Type WinRTType
            {
                get
                {
                    return s_WinRTType;
                }
            }

            private static Type s_WinRTType;
            private static string s_TypeName = "Windows.Data.Text.TextSegment, Windows, ContentType=WindowsRuntime";

            private object _textSegment = null;

            private uint? _length = null;
            private uint? _startPosition = null;
        }
}
}