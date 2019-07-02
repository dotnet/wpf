// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.Drawing;
        using System.Collections;
        using System.Drawing.Imaging;
        using Microsoft.Test.RenderingVerification;
    #endregion using

    /// <summary>
    /// A Tint Shade Filter
    /// </summary>
    public class TintShadeFilter: Filter
    {
    
        #region Constants
            private const string AMOUNT = "Amount";
            private const string HUE = "Hue";
            private const string REDLUMINANCE = "RedLuminance";
            private const string GREENLUMINANCE = "GreenLuminance";
            private const string BLUELUMINANCE = "BlueLuminance";
        #endregion Constants

        #region Properties
            #region Private Properties
                private double _redLuminance = 0.212671;
                private double _greenLuminance = 0.715160;
                private double _blueLuminance = 0.072169;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Amount of the Tint (-1 to 1)
                /// </summary>
                public double Amount
                {
                    get
                    {
                        return (double)this[AMOUNT].Parameter;
                    }
                    set
                    {
                        if (value < -1.0 || value > 1.0)
                        { 
                            throw new ArgumentOutOfRangeException("Amount value must be between -1 and 1");
                        }
                        this[AMOUNT].Parameter = value;
                    }
                }
                /// <summary>
                /// Hue of the Tint (-180 to 180)
                /// </summary>
                public double Hue
                {
                    get
                    {
                        return (double)this[HUE].Parameter;
                    }
                    set
                    {
                        if (value < -180.0 || value > 180.0)
                        {
                            throw new ArgumentOutOfRangeException("Hue value must be between -180.0 and 180.0 degree");
                        }
                        this[HUE].Parameter = value;
                    }
                }
                /// <summary>
                /// Get/set the Red participation in the luminance (R+G+B = 1.0)
                /// </summary>
                /// <value></value>
                public double RedLuminance
                {
                    get { return _redLuminance;}
                    set 
                    {
                        if (_redLuminance < 0.0 || _redLuminance > 1.0)
                        {
                            throw new ArgumentOutOfRangeException("RedLuminance", "Lumiance participation must be set between 0.0 and 1.0");
                        }
                        _redLuminance = value;
                    }
                }
                /// <summary>
                /// Get/set the Green participation in the luminance (R+G+B = 1.0)
                /// </summary>
                /// <value></value>
                public double GreenLuminance
                {
                    get { return _greenLuminance;}
                    set 
                    {
                        if (_greenLuminance < 0.0 || _greenLuminance > 1.0)
                        {
                            throw new ArgumentOutOfRangeException("GreenLuminance", "Lumiance participation must be set between 0.0 and 1.0");
                        }
                        _greenLuminance = value;
                    }
                }
                /// <summary>
                /// Get/set the Blue participation in the luminance (R+G+B = 1.0)
                /// </summary>
                /// <value></value>
                public double BlueLuminance
                {
                    get { return _blueLuminance;}
                    set 
                    {
                        if (_blueLuminance < 0.0 || _blueLuminance > 1.0)
                        {
                            throw new ArgumentOutOfRangeException("BlueLuminance", "Lumiance participation must be set between 0.0 and 1.0");
                        }
                        _blueLuminance = value;
                    }
                }
                /// <summary>
                /// Get the description for this filter
                /// </summary>
                /// <value></value>
                public override string FilterDescription
                {
                    get
                    {
                        return "Tint an image by adding/removing an amount of color to/from the image";
                    }
                }

            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Tint Filter constructor
            /// </summary>
            public TintShadeFilter()
            {
                FilterParameter amount = new FilterParameter(AMOUNT, "Tint Amount", (double)0.0);
                FilterParameter hue = new FilterParameter(HUE, "Tint Hue", (double)0.0);
                FilterParameter redLuminance = new FilterParameter(REDLUMINANCE, "Red paricipation in Luminance", (double)_redLuminance);
                FilterParameter greenLuminance = new FilterParameter(GREENLUMINANCE, "Green paricipation in Luminance", (double)_greenLuminance);
                FilterParameter blueLuminance = new FilterParameter(BLUELUMINANCE, "Blue paricipation in Luminance", (double)_blueLuminance);

                AddParameter(amount);
                AddParameter(hue);
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Filter Implementation
                /// </summary>
                protected override IImageAdapter ProcessFilter(IImageAdapter source)
                {
                    if (_redLuminance + _greenLuminance + _blueLuminance != 1.0)
                    {
                        throw new RenderingVerificationException("The sum of RedLuminance, GreenLuminance and BlueLuminance must be 1.0 (currently sum = " + (_redLuminance + _greenLuminance + _blueLuminance).ToString() + ")");
                    }

                    ImageAdapter retVal = new ImageAdapter(source.Width, source.Height);

                    // Convert HSL to RGB
                    HSLColor hsl = new HSLColor();
                    hsl.H = ((Hue + 360.0) % 360) / 360.0;  // Normalize -180/180 to 0/1
                    hsl.S = 1.0;
                    hsl.L = 0.5;
                    ColorSpaceConversion colorConvert = new ColorSpaceConversion();
                    colorConvert.HSL = hsl;
                    IColor tintColor = colorConvert.ColorDouble;
                    // multiply every color channel by amount
                    tintColor.ExtendedRed *= Amount;
                    tintColor.ExtendedGreen *= Amount;
                    tintColor.ExtendedBlue *= Amount;
                    double amount = (Amount < 0) ? 1.0  : (1.0 - Amount) ;

                    // Tint all pixels
                    IColor newColor = null;
                    double luminance = 0.0;
                    int height = source.Height;
                    int width = source.Width;
                    double temp = 0.0;
                    double maxChannel = 0.0;
                    int x = 0;
                    int y = 0;
                    for(y = 0; y < height; y++)
                    {
                        for(x = 0; x < width; x++)
                        {
                            newColor = (IColor)source[x, y].Clone();
                            if (newColor.IsEmpty)
                            {
                                retVal[x, y] = newColor;
                                continue;
                            }
                            luminance = newColor.ExtendedRed * _redLuminance + newColor.ExtendedGreen* _greenLuminance + newColor.ExtendedBlue* _blueLuminance;
                            maxChannel = Math.Max(Math.Max(source[x, y].ExtendedRed, source[x, y].ExtendedGreen), source[x, y].ExtendedBlue);

                            temp = source[x, y].ExtendedRed * amount + tintColor.ExtendedRed * maxChannel;
                            newColor.ExtendedRed = temp;
                            
                            temp = source[x, y].ExtendedGreen * amount+ tintColor.ExtendedGreen * maxChannel;
                            newColor.ExtendedGreen = temp;

                            temp = source[x, y].ExtendedBlue * amount + tintColor.ExtendedBlue * maxChannel;
                            newColor.ExtendedBlue = temp;

                            // restore the original luminance
                            double deltaLum = luminance - (newColor.ExtendedRed * _redLuminance + newColor.ExtendedGreen * _greenLuminance + newColor.ExtendedBlue * _blueLuminance);
                            newColor.ExtendedRed = newColor.ExtendedRed + deltaLum;
                            newColor.ExtendedGreen = newColor.ExtendedGreen + deltaLum;
                            newColor.ExtendedBlue = newColor.ExtendedBlue + deltaLum;
                            retVal[x, y] = newColor;
                        }
                    }
                    return retVal;
                }
            #endregion Public Methods
        #endregion Methods
    }
}
