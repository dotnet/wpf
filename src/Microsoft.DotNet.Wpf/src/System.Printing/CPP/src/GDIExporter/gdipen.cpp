// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#define WidthIsOne 1

bool DashStylesEqual(DashStyle^ da, DashStyle^ db)
{
    Debug::Assert(da != nullptr && db != nullptr);

    if (!AreCloseReal(da->Offset, db->Offset))
    {
        return false;
    }

    DoubleCollection^ a = da->Dashes;
    DoubleCollection^ b = db->Dashes;

    Debug::Assert(a != nullptr && b != nullptr);
    
    int count = a->Count;
    
    if (count != b->Count)
    {
        return false;
    }

    for (int i = count-1; i >= 0; i--)
    {
        if (!AreCloseReal(a[i], b[i]))
        {
            return false;
        }
    }

    return true;
}


bool PenSupported(Pen ^ pPen, Matrix matrix, unsigned dpi)
{
    Debug::Assert(pPen != nullptr);

    // Pen width in inch
    double width  = pPen->Thickness * GetScaleX(matrix) / dpi;

    if ((width > 0.028) && !DashStylesEqual(pPen->DashStyle, DashStyles::Solid))
    {
        // Reject dash pen thicker than 2/72 inch. Avalon dash pattern  may not match device dash pattern
        return false;
    }

    return true;
}


bool IsInteger(double value)
{
    return AreCloseReal(value, (double)((int)value));
}

// We can convert dashes from WPF doubles to GDI integers
// If the dash offset and the array of dashes and gaps  all add up to integers
// and the pen thickness is at most 1 gdi logical unit
bool CanConvertDashes(DashStyle ^dashStyle)
{
    double value = dashStyle->Offset;
    
    if(!IsInteger(value))
    {
        return false;
    }
    
    for(int i = 0; i < dashStyle->Dashes->Count; i++)
    {
        double dashOrGap = dashStyle->Dashes[i];
        double isPositive = AreCloseReal(dashOrGap, 0.0) || dashOrGap > 0.0;
        if(!isPositive)
        {
            return false;
        }
        
        value += dashOrGap;
    
        // Test the accumulated value to make sure
        // rounding errors dont accumulate to non integer values
        if(!IsInteger(value))
        {
            return false;
        }
    }
    
    return true;
}

// Convert WPF DashStyle to GDI dashes array
// Conversion will be incorrect if CanConvertDashes returns false given the same dashStyle
// A null return value is equivalent to an empty array
array<DWORD>^ ConvertDashes(DashStyle ^dashStyle)
{	
        
    int dashCount = dashStyle->Dashes->Count;
    
    // Corner case empty dash array
    if(dashCount < 1)
    {
        return nullptr;
    }
    
    // if the number of dashes is odd wpf doubles the dash pattern when rendering
    if((dashCount % 2) != 0)
    {
        dashCount *= 2;
    }
    
    // make sure dashOffset is safe for "modulo Dashes->Count" math
    int dashOffset = (int)Math::Round(dashStyle->Offset);
    while(dashOffset < 0)
    {
        dashOffset += dashCount;
    }
    
    // Copy the dashStyle's DoubleCollection into an int array taking Offset into account
    array<DWORD>^ dashes = gcnew array<DWORD>(dashCount);
    for(int i = 0; i < dashCount; i++)
    {
        double dashOrGap = dashStyle->Dashes[(dashOffset + i) % dashStyle->Dashes->Count];
        dashes[i] = (DWORD)Math::Round(dashOrGap);
    }
    
    return dashes;
}

int GetStyle(Pen ^ pen, bool thick, array<DWORD>^ % dashes)
{
    int style = -1;
    dashes = nullptr;

    // Determine dash style.
    DashStyle ^ da = pen->DashStyle;

    if (da == nullptr)
    {
        style = PS_SOLID;
    }
    else if(DashStylesEqual(da, DashStyles::Solid))
    {
        style = PS_SOLID;
    }
    else if (DashStylesEqual(da, DashStyles::Dash))
    {
        style = PS_DASH;
    }
    else if (DashStylesEqual(da, DashStyles::Dot))
    {	        
        style = PS_DOT;
    }
    else if (DashStylesEqual(da, DashStyles::DashDot))
    {
        style = PS_DASHDOT;
    }
    else if (DashStylesEqual(da, DashStyles::DashDotDot))
    {
        style = PS_DASHDOTDOT;
    }
    
    else if(CanConvertDashes(da))
    {
        style = PS_USERSTYLE;
    }
    
    if (style >= 0 && thick)
    {
        // Dash cap is irrelevant if there are no dashes
        PenLineCap gdipStartCap = pen->StartLineCap;
        PenLineCap gdipEndCap   = pen->EndLineCap;

        PenLineCap gdipDashCap = PenLineCap::Flat; 
            
        if (style == PS_SOLID)
        {
            gdipDashCap = pen->StartLineCap;
        }
        else
        {
            gdipDashCap = pen->DashCap;
        }
        
        if ((gdipStartCap != gdipEndCap)  ||
            (gdipEndCap   != gdipDashCap))
        {
            return -1;     // Gdi doesn't support differing line caps.
        }

        switch (gdipStartCap)
        {
        case PenLineCap::Flat:   
            style |= PS_ENDCAP_FLAT;   
            break;
        
        case PenLineCap::Round:  
            style |= PS_ENDCAP_ROUND;  
            break;
        
        case PenLineCap::Square: 
            style |= PS_ENDCAP_SQUARE; 
            break;

        case PenLineCap::Triangle: 
        default:
            style = -1;
        }
    }

    if(style >= 0 && ((style & PS_USERSTYLE) == PS_USERSTYLE))
    {
        dashes = ConvertDashes(da);
    }
    
    return style;
}


struct LOGPEN
{
    LOGBRUSH brush;
    int      style;
    int      width;
};


GdiSafeHandle^ CGDIDevice::ConvertPen(
    Pen               ^ pen,
    Brush             ^ pStrokeBrush,
    Matrix              matrix,
    CGDIPath          ^ pPath,
    int                 dpi
    )
{
    // !!! Can't support transformed pens or compound pens.
    if (! PenSupported(pen, matrix, dpi))
    {
        return nullptr;
    }

    Debug::Assert(pen->Thickness != 0, "GDI doesn't support 0-width pens");

    double widthF = pen->Thickness * GetScaleX(matrix) * pPath->GetResolutionScale();

    if (widthF <= 0)
    {
        return nullptr;
    }

    int    width  = (int) Math::Round(widthF);

    bool thick = true;
        
    if (width <= 1)
    {
        width = 1;

        thick = false;
    }

    // Determine dash style.
    array<DWORD>^ dashes = nullptr;
    int style = GetStyle(pen, thick, dashes);

    if (style < 0)
    {
        return nullptr;
    }

    // Don't have to worry about caps and joins for thin pens.
    if (thick)
    {
        bool miterClipped = false;

        PenLineJoin join  = pen->LineJoin;
    
        // fyuan 3/12/04
        // Avalon miter is different from GDI miter. It's quite expensive to fail the pen conversion
        // and convert to fill the widened path. So we try to find the sharpest angle within a path.
        // If miter limit is not reached, it's safe to switch to MILLineJoinMiterClipped.
        if ((join == PenLineJoin::Miter) && (pPath != nullptr))
        {
            double maxcos = pPath->MaxCos();
            double miter  = pen->MiterLimit;

            // miter = 1 / sin(theta/2)
            // sin(theta/2) = sqrt((1+cos(theta))/2)
            // cos(theta) = 1 - 2 (1/miter)^2

            if (miter > 0.5)
            {
                double t = 1 - 2 / miter / miter;

                if (maxcos < t) // Sharpest angle not small enough to generate a huge miter spike
                {
                    miterClipped = true;
                }
            }
        }

        switch (join)
        {
        // Avalon Miter is different from GDI Miter (which is renamed MiterClipped here)
        // Abort conversion for thicker pen to force converting to widened path
        case PenLineJoin::Miter:
            if (!miterClipped && (width > 1))
            {
                return nullptr;
            }
            
            style |= PS_JOIN_MITER;
            SetMiterLimit((float)pen->MiterLimit);
            break;
        
        case PenLineJoin::Round: 
            style |= PS_JOIN_ROUND; 
            break;
        
        case PenLineJoin::Bevel: 
            style |= PS_JOIN_BEVEL; 
            break;

        default:
            return nullptr;
        }
    }

    if (pStrokeBrush->GetType() == SolidColorBrush::typeid)
    {
        SolidColorBrush ^ pSolid = (SolidColorBrush ^) pStrokeBrush;

        LOGPEN   lp;
        
        lp.brush.lbStyle = BS_SOLID;
        lp.brush.lbColor = ToCOLORREF(pSolid);
        lp.brush.lbHatch = 0;
        lp.style         = style;
        lp.width         = width;

        GdiSafeHandle ^ pen = CacheMatch((interior_ptr<Byte>) & lp, sizeof(lp));

        if (pen == nullptr)
        {
            int dashCount = (dashes != nullptr) ? dashes->Length : 0;
            pen = CNativeMethods::ExtCreatePen(PS_GEOMETRIC | lp.style, lp.width, & lp.brush, dashCount, dashes);  // ROBERTAN

            if (pen != nullptr)
            {
                // Dont cache pens with user defined dashes because the cache key does not include the user dash array
                // So the cache cannot distinguish between LOGPENS that create pens differing only in dash styles
                if((lp.style & PS_USERSTYLE) != PS_USERSTYLE)
                {
                    CacheObject((interior_ptr<Byte>) & lp, sizeof(lp), pen);
                }
            }
            else
            {
                Debug::Assert(false, "ExtCreatePen failed");
            }
        }

        return pen;
    }

    return nullptr;
}

