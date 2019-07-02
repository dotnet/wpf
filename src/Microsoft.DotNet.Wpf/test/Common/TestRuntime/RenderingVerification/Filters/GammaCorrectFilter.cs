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
    /// A Gamma Correction Filter
    /// </summary>
    public class GammaCorrectFilter: Filter
    {
    
        #region Constants
            private const string REDGAMMA   = "RedGamma";
            private const string GREENGAMMA = "GreenGamma";
            private const string BLUEGAMMA  = "BlueGamma";

            private const int MAXCHANNELVALUE = 255;
            private const int MINCHANNELVALUE = 0;
        #endregion Constants

        #region Properties
            #region Private Properties
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Red Channel
                /// </summary>
                public double RedGamma
                {
                    get
                    {
                        return (double)this[REDGAMMA].Parameter;
                    }
                    set
                    {
                        this[REDGAMMA].Parameter = value;
                    }
                }
                /// <summary>
                /// Green Channel
                /// </summary>
                public double GreenGamma
                {
                    get
                    {
                        return (double)this[GREENGAMMA].Parameter;
                    }
                    set
                    {
                        this[GREENGAMMA].Parameter = value;
                    }
                }
                /// <summary>
                /// Blue Channel
                /// </summary>
                public double BlueGamma
                {
                    get
                    {
                        return (double)this[BLUEGAMMA].Parameter;
                    }
                    set
                    {
                        this[BLUEGAMMA].Parameter = value;
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
                        return "Correct the Gamma of an image -- Mil filter";
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// GammaCorrect Filter constructor
            /// </summary>
            public GammaCorrectFilter()
            {
                FilterParameter redgamma   = new FilterParameter(REDGAMMA, "Gamma of Red Channel", (double)1.0);
                FilterParameter greengamma = new FilterParameter(GREENGAMMA, "Gamma of Green Channel", (double)1.0);
                FilterParameter bluegamma  = new FilterParameter(BLUEGAMMA, "Gamma of Blue Channel", (double)1.0);

                AddParameter(redgamma);
                AddParameter(greengamma);
                AddParameter(bluegamma);
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                private byte[] CreateLUT(double gammaCorrection)
                {
                    byte[] retVal = new byte[MAXCHANNELVALUE + 1];
                    int newVal = 0;
                    for (int t = 0; t <= MAXCHANNELVALUE; t++)
                    {
                        newVal = (int)(Math.Pow(t / (double)MAXCHANNELVALUE, gammaCorrection) * MAXCHANNELVALUE);
                        if (newVal > MAXCHANNELVALUE) { newVal = MAXCHANNELVALUE; } 
                        if (newVal < MINCHANNELVALUE) { newVal = MINCHANNELVALUE; }
                        retVal[t] = (byte)newVal;
                    }
                    return retVal;
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Filter Implementation
                /// </summary>
                protected override IImageAdapter ProcessFilter(IImageAdapter source)
                {
                    ImageAdapter iret = null;
                    iret = new ImageAdapter(source.Width, source.Height);

                    // Compute the curve for all channels (except Alpha which remains constant)
                    byte[] redLUT = CreateLUT(RedGamma);
                    byte[] greenLUT = CreateLUT(GreenGamma);
                    byte[] blueLUT = CreateLUT(BlueGamma);

                    IColor color = null;
                    for (int y = 0; y < iret.Height; y++)
                    {
                        for (int x = 0; x < iret.Width; x++)
                        {
                            color = (IColor)source[x,y].Clone();
                            color.RGB  = 0; // Keep alpha value
                            color.R = redLUT[source[x, y].R];
                            color.G = greenLUT[source[x, y].G];
                            color.B = blueLUT[source[x, y].B];
                            iret[x, y] = color;
                        }
                    }
                    return iret;
                }
            #endregion Public Methods
        #endregion Methods
    }
}
