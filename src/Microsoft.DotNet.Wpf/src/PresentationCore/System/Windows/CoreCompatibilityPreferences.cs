// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Specialized;   // NameValueCollection
using System.Configuration;             // ConfigurationManager
using System.Runtime.Versioning;
using MS.Internal.PresentationCore;

namespace System.Windows
{
    public static class CoreCompatibilityPreferences
    {
        #region Constructor

        static CoreCompatibilityPreferences()
        {
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
                SetIncludeAllInkInBoundingBoxFromAppSettings(appSettings);
                SetEnableMultiMonitorDisplayClippingFromAppSettings(appSettings);
            }
        }

        #endregion Constructor

        #region CLR compat flags

        internal static bool TargetsAtLeast_Desktop_V4_5
        {
            get
            {
#if NETFX && !NETCOREAPP
                return BinaryCompatibility.TargetsAtLeast_Desktop_V4_5;
#elif NETCOREAPP
                return true;
#else
                return true;
#endif
            }
        }

        #endregion CLR compat flags

        #region IsAltKeyRequiredInAccessKeyDefaultScope

        // We decided NOT to opt-in this feature by default.
        private static bool _isAltKeyRequiredInAccessKeyDefaultScope = false;

        public static bool IsAltKeyRequiredInAccessKeyDefaultScope
        {
            get { return _isAltKeyRequiredInAccessKeyDefaultScope; }
            set
            {
                lock (_lockObject)
                {
                    if (_isSealed)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.CompatibilityPreferencesSealed, "IsAltKeyRequiredInAccessKeyDefaultScope", "CoreCompatibilityPreferences"));
                    }

                    _isAltKeyRequiredInAccessKeyDefaultScope = value;
                }
            }
        }

        internal static bool GetIsAltKeyRequiredInAccessKeyDefaultScope()
        {
            Seal();

            return IsAltKeyRequiredInAccessKeyDefaultScope;
        }

        #endregion IsAltKeyRequiredInAccessKeyDefaultScope

        #region IncludeAllInkInBoundingBox

        // GlyphRun.ComputeInkBoundingBox is supposed to return a box that contains
        // all the ink in the GlyphRun, but in some circumstances it computes a
        // box that is too small.  This was "fixed" in 4.5 by inflating the box
        // slightly.  Apps that depend on the old non-inflated result can opt out
        // of the fix by adding an entry to the <appSettings> section of the
        // app config file:
        //          <add key="IncludeAllInkInBoundingBox" value="false"/>
        // By doing so, the app loses the fix - certain strings may not render
        // or hit-test correctly.  (See GlyphRun.ComputeInkBoundingBox for more.)

        private static bool _includeAllInkInBoundingBox = true;

        internal static bool IncludeAllInkInBoundingBox
        {
            get { return _includeAllInkInBoundingBox; }
            set
            {
                lock (_lockObject)
                {
                    if (_isSealed)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.CompatibilityPreferencesSealed, "IncludeAllInkInBoundingBox", "CoreCompatibilityPreferences"));
                    }

                    _includeAllInkInBoundingBox = value;
                }
            }
        }

        internal static bool GetIncludeAllInkInBoundingBox()
        {
            Seal();

            return IncludeAllInkInBoundingBox;
        }

        static void SetIncludeAllInkInBoundingBoxFromAppSettings(NameValueCollection appSettings)
        {
            // user can use config file to opt out of GlyphRun.ComputeInkBoundingBox fixes
            string s = appSettings["IncludeAllInkInBoundingBox"];
            bool value;
            if (Boolean.TryParse(s, out value))
            {
                IncludeAllInkInBoundingBox = value;
            }
        }

        #endregion IncludeAllInkInBoundingBox

        #region EnableMultimonitorDisplayClipping

        private static bool? _enableMultiMonitorDisplayClipping = null;

        public static bool? EnableMultiMonitorDisplayClipping
        {
            get { return GetEnableMultiMonitorDisplayClipping(); }
            set
            {
                lock(_lockObject)
                {
                    if(_isSealed)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.CompatibilityPreferencesSealed, "DisableMultimonDisplayClipping", "CoreCompatibilityPreferences"));
                    }

                    _enableMultiMonitorDisplayClipping = value; 
                }
            }
        }

        internal static bool? GetEnableMultiMonitorDisplayClipping()
        {
            Seal();

            return _enableMultiMonitorDisplayClipping;
        }

        static void SetEnableMultiMonitorDisplayClippingFromAppSettings(NameValueCollection appSettings)
        {
            string s = appSettings["EnableMultiMonitorDisplayClipping"];
            bool value; 
            if (Boolean.TryParse(s, out value))
            {
                EnableMultiMonitorDisplayClipping = value; 
            }
        }

        #endregion

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
