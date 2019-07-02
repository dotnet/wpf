// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region usings
        using System;
        using System.Drawing;
        using System.Runtime.Serialization;
        //using System.Runtime.Serialization.Formatters.Soap;
        using Microsoft.Test.RenderingVerification.Filters;
    #endregion usings


    /// <summary>
    /// Specify how to display an image behind the GlyphBase area
    /// </summary>
    public enum PanelLayoutMode
    {
        /// <summary>
        /// Image is copied to TopLeft location
        /// </summary>
        None = 0,
        /// <summary>
        /// image is repeatedly used to for a tile pattern
        /// </summary>
        Tile = 1,
        /// <summary>
        /// Image is centered
        /// </summary>
        Center = 2,
        /// <summary>
        /// Image is zoomed to fit into the GlyphBase Width and Height (aspect ratio preserved)
        /// </summary>
        Zoom = 3,
        /// <summary>
        /// The image is stretch to fit in the GlyphBase Width and Height
        /// </summary>
        Stretch = 4
    }

    /// <summary>
    /// Summary description for GlyphPanel.
    /// </summary>
    [SerializableAttribute]
    public class GlyphPanel : ISerializable
    {
        #region Properties
            #region Private Properties
                private GlyphBase _associatedGlyph = null;
                private SizeF _size = SizeF.Empty;
                private PointF _position = PointF.Empty;
                private IColor _color = ColorByte.Empty;
                private double _opacity = double.NaN;
                private GeometryFilter _geoFilter = null;
                private PanelLayoutMode _layout = PanelLayoutMode.None;
                private double _padding = 0.0;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// The padding around the panel
                /// </summary>
                /// <value></value>
                public double Padding
                {
                    get { return _padding; }
                    set 
                    {
                        throw new NotImplementedException("Not implemented yet, contact the avalon tool team to have this turned on");
//                        if (value < 0.0) { throw new ArgumentOutOfRangeException("Padding","Parameter must be positive"); }
//                        _padding = value; 
                    }
                }
                /// <summary>
                /// The size of the panel
                /// </summary>
                /// <value></value>
                public SizeF Size
                {
                    get 
                    {
                        if (_size.IsEmpty)
                        {
                            _size = new SizeF (_associatedGlyph.Size.Width, _associatedGlyph.Size.Height);
                        }
                        return _size; 
                    }
                    set { _size = value; }
                }
                /// <summary>
                /// The absolute position of the panel
                /// </summary>
                /// <value></value>
                public PointF Position
                {
                    get { return _position; }
                    set { _position = value; }
                }
                /// <summary>
                /// The background color of the Panel
                /// </summary>
                /// <value></value>
                public IColor Color
                {
                    get { return _color; }
                    set 
                    {
                        if (value == null) { throw new ArgumentNullException("Color", "value must be a valid instancew of an object implementing IColor (null passed in)"); }
                        _color = value;
                    }
                }
                /// <summary>
                /// Get/set the opacity for this Glyph
                /// </summary>
                /// <value></value>
                public double Opacity
                {
                    get { return _opacity; }
                    set 
                    {
                        if (value > 1.0 || value < 0.0) { throw new ArgumentOutOfRangeException("Opacity", value, "This value is normalized (must be between 0.0 and 1.0; with the upperbound representing 100% opacity - or 255"); }
                        _opacity = value; 
                    }
                }
                /// <summary>
                /// The layout mode used to draw the GlypBase into the panel
                /// </summary>
                /// <value></value>
                public PanelLayoutMode PanelLayoutMode
                {
                    get { return _layout; }
                    set 
                    {
                        if( Enum.Parse(typeof(PanelLayoutMode), value.ToString()) == null )
                        {
                            throw new ArgumentException("Unknown enum value passed in", "PanelLayoutMode");
                        }
                        _layout = value;
                    }
                }
                /// <summary>
                /// The geometry filter
                /// </summary>
                public GeometryFilter Transform
                {
                    get
                    {
                        if (_geoFilter == null) { _geoFilter = new GeometryFilter(); }
                        return _geoFilter;
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            private GlyphPanel() { }
            /// <summary>
            /// Instantiate a new GlyphPanel object
            /// </summary>
            /// <param name="glyph"></param>
            public GlyphPanel(GlyphBase glyph)
            {
                _associatedGlyph = glyph;
                _padding = 0.0;
                _position = new PointF(0f, 0f);
                _size = SizeF.Empty;
                _layout= PanelLayoutMode.None;
                _opacity = 1.0;
            }
            /// <summary>
            /// Deserialization Constructor
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected GlyphPanel(SerializationInfo info, StreamingContext context)
            {
                _padding = (double)info.GetValue("Padding", typeof(double));
                _size = (SizeF)info.GetValue("Size", typeof(SizeF));
                _position = (PointF)info.GetValue("Position", typeof(PointF));
                _color = (IColor)info.GetValue("Color", typeof(IColor));
                _layout = (PanelLayoutMode)info.GetValue("PanelLayoutMode", typeof(PanelLayoutMode));
                _geoFilter = (GeometryFilter)info.GetValue("Transform", typeof(Filter));
//                _associatedGlyph = 
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
            #endregion Private Methods
            #region Public Methods
            #endregion Public Methods
        #endregion Methods

        #region ISerializable Members
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("Padding", Padding);
                info.AddValue("Size", Size);
                info.AddValue("Position",Position);
                info.AddValue("Color",Color);
                info.AddValue("Transform", Transform);
                info.AddValue("PanelLayoutMode", PanelLayoutMode);
            }
        #endregion
    }
}
