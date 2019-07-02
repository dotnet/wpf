// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.Collections;
    #endregion using

    /// <summary>
    /// Summary description for PixelizeFilter.
    /// </summary>
    public class PixelizeFilter : Filter
    {
        #region Constants
            private const string SQUARESIZE = "SquareSize";
            private const string USEMEAN = "UseMean";
            private const string EXTENDEDSIZE = "ExtendedSize";
        #endregion Constants

        #region Properties
            /// <summary>
            /// Get/set the size of the mosaic
            /// </summary>
            /// <value></value>
            public int SquareSize 
            {
                get 
                {
                    return (int)this[SQUARESIZE].Parameter;
                }
                set 
                {
                    if (value <= 0)
                    {
                        throw new ArgumentOutOfRangeException("SquareSize", value, "the value to be set must be strictly greater than 0");
                    }
                    this[SQUARESIZE].Parameter = value;
                }
            } 
            /// <summary>
            /// Use the Mean (Median) value instead of the average one
            /// </summary>
            /// <value></value>
            public bool UseMean
            {
                get
                {
                    return (bool)this[USEMEAN].Parameter;
                }
                set
                {
                    this[USEMEAN].Parameter = value;
                }
            }
            /// <summary>
            /// The area to process information from(Typically same size as SquareSize)
            /// </summary>
            /// <value></value>
            public int ExtendedSize
            {
                get
                {
                    int retVal = (int)this[EXTENDEDSIZE].Parameter;
                    return (retVal == -1) ? SquareSize : retVal ;
                }
                set
                {
                    if (value <= 0)
                    {
                        throw new ArgumentOutOfRangeException ("ExtendedSize", value, "value to set should be strictly greater than zero");
                    }
                    this[EXTENDEDSIZE].Parameter = value;
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
                    return "Pixelize an image.";
                }
            }
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Create a new instance of the PixelizeFilter class
            /// </summary>
            public PixelizeFilter()
            {
                FilterParameter squareSize = new FilterParameter (SQUARESIZE, "The size of the square in the mosaic", (int)1);
                FilterParameter useMean = new FilterParameter (USEMEAN, "Mean or average", (bool)false);
                FilterParameter extendedSize = new FilterParameter (EXTENDEDSIZE, "Mean or average", (int)-1);

                AddParameter (squareSize);
                AddParameter (useMean);
                AddParameter (extendedSize);
            }
        #endregion Constructors

        #region Methods
            /// <summary>
            /// Perform the PixelizeFilter
            /// </summary>
            /// <param name="source">The image to be processed</param>
            /// <returns>the processed image</returns>
            protected override IImageAdapter ProcessFilter (IImageAdapter source)
            {
                int squareSize = SquareSize;
                ImageAdapter retVal = new ImageAdapter(source.Width, source.Height);
                int extendedSize = ExtendedSize;
                if (extendedSize == 1) { return (IImageAdapter)source.Clone(); }
                ArrayList tempAlpha = null;
                ArrayList tempRed = null;
                ArrayList tempGreen = null;
                ArrayList tempBlue = null;
                int mean = 0;
                int count = 0;
                IColor tempColor = null;
                IColor color = null;

                if (UseMean)
                {
                    int extended = extendedSize * extendedSize;
                    tempAlpha = new ArrayList (extended);
                    tempRed = new ArrayList (extended);
                    tempGreen = new ArrayList (extended);
                    tempBlue = new ArrayList (extended);
                }

                for (int y = 0; y < source.Height; y += squareSize)
                { 
                    for (int x = 0; x < source.Width;  x+= squareSize)
                    {
                        count = 0;
                        color = new ColorDouble(0.0,0.0,0.0,0.0);
                        for (int height = - extendedSize/2; height < extendedSize/2f; height++)
                        {
                            for (int width = - extendedSize/2; width < extendedSize/2f; width++)
                            {
                                if (x + width >=0 && y + height >=0 && x + width < source.Width && y + height < source.Height)
                                {
                                    tempColor = source[x + width, y + height];
                                    if (UseMean)
                                    {
                                        tempAlpha.Add (tempColor.ExtendedAlpha);
                                        tempRed.Add (tempColor.ExtendedRed);
                                        tempGreen.Add (tempColor.ExtendedGreen);
                                        tempBlue.Add (tempColor.ExtendedBlue);
                                    }
                                    else 
                                    {
                                        count++;
                                        color.ExtendedAlpha += tempColor.ExtendedAlpha;
                                        color.ExtendedRed += tempColor.ExtendedRed;
                                        color.ExtendedGreen += tempColor.ExtendedGreen;
                                        color.ExtendedBlue += tempColor.ExtendedBlue;
                                    }
                                }
                            }
                        }

                        if (UseMean)
                        {
                            tempAlpha.Sort ();
                            tempRed.Sort ();
                            tempGreen.Sort ();
                            tempBlue.Sort ();
                            mean = tempAlpha.Count / 2;

                            color.ExtendedAlpha = (double)tempAlpha[mean];
                            color.ExtendedRed = (double)tempRed[mean];
                            color.ExtendedGreen = (double)tempGreen[mean];
                            color.ExtendedBlue = (double)tempBlue[mean];

                            tempAlpha.Clear ();
                            tempRed.Clear ();
                            tempGreen.Clear ();
                            tempBlue.Clear ();
                        }
                        else 
                        {
                            color.ExtendedAlpha /= count;
                            color.ExtendedRed /= count;
                            color.ExtendedGreen /= count;
                            color.ExtendedBlue /= count;
                        }

                        for (int y2 = 0; y2 < squareSize; y2++)
                        {
                            for (int x2 = 0; x2 < squareSize; x2++)
                            {
                                if (x + x2 < source.Width && y + y2 < source.Height)
                                {
                                    retVal[x + x2, y + y2] = color;
                                }
                            }
                        }
                    }
                }
                return retVal;
            }
        #endregion Methods
    }
}
