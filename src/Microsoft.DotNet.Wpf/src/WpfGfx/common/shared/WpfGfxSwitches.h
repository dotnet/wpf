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
