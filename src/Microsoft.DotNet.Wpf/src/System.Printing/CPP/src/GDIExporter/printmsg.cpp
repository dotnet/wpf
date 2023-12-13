// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifdef DBG

// Debugging code which is only enabled in DBG build and full trust

#include <string.h>
#include <tchar.h>


#include <strsafe.h>

//
// Disable "function compiled as native". The below function is native, and
// uses other native functions. And is DBG only.
//
#pragma warning (push)
#pragma warning (disable : 4793)

/**************************************************************************\
*
* Function Description:
*     Print a formated text message
*
\**************************************************************************/
void 
PrintMsg(
    IN HDC hDC, 
    IN OUT INT *pX, 
    IN int nSkip,    // extra space to skip to avoid font metrics inaccuracy
    IN LPCTSTR szFormat,
    ...
    )
{
    static const INT TEMP_SIZE = 256;
    va_list     marker;
    TCHAR       szTemp[TEMP_SIZE];
    SIZE        size;

    va_start(marker, szFormat);

    StringCchVPrintf(szTemp, TEMP_SIZE, szFormat, marker);

    va_end(marker);

    // szTemp is no longer than 256 so this is fine
    INT iTempLen = static_cast<INT>(_tcslen(szTemp));

    ::TextOut(hDC, *pX, 0, szTemp, iTempLen);

    ::GetTextExtentPoint32(hDC, szTemp, iTempLen, &size);

    *pX += size.cx + nSkip * ::GetDeviceCaps(hDC, LOGPIXELSX) / 72;
}

#pragma warning (pop)

/**************************************************************************\
*
* Function Description:
*     Print trimmed module name
*
\**************************************************************************/
void PrintModuleName(
    IN HDC hDC,
    IN OUT int *pX,
    IN HMODULE hModule
    )
{
    TCHAR szTemp[256];

    ::GetModuleFileName(hModule, szTemp, sizeof(szTemp)/sizeof(TCHAR));
    
    const TCHAR * pName = szTemp;

    while ((_tcslen(pName) > 40) && _tcschr(pName, '\\'))
    {
        pName = _tcschr(pName, '\\') + 1;
    }

    if (pName != szTemp)
    {
        PrintMsg(hDC, pX, 6, _T("'..\\%s'"), pName);
    }
    else
    {
        PrintMsg(hDC, pX, 6, _T("'%s'"), pName);
    }
}

#define B3x(x,y) _T(" (") _T(#x) _T(".") _T(#y) _T(")")

// force expansion of parameters. oddly enough this seems to work without this
// when compiled as unmanaged C++
#define B3(x,y) B3x(x,y)


int ReadShort(array<byte>^ ary, int offset)
{
    return ary[offset] + ary[offset + 1] * 256;
}


/**************************************************************************\
*
* Function Description:
*     Print various diagnosis information in fine print. Called from create.cpp
*
\**************************************************************************/
void FinePrint(
    IN HDC    hDC,
    IN int    NumColors,
    IN bool   SupportJPEGpassthrough,
    IN bool   SupportPNGpassthrough,
    array<Byte>^ devmode
    )
{
    // Save DC, set default DC settings
    ::SaveDC(hDC);

    ::SetMapMode(hDC, MM_TEXT);
    ::SetTextColor(hDC, RGB(0, 0, 0));
    ::SetBkColor(hDC, RGB(255, 255, 255));
    ::SetWindowOrgEx(hDC, 0, 0, NULL);
    ::SetViewportOrgEx(hDC, 0, 0, NULL);
    ::SetBkMode(hDC, OPAQUE);
    ::SelectClipRgn(hDC, NULL);
    ::SetTextAlign(hDC, TA_LEFT | TA_TOP);

    // 5 point Arial font
    HFONT hFont = ::CreateFont(
        - GetDeviceCaps(hDC, LOGPIXELSX) * 5 / 72, 
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, _T("Arial"));

    HGDIOBJ hOld = ::SelectObject(hDC, hFont);

    int nX = 0; // horizontal position

    // OS Version
    IntPtr pVersion = Marshal::StringToCoTaskMemUni(System::Environment::OSVersion->ToString());

    PrintMsg(hDC, &nX, 2, (LPCWSTR) pVersion.ToPointer());

    Marshal::FreeCoTaskMem(pVersion);

    // Application EXE path
    PrintModuleName(hDC, &nX, NULL);
    
    // Builder date/time
    PrintMsg(hDC, &nX, 6, B3(__BUILDMACHINE__, __BUILDDATE__));


    // Resolution/Paper information
    {
        PrintMsg(
            hDC,
            &nX,
            2,
            _T("C%d J%d P%d"),
            NumColors,
            SupportJPEGpassthrough,
            SupportPNGpassthrough);

        PrintMsg(
            hDC, 
            &nX, 
            6,
            _T("%dx%d dpi [%d,%d,%d,%d] %dx%d bpp"),
            GetDeviceCaps(hDC, LOGPIXELSX),
            GetDeviceCaps(hDC, LOGPIXELSY),
            GetDeviceCaps(hDC, PHYSICALOFFSETX),
            GetDeviceCaps(hDC, PHYSICALOFFSETY),
            GetDeviceCaps(hDC, PHYSICALOFFSETX) + GetDeviceCaps(hDC, HORZRES),
            GetDeviceCaps(hDC, PHYSICALOFFSETY) + GetDeviceCaps(hDC, VERTRES),
            GetDeviceCaps(hDC, BITSPIXEL),
            GetDeviceCaps(hDC, PLANES)
            );
    }
    
    if (devmode != nullptr)
    {
        int orientation = ReadShort(devmode, 76);
        int papersize   = ReadShort(devmode, 78);
        int length      = ReadShort(devmode, 80);
        int width       = ReadShort(devmode, 82);
        
        int scale       = ReadShort(devmode, 84);
        int copy        = ReadShort(devmode, 86);
        int source      = ReadShort(devmode, 88);
        int quality     = ReadShort(devmode, 90);

        PrintMsg(
            hDC,
            &nX,
            6,
            _T("o=%d p=%d l=%d w=%d %d%% c=%d s=%d q=%d"),
            orientation,
            papersize,
            length,
            width,
            scale,
            copy,
            source,
            quality
            );
    }

    // Time of printing
    {
        SYSTEMTIME tm;

        GetLocalTime(&tm);
    
        PrintMsg(
            hDC, 
            &nX, 
            4,
            _T("%d/%d/%d %d:%02d:%02d"),
            tm.wMonth, 
            tm.wDay, 
            tm.wYear , 
            tm.wHour, 
            tm.wMinute, 
            tm.wSecond
            );
    }        

    ::SelectObject(hDC, hOld);
    ::DeleteObject(hFont);

    // restore DC settings
    ::RestoreDC(hDC, -1);
}


#endif // DBG
