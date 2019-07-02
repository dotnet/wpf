// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.Collections;
        using Microsoft.Test.RenderingVerification;
    #endregion using

    /// <summary>
    /// BrightnessContrastFilter will increase of decrease the brightness or contrast of an image.
    /// Note : this support scRGB (128 bits colors)
    /// </summary>
    public class BrightnessContrastFilter : Filter
    {
        #region Constants
            private const string BRIGHTNESS = "Brightness";
            private const string CONTRAST = "Contrast";
            private const string AUTOADJUST = "AutoAdjust";
            // Constants for auto brightness/contrast function
            // See BrightnessContrast.cpp
            private const float LOW_1 = 0.008f;
            private const float LOW_2 = 0.013f;
            private const float HIGH_2 = 0.990f;
            private const float HIGH_1 = 0.995f;
            private const int DELTA_20 = 20;
            private const int DELTA_5 = 5;
//            private const float LOW_3 = 0.2f;
//            private const float HIGH_3 = 0.8f;
//            private const int DELTA_1_2 = 20;
//            private const int DELTA_2_3 = 5;


            // Luminance indices from EffectUtil.h
            private const double LUMINANCE_RED = 0.212671;
            private const double LUMINANCE_GREEN = 0.715160;
            private const double LUMINANCE_BLUE = 0.072169;

        #endregion Constants

        #region Properties
            #region Private Properties
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Get/set the brightness
                /// Note : value must be between -1.0 and 1.0
                /// </summary>
                public double Brightness
                {
                    get
                    {
                        return (double)this[BRIGHTNESS].Parameter;
                    }
                    set
                    {
                        if (value < -1.0 || value > 1.0)
                        {
                            throw new ArgumentOutOfRangeException("Brightness must be between -1.0 and 1.0"); 
                        }
                        this[BRIGHTNESS].Parameter = value;
                    }
                }
                /// <summary>
                /// Get/set the contrast
                /// Note : Value must be betwwen -1f and 1f
                /// </summary>
                /// <value></value>
                public double Contrast
                {
                    get 
                    {
                        return (double)this[CONTRAST].Parameter;
                    }
                    set 
                    {
                        if (value < -1.0 || value > 1.0)
                        {
                            throw new ArgumentOutOfRangeException("Contrast must be between -1.0 and 1.0");
                        }

                        this[CONTRAST].Parameter = value;
                    }
                }
                /// <summary>
                /// Get/set the Auto adjustment for the contrast and brightness values.
                /// Note : This will override any value passed in the other params
                /// </summary>
                /// <value></value>
                public bool AutoBrightnessContrast
                {
                    get 
                    {
                        return (bool)this[AUTOADJUST].Parameter;
                    }
                    set 
                    {
                        this[AUTOADJUST].Parameter = value;
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
                        return "Increase/Decrease the brightness and/or contrast of an image.";
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Instanciate a BrightnessContrastFilter class
            /// </summary>
            public BrightnessContrastFilter()
            {
                FilterParameter contrast = new FilterParameter(CONTRAST, "Adjust the contrast of the image", (double)0.0);
                FilterParameter brightness = new FilterParameter(BRIGHTNESS, "Adjust the brightness of the image", (double)0.0);
                FilterParameter autoAdjust = new FilterParameter(AUTOADJUST, "Automatically adjust the contrast and brightness of the image", (bool)false);

                AddParameter(brightness);
                AddParameter(contrast);
                AddParameter(autoAdjust);
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                private void ComputeBrightnessContrast(IImageAdapter source)
                {
                    int lowerBound = int.MaxValue;
                    int pixelsSum = (int)(source.Width * source.Height);

                    // Apparently the Low3 and High3 are not used because the output image looks bad
                    // => comment them out
                    int low1Pixels = (int)(LOW_1 * pixelsSum);
                    int low2Pixels = (int)(LOW_2 * pixelsSum);
                    int high1Pixels = (int)(HIGH_1 * pixelsSum);
                    int high2Pixels = (int)(HIGH_2 * pixelsSum);
//                    int low3Pixels = (int)(LOW_3 * pixelsSum);
//                    int high3Pixels = (int)(HIGH_3 * pixelsSum);

                    int low1 = int.MinValue;
                    int low2 = int.MinValue;
                    int high1 = int.MinValue;
                    int high2 = int.MinValue;
//                    int low3 = int.MinValue;
//                    int high3 = int.MinValue;

                    // Get indexes for Low middle and high values
                    int[] luminance = CreateGreyHistogram(source, out lowerBound);
                    int pixelCount = 0;
                    for (int index = 0; index < luminance.Length; index++)
                    { 
                        pixelCount += luminance[index];
                        if (pixelCount > low1Pixels && low1 == int.MinValue) { low1 = index; }
                        if (pixelCount > low2Pixels && low2 == int.MinValue) { low2 = index; }
                        if (pixelCount > high2Pixels && high2 == int.MinValue) { high2 = index; }
                        if (pixelCount > high1Pixels && high1 == int.MinValue) { high1 = index; }
//                        if (pixelCount > low3Pixels && low3 == int.MinValue) { low3 = index; }
//                        if (pixelCount > high3Pixels && high3 == int.MinValue) { high3 = index; }
                    }

                    // Get the more appropriate low and high
                    int highIndex = int.MinValue;
                    int lowIndex = int.MaxValue;
/*
                    if (low2 - low1 > DELTA_1_2)
                    {
                          if (low3 - low2 > DELTA_2_3) { lowIndex = low3; }
                        else { lowIndex = low2; }
                    }
                    else { lowIndex = low1; }

                    if (high2 - high1 > DELTA_1_2)
                    {
                        if (high3 - high2 > DELTA_2_3) { highIndex = high3; }
                        else { highIndex = high2; }
                    }
                    else { highIndex = high1; }
*/
                    if (low2 - low1 > DELTA_20 || low2 - low1 < DELTA_5) { lowIndex = low2; }
                    else { lowIndex = low1; }

                    if (high2 - high1 > DELTA_20 || high2 - high1 < DELTA_5) { highIndex = high2; }
                    else { highIndex = high1; }

                    // Determine out best adjustment for brightness and contrast
                    // Note : Need to adjust brightness using lowerBound
                    this[BRIGHTNESS].Parameter = (double)((127.5 - (highIndex + lowIndex + lowerBound * 2) / 2.0) / 255.0);
                    this[CONTRAST].Parameter = (double)((100.0 - (highIndex - lowIndex) * 100 / 255.0) / 100.0);
                }
                private int[] CreateGreyHistogram(IImageAdapter source, out int lowerBound)
                {
                    int redLuminance = 0;
                    int greenLuminance = 0;
                    int blueLuminance = 0;
                    int luminance = 0;
                    int maxLuminance = int.MinValue;
                    int minLuminance = int.MaxValue;
                    IColor color = ColorByte.Empty;
                    Hashtable hash = new Hashtable((int)((source[0,0].MaxChannelValue - source[0,0].MinChannelValue) * source[0,0].NormalizedValue + 1));

                    for (int y = 0; y < source.Height; y++)
                    { 
                        for (int x = 0; x < source.Width; x++)
                        { 
                            color = source[x, y];
                            redLuminance = (int)(color.R * LUMINANCE_RED);
                            greenLuminance = (int)(color.G * LUMINANCE_GREEN); 
                            blueLuminance = (int)(color.B * LUMINANCE_BLUE);
                            luminance = redLuminance + greenLuminance + blueLuminance;
                            if (luminance > maxLuminance) { maxLuminance = luminance;  }
                            if (luminance < minLuminance) { minLuminance = luminance; }
                            if (hash.Contains(luminance)) { int count = (int)hash[luminance]; count++; hash[luminance] = count; }
                            else { int one = 1; hash.Add(luminance, one); }
                        }
                    }
                    lowerBound = minLuminance;
                    int[] retVal = new int[maxLuminance - minLuminance + 1];
                    IDictionaryEnumerator iter = hash.GetEnumerator();
                    while (iter.MoveNext())
                    { 
                        retVal[(int)iter.Key - minLuminance] = (int)iter.Value;
                    }
                    return retVal;

                }
/*
                private int[] ComputeLUT()
                {
                    double delta = NormalizedColor.ComputedUpperBound - NormalizedColor.ComputedLowerBound + 1;


                    double scale = 0;
                    double translate = 0;
                    int[] retVal = new int[(int)(delta)];
                    double normalizedBrightness = (Brightness + 1.0) / 2.0;
                    if (Contrast <= 0.0)
                    {
                        scale = Contrast + 1.0;
                        translate = (normalizedBrightness - (1.0 - normalizedBrightness) * scale) * NormalizedColor.NormalizeValue;
                        for (int index = 0; index < (int)delta; index++)
                        {
                            retVal[index] = (int)(SaturateColor((index * scale + translate) / NormalizedColor.NormalizeValue) * NormalizedColor.NormalizeValue);
                        }

                    }
                    else
                    {
                        scale = 1.0 - Contrast;
                        translate = (1.0 - normalizedBrightness * (scale + 1.0)) * NormalizedColor.NormalizeValue;
                        for (int index = (int)NormalizedColor.ComputedLowerBound; index <= NormalizedColor.ComputedUpperBound; index++)
                        {
                            retVal[index] = (int)(SaturateColor((index - translate) / (scale * NormalizedColor.NormalizeValue)) * NormalizedColor.NormalizeValue);
                        }
                    }
                    return retVal;
                }
*/
                private IColor MapColor(IColor originalColor)
                {
                    IColor  mappedColor = (IColor)originalColor.Clone();
                    double scale = 0;
                    double translate = 0;
                    double normalizedBrightness = (Brightness + 1.0) / 2.0;
                    if (Contrast <= 0.0)
                    {
                        scale = Contrast + 1.0;
                        translate = (normalizedBrightness - (1.0 - normalizedBrightness) * scale);
                        mappedColor.ExtendedAlpha = originalColor.ExtendedAlpha;
                        mappedColor.ExtendedRed = originalColor.ExtendedRed * scale + translate;
                        mappedColor.ExtendedGreen = originalColor.ExtendedGreen * scale + translate;
                        mappedColor.ExtendedBlue = originalColor.ExtendedBlue * scale + translate;

                    }
                    else
                    {
                        scale = 1.0 - Contrast;
                        translate = (1.0 - normalizedBrightness * (scale + 1.0));
                        mappedColor.ExtendedAlpha = originalColor.Alpha;
                        mappedColor.ExtendedRed = (originalColor.ExtendedRed - translate) / scale;
                        mappedColor.ExtendedGreen = (originalColor.ExtendedGreen - translate) / scale;
                        mappedColor.ExtendedBlue = (originalColor.ExtendedBlue - translate) / scale;
                    }

                    return mappedColor;
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Filter Implementation
                /// </summary>
                protected override IImageAdapter ProcessFilter(IImageAdapter source)
                {
                    ImageAdapter iret = new ImageAdapter(source.Width, source.Height);
/*
                    bool isScRGB = false;
*/
                    if (AutoBrightnessContrast)
                    { 
                        ComputeBrightnessContrast(source);
//                        isScRGB = true;
                    }
/*
                    // Compute Lookup table only if there are no scRGB colors.
                    int[] adjustedColorLUT = null;
                    for (int y = 0; y < source.Height && isScRGB == false; y++)
                    {
                        for (int x = 0; x < source.Width; x++)
                        { 
                            if (source[x, y].IsScRGB == true)
                            { 
                                isScRGB = true;
                                break;
                            }
                        }
                    }
                    if (isScRGB == false)
                    {
                        adjustedColorLUT = ComputeLUT();
                    }
*/
                    IColor originalColor = ColorByte.Empty;
                    IColor adjustedColor = ColorByte.Empty;
                    for (int y = 0; y < source.Height; y++)
                    { 
                        for (int x = 0; x < source.Width; x++)
                        { 
/*
                            originalColor = source[x, y];
                            adjustedColor = new ColorDouble();
                            if ( ! isScRGB)
                            {
                                adjustedColor.IsScRGB = false ;
                                adjustedColor.Red = adjustedColorLUT[(int)(originalColor.Red * NormalizedColor.NormalizeValue - NormalizedColor.ComputedLowerBound)] / NormalizedColor.NormalizeValue;
                                adjustedColor.Green = adjustedColorLUT[(int)(originalColor.Green * NormalizedColor.NormalizeValue - NormalizedColor.ComputedLowerBound)] / NormalizedColor.NormalizeValue;
                                adjustedColor.Blue = adjustedColorLUT[(int)(originalColor.Blue * NormalizedColor.NormalizeValue - NormalizedColor.ComputedLowerBound)] / NormalizedColor.NormalizeValue;
                            }
                            else 
                            {
                                // Compute all entries
                                adjustedColor.IsScRGB = true;
                                adjustedColor = MapColor(originalColor);
                            }

                            iret[x,y] = adjustedColor;
*/
                            iret[x, y] = MapColor(source[x, y]);
                        }
                    }
                    return iret;
                }
            #endregion Public Methods
        #endregion Methods
    }
}
