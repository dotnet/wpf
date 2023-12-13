// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Rtf lexer.
//

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.IO; // Stream   
using MS.Internal.Text;

namespace System.Windows.Documents
{
    /// <summary>
    /// RtfToXamlLexer.
    /// </summary>
    internal class RtfToXamlLexer
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// RtfToXamlLexer 
        /// </summary>
        internal RtfToXamlLexer(byte[] rtfBytes)
        {
            _rtfBytes = rtfBytes;

            _currentCodePage = CultureInfo.CurrentCulture.TextInfo.ANSICodePage;
            _currentEncoding = InternalEncoding.GetEncoding(_currentCodePage);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="formatState"></param>
        /// <returns></returns>
        internal RtfToXamlError Next(RtfToken token, FormatState formatState)
        {
            RtfToXamlError rtfToXamlError = RtfToXamlError.None;

            _rtfLastIndex = _rtfIndex;

            token.Empty();

            if (_rtfIndex >= _rtfBytes.Length)
            {
                token.Type = RtfTokenType.TokenEOF;
                return rtfToXamlError;
            }

            int rtfStartIndex = _rtfIndex;
            byte tokenChar = _rtfBytes[_rtfIndex++];

            switch (tokenChar)
            {
                // GroupStart
                case (byte)'{':
                    token.Type = RtfTokenType.TokenGroupStart;
                    break;

                // GroupEnd
                case (byte)'}':
                    token.Type = RtfTokenType.TokenGroupEnd;
                    break;

                // Control Word
                case (byte)'\r':
                case (byte)'\n':
                    token.Type = RtfTokenType.TokenNewline;
                    break;

                case (byte)0:
                    token.Type = RtfTokenType.TokenNullChar;
                    break;

                case (byte)'\\':
                    // Input ends with control sequence
                    if (_rtfIndex >= _rtfBytes.Length)
                    {
                        token.Type = RtfTokenType.TokenInvalid;
                    }
                    // Normal control character
                    else
                    {
                        if (IsControlCharValid(CurByte))
                        {
                            int controlStartIndex = _rtfIndex;

                            // Set _rtfIndex to get actual control
                            SetRtfIndex(token, controlStartIndex);

                            // Also provide actual control text - useful for unknown controls
                            token.Text = CurrentEncoding.GetString(_rtfBytes, controlStartIndex - 1, _rtfIndex - rtfStartIndex);
                        }
                        // Hex character
                        else if (CurByte == (byte)'\'')
                        {
                            _rtfIndex--;
                            return NextText(token);
                        }
                        // Explicit destination
                        else if (CurByte == '*')
                        {
                            _rtfIndex++;
                            token.Type = RtfTokenType.TokenDestination;
                        }
                        // Quoted control character (be generous) - should be limited to "'-*;\_{|}~"
                        else
                        {
                            token.Type = RtfTokenType.TokenTextSymbol;
                            token.Text = CurrentEncoding.GetString(_rtfBytes, _rtfIndex, 1);

                            _rtfIndex++;
                        }
                    }

                    break;

                // Text or Picture data
                default:
                    _rtfIndex--;

                    if (formatState != null && formatState.RtfDestination == RtfDestination.DestPicture)
                    {
                        token.Type = RtfTokenType.TokenPictureData;
                        break;
                    }
                    else
                    {
                        return NextText(token);
                    }
            }

            return rtfToXamlError;
        }

        internal RtfToXamlError AdvanceForUnicode(long nSkip)
        {
            RtfToXamlError rtfToXamlError = RtfToXamlError.None;

            // Advancing for text is a little tricky
            RtfToken token = new RtfToken();
            while (nSkip > 0 && rtfToXamlError == RtfToXamlError.None)
            {
                rtfToXamlError = Next(token, /*formatState:*/null);

                if (rtfToXamlError != RtfToXamlError.None)
                    break;

                switch (token.Type)
                {
                    default:
                    case RtfTokenType.TokenGroupStart:
                    case RtfTokenType.TokenGroupEnd:
                    case RtfTokenType.TokenInvalid:
                    case RtfTokenType.TokenEOF:
                    case RtfTokenType.TokenDestination:
                        Backup();
                        nSkip = 0;
                        break;

                    case RtfTokenType.TokenControl:
                        if (token.RtfControlWordInfo != null && token.RtfControlWordInfo.Control == RtfControlWord.Ctrl_BIN)
                        {
                            AdvanceForBinary((int)token.Parameter);
                        }
                        nSkip--;
                        break;

                    case RtfTokenType.TokenNewline:
                        // Newlines don't count for skipping purposes
                        break;

                    case RtfTokenType.TokenNullChar:
                        // Null chars don't count for skipping purposes
                        break;

                    case RtfTokenType.TokenText:
                        // We need to skip *bytes*, considering hex-encoded control words as a single byte.
                        // Since Next() returned TokenText, we know that we can safely assume that the next
                        // sequence of bytes is either simple text or hex-encoded bytes.
                        int nEndTextIndex = _rtfIndex;
                        Backup();
                        while (nSkip > 0 && _rtfIndex < nEndTextIndex)
                        {
                            if (CurByte == '\\')
                            {
                                _rtfIndex += 4;
                            }
                            else
                            {
                                _rtfIndex++;
                            }
                            nSkip--;
                        }
                        break;
                }
            }

            return rtfToXamlError;
        }

        internal void AdvanceForBinary(int skip)
        {
            if (_rtfIndex + skip < _rtfBytes.Length)
            {
                _rtfIndex += skip;
            }
            else
            {
                _rtfIndex = _rtfBytes.Length - 1;
            }
        }

        // Advance for the image data
        internal void AdvanceForImageData()
        {
            byte tokenChar = _rtfBytes[_rtfIndex];

            // Find the end position of image data
            while (tokenChar != (byte)'}')
            {
                tokenChar = _rtfBytes[_rtfIndex++];
            }

            // Move back to the group end char('}') to handle the group end token
            _rtfIndex--;
        }

        // Write the rtf image binary data to the image stream which is the image part of 
        // the container on WpfPayLoad
        internal void WriteImageData(Stream imageStream, bool isBinary)
        {
            byte tokenChar = _rtfBytes[_rtfIndex];
            byte tokenNextChar;

            // Write the rtf image data(binary or hex) to the image stream of WpfPayLoad
            while (tokenChar != (byte)'{' && tokenChar != (byte)'}' && tokenChar != (byte)'\\')
            {
                if (isBinary)
                {
                    // Write the image binary data directly
                    imageStream.WriteByte(tokenChar);
                }
                else
                {
                    tokenNextChar = _rtfBytes[_rtfIndex + 1];

                    // Write the image data after convert rtf image hex data to binary data
                    if (IsHex(tokenChar) && IsHex(tokenNextChar))
                    {
                        byte firstHex = HexToByte(tokenChar);
                        byte secondHex = HexToByte(tokenNextChar);

                        imageStream.WriteByte((byte)(firstHex << 4 | secondHex));

                        _rtfIndex++;
                    }
                }

                _rtfIndex++;
                tokenChar = _rtfBytes[_rtfIndex];
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal int CodePage
        {
            set
            {
                if (_currentCodePage != value)
                {
                    _currentCodePage = value;
                    _currentEncoding = InternalEncoding.GetEncoding(_currentCodePage);
                }
            }
        }

        internal Encoding CurrentEncoding
        {
            get
            {
                return _currentEncoding;
            }
        }

        internal byte CurByte
        {
            get
            {
                return _rtfBytes[_rtfIndex];
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Called to process sequence of text and \'hh encoded bytes.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private RtfToXamlError NextText(RtfToken token)
        {
            RtfToXamlError rtfToXamlError = RtfToXamlError.None;

            _rtfLastIndex = _rtfIndex;

            token.Empty();
            token.Type = RtfTokenType.TokenText;

            int s = _rtfIndex;
            int e = s;
            bool bSawHex = false;

            while (e < _rtfBytes.Length)
            {
                if (IsControl(_rtfBytes[e]))
                {
                    if (_rtfBytes[e] == (byte)'\\'
                        && e + 3 < _rtfBytes.Length
                        && _rtfBytes[e + 1] == '\''
                        && IsHex(_rtfBytes[e + 2])
                        && IsHex(_rtfBytes[e + 3]))
                    {
                        e += 4;
                        bSawHex = true;
                    }
                    else
                    {
                        break;
                    }
                }
                else if (_rtfBytes[e] == '\r' || _rtfBytes[e] == '\n' || _rtfBytes[e] == 0)
                {
                    break;
                }
                else
                {
                    e++;
                }
            }

            if (s == e)
            {
                token.Type = RtfTokenType.TokenInvalid;
            }
            else
            {
                _rtfIndex = e;

                if (bSawHex)
                {
                    int i = 0;
                    int n = e - s;
                    byte[] bytes = new byte[n];
                    while (s < e)
                    {
                        if (_rtfBytes[s] == '\\')
                        {
                            bytes[i++] = (byte)((byte)(HexToByte(_rtfBytes[s + 2]) << 4) + HexToByte(_rtfBytes[s + 3]));
                            s += 4;
                        }
                        else
                        {
                            bytes[i++] = _rtfBytes[s++];
                        }
                    }

                    token.Text = CurrentEncoding.GetString(bytes, 0, i);
                }
                else
                {
                    token.Text = CurrentEncoding.GetString(_rtfBytes, s, e - s);
                }
            }

            return rtfToXamlError;
        }

        private RtfToXamlError Backup()
        {
            if (_rtfLastIndex == 0)
            {
                // This is a programming error.
                Debug.Assert(false);
                return RtfToXamlError.InvalidFormat;
            }

            _rtfIndex = _rtfLastIndex;
            _rtfLastIndex = 0;

            return RtfToXamlError.None;
        }

        private void SetRtfIndex(RtfToken token, int controlStartIndex)
        {
            while (_rtfIndex < _rtfBytes.Length && IsControlCharValid(CurByte))
            {
                _rtfIndex++;
            }

            int controlLength = _rtfIndex - controlStartIndex;
            string controlName = CurrentEncoding.GetString(_rtfBytes, controlStartIndex, controlLength);

            // If control sequence > MAX_CONTROL_LENGTH characters, invalid input.
            if (controlLength > MAX_CONTROL_LENGTH)
            {
                token.Type = RtfTokenType.TokenInvalid;
            }
            else
            {
                token.Type = RtfTokenType.TokenControl;
                token.RtfControlWordInfo = RtfControlWordLookup(controlName);

                if (_rtfIndex < _rtfBytes.Length)
                {
                    if (CurByte == ' ')
                    {
                        _rtfIndex++;
                    }
                    else if (IsParameterStart(CurByte))
                    {
                        bool isNegative = false;

                        if (CurByte == '-')
                        {
                            isNegative = true;
                            _rtfIndex++;
                        }

                        long parameter = 0;

                        int paramStartIndex = _rtfIndex;

                        while (_rtfIndex < _rtfBytes.Length && IsParameterFollow(CurByte))
                        {
                            parameter = (parameter * 10) + (CurByte - '0');
                            _rtfIndex++;
                        }

                        int paramLength = _rtfIndex - paramStartIndex;

                        // Following space is not part of text input
                        if (_rtfIndex < _rtfBytes.Length && CurByte == ' ')
                        {
                            _rtfIndex++;
                        }

                        if (isNegative)
                        {
                            parameter = -parameter;
                        }

                        // If parameter is too long, invalid input.
                        if (paramLength > MAX_PARAM_LENGTH)
                        {
                            token.Type = RtfTokenType.TokenInvalid;
                        }
                        else
                        {
                            token.Parameter = parameter;
                        }
                    }
                }
            }
        }

        private bool IsControl(byte controlChar)
        {
            return ((controlChar) == (byte)'\\' || (controlChar) == (byte)'{' || (controlChar) == (byte)'}');
        }

        private bool IsControlCharValid(byte controlChar)
        {
            return (((controlChar) >= (byte)'a' && (controlChar) <= (byte)'z') || ((controlChar) >= (byte)'A' && (controlChar) <= (byte)'Z'));
        }

        private bool IsParameterStart(byte controlChar)
        {
            return ((controlChar) == (byte)'-' || ((controlChar) >= (byte)'0' && (controlChar) <= (byte)'9'));
        }

        private bool IsParameterFollow(byte controlChar)
        {
            return (((controlChar) >= (byte)'0' && (controlChar) <= (byte)'9'));
        }

        private bool IsHex(byte controlChar)
        {
            return ((controlChar >= (byte)'0' && controlChar <= (byte)'9') ||
                    (controlChar >= (byte)'a' && controlChar <= (byte)'f') ||
                    (controlChar >= (byte)'A' && controlChar <= (byte)'F'));
        }

        private byte HexToByte(byte hexByte)
        {
            if (hexByte >= (byte)'0' && hexByte <= (byte)'9')
            {
                return (byte)(hexByte - ((byte)'0'));
            }
            else if (hexByte >= (byte)'a' && hexByte <= (byte)'f')
            {
                return (byte)(10 + hexByte - ((byte)'a'));
            }
            else if (hexByte >= (byte)'A' && hexByte <= (byte)'F')
            {
                return (byte)(10 + hexByte - ((byte)'A'));
            }
            else
            {
                return 0;
            }
        }

        private static RtfControlWordInfo RtfControlWordLookup(string controlName)
        {
            // Initialize hashtable
            lock (_rtfControlTableMutex)
            {
                if (_rtfControlTable == null)
                {
                    RtfControlWordInfo[] controlWordInfoTable = RtfControls.ControlTable;
                    _rtfControlTable = new Hashtable(controlWordInfoTable.Length);

                    for (int i = 0; i < controlWordInfoTable.Length; i++)
                    {
                        _rtfControlTable.Add(controlWordInfoTable[i].ControlName, controlWordInfoTable[i]);
                    }
                }
            }

            RtfControlWordInfo cwi = (RtfControlWordInfo)_rtfControlTable[controlName];
            if (cwi == null)
            {
                // OK, then canonicalize it
                controlName = controlName.ToLower(CultureInfo.InvariantCulture);
                cwi = (RtfControlWordInfo)_rtfControlTable[controlName];
            }
            return cwi;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private byte[] _rtfBytes;

        private int _rtfIndex;
        private int _rtfLastIndex;

        private int _currentCodePage;
        private Encoding _currentEncoding;

        private static object _rtfControlTableMutex = new object();
        private static Hashtable _rtfControlTable = null;

        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Private Const
        //
        //------------------------------------------------------

        #region Private Const

        private const int MAX_CONTROL_LENGTH = 32;
        private const int MAX_PARAM_LENGTH = 10;    // 10 decimal digits in 32 bits

        #endregion Private Const
    } // RtfToXamlLexer
}
