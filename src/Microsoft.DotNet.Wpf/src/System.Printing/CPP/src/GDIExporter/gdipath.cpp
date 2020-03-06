// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#define PT_TYPEMASK    (PT_MOVETO | PT_LINETO | PT_BEZIERTO) 
#define PT_INVALID    ~(PT_TYPEMASK | PT_CLOSEFIGURE)

/**************************************************************************\
*
* Class Description:
*   Converts StreamGeometry/PathGeometry to GDI path data.
*
*   Handles the following differences between Avalon and GDI:
*
*     - For a corner defined by two consecutive points with same coordinates,
*       Avalon disables mitering while GDI will miter that corner.
*
* Usage:
*
*   See Convert().
*
*/
ref class GdiGeometryConverter : public CapacityStreamGeometryContext
{
// Constructors
private:
    // Point count estimation must exceed or equal actual point count.
    GdiGeometryConverter(Matrix transform, bool stroking, int resolutionScale, int estimatedPointCount)
    {
        // input
        _transform = transform;
        _stroking = stroking;
        _resolutionScale = resolutionScale;

        // output
        _isValid = true;
        //_forceEmpty = false;

        // gdi
        _points = gcnew array<PointI>(estimatedPointCount);
        _types = gcnew array<Byte>(estimatedPointCount);

        //_pointCount = 0;
        //_figureCount = 0;

        // current figure state
        //_bezierIndex = 0;

        //_figureVisible = false;
        //_figureClosed = false;

        _figureStartIndex = -1;
        //_figureHasGaps = false;
    }

public:
    //
    // Converts Geometry to GDI path data.
    //
    // Returns null if geometry too complex for GDI, in which case we will fill the widened stroke.
    // null will only occur only when stroking; currently there is no filling scenario that results
    // in geometry too complex.
    //
    static GdiGeometryConverter^ Convert(GeometryProxy% geometry, Matrix geometryToWorldTransform, bool stroking)
    {
        // Fix bug 1534923: See GdiGeometryConverter.ResolutionScale.
        int resolutionScale = GetResolutionScale(geometry);

        if (resolutionScale > 1)
        {
            geometryToWorldTransform.Scale(resolutionScale, resolutionScale);
        }

        // prepend world transform with geometry data's transform
        Geometry::PathGeometryData% geometryData = geometry.GetGeometryData();

        geometryToWorldTransform.Prepend(
            System::Windows::Media::Composition::CompositionResourceManager::MilMatrix3x2DToMatrix(geometryData.Matrix)
            );

        // perform actual conversion
        GdiGeometryConverter^ converter = gcnew GdiGeometryConverter(
            geometryToWorldTransform,
            stroking,
            resolutionScale,
            geometry.GetPointCount()
            );

        PathGeometry::ParsePathGeometryData(geometryData, converter);

        converter->CloseGeometry();

        if (converter->_isValid)
        {
            return converter;
        }
        else
        {
            Debug::Assert(stroking, "GdiGeometryConverter failed when filling");
            return nullptr;
        }
    }

// Private Fields
private:
    // conversion input
    Matrix _transform;          // geometry transformation to world space
    bool _stroking;             // if we're building GDI path for stroking or filling
    int _resolutionScale;       // Fix bug 1534923: Increase path resolution if it has curves to preserve fidelity.

    // conversion output
    bool _isValid;              // if conversion is valid, otherwise fallback to filling widened path
    bool _forceEmpty;           // if geometry has been forced to become empty, such as upon encountering NaN

    // GDI output
    array<PointI>^ _points;     // output GDI integer points
    array<Byte>^ _types;        // output GDI point types

    int _pointCount;
    int _figureCount;           // GDI figure count, which corresponds to number of PT_MOVETOs

    // current conversion state
    Point _lastPoint;           // last point untransformed added to _points
    int _bezierIndex;           // index we're at while adding PT_BEZIERTO

    bool _figureVisible;        // if current figure is visible
    bool _figureClosed;         // if current figure is closed

    int _figureStartIndex;      // current figure's start point index
    Point _figureStartPoint;    // current figure's start point untransformed
    bool _figureHasGaps;        // if figure has PT_MOVETO after figure start

// Public Properties
public:
    // GDI point data.
    property array<PointI>^ Points
    {
        array<PointI>^ get()
        {
            return _points;
        }
    }

    // GDI point flags (one of PT_* flags).
    property array<Byte>^ Types
    {
        array<Byte>^ get()
        {
            return _types;
        }
    }

    // GDI point count.
    property int PointCount
    {
        int get()
        {
            return _pointCount;
        }
    }

    // GDI figure count, which corresponds to number of PT_MOVETOs.
    property int FigureCount
    {
        int get()
        {
            return _figureCount;
        }
    }

    //
    // Fix bug 1534923: Small Glyphs converted to Geometry loses fidelity.
    //
    // Factor by which the geometry has been scaled to preserve fidelity.
    // To render this path, world transformation must be scaled by inverse of
    // ResolutionScale.
    //
    property int ResolutionScale
    {
        int get()
        {
            return _resolutionScale;
        }
    }

// Private Methods
private:
    // Fix bug 1534923: See GdiGeometryConverter.ResolutionScale.
    static int GetResolutionScale(GeometryProxy% geometry)
    {
        // currently we'll treat all geometry with curves as susceptible.
        // this is often the case with text.
        if (geometry.MayHaveCurves())
        {
            return 16;
        }
        else
        {
            // no resolution increase
            return 1;
        }
    }

    // Closes the geometry after passing to PathGeometry::ParsePathGeometryData.
    void CloseGeometry()
    {
        EndFigure();
    }

    // Marks geometry as invalid to fall back to widening the path and filling it.
    // Called due to geometry that's too complex to convert to GDI path.
    void FailGeometry()
    {
        // currently no known fill scenario that results in geometry too complex for GDI
        Debug::Assert(_stroking, "GdiGeometryConverter failed when filling");

        _isValid = false;
    }

    // Forces geometry be empty. GDI won't render anything.
    void ForceGeometryEmpty()
    {
        _forceEmpty = true;

        _points = nullptr;
        _types = nullptr;

        _pointCount = 0;
        _figureCount = 0;
    }

    void RealBeginFigure(Point point, bool filled, bool closed)
    {
        if (!_isValid || _forceEmpty)
            return;

        if (_stroking || filled)
        {
            _figureVisible = true;
            _figureClosed = closed;
        }
        else
        {
            // non-visible geometry, ignore until next figure
            _figureVisible = false;
            _figureClosed = false;
        }

        _figureStartIndex = AddPoint(point, PT_MOVETO);
        Debug::Assert(_figureStartIndex != -1);

        _figureStartPoint = point;
        _figureHasGaps = false;
    }

    void EndFigure()
    {
        if (!_isValid || _forceEmpty || _pointCount == 0)
            return;

        if (_figureClosed)
        {
            // PT_CLOSEFIGURE will close with most recent PT_MOVETO. This results in incorrect closing
            // if we have gaps (PT_MOVETO after figure's initial PT_MOVETO), thus we need to manually close
            // in such cases.
            if (_figureHasGaps)
            {
                AddPoint(_figureStartPoint, PT_LINETO);
            }
            else
            {
                // otherwise GDI can close for us, add PT_CLOSEFIGURE to last point
                _types[_pointCount - 1] = _types[_pointCount - 1] | PT_CLOSEFIGURE;
            }
        }
        else
        {
            // eliminate redundant PT_MOVETO at end of unclosed figure
            if ((_types[_pointCount - 1] & PT_TYPEMASK) == PT_MOVETO)
            {
                RemoveLastPoint();
            }
        }

        // eliminate 1-point figures
        int figurePointCount = _pointCount - _figureStartIndex;

        if (figurePointCount == 1)
        {
            RemoveLastPoint();
        }
    }

    // Returns true if segment is visible and should be further processed.
    bool BeginSegment(bool stroked, bool smoothJoin, Point endPoint)
    {
        if (!_isValid || _forceEmpty || !_figureVisible)
            return false;

        Debug::Assert(_bezierIndex == 0, "Non-multiple of 3 PT_BEZIER points added during previous segment");

        bool visible = false;

        if (_stroking && smoothJoin)
        {
            // GDI doesn't support smooth join
            FailGeometry();
        }
        else if (!_stroking || stroked)
        {
            // segment is visible if we're filling the geometry or we're stroking the segment
            visible = true;
        }
        else
        {
            // segment isn't visible, jump to segment endpoint
            visible = false;

            AddPoint(endPoint, PT_MOVETO);
        }

        return visible;
    }

    // Check if p1->p2 as the same slope as (dx, dy)
    bool Colinear(int dx, int dy, int p1, int p2)
    {
        int dx0 = _points[p2].x - _points[p1].x; 
        int dy0 = _points[p2].y - _points[p1].y; 

        return ((dx0 * dy) == (dx * dy0)) && // colinear
               (Math::Sign(dx) == Math::Sign(dx0)) &&
               (Math::Sign(dy) == Math::Sign(dy0));
    }

    // Adds a point with the specified segment type.
    //
    // Returns index of added point, -1 if none added.
    int AddPoint(Point point, Byte type)
    {
        // Transform and convert to integer point. Also calculate delta with last point.
        Point transformedPoint = _transform.Transform(point);

        if (Double::IsNaN(transformedPoint.X) || Double::IsNaN(transformedPoint.Y))
        {
            Debug::Assert(false, "Invalid path input: NaN encountered");
            ForceGeometryEmpty();
            return -1;
        }

        PointI intPoint;
        intPoint.x = (int)Math::Round(transformedPoint.X);
        intPoint.y = (int)Math::Round(transformedPoint.Y);

        int dx = 0, dy = 0;

        if (_pointCount > 0)
        {
            dx = intPoint.x - _points[_pointCount - 1].x;
            dy = intPoint.y - _points[_pointCount - 1].y;
        }

        // If point duplicates previous point in the same figure, fail rendering
        // since GDI will miter on duplicated points, but Avalon won't.
        if (_pointCount > 0 &&
            _stroking &&
            dx == 0 && dy == 0 &&
            (type & PT_TYPEMASK) != PT_MOVETO)
        {
            FailGeometry();
            return -1;
        }

        switch (type & PT_TYPEMASK)
        {
            case PT_MOVETO:
                _figureCount++;
                _figureHasGaps = true;

                // Optimization: Reduce consecutive PT_MOVETO to most recent PT_MOVETO.
                if (_pointCount > 0 &&
                    (_types[_pointCount - 1] & PT_TYPEMASK) == PT_MOVETO)
                {
                    // will add a point below
                    RemoveLastPoint();
                }
                break;

            case PT_LINETO:
                // Optimization: Remove line point colinear with line segment.
                if (_pointCount >= 2 &&
                    (_types[_pointCount - 1] & PT_TYPEMASK) == PT_LINETO)
                {
                    if (Colinear(dx, dy, _pointCount - 2, _pointCount - 1))
                    {
                        RemoveLastPoint();
                    }
                }
                break;

            case PT_BEZIERTO:
                // Optimization: Reduce colinear Bezier curve to line.
                if (_bezierIndex == 2 && _pointCount >= 3)
                {
                    Debug::Assert((_types[_pointCount - 1] & PT_TYPEMASK) == PT_BEZIERTO &&
                        (_types[_pointCount - 2] & PT_TYPEMASK) == PT_BEZIERTO);

                    if (Colinear(dx, dy, _pointCount - 2, _pointCount - 1) &&
                        Colinear(dx, dy, _pointCount - 3, _pointCount - 1))
                    {
                        // this is last point in bezier trio, remove previous two points
                        // and convert to line
                        RemoveLastPoint();
                        RemoveLastPoint();

                        type = PT_LINETO;
                    }
                }

                _bezierIndex = (_bezierIndex + 1) % 3;
                break;
        }

        // Add the point.
        Debug::Assert(_pointCount <= _points->Length, "Point count estimation too small");

        _lastPoint = point;
        _points[_pointCount] = intPoint;
        _types[_pointCount] = type;
        _pointCount++;

        return _pointCount - 1;
    }

    // Remove last added point.
    // MUST be followed by adding a new point, or end geometry.
    void RemoveLastPoint()
    {
        Debug::Assert(_pointCount > 0);

        if ((_types[_pointCount - 1] & PT_TYPEMASK) == PT_MOVETO)
        {
            _figureCount--;
        }

        _pointCount--;

        // we don't remember last point anymore
        _lastPoint = Point(Double::NaN, Double::NaN);
    }

    Point GetLastPoint()
    {
        Debug::Assert(!Double::IsNaN(_lastPoint.X) && !Double::IsNaN(_lastPoint.Y));

        return _lastPoint;
    }

    // Add a quadratic Bezier section
    void AddQuadratic(Point p1, Point p2)
    {
        Point p0 = GetLastPoint();

        // Think of the  quadratic Bezier points as Q0, Q1, Q2, where Q0 is the 
        // current figure's last point, Q1=(x1,y1), and Q2=(x2,y2). Then the cubic Bezier 
        // points are C0 = Q0, 
        // C1 = (1/3)*Q0 + (2/3)*Q1,   C2 = (2/3)*Q1 + (1/3)*Q2,   C3=Q2.

        Point b;

        b.X = (p0.X + p1.X * 2) / 3;
        b.Y = (p0.Y + p1.Y * 2) / 3;

        AddPoint(b, PT_BEZIERTO);

        b.X = (p1.X * 2 + p2.X) / 3;
        b.Y = (p1.Y * 2 + p2.Y) / 3;

        AddPoint(b,  PT_BEZIERTO);
        
        AddPoint(p2, PT_BEZIERTO);
    }


    void AddArc(Point point, Size size, double rotationAngle, bool largeArc, SweepDirection sweepDirection)
    {
        // TODO: Remove NaN checks once bug 1466417 gets fixed, otherwise we need to check arc for
        // invalid values, which currently doesn't result in empty bounds.
        if (System::Double::IsNaN(point.X) ||
            System::Double::IsNaN(point.Y))
        {
            ForceGeometryEmpty();
            return;
        }
        else if (size.IsEmpty ||
            System::Double::IsNaN(size.Width) ||
            System::Double::IsNaN(size.Height) ||
            System::Double::IsNaN(rotationAngle))
        {
            // current behavior due to 1466417 is degenerate to line segment
            AddPoint(point, PT_LINETO);
        }
        else
        {
            int cPieces = 0;

            Point lastPoint = GetLastPoint();

            PointCollection^ pc = System::Windows::Xps::Serialization::GeometryHelper::ArcToBezier(
                lastPoint.X,    // X coordinate of the last point
                lastPoint.Y,    // Y coordinate of the last point
                size.Width,     // The ellipse's X radius
                size.Height,    // The ellipse's Y radius
                rotationAngle,  // Rotation angle of the ellipse's x axis
                largeArc,       // Choose the larger of the 2 possible arcs if TRUE
                sweepDirection, // Sweep the arc while increasing the angle if TRUE
                point.X,        // X coordinate of arc endpoint
                point.Y,        // Y coordinate of arc endpoint
                cPieces);       // The number of output Bezier curves

            // cPieces = -1 indicates a degenerate line, but we still treat it as a line, so--
            if (cPieces <= 0)
            {
                AddPoint(point, PT_LINETO);
            }
            else
            {
                for (int i = 0; i < pc->Count; i ++)
                {
                    AddPoint(pc[i], PT_BEZIERTO);
                }
            }
        }
    }

// CapacityStreamGeometryContext Members
public:
    virtual void BeginFigure(Point startPoint, bool isFilled, bool isClosed) override
    {
        // end any previously-started figure
        EndFigure();

        RealBeginFigure(startPoint, isFilled, isClosed);
    }
    
    virtual void LineTo(Point point, bool isStroked, bool isSmoothJoin) override
    {
        if (BeginSegment(isStroked, isSmoothJoin, point))
        {
            AddPoint(point, PT_LINETO);
        }
    }

    virtual void QuadraticBezierTo(Point point1, Point point2, bool isStroked, bool isSmoothJoin) override
    {
        if (BeginSegment(isStroked, isSmoothJoin, point2))
        {
            AddQuadratic(point1, point2);
        }
    }
    
    virtual void BezierTo(Point point1, Point point2, Point point3, bool isStroked, bool isSmoothJoin) override
    {
        if (BeginSegment(isStroked, isSmoothJoin, point3))
        {
            AddPoint(point1, PT_BEZIERTO);
            AddPoint(point2, PT_BEZIERTO);
            AddPoint(point3, PT_BEZIERTO);
        }
    }
    
    virtual void PolyLineTo(System::Collections::Generic::IList<Point>^ points, bool isStroked, bool isSmoothJoin) override
    {
        if (points->Count > 0 && BeginSegment(isStroked, isSmoothJoin, points[points->Count - 1]))
        {
            for (int index = 0; index < points->Count; index++)
            {
                AddPoint(points[index], PT_LINETO);
            }
        }
    }

    virtual void PolyQuadraticBezierTo(System::Collections::Generic::IList<Point>^ points, bool isStroked, bool isSmoothJoin) override
    {
        if (points->Count > 0 && BeginSegment(isStroked, isSmoothJoin, points[points->Count - 1]))
        {
            for (int index = 0; index < points->Count; index += 2)
            {
                AddQuadratic(points[index], points[index + 1]);
            }
        }
    }

    virtual void PolyBezierTo(System::Collections::Generic::IList<Point>^ points, bool isStroked, bool isSmoothJoin) override
    {
        if (points->Count > 0 && BeginSegment(isStroked, isSmoothJoin, points[points->Count - 1]))
        {
            for (int index = 0; index < points->Count; index++)
            {
                AddPoint(points[index], PT_BEZIERTO);
            }
        }
    }

    virtual void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked, bool isSmoothJoin) override
    {
        if (BeginSegment(isStroked, isSmoothJoin, point))
        {
            AddArc(point, size, rotationAngle, isLargeArc, sweepDirection);
        }
    }

    virtual void SetClosedState(bool closed) override
    {
        _figureClosed = closed;
    }

    virtual void SetFigureCount(int figureCount) override
    {
    }

    virtual void SetSegmentCount(int segmentCount) override
    {
    }
};

CGDIPath::CGDIPath(
    GeometryProxy% geometry,
    Matrix matrix,
    bool  ForFill,
    Pen ^ pPen)
{
    Debug::Assert(ForFill || pPen != nullptr);

    // get path GDI fill mode
    m_PathFillMode = (geometry.GetFillRule() == FillRule::EvenOdd) ? ALTERNATE : WINDING;

    m_DeviceBounds.X      = 0;
    m_DeviceBounds.Y      = 0;
    m_DeviceBounds.Width  = 0;
    m_DeviceBounds.Height = 0;
    m_ResolutionScale     = 1;

    // Convert Geometry to GDI point data.
    GdiGeometryConverter^ converter = GdiGeometryConverter::Convert(geometry, matrix, !ForFill);

    if (converter == nullptr)
    {
        //
        // Can fail if geometry too complex for GDI, or GDI unable to handle differences from Avalon.
        // Example: duplicate points in Avalon disables mitering for that point.
        //
        // We fallback to filling the path.
        //
        return;
    }

    m_Points   = converter->Points;
    m_Types    = converter->Types;
    m_HasCurve = geometry.MayHaveCurves();
    m_ResolutionScale = converter->ResolutionScale;
    
    int count = converter->PointCount;

    if (count == 0)
    {
        // zero-point figures are valid
        m_IsValid = true;
    }
    else
    {
        if (m_HasCurve)
        {   
            ProcessCurve(count, ForFill);
        }
        else
        {
            ProcessPolygon(count, ForFill, converter->FigureCount);
        }
    }
}


void CGDIPath::ProcessPolygon(int count, bool ForFill, int figureCount)
{
    // construct a polyPolygon
    m_Flags |= IsPolygon;

    array<Byte>^ curGdipType = m_Types;

    if (figureCount == 1)
    {
        // Check if all points after the first one is PT_LINETO

        bool allline = true;

        for (int i = 1; i < count; i++)
        {
            if ((curGdipType[i] & PT_TYPEMASK) != PT_LINETO)
            {
                allline = false;
                break;
            }
        }
        
        if (allline)
        {
            // put transformed points into the Points array and get the device bounds.
            GetDeviceBounds(m_Points, count);
            
            m_NumPoints   = count;
            m_NumPolygons = 1;

            // one polygon containing all the points
            m_PolyCounts = gcnew array<unsigned int>(1);
            m_PolyCounts[0] = m_NumPoints;

            if (ForFill || (m_Types[m_NumPoints - 1] & PT_CLOSEFIGURE))
            {
                m_Flags |= IsClosedPolygon;
            }
            
            m_IsValid = true;
            return;
        }
    }

    // else multliple sub paths, draw with PolyPolyline.
    // We compute number of polygons and number of points per polygon here. Subpaths are closed
    // if necessary by replacing dummy points with start point of polyline.

    m_PolyCounts = gcnew array<unsigned int>(figureCount);

    int prevType = PT_INVALID;
    int iStartPoint = 0;          // polygon start point index

    int iPolygon = -1;   // current polygon index, we start at no polygon

    for (int iPoint = 0; iPoint < count; iPoint++)
    {
        switch (curGdipType[iPoint] & PT_TYPEMASK)
        {
        case PT_MOVETO:
            if ((prevType & PT_TYPEMASK) != PT_MOVETO)
            {
                // this is start of new polygon. check if previous polygon is closed
                if (iPolygon >= 0)
                {
                    // PolyPolygon requires all subpaths to be closed
                    if (ForFill || (prevType & PT_CLOSEFIGURE))
                    {
                        m_Flags |= IsClosedPolygon;
                    }
                    else
                    {
                        m_Flags |= IsOpenPolygon;
                    }

                    // add polygon count for previous polygon
                    Debug::Assert((iPoint - iStartPoint) >= 2);
                    m_PolyCounts[iPolygon] = iPoint - iStartPoint;
                }
                
                iPolygon++;
            }
            
            // ignore multiple start points, use most recent one
            iStartPoint = iPoint;
            break;

        default:
            // invalid type
            Debug::Assert(0);
            // FALLTHRU

        case PT_LINETO:
            // keep this point
            break;
        }
        
        prevType = curGdipType[iPoint];
    }

    if (ForFill || (m_Types[count - 1] & PT_CLOSEFIGURE))
    {
        m_Flags |= IsClosedPolygon;
    }
    else
    {
        m_Flags |= IsOpenPolygon;
    }
    
    m_PolyCounts[iPolygon] = count - iStartPoint;
    m_NumPolygons = iPolygon + 1;

    // Get the device bounds.
    GetDeviceBounds(m_Points, count);
    m_NumPoints = count;
    
    m_IsValid = true;
}


void CGDIPath::ProcessCurve(int count, bool ForFill)
{
    // Getthe device bounds.
    GetDeviceBounds(m_Points, count);
    
    m_NumPoints = count;

    for (int i = 1; i < m_NumPoints; i++)
    {
        if ((m_Types[i] & PT_TYPEMASK) != PT_BEZIERTO)
        {
            goto MixedData;
        }
    }

    // Make sure it's closed if it needs to be, so that we don't have
    // to use a path to stroke the Bezier.
    if (!(ForFill) && (m_Types[m_NumPoints - 1] & PT_CLOSEFIGURE))
    {
        if ((m_Points[0].x != m_Points[m_NumPoints - 1].x) ||
            (m_Points[0].y != m_Points[m_NumPoints - 1].y))
        {
            goto MixedData;
        }
    }

    // It is a polyBezier
    m_Flags |= IsBezier;
    
MixedData:
    m_IsValid = true;
}


// Get the actual device bounds of the transformed points
void CGDIPath::GetDeviceBounds(array<PointI>^ p, int count)
{
    Debug::Assert(count >= 1);

    int  minX, minY, maxX, maxY;

    minX = maxX = p[0].x;
    minY = maxY = p[0].y;

    for (int i = 1; i < count; i++)
    {
        if (p[i].x < minX)
        {
            minX = p[i].x;
        }
        else if (p[i].x > maxX)
        {
            maxX = p[i].x;
        }
        
        if (p[i].y < minY)
        {
            minY = p[i].y;
        }
        else if (p[i].y > maxY)
        {
            maxY = p[i].y;
        }
    }

    m_DeviceBounds.X      = minX;
    m_DeviceBounds.Y      = minY;
    m_DeviceBounds.Width  = (maxX - minX + 1);
    m_DeviceBounds.Height = (maxY - minY + 1);
}

// !!! This is a problem if there is already a path opened or defined
HRESULT CGDIPath::Fill(CGDIDevice ^ dc, GdiSafeHandle^ brush)
{
    Debug::Assert(IsValid());
    Debug::Assert(brush != nullptr);

    HRESULT hr = S_OK;

    if (m_NumPoints > 0)
    {
        XFORM oldTransform;
        dc->SetupForIncreasedResolution(m_ResolutionScale, OUT oldTransform);

        dc->SelectObject(brush, OBJ_BRUSH);
        
        dc->SetPolyFillMode(m_PathFillMode);

        if (m_Flags & IsPolygon)
        {
            dc->SelectObject(dc->m_nullPen, OBJ_PEN);
            
            if (m_NumPolygons == 1)
            {
                hr = dc->Polygon(m_Points, 0, m_NumPoints);
            }
            else if (! (dc->GetCaps() & CAP_PolyPolygon) )
            {
                // On Win9x printing, PolyPolygon() sends output in scan line
                // blits when filling, but Polygon() doesn't.

                // !!! If the polygons overlap, this won't fill them correctly

                int offset = 0;

                for (int i = 0; i < m_NumPolygons && SUCCEEDED(hr); i ++)
                {
                    hr = dc->Polygon(m_Points, offset, m_PolyCounts[i]);
                    offset += m_PolyCounts[i];
                }
            }
            else
            {
                // FIX #NTBUG9-451307-2001-08-04-milindma "GDI+: Dashed (round
                //     end caps) lines cause excessively large spools on PCL 5/6
                //     printers" 
                // Fix 451307 (4/4): Use C_PolyPolygon to divide a PolyPolygon 
                //     into disjointing portions if possible
                CPolyPolygon poly;

                poly.Set(m_Points, 0, m_PolyCounts, 0, m_NumPolygons);
                hr = poly.Draw(dc);
            }
        }
        else
        {
            // If ForceFill is set, we are safe because the fill mode is
            // winding and so we will be opaque if we intersect ourselves.

            hr = dc->BeginPath();

            if (SUCCEEDED(hr))
            {
                if (m_Flags & IsBezier)
                {
                    hr = dc->PolyBezier(m_Points, m_NumPoints);
                }
                else
                {
                    hr = dc->DrawMixedPath(m_Points, m_Types, m_NumPoints);
                }
            }

            if (SUCCEEDED(hr))
            {
                hr = dc->EndPath();
            }

            if (SUCCEEDED(hr))
            {
                hr = dc->FillPath();
            }
        }

        dc->CleanupForIncreasedResolution(m_ResolutionScale, IN oldTransform);
    }

    return hr;
}


HRESULT CGDIPath::Draw(CGDIDevice ^ dc, GdiSafeHandle ^ pen)
{
    Debug::Assert(IsValid());
    Debug::Assert(pen != nullptr);

    HRESULT hr = S_OK;

    if (m_NumPoints > 0)
    {
        XFORM oldTransform;
        dc->SetupForIncreasedResolution(m_ResolutionScale, OUT oldTransform);

        dc->SelectObject(pen, OBJ_PEN);
        dc->SelectObject(dc->m_nullBrush, OBJ_BRUSH);

        if (m_Flags & IsPolygon)
        {
            if (m_NumPolygons == 1)
            {
                if (m_Flags & IsClosedPolygon)
                {
                    hr = dc->Polygon(m_Points, 0, m_NumPoints);
                }
                else
                {
                    hr = dc->Polyline(m_Points, 0, m_NumPoints);
                }
            }
            else
            {
                if (m_Flags & IsClosedPolygon)
                {
                    if (m_Flags & IsOpenPolygon)
                    {
                        // Mix of open and closed polygons
                        int offset = 0;

                        for (int i = 0; i < m_NumPolygons && SUCCEEDED(hr); i++)
                        {
                            int count = m_PolyCounts[i];

                            // The polygons are generated through our API and
                            // should have been verified above.
                            Debug::Assert(count > 0);

                            if ((m_Points[offset].x == m_Points[offset + count - 1].x) &&
                                (m_Points[offset].y == m_Points[offset + count - 1].y))
                            {
                                hr = dc->Polygon(m_Points, offset, count);
                            }
                            else
                            {
                                hr = dc->Polyline(m_Points, offset, count);
                            }
                            
                            offset += count;
                        }
                    }
                    else
                    {
                        // All polygons are closed...
                        if (! (dc->GetCaps() & CAP_PolyPolygon))
                        {
                            int offset = 0;
                           
                            for (int i = 0; i < m_NumPolygons && SUCCEEDED(hr); i ++)
                            {
                                hr = dc->Polygon(m_Points, offset, m_PolyCounts[i]);
                                offset += m_PolyCounts[i];
                            }
                        }
                        else
                        {
                            CPolyPolygon poly;

                            poly.Set(m_Points, 0, m_PolyCounts, 0, m_NumPolygons);

                            hr = poly.Draw(dc);
                        }                                        
                    }
                }
                else
                {
                    // All polygons are open...
                    hr = dc->PolyPolyline(m_Points, m_PolyCounts, m_NumPolygons);
                }
            }
        }
        else if (m_Flags & IsBezier)
        {
            hr = dc->PolyBezier(m_Points, m_NumPoints);
        }
        else
        {
            hr = dc->DrawMixedPath(m_Points, m_Types, m_NumPoints);
        }

        dc->CleanupForIncreasedResolution(m_ResolutionScale, IN oldTransform);
    }

    return hr;
}


HRESULT CGDIPath::SelectClip(CGDIDevice ^ dc, int mode)
{
    Debug::Assert(IsValid());

    HRESULT hr = S_OK;

    if (m_NumPoints > 0)
    {
        XFORM oldTransform;
        dc->SetupForIncreasedResolution(m_ResolutionScale, OUT oldTransform);

        hr = dc->BeginPath();

        if (SUCCEEDED(hr))
        {
            dc->SetPolyFillMode(m_PathFillMode);

            if (m_Flags & IsPolygon)
            {
                if (m_NumPolygons == 1)
                {
                    hr = dc->Polygon(m_Points, 0, m_NumPoints);
                }
                else
                {
                    hr = dc->PolyPolygon(m_Points, 0, m_PolyCounts, 0, m_NumPolygons);
                }
            }
            else
            {
                if (m_Flags & IsBezier)
                {
                    hr = dc->PolyBezier(m_Points, m_NumPoints);
                }
                else
                {
                    hr = dc->DrawMixedPath(m_Points, m_Types, m_NumPoints);
                }
            }
        }

        if (SUCCEEDED(hr))
        {
            hr = dc->EndPath();
        }

        if (SUCCEEDED(hr))
        {
            hr = dc->SelectClipPath(mode);
        }

        dc->CleanupForIncreasedResolution(m_ResolutionScale, IN oldTransform);
    }

    return hr;
}


// Find the maxium cos(theta) for all the corners within the path.
// Bezier curve segment is treated as three lines segement as it can't 
// generate shaper angle than them.
double CGDIPath::MaxCos(void)
{
    double maxcos = -1;

    int            nCount = m_NumPoints;
    
    array<PointI>^ pPoints = m_Points;
    array<Byte> ^  pTypes  = m_Types;

    // Keep track of location of all PT_MOVETO and PT_CLOSEFIGUREs.
    // We'll use this to track current figure start/close points, used to
    // properly calculate neighboring points within closed figures.
    System::Collections::Generic::List<int>^ figureStartClose = gcnew System::Collections::Generic::List<int>();

    {
        int lastMoveTo = -1;
        for (int i = 0; i < nCount; i++)
        {
            if ((pTypes[i] & PT_TYPEMASK) == PT_MOVETO)
            {
                // start of figure
                lastMoveTo = i;
            }
            else if (pTypes[i] & PT_CLOSEFIGURE)
            {
                // close of figure.
                // figure is from this point and last PT_MOVETO inclusive.
                Debug::Assert(lastMoveTo != -1);

                figureStartClose->Add(lastMoveTo);
                figureStartClose->Add(i);
            }
        }
    }

    // current figureStartClose index, indicating current figure start and close point
    int nextFigureStartCloseIndex = 0;
    int figureStartPoint = -1;
    int figureClosePoint = -1;

    for (int i = 0; i < nCount; i ++)
    {
        // Get figure start and close points for use in wrapping indices to
        // find neighbor points. Update if we've reached new figure (i > figure's close point)
        if (i > figureClosePoint)
        {
            if ((nextFigureStartCloseIndex+1) < figureStartClose->Count)
            {
                figureStartPoint = figureStartClose[nextFigureStartCloseIndex];
                figureClosePoint = figureStartClose[nextFigureStartCloseIndex+1];

                nextFigureStartCloseIndex += 2;
            }
        }

        //
        // For point i, find its neighboring points. The indices may wrap around if
        // figure is closed.
        //
        bool corner = true;
        int prevIndex = i - 1;
        int nextIndex = i + 1;

        if (figureStartPoint != -1 && figureClosePoint != -1)
        {
            // check for closed figure wrapping.
            if (prevIndex < figureStartPoint)
            {
                //
                // Fix bug 1334425: MGC: Miter incorrect when last point == start point and IsClosed == true
                //
                // If close point is same as start point, ignore it and use point previous to close in
                // mitering calculations.
                //
                PointI s = pPoints[figureStartPoint];
                PointI e = pPoints[figureClosePoint];

                if (figureClosePoint > figureStartPoint && s.x == e.x && s.y == e.y)
                {
                    prevIndex = figureClosePoint - 1;
                }
                else
                {
                    prevIndex = figureClosePoint;
                }
            }

            if (nextIndex > figureClosePoint)
            {
                nextIndex = figureStartPoint;
            }
        }
        else if (prevIndex < 0 || nextIndex >= nCount)
        {
            // figure is not closed. edge points cannot be corners.
            corner = false;
        }

        if (corner) // We have an angle when there are 3 points
        {
            // cos(a) = dot/r^2
            PointI p = pPoints[prevIndex];
            PointI q = pPoints[i];   // corner point
            PointI r = pPoints[nextIndex];
           
            int dx1 = p.x - q.x; // vector q -> p
            int dy1 = p.y - q.y;

            int dx2 = r.x - q.x; // vector q -> r
            int dy2 = r.y - q.y;

            // Neither is 0 length
            if (((dx1 != 0) || (dy1 != 0)) && ((dx2 != 0) || (dy2 != 0)))
            {
                double dot = ((double)dx1) * dx2 + ((double)dy1) * dy2;
                double r2  = Math::Sqrt((((double)dx1) * dx1 + ((double)dy1) * dy1) * 
                                       (((double)dx2) * dx2 + ((double)dy2) * dy2));

                double cos = dot / r2;

                if (cos > maxcos)
                {
                    maxcos = cos;
                }
            }
        }
    }
    
    return maxcos;
}

/**************************************************************************\
*
* Function Description:
*    inline function to calculate min and max values of a sequence
*
* Return Value:
*    update valmin and valmax through reference
*
* Created:
*    9/28/2001 fyuan
*
\**************************************************************************/

inline void UpdateMinMax(
    IN LONG nValue,         // input vale
    interior_ptr<int> pnMin,        // current minimum
    interior_ptr<int> pnMax)        // current maximum
{
    if (nValue < *pnMin)
    {
        *pnMin = nValue;
    }
    else if (nValue > *pnMax)
    {
        *pnMax = nValue;
    }
}


/**************************************************************************\
*
* Function Description:
*    Initialize member variables to actual meaningful values
*
* Return Value:
*    All member variables initialized to input values
*
* Created:
*    9/28/2001 fyuan
*
\**************************************************************************/

void CPolyPolygon::Set(
    array<PointI>^ rgptVertex,     // all coordinates
    int                 offsetP,
    array<unsigned int> ^   rgcPoly,        // array of vertex count
    int                 offsetC,
    int                 cPolygons       // number of polygons
    )
{
    m_rgptVertex = rgptVertex;
    m_offsetP    = offsetP;

    m_rgcPoly    = rgcPoly;
    m_offsetC    = offsetC;
    
    m_cPolygons  = cPolygons;
}


/**************************************************************************\
*
* Function Description:
*    Calculate Polypolygon bounding box
*
* Return Value:
*    m_rcBounds changed
*
* Created:
*    9/28/2001 fyuan
*
\**************************************************************************/

void CPolyPolygon::GetBounds(void)
{
    m_topleft     = m_rgptVertex[m_offsetP];
    m_bottomright = m_rgptVertex[m_offsetP];
    
    INT nTotal = 0;

    for (INT i = 0; i < m_cPolygons; i++)
    {
        nTotal += m_rgcPoly[m_offsetC + i];
    }

    Debug::Assert(nTotal >= 1);

    for (int i = 1; i < nTotal; i++)
    {
        UpdateMinMax(m_rgptVertex[m_offsetP + i].x, &m_topleft.x, &m_bottomright.x);
        UpdateMinMax(m_rgptVertex[m_offsetP + i].y, &m_topleft.y, &m_bottomright.y);
    }
}


/**************************************************************************\
*
* Function Description:
*    divide CPolyPolygon into multiple
*
* Return Value:
*    * pPolygons changed
*
* Created:
*    9/28/2001 fyuan
*
\**************************************************************************/

void CPolyPolygon::Divide(
    array<CPolyPolygon ^> ^ pPolygons,    // pointer to CPolyPolygon array
    IN INT cGroup                   // number of CPolyPolygon to divide into
    )
{
    Debug::Assert(m_cPolygons >= cGroup);

    int part = m_cPolygons / cGroup;        // number of polygons per group  

    int offsetP = 0;

    for (int n = 0; n < cGroup; n ++)
    {
        int num = part;

        if (n == (cGroup - 1))                // last group get the remainder
        {
            num = m_cPolygons - n * part;
        }

        pPolygons[n]->Set(m_rgptVertex, m_offsetP + offsetP, m_rgcPoly, n * part, num);
        pPolygons[n]->GetBounds();

        if (n != (cGroup - 1))
        {
            for (int i = 0; i < part; i ++)   // move to starting of next group
            {
                offsetP += m_rgcPoly[m_offsetC + n * part + i];
            }
        }
    }
}


/**************************************************************************\
*
* Function Description:
*    Check if two PolyPolygs's bounding boxes are disjoint
*
* Return Value:
*    TRUE if disjoint, FALSE otherwise
*
* Created:
*    9/28/2001 fyuan
*
\**************************************************************************/

bool
CPolyPolygon::DisJoint(
    CPolyPolygon ^poly2   // second CPolyPolygon to compare with
    )
{
    // GDI uses right/bottom exclusive. So it's safe to use >= amd <=.
    return (m_topleft.x   >= poly2->m_bottomright.x)  ||
           (m_topleft.y   >= poly2->m_bottomright.y) ||
           (m_bottomright.x <= poly2->m_topleft.x)   ||
           (m_bottomright.y <= poly2->m_topleft.y);
}
    

/**************************************************************************\
*
* Function Description:
*    Calculate if a group of PolyPolygons are mutually disjoint
*
* Return Value:
*    TRUE if all disjoint, FALSE if any two touches
*
* Created:
*    9/28/2001 fyuan
*
\**************************************************************************/

bool 
CPolyPolygon::DisJoint(
    array<CPolyPolygon ^> ^pPolygons, // array of CPolyPolygon
    IN INT cGroup                     // number of CPolyPolygon 
    )
{
    for (INT i = 0; i < cGroup; i ++)
    {
        for (INT j = i + 1; j < cGroup; j ++)
        {
            if (! pPolygons[i]->DisJoint(pPolygons[j]))
            {
                return false;
            }
        }
    }

    return true; // no group touches another portion
}


/**************************************************************************\
*
* Function Description:
*    Divide a PolyPolygon into disjoint groups if possible, and recursively
*    draw each group; or draw as a whole
*
* Return Value:
*    Call GDI commands. TRUE if success, FALSE otherwise
*
* Created:
*    9/28/2001 fyuan
*
\**************************************************************************/

const int c_LARGEPOLYPOLYGON = 32;
const int c_GROUPS           = 8;

HRESULT
CPolyPolygon::Draw(
    CGDIDevice ^ dc          // device context to draw on
    )
{
    if (m_cPolygons >= c_LARGEPOLYPOLYGON)    // if more than 32 polygons
    {
        array<CPolyPolygon^> ^ rgRegion = gcnew array<CPolyPolygon ^>(c_GROUPS);

        for (int j = 0; j < c_GROUPS; j ++)
        {
            rgRegion[j] = gcnew CPolyPolygon();
        }

        Divide(rgRegion, c_GROUPS); // divide more than 2 for drawing closed shape

        if (DisJoint(rgRegion, c_GROUPS))       // if none touches each other
        {
            HRESULT hr = S_OK;

            for (int i = 0; i < c_GROUPS && SUCCEEDED(hr); i++) // recursive for each group
            {
                hr = rgRegion[i]->Draw(dc);
            }

            return hr;
        }
        else
        {
            // call original GDI function if divided group touch each other
            return dc->PolyPolygon(m_rgptVertex, m_offsetP, m_rgcPoly, m_offsetC, m_cPolygons) != 0;
        }
    }
    else
    {
        // call original GDI function if there are less than 32 polygons
        return dc->PolyPolygon(m_rgptVertex, m_offsetP, m_rgcPoly, m_offsetC, m_cPolygons) != 0;
    }
}
