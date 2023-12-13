// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Collections.Generic;

namespace System
{
// error CS0436: When building PresentationFramework, the type 'LocalAppContext' 
// conflicts with the imported type 'LocalAppContext' in 'PresentationCore
#pragma warning disable 436
    internal partial class LocalAppContext
    {
        private static Dictionary<string, bool> s_switchMap = new Dictionary<string, bool>();
        private static readonly object s_syncLock = new object();

        private static bool DisableCaching { get; set; }

        static LocalAppContext()
        {
            // Populate the default values of the local app context 
            AppContextDefaultValues.PopulateDefaultValues();

            // Cache the value of the switch that help with testing
            DisableCaching = IsSwitchEnabled(@"TestSwitch.LocalAppContext.DisableCaching");
        }

        public static bool IsSwitchEnabled(string switchName)
        {
            if (System.AppContext.TryGetSwitch(switchName, out var isEnabledCentrally))
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
            lock (s_switchMap)
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
            if (switchValue < 0) return false;
            if (switchValue > 0) return true;

            return GetCachedSwitchValueInternal(switchName, ref switchValue);
        }

        private static bool GetCachedSwitchValueInternal(string switchName, ref int switchValue)
        {
            if (LocalAppContext.DisableCaching)
            {
                return LocalAppContext.IsSwitchEnabled(switchName);
            }

            bool isEnabled = LocalAppContext.IsSwitchEnabled(switchName);
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
#pragma warning restore 436
}
