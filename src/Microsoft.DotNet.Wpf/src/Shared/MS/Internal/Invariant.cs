// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Provides methods that assert an application is in a valid state.
//

#if WINDOWS_BASE
using MS.Internal.WindowsBase;
#elif DRT
using MS.Internal.Drt;
#else
#error There is an attempt to use this class from an unexpected assembly.
#endif
namespace MS.Internal
{
    using System;
    using System.Security; 
    using Microsoft.Win32;
    using System.Diagnostics;
    using System.Windows;
    
    /// <summary>
    /// Provides methods that assert an application is in a valid state. 
    /// </summary>
    [FriendAccessAllowed] // Built into Base, used by Framework.
    internal // DO NOT MAKE PUBLIC - See security notes on Assert
        static class Invariant
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Static ctor.  Initializes the Strict property.
        /// </summary>
        static Invariant()
        {
            _strict = _strictDefaultValue;

#if PRERELEASE
            //
            // Let the user override the inital value of the Strict property from the registry.
            //
            RegistryKey key = Registry.LocalMachine.OpenSubKey(RegistryKeys.WPF);

            if (key != null)
            {
                object obj = key.GetValue("InvariantStrict");

                if (obj is int)
                {
                    _strict = (int)obj != 0;
                }
            }
#endif // PRERELEASE
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Checks for a condition and shuts down the application if false.
        /// </summary>
        /// <param name="condition">
        /// If condition is true, does nothing.
        ///
        /// If condition is false, raises an assert dialog then shuts down the
        /// process unconditionally.
        /// </param>
        internal static void Assert(bool condition)
        {
            if (!condition)
            {
                FailFast(null, null);
            }
        }

        /// <summary>
        /// Checks for a condition and shuts down the application if false.
        /// </summary>
        /// <param name="condition">
        /// If condition is true, does nothing.
        ///
        /// If condition is false, raises an assert dialog then shuts down the
        /// process unconditionally.
        /// </param>
        /// <param name="invariantMessage">
        /// Message to display before shutting down the application.
        /// </param>
        internal static void Assert(bool condition, string invariantMessage)
        {
            if (!condition)
            {
                FailFast(invariantMessage, null);
            }
        }

        /// <summary>
        /// Checks for a condition and shuts down the application if false.
        /// </summary>
        /// <param name="condition">
        /// If condition is true, does nothing.
        ///
        /// If condition is false, raises an assert dialog then shuts down the
        /// process unconditionally.
        /// </param>
        /// <param name="invariantMessage">
        /// Message to display before shutting down the application.
        /// </param>
        /// <param name="detailMessage">
        /// Additional message to display before shutting down the application.
        /// </param>
        internal static void Assert(bool condition, string invariantMessage, string detailMessage)
        {
            if (!condition)
            {
                FailFast(invariantMessage, detailMessage);
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Property specifying whether or not the user wants to enable expensive
        /// verification diagnostics.  The Strict property is rarely used -- only
        /// when performance profiling shows a real problem.
        ///
        /// Default value is false on FRE builds, true on CHK builds.
        ///
        /// On any build flavor the user may override this by setting
        /// [HKLM\Software\Microsoft\Avalon] InvariantStrict in the registry.
        /// (0 to disable strict asserts, 1 to enable them.)
        ///
        /// Example:
        ///
        ///  // Cheap assert always runs...
        ///  Invariant.Assert(_array.Length > 0, "_array should never be zero length!");
        ///  // Expensive assert only runs when full diagnostics are enabled.
        ///  if (Invariant.Strict)
        ///  {
        ///      for (int i=0; i != _array.Length; i++)
        ///      {
        ///          Invariant.Assert(_array[i] != 0, "_array contains zero value!");
        ///      }
        ///  }
        /// </summary>
        internal static bool Strict
        {
            get { return _strict; }

            set { _strict = value; }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        ///     Shuts down the process immediately, with no chance for additional
        ///     code to run.
        /// 
        ///     In debug we raise a Debug.Assert dialog before shutting down.
        /// </summary>
        /// <param name="message">
        ///     Message to display before shutting down the application.
        /// </param>
        /// <param name="detailMessage">
        ///     Additional message to display before shutting down the application.
        /// </param>
        private // DO NOT MAKE PUBLIC OR INTERNAL -- See security note
            static void FailFast(string message, string detailMessage)
        {
            if (Invariant.IsDialogOverrideEnabled)
            {
                // This is the override for stress and other automation.
                // Automated systems can't handle a popup-dialog, so let
                // them jump straight into the debugger.
                Debugger.Break();
            }

            Debug.Assert(false, "Invariant failure: " + message, detailMessage);

            Environment.FailFast(SR.InvariantFailure);
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        // Returns true if the default assert failure dialog has been disabled
        // on this machine.
        //
        // The dialog may be disabled by
        //   Installing a JIT debugger to the [HKEY_LOCAL_MACHINE\Software\Microsoft\.NETFramework]
        //     DbgJITDebugLaunchSetting and DbgManagedDebugger registry keys.
        private static bool IsDialogOverrideEnabled
        {
            get
            {
                RegistryKey key;
                bool enabled;

                enabled = false;

                //extracting all the data under an elevation.
                key = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\.NETFramework");
                //
                // Check for the enable.
                //
                if (key != null)
                {
                    object dbgJITDebugLaunchSettingValue = key.GetValue("DbgJITDebugLaunchSetting");
                    string dbgManagedDebuggerValue = key.GetValue("DbgManagedDebugger") as string;

                    //
                    // Only count the enable if there's a JIT debugger to launch.
                    //
                    enabled = (dbgJITDebugLaunchSettingValue is int && ((int)dbgJITDebugLaunchSettingValue & 2) != 0);
                    if (enabled)
                    {
                        enabled = dbgManagedDebuggerValue != null && dbgManagedDebuggerValue.Length > 0;
                    }
                }
                return enabled;
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Property specifying whether or not the user wants to enable expensive
        // verification diagnostics.
        private static bool _strict;

        // Used to initialize the default value of _strict in the static ctor.
        private const bool _strictDefaultValue
#if DEBUG
            = true;     // Enable strict asserts by default on CHK builds.
#else
            = false;
#endif

        #endregion Private Fields
    }
}

