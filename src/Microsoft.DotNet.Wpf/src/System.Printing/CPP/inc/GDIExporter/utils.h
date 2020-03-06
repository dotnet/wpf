// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __GDIEXPORTER_UTILS_H__
#define __GDIEXPORTER_UTILS_H__

// Utility Functions

/////////////////////////////////////////////////////////////////////////////
// Error Handling

// Returns HRESULT for last Win32 error if false result, otherwise S_OK.
// Must be called immediately after native call, otherwise last error can be
// overwritten/garbage.
inline HRESULT ErrorCode(BOOL rslt)
{
    HRESULT hr = S_OK;

    if (rslt == 0)
    {
        hr = Marshal::GetHRForLastWin32Error();
    }

    return hr;
}

/////////////////////////////////////////////////////////////////////////////
// Math

// Check if a double is close to zero
inline bool IsZero(double r)
{
    return (r > - Double::Epsilon) && (r < Double::Epsilon);
}

const double PixelEpsilon = 0.015625 / 100;     // 1/64/100

// Less than 1/16 pixel in 9600-dpi, assuming a/b in 96-dpi
inline bool AreClosePixel(double a, double b)
{
    return Math::Abs(a - b) < PixelEpsilon;
}

inline bool AreCloseReal(double a, double b)
{
    return Math::Abs(a - b) < Double::Epsilon;
}

inline bool IsTranslateOrScale(Matrix matrix)
{
    return (Math::Abs(matrix.M12) + Math::Abs(matrix.M21)) < Double::Epsilon;
}

inline bool IsRenderVisible(Rect rect)
{
    bool result = false;

    if (!rect.IsEmpty &&
        !Double::IsNaN(rect.X) &&
        !Double::IsNaN(rect.Y) &&
        !Double::IsNaN(rect.Width) &&
        !Double::IsNaN(rect.Height) &&
        !Double::IsInfinity(rect.X) &&
        !Double::IsInfinity(rect.Y) &&
        !Double::IsInfinity(rect.Width) &&
        !Double::IsInfinity(rect.Height) &&
        rect.Width > 0 &&
        rect.Height > 0)
    {
        result = true;
    }

    return result;
}

double GetScaleX(Matrix% matrix);
double GetScaleY(Matrix% matrix);

enum
{
    IsPolygon          = 0x00000001,
    IsBezier           = 0x00000010,
    IsClosedPolygon    = 0x00000020,
    IsOpenPolygon      = 0x00000040
};

enum MatrixRotate
{
    MatrixRotateBy0,
    MatrixRotateBy90,
    MatrixRotateBy180,
    MatrixRotateBy270,
    MatrixRotateByOther
};

MatrixRotate GetRotation(Matrix matrix);

// Returns TRUE if the intersection is not empty

bool IntersectRect(Int32Rect & prcDst, Int32Rect & prcSrc1);

void
TransformBounds(
    Matrix matrix,
    double x0,
    double y0,
    double x1,
    double y1,
	System::Windows::Rect % bounds
    );

HRESULT RectFToGDIRect(System::Windows::Rect % boundsF, Int32Rect % rect); // Lower-right exclusive

HRESULT MatrixRectangleTransform(Matrix & pmat, int width, int height, Rect & prcSrc);

// Appends transformation to geometry transformation.
Geometry^ TransformGeometry(Geometry^ geometry, Matrix transform);

/////////////////////////////////////////////////////////////////////////////
// Geometry

// Maximum rasterization size in pixels
const int RasterizeBandPixelLimit = 1600 * 1200;

// Proxy for Geometry that caches Geometry conversions and attributes.
ref struct GeometryProxy
{
public:
    GeometryProxy(Geometry^ geometry);

private:
    Geometry^ _geometry;

    bool _dataValid;
    Geometry::PathGeometryData _data;

    int _estimatedPoints;   // estimated number of points in path geometry

    bool _hasCurveValid;
    bool _hasCurve;

    bool _isRectangleValid;
    bool _isRectangle;

public:
    // Attaches proxy to new Geometry.
    void Attach(Geometry^ geometry);

    // Gets Geometry object.
    property Geometry^ Geometry
    {
        System::Windows::Media::Geometry^ get();
    }

    // Gets geometry as PathGeometry. If internal geometry is not PathGeometry,
    // it is converted and the results saved.
    PathGeometry^ GetPathGeometry();

    // Gets raw Geometry data. This can be expensive under some cases (GeometryGroup
    // and CombinedGeometry require conversion to PathGeometry), and so the
    // data is cached.
    System::Windows::Media::Geometry::PathGeometryData% GetGeometryData();

    // Gets bounds of geometry with optional stroke pen.
    Rect GetBounds(Pen^ pen);

    // Gets GDI bounds of geometry with optional stroke pen.
    bool GetDrawBounds(Pen^ pen, Matrix transform, OUT Int32Rect% bounds);

// Attributes
public:
    // Gets the GDI point count upper bound for the Geometry.
    int GetPointCount();

    // Returns true if Geometry might have curves.
    bool MayHaveCurves();

    // Gets Geometry fill rule. Returns FillRule::EvenOdd if geometry doesn't have fill rule.
    FillRule GetFillRule();

    // Checks if geometry is definitely a rectangle. It may return false negatives.
    bool IsRectangle();

private:
    // Converts _geometry to PathGeometry.
    void ConvertToPathGeometry();

    //
    // Avalon internal: Calling Geometry.GetPathGeometryData() may internally convert to
    // PathGeometry to get the data. This returns true if this is the case with _geometry.
    //
    bool DoesGetDataHavePathGeometryConversion();
};

#endif
