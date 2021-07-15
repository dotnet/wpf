// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for SynchronizedInput Pattern

using System;
using System.Collections;
using System.ComponentModel;
using MS.Internal.Automation;
using System.Runtime.InteropServices;


namespace System.Windows.Automation
{
    [ComVisible(true)]
    [Guid("fdc8f176-aed2-477a-8c89-5604c66f278d")]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal  enum SynchronizedInputType
#else
    public enum SynchronizedInputType
#endif
    {
        KeyUp                = 0x01,
        KeyDown              = 0x02,
        MouseLeftButtonUp    = 0x04,
        MouseLeftButtonDown  = 0x08,
        MouseRightButtonUp   = 0x10,
        MouseRightButtonDown = 0x20
    }
	
    /// <summary>
    /// Class representing containers that manage SynchronizedInput.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class SynchronizedInputPatternIdentifiers
#else
    public static class SynchronizedInputPatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>SynchronizedInput pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.SynchronizedInput, "SynchronizedInputPatternIdentifiers.Pattern");

        
        /// <summary>
        /// Event ID: InputReachedTarget - indicates input received by the current listening element.
        /// sourceElement  refers to the current listening element.
        /// </summary>
        public static readonly AutomationEvent InputReachedTargetEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.InputReachedTarget, "SynchronizedInputPatternIdentifiers.InputReachedTargetEvent");
        /// <summary>
        /// Event ID: InputReachedOtherElement - indicates an input is handled by different element than the one currently listening.
        /// sourceElement refers to the current listening element..
        /// </summary>
        public static readonly AutomationEvent InputReachedOtherElementEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.InputReachedOtherElement, "SynchronizedInputPatternIdentifiers.InputReachedOtherElementEvent");
        /// <summary>
        /// Event ID: InputDiscarded - indicates that input is discarded by the framework.
        /// sourceElement refers to the  current listening element.
        /// </summary>
        public static readonly AutomationEvent InputDiscardedEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.InputDiscarded, "SynchronizedInputPatternIdentifiers.InputDiscardedEvent");

        #endregion Public Constants and Readonly Fields

        
    
    }
}
