// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using BindingFlags = System.Reflection.BindingFlags;

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// General utilities/interface for Code Generation
    /// </summary>
    public abstract class CodeGenerator
    {
        /// <summary/>
        public abstract string GenerateCode();

        /// <summary>
        /// Indent 1 to n lines of code.  Lines of code should end with '\n'.  string.Empty will not be indented.
        /// </summary>
        protected string Indent(string code)
        {
            // The input will be any of the following:
            //      string.Empty
            //      "\n"
            //      a single line of code
            //      multiple lines of code

            string temp = code.Replace("\n", "\n" + indent);
            if (temp.Length < indentLength)
            {
                return string.Empty;
            }

            // The replace call will not indent the first line of code
            temp = indent + temp;

            // Remove the trailing spaces added after the last line of code
            return temp.Substring(0, temp.Length - indentLength);
        }

        /// <summary/>
        protected const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private const string indent = "    ";
        private const int indentLength = 4;
    }
}
