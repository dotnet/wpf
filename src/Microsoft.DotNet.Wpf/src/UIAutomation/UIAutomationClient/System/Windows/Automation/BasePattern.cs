// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for Base Pattern

using System;
using System.Windows.Automation;
using System.Diagnostics;
using MS.Internal.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation
{
    /// <summary>
    /// Internal class
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class BasePattern
#else
    public class BasePattern
#endif
    {
        internal AutomationElement _el;
        private SafePatternHandle _hPattern;

        internal BasePattern( AutomationElement el, SafePatternHandle hPattern )
        {
            Debug.Assert(el != null);

            _el = el;
            _hPattern = hPattern;
        }

        /// <summary>
        /// Overrides Object.Finalize
        /// </summary>
        ~BasePattern()
        {
        }
    }
}
