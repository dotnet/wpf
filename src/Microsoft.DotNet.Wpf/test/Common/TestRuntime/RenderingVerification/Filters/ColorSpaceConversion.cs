// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification.Filters
{
    #region usings
        using System;
        using System.Drawing;
        using System.Globalization;
    #endregion usings

    /// <summary>
    /// Convert between different color space representation (RGB / HSI / HSL / CMY / YUV)
    /// </summary>
    internal class ColorSpaceConversion
    { 
        #region Constants
            private const double PI_DIV_3 = Math.PI / 3;    // 60 Deg
            private const double TWO_PI_DIV_3 = 2 * Math.PI / 3;    // 120 Deg
            private const double FOUR_PI_DIV_3 = 4 * Math.PI / 3;   // 240 Deg
            private const double TWO_PI = 2 * Math.PI ; // 360 Deg
        #endregion Constants

        #region Properties
            #region Private Properties
                /// <summary>
                /// Placeholder for the color
                /// </summary>
                private ColorDouble _argb;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Convert from/to ColorDouble
                /// </summary>
                /// <value></value>
                public ColorDouble ColorDouble
                {
                    get
                    {
                        return _argb;
                    }
                    set
                    {
                        _argb = value;
                    }
                }
                /// <summary>
                /// Convert from/to ARGB (0 to 255) 
                /// </summary>
                /// <value></value>
                public System.Drawing.Color Color
                {
                    get
                    {
                        return (System.Drawing.Color)_argb;
/*
                        if (_argb == ColorDouble.Empty)
                        {
                            return Color.Empty;
                        }
                        return Color.FromArgb((int)(_argb.Alpha * NormalizedColor.NormalizeValue), (int)(_argb.Red * NormalizedColor.NormalizeValue), (int)(_argb.Green * NormalizedColor.NormalizeValue), (int)(_argb.Blue * NormalizedColor.NormalizeValue));
*/
                    }
                    set
                    {
                        _argb = (ColorDouble)value;
/*
                        if (value == Color.Empty)
                        {
                            _argb = new NormalizedColor();
                        }
                        else
                        {
                            _argb.Alpha = (double)value.A / NormalizedColor.NormalizeValue;
                            _argb.Red = (double)value.R / NormalizedColor.NormalizeValue;
                            _argb.Green = (double)value.G / NormalizedColor.NormalizeValue;
                            _argb.Blue = (double)value.B / NormalizedColor.NormalizeValue;
                        }
*/
                    }
                }
                /// <summary>
                /// Convert from/to Hue-Saturation-Intensity
                /// </summary>
                /// <value></value>
                public HSIColor HSI
                {
                    get
                    {
                        // RGB to HSI

                        HSIColor hsi = new HSIColor();
                        double minRGB = Math.Min(Math.Min(_argb.Red, _argb.Green), _argb.Blue);
                        if ((_argb.Red == _argb.Green) && (_argb.Green == _argb.Blue))
                        { 
                            // Greyscale
                            hsi.H = 0.0;
                            hsi.I = Luminance; 
                            hsi.S = 0.0;
                            return hsi;

                        }
                        double degree = (2 * _argb.Red - (_argb.Green + _argb.Blue)) / (2 * Math.Sqrt(Math.Pow(_argb.Red - _argb.Green, 2) + (_argb.Red - _argb.Blue) * (_argb.Green - _argb.Blue)));
//                        double degree = (0.5 * (_argb.Red - _argb.Green) + (_argb.Red -_argb.Blue)) / ( Math.Sqrt(Math.Pow(_argb.Red - _argb.Green, 2) + (_argb.Red - _argb.Blue) * (_argb.Green - _argb.Blue)));
                        double thetaRadian = Math.Acos(degree); //* Math.PI / 180);

                        hsi.H = (_argb.Blue > _argb.Green) ? (TWO_PI - thetaRadian) : thetaRadian;
                        hsi.S = 1 - 3 * minRGB / (_argb.Red + _argb.Green + _argb.Blue);
                        hsi.I = (_argb.Red + _argb.Green + _argb.Blue) / 3;
                        // Now normalize the angle
                        hsi.H = thetaRadian / TWO_PI;
                        return hsi;
                    }
                    set
                    {
                        // HSI to RGB

                        // Check args
                        if (value.S < 0.0 || value.S > 1.0)
                        {
                            throw new ArgumentOutOfRangeException();
                        }

                        if (value.I < 0.0 || value.I > 1.0)
                        {
                            throw new ArgumentOutOfRangeException();
                        }

                        if (value.H < 0 || value.H > 1.0)
                        {
                            throw new ArgumentOutOfRangeException();
                        }
                        if (value.S == 0.0)
                        { 
                            // Saturation null -> Grayscale
                            _argb.Red = value.I;
                            _argb.Green = value.I;
                            _argb.Blue = value.I;
                            return;
                        }

                        value.H *= TWO_PI;  // Hue is normalized, convert into Radian
                        double s = (1 - value.S) / 3;
                        double h = 0;

                        if (value.H >= FOUR_PI_DIV_3)   // > 240
                        {
                            value.H = value.H - FOUR_PI_DIV_3;
                            h = (1 + value.S * Math.Cos(value.H) / Math.Cos(PI_DIV_3 - value.H)) / 3;
                            _argb.Green = s;
                            _argb.Blue = h;
                            _argb.Red =  1 - (_argb.Green + _argb.Blue);
                            return;
                        }

                        if (value.H >= TWO_PI_DIV_3)    // > 120
                        {
                            value.H = value.H - TWO_PI_DIV_3;
                            h = (1 + value.S * Math.Cos(value.H) / Math.Cos(PI_DIV_3 - value.H)) / 3;
                            _argb.Red = s;
                            _argb.Green = h;
                            _argb.Blue = 1 - (_argb.Red + _argb.Green);
                            return;
                        }

                        h = (1 + value.S * Math.Cos(value.H) / Math.Cos(PI_DIV_3 - value.H)) / 3;
                        _argb.Blue = s;
                        _argb.Red = h;
                        _argb.Green = 1 - (_argb.Red + _argb.Blue);
                    }
                }
                /// <summary>
                /// Convert from/to Hue-Saturation-Luminance
                /// </summary>
                /// <value></value>
                public HSLColor HSL
                {
                    get 
                    {
                        // Normalize color
                        if (_argb.MinChannelValue != 0.0 || _argb.MaxChannelValue != 1.0)
                        { 
                            // TODO : Normalize to 0-1
                            throw new NotImplementedException("More code needed, contact the tool team");
                        }

                        HSLColor hsl = new HSLColor();

                        double max = Math.Max(Math.Max(_argb.Red, _argb.Green), _argb.Blue);
                        double min = Math.Min(Math.Min(_argb.Red, _argb.Green), _argb.Blue);
                        if (max - min == 0.0)
                        {
                            // test for gray image
                            hsl.H = 0;
                            hsl.S = 0;
                            hsl.L = _argb.Green;
                            return hsl;
                        }

                        double offset = max - min;

                        hsl.L = (max - min) / 2;
                        if (hsl.L < 0.5) { hsl.S = offset / (max + min); }
                        else { hsl.S = offset / (2.0 - max - min); }

                        if (max == _argb.Red)
                        {
                            // max is red
                            hsl.H = (_argb.Green - _argb.Blue) / offset;
                        }
                        else 
                        {
                            if (max == _argb.Green)
                            {
                                // max is green
                                hsl.H = 2.0 + (_argb.Blue - _argb.Red) / offset;
                            }
                            else 
                            {
                                // max is Blue
                                hsl.H = 4.0 + (_argb.Red - _argb.Green) / offset;
                            }
                        }

                        return hsl;
                    }
                    set 
                    {
                        if (value.S == 0)
                        {
                            // Gray value;
                            _argb.Red = _argb.Green = _argb.Blue = HSL.L;
                            return;
                        }
                        double temp2 = double.NaN;
                        if (value.L < 0.5) { temp2 = value.L * (1.0 + value.S);}
                        else { temp2 = value.L + value.S - value.L * value.S; }
                        double temp1 = 2.0 * value.L - temp2;

                        double temp3 = double.NaN;
                        // Red
                        temp3 = value.H + 1.0 / 3.0;
                        _argb.Red = SaturateChannel(ComputeChannelForHSL(temp1, temp2, temp3));
                        // Green
                        temp3 = value.H;
                        _argb.Green = SaturateChannel(ComputeChannelForHSL(temp1, temp2, temp3));
                        // Blue
                        temp3 = value.H - 1.0 / 3.0;
                        _argb.Blue = SaturateChannel(ComputeChannelForHSL(temp1, temp2, temp3));
                    }
                }
                /// <summary>
                /// Convert from/to Cyan-Magenta-Yellow
                /// </summary>
                /// <value></value>
                public CMYColor CMY
                {
                    get
                    {
                        CMYColor cmy = new CMYColor();

                        cmy.C = 1.0 - _argb.Red;
                        cmy.M = 1.0 - _argb.Green;
                        cmy.Y = 1.0 - _argb.Blue;
                        return cmy;
                    }
                    set
                    {
                        _argb.Red = 1.0 - value.C;
                        _argb.Green = 1.0 - value.M;
                        _argb.Blue = 1.0 - value.Y;
                    }
                }
                /// <summary>
                /// Convert from/to Luminance-ChrominanceRed-ChromianceBlue (used by many image compression algorithms)
                /// </summary>
                /// <value></value>
                public YUVColor YUV
                {
                    get
                    {
                        // From Intel's web site
                        YUVColor yuv = new YUVColor();
                        yuv.Y = this.Luminance;
                        yuv.U = -0.146 * _argb.Red - 0.288 * _argb.Green + 0.434 * _argb.Blue;
                        yuv.V = 0.617 * _argb.Red - 0.517 * _argb.Green - 0.100 * _argb.Blue;
                        return yuv;
                    }
                    set
                    {
                        // warning : Unverified code.
                        // Conversion grabbed from the web
                        System.Diagnostics.Debug.WriteLine("(ColorModelConversion::set_YUV) WARNING : Unverified code (grabbed from the web), if you are using this contact the avalon tool team to have this code double-checked !");
                        _argb.Red = 1 * value.Y - 0.0009267 * (value.U - 128) + 1.4016868 * (value.V - 128);
                        _argb.Green = 1 * value.Y - 0.3436954 * (value.U - 128) - 0.7141690 * (value.V - 128);
                        _argb.Blue = 1 * value.Y + 1.7721604 * (value.U - 128) + 0.0009902 * (value.V - 128);
                    }
                }
                /// <summary>
                /// Get the luminance value for this color
                /// </summary>
                /// <value>(double) returns the luminance of the specified color</value>
                public double Luminance
                {
                    get 
                    {
                        if (_argb.IsEmpty)
                        { 
                            throw new ArgumentException("The IColor is currently not valid (set to 'Empty')");
                        }
                        return _argb.Red * .299 + _argb.Green * .587 + _argb.Blue * .114;
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Contructors
            /// <summary>
            /// Instanciate a ColorSpaceConversion Class
            /// </summary>
            public ColorSpaceConversion()
            { 
                _argb = new ColorDouble();
            }
            /// <summary>
            /// Instanciate a ColorSpaceConversion Class
            /// </summary>
            /// <param name="colorPrecision">The NormalizedColor to be converted</param>
            public ColorSpaceConversion(ColorDouble colorPrecision)
            {
                _argb = (ColorDouble)colorPrecision.Clone();
            }
            /// <summary>
            /// Instanciate a ColorSpaceConversion Class
            /// </summary>
            /// <param name="color">The System.Drawing.Color to be converted</param>
            public ColorSpaceConversion(System.Drawing.Color color)
            {
                _argb = (ColorDouble)color;
/*
                if (color == Color.Empty)
                {
                    _argb = new ColorDouble()();
                }
                else
                {
                    this.Color = color;
                }
*/
            }
            /// <summary>
            /// Instanciate a ColorSpaceConversion Class
            /// </summary>
            /// <param name="hsi">The HSI color to be converted</param>
            public ColorSpaceConversion(HSIColor hsi)
            { 
                this.HSI = hsi;
            }
            /// <summary>
            /// Instanciate a ColorSpaceConversion Class
            /// </summary>
            /// <param name="cmy">The CMY color to be converted</param>
            public ColorSpaceConversion(CMYColor cmy)
            { 
                this.CMY = cmy;
            }
            /// <summary>
            /// Instanciate a ColorSpaceConversion Class
            /// </summary>
            /// <param name="yuv">The YUV color to be converted</param>
            public ColorSpaceConversion(YUVColor yuv)
            { 
                this.YUV = yuv;
            }
        #endregion Contructors

        #region Methods
            private double ComputeChannelForHSL(double temp1, double temp2, double temp3)
            {
                double retVal = double.NaN;

                if (6.0 * temp3 < 1.0)
                {
                    retVal = temp1 + (temp2 - temp1) * 6.0 * temp3;
                }
                else
                {
                    if (2.0 * temp3 < 1.0)
                    {
                        retVal = temp2;
                    }
                    else
                    {
                        retVal = temp1 + (temp2 - temp1) * ((2.0 / 3.0) - temp3) * 6.0;
                    }
                }

                return retVal;
            }
            private double SaturateChannel(double channelValue)
            {
                // BUGBUG : ColorByte and ColorDouble Normalized value can be !=
                if (channelValue > ColorDouble._maxChannelValue)
                {
                    return ColorDouble._maxChannelValue;
                }
                if (channelValue < ColorDouble._minChannelValue)
                {
                    return ColorDouble._minChannelValue;
                }

                return channelValue;
            }
        #endregion Methods
    }


    /// <summary>
    /// Hue-Saturation-Intensity color model
    /// (To be used with the ColorConversion class)
    /// </summary>
    internal struct HSIColor
    { 
        /// <summary>
        /// The Hue value (angle in radian)
        /// </summary>
        public double H;
        /// <summary>
        /// The Saturation value (0 to 1)
        /// </summary>
        public double S;
        /// <summary>
        /// The Intensity value (0 to 1)
        /// </summary>
        public double I;
    }
    /// <summary>
    /// Hue-Saturation-Luminence color model
    /// (To be used with the ColorConversion class)
    /// </summary>
    internal struct HSLColor
    {
        /// <summary>
        /// The Hue value (angle in radian)
        /// </summary>
        public double H;
        /// <summary>
        /// The Saturation value (0 to 1)
        /// </summary>
        public double S;
        /// <summary>
        /// The Luminance value (0 to 1)
        /// </summary>
        public double L;
    }
    /// <summary>
    /// Cyan-Magenta-Yellow color model
    /// (To be used with the ColorConversion class)
    /// </summary>
    internal struct CMYColor
    {
        /// <summary>
        /// The Cyan value
        /// </summary>
        public double C;
        /// <summary>
        /// The Magenta value
        /// </summary>
        public double M;
        /// <summary>
        /// The Yellow value
        /// </summary>
        public double Y;
    }
    /// <summary>
    /// Luminance-ChrominaceRed-ChrominanceBlue color model
    /// (To be used with the ColorConversion class)
    /// </summary>
    internal struct YUVColor
    {
        /// <summary>
        /// The Luminance value
        /// </summary>
        public double Y;
        /// <summary>
        /// The Red Chrominance value
        /// </summary>
        public double U;
        /// <summary>
        /// The Blue Chrominance value
        /// </summary>
        public double V;
    }

}
