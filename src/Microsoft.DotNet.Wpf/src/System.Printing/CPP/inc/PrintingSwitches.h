// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

/// <summary>
/// Switch for System.Printing security fixes, initialized lazily
/// from AppContext at first use.
/// </summary>
class PrintingSwitches
{
public:
    static bool IsPrintingBoundsCheckProtectionDisabled()
    {
        static bool s_initialized = false;
        static bool s_disabled = false;

        if (!s_initialized)
        {
            bool switchValue = false;
            System::AppContext::TryGetSwitch(
                "Switch.MS.Internal.Printing.DisablePrintingBoundsCheckProtection",
                switchValue);
            s_disabled = switchValue;
            s_initialized = true;
        }
        return s_disabled;
    }
};
