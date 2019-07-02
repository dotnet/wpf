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
    /// A Glow Filter
    /// </summary>
    [ObsoleteAttribute("This filter will be removed soon, please update your testcase")]
    public class GlowFilter: Filter
    {
        #region Constants
            private const string RADIUS = "Radius";
            private const string INTENSITY = "Intensity";
            private const string ASPECTRATIO = "AspectRatio";
            private const string COMPOSITE = "Composite";
            private const string INNERCOLOR = "InnerColor";
            private const string OUTERCOLOR = "OuterColor";

            private const int GLOW_GAUSS_SAMPLES = 600;
            private const int BLENT_LUT_SIZE = 256;
            private const int GLOW_ALPHA_THRESHOLD = 10;

        #endregion Constants

        #region Properties
            #region Private Properties
                IColor[] _blendLUT = new IColor[BLENT_LUT_SIZE];
                float[] _gaussLUT = new float[GLOW_GAUSS_SAMPLES];
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Radius of the Glow (varies between 0.0 and double.MaxValue)
                /// </summary>
                public double Radius
                {
                    get
                    {
                        return (double)this[RADIUS].Parameter;
                    }
                    set
                    {
                        if (value >= 0.0 && value <= double.MaxValue)
                        {
                            this[RADIUS].Parameter = value;
                            return;
                        }
                        throw new ArgumentOutOfRangeException("Radius", value, "The value passed in is invalid, should be between 0.0 and double.MaxValue");
                    }
                }
                /// <summary>
                /// Intensity of the outerColor against innerColor (varies between 1 and 500)
                /// </summary>
                public double Intensity
                {
                    get
                    {
                        return (double)this[INTENSITY].Parameter;
                    }
                    set
                    {
                        if (value >= 0.0 && value <= 1.0)
                        {
                            this[INTENSITY].Parameter = value;
                            return;
                        }
                        throw new ArgumentOutOfRangeException("Internsity", value, "The value passed in is invalid, should be between 0.0 and 1.0");
                    }
                }
                /// <summary>
                /// Expand the glow in one direction, an other, or none (positive value is a horizontal stretch, negative value is a vertical stretch) -- (varies between 0.1 and 10)
                /// </summary>
                public double AspectRatio
                {
                    get
                    {
                        return (double)this[ASPECTRATIO].Parameter;
                    }
                    set
                    {
                        if (value >= -1.0 && value <= 1.0)
                        {
                            this[ASPECTRATIO].Parameter = value;
                            return;
                        }
                        throw new ArgumentOutOfRangeException("AspectRarion", value, "The value passed in is invalid, should be between -1.0 and 1.0");
                    }
                }
                /// <summary>
                /// reinject the source image into the destination image (source + glow or glow alone)
                /// </summary>
                public bool Composite
                {
                    get
                    {
                        return (bool)this[COMPOSITE].Parameter;
                    }
                    set
                    {
                        this[COMPOSITE].Parameter = value;
                    }
                }
                /// <summary>
                /// Inner Color of the Glow; for best result, opacity should be HIGH
                /// </summary>
                public Color InnerColor
                {
                    get
                    {
                        return (Color)this[INNERCOLOR].Parameter;
                    }
                    set
                    {
                        this[INNERCOLOR].Parameter = value;
                    }
                }
                /// <summary>
                /// Outer Color of the Glow; for best result, opacity should be LOW
                /// </summary>
                public Color OuterColor
                {
                    get
                    {
                        return (Color)this[OUTERCOLOR].Parameter;
                    }
                    set
                    {
                        this[OUTERCOLOR].Parameter = value;
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
                        return "Perform a Glow around object with low alpha";
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Glow Filter constructor
            /// </summary>
            public GlowFilter()
            {
                FilterParameter radius = new FilterParameter(RADIUS, "Radius of the Glow", (double)10.0);
                FilterParameter intensity = new FilterParameter(INTENSITY, "Intensity of the Glow", (double)0.5);
                FilterParameter aspectratio = new FilterParameter(ASPECTRATIO, "Aspect Ratio of the Glow", (double)0.0);
                FilterParameter composite = new FilterParameter(COMPOSITE, "Compose original image with Glow?", (bool)true);
                FilterParameter innercolor = new FilterParameter(INNERCOLOR, "Inner Color of the Glow", (Color)Color.FromArgb(0x00,0xff,0xff,0xff));
                FilterParameter outercolor = new FilterParameter(OUTERCOLOR, "Outer Color of the Glow", (Color)Color.FromArgb(0x00,0x00,0xff,0x00));

                AddParameter(radius);
                AddParameter(intensity);
                AddParameter(aspectratio);
                AddParameter(composite);
                AddParameter(innercolor);
                AddParameter(outercolor);
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                private void CreateLUT()
                {
                    // Create curve for color transition (inner to outer color)
                    double dist = 0.0;
                    for (int i = 0; i < _gaussLUT.Length; i++)
                    {
                        _gaussLUT[i] = (float)Math.Pow(Math.E, -dist * dist);
                        dist += 0.01;
                    }

                    // Create Composition Lookup Table
                    double scale1 = float.NaN;
                    double scale2 = float.NaN;
                    double premul = float.NaN;
                    IColor color = null;
                    for (int i = 0; i < _blendLUT.Length; i++)
                    {
                        color = new ColorByte();
                        scale1 = i / color.NormalizedValue;
                        scale2 = (color.NormalizedValue - i) / color.NormalizedValue;
                        color.Alpha = InnerColor.A / color.NormalizedValue * scale1 + OuterColor.A / color.NormalizedValue * scale2;
                        premul = color.Alpha;
                        color.Red = premul * InnerColor.R / color.NormalizedValue * scale1 + OuterColor.R / color.NormalizedValue * scale2;
                        color.Green = premul * InnerColor.G / color.NormalizedValue * scale1 + OuterColor.G / color.NormalizedValue * scale2;
                        color.Blue = premul * InnerColor.B / color.NormalizedValue * scale1 + OuterColor.B / color.NormalizedValue * scale2;
                        _blendLUT[i] = color;
                    }
/*
ImageAdapter test = new ImageAdapter(256,256);
for(int y=0;y<256;y++)
{
for(int x=0;x<256;x++)
{
test[x, y] = _blendLUT[x];
}
}
test.ToBitmap().Save("c:\\test.png");
*/
}
                private ImageAdapter GenerateGlowImage(IImageAdapter source)
                {
                    int xSize = (int)((Radius + 1) / 2);
                    int ySize = (int)((Radius + 1) / 2);
                    int width = source.Width;
                    int height = source.Height;
                    ImageAdapter retVal = (ImageAdapter)source.Clone();

                    // for each transparent pixel, find the closest color
                    for(int y = 0; y < retVal.Height; y++)
                    {
                        for (int x = 0; x < retVal.Width; x++)
                        {
                            if (retVal[x, y].A > GLOW_ALPHA_THRESHOLD) { retVal[x, y].IsEmpty = true; continue; }

                            IColor color = new ColorDouble();
                            retVal[x, y] = color;
                            for (int xDelta = -xSize; xDelta < xSize; xDelta++)
                            {
                                for (int yDelta = -ySize; yDelta < ySize; yDelta++)
                                {
                                    if(x+ xDelta < 0 || y+yDelta < 0 || x+xDelta >= width || y+yDelta >= height) { continue; }
                                    IColor temp = source[x + xDelta, y + yDelta];
                                    if (temp.Alpha > GLOW_ALPHA_THRESHOLD / temp.NormalizedValue)
                                    {
                                        double distance = Math.Sqrt(xDelta * xDelta + yDelta * yDelta);
                                        if (distance > Radius || (color.ExtendedAlpha != 0 && distance >= color.ExtendedAlpha) ) {continue;}
                                        color.ExtendedAlpha = distance;
                                    }
                                }
                            }
                        }
                    }

                    // Parse the Array and map the distance to the appropriate color
                    for (int y = 0; y < retVal.Height; y++)
                    {
                        for (int x = 0; x < retVal.Width; x++)
                        {
                            IColor color = retVal[x, y];
                            if (color.Alpha != 0.0)
                            {
                                retVal[x, y] = GetMappedColor(color);
                            }
                            else 
                            {
                                if (Composite)
                                {
                                    retVal[x, y] = (IColor)source[x, y].Clone();
                                }
                            }
                        }
                    }

                    return retVal;
                }
                private IColor GetMappedColor(IColor colorDistance)
                {
                    return (IColor)_blendLUT[_blendLUT.Length - (int)(colorDistance.ExtendedAlpha * _blendLUT.Length / Radius)].Clone();
                }
            #endregion Private Methods
                #region Public Methods
                /// <summary>
                /// Filter Implementation
                /// </summary>
                protected override IImageAdapter ProcessFilter(IImageAdapter source)
                {
                    double maxSize = Math.Max(source.Width, source.Height);
                    if (Radius > maxSize)
                    {
                        Radius = Math.Max(source.Width, source.Height);
                    }

                    // BUGBUG : Might need more code here :
                    //  * Assuming returned image has the same size as input image
                    //  * AspectRatio not implemented
                    //  * Intensity not implemented

                    CreateLUT();
                    double mappedAspectRatio = Math.Pow(10.0, AspectRatio / 100.0);
                    return GenerateGlowImage(source);
                }
            #endregion Public Methods
        #endregion Methods
    }
}
