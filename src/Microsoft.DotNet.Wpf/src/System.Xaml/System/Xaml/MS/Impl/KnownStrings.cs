// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xaml.MS.Impl
{
    internal static class KnownStrings
    {
        // Built-in strings.
        public const string XmlPrefix   = "xml";
        public const string XmlNsPrefix = "xmlns";

        public const string Preserve = "preserve";
        public const string Default = "default";

        public const string UriClrNamespace = "clr-namespace";
        public const string UriAssembly = "assembly";

        public const string Get = "Get";
        public const string Set = "Set";
        public const string Add = "Add";
        public const string Handler = "Handler";
        public const string Extension = "Extension";
        public const string IsReadOnly = "IsReadOnly";
        public const string ShouldSerialize = "ShouldSerialize";

        public const char GraveQuote = '`';
        public const char NestedTypeDelimiter = '+';
        public const string GetEnumerator = "GetEnumerator";
        public const string NullableOfT = "Nullable`1";

        public const string LocalPrefix = "local";
        public const string DefaultPrefix = "p";

        public const string ReferenceName = "__ReferenceID";
        public static readonly char[] WhitespaceChars = new char[] { ' ', '\t', '\n', '\r', '\f' };
        public const char SpaceChar = ' ';
        public const char TabChar = '\t';
        public const char NewlineChar = '\n';
        public const char ReturnChar = '\r';

        public const string CreateDelegateHelper = "_CreateDelegate";
        public const string CreateDelegate = "CreateDelegate";
        public const string InvokeMember = "InvokeMember";
        public const string GetTypeFromHandle = "GetTypeFromHandle";

        public const string Member = "Member";
        public const string Property = "Property";
    }

    /// <summary>
    /// String compare and formating class.
    /// To control standards of Localization and generally keep FxCop under control.
    /// </summary>
    internal static class KS
    {
        /// <summary>
        /// Standard String Compare operation.
        /// </summary>
        public static bool Eq(string a, string b)
        {
            return string.Equals(a, b, StringComparison.Ordinal);
        }

        /// <summary>
        /// Standard String Index search operation.
        /// </summary>
        public static int IndexOf(string src, char value)
        {
            return src.IndexOf(value);
        }

        /// <summary>
        /// Standard String Index search operation.
        /// </summary>
        public static int IndexOf(string src, string chars)
        {
            return src.IndexOf(chars, StringComparison.Ordinal);
        }

        /// <summary>
        /// Standard String Index search operation.
        /// </summary>
        public static int IndexOf(string src, char ch)
        {
            return src.IndexOf(ch, StringComparison.Ordinal);
        }

        public static bool EndsWith(string src, string target)
        {
            return src.EndsWith(target, StringComparison.Ordinal);
        }

        public static bool StartsWith(string src, string target)
        {
            return src.StartsWith(target, StringComparison.Ordinal);
        }

        public static string Fmt(string formatString, params object[] otherArgs)
        {
            return string.Format(TypeConverterHelper.InvariantEnglishUS, formatString, otherArgs);
        }
    }
}
