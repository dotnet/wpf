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
    /// A Grayscale Filter
    /// </summary>
    public class GrayscaleFilter: Filter
    {
    
        #region Constants
            const double RED = 0.212671;
            const double GREEN = 0.715160;
            const double BLUE = 0.072169;
        #endregion Constants

        #region Properties
            #region Private Properties
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Get the description for this filter
                /// </summary>
                /// <value></value>
                public override string FilterDescription
                {
                    get
                    {
                        return "Convert an image to grayscale -- MIL Filter.";
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Grayscale Filter constructor
            /// </summary>
            public GrayscaleFilter()
            {
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
                    if (source == null)
                    {
                        throw new ArgumentNullException("source", "The IImageAdapter cannot be null");
                    }
                    if (source.Width == 0 || source.Height == 0)
                    {
                        throw new ArgumentException("The IImageAdapter is invalid (width and/or height is zero)");
                    }

                    int width = source.Width;
                    int height = source.Height;

                    IColor lcol = null;
                    ImageAdapter retVal = new ImageAdapter(width, height);
                    for (int j = 0; j < height; j++)
                    {
                        for (int i = 0; i < width; i++)
                        {
                            lcol = (IColor)source[i, j].Clone();
                            // Do not try to convert an empty color
                            if (lcol.IsEmpty == false)
                            {
                                double gval = lcol.Red * RED + lcol.Green * GREEN + lcol.Blue * BLUE;
                                lcol.Red = gval;
                                lcol.Green = gval;
                                lcol.Blue = gval;
                            }
                            retVal[i, j] = lcol;
                        }
                    }
                    return retVal;
                }
            #endregion Public Methods
        #endregion Methods
    }
}
