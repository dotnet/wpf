// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __GDIEXPORTER_DEVICE_H__
#define __GDIEXPORTER_DEVICE_H__

ref class CGDIPath;

#pragma warning ( push )
#pragma warning ( disable:4091 )
typedef enum
{
    CAP_WorldTransform = 0x0004, // Support world transform
    CAP_PolyPolygon    = 0x0008, // Support Polypolygon

    CAP_JPGPassthrough = 0x0080, // Support JPG passthrough
    CAP_PNGPassthrough = 0x0100, // Support PNG passthrough
    
    CAP_GradientRect   = 0x1000,
    CAP_CharacterStream = 0x2000,  // Is a text only device
};
#pragma warning ( pop )

ref class CachedGDIObject
{
protected:
    array<Byte>   ^ m_RawData;

    GdiSafeHandle ^ m_handle;
    
public:
    
    property GdiSafeHandle^ Handle
    {
        GdiSafeHandle^ get() { return m_handle; }
    }

    CachedGDIObject(const interior_ptr<Byte> pData, int size, GdiSafeHandle ^ handle)
    {
        m_RawData = gcnew array<Byte>(size);

        for (int i = 0; i < size; i ++)
        {
            m_RawData[i] = pData[i];
        }

        m_handle = handle;
    }

    GdiSafeHandle^ Match(const interior_ptr<Byte> pData, int size)
    {
        if (size != m_RawData->Length)
        {
            return nullptr;
        }

        for (int i = 0; i < size; i ++)
        {
            if (m_RawData[i] != pData[i])
            {
                return nullptr;
            }
        }

        return m_handle;
    }
};


/// <Summary>
///     Thin wrapper over an HDC
/// </Summary>
ref class CGDIDevice
{
protected:
    GdiSafeDCHandle ^ m_hDC;

    unsigned long m_Caps;

    unsigned m_nDpiX;
    unsigned m_nDpiY;
    double   m_RasterizationDPI;

    array<Byte>^ m_lastDevmode;

    array<CachedGDIObject ^> ^ m_Cache;
    int                             m_CacheFirst;
    
    GdiSafeHandle^                  m_lastFont;

    GdiSafeHandle^                  m_lastPen;
    
    GdiSafeHandle^                  m_lastBrush;
    
    COLORREF                        m_lastTextColor;
    int                             m_lastPolyFillMode;
    unsigned                        m_lastTextAlign;
    float                           m_lastMiterLimit;

    /// <Remarks>
    /// Hash table mapping from font name string to FontInfo. An entry here does not imply
    /// the font is installed and usable; see FontInfo for more information.
    /// </Remarks>
    static Hashtable              ^ s_InstalledFonts;   // For local EMF spooling, we can't unstall fonts until print job finishes
                                                        // So for the moment, we will leak the fonts until applications closes
                                                        // In long term, we need a way to wait for job completion
    static Object                 ^ s_lockObject = gcnew Object();            // Synchronization
    static ArrayList              ^ s_oldPrivateFonts = gcnew ArrayList();    // Fonts to be deleted after 10 minutes, upon new print job  
    
public:

    GdiSafeHandle                 ^ m_nullPen;

    GdiSafeHandle                 ^ m_nullBrush;

    GdiSafeHandle                 ^ m_whiteBrush;

    GdiSafeHandle                 ^ m_blackBrush;

    static property ArrayList^ OldPrivateFonts
    {
        ArrayList^ get()
        {
            return s_oldPrivateFonts;
        }
    }

    // Constructor
    CGDIDevice();

    void Release();

    /// <SecurityCritical>
    /// Critical : References security critical type 'GdiSafeHandle'
    /// </SecurityCritical>
    void ResetStates();
    
    // Initialization
    HRESULT InitializeDevice();

    // Query

    unsigned long GetCaps(void)
    {
        return m_Caps;
    }

    unsigned GetDpiX(void)
    {
        return m_nDpiX;
    }

    unsigned GetDpiY(void)
    {
        return m_nDpiY;
    }

    BOOL GetDCOrgEx(POINT & pOrigin);

    void SelectObject(GdiSafeHandle^ hObj, int type);

    HRESULT SetupForIncreasedResolution(int resolutionMultiplier, XFORM& oldTransform);

    HRESULT CleanupForIncreasedResolution(int resolutionMultiplier, const XFORM& oldTransform);

    HRESULT SetPolyFillMode(int polyfillmode);

    BOOL SelectClipPath(int iMode);

    HRESULT SetMiterLimit(FLOAT eNewLimit);

    HRESULT SetTextColor(COLORREF color);

    // Drawing primitives

    HRESULT Polygon (array<PointI>^ pPoints, int offset, int nCount);

    HRESULT Polyline(array<PointI>^ pPoints, int offset, int nCount);    
    
    HRESULT PolyPolygon(array<PointI>^ pPoints,     int offsetP, 
                     array<unsigned int> ^   pPolyCounts, int offsetC, 
                     int nCount);

    HRESULT PolyPolyline(array<PointI>^ pPoints, array<unsigned int>^ pPolyCounts, int nCount);
    
    HRESULT BeginPath(void);

    HRESULT EndPath(void);

    HRESULT FillPath(void);

    HRESULT StretchDIBits(
            int XDest,                    // x-coord of destination upper-left corner
            int YDest,                    // y-coord of destination upper-left corner
            int nDestWidth,               // width of destination rectangle
            int nDestHeight,              // height of destination rectangle
            int XSrc,                     // x-coord of source upper-left corner
            int YSrc,                     // y-coord of source upper-left corner
            int nSrcWidth,                // width of source rectangle
            int nSrcHeight,               // height of source rectangle
            interior_ptr<Byte> pBits,  // bitmap bits
            interior_ptr<BITMAPINFO> pBitsInfo         // bitmap data
            );

    HRESULT FillRect(int x, int y, int width, int height, GdiSafeHandle^ hBrush);

    HRESULT PolyBezier(array<PointI>^ pPoints, int nCount);

    HRESULT DrawMixedPath(array<PointI>^ pPoints, array<Byte> ^ pTypes, int nCount);

    HRESULT HrEndDoc(void);

    HRESULT HrStartPage(array<Byte>^ pDevmode);
    
    HRESULT HrEndPage(void);

    HRESULT SetTextAlign(
        unsigned     textAlign // text-alignment option
    );

    BOOL EscapeSupported(DWORD function)
    {
        return CNativeMethods::ExtEscape(m_hDC, QUERYESCSUPPORT, sizeof(DWORD), (void*)(LPCSTR)& function, 0, NULL) != 0;
    }

    GdiSafeHandle^ CacheMatch(const interior_ptr<Byte> pData, int size);

    void CacheObject(const interior_ptr<Byte> pData, int size, GdiSafeHandle^ handle);

    GdiSafeHandle ^ ConvertPen(
        Pen           ^ pen,
        Brush         ^ pStrokeBrush,
        Matrix          matrix,
        CGDIPath      ^ pPath,
        int             dpi
        );

    GdiSafeHandle^ ConvertBrush(Brush ^brush);
    
    GdiSafeHandle^ ConvertBrush(COLORREF color);

    
    /// <Remarks>
    /// Checks if font is installed, and performs necessary installs and uninstalls to make font usable by GDI.
    /// </Remarks>
    /// <Returns>Returns nullptr if unable to retrieve font directory name or install the font.
    /// Otherwise, (new) font family name will be returned.
    /// If nullptr is returned, caller should fallback to filling text geometry.</Returns>
    String^ CheckFont(GlyphTypeface^ typeface, String ^ name, [Out] Boolean% isPrivateFont);

    /// <Remarks>
    /// Uninstalls only private fonts, i.e. fonts that we manually install during glyph printing.
    /// </Remarks>
    void UninstallFonts();

    property
    bool
    HasDC
    {
        bool get();
    }
};

#endif
