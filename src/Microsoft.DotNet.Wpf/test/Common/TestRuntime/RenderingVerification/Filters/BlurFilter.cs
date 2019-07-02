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
    /// A Gaussian Blur Filter (MIL Filter)
    /// </summary>
    public class BlurFilter:Filter
    {
        #region Constants
            private const string RADIUS = "Radius";
            private const string EXPAND = "Expand";
            // From BitmapEffectBlur.cpp
            private const double DECIMATIONFACTOR = 0.1;
            private const double CUTOFF = 1.4;
        #endregion Constants

        #region Properties
            #region Private Properties
                private double[] _kernel = null;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// The radius of the blur
                /// </summary>
                /// <value></value>
                public double Radius
                {
                    get 
                    {
                        return (double)this[RADIUS].Parameter;
                    }
                    set 
                    {
                        if (value < 0.0) { throw new ArgumentOutOfRangeException("Radius must be positive"); }
                        this[RADIUS].Parameter = value;
                    }
                }
                /// <summary>
                /// Miror Image to fix edge effect
                /// </summary>
                /// <value></value>
                public bool Expand
                {
                    get 
                    {
                        return (bool)this[EXPAND].Parameter;
                    }
                    set 
                    {
                        this[EXPAND].Parameter = value;
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
                        return "Blur the image (MIL filter)";
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Blur Filter constructor
            /// </summary>
            public BlurFilter()
            {
                FilterParameter radius = new FilterParameter(RADIUS, "The value of the Radius to use for the blur", (double)0.0);
                FilterParameter expand = new FilterParameter(EXPAND, "Indicates if the data should be mirrored to fit in the filter (side effect) or dropped resulting in fading on the edges", false);
                AddParameter(radius);
                AddParameter(expand);
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                /// <summary>
                /// Initialize the Gaussian kernel for Blur
                /// ( Formula below extracted from GDI+ / Avalon code )
                /// </summary>
                private void InitializeKernel()
                {
                    // Create Kernel
                    int plotResDiff = 0;
                    if (Radius >= 1.0)
                    {
                        plotResDiff = (int)Math.Max( 0.0, Math.Log(Radius * DECIMATIONFACTOR) / Math.Log(2) );
                    }
                    double plotRadius = Radius / Math.Pow(2, plotResDiff);
                    _kernel = new double[(int)Math.Ceiling(plotRadius) + 1];

                    // Populate Kernel
                    double interval = CUTOFF / plotRadius;
                    double rX = interval;
                    _kernel[0] = Math.Exp(-rX * rX);
                    double sum = 0.0;//_kernel[0];
                    for (int t = 0; t < _kernel.Length; t++)
                    {
                        rX += interval;
                        _kernel[t] = Math.Exp(-rX * rX);
                        sum += _kernel[t] * 2;
                    }

                    // Normalize Kernel
                    for (int t = 0; t < _kernel.Length; t++)
                    {
                        _kernel[t] /= sum;
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
                        throw new ArgumentNullException("source", "The value passed in cannot be null");
                    }
                    if (Radius == 0)
                    {
                        // TODO : wrong if Expanded is true
                        return (IImageAdapter)source.Clone();
                    }

                    int width = source.Width;
                    int height = source.Height;

                    ImageAdapter retVal = null;
                    if (Expand)
                    {
                        retVal = new ImageAdapter((int)(width + (Radius + 0.5) * 2), (int)(height + (Radius + 0.5) * 2));
                    }
                    else 
                    {
                        retVal = new ImageAdapter(width, height);
                    }

                    InitializeKernel();
                    GaussianFilter gaussianFilter = new GaussianFilter();
                    gaussianFilter.Kernel = _kernel;

                    return gaussianFilter.Process(source);
                }
            #endregion Public Methods
        #endregion Methods
    }
}
