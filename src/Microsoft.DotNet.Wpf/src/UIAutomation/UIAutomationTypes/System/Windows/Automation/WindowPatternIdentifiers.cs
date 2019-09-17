// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for Window Pattern

using System;
using MS.Internal.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation
{
    // Disable warning for obsolete types.  These are scheduled to be removed in M8.2 so 
    // only need the warning to come out for components outside of APT.
    #pragma warning disable 0618

    /// <summary>
    /// following the Office and HTML definition of WindowState.
    /// </summary>
    [ComVisible(true)]
    [Guid("fdc8f176-aed2-477a-8c89-ea04cc5f278d")]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal enum WindowVisualState
#else
    public enum WindowVisualState
#endif
    {
        /// <summary>window is normal</summary>
        Normal, 
        
        /// <summary>window is maximized</summary>
        Maximized,
        
        /// <summary>window is minimized</summary>
        Minimized        
    }

    /// <summary>
    /// The current state of the window for user interaction
    /// </summary>
    [ComVisible(true)]
    [Guid("65101cc7-7904-408e-87a7-8c6dbd83a18b")]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal enum WindowInteractionState
#else
    public enum WindowInteractionState
#endif
    {
        /// <summary>
        /// window is running.  This does not guarantee that the window ready for user interaction,
        /// nor does it guarantee the windows is not "not responding".
        /// </summary>
        Running, 
        
        /// <summary>window is closing</summary>
        Closing,
        
        /// <summary>window is ready for the user to interact with it</summary>
        ReadyForUserInteraction, 
        
        /// <summary>window is block by a modal window.</summary>
        BlockedByModalWindow,   
        
        /// <summary>window is not responding</summary>
        NotResponding   
    }
       
    ///<summary>wrapper class for Window pattern </summary>
#if (INTERNAL_COMPILE)
    internal static class WindowPatternIdentifiers
#else
    public static class WindowPatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Returns the Window pattern identifier</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.Window, "WindowPatternIdentifiers.Pattern");

        /// <summary>Property ID: CanMaximize - </summary>
        public static readonly AutomationProperty CanMaximizeProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.WindowCanMaximize, "WindowPatternIdentifiers.CanMaximizeProperty");

        /// <summary>Property ID: CanMinimize - </summary>
        public static readonly AutomationProperty CanMinimizeProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.WindowCanMinimize, "WindowPatternIdentifiers.CanMinimizeProperty");

        /// <summary>Property ID: IsModal - Is this is a modal window</summary>
        public static readonly AutomationProperty IsModalProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.WindowIsModal, "WindowPatternIdentifiers.IsModalProperty");

        /// <summary>Property ID: WindowVisualState - Is the Window Maximized, Minimized, or Normal (aka restored)</summary>
        public static readonly AutomationProperty WindowVisualStateProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.WindowWindowVisualState, "WindowPatternIdentifiers.WindowVisualStateProperty");

        /// <summary>Property ID: WindowInteractionState - Is the Window Closing, ReadyForUserInteraction, BlockedByModalWindow or NotResponding.</summary>
        public static readonly AutomationProperty WindowInteractionStateProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.WindowWindowInteractionState, "WindowPatternIdentifiers.WindowInteractionStateProperty");

        /// <summary>Property ID: - This window is always on top</summary>
        public static readonly AutomationProperty IsTopmostProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.WindowIsTopmost, "WindowPatternIdentifiers.IsTopmostProperty");

        /// <summary>Event ID: WindowOpened - Immediately after opening the window - ApplicationWindows or Window Status is not guarantee to be: ReadyForUserInteraction</summary>
        public static readonly AutomationEvent WindowOpenedEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.Window_WindowOpened, "WindowPatternIdentifiers.WindowOpenedProperty");

        /// <summary>Event ID: WindowClosed - Immediately after closing the window</summary>
        public static readonly AutomationEvent WindowClosedEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.Window_WindowClosed, "WindowPatternIdentifiers.WindowClosedProperty");

        #endregion Public Constants and Readonly Fields
    }
}
