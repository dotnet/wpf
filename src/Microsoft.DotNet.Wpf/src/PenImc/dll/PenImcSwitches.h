// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

/// <summary>
/// Switch for PenImc security fixes, set from managed CoreAppContextSwitches
/// via P/Invoke at startup.
/// </summary>
class PenImcSwitches
{
public:
    static bool IsPenImcBoundsCheckProtectionDisabled()
    {
        return s_boundsCheckDisabled;
    }

    static void SetPenImcBoundsCheckProtectionDisabled(bool disabled)
    {
        s_boundsCheckDisabled = disabled;
    }

private:
    static bool s_boundsCheckDisabled;
};
