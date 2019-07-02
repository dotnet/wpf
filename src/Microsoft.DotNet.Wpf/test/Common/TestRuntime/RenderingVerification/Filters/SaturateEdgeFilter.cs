// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification.Filters
{

    #region usings
        using System;
        using System.Drawing;
        using System.ComponentModel;
    #endregion usings

    /// <summary>
    /// Summary description for PainterFilter.
    /// </summary>
    public class SaturateEdgeFilter : Filter
    {
        #region Constants
            private const string THRESHOLD = "Threshold";
            private const string COLORBELOW = "ColorBelow";
            private const string COLORABOVE = "ColorAbove";
            private const string KERNEL = "Kernel";
            private const double EPSILON = 1e-14;
        #endregion Constants

        #region Properties
            #region Private Properties
                private static string _filterDescription = string.Empty;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Self-Description of this filter
                /// </summary>
                /// <value></value>
                public override string FilterDescription
                {
                    get
                    {
                        return _filterDescription;
                    }
                }
                /// <summary>
                /// The threshold to use to draw edge
                /// Note : Expecte a normalized percentage as value
                /// </summary>
                /// <value></value>
                public double Threshold
                {
                    get 
                    {
                        return (double)this[THRESHOLD].Parameter;
                    }
                    set 
                    {
                        if (value > 1.0 || value < 0.0)
                        {
                            throw new ArgumentOutOfRangeException("Threshold", value, "value is in percentage (Normalized). Must be between 0.0 and 1.0");
                        }
                        this[THRESHOLD].Parameter = value;
                    }
                }
                /// <summary>
                /// The color to use for color above the specified threshold
                /// </summary>
                /// <value></value>
                [TypeConverterAttribute(typeof(RenderingColorConverter))] 
                [EditorAttribute(typeof(RenderingColorEditor), typeof(System.Drawing.Design.UITypeEditor))]
                public IColor ColorAboveThresold
                {
                    get
                    {
                        return (IColor)this[COLORABOVE].Parameter;
                    }
                    set
                    {
                        if (value == null)
                        {
                            throw new ArgumentNullException("ColorAboveThresold", "Must be set to a valid instance of IColor (null was passed in)");
                        }
                        this[COLORABOVE].Parameter = value;
                    }
                }
                /// <summary>
                /// The color to use for color below (or equal to) the specified threshold
                /// </summary>
                /// <value></value>
                [TypeConverterAttribute(typeof(RenderingColorConverter))] 
                [EditorAttribute(typeof(RenderingColorEditor), typeof(System.Drawing.Design.UITypeEditor))]
                public IColor ColorBelowThresold
                {
                    get
                    {
                        return (IColor)this[COLORBELOW].Parameter;
                    }
                    set
                    {
                        if (value == null)
                        {
                            throw new ArgumentNullException("ColorBelowThresold", "Must be set to a valid instance of IColor (null was passed in)");
                        }
                        this[COLORBELOW].Parameter = value;
                    }
                }
                /// <summary>
                /// The kernel to be used in the shape finding algorithm
                /// </summary>
                /// <value></value>
                [BrowsableAttribute(false)]
                public double[,] Kernel
                {
                    get
                    {
                        return (double[,])this[KERNEL].Parameter;
                    }
                    set
                    {
                        int kernelHeight = Kernel.GetUpperBound(0);
                        int kernelWidth = Kernel.GetUpperBound(1);
                        if (kernelHeight == 0 || kernelWidth == 0)
                        {
                            throw new ArgumentOutOfRangeException("Kernel", "neither width nor height can be of zero length");
                        }
                        if (IsEvenNumber(kernelHeight) || IsEvenNumber(kernelWidth))
                        {
                            throw new ArgumentException("Kernel width and height must be of odd length", "Kernel");
                        }
                        this[KERNEL].Parameter = value;
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Instanciate a new SaturateEdgeFilter object
            /// </summary>
            public SaturateEdgeFilter ()
            {
                double [,] kernelValues = new double[,] { { 0, -1, 0 }, { -1, 4, -1 }, { 0, -1, 0 } };
                FilterParameter threshold = new FilterParameter(THRESHOLD, "Thresold for the saturating value", (double)0.01);
                FilterParameter kernel = new FilterParameter(KERNEL, "Laplacian Kernel", (double[,])kernelValues);
                FilterParameter colorAbove = new FilterParameter(COLORABOVE, "Color to use if edge find and above threshold", (IColor)new ColorByte(255, 255, 0, 0));
                FilterParameter colorBelow = new FilterParameter(COLORBELOW, "Color to use if below threshold", (IColor)new ColorByte());

                AddParameter(threshold);
                AddParameter(kernel);
                AddParameter(colorAbove);
                AddParameter(colorBelow);
            }
            static SaturateEdgeFilter()
            {
                _filterDescription = "Find the edge of an image with over the specified threshold, saturate the resulting image";
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                private bool IsEvenNumber(int number)
                {
                    return ((number >> 1) << 1) == number;
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Filter implementation
                /// </summary>
                protected override IImageAdapter ProcessFilter(IImageAdapter source)
                {
                    if (ColorAboveThresold == ColorBelowThresold)
                    {
                        throw new ArgumentException("ColorAboveThreshold and ColorBelowThreshold cannot be the same");
                    }

                    IImageAdapter retVal = (IImageAdapter)source.Clone();

                    double[,] kernel = Kernel;
                    int kHeight = kernel.GetUpperBound(0) / 2;
                    int kWidth = kernel.GetUpperBound(1) / 2;
                    IColor colorSum = null;
                    IColor tempColor = null;
                    int count = 0;
                    for (int y = 0; y < source.Height; y++)
                    {
                        for (int x = 0; x < source.Width; x++)
                        {
                            colorSum = new ColorDouble();
                            count = 0;
                            for (int height = -kHeight; height <= kHeight; height++)
                            {
                                for (int width = -kWidth; width <= kWidth; width++)
                                {
                                    // If out of bound mirror the image 
                                    // BUGBUG : *ASSUMING* kernel size < image size
                                    int xPos = x + width;
                                    int yPos = y + height;
                                    if (xPos < 0) { xPos = Math.Abs(xPos); }
                                    if (xPos >= source.Width) { xPos = source.Width - xPos; }
                                    if (yPos < 0) { yPos = Math.Abs(yPos); }
                                    if (yPos >= source.Height) { yPos = source.Height - yPos; }

                                    tempColor = source[xPos, yPos];
                                    colorSum.ExtendedAlpha += tempColor.ExtendedAlpha * kernel[width+kWidth,height+kHeight];
                                    colorSum.ExtendedRed += tempColor.ExtendedRed * kernel[width + kWidth, height + kHeight];
                                    colorSum.ExtendedGreen += tempColor.ExtendedGreen * kernel[width + kWidth, height + kHeight];
                                    colorSum.ExtendedBlue += tempColor.ExtendedBlue * kernel[width + kWidth, height + kHeight];
                                    count++;
                                }
                            }
                            if ( (Math.Abs(colorSum.ExtendedAlpha) + Math.Abs(colorSum.ExtendedRed) + Math.Abs(colorSum.ExtendedGreen) + Math.Abs(colorSum.ExtendedBlue)) / (count * 4) > Threshold + EPSILON)
                            {
                                retVal[x, y] = (IColor)ColorAboveThresold.Clone();
                            }
                            else 
                            {
                                retVal[x, y] = (IColor)ColorBelowThresold.Clone();
                            }
                        }
                    }
                    return retVal;
                }
            #endregion Public Methods
        #endregion Methods
    }
}

