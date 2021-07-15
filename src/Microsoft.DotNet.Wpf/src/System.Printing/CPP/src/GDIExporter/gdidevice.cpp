// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
CGDIDevice::CGDIDevice()
{
    m_Caps     = CAP_WorldTransform | // world transform is only supported on NT-based systems
                 CAP_PolyPolygon    | // Polypolygon can cause performance problems on 9X
                 0;

    m_nDpiX    = 300;
    m_nDpiY    = 300;

    m_nullBrush  = CNativeMethods::GetStockObject(NULL_BRUSH);  // ROBERTAN
    m_nullPen    = CNativeMethods::GetStockObject(NULL_PEN);    // ROBERTAN
    
    m_whiteBrush = CNativeMethods::GetStockObject(WHITE_BRUSH);
    m_blackBrush = CNativeMethods::GetStockObject(BLACK_BRUSH);

    m_RasterizationDPI = 96;
}

bool CGDIDevice::HasDC::get(void)
{
    return m_hDC != nullptr;
}


void CGDIDevice::Release()
{
    if (HasDC)
    {
        m_hDC->Close();
        m_hDC = nullptr;
    }
}


HRESULT CGDIDevice::InitializeDevice()
{
    HRESULT hr = S_OK;

    DWORD objType = CNativeMethods::GetObjectType(m_hDC);

    // Allow rendering into EMF/comptabile DCs
    if ((objType != OBJ_DC) && (objType != OBJ_ENHMETADC) && (objType != OBJ_MEMDC))
    {
        hr = E_INVALIDARG;
    }

    if (SUCCEEDED(hr))
    {
        m_nDpiX     = CNativeMethods::GetDeviceCaps(m_hDC, LOGPIXELSX);
        m_nDpiY     = CNativeMethods::GetDeviceCaps(m_hDC, LOGPIXELSY);

        if (EscapeSupported(CHECKJPEGFORMAT))
        {
            m_Caps |= CAP_JPGPassthrough;
        }

        if (EscapeSupported(CHECKPNGFORMAT))
        {
            m_Caps |= CAP_PNGPassthrough;
        }

        if (CNativeMethods::GetDeviceCaps(m_hDC, SHADEBLENDCAPS) & SB_GRAD_RECT)
        {
            m_Caps |= CAP_GradientRect;
        }
        
        if (DT_CHARSTREAM == CNativeMethods::GetDeviceCaps(m_hDC, TECHNOLOGY))
        {
            m_Caps |= CAP_CharacterStream;
        }
    }

    return hr;
}


HRESULT CGDIDevice::FillRect(int x, int y, int width, int height, GdiSafeHandle^ brush)
{
    SelectObject(brush, OBJ_BRUSH);

    return ErrorCode(CNativeMethods::PatBlt(m_hDC, x, y, width, height, PATCOPY));
}

HRESULT CGDIDevice::BeginPath(void)
{
    return ErrorCode(CNativeMethods::BeginPath(m_hDC));    // ROBERTAN
}


HRESULT CGDIDevice::EndPath(void)
{
    return ErrorCode(CNativeMethods::EndPath(m_hDC));  // ROBERTAN
}


HRESULT CGDIDevice::FillPath(void)
{
    return ErrorCode(CNativeMethods::FillPath(m_hDC));     // ROBERTAN
}


HRESULT CGDIDevice::Polygon(array<PointI>^ pPoints, int offset, int nCount)
{
    if (nCount <= 1)
    {
        return S_OK;
    }

    pin_ptr<PointI> pPointsPin = &pPoints[offset];

    return ErrorCode(CNativeMethods::Polygon(m_hDC, pPointsPin, nCount));   // ROBERTAN
}


HRESULT CGDIDevice::Polyline(array<PointI>^ pPoints, int offset, int nCount)
{
    if (nCount <= 1)
    {
        return S_OK;
    }

    // Bug fix for 1139723: line with 30K points does not print on PCL/PS printers, display very slowly in GDI
    // Break lines could cause start/end/line cap differences. But avoiding unprintable cases may be more important.
    if (nCount > 4096)
    {
        HRESULT hr = Polyline(pPoints, offset,              nCount / 2);

        if (SUCCEEDED(hr))
        {
            hr = Polyline(pPoints, offset + nCount / 2 - 1, nCount - nCount / 2 + 1);
        }

        return hr;
    }
    else
    {
        pin_ptr<PointI> pPointsPin = &pPoints[offset];

        return ErrorCode(CNativeMethods::Polyline(m_hDC, pPointsPin, nCount));  // ROBERTAN
    }
}


HRESULT CGDIDevice::PolyPolygon(array<PointI>^        pPoints,     int offsetP, 
                                array<unsigned int> ^ pPolyCounts, int offsetC, 
                                int nCount)
{
    if (nCount <= 0)
    {
        return S_OK;
    }

    pin_ptr<PointI> pPointsPin = &pPoints[offsetP];
    pin_ptr<unsigned int> pPolyCountsPin = &pPolyCounts[offsetC];

    return ErrorCode(CNativeMethods::PolyPolygon(m_hDC, pPointsPin, pPolyCountsPin, nCount));  // ROBERTAN
}


HRESULT CGDIDevice::PolyPolyline(array<PointI>^ pPoints, array<unsigned int> ^ pPolyCounts, int nCount)
{
    if (nCount <= 0)
    {
        return S_OK;
    }

    pin_ptr<PointI> pPointsPin = &pPoints[0];
    pin_ptr<unsigned int> pPolyCountsPin = &pPolyCounts[0];

    return ErrorCode(CNativeMethods::PolyPolyline(m_hDC, pPointsPin, pPolyCountsPin, nCount));     // ROBERTAN
}


HRESULT CGDIDevice::PolyBezier(array<PointI>^ pPoints, int nCount)
{
    if (nCount <= 0)
    {
        return S_OK;
    }

    pin_ptr<PointI> pPointsPin = &pPoints[0];

    return ErrorCode(CNativeMethods::PolyBezier(m_hDC, pPointsPin, nCount));
}


HRESULT CGDIDevice::SetPolyFillMode(int polyfillmode)
{
    if (polyfillmode != m_lastPolyFillMode)
    {
        m_lastPolyFillMode = polyfillmode;

        return ErrorCode(CNativeMethods::SetPolyFillMode(m_hDC, polyfillmode));     // ROBERTAN
    }
    
    return S_OK;
}


BOOL CGDIDevice::GetDCOrgEx(POINT & Origin)
{
    return CNativeMethods::GetDCOrgEx(m_hDC, & Origin);      // ROBERTAN
}


HRESULT CGDIDevice::StretchDIBits(
    int XDest,                    // x-coord of destination upper-left corner
    int YDest,                    // y-coord of destination upper-left corner
    int nDestWidth,               // width of destination rectangle
    int nDestHeight,              // height of destination rectangle
    int XSrc,                     // x-coord of source upper-left corner
    int YSrc,                     // y-coord of source upper-left corner
    int nSrcWidth,                // width of source rectangle
    int nSrcHeight,               // height of source rectangle
    interior_ptr<Byte> pBits,                  // bitmap bits
    interior_ptr<BITMAPINFO> pBitsInfo         // bitmap data
    )
{
    if ((nSrcWidth == 0) || (nSrcHeight == 0))
    {
        return S_OK;
    }

    // Change tiny bitmap to less expensive call
    if ((XSrc == 0) && (YSrc == 0) && 
        (pBitsInfo->bmiHeader.biCompression == BI_RGB) && 
        (pBitsInfo->bmiHeader.biBitCount == 24))
    {
        if ((nSrcWidth == 1) && (nSrcHeight == 2))
        {
            int diff = Math::Abs(pBits[0] - pBits[4]) + 
                       Math::Abs(pBits[1] - pBits[5]) + 
                       Math::Abs(pBits[2] - pBits[6]);

            if (diff < 3) // ignore tiny difference
            {
                nSrcHeight = 1;
            }
        }

        if ((nSrcWidth == 1) && (nSrcHeight == 1))
        {
            GdiSafeHandle^ brush = ConvertBrush(RGB(pBits[2], pBits[1], pBits[0]));

            if (brush != nullptr)
            {
                return FillRect(XDest, YDest, nDestWidth, nDestHeight, brush);
            }
        }
    }

    pin_ptr<Byte> pBitsPin = pBits;
    pin_ptr<BITMAPINFO> pBitsInfoPin = pBitsInfo;

    return ErrorCode(CNativeMethods::StretchDIBits(m_hDC,
        XDest, YDest, nDestWidth, nDestHeight,
        XSrc,  YSrc,  nSrcWidth,  nSrcHeight,
        (void*)pBitsPin, pBitsInfoPin, DIB_RGB_COLORS, SRCCOPY) != GDI_ERROR);
}


void CGDIDevice::SelectObject(GdiSafeHandle^ hObj, int type)
{
    switch (type)
    {
    case OBJ_FONT:
        if (Object::ReferenceEquals(hObj, m_lastFont))
        {
            return;
        }

        m_lastFont = hObj;
        break;

    case OBJ_BRUSH:
        if (Object::ReferenceEquals(hObj, m_lastBrush))
        {
            return;
        }
        
        m_lastBrush = hObj;
        break;

    case OBJ_PEN:
        if (Object::ReferenceEquals(hObj, m_lastPen))
        {
            return;
        }

        m_lastPen = hObj;
        break;

    default:
        break;
    }

    CNativeMethods::SelectObject(m_hDC, hObj);    // ROBERTAN
}


HRESULT CGDIDevice::SetupForIncreasedResolution(int resolutionMultiplier, XFORM& oldTransform)
{
    if (resolutionMultiplier > 1)
    {
        Debug::Assert((GetCaps() & CAP_WorldTransform) != 0);

        // The points are greater than we want them so we need
        // to set a scaling transform to get them to the right size.
        // We do this to avoid rounding errors when outputting to a metafile.

        if(CNativeMethods::GetWorldTransform(m_hDC, &oldTransform) == 0)
        {
            return ErrorCode(E_FAIL);
        }

        XFORM xform;
        xform.eM11 = 1.0f / resolutionMultiplier;
        xform.eM22 = xform.eM11;
        xform.eM12 = 0.0f;
        xform.eM21 = 0.0f;
        xform.eDx  = 0.0f;
        xform.eDy  = 0.0f;

        return ErrorCode(CNativeMethods::ModifyWorldTransform(m_hDC, &xform, MWT_LEFTMULTIPLY) != 0);      // ROBERTAN
    }
    
    return S_OK;
}


HRESULT 
CGDIDevice::CleanupForIncreasedResolution(int resolutionMultiplier, const XFORM& oldTransform)
{
    if (resolutionMultiplier > 1)
    {
        Debug::Assert((GetCaps() & CAP_WorldTransform) != 0);

        return ErrorCode(CNativeMethods::SetWorldTransform(m_hDC, &oldTransform));
    }
    
    return S_OK;
}



HRESULT CGDIDevice::DrawMixedPath(array<PointI>^ pPoints, array<Byte> ^ pTypes, int nCount)
{
    // this assumes pTypes uses GDI PolyDraw types
    pin_ptr<PointI> pPointsPin = &pPoints[0];
    pin_ptr<Byte> pTypesPin = &pTypes[0];

    return ErrorCode(CNativeMethods::PolyDraw(m_hDC, pPointsPin, pTypesPin, nCount));      // ROBERTAN
}


HRESULT CGDIDevice::HrEndDoc(void)
{
    HRESULT hr = ErrorCode(CNativeMethods::EndDoc(m_hDC) > 0);     // ROBERTAN

    // Uninstall fonts that were installed through GDI.
    UninstallFonts();

    return hr;
}


// Initialize cached DC stats to invalid values, so they will be set explicitly next time
void CGDIDevice::ResetStates(void)
{
    m_lastFont         = nullptr;
    m_lastBrush        = nullptr;
    m_lastPen          = nullptr;
    m_lastTextColor    = 0xFFFFFFFF;
    m_lastPolyFillMode = -1;
    m_lastTextAlign    = 0xFFFFFFFF;
    m_lastMiterLimit   = 0;
}


HRESULT CGDIDevice::HrStartPage(array<Byte>^ devmode)
{
    if (devmode != nullptr)
    {
        m_lastDevmode = devmode;

        CNativeMethods::ResetDCW(m_hDC, devmode);   // ROBERTAN
    }

    int rslt = CNativeMethods::StartPage(m_hDC);    // ROBERTAN

    HRESULT hr = ErrorCode(rslt > 0);

    if (rslt> 0)
    {
        // Always use TRANSPARENT backmode, for GDI text
        int errCode1 = CNativeMethods::SetBackMode(m_hDC, TRANSPARENT);

        // set some default states to avoid state changes while drawing
        int errCode2 = CNativeMethods::SetStretchBltMode(m_hDC, COLORONCOLOR);

        // always use advanced mode in case we require increased resolution via transformation
        // for complex paths
        int errCode3 = CNativeMethods::SetGraphicsMode(m_hDC, GM_ADVANCED);
        
        if(errCode1 > 0 && errCode2 > 0 && errCode3 > 0)
        {
            hr = S_OK;
        }

        ResetStates();
    }

    return hr;
}

HRESULT CGDIDevice::HrEndPage(void)
{
 #ifdef DBG
    if (Microsoft::Internal::AlphaFlattener::Utility::DisplayPageDebugHeader)
    {
        FinePrint(
            m_hDC->GetHDC(),
            CNativeMethods::GetDeviceCaps(m_hDC, NUMCOLORS),    // ROBERTAN
            (m_Caps & CAP_JPGPassthrough) != 0, 
            (m_Caps & CAP_PNGPassthrough) != 0,
            m_lastDevmode
            );
    }
#endif

    HRESULT hr = ErrorCode(CNativeMethods::EndPage(m_hDC) > 0);  // ROBERTAN

    m_lastFont  = nullptr;
    m_lastBrush = nullptr;
    m_lastPen   = nullptr;

    if (m_Cache != nullptr)
    {
        for (int i = 0; i < m_Cache->Length; i ++)
        {
            if (m_Cache[i] != nullptr)
            {
                GdiSafeHandle ^old = m_Cache[i]->Handle;

                if(old != nullptr && !old->IsInvalid) 
                {
                    old->Close();
                }

                m_Cache[i] = nullptr;
            }
        }
    }

    return hr;
}


BOOL CGDIDevice::SelectClipPath(int iMode)
{
    return CNativeMethods::SelectClipPath(m_hDC, iMode);    // ROBERTAN
}


HRESULT CGDIDevice::SetMiterLimit(float eNewLimit)
{
    if (! AreCloseReal(m_lastMiterLimit, eNewLimit))
    {
        m_lastMiterLimit = eNewLimit;

        return ErrorCode(CNativeMethods::SetMiterLimit(m_hDC, eNewLimit, NULL));    // ROBERTAN
    }
    
    return S_OK;
}


HRESULT CGDIDevice::SetTextColor(COLORREF color)
{
    if (color != m_lastTextColor)
    {
        m_lastTextColor = color;
        return ErrorCode(CNativeMethods::SetTextColor(m_hDC, color));     // ROBERTAN
    }
    
    return S_OK;
}


// Text

HRESULT CGDIDevice::SetTextAlign(unsigned textAlign)
{
    if (m_lastTextAlign != textAlign)
    {
        m_lastTextAlign = textAlign;

        ErrorCode(CNativeMethods::SetTextAlign(m_hDC, textAlign));   // ROBERTAN
    }
    
    return S_OK;
}


