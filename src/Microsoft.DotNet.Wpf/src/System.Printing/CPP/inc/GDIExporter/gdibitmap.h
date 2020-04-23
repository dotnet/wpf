// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __GDIEXPORTER_BITMAP_H__
#define __GDIEXPORTER_BITMAP_H__

inline int GetDIBStride(int width, int bitcount)
{
    return (width * bitcount + 31) / 32 * 4;
}

ref class PaletteSorter;

value class CGDIBitmap
{
private:
    // m_Buffer[m_Offset] points to the top scanline.
    // This implies that if (m_Stride > 0), then it's a top-down buffer, otherwise
    // it's a bottom-up buffer.
    int                m_Width;
    int                m_Height;
    int                m_Stride;
    PixelFormat        m_PixelFormat;

    int                m_Offset;
    BitmapSource     ^ m_pBitmap;
    array<BYTE>      ^ m_Bi;


    array<BYTE> ^ m_Buffer;
    
    PaletteSorter    ^ m_pSorter;

public: 

    HRESULT Load(BitmapSource ^ pBitmap, array<Byte>^ buffer, PixelFormat LoadFormat);

    System::Collections::Generic::IList<Color> ^ GetColorTable();

//  HRESULT CopyCropImage();
    
    HRESULT ColorReduction();

    void SetBits(interior_ptr<BITMAPINFO> bmi);

    HRESULT StretchBlt(CGDIDevice ^ pDevice, const Int32Rect & dst, bool flipHoriz, bool flipVert);
    
    bool IsValid(void)
    {
        return m_Buffer != nullptr;
    }

    void SetupPalette(interior_ptr<BITMAPINFO> bmi, int bitCount);
};

// Pushes transform, then rasterizes rectangle with specified bounds and brush
// to bitmap.
BitmapSource ^ CreateBitmapAndFillWithBrush(
    int width,              // rasterization bitmap size
    int height,
    Brush ^ brush,
    Rect bounds,            // geometry bounds in local space
    Transform^ transform,   // DrawingContext transform
    PixelFormat pixelFormat
    );

#endif
