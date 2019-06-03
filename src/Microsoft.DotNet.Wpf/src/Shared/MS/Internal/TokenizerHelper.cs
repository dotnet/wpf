// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: This file contains the implementation of TokenizerHelper.
//              This class should be used by most - if not all - MIL parsers.
//

using System;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;

#if !PBTCOMPILER
using System.Windows;
#endif

#if WINDOWS_BASE
    using MS.Internal.WindowsBase;
#elif PRESENTATION_CORE
    using MS.Internal.PresentationCore;
#elif PRESENTATIONFRAMEWORK
    using MS.Internal.PresentationFramework;
#elif PBTCOMPILER 
    using MS.Utility ;     
    using MS.Internal.Markup;
#elif DRT
    using MS.Internal.Drt;
#else
#error Attempt to use FriendAccessAllowedAttribute from an unknown assembly.
using MS.Internal.YourAssemblyName;
#endif

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace MS.Internal
#endif
{
#if !PBTCOMPILER 
    [FriendAccessAllowed]
#endif
    internal class TokenizerHelper
    {
        /// <summary>
        /// Constructor for TokenizerHelper which accepts an IFormatProvider.
        /// If the IFormatProvider is null, we use the thread's IFormatProvider info.
        /// We will use ',' as the list separator, unless it's the same as the
        /// decimal separator.  If it *is*, then we can't determine if, say, "23,5" is one
        /// number or two.  In this case, we will use ";" as the separator.
        /// </summary>
        /// <param name="str"> The string which will be tokenized. </param>
        /// <param name="formatProvider"> The IFormatProvider which controls this tokenization. </param>
        internal TokenizerHelper(string str, IFormatProvider formatProvider)
        {
            char numberSeparator = GetNumericListSeparator(formatProvider);

            Initialize(str, '\'', numberSeparator);
        }

        /// <summary>
        /// Initialize the TokenizerHelper with the string to tokenize,
        /// the char which represents quotes and the list separator.
        /// </summary>
        /// <param name="str"> The string to tokenize. </param>
        /// <param name="quoteChar"> The quote char. </param>
        /// <param name="separator"> The list separator. </param>
        internal TokenizerHelper(string str,
                                 char quoteChar,
                                 char separator)
        {
            Initialize(str, quoteChar, separator);
        }

        /// <summary>
        /// Initialize the TokenizerHelper with the string to tokenize,
        /// the char which represents quotes and the list separator.
        /// </summary>
        /// <param name="str"> The string to tokenize. </param>
        /// <param name="quoteChar"> The quote char. </param>
        /// <param name="separator"> The list separator. </param>
        private void Initialize(string str,
                                char quoteChar,
                                char separator)
        {
            _str = str;
            _strLen = str == null ? 0 : str.Length;
            _currentTokenIndex = -1;
            _quoteChar = quoteChar;
            _argSeparator = separator;

            // immediately forward past any whitespace so
            // NextToken() logic always starts on the first
            // character of the next token.
            while (_charIndex < _strLen)
            {
                if (!Char.IsWhiteSpace(_str, _charIndex))
                {
                    break;
                }

                ++_charIndex;
            }
        }

        internal string GetCurrentToken()
        {
            // if no current token, return null
            if (_currentTokenIndex < 0)
            {
                return null;
            }

            return _str.Substring(_currentTokenIndex,_currentTokenLength);
        }
        
        /// <summary>
        /// Throws an exception if there is any non-whitespace left un-parsed.
        /// </summary>
        internal void LastTokenRequired()
        {
            if (_charIndex != _strLen)
            {
                throw new System.InvalidOperationException(SR.Get(SRID.TokenizerHelperExtraDataEncountered, _charIndex, _str));
            }
        }

        /// <summary>
        /// Advances to the NextToken
        /// </summary>
        /// <returns>true if next token was found, false if at end of string</returns>
        internal bool NextToken()
        {
            return NextToken(false);
        }

        /// <summary>
        /// Advances to the NextToken, throwing an exception if not present
        /// </summary>
        /// <returns>The next token found</returns>
        internal string NextTokenRequired()
        {
            if (!NextToken(false))
            {
                throw new System.InvalidOperationException(SR.Get(SRID.TokenizerHelperPrematureStringTermination, _str));
            }

            return GetCurrentToken();
        }

        /// <summary>
        /// Advances to the NextToken, throwing an exception if not present
        /// </summary>
        /// <returns>The next token found</returns>
        internal string NextTokenRequired(bool allowQuotedToken)
        {
            if (!NextToken(allowQuotedToken))
            {
                throw new System.InvalidOperationException(SR.Get(SRID.TokenizerHelperPrematureStringTermination, _str));
            }

            return GetCurrentToken();
        }

        /// <summary>
        /// Advances to the NextToken
        /// </summary>
        /// <returns>true if next token was found, false if at end of string</returns>
        internal bool NextToken(bool allowQuotedToken)
        {
            // use the currently-set separator character.
            return NextToken(allowQuotedToken, _argSeparator);
        }

        /// <summary>
        /// Advances to the NextToken.  A separator character can be specified
        /// which overrides the one previously set.
        /// </summary>
        /// <returns>true if next token was found, false if at end of string</returns>
        internal bool NextToken(bool allowQuotedToken, char separator)
        {
            _currentTokenIndex = -1; // reset the currentTokenIndex
            _foundSeparator = false; // reset

            // If we're at end of the string, just return false.
            if (_charIndex >= _strLen)
            {
                return false;
            }

            char currentChar = _str[_charIndex];

            Debug.Assert(!Char.IsWhiteSpace(currentChar),"Token started on Whitespace");

            // setup the quoteCount
            int quoteCount = 0;

            // If we are allowing a quoted token and this token begins with a quote,
            // set up the quote count and skip the initial quote
            if (allowQuotedToken &&
                currentChar == _quoteChar)
            {
                quoteCount++; // increment quote count
                ++_charIndex; // move to next character
            }

            int newTokenIndex = _charIndex;
            int newTokenLength = 0;

            // loop until hit end of string or hit a , or whitespace
            // if at end of string ust return false.
            while (_charIndex < _strLen)
            {
                currentChar = _str[_charIndex];

                // if have a QuoteCount and this is a quote
                // decrement the quoteCount
                if (quoteCount > 0)
                {
                    // if anything but a quoteChar we move on
                    if (currentChar == _quoteChar)
                    {
                        --quoteCount;

                        // if at zero which it always should for now
                        // break out of the loop
                        if (0 == quoteCount)
                        {
                            ++_charIndex; // move past the quote
                            break;
                        }
                    }
                }
                else if ((Char.IsWhiteSpace(currentChar)) || (currentChar == separator))
                {
                    if (currentChar == separator)
                    {
                        _foundSeparator = true;
                    }
                    break;
                }

                ++_charIndex;
                ++newTokenLength;
            }

            // if quoteCount isn't zero we hit the end of the string
            // before the ending quote
            if (quoteCount > 0)
            {
                throw new System.InvalidOperationException(SR.Get(SRID.TokenizerHelperMissingEndQuote, _str));                
            }

            ScanToNextToken(separator); // move so at the start of the nextToken for next call

            // finally made it, update the _currentToken values
            _currentTokenIndex = newTokenIndex;
            _currentTokenLength = newTokenLength;

            if (_currentTokenLength < 1)
            {
                throw new System.InvalidOperationException(SR.Get(SRID.TokenizerHelperEmptyToken, _charIndex, _str));
            }

            return true;
        }

        // helper to move the _charIndex to the next token or to the end of the string
        void ScanToNextToken(char separator)
        {
            // if already at end of the string don't bother
            if (_charIndex < _strLen)
            {
                char currentChar = _str[_charIndex];

                // check that the currentChar is a space or the separator.  If not
                // we have an error. this can happen in the quote case
                // that the char after the quotes string isn't a char.
                if (!(currentChar == separator) &&
                    !Char.IsWhiteSpace(currentChar))
                {
                    throw new System.InvalidOperationException(SR.Get(SRID.TokenizerHelperExtraDataEncountered, _charIndex, _str));                    
                }

                // loop until hit a character that isn't
                // an argument separator or whitespace.
                int argSepCount = 0;
                while (_charIndex < _strLen)
                {
                    currentChar = _str[_charIndex];

                    if (currentChar == separator)
                    {
                        _foundSeparator = true;
                        ++argSepCount;
                        _charIndex++;

                        if (argSepCount > 1)
                        {
                            throw new System.InvalidOperationException(SR.Get(SRID.TokenizerHelperEmptyToken, _charIndex, _str));
                        }
                    }
                    else if (Char.IsWhiteSpace(currentChar))
                    {
                        ++_charIndex;
                    }
                    else
                    {
                        break;
                    }
                }

                // if there was a separatorChar then we shouldn't be
                // at the end of string or means there was a separator
                // but there isn't an arg

                if (argSepCount > 0 && _charIndex >= _strLen)
                {
                    throw new System.InvalidOperationException(SR.Get(SRID.TokenizerHelperEmptyToken, _charIndex, _str));
                }
            }
        }

        // Helper to get the numeric list separator for a given IFormatProvider.
        // Separator is a comma [,] if the decimal separator is not a comma, or a semicolon [;] otherwise.
        static internal char GetNumericListSeparator(IFormatProvider provider)
        {
            char numericSeparator = ',';

            // Get the NumberFormatInfo out of the provider, if possible
            // If the IFormatProvider doesn't not contain a NumberFormatInfo, then
            // this method returns the current culture's NumberFormatInfo.
            NumberFormatInfo numberFormat = NumberFormatInfo.GetInstance(provider);

            Debug.Assert(null != numberFormat);

            // Is the decimal separator is the same as the list separator?
            // If so, we use the ";".
            if ((numberFormat.NumberDecimalSeparator.Length > 0) && (numericSeparator == numberFormat.NumberDecimalSeparator[0]))
            {
                numericSeparator = ';';
            }

            return numericSeparator;
        }

        internal bool FoundSeparator
        {
            get
            {
                return _foundSeparator;
            }
        }

        char _quoteChar;
        char _argSeparator;
        string _str;
        int _strLen;
        int _charIndex;
        internal int _currentTokenIndex;
        internal int _currentTokenLength;
        bool _foundSeparator;
    }
}
