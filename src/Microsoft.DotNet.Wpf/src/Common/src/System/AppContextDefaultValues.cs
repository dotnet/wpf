// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// There are cases where we have multiple assemblies that are going to import this file and 
// if they are going to also have InternalsVisibleTo between them, there will be a compiler warning
// that the type is found both in the source and in a referenced assembly. The compiler will prefer 
// the version of the type defined in the source
//
// In order to disable the warning for this type we are disabling this warning for this entire file.
#pragma warning disable 436

using System;
using System.Collections.Generic;

namespace System
{
    internal static partial class AppContextDefaultValues
    {
        public static void PopulateDefaultValues()
        {
            string platformIdentifier, profile;
            int version;

            ParseTargetFrameworkName(out platformIdentifier, out profile, out version);

            // Call into each library to populate their default switches
            PopulateDefaultValuesPartial(platformIdentifier, profile, version);
        }

        /// <summary>
        /// We have this separate method for getting the parsed elements out of the TargetFrameworkName so we can
        /// more easily support this on other platforms.
        /// </summary>
        private static void ParseTargetFrameworkName(out string identifier, out string profile, out int version)
        {
            // AppDomain.CurrentDomain.SetupInformation is not available on .NET Core prior to 3.0
            // Use Reflection to obtain this value if available. 
            string targetFrameworkMoniker = GetTargetFrameworkMoniker();

            // This is our default
            // When TFM cannot be found, it probably means we are running 
            // on .NET Core 2.2 or lower, in which case we will default to .NET Core 3.0
            if (targetFrameworkMoniker == null)
            {
                targetFrameworkMoniker = ".NETCoreApp,Version=v3.0";
            }

            // If we don't have a TFM then we should default to the .NET Framework 4.8+ behavior where all quirks are turned on.
            // For .NET Core 3.0, the result would be something like this: 
            //      identifier = ".NETCore";
            //      version = 30000;
            //      profile = string.Empty;
            if (!TryParseFrameworkName(targetFrameworkMoniker, out identifier, out version, out profile))
            {
                // If we want to use the latest behavior it is enough to set the value of the switch to string.Empty.
                // When the get to the caller of this method (PopulateDefaultValuesPartial) we are going to use the 
                // identifier we just set to decide which switches to turn on. By having an empty string as the 
                // identifier we are simply saying -- don't turn on any switches, and we are going to get the latest
                // behavior for all the switches
                // 
                // In practices, this may not work reliably due to the lack of targetFrameworkMoniker information.
                identifier = string.Empty;
            }
        }

        /// <summary>
        ///  This is equivalent to calling <code>AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName</code>
        /// </summary>
        /// <remarks>
        /// <code>AppDomain.CurrentDomain.SetupInformation</code> is not available until .NET Core 3.0, but we 
        /// have a need to run this code in .NET Core 2.2, we attempt to obtain this information via Reflection.
        /// </remarks>
        /// <returns>TargetFrameworkMoniker on .NET Framework and .NET Core 3.0+; null on .NET Core 2.2 or older runtimes</returns>
        private static string GetTargetFrameworkMoniker()
        {
            try
            {
                var pSetupInformation = typeof(AppDomain).GetProperty("SetupInformation");
                object appDomainSetup = pSetupInformation?.GetValue(AppDomain.CurrentDomain);
                Type tAppDomainSetup = Type.GetType("System.AppDomainSetup");
                var pTargetFrameworkName = tAppDomainSetup?.GetProperty("TargetFrameworkName");

                return
                    appDomainSetup != null ?
                    pTargetFrameworkName?.GetValue(appDomainSetup) as string :
                    null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // This code was a constructor copied from the FrameworkName class, which is located in System.dll.
        // Parses strings in the following format: "<identifier>, Version=[v|V]<version>, Profile=<profile>"
        //  - The identifier and version is required, profile is optional
        //  - Only three components are allowed.
        //  - The version string must be in the System.Version format; an optional "v" or "V" prefix is allowed
        private static bool TryParseFrameworkName(String frameworkName, out String identifier, out int version, out String profile)
        {
            // For parsing a target Framework moniker, from the FrameworkName class
            const char c_componentSeparator = ',';
            const char c_keyValueSeparator = '=';
            const char c_versionValuePrefix = 'v';
            const String c_versionKey = "Version";
            const String c_profileKey = "Profile";

            identifier = profile = string.Empty;
            version = 0;

            if (frameworkName == null || frameworkName.Length == 0)
            {
                return false;
            }

            String[] components = frameworkName.Split(c_componentSeparator);
            version = 0;

            // Identifier and Version are required, Profile is optional.
            if (components.Length < 2 || components.Length > 3)
            {
                return false;
            }

            //
            // 1) Parse the "Identifier", which must come first. Trim any whitespace
            //
            identifier = components[0].Trim();

            if (identifier.Length == 0)
            {
                return false;
            }

            bool versionFound = false;
            profile = null;

            // 
            // The required "Version" and optional "Profile" component can be in any order
            //
            for (int i = 1; i < components.Length; i++)
            {
                // Get the key/value pair separated by '='
                string[] keyValuePair = components[i].Split(c_keyValueSeparator);

                if (keyValuePair.Length != 2)
                {
                    return false;
                }

                // Get the key and value, trimming any whitespace
                string key = keyValuePair[0].Trim();
                string value = keyValuePair[1].Trim();

                //
                // 2) Parse the required "Version" key value
                //
                if (key.Equals(c_versionKey, StringComparison.OrdinalIgnoreCase))
                {
                    versionFound = true;

                    // Allow the version to include a 'v' or 'V' prefix...
                    if (value.Length > 0 && (value[0] == c_versionValuePrefix || value[0] == 'V'))
                    {
                        value = value.Substring(1);
                    }
                    Version realVersion = new Version(value);
                    // The version class will represent some unset values as -1 internally (instead of 0).
                    version = realVersion.Major * 10000;
                    if (realVersion.Minor > 0)
                        version += realVersion.Minor * 100;
                    if (realVersion.Build > 0)
                        version += realVersion.Build;
                }
                //
                // 3) Parse the optional "Profile" key value
                //
                else if (key.Equals(c_profileKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (!String.IsNullOrEmpty(value))
                    {
                        profile = value;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (!versionFound)
            {
                return false;
            }

            return true;
        }

        // This is a partial method. Platforms (such as Desktop) can provide an implementation of it that will read override value
        // from whatever mechanism is available on that platform. If no implementation is provided, the compiler is going to remove the calls
        // to it from the code
        static partial void TryGetSwitchOverridePartial(string switchName, ref bool overrideFound, ref bool overrideValue);

        /// This is a partial method. This method is responsible for populating the default values based on a TFM.
        /// It is partial because each library should define this method in their code to contain their defaults.
        static partial void PopulateDefaultValuesPartial(string platformIdentifier, string profile, int version);
    }
}

#pragma warning restore 436
