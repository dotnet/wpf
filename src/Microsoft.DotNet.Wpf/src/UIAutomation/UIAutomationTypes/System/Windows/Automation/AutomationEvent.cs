// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Identifier for Automation Events

using System;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Identifier for Automation Events
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class AutomationEvent: AutomationIdentifier
#else
    public class AutomationEvent: AutomationIdentifier
#endif
    {
        internal AutomationEvent(int id, string programmaticName)
            : base(UiaCoreTypesApi.AutomationIdType.Event, id, programmaticName)
        {
        }

        /// <summary>
        /// </summary>
        internal static AutomationEvent Register(AutomationIdentifierConstants.Events id, string programmaticName)
        {
            return (AutomationEvent)AutomationIdentifier.Register(UiaCoreTypesApi.AutomationIdType.Event, (int)id, programmaticName);
        }

        /// <summary>
        /// </summary>
        public static AutomationEvent LookupById(int id)
        {
            return (AutomationEvent)AutomationIdentifier.LookupById(UiaCoreTypesApi.AutomationIdType.Event, id);
        }
    }
}
