// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.FaultInjection
{
    internal static class BuiltInTypeHelper
    {
        #region Public Methods

        public static string AliasToFullName(string alias)
        {
            switch (alias)
            {
                case "bool": return "System.Boolean";
                case "byte": return "System.Byte";
                case "sbyte": return "System.SByte";
                case "char": return "System.Char";
                case "decimal": return "System.Decimal";
                case "double": return "System.Double";
                case "float": return "System.Single";
                case "int": return "System.Int32";
                case "uint": return "System.UInt32";
                case "long": return "System.Int64";
                case "ulong": return "System.UInt64";
                case "object": return "System.Object";
                case "short": return "System.Int16";
                case "ushort": return "System.UInt16";
                case "string": return "System.String";
                case "void": return "System.Void";
            }
            return null;
        }

        #endregion  // Public Methods
    }
}