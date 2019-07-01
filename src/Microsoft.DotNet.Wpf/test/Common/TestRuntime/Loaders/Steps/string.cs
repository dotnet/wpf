// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Utilities
{
    /// <summary>
    /// String contains methods to manipulate strings not available in the System.String class
    /// </summary>
    internal class StringUtils
    {
        /// <summary>
        /// Takes spaces both from left and right of a string
        /// </summary>
        /// <param name="S">the string the leave with no spaces neither in the beggining nor in the end</param>
        /// <returns>the resulting string</returns>
        internal static string TrimSpaces( String S )
        {
            int start = 0;
            int end = 0;

            // find the first non-space char
            for ( start = 0 ;
                start < S.Length && (S[start] == ' ' || S[start] == '\r' || S[start] == '\n' || S[start] == '\t') ;
                start++ ) ;

            // find the last non-space char
            for ( end = S.Length - 1 ;
                end >= 0 && ( S[end] == ' ' || S[end] == '\r' || S[end] == '\n' || S[end] == '\t' ) ;
                end-- ) ;

            // return the trimed string
            return ( S.Substring(start, end - start + 1) ) ;
        }

        /// <summary>
        /// Builds a memory stream from a given string.
        /// </summary>
        /// <param name="s">the string to be used to build the memory stream</param>
        /// <returns>the memory stream built from the given string</returns>
        internal static MemoryStream MemoryStreamFromString(string s)
        {
            // create a new memory stream
            MemoryStream ms = new MemoryStream();

            // we need to move the string chars to a byte array to write to the memory stream
            char[] chars = s.ToCharArray();
            byte[] bytes = new byte[chars.Length];
            for (int i = 0; i < chars.Length; i++)
            {
                bytes[i] = (byte)chars[i];
            }

            // fill the memory stream with the bytes taken from the string
            ms.Write(bytes, 0, chars.Length);

            // leave the memory stream cursor in the start
            ms.Seek(0, SeekOrigin.Begin);

            // return the memory stream
            return (ms);
        }

        internal delegate string MatchProcessor(string match);

        /// <summary>
        /// looks for a pattern on a given text, calling a method that can process a pattern and return a string to replace that ocurrence
        /// or null to just look for it.
        /// </summary>
        /// <param name="originalText"></param>
        /// <param name="pattern"></param>
        /// <param name="matchProcessor"></param>
        /// <returns></returns>
        internal static string ProcessMatches(string originalText, string pattern, MatchProcessor matchProcessor)
        {
            string text = originalText;
            Regex r = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
            Match m;
            int offset = 0;
            for (m = r.Match(text); m.Success; m = m.NextMatch())
            {
                GlobalLog.LogDebug("found=" + m.Groups[1] + " at " + m.Groups[1].Index);
                if (matchProcessor != null)
                {
                    Group current = m.Groups[1];
                    string replacement = matchProcessor(current.Value);
                    if (replacement != null)
                    {
                        text = Replace(text, replacement, current.Index + offset, current.Length);
                        offset += (replacement.Length - current.Value.Length);
                    }
                }
            }
            return (text);
        }

        /// <summary>
        /// Replaces the specified characters of a string
        /// </summary>
        /// <param name="text">the string whose specified characters will be replaced</param>
        /// <param name="toInsert">the replacement string</param>
        /// <param name="indexFrom">the index of the first character to be replaced</param>
        /// <param name="length">the quantity of characters to be replaced from indexFrom</param>
        /// <returns>a string with the replacements made</returns>
        internal static string Replace(string text, string toInsert, int indexFrom, int length)
        {
            // get string chars before the positions where change begins
            string result = text.Substring(0, indexFrom);

            // insert the toInsert
            result += toInsert;

            // copy string chars after the changed substring
            result += text.Substring(indexFrom + length);

            // return the new string
            return (result);
        }

        /// <summary>
        /// Returns whether a string matches a pattern
        /// </summary>
        /// <param name="pattern">the regular expression pattern</param>
        /// <param name="s">the string to match</param>
        /// <returns>true if matches, false otherwise</returns>
        internal static bool Matches(string pattern, string s)
        {
            // create reg exp
            Regex r = new Regex(pattern);

            // evaluate if matches
            return (r.Match(s).Success);
        }

        /// <summary>
        /// Returns an array with the matches of the pattern in the text
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static string[] GetMatches(string text, string pattern)
        {
            ArrayList list = new ArrayList();

            // compile the regular expression
            Regex r = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

            // match the regular expression pattern against a text string
            Match m = r.Match(text);
            while (m.Success)
            {
                for (int i = 1; i < m.Length; i++)
                {
                    foreach (Capture c in m.Groups[i].Captures)
                    {
                        list.Add(c.Value);
                    }
                }
                m = m.NextMatch();
            }

            // return a string array
            object[] o = list.ToArray();
            string[] s = new string[list.Count];
            for (uint i = 0; i < o.Length; i++)
            {
                s[i] = (string)o[i];
            }
            return (s);
        }

        /// <summary>
        /// ExtractSiteFromUrl
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal static string ExtractSiteFromUrl(string url)
        {
            string site = url.Substring(0, url.LastIndexOf('/'));
            return (site);
        }
    }

    /// <summary>
    /// UrlGetParameters
    /// </summary>
    internal class UrlGetParameters
    {
        /// <summary>
        /// parameters collection
        /// </summary>
        private Hashtable parameters = new Hashtable();

        /// <summary>
        /// UrlGetParameters
        /// </summary>
        /// <param name="url"></param>
        internal UrlGetParameters(string url)
        {
            // grab the stuff after '?'
            const char questionMark = '?';
            int firstQuestionMarkPosition = url.IndexOf(questionMark);
            int parametersStartPosition;
            if (firstQuestionMarkPosition == -1)
            {
                // no question mark; take entire string
                parametersStartPosition = 0;
            }
            else
            {
                parametersStartPosition = firstQuestionMarkPosition + 1;
            }
            string parametersPart = url.Substring(parametersStartPosition);

            // parse
            char[] etSeparator = { '&' };
            string[] pairs = parametersPart.Split(etSeparator);
            foreach (string pair in pairs)
            {
                AddParameter(pair);
            }
        }

        /// <summary>
        /// AddParam
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        internal void AddParameter(string name, string value)
        {
            parameters.Add(name, value);
        }

        /// <summary>
        /// AddParameter("name=value")
        /// </summary>
        /// <param name="pair"></param>
        internal void AddParameter(string pair)
        {
            char[] equalSeparator = { '=' };
            string[] members = pair.Split(equalSeparator);
            if (members.Length == 2)
            {
                parameters.Add(members[0], members[1]);
            }
        }

        /// <summary>
        /// []
        /// </summary>
        /// <param name="var"></param>
        /// <returns></returns>
        internal string this[string var]
        {
            get
            {
                string val = (string)parameters[var];
                if (val == null)
                {
                    return (string.Empty);
                }
                else
                {
                    return (val);
                }
            }
        }
    }
}