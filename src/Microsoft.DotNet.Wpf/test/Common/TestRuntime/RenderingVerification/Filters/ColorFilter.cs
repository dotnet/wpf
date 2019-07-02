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
    /// Color modification Filter
    /// Channels are modified using the specified linear combinations
    /// One per channel, default is Identity
    /// </summary>
    public class ColorFilter : Filter
    {
        #region Constants
            private const string COEFF ="Coefficient";
            private const string OFFSET ="Offset";
            private const string ALPHA ="Alpha";
            private const string RED ="Red";
            private const string GREEN ="Green";
            private const string BLUE ="Blue";
            private const string GRAYSCALE = "Gray Scale";

            private const double RED2GRAY = 0.299;
            private const double GREEN2GRAY = 0.587;
            private const double BLUE2GRAY= 0.114;
        #endregion Constants

        #region Properties
            /// <summary>
            /// The Alpha coefficient 
            /// y'= A * y + B
            /// where 
            /// A = AlphaCoefficient
            /// B = AlphaOffset
            /// </summary>
            public double AlphaCoefficient
            {
                get
                {
                    return (double)this[ALPHA + COEFF].Parameter;
                }
                set
                {
                    this[ALPHA + COEFF].Parameter = value;
                }
            }
            /// <summary>
            /// The Alpha offset 
            /// y'= A * y + B
            /// where 
            /// A = AlphaCoefficient
            /// B = AlphaOffset
            /// </summary>
            public double AlphaOffset
            {
                get
                {
                    return (double)this[ALPHA + OFFSET].Parameter;
                }
                set
                {
                    this[ALPHA + OFFSET].Parameter = value;
                }
            }
            /// <summary>
            /// The Red coefficient 
            /// y'= A * y + B
            /// where 
            /// A = RedCoefficient
            /// B = RedOffset
            /// </summary>
            public double RedCoefficient
            {
                get
                {
                    return (double)this[RED + COEFF].Parameter;
                }
                set
                {
                    this[RED + COEFF].Parameter = value;
                }
            }
            /// <summary>
            /// The Red offset 
            /// y'= A * y + B
            /// where 
            /// A = RedCoefficient
            /// B = RedOffset
            /// </summary>
            public double RedOffset
            {
                get
                {
                    return (double)this[RED + OFFSET].Parameter;
                }
                set
                {
                    this[RED + OFFSET].Parameter = value;
                }
            }
            /// <summary>
            /// The Green coefficient 
            /// y'= A * y + B
            /// where 
            /// A = GreenCoefficient
            /// B = GreenOffset
            /// </summary>
            public double GreenCoefficient
            {
                get
                {
                    return (double)this[GREEN + COEFF].Parameter;
                }
                set
                {
                    this[GREEN + COEFF].Parameter = value;
                }
            }
            /// <summary>
            /// The Green offset 
            /// y'= A * y + B
            /// where 
            /// A = GreenCoefficient
            /// B = GreenOffset
            /// </summary>
            public double GreenOffset
            {
                get
                {
                    return (double)this[GREEN + OFFSET].Parameter;
                }
                set
                {
                    this[GREEN + OFFSET].Parameter = value;
                }
            }
            /// <summary>
            /// The Blue coefficient 
            /// y'= A * y + B
            /// where 
            /// A = BlueCoefficient
            /// B = BlueOffset
            /// </summary>
            public double BlueCoefficient
            {
                get
                {
                    return (double)this[BLUE + COEFF].Parameter;
                }
                set
                {
                    this[BLUE + COEFF].Parameter = value;
                }
            }
            /// <summary>
            /// The Blue offset 
            /// y'= A * y + B
            /// where 
            /// A = BlueCoefficient
            /// B = BlueOffset
            /// </summary>
            public double BlueOffset
            {
                get
                {
                    return (double)this[BLUE + OFFSET].Parameter;
                }
                set
                {
                    this[BLUE + OFFSET].Parameter = value;
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
                    return "Modify the image by using a linear combinatination : Cooeficient * Color + Offset";
                }
            }
            /// <summary>
            /// The color image is transformed to Gray scale
            /// </summary>
            public bool GrayScaleConversion
            {
                get
                {
                    return (bool)this[GRAYSCALE].Parameter;
                }
                set
                {
                    this[GRAYSCALE].Parameter = value;
                }
            }
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Color Filter constructor
            /// </summary>
            public ColorFilter()
            {
                FilterParameter grayScaleConversion = new FilterParameter(GRAYSCALE, "Conversion to GrayScale", false);
                FilterParameter alphaCoefficient = new FilterParameter(ALPHA + COEFF, "Alpha coefficient", 1.0);
                FilterParameter alphaOffset = new FilterParameter(ALPHA + OFFSET, "Alpha Offset", 0.0);
                FilterParameter redCoefficient = new FilterParameter(RED + COEFF, "Red coefficient", 1.0);
                FilterParameter redOffset = new FilterParameter(RED + OFFSET, "Red Offset", 0.0);
                FilterParameter greenCoefficient = new FilterParameter(GREEN + COEFF, "Green coefficient", 1.0);
                FilterParameter greenOffset = new FilterParameter(GREEN + OFFSET, "Green Offset", 0.0);
                FilterParameter blueCoefficient = new FilterParameter(BLUE + COEFF, "Blue coefficient", 1.0);
                FilterParameter blueOffset = new FilterParameter(BLUE + OFFSET, "Blue Offset", 0.0);


                AddParameter(alphaCoefficient);
                AddParameter(alphaOffset);

                AddParameter(redCoefficient);
                AddParameter(redOffset);

                AddParameter(greenCoefficient);
                AddParameter(greenOffset);

                AddParameter(blueCoefficient);
                AddParameter(blueOffset);

                AddParameter(grayScaleConversion);
            }
        #endregion Constructors

        #region Methods
            /// <summary>
            /// direct access to the transformation matrix
            /// </summary>
            internal void SetCoefficients(double [,] matrix)
            {
                if (matrix == null)
                {
                    throw new ArgumentNullException("The Coefficient matrix is null");
                }
                if (matrix.GetUpperBound(0) != 4 && matrix.GetUpperBound(1) != 2)
                {
                    throw new ArgumentException("The Coefficient matrix must be  a 4x2 array");
                }
                AlphaCoefficient = matrix[0,0];
                AlphaOffset = matrix[0,1];
                
                RedCoefficient = matrix[1,0];
                RedOffset = matrix[1,1];

                GreenCoefficient = matrix[2,0];
                GreenOffset = matrix[2,1];

                BlueCoefficient = matrix[3,0];
                BlueOffset = matrix[3,1];
            }
            /// <summary>
            /// filter implementation
            /// </summary>
            protected override IImageAdapter ProcessFilter(IImageAdapter source)
            {
                ImageAdapter iret = null;
                if (source != null)
                {
                    int width = source.Width;
                    int height = source.Height;
                                    
                    iret = new ImageAdapter(width,height);
                    for (int j =0;j < height;j++)
                    {
                        for (int i =0;i < width;i++)
                        {
                            IColor lcol = source[i,j];
                            lcol.ExtendedAlpha = AlphaCoefficient * lcol.ExtendedAlpha + AlphaOffset;
                            lcol.ExtendedRed   = RedCoefficient   * lcol.ExtendedRed   + RedOffset;
                            lcol.ExtendedGreen = GreenCoefficient * lcol.ExtendedGreen + GreenOffset;
                            lcol.ExtendedBlue  = BlueCoefficient  * lcol.ExtendedBlue  + BlueOffset;
                        }
                    }

                    if (GrayScaleConversion == true)
                    {
                        for (int j =0;j < height;j++)
                        {
                            for (int i =0;i < width;i++)
                            {
                                IColor lcol = iret[i,j];
                                double gval =  lcol.ExtendedRed * RED2GRAY + lcol.ExtendedGreen * GREEN2GRAY + lcol.ExtendedBlue * BLUE2GRAY;
                                lcol.ExtendedRed   = gval;
                                lcol.ExtendedGreen = gval;
                                lcol.ExtendedBlue  = gval;
                            }
                        }
                    }
                }
                return iret;
            }
        #endregion Methods
    }
}
