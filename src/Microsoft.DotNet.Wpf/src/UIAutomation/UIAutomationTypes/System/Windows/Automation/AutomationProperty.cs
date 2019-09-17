// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Identifier for Automation Properties


using System;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Identifier for Automation Properties
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class AutomationProperty: AutomationIdentifier
#else
    public class AutomationProperty: AutomationIdentifier
#endif
    {
        internal AutomationProperty(int id, string programmaticName)
            : base(UiaCoreTypesApi.AutomationIdType.Property, id, programmaticName)
        {
        }

        /// <summary>
        /// </summary>
        internal static AutomationProperty Register(AutomationIdentifierConstants.Properties id, string programmaticName)
        {
            return (AutomationProperty)AutomationIdentifier.Register(UiaCoreTypesApi.AutomationIdType.Property, (int)id, programmaticName);
        }


        /// <summary>
        /// </summary>
        public static AutomationProperty LookupById(int id)
        {
            return (AutomationProperty)AutomationIdentifier.LookupById(UiaCoreTypesApi.AutomationIdType.Property, id);
        }
    }
}
