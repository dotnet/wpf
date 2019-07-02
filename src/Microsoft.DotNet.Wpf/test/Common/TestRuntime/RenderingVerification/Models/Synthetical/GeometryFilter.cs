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
        using System.Runtime.Serialization;
        using Microsoft.Test.RenderingVerification;
        //using System.Runtime.Serialization.Formatters.Soap;
    #endregion using

    /// <summary>
    /// provides geometrical transforms based on a matrix
    /// </summary>
    [SerializableAttribute]
    public class GeometryFilter: Filter, ISerializable
    {
        #region Constants
            private const string MATRIX = "Matrix";
            private const string WIDTH  = "Width";
            private const string HEIGHT = "Height";
            private const string LPFSUBSAMPLING="LowPassFilteringOnSubSampling";
        #endregion Constants

        #region Properties
            #region private properties
                private ArrayList _transformList = new ArrayList();
            #endregion private properties
            #region Public Properties
                /// <summary>
                /// The matrix of the transform 
                /// </summary>
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
                    }
                }
                /// <summary>
                /// The Width of the transformed image
                /// </summary>
                public int Width
                {
                    get
                    {
                        return (int)this[WIDTH].Parameter;
                    }
                    set
                    {
                        if (value < 0)
                        {
                            throw new ArgumentOutOfRangeException("Width", "Value to be set must be positive (or zero)");
                        }
                        this[WIDTH].Parameter = value;
                    }
                }
                /// <summary>
                /// The Height of the transformed image
                /// </summary>
                public int Height
                {
                    get
                    {
                        return (int)this[HEIGHT].Parameter;
                    }
                    set
                    {
                        if (value < 0)
                        {
                            throw new ArgumentOutOfRangeException("Width", "Value to be set must be positive (or zero)");
                        }
                        this[HEIGHT].Parameter = value;
                    }
                }
                /// <summary>
                /// The Width of the transformed image
                /// </summary>
                public bool LowPassFilterOnSubSampling
                {
                    get
                    {
                        return (bool)this[LPFSUBSAMPLING].Parameter;
                    }
                    set
                    {
                        this[LPFSUBSAMPLING].Parameter = value;
                    }
                }
                /// <summary>
                /// The elementary transforms
                /// </summary>
                public ArrayList Transforms
                {
                    get
                    {
                        return _transformList;
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
                        return "use a convertion matrix on an image (rotation/scaling/offset)";
                    }
                }

            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Affine Filter constructor
            /// </summary>
            public GeometryFilter() : base()
            {
                FilterParameter matrix = new FilterParameter(MATRIX, "Transform matrix", new Matrix2D());
                FilterParameter width  = new FilterParameter(WIDTH,  "Result width",    (int) 0);
                FilterParameter height = new FilterParameter(HEIGHT, "Result Height",    (int) 0);
                FilterParameter smoothSubSampling = new FilterParameter(LPFSUBSAMPLING, "Low Pass filter the image before sub-sampling (Width and Height reduction)", true);

                AddParameter(matrix);
                AddParameter(width);
                AddParameter(height);
                AddParameter(smoothSubSampling);
            }
            /// <summary>
            /// Serialization Constructor
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected GeometryFilter(SerializationInfo info, StreamingContext context) : this()
            {
                Matrix = (Matrix2D)info.GetValue("Matrix2D", typeof(Matrix2D));
                Width = (int)info.GetValue("Width", typeof(int));
                Height = (int)info.GetValue("Height", typeof(int));
                LowPassFilterOnSubSampling = (bool)info.GetValue("LowPassFilterOnSubSampling", typeof(bool));
            }
        #endregion Constructors

        #region Methods
            #region Protected Method (ProcessFilter)
                /// <summary>
                /// filter implementation
                /// </summary>
                protected override IImageAdapter ProcessFilter(IImageAdapter source)
                {
                    // Check Params
                    if (source == null)
                    {
                        throw new ArgumentNullException("source", "Argument cannot be null");
                    }
                    if (Width == 0)
                    {
                        Width  = source.Width;
                    }
                    if (Height == 0)
                    {
                        Height = source.Height;
                    }
            
                    if((Height < source.Height || Width < source.Width) && (LowPassFilterOnSubSampling == true)) 
                    {
                        GaussianFilter gf = new GaussianFilter();
                        int ratio = (int)Math.Max(source.Width/Width,source.Height/Height);
                        if (ratio < 1) 
                        {
                            ratio = 2;
                        }
                        gf.Length = ratio;
                        source = gf.Process(source);
                        Console.WriteLine("<<Proceed with GF subsampling>>");
                    }
/*
                    //BUGBUG
                    //ImageAdapter iaret = DirectInterpolator.Transform(source,Matrix,Width,Height);
                    DirectInterpolator dirint = new DirectInterpolator();
                    return dirint.Transform(source, Matrix);
*/
                    Image2DTransforms transform = new Image2DTransforms(source);
                    transform.Transform(Matrix);
                    return transform.ImageTransformed;
                }
            #endregion Protected Method (ProcessFilter)
            #region Public methods
                /// <summary>
                /// Add a Translate to the list of transforms to Perform
                /// </summary>
                /// <param name="xAxis">Translation on the x Axis</param>
                /// <param name="yAxis">Translation on the y Axis</param>
                public void TranslateTransform(double xAxis, double yAxis)
                {
                    _transformList.Add(new DrawingTransform(TranformType.Translate, xAxis, yAxis));
                    ApplyTransforms();
                }
                /// <summary>
                /// Add a Scaling factor to the list of transforms to Perform
                /// </summary>
                /// <param name="xAxis">scaling factor on the x axis </param>
                /// <param name="yAxis">scaling factor on the y Axis</param>
                public void ScaleTransform(double xAxis, double yAxis)
                {
                    _transformList.Add(new DrawingTransform(TranformType.Scale, xAxis, yAxis));
                    ApplyTransforms();
                }
                /// <summary>
                /// Add a Scaling factor to the list of transforms to Perform
                /// </summary>
                /// <param name="scale">scaling factor (applied to both axis)</param>
                public void ScaleTransform(double scale)
                {
                    _transformList.Add(new DrawingTransform(TranformType.Scale, scale, scale));
                    ApplyTransforms();
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
                        angleValue *= Math.PI / 180;
                    }
                    else
                    {
                        if (radianDegree != AngleUnit.Radian)
                        {
                            throw new Exception("Unsupported angle type");
                        }
                    }

                    _transformList.Add(new DrawingTransform(TranformType.Rotate, angleValue));
                    ApplyTransforms();
                }
                /// <summary>
                /// Perform all Transformation from the list
                /// </summary>
                /// <returns>The resulting matrix after all the transform</returns>
                public double[] ApplyTransforms()
                {
//                    double[] matrix = new double[] { 1f, 0f, 0f, 1f, 0f, 0f };
                    Matrix2D matrix = new Matrix2D();

                    foreach (DrawingTransform transform in _transformList)
                    {
                        switch (transform.TransformType)
                        {
                            case TranformType.Translate :
                                matrix.T1 += transform.Data[0];
                                matrix.T2 += transform.Data[1];
//                                matrix[4] += transform.Data[0];
//                                matrix[5] += transform.Data[1];
                                break;

                            case TranformType.Scale :
                                    matrix.X1 *= transform.Data[0];
                                    matrix.Y1 *= transform.Data[0];
                                    matrix.T1 *= transform.Data[0];
                                    matrix.X2 *= transform.Data[1];
                                    matrix.Y2 *= transform.Data[1];
                                    matrix.T2 *= transform.Data[1];
//                                matrix[0] *= transform.Data[0];
//                                matrix[1] *= transform.Data[0];
//                                matrix[4] *= transform.Data[1];
//                                matrix[2] *= transform.Data[1];
//                                matrix[3] *= transform.Data[1];
//                                matrix[5] *= transform.Data[0];
                                break;

                            case TranformType.Rotate :
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

//                                double[] temp = (double[])matrix.Clone();
//
//                                matrix[0] = temp[0] * cos - temp[2] * sin;
//                                matrix[1] = temp[1] * cos - temp[3] * sin;
//                                matrix[4] = temp[4] * cos - temp[5] * sin;
//                                matrix[2] = temp[0] * sin + temp[2] * cos;
//                                matrix[3] = temp[1] * sin + temp[3] * cos;
//                                matrix[5] = temp[4] * sin + temp[5] * cos;
                                break;

                            default :
                                throw new Exception("Unsupported transform type passed in");
                        }
                    }

                    Matrix = matrix;
                    return matrix;
                }
            #endregion Public Methods
        #endregion Methods

        #region ISerializable Members
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("Matrix2D", Matrix);
                info.AddValue("Width",Width);
                info.AddValue("Height",Height);
                info.AddValue("LowPassFilterOnSubSampling",LowPassFilterOnSubSampling);
            }
        #endregion
    }
}
