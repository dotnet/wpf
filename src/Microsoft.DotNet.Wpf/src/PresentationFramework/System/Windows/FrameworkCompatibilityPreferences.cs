// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Specialized;   // NameValueCollection
using System.Configuration;             // ConfigurationManager
using System.Runtime.Versioning;
using MS.Internal;

namespace System.Windows
{
    public static class FrameworkCompatibilityPreferences
    {
        #region Constructor

        static FrameworkCompatibilityPreferences()
        {
#if NETFX && !NETCOREAPP
            _targetsDesktop_V4_0 = BinaryCompatibility.AppWasBuiltForFramework == TargetFrameworkId.NetFramework
                && !BinaryCompatibility.TargetsAtLeast_Desktop_V4_5;
#elif NETCOREAPP
            // When building for NETCOREAPP, set this to false
            // to indicate that quirks should be treated as if they are running on 
            // .NET 4.5+
            _targetsDesktop_V4_0 = false;
#else
            _targetsDesktop_V4_0 = false;
#endif

            // user can use config file to set preferences
            NameValueCollection appSettings = null;
            try
            {
                appSettings = ConfigurationManager.AppSettings;
            }
            catch (ConfigurationErrorsException)
            {
            }

            if (appSettings != null)
            {
                SetUseSetWindowPosForTopmostWindowsFromAppSettings(appSettings);
                SetVSP45CompatFromAppSettings(appSettings);
                SetScrollingTraceFromAppSettings(appSettings);
                SetShouldThrowOnCopyOrCutFailuresFromAppSettings(appSettings);
            }
        }

        #endregion Constructor

        #region TargetsDesktop_V4_0

        // CLR's BinaryCompatibility class doesn't expose a convenient way to determine
        // if the app targets 4.0 exactly.  We use that a lot, so encapsulate it here
        static bool _targetsDesktop_V4_0;

        internal static bool TargetsDesktop_V4_0
        {
            get { return _targetsDesktop_V4_0; }
        }

        #endregion TargetsDesktop_V4_0

        #region AreInactiveSelectionHighlightBrushKeysSupported

#if NETFX && !NETCOREAPP
        private static bool _areInactiveSelectionHighlightBrushKeysSupported = BinaryCompatibility.TargetsAtLeast_Desktop_V4_5 ? true : false;
#elif NETCOREAPP
        private static bool _areInactiveSelectionHighlightBrushKeysSupported = true;
#else
        private static bool _areInactiveSelectionHighlightBrushKeysSupported = true;
#endif

        public static bool AreInactiveSelectionHighlightBrushKeysSupported
        {
            get { return _areInactiveSelectionHighlightBrushKeysSupported; }
            set
            {
                lock (_lockObject)
                {
                    if (_isSealed)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.CompatibilityPreferencesSealed, "AreInactiveSelectionHighlightBrushKeysSupported", "FrameworkCompatibilityPreferences"));
                    }

                    _areInactiveSelectionHighlightBrushKeysSupported = value;
                }
            }
        }

        internal static bool GetAreInactiveSelectionHighlightBrushKeysSupported()
        {
            Seal();

            return AreInactiveSelectionHighlightBrushKeysSupported;
        }

        #endregion AreInactiveSelectionHighlightBrushKeysSupported

        #region KeepTextBoxDisplaySynchronizedWithTextProperty

#if NETFX && !NETCOREAPP
        private static bool _keepTextBoxDisplaySynchronizedWithTextProperty = BinaryCompatibility.TargetsAtLeast_Desktop_V4_5 ? true : false;
#elif NETCOREAPP
        private static bool _keepTextBoxDisplaySynchronizedWithTextProperty = true;
#else
        private static bool _keepTextBoxDisplaySynchronizedWithTextProperty = true;
#endif

        /// <summary>
        /// In WPF 4.0, a TextBox can reach a state where its Text property
        /// has some value X, but a different value Y is displayed.   Setting
        /// FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty=true
        /// ensures that the displayed value always agrees with the value of the Text property.
        ///
        /// </summary>
        /// <notes>
        /// The inconsistent state can be reached as follows:
        /// 1. The TextBox is data-bound with property-changed update trigger
        ///         <TextBox Text="{Binding UpdateSourceTrigger=PropertyChanged, Path=ABC}"/>
        /// 2. The TextBox displays a value X
        /// 3. The user types a character to produce a new value Y
        /// 4. The new value Y is sent to the data item's property ABC then read
        ///     back again, possibly applying conversions in each direction.   The
        ///     data item may "normalize" the value as well - upon receiving value V it
        ///     may store a different value V'.  Denote by Z the result of this round-trip.
        /// 5. The Text property is set to Z.
        /// 6. Usually the text box will now display Z.   But if Z and X are the same,
        ///     it will display Y (which is different from X).
        ///
        /// For example, suppose the data item normalizes by trimming spaces:
        ///     public string ABC { set { _abc = value.Trim(); } }
        /// And suppose the user types "hi ".  Upon typing the space, the binding
        /// sends "hi " to the data item, which stores "hi".  The result of the round-trip
        /// is "hi", which is identical to the string before typing the space.  In
        /// this case, the TextBox reaches a state where its Text property has value
        /// "hi" although it displays "hi ".
        ///
        /// As a second example, suppose the data item normalizes an integer by
        /// capping its value to a maximum, say 100:
        ///     public int Score { set { _score = Math.Min(value, 100); } }
        /// And suppose the user types "1004".  Upon typing the 4, the binding converts
        /// string "1004" to int 1004 and sends 1004 to the data item, which stores 100.
        /// The round-trip continues, converting int 100 to string "100", which is
        /// identical to the text before typing the 4.   The TextBox reaches a state
        /// where its Text property has value "100", but it displays "1004".
        /// </notes>
        public static bool KeepTextBoxDisplaySynchronizedWithTextProperty
        {
            get { return _keepTextBoxDisplaySynchronizedWithTextProperty; }
            set
            {
                lock (_lockObject)
                {
                    if (_isSealed)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.CompatibilityPreferencesSealed, "AextBoxDisplaysText", "FrameworkCompatibilityPreferences"));
                    }

                    _keepTextBoxDisplaySynchronizedWithTextProperty = value;
                }
            }
        }

        internal static bool GetKeepTextBoxDisplaySynchronizedWithTextProperty()
        {
            Seal();

            return KeepTextBoxDisplaySynchronizedWithTextProperty;
        }

        #endregion KeepTextBoxDisplaySynchronizedWithTextProperty

        // There is a bug in the Windows desktop window manager which can cause
        // incorrect z-order for windows when several conditions are all met:
        // (a) windows are parented/owned across different threads or processes
        // (b) a parent/owner window is also owner of a topmost window (which needn't be visible)
        // (c) the child window on a different thread/process tries to show an owned topmost window
        //     (like a popup or tooltip) using ShowWindow().
        // To avoid this window manager bug, this option causes SetWindowPos() to be used instead of
        // ShowWindow() for topmost windows, avoiding condition (c).  Ideally the window manager bug
        // will be fixed, but the risk of making a change there is considered too great at this time.
        #region UseSetWindowPosForTopmostWindows

        private static bool _useSetWindowPosForTopmostWindows = false; // use old behavior by default

        internal static bool UseSetWindowPosForTopmostWindows
        {
            get { return _useSetWindowPosForTopmostWindows; }
            set
            {
                lock (_lockObject)
                {
                    if (_isSealed)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.CompatibilityPreferencesSealed, "UseSetWindowPosForTopmostWindows", "FrameworkCompatibilityPreferences"));
                    }

                    _useSetWindowPosForTopmostWindows = value;
                }
            }
        }

        internal static bool GetUseSetWindowPosForTopmostWindows()
        {
            Seal();

            return UseSetWindowPosForTopmostWindows;
        }

        static void SetUseSetWindowPosForTopmostWindowsFromAppSettings(NameValueCollection appSettings)
        {
            // user can use config file to enable this behavior change
            string s = appSettings["UseSetWindowPosForTopmostWindows"];
            bool useSetWindowPos;
            if (Boolean.TryParse(s, out useSetWindowPos))
            {
                UseSetWindowPosForTopmostWindows = useSetWindowPos;
            }
        }

        #endregion UseSetWindowPosForTopmostWindows

        #region VSP45Compat

        // VirtualizingStackPanel added support for virtualization-when-grouping in 4.5,
        // generalizing and subsuming the support for virtualizing a TreeView that existed in 4.0.
        // The 4.5 algorithm had many flaws, leading to infinite loops, scrolling
        // to the wrong place, and other bad symptoms.  DDCC is worried that fixing
        // these issues may introduce new compat problems, and asked for a way to opt out
        // of the fixes.  To opt out, add an entry to the <appSettings> section of the
        // app config file:
        //          <add key="IsVirtualizingStackPanel_45Compatible" value="true"/>

        private static bool _vsp45Compat = false;

        internal static bool VSP45Compat
        {
            get { return _vsp45Compat; }
            set
            {
                lock (_lockObject)
                {
                    if (_isSealed)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.CompatibilityPreferencesSealed, "IsVirtualizingStackPanel_45Compatible", "FrameworkCompatibilityPreferences"));
                    }

                    _vsp45Compat = value;
                }
            }
        }

        internal static bool GetVSP45Compat()
        {
            Seal();

            return VSP45Compat;
        }

        static void SetVSP45CompatFromAppSettings(NameValueCollection appSettings)
        {
            // user can use config file to opt out of VSP fixes
            string s = appSettings["IsVirtualizingStackPanel_45Compatible"];
            bool value;
            if (Boolean.TryParse(s, out value))
            {
                VSP45Compat = value;
            }
        }

        #endregion VSP45Compat

        #region ScrollingTrace

        private static string _scrollingTraceTarget;

        internal static string GetScrollingTraceTarget()
        {
            Seal();
            return _scrollingTraceTarget;
        }

        private static string _scrollingTraceFile;

        internal static string GetScrollingTraceFile()
        {
            Seal();
            return _scrollingTraceFile;
        }

        static void SetScrollingTraceFromAppSettings(NameValueCollection appSettings)
        {
            // user can use config file to select a control (TreeView, DataGrid, etc.)
            // for in-flight tracing of scrolling behavior:
            //      <add key="ScrollingTraceTarget" value="NameOfControl"/>
            _scrollingTraceTarget = appSettings["ScrollingTraceTarget"];

            // user can direct scroll-tracing output to a file:
            //      <add key="ScrollingTraceFile" value="NameOfFile"/>
            // If the key is not present, or the filename is absent or "default",
            // the output goes to "ScrollTrace.stf".  If the filename is "none",
            // no file output is produced.
            //
            // User can also specify a parameter to control when output is flushed
            // to the file:
            //      <add key="ScrollingTraceFile" value="NameOfFile;nnn"/>
            // If not specified, the output is flushed after completing Measure or
            // Arrange of the top-level VirtualizingStackPanel below the trace
            // target.   In some scenarios it may be desirable to flush the output
            // more often - for example, an infinite loop that never measures the
            // top-level panel.   Use the optional nnn parameter to flush after
            // Measure or Arrange of any panel whose depth is nnn or less.  This flushes
            // more often, but is more likely to interfere with the timing of the app.
            _scrollingTraceFile = appSettings["ScrollingTraceFile"];

            // Alternatively, the user can control tracing from the VS debugger.
            // To enable tracing:
            //      1. Locate the desired control (TreeView, DataGrid, etc.) and
            //          make an Object ID for it.
            //      2. From the Immediate window, execute
            //          VirtualizingStackPanel.ScrollTracer.SetTarget(1#)
            //          (using the appropriate ID instead of 1#)
            // To control the file output
            //      1. From the Immediate window, execute
            //          VirtualizaingStackPanel.ScrollTracer.SetFileAndDepth("filename", n)
            //          to specify the file and the desired flushing depth.
            // To flush the current trace data to the file (useful if the app is
            // about to terminate - including force-termination from the debugger
            // or TaskManager - but you want to capture the latest trace data):
            //      1. From the Immediate window, execute
            //          VirtualizaingStackPanel.ScrollTracer.Flush()
        }

        #endregion ScrollingTrace

        #region ShouldThrowOnCopyOrCutFailure

        private static bool _shouldThrowOnCopyOrCutFailure = false;

        /// <summary>
        /// When True, a failed Copy or Cut operation in a TextBoxBase instance will result in 
        /// a <see cref="System.Runtime.InteropServices.ExternalException"/>. 
        /// When False (default), a failed Copy or Cut operation will be silently ignored. 
        /// </summary>
        /// <remarks>
        /// When a clipboard operation fails,for e.g., with HRESULT 0x800401D0 (CLIPBRD_E_CANT_OPEN), 
        /// a corresponding <see cref="System.Runtime.InteropServices.COMException"/> (which is a type of 
        /// ExternalException) is thrown. 
        /// 
        /// The Win32 OpenClipboard API acts globally, and the corresponding 
        /// CloseClipboard call should be made by well written applications as soon as they have
        /// completed their clipboard operations. When an application calls OpenClipboard and then fails 
        /// to call CloseClipboard, it results in all other applications running the same session 
        /// being unable to access clipboard functions. 
        /// 
        /// In WPF, such a denial of access to clipboard is 
        /// normally ignored silently. Applications can opt into receiving an ExternalException upon
        /// failure by setting this flag. Opting to receive exceptions requires that 
        /// the application would take control of handling <see cref="System.Windows.Input.ApplicationCommands.Cut"/>
        /// and <see cref="System.Windows.Input.ApplicationCommands.Copy"/> RoutedUICommands through a 
        /// <see cref="System.Windows.Input.CommandBinding"/>, and apply that binding to all TextBoxBase
        /// controls (<see cref="System.Windows.Controls.TextBox"/> and <see cref="System.Windows.Controls.RichTextBox"/>) 
        /// in the application. The application should ensure that it handles ExternalExeptions arising from Copy/Cut 
        /// operations in the CommandBinding's Executed handler. 
        /// </remarks>
        public static bool ShouldThrowOnCopyOrCutFailure
        {
            get
            {
                return _shouldThrowOnCopyOrCutFailure;
            }

            set
            {
                if (_isSealed)
                {
                    throw new InvalidOperationException(
                        SR.Get(SRID.CompatibilityPreferencesSealed, 
                        nameof(ShouldThrowOnCopyOrCutFailure), 
                        nameof(FrameworkCompatibilityPreferences)));
                }

                _shouldThrowOnCopyOrCutFailure = value;
            }
        }

        internal static bool GetShouldThrowOnCopyOrCutFailure()
        {
            Seal();
            return ShouldThrowOnCopyOrCutFailure;
        }

        static void SetShouldThrowOnCopyOrCutFailuresFromAppSettings(NameValueCollection appSettings)
        {
            // user can use config file to enable this behavior change
            string s = appSettings[nameof(ShouldThrowOnCopyOrCutFailure)];

            bool shouldThrowOnCopyOrCutFailure;
            if (Boolean.TryParse(s, out shouldThrowOnCopyOrCutFailure))
            {
                ShouldThrowOnCopyOrCutFailure = shouldThrowOnCopyOrCutFailure;
            }
        }

        #endregion ShouldThrowOnCopyOrCutFailure

        private static void Seal()
        {
            if (!_isSealed)
            {
                lock (_lockObject)
                {
                    _isSealed = true;
                }
            }
        }

        private static bool _isSealed;
        private static object _lockObject = new object();
    }
}
