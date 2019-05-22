// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Utility that handles parsing Baml Resource Content
// 

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

using System.Windows;

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
            if (content == null) return null;

            StringBuilder builder = new StringBuilder();
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
        /// Unescape a string. Note:
        /// Backslash following any character will become that character.
        /// Backslash by itself will be skipped.
        /// </summary>
        internal static string UnescapeString(string content)
        {
            return UnescapePattern.Replace(
                content,
                UnescapeMatchEvaluator
                );
        }

        // Regular expression
        // need to use 4 backslash here because it is escaped by compiler and regular expressions
        private static Regex UnescapePattern = new Regex("(\\\\.?|&lt;|&gt;|&quot;|&apos;|&amp;)", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        // delegates to escape and unesacpe a matched pattern
        private static MatchEvaluator UnescapeMatchEvaluator = new MatchEvaluator(UnescapeMatch);

        /// <summary>
        /// the delegate to Unescape the matched pattern
        /// </summary>
        private static string UnescapeMatch(Match match)
        {
            switch (match.Value)
            {
                case "&lt;": return "<";
                case "&gt;": return ">";
                case "&amp;": return "&";
                case "&apos;": return "'";
                case "&quot;": return "\"";
                default:
                    {
                        // this is a '\' followed by 0 or 1 character                    
                        Debug.Assert(match.Value.Length > 0 && match.Value[0] == BamlConst.EscapeChar);
                        if (match.Value.Length == 2)
                        {
                            return match.Value[1].ToString();
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }
            }
        }

        /// <summary>
        /// Parse the input string into an array of text/child-placeholder tokens. 
        /// Element placeholders start with '#' and end with ';'. 
        /// In case of error, a null array is returned. 
        /// </summary>
        internal static BamlStringToken[] ParseChildPlaceholder(string input)
        {
            if (input == null) return null;

            List<BamlStringToken> tokens = new List<BamlStringToken>(8);
            int tokenStart = 0; bool inPlaceHolder = false;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == BamlConst.ChildStart)
                {
                    if (i == 0 || input[i - 1] != BamlConst.EscapeChar)
                    {
                        if (inPlaceHolder)
                        {
                            // All # needs to be escaped in a child place holder
                            return null; // error
                        }

                        inPlaceHolder = true;
                        if (tokenStart < i)
                        {
                            tokens.Add(
                                new BamlStringToken(
                                    BamlStringToken.TokenType.Text,
                                    UnescapeString(input.Substring(tokenStart, i - tokenStart))
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
                                UnescapeString(input.Substring(tokenStart + 1, i - tokenStart - 1))
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
                return null; // error
            }

            if (tokenStart < input.Length)
            {
                tokens.Add(
                    new BamlStringToken(
                        BamlStringToken.TokenType.Text,
                        UnescapeString(input.Substring(tokenStart))
                        )
                    );
            }

            return tokens.ToArray();
        }
    }


    internal struct BamlStringToken
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

