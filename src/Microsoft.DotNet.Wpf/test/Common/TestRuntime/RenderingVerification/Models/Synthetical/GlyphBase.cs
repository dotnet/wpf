// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.Drawing;
        using System.Threading; 
        using System.Collections;
        using System.Windows.Forms;
        using System.Drawing.Design;
        using System.ComponentModel;
        using System.Drawing.Drawing2D;
        using System.Runtime.Serialization;
        //using System.Runtime.Serialization.Formatters.Soap;
         using Microsoft.Test.RenderingVerification.Filters;
   #endregion using

    /// <summary>
    /// Summary description for GlyphBase.
    /// </summary>
    [SerializableAttribute]
    public abstract class GlyphBase: ISerializable
    {
        #region Properties
            #region Private Properties
                private GlyphContainer _owner = null;
                private GlyphPanel _panel = null;
                private GlyphLayout _computedLayout = null;
                private GlyphCompareInfo _compareInfo = null;
                private IImageAdapter _generatedImage = null;
                private IColor _fgColor = ColorByte.Empty;
                private IColor _bgColor = ColorByte.Empty;
                private RectangleF _boundingBox = RectangleF.Empty;
                private Size _size = Size.Empty;
                private PointF _position = PointF.Empty;
                private object _userData = null;
            #endregion Private Properties
            #region Internal Properties
                internal GlyphContainer Owner
                {
                    get { return _owner; }
                    set 
                    { 
                        if (_owner != this) { _owner = value; } 
                    }
                }
            #endregion Internal Properties
            #region Public Properties
                /// <summary>
                /// Return the hosting panel associated with this glyph
                /// </summary>
                /// <value></value>
                public GlyphPanel Panel
                {
                    get
                    {
                        return _panel;
                    }
                }
                /// <summary>
                /// The layout info -- contains the actual position after the LayoutEngine computed it
                /// Note : The position computed is relative to its parent/owner
                /// </summary>
                public GlyphLayout ComputedLayout
                {
                    get { return _computedLayout; }
                }
                /// <summary>
                /// The resulting image
                /// </summary>
                public IImageAdapter GeneratedImage
                {
                    get { return _generatedImage; }
                    set 
                    {
                        if (value == null) { throw new ArgumentNullException("Image", "Must be set to a valid instance of an object implementing IImageAdapter"); }
                        _generatedImage = value;
                    }
                }
                /// <summary>
                /// Get/set the Size of the Glyph
                /// </summary>
                /// <value></value>
                public Size Size
                {
                    get
                    {
                        if (_size.IsEmpty) { Measure(); }
                        return _size;
                    }
                    set
                    {
                        if (value.Width <= 0) { throw new ArgumentOutOfRangeException("Size.Width", "Value to be set must be strictly positive"); }
                        if (value.Height <= 0) { throw new ArgumentOutOfRangeException("Size.Height", "Value to be set must be strictly positive"); }
                        _size = value;
                    }
                }
                /// <summary>
                /// Glyph offset within the panel
                /// </summary>
                /// <value></value>
                public PointF Position
                {
                    get 
                    {
                        return _position;
                    }
                    set 
                    {
                        _position = value;
                    }
                }
                /// <summary>
                /// The Foreground color
                /// </summary>
                [Editor(typeof(GlyphColorChooser), typeof(UITypeEditor))]
                public virtual IColor ForegroundColor
                {
                    get { return _fgColor; }
                    set
                    {
                        if (value == null) { throw new ArgumentNullException("ForegroundColor", "Must be set to a valid instance of an object implementing IColor (null passed in)"); }
                        _fgColor = value;
                    }
                }
                /// <summary>
                /// The Backgorund color 
                /// </summary>
                [Editor(typeof(GlyphColorChooser), typeof(UITypeEditor))]
                public virtual IColor BackgroundColor
                {
                    get { return _bgColor; }
                    set
                    {
                        if (value == null) { throw new ArgumentNullException("BackgroundColor", "Must be set to a valid instance of an object implementing IColor (null passed in)"); }
                        _bgColor = value;
                    }
                }
                /// <summary>
                /// Access placeholder for comparison stuff (tolerance / results / ...)
                /// </summary>
                /// <value></value>
                public GlyphCompareInfo CompareInfo
                {
                    get 
                    {
                        if (_compareInfo == null) { _compareInfo = new GlyphCompareInfo(); }
                        return _compareInfo;
                    }
                }
                /// <summary>
                /// The Bounding box
                /// </summary>
                /// 
                public RectangleF BoundingBox
                {
                    get { return _boundingBox; }
                    set { _boundingBox = value; }
                }
                /// <summary>
                /// Provide a placeholder for user data
                /// </summary>
                /// <value></value>                
                public object Tag
                {
                    get { return _userData; }
                    set { _userData = value; }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// The default constructor - XML serialixation only
            /// </summary>
            protected GlyphBase() 
            {
                _position = new PointF(0, 0);
                _fgColor = new ColorByte(255, 0, 0, 0);
                _bgColor = new ColorByte(); // same as ColorByte.Empty

                _panel = new GlyphPanel(this);
                _computedLayout = new GlyphLayout(this);
                _compareInfo = new GlyphCompareInfo();
            }
            /// <summary>
            /// The constructor
            /// </summary>
            protected GlyphBase(GlyphContainer container) : this()
            {
                _owner = container;
                container.Glyphs.Add(this);
            }
            /// <summary>
            /// Deserialization Constructor
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected GlyphBase(SerializationInfo info, StreamingContext context)
            {
                _panel = (GlyphPanel)info.GetValue("Panel", typeof(GlyphPanel));
                _size = (Size)info.GetValue("Size", typeof(System.Drawing.Size));
                _position = (PointF) info.GetValue("Position", typeof(PointF));
                ForegroundColor = (IColor) info.GetValue("ForegroundColor", typeof(IColor));
                BackgroundColor = (IColor)info.GetValue("BackgroundColor", typeof(IColor));
                _owner = (GlyphContainer)info.GetValue("Owner", typeof(GlyphContainer));
                Tag = (object)info.GetValue("Tag", typeof(object));
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
            #endregion Private Methods
            #region Internal Abstract Methods
                /// <summary>
                /// internal rendering
                /// </summary>
                internal abstract IImageAdapter _Render();
            #endregion Internal Abstract Methods
            #region Public Methods
                /// <summary>
                /// Measures the geometry
                /// </summary>
                public virtual SizeF Measure()
                {
                    return Panel.Size;
                }
                /// <summary>
                /// Renders the glyph in an IImageAdapter
                /// </summary>
                public virtual IImageAdapter Render()
                {
                    IImageAdapter retVal = _Render ();

                    // Apply potential transform and draw self
                    if (Panel.Transform.Matrix.IsIdentity == false) 
                    { 
                        retVal = Panel.Transform.Process(retVal);
                        Size = new Size(retVal.Width, retVal.Height);
                    }

                    // Perform Layout
                    Image2DTransforms layoutTransform = new Image2DTransforms(retVal);
                    layoutTransform.ResizeToFitOutputImage = true;
                    Matrix2D matrix = layoutTransform.Matrix;
                    switch (Panel.PanelLayoutMode)
                    {
                        case PanelLayoutMode.None:
                            break;
                        case PanelLayoutMode.Center:
                            float x = (Panel.Size.Width - Size.Width) / 2;
                            float y = (Panel.Size.Height - Size.Height) / 2;
                            Position = new PointF(Position.X + x, Position.Y + y);
                            break;
                        case PanelLayoutMode.Stretch:
                            layoutTransform.ScaleTransform (Panel.Size.Width / Size.Width, Panel.Size.Height / Size.Height);
                            break;
                        case PanelLayoutMode.Zoom:
                            double ratio = Math.Min (Panel.Size.Width / Size.Width, Panel.Size.Height / Size.Height);
                            layoutTransform.ScaleTransform (ratio, ratio);
                            break;
                        case PanelLayoutMode.Tile:
                            throw new NotImplementedException ("Contact the Avalon tool team if you need this feature");
//                            break;
                        default:
                            throw new ArgumentException ("Unexpected value for GlyphImageLayoutOut (Should never occur since value was checked in setter -- except if private value changed thru reflection)");
                    }
                    layoutTransform.ApplyTransforms ();
                    IImageAdapter transformed = layoutTransform.ImageTransformed;

                    Point offset = new Point((int)(Position.X + .5), (int)(Position.Y + .5));
                    retVal = new ImageAdapter((int)Panel.Size.Width, (int)Panel.Size.Height, Panel.Color);
                    retVal = ImageUtility.CopyImageAdapter(retVal, transformed, offset, new Size(transformed.Width, transformed.Height), true);
                    // Apply opacity if needed
                    if (_panel.Opacity != 1.0)
                    {
                        for (int y = 0; y < retVal.Height; y++)
                        {
                            for (int x = 0; x < retVal.Width; x++)
                            {
                                retVal[x, y].ExtendedAlpha *= _panel.Opacity;
                            }
                        }
                    }

                    // update Image
                    GeneratedImage = retVal;

                    return retVal;
                }
            #endregion Public Methods
        #endregion Methods

        #region ISerializable Members
            /// <summary>
            /// Serialization Method
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("Panel", Panel);
                info.AddValue("Size",Size);
                info.AddValue("Position",Position);
                info.AddValue("ForegroundColor",ForegroundColor);
                info.AddValue("BackgroundColor",BackgroundColor);
                info.AddValue("Tag", (Tag != null && Tag is ISerializable) ? Tag : null);
                info.AddValue("Owner", Owner);
            }
        #endregion
    }
}
