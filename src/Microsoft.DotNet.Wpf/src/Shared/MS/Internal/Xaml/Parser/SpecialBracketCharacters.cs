// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

//  Description: Data model for the Bracket characters specified on a Markup Extension property.

using System;
using System.Text;
using System.Buffers;
using System.Windows.Markup;
using System.ComponentModel;
using System.Collections.Generic;

namespace MS.Internal.Xaml.Parser
{
    /// <summary>
    /// Class that provides helper functions for the parser/Xaml Reader
    /// to process Bracket Characters specified on a Markup Extension Property
    /// </summary>
    internal sealed class SpecialBracketCharacters : ISupportInitialize
    {
#if !NETFX
        /// <summary>
        /// Stores characters that cannot be specified in <see cref="MarkupExtensionBracketCharactersAttribute"/>.
        /// </summary>
        private static readonly SearchValues<char> s_restrictedCharSet = SearchValues.Create('=', ',', '\'', '"', '{', '}', '\\');

        private bool _initializing;

        private string _startChars;
        private string _endChars;
        private StringBuilder _startCharactersStringBuilder;
        private StringBuilder _endCharactersStringBuilder;

        internal SpecialBracketCharacters()
        {
            BeginInit();
        }

        internal SpecialBracketCharacters(IReadOnlyDictionary<char, char> attributeList)
        {
            BeginInit();

            if (attributeList?.Count > 0)
            {
                Tokenize(attributeList);
            }
        }

        internal void AddBracketCharacters(char openingBracket, char closingBracket)
        {
            if (!_initializing)
                throw new InvalidOperationException();

            _startCharactersStringBuilder.Append(openingBracket);
            _endCharactersStringBuilder.Append(closingBracket);
        }

        private void Tokenize(IReadOnlyDictionary<char, char> attributeList)
        {
            if (!_initializing)
                throw new InvalidOperationException();

            foreach (char openingBracket in attributeList.Keys)
            {
                char closingBracket = attributeList[openingBracket];

                if (IsValidBracketCharacter(openingBracket, closingBracket))
                {
                    _startCharactersStringBuilder.Append(openingBracket);
                    _endCharactersStringBuilder.Append(closingBracket);
                }
            }
        }

        private static bool IsValidBracketCharacter(char openingBracket, char closingBracket)
        {
            if (openingBracket == closingBracket)
                throw new InvalidOperationException("Opening bracket character cannot be the same as closing bracket character.");
            else if (char.IsLetterOrDigit(openingBracket) || char.IsLetterOrDigit(closingBracket) || char.IsWhiteSpace(openingBracket) || char.IsWhiteSpace(closingBracket))
                throw new InvalidOperationException("Bracket characters cannot be alpha-numeric or whitespace.");
            else if (s_restrictedCharSet.Contains(openingBracket) || s_restrictedCharSet.Contains(closingBracket))
                throw new InvalidOperationException("Bracket characters cannot be one of the following: '=' , ',', '\'', '\"', '{ ', ' }', '\\'");

            return true;
        }

        internal bool IsSpecialCharacter(char ch)
        {
            return _startChars.Contains(ch) || _endChars.Contains(ch);
        }

        internal bool StartsEscapeSequence(char ch)
        {
            return _startChars.Contains(ch);
        }

        internal bool EndsEscapeSequence(char ch)
        {
            return _endChars.Contains(ch);
        }

        internal bool Match(char start, char end)
        {
            return _endChars.IndexOf(end, StringComparison.Ordinal) == _startChars.IndexOf(start, StringComparison.Ordinal);
        }

        internal string StartBracketCharacters
        {
            get { return _startChars; }
        }

        internal string EndBracketCharacters
        {
            get { return _endChars; }
        }

        public void BeginInit()
        {
            if (_initializing)
                throw new InvalidOperationException();

            _initializing = true;
            _startCharactersStringBuilder = new StringBuilder();
            _endCharactersStringBuilder = new StringBuilder();
        }

        public void EndInit()
        {
            if (!_initializing)
                throw new InvalidOperationException();

            _startChars = _startCharactersStringBuilder.ToString();
            _endChars = _endCharactersStringBuilder.ToString();

            // Clear the references, otherwise we could consider this a memory leak
            _startCharactersStringBuilder = null;
            _endCharactersStringBuilder = null;

            _initializing = false;
        }
#else
        /// <summary>
        /// Stores characters that cannot be specified in <see cref="MarkupExtensionBracketCharactersAttribute"/>.
        /// </summary>
        private static readonly ISet<char> s_restrictedCharSet = new SortedSet<char>(new char[] { '=', ',', '\'', '"', '{', '}', '\\' });

        private bool _initializing;

        private string _startChars;
        private string _endChars;
        private StringBuilder _startCharactersStringBuilder;
        private StringBuilder _endCharactersStringBuilder;

        internal SpecialBracketCharacters()
        {
            BeginInit();
        }

        internal SpecialBracketCharacters(IReadOnlyDictionary<char, char> attributeList)
        {
            BeginInit();

            if (attributeList?.Count > 0)
            {
                Tokenize(attributeList);
            }
        }

        internal void AddBracketCharacters(char openingBracket, char closingBracket)
        {
            if (!_initializing)
                throw new InvalidOperationException();

            _startCharactersStringBuilder.Append(openingBracket);
            _endCharactersStringBuilder.Append(closingBracket);
        }

        private void Tokenize(IReadOnlyDictionary<char, char> attributeList)
        {
            if (!_initializing)
                throw new InvalidOperationException();

            foreach (char openingBracket in attributeList.Keys)
            {
                char closingBracket = attributeList[openingBracket];

                if (IsValidBracketCharacter(openingBracket, closingBracket))
                {
                    _startCharactersStringBuilder.Append(openingBracket);
                    _endCharactersStringBuilder.Append(closingBracket);
                }
            }
        }

        private bool IsValidBracketCharacter(char openingBracket, char closingBracket)
        {
            if (openingBracket == closingBracket)
                throw new InvalidOperationException("Opening bracket character cannot be the same as closing bracket character.");
            else if (char.IsLetterOrDigit(openingBracket) || char.IsLetterOrDigit(closingBracket) || char.IsWhiteSpace(openingBracket) || char.IsWhiteSpace(closingBracket))
                throw new InvalidOperationException("Bracket characters cannot be alpha-numeric or whitespace.");
            else if (s_restrictedCharSet.Contains(openingBracket) || s_restrictedCharSet.Contains(closingBracket))
                throw new InvalidOperationException("Bracket characters cannot be one of the following: '=' , ',', '\'', '\"', '{ ', ' }', '\\'");

            return true;
        }

        internal bool IsSpecialCharacter(char ch)
        {
            return _startChars.Contains(ch.ToString()) || _endChars.Contains(ch.ToString());
        }

        internal bool StartsEscapeSequence(char ch)
        {
            return _startChars.Contains(ch.ToString());
        }

        internal bool EndsEscapeSequence(char ch)
        {
            return _endChars.Contains(ch.ToString());
        }

        internal bool Match(char start, char end)
        {
            return _endChars.IndexOf(end.ToString(), StringComparison.Ordinal) == _startChars.IndexOf(start.ToString(), StringComparison.Ordinal);
        }

        internal string StartBracketCharacters
        {
            get { return _startChars; }
        }

        internal string EndBracketCharacters
        {
            get { return _endChars; }
        }

        public void BeginInit()
        {
            if (_initializing)
                throw new InvalidOperationException();

            _initializing = true;
            _startCharactersStringBuilder = new StringBuilder();
            _endCharactersStringBuilder = new StringBuilder();
        }

        public void EndInit()
        {
            if (!_initializing)
                throw new InvalidOperationException();

            _startChars = _startCharactersStringBuilder.ToString();
            _endChars = _endCharactersStringBuilder.ToString();

            // Clear the references, otherwise we could consider this a memory leak
            _startCharactersStringBuilder = null;
            _endCharactersStringBuilder = null;

            _initializing = false;
        }
#endif
    }
}
