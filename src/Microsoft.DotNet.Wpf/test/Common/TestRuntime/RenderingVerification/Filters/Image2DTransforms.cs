// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.IO;
        using System.Xml;
        using System.Drawing;
        using System.Collections;
        using Microsoft.Test.RenderingVerification;
    using System.Globalization;
    #endregion using

    /// <summary>
    /// Summary description for Image2DTransforms.
    /// </summary>
    public class Image2DTransforms
    {
        #region Constants
            private const double EPSILON = 1e-14;
        #endregion Constants

        #region Delegate 
            /// <summary>
            /// Used to defined what kind of interpolation to use when applying a transform (BiBubic / BiLinear)
            /// </summary>
            /// <param name="x">position on the x Axis</param>
            /// <param name="y">position on the y Axis</param>
            /// <param name="imageAdapter">The image to perform interpolation on</param>
            /// <param name="unassignedColor">The color to use for pixel that are unassigned\nCan be set to null (Empty color will be used)</param>
            /// <returns></returns>
            internal delegate IColor InterpolationMethod(double x, double y, IImageAdapter imageAdapter, IColor unassignedColor);
        #endregion Delegate

        #region Properties
            #region Private Properties
                private InterpolationMethod _interpolationMethod = null;
                private Matrix2D _transfCoeffMatrix = null;
                private ArrayList _transformList = null;
                private string _fileName =null;
                private  IImageAdapter _imageSource = null;
                private IImageAdapter _imageTransformed = null;
                private bool _resizeToFitOutputImage = true;
                private IColor _unassignedColor = null;
            #endregion Private Properties
            #region Internal properties
                internal InterpolationMethod ActiveInterpolationMethod
                {
                    get 
                    {
                        return _interpolationMethod;
                    }
                    set 
                    {
                        if (value == null)
                        {
                            throw new ArgumentNullException("ActiveInterpolationMethod", "Cannot set the property to a null value");
                        }
                        _interpolationMethod = value;
                    }
                }
            #endregion Internal properties
            #region Public Properties (and Getter/Setter Properties)
                /// <summary>
                /// Get/Set the image to be used as source (the image to transform)
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
                        if (value == null) { throw new ArgumentNullException("ImageSource", "Cannot pass a null value as image source"); }
                        if (value.Width <= 0 || value.Height <= 0) { throw new ArgumentException("Neither Width nor Height of image source passed in can be zero."); }
                        _imageSource = value;
                    }
                }
                /// <summary>
                /// Contains the transposed image
                /// </summary>
                public IImageAdapter ImageTransformed
                {
                    get 
                    {
                        if (_imageTransformed == null && _imageSource != null) 
                        {
                            _imageTransformed = (IImageAdapter)_imageSource.Clone();
                        }
                        return _imageTransformed;
                    }
                }
                /// <summary>
                /// Note : Matrix should be populated as : x1, y1, x2, y2, t1, t2
                /// </summary>
                public Matrix2D Matrix
                {
                    get
                    {
                        return _transfCoeffMatrix;
                    }
                    set
                    {
                        if (value == null)
                        {
                            _transfCoeffMatrix = new Matrix2D (); // Identity Matrix
                        }
                        _transfCoeffMatrix = value;
                    }
                }
                /// <summary>
                /// File name of the transform file to save / load
                /// </summary>
                public string FileName
                {
                    get
                    {
                        return _fileName;
                    }
                    set
                    {
                        _fileName = value;
                    }
                }
                /// <summary>
                /// Stretch/shrink the output image to fit the transformed image
                /// </summary>
                public bool ResizeToFitOutputImage
                {
                    get 
                    {
                        return _resizeToFitOutputImage;
                    }
                    set 
                    {
                        _resizeToFitOutputImage = value;
                    }
                }
                /// <summary>
                /// Color to use for pixel unassigned (for instance after a rotation occurs)
                /// </summary>
                /// <value></value>
                public IColor UnassignedColor
                {
                    get 
                    {
                        return _unassignedColor;
                    }
                    set 
                    {
                        if (value == null)
                        {
                            _unassignedColor = ColorByte.Empty;
                        }
                        else 
                        {
                            _unassignedColor = value;
                        }
                    }
                }
            #endregion Public Properties (Get / Set)
        #endregion Properties

        #region Constructor
            /// <summary>
            /// Create a new instance of the Image2DTransform
            /// </summary>
            internal Image2DTransforms()
            {
                _transfCoeffMatrix = new Matrix2D();
                _transformList = new ArrayList();
                _unassignedColor = ColorByte.Empty;
                _interpolationMethod = new InterpolationMethod(BilinearInterpolator.ProcessPoint);
            }
            /// <summary>
            /// Create a new instance of the Image2DTransform, use the BilinearInterpoloation
            /// </summary>
            /// <param name="source">The imageAdapter to process</param>
            public Image2DTransforms(IImageAdapter source) : this(source, new InterpolationMethod(BilinearInterpolator.ProcessPoint))
            {
            }
            /// <summary>
            /// Create a new instance of the Image2DTransform using the specify Interpolator
            /// </summary>
            /// <param name="source">The imageAdapter to process</param>
            /// <param name="interpolationMethod">A delegate to an interpolation Method</param>
            internal Image2DTransforms(IImageAdapter source, InterpolationMethod interpolationMethod) : this()
            {
                _imageSource = source;
                _interpolationMethod = interpolationMethod;
            }
        #endregion Constructor
 
        #region Methods
            #region Private Methods
                private void ConvertCoordinates(Matrix2D matrix, int x, int y, out double ix, out double iy)
                {
                    double Determinant = matrix.Determinant;
                    double Di = matrix.X2 * matrix.T1 - matrix.X1 * matrix.T2;

                    // Since Avalon is vector based, we need to convert GDI coordinate into vector coordinate (thus the +.5)
                    iy = (matrix.X1 * (y + .5) - matrix.X2 * (x + .5) + Di) / Determinant;
                    ix = 0.0;
                    // TODO : Check if EPSILON make sense or should be replaced by 0
                    if (matrix.X1 > EPSILON || matrix.X1 < -EPSILON)
                    {
                        ix = ((x + .5) - matrix.Y1 * iy - matrix.T1) / matrix.X1;
                    }
                    else
                    {
                        ix = ((y + .5) - matrix.Y2 * iy - matrix.T2) / matrix.X2;
                    }
                    // Now revert to GDI coordinate for the interpolation to work as expected
                    iy -= 0.5 * (matrix.X2 - matrix.X1) / Determinant;
                    if (matrix.X1 > EPSILON || matrix.X1 < -EPSILON)
                    {
                        ix -= 0.5 / matrix.X1;
                    }
                    else
                    {
                        ix -= 0.5 / matrix.X2;
                    }
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Add a Translate to the list of transforms to Perform
                /// </summary>
                /// <param name="xAxis">Translation on the x Axis</param>
                /// <param name="yAxis">Translation on the y Axis</param>
                public void TranslateTransform(double xAxis, double yAxis)
                {
                    _transformList.Add(new DrawingTransform(TranformType.Translate, xAxis, yAxis)); 
                }
                /// <summary>
                /// Add a Scaling factor to the list of transforms to Perform
                /// </summary>
                /// <param name="xAxis">scaling factor on the x axis </param>
                /// <param name="yAxis">scaling factor on the y Axis</param>
                public void ScaleTransform(double xAxis, double yAxis) 
                {
                    _transformList.Add(new DrawingTransform(TranformType.Scale, xAxis, yAxis)); 
                }
                /// <summary>
                /// Add a Rotation to the list of transforms to Perform
                /// </summary>
                /// <param name="angleValue">The angle of the rotation</param>
                /// <param name="radianDegree">Specify the unit ofd the rotation (degree/radian)</param>
                public void RotateTransform(double angleValue, AngleUnit radianDegree)
                {
                    if (radianDegree == AngleUnit.Degree)
                    {
                        // convert to Radial angle
                        angleValue *= Math.PI / 180.0;
                    }
                    else
                    {
                        if (radianDegree != AngleUnit.Radian)
                        {
                            throw new RenderingVerificationException("Unsupported angle type");
                        }
                    }
                    _transformList.Add(new DrawingTransform(TranformType.Rotate, angleValue)); 
                }
                /// <summary>
                /// Map the transform into a Matrix describing this transformation
                /// </summary>
                /// <returns>(double[])The generated Matrix (x1,y1,x2,y2,offset1,offset2) </returns>
                public Matrix2D ConvertTransformsToMatrix()
                {
                    Matrix2D matrix = new Matrix2D();
                    foreach (DrawingTransform transform in _transformList)
                    {
                        switch (transform.TransformType)
                        {
                            case TranformType.Translate :
                                matrix.T1 += transform.Data[0];
                                matrix.T2 += transform.Data[1];
                                break;
                            case TranformType.Scale :
                                matrix.X1 *= transform.Data[0];
                                matrix.Y1 *= transform.Data[0];
                                matrix.T1 *= transform.Data[0];
                                matrix.X2 *= transform.Data[1];
                                matrix.Y2 *= transform.Data[1];
                                matrix.T2 *= transform.Data[1];
                                break;
                            case TranformType.Rotate:
                                double angle = transform.Data[0];
                                double sin = Math.Sin(angle);
                                double cos = Math.Cos(angle);
                                Matrix2D temp = (Matrix2D)matrix.Clone();
                                matrix.X1 = temp.X1 * cos + temp.X2 * sin;
                                matrix.X2 = temp.Y1 * cos + temp.Y2 * sin;
                                matrix.T1 = temp.T1 * cos + temp.T2 * sin;
                                matrix.Y1 = -temp.X1 * sin + temp.X2 * cos;
                                matrix.Y2 = -temp.Y1 * sin + temp.Y2 * cos;
                                matrix.T2 = -temp.T1 * sin + temp.T2 * cos;
                                break;
                            default :
                                throw new RenderingVerificationException("Unsupported transform type passed in");
                        }
                    }
                    return matrix;
                }
                /// <summary>
                /// Perform all Transformation from the list
                /// </summary>
                /// <returns>The resulting matrix after all the transform</returns> 
                public void ApplyTransforms()
                {
                    Matrix = ConvertTransformsToMatrix();
                    Transform(Matrix);
                }
                /// <summary>
                /// Apply the transform using the Public Matrix as input
                /// </summary>
                public void Transform()
                {
                    Transform(_transfCoeffMatrix);
                }
                /// <summary>
                /// Apply the transform using the supplied matrix
                /// </summary>
                /// <param name="matrix">the matrix to be applied (format if x1,y1,x2,y2,t1,t2)</param>
                public void Transform(Matrix2D matrix)
                {
                    if (_imageSource == null) { throw new RenderingVerificationException("ImageSource not Assigned, cannot transform a unknow image"); }
                    if (matrix == null) { throw new ArgumentException("Invalid argument passed in, must be a valid instance (not set to null) of Matrix2D or an array of double of length " + Matrix2D.Length); }
                    if (matrix.IsInvertible == false) { throw new Matrix2DException("Matrix passed in is not inversible, cannot transform using this matrix (matrix passed in : )" + matrix.ToString()); }
                    if (matrix.IsIdentity)
                    {
                        _imageTransformed = (IImageAdapter)_imageSource.Clone();
                        return;
                    }

                    // Create the IImageAdapter to return
                    int width = _imageSource.Width;
                    int height = _imageSource.Height;
                    if (_resizeToFitOutputImage)
                    {
                        double x = 0.0;
                        double y = 0.0;
                        Point topLeft = new Point((int)(x + Math.Sign(x) * 0.5), (int)(y + Math.Sign(y) * 0.5));
                        x = matrix.X1 * width;
                        y = matrix.X2 * width;
                        Point topRight = new Point((int)(x + Math.Sign(x) * 0.5), (int)(y + Math.Sign(y) * 0.5));
                        x = matrix.X1 * width + matrix.Y1 * height;
                        y = matrix.X2 * width + matrix.Y2 * height;
                        Point bottomRight = new Point((int)(x + Math.Sign(x) * 0.5), (int)(y + Math.Sign(y) * 0.5));
                        x = matrix.Y1 * height;
                        y = matrix.Y2 * height;
                        Point bottomLeft = new Point((int)(x + Math.Sign(x) * 0.5), (int)(y + Math.Sign(y) * 0.5));
                        width = (int)Math.Max(Math.Abs(bottomRight.X - topLeft.X), Math.Abs(topRight.X - bottomLeft.X));
                        height = (int)Math.Max(Math.Abs(bottomRight.Y - topLeft.Y), Math.Abs(topRight.Y - bottomLeft.Y));
                    }
                    _imageTransformed = new ImageAdapter(width, height, _unassignedColor);

                    // Scale down imnage if needed (average pixels)    
                    // BUGBUG : Hardcoded to BilinearInterpolator, use a delegate instead.
                    double horizontalScaling = double.NaN;
                    if(matrix.X1 != 0)
                    {
                        horizontalScaling = Math.Sqrt(Math.Abs( matrix.Determinant * matrix.Y2 / matrix.X1) );
                    }
                    else
                    {
                        horizontalScaling = Math.Sqrt(Math.Abs(matrix.Determinant * matrix.Y1 / matrix.X2));
                    }
                    double verticalScaling = Math.Sqrt(matrix.Determinant / horizontalScaling);

                    IImageAdapter scaledImage = BilinearInterpolator.ScaleDown(horizontalScaling, verticalScaling, _imageSource);

                    // Interpolate image (if scaling up and/or rotation applied to image)
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            double ix = 0.0;
                            double iy = 0.0;
                            ConvertCoordinates(matrix, x, y, out ix, out iy);
                            _imageTransformed[x, y] = _interpolationMethod(ix, iy, scaledImage, _unassignedColor);
                        }
                    }
                }
                /// <summary>
                /// Load a transfrom from an xml file (using the FileName) to the public Matrix
                /// </summary>
                public void LoadTransform()
                {
                    bool success = false;
                    XmlTextReader xmlr = null;
                    try
                    {
                        if (FileName != null)
                        {
                            xmlr = new XmlTextReader(FileName);
                            Matrix2D lcoeff = new Matrix2D();
                            while (xmlr.Read())
                            {
                                if (xmlr.Name == "Transform")
                                {
                                    if (xmlr["type"] == "2D")
                                    {
                                        lcoeff[0] = float.Parse(xmlr["c0"], NumberFormatInfo.InvariantInfo);
                                        lcoeff[1] = float.Parse(xmlr["c1"], NumberFormatInfo.InvariantInfo);
                                        lcoeff[2] = float.Parse(xmlr["c2"], NumberFormatInfo.InvariantInfo);
                                        lcoeff[3] = float.Parse(xmlr["c3"], NumberFormatInfo.InvariantInfo);
                                        lcoeff[4] = float.Parse(xmlr["c4"], NumberFormatInfo.InvariantInfo);
                                        lcoeff[5] = float.Parse(xmlr["c5"], NumberFormatInfo.InvariantInfo);
                                        Matrix = lcoeff;
                                        success = true;
                                    }
                                }
                            }
                        }
                    } 
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    finally
                    {
                        if (xmlr != null)
                        {
                            xmlr.Close();
                        }
                        if (!success)
                        {
                        //throw new RenderingVerificationException("Could not read/parse Transform");
                        }
                    }        
                }
                /// <summary>
                /// Write the Transform contained into the Matrix onto a xml file (using the FileName)
                /// </summary>
                public void WriteTransform()
                {
                    StreamWriter strw = null;
                    try
                    {
                        if (FileName != null)
                        {
                            strw = new StreamWriter(FileName);
                            string str = "<Transform type=\"2D\" ";
                            for (int j =0;j < Matrix2D.Length; j++)
                            {
                                str += " c" + j + "=\"" + Matrix[j] + "\"";
                            }
                            str += " />";

                            strw.WriteLine(str);
                            strw.Flush();
                            strw.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    finally
                    {
                        if (strw != null)
                        {
                            strw.Close();
                        }
                    }
                }
            #endregion Public Methods
        #endregion Methods
    }


    /// <summary>
    /// The unit for the angle passed in (degree / radian)
    /// </summary>
    public enum AngleUnit
    {
        /// <summary>
        /// Pass this enum value for an angle in degree
        /// </summary>
        Degree = 1,
        /// <summary>
        /// Pass this enum value for an angle in radian.
        /// </summary>
        Radian = 2
    }

    #region internal stuff (used for Transform by "GDI+ like" APIs)
        internal enum TranformType 
        {
            None = 0,
            Translate,
            Scale,
            Rotate
        }
        internal class DrawingTransform
        {
            internal TranformType TransformType = TranformType.None;
            internal double[] Data = null;
            internal DrawingTransform(TranformType transformType, params double[] parameters)
            {
                TransformType = transformType;
                Data = parameters;
            }
        }
    #endregion internal class (used for Transform by "GDI+ like" APIs)

}
