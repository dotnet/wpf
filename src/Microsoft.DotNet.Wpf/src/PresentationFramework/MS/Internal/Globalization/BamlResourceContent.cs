// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Utility that handles parsing Baml Resource Content
// 

using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Buffers;

namespace MS.Internal.Globalization
{
    internal static class BamlResourceContentUtil
    {
        //-------------------------------------
        // Internal methods
        //-------------------------------------                
        /// <summary>
        /// Escape a string
        /// </summary>
        internal static string EscapeString(string content)
        {
            if (content is null)
                return null;

            StringBuilder builder = new(content.Length * 2);
            for (int i = 0; i < content.Length; i++)
            {
                switch (content[i])
                {
                    case BamlConst.ChildStart:
                    case BamlConst.ChildEnd:
                    case BamlConst.EscapeChar:
                        {
                            builder.Append(BamlConst.EscapeChar);
                            builder.Append(content[i]);
                            break;
                        }
                    case '&':
                        {
                            builder.Append("&amp;");
                            break;
                        }
                    case '<':
                        {
                            builder.Append("&lt;");
                            break;
                        }
                    case '>':
                        {
                            builder.Append("&gt;");
                            break;
                        }
                    case '\'':
                        {
                            builder.Append("&apos;");
                            break;
                        }
                    case '\"':
                        {
                            builder.Append("&quot;");
                            break;
                        }
                    default:
                        {
                            builder.Append(content[i]);
                            break;
                        }
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Holds all escape tokens used for initial string-search loop to find out whether we need to unescape the string.
        /// </summary>
        private static readonly SearchValues<string> s_escapeTokens = SearchValues.Create(["\\", "&quot;", "&apos;", "&amp;", "&lt;", "&gt;"], StringComparison.Ordinal);

        /// <summary>
        /// Unescape a string. Note:
        /// Backslash following any character will become that character.
        /// Backslash by itself will be skipped.
        /// </summary>
        /// <remarks>Prefer <see cref="UnescapeString(ReadOnlySpan{char})"/> overload when possible.</remarks>
        internal static string UnescapeString(string content) => UnescapeString(content.AsSpan(), false) ?? content;

        /// <summary>
        /// Unescape a string. Note:
        /// Backslash following any character will become that character.
        /// Backslash by itself will be skipped.
        /// </summary>
        internal static string UnescapeString(ReadOnlySpan<char> contentSpan, bool returnNewInstance = true)
        {
            // Check whether there's anything to unescape
            int firstEscapeToken = contentSpan.IndexOfAny(s_escapeTokens);
            if (firstEscapeToken == -1)
                return returnNewInstance ? new string(contentSpan) : null;

            // Allocate buffer and append the chunk without tokens (unescaped)
            StringBuilder stringBuilder = new(contentSpan.Length);
            stringBuilder.Append(contentSpan.Slice(0, firstEscapeToken));

            for (int i = firstEscapeToken; i < contentSpan.Length; i++)
            {
                if (contentSpan[i] == BamlConst.EscapeChar) // An escape token ('\')
                {
                    if (contentSpan.Length > i + 1) // Check whether we're at the end
                    {
                        i++;
                        stringBuilder.Append(contentSpan[i]);
                    }
                    else // We are, break out of the loop
                        break;
                }
                else if (contentSpan[i] == '&') // A known escape sequence shall follow
                {
                    EvaulateEscapeSequence(stringBuilder, contentSpan, ref i);
                }
                else // Nothing interesting, append character
                    stringBuilder.Append(contentSpan[i]);
            }

            // Evaluates whether any of the known escape sequences follows '&' (&quot; - &apos; - &amp; - &lt; - &gt;)
            static void EvaulateEscapeSequence(StringBuilder stringBuilder, ReadOnlySpan<char> contentSpan, ref int i)
            {
                contentSpan = contentSpan.Slice(i);

                if (contentSpan.Length > 5 && contentSpan[5] == ';')
                {
                    if (contentSpan.Slice(0, 6).SequenceEqual("&quot;"))
                    {
                        stringBuilder.Append('"');
                        i += 5;
                        return;
                    }
                    else if (contentSpan.Slice(0, 6).SequenceEqual("&apos;"))
                    {
                        stringBuilder.Append('\'');
                        i += 5;
                        return;
                    }
                }
                else if (contentSpan.Length > 4 && contentSpan[4] == ';')
                {
                    if (contentSpan.Slice(0, 5).SequenceEqual("&amp;"))
                    {
                        stringBuilder.Append('&');
                        i += 4;
                        return;
                    }
                }
                else if (contentSpan.Length > 3 && contentSpan[3] == ';')
                {
                    if (contentSpan.Slice(0, 4).SequenceEqual("&lt;"))
                    {
                        stringBuilder.Append('<');
                        i += 3;
                        return;
                    }
                    else if (contentSpan.Slice(0, 4).SequenceEqual("&gt;"))
                    {
                        stringBuilder.Append('>');
                        i += 3;
                        return;
                    }
                }

                // Default case, no escaped sequence found
                stringBuilder.Append('&');
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Parse the input string into an array of text/child-placeholder tokens. 
        /// Element placeholders start with '#' and end with ';'. 
        /// In case of error, a null array is returned. 
        /// </summary>
        internal static ReadOnlySpan<BamlStringToken> ParseChildPlaceholder(string input)
        {
            if (input is null)
                return ReadOnlySpan<BamlStringToken>.Empty;

            List<BamlStringToken> tokens = new(8);
            int tokenStart = 0;
            bool inPlaceHolder = false;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == BamlConst.ChildStart)
                {
                    if (i == 0 || input[i - 1] != BamlConst.EscapeChar)
                    {
                        if (inPlaceHolder)
                        {
                            // All # needs to be escaped in a child place holder
                            return ReadOnlySpan<BamlStringToken>.Empty; // error
                        }

                        inPlaceHolder = true;
                        if (tokenStart < i)
                        {
                            tokens.Add(
                                new BamlStringToken(
                                    BamlStringToken.TokenType.Text,
                                    UnescapeString(input.AsSpan(tokenStart, i - tokenStart))
                                    )
                                );
                            tokenStart = i;
                        }
                    }
                }
                else if (input[i] == BamlConst.ChildEnd)
                {
                    if (i > 0
                       && input[i - 1] != BamlConst.EscapeChar
                       && inPlaceHolder)
                    {
                        // It is a valid child placeholder end
                        tokens.Add(
                            new BamlStringToken(
                                BamlStringToken.TokenType.ChildPlaceHolder,
                                UnescapeString(input.AsSpan(tokenStart + 1, i - tokenStart - 1))
                            )
                        );

                        // Advance the token index
                        tokenStart = i + 1;
                        inPlaceHolder = false;
                    }
                }
            }

            if (inPlaceHolder)
            {
                // at the end of the string, all child placeholder must be closed
                return ReadOnlySpan<BamlStringToken>.Empty; // error
            }

            if (tokenStart < input.Length)
            {
                tokens.Add(
                    new BamlStringToken(
                        BamlStringToken.TokenType.Text,
                        UnescapeString(input.AsSpan(tokenStart))
                        )
                    );
            }

            return CollectionsMarshal.AsSpan(tokens);
        }
    }


    internal readonly struct BamlStringToken
    {
        internal readonly TokenType Type;
        internal readonly string Value;

        internal BamlStringToken(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }

        internal enum TokenType
        {
            Text,
            ChildPlaceHolder,
        }
    }
}

