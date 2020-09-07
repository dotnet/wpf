// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;
using System.Xml;
using System.ComponentModel;

using System.Windows;
using System.Windows.Automation;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps.Packaging;
using Microsoft.Internal.AlphaFlattener;

using System.Security;
using MS.Utility;

namespace System.Windows.Xps.Serialization
{
    #region class VisualSerializer
    /// <summary>
    /// Visual Serializer
    /// </summary>
    internal class VisualSerializer: IMetroDrawingContext
    {
        public const double PrecisionDPI = 9600; // Numeric values generated should be at least precise to that resolution

        // XPS Specification and Reference Guide
        // 11.2 Implementation Limits
        // Producers SHOULD produce only XPS Documents that stay within these implementation limits

        public const double PositiveLargestFloat  =  1e38;
        public const double NegativeLargestFloat  = -1e38;

        public const double PositiveSmallestFloat =  2e-38; // spec says 1e-38, but it's not representable in float
        public const double NegativeSmallestFloat = -2e-38;

        public const int    MaxElementCount       = 1000 * 1000;
        public const int    MaxPointCount         =  100 * 1000;
        public const int    MaxResourceCount      =   10 * 1000;
        public const int    MaxGlyphCount         =    5 * 1000;

        public const int    MaxGradientStops      = 100;

        #region Constructor
        /// <summary>
        /// Constructor for DrawingContext which accepts the context with which this
        /// instance should be affiliated.
        /// </summary>
        /// <param name="resWriter"> XmlWriter for resource</param>
        /// <param name="bodyWriter"> XmlWriter for body</param>
        /// <param name="manager"></param>
        internal VisualSerializer(System.Xml.XmlWriter resWriter, System.Xml.XmlWriter bodyWriter, PackageSerializationManager manager)
        {
            _objects = new ArrayList();
            _objnams = new ArrayList();

            _resWriter  = resWriter;
            _bodyWriter = bodyWriter;
            _manager    = manager;

            if (_manager != null)
            {
                _context = new XpsTokenContext(_manager, null, null);
            }

            _writer     = _bodyWriter;
            _tcoStack   = new Stack();

            SetCoordinateFormat(1);
        }

        #endregion Constructor

        #region protected methods

        protected double CheckFloat(double v)
        {
            if (v > 0)
            {
                if (v < PositiveSmallestFloat)
                {
                    return 0;
                }

                if (v > PositiveLargestFloat)
                {
                    _exceedFloatLimit = true;
                }
            }
            else if (v < 0)
            {
                if (v > NegativeSmallestFloat)
                {
                    return 0;
                }

                if (v < NegativeLargestFloat)
                {
                    _exceedFloatLimit = true;
                }
            }

            return v;
        }

        void ReportLimitViolation()
        {
            if (_exceedFloatLimit)
            {
                _writer.WriteComment("XPSLimit:FloatRange");
                _exceedFloatLimit = false;
            }

            if (_exceedPointLimit)
            {
                _writer.WriteComment("XPSLimit:PointCount");
                _exceedPointLimit = false;
            }
        }

        protected void AppendCoordinate(StringBuilder rslt, double v)
        {
            v = CheckFloat(v);

            if (_forceGeneral > 0) // precision control ignored
            {
                rslt.Append(v.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                rslt.Append(v.ToString(_coordFormat, CultureInfo.InvariantCulture));
            }
        }

        protected void AppendPoint(StringBuilder builder, Point p, Matrix mat)
        {
            if (!mat.IsIdentity)
            {
                p = mat.Transform(p);
            }

            AppendCoordinate(builder, p.X);
            builder.Append(',');
            AppendCoordinate(builder, p.Y);
        }

        protected int AppendPoints(StringBuilder builder, PointCollection pc, Matrix mat)
        {
            bool first = true;

            foreach (Point p in pc)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(' ');
                }

                AppendPoint(builder, p, mat);
            }

            return pc.Count;
        }

        protected string GetString(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            StringBuilder rslt = new StringBuilder();

            while (true)
            {
                DoubleCollection dc = obj as DoubleCollection;

                if (dc != null)
                {
                    foreach (double d in dc)
                    {
                        if (rslt.Length != 0)
                        {
                            rslt.Append(' ');
                        }

                        rslt.Append(CheckFloat(d).ToString(CultureInfo.InvariantCulture));
                    }

                    break;
                }

                PointCollection pc = obj as PointCollection;

                if (pc != null)
                {
                    foreach (Point p in pc)
                    {
                        if (rslt.Length != 0)
                        {
                            rslt.Append(' ');
                        }

                        AppendPoint(rslt, p, Matrix.Identity);
                    }

                    break;
                }

                if (obj is Matrix)
                {
                    AppendMatrix(rslt, (Matrix)obj);
                }
                else if (obj is Point)
                {
                    AppendPoint(rslt, (Point)obj, Matrix.Identity);
                }
                else if (obj is double)
                {
                    Double d = CheckFloat((double) obj);

                    rslt.Append(d.ToString(CultureInfo.InvariantCulture));
                }
                else if (obj is Uri)
                {
                    rslt.Append(GetUriAsString((Uri)obj));
                }
                else
                {
                    rslt.Append(Convert.ToString(obj, CultureInfo.InvariantCulture));
                }

                break;
            }

            if (rslt.Length == 0)
            {
                return null;
            }
            else
            {
                return rslt.ToString();
            }
        }

        /// <summary>
        /// Search for cached brush
        /// </summary>
        protected string FindBrush(Brush brush, Rect bounds)
        {
            Debug.Assert(!BrushProxy.IsEmpty(brush), "Should not be serializing empty brush");

            StringBuilder sbBrush;

            _forceGeneral++;

            try
            {
                sbBrush = BrushToString(brush, bounds);
            }
            finally
            {
                _forceGeneral--;
            }

            string sBrush = sbBrush.ToString();

            for (int i = 0; i < _objects.Count; i++)
            {
                if ((string) _objects[i] == sBrush)
                {
                    return _objnams[i] as string;
                }
            }

            _objects.Add(sBrush);
            _objnams.Add("b" + _brushId);

            // Replace brush ID place holder with the real ID:
            //   Can't put ID in for brush reuse.
            //   Can't replace within the whole StringBuilder because of possible appearance of bxx in GlyphRun

            // <LinearGradientBrush x:Key="bxx"  ->
            // <LinearGradientBrush x:Key="bid"  ->
            sbBrush = sbBrush.Replace("bxx", "b" + _brushId, 0, 40);

            _brushId++;

            _resWriter.WriteRaw(sbBrush.ToString());
            _resWriter.WriteWhitespace("\r\n");

            return _objnams[_objects.Count - 1] as string;
        }


        // Output an object
        protected void WriteAttr(string attribute, object val)
        {
            _writer.WriteAttributeString(attribute, GetString(val));
        }

        // Output an object if it's not the same as default
        protected void WriteAttr(string attribute, object val, object valDefault)
        {
            string sval = GetString(val);

            if (sval != GetString(valDefault))
            {
                _writer.WriteAttributeString(attribute, sval);
            }
        }

        protected string ColorToString(Color color)
        {
            string colorString = "#00FFFFFF";

            TypeConverter converter = _manager.GetTypeConverter(typeof(Color));

            if (converter != null)
            {
                colorString = converter.ConvertTo(_context, CultureInfo.InvariantCulture, color, typeof(string)) as string;
            }

            return colorString;
        }

        // Convert simple brush to inline string
        protected string SimpleBrushToString(Brush brush)
        {
            SolidColorBrush solidBrush = brush as SolidColorBrush;

            if (solidBrush != null) // SolidColorBrush
            {
                // Scale will normalize colors
                Color color = Utility.Scale(solidBrush.Color, solidBrush.Opacity);

                return ColorToString(color);
            }

            return null;
        }

        // Write GradientStopCollection
        protected void WriteGradientStops(string prefix, GradientStopCollection gsc)
        {
            _writer.WriteStartElement(prefix + ".GradientStops");

            int count = gsc.Count;

            if (count > 0)
            {
                bool[] taken = new bool[count];

                // Sort gradientstops according to offsets, without changing order of stops with the same offset
                for (int i = 0; i < count; i++)
                {
                    int    pos = -1;
                    double val = double.MaxValue;

                    // Find the first, free, smallest offset
                    for (int j = count - 1; j >= 0; j--)
                    {
                        if (!taken[j])
                        {
                            // treat NaN offset as MaxValue when sorting
                            double offset = gsc[j].Offset;

                            if (double.IsNaN(offset))
                            {
                                offset = double.MaxValue;
                            }

                            if (offset <= val)
                            {
                                pos = j;
                                val = offset;
                            }
                        }
                    }

                    Debug.Assert(pos >= 0, "Missing offset");

                    GradientStop stop = gsc[pos];

                    taken[pos] = true;

                    _writer.WriteStartElement("GradientStop");
                    WriteAttr("Color", ColorToString(stop.Color));
                    WriteAttr("Offset", stop.Offset);
                    _writer.WriteEndElement();
                }
            }

            _writer.WriteEndElement();

            if (count > MaxGradientStops)
            {
                _writer.WriteComment("XPSLimit:GradientStopCount");
            }
        }

        private void WriteBrushHeader(string element, Brush brush)
        {
            _writer.WriteStartElement(element);

            WriteAttr("x:Key", "bxx");
            WriteAttr("Opacity", brush.Opacity, 1.0);
        }

        static Rect UnitRect = new Rect(0, 0, 1, 1);

        private void WriteTileBrush(string element, TileBrush brush, Rect bounds)
        {
            WriteBrushHeader(element, brush);

            BrushMappingMode mapmode = brush.ViewportUnits;

            if (mapmode == BrushMappingMode.Absolute)
            {
                bounds = UnitRect;
            }

            mapmode = BrushMappingMode.Absolute;

            WriteAttr("ViewportUnits", mapmode);
            WriteAttr("TileMode",      brush.TileMode);

            // Remove AlignmentX/AlignmentY.
            // Or more precisely, change Viewbox/ViewPort so center alignment would replace current setting
            double dstwidth  = brush.Viewport.Width  * bounds.Width;
            double dstheight = brush.Viewport.Height * bounds.Height;

            Rect vb = Utility.GetTileAbsoluteViewbox(brush);

            double srcwidth  = vb.Width;
            double srcheight = vb.Height;

            double scalex;
            double scaley;

            bool adjustViewport = true; // viewport

            switch (brush.Stretch)
            {
                case Stretch.None:
                    scalex = 1;
                    scaley = 1;

                    if (srcwidth > dstwidth || srcheight > dstheight)
                    {
                        //
                        // Fix bug 1326548: XPS serialization: Incorrect viewbox value in S0 after ImageBrush conversion
                        //
                        // Incorrect TileBrush serialization when Stretch.None, content larger than target bounds,
                        // and alignment specified. Need to adjust viewbox to handle alignment, not viewport.
                        //
                        adjustViewport = false;
                    }
                    break;

                case Stretch.Uniform:
                    scalex = Math.Min(dstwidth / srcwidth, dstheight / srcheight);
                    scaley = scalex;
                    break;

                case Stretch.UniformToFill:
                    scalex = Math.Max(dstwidth / srcwidth, dstheight / srcheight);
                    scaley = scalex;

                    // UniformToFill normally maps Viewbox to an area which is larger than Viewport.
                    // So we need to adjust Viewbox to take care of alignment
                    adjustViewport = false; // need to adjust viewbox
                    break;

                case Stretch.Fill:
                default:
                    scalex = dstwidth  / srcwidth;
                    scaley = dstheight / srcheight;
                    break;
            }

            double width  = srcwidth  * scalex;
            double height = srcheight * scaley;

            double dx, dy;

            // Calculate differeences between current alignments and the default center alignment

            switch (brush.AlignmentX)
            {
                case AlignmentX.Left:
                    dx = - (dstwidth - width) / 2;
                    break;

                case AlignmentX.Right:
                    dx = (dstwidth - width) / 2;
                    break;

                case AlignmentX.Center:
                default:
                    dx = 0;
                    break;
            }

            switch (brush.AlignmentY)
            {
                case AlignmentY.Top:
                    dy = - (dstheight - height) / 2;
                    break;

                case AlignmentY.Bottom:
                    dy = (dstheight - height) / 2;
                    break;

                case AlignmentY.Center:
                default:
                    dy = 0;
                    break;
            }

            //WriteAttr("AlignmentX",   brush.AlignmentX);
            //WriteAttr("AlignmentY",   brush.AlignmentY);
            WriteAttr("ViewboxUnits", BrushMappingMode.Absolute);

            Rect vp = brush.Viewport;

            vp = new Rect(bounds.X + vp.X * bounds.Width,
                          bounds.Y + vp.Y * bounds.Height,
                          vp.Width  * bounds.Width,
                          vp.Height * bounds.Height);

            if (adjustViewport)
            {
                vp = new Rect(vp.Left + dx, vp.Top + dy, vp.Width, vp.Height);
            }
            else
            {
                // Change direction and scale to viewbox coordinate space
                vb = new Rect(vb.Left - dx / scalex, vb.Top - dy / scaley, vb.Width, vb.Height);
            }

            // Adjusting Viewbox so that Stretch can be changed to "Fill"

            // Calculate real width/height being used in stretching
            double w1 = vp.Width  / scalex;
            double h1 = vp.Height / scaley;

            vb = new Rect(vb.Left + (vb.Width  - w1) / 2,   // center alignment, could be negative
                          vb.Top  + (vb.Height - h1) / 2,   // center alignment, could be negative
                          w1,
                          h1);

            WriteAttr("Viewbox",  vb.ToString(CultureInfo.InvariantCulture));
            WriteAttr("Viewport", vp.ToString(CultureInfo.InvariantCulture));
        }

        void SaveResetState()
        {
            // Save critical state info
            _tcoStack.Push(_opacity);
            _tcoStack.Push(_opacityMask);
            _tcoStack.Push(_transform);
            _tcoStack.Push(_clip);

            // Reset critical state info
            _opacity     = 1;
            _opacityMask = null;
            _transform   = null;
            _clip        = null;
        }

        void RestoreState()
        {
            // Restore critical state info
            _clip        = _tcoStack.Pop() as Geometry;
            _transform   = _tcoStack.Pop() as Transform;
            _opacityMask = _tcoStack.Pop() as Brush;
            _opacity     = (double)_tcoStack.Pop();
        }

        // Output primitives in drawing
        private void WriteDrawingBody(System.Windows.Media.Drawing drawing, Matrix worldTransform)
        {
            SaveResetState();

            _writer.WriteStartElement("Canvas");

            VisualTreeFlattener vtf = new VisualTreeFlattener(this, _pageSize, new TreeWalkProgress());

            vtf.DrawingWalk(drawing, worldTransform);

            _writer.WriteEndElement();

            RestoreState();
        }

        protected void WriteBitmap(string attribute, ImageSource imageSource)
        {
            if ((imageSource != null) && (imageSource.Height > 0) && (imageSource.Width > 0))
            {
                string bitmapUri = null;

                TypeConverter converter = _manager.GetTypeConverter(typeof(BitmapSource));

                if (converter != null)
                {
                    ColorConvertedBitmap colorConvertedBitmap = imageSource as ColorConvertedBitmap;

                    if ( colorConvertedBitmap!=null )
                    {
                        if (colorConvertedBitmap.Source is FormatConvertedBitmap)
                        {
                            FormatConvertedBitmap formatConvertedBitmap = colorConvertedBitmap.Source as FormatConvertedBitmap;
                            if (formatConvertedBitmap.Source is BitmapFrame)
                            {
                                imageSource = formatConvertedBitmap.Source;
                            }
                            else
                            {
                                colorConvertedBitmap = null;
                            }
                        }
                        else if (colorConvertedBitmap.Source is BitmapFrame)
                        {
                            imageSource = colorConvertedBitmap.Source;
                        }
                        else
                        {
                            colorConvertedBitmap = null;
                        }
                    }

                    Object obj = converter.ConvertTo(_context, null, imageSource, typeof(Uri));

                    Uri uri = obj as Uri;

                    if (uri != null)
                    {
                        bitmapUri = GetUriAsString(uri);
                    }
                    else
                    {
                        bitmapUri = obj as String;
                    }

                    if (colorConvertedBitmap != null)
                    {
                        string sourceProfile = ColorTypeConverter.SerializeColorContext(_context,colorConvertedBitmap.SourceColorContext);

                        bitmapUri = "{ColorConvertedBitmap " + bitmapUri + " " + sourceProfile;

                        if (new ColorContext(PixelFormats.Default) != colorConvertedBitmap.DestinationColorContext)
                        {
                            string destinationProfile = ColorTypeConverter.SerializeColorContext(_context, colorConvertedBitmap.DestinationColorContext);
                            bitmapUri = bitmapUri + " " + destinationProfile;
                        }

                        bitmapUri = bitmapUri + "}";
                    }
                }

                WriteAttr(attribute, bitmapUri);
            }
        }

        // Convert a brush to a StringBuilder
        protected StringBuilder BrushToString(Brush brush, Rect bounds)
        {
            StringWriter swriter = new StringWriter(CultureInfo.InvariantCulture);
            XmlTextWriter xwriter = new XmlTextWriter(swriter);

            xwriter.Formatting  = System.Xml.Formatting.Indented;
            xwriter.Indentation = 4;

            XmlWriter oldwriter = _writer;

            _writer = xwriter;

            while (true)
            {
                SolidColorBrush sb = brush as SolidColorBrush;

                if (sb != null) // SolidColorBrush
                {
                    WriteBrushHeader("SolidColorBrush", sb);
                    WriteAttr("Color", sb.Color);

                    break;
                }

                LinearGradientBrush lb = brush as LinearGradientBrush;

                if (lb != null)
                {
                    WriteBrushHeader("LinearGradientBrush", lb);

                    WriteTransform("Transform", lb.Transform, lb.RelativeTransform, bounds);

                    BrushMappingMode mapmode = lb.MappingMode;

                    if (mapmode == BrushMappingMode.Absolute)
                    {
                        bounds = UnitRect;
                    }

                    mapmode = BrushMappingMode.Absolute;

                    WriteAttr("StartPoint", Utility.MapPoint(bounds, lb.StartPoint));
                    WriteAttr("EndPoint", Utility.MapPoint(bounds, lb.EndPoint));
                    WriteAttr("ColorInterpolationMode", lb.ColorInterpolationMode);
                    WriteAttr("MappingMode", mapmode);

                    WriteAttr("SpreadMethod", lb.SpreadMethod);

                    WriteGradientStops("LinearGradientBrush", lb.GradientStops);

                    break;
                }

                RadialGradientBrush rb = brush as RadialGradientBrush;

                if (rb != null)
                {
                    WriteBrushHeader("RadialGradientBrush", rb);

                    WriteTransform("Transform", rb.Transform, rb.RelativeTransform, bounds);

                    BrushMappingMode mapmode = rb.MappingMode;

                    if (mapmode == BrushMappingMode.Absolute)
                    {
                        bounds = UnitRect;
                    }

                    mapmode = BrushMappingMode.Absolute;

                    WriteAttr("MappingMode", mapmode);

                    WriteAttr("SpreadMethod", rb.SpreadMethod);
                    WriteAttr("ColorInterpolationMode", rb.ColorInterpolationMode);

                    WriteAttr("Center", Utility.MapPoint(bounds, rb.Center));
                    WriteAttr("RadiusX", Math.Abs(rb.RadiusX * bounds.Width));
                    WriteAttr("RadiusY", Math.Abs(rb.RadiusY * bounds.Height));
                    WriteAttr("GradientOrigin", Utility.MapPoint(bounds, rb.GradientOrigin));

                    WriteGradientStops("RadialGradientBrush", rb.GradientStops);

                    break;
                }

                ImageBrush ib = brush as ImageBrush;

                if (ib != null)
                {
                    WriteTileBrush("ImageBrush", ib, bounds);

                    WriteBitmap("ImageSource", ib.ImageSource);

                    WriteTransform("Transform", ib.Transform, ib.RelativeTransform, bounds);

                    break;
                }

                DrawingBrush db = brush as DrawingBrush;

                if (db != null)
                {
                    WriteTileBrush("VisualBrush", db, bounds);

                    Matrix trans = Utility.MergeTransform(db.Transform, db.RelativeTransform, bounds);
                    WriteTransform("Transform", trans);

                    // Calculate approximate transformation from viewbox to world to serve as hint
                    // for bitmap effect rasterization size.
                    Matrix drawingToWorldTransform = Utility.CreateViewboxToViewportTransform(db, bounds);
                    drawingToWorldTransform.Append(trans);

                    _writer.WriteStartElement("VisualBrush.Visual");
                    WriteDrawingBody(db.Drawing, drawingToWorldTransform);
                    _writer.WriteEndElement();

                    break;
                }

                VisualBrush vb = brush as VisualBrush;

                if (vb != null)
                {
                    SaveResetState();

                    WriteTileBrush("VisualBrush", vb, bounds);

                    WriteTransform("Transform", vb.Transform, vb.RelativeTransform, bounds);

                    _writer.WriteStartElement("VisualBrush.Visual");

                    VisualTreeFlattener flattener = new VisualTreeFlattener(this, _pageSize, new TreeWalkProgress());

                    _writer.WriteStartElement("Canvas");

                    flattener.VisualWalk(vb.Visual);

                    _writer.WriteEndElement();

                    _writer.WriteEndElement();

                    RestoreState();

                    break;
                }

                {
                    Debug.Assert(false, "Brush not supported");
                    WriteBrushHeader(brush.GetType().ToString(), brush);
                }

                break;
            }

            _writer.WriteEndElement();
            _writer.Flush();

            _writer = oldwriter;

            return swriter.GetStringBuilder();
        }


#endregion

        #region Protected Fields

        protected System.Xml.XmlWriter _writer;
        protected System.Xml.XmlWriter _resWriter;
        protected System.Xml.XmlWriter _bodyWriter;

        protected int _brushId;  // = 0;
        protected int _bitmapId; // = 0;

        protected PackageSerializationManager  _manager;
        protected XpsTokenContext              _context;

        protected Stack     _tcoStack;     // Transform, Clip, Opacity stack

        // resource dictionary objects
        protected ArrayList _objects;
        protected ArrayList _objnams;

        // common properties to apply to next element write
        protected double    _opacity         = 1.0;
        protected Brush     _opacityMask; // = null;
        protected Transform _transform;   // = null;
        protected Geometry  _clip;        // = null;

        protected string    _coordFormat     = "#0.##";
        protected Matrix    _worldTransform  = Matrix.Identity;
        protected int       _forceGeneral; //= 0;

        // preserve serialization attributes
        protected String    _nameAttr;    // = null;
        protected Visual    _node;
        protected Uri       _navigateUri;

        protected Size      _pageSize;

        protected bool      _exceedFloatLimit;
        protected bool      _exceedPointLimit;
        protected int       _totalElementCount;

        #endregion Protected Fields

        #region Private methods

        // Writes a brush even if transparent.
        // Transparent brushes are used with hyperlink Paths.
        protected void WriteBrush(string attribute, Brush brush, Rect bounds)
        {
            if (brush != null)
            {
                string str = SimpleBrushToString(brush);

                if (str != null)
                {
                    _writer.WriteAttributeString(attribute, str);
                }
                else
                {
                    string ob = FindBrush(brush, bounds);

                    if ((_manager != null) || (_forceGeneral >= 1) ) // to container | within resource dictionary
                    {
                        _writer.WriteAttributeString(attribute, "{StaticResource " + ob + "}");
                    }                     // to loose files
                    else
                    {
                        _writer.WriteAttributeString(attribute, "{DynamicResource " + ob + "}");
                    }
                }
            }
        }

        // Writes a pen even if transparent (but not if null).
        // Transparent pen still affects rendering, since it'll shrink the fill.
        protected void WritePen(Pen pen, Rect bounds, bool isLineGeometry)
        {
            if (pen != null && !PenProxy.IsNull(pen))
            {
                WriteBrush("Stroke", pen.Brush, bounds);
                WriteAttr("StrokeThickness", Math.Abs(pen.Thickness));

                WriteAttr("StrokeStartLineCap", pen.StartLineCap, PenLineCap.Flat);
                WriteAttr("StrokeEndLineCap", pen.EndLineCap, PenLineCap.Flat);

                if (!isLineGeometry)
                {
                    // not a single line segment, properties affecting line joins are relevant
                    if (pen.LineJoin == PenLineJoin.Miter)
                    {
                        WriteAttr("StrokeMiterLimit", Math.Max(1.0, pen.MiterLimit));
                    }
                    else
                    {
                        WriteAttr("StrokeLineJoin", pen.LineJoin);
                    }
                }

                if ((pen.DashStyle != null) && (pen.DashStyle.Dashes.Count != 0))
                {
                    WriteAttr("StrokeDashCap", pen.DashCap);

                    WriteAttr("StrokeDashOffset", pen.DashStyle.Offset);

                    //
                    // If there are an odd number of elements in StrokeDashArray
                    // duplicate the elements.  This results in the repeating pattern
                    // demonstrated by odd elements in pen.
                    //
                    DoubleCollection dashes = new DoubleCollection();
                    foreach( double d in pen.DashStyle.Dashes )
                    {
                        dashes.Add(Math.Abs(d));
                    }
                    if( pen.DashStyle.Dashes.Count%2 == 0 )
                    {
                        WriteAttr("StrokeDashArray", dashes);
                    }
                    else
                    {
                        string doubleString = GetString(dashes)+" "+GetString(dashes);
                        _writer.WriteAttributeString("StrokeDashArray",doubleString );
                    }
                }
            }
        }

        static private bool IsUniformScale(Matrix mat)
        {
            if (mat.IsIdentity)
            {
                return true;
            }
            else
            {
                return Utility.IsZero(mat.M11 - mat.M22) &&
                       Utility.IsZero(mat.M12) &&
                       Utility.IsZero(mat.M21);
            }
        }

        // convert Matrix to string
        private void AppendMatrix(StringBuilder rslt, Matrix mat)
        {
            if (!Utility.IsIdentity(mat))
            {
                rslt.Append(CheckFloat(mat.M11).ToString(CultureInfo.InvariantCulture)); rslt.Append(",");
                rslt.Append(CheckFloat(mat.M12).ToString(CultureInfo.InvariantCulture)); rslt.Append(",");
                rslt.Append(CheckFloat(mat.M21).ToString(CultureInfo.InvariantCulture)); rslt.Append(",");
                rslt.Append(CheckFloat(mat.M22).ToString(CultureInfo.InvariantCulture)); rslt.Append(",");
                rslt.Append(CheckFloat(mat.OffsetX).ToString(CultureInfo.InvariantCulture)); rslt.Append(",");
                rslt.Append(CheckFloat(mat.OffsetY).ToString(CultureInfo.InvariantCulture));
            }
        }

        private void WritePathFigureCollection(PathFigureCollection figures, bool forFill, bool forStroke)
        {
            int pc = 0;

            foreach (PathFigure p in figures)
            {
                // When filling only, skip not filled figures
                if (forFill && !forStroke && !p.IsFilled)
                {
                    continue;
                }

                _writer.WriteStartElement("PathFigure");

                pc ++;

                WriteAttr("StartPoint", p.StartPoint);
                WriteBool("IsClosed", p.IsClosed);

                if (!p.IsFilled)
                {
                    WriteAttr("IsFilled", "false");
                }

                PathSegmentCollection segments = p.Segments;

                int count = segments.Count;

                for (int i = 0; i < count; i ++)
                {
                    PathSegment ps = segments[i];

                    // When stroking only, skip non-stroked segment if next segment is not either
                    if (forStroke && !forFill && !ps.IsStroked)
                    {
                        // Non-stroked segments is still useful for providing starting point,
                        // so they can't be totally skipped
                        if ((i < count - 1) && !segments[i + 1].IsStroked)
                        {
                            continue;
                        }
                    }

                    PolyLineSegment pl = ps as PolyLineSegment;

                    if (pl != null)
                    {
                        _writer.WriteStartElement("PolyLineSegment");
                        WriteAttr("Points", pl.Points);
                        pc += pl.Points.Count;
                    }
                    else if (ps is PolyBezierSegment)
                    {
                        PolyBezierSegment l = ps as PolyBezierSegment;

                        _writer.WriteStartElement("PolyBezierSegment");
                        WriteAttr("Points", l.Points);
                        pc += l.Points.Count;
                    }
                    else if (ps is LineSegment)
                    {
                        LineSegment l = ps as LineSegment;

                        _writer.WriteStartElement("PolyLineSegment");
                        WriteAttr("Points", l.Point);
                        pc ++;
                    }
                    else if (ps is BezierSegment)
                    {
                        BezierSegment b = ps as BezierSegment;

                        _writer.WriteStartElement("PolyBezierSegment");

                        StringBuilder rslt = new StringBuilder();

                        AppendPoint(rslt, b.Point1, Matrix.Identity);
                        rslt.Append(' ');
                        AppendPoint(rslt, b.Point2, Matrix.Identity);
                        rslt.Append(' ');
                        AppendPoint(rslt, b.Point3, Matrix.Identity);

                        _writer.WriteAttributeString("Points", rslt.ToString());
                        pc += 3;
                    }
                    else if (ps is ArcSegment)
                    {
                        ArcSegment a = ps as ArcSegment;

                        if (a.Size.IsEmpty || a.Size.Width == 0 || a.Size.Height == 0)
                        {
                            // empty size results in line segment
                            _writer.WriteStartElement("PolyLineSegment");
                            WriteAttr("Points", a.Point);
                            pc ++;
                        }
                        else
                        {
                            _writer.WriteStartElement("ArcSegment");

                            WriteAttr("Point", a.Point);
                            WriteAttr("Size", a.Size);
                            WriteAttr("RotationAngle", a.RotationAngle);
                            WriteBool("IsLargeArc", a.IsLargeArc);
                            WriteAttr("SweepDirection", a.SweepDirection);
                            pc += 2;
                        }
                    }
                    else if (ps is QuadraticBezierSegment)
                    {
                        QuadraticBezierSegment b = ps as QuadraticBezierSegment;

                        _writer.WriteStartElement("PolyQuadraticBezierSegment");

                        StringBuilder rslt = new StringBuilder();

                        AppendPoint(rslt, b.Point1, Matrix.Identity);
                        rslt.Append(' ');
                        AppendPoint(rslt, b.Point2, Matrix.Identity);

                        _writer.WriteAttributeString("Points", rslt.ToString());
                        pc += 2;
                    }
                    else if (ps is PolyQuadraticBezierSegment)
                    {
                        PolyQuadraticBezierSegment b = ps as PolyQuadraticBezierSegment;

                        _writer.WriteStartElement("PolyQuadraticBezierSegment");

                        WriteAttr("Points", b.Points);
                        pc += b.Points.Count;
                    }
                    else
                    {
                        _writer.WriteStartElement(ps.ToString() + "PathSegment not handled");
                    }

                    if (!ps.IsStroked)
                    {
                        WriteAttr("IsStroked", "false");
                    }

                    _writer.WriteEndElement();
                }

                _writer.WriteEndElement();
            }

            if (pc > MaxPointCount)
            {
                _exceedPointLimit = true;
            }
        }

        // Check if brush and/or pen actually paint anything visible
        static private bool Visible(Brush brush, Pen pen)
        {
            if (brush != null)
            {
                if (!BrushProxy.IsEmpty(brush))
                {
                    return true;
                }
            }

            if (pen != null)
            {
                if ((pen.Brush != null) && !BrushProxy.IsEmpty(pen.Brush))
                {
                    return true;
                }
            }

            return false;
        }

        static private char Ord(bool b)
        {
            if (b)
            {
                return '1';
            }
            else
            {
                return '0';
            }
        }

        static private char Ord(SweepDirection d)
        {
            if (d == SweepDirection.Clockwise)
            {
                return '1';
            }
            else
            {
                return '0';
            }
        }

        // Convert a simple PathGeometry to a string which can be inlined.
        // Return null if it does not fit short-hand syntax
        private string PathGeometryToString(PathGeometry path, Matrix map, bool forFill, bool forStroke)
        {
            if ((path.Transform != null) && !Utility.IsIdentity(path.Transform))
            {
                map = path.Transform.Value * map;
            }

            PathFigureCollection figures = path.Figures;

            StringBuilder rslt = new StringBuilder();

            int pc = 0;

            foreach (PathFigure p in figures)
            {
                if (!p.IsFilled)
                {
                    // Mini-language does not support IsFilled=false
                    if (forStroke && forFill)
                    {
                        return null;
                    }

                    // When filling only, skip not filled PathFigure
                    if (forFill)
                    {
                        continue;
                    }

                    // When stroking only, ignore IsFilled flag
                }

                PathSegmentCollection segments = p.Segments;

                // Start point
                if (rslt.Length == 0)
                {
                    if (path.FillRule == FillRule.EvenOdd)
                    {
                        // EvenOdd is the default, don't write anything
                    }
                    else
                    {
                        rslt.Append("F1");
                    }
                }

                rslt.Append('M');

                AppendPoint(rslt, p.StartPoint, map);
                pc ++;

                // Segments
                foreach (PathSegment ps in segments)
                {
                    if (forStroke)
                    {
                        if (!ps.IsStroked || ps.IsSmoothJoin)
                        {
                            // Mini-language does not support IsStroked=false, IsSmoothJoin=false
                            return null;
                        }
                    }

                    if (ps is PolyLineSegment)
                    {
                        PolyLineSegment l = ps as PolyLineSegment;

                        rslt.Append('L');
                        pc += AppendPoints(rslt, l.Points, map);
                    }
                    else if (ps is PolyBezierSegment)
                    {
                        PolyBezierSegment l = ps as PolyBezierSegment;

                        rslt.Append('C');
                        pc += AppendPoints(rslt, l.Points, map);
                    }
                    else if (ps is LineSegment)
                    {
                        LineSegment l = ps as LineSegment;

                        rslt.Append('L');
                        AppendPoint(rslt, l.Point, map);
                        pc ++;
                    }
                    else if (ps is BezierSegment)
                    {
                        BezierSegment b = ps as BezierSegment;

                        rslt.Append('C');
                        AppendPoint(rslt, b.Point1, map);
                        rslt.Append(' ');
                        AppendPoint(rslt, b.Point2, map);
                        rslt.Append(' ');
                        AppendPoint(rslt, b.Point3, map);
                        pc += 3;
                    }
                    else if (ps is ArcSegment)
                    {
                        if (IsUniformScale(map))
                        {
                            ArcSegment a = ps as ArcSegment;

                            Size s = a.Size;

                            if (s.IsEmpty || s.Width == 0 || s.Height == 0)
                            {
                                // empty size results in line segment
                                rslt.Append('L');
                                AppendPoint(rslt, a.Point, map);
                                pc ++;
                            }
                            else
                            {
                                rslt.Append('A');
                                AppendPoint(rslt, new Point(s.Width * map.M11, s.Height * map.M22), Matrix.Identity);
                                rslt.Append(' ');
                                rslt.Append(a.RotationAngle);
                                rslt.Append(' ');
                                rslt.Append(Ord(a.IsLargeArc));
                                rslt.Append(' ');
                                rslt.Append(Ord(a.SweepDirection));
                                rslt.Append(' ');
                                AppendPoint(rslt, a.Point, map);
                                pc += 2;
                            }
                        }
                        else
                        {
                            // ArcSegment can only be transformed inline by uniform scaling + translation
                            return null;
                        }
                    }
                    else if (ps is QuadraticBezierSegment)
                    {
                        QuadraticBezierSegment b = ps as QuadraticBezierSegment;

                        rslt.Append('Q');
                        AppendPoint(rslt, b.Point1, map);
                        rslt.Append(' ');
                        AppendPoint(rslt, b.Point2, map);
                        pc += 2;
                    }
                    else if (ps is PolyQuadraticBezierSegment)
                    {
                        PolyQuadraticBezierSegment l = ps as PolyQuadraticBezierSegment;

                        rslt.Append('Q');
                        pc += AppendPoints(rslt, l.Points, map);
                    }
                    else
                    {
                        return null;
                    }
                }

                // Closed?
                if (p.IsClosed)
                {
                    rslt.Append('Z');
                }

            }

            if (pc > MaxPointCount)
            {
                _exceedPointLimit = true;
            }

            return rslt.ToString();
        }

        private void WriteTransform(string attribute, Transform trans, Transform relative, Rect bounds)
        {
            Matrix mat = Utility.MergeTransform(trans, relative, bounds);

            if (!Utility.IsIdentity(mat))
            {
                _writer.WriteAttributeString(attribute, GetString(mat));
            }
        }

        private void WriteTransform(string attribute, Matrix trans)
        {
            if (!Utility.IsIdentity(trans))
            {
                _writer.WriteAttributeString(attribute, GetString(trans));
            }
        }

        private static Transform Append(Transform trans, Matrix mat)
        {
            Matrix m = Matrix.Identity;

            if (trans != null)
            {
                m = trans.Value;
            }

            m.Append(mat);

            return new MatrixTransform(m);
        }

        private void WriteBool(string attr, bool val)
        {
            string str;

            if (val)
            {
                str = "true";
            }
            else
            {
                str = "false";
            }

            _writer.WriteAttributeString(attr, str);
        }

        private void WriteFillRule(FillRule rule)
        {
            string str = null;

            if (rule == FillRule.Nonzero)
            {
                str = "NonZero";
            }
            else
            {
                // EvenOdd is the default, don't write anything
            }

            if (str != null)
            {
                _writer.WriteAttributeString("FillRule", str);
            }
        }

        internal bool WriteGeometry(string element, string attribute, Geometry geo, Matrix map, bool asElement, bool forFill, bool forStroke)
        {
            Debug.Assert(forFill || forStroke, "Either forFill or forStoke should be true");

            PathGeometry     pg = null;
            string           p = null;

            pg = Utility.GetAsPathGeometry(geo);

            if (IsPathGeometryEmpty(pg, forFill, forStroke))
            {
                return false;
            }

            if (attribute != null)
            {
                if (!asElement && (pg != null))
                {
                    p = PathGeometryToString(pg, map, forFill, forStroke);

                    // If it can be converted to a string, output as attribute
                    if (p != null)
                    {
                        _writer.WriteAttributeString(attribute, p);

                        return false;
                    }
                }

                // Output as element
                _writer.WriteStartElement(element + "." + attribute);
            }

            _writer.WriteStartElement("PathGeometry");

            Transform trans = Append(pg.Transform, map);

            WriteFillRule(pg.FillRule);

            p = PathGeometryToString(pg, trans.Value, forFill, forStroke);

            if (p != null)
            {
                // Remove "Fn " prefix
                _writer.WriteAttributeString("Figures", p.Substring(3));
            }
            else
            {
                PushCoordinateScope(trans);

                WriteTransform("Transform", trans, null, Rect.Empty);

                WritePathFigureCollection(pg.Figures, forFill, forStroke);

                PopCoordinateScope();
            }

            _writer.WriteEndElement();

            if (attribute != null)
            {
                _writer.WriteEndElement();
            }

            return true;
        }

        private static bool IsPathFigureEmpty(PathFigureCollection figures, bool forFill, bool forStroke)
        {
            if (figures == null)
            {
                return true;
            }

            foreach (PathFigure p in figures)
            {
                // When filling only, skip not filled PathFigure
                if (forFill && !forStroke && !p.IsFilled)
                {
                    continue;
                }

                PathSegmentCollection segments = p.Segments;

                if (segments != null)
                {
                    foreach (PathSegment ps in segments)
                    {
                        // When stroking only, skip not stroked PathSegment
                        if (forStroke && !forFill && !ps.IsStroked)
                        {
                            continue;
                        }

                        // Found something real
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Check geometry for emptyness before generating it. Empty geometry not allowed in Metro
        /// </summary>
        /// <param name="pg"></param>
        /// <param name="forFill"></param>
        /// <param name="forStroke"></param>
        /// <returns></returns>
        private static bool IsPathGeometryEmpty(PathGeometry pg, bool forFill, bool forStroke)
        {
            if ((pg == null) || pg.Bounds.IsEmpty)
            {
                return true;
            }

            if (!forStroke && !Utility.IsRenderVisible(pg.Bounds))
            {
                // When not stroking, geometry with zero-area bounds displays nothing, otherwise
                // the stroke may widen geometry to display something.
                return true;
            }

            return IsPathFigureEmpty(pg.Figures, forFill, forStroke);
        }

        /// <summary>
        /// Returns true if input is valid in an XML file, according to specification at
        /// http://www.w3.org/TR/2004/REC-xml-20040204/#charsets.
        /// </summary>
        /// <param name="c">input character</param>
        private static bool IsXmlValidChar(char c)
        {
            if (
                c == 0x09 || c == 0x0A || c == 0x0D ||
                (c >= 0x20 && c <= 0xD7FF) ||
                (c >= 0xE000 && c <= 0xFFFD)
                )
            {
                // valid
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Filters invalid XML characters from GlyphRun by replacing with spaces.
        /// </summary>
        /// <param name="glyphRun"></param>
        /// <returns>Returns the filtered GlyphRun if modified, otherwise the original GlyphRun</returns>
        private static GlyphRun FilterXmlInvalidChar(GlyphRun glyphRun)
        {
            if (glyphRun.Characters == null)
            {
                // no characters to worry about
                return glyphRun;
            }

            //
            // Fix bug 1334838: XPS Serialization: Incorrect XML characters not filtered out of S0 markup
            //
            // We temporarily fix by replacing invalid characters with spaces. The advance widths aren't
            // touched, and so visual appearance should remain the same except for missing glyphs
            // corresponding to replaced characters.
            //
            List<char> filteredCharacters = null;

            for (int i = 0; i < glyphRun.Characters.Count; i++)
            {
                if (!IsXmlValidChar(glyphRun.Characters[i]))
                {
                    if (filteredCharacters == null)
                    {
                        filteredCharacters = new List<char>(glyphRun.Characters);
                    }

                    // replace invalid character with space
                    filteredCharacters[i] = ' ';
                }
            }

            if (filteredCharacters == null)
            {
                // no filtering needed
                return glyphRun;
            }
            else
            {
                // return filtered GlyphRun
                GlyphRun filtered = new GlyphRun(
                    glyphRun.GlyphTypeface,
                    glyphRun.BidiLevel,
                    glyphRun.IsSideways,
                    glyphRun.FontRenderingEmSize,
                    glyphRun.PixelsPerDip,
                    glyphRun.GlyphIndices,
                    glyphRun.BaselineOrigin,
                    glyphRun.AdvanceWidths,
                    glyphRun.GlyphOffsets,
                    filteredCharacters,
                    glyphRun.DeviceFontName,
                    glyphRun.ClusterMap,
                    glyphRun.CaretStops,
                    glyphRun.Language
                    );

                return filtered;
            }
        }

        /// <summary>
        /// Gets URI as string, which may be relative if pointing within same XPS document.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private string GetUriAsString(Uri uri)
        {
            return uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped);
        }

        /// <summary>
        /// Determines if common attributes need to be preserved even if the element is transparent.
        /// </summary>
        /// <remarks>
        /// Attributes that need to be preserved even if the element is transparent:
        /// - Name: May be used as link target.
        /// - NavigateUri: Hyperlink clickable area.
        /// </remarks>
        private bool PreserveTransparent()
        {
            return _nameAttr != null || _navigateUri != null;
        }

        /// <summary>
        /// Writes attributes common to Canvas/Path/Glyphs.
        /// </summary>
        /// <param name="bWriteAutomation">Also write automation properties. Glyphs does not have automation properties.</param>
        private void WriteCommonAttrs(bool bWriteAutomation)
        {
            if (_nameAttr != null)
            {
                Debug.Assert(_nameAttr.Length > 0, "Empty _nameAttr");

                WriteAttr("Name", _nameAttr);
                _nameAttr = null;
            }

            if (_node != null)
            {
                if( bWriteAutomation )
                {
                    string apName = AutomationProperties.GetName(_node);

                    if (! String.IsNullOrEmpty(apName))
                    {
                        WriteAttr("AutomationProperties.Name", apName);
                    }

                    string apHelpText = AutomationProperties.GetHelpText(_node);

                    if (! String.IsNullOrEmpty(apHelpText))
                    {
                        WriteAttr("AutomationProperties.HelpText", apHelpText);
                    }
                }

                _node = null;
            }

            if (_navigateUri != null)
            {
                WriteAttr("FixedPage.NavigateUri", _navigateUri);
                _navigateUri = null;
            }
        }

        #endregion

        #region IMetroDrawingContext virtual methods override

        // Check for relative brush
        static public bool NeedBounds(Brush b)
        {
            if (b == null)
            {
                return false;
            }

            TileBrush tb = b as TileBrush;

            if (tb != null)
            {
                return (tb is VisualBrush) || (tb.ViewportUnits == BrushMappingMode.RelativeToBoundingBox);
            }

            GradientBrush gb = b as GradientBrush;

            if (gb != null)
            {
                return gb.MappingMode == BrushMappingMode.RelativeToBoundingBox;
            }

            return false;
        }

#if INVERSE_PATH_FOR_BRUSH_SHARING

        // Get inverse transfrom from bounding rectangle, adjust RenderTransform accordingly
        // so that brushes can be independent of bounding boxes
        Matrix GetBoundsInverse(Rect bounds)
        {
            Matrix mat = Matrix.Identity;

            mat.Scale(bounds.Width, bounds.Height);
            mat.Translate(bounds.Left, bounds.Top);

            if (_transform != null)
            {
                mat.Append(_transform.Value);
            }

            _transform = new MatrixTransform(mat);

            Matrix inverse = Matrix.Identity;

            inverse.Translate(-bounds.Left, -bounds.Top);
            inverse.Scale(1 / bounds.Width, 1 / bounds.Height);

            return inverse;
        }

#endif

#if SPLITTING_PATH

        /// <summary>
        /// Check if a geometry is different under filling and stroking, check for IsFilled=false and IsStroked=false
        /// </summary>
        /// <param name="geo"></param>
        /// <returns></returns>
        internal static bool IsGeometryPolymophic(Geometry geo)
        {
            PathGeometry pg = geo as PathGeometry;

            if (pg != null)
            {
                PathFigureCollection figures = pg.Figures;

                if (figures != null)
                {
                    foreach (PathFigure p in figures)
                    {
                        if (!p.IsFilled)
                        {
                            return true;
                        }

                        PathSegmentCollection segments = p.Segments;

                        if (segments != null)
                        {
                            foreach (PathSegment ps in segments)
                            {
                                if (!ps.IsStroked)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }

                return false;
            }

            CombinedGeometry cg = geo as CombinedGeometry;

            if (cg != null)
            {
                return IsGeometryPolymophic(cg.Geometry1)  ||
                       IsGeometryPolymophic(cg.Geometry2);
            }

            GeometryGroup gg = geo as GeometryGroup;

            if (gg != null)
            {
                if (gg.Children != null)
                {
                    foreach (Geometry g in gg.Children)
                    {
                        if (IsGeometryPolymophic(g))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

#endif

        /// <summary>
        /// Draw a Geometry with the provided Brush and/or Pen.
        /// If both the Brush and Pen are null this call is a no-op.
        /// </summary>
        void IMetroDrawingContext.DrawGeometry(Brush brush, Pen pen, Geometry geometry)
        {
            if (geometry == null)
            {
                return;
            }

            bool forFill = Visible(brush, null);
            bool forStroke = Visible(null, pen);

            if (PreserveTransparent())
            {
                //
                // Preserve Path element even if transparent brush/pen, since attributes
                // attached to this Path need preservation, i.e. hyperlink.
                //
                // If null fill, change to transparent fill to ensure hyperlink is clickable.
                //
                if (brush == null)
                {
                    forFill = true; // have WriteGeometry treat as fill even though it isn't visible
                    brush = Brushes.Transparent;
                }
            }
            else
            {
                if (!Visible(brush, pen))
                    return;
            }

#if SPLITTING_PATH

            if ((brush != null) && (pen != null) && IsGeometryPolymophic(geometry))
            {
                ((IMetroDrawingContext) this).DrawGeometry(brush, null, geometry);

                brush = null;
            }
#endif

            Rect bounds = geometry.GetRenderBounds(pen);

            if (!Utility.IsRenderVisible(bounds))
            {
                return;
            }

            _tcoStack.Push(_transform);

            Matrix inverse = Matrix.Identity;

#if INVERSE_PATH_FOR_BRUSH_SHARING
            // To generate more sharable relative brushes, it's possible to use a unit bounding box
            // and apply a reverse transformation to geometry.
            // This can generate strange viewbox and complicated path.
            // Disable for the memoent
            if ((pen == null) && (_clip == null) && (_opacityMask == null) &&
                !(geometry is CombinedGeometry) && NeedBounds(brush))
            {
                inverse = GetBoundsInverse(bounds);
                bounds  = UnitRect;
            }
#endif

            _writer.WriteStartElement("Path");

            WriteCommonAttrs(true);

            bool isLineGeometry = Utility.IsLineSegment(geometry);
            WritePen(pen, bounds, isLineGeometry);

            WriteBrush("Fill", brush, geometry.Bounds);

            bool asElement = WriteTCO("Path", _transform, _clip, Matrix.Identity, _opacity, _opacityMask, bounds);

            WriteGeometry("Path", "Data", geometry, inverse, asElement, forFill, forStroke);

            _writer.WriteEndElement();

            _transform = _tcoStack.Pop() as Transform;

            _totalElementCount ++;
            ReportLimitViolation();
        }

        /// <summary>
        /// Draw an Image into the region specified by the Rect, which may be animate.
        /// The Image will potentially be stretched and distorted to fit the Rect.
        /// For more fine grained control, consider filling a Rect with an ImageBrush via DrawRect.
        /// </summary>
        void IMetroDrawingContext.DrawImage(ImageSource image, Rect rectangle)
        {
            if (image != null)
            {
                Brush brush = new ImageBrush((BitmapSource)image);

                ((IMetroDrawingContext)this).DrawGeometry(brush, null, new RectangleGeometry(rectangle));
            }
            else if (PreserveTransparent())
            {
                // Transparent image, but element has attributes that need preservation (i.e. Name or NavigateUri).
                ((IMetroDrawingContext)this).DrawGeometry(null, null, new RectangleGeometry(rectangle));
            }
        }

        /// <summary>
        /// For translation only transform, extract out translation
        /// </summary>
        static private Transform ExtractTranslation(Transform trans, out double dx, out double dy)
        {
            dx = 0;
            dy = 0;

            if (!Utility.IsIdentity(trans))
            {
                Matrix mat = trans.Value;

                if (Utility.IsOne(mat.M11) &&
                    Utility.IsOne(mat.M22) &&
                    Utility.IsZero(mat.M12) &&
                    Utility.IsZero(mat.M21))
                {
                    dx = mat.OffsetX;
                    dy = mat.OffsetY;

                    return Transform.Identity;
                }
            }

            return trans;
        }
        /// <summary>
        /// Draw a GlyphRunAsImage.
        /// </summary>
        /// <remarks>
        /// This is used if GlyphRun cannot be embedded in XPS; therefore rasterization is needed.
        /// </remarks>
        internal
        void
        DrawGlyphRunAsImage(Brush foreground, GlyphRun glyphRun)
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXRasterStart);

            // rasterize GlyphRunDrawing to bitmap
            GlyphRunDrawing drawing = new GlyphRunDrawing(foreground, glyphRun);

            Matrix bitmapToDrawingTransform;

            BitmapSource bitmap = Utility.RasterizeDrawing(
                drawing,
                drawing.Bounds,
                _worldTransform,
                out bitmapToDrawingTransform
                );

            if (bitmap != null)
            {
                //
                // Draw the rasterized glyphs. bitmapToDrawingTransform should strictly
                // be a translation/scaling transform, which is why we can use Rect.Transform and
                // avoid a Push/Pop.
                //
                Debug.Assert(Utility.IsScaleTranslate(bitmapToDrawingTransform));

                Rect rect = new Rect(0, 0, bitmap.Width, bitmap.Height);
                rect.Transform(bitmapToDrawingTransform);

                ((IMetroDrawingContext)this).DrawImage(bitmap, rect);
            }

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXRasterEnd);
        }


        private static bool EmbeddingAllowed(GlyphTypeface typeface)
        {
            return (XpsFontSubsetter.DetermineEmbeddingAction(typeface.EmbeddingRights) != FontEmbeddingAction.ImageOnlyFont);
        }

        /// <summary>
        /// Draw a GlyphRun.
        /// </summary>
        void IMetroDrawingContext.DrawGlyphRun(Brush foreground, GlyphRun glyphRun)
        {
            if (glyphRun == null)
            {
                return;
            }

            if (PreserveTransparent())
            {
                if (foreground == null)
                {
                    // Give transparent foreground to ensure hyperlink is clickable.
                    foreground = Brushes.Transparent;
                }
            }
            else
            {
                if (!Visible(foreground, null))
                    return;
            }

            if (!EmbeddingAllowed(glyphRun.GlyphTypeface))
            {
                this.DrawGlyphRunAsImage(foreground, glyphRun);
                return;
            }

            _writer.WriteStartElement("Glyphs");

            double dx = 0, dy = 0;

            Transform trans = _transform;
            Matrix clipMat = Matrix.Identity;

            // Optimization: apply translation to OriginX/OriginY if possible
            if ((foreground is SolidColorBrush) && (_opacityMask == null))
            {
                trans = ExtractTranslation(_transform, out dx, out dy);

                if (_clip != null)
                {
                    clipMat = new Matrix(1, 0, 0, 1, dx, dy);
                }
            }

            WriteCommonAttrs(false);

            WriteAttr("OriginX", glyphRun.BaselineOrigin.X + dx);
            WriteAttr("OriginY", glyphRun.BaselineOrigin.Y + dy);
            WriteAttr("FontRenderingEmSize", glyphRun.FontRenderingEmSize);

            Uri uri = Utility.GetFontUri(glyphRun.GlyphTypeface);

            if (_manager != null)
            {
                TypeConverter converter = _manager.GetTypeConverter(typeof(GlyphRun));

                if (converter != null)
                {
                    uri = converter.ConvertTo(_context, null, glyphRun, typeof(Uri)) as Uri;
                }
            }

            WriteAttr("FontUri", uri);
            WriteAttr("StyleSimulations", glyphRun.GlyphTypeface.StyleSimulations, StyleSimulations.None);

            // BidiLevel must be in range [0, 61]
            int bidiLevel = glyphRun.BidiLevel;
            if (bidiLevel > 61)
            {
                bidiLevel = 61;
            }
            else if (bidiLevel < 0)
            {
                bidiLevel = 0;
            }

            WriteAttr("BidiLevel", bidiLevel, 0);

            if (glyphRun.IsSideways)
            {
                WriteBool("IsSideways", glyphRun.IsSideways);
            }

            //
            // Fix bug 1334838: XPS Serialization: Incorrect XML characters not filtered out of S0 markup
            //
            // Filter XML-invalid characters from GlyphRun, returning the filtered GlyphRun. The filtering
            // affects only UnicodeString.
            //
            GlyphRun serializeGlyphRun = FilterXmlInvalidChar(glyphRun);

            // serialize complex properties, running full markup size optimizations
            // per http://avalon/text/DesignDocsAndSpecs/Glyphs%20element%20and%20GlyphRun%20object.htm#optimizing
            GlyphsSerializer glyphsSerializer = new GlyphsSerializer(serializeGlyphRun);
            string characters, indices, caretStops;
            glyphsSerializer.ComputeContentStrings(out characters, out indices, out caretStops);

            bool exceedGlyphCount = false;

            if (!String.IsNullOrEmpty(characters))
            {
                // Leading '{' in a string is used for markup extension.
                // Prefix it with '{}' to avoid being confused with markup extension
                if (characters[0] == '{')
                {
                    characters = "{}" + characters;
                }

                if (serializeGlyphRun.Characters.Count > MaxGlyphCount)
                {
                    exceedGlyphCount = true;
                }

                WriteAttr("UnicodeString", characters);
            }

            if (!String.IsNullOrEmpty(indices))
            {
                if (serializeGlyphRun.GlyphIndices.Count > MaxGlyphCount)
                {
                    exceedGlyphCount = true;
                }

                WriteAttr("Indices", indices);
            }

            if (!String.IsNullOrEmpty(caretStops))
            {
                WriteAttr("CaretStops", caretStops);
            }

            Rect bounds = UnitRect;

            if (NeedBounds(_opacityMask) || NeedBounds(foreground))
            {
                bounds = glyphRun.ComputeInkBoundingBox();

                bounds.X += glyphRun.BaselineOrigin.X + dx;
                bounds.Y += glyphRun.BaselineOrigin.Y + dy;
            }

            WriteBrush("Fill", foreground, bounds);

            if (glyphRun.Language != null &&
                glyphRun.Language != _manager.Language)
            {
                // Only write language attribute if it doesn't match the fixedpage language.
                // WriteTCO might generate an element, so must write this attribute before.

                // As per w3 standards language attribute cannot contain empty string
                // InvariantCulture is associated with english language but not with any country/region and comes with empty string as name
                // Since language attribute with empty string is an invalid xml and InvariantCulture is associated with en-us
                // add language attribute as en-us if it is an InvariantCulture
                if (glyphRun.Language == XmlLanguage.Empty)
                {
                    WriteAttr(XpsS0Markup.XmlLang, XpsS0Markup.XmlEngLangValue);
                }
                else
                {
                    WriteAttr(XpsS0Markup.XmlLang, glyphRun.Language.ToString());
                }
            }

            WriteTCO("Glyphs", trans, _clip, clipMat, _opacity, _opacityMask, bounds);

            _writer.WriteEndElement();

            if (exceedGlyphCount)
            {
                _writer.WriteComment("XPSLimit:GlyphCount");
            }

            _totalElementCount ++;
            ReportLimitViolation();
        }

        /// <summary>
        /// Pop the most recent Push operation, which may have been a Clip, Opacity, Transform, etc.
        /// </summary>
        void IMetroDrawingContext.Pop()
        {
            _transform   = null;
            _clip        = null;
            _opacity     = 1.0;
            _opacityMask = null;

            int level = (int)_tcoStack.Pop();

            PopCoordinateScope();

            while (level > 0)
            {
                _writer.WriteEndElement();
                level --;
            }

            if (_tcoStack.Count == 0) // end of page
            {
                if (_brushId > MaxResourceCount)
                {
                    _writer.WriteComment("XPSLimit:ResourceCount");
                }

                if (_totalElementCount > MaxElementCount)
                {
                    _writer.WriteComment("XPSLimit:ElementCount");
                }
            }
        }

        void IMetroDrawingContext.Comment(String str)
        {
            _writer.WriteComment(str);
        }

        bool WriteTCO(string element, Transform transform, Geometry clip, Matrix clipMat, double opacity, Brush opacityMask, Rect bounds)
        {
            // Extract opacity from SolidColorBrush OpacityMask
            if (opacityMask != null)
            {
                SolidColorBrush sb = opacityMask as SolidColorBrush;

                if (sb != null)
                {
                    opacity *= Utility.NormalizeOpacity(sb.Color.ScA) * Utility.NormalizeOpacity(opacityMask.Opacity);
                    opacityMask = null;
                }
            }

            if (!Utility.IsOne(opacity))
            {
                WriteAttr("Opacity", Math.Min(Math.Max( opacity, 0.0),1.0));
            }

            if (opacityMask != null && !BrushProxy.IsEmpty(opacityMask))
            {
                WriteBrush("OpacityMask", opacityMask, bounds);
            }

            WriteTransform("RenderTransform", transform, null, Rect.Empty);

            bool asElement = false;

            if (clip != null)
            {
                asElement = WriteGeometry(element, "Clip", clip, clipMat, false, true, false);
            }

            return asElement;
        }

        void SetCoordinateFormat(double scale)
        {
            scale = scale * PrecisionDPI / 9600;

            if (scale > 1000)
            {
                _coordFormat = "G";        // 15 digits
            }
            else if (scale > 100)
            {
                _coordFormat = "#0.#####"; //  5 digits
            }
            else if (scale > 10)
            {
                _coordFormat = "#0.####";  //  4 digits
            }
            else if (scale > 1)
            {
                _coordFormat = "#0.###";   //  3 digits
            }
            else if (scale > 0.1)
            {
                _coordFormat = "#0.##";    //  2 digits
            }
            else
            {
                _coordFormat = "#0.#";     //  1 digit
            }
        }

        void PushCoordinateScope(Transform transform)
        {
            _tcoStack.Push(_worldTransform);
            _tcoStack.Push(_coordFormat);

            if (!Utility.IsIdentity(transform))
            {
                Matrix mat = transform.Value;

                mat.Append(_worldTransform);

                _worldTransform = mat;

                SetCoordinateFormat(Math.Min(Utility.GetScaleX(mat), Utility.GetScaleY(mat)));
            }
        }

        void PopCoordinateScope()
        {
            _coordFormat    = _tcoStack.Pop() as string;
            _worldTransform = (Matrix)_tcoStack.Pop();
        }

        void IMetroDrawingContext.Push(
            Matrix mat,
            Geometry clip,
            double opacity,
            Brush opacityMask,
            Rect maskBounds,
            bool onePrimitive,

            // serialization attributes
            String nameAttr,
            Visual node,
            Uri navigateUri,
            EdgeMode edgeMode
            )
        {
            Debug.Assert(nameAttr == null || nameAttr.Length > 0, "Bad name attribute");

            Transform transform = new MatrixTransform(mat);

            PushCoordinateScope(transform);

            bool noTrans = Utility.IsIdentity(transform);

            if (node != null)
            {
                _node = node;
            }

            int elementLevels = 0;

            if ((clip == null) &&
                noTrans &&
                Utility.IsOne(opacity) &&
                (opacityMask == null) &&
                nameAttr == null &&
                navigateUri == null &&
                edgeMode == EdgeMode.Unspecified)
            {
                // If there is no clip, transform and opacity, nothing to generate
            }
            else if (onePrimitive &&
                edgeMode == EdgeMode.Unspecified)   // EdgeMode only valid on Canvas
            {
                _transform   = transform;
                _clip        = clip;
                _opacity     = opacity;
                _opacityMask = opacityMask;

                if (nameAttr != null)
                {
                    Debug.Assert(_nameAttr == null, "Empty");
                    _nameAttr = nameAttr;
                }

                if (navigateUri != null)
                {
                    _navigateUri = navigateUri;
                }
            }
            else
            {
                _writer.WriteStartElement("Canvas");

                // write common attributes
                _nameAttr = nameAttr;
                _navigateUri = navigateUri;

                WriteCommonAttrs(true);

                // write RenderOptions.EdgeMode
                if (edgeMode != EdgeMode.Unspecified)
                {
                    WriteAttr("RenderOptions.EdgeMode", edgeMode);
                }

                // write transform, clip, opacity
                WriteTCO("Canvas", transform, clip, Matrix.Identity, opacity, opacityMask, maskBounds);
                elementLevels = 1;
            }

            // push number of element levels introduced by this IMetroDrawingContext.Push
            _tcoStack.Push(elementLevels);
        }

        #endregion Public Methods

    }
    #endregion
}

