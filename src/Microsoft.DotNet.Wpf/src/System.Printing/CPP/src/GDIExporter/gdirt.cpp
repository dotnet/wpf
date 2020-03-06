// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

// Helper functions for CGDIRenderTarget::FillLinearGradient.
System::Collections::SortedList^ GetSortedGradientStops(GradientStopCollection^ stopCollection);

Matrix GetGradientWorldToXTransform(LinearGradientBrush^ brush);

void GetLinearGradientAxisAligned(IN LinearGradientBrush^ brush, [Out] bool% isVertical, [Out] bool% isHorizontal);

void GenerateGradientBandVertices(
    IN array<CNativeMethods::TriVertex>^ vertices,
    IN OUT int% vertexOffset,
    IN Matrix% transform,
    IN double x,
    IN double top,
    IN double bottom,
    IN Color% color
    );

void GenerateGradientBandTriangles(
    IN array<unsigned long>^ indices,
    IN OUT int% indexOffset,
    int vertexOffset
    );

GdiSafeHandle^ CGDIDevice::CacheMatch(const interior_ptr<Byte> pData, int size)
{
    if (m_Cache == nullptr)
    {
        return nullptr;
    }

    for (int i = 0; i < m_Cache->Length; i ++)
    {
        if (m_Cache[i] != nullptr)
        {
            GdiSafeHandle^ result = m_Cache[i]->Match(pData, size);

            if (result != nullptr)
            {
                return result;
            }
        }
    }

    return nullptr;
}


void CGDIDevice::CacheObject(interior_ptr<Byte> pData, int size, GdiSafeHandle^ handle)
{
    if (m_Cache != nullptr)
    {
        // Find an empty slot
        while (m_Cache[m_CacheFirst] != nullptr)
        {
            GdiSafeHandle ^old = m_Cache[m_CacheFirst]->Handle;

            if ((old != m_lastFont) && (old != m_lastBrush) && (old != m_lastPen))
            {
                // Release corresponding GDI object ASAP if it's not needed to reduce active GDI object count
            
                old->Close();
                m_Cache[m_CacheFirst] = nullptr;
                break;
            }

            // Try next one
            m_CacheFirst = (m_CacheFirst + 1) % m_Cache->Length;
        }

        m_Cache[m_CacheFirst] = gcnew CachedGDIObject(pData, size, handle);
        m_CacheFirst = (m_CacheFirst + 1) % m_Cache->Length;
    }
}

void CGDIRenderTarget::ThrowOnFailure(HRESULT hr)
{
    if (SUCCEEDED(hr) ||
        hr == HRESULT_FROM_WIN32(ERROR_SUCCESS) ||
        hr == MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, ERROR_SUCCESS))
    {
        // hr is success; don't throw. ERROR_SUCCESS can come in two forms due to
        // Marshal.GetHRForLastWin32Error differing from HRESULT_FROM_WIN32.
    }
    else if (hr == HRESULT_FROM_WIN32(ERROR_CANCELLED) ||
        hr == HRESULT_FROM_WIN32(ERROR_PRINT_CANCELLED))
    {
        throw gcnew PrintingCanceledException(
            hr,
            "PrintSystemException.PrintingCancelled.Generic"
            );
    }
    else
    {
        throw gcnew PrintSystemException(hr, "PrintSystemException.PrintingCancelled.Generic");
    }
}

HRESULT CGDIRenderTarget::Initialize()
{
    HRESULT hr = S_OK;

    hr = InitializeDevice();

    if (SUCCEEDED(hr))
    {
        m_DeviceTransform = Matrix::Identity;
        m_DeviceTransform.Scale(m_nDpiX / 96.0f, m_nDpiY / 96.0f);

        // Avalon coordinates are relative to physical top left corner of the paper, while GDI
        // coordinates are relative to printable region. Subtract by PhysicalOffsetX, PhysicalOffsetY.

        m_DeviceTransform.Translate(
            - CNativeMethods::GetDeviceCaps(m_hDC, PHYSICALOFFSETX),
            - CNativeMethods::GetDeviceCaps(m_hDC, PHYSICALOFFSETY)
            );

        // Page dimensions filled in StartPage.
        m_nWidth = m_nHeight = 0;

        // Caching 32 GDI objects
        m_Cache      = gcnew array<CachedGDIObject^>(32);
        m_CacheFirst = 0;
    }

    m_state = gcnew System::Collections::Stack();

    m_transform = Matrix::Identity;

    m_clipLevel = 0;

    m_cachedUnstyledFontCharsets = gcnew System::Collections::Hashtable();

    return hr;
}


array<Byte>^ GetRawBitmap(BitmapSource ^ pIBitmap, BitmapCodecInfo ^ codec)
{
    MemoryStream^ stream = gcnew MemoryStream(0);

    BitmapEncoder ^ encoder = BitmapEncoder::Create(codec->ContainerFormat);

    BitmapFrame ^ frame = dynamic_cast<BitmapFrame ^>(pIBitmap);

    if (frame != nullptr)
    {
        encoder->Frames->Add(frame);
    }
    else
    {
        encoder->Frames->Add(BitmapFrame::Create(pIBitmap));
    }

    encoder->Save(stream);

    return stream->GetBuffer();
}


BitmapCodecInfo^ GetBitmapCodec(BitmapSource ^ pIBitmap)
{
    if (pIBitmap != nullptr)
    {
        BitmapFrame^ bmp = dynamic_cast<BitmapFrame ^>(pIBitmap);

        if (bmp != nullptr)
        {
            return bmp->Decoder->CodecInfo;
        }
    }

    return nullptr;
}


/**************************************************************************
*
* Function Description:
*
* DrawBitmap_PassThrough
*     Pass PNG/JPEG image to device if the source rectangle to destination
*     transformation is simply a 90, 180, or 270 rotation, and the driver
*     supports JPEG/PNG passthrough
*
* Created:
*
*   5/3/2003 fyuan Created
*
**************************************************************************/

HRESULT CGDIRenderTarget::DrawBitmap_PassThrough(
    BitmapSource     ^ pIBitmap,
    Int32Rect        % rcDstBounds,
    int                nImageWidth,
    int                nImageHeight
    )
{
    Debug::Assert(pIBitmap != nullptr);

    HRESULT hr = E_NOTIMPL;

    bool bJPEG = false;
    bool bPNG  = false;

    BitmapCodecInfo ^ codec = nullptr;

    if (IsTranslateOrScale(m_transform) && (GetRotation(m_transform) != MatrixRotateByOther))
    {
        bJPEG = (GetCaps() & CAP_JPGPassthrough) != 0;
        bPNG  = (GetCaps() & CAP_PNGPassthrough) != 0;

        if (bJPEG || bPNG)
        {
            codec = GetBitmapCodec(pIBitmap);
            if (codec != nullptr)
            {
                String ^ mime = codec->MimeTypes;


                if (bJPEG && (mime->IndexOf("image/jpeg", StringComparison::OrdinalIgnoreCase) != -1))
                {
                    hr   = S_OK;
                    bPNG = false;
                }
                else if (bPNG && (mime->IndexOf("image/png", StringComparison::OrdinalIgnoreCase) != -1))
                {
                    hr    = S_OK;
                    bJPEG = false;
                }
            }
        }
    }

    if (SUCCEEDED(hr))
    {
        hr = E_NOTIMPL;

        array<Byte>^ raw = GetRawBitmap(pIBitmap, codec);

        if (raw != nullptr)
        {
            unsigned long result = 0;

            int nDataSize = raw->Length;

            pin_ptr<Byte> rawPin = &raw[0];

            // Call escape to determine if this particular image is supported
            if ((CNativeMethods::ExtEscape(
                      m_hDC,
                      bJPEG ? CHECKJPEGFORMAT : CHECKPNGFORMAT,
                      nDataSize,
                      (void*)rawPin,
                      sizeof(result),
                      &result) > 0) && (result > 0))
            {
                BITMAPINFO bmi;

                memset(&bmi, 0, sizeof(bmi));

                bmi.bmiHeader.biSize        = sizeof(BITMAPINFOHEADER);
                bmi.bmiHeader.biWidth       = nImageWidth;
                bmi.bmiHeader.biHeight      = - nImageHeight; // top-down image
                bmi.bmiHeader.biPlanes      = 1;
                bmi.bmiHeader.biBitCount    = 0;
                bmi.bmiHeader.biCompression = bJPEG ? BI_JPEG : BI_PNG;
                bmi.bmiHeader.biSizeImage   = nDataSize;

                hr = StretchDIBits(
                        rcDstBounds.X,
                        rcDstBounds.Y,
                        rcDstBounds.Width,
                        rcDstBounds.Height,
                        0,
                        0,
                        nImageWidth,
                        nImageHeight,
                        & raw[0],
                        & bmi
                        );
            }
        }
    }

    return hr;
}


PixelFormat GetLoadFormat(PixelFormat format)
{
    if (format == PixelFormats::Indexed2)
    {
        return PixelFormats::Indexed4;
    }

    if (format == PixelFormats::Gray2)
    {
        return PixelFormats::Gray4;
    }

    if ((format == PixelFormats::Indexed1) ||
        (format == PixelFormats::Indexed4) ||
        (format == PixelFormats::Indexed8) ||
        (format == PixelFormats::BlackWhite) ||
        (format == PixelFormats::Gray4) ||
        (format == PixelFormats::Gray8) ||
        (format == PixelFormats::Bgr555) ||
        (format == PixelFormats::Bgr565)
        )
    {
        // We know how to handle those once decoded
        return format;
    }

    if ((format == PixelFormats::Indexed1) ||
        (format == PixelFormats::Indexed2) ||
        (format == PixelFormats::Indexed4) ||
        (format == PixelFormats::Indexed8) ||
        (format == PixelFormats::BlackWhite) ||
        (format == PixelFormats::Gray2) ||
        (format == PixelFormats::Gray4) ||
        (format == PixelFormats::Gray8) ||
        (format == PixelFormats::Bgr555) ||
        (format == PixelFormats::Bgr565)
        )
    {
        // We know how to handle those once decoded
        return format;
    }

    if ((format == PixelFormats::Gray16) ||
        (format == PixelFormats::Gray32Float)
        )
    {
        // Only 8-bit per channel for GDI
        return PixelFormats::Gray8;
    }

    // Everything else goes to 24-bpp RGB first
    return PixelFormats::Bgr24;
}


/**************************************************************************
*
* Function Description:
*
*   DrawBitmap
*
* Created:
*
*   7/7/2004 t-bnguy
*       Added optional rectangle destination rectDest parameter.
*
*   6/10/2002 fyuan
*      Created it.
**************************************************************************/

HRESULT  CGDIRenderTarget::DrawBitmap(BitmapSource ^ pImage, array<Byte>^ buffer, Rect rectDest)
{
    Debug::Assert(pImage != nullptr);
    Debug::Assert(!rectDest.IsEmpty);

    // Compute destination bounding rectangle in measure units, then transform to device units.
    // Afterwards clip.

    Rect rcDst;
    TransformBounds(
        m_transform,
        rectDest.X,
        rectDest.Y,
        rectDest.X + rectDest.Width,
        rectDest.Y + rectDest.Height,
        rcDst
        );

    Int32Rect rcDstBounds;

    if (FAILED(RectFToGDIRect(rcDst, rcDstBounds)))
    {
        Debug::Write("DrawBitmap skipping out of bound image\r\n");

        return S_OK;
    }

    // Quit if the drawbounds are outside the clip region
//  if (Invisible(m_clip, & rcDstBounds))
//  {
//      return S_OK;
//  }

    HRESULT hr = E_NOTIMPL;
    
    if (buffer == nullptr)
    {
        // Try PNG/PNG pass through
        hr = DrawBitmap_PassThrough(
                pImage,
                rcDstBounds,
                pImage->PixelWidth,
                pImage->PixelHeight
                );
    }
    
    if (FAILED(hr) && IsTranslateOrScale(m_transform)) // translate and scale only
    {
        CGDIBitmap source;
        
        PixelFormat LoadFormat;
        
        if (buffer != nullptr)
        {
            LoadFormat = PixelFormats::Bgra32;
        }
        else
        {
            LoadFormat = GetLoadFormat(pImage->Format);
        }

        hr = source.Load(pImage, buffer, LoadFormat);

        if (SUCCEEDED(hr) && source.IsValid())
        {
            // we can handle flipping now; this produces better quality than avalon rasterization
            bool flipHoriz = m_transform.M11 < 0;
            bool flipVert  = m_transform.M22 < 0;

            hr = source.StretchBlt(this, rcDstBounds, flipHoriz, flipVert);
        }
    }

    // Convert to FillPath with a texture brush if simple case fails
    if (FAILED(hr))
    {
        StreamGeometry^ shape = gcnew StreamGeometry();

        {
            StreamGeometryContext^ context = shape->Open();

            context->BeginFigure(rectDest.TopLeft, /*filled=*/true, /*closed=*/true);
            context->LineTo(rectDest.TopRight, /*stroked=*/false, /*smoothJoin=*/false);
            context->LineTo(rectDest.BottomRight, /*stroked=*/false, /*smoothJoin=*/false);
            context->LineTo(rectDest.BottomLeft, /*stroked=*/false, /*smoothJoin=*/false);

            context->Close();
        }
        
        // If decoded buffer is provided, it could have modified bits, use it.
        // TODO: If image is decoded into buffer, but not modified, then use original pImage is faster.
        if (buffer != nullptr)
        {
            int width  = pImage->PixelWidth;
            int height = pImage->PixelHeight;
            
            pImage = BitmapSource::Create(width, height, pImage->DpiX, pImage->DpiY, PixelFormats::Bgra32, nullptr, buffer, width * 4);
        }
                    
        System::Windows::Media::ImageBrush ^ pBrush = gcnew ImageBrush(pImage);

        if (pBrush != nullptr)
        {
            pBrush->ViewportUnits = BrushMappingMode::Absolute;
            pBrush->Viewport      = rectDest;

            GeometryProxy shapeProxy(shape);
            hr = FillPath(shapeProxy, pBrush);
        }
        else
        {
            hr = E_OUTOFMEMORY;
        }
    }

    return hr;
}


/**************************************************************************
*
* Function Description:
*
*   GetDeviceTransform
*   Return a matrix representing the device transform including the DPI
*   adjustment.
*
* Created:
*
*   6/10/2002 fyuan
*      Created it.
*
**************************************************************************/


// Check if a matrix is translation with the same x/y scaling
bool UniformScale(Matrix mat)
{
    return IsZero(mat.M12) &&
           IsZero(mat.M21) &&
           IsZero(Math::Abs(mat.M11) - Math::Abs(mat.M22));
}

/**************************************************************************
*
* Function Description:
*
*   PushClipProxy
*   Pushes clipping represented by GeometryProxy. Geometry may be converted
*   to PathGeometry, hence the usage of GeometryProxy so that the caller
*   may also reuse this expensive conversion if needed.
*
* Created:
*
*   11/15/2005 bnguyen
*      Created it.
*
**************************************************************************/

void CGDIRenderTarget::PushClipProxy(GeometryProxy% geometry)
{
    if (!HasDC)
    {
        return;
    }

    Debug::Assert(m_startPage, "StartPage has not been called yet (PushClip).");
    
    // remember devicetransform goes from avalon -> gdi coordinate space, hence the negative
    int physicalOffsetX =  -(int) m_DeviceTransform.OffsetX;
    int physicalOffsetY =  -(int) m_DeviceTransform.OffsetY;

    // Check for infinite clip area (wrt to paper)
    
    int fullyContain = 1; // may be
    
    if (IsTranslateOrScale(m_transform))
    {
        Point tl, br;
        
        tl.X = (physicalOffsetX - m_transform.OffsetX) / m_transform.M11;
        tl.Y = (physicalOffsetY - m_transform.OffsetY) / m_transform.M22;

        br.X = (physicalOffsetX + m_nWidth  - m_transform.OffsetX) / m_transform.M11;
        br.Y = (physicalOffsetY + m_nHeight - m_transform.OffsetY) / m_transform.M22;

        Rect bounds = geometry.GetBounds(nullptr);

        if ((bounds.Left <= tl.X) &&
            (bounds.Top  <= tl.Y) &&
            (bounds.Right >= br.X) &&
            (bounds.Bottom >= br.Y) &&
            geometry.IsRectangle())
        {
            fullyContain = 2;
        }
        // Clipping is withing page bounds
        else if ((bounds.Left   > tl.X) && 
            (bounds.Top    > tl.Y) &&
            (bounds.Right  < br.X) && 
            (bounds.Bottom < br.Y))
        {
            fullyContain = 0; // Avoid expensive ContainsWithDetail call
        }
    }
    
    if (fullyContain == 1)
    {
        // construct page geometry. shrink rect slightly so that if clip region is exactly rectangle, we'll get
        // FullyContains below.
        Rect pageRect(physicalOffsetX, physicalOffsetY, m_nWidth, m_nHeight);
        pageRect.Inflate(-0.1, -0.1);
        RectangleGeometry^ pageGeometry = gcnew RectangleGeometry(pageRect);

        Matrix mat = m_transform;

        if (mat.HasInverse)
        {
            mat.Invert();

            // Transform page bounds to clip coordinate space
            pageGeometry->Transform = gcnew MatrixTransform(mat);

            // see if page inside clip
            IntersectionDetail isect = geometry.Geometry->FillContainsWithDetail(pageGeometry);

            if (isect == IntersectionDetail::FullyContains)
            {
                fullyContain = 2;
            }
        }
    }

    if (fullyContain != 2)
    {
        // set new clip
        CGDIPath ^ path = CGDIPath::CreateFillPath(geometry, m_transform);

        Debug::Assert(path->IsValid(), "Invalid CGDIPath");

        if (m_clipLevel == 0)
        {
            path->SelectClip(this, RGN_COPY);
        }
        else
        {
            int errCode = CNativeMethods::SaveDC(m_hDC);    // ROBERTAN
            Debug::Assert(errCode != 0, "SaveDC failed");
            
            path->SelectClip(this, RGN_AND);
        }

        m_clipLevel ++;

        m_state->Push(1);
    }
    else
    {
        m_state->Push(2);
    }
}

// Called to render pieces of the original stroke geometry.
// Return a failure HRESULT to prematurely stop the splitting.
delegate ::HRESULT RenderStrokePieceCallback(GeometryProxy% geometry, Pen^ pen, Brush^ strokeBrush);

// Splits stroke Geometry into multiple Geometry pieces and passes them to a callback.
ref class StrokeGeometrySplitter : public CapacityStreamGeometryContext
{
// Constructors
private:
    StrokeGeometrySplitter(
        Matrix transform,
        RenderStrokePieceCallback^ callback,
        Pen^ callbackPen,
        Brush^ callbackBrush
        )
    {
        Debug::Assert(callback != nullptr);

        _transform = transform;

        _callback = callback;
        _callbackPen = callbackPen;
        _callbackBrush = callbackBrush;
    }

public:
    //
    // Splits Geometry into multiple pieces that have approximately at most MaximumPieceRawPointCount GDI points.
    // Returns S_OK or the first failure result returned from the callback.
    //
    static HRESULT RenderSubstrokes(
        GeometryProxy% geometry,
        RenderStrokePieceCallback^ callback,
        Pen^ callbackPen,
        Brush^ callbackBrush
        )
    {
        Geometry::PathGeometryData% geometryData = geometry.GetGeometryData();
        Matrix transform =  System::Windows::Media::Composition::CompositionResourceManager::MilMatrix3x2DToMatrix(geometryData.Matrix);

        StrokeGeometrySplitter^ splitter = gcnew StrokeGeometrySplitter(transform, callback, callbackPen, callbackBrush);
        PathGeometry::ParsePathGeometryData(geometryData, splitter);
        splitter->CloseGeometry();

        return splitter->_callbackResult;
    }

// Constants
public:
    // maximum raw GDI point count for each piece
    static const int MaximumPieceRawPointCount = 512;

// Private Fields
private:
    Matrix _transform;                  // transform to apply to geometry before returning to callback

    RenderStrokePieceCallback^ _callback;
    Pen^ _callbackPen;
    Brush^ _callbackBrush;

    HRESULT _callbackResult;            // callback result

    // current geometry piece
    StreamGeometry^ _geometry;
    StreamGeometryContext^ _context;

    int _rawPointCount;                 // raw GDI point count for current piece
    Point _lastPoint;                   // last point added

    // figure state
    bool _figureClosed;
    Point _figureStartPoint;            // piece start point

// Private Methods
private:
    void CloseGeometry()
    {
        EndFigure();
        EndPiece();
    }

    // Starts a new geometry piece, ending the previous one if it existed.
    void BeginPiece(Point pieceStartPoint)
    {
        if (SUCCEEDED(_callbackResult))
        {
            EndPiece();

            // Start new piece. We always manually close figures since a split might occur
            // in the middle of a figure.
            _geometry = gcnew StreamGeometry();
            _context = _geometry->Open();

            _context->BeginFigure(pieceStartPoint, /*isFilled=*/false, /*isClosed=*/false);
            _rawPointCount = 1;
        }
    }

    // Ends current geometry piece and passes it to callback.
    void EndPiece()
    {
        if (_geometry != nullptr)
        {
            // close geometry and apply transform
            _context->Close();
            _context = nullptr;

            if (_geometry->Transform == nullptr)
            {
                _geometry->Transform = gcnew MatrixTransform(_transform);
            }
            else
            {
                _geometry->Transform = gcnew MatrixTransform(_geometry->Transform->Value * _transform);
            }

            // pass geometry to callback
            _callbackResult = _callback(
                GeometryProxy(_geometry),
                _callbackPen,
                _callbackBrush
                );

            _geometry = nullptr;
        }
    }

    // Begins new geometry figure. May begin a new piece.
    void RealBeginFigure(Point startPoint, bool isClosed)
    {
        if (SUCCEEDED(_callbackResult))
        {
            EndFigure();

            if (ShouldStartPiece())
            {
                // no piece currently exists or we exceed the point count for a piece
                BeginPiece(startPoint);
            }
            else
            {
                // start new figure in same piece, always manually close
                _context->BeginFigure(startPoint, /*isFilled=*/false, /*isClosed=*/false);
                AddRawPoints(1);
            }

            _figureClosed = isClosed;
            _figureStartPoint = startPoint;
        }
    }

    // Ends current geometry figure.
    void EndFigure()
    {
        if (SUCCEEDED(_callbackResult))
        {
            // manually close previous figure
            if (_figureClosed)
            {
                _context->LineTo(_figureStartPoint, /*isStroked=*/true, /*isSmoothJoin=*/false);
                AddRawPoints(1);
            }
        }
    }

    // Begins a new figure segment, returning false if segment should not be processed
    // further.
    bool BeginSegment(Point lastPoint, bool isStroked)
    {
        bool result = false; // continue processing segment

        if (SUCCEEDED(_callbackResult))
        {
            if (ShouldStartPiece())
            {
                // start new piece at segment boundary
                BeginPiece(_lastPoint);
            }

            _lastPoint = lastPoint;

            if (isStroked)
            {
                // segment is visible
                result = true;
            }
            else
            {
                // unstroked segment, skip this segment
                _context->LineTo(lastPoint, /*isStroked=*/false, /*isSmoothJoin=*/false);
                AddRawPoints(1);
            }
        }

        return result;
    }

    // Determines if a new geometry piece should be started.
    bool ShouldStartPiece()
    {
        return _geometry == nullptr || _rawPointCount >= MaximumPieceRawPointCount;
    }

    // Adds raw GDI points to current piece.
    void AddRawPoints(int count)
    {
        _rawPointCount += count;
    }

    //
    // Adds segment points to current piece. Point count is translated into raw GDI point count.
    //
    // count: Number of points in the segment to add.
    //
    // groupSize: Number of points in a segment group. For example, QuadraticBezierSegment points are
    // in groups of 2.
    //
    // rawPointsPerGroup: Number of raw GDI points each group is converted into.
    //
    void AddPoints(int count, int groupSize, int rawPointsPerGroup)
    {
        AddRawPoints((count + groupSize - 1) / groupSize * rawPointsPerGroup);
    }

    //
    // Gets available number of segment points in current piece.
    //
    // Always at least groupSize available points so that at least some part of the
    // segment will fit in current piece.
    //
    int GetAvailablePointCount(int groupSize, int rawPointsPerGroup)
    {
        return (MaximumPieceRawPointCount - _rawPointCount) / rawPointsPerGroup * groupSize + groupSize;
    }

    // Gets a subset of points.
    static System::Collections::Generic::List<Point>^ GetSubPoints(System::Collections::Generic::IList<Point>^ points, int startIndex, int count)
    {
        if ((startIndex + count) > points->Count)
        {
            // get rest of points
            count = points->Count - startIndex;
        }

        System::Collections::Generic::List<Point>^ subpoints = gcnew System::Collections::Generic::List<Point>(count);

        for (int index = startIndex; index < (startIndex + count) && index < points->Count; index++)
        {
            Point point = points[index];
            subpoints->Add(point);
        }

        return subpoints;
    }

    enum class PolySegmentType
    {
        Line,
        QuadraticBezier,
        Bezier
    };

    // Processes poly-point segment, with possible splitting occurring within the segment.
    void PolySegmentTo(
        System::Collections::Generic::IList<Point>^ points,
        bool isStroked,
        bool isSmoothJoin,
        PolySegmentType segmentType
        )
    {
        if (points->Count == 0 || !BeginSegment(points[points->Count - 1], isStroked))
            return;

        int pointCount = points->Count;

        int groupSize = 1, rawPointsPerGroup = 1; 
        switch (segmentType)
        {
            case PolySegmentType::Line:
                groupSize = 1;
                rawPointsPerGroup = 1;
                break;
            case PolySegmentType::QuadraticBezier:
                // quadratic bezier points occur in groups of two, each group of which can be
                // converted into 3 PT_BEZIERTO
                groupSize = 2;
                rawPointsPerGroup = 3;
                break;
            case PolySegmentType::Bezier:
                groupSize = 3;
                rawPointsPerGroup = 3;
                break;
        }

        // get available count in terms of segment points
        int availCount = GetAvailablePointCount(groupSize, rawPointsPerGroup);

        if (availCount >= pointCount)
        {
            // enough space to add the segment in its entirety to current piece
            switch (segmentType)
            {
                case PolySegmentType::Line:
                    _context->PolyLineTo(points, isStroked, isSmoothJoin);
                    break;
                case PolySegmentType::QuadraticBezier:
                    _context->PolyQuadraticBezierTo(points, isStroked, isSmoothJoin);
                    break;
                case PolySegmentType::Bezier:
                    _context->PolyBezierTo(points, isStroked, isSmoothJoin);
                    break;
            }

            AddPoints(pointCount, groupSize, rawPointsPerGroup);
        }
        else
        {
            // need to split segment into multiple pieces
            int pointIndex = 0;

            while (pointIndex < pointCount)
            {
                availCount = GetAvailablePointCount(groupSize, rawPointsPerGroup);

                // Must always make progress when splitting, and available count must
                // be multiple of segment group size.
                Debug::Assert(availCount > 0 && (availCount % groupSize) == 0, "Invalid available point count");

                // add subset of points to current piece
                System::Collections::Generic::List<Point>^ subpoints = GetSubPoints(points, pointIndex, availCount);

                switch (segmentType)
                {
                    case PolySegmentType::Line:
                        _context->PolyLineTo(subpoints, isStroked, isSmoothJoin);
                        break;
                    case PolySegmentType::QuadraticBezier:
                        _context->PolyQuadraticBezierTo(subpoints, isStroked, isSmoothJoin);
                        break;
                    case PolySegmentType::Bezier:
                        _context->PolyBezierTo(subpoints, isStroked, isSmoothJoin);
                        break;
                }

                // move to next piece
                BeginPiece(subpoints[subpoints->Count - 1]);

                pointIndex += availCount;
            }
        }
    }

// CapacityStreamGeometryContext Members
public:
    virtual void BeginFigure(Point startPoint, bool isFilled, bool isClosed) override
    {
        RealBeginFigure(startPoint, isClosed);
    }
    
    virtual void LineTo(Point point, bool isStroked, bool isSmoothJoin) override
    {
        if (BeginSegment(point, isStroked))
        {
            _context->LineTo(point, isStroked, isSmoothJoin);
            AddRawPoints(1);
        }
    }

    virtual void QuadraticBezierTo(Point point1, Point point2, bool isStroked, bool isSmoothJoin) override
    {
        if (BeginSegment(point2, isStroked))
        {
            _context->QuadraticBezierTo(point1, point2, isStroked, isSmoothJoin);
            AddRawPoints(3);
        }
    }
    
    virtual void BezierTo(Point point1, Point point2, Point point3, bool isStroked, bool isSmoothJoin) override
    {
        if (BeginSegment(point3, isStroked))
        {
            _context->BezierTo(point1, point2, point3, isStroked, isSmoothJoin);
            AddRawPoints(3);
        }
    }
    
    virtual void PolyLineTo(System::Collections::Generic::IList<Point>^ points, bool isStroked, bool isSmoothJoin) override
    {
        PolySegmentTo(points, isStroked, isSmoothJoin, PolySegmentType::Line);
    }

    virtual void PolyQuadraticBezierTo(System::Collections::Generic::IList<Point>^ points, bool isStroked, bool isSmoothJoin) override
    {
        PolySegmentTo(points, isStroked, isSmoothJoin, PolySegmentType::QuadraticBezier);
    }

    virtual void PolyBezierTo(System::Collections::Generic::IList<Point>^ points, bool isStroked, bool isSmoothJoin) override
    {
        PolySegmentTo(points, isStroked, isSmoothJoin, PolySegmentType::Bezier);
    }

    virtual void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked, bool isSmoothJoin) override
    {
        if (BeginSegment(point, isStroked))
        {
            // An arc can be converted to a maxiumum of 4 Bezier segments, Check ArcToBezier in DrawingContextFlattener.cs
            _context->ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection, isStroked, isSmoothJoin);
            AddRawPoints(4 * 3);
        }
    }

    virtual void SetClosedState(bool closed) override
    {
    }

    virtual void SetFigureCount(int figureCount) override
    {
    }

    virtual void SetSegmentCount(int segmentCount) override
    {
    }
};

/**************************************************************************
*
* Function Description:
*
*   StrokePath
*
* Created:
*
*   6/17/2002 fyuan
*      Created it.
*
**************************************************************************/

HRESULT CGDIRenderTarget::StrokePath(
    IN GeometryProxy%     geometry,
    IN Pen                ^pPen,
    IN Brush              ^pStrokeBrush
    )
{
    Debug::Assert(pPen != nullptr);

    Int32Rect drawbounds;

    if (!geometry.GetDrawBounds(pPen, m_transform, drawbounds))
    {
        Debug::Write("StrokePath skipping out of bounds geometry\r\n");

        return S_OK;
    }

    if (pPen->Thickness == 0)
    {
        return S_OK;
    }

    HRESULT hr = E_NOTIMPL;

    CGDIPath ^ gdiPath = CGDIPath::CreateStrokePath(geometry, m_transform, pPen);

    if (gdiPath->IsValid() && UniformScale(m_transform))
    {
        GdiSafeHandle^ pen = ConvertPen(pPen, pStrokeBrush, m_transform, gdiPath, m_nDpiX);
                
        if (pen != nullptr)
        {
            hr = gdiPath->Draw(this, pen);
        }
    }

    // 
    // Fix bug 1394806: MGC: simple paths are widened inappropriately
    //
    // This is performance regression due to fix to emulate Avalon mitering behavior.
    // Emulation is done by widening the path and filling it. On already complex paths,
    // this can cause tremendous increase in complexity. To fix, we detect overly long
    // paths and split into smaller paths.
    //
    // Fix bug 1531873: Update to use StreamGeometry instead of PathGeometry.
    //
    if (hr == E_NOTIMPL)
    {
        // GetPointCount() should be called in CreateStrokePath and cached by GeometryProxy
        if (geometry.GetPointCount() > (2 * StrokeGeometrySplitter::MaximumPieceRawPointCount))
        {
            //
            // Split geometry into parts and render individually. StrokeGeometrySplitter will generate
            // pieces with approximately MaximumPieceRawPointCount of raw GDI points. We require significantly
            // more than MaximumPieceRawPointCount to trigger splitting to avoid infinite recursion.
            //
            hr = StrokeGeometrySplitter::RenderSubstrokes(
                geometry,
                gcnew RenderStrokePieceCallback(this, &CGDIRenderTarget::StrokePath),
                pPen,
                pStrokeBrush
                );
        }
    }

    // get the widened path, then fill path with pen's interal brush
    // also the bitmap can be quite hugh for a simple path
    if (hr == E_NOTIMPL)
    {
        // Note it's not necessary
        // for the widener to remove self intersects.  On PCL we don't care
        // and on postscript, SetupPathClipping will remove self intersects
        // and reorient as appropriate.

        // Widen and then fill the path
        double rTolerance = .25;

        PathGeometry ^ widened = geometry.Geometry->GetWidenedPathGeometry(pPen);
        hr = (widened == nullptr) ? E_FAIL : S_OK;

        if (SUCCEEDED(hr))
        {
            // Flatten path with curve drawn using thin pen to avoid unprintable job
            GeometryProxy widenedProxy(widened);
            bool hasCurve;

            if (gdiPath != nullptr && gdiPath->IsValid())
            {
                // use previously-computed HasCurve value
                hasCurve = gdiPath->HasCurve();
            }
            else
            {
                // otherwise compute from geometry
                hasCurve = widenedProxy.MayHaveCurves();
            }

            if (hasCurve && pPen->Thickness < 0.8f)
            {
                PathGeometry ^ flattened = widened->GetFlattenedPathGeometry(rTolerance, ToleranceType::Absolute);
                hr = (flattened == nullptr) ? E_FAIL : S_OK;

                if (SUCCEEDED(hr))
                {
                    widened = flattened;
                    widenedProxy.Attach(widened);
                }
            }

            if (SUCCEEDED(hr))
            {
                hr = FillPath(widenedProxy, pStrokeBrush);
            }

            widened = nullptr;
        }
        else
        {
            Debug::Assert(false, "PathGeometry.GetWidenedPathGeometry failed.");
        }
    }

    if (gdiPath != nullptr)
    {
        delete gdiPath;
    }

    return hr;
}


/**************************************************************************
*
* Function Description:
*
*   FillPath
*
* Created:
*
*   6/24/2002 fyuan
*      Created it.
*
**************************************************************************/

HRESULT CGDIRenderTarget::FillPath(
    IN GeometryProxy% geometry,
    IN Brush ^pFillBrush
    )
{
    HRESULT hr = S_OK;

    Int32Rect drawbounds;

    if (!geometry.GetDrawBounds(nullptr, m_transform, drawbounds))
    {
        Debug::Write("FillPath skipping out of bounds geometry\r\n");

        return hr;
    }

    // Quit if the drawbounds are outside the clip region
//  if (! Invisible(m_clip, & drawbounds))
    {
        hr = E_NOTIMPL;

        GdiSafeHandle^ brush = ConvertBrush(pFillBrush);

        if (brush != nullptr)
        {
            CGDIPath ^ gdiPath = CGDIPath::CreateFillPath(geometry, m_transform);

            Debug::Assert(gdiPath->IsValid(), "Invalid CGDIPath");

            hr = gdiPath->Fill(this, brush);
            
            delete gdiPath;
        }
        else if (GetCaps() & CAP_GradientRect)
        {
            // try to do a linear gradient fill
            hr = FillLinearGradient(geometry, pFillBrush);
        }

        if (hr == E_NOTIMPL)
        {
            // brush too complicated for gdi or some other failure. fallback by going rasterizing through UCE
            hr = RasterizeShape(geometry, drawbounds, pFillBrush);
        }
    }

    return hr;
}

HRESULT CGDIRenderTarget::FillImage(
    IN GeometryProxy% geometry,
    IN ImageBrush^ brush
    )
{
    // Change filling with single image to DrawImage to avoid rasterization
    // Single image may be bigger than rastered image
    if (!geometry.IsRectangle())
    {
        return E_NOTIMPL;
    }

    HRESULT hr = S_OK;

    Rect bounds       = geometry.GetBounds(nullptr);
    Rect viewport     = brush->Viewport;
    Transform ^ trans = brush->Transform;

    // apply translate/scaling brush transform to viewport
    if (trans != nullptr)
    {
        Matrix mat = trans->Value;

        if (IsTranslateOrScale(mat) && (mat.M11 > 0) && (mat.M22 > 0))
        {
            Point p1 = mat.Transform(viewport.TopLeft);
            Point p2 = mat.Transform(viewport.BottomRight);
                                    
            viewport.X      = p1.X;
            viewport.Y      = p1.Y;
            viewport.Width  = p2.X - p1.X;
            viewport.Height = p2.Y - p1.Y;
        }
        else
        {
            hr = E_NOTIMPL;
        }
    }

    if (hr == S_OK)
    {
        hr = E_NOTIMPL;

        if (AreClosePixel(bounds.X, viewport.X) &&
            AreClosePixel(bounds.Y, viewport.Y) &&
            AreClosePixel(bounds.Width, viewport.Width) &&
            AreClosePixel(bounds.Height, viewport.Height))
        {
            // brush covers entire geometry
            BitmapSource^ image = dynamic_cast<BitmapSource^>(brush->ImageSource);
            
            if (image != nullptr)
            {
                Rect viewbox = brush->Viewbox;
                if (brush->ViewboxUnits == BrushMappingMode::RelativeToBoundingBox)
                {
                    // convert viewbox to absolute units
                    viewbox.X *= image->Width;
                    viewbox.Y *= image->Height;
                    viewbox.Width *= image->Width;
                    viewbox.Height *= image->Height;
                }

                // Some pixel has resolution of 96.012 dpi, viewbox.Width and image width may be off by 0.1 pixel
                if (AreClosePixel(viewbox.X, 0) &&
                    AreClosePixel(viewbox.Y, 0) &&
                    (Math::Abs(viewbox.Width  - image->Width ) < 0.5) &&
                    (Math::Abs(viewbox.Height - image->Height) < 0.5))
                {
                    hr = DrawBitmap(image, nullptr, viewport);
                }
            }
        }
    }

    return hr;
}

// Gets gradient stops as a SortedList. Also adds stops at offsets 0 and 1 if needed.
System::Collections::SortedList^ GetSortedGradientStops(GradientStopCollection^ stopCollection)
{
    System::Collections::SortedList^ stops = gcnew System::Collections::SortedList(
        stopCollection->Count + 2
        );

    // add stops from the collection
    for (int stopIndex = 0; stopIndex < stopCollection->Count; stopIndex++)
    {
        GradientStop^ stop = stopCollection[stopIndex];

        stops[stop->Offset] = stop->Color;
    }

    // add stops at offsets 0 and 1 if needed
    if ((double)stops->GetKey(0) > 0)
    {
        stops->Add(0.0, stops->GetByIndex(0));
    }

    if ((double)stops->GetKey(stops->Count - 1) < 1)
    {
        stops->Add(1.0, stops->GetByIndex(stops->Count - 1));
    }

    return stops;
}

// Constructs transformation that maps the segment StartPoint->EndPoint onto (0,0)->(1,0).
Matrix GetGradientWorldToXTransform(LinearGradientBrush^ brush)
{
    Matrix worldToXTransform;

    if (brush->Transform != nullptr)
    {
        // transform from world to brush
        worldToXTransform = brush->Transform->Value;
        worldToXTransform.Invert();
    }

    // transform from brush to x-axis
    Vector gradientVector = brush->EndPoint - brush->StartPoint;
    double rotateAngle = Math::Atan2(-gradientVector.Y, gradientVector.X) * 180.0 / Math::PI;

    worldToXTransform.Translate(-brush->StartPoint.X, -brush->StartPoint.Y);
    worldToXTransform.Rotate(rotateAngle);
    worldToXTransform.Scale(1.0 / gradientVector.Length, 1.0);

    return worldToXTransform;
}

// Checks if a LinearGradientBrush's gradient vector is axis-aligned.
void GetLinearGradientAxisAligned(IN LinearGradientBrush^ brush, [Out] bool% isVertical, [Out] bool% isHorizontal)
{
    if (brush->Transform == nullptr || IsTranslateOrScale(brush->Transform->Value))
    {
        isVertical = AreCloseReal(brush->StartPoint.X, brush->EndPoint.X);
        isHorizontal = AreCloseReal(brush->StartPoint.Y, brush->EndPoint.Y);
    }
    else
    {
        isVertical = false;
        isHorizontal = false;
    }
}

// Generates the two vertices at current band. See FillLinearGradient.
void GenerateGradientBandVertices(
    IN array<CNativeMethods::TriVertex>^ vertices,
    IN OUT int% vertexOffset,
    IN Matrix% transform,
    IN double x,
    IN double top,
    IN double bottom,
    IN Color% color
    )
{
    Debug::Assert((vertexOffset + 2) <= vertices->Length);

    // vertices differ only in y coordinate
    vertices[vertexOffset++].Fill(
        transform,
        x,
        bottom,
        color
        );

    vertices[vertexOffset++].Fill(
        transform,
        x,
        top,
        color
        );
}

// Generates triangle indices for the two triangles at this band. See FillLinearGradient.
void GenerateGradientBandTriangles(
    IN array<unsigned long>^ indices,
    IN OUT int% indexOffset,
    IN int vertexOffset
    )
{
    Debug::Assert((indexOffset + 6) <= indices->Length);

    // first triangle starts at bottom vertex
    for (int i = 0; i < 3; i++)
    {
        indices[indexOffset++] = vertexOffset + i;
    }

    // second triangle starts at top vertex
    for (int i = 0; i < 3; i++)
    {
        indices[indexOffset++] = vertexOffset + 1 + i;
    }
}

HRESULT CGDIRenderTarget::FillLinearGradient(
    IN GeometryProxy% geometry,
    IN Brush^ brush
    )
{
    //
    // We convert GradientBrush to triangles which are then passed to GradientFill. First the
    // brush is transformed so that the gradient vector StartPoint->EndPoint is mapped to
    // (0,0)->(1,0). geometry is then transformed into this space (called x-space), and triangles
    // constructed to cover the x-space geometry.
    //
    // For each gradient band (a region filled gradient between 2 colors) we generate 4 vertices
    // and 2 triangles. Each set of gradient stops is called a "group". There is 1 group for
    // the Padding spread method; other spread methods may result in more groups to cover
    // the region outside the gradient vector.
    //
    //       1    3
    //        .--.      .      .--.
    //        |\ |      |\      \ |
    //        | \|  ->  | \  +   \|
    //        .--.      .--.      .
    //       0    2
    //
    // The numbers indicate the vertex index. Triangles are generated in the following pattern:
    // (0,1,2) and (1,2,3).
    //

    LinearGradientBrush^ gradientBrush = dynamic_cast<LinearGradientBrush^>(brush);

    if (gradientBrush == nullptr ||
        gradientBrush->ColorInterpolationMode != ColorInterpolationMode::SRgbLinearInterpolation)
    {
        // GDI only supports sRGB
        return E_NOTIMPL;
    }
    else if (gradientBrush->GradientStops == nullptr || gradientBrush->GradientStops->Count == 0)
    {
        Debug::Assert(false, "Empty LinearGradientBrush, should've been culled");
        return S_OK;
    }
    else if (gradientBrush->GradientStops->Count == 1)
    {
        Debug::Assert(false, "Single-stop LinearGradientBrush, should've been converted to SolidColorBrush");
        return FillPath(geometry, gcnew SolidColorBrush(gradientBrush->GradientStops[0]->Color));
    }

    Debug::Assert(gradientBrush->MappingMode == BrushMappingMode::Absolute, "Brush should've been made absolute");
    System::Collections::SortedList^ stops = GetSortedGradientStops(gradientBrush->GradientStops);

// Transform geometry to x-space and retrieve bounds.
    Matrix worldToXTransform = GetGradientWorldToXTransform(gradientBrush);

    Geometry^ xGeometry = TransformGeometry(geometry.Geometry, worldToXTransform);
    Rect xGeometryBounds = xGeometry->Bounds;
    xGeometryBounds.Inflate(0, 1);  // grow in y direction; FillGradient skips first pixel of triangle sometimes

    // transform the triangles back to device space
    Matrix xToDeviceTransform = worldToXTransform;
    xToDeviceTransform.Invert();
    xToDeviceTransform.Append(m_transform);

// Calculate group and band count, and allocate vertex/index arrays.
    // calculate group count according to spread method
    bool padLeft = false, padRight = false;
    int firstGroupIndex = 0;    // group at index 0 corresponds to x-coordinate range [0,1]
    int lastGroupIndex = 0;

    switch (gradientBrush->SpreadMethod)
    {
        case GradientSpreadMethod::Pad:
            if (xGeometryBounds.Left < 0)
            {
                padLeft = true;
            }

            if (xGeometryBounds.Right > 1)
            {
                padRight = true;
            }
            break;

        case GradientSpreadMethod::Reflect:
        case GradientSpreadMethod::Repeat:
            firstGroupIndex = (int)Math::Floor(xGeometryBounds.Left);
            lastGroupIndex = (int)Math::Floor(xGeometryBounds.Right);
            break;

        default:
            Debug::Assert(false, "Unknown GradientSpreadMethod");
            return E_NOTIMPL;
    }

    Debug::Assert(lastGroupIndex >= firstGroupIndex);

    // Number of gradient bands (regions filled with gradient between 2 colors).
    // Handle padding spread method by adding edge bands.
    int bandCount = (lastGroupIndex - firstGroupIndex + 1) * (stops->Count - 1);
    Debug::Assert(bandCount > 0, "0 gradient bands in FillLinearGradient");

    bandCount += padLeft ? 1 : 0;
    bandCount += padRight ? 1 : 0;

    int vertexCount = bandCount * 4;
    int triangleCount = bandCount * 2;
    int indexCount = triangleCount * 3;

    // Revert to rasterizing if it's axis-aligned and GradientFill call is larger than
    // the rasterization bitmap.
    if (IsTranslateOrScale(m_transform))
    {
        bool isVertical, isHorizontal;
        GetLinearGradientAxisAligned(IN gradientBrush, OUT isVertical, OUT isHorizontal);

        if (isVertical || isHorizontal)
        {
            // estimate rasterization bitmap size in bytes
            Rect deviceBounds = geometry.GetBounds(nullptr);
            deviceBounds.Transform(m_transform);    // approximate device bounds

            int rasterizationSize;

            if (isVertical)
            {
                rasterizationSize = (int)deviceBounds.Height;
            }
            else if (isHorizontal)
            {
                rasterizationSize = (int)deviceBounds.Width;
            }
            else
            {
                rasterizationSize = (int)(deviceBounds.Width * deviceBounds.Height);
            }
            
            rasterizationSize *= 4; // get size in bytes

            int gradientFillSize = static_cast<int>( vertexCount * sizeof(CNativeMethods::TriVertex) + indexCount * sizeof(unsigned long) );
            //REVIEW: Is int always big enough?

            if (rasterizationSize < gradientFillSize)
            {
                // fallback to rasterizing the brush since it's cheaper
                return E_NOTIMPL;
            }
        }
    }

    // allocate vertex and triangle index arrays
    array<CNativeMethods::TriVertex>^ vertices = gcnew array<CNativeMethods::TriVertex>(vertexCount);
    array<unsigned long>^ indices = gcnew array<unsigned long>(indexCount);

    int vertexOffset = 0;
    int indexOffset = 0;

// Generate gradient vertices and triangle indices.
    if (padLeft)
    {
        // pad from left geometry edge to left gradient vector edge
        Color color = (Color)stops[0.0];

        // generate two left-padding triangles starting at vertex 0
        GenerateGradientBandTriangles(
            IN indices,
            IN OUT indexOffset,
            IN vertexOffset
            );

        // generate the vertices for those triangles
        GenerateGradientBandVertices(
            IN vertices,
            IN OUT vertexOffset,
            IN xToDeviceTransform,
            IN xGeometryBounds.Left,
            IN xGeometryBounds.Top,
            IN xGeometryBounds.Bottom,
            IN color
            );
    }

    for (int groupIndex = firstGroupIndex; groupIndex <= lastGroupIndex; groupIndex++)
    {
        bool flipOffsets = false;

        if ((groupIndex % 2) != 0 &&
            gradientBrush->SpreadMethod == GradientSpreadMethod::Reflect)
        {
            flipOffsets = true;
        }

        for (int stopIndex = 0; stopIndex < stops->Count; stopIndex++)
        {
            int realStopIndex = flipOffsets ? (stops->Count - stopIndex - 1) : stopIndex;

            double offset = (double)stops->GetKey(realStopIndex);
            Color color = (Color)stops->GetByIndex(realStopIndex);

            if (flipOffsets)
            {
                offset = 1.0 - offset;
            }

            // convert offset to x-space
            offset += groupIndex;

            // generate triangles between this and next stops (not applicable on last stop)
            if (stopIndex < (stops->Count - 1))
            {
                GenerateGradientBandTriangles(
                    IN indices,
                    IN OUT indexOffset,
                    IN vertexOffset
                    );
            }

            GenerateGradientBandVertices(
                IN vertices,
                IN OUT vertexOffset,
                IN xToDeviceTransform,
                IN offset,
                IN xGeometryBounds.Top,
                IN xGeometryBounds.Bottom,
                IN color
                );
        }
    }

    if (padRight)
    {
        Color color = (Color)stops[1.0];

        // generate triangles connecting last group's last stop to geometry right edge
        GenerateGradientBandTriangles(
            IN indices,
            IN OUT indexOffset,
            IN vertexOffset - 2
            );

        GenerateGradientBandVertices(
            IN vertices,
            IN OUT vertexOffset,
            IN xToDeviceTransform,
            IN xGeometryBounds.Right,
            IN xGeometryBounds.Top,
            IN xGeometryBounds.Bottom,
            IN color
            );
    }

// Perform gradient fill.
    PushClipProxy(geometry);

    HRESULT hr = ErrorCode(CNativeMethods::GradientFill(
        m_hDC,
        vertices,
        vertexCount,
        indices,
        triangleCount,
        GRADIENT_FILL_TRIANGLE
        ));

    PopClip();

    return hr;
}


HRESULT CGDIRenderTarget::GetBrushScale(
    Brush ^ pFillBrush,
    double % ScaleX,
    double % ScaleY)
{
    if (pFillBrush->GetType() == LinearGradientBrush::typeid)
    {
        // Use lower dpi for wide-span LinearGradientBrush
        LinearGradientBrush ^ linear = (LinearGradientBrush ^) pFillBrush;

        Point s = linear->StartPoint;
        Point e = linear->EndPoint;

        double distance = Hypotenuse(s.X - e.X, s.Y - e.Y) / 96;

        double density = 256 / distance; // Change per inch, assuming 256 levels from StartPoint to EndPoint

        double dpi = m_RasterizationDPI;

        if (density < dpi)
        {
            dpi = Math::Max(density, dpi / 5);
        }

        ScaleX = m_nDpiX / dpi;
        ScaleY = m_nDpiY / dpi;
    }
    else
    {
        ScaleX = m_nDpiX / m_RasterizationDPI;
        ScaleY = m_nDpiY / m_RasterizationDPI;
    }

    if (ScaleX < 1)
    {
        ScaleX = 1;
    }

    if (ScaleY < 1)
    {
        ScaleY = 1;
    }

    return S_OK;
}

// Rasterize brush for area specified by pBounds, load into bmpdata
HRESULT CGDIRenderTarget::RasterizeBrush(
    CGDIBitmap         % bmpdata,
    Int32Rect            renderBounds,     // render bounds in device space, rounded
    Int32Rect            bounds,           // geometry bounds in device space, rounded
    Rect                 geometryBounds,   // geometry bounds in local space
    Brush              ^ pFillBrush,
    bool                 vertical,
    bool                 horizontal,
    double               ScaleX,
    double               ScaleY
    )
{
    Debug::Assert(pFillBrush != nullptr);
    
    HRESULT hr = S_OK;

    int bmpWidth  = (int) Math::Round(renderBounds.Width  / ScaleX); // scale from device resolution size to a smaller size
    int bmpHeight = (int) Math::Round(renderBounds.Height / ScaleY);

    // axis-aligned linear brushes can be optimized in terms of rasterized bitmap
    if (IsTranslateOrScale(m_transform))
    {
        if (horizontal)
        {
            bmpHeight = 1;
        }
        else if (vertical)
        {
            bmpWidth = 1;
        }
    }

    //
    // Transform geometry to rasterization bitmap.
    //
    // Fix bug 1390129: MGC: Images have black edges along bottom and right sides
    //
    // Edges are caused due to rounding of geometry bounds, which results in rasterization not completely
    // filling the rasterization bitmap. We use original geometry bounds to avoid rounding errors.
    //
    Matrix transform = m_transform;

    Rect deviceBounds = geometryBounds;
    deviceBounds.Transform(transform);

    // Calculate the part of deviceBounds we're rendering based on bounds and renderBounds.
    // renderBounds is a portion of bounds, due to clipping and/or banding.
    Rect box(renderBounds.X, renderBounds.Y, renderBounds.Width, renderBounds.Height);
    box.Offset(-bounds.X, -bounds.Y);
    box.Scale(deviceBounds.Width / bounds.Width, deviceBounds.Height / bounds.Height);
    box.Offset(deviceBounds.X, deviceBounds.Y);

    // Select the box, and scale to rasterization bitmap.
    transform.Translate(-box.X, -box.Y);
    transform.Scale(bmpWidth / box.Width, bmpHeight / box.Height);

    // rasterize
    BitmapSource ^ pBrushRaster = 
        CreateBitmapAndFillWithBrush(
            bmpWidth,
            bmpHeight,
            pFillBrush,
            geometryBounds,
            gcnew MatrixTransform(transform),
            PixelFormats::Pbgra32);

    hr = bmpdata.Load(pBrushRaster, nullptr, PixelFormats::Bgr24);

    return hr;
}

void ClipToBounds(Int32Rect % bounds, int width, int height)
{
    if (bounds.X < 0)
    {
        bounds.Width += bounds.X;
        bounds.X = 0;
    }

    if (bounds.Y < 0)
    {
        bounds.Height += bounds.Y;
        bounds.Y = 0;
    }

    if ((bounds.X + bounds.Width) > width)
    {
        bounds.Width = width - bounds.X;
    }

    if ((bounds.Y + bounds.Height) > height)
    {
        bounds.Height = height - bounds.Y;
    }
}

HRESULT CGDIRenderTarget::RasterizeShape(
    GeometryProxy% geometry,
    Int32Rect      % pMILBounds,
    Brush ^ pFillBrush
    )
{
    Int32Rect bounds = pMILBounds;
    
    // Clip to [0, 0, m_nWidth, m_nHeight]
    Int32Rect clippedBounds = bounds;
    ClipToBounds(clippedBounds, m_nWidth, m_nHeight);

    CGDIBitmap bitmapdata;

    double ScaleX, ScaleY;

    HRESULT hr = GetBrushScale(pFillBrush, ScaleX, ScaleY);

    // Skip area which is too small
    if ((clippedBounds.Width >= ScaleX) && (clippedBounds.Height >= ScaleY) && SUCCEEDED(hr))
    {
        bool IsVertical = false;
        bool IsHorizontal = false;

        LinearGradientBrush^ pLinear = dynamic_cast<LinearGradientBrush^>(pFillBrush);

        if (pLinear != nullptr)
        {
            GetLinearGradientAxisAligned(IN pLinear, OUT IsVertical, OUT IsHorizontal);
        }

        // Fix bug 1390129: Pass original geometry bounds to avoid rounding errors during rasterization.
        Rect geometryBounds = geometry.GetBounds(nullptr);
    
        if (IsHorizontal || IsVertical)
        {
            hr = RasterizeBrush(
                bitmapdata,
                clippedBounds,
                bounds,
                geometryBounds,
                pFillBrush,
                IsVertical,
                IsHorizontal,
                ScaleX,
                ScaleY
                );

            if (SUCCEEDED(hr))
            {
                bool clipPushed = false;
                      
                if (! geometry.IsRectangle() || ! IsTranslateOrScale(m_transform))
                {
                    PushClipProxy(geometry);
                    clipPushed = true;
                }

                if (bitmapdata.IsValid())
                {
                    hr = bitmapdata.StretchBlt(this, clippedBounds, false, false);
                }

                if (clipPushed)
                {
                    PopClip();
                }
            }
        }
        else // general case
        {
            bool clipPushed = false;
            
            if (! geometry.IsRectangle() || ! IsTranslateOrScale(m_transform))
            {
                PushClipProxy(geometry);
                clipPushed = true;
            }

            Int32Rect bandBounds = clippedBounds;

            int bmpWidth  = (int) Math::Round(bandBounds.Width  / ScaleX);
            int bmpHeight = (int) Math::Round(bandBounds.Height / ScaleY);

            // Divide whole area into bands if the whole is too big (1600x1200 pixels)
            int pixelLimit = RasterizeBandPixelLimit;

            pixelLimit = (pixelLimit + bmpWidth - 1) / bmpWidth;

            int nBands = (bmpHeight + pixelLimit - 1) / pixelLimit;

            int nBandHeight = bandBounds.Height / nBands + 1;
            int nRemain     = bandBounds.Height;

            bandBounds.Height = nBandHeight;

            while (SUCCEEDED(hr) && nRemain)
            {
                if (bandBounds.Height > nRemain)
                {
                    bandBounds.Height = nRemain;
                }

                hr = RasterizeBrush(
                        bitmapdata,
                        bandBounds,
                        bounds,
                        geometryBounds,
                        pFillBrush,
                        false,
                        false,
                        ScaleX,
                        ScaleY
                        );

                if (SUCCEEDED(hr))
                {
                    CGDIBitmap gdiBitmap(bitmapdata);

                    if (gdiBitmap.IsValid())
                    {
                        // Perform StretchDIBits of bitmap
                        hr = gdiBitmap.StretchBlt(this, bandBounds, false, false);
                    }
                }

                nRemain -= bandBounds.Height;
                bandBounds.Y += bandBounds.Height; // move to the next band
            }

            if (SUCCEEDED(hr))
            {
                Debug::Assert(nRemain == 0);
            }

            if (clipPushed)
            {
                PopClip();
            }
        }
    }

    return hr;
}
