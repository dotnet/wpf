// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Internal.OSVersionHelper
{
    // This is not a complete list of operating system versions and service packs.
    // These are the interesting versions where features or behaviors were introduced 
    // or changed, and code needs to detect those points and do different things.
    //
    // This list has been expanded in order to support our new OSVersionHelper lib.
    //
    // If you need to add an OS, do the following steps:
    //     Update NativeConstants.cs with the new OS version/build numbers
    //     Add it to the OperatingSystemVersion enumeration (Congrats, you're here already!)
    //     Create the appropriate managed functions in VersionHelper.cs
    //     Detect your freshly minted OS!
    public enum OperatingSystemVersion
    {
        /// <summary>
        /// WPF minimum requirement.
        /// </summary>
        WindowsXPSP2,

        /// <summary>
        /// DevDiv:1158540
        /// Added for completeness
        /// </summary>
        WindowsXPSP3,

        /// <summary>
        /// Introduced Aero glass, DWM, new common dialogs.
        /// </summary>
        WindowsVista,

        /// <summary>
        /// DevDiv:1158540
        /// Added for completeness
        /// </summary>
        WindowsVistaSP1,

        /// <summary>
        /// DevDiv:1158540
        /// Added for completeness
        /// </summary>
        WindowsVistaSP2,

        /// <summary>
        /// Introduced multi-touch.
        /// </summary>
        Windows7,

        /// <summary>
        /// DevDiv:1158540
        /// Added for completeness
        /// </summary>
        Windows7SP1,

        /// <summary>
        /// Introduced feature on demand
        /// </summary>
        Windows8,

        /// <summary>
        /// Downlevel font fallback differentiation
        /// </summary>
        Windows8Point1,

        /// <summary>
        /// New font fallbacks
        /// </summary>
        Windows10,
    }
}
