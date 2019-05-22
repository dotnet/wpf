// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++


Abstract:

    Definition and implementation of the internal PTUtility class, which
    contains static utility functions used internally only.



--*/

using System;
using System.Collections.Specialized;
using System.Resources;

namespace MS.Internal.Printing.Configuration
{
    /// <summary>
    /// PTUtility class that supports static utility functions
    /// </summary>
    internal static class PTUtility
    {
        /// <summary>
        /// Checks whether or not the HRESULT code indicates a success
        /// </summary>
        /// <param name="hResult">the HRESULT code to check</param>
        /// <returns>true if the HRESULT indicates a success, false otherwise</returns>
        public static bool IsSuccessCode(uint hResult)
        {
            return (hResult < 0x80000000);
        }

        /// <summary>
        /// Gets localized text from assembly embedded resource.
        /// </summary>
        public static string GetTextFromResource(string key)
        {
            return _resManager.GetString(key, System.Threading.Thread.CurrentThread.CurrentUICulture);
        }

        private static ResourceManager _resManager =
            new ResourceManager("System.Printing", typeof(PTUtility).Assembly);
    }
}
