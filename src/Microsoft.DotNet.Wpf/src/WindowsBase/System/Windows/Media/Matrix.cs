// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Diagnostics;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using MS.Internal.WindowsBase;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Security;

// IMPORTANT
//
// Rules for using matrix types.
// 
//    internal enum MatrixTypes
//    {
//        TRANSFORM_IS_IDENTITY    = 0,
//        TRANSFORM_IS_TRANSLATION = 1,
//        TRANSFORM_IS_SCALING     = 2,
//        TRANSFORM_IS_UNKNOWN     = 4
//    }
//
// 1. Matrix type must be one of 0, 1, 2, 4, or 3 (for scale and translation)
// 2. Matrix types are true but not exact!  (E.G. A scale or identity transform could be marked as unknown or scale+translate.)
// 3. Therefore read-only operations can ignore the type with one exception
//      EXCEPTION: A matrix tagged identity might have any coefficients instead of 1,0,0,1,0,0
//                 This is the (now) classic no default constructor for structs issue
// 4. Matrix._type must be maintained by mutation operations
// 5. MS.Internal.MatrixUtil uses unsafe code to access the private members of Matrix including _type.
//
// In Jan 2005 the matrix types were changed from being EXACT (i.e. a
// scale matrix is always tagged as a scale and not something more
// general.)  This resulted in about a 2% speed up in matrix
// multiplication.
//
// The special cases for matrix multiplication speed up scale*scale
// and translation*translation by 30% compared to a single "no-branch"
// multiplication algorithm.  Matrix multiplication of two unknown
// matrices is slowed by 20% compared to the no-branch algorithm.
//
// windows/wcp/DevTest/Drts/MediaApi/MediaPerf.cs includes the
// simple test of matrix multiplication speed used for these results.

namespace System.Windows.Media
{
    ///<summary>
    /// Matrix
    ///</summary>
    public partial struct Matrix: IFormattable
    {
        // the transform is identity by default
        // Actually fill in the fields - some (internal) code uses the fields directly for perf.
        private static Matrix s_identity = CreateIdentity();

#region Constructor

        /// <summary>
        /// Creates a matrix of the form
        ///             / m11, m12, 0 \
        ///             | m21, m22, 0 |
        ///             \ offsetX, offsetY, 1 /
        /// </summary>
        public Matrix(double m11, double m12,
                      double m21, double m22,
                      double offsetX, double offsetY)
        {
            this._m11 = m11;
            this._m12 = m12;
            this._m21 = m21;
            this._m22 = m22;
            this._offsetX = offsetX;
            this._offsetY = offsetY;
            _type = MatrixTypes.TRANSFORM_IS_UNKNOWN;
            _padding = 0;

            // We will detect EXACT identity, scale, translation or
            // scale+translation and use special case algorithms.
            DeriveMatrixType();
        }

#endregion Constructor

#region Identity

        /// <summary>
        /// Identity
        /// </summary>
        public static Matrix Identity
        {
            get
            {
                return s_identity;
            }
        }

        /// <summary>
        /// Sets the matrix to identity.
        /// </summary>
        public void SetIdentity()
        {
            _type = MatrixTypes.TRANSFORM_IS_IDENTITY;
        }

        /// <summary>
        /// Tests whether or not a given transform is an identity transform
        /// </summary>
        public bool IsIdentity
        {
            get
            {
                return (_type == MatrixTypes.TRANSFORM_IS_IDENTITY ||
                        (_m11 == 1 && _m12 == 0 && _m21 == 0 && _m22 == 1 && _offsetX == 0 && _offsetY == 0));
            }
        }

#endregion Identity

#region Operators
        /// <summary>
        /// Multiplies two transformations.
        /// </summary>
        public static Matrix operator *(Matrix trans1, Matrix trans2)
        {
            MatrixUtil.MultiplyMatrix(ref trans1, ref trans2);
            trans1.Debug_CheckType();
            return trans1;
        }

        /// <summary>
        /// Multiply
        /// </summary>
        public static Matrix Multiply(Matrix trans1, Matrix trans2)
        {
            MatrixUtil.MultiplyMatrix(ref trans1, ref trans2);
            trans1.Debug_CheckType();
            return trans1;
        }

#endregion Operators

#region Combine Methods

        /// <summary>
        /// Append - "this" becomes this * matrix, the same as this *= matrix.
        /// </summary>
        /// <param name="matrix"> The Matrix to append to this Matrix </param>
        public void Append(Matrix matrix)
        {
            this *= matrix;
        }

        /// <summary>
        /// Prepend - "this" becomes matrix * this, the same as this = matrix * this.
        /// </summary>
        /// <param name="matrix"> The Matrix to prepend to this Matrix </param>
        public void Prepend(Matrix matrix)
        {
            this = matrix * this;
        }

        /// <summary>
        /// Rotates this matrix about the origin
        /// </summary>
        /// <param name='angle'>The angle to rotate specifed in degrees</param>
        public void Rotate(double angle)
        {
            angle %= 360.0; // Doing the modulo before converting to radians reduces total error
            this *= CreateRotationRadians(angle * (Math.PI/180.0));
        }

        /// <summary>
        /// Prepends a rotation about the origin to "this"
        /// </summary>
        /// <param name='angle'>The angle to rotate specifed in degrees</param>
        public void RotatePrepend(double angle)
        {
            angle %= 360.0; // Doing the modulo before converting to radians reduces total error
            this = CreateRotationRadians(angle * (Math.PI/180.0)) * this;
        }

        /// <summary>
        /// Rotates this matrix about the given point
        /// </summary>
        /// <param name='angle'>The angle to rotate specifed in degrees</param>
        /// <param name='centerX'>The centerX of rotation</param>
        /// <param name='centerY'>The centerY of rotation</param>
        public void RotateAt(double angle, double centerX, double centerY)
        {
            angle %= 360.0; // Doing the modulo before converting to radians reduces total error
            this *= CreateRotationRadians(angle * (Math.PI/180.0), centerX, centerY);
        }

        /// <summary>
        /// Prepends a rotation about the given point to "this"
        /// </summary>
        /// <param name='angle'>The angle to rotate specifed in degrees</param>
        /// <param name='centerX'>The centerX of rotation</param>
        /// <param name='centerY'>The centerY of rotation</param>
        public void RotateAtPrepend(double angle, double centerX, double centerY)
        {
            angle %= 360.0; // Doing the modulo before converting to radians reduces total error
            this = CreateRotationRadians(angle * (Math.PI/180.0), centerX, centerY) * this;
        }

        /// <summary>
        /// Scales this matrix around the origin
        /// </summary>
        /// <param name='scaleX'>The scale factor in the x dimension</param>
        /// <param name='scaleY'>The scale factor in the y dimension</param>
        public void Scale(double scaleX, double scaleY)
        {
            this *= CreateScaling(scaleX, scaleY);
        }

        /// <summary>
        /// Prepends a scale around the origin to "this"
        /// </summary>
        /// <param name='scaleX'>The scale factor in the x dimension</param>
        /// <param name='scaleY'>The scale factor in the y dimension</param>
        public void ScalePrepend(double scaleX, double scaleY)
        {
            this = CreateScaling(scaleX, scaleY) * this;
        }

        /// <summary>
        /// Scales this matrix around the center provided
        /// </summary>
        /// <param name='scaleX'>The scale factor in the x dimension</param>
        /// <param name='scaleY'>The scale factor in the y dimension</param>
        /// <param name="centerX">The centerX about which to scale</param>
        /// <param name="centerY">The centerY about which to scale</param>
        public void ScaleAt(double scaleX, double scaleY, double centerX, double centerY)
        {
            this *= CreateScaling(scaleX, scaleY, centerX, centerY);
        }

        /// <summary>
        /// Prepends a scale around the center provided to "this"
        /// </summary>
        /// <param name='scaleX'>The scale factor in the x dimension</param>
        /// <param name='scaleY'>The scale factor in the y dimension</param>
        /// <param name="centerX">The centerX about which to scale</param>
        /// <param name="centerY">The centerY about which to scale</param>
        public void ScaleAtPrepend(double scaleX, double scaleY, double centerX, double centerY)
        {
            this = CreateScaling(scaleX, scaleY, centerX, centerY) * this;
        }

        /// <summary>
        /// Skews this matrix
        /// </summary>
        /// <param name='skewX'>The skew angle in the x dimension in degrees</param>
        /// <param name='skewY'>The skew angle in the y dimension in degrees</param>
        public void Skew(double skewX, double skewY)
        {
            skewX %= 360;
            skewY %= 360;
            this *= CreateSkewRadians(skewX * (Math.PI/180.0),
                                      skewY * (Math.PI/180.0));
        }

        /// <summary>
        /// Prepends a skew to this matrix
        /// </summary>
        /// <param name='skewX'>The skew angle in the x dimension in degrees</param>
        /// <param name='skewY'>The skew angle in the y dimension in degrees</param>
        public void SkewPrepend(double skewX, double skewY)
        {
            skewX %= 360;
            skewY %= 360;
            this = CreateSkewRadians(skewX * (Math.PI/180.0),
                                     skewY * (Math.PI/180.0)) * this;
        }

        /// <summary>
        /// Translates this matrix
        /// </summary>
        /// <param name='offsetX'>The offset in the x dimension</param>
        /// <param name='offsetY'>The offset in the y dimension</param>
        public void Translate(double offsetX, double offsetY)
        {
            //
            // / a b 0 \   / 1 0 0 \    / a      b       0 \
            // | c d 0 | * | 0 1 0 | = |  c      d       0 |
            // \ e f 1 /   \ x y 1 /    \ e+x    f+y     1 /
            //
            // (where e = _offsetX and f == _offsetY)
            //

            if (_type == MatrixTypes.TRANSFORM_IS_IDENTITY)
            {
                // Values would be incorrect if matrix was created using default constructor.
                // or if SetIdentity was called on a matrix which had values.
                //
                SetMatrix(1, 0,
                          0, 1,
                          offsetX, offsetY,
                          MatrixTypes.TRANSFORM_IS_TRANSLATION);
            }
            else if (_type == MatrixTypes.TRANSFORM_IS_UNKNOWN)
            {
                _offsetX += offsetX;
                _offsetY += offsetY;
            }
            else
            {
                _offsetX += offsetX;
                _offsetY += offsetY;

                // If matrix wasn't unknown we added a translation
                _type |= MatrixTypes.TRANSFORM_IS_TRANSLATION;
            }

            Debug_CheckType();
        }

        /// <summary>
        /// Prepends a translation to this matrix
        /// </summary>
        /// <param name='offsetX'>The offset in the x dimension</param>
        /// <param name='offsetY'>The offset in the y dimension</param>
        public void TranslatePrepend(double offsetX, double offsetY)
        {
            this = CreateTranslation(offsetX, offsetY) * this;
        }

#endregion Set Methods

#region Transformation Services

        /// <summary>
        /// Transform - returns the result of transforming the point by this matrix
        /// </summary>
        /// <returns>
        /// The transformed point
        /// </returns>
        /// <param name="point"> The Point to transform </param>
        public Point Transform(Point point)
        {
            Point newPoint = point;
            MultiplyPoint(ref newPoint._x, ref newPoint._y);
            return newPoint;
        }

        /// <summary>
        /// Transform - Transforms each point in the array by this matrix
        /// </summary>
        /// <param name="points"> The Point array to transform </param>
        public void Transform(Point[] points)
        {
            if (points != null)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    MultiplyPoint(ref points[i]._x, ref points[i]._y);
                }
            }
        }

        /// <summary>
        /// Transform - returns the result of transforming the Vector by this matrix.
        /// </summary>
        /// <returns>
        /// The transformed vector
        /// </returns>
        /// <param name="vector"> The Vector to transform </param>
        public Vector Transform(Vector vector)
        {
            Vector newVector = vector;
            MultiplyVector(ref newVector._x, ref newVector._y);
            return newVector;
        }

        /// <summary>
        /// Transform - Transforms each Vector in the array by this matrix.
        /// </summary>
        /// <param name="vectors"> The Vector array to transform </param>
        public void Transform(Vector[] vectors)
        {
            if (vectors != null)
            {
                for (int i = 0; i < vectors.Length; i++)
                {
                    MultiplyVector(ref vectors[i]._x, ref vectors[i]._y);
                }
            }
        }

#endregion Transformation Services

#region Inversion

        /// <summary>
        /// The determinant of this matrix
        /// </summary>
        public double Determinant
        {
            get
            {
                switch (_type)
                {
                case MatrixTypes.TRANSFORM_IS_IDENTITY:
                case MatrixTypes.TRANSFORM_IS_TRANSLATION:
                    return 1.0;
                case MatrixTypes.TRANSFORM_IS_SCALING:
                case MatrixTypes.TRANSFORM_IS_SCALING | MatrixTypes.TRANSFORM_IS_TRANSLATION:
                    return(_m11  * _m22);
                default:
                    return(_m11  * _m22) - (_m12 * _m21);
                }
            }
        }

        /// <summary>
        /// HasInverse Property - returns true if this matrix is invertable, false otherwise.
        /// </summary>
        public bool HasInverse
        {
            get
            {
                return !DoubleUtil.IsZero(Determinant);
            }
        }

        /// <summary>
        /// Replaces matrix with the inverse of the transformation.  This will throw an InvalidOperationException
        /// if !HasInverse
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// This will throw an InvalidOperationException if the matrix is non-invertable
        /// </exception>
        public void Invert()
        {
            double determinant = Determinant;

            if (DoubleUtil.IsZero(determinant))
            {
                throw new System.InvalidOperationException(SR.Get(SRID.Transform_NotInvertible));
            }

            // Inversion does not change the type of a matrix.
            switch (_type)
            {
            case MatrixTypes.TRANSFORM_IS_IDENTITY:
                break;
            case MatrixTypes.TRANSFORM_IS_SCALING:
                {
                    _m11 = 1.0 / _m11;
                    _m22 = 1.0 / _m22;
                }
                break;
            case MatrixTypes.TRANSFORM_IS_TRANSLATION:
                _offsetX = -_offsetX;
                _offsetY = -_offsetY;
                break;
            case MatrixTypes.TRANSFORM_IS_SCALING | MatrixTypes.TRANSFORM_IS_TRANSLATION:
                {
                    _m11 = 1.0 / _m11;
                    _m22 = 1.0 / _m22;
                    _offsetX = -_offsetX * _m11;
                    _offsetY = -_offsetY * _m22;
                }
                break;
            default:
                {
                    double invdet = 1.0/determinant;
                    SetMatrix(_m22 * invdet,
                              -_m12 * invdet,
                              -_m21 * invdet,
                              _m11 * invdet,
                              (_m21 * _offsetY - _offsetX * _m22) * invdet,
                              (_offsetX * _m12 - _m11 * _offsetY) * invdet,
                              MatrixTypes.TRANSFORM_IS_UNKNOWN);
                }
                break;
            }
        }

#endregion Inversion

#region Public Properties

        /// <summary>
        /// M11
        /// </summary>
        public double M11
        {
            get
            {
                if (_type == MatrixTypes.TRANSFORM_IS_IDENTITY)
                {
                    return 1.0;
                }
                else
                {
                    return _m11;
                }
            }
            set
            {
                if (_type == MatrixTypes.TRANSFORM_IS_IDENTITY)
                {
                    SetMatrix(value, 0,
                              0, 1,
                              0, 0,
                              MatrixTypes.TRANSFORM_IS_SCALING);
                }
                else
                {
                    _m11 = value;
                    if (_type != MatrixTypes.TRANSFORM_IS_UNKNOWN)
                    {
                        _type |= MatrixTypes.TRANSFORM_IS_SCALING;
                    }
                }
            }
        }

        /// <summary>
        /// M12
        /// </summary>
        public double M12
        {
            get
            {
                if (_type == MatrixTypes.TRANSFORM_IS_IDENTITY)
                {
                    return 0;
                }
                else
                {
                    return _m12;
                }
            }
            set
            {
                if (_type == MatrixTypes.TRANSFORM_IS_IDENTITY)
                {
                    SetMatrix(1, value,
                              0, 1,
                              0, 0,
                              MatrixTypes.TRANSFORM_IS_UNKNOWN);
                }
                else
                {
                    _m12 = value;
                    _type = MatrixTypes.TRANSFORM_IS_UNKNOWN;
                }
            }
        }

        /// <summary>
        /// M22
        /// </summary>
        public double M21
        {
            get
            {
                if (_type == MatrixTypes.TRANSFORM_IS_IDENTITY)
                {
                    return 0;
                }
                else
                {
                    return _m21;
                }
            }
            set
            {
                if (_type == MatrixTypes.TRANSFORM_IS_IDENTITY)
                {
                    SetMatrix(1, 0,
                              value, 1,
                              0, 0,
                              MatrixTypes.TRANSFORM_IS_UNKNOWN);
                }
                else
                {
                    _m21 = value;
                    _type = MatrixTypes.TRANSFORM_IS_UNKNOWN;
                }
            }
        }

        /// <summary>
        /// M22
        /// </summary>
        public double M22
        {
            get
            {
                if (_type == MatrixTypes.TRANSFORM_IS_IDENTITY)
                {
                    return 1.0;
                }
                else
                {
                    return _m22;
                }
            }
            set
            {
                if (_type == MatrixTypes.TRANSFORM_IS_IDENTITY)
                {
                    SetMatrix(1, 0,
                              0, value,
                              0, 0,
                              MatrixTypes.TRANSFORM_IS_SCALING);
                }
                else
                {
                    _m22 = value;
                    if (_type != MatrixTypes.TRANSFORM_IS_UNKNOWN)
                    {
                        _type |= MatrixTypes.TRANSFORM_IS_SCALING;
                    }
                }
            }
        }

        /// <summary>
        /// OffsetX
        /// </summary>
        public double OffsetX
        {
            get
            {
                if (_type == MatrixTypes.TRANSFORM_IS_IDENTITY)
                {
                    return 0;
                }
                else
                {
                    return _offsetX;
                }
            }
            set
            {
                if (_type == MatrixTypes.TRANSFORM_IS_IDENTITY)
                {
                    SetMatrix(1, 0,
                              0, 1,
                              value, 0,
                              MatrixTypes.TRANSFORM_IS_TRANSLATION);
                }
                else
                {
                    _offsetX = value;
                    if (_type != MatrixTypes.TRANSFORM_IS_UNKNOWN)
                    {
                        _type |= MatrixTypes.TRANSFORM_IS_TRANSLATION;
                    }
                }
            }
        }

        /// <summary>
        /// OffsetY
        /// </summary>
        public double OffsetY
        {
            get
            {
                if (_type == MatrixTypes.TRANSFORM_IS_IDENTITY)
                {
                    return 0;
                }
                else
                {
                    return _offsetY;
                }
            }
            set
            {
                if (_type == MatrixTypes.TRANSFORM_IS_IDENTITY)
                {
                    SetMatrix(1, 0,
                              0, 1,
                              0, value,
                              MatrixTypes.TRANSFORM_IS_TRANSLATION);
                }
                else
                {
                    _offsetY = value;
                    if (_type != MatrixTypes.TRANSFORM_IS_UNKNOWN)
                    {
                        _type |= MatrixTypes.TRANSFORM_IS_TRANSLATION;
                    }
                }
            }
        }

        #endregion Public Properties

        #region Internal Methods
        /// <summary>
        /// MultiplyVector
        /// </summary>
        internal void MultiplyVector(ref double x, ref double y)
        {
            switch (_type)
            {
            case MatrixTypes.TRANSFORM_IS_IDENTITY:
            case MatrixTypes.TRANSFORM_IS_TRANSLATION:
                return;
            case MatrixTypes.TRANSFORM_IS_SCALING:
            case MatrixTypes.TRANSFORM_IS_SCALING | MatrixTypes.TRANSFORM_IS_TRANSLATION:
                x *= _m11;
                y *= _m22;
                break;
            default:
                double xadd = y * _m21;
                double yadd = x * _m12;
                x *= _m11;
                x += xadd;
                y *= _m22;
                y += yadd;
                break;
            }
        }

        /// <summary>
        /// MultiplyPoint
        /// </summary>
        internal void MultiplyPoint(ref double x, ref double y)
        {
            switch (_type)
            {
            case MatrixTypes.TRANSFORM_IS_IDENTITY:
                return;
            case MatrixTypes.TRANSFORM_IS_TRANSLATION:
                x += _offsetX;
                y += _offsetY;
                return;
            case MatrixTypes.TRANSFORM_IS_SCALING:
                x *= _m11;
                y *= _m22;
                return;
            case MatrixTypes.TRANSFORM_IS_SCALING | MatrixTypes.TRANSFORM_IS_TRANSLATION:
                x *= _m11;
                x += _offsetX;
                y *= _m22;
                y += _offsetY;
                break;
            default:
                double xadd = y * _m21 + _offsetX;
                double yadd = x * _m12 + _offsetY;
                x *= _m11;
                x += xadd;
                y *= _m22;
                y += yadd;
                break;
            }
        }

        /// <summary>
        /// Creates a rotation transformation about the given point
        /// </summary>
        /// <param name='angle'>The angle to rotate specifed in radians</param>
        internal static Matrix CreateRotationRadians(double angle)
        {
            return CreateRotationRadians(angle, /* centerX = */ 0, /* centerY = */ 0);
        }

        /// <summary>
        /// Creates a rotation transformation about the given point
        /// </summary>
        /// <param name='angle'>The angle to rotate specifed in radians</param>
        /// <param name='centerX'>The centerX of rotation</param>
        /// <param name='centerY'>The centerY of rotation</param>
        internal static Matrix CreateRotationRadians(double angle, double centerX, double centerY)
        {
            Matrix matrix = new Matrix();

            double sin = Math.Sin(angle);
            double cos = Math.Cos(angle);
            double dx    = (centerX * (1.0 - cos)) + (centerY * sin);
            double dy    = (centerY * (1.0 - cos)) - (centerX * sin);

            matrix.SetMatrix( cos, sin,
                              -sin, cos,
                              dx,    dy,
                              MatrixTypes.TRANSFORM_IS_UNKNOWN);

            return matrix;
        }

        /// <summary>
        /// Creates a scaling transform around the given point
        /// </summary>
        /// <param name='scaleX'>The scale factor in the x dimension</param>
        /// <param name='scaleY'>The scale factor in the y dimension</param>
        /// <param name='centerX'>The centerX of scaling</param>
        /// <param name='centerY'>The centerY of scaling</param>
        internal static Matrix CreateScaling(double scaleX, double scaleY, double centerX, double centerY)
        {
            Matrix matrix = new Matrix();

            matrix.SetMatrix(scaleX,  0,
                             0, scaleY,
                             centerX - scaleX*centerX, centerY - scaleY*centerY,
                             MatrixTypes.TRANSFORM_IS_SCALING | MatrixTypes.TRANSFORM_IS_TRANSLATION);

            return matrix;
        }

        /// <summary>
        /// Creates a scaling transform around the origin
        /// </summary>
        /// <param name='scaleX'>The scale factor in the x dimension</param>
        /// <param name='scaleY'>The scale factor in the y dimension</param>
        internal static Matrix CreateScaling(double scaleX, double scaleY)
        {
            Matrix matrix = new Matrix();
            matrix.SetMatrix(scaleX,  0,
                             0, scaleY,
                             0, 0,
                             MatrixTypes.TRANSFORM_IS_SCALING);
            return matrix;
        }

        /// <summary>
        /// Creates a skew transform
        /// </summary>
        /// <param name='skewX'>The skew angle in the x dimension in degrees</param>
        /// <param name='skewY'>The skew angle in the y dimension in degrees</param>
        internal static Matrix CreateSkewRadians(double skewX, double skewY)
        {
            Matrix matrix = new Matrix();

            matrix.SetMatrix(1.0,  Math.Tan(skewY),
                             Math.Tan(skewX), 1.0,
                             0.0, 0.0,
                             MatrixTypes.TRANSFORM_IS_UNKNOWN);

            return matrix;
        }

        /// <summary>
        /// Sets the transformation to the given translation specified by the offset vector.
        /// </summary>
        /// <param name='offsetX'>The offset in X</param>
        /// <param name='offsetY'>The offset in Y</param>
        internal static Matrix CreateTranslation(double offsetX, double offsetY)
        {
            Matrix matrix = new Matrix();

            matrix.SetMatrix(1, 0,
                             0, 1,
                             offsetX, offsetY,
                             MatrixTypes.TRANSFORM_IS_TRANSLATION);

            return matrix;
        }

#endregion Internal Methods

#region Private Methods
        /// <summary>
        /// Sets the transformation to the identity.
        /// </summary>
        private static Matrix CreateIdentity()
        {
            Matrix matrix = new Matrix();
            matrix.SetMatrix(1, 0,
                             0, 1,
                             0, 0,
                             MatrixTypes.TRANSFORM_IS_IDENTITY);
            return matrix;
        }

        ///<summary>
        /// Sets the transform to
        ///             / m11, m12, 0 \
        ///             | m21, m22, 0 |
        ///             \ offsetX, offsetY, 1 /
        /// where offsetX, offsetY is the translation.
        ///</summary>
        private void SetMatrix(double m11, double m12,
                               double m21, double m22,
                               double offsetX, double offsetY,
                               MatrixTypes type)
        {
            this._m11 = m11;
            this._m12 = m12;
            this._m21 = m21;
            this._m22 = m22;
            this._offsetX = offsetX;
            this._offsetY = offsetY;
            this._type = type;
        }

        /// <summary>
        /// Set the type of the matrix based on its current contents
        /// </summary>
        private void DeriveMatrixType()
        {
            _type = 0;

            // Now classify our matrix.
            if (!(_m21 == 0 && _m12 == 0))
            {
                _type = MatrixTypes.TRANSFORM_IS_UNKNOWN;
                return;
            }

            if (!(_m11 == 1 && _m22 == 1))
            {
                _type = MatrixTypes.TRANSFORM_IS_SCALING;
            }

            if (!(_offsetX == 0 && _offsetY == 0))
            {
                _type |= MatrixTypes.TRANSFORM_IS_TRANSLATION;
            }

            if (0 == (_type & (MatrixTypes.TRANSFORM_IS_TRANSLATION | MatrixTypes.TRANSFORM_IS_SCALING)))
            {
                // We have an identity matrix.
                _type = MatrixTypes.TRANSFORM_IS_IDENTITY;
            }
            return;
        }

        /// <summary>
        /// Asserts that the matrix tag is one of the valid options and
        /// that coefficients are correct.   
        /// </summary>
        [Conditional("DEBUG")]
        private void Debug_CheckType()
        {
            switch(_type)
            {
            case MatrixTypes.TRANSFORM_IS_IDENTITY:
                return;
            case MatrixTypes.TRANSFORM_IS_UNKNOWN:
                return;
            case MatrixTypes.TRANSFORM_IS_SCALING:
                Debug.Assert(_m21 == 0);
                Debug.Assert(_m12 == 0);
                Debug.Assert(_offsetX == 0);
                Debug.Assert(_offsetY == 0);
                return;
            case MatrixTypes.TRANSFORM_IS_TRANSLATION:
                Debug.Assert(_m21 == 0);
                Debug.Assert(_m12 == 0);
                Debug.Assert(_m11 == 1);
                Debug.Assert(_m22 == 1);
                return;
            case MatrixTypes.TRANSFORM_IS_SCALING|MatrixTypes.TRANSFORM_IS_TRANSLATION:
                Debug.Assert(_m21 == 0);
                Debug.Assert(_m12 == 0);
                return;
            default:
                Debug.Assert(false);
                return;
            }
        }

#endregion Private Methods
    
#region Private Properties and Fields

        /// <summary>
        /// Efficient but conservative test for identity.  Returns
        /// true if the the matrix is identity.  If it returns false
        /// the matrix may still be identity.
        /// </summary>
        private bool IsDistinguishedIdentity
        {
            get
            {
                return _type == MatrixTypes.TRANSFORM_IS_IDENTITY;
            }
        }

        // The hash code for a matrix is the xor of its element's hashes.
        // Since the identity matrix has 2 1's and 4 0's its hash is 0.
        private const int c_identityHashCode = 0;
    
#endregion Private Properties and Fields

        internal double _m11;
        internal double _m12;
        internal double _m21;
        internal double _m22;
        internal double _offsetX;
        internal double _offsetY;
        internal MatrixTypes _type;

// This field is only used by unmanaged code which isn't detected by the compiler.
#pragma warning disable 0414
        // Matrix in blt'd to unmanaged code, so this is padding 
        // to align structure.
        //
        // Testing note: Validate that this blt will work on 64-bit
        //
        internal Int32 _padding;
#pragma warning restore 0414
    }
}
