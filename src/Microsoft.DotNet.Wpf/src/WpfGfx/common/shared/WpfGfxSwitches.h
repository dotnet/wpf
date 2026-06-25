// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

/// <summary>
/// Switch for WpfGfx security fixes, set from managed CoreAppContextSwitches
/// via P/Invoke at startup.
/// </summary>
class WpfGfxSwitches
{
public:
    static bool IsWpfGfxBoundsCheckProtectionDisabled()
    {
        return g_fWpfGfxBoundsCheckProtectionDisabled;
    }

    static void SetWpfGfxBoundsCheckProtectionDisabled(bool disabled)
    {
        g_fWpfGfxBoundsCheckProtectionDisabled = disabled;
    }

private:
    static bool g_fWpfGfxBoundsCheckProtectionDisabled;
};
