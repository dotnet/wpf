// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using MS.Internal.WindowsBase;

namespace MS.Internal
{
    /// <summary>
    /// Represents a <see langword="ref struct"/> implementation of <see cref="TokenizerHelper"/> operating over <see cref="ReadOnlySpan{char}"/>.
    /// </summary>
    internal ref struct ValueTokenizerHelper
    {
        /// <summary>
        /// Constructor for <see cref="ValueTokenizerHelper"/> which accepts an <see cref="IFormatProvider"/>.
        /// If the <see cref="IFormatProvider"/> is null, we use the thread's <see cref="IFormatProvider"/> info.
        /// We will use ',' as the list separator, unless it's the same as the
        /// decimal separator.  If it *is*, then we can't determine if, say, "23,5" is one
        /// number or two.  In this case, we will use ";" as the separator.
        /// </summary>
        /// <param name="input"> The string which will be tokenized. </param>
        /// <param name="formatProvider"> The <see cref="IFormatProvider"/> which controls this tokenization. </param>
        internal ValueTokenizerHelper(ReadOnlySpan<char> input, IFormatProvider formatProvider) : this(input, '\'', GetNumericListSeparator(formatProvider)) { }

        /// <summary>
        /// Initialize the <see cref="ValueTokenizerHelper"/> with the string to tokenize,
        /// the char which represents quotes and the list separator.
        /// </summary>
        /// <param name="input"> The string to tokenize. </param>
        /// <param name="quoteChar"> The quote char. </param>
        /// <param name="separator"> The list separator. </param>
        internal ValueTokenizerHelper(ReadOnlySpan<char> input, char quoteChar, char separator)
        {
            _input = input;
            _currentTokenIndex = -1;
            _quoteChar = quoteChar;
            _argSeparator = separator;

            // immediately forward past any whitespace so
            // NextToken() logic always starts on the first
            // character of the next token.
            while (_charIndex < _input.Length)
            {
                if (!char.IsWhiteSpace(_input[_charIndex]))
                {
                    break;
                }

                ++_charIndex;
            }
        }

        /// <summary>
        /// Returns the next available token or <see cref="ReadOnlySpan{char}.Empty"/> if there's none ready.
        /// </summary>
        /// <returns>A slice of the next token or <see cref="ReadOnlySpan{char}.Empty"/>.</returns>
        internal readonly ReadOnlySpan<char> GetCurrentToken()
        {
            // If there's no current token, return an empty span
            if (_currentTokenIndex < 0)
            {
                return ReadOnlySpan<char>.Empty;
            }

            return _input.Slice(_currentTokenIndex, _currentTokenLength);
        }

        /// <summary>
        /// Throws an exception if there is any non-whitespace left un-parsed.
        /// </summary>
        internal readonly void LastTokenRequired()
        {
            if (_charIndex != _input.Length)
            {
                throw new InvalidOperationException(SR.Format(SR.TokenizerHelperExtraDataEncountered, _charIndex, _input.ToString()));
            }
        }

        /// <summary>
        /// Advances to the next token.
        /// </summary>
        /// <returns><see langword="true"/> if next token was found, <see langword="false"/> if at end of string.</returns>
        internal bool NextToken()
        {
            return NextToken(false);
        }

        /// <summary>
        /// Advances to the next token, throwing an exception if not present.
        /// </summary>
        /// <returns>A slice of the next next token.</returns>
        internal ReadOnlySpan<char> NextTokenRequired()
        {
            if (!NextToken(false))
            {
                throw new InvalidOperationException(SR.Format(SR.TokenizerHelperPrematureStringTermination, _input.ToString()));
            }

            return GetCurrentToken();
        }

        /// <summary>
        /// Advances to the next token, throwing an exception if not present.
        /// </summary>
        /// <returns>A slice of the next next token.</returns>
        internal ReadOnlySpan<char> NextTokenRequired(bool allowQuotedToken)
        {
            if (!NextToken(allowQuotedToken))
            {
                throw new InvalidOperationException(SR.Format(SR.TokenizerHelperPrematureStringTermination, _input.ToString()));
            }

            return GetCurrentToken();
        }

        /// <summary>
        /// Advances to the next token.
        /// </summary>
        /// <returns><see langword="true"/> if next token was found, <see langword="false"/> if at end of string.</returns>
        internal bool NextToken(bool allowQuotedToken)
        {
            // use the currently-set separator character.
            return NextToken(allowQuotedToken, _argSeparator);
        }

        /// <summary>
        /// Advances to the next token. A separator character can be specified which overrides the one previously set.
        /// </summary>
        /// <returns><see langword="true"/> if next token was found, <see langword="false"/> if at end of string.</returns>
        internal bool NextToken(bool allowQuotedToken, char separator)
        {
            _currentTokenIndex = -1; // reset the currentTokenIndex
            _foundSeparator = false; // reset

            // If we're at end of the string, just return false.
            if (_charIndex >= _input.Length)
            {
                return false;
            }

            char currentChar = _input[_charIndex];

            Debug.Assert(!char.IsWhiteSpace(currentChar), "Token started on Whitespace");

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
            while (_charIndex < _input.Length)
            {
                currentChar = _input[_charIndex];

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
                else if (char.IsWhiteSpace(currentChar) || (currentChar == separator))
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
                throw new InvalidOperationException(SR.Format(SR.TokenizerHelperMissingEndQuote, _input.ToString()));
            }

            ScanToNextToken(separator); // move so at the start of the nextToken for next call

            // finally made it, update the _currentToken values
            _currentTokenIndex = newTokenIndex;
            _currentTokenLength = newTokenLength;

            if (_currentTokenLength < 1)
            {
                throw new InvalidOperationException(SR.Format(SR.TokenizerHelperEmptyToken, _charIndex, _input.ToString()));
            }

            return true;
        }

        // helper to move the _charIndex to the next token or to the end of the string
        void ScanToNextToken(char separator)
        {
            // if already at end of the string don't bother
            if (_charIndex < _input.Length)
            {
                char currentChar = _input[_charIndex];

                // check that the currentChar is a space or the separator.  If not
                // we have an error. this can happen in the quote case
                // that the char after the quotes string isn't a char.
                if (!(char.IsWhiteSpace(currentChar) || (currentChar == separator)))
                {
                    throw new InvalidOperationException(SR.Format(SR.TokenizerHelperExtraDataEncountered, _charIndex, _input.ToString()));
                }

                // loop until hit a character that isn't
                // an argument separator or whitespace.
                int argSepCount = 0;
                while (_charIndex < _input.Length)
                {
                    currentChar = _input[_charIndex];

                    if (currentChar == separator)
                    {
                        _foundSeparator = true;
                        ++argSepCount;
                        _charIndex++;

                        if (argSepCount > 1)
                        {
                            throw new InvalidOperationException(SR.Format(SR.TokenizerHelperEmptyToken, _charIndex, _input.ToString()));
                        }
                    }
                    else if (char.IsWhiteSpace(currentChar))
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

                if (argSepCount > 0 && _charIndex >= _input.Length)
                {
                    throw new InvalidOperationException(SR.Format(SR.TokenizerHelperEmptyToken, _charIndex, _input.ToString()));
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

            Debug.Assert(numberFormat != null);

            // Is the decimal separator is the same as the list separator?
            // If so, we use the ";".
            if ((numberFormat.NumberDecimalSeparator.Length > 0) && (numericSeparator == numberFormat.NumberDecimalSeparator[0]))
            {
                numericSeparator = ';';
            }

            return numericSeparator;
        }

        internal readonly bool FoundSeparator
        {
            get
            {
                return _foundSeparator;
            }
        }

        private readonly char _quoteChar;
        private readonly char _argSeparator;
        private readonly ReadOnlySpan<char> _input;

        private int _charIndex;
        internal int _currentTokenIndex;
        internal int _currentTokenLength;
        private bool _foundSeparator;
    }
}
