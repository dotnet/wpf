// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Identifier for Automation Text Attributes


using System;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Identifier for Automation Text Attributes
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class AutomationTextAttribute: AutomationIdentifier
#else
    public class AutomationTextAttribute: AutomationIdentifier
#endif
    {

        internal AutomationTextAttribute(int id, string programmaticName)
            : base(UiaCoreTypesApi.AutomationIdType.TextAttribute, id, programmaticName)
        {
        }

        /// <summary>
        /// </summary>
        internal static AutomationTextAttribute Register(AutomationIdentifierConstants.TextAttributes id, string programmaticName)
        {
            return (AutomationTextAttribute)AutomationIdentifier.Register(UiaCoreTypesApi.AutomationIdType.TextAttribute, (int)id, programmaticName);
        }

        /// <summary>
        /// </summary>
        public static AutomationTextAttribute LookupById(int id)
        {
            return (AutomationTextAttribute)AutomationIdentifier.LookupById(UiaCoreTypesApi.AutomationIdType.TextAttribute, id);
        }
    }
}
