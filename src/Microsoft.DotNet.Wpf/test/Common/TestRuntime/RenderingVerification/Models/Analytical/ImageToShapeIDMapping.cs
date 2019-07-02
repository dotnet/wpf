// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification.Model.Analytical
{

    #region using
        using System;
        using System.Drawing;
    #endregion using

    /// <summary>
    /// Summary description for ImageToShapeIDMapping.
    /// </summary>
    internal class ImageToShapeIDMapping
    {
        #region Properties
            #region Private Properties
                private IImageAdapter _imageSource = null;
                private int[,] _shapeID = null;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Return the image associated with ShapeID mapping
                /// </summary>
                /// <value></value>
                public IImageAdapter ImageSource
                {
                    get 
                    {
                        return _imageSource;
                    }
                    set 
                    {
                        if (value == null)
                        {
                            throw new ArgumentNullException("ImageSource", "value must be a valid instance of ImageAdapter");
                        }
                        if (value.Width <= 0 || value.Height <= 0)
                        {
                            throw new ArgumentOutOfRangeException("ImageSource");
                        }
                        _imageSource = value;
                        _shapeID = new int[value.Height, value.Width];
                    }
                }
                /// <summary>
                /// Get the ShapeID mapping
                /// </summary>
                /// <value></value>
                public int[,] ShapeID
                {
                    get { return _shapeID; }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            internal ImageToShapeIDMapping()
            {
                // Block instanciation 
            }
            /// <summary>
            /// Create a ImageToShapeIDMapping based on an IImageAdapter
            /// </summary>
            /// <param name="imageAdapter">The IImageAdapter to use</param>
            public ImageToShapeIDMapping(IImageAdapter imageAdapter)
            {
                ImageSource = imageAdapter;
            }
            /// <summary>
            /// Create a ImageToShapeIDMapping based on a Bitmap
            /// </summary>
            /// <param name="imageSource">The Bitmap to use</param>
            public ImageToShapeIDMapping(Bitmap imageSource)
            {
                ImageSource = new ImageAdapter(imageSource);
            }
            /// <summary>
            /// Create a ImageToShapeIDMapping based on an ImageUtility
            /// </summary>
            /// <param name="imageUtility">The ImageUtility to use</param>
            public ImageToShapeIDMapping(ImageUtility imageUtility) 
            {
                ImageSource = new ImageAdapter(imageUtility.Bitmap32Bits);
            }
            /// <summary>
            /// Create a ImageToShapeIDMapping based on a serialized image
            /// </summary>
            /// <param name="imageFileName">The file to use</param>
            public ImageToShapeIDMapping(string imageFileName)
            {
                ImageSource = new ImageAdapter(imageFileName);
            }
        #endregion Constructors
    }
}
