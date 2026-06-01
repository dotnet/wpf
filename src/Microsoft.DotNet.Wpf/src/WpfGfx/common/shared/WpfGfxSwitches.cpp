// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <precomp.hpp>
#include "WpfGfxSwitches.h"

// Static storage for WpfGfxSwitches
bool WpfGfxSwitches::g_fWpfGfxBoundsCheckProtectionDisabled = false;

void WINAPI
WpfGfx_SetDisableBoundsCheckProtection(BOOL fDisable)
{
    WpfGfxSwitches::SetWpfGfxBoundsCheckProtectionDisabled(!!fDisable);
}
