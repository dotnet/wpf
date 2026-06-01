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
