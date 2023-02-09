// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Resources;

namespace MS.Internal.PresentationCore
{
    internal static partial class SR
    {
        public static string Get(string name)
        {
            return GetResourceString(name, null);
        }
        
        public static string Get(string name, params object[] args)
        {
            return Format(GetResourceString(name, null), args);
        }

        // Remove properties below when this project uses properties for resources.

        /// <summary>MC3098: Unexpected token '{0}' at position '{1}'.</summary>
        internal static string @Parser_UnexpectedToken => GetResourceString("Parser_UnexpectedToken");
        /// <summary>MC3096: Token is not valid.</summary>
        internal static string @Parsers_IllegalToken => GetResourceString("Parsers_IllegalToken");
        /// <summary>Unknown path operation attempted.</summary>
        internal static string @UnknownPathOperationType => GetResourceString("UnknownPathOperationType");
    }
}
