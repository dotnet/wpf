// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.Drawing;
        using System.Runtime.Serialization;
        //using System.Runtime.Serialization.Formatters.Soap;
   #endregion using

    /// <summary>
    /// Summary description for GlyphFont.
    /// </summary>
    [SerializableAttribute]
    public sealed class GlyphFont : ISerializable
    {
        #region Properties
            #region Private Properties
                private float _size = float.NaN;
                private string _name = string.Empty;
                private FontStyle _style = FontStyle.Regular;
                private IColor _fgColor = null;
                private IColor _bgColor = null;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// The size of the font to use
                /// </summary>
                /// <value></value>
                public float Size
                {
                    get { return _size; }
                    set 
                    {
                        if (value <= 0) { throw new ArgumentOutOfRangeException("Size", value, "value must be strictly positive"); }
                        _size = value;
                    }
                }
                /// <summary>
                /// The name of the font to use
                /// </summary>
                /// <value></value>
                public string Name
                {
                    get { return _name; }
                    set 
                    {
                        if (value == null) { throw new ArgumentNullException("Name", "must be set to a valid instance of an object (null passed in)"); }
                        _name = value;
                    }
                }
                /// <summary>
                /// The style of the font to use
                /// </summary>
                /// <value></value>
                public FontStyle Style
                {
                    get { return _style;}
                    set { _style = value; } // Font check value by using enum.parse since value can be other (FlagAttribute defined on this enum)
                }
                /// <summary>
                /// The Background color of the font to use
                /// </summary>
                /// <value></value>
                public IColor BackgroundColor
                {
                    get { return _bgColor; }
                    set 
                    {
                        if (value == null) { throw new ArgumentNullException("BackgroundColor", "Must be set to a valid instance of an object implementing IColor (null was passed in)"); }
                        _bgColor = value;
                    }
                }
                /// <summary>
                /// The Foreground color of the font to use
                /// </summary>
                /// <value></value>
                public IColor ForegroundColor
                {
                    get { return _fgColor; }
                    set 
                    {
                        if (value == null) { throw new ArgumentNullException("ForegroundColor", "Must be set to a valid instance of an object implementing IColor (null was passed in)"); }
                        _fgColor = value;
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// The default constructor - XML serialixation only
            /// </summary>
            public GlyphFont() 
            {
                _size = 12f;
                _name = "Arial";
                _style = FontStyle.Regular;
                _fgColor = (ColorByte)Color.Black;
                _bgColor = (ColorByte)Color.Empty;
            }
            /// <summary>
            /// Deserialization Constructor
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            private GlyphFont(SerializationInfo info, StreamingContext context)
            {
                _size = (float)info.GetValue("Size", typeof(float));
                _name = (string)info.GetValue("Name", typeof(string));
                _style = (System.Drawing.FontStyle)info.GetValue("Style", typeof(System.Drawing.FontStyle));
                _fgColor = (IColor)info.GetValue("ForegroundColor", typeof(IColor));
                _bgColor = (IColor)info.GetValue("BackgroundColor", typeof(IColor));
            }
        #endregion Constructors

        #region ISerializable Members
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("Size", Size);
                info.AddValue("Name", Name);
                info.AddValue("Style", Style);
                info.AddValue("ForegroundColor",ForegroundColor);
                info.AddValue("BackgroundColor", BackgroundColor);
            }
        #endregion
    }
}
