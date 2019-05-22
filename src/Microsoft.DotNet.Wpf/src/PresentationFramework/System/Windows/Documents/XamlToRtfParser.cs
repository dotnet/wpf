// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Xaml parser to convert the xaml content into Rtf content.
//

using System.Collections;

namespace System.Windows.Documents
{
    /// <summary>
    /// Xaml parser to convert the xaml content into Rtf content.
    /// </summary>
    internal class XamlToRtfParser
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// XamlToRtfParser constructor
        /// </summary>
        internal XamlToRtfParser(string xaml)
        {
            _xaml = xaml;

            _xamlLexer = new XamlLexer(_xaml);
            _xamlTagStack = new XamlTagStack();
            _xamlAttributes = new XamlAttributes(_xaml);
        }

        #endregion Constructors

        // ---------------------------------------------------------------------
        //
        // internal Methods
        //
        // ---------------------------------------------------------------------

        #region internal Methods

        internal XamlToRtfError Parse()
        {
            // Need callbacks
            if (_xamlContent == null || _xamlError == null)
            {
                return XamlToRtfError.Unknown;
            }

            // We are simply looking for well-formedness: that is, that the XML is lexically valid and tags are balanced.
            XamlToRtfError xamlToRtfError = XamlToRtfError.None;

            XamlToken xamlToken = new XamlToken(); 
            string name = string.Empty;

            // Fire things off
            xamlToRtfError = _xamlContent.StartDocument();

            while (xamlToRtfError == XamlToRtfError.None)
            {
                xamlToRtfError = _xamlLexer.Next(xamlToken);

                if (xamlToRtfError != XamlToRtfError.None || xamlToken.TokenType == XamlTokenType.XTokEOF)
                {
                    break;
                }

                switch (xamlToken.TokenType)
                {
                    case XamlTokenType.XTokInvalid:
                        xamlToRtfError = XamlToRtfError.Unknown;
                        break;

                    case XamlTokenType.XTokCharacters:
                        xamlToRtfError = _xamlContent.Characters(xamlToken.Text);
                        break;

                    case XamlTokenType.XTokEntity:
                        xamlToRtfError = _xamlContent.SkippedEntity(xamlToken.Text);
                        break;

                    case XamlTokenType.XTokStartElement:
                        xamlToRtfError = ParseXTokStartElement(xamlToken, ref name);
                        break;

                    case XamlTokenType.XTokEndElement:
                        xamlToRtfError = ParseXTokEndElement(xamlToken, ref name);
                        break;

                    case XamlTokenType.XTokCData:
                        // Ignore
                        break;

                    case XamlTokenType.XTokPI:
                        // Ignore
                        break;

                    case XamlTokenType.XTokComment:
                        // Ignore
                        break;

                    case XamlTokenType.XTokWS:
                        xamlToRtfError = _xamlContent.IgnorableWhitespace(xamlToken.Text);
                        break;

                    default:
                        xamlToRtfError = XamlToRtfError.Unknown;
                        break;
                }
            }

            // All tags need to have been popped.
            if (xamlToRtfError == XamlToRtfError.None && _xamlTagStack.Count != 0)
            {
                xamlToRtfError = XamlToRtfError.Unknown;
            }

            // Wrap things up
            if (xamlToRtfError == XamlToRtfError.None)
            {
                xamlToRtfError = _xamlContent.EndDocument();
            }

            return xamlToRtfError;
        }

        internal void SetCallbacks(IXamlContentHandler xamlContent, IXamlErrorHandler xamlError)
        {
            _xamlContent = xamlContent;
            _xamlError = xamlError;
        }

        #endregion internal Methods

        // ---------------------------------------------------------------------
        //
        // internal Properties
        //
        // ---------------------------------------------------------------------

        #region internal Properties

        #endregion internal Properties

        // ---------------------------------------------------------------------
        //
        // Private Methods
        //
        // ---------------------------------------------------------------------

        #region Private Methods

        // Helper for Parse method.
        private XamlToRtfError ParseXTokStartElement(XamlToken xamlToken, ref string name)
        {
            XamlToRtfError xamlToRtfError = _xamlAttributes.Init(xamlToken.Text);

            if (xamlToRtfError == XamlToRtfError.None)
            {
                xamlToRtfError = _xamlAttributes.GetTag(ref name);

                if (xamlToRtfError == XamlToRtfError.None)
                {
                    xamlToRtfError = _xamlContent.StartElement(string.Empty, name, name, _xamlAttributes);

                    if (xamlToRtfError == XamlToRtfError.None)
                    {
                        if (_xamlAttributes.IsEmpty)
                        {
                            xamlToRtfError = _xamlContent.EndElement(string.Empty, name, name);
                        }
                        else
                        {
                            xamlToRtfError = (XamlToRtfError)_xamlTagStack.Push(name);
                        }
                    }
                }
            }

            return xamlToRtfError;
        }

        // Helper for Parse method.
        private XamlToRtfError ParseXTokEndElement(XamlToken xamlToken, ref string name)
        {
            XamlToRtfError xamlToRtfError = _xamlAttributes.Init(xamlToken.Text);

            if (xamlToRtfError == XamlToRtfError.None)
            {
                xamlToRtfError = _xamlAttributes.GetTag(ref name);

                if (xamlToRtfError == XamlToRtfError.None)
                {
                    if (_xamlTagStack.IsMatchTop(name))
                    {
                        _xamlTagStack.Pop();

                        xamlToRtfError = _xamlContent.EndElement(string.Empty, name, name);
                    }
                }
            }

            return xamlToRtfError;
        }

        #endregion Private Methods

        // ---------------------------------------------------------------------
        //
        // Private Fields
        //
        // ---------------------------------------------------------------------

        #region Private Fields

        private string _xaml;

        private XamlLexer _xamlLexer;
        private XamlTagStack _xamlTagStack;
        private XamlAttributes _xamlAttributes;

        private IXamlContentHandler _xamlContent;
        private IXamlErrorHandler _xamlError;

        #endregion Private Fields

        // ---------------------------------------------------------------------
        //
        // Internal Class
        //
        // ---------------------------------------------------------------------
        #region Internal Class

        internal class XamlLexer
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            /// <summary>
            /// 
            /// </summary>
            internal XamlLexer(string xaml)
            {
                _xaml = xaml;
            }

            #endregion Constructors

            // ---------------------------------------------------------------------
            //
            // internal Methods
            //
            // ---------------------------------------------------------------------

            #region internal Methods

            internal XamlToRtfError Next(XamlToken token)
            {
                XamlToRtfError xamlToRtfError = XamlToRtfError.None;

                int startIndex = _xamlIndex;

                if (_xamlIndex < _xaml.Length)
                {
                    char tokenChar = _xaml[_xamlIndex];

                    switch (tokenChar)
                    {
                        case ' ':
                        case '\t':
                        case '\r':
                        case '\n':
                            token.TokenType = XamlTokenType.XTokWS;

                            for (_xamlIndex++; IsCharsAvailable(1) && IsSpace(_xaml[_xamlIndex]); _xamlIndex++)
                            {
                                continue;
                            }
                            break;

                        case '<':
                            NextLessThanToken(token);
                            break;

                        case '&':
                            // Entity
                            token.TokenType = XamlTokenType.XTokInvalid;

                            for (_xamlIndex++; IsCharsAvailable(1); _xamlIndex++)
                            {
                                if (_xaml[_xamlIndex] == ';')
                                {
                                    _xamlIndex++;
                                    token.TokenType = XamlTokenType.XTokEntity;
                                    break;
                                }
                            }
                            break;

                        default:
                            // Plain text
                            token.TokenType = XamlTokenType.XTokCharacters;

                            for (_xamlIndex++; IsCharsAvailable(1); _xamlIndex++)
                            {
                                if (_xaml[_xamlIndex] == '&' || _xaml[_xamlIndex] == '<')
                                {
                                    break;
                                }
                            }
                            break;
                    }
                }

                token.Text = _xaml.Substring(startIndex, _xamlIndex - startIndex);

                if (token.Text.Length == 0)
                {
                    token.TokenType = XamlTokenType.XTokEOF;
                }

                return xamlToRtfError;
            }

            #endregion internal Methods

            #region Private Methods

            // ---------------------------------------------------------------------
            //
            // Private Methods
            //
            // ---------------------------------------------------------------------

            private bool IsSpace(char character)
            {
                return (character == ' ' || character == '\t' || character == '\n' || character == '\r');
            }

            private bool IsCharsAvailable(int index)
            {
                return ((_xamlIndex + index) <= _xaml.Length);
            }

            // Helper for the Next method, handles '<' token.
            private void NextLessThanToken(XamlToken token)
            {
                _xamlIndex++;

                // Careful...
                if (!IsCharsAvailable(1))
                {
                    token.TokenType = XamlTokenType.XTokInvalid;
                    return;
                }

                // Default is we're going to see an invalid sequence
                token.TokenType = XamlTokenType.XTokInvalid;

                char currentChar = _xaml[_xamlIndex];

                switch (currentChar)
                {
                    case '?':
                        // Processing Instruction
                        for (_xamlIndex++; IsCharsAvailable(2); _xamlIndex++)
                        {
                            if (_xaml[_xamlIndex] == '?' && _xaml[_xamlIndex + 1] == '>')
                            {
                                _xamlIndex += 2;
                                token.TokenType = XamlTokenType.XTokPI;
                                break;
                            }
                        }
                        break;

                    case '!':
                        // Need to Check if <!--> is really valid comment - browsers accept it
                        _xamlIndex++;

                        for (; IsCharsAvailable(3); _xamlIndex++)
                        {
                            if (_xaml[_xamlIndex] == '-' && _xaml[_xamlIndex + 1] == '-' && _xaml[_xamlIndex + 2] == '>')
                            {
                                _xamlIndex += 3;
                                token.TokenType = XamlTokenType.XTokComment;
                                break;
                            }
                        }
                        break;

                    case '>':
                        // Anomaly
                        _xamlIndex++;
                        token.TokenType = XamlTokenType.XTokInvalid;
                        break;

                    case '/':
                        // End Element
                        for (_xamlIndex++; IsCharsAvailable(1); _xamlIndex++)
                        {
                            if (_xaml[_xamlIndex] == '>')
                            {
                                _xamlIndex++;
                                token.TokenType = XamlTokenType.XTokEndElement;
                                break;
                            }
                        }
                        break;

                    default:
                        // Start Element
                        // Tricky element here is making sure we correctly parse quoted strings so that we don't
                        // incorrectly treat a '>' in a string as ending the token.
                        {
                            char quoteChar = (char)0x00;

                            for (; IsCharsAvailable(1); _xamlIndex++)
                            {
                                if (quoteChar != 0x00)
                                {
                                    if (_xaml[_xamlIndex] == quoteChar)
                                    {
                                        quoteChar = (char)0x00;
                                    }
                                }
                                else if (_xaml[_xamlIndex] == '"' || _xaml[_xamlIndex] == '\'')
                                {
                                    quoteChar = _xaml[_xamlIndex];
                                }
                                else if (_xaml[_xamlIndex] == '>')
                                {
                                    _xamlIndex++;
                                    token.TokenType = XamlTokenType.XTokStartElement;
                                    break;
                                }
                            }
                        }
                        break;
                }
            }

            #endregion Private Methods

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            private string _xaml;
            private int _xamlIndex;

            #endregion Private Fields
        }

        internal class XamlTagStack : ArrayList
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            /// <summary>
            /// 
            /// </summary>
            internal XamlTagStack() : base(10)
            {
            }

            #endregion Constructors

            // ---------------------------------------------------------------------
            //
            // internal Methods
            //
            // ---------------------------------------------------------------------

            #region internal Methods

            internal RtfToXamlError Push(string xamlTag)
            {
                Add(xamlTag);

                return RtfToXamlError.None;
            }

            internal void Pop()
            {
                if (Count > 0)
                {
                    RemoveAt(Count - 1);
                }
            }

            internal bool IsMatchTop(string xamlTag)
            {
                if (Count == 0)
                {
                    return false;
                }

                string top = (string)this[Count - 1];

                if (top.Length == 0)
                {
                    return false;
                }

                if (string.Compare(xamlTag, xamlTag.Length, top, top.Length, top.Length, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            #endregion internal Methods
        }

        internal class XamlAttributes : IXamlAttributes
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            /// <summary>
            /// 
            /// </summary>
            internal XamlAttributes(string xaml)
            {
                _xamlParsePoints = new XamlParsePoints();
            }

            #endregion Constructors

            //------------------------------------------------------
            //
            //  internal Methods
            //
            //------------------------------------------------------

            #region internal Methods

            internal XamlToRtfError Init(string xaml)
            {
                return _xamlParsePoints.Init(xaml);
            }

            internal XamlToRtfError GetTag(ref string xamlTag)
            {
                XamlToRtfError rtfToXamlError = XamlToRtfError.None;

                if (!_xamlParsePoints.IsValid)
                {
                    return XamlToRtfError.Unknown;
                }

                xamlTag = (string)_xamlParsePoints[0];

                return rtfToXamlError;
            }

            XamlToRtfError IXamlAttributes.GetLength(ref int length)
            {
                XamlToRtfError rtfToXamlError = XamlToRtfError.None;

                if (_xamlParsePoints.IsValid)
                {
                    length = (_xamlParsePoints.Count - 1) / 2;

                    return rtfToXamlError;
                }
                else
                {
                    return XamlToRtfError.Unknown;
                }
            }

            XamlToRtfError IXamlAttributes.GetUri(int index, ref string uri)
            {
                XamlToRtfError rtfToXamlError = XamlToRtfError.None;

                return rtfToXamlError;
            }

            XamlToRtfError IXamlAttributes.GetLocalName(int index, ref string localName)
            {
                XamlToRtfError rtfToXamlError = XamlToRtfError.None;

                return rtfToXamlError;
            }

            XamlToRtfError IXamlAttributes.GetQName(int index, ref string qName)
            {
                XamlToRtfError rtfToXamlError = XamlToRtfError.None;

                return rtfToXamlError;
            }

            XamlToRtfError IXamlAttributes.GetName(int index, ref string uri, ref string localName, ref string qName)
            {
                XamlToRtfError rtfToXamlError = XamlToRtfError.None;

                int nLength = (_xamlParsePoints.Count - 1) / 2;

                if (index < 0 || index > nLength - 1)
                {
                    return XamlToRtfError.Unknown;
                }

                localName = (string)_xamlParsePoints[index * 2 + 1];
                qName = (string)_xamlParsePoints[index * 2 + 2];

                return rtfToXamlError;
            }

            XamlToRtfError IXamlAttributes.GetIndexFromName(string uri, string localName, ref int index)
            {
                XamlToRtfError rtfToXamlError = XamlToRtfError.None;

                return rtfToXamlError;
            }

            XamlToRtfError IXamlAttributes.GetIndexFromQName(string qName, ref int index)
            {
                XamlToRtfError rtfToXamlError = XamlToRtfError.None;

                return rtfToXamlError;
            }

            XamlToRtfError IXamlAttributes.GetType(int index, ref string typeName)
            {
                XamlToRtfError rtfToXamlError = XamlToRtfError.None;

                return rtfToXamlError;
            }

            XamlToRtfError IXamlAttributes.GetTypeFromName(string uri, string localName, ref string typeName)
            {
                XamlToRtfError rtfToXamlError = XamlToRtfError.None;

                return rtfToXamlError;
            }

            XamlToRtfError IXamlAttributes.GetValue(int index, ref string valueName)
            {
                XamlToRtfError rtfToXamlError = XamlToRtfError.None;

                int nLength = (_xamlParsePoints.Count - 1) / 2;

                if (index < 0 || index > nLength - 1)
                {
                    return XamlToRtfError.OutOfRange;
                }

                valueName = (string)_xamlParsePoints[index * 2 + 2];

                return rtfToXamlError;
            }

            XamlToRtfError IXamlAttributes.GetValueFromName(string uri, string localName, ref string valueName)
            {
                XamlToRtfError rtfToXamlError = XamlToRtfError.None;

                return rtfToXamlError;
            }

            XamlToRtfError IXamlAttributes.GetValueFromQName(string qName, ref string valueName)
            {
                XamlToRtfError rtfToXamlError = XamlToRtfError.None;

                return rtfToXamlError;
            }

            XamlToRtfError IXamlAttributes.GetTypeFromQName(string qName, ref string typeName)
            {
                XamlToRtfError rtfToXamlError = XamlToRtfError.None;

                return rtfToXamlError;
            }

            #endregion internal Methods

            #region internal Properties

            //------------------------------------------------------
            //
            //  internal Properties
            //
            //------------------------------------------------------

            internal bool IsEmpty
            {
                get
                {
                    return _xamlParsePoints.IsEmpty;
                }
            }

            #endregion internal Properties

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            private XamlParsePoints _xamlParsePoints;

            #endregion Private Fields
        }

        internal class XamlParsePoints : ArrayList
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            /// <summary>
            /// 
            /// </summary>
            internal XamlParsePoints() : base(10)
            {
            }

            #endregion Constructors

            // ---------------------------------------------------------------------
            //
            // internal Methods
            //
            // ---------------------------------------------------------------------

            #region internal Methods

            internal XamlToRtfError Init(string xaml)
            {
                XamlToRtfError xamlToRtfError = XamlToRtfError.None;

                // Initialize
                _empty = false;
                _valid = false;

                Clear();

                char quoteChar;
                int xamlIndex = 0;

                // Need to have at least "<...>".  Note that verification at this point that the string ends with angle
                // bracket allows me below to safely loop looking for specific character sets that don't match angle bracket
                // without also explicitly having to test for looping past the end of the string.
                if (xaml.Length < 2 || xaml[0] != '<' || xaml[xaml.Length - 1] != '>')
                {
                    return XamlToRtfError.Unknown;
                }

                xamlIndex++;

                if (IsSpace(xaml[xamlIndex]))
                {
                    return XamlToRtfError.Unknown;
                }

                // An end tag?
                if (xaml[xamlIndex] == '/')
                {
                    return HandleEndTag(xaml, xamlIndex);
                }

                // Add the start and end of the tag pointers
                //AddParseData(xaml.Substring(xamlIndex));
                int startIndex = xamlIndex;

                // Note that check above that the string ends in angle simplifies loop check
                for (xamlIndex++; IsNameChar(xaml[xamlIndex]); xamlIndex++)
                {
                    continue;
                }

                AddParseData(xaml.Substring(startIndex, xamlIndex - startIndex));

                // Start parsing name/value pairs
                while (xamlIndex < xaml.Length)
                {
                    // Move past spaces
                    for (; IsSpace(xaml[xamlIndex]); xamlIndex++)
                    {
                        continue;
                    }

                    // Done?
                    if (xamlIndex == xaml.Length - 1)
                    {
                        break;
                    }

                    // Empty tag?
                    if (xaml[xamlIndex] == '/')
                    {
                        if (xamlIndex == xaml.Length - 2)
                        {
                            _empty = true;
                            break;
                        }
                        else
                        {
                            return XamlToRtfError.Unknown;
                        }
                    }

                    // OK, have another attribute
                    //AddParseData(xaml.Substring(xamlIndex));
                    startIndex = xamlIndex;

                    for (xamlIndex++; IsNameChar(xaml[xamlIndex]); xamlIndex++)
                    {
                        continue;
                    }

                    AddParseData(xaml.Substring(startIndex, xamlIndex - startIndex));

                    // Move past optional trailing spaces
                    if (xamlIndex < xaml.Length)
                    {
                        for (; IsSpace(xaml[xamlIndex]); xamlIndex++)
                        {
                            continue;
                        }
                    }

                    // Attribute with no '='?
                    if (xamlIndex == xaml.Length || xaml[xamlIndex] != '=')
                    {
                        return XamlToRtfError.Unknown;
                    }

                    // Move past '=' and optional trailing spaces
                    xamlIndex++;

                    for (; IsSpace(xaml[xamlIndex]); xamlIndex++)
                    {
                        continue;
                    }

                    // Value needs to be quoted
                    if (xaml[xamlIndex] != '\'' && xaml[xamlIndex] != '"')
                    {
                        return XamlToRtfError.Unknown;
                    }

                    quoteChar = xaml[xamlIndex++];

                    //AddParseData(xaml.Substring(xamlIndex));
                    startIndex = xamlIndex;

                    for (; xamlIndex < xaml.Length && xaml[xamlIndex] != quoteChar; xamlIndex++)
                    {
                        continue;
                    }

                    if (xamlIndex == xaml.Length)
                    {
                        return XamlToRtfError.Unknown;
                    }

                    AddParseData(xaml.Substring(startIndex, xamlIndex - startIndex));

                    xamlIndex++;
                }

                _valid = true;

                return xamlToRtfError;
            }

            internal void AddParseData(string parseData)
            {
                Add(parseData);
            }

            #endregion internal Methods

            // ---------------------------------------------------------------------
            //
            // internal Properties
            //
            // ---------------------------------------------------------------------

            #region internal Properties

            internal bool IsEmpty
            {
                get
                {
                    return _empty;
                }
            }

            internal bool IsValid
            {
                get
                {
                    return _valid;
                }
            }

            #endregion internal Properties

            #region Private Methods

            // ---------------------------------------------------------------------
            //
            // Private Methods
            //
            // ---------------------------------------------------------------------

            private bool IsSpace(char character)
            {
                return (character == ' ' || character == '\t' || character == '\n' || character == '\r');
            }

            private bool IsNameChar(char character)
            {
                return (!IsSpace(character) && character != '=' && character != '>' && character != '/');
            }

            // Helper for Init method.
            private XamlToRtfError HandleEndTag(string xaml, int xamlIndex)
            {
                xamlIndex++;

                // Move past spaces
                for (; IsSpace(xaml[xamlIndex]); xamlIndex++)
                {
                    continue;
                }

                // At name start
                int startIndex = xamlIndex;

                // Move past name
                for (xamlIndex++; IsNameChar(xaml[xamlIndex]); xamlIndex++)
                {
                    continue;
                }

                AddParseData(xaml.Substring(startIndex, xamlIndex - startIndex));

                // Move past spaces
                for (; IsSpace(xaml[xamlIndex]); xamlIndex++)
                {
                    continue;
                }

                // Now must be at end of token
                if (xamlIndex == xaml.Length - 1)
                {
                    _valid = true;

                    return XamlToRtfError.None;
                }

                return XamlToRtfError.Unknown;
            }

            #endregion Private Methods

            // ---------------------------------------------------------------------
            //
            // Private Fields
            //
            // ---------------------------------------------------------------------

            #region Private Fields

            private bool _empty;
            private bool _valid;

            #endregion Private Fields
        }

        internal class XamlToken
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            /// <summary>
            /// 
            /// </summary>
            internal XamlToken()
            {
            }

            #endregion Constructors

            // ---------------------------------------------------------------------
            //
            // internal Properties
            //
            // ---------------------------------------------------------------------

            #region internal Properties

            internal XamlTokenType TokenType
            {
                get
                {
                    return _tokenType;
                }
                set
                {
                    _tokenType = value;
                }
            }

            internal string Text
            {
                get
                {
                    return _text;
                }
                set
                {
                    _text = value;
                }
            }

            #endregion internal Properties

            // ---------------------------------------------------------------------
            //
            // Private Fields
            //
            // ---------------------------------------------------------------------

            #region Private Fields

            private XamlTokenType _tokenType;

            private string _text;

            #endregion Private Fields
        }

        #endregion Private Class
    }
}
