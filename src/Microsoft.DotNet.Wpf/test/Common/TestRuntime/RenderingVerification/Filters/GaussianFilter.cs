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
    /// A Gaussian Filter
    /// </summary>
    public class GaussianFilter:Filter
    {
    
        #region Constants
            private const string LENGTH ="Length";
            private const string SIGMA  ="Sigma";
            private const string KERNEL = "Kernel";
        #endregion Constants

        #region Properties
            #region Private Properties
                private double[] _kernel  = new double[0];
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// The length of the kernel
                /// </summary>
                public int Length
                {
                    get
                    {
                        return (int)this[LENGTH].Parameter;
                    }
                    set
                    {
                        if (Length < 0)
                        {
                            throw new ArgumentOutOfRangeException("Length", "Value to be set must be positive (or zero)");
                        }
                        this[LENGTH].Parameter = value;
                        Kernel = null;
                    }
                }
                /// <summary>
                /// The Sigma parameter of the gaussian
                /// </summary>
                public double Sigma
                {
                    get
                    {
                        return (double)this[SIGMA].Parameter;
                    }
                    set
                    {
                        if (Sigma <= 0.0)
                        {
                            throw new ArgumentOutOfRangeException("Sigma must be strictly positive");
                        }

                        this[SIGMA].Parameter = value;
                        Kernel = null;
                    }
                }
                /// <summary>
                /// The Kernel to use for the Gaussian (Override Length and sigma, must be set to null to use Length and sigma)
                /// </summary>
                [System.ComponentModel.BrowsableAttribute(false)]   // Do not show this on the UI
                public double[] Kernel
                {
                    get 
                    {
                        return _kernel;
                    }
                    set 
                    {
                        if (value == null)
                        {
                            _kernel = new double[0];
                        }
                        else
                        {
                            _kernel = value;
                        }
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
                        return "Blur the image using a gaussian.";
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Gaussian Filter constructor
            /// </summary>
            public GaussianFilter()
            {
                FilterParameter length = new FilterParameter(LENGTH,"The length of the filter",(int)0);
                FilterParameter sigma  = new FilterParameter(SIGMA,"The value of the Gaussian",(double)1.0);
                FilterParameter kernel = new FilterParameter(KERNEL, "The kernel to be used for the Gaussian", (double[])new double[0]);

                AddParameter(length);
                AddParameter(sigma);
                AddParameter(kernel);
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                private void GenerateKernel()
                {
                    double weight = 0.0;
                    _kernel = new double [Length];
                    for(int i = 0; i < Length; i++)
                    {
                        _kernel[i] = Math.Exp((-Sigma * i * i) / (Length * Length));
                        weight += _kernel[i];
                    }
                    weight *= 2.0;
                    for(int j = 0; j  < Length; j++)
                    {
                        _kernel[j] /= weight;
                    }
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Filter Implementation
                /// </summary>
                protected override IImageAdapter ProcessFilter(IImageAdapter source)
                {
                    if (source == null)
                    {
                        throw new ArgumentNullException("source", "The IImageAdapter must be set to a valid instance (null passed in)");
                    }
                    if (Length == 0 && _kernel.Length == 0)
                    {
                        // Length is zero, no gaussian to be applied; return same imageAdapter as the one passed in
                        return (IImageAdapter)source.Clone();
                    }

                    int width = source.Width;
                    int height = source.Height;
                    ImageAdapter retVal = new ImageAdapter(width, height);

                    // If necessary, create the kernel to be used by the Gaussian filter
                    if (_kernel.Length == 0) { GenerateKernel(); }

                    // ( the kernel is separable -- two pass algorithm : horizontal then vertical pass )
                    ImageAdapter itemp = new ImageAdapter(width, height);
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            IColor lcol = new ColorDouble();

                            for (int i = 0; i < _kernel.Length; i++)
                            {
                                int minus = x - i;
                                if (minus < 0) 
                                {
                                    // out of bound, mirror it
                                    int remainder = 0;
                                    int evenNumber = Math.DivRem(1+minus, width, out remainder);
                                    minus = (((evenNumber >> 1) << 1) == evenNumber) ? -remainder : width - 1 + remainder;
                                }
                                IColor lbc = source[minus, y];
                                lcol.ExtendedAlpha += _kernel[i] * lbc.ExtendedAlpha;
                                lcol.ExtendedRed += _kernel[i] * lbc.ExtendedRed;
                                lcol.ExtendedGreen += _kernel[i] * lbc.ExtendedGreen;
                                lcol.ExtendedBlue += _kernel[i] * lbc.ExtendedBlue;

                                int plus = x + i;
                                if (plus >= width)
                                {
                                    // out of bound, mirror it
                                    int remainder = 0;
                                    int evenNumber = Math.DivRem(plus, width, out remainder);
                                    plus = (((evenNumber >> 1) << 1) == evenNumber) ? remainder : width - 1 - remainder;
                                }
                                IColor lfc = source[plus, y];
                                lcol.ExtendedAlpha += _kernel[i] * lfc.ExtendedAlpha;
                                lcol.ExtendedRed += _kernel[i] * lfc.ExtendedRed;
                                lcol.ExtendedGreen += _kernel[i] * lfc.ExtendedGreen;
                                lcol.ExtendedBlue += _kernel[i] * lfc.ExtendedBlue;
                            }
                            itemp[x, y] = lcol;
                        }
                    }
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            IColor lcol = new ColorDouble();

                            for (int i = 0; i < _kernel.Length; i++)
                            {
                                int minus = y - i;
                                if (minus < 0) 
                                {
                                    // out of bound, mirror it
                                    int remainder = 0;
                                    int evenNumber = Math.DivRem(1+minus, height, out remainder);
                                    minus = (((evenNumber >> 1) << 1) == evenNumber) ? -remainder : height - 1 + remainder;
                                }
                                IColor lbc = itemp[x, minus];
                                lcol.ExtendedAlpha += _kernel[i] * lbc.ExtendedAlpha;
                                lcol.ExtendedRed += _kernel[i] * lbc.ExtendedRed;
                                lcol.ExtendedGreen += _kernel[i] * lbc.ExtendedGreen;
                                lcol.ExtendedBlue += _kernel[i] * lbc.ExtendedBlue;

                                int plus = y + i;
                                if (plus >= height)
                                {
                                    // out of bound, mirror it
                                    int remainder = 0;
                                    int evenNumber = Math.DivRem(plus, height, out remainder);
                                    plus = (((evenNumber >> 1) << 1) == evenNumber) ? remainder : height - 1 - remainder;
                                }
                                IColor lfc = itemp[x, plus];
                                lcol.ExtendedAlpha += _kernel[i] * lfc.ExtendedAlpha;
                                lcol.ExtendedRed += _kernel[i] * lfc.ExtendedRed;
                                lcol.ExtendedGreen += _kernel[i] * lfc.ExtendedGreen;
                                lcol.ExtendedBlue += _kernel[i] * lfc.ExtendedBlue;

                            }
                            retVal[x, y] = lcol;
                        }
                    }

                    // Convert ColorDouble to the IColor type of the source imageAdapter
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            if (retVal[x, y].GetType() != source[x, y].GetType())
                            { 
                                IColor temp = (IColor)source[x, y].Clone();
                                temp.ExtendedAlpha = retVal[x, y].ExtendedAlpha;
                                temp.ExtendedRed = retVal[x, y].ExtendedRed;
                                temp.ExtendedGreen = retVal[x, y].ExtendedGreen;
                                temp.ExtendedBlue = retVal[x, y].ExtendedBlue;
                                retVal[x, y] = temp;
                            }
                        }
                    }

                    return retVal;
                }

            #endregion Public Methods
        #endregion Methods
    }
}
