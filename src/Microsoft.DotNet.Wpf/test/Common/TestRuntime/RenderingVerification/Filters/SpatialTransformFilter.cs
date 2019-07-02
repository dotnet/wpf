// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.Drawing;
        using System.Collections;
        using System.ComponentModel;
        using System.Drawing.Imaging;
        using Microsoft.Test.RenderingVerification;
    #endregion using

    /// <summary>
    /// The SpatialTransformFilter can perform rotation, offset, scaling and miroring of an image
    /// </summary>
    public class SpatialTransformFilter : Filter
    {
        #region Constants
            private const string ROTATION = "Rotation";
            private const string FLIPX = "HorizontalFlip";
            private const string FLIPY = "VerticalFlip";
            private const string OFFSETX = "xOffset";
            private const string OFFSETY = "yOffset";
            private const string SCALINGX = "xScaling";
            private const string SCALINGY = "yScaling";
            private const string MATRIX = "Matrix";
            private const string USEMATRIX = "UseMatrix";
            private const string RESIZEOUTPUTIMAGE = "ResizeOutputImage";
        #endregion Constants

        #region Properties
            #region Private Properties
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// The Angle (in degree) you want to rotate the image (clockwise)
                /// </summary>
                /// <value></value>
                public double Rotation
                {
                    get 
                    {
                        return (double)this[ROTATION].Parameter;
                    }
                    set 
                    {
                        this[ROTATION].Parameter = value;
                        UseMatrix = false;
                    }
                }
                /// <summary>
                /// Get/set the Flip on the horizontal axis 
                /// </summary>
                /// <value></value>
                public bool HorizontalFlip
                {
                    get 
                    {
                        return (bool)this[FLIPX].Parameter;
                    }
                    set 
                    {
                        this[FLIPX].Parameter = value;
                        UseMatrix = false;
                    }
                }
                /// <summary>
                /// Get/set the Flip on the vertical axis 
                /// </summary>
                /// <value></value>
                public bool VerticalFlip
                {
                    get 
                    {
                        return (bool)this[FLIPY].Parameter;
                    }
                    set 
                    {
                        this[FLIPY].Parameter = value;
                        UseMatrix = false;
                    }
                }
                /// <summary>
                /// Get/set the offset on the horizontal axis 
                /// </summary>
                /// <value></value>
                public double HorizontalOffset
                {
                    get 
                    {
                        return (double)this[OFFSETX].Parameter;
                    }
                    set 
                    {
                        this[OFFSETX].Parameter = value;
                        UseMatrix = false;
                    }
                }
                /// <summary>
                /// Get/set the offset on the vertical axis 
                /// </summary>
                /// <value></value>
                public double VerticalOffset
                {
                    get 
                    {
                        return (double)this[OFFSETY].Parameter;
                    }
                    set 
                    {
                        this[OFFSETY].Parameter = value;
                        UseMatrix = false;
                    }
                }
                /// <summary>
                /// Get/set the scaling on the horizontal axis 
                /// </summary>
                /// <value></value>
                public double HorizontalScaling
                {
                    get 
                    {
                        return (double)this[SCALINGX].Parameter;
                    }
                    set 
                    {
                        if (value <= 0)
                        {
                            throw new ArgumentOutOfRangeException("HorizontalScaling", "Value to be set must be strictly positive");
                        }
                        this[SCALINGX].Parameter = value;
                        UseMatrix = false;
                    }
                }
                /// <summary>
                /// Get/set the scaling on the vertical axis 
                /// </summary>
                /// <value></value>
                public double VerticalScaling
                {
                    get 
                    {
                        return (double)this[SCALINGY].Parameter;
                    }
                    set 
                    {
                        if (value <= 0)
                        {
                            throw new ArgumentOutOfRangeException("VerticalScaling", "Value to be set must be strictly positive");
                        }
                        this[SCALINGY].Parameter = value;
                        UseMatrix = false;
                    }
                }
                /// <summary>
                /// Get/set the Matrix for the transform
                /// </summary>
                /// <value></value>
                [TypeConverterAttribute(typeof(Matrix2DConverter))]
                public Matrix2D Matrix
                {
                    get 
                    {
                        return (Matrix2D)this[MATRIX].Parameter;
                    }
                    set 
                    {
                        this[MATRIX].Parameter = value;
                        UseMatrix = true;
                    }
                }
                /// <summary>
                /// Get/set the feature to use (true will use the Matrix, false will use the helper units)
                /// </summary>
                /// <value></value>
                public bool UseMatrix
                {
                    get 
                    {
                        return (bool)this[USEMATRIX].Parameter;
                    }
                    set 
                    {
                        this[USEMATRIX].Parameter = value;
                    }
                }
                /// <summary>
                /// Get/set the flag to resize the output image.
                /// </summary>
                /// <value></value>
                public bool ResizeOutputImage
                {
                    get 
                    {
                        return (bool)this[RESIZEOUTPUTIMAGE].Parameter;
                    }
                    set 
                    {
                        this[RESIZEOUTPUTIMAGE].Parameter = value;
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
                        return "Translate and/or Scale and/or rotate and/or flip an image.";
                    }
                }

            #endregion Public Properties
        #endregion Properties
    
        #region Constructors
            /// <summary>
            /// Instanciate a new SpatialTransformFilter object
            /// </summary>
            public SpatialTransformFilter()
            {
                FilterParameter rotation = new FilterParameter(ROTATION, "Rotate the image", (double)0.0);
                FilterParameter flipx = new FilterParameter(FLIPX, "Flip the image horizontally?", (bool)false);
                FilterParameter flipy = new FilterParameter(FLIPY, "Flip the image veritcally?", (bool)false);
                FilterParameter offsetx = new FilterParameter(OFFSETX, "shift the image on the X axis", (double)0.0);
                FilterParameter offsety = new FilterParameter(OFFSETY, "shift the image on the Y axis", (double)0.0);
                FilterParameter scalingx = new FilterParameter(SCALINGX, "Scale the the image on the X axis", (double)1.0);
                FilterParameter scalingy = new FilterParameter(SCALINGY, "Scale the the image on the X axis", (double)1.0);
                FilterParameter matrix = new FilterParameter(MATRIX, "Custom matrix used for the transform", new Matrix2D());
                FilterParameter useMatrix = new FilterParameter(USEMATRIX, "Direct to use the matrix or the helper units", (bool)true);
                FilterParameter ResizeOutputImage = new FilterParameter(RESIZEOUTPUTIMAGE, "Resize the returned  IImageAdapter to fit the output image", (bool)true);

                AddParameter(rotation);
                AddParameter(flipx);
                AddParameter(flipy);
                AddParameter(offsetx);
                AddParameter(offsety);
                AddParameter(scalingx);
                AddParameter(scalingy);
                AddParameter(matrix);
                AddParameter(useMatrix);
                AddParameter(ResizeOutputImage);
            }
        #endregion Constructors

        #region Methods
            /// <summary>
            /// Filter implementation
            /// </summary>
            protected override IImageAdapter ProcessFilter(IImageAdapter source)
            {
                IImageAdapter retVal = null;
                Image2DTransforms transform = new Image2DTransforms(source);
                transform.ResizeToFitOutputImage = ResizeOutputImage;
                if (UseMatrix == false)
                {
                    if (Rotation % 360.0 != 0.0)
                    {
                        transform.RotateTransform(Rotation, AngleUnit.Degree);
                    }

                    transform.TranslateTransform(HorizontalOffset, VerticalOffset);
                    transform.ScaleTransform(HorizontalScaling, VerticalScaling);
                    // Note : Cannot use Matrix property as it will reset the "UseMatrix" field
                    this[MATRIX].Parameter = transform.ConvertTransformsToMatrix();
                }

                transform.Transform(Matrix);
                retVal = transform.ImageTransformed;

                if (VerticalFlip || HorizontalFlip)
                {
                    int x = 0;
                    int y = 0;
                    IImageAdapter imageTransformed = retVal;
                    int width = (int)imageTransformed.Width;
                    int height = (int)imageTransformed.Height;
                    if (UseMatrix == false)
                    {
                        if (HorizontalFlip) { y = height - 1; }
                        if (VerticalFlip) { x = width - 1; }
                    }

                    retVal = new ImageAdapter (imageTransformed.Width, imageTransformed.Height);
                    for (int j = 0; j < height; j++)
                    {
                        for (int i = 0; i < width; i++)
                        {
                            retVal[(int)Math.Abs (x - i), (int)Math.Abs (y - j)] = imageTransformed[i, j];
                        }
                    }
                }
                return retVal;
            }
        #endregion Methods    
    }
}
