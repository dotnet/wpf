// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿/*++
All rights reserved.

 Internal API for testing
  
--*/

namespace MS.Internal.Printing
{
    using System;

    /// <summary>
    /// Internal API for testing
    /// </summary>
    internal static class TestHook
    {
        public static void EnableFallbackPrinting(bool value)
        {
            _isFallbackPrintingEnabled = value;
        }

        internal static bool _isFallbackPrintingEnabled;
    }
}
