// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace System.Xaml.MS.Impl
{
    /// <summary>
    /// AppContext switches for the System.Xaml assembly.
    ///
    /// System.Xaml does not have LocalAppContext / LocalAppContext.GetCachedSwitchValue
    /// infrastructure (those live in PresentationCore and above), so this class reads
    /// switches directly via <see cref="AppContext.TryGetSwitch"/>.
    ///
    /// The cache fields are written without synchronization; this is benign because
    /// both racing threads compute the same deterministic value and int writes are
    /// atomic on all .NET platforms.
    /// </summary>
    internal static class XamlAppContextSwitches
    {
        #region DisableMarkupExtensionDepthGuard

        /// <summary>
        /// Switch: Switch.System.Xaml.DisableMarkupExtensionDepthGuard
        ///   Default (false): The markup-extension nesting depth cap is enforced,
        ///                    preventing stack overflow from deeply nested {Binding ...}
        ///                    constructs in attacker-supplied XAML (CWE-674 / CWE-770).
        ///   Set to true:     The depth cap is skipped, restoring previous behavior.
        /// </summary>
        internal const string DisableMarkupExtensionDepthGuardSwitchName =
            "Switch.System.Xaml.DisableMarkupExtensionDepthGuard";

        private static int _disableMarkupExtensionDepthGuard; // 0 = not read, 1 = true, -1 = false

        internal static bool DisableMarkupExtensionDepthGuard
        {
            get
            {
                if (_disableMarkupExtensionDepthGuard == 0)
                {
                    bool switchValue = false;
                    try
                    {
                        AppContext.TryGetSwitch(DisableMarkupExtensionDepthGuardSwitchName, out switchValue);
                    }
                    catch (Exception)
                    {
                        // If AppContext is not available, default to the protected behavior.
                    }

                    _disableMarkupExtensionDepthGuard = switchValue ? 1 : -1;
                }

                return _disableMarkupExtensionDepthGuard > 0;
            }
        }

        #endregion
    }
}
