// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    #region usings
        using System;
        using System.Drawing;
    #endregion usings

    /// <summary>
    /// ColorDouble : precise and support scRGB but slow and consumes lots of memory
    /// </summary>
    [SerializableAttribute ()]
#if CLR_VERSION_BELOW_2
    public struct ColorDouble: IColor 
#else
    public partial struct ColorDouble: IColor
#endif
    {
        #region Properties
            /// <summary>
            /// Defines an empty color for this type
            /// </summary>
            public static readonly ColorDouble Empty = new ColorDouble();
            internal static double _maxChannelValue = 1.0;
            internal static double _minChannelValue = 0.0;
            internal static double _normalizedValue = 255.0;
            private bool _isDefined; 
            private bool _isScRgb;
            private double _alpha;
            private double _red;
            private double _green;
            private double _blue;
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Create an instance of ColorDouble using alpha, red, green and blue as byte
            /// </summary>
            /// <param name="A">The Alpha channel</param>
            /// <param name="R">The Red channel</param>
            /// <param name="G">The Green channel</param>
            /// <param name="B">The Blue channel</param>
            public ColorDouble(byte A, byte R, byte G, byte B) : this(A / ColorDouble._normalizedValue, R / ColorDouble._normalizedValue, G / ColorDouble._normalizedValue, B / ColorDouble._normalizedValue, false)
            {
            }
            /// <summary>
            /// Create an instance of ColorDouble using alpha, red, green and blue as normalized value
            /// </summary>
            /// <param name="alpha">The normalized Alpha value</param>
            /// <param name="red">The normalized Red value</param>
            /// <param name="green">The normalized Green value</param>
            /// <param name="blue">The normalized Blue value</param>
            public ColorDouble(double alpha, double red, double green, double blue) : this(alpha, red, green, blue, false) 
            {
            }
            /// <summary>
            /// Create an instance of ColorDouble using alpha, red, green and blue as normalized/extended value
            /// Note : Extended value do not have to be within the bound set by MinChannleValue and MaxChannleValue.
            /// </summary>
            /// <param name="alpha">The normalized Alpha value</param>
            /// <param name="red">The normalized Red value</param>
            /// <param name="green">The normalized Green value</param>
            /// <param name="blue">The normalized Blue value</param>
            /// <param name="isScRgb">If true the values passed in should be treated as extended values (no bounds), if false, value must be between 0 and 1 </param>
            public ColorDouble(double alpha, double red, double green, double blue, bool isScRgb)
            {
                if (isScRgb)
                {
                    _alpha = alpha;
                    _red = red;
                    _green = green;
                    _blue = blue;
                    _isScRgb = true;
                }
                else 
                {
                    _alpha = CheckNormalizedChannelBound(alpha);
                    _red = CheckNormalizedChannelBound(red);
                    _green = CheckNormalizedChannelBound(green);
                    _blue = CheckNormalizedChannelBound(blue);
                    _isScRgb = false;
                }
                _isDefined = true;
            }
            /// <summary>
            ///  Create an instance of ColorDouble based on a System.Drawing.Color
            /// </summary>
            /// <param name="gdiColor">The color to be using as input value</param>
            public ColorDouble(System.Drawing.Color gdiColor)
            {
                if (gdiColor != System.Drawing.Color.Empty)
                {
                    _alpha = gdiColor.A / ColorDouble._normalizedValue;
                    _red = gdiColor.R / ColorDouble._normalizedValue;
                    _green = gdiColor.G / ColorDouble._normalizedValue;
                    _blue = gdiColor.B / ColorDouble._normalizedValue;
                    _isDefined = true;
                }
                else 
                {
                    _alpha = 0.0;
                    _red = 0.0;
                    _green = 0.0;
                    _blue = 0.0;
                    _isDefined = false;
                }
                _isScRgb = false;
            }
            /// <summary>
            ///  Create an instance of ColorDouble using any color implementing IColor
            /// </summary>
            /// <param name="color">An instance of a type implementing IColor</param>
            public ColorDouble(IColor color)
            {
                if (color == null)
                {
                    throw new ArgumentNullException("color", "The value passed in must be a valid instance of an object implementing IColor (null was passed in)");
                }
                this._alpha = color.ExtendedAlpha;
                this._red = color.ExtendedRed;
                this._green = color.ExtendedGreen;
                this._blue = color.ExtendedBlue;
                this._isDefined = ! color.IsEmpty;
                this._isScRgb = color.IsScRgb;
            }
        #endregion Constructors

        #region IColor interface implementation
            /// <summary>
            /// Get/set the all the channels at once
            /// </summary>
            /// <value></value>
            public int ARGB
            {
                get { return ((int)A << 24) + ((int)R << 16) + ((int)G << 8) + (int)B; }
                set
                {
                    A = (byte)(value >> 24);
                    R = (byte)((value & 0x00FF0000) >> 16);
                    G = (byte)((value & 0x0000FF00) >> 8);
                    B = (byte)(value & 0x000000FF);
                }
            }
            /// <summary>
            /// Get/set the Red, Green and Blue channels at once
            /// </summary>
            /// <value></value>
            public int RGB 
            {
                get { return (int)R >> 16 + (int)G>> 8 + (int)B; }
                set 
                {
                    // Do not shild against value > 0x00ffffff () so we can do something like a.RGB = b.ARGB, just ignore Alpha channel
                    R = (byte)((value & 0x00FF0000) >> 16);
                    G = (byte)((value & 0x0000FF00) >> 8);
                    B = (byte)(value & 0x000000FF);
                }
            }
            /// <summary>
            /// Get/set the Extended value for the Alpha channel
            /// </summary>
            /// <value></value>
            public double ExtendedAlpha 
            {
                get { return _alpha; }
                set { this._isDefined = true; _alpha = value; }
            }
            /// <summary>
            /// Get/set the normalized value for the Alpha channel
            /// </summary>
            /// <value></value>
            public double Alpha
            {
                get { return SaturateNormalizedChannel(_alpha); }
                set { ExtendedAlpha = CheckNormalizedChannelBound(value); }
            }
            /// <summary>
            /// Get/set the Alpha channel
            /// </summary>
            /// <value></value>
            public byte A
            { 
                get { return SaturateChannel(_alpha); }
                set { ExtendedAlpha = value / ColorDouble._normalizedValue; }
            }
            /// <summary>
            /// Get/set the Extended value for the Red channel
            /// </summary>
            /// <value></value>
            public double ExtendedRed
            {
                get { return _red; }
                set { this._isDefined = true; _red = value; }
            }
            /// <summary>
            /// Get/set the normalized value for the Red channel
            /// </summary>
            /// <value></value>
            public double Red
            {
                get { return SaturateNormalizedChannel(_red); }
                set { ExtendedRed = CheckNormalizedChannelBound(value); }
            }
            /// <summary>
            /// Get/set the Red channel
            /// </summary>
            /// <value></value>
            public byte R
            {
                get { return SaturateChannel(_red); }
                set { ExtendedRed = value / ColorDouble._normalizedValue; }
            }
            /// <summary>
            /// Get/set the Extended value for the Green channel
            /// </summary>
            /// <value></value>
            public double ExtendedGreen
            {
                get { return _green; }
                set { this._isDefined = true; _green = value; }
            }
            /// <summary>
            /// Get/set the normalized value for the Green channel
            /// </summary>
            /// <value></value>
            public double Green
            {
                get { return SaturateNormalizedChannel(_green); }
                set { ExtendedGreen = CheckNormalizedChannelBound(value); }
            }
            /// <summary>
            /// Get/set the Green channel
            /// </summary>
            /// <value></value>
            public byte G
            {
                get { return SaturateChannel(_green); }
                set { ExtendedGreen = value / ColorDouble._normalizedValue; }
            }
            /// <summary>
            /// Get/set the Extended value for the Blue channel
            /// </summary>
            /// <value></value>
            public double ExtendedBlue
            {
                get { return _blue; }
                set { this._isDefined = true; _blue = value; }
            }
            /// <summary>
            /// Get/set the normalized value for the Blue channel
            /// </summary>
            /// <value></value>
            public double Blue
            {
                get { return SaturateNormalizedChannel(_blue); }
                set { ExtendedBlue = CheckNormalizedChannelBound(value); }
            }
            /// <summary>
            /// Get/set the Blue channel
            /// </summary>
            /// <value></value>
            public byte B
            {
                get { return SaturateChannel(_blue); }
                set { ExtendedBlue = value / ColorDouble._normalizedValue; }
            }
            /// <summary>
            /// Convert this Color to the standard "System.Drawing.Color" type
            /// </summary>
            /// <returns></returns>
            public System.Drawing.Color ToColor()
            {
                return (_isDefined) ? System.Drawing.Color.FromArgb(A, R, G, B) : System.Drawing.Color.Empty;
            }
            /// <summary>
            /// Get/set the color as Empty 
            /// </summary>
            /// <value></value>
            public bool IsEmpty
            {
                get{return ! _isDefined;}
                set
                {
                    if (value == true) { _alpha = 0.0; _red = 0.0; _green = 0.0; _blue = 0.0; }
                    _isDefined = ! value;
                }
            }
            /// <summary>
            /// Retrieve if this type can effectively deal with scRGB color (no information loss when filtering)
            /// </summary>
            /// <value></value>
            public bool SupportExtendedColor
            {
                get { return true; }
            }
            /// <summary>
            /// Get/set the color to scRgb (Gamma 1.0) or not (Gamma 2.2)
            /// </summary>
            /// <value></value>
            public bool IsScRgb 
            { 
                get { return _isScRgb; }
                set { _isScRgb = value; }
            }
            /// <summary>
            /// Get/set the Max value for all channels when normalizing
            /// </summary>
            /// <value></value>
            public double MaxChannelValue
            {
                get { return ColorDouble._maxChannelValue; }
                set { ColorDouble._maxChannelValue = value; }
            }
            /// <summary>
            /// Get/set the Min value for all channels when normalizing
            /// </summary>
            /// <value></value>
            public double MinChannelValue
            {
                get { return ColorDouble._minChannelValue; }
                set { ColorDouble._minChannelValue = value; }
            }
            /// <summary>
            /// Get/set the Normalization value
            /// </summary>
            /// <value></value>
            public double NormalizedValue
            {
                get { return ColorDouble._normalizedValue; }
                set { ColorDouble._normalizedValue = value; }
            }
        #endregion IColor interface implementation

        #region IClonable interface implementation
            /// <summary>
            /// Clone the current object
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                return new ColorDouble(this);
            }
        #endregion IClonable interface implementation

        #region Methods
            #region Private Methods
                private static double CheckNormalizedChannelBound(double normalizedValue)
                {
                    if (normalizedValue < ColorDouble._minChannelValue || normalizedValue > ColorDouble._maxChannelValue)
                    {
                        throw new ArgumentOutOfRangeException("normalizedValue", normalizedValue, "Value must be between " + ColorDouble._minChannelValue + " and " + ColorDouble._maxChannelValue);
                    }
                    return normalizedValue;
                }
                private static byte SaturateChannel(double normalizedValue)
                {
                    int channelValue = (int)(normalizedValue * ColorDouble._normalizedValue + 0.5);
                    if (channelValue > 255) { channelValue = 255; }
                    if (channelValue < 0) { channelValue = 0; }
                    return (byte)channelValue;
                }
                internal static double SaturateNormalizedChannel(double normalizedChannel)
                {
                    if (normalizedChannel > ColorDouble._maxChannelValue) { normalizedChannel = ColorDouble._maxChannelValue; }
                    if (normalizedChannel < ColorDouble._minChannelValue) { normalizedChannel = ColorDouble._minChannelValue; }
                    return normalizedChannel;
                }
            #endregion Private Methods
            #region Operators overload
                /// <summary>
                /// Cast a GDI color (System.Drawing.Color) into this type
                /// </summary>
                /// <param name="gdiColor">The GDI color to cast</param>
                /// <returns>The color translated in this type</returns>
                public static explicit operator ColorDouble(System.Drawing.Color gdiColor)
                { 
                    ColorDouble colorPrecision = new ColorDouble();
                    if(gdiColor != Color.Empty)
                    {
                        colorPrecision.A = gdiColor.A;
                        colorPrecision.R = gdiColor.R;
                        colorPrecision.G = gdiColor.G;
                        colorPrecision.B = gdiColor.B;
                    }
                    return colorPrecision;
                }
                /// <summary>
                /// Cast this type into a GDI color (System.Drawing.Color)
                /// </summary>
                /// <param name="colorPrecision">This ColorDouble type to cast</param>
                /// <returns>The color translated in the standard GDI type</returns>
                public static explicit operator System.Drawing.Color(ColorDouble colorPrecision)
                {
                    System.Drawing.Color gdiColor = System.Drawing.Color.Empty;
                    if (colorPrecision.IsEmpty == false)
                    {
                        gdiColor = System.Drawing.Color.FromArgb(colorPrecision.A, colorPrecision.R, colorPrecision.G, colorPrecision.B);
                    }
                    return gdiColor;
                }
                /// <summary>
                /// Cast a ColorByte type into a ColorDouble type.
                /// </summary>
                /// <param name="colorFast">The ColorByte to be converted</param>
                /// <returns>Returns a ColorDouble type representing the ColorByte value</returns>
                public static implicit operator ColorDouble(ColorByte colorFast)
                {
                    ColorDouble retVal = new ColorDouble();
                    if (colorFast.IsEmpty) { return retVal; }
                    retVal.ExtendedAlpha = colorFast.ExtendedAlpha;
                    retVal.ExtendedRed = colorFast.ExtendedRed;
                    retVal.ExtendedGreen = colorFast.ExtendedGreen;
                    retVal.ExtendedBlue = colorFast.ExtendedBlue;
                    retVal._isDefined = ! colorFast.IsEmpty;
                    return retVal;
                }
                /// <summary>
                /// Merge two color using the Alpha channel value as multiplier
                /// </summary>
                /// <param name="colorPrecision1">The first color to merge</param>
                /// <param name="colorPrecision2">The second color to merge</param>
                /// <returns>The merge color (averaging based on the alpha channel)</returns>
                // @ review : assume not premultiply ?
                // @ review : Use Extended or regular value ?
                // @ review : if both color are opaque should be just add them (instead of averaging them) ? 
                //    ...Might help some customer but would be inconstient (not done for now)
                public static ColorDouble operator +(ColorDouble colorPrecision1, ColorDouble colorPrecision2)
                {
                    ColorDouble retVal = new ColorDouble();
                    if (colorPrecision1.IsEmpty == false && colorPrecision2.IsEmpty == false)
                    {
                        double dividend = ColorDouble._maxChannelValue * 2 - ColorDouble._minChannelValue;
                        retVal.ExtendedAlpha = Math.Max(colorPrecision1.ExtendedAlpha, colorPrecision2.ExtendedAlpha);
                        retVal.ExtendedRed = (colorPrecision1.ExtendedRed * colorPrecision1.ExtendedAlpha + colorPrecision2.ExtendedRed * colorPrecision2.ExtendedAlpha) / dividend;
                        retVal.ExtendedGreen = (colorPrecision1.ExtendedGreen * colorPrecision1.ExtendedAlpha + colorPrecision2.ExtendedGreen * colorPrecision2.ExtendedAlpha) / dividend;
                        retVal.ExtendedBlue = (colorPrecision1.ExtendedBlue * colorPrecision1.ExtendedAlpha + colorPrecision2.ExtendedBlue * colorPrecision2.ExtendedAlpha) / dividend;
                        retVal._isDefined = true;
                    }
                    else 
                    {
                        if (colorPrecision1.IsEmpty) { retVal = (ColorDouble)colorPrecision2.Clone(); }
                        else { retVal = (ColorDouble)colorPrecision1.Clone(); }
                    }
                    return retVal;
                }
                /// <summary>
                /// Un-Merge two color using the Alpha channel value as multiplier
                /// </summary>
                /// <param name="colorPrecision1">The resulting color</param>
                /// <param name="colorPrecision2">The color to remove from the resulting image</param>
                /// <returns>The merging color (color that would lead to color one perform "thisColor + color2")</returns>
                // @ review : assume not premultiply ?
                // @ review : Use Extended or regular value ?
                // @ review : if both color are opaque should be just subtract them (instead of un-averaging them) ? 
                //    ...Might help some customer but would be inconstient (not done for now)
                public static ColorDouble operator -(ColorDouble colorPrecision1, ColorDouble colorPrecision2)
                {
                    ColorDouble retVal = new ColorDouble();
                    if (colorPrecision1.IsEmpty == false || colorPrecision2.IsEmpty == false)
                    {
                        double dividend = ColorDouble._maxChannelValue;
                        retVal.ExtendedAlpha = Math.Max(colorPrecision1.ExtendedAlpha, colorPrecision2.ExtendedAlpha);
                        retVal.ExtendedRed = (colorPrecision1.ExtendedRed * colorPrecision1.ExtendedAlpha * 2 - colorPrecision2.ExtendedRed * colorPrecision2.ExtendedAlpha) / dividend;
                        retVal.ExtendedGreen = (colorPrecision1.ExtendedGreen * colorPrecision1.ExtendedAlpha * 2 - colorPrecision2.ExtendedGreen * colorPrecision2.ExtendedAlpha) / dividend;
                        retVal.ExtendedBlue = (colorPrecision1.ExtendedBlue * colorPrecision1.ExtendedAlpha * 2 - colorPrecision2.ExtendedBlue * colorPrecision2.ExtendedAlpha) / dividend;
                        retVal._isDefined = true;
                   }
                    return retVal;
                }
                /// <summary>
                /// Negate a color without affecting the alpha channel.
                /// </summary>
                /// <param name="colorPrecision">The color to negate</param>
                /// <returns>The negated color</returns>
                // @ review : IGNORE alpha ? (we are right now)
                // @review  : Should we Use ExtendedAlpha here ? Does not make much sense for Negating a color
                public static ColorDouble operator !(ColorDouble colorPrecision)
                {
                    ColorDouble retVal = new ColorDouble();
                    if (colorPrecision.IsEmpty)
                    {
                        return retVal;
                    }
                    retVal.ExtendedAlpha = colorPrecision.ExtendedAlpha;
                    retVal.ExtendedRed = ColorDouble._maxChannelValue - colorPrecision.ExtendedRed;
                    retVal.ExtendedGreen = ColorDouble._maxChannelValue - colorPrecision.ExtendedGreen;
                    retVal.ExtendedBlue = ColorDouble._maxChannelValue - colorPrecision.ExtendedBlue;
                    retVal._isDefined = true;
                    return retVal;
                }
                /// <summary>
                /// Compare two ColorDouble type for equality
                /// </summary>
                /// <param name="colorPrecision1">The first color to compare</param>
                /// <param name="colorPrecision2">The second color to compare</param>
                /// <returns>return true if the color are the same, false otherwise</returns>
                public static bool operator ==(ColorDouble colorPrecision1, ColorDouble colorPrecision2)
                {
                    if(colorPrecision1._alpha == colorPrecision2._alpha && 
                        colorPrecision1._red == colorPrecision2._red &&
                        colorPrecision1._green == colorPrecision2._green && 
                        colorPrecision1._blue == colorPrecision2._blue &&
                        colorPrecision1._isDefined == colorPrecision2._isDefined)
                    {
                        return true;
                    }
                    return false;
                }
                /// <summary>
                /// Compare two ColorPrecison type for inequality
                /// </summary>
                /// <param name="colorPrecision1">The first color to compare</param>
                /// <param name="colorPrecision2">The second color to compare</param>
                /// <returns>return true if the color are the different, false if they are the same</returns>
                public static bool operator !=(ColorDouble colorPrecision1, ColorDouble colorPrecision2)
                {
                    return ! (colorPrecision1 == colorPrecision2);
                }
            #endregion Operators overload
            #region Overriden Methods
                /// <summary>
                /// Compare two ColorDouble for equality
                /// </summary>
                /// <param name="obj">The color to compare against</param>
                /// <returns>returns true if the color are the same, false otherwise</returns>
                public override bool Equals(object obj)
                {
                    if (obj is ColorDouble)
                    {
                        return this == (ColorDouble)obj;
                    }
                    throw new InvalidCastException("The type passed ('" + obj.GetType().ToString() + "') in cannot be casted to a ColorDouble object");
                }
                /// <summary>
                /// Get the hashcode for this color
                /// </summary>
                /// <returns></returns>
                public override int GetHashCode()
                {
                    if (_isDefined == false)
                    {
                        return System.Drawing.Color.Empty.GetHashCode();
                    }
                    return (_alpha + _red * ColorDouble._normalizedValue + _green * ColorDouble._normalizedValue * ColorDouble._normalizedValue + _blue * ColorDouble._normalizedValue*ColorDouble._normalizedValue*ColorDouble._normalizedValue).GetHashCode();
                }
                /// <summary>
                /// Display the value of this color in a friendly manner
                /// </summary>
                /// <returns></returns>
                public override string ToString()
                {
                    if (_isDefined == false) { return "ColorDouble [Empty]"; }

                    return "ColorDouble [ Alpha:" + _alpha.ToString("G17") + " / Red:" + _red.ToString("G17") + " / Green:" + _green.ToString("G17") + " / Blue:" + _blue.ToString("G17") + " ] ~ or rougly about ~ [ A:" + A.ToString() + " / R:" + R.ToString() + " / G:" + G.ToString() + " / B:" + B.ToString() + " ]";
                }
            #endregion Overriden Methods
        #endregion Methods
    }
}
