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
    /// convolution Filter 
    /// a convolution mask is applied to every pixel of the image to be processed
    /// the mask is a n x m matrix
    /// foreach value of the matrix the corresponding pixel in the image is added
    /// to the result pixel as a pixel[i,j] * kernel [i+in,j+jm]
    /// no normalization is inforced, it is up to the convolution kenel designer to normalize or not
    /// </summary>
    public class ConvolutionFilter : Filter
    {
        #region Constants
            private const string DERIVE ="Derive";
            private const string EMBOSS ="Emboss";
            private const string LAPLACIAN ="Laplacian";
            private const string SHARPEN ="Sharpen";
            private const string SMOOTH = "Smooth";
            private const string MATRIX = "Matrix";
            private const string CUSTOM = "Custom";
            private const string ABS = "Abs";
        #endregion Constants

        #region Properties
            #region Private Properties
                private string _filter = string.Empty;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// The convolution kernel
                /// </summary>
                public double[,] Matrix
                {
                    get
                    {
                        return (double[,])this[MATRIX].Parameter;
                    }
                    set
                    {
                        this[MATRIX].Parameter = value;
                        _filter = CUSTOM;
                    }
                }

                /// <summary>
                /// one Implementation of a Derive filter
                /// </summary>
                public bool Derive
                {
                    get
                    {
                        this[DERIVE].Parameter = (_filter == DERIVE);
                        return (bool)this[DERIVE].Parameter;
                    }
                    set
                    {
                        Matrix = new double[,] { { -2, -1, 0 }, { -1, 0, 1 }, { 0, 1, 2 } };
                        _filter = DERIVE;
                        this[DERIVE].Parameter = value;
                    }
                }

                /// <summary>
                /// one Implementation of an Emboss filter
                /// </summary>
                public bool Emboss
                {
                    get
                    {
                        this[EMBOSS].Parameter = (_filter == EMBOSS);
                        return (bool)this[EMBOSS].Parameter;
                    }
                    set
                    {
                        Matrix = new double[,] { { -2, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
                        _filter = EMBOSS;
                        this[EMBOSS].Parameter = value;
                    }
                }

                /// <summary>
                /// one Implementation of a Laplacian filter
                /// </summary>
                public bool Laplacian
                {
                    get
                    {
                        this[LAPLACIAN].Parameter = (_filter == LAPLACIAN);
                        return (bool)this[LAPLACIAN].Parameter;
                    }
                    set
                    {
                        Matrix = new double[,] { { -0.7, -1, -0.7 }, { -1, 6.8, -1 }, { -0.7, -1, -0.7 } };
                        _filter = LAPLACIAN;
                        this[LAPLACIAN].Parameter = value;
                    }
                }

                /// <summary>
                /// one Implementation of a Sharpen filter
                /// </summary>
                public bool Sharpen
                {
                    get
                    {
                        this[SHARPEN].Parameter = (_filter == SHARPEN);
                        return (bool)this[SHARPEN].Parameter;
                    }
                    set
                    {
                        Matrix = new double[,] { { -0.1, -0.1, -0.1 }, { -0.1, 1.8, -0.1 }, { -0.1, -0.1, -0.1 } };
                        _filter = SHARPEN;
                        this[SHARPEN].Parameter = value;
                    }
                }
        
                /// <summary>
                /// one Implementation of a Smooth filter (Mean)
                /// </summary>
                /// <value></value>
                public bool Smooth
                {
                    get 
                    {
                        this[SMOOTH].Parameter = (_filter == SMOOTH);
                        return (bool)this[SMOOTH].Parameter;
                    }
                    set 
                    {
                        double center = (1 / 1.8);
                        double outside = (1.0 - center) / 8;
                        Matrix = new double[,] { { outside, outside, outside }, { outside, center, outside }, { outside, outside, outside } };

                        _filter = SMOOTH;
                        this[SMOOTH].Parameter = value;
                    }
                }

                /// <summary>
                /// Take Abs() of the result
                /// </summary>
                public bool Abs
                {
                    get
                    {
                        return (bool)this[ABS].Parameter;
                    }
                    set
                    {
                        this[ABS].Parameter = value;
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
                        return "Kernel based filtering (Smooth/Emboss/Derive/Laplacian/Sharpen/...)";
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Convolution Filter constructor
            /// </summary>
            public ConvolutionFilter()
            {
                FilterParameter laplacian = new FilterParameter(LAPLACIAN,"Set a Laplacian filter kernel", false);
                FilterParameter emboss = new FilterParameter(EMBOSS, "Set an Emboss filter kernel", false);
                FilterParameter matrix = new FilterParameter(MATRIX, "The convolution matrix kernel", new double[,] { { 1.0 } });
                FilterParameter derive = new FilterParameter(DERIVE, "Set a Derive filter kernel", false);
                FilterParameter sharpen = new FilterParameter(SHARPEN, "Set a Sharpen filter kernel", false);
                FilterParameter smooth = new FilterParameter(SMOOTH , "Set a Smoothing filter kernel", false);
                FilterParameter abs = new FilterParameter(ABS, "Compute the Abs() of the transform", true);

                AddParameter(matrix);
                AddParameter(derive);
                AddParameter(emboss);
                AddParameter(laplacian);
                AddParameter(sharpen);
                AddParameter(smooth);
                AddParameter(abs);
            }
        #endregion Constructors

        #region Methods
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

                    if ((bool)this[DERIVE].Parameter) { Derive = true; }

                    if ((bool)this[EMBOSS].Parameter) { Emboss = true; }

                    if ((bool)this[LAPLACIAN].Parameter) { Laplacian = true; }

                    if ((bool)this[SHARPEN].Parameter) { Sharpen = true; }

                    if ((bool)this[SMOOTH].Parameter) { Smooth = true; }

                    if (Matrix == null)
                    {
                        throw new ArgumentException("Convolution Matrix is null");
                    }

                    Console.WriteLine("Convolution Kernel" + Matrix.ToString());

                    // get the convolution kernel dimensions
                    int kernelWidth = Matrix.GetUpperBound(0) + 1;
                    int kernelHeight = Matrix.GetUpperBound(1) + 1;
                    int kxc = kernelWidth / 2;
                    int kyc = kernelHeight / 2;

                    if (kernelWidth > width || kernelHeight > height)
                    {
                        throw new ArgumentException("Convolution kernel can't be larger than image dimensions");
                    }

                    ImageAdapter itemp = new ImageAdapter(width, height);

                    iret = new ImageAdapter(width, height);

                    // convolve the source with the kernel
                    // even kernel are centered on kw/2 kh/2 
                    for (int j = 0; j < height; j++)
                    {
                        for (int i = 0; i < width; i++)
                        {
                            IColor rCol = (IColor)source[i, j].Clone();
                            rCol.RGB = 0;
                            double r = 0;
                            double g = 0;
                            double b = 0;

                            bool goon = true;
                            for (int l = 0; l < kernelWidth; l++)
                            {
                                int idx = i + l - kxc;

                                if (idx < 0 || idx > width - 1)
                                {
                                    idx = i + kxc - l;
                                }

                                for (int m = 0; m < kernelHeight; m++)
                                {
                                    int idy = j + m - kyc;

                                    if (idy < 0 || idy > height - 1)
                                    {
                                        idy = j + kyc - m;
                                    }

                                    IColor lcol = (IColor)source[idx, idy].Clone();
                                
                                    r += lcol.Red * Matrix[l, m];
                                    g += lcol.Green * Matrix[l, m];
                                    b += lcol.Blue * Matrix[l, m];

                                    if (lcol.Alpha == 0) { goon = false; }
                                }
                            }

                            if (r < 0) { r = 0; }if (r > 1) { r = 1; }
                            if (g < 0) { g = 0; }if (g > 1) { g = 1; }
                            if (b < 0) { b = 0; }if (b > 1) { b = 1; }

                            rCol.Red = r;
                            rCol.Green = g;
                            rCol.Blue = b;

                            if (Abs == true)
                            {
                                rCol.Red = Math.Abs(rCol.Red);
                                rCol.Green = Math.Abs(rCol.Green);
                                rCol.Blue = Math.Abs(rCol.Blue);
                            }
                    
                            if (goon == false) { rCol.ARGB = 0; }
                            
                            iret[i, j] = rCol;
                        }
                    }
                }

                return iret;
            }
        #endregion Methods
    }
}
