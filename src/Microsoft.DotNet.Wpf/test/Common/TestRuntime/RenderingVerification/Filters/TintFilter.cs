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
    /// An Avalon Tint Filter
    /// </summary>
    public class TintFilter: Filter
    {
    
        #region Constants
            private const string AMOUNT = "Amount";
            private const string HUE = "Hue";
            private const double MIL_LUMINANCE_RED = 0.212671;
            private const double MIL_LUMINANCE_GREEN = 0.715160;
            private const double MIL_LUMINANCE_BLUE = 0.072169;
        #endregion Constants

        #region Properties
            #region Private Properties
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
                /// Get the description for this filter
                /// </summary>
                /// <value></value>
                public override string FilterDescription
                {
                    get
                    {
                        return "Tint an image by adding/removing an amount of color to/from the image -- MIL filter";
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Tint Filter constructor
            /// </summary>
            public TintFilter()
            {
                FilterParameter amount = new FilterParameter(AMOUNT, "Tint Amount", (double)0.0);
                FilterParameter hue = new FilterParameter(HUE, "Tint Hue", (double)0.0);

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
                    double amount = Amount;
                    tintColor.ExtendedRed *= Amount;
                    tintColor.ExtendedGreen *= Amount;
                    tintColor.ExtendedBlue *= Amount;
                    if (Amount < 0.0)
                    {
                        amount *= -1;
                        tintColor.ExtendedRed = 1.0 + tintColor.ExtendedRed;
                        tintColor.ExtendedGreen = 1.0 + tintColor.ExtendedGreen;
                        tintColor.ExtendedBlue = 1.0 + tintColor.ExtendedBlue;
                    }

                    // Tint all pixels
                    IColor newColor = null;
                    IColor tempColor = null;
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
                            tempColor = new ColorDouble((IColor)source[x, y]);
                            if (tempColor.IsEmpty)
                            {
                                retVal[x, y] = tempColor;
                                continue;
                            }
                            luminance = tempColor.ExtendedRed * MIL_LUMINANCE_RED + tempColor.ExtendedGreen* MIL_LUMINANCE_GREEN + tempColor.ExtendedBlue* MIL_LUMINANCE_BLUE;
                            maxChannel = Math.Max(Math.Max(source[x, y].ExtendedRed, source[x, y].ExtendedGreen), source[x, y].ExtendedBlue);

                            temp = source[x, y].ExtendedRed * (1.0 - amount) + tintColor.ExtendedRed * maxChannel;
                            tempColor.ExtendedRed = temp;
                            temp = source[x, y].ExtendedGreen * (1.0 - amount) + tintColor.ExtendedGreen * maxChannel;
                            tempColor.ExtendedGreen = temp;
                            temp = source[x, y].ExtendedBlue * (1.0 - amount) + tintColor.ExtendedBlue * maxChannel;
                            tempColor.ExtendedBlue = temp;

                            // restore the original luminance
                            double deltaLum = luminance - (tempColor.ExtendedRed * MIL_LUMINANCE_RED + tempColor.ExtendedGreen * MIL_LUMINANCE_GREEN + tempColor.ExtendedBlue * MIL_LUMINANCE_BLUE);

                            newColor = (IColor)source[x, y].Clone();
                            newColor.ExtendedRed = tempColor.ExtendedRed + deltaLum;
                            newColor.ExtendedGreen = tempColor.ExtendedGreen + deltaLum;
                            newColor.ExtendedBlue = tempColor.ExtendedBlue + deltaLum;
                            retVal[x, y] = newColor;
                        }
                    }
                    return retVal;
                }

            #endregion Public Methods
        #endregion Methods
    }
}
