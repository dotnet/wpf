// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    using System;
    using System.Drawing;
    using System.Collections;
//    using System.Drawing.Imaging;
    using Microsoft.Test.RenderingVerification;

    /// <summary>
    /// The type of shape available to the MorphShape Filter
    /// </summary>
    public enum Shape
    {
        /// <summary>
        /// Undefined shape
        /// </summary>
        None = 0,
        /// <summary>
        /// A square
        /// </summary>
        Square,
        /// <summary>
        /// A circle
        /// </summary>
        Circle,
        /// <summary>
        /// An equilateral triangle
        /// </summary>
        Triangle,
        /// <summary>
        /// A regular polygon
        /// </summary>
        Polygon
    }
    /// <summary>
    /// A Gaussian Filter
    /// </summary>
    public class MorphShapeFilter : Filter
    {
    
        #region Constants
            private const string MORPHSHAPE ="MorphShape";
            private const string EXTENDTOMAXSIDE = "ExtendToMaxSide";
        #endregion Constants

        #region Properties
            #region Private Properties
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Morph the image to fit into a different shape
                /// </summary>
                public Shape MorphToShape
                {
                    get
                    {
                        return (Shape)this[MORPHSHAPE].Parameter;
                    }
                    set
                    {
                        this[MORPHSHAPE].Parameter = value;
                    }
                }
                /// <summary>
                /// Use the Largest value of Height ot Width
                /// </summary>
                public bool ExtendToMaxSide
                {
                    get
                    {
                        return (bool)this[EXTENDTOMAXSIDE].Parameter;
                    }
                    set
                    {
                        this[EXTENDTOMAXSIDE].Parameter = value;
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
                        return "Morph an image to fit into a shape.";
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// MorphShape Filter constructor
            /// </summary>
            public MorphShapeFilter()
            {
                FilterParameter morphShape = new FilterParameter(MORPHSHAPE, "Morph the image to be contained in another shape", (Shape)Shape.Circle);
                FilterParameter extendToMaxSide = new FilterParameter (EXTENDTOMAXSIDE, "Extend the image to the biggest size (Width / Height)", (bool)false);

                AddParameter (extendToMaxSide);
                AddParameter (morphShape);
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
                    IImageAdapter retVal = null;

                    if (source == null)
                    {
                        throw new ArgumentNullException("source", "IImageAdapter must be set to a valid instance (null passed in)");
                    }

                    if (MorphToShape != Shape.Square && MorphToShape != Shape.Circle)
                    {
                        throw new NotImplementedException("Will be implemented if requested");
                    }

                    int width = source.Width;
                    int height = source.Height;
                    int side = 0;

                    if (ExtendToMaxSide == false)
                    {
                        side = (int)Math.Min (width, height);
                    }
                    else 
                    {
                        side = (int)Math.Max(width,height);
                    }

                    retVal = new ImageAdapter(side, side);

                    if (MorphToShape == Shape.Square || MorphToShape == Shape.Circle)
                    {
                        // Shrink/Expand image to square
                        double ratio = (double)width / (double)height;
                        double[] matrix = new double[] {1,0,0,1,0,0};
                        if (ratio >= 1.0)
                        {
                            if (ExtendToMaxSide)
                            {
                                matrix[3] *= ratio;
                            }
                            else 
                            {
                                matrix[0] /= ratio;
                            }
                        }
                        else 
                        {
                            if (ExtendToMaxSide)
                            {
                                matrix[0] /= ratio;
                            }
                            else
                            {
                                matrix[3] *= ratio;
                            }
                        }
                        SpatialTransformFilter filter = new SpatialTransformFilter();
                        filter.Matrix = matrix;
                        retVal = filter.Process(source);
                    }

                    // Step 2 : Compute every pixel in resulting image
                    if (MorphToShape == Shape.Circle)
                    {
                        int x = 0;
                        int y = 0;
                        double center = side / 2.0;
                        double Y = double.NaN;
                        double X = double.NaN;
                        double theta = double.NaN;
                        double ratio = double.NaN;
                        double xSource = double.NaN;
                        double ySource = double.NaN;
                        ImageAdapter temp = new ImageAdapter(side, side);

                        for (y = 0; y < side; y++)
                        {
                            for (x = 0; x < side; x++)
                            {
                                X = x - center;
                                Y = center - y;
                                theta = Math.Atan (Y / X);
                                ratio = Math.Max (Math.Abs (Math.Cos (theta)), Math.Abs (Math.Sin (theta)));
                                xSource = X / ratio + center;
                                ySource = -(Y / ratio - center);
                                if (xSource >= 0 && xSource < side && ySource >= 0 && ySource < side)
                                {
                                    temp[x, y] = (IColor)retVal[(int)xSource, (int)ySource].Clone();
                                }
                                else 
                                {
                                    temp[x, y] = (IColor)retVal[0, 0].Clone(); temp[x, y].IsEmpty = true;
                                }
                            }
                        }
                        retVal = temp;
                    }

                    return retVal;
                }

            #endregion Public Methods
        #endregion Methods
    }
}
