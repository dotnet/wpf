// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Identifier for Automation Patterns


using System;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Identifier for Automation Patterns
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class AutomationPattern: AutomationIdentifier
#else
    public class AutomationPattern: AutomationIdentifier
#endif
    {
        internal AutomationPattern(int id, string programmaticName)
            : base(UiaCoreTypesApi.AutomationIdType.Pattern, id, programmaticName)
        {
        }

        /// <summary>
        /// </summary>
        internal static AutomationPattern Register(AutomationIdentifierConstants.Patterns id, string programmaticName)
        {
            return (AutomationPattern)AutomationIdentifier.Register(UiaCoreTypesApi.AutomationIdType.Pattern, (int)id, programmaticName);
        }

        /// <summary>
        /// </summary>
        public static AutomationPattern LookupById(int id)
        {
            return (AutomationPattern)AutomationIdentifier.LookupById(UiaCoreTypesApi.AutomationIdType.Pattern, id);
        }
    }
}
