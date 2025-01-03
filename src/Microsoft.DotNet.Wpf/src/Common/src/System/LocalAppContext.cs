// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;

namespace System
{
    internal static class LocalAppContext
    {
        /// <summary>
        /// Holds the switch names and their values. In case it is modified outside <see cref="DefineSwitchDefault"/>,
        /// proper thread synchronization is required as the switch state can be queried from any thread.
        /// </summary>
        private static readonly Dictionary<string, bool> s_switchMap = new();
#if !NETFX
        private static readonly Lock s_syncLock = new();
#else
        private static readonly object s_syncLock = new();
#endif

        private static bool DisableCaching { get; set; }

        static LocalAppContext()
        {
            // When building PresentationFramework, 'LocalAppContext' from WindowsBase.dll conflicts
            // with 'LocalAppContext' from PresentationCore.dll since there is InternalsVisibleTo set
#pragma warning disable CS0436 // Type conflicts with imported type

            // Populate the default values of the local app context 
            AppContextDefaultValues.PopulateDefaultValues();

#pragma warning restore CS0436 // Type conflicts with imported type

            // Cache the value of the switch that help with testing
            DisableCaching = IsSwitchEnabled(@"TestSwitch.LocalAppContext.DisableCaching");
        }

        public static bool IsSwitchEnabled(string switchName)
        {
            if (AppContext.TryGetSwitch(switchName, out bool isEnabledCentrally))
            {
                // we found the switch, so return whatever value it has
                return isEnabledCentrally;
            }

            // if we could not get the value from the central authority, try the local storage.
            return IsSwitchEnabledLocal(switchName);
        }

        private static bool IsSwitchEnabledLocal(string switchName)
        {
            // read the value from the set of local defaults
            bool isEnabled, isPresent;
            lock (s_syncLock)
            {
                isPresent = s_switchMap.TryGetValue(switchName, out isEnabled);
            }

            // If the value is in the set of local switches, return the value
            if (isPresent)
            {
                return isEnabled;
            }

            // if we could not find the switch name, we should return 'false'
            // This will preserve the concept of switches been 'off' unless explicitly set to 'on'
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool GetCachedSwitchValue(string switchName, ref int switchValue)
        {
            if (switchValue < 0)
                return false;
            if (switchValue > 0)
                return true;

            return GetCachedSwitchValueInternal(switchName, ref switchValue);
        }

        private static bool GetCachedSwitchValueInternal(string switchName, ref int switchValue)
        {
            if (DisableCaching)
            {
                return IsSwitchEnabled(switchName);
            }

            bool isEnabled = IsSwitchEnabled(switchName);
            switchValue = isEnabled ? 1 /*true*/ : -1 /*false*/;
            return isEnabled;
        }

        /// <summary>
        /// This method is going to be called from the AppContextDefaultValues class when setting up the 
        /// default values for the switches. !!!! This method is called during the static constructor so it does not
        /// take a lock !!!! If you are planning to use this outside of that, please ensure proper locking.
        /// </summary>
        internal static void DefineSwitchDefault(string switchName, bool initialValue)
        {
            s_switchMap[switchName] = initialValue;
        }
    }
}
