// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Euclidean distance: optimized for common cases where either x or y is zero.
double Hypotenuse(double x, double y)
{
    x = Math::Abs(x);
    y = Math::Abs(y);

    if (IsZero(x))
    {
        return y;
    }
    else if (IsZero(y))
    {
        return x;
    }
    else
    {
        if (y < x)
        {
            double temp = x;
            x = y;
            y = temp;
        }
    
        double r = x / y;
    
        return y * Math::Sqrt(r * r + 1.0);
    }
}

double GetScaleX(Matrix% matrix)
{
    return Hypotenuse(matrix.M11, matrix.M21);
}

double GetScaleY(Matrix% matrix)
{
    return Hypotenuse(matrix.M12, matrix.M22);
}


MatrixRotate GetRotation(Matrix matrix)
{
    // Check for no rotate.
    
    if( IsTranslateOrScale(matrix))
    {
        return MatrixRotateBy0;
    }
    
    // Check for Rotate by 90 degrees
    
	if (Math::Abs(matrix.M12) < Double::Epsilon &&
		Math::Abs(matrix.M21) < Double::Epsilon &&
        (matrix.M11 < 0.0) && (matrix.M22 < 0.0) )
    {
        return MatrixRotateBy180;
    }
	else if (Math::Abs(matrix.M11) < Double::Epsilon &&
		Math::Abs(matrix.M22) < Double::Epsilon)
    {
        if (matrix.M12 > 0.0) 
        {
            return MatrixRotateBy90;
        }
        else
        {
            return MatrixRotateBy270;
        }
    }

    return MatrixRotateByOther;
}


/**************************************************************************\
*
* Function Description:
*
*   Convert a floating-point coordinate bounds to an integer pixel bounds for GDI.
*
* Arguments:
*
* Return Value:
*
*   S_OK for success, E_INVALIDARG for invalid input that results in overflow.
*
\**************************************************************************/

// GDI uses signed 28.4 point internally.
#define INT_BOUNDS_MIN  -134217728          // 2^27
#define INT_BOUNDS_MAX   (134217728 - 1)    // 2^27 - 1

HRESULT RectFToGDIRect(Rect % boundsF, Int32Rect % rect)   // Lower-right exclusive
{
    bool fSucceeded = true;

    if (boundsF.X >= INT_BOUNDS_MIN && boundsF.X <= INT_BOUNDS_MAX)
    {
        rect.X = (int) Math::Floor(boundsF.X);
    }
    else
    {
        fSucceeded = false;
    }

    if (fSucceeded && (boundsF.Y >= INT_BOUNDS_MIN) &&
        (boundsF.Y <= INT_BOUNDS_MAX))
    {
        rect.Y = (int) Math::Floor(boundsF.Y);
    }
    else
    {
        fSucceeded = false;
    }

    if (fSucceeded && (boundsF.Width >= 0) &&
        (boundsF.Width <= INT_BOUNDS_MAX))
    {
        rect.Width  = (int) Math::Ceiling(boundsF.X + boundsF.Width) - rect.X;
    }
    else
    {
        fSucceeded = false;
    }

    if (fSucceeded && (boundsF.Height >= 0) &&
        (boundsF.Height <= INT_BOUNDS_MAX))
    {
        rect.Height = (int) Math::Ceiling(boundsF.Y + boundsF.Height) - rect.Y;
    }
    else
    {
        fSucceeded = false;
    }

    if (!fSucceeded)
    {
        // Make sure the rect is always initialized.
        // Also this makes the ASSERT below valid.

        rect.Width = 0;
        rect.Height = 0;
        rect.X = 0;
        rect.Y = 0;
    }

    // Don't forget that 'Width' and 'Height' are effectively
    // lower-right exclusive.  That is, if (x, y) are (1, 1) and
    // (width, height) are (2, 2), then the object is 2 pixels by
    // 2 pixels in size, and does not touch any pixels in column
    // 3 or row 3:

    Debug::Assert((rect.Width >= 0) && (rect.Height >= 0));

    if (fSucceeded)
        return S_OK;
    else
        return E_INVALIDARG;
}

/**************************************************************************\
*
* Function Description:
*
*   Given two coordinates defining opposite corners of a rectangle, this
*   routine transforms the rectangle according to the specified transform
*   and computes the resulting integer bounds, taking into account the
*   possibility of non-scaling transforms.
*
*   Note that it operates entirely in floating point, and as such takes
*   no account of rasterization rules, pen width, etc.
*
* Arguments:
*
*   [IN] matrix - Transform to be applied (or NULL)
*   [IN] x0, y0, x1, y1 - 2 points defining the bounds (they don't have
*                         to be well ordered)
*   [OUT] bounds - Resulting (apparently floating point) bounds
*
* Return Value:
*
*   NONE
*
* History:
*
*   12/08/1998 andrewgo
*       Created it.
*
\**************************************************************************/

// This is for computing the complexity of matrices. When you compose matrices
// or scale them up by large factors, it's really easy to hit the REAL_EPSILON
// limits without actually affecting the transform in any noticable way.
// e.g. a matrix with a rotation of 1e-5 degrees is, for all practical purposes,
// not a rotation.
#define MATRIX_EPSILON    (Double::Epsilon*5000.0)

void
TransformBounds(
    Matrix matrix,
    double left,
    double top,
    double right,
    double bottom,
    Rect % bounds
    )
{
    // Note that we don't have to order the points before the transform
    // (in part because the transform may flip the points anyways):

    cli::array<Point, 1>^ vertex = gcnew cli::array<Point>(4);

    vertex[0].X = left;
    vertex[0].Y = top;
    vertex[1].X = right;
    vertex[1].Y = bottom;

    // Ugh, the result is not a rectangle in device space (it might be
    // a parallelogram, for example).  Consequently, we have to look at
    // the bounds of all the vertices:

    vertex[2].X = left;
    vertex[2].Y = bottom;
    vertex[3].X = right;
    vertex[3].Y = top;

    matrix.Transform(vertex);

    left = right = vertex[0].X;
    top = bottom = vertex[0].Y;

    for (int i = 1; i < 4; i++)
    {
        if (left > vertex[i].X)
            left = vertex[i].X;

        if (right < vertex[i].X)
            right = vertex[i].X;

        if (top > vertex[i].Y)
            top = vertex[i].Y;

        if (bottom < vertex[i].Y)
            bottom = vertex[i].Y;
    }

    delete [] vertex;

    Debug::Assert((left <= right) && (top <= bottom));

    bounds.X      = left;
    bounds.Y      = top;

    //!!! Watch out for underflow.

    if (right - left > MATRIX_EPSILON)
    {
        bounds.Width  = right - left;
    }
    else
    {
        bounds.Width = 0;
    }

    if (bottom - top > MATRIX_EPSILON)
    {
        bounds.Height = bottom - top;
    }
    else
    {
        bounds.Height = 0;
    }
}

/**************************************************************************
*
* Function Description:
*
*   IntersectRect
*   returns TRUE if there is an intersection
*
**************************************************************************/

bool IntersectRect(
    Int32Rect & prcSrc1,
    Int32Rect & prcSrc2
    )
{
    // we want normalized rects here
    Debug::Assert(prcSrc1.Width >= 0);
    Debug::Assert(prcSrc2.Width >= 0);
    Debug::Assert(prcSrc1.Height >= 0);
    Debug::Assert(prcSrc2.Height >= 0);

    int w  = min(prcSrc1.X + prcSrc1.Width, prcSrc2.X + prcSrc2.Width) - max(prcSrc1.X, prcSrc2.X);

    // check for empty rect

    if (w > 0)
    {
        int h = min(prcSrc1.Y + prcSrc1.Height, prcSrc2.Y + prcSrc2.Height) - max(prcSrc1.Y, prcSrc2.Y);

        return h > 0;        // not empty
    }

    return false;
}

/**************************************************************************\
*
* Function Description:
*
*   Compute the 2D scale transform from the source rectangle to the
*   destination rectangle.
*
\**************************************************************************/

HRESULT MatrixRectangleTransform(Matrix & pmat, int width, int height, Rect & prcSrc)
{
    if ((prcSrc.Width < Double::Epsilon) || (prcSrc.Height < Double::Epsilon))
    {
        return E_INVALIDARG;
    }

    pmat.M12     = 0;
    pmat.M21     = 0;
    pmat.M11     = width  / prcSrc.Width;
    pmat.M22     = height / prcSrc.Height;
    pmat.OffsetX = - pmat.M11 * prcSrc.Left;
    pmat.OffsetY = - pmat.M22 * prcSrc.Top;

    return S_OK;
}

Geometry^ TransformGeometry(Geometry^ geometry, Matrix transform)
{
    geometry = geometry->CloneCurrentValue();

    if (geometry->Transform == nullptr || geometry->Transform->Value.IsIdentity)
    {
        geometry->Transform = gcnew MatrixTransform(transform);
    }
    else
    {
        geometry->Transform = gcnew MatrixTransform(
            geometry->Transform->Value * transform
            );
    }

    return geometry;
}

// GeometryProxy
GeometryProxy::GeometryProxy(System::Windows::Media::Geometry^ geometry)
{
    Attach(geometry);
}

void GeometryProxy::Attach(System::Windows::Media::Geometry^ geometry)
{
    if (geometry == nullptr)
        throw gcnew ArgumentNullException("geometry");

    _geometry = geometry;

    _dataValid = false;
    _estimatedPoints = -1;
    _hasCurveValid = false;
    _isRectangleValid = false;
}

Geometry^ GeometryProxy::Geometry::get()
{
    return _geometry;
}

PathGeometry^ GeometryProxy::GetPathGeometry()
{
    ConvertToPathGeometry();

    return (PathGeometry^)_geometry;
}

Geometry::PathGeometryData% GeometryProxy::GetGeometryData()
{
    _dataValid = true;

    if (DoesGetDataHavePathGeometryConversion())
    {
        // Avalon will convert to PathGeometry to get the data, so we might as well do it
        // and cache the conversion.
        ConvertToPathGeometry();
    }

    _data = _geometry->GetPathGeometryData();

    return _data;
}

Rect GeometryProxy::GetBounds(Pen^ pen)
{
    Rect bounds;

    if (pen == nullptr)
    {
        bounds = _geometry->Bounds;
    }
    else
    {
        bounds = _geometry->GetRenderBounds(pen);
    }

    return bounds;
}

bool GeometryProxy::GetDrawBounds(Pen^ pen, Matrix transform, OUT Int32Rect% bounds)
{
	Rect geometryBounds = GetBounds(pen);

    if (geometryBounds.IsEmpty)
    {
        // empty shape, generate rect with zero area
        bounds.X = 0;
        bounds.Y = 0;
        bounds.Width = 0;
        bounds.Height = 0;
        
        return true;
    }
    else
    {
        TransformBounds(
            transform,
            geometryBounds.Left,
            geometryBounds.Top,
            geometryBounds.Right,
            geometryBounds.Bottom,
            geometryBounds);

        return SUCCEEDED(RectFToGDIRect(geometryBounds, bounds));
    }
}

int GeometryProxy::GetPointCount()
{
    if (_estimatedPoints < 0)
    {
        //
        // ReachFramework!Utility has two paths to estimate point count: one for PathGeometry, and another
        // that uses Geometry.PathGeometryData. However, Geometry.GetPathGeometryData may incur a PathGeometry
        // conversion followed by serialization of PathGeometry to PathGeometryData.
        //
        // In such cases we merely convert to PathGeometry and walk it to get point count.
        //

        bool usePathGeometry = false;

        if (!_dataValid)
        {
            if (_geometry->GetType() == PathGeometry::typeid || DoesGetDataHavePathGeometryConversion())
            {
                usePathGeometry = true;
            }
        }

        if (usePathGeometry)
        {
            PathGeometry^ pathGeometry = GetPathGeometry();

            _estimatedPoints = Microsoft::Internal::AlphaFlattener::Utility::GetPathPointCount(pathGeometry);
        }
        else
        {
            // use serialized geometry data to estimate point count
            System::Windows::Media::Geometry::PathGeometryData% data = GetGeometryData();

            _estimatedPoints = Microsoft::Internal::AlphaFlattener::Utility::GetGeometryDataPointCount(data);
        }
    }

    return _estimatedPoints;
}

bool GeometryProxy::MayHaveCurves()
{
    if (!_hasCurveValid)
    {
        _hasCurveValid = true;
        _hasCurve = _geometry->MayHaveCurves();
    }

    return _hasCurve;
}

FillRule GeometryProxy::GetFillRule()
{
    StreamGeometry^ streamGeometry = dynamic_cast<StreamGeometry^>(_geometry);

    if (streamGeometry != nullptr)
    {
        return streamGeometry->FillRule;
    }

    PathGeometry^ pathGeometry = dynamic_cast<PathGeometry^>(_geometry);

    if (pathGeometry != nullptr)
    {
        return pathGeometry->FillRule;
    }

    GeometryGroup^ geometryGroup = dynamic_cast<GeometryGroup^>(_geometry);

    if (geometryGroup != nullptr)
    {
        return geometryGroup->FillRule;
    }

    return FillRule::EvenOdd;
}

bool GeometryProxy::IsRectangle()
{
    if (!_isRectangleValid)
    {
        _isRectangleValid = true;
        _isRectangle = Microsoft::Internal::AlphaFlattener::Utility::IsRectangle(_geometry);
    }
    
    return _isRectangle;
}

void GeometryProxy::ConvertToPathGeometry()
{
    if (_geometry->GetType() != PathGeometry::typeid)
    {
        _geometry = PathGeometry::CreateFromGeometry(_geometry);
    }
}

bool GeometryProxy::DoesGetDataHavePathGeometryConversion()
{
    Type^ geometryType = _geometry->GetType();

// OACR false positive: as CombinedGeometry and GeometryGroup are different types with a common base class.
#pragma warning (suppress : 6287)
    return (geometryType == CombinedGeometry::typeid || geometryType == GeometryGroup::typeid);
}
