// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
ref class PaletteSorter
{
public:
    PaletteSorter()
    {
        Size      = 256;
    //  IndexUsed = 0;
        ColorTable = gcnew array<COLORREF>(Size);
    }

    bool AddColor(COLORREF color);
    
    int Find(COLORREF color)
    {
        int slot = Search(0, IndexUsed - 1, color);

        if (slot >= IndexUsed)
        {
            return -1;
        }
        else
        {
            return slot;
        }
    }

    bool ProcessScanline(array<BYTE>^ scan, int offset, int width, int pixelsize);

protected:
    int  Search(int start, int end, COLORREF color);

    int      Size;

public:
    array<COLORREF> ^ColorTable;
    int      IndexUsed;
};


// Return false if palette is more than 256 colors
bool PaletteSorter::AddColor(COLORREF color)
{
    int slot = Search(0, IndexUsed - 1, color);

    if (slot >= Size)
    {
        return false;
    }
    else 
    {
        if ((slot >= IndexUsed) || (ColorTable[slot] != color))
        {
            // Move all the entries above the index specified up by one slot
            for (int i = IndexUsed; i > slot; i --)
            {
                ColorTable[i] = ColorTable[i - 1];
            }

            ColorTable[slot] = color;
    
            IndexUsed ++;
        }

        return true;
    }
}


// Binary search using COLORREF
int PaletteSorter::Search(int start, int end, COLORREF color)
{
    for (;;)
    {
        Debug::Assert((start >= 0) && (end < IndexUsed));

        // Break condition
        if (start > end)
        {
            if (IndexUsed == Size) // Missing in full color table
            {
                return Size;
            }

            // If we are at the end of the table, then we might have to insert
            // in a position that's after.
            if ((start == IndexUsed - 1) && ColorTable[start] < color)
            {
                return start + 1;
            }
            else
            {
                return start;
            }
        }

        int middle = (start + end) / 2;
        
        if (ColorTable[middle] == color)
        {
            return middle;
        }
        else if (color < ColorTable[middle])
        {
            end = middle - 1;
        }
        else
        {
            start = middle + 1;
        }
    }
}

// Return false if more than 256 colors
bool PaletteSorter::ProcessScanline(array<BYTE>^ scan, int offset, int width, int pixelsize)
{
    while (width > 0)
    {
        if (! AddColor(RGB(scan[offset + 2], scan[offset + 1], scan[offset])))
        {
            return false;
        }

        offset += pixelsize;
        width --;
    }

    return true;
}

void SetQuad(interior_ptr<BITMAPINFO> bmi, int i, int r, int g, int b)
{
    bmi->bmiColors[i].rgbRed      = (Byte) r;
    bmi->bmiColors[i].rgbGreen    = (Byte) g;
    bmi->bmiColors[i].rgbBlue     = (Byte) b;
    bmi->bmiColors[i].rgbReserved = 0;
}

// Setup palette from srcSurface
void CGDIBitmap::SetupPalette(interior_ptr<BITMAPINFO> bmi, int bitCount)
{
    System::Collections::Generic::IList<Color> ^ colors = GetColorTable();

    int count = 0;

    if (colors != nullptr)
    {
        count = colors->Count;

        if (count > (1 << bitCount))
        {
            count = 1 << bitCount;
        }

        for (int cnt = 0; cnt < count; cnt ++)
        {
            SetQuad(bmi, cnt, colors[cnt].R, colors[cnt].G, colors[cnt].B);
        }

    }
    else if (m_PixelFormat == PixelFormats::BlackWhite)
    {
        count = 2;

        SetQuad(bmi, 0, 0x00, 0x00, 0x00);
        SetQuad(bmi, 1, 0xFF, 0xFF, 0xFF);
    }
    else if (m_PixelFormat == PixelFormats::Gray4)
    {
        count = 16;

        for (int i = 0; i < 16; i ++)
        {
            SetQuad(bmi, i, i * 17, i * 17, i * 17);
        }
    }
    else if (m_PixelFormat == PixelFormats::Gray8)
    {
        count = 256;

        for (int i = 0; i < 256; i ++)
        {
            SetQuad(bmi, i, i, i, i);
        }
    }
    else
    {
        Debug::Assert(false, "Unsupported format");
    }

    bmi->bmiHeader.biClrUsed      = count;
    bmi->bmiHeader.biClrImportant = count;
}


void CGDIBitmap::SetBits(interior_ptr<BITMAPINFO> bmi)
{
    // Setup BITMAPINFO to reflect the source surface bitmap pixel
    // format.  If it is 32bpp, then use palette generator/cropping
    // code below.

    UINT bitCount = m_PixelFormat.BitsPerPixel;

    // When switch was used, cyclomatic complexity of this routine was 33. Now it's under 25
    if ((bitCount == 1) || (bitCount == 4) || (bitCount == 8))
    {
        SetupPalette(bmi, bitCount);
    }
    else if (bitCount == 16)
    {
        // Specify 5-5-5 or 5-6-5 16bpp bitfields
        bmi->bmiHeader.biCompression = BI_BITFIELDS;

        interior_ptr<DWORD> mask = (interior_ptr<DWORD>) & bmi->bmiColors[0];
        
        if (m_PixelFormat == PixelFormats::Bgr555)
        {
            mask[0] = 0x7C00;
            mask[1] = 0x03E0;
            mask[2] = 0x001F;
        }
        else if (m_PixelFormat == PixelFormats::Bgr565)
        {
            mask[0] = 0xF800;
            mask[1] = 0x07E0;
            mask[2] = 0x001F;
        }
        else
        {
            m_Buffer = nullptr;
        }
    }
    else if (bitCount >= 24)
    {
        if (m_pSorter != nullptr)
        {
            ColorReduction();
        }
    }
    else
    {
        Debug::Assert(false, "Unexpected bitcount");
        m_Buffer = nullptr;
    }
}


// Convert to indexed bitmap
HRESULT CGDIBitmap::ColorReduction()
{
    Debug::Assert(m_pSorter != nullptr);
    Debug::Assert(m_pSorter->IndexUsed <= 256);

    int bpp = 8;
    
    if (m_pSorter->IndexUsed <= 2)
    {
        bpp = 1;
    }
    else if (m_pSorter->IndexUsed <= 16)
    {
        bpp = 4;
    }

    // new buffer is top-down
    int stride = GetDIBStride(m_Width, bpp);
    array<Byte> ^ nuBuffer = gcnew array<Byte>(stride * m_Height);

    int pixelSize = m_PixelFormat.BitsPerPixel / 8;
    
    for (int h = 0; h < m_Height; h ++)
    {
        interior_ptr<BYTE> src = & m_Buffer[m_Offset + m_Stride * h];
        interior_ptr<BYTE> dst = & nuBuffer[           stride   * h];

        if (bpp == 1)
        {
            Byte mask = 0x80;

            for (int w = 0; w < m_Width; w ++)
            {
                int index = m_pSorter->Find(RGB(src[2], src[1], src[0]));

                Debug::Assert(index < 2);
                
                dst[0] |= mask * index;
        
                mask >>= 1;

                if (mask == 0)
                {
                    mask = 0x80;
                    dst ++;
                }

                src += pixelSize;
            }
        }
        else if (bpp == 4)
        {
            for (int w = 0; w < m_Width; w ++)
            {
                int index = m_pSorter->Find(RGB(src[2], src[1], src[0]));

                Debug::Assert(index < 16);
            
                if (w & 1)
                {
                    dst[0] |= (Byte) index;
                    dst ++;
                }
                else
                {
                    dst[0] = (Byte) (index * 16);
                }

                src += pixelSize;
            }
        }
        else
        {
            Debug::Assert(bpp == 8);

            for (int w = 0; w < m_Width; w ++)
            {
                int index = m_pSorter->Find(RGB(src[2], src[1], src[0]));

                Debug::Assert(index < 256);
            
                dst[0] = (BYTE) index;
    
                src += pixelSize;
                dst ++;
            }
        }
    }
    
    interior_ptr<BITMAPINFO> bmi  = (interior_ptr<BITMAPINFO>) & m_Bi[0];

    bmi->bmiHeader.biBitCount = (WORD) bpp;
    
    // new buffer is top-down
    m_Buffer = nuBuffer;
    m_Offset = 0;
    m_Stride = stride;
    
    for (int i = 0; i < m_pSorter->IndexUsed; i ++)
    {
        COLORREF c = m_pSorter->ColorTable[i];

        SetQuad(bmi, i, GetRValue(c), GetGValue(c), GetBValue(c));
    }

    return S_OK;
}


#ifdef CROPIMAGE

//
// TODO: Currently unused; need to check stride semantics before enabling.
// This code can be used to crop images before sending to GDI to save spool
// file space.
//

value class CCropper
{
public:

    CGDIBitmap m_Result;

    CCropper(const CGDIBitmap ^ pSrc, Int32Rect & rect);
};


CCropper::CCropper(const CGDIBitmap ^ pSrc, Int32Rect & rect)
{
    m_Result = * pSrc;

    if ((rect.Y > 0) && ((int) m_Result.m_Height >= rect.Y)) // if first row does not start at 0, move to it
    {
        m_Result.m_Offset += m_Result.m_Stride * rect.Y;
        m_Result.m_Height -= rect.Y;
        rect.Y = 0;
    }

    if (rect.Height < (int) m_Result.m_Height) // if less rows are needed, reduce height
    {
        m_Result.m_Height = rect.Height;
    }

    int pixelsize = m_Result.m_PixelFormat.BitsPerPixel; // pixel size in bits
    int group     = 8;

    group = max(1, pixelsize / 8);           // number of pixels grouped on BYTE boundary

    if (rect.X > group) // if first there are group of pixels to be removed on the left
    {
        int cropleft = rect.X / group * group;

        if (m_Result.m_Width >= cropleft)
        {
            m_Result.m_Width  -= cropleft;
            m_Result.m_Offset += cropleft * pixelsize / 8;
            rect.X -= cropleft;
        }
    }

    if (rect.Width < (int) m_Result.m_Width) // if less columns are needed, reduce width
    {
        m_Result.m_Width = rect.Width;
    }
}


HRESULT CGDIBitmap::CopyCropImage()
{
    interior_ptr<BITMAPINFO> bmi = (interior_ptr<BITMAPINFO>) & m_Bi[0];

    int BmpStride = GetDIBStride(m_Width, bmi->bmiHeader.biBitCount);

    if (BmpStride == m_Stride)
    {
        return S_OK;
    }
        
    // GDI DIB is DWORD aligned, bottom-up by default
    // If stride does not match source bitmap stride, we need to make a copy

    // Optimization: copy is also made if source rectangle is smaller

    Int32Rect rect;

    rect.X      = 0;
    rect.Y      = 0;
    rect.Width  = m_Width;
    rect.Height = m_Height;

    CCropper cropper(*this, rect);

    // if ((- BmpStride) != cropper->Stride)
    {
        array<Byte, 1>^ NewBits = gcnew array<Byte>(BmpStride * cropper.m_Result.m_Height);

        if (NewBits != nullptr)
        {
            interior_ptr<BYTE> origBits = & cropper.m_Result.m_Buffer[cropper.m_Result.m_Offset];

            interior_ptr<BYTE> destBits = & NewBits[0];

            destBits += (cropper.m_Result.m_Height - 1) * BmpStride;

            for (int y = 0; y < cropper.m_Result.m_Height; y ++)
            {
                memcpy(destBits, origBits, cropper.m_Result.m_Width * bmi->bmiHeader.biBitCount / 8);
                origBits += cropper.m_Result.m_Stride;
                destBits -= BmpStride;
            }

            m_Buffer   = NewBits;
            m_Offset   = 0;

            bmi->bmiHeader.biWidth  = cropper.m_Result.m_Width;
            bmi->bmiHeader.biHeight = cropper.m_Result.m_Height;

            m_Width  = rect.Width;
            m_Height = rect.Height;
        }
    }

    return S_OK;
}

#endif

// ceil(x/y)
int DivideAndRoundUp(int x, int y)
{
    return (x + y - 1) / y;
}

value class BandIterator
{
public:

    BandIterator(Int32Rect rect, int count)
    {
        _rect = rect;
        _count = count;

        _band = rect;

        Reset();
    }

private:
    Int32Rect _rect;
    int _count;	        // band count

    // current band
    Int32Rect _band;

    int _index;
    int _prevBottom;    // previous band bottom, used to adjust for error

public:
    // Gets the current band's rectangle.
    Int32Rect GetCurrent()
    {
        Debug::Assert(_index < _count && _index >= 0);

        return _band;
    }

    // Moves to the next band.
    bool MoveNext()
    {
        Debug::Assert(_index < _count && _index >= -1);

        _index++;

        if (_index < _count)
        {
            // bandY = _rect.Y + (_rect.Height / _count) * _index;
            _band.Y = _rect.Y + DivideAndRoundUp(_index * _rect.Height, _count);
            _band.Height = DivideAndRoundUp(_rect.Height, _count);

            // adjust for rounding error between this and previous band
            int error = _band.Y - _prevBottom;
            _band.Y -= error;

            if (_index == (_count - 1))
            {
                // adjust for last band to fill remaining rect
                _band.Height = _rect.Height - (_band.Y - _rect.Y);
            }
            else
            {
                _band.Height += error;
            }

            _prevBottom = _band.Y + _band.Height;

            return true;
        }
        else
        {
            // no more bands
            return false;
        }

        //[unreachable] return _index < _count;
    }

    void Reset()
    {
        _index = -1;
        _prevBottom = _rect.Y;
    }
};

HRESULT
CGDIBitmap::StretchBlt(CGDIDevice ^ pDevice, const Int32Rect & dst, bool flipHoriz, bool flipVert)
{
    Debug::Assert(pDevice != nullptr);
    Debug::Assert(IsValid());

    if ((m_Height > 0) && (m_Width > 0))
    {
        Int32Rect dest = dst;
        Int32Rect src(0, 0, m_Width, m_Height);

        // Some PCL print drivers refuse to honor negative destination origins,
        // so adjust the source and destination rectangles, if needed.
        {
            POINT dcOrigin;

            pDevice->GetDCOrgEx(dcOrigin);

            int xDest = dest.X + dcOrigin.x;

            if (xDest < 0)
            {
               int  srcXDelta = (xDest * m_Width + dest.Width / 2) / dest.Width;

               src.X -= srcXDelta;
               src.Width += srcXDelta;
               dest.Width += xDest;
               dest.X = 0;
            }

            int yDest = dest.Y + dcOrigin.y;

            if (yDest < 0)
            {
               int  srcYDelta = (yDest * m_Height + dest.Height / 2) / dest.Height;

               src.Y -= srcYDelta;
               src.Height += srcYDelta;
               dest.Height += yDest;
               dest.Y = 0;
            }

            // Destination rectangle for printing is normalized to have
            // positive width and height, so check for out of positive
            // bounds case.
            if ((dest.X + dest.Width) <= 0 ||
                (dest.Y + dest.Height) <= 0)
            {
                return S_OK;
            }
        }

        // Don't do anything if we don't have to
        if (dest.Height == 0 || dest.Width == 0)
        {
            return S_OK;
        }

        interior_ptr<BITMAPINFO> Bmi = (interior_ptr<BITMAPINFO>) & m_Bi[0];

        if (flipHoriz)
        {
            dest.X += dest.Width;
            dest.Width *= -1;
        }

        if (flipVert)
        {
            dest.Y += dest.Height;
            dest.Height *= -1;
        }

        //
        // Render bitmap with banding. For each band, also pass GDI only the relevant bits to cut down
        // on bitmap size.
        //
        HRESULT hr = S_OK;

        int pixelLimit = RasterizeBandPixelLimit;

        int bandCount = DivideAndRoundUp(src.Width * src.Height, pixelLimit);

        BandIterator sourceBand(src, bandCount);
        BandIterator destBand(dest, bandCount);

        while (SUCCEEDED(hr) && sourceBand.MoveNext() && destBand.MoveNext())
        {
            Int32Rect sourceBandRect = sourceBand.GetCurrent();
            Int32Rect destBandRect = destBand.GetCurrent();

            // select the band from DIB, this implies source Y is zero
            interior_ptr<BYTE> bits = &m_Buffer[m_Offset + m_Stride * sourceBandRect.Y];

            if (m_Stride < 0)
            {
                // Buffer is bottom-up, which corresponds to positive height.
                Bmi->bmiHeader.biHeight = sourceBandRect.Height;
            }
            else
            {
                // Buffer is top-down, which corresponds to negative height.
                Bmi->bmiHeader.biHeight = -sourceBandRect.Height;
            }

            hr = pDevice->StretchDIBits(
                destBandRect.X, destBandRect.Y, destBandRect.Width, destBandRect.Height,
                sourceBandRect.X, 0, sourceBandRect.Width, sourceBandRect.Height,
                bits, Bmi
                );
        }

        return hr;
    }

    return S_OK;
}


HRESULT CGDIBitmap::Load(BitmapSource ^ pBitmap, array<Byte>^ buffer, PixelFormat LoadFormat)
{
    Debug::Assert(pBitmap != nullptr);

    m_pBitmap     = pBitmap;

    // don't use ImageSource.Width or Height, since they're in measure units. we want pixels
    m_Width       = (int) pBitmap->PixelWidth;
    m_Height      = (int) pBitmap->PixelHeight;
    m_PixelFormat = LoadFormat;
    m_Stride      = GetDIBStride(m_Width, m_PixelFormat.BitsPerPixel);
    m_Offset      = 0;

    if (buffer != nullptr)
    {
        // buffer is top-down bitmap
        m_Buffer = buffer;
    }
    else
    {
        m_Buffer      = gcnew array<Byte>(m_Stride * m_Height);

        BitmapSource ^ source = pBitmap;
        
        if (m_PixelFormat != source->Format)
        {    
            FormatConvertedBitmap ^ converter = gcnew FormatConvertedBitmap();
            converter->BeginInit();
            converter->Source = pBitmap;
            converter->DestinationFormat = m_PixelFormat;

            if (m_PixelFormat.Palettized)
            {
                converter->DestinationPalette = pBitmap->Palette;
            }

            converter->EndInit();
            
            source = converter;
        }
        
        Int32Rect rect(0, 0, m_Width, 1); // Single scanline
    
        // copy to top-down buffer
        for (int y = 0; y < m_Height; y ++)
        {
            int offset = y * m_Stride;

            source->CriticalCopyPixels(rect, m_Buffer, m_Stride, offset);

            rect.Y ++;
        }
    }

    int bpp = m_PixelFormat.BitsPerPixel;

    // If bitmap is large enough, consider convert to indexed formats
    if (bpp >= 24)
    {
        int orgSize = GetDIBStride(m_Width, bpp) * m_Height;
        int newSize = GetDIBStride(m_Width, 8  ) * m_Height;

        if ((int)((newSize + 256 * sizeof(RGBQUAD))) < orgSize)
        {
            m_pSorter = gcnew PaletteSorter();

            for (int y = 0; y < m_Height; y ++)
            {
                int offset = m_Offset + y * m_Stride;

                if (m_pSorter != nullptr)
                {
                    // m_Buffer is passed to m_pSorter, which is marked as SecurityCritical
                    if (! m_pSorter->ProcessScanline(m_Buffer, offset, m_Width, bpp / 8))
                    {
                        // Get rid of palette sorter if more than 256 colors
                        m_pSorter = nullptr;
                    }
                }
            }
        }
    }

    m_Bi = gcnew array<BYTE>(sizeof(BITMAPINFOHEADER) + 256 * sizeof(RGBQUAD));

    interior_ptr<BITMAPINFO> bmi = (interior_ptr<BITMAPINFO>) & m_Bi[0];

    bmi->bmiHeader.biSize        = sizeof(BITMAPINFOHEADER);
    bmi->bmiHeader.biWidth       = m_Width;
    bmi->bmiHeader.biHeight      = -m_Height;   // top-down bitmaps, use negative height
    bmi->bmiHeader.biPlanes      = 1;
    bmi->bmiHeader.biBitCount    = (WORD) bpp;
    bmi->bmiHeader.biCompression = BI_RGB;

    SetBits(bmi);

    return S_OK;
}


System::Collections::Generic::IList<Color> ^ CGDIBitmap::GetColorTable()
{
    if (m_pBitmap)
    {
        BitmapPalette ^ palette = m_pBitmap->Palette;

        if (palette != nullptr)
        {
            return palette->Colors;
        }
    }

    return nullptr;
}


ref class GeometryVisual : public DrawingVisual
{
public:
    GeometryVisual(int bitmapWidth, int bitmapHeight, Brush^ brush, Rect rect, System::Windows::Media::Transform^ transform)
    {
        DrawingContext ^ ctx = RenderOpen();

        //
        // Fix bug 1487589: By this point no transparency should remain, and if brush content
        // doesn't completely fill the bitmap, the remaining transparent areas should be
        // clipped away.
        //
        // Any visible transparent areas are bugs and will show up as black, thus we fill
        // the bitmap with white to lessen the impact of those bugs.
        //
        ctx->DrawRectangle(Brushes::White, nullptr, Rect(0, 0, bitmapWidth, bitmapHeight));

        ctx->PushTransform(transform);
        ctx->DrawRectangle(brush, nullptr, rect);
        ctx->Pop();

        ctx->Close();
    }
};


BitmapSource ^ CreateBitmapAndFillWithBrush(
    int          nWidth,
    int          nHeight,
    Brush      ^ brush,
    Rect         bounds,
    Transform  ^ transform,
    PixelFormat  pixelFormat
    )
{
    Debug::Assert(brush != nullptr);
    Debug::Assert((nWidth > 0) && (nHeight > 0));

    RenderTargetBitmap ^pBitmap = gcnew RenderTargetBitmap(nWidth, nHeight, 96, 96, pixelFormat);

    Visual ^ visual = gcnew GeometryVisual(nWidth, nHeight, brush, bounds, transform);

    pBitmap->Render(visual);

    return pBitmap;
}
