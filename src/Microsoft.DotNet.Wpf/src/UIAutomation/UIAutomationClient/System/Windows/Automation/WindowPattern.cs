// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for Window Pattern

using System;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation
{
    // Disable warning for obsolete types.  These are scheduled to be removed in M8.2 so 
    // only need the warning to come out for components outside of APT.
    #pragma warning disable 0618

    ///<summary>wrapper class for Window pattern </summary>
#if (INTERNAL_COMPILE)
    internal class WindowPattern: BasePattern
#else
    public class WindowPattern: BasePattern
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private WindowPattern(AutomationElement el, SafePatternHandle hPattern, bool cached)
            : base(el, hPattern)
        {
            _hPattern = hPattern;
            _cached = cached;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Returns the Window pattern identifier</summary>
        public static readonly AutomationPattern Pattern = WindowPatternIdentifiers.Pattern;

        /// <summary>Property ID: CanMaximize - </summary>
        public static readonly AutomationProperty CanMaximizeProperty = WindowPatternIdentifiers.CanMaximizeProperty;

        /// <summary>Property ID: CanMinimize - </summary>
        public static readonly AutomationProperty CanMinimizeProperty = WindowPatternIdentifiers.CanMinimizeProperty;

        /// <summary>Property ID: IsModal - Is this is a modal window</summary>
        public static readonly AutomationProperty IsModalProperty = WindowPatternIdentifiers.IsModalProperty;

        /// <summary>Property ID: WindowVisualState - Is the Window Maximized, Minimized, or Normal (aka restored)</summary>
        public static readonly AutomationProperty WindowVisualStateProperty = WindowPatternIdentifiers.WindowVisualStateProperty;

        /// <summary>Property ID: WindowInteractionState - Is the Window Closing, ReadyForUserInteraction, BlockedByModalWindow or NotResponding.</summary>
        public static readonly AutomationProperty WindowInteractionStateProperty = WindowPatternIdentifiers.WindowInteractionStateProperty;

        /// <summary>Property ID: - This window is always on top</summary>
        public static readonly AutomationProperty IsTopmostProperty = WindowPatternIdentifiers.IsTopmostProperty;

        /// <summary>Event ID: WindowOpened - Immediately after opening the window - ApplicationWindows or Window Status is not guarantee to be: ReadyForUserInteraction</summary>
        public static readonly AutomationEvent WindowOpenedEvent = WindowPatternIdentifiers.WindowOpenedEvent;

        /// <summary>Event ID: WindowClosed - Immediately after closing the window</summary>
        public static readonly AutomationEvent WindowClosedEvent = WindowPatternIdentifiers.WindowClosedEvent;

        #endregion Public Constants and Readonly Fields


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods
        
        /// <summary>
        /// Changes the State of the window based on the passed enum.
        /// </summary>
        /// <param name="state">The requested state of the window.</param>
        public void SetWindowVisualState( WindowVisualState state )
        {
            UiaCoreApi.WindowPattern_SetWindowVisualState(_hPattern, state);
        }

        /// <summary>
        /// Non-blocking call to close this non-application window. 
        /// When called on a split pane, it will close the pane (thereby removing a 
        /// split), it may or may not also close all other panes related to the 
        /// document/content/etc. This behavior is application dependent.
        /// </summary>
        public void Close()
        {
            UiaCoreApi.WindowPattern_Close(_hPattern);
        }

        /// <summary>
        /// Causes the calling code to block, waiting the specified number of milliseconds, for the 
        /// associated window to enter an idle state.
        /// </summary>
        /// <remarks>
        /// The implementation is dependent on the underlying application framework therefore this
        /// call may return sometime after the window is ready for user input.  The calling code
        /// should not rely on this call to understand exactly when the window has become idle. 
        /// 
        /// For now this method works reliably for both WinFx and Win32 Windows that are starting
        /// up.  However, if called at other times on WinFx Windows (e.g. during a long layout) 
        /// WaitForInputIdle may return true before the Window is actually idle.  Additional work
        /// needs to be done to detect when WinFx Windows are idle.
        /// </remarks>
        /// <param name="milliseconds">The amount of time, in milliseconds, to wait for the 
        /// associated process to become idle. The maximum is the largest possible value of a 
        /// 32-bit integer, which represents infinity to the operating system
        /// </param>
        /// <returns>
        /// returns true if the window has reached the idle state and false if the timeout occurred.
        /// </returns>
        public bool WaitForInputIdle( int milliseconds )
        {
            return UiaCoreApi.WindowPattern_WaitForInputIdle(_hPattern, milliseconds);
        }

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        /// <summary>
        /// This member allows access to previously requested
        /// cached properties for this element. The returned object
        /// has accessors for each property defined for this pattern.
        /// </summary>
        /// <remarks>
        /// Cached property values must have been previously requested
        /// using a CacheRequest. If you try to access a cached
        /// property that was not previously requested, an InvalidOperation
        /// Exception will be thrown.
        /// 
        /// To get the value of a property at the current point in time,
        /// access the property via the Current accessor instead of
        /// Cached.
        /// </remarks>
        public WindowPatternInformation Cached
        {
            get
            {
                Misc.ValidateCached(_cached);
                return new WindowPatternInformation(_el, true);
            }
        }

        /// <summary>
        /// This member allows access to current property values
        /// for this element. The returned object has accessors for
        /// each property defined for this pattern.
        /// </summary>
        /// <remarks>
        /// This pattern must be from an AutomationElement with a
        /// Full reference in order to get current values. If the
        /// AutomationElement was obtained using AutomationElementMode.None,
        /// then it contains only cached data, and attempting to get
        /// the current value of any property will throw an InvalidOperationException.
        /// 
        /// To get the cached value of a property that was previously
        /// specified using a CacheRequest, access the property via the
        /// Cached accessor instead of Current.
        /// </remarks>
        public WindowPatternInformation Current
        {
            get
            {
                Misc.ValidateCurrent(_hPattern);
                return new WindowPatternInformation(_el, false);
            }
        }

        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static object Wrap(AutomationElement el, SafePatternHandle hPattern, bool cached)
        {
            return new WindowPattern(el, hPattern, cached);
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private SafePatternHandle _hPattern;
        private bool _cached;

        #endregion Private Fields


        //------------------------------------------------------
        //
        //  Nested Classes
        //
        //------------------------------------------------------

        #region Nested Classes

        /// <summary>
        /// This class provides access to either Cached or Current
        /// properties on a pattern via the pattern's .Cached or
        /// .Current accessors.
        /// </summary>
        public struct WindowPatternInformation
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal WindowPatternInformation(AutomationElement el, bool useCache)
            {
                _el = el;
                _useCache = useCache;
            }

            #endregion Constructors


            //------------------------------------------------------
            //
            //  Public Properties
            //
            //------------------------------------------------------
 
            #region Public Properties

            /// <summary>Is this window Maximizable</summary>
            public bool CanMaximize
            {
                get
                {
                    return (bool)_el.GetPatternPropertyValue(CanMaximizeProperty, _useCache);
                }
            }

            /// <summary>Is this window Minimizable</summary>
            public bool CanMinimize
            {
                get
                {
                    return (bool)_el.GetPatternPropertyValue(CanMinimizeProperty, _useCache);
                }
            }

            /// <summary>Is this is a modal window.</summary>
            public bool IsModal
            {
                get
                {
                    return (bool)_el.GetPatternPropertyValue(IsModalProperty, _useCache);
                }
            }

            /// <summary>Is the Window Maximized, Minimized, or Normal (aka restored)</summary>
            public WindowVisualState WindowVisualState
            {
                get
                {
                    return (WindowVisualState)_el.GetPatternPropertyValue(WindowVisualStateProperty, _useCache);
                }
            }

            /// <summary>Is the Window Closing, ReadyForUserInteraction, BlockedByModalWindow or NotResponding.</summary>
            public WindowInteractionState WindowInteractionState
            {
                get
                {
                    return (WindowInteractionState)_el.GetPatternPropertyValue(WindowInteractionStateProperty, _useCache);
                }
            }

            /// <summary>Is this window is always on top</summary>
            public bool IsTopmost
            {
                get
                {
                    return (bool)_el.GetPatternPropertyValue(IsTopmostProperty, _useCache);
                }
            }


            #endregion Public Properties

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            private AutomationElement _el; // AutomationElement that contains the cache or live reference
            private bool _useCache; // true to use cache, false to use live reference to get current values

            #endregion Private Fields
        }
        #endregion Nested Classes
    }
}
