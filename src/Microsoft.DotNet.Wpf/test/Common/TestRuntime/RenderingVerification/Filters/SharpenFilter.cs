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
    /// A Sharpen Filter
    /// </summary>
    public class SharpenFilter: Filter
    {
        #region Constants
            private const string AMOUNT = "Amount";
            private const string RADIUS = "Radius";
        #endregion Constants

        #region Properties
            #region Private Properties
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Amount of the Sharpen
                /// </summary>
                public double Amount
                {
                    get
                    {
                        return (double)this[AMOUNT].Parameter;
                    }
                    set
                    {
                        if (value >= 0.0 && value <= 1.0)
                        {
                            // Acceptable value
                            this[AMOUNT].Parameter = value;
                            return;
                        }
                        // Out of bounds (or wrong value such as float.NaN)
                        throw new ArgumentOutOfRangeException("Amount", value, "Value must be between 0.0 and 1.0");
                    }
                }
                /// <summary>
                /// Radius of the Sharpen
                /// </summary>
                public double Radius
                {
                    get
                    {
                        return (double)this[RADIUS].Parameter;
                    }
                    set
                    {
                        if (value >= 0.0 && value <= 255.0)
                        {
                            // Acceptable value
                            this[RADIUS].Parameter = value;
                            return;
                        }
                        // Out of bounds (or wrong value such as float.NaN)
                        throw new ArgumentOutOfRangeException("Radius", value, "Value must be between 0.0 and 255.0");
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
                        return "Sharpen an image -- MIL Filter.";
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Sharpen Filter constructor
            /// </summary>
            public SharpenFilter()
            {
                FilterParameter amount = new FilterParameter(AMOUNT, "Sharpen Amount", (double)0.0);
                FilterParameter radius = new FilterParameter(RADIUS, "Sharpen Radius", (double)0.0);

                AddParameter(amount);
                AddParameter(radius);
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
                    //Check params
                    if (source == null)
                    {
                        throw new ArgumentNullException("source", "The IImageAdapter passed in cannot be null");
                    }

                    ImageAdapter retVal = new ImageAdapter(source.Width, source.Height);
                    if (Amount == 0.0 || Radius == 0.0)
                    { 
                        // Do nothing filter, return a clone of the original IImageAdapter
                        for (int y = 0; y < source.Height; y++)
                        {
                            for (int x = 0; x < source.Width; x++)
                            {
                                retVal[x, y] = source[x, y];
                            }
                        }
                        return retVal;
                    }

                    // Perform Blur (Gaussian Filter)
                    BlurFilter blurFilter = new BlurFilter();
                    blurFilter.Radius = Radius;
                    blurFilter.Expand = true;
                    retVal = (ImageAdapter)blurFilter.Process(source);

                    // Compute weights factors
                    double weight = (0.4 - 0.4 * Amount) + 0.6;
                    double originalFactor = (weight / (2.0 * weight - 1.0));
                    double blurFactor = ((1.0 - weight) / (2.0 * weight - 1.0));

                    // Combine original image with blurred one to get the Sharpen Image
                    IColor sourceColor  = null;
                    IColor blurColor = null;
                    IColor retValColor = null;
                    for (int y = 0; y < retVal.Height; y++)
                    {
                        for (int x = 0; x < retVal.Width; x++)
                        {
                            sourceColor = (IColor)source[x, y].Clone();
                            blurColor = retVal[x, y];
                            retValColor = retVal[x,y];
                            retValColor.ExtendedAlpha = sourceColor.ExtendedAlpha * originalFactor - blurColor.ExtendedAlpha * blurFactor;
                            retValColor.ExtendedRed = sourceColor.ExtendedRed * originalFactor - blurColor.ExtendedRed * blurFactor;
                            retValColor.ExtendedGreen = sourceColor.ExtendedGreen * originalFactor - blurColor.ExtendedGreen * blurFactor;
                            retValColor.ExtendedBlue = sourceColor.ExtendedBlue * originalFactor - blurColor.ExtendedBlue * blurFactor;
//                            retVal[x, y] = retValColor;
                        }
                    }

                    return retVal;
                }
            #endregion Public Methods
        #endregion Methods
    }
}
