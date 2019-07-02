// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region usings
        using System;
        using System.Runtime.Serialization;
    #endregion usings

    /// <summary>
    /// Exception thrown by the Matrix2D class
    /// </summary>
    public class Matrix2DException: Exception
    {
        /// <summary>
        /// Instantiate a new Matrix2DException Exception
        /// </summary>
        public Matrix2DException() : base() { }
        /// <summary>
        /// Instantiate a new Matrix2DException Exception with a message
        /// </summary>
        /// <param name="message">The message to be displayed to the user</param>
        public Matrix2DException(string message) : base(message) { }
        /// <summary>
        /// Instantiate a new Matrix2DException Exception with a message and an inner Exception
        /// </summary>
        /// <param name="message">The message to be displayed to the user</param>
        /// <param name="innerException">The nested exception causing this to happen</param>
        public Matrix2DException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Class containing the information for a 2 dimensional matrix
    /// </summary>
    [SerializableAttribute()]
    public class Matrix2D : ICloneable, ISerializable
    {
        #region Constants
            private const int MATRIX_LENGTH = 6;
        #endregion Constants

        #region Properties
            #region Private Properties
                private double[] _matrix = null;    // Identity matrix
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Retrieve the length of the Matrix
                /// </summary>
                /// <value></value>
                public static int Length
                {
                    get 
                    {
                        return MATRIX_LENGTH;
                    }
                }
                /// <summary>
                /// The identity matrix
                /// </summary>
                public static readonly Matrix2D Identity = null;                
                /// <summary>
                /// The first X value
                /// </summary>
                /// <value></value>
                public double X1
                {
                    get { return _matrix[0]; }
                    set { _matrix[0] = value; }
                }
                /// <summary>
                /// The first Y value
                /// </summary>
                /// <value></value>
                public double Y1
                {
                    get { return _matrix[1]; }
                    set { _matrix[1] = value; }
                }
                /// <summary>
                /// The second X value
                /// </summary>
                /// <value></value>
                public double X2
                {
                    get { return _matrix[2]; }
                    set { _matrix[2] = value; }
                }
                /// <summary>
                /// The second Y value
                /// </summary>
                /// <value></value>
                public double Y2
                {
                    get { return _matrix[3]; }
                    set { _matrix[3] = value; }
                }
                /// <summary>
                /// The first translation value (offset)
                /// </summary>
                /// <value></value>
                public double T1
                {
                    get { return _matrix[4]; }
                    set { _matrix[4] = value; }
                }
                /// <summary>
                /// The second translation value (offset)
                /// </summary>
                /// <value></value>
                public double T2
                {
                    get { return _matrix[5]; }
                    set { _matrix[5] = value; }
                }
                /// <summary>
                /// Return the determinant for the current Matrix 
                /// </summary>
                /// <value></value>
                public double Determinant
                {
                    get 
                    {
                        return _matrix[0] * _matrix[3] - _matrix[1] * _matrix[2];
                    }
                }
                /// <summary>
                /// Return if a matrix can be inversed (matrix * inverse = Identity)
                /// </summary>
                /// <value></value>
                public bool IsInvertible
                {
                    get
                    {
                        // if determinant is zero, matrix is not inversible
                        return (Determinant != 0);
                    }
                }
                /// <summary>
                /// Retreive if the current Matrix is the identity matrix
                /// </summary>
                /// <value></value>
                public bool IsIdentity
                {
                    get 
                    {
                        return (X1 == 1.0 && Y1 == 0.0 && X2 == 0.0 && Y2 == 1.0 && T1 == 0.0 && T2 == 0.0);
                    }
                }

            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Create an instance of the Matrix2D class (Matrix set to Identity)
            /// </summary>
            public Matrix2D()
            {
                _matrix = new double[]{1,0,0,1,0,0};    // Identity Matrix
            }
            /// <summary>
            /// Create an copy of a Matrix2D class
            /// </summary>
            /// <param name="matrix2D">The Matrix2D to copy</param>
            public Matrix2D(Matrix2D matrix2D) : this()
            {
                _matrix = (double[])matrix2D._matrix.Clone();
            }
            /// <summary>
            /// Create an instance of the Matrix2D class
            /// </summary>
            /// <param name="x1">The first X value</param>
            /// <param name="y1">The first Y value</param>
            /// <param name="x2">The second X value</param>
            /// <param name="y2">The second Y value</param>
            /// <param name="t1">The first translate value (offset)</param>
            /// <param name="t2">The second translate value (offset)</param>
            public Matrix2D(double x1, double y1, double x2, double y2, double t1, double t2) : this()
            {
                CheckValidity(x1, y1, x2, y2, t1, t2);
                _matrix[0] = x1;
                _matrix[1] = y1;
                _matrix[2] = x2;
                _matrix[3] = y2;
                _matrix[4] = t1;
                _matrix[5] = t2;
            }
            /// <summary>
            /// Constructor needed for serialization
            /// </summary>
            /// <param name="info">The SerializationInfo member</param>
            /// <param name="context">The StreamingContext member</param>
            protected Matrix2D(SerializationInfo info, StreamingContext context) : this()
            {
                _matrix = (double[])info.GetValue("Matrix", typeof(double[]));
            }
            static Matrix2D()
            {
                Identity = new Matrix2D(1.0, 0.0, 0.0, 1.0, 0.0, 0.0);
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                private void CheckValidity(params double[] entries)
                {
                    for (int t = 0; t < entries.Length; t++)
                    {
                        if ( double.IsNaN(entries[t]) || double.IsInfinity(entries[t]) )
                        {
                            throw new Matrix2DException("One of the entry in the matrix is invalid; entry # " + t + " / value=" + entries[t].ToString());
                        }
                    }
                }
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Get/set a value of the underlying double[] thru Indexer access
                /// </summary>
                /// <param name="index">The value to get/set</param>
                /// <returns></returns>
                public double this[int index]
                {
                    get
                    {
                        if (index < 0 || index >= MATRIX_LENGTH )
                        {
                            throw new ArgumentOutOfRangeException("index", "Indexer / Item() index must be between 0 and " + (MATRIX_LENGTH-1).ToString() + " (both included)");
                        }
                        return _matrix[index];
                    }
                    set 
                    {
                        if (index < 0 || index >= MATRIX_LENGTH)
                        {
                            throw new ArgumentOutOfRangeException("index", "Indexer / Item() index must be between 0 and " + (MATRIX_LENGTH-1).ToString() + " (both included)");
                        }
                        _matrix[index] = value;
                    }
                }
                /// <summary>
                /// Return the inverse matrix (matrix * inverse = Identity)
                /// </summary>
                /// <returns></returns>
                public static Matrix2D InvertMatrix(Matrix2D matrix)
                {
                    Matrix2D retVal = new Matrix2D();

                    if (matrix.IsInvertible == false)
                    {
                        throw new Matrix2DException("This matrix cannot be inverted");
                    }


                    double minusDeterminant = - matrix.Determinant;

                    retVal[1] = matrix[1] / minusDeterminant;
                    retVal[2] = matrix[2] / minusDeterminant;
                    retVal[3] = -matrix[0] / minusDeterminant;
                    retVal[5] = (matrix[0] * matrix[5] - matrix[1] * matrix[4]) / minusDeterminant;
                    if (matrix[0] != 0)
                    {
                        retVal[0] = 1 / matrix[0] - matrix[1] * matrix[2] / (matrix[0] * minusDeterminant);
                        retVal[4] = -matrix[4] / matrix[0] - matrix[2] / matrix[0] * (matrix[0] * matrix[5] - matrix[1] * matrix[4]) / (minusDeterminant);
                    }
                    else 
                    {
                        retVal[0] = -matrix[3] / (matrix[1] * matrix[2]);
                        retVal[4] = -matrix[5] / matrix[1] + (matrix[3] * matrix[4]) / (matrix[1] * matrix[2]);
                    }
                    return retVal;
                }
            #endregion Public Methods
            #region Overriden methods
                /// <summary>
                /// Compare two Matrix2D object for equality
                /// </summary>
                /// <param name="obj">The Matrix2D to compare against</param>
                /// <returns>true if matrix have the same inner value, false otherwise</returns>
                public override bool Equals(object obj)
                {
                    if(obj == null)
                    {
                        throw new ArgumentNullException("obj", "The paramter passed in cannot be null");
                    }
                    if( ! (obj is Matrix2D))
                    {
                        throw new ArgumentException("The specified object does not derive from the type '" + this.GetType().ToString() + "'", "obj");
                    }
                    return this == (Matrix2D)obj;
                }
                /// <summary>
                /// Return the HashCode for this object
                /// </summary>
                /// <returns></returns>
                public override int GetHashCode()
                {
                    return base.GetHashCode ();
                }
                /// <summary>
                /// Display the object into a user friendly output
                /// </summary>
                /// <returns></returns>
                public override string ToString()
                {
                    return "{ x1 : "+X1+" ; y1 : "+Y1+" ; t1 : "+T1+"\nx2 : "+X2+" ; y2 : "+Y2+" ; t2 : "+T2+" }";
                }
            #endregion Overriden methods
        #endregion Methods

        #region Operator overload (casting / methematical operation)
            /// <summary>
            /// Implicit cast between a Matrix2D and a array of double -- FOR AVALON USE (ROW vector)
            /// </summary>
            /// <param name="matrix">The matrix to be converted into double[]</param>
            /// <returns>A array of 6 double representing the matrix</returns>
            public static implicit operator double[](Matrix2D matrix)
            {
                return new double[] { matrix.X1, matrix.X2, matrix.Y1, matrix.Y2, matrix.T1, matrix.T2 };
            }
            /// <summary>
            /// Implicit cast between an array of double and a Matrix2D -- FOR AVALON USE (ROW vector)
            /// Note: the array must contains 6 entries for the cast to be valid (x1,x2,y1,y2,t1,t2)
            /// </summary>
            /// <param name="transformMatrix">The array of double to be converted into the Matrix2D</param>
            /// <returns>The Matrix2D created from the array of doubles</returns>
            public static implicit operator Matrix2D(double[] transformMatrix)
            {
                if (transformMatrix == null)
                {
                    throw new ArgumentNullException("Cannot convert null to a Matrix2D");
                }
                if(transformMatrix.Length != 6)
                {
                    throw new Matrix2DException("The array of double to be convert must be of length 6 (x1,y1,x2,y2,t1,t2)");
                }

                return new Matrix2D(transformMatrix[0], transformMatrix[2], transformMatrix[1], transformMatrix[3], transformMatrix[4], transformMatrix[5]);
            }
            /// <summary>
            /// Implicit cast between a Matrix2D and a array of float -- FOR System.Drawing.Matrix USE (COLUMN vector)
            /// </summary>
            /// <param name="matrix">The matrix to be converted into float[]</param>
            /// <returns>A array of 6 float representing the matrix</returns>
            public static implicit operator float[](Matrix2D matrix)
            {
                return new float[] { (float)matrix.X1, (float)matrix.Y1, (float)matrix.X2, (float)matrix.Y2, (float)matrix.T1, (float)matrix.T2 };
            }
            /// <summary>
            /// Implicit cast between an array of float and a Matrix2D -- FOR System.Drawing.Matrix USE (COLUMN vector)
            /// Note: the array must contains 6 entries for the cast to be valid (x1,y1,x2,y2,t1,t2)
            /// </summary>
            /// <param name="transformMatrix">The array of float to be converted into the Matrix2D</param>
            /// <returns>The Matrix2D created from the array of floats</returns>
            public static implicit operator Matrix2D(float[] transformMatrix)
            {
                if (transformMatrix == null)
                {
                    throw new ArgumentNullException("Cannot convert null to a Matrix2D");
                }
                if (transformMatrix.Length != 6)
                {
                    throw new Matrix2DException("The array of float to be convert must be of length 6 (x1,y1,x2,y2,t1,t2)");
                }

                return new Matrix2D(transformMatrix[0], transformMatrix[1], transformMatrix[2], transformMatrix[3], transformMatrix[4], transformMatrix[5]);
            }
            /// <summary>
            /// Implicit cast between a Matrix2D and the GDI+ Matrix (System.Drawing.Drawing2D.Matrix)
            /// </summary>
            /// <param name="matrix2d">The instance of Matrix2D to be converted into the GDI+ Matrix</param>
            /// <returns>The GDI+ Matrix (System.Drawing.Drawing2D.Matrix) created from the Matrix2D</returns>
            public static implicit operator System.Drawing.Drawing2D.Matrix(Matrix2D matrix2d)
            {
                if (matrix2d == null) { throw new ArgumentNullException("Cannot convert null into a System.Drawing.Drawing2D.Matrix"); }
                return new System.Drawing.Drawing2D.Matrix((float)matrix2d.X1, (float)matrix2d.Y1, (float)matrix2d.X2, (float)matrix2d.Y2, (float)matrix2d.T1, (float)matrix2d.T2);
            }
            /// <summary>
            /// Implicit cast between  a GDI+ Matrix (System.Drawing.Drawing2D.Matrix) and a Matrix2D
            /// </summary>
            /// <param name="gdiMatrix">The instance of the GDI+ Matrix to be converted into a Matrix2D</param>
            /// <returns>The Matrix2D created from the GDI Matrix (System.Drawing.Drawing2D.Matrix)</returns>
            public static implicit operator Matrix2D(System.Drawing.Drawing2D.Matrix gdiMatrix)
            {
                if (gdiMatrix == null) { throw new ArgumentNullException("Cannot convert null into a Matrix2D"); }
                return (Matrix2D)gdiMatrix.Elements;
            }

            /// <summary>
            /// Compare two Matrix2D object for equality
            /// </summary>
            /// <param name="matrix1">The matrix to be compared</param>
            /// <param name="matrix2">The matrix to compare against</param>
            /// <returns>true if all the inner values are the same, false otherwise</returns>
            public static bool operator==(Matrix2D matrix1, Matrix2D matrix2)
            {
                if ((object)matrix1 == null || (object)matrix2 == null)
                {
                    if ((object)matrix1 == null && (object)matrix2 == null) { return true; }
                    return false;
                }
                if (matrix1.X1 == matrix2.X1 && matrix1.Y1 == matrix2.Y1 && matrix1.T1 == matrix2.T1 &&
                    matrix1.X2 == matrix2.X2  &&matrix1.Y2 == matrix2.Y2 && matrix1.T2 == matrix2.T2 )
                {
                    return true;
                }
                return false;

            }
            /// <summary>
            /// Compare two Matrix2D object for inequality
            /// </summary>
            /// <param name="matrix1">The matrix to be compared</param>
            /// <param name="matrix2">The matrix to compare against</param>
            /// <returns>false if all the inner values are the same, true otherwise</returns>
            public static bool operator!=(Matrix2D matrix1, Matrix2D matrix2)
            {
                return !(matrix1 == matrix2);
            }
            /// <summary>
            /// Add two matrix togheter
            /// </summary>
            /// <param name="matrix1">The original matrix </param>
            /// <param name="matrix2">The matrix to add</param>
            /// <returns>The resulting matrix</returns>
            public static Matrix2D operator+(Matrix2D matrix1, Matrix2D matrix2)
            {
                Matrix2D retVal = new Matrix2D();
                retVal.X1 = matrix1.X1 + matrix2.X1;
                retVal.X2 = matrix1.X2 + matrix2.X2;
                retVal.Y1 = matrix2.Y1 + matrix1.Y1;
                retVal.Y2 = matrix1.Y2 +  matrix2.Y2;
                retVal.T1 = matrix1.T1 + matrix2.T1;
                retVal.T2 = matrix1.T2 + matrix2.T2;
                return retVal;
            }
            /// <summary>
            /// Subtract a matrix from another one
            /// </summary>
            /// <param name="matrix1">The original matrix</param>
            /// <param name="matrix2">The matrix to substract</param>
            /// <returns>The resulting matrix</returns>
            public static Matrix2D operator -(Matrix2D matrix1, Matrix2D matrix2)
            {
                Matrix2D retVal = new Matrix2D();
                retVal.X1 = matrix1.X1 - matrix2.X1;
                retVal.X2 = matrix1.X2 - matrix2.X2;
                retVal.Y1 = matrix2.Y1 - matrix1.Y1;
                retVal.Y2 = matrix1.Y2 -  matrix2.Y2;
                retVal.T1 = matrix1.T1 - matrix2.T1;
                retVal.T2 = matrix1.T2 - matrix2.T2;
                return retVal;
            }
            /// <summary>
            /// Multiply a matrix by another one
            /// </summary>
            /// <param name="matrix1">The original matrix</param>
            /// <param name="matrix2">The matrix to multiply with</param>
            /// <returns>The resulting matrix</returns>
            public static Matrix2D operator*(Matrix2D matrix1, Matrix2D matrix2)
            {
                Matrix2D retVal = new Matrix2D();
                retVal.X1 = matrix1.X1 * matrix2.X1 - matrix1.Y1 * matrix2.X2;
                retVal.X2 = matrix1.X2 * matrix2.X1 - matrix1.Y2* matrix2.X2;
                retVal.Y1 = matrix1.X1 * matrix2.Y1 - matrix1.Y1 * matrix2.Y2;
                retVal.Y2 = matrix1.X2 * matrix2.Y1 - matrix1.Y2* matrix2.Y2;
                retVal.T1 = matrix1.X1 * matrix2.T1 - matrix1.Y1 * matrix2.T2;
                retVal.T2 = matrix1.X2 * matrix2.T1 - matrix1.Y2* matrix2.T2;
                return retVal;
            }
            /// <summary>
            /// Divide a matrix by another one
            /// NOT IMPLEMENTED YET, contact the avalon tool team if you need this feature
            /// </summary>
            /// <param name="matrix1">The original matrix</param>
            /// <param name="matrix2">The matrix to divide by</param>
            /// <returns>The resulting matrix</returns>
            public static Matrix2D operator /(Matrix2D matrix1, Matrix2D matrix2)
            {
                Matrix2D retVal = new Matrix2D();
                throw new NotImplementedException("Not implemented, contact the Avalon tool team if you need this feature");
            }
        #endregion Operator overload (casting / methematical operation)

        #region ICloneable Implementation
            /// <summary>
            /// Create a deep copy of the current Matrix
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                return new Matrix2D(this);
            }
        #endregion ICloneable Implementation

        #region ISerializable Members
            /// <summary>
            /// ISerializable unique Method implementation -- will be called by the formatter
            /// </summary>
            /// <param name="info">The SerializationInfo member</param>
            /// <param name="context">The StreamingContext member</param>
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("Matrix", _matrix);
            }

        #endregion
    }
}
