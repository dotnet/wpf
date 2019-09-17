// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      Visual tree diagnostic API.
//

using Microsoft.Win32;              // Registry, RegistryKey
using MS.Internal;                  // CoreAppContextSwitches
using System.Security;
using System.Windows.Interop;       // HwndSource
using System.Windows.Media;
using System.Windows.Media.Media3D;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Diagnostics
{
    public static class VisualDiagnostics
    {
#pragma warning disable 649
        // Warning CS0649: The Field 'VisualDiagnostics.s_isDebuggerCheckDisabledForTestPurposes' is never
        // assigned to, and will always have its default value false
        //
        // This field exists for test purposes
        private static bool s_isDebuggerCheckDisabledForTestPurposes;
#pragma warning restore 649

        private static readonly bool s_IsEnabled;
        private static event EventHandler<VisualTreeChangeEventArgs> s_visualTreeChanged;
        private static bool s_HasVisualTreeChangedListeners;

        [ThreadStatic]
        private static bool s_IsVisualTreeChangedInProgress;

        [ThreadStatic]
        private static HwndSource s_ActiveHwndSource;

        static VisualDiagnostics()
        {
            s_IsEnabled = !CoreAppContextSwitches.DisableDiagnostics;
        }

        #region Public

        /// <summary>
        /// Visual tree change notification. Fires for any visual change regardless whether it is
        /// connected to any root visual, e.g. Window. Subscribers are responsible for performing
        /// necessary filtering.
        /// </summary>
        /// <remarks>
        /// This event is for diagnostic use only.  Handlers of this event should
        /// limit themselves to read-only access to elements, properties, and resources.
        ///
        /// Microsoft does not support the use of this event in a production application
        /// under any circumstance.
        /// </remarks>
        public static event EventHandler<VisualTreeChangeEventArgs> VisualTreeChanged
        {
            add
            {
                if (EnableHelper.IsVisualTreeChangeEnabled)
                {
                    s_visualTreeChanged += value;
                    s_HasVisualTreeChangedListeners = true;
                }
            }
            remove
            {
                s_visualTreeChanged -= value;
            }
        }

        /// <summary>
        /// Enable the VisualTreeChanged event.
        /// </summary>
        /// <remarks>
        /// This method is for diagnostic use only.
        /// Microsoft does not support the use of this method in a production application
        /// under any circumstance.
        /// </remarks>
        public static void EnableVisualTreeChanged()
        {
            EnableHelper.EnableVisualTreeChanged();
        }

        /// <summary>
        /// Disable the VisualTreeChanged event.
        /// </summary>
        public static void DisableVisualTreeChanged()
        {
            EnableHelper.DisableVisualTreeChanged();
        }

        /// <summary>
        /// Provides object source info which will be available for objects created from BAML or XAML.
        /// Source info will not be available if diagnostics have not been enabled at the time of
        /// loading BAML or XAML.
        /// </summary>
        public static XamlSourceInfo GetXamlSourceInfo(object obj)
        {
            return XamlSourceInfoHelper.GetXamlSourceInfo(obj);
        }

        #endregion Public

        internal static void OnVisualChildChanged(DependencyObject parent, DependencyObject child, bool isAdded)
        {
            EventHandler<VisualTreeChangeEventArgs> visualTreeChanged = VisualDiagnostics.s_visualTreeChanged;
            if (visualTreeChanged != null && EnableHelper.IsVisualTreeChangeEnabled)
            {
                int index;
                VisualTreeChangeType changeType;
                if (isAdded)
                {
                    index = VisualDiagnostics.GetChildIndex(parent, child);
                    changeType = VisualTreeChangeType.Add;
                }
                else
                {
                    // We cannot reliably get correct child index for a removed child. We'll force it to be -1;
                    index = -1;
                    changeType = VisualTreeChangeType.Remove;
                }

                RaiseVisualTreeChangedEvent(
                    visualTreeChanged,
                    new VisualTreeChangeEventArgs(parent, child, index, changeType),
                    // see EnableHelper.IsChangePermitted
                    isPotentialOuterChange: (changeType==VisualTreeChangeType.Add && index==0 && VisualTreeHelper.GetParent(parent) == null));
            }
        }

        private static void RaiseVisualTreeChangedEvent(
                                EventHandler<VisualTreeChangeEventArgs> visualTreeChanged,
                                VisualTreeChangeEventArgs args,
                                bool isPotentialOuterChange)
        {
            bool savedIsVisualTreeChangedInProgress = s_IsVisualTreeChangedInProgress;
            HwndSource savedActiveHwndSource = s_ActiveHwndSource;

            try
            {
                s_IsVisualTreeChangedInProgress = true;

                if (isPotentialOuterChange)
                {
                    s_ActiveHwndSource = PresentationSource.FromDependencyObject(args.Parent) as System.Windows.Interop.HwndSource;
                }

                visualTreeChanged(null, args);
            }
            finally
            {
                s_IsVisualTreeChangedInProgress = savedIsVisualTreeChangedInProgress;
                s_ActiveHwndSource = savedActiveHwndSource;
            }
        }

        private static int GetChildIndex(DependencyObject parent, DependencyObject child)
        {
            int index = -1;
            Visual asVisual = child as Visual;
            if (asVisual != null)
            {
                index = asVisual._parentIndex;
            }
            else
            {
                Visual3D asVisual3D = child as Visual3D;
                if (asVisual3D != null)
                {
                    index = asVisual3D.ParentIndex;
                }
            }

            // Sometimes index is not up to date. We'll have to find it manually.
            if (index < 0)
            {
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    DependencyObject obj = VisualTreeHelper.GetChild(parent, i);
                    if (obj == child)
                    {
                        index = i;
                        break;
                    }
                }
            }

            return index;
        }

        internal static bool IsEnabled
        {
            get { return s_IsEnabled;}
        }

        // detect whether a VisualTreeChanged event is in progress.  If so,
        // throw an exception unless overridden by the app-context flag.
        internal static void VerifyVisualTreeChange(DependencyObject d)
        {
            // write this so that the 90% case is inlined - check the flag and move on
            if (s_HasVisualTreeChangedListeners)
            {
                VerifyVisualTreeChangeCore(d);
            }
        }

        private static void VerifyVisualTreeChangeCore(DependencyObject d)
        {
            if (s_IsVisualTreeChangedInProgress)
            {
                if (!EnableHelper.AllowChangesDuringVisualTreeChanged(d))
                {
                    throw new InvalidOperationException(SR.Get(SRID.ReentrantVisualTreeChangeError, nameof(VisualTreeChanged)));
                }
            }
        }

        internal static bool IsEnvironmentVariableSet(string value, string environmentVariable)
        {
            if (value != null)
            {
                return IsEnvironmentValueSet(value);
            }

            value = Environment.GetEnvironmentVariable(environmentVariable);

            return IsEnvironmentValueSet(value);
        }

        internal static bool IsEnvironmentValueSet(string value)
        {
            value = (value ?? string.Empty).Trim().ToLowerInvariant();
            return !(value == string.Empty || value == "0" || value == "false");
        }

        // this class does all the work for checking whether VisualTreeChanged features are
        // enabled, disallowed, etc.  It's a separate class so that its static
        // cctor doesn't run until needed, giving apps time to initialize factors
        // that influence the decisions:  environment, registry, app-context switches, etc.
        private static class EnableHelper
        {
            static EnableHelper()
            {
                if (IsEnabled)
                {
                    s_IsDevMode = GetDevModeFromRegistry();
                    s_IsEnableVisualTreeChangedAllowed = PrecomputeIsEnableVisualTreeChangedAllowed();
                }
            }

            internal static void EnableVisualTreeChanged()
            {
                if (!IsEnableVisualTreeChangedAllowed)
                    throw new InvalidOperationException(SR.Get(SRID.MethodCallNotAllowed, nameof(VisualDiagnostics.EnableVisualTreeChanged)));

                s_IsVisualTreeChangedEnabled = true;
            }

            internal static void DisableVisualTreeChanged()
            {
                s_IsVisualTreeChangedEnabled = false;
            }

            internal static bool IsVisualTreeChangeEnabled
            {
                get
                {
                    return IsEnabled &&
                       (s_IsVisualTreeChangedEnabled ||
                        System.Diagnostics.Debugger.IsAttached ||
                        s_isDebuggerCheckDisabledForTestPurposes);
                }
            }

            internal static bool AllowChangesDuringVisualTreeChanged(DependencyObject d)
            {
                if (s_AllowChangesDuringVisualTreeChanged == null)
                {
                    if (IsChangePermitted(d))
                        return true;

                    s_AllowChangesDuringVisualTreeChanged = CoreAppContextSwitches.AllowChangesDuringVisualTreeChanged;

                    if (s_AllowChangesDuringVisualTreeChanged == true)
                    {
                        // user wants to allow re-entrant changes, and this is the first one.
                        // Issue a warning to debug output
                        System.Diagnostics.Debug.WriteLine(SR.Get(SRID.ReentrantVisualTreeChangeWarning, nameof(VisualTreeChanged)));
                    }

                    return (s_AllowChangesDuringVisualTreeChanged == true);
                }
                else
                {
                    return (s_AllowChangesDuringVisualTreeChanged == true) || IsChangePermitted(d);
                }
            }

            // For compat, allow nested changes when:
            // a. The outer change added a child to an element that is a visual
            //      root of a window.  (More precisely - the element has no parent,
            //      but does have an HwndSource.)
            // b. The inner change affects an element belonging to a different
            //      PresentationSource.
            //
            // Handlers for VisualTreeChanged should not cause side-effects - that
            // creates a situation where the app can behave differently during
            // diagnosis than it does in production.   But some already-shipped
            // diagnostic assistants cause side-effects in the situation above.
            // [VS 2015 and 2017 create a new window to hold the "little box"
            // containing the diagnostic icons/buttons.  They should have done this
            // asynchronously, outside the scope of VisualTreeChanged, but they
            // already shipped with this flaw.]
            //
            private static bool IsChangePermitted(DependencyObject d)
            {
                // if the outer change was type (a), OnVisualChildChanged saved
                // the presentation source in s_ActiveHwndSource
                return  (s_ActiveHwndSource != null) && (d != null) &&
                        (s_ActiveHwndSource != PresentationSource.FromDependencyObject(d));
            }

            /// <summary>
            ///     EnableVisualTreeChanged can be called only in certain scenarios.
            ///     Here we precompute the parts of the rule that can't change at runtime.
            /// </summary>
            private static bool? PrecomputeIsEnableVisualTreeChangedAllowed()
            {
                if (!IsEnabled)
                    return false;           // if diagnostics are disabled, not allowed
                if (IsDevMode)
                    return true;            // if DevMode is on, allowed
                if (IsEnvironmentVariableSet(null, c_enableVisualTreeNotificationsEnvironmentVariable))
                    return true;            // if environment variable is set, allowed
                return null;                // otherwise, need to check at runtime (for debugger attached, etc.)
            }

            /// <summary>
            ///     read the registry to see if Win10 Dev Mode is set
            /// </summary>
            private static bool GetDevModeFromRegistry()
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(c_devmodeRegKey);

                if (key != null)
                {
                    using (key)
                    {
                        object obj = key.GetValue(c_devmodeValueName);

                        if (obj is int)
                        {
                            return ((int)obj != 0);
                        }
                    }
                }

                return false;
            }

            private static bool IsDevMode
            {
                get { return s_IsDevMode; }
            }

            private static bool IsEnableVisualTreeChangedAllowed
            {
                get { return s_IsEnableVisualTreeChangedAllowed ?? System.Diagnostics.Debugger.IsAttached; }
            }

            private static readonly bool s_IsDevMode;

            private static readonly bool? s_IsEnableVisualTreeChangedAllowed;
            private static bool s_IsVisualTreeChangedEnabled;
            private static bool? s_AllowChangesDuringVisualTreeChanged;

            const string c_enableVisualTreeNotificationsEnvironmentVariable = "ENABLE_XAML_DIAGNOSTICS_VISUAL_TREE_NOTIFICATIONS";
            const string c_devmodeRegKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock";
            const string c_devmodeRegKeyFullPath = @"HKEY_LOCAL_MACHINE\" + c_devmodeRegKey;
            const string c_devmodeValueName = "AllowDevelopmentWithoutDevLicense";
        }
    }
}
