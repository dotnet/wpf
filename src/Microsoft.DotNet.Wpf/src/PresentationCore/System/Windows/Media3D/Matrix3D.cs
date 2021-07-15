// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: 3D matrix implementation.
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht
//
// NOTE:
//
// Structs do not have default constructors and all struct variables are initialized to
// zero on construction. For this reason, we cannot simply initialize member variables
// to identity. So we use an auxiliary variable _isNotKnownToBeIdentity. This variable
// will default to false when the struct is initialized, meaning that the matrix is not
// notIdentity (i.e. it is an identity matrix).
//
// All methods that read the _mXX fields on the diagonal (_m11, _m22, _m33, _m44) need
// to be use the IsDistinguishedIdentity property to special case this (which frequently
// turns out to be a nice optimization).
//
//

using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using MS.Internal;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// 3D Matrix.
    /// The matrix is represented in the following row-vector syntax form:
    ///
    /// [ m11      m12      m13      m14 ]
    /// [ m21      m22      m23      m24 ]
    /// [ m31      m32      m33      m34 ]
    /// [ offsetX  offsetY  offsetZ  m44 ]
    ///
    /// Note that since the fourth column is also accessible, the matrix allows one to
    /// represent affine as well as non-affine transforms.
    /// Matrices can be appended or prepended to other matrices. Appending A to B denotes
    /// a transformation by B and then by A - i.e. A(B(...)), whereas prepending A to B denotes a
    /// transformation by A and then by B - i.e. B(A(...)). Thus for example if we want to
    /// transform point P by A and then by B, we append B to A:
    /// C = A.Append(B)
    /// P' = C.Transform(P)
    /// </summary>
    public partial struct Matrix3D
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor that sets matrix's initial values.
        /// </summary>
        /// <param name="m11">Value of the (1,1) field of the new matrix.</param>
        /// <param name="m12">Value of the (1,2) field of the new matrix.</param>
        /// <param name="m13">Value of the (1,3) field of the new matrix.</param>
        /// <param name="m14">Value of the (1,4) field of the new matrix.</param>
        /// <param name="m21">Value of the (2,1) field of the new matrix.</param>
        /// <param name="m22">Value of the (2,2) field of the new matrix.</param>
        /// <param name="m23">Value of the (2,3) field of the new matrix.</param>
        /// <param name="m24">Value of the (2,4) field of the new matrix.</param>
        /// <param name="m31">Value of the (3,1) field of the new matrix.</param>
        /// <param name="m32">Value of the (3,2) field of the new matrix.</param>
        /// <param name="m33">Value of the (3,3) field of the new matrix.</param>
        /// <param name="m34">Value of the (3,4) field of the new matrix.</param>
        /// <param name="offsetX">Value of the X offset field of the new matrix.</param>
        /// <param name="offsetY">Value of the Y offset field of the new matrix.</param>
        /// <param name="offsetZ">Value of the Z offset field of the new matrix.</param>
        /// <param name="m44">Value of the (4,4) field of the new matrix.</param>
        public Matrix3D(double m11, double m12, double m13, double m14,
                        double m21, double m22, double m23, double m24,
                        double m31, double m32, double m33, double m34,
                        double offsetX, double offsetY, double offsetZ, double m44)
        {
            _m11 = m11;
            _m12 = m12;
            _m13 = m13;
            _m14 = m14;
            _m21 = m21;
            _m22 = m22;
            _m23 = m23;
            _m24 = m24;
            _m31 = m31;
            _m32 = m32;
            _m33 = m33;
            _m34 = m34;
            _offsetX = offsetX;
            _offsetY = offsetY;
            _offsetZ = offsetZ;
            _m44 = m44;

            // This is not known to be an identity matrix so we need
            // to change our flag from it's default value.  We use the field
            // in the ctor rather than the property because of CS0188.
            _isNotKnownToBeIdentity = true;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Identity
        //
        //------------------------------------------------------

        #region Identity
        // Identity
        /// <summary>
        /// Returns identity matrix.
        /// </summary>
        public static Matrix3D Identity
        {
            get
            {
                return s_identity;
            }
        }

        /// <summary>
        /// Sets matrix to identity.
        /// </summary>
        public void SetIdentity()
        {
            this = s_identity;
        }

        /// <summary>
        /// Returns whether the matrix is identity.
        /// </summary>
        public bool IsIdentity
        {
            get
            {
                if (IsDistinguishedIdentity)
                {
                    return true;
                }
                else
                {
                    // Otherwise check all elements one by one.
                    if (_m11 == 1.0 && _m12 == 0.0 && _m13 == 0.0 && _m14 == 0.0 &&
                        _m21 == 0.0 && _m22 == 1.0 && _m23 == 0.0 && _m24 == 0.0 &&
                        _m31 == 0.0 && _m32 == 0.0 && _m33 == 1.0 && _m34 == 0.0 &&
                        _offsetX == 0.0 && _offsetY == 0.0 && _offsetZ == 0.0 && _m44 == 1.0)
                    {
                        // If matrix is identity, cache this with the IsDistinguishedIdentity flag.
                        IsDistinguishedIdentity = true;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        #endregion Identity

        //------------------------------------------------------
        //
        //  Math Operations
        //
        //------------------------------------------------------

        #region Math Operations
        // Math operations

        /// <summary>
        /// Prepends the given matrix to the current matrix.
        /// </summary>
        /// <param name="matrix">Matrix to prepend.</param>
        public void Prepend(Matrix3D matrix)
        {
            this = matrix * this;
        }

        /// <summary>
        /// Appends the given matrix to the current matrix.
        /// </summary>
        /// <param name="matrix">Matrix to append.</param>
        public void Append(Matrix3D matrix)
        {
            this *= matrix;
        }

        #endregion Math Operations

        //------------------------------------------------------
        //
        //  Rotate
        //
        //------------------------------------------------------

        #region Rotate

        /// <summary>
        /// Appends rotation transform to the current matrix.
        /// </summary>
        /// <param name="quaternion">Quaternion representing rotation.</param>
        public void Rotate(Quaternion quaternion)
        {
            Point3D center = new Point3D();

            this *= CreateRotationMatrix(ref quaternion, ref center);
        }

        /// <summary>
        /// Prepends rotation transform to the current matrix.
        /// </summary>
        /// <param name="quaternion">Quaternion representing rotation.</param>
        public void RotatePrepend(Quaternion quaternion)
        {
            Point3D center = new Point3D();
            
            this = CreateRotationMatrix(ref quaternion, ref center) * this;
        }

        /// <summary>
        /// Appends rotation transform to the current matrix.
        /// </summary>
        /// <param name="quaternion">Quaternion representing rotation.</param>
        /// <param name="center">Center to rotate around.</param>
        public void RotateAt(Quaternion quaternion, Point3D center)
        {
            this *= CreateRotationMatrix(ref quaternion, ref center);
        }

        /// <summary>
        /// Prepends rotation transform to the current matrix.
        /// </summary>
        /// <param name="quaternion">Quaternion representing rotation.</param>
        /// <param name="center">Center to rotate around.</param>
        public void RotateAtPrepend(Quaternion quaternion, Point3D center)
        {
            this = CreateRotationMatrix(ref quaternion, ref center) * this;
        }

        #endregion Rotate


        //------------------------------------------------------
        //
        //  Scale
        //
        //------------------------------------------------------

        #region Scale

        /// <summary>
        /// Appends scale transform to the current matrix.
        /// </summary>
        /// <param name="scale">Scaling vector for transformation.</param>
        public void Scale(Vector3D scale)
        {
            if (IsDistinguishedIdentity)
            {
                SetScaleMatrix(ref scale);
            }
            else
            {
                _m11     *= scale.X; _m12     *= scale.Y; _m13     *= scale.Z;
                _m21     *= scale.X; _m22     *= scale.Y; _m23     *= scale.Z;
                _m31     *= scale.X; _m32     *= scale.Y; _m33     *= scale.Z;
                _offsetX *= scale.X; _offsetY *= scale.Y; _offsetZ *= scale.Z;              
            }
        }

        /// <summary>
        /// Prepends scale transform to the current matrix.
        /// </summary>
        /// <param name="scale">Scaling vector for transformation.</param>
        public void ScalePrepend(Vector3D scale)
        {
            if (IsDistinguishedIdentity)
            {
                SetScaleMatrix(ref scale);
            }
            else
            {
                _m11 *= scale.X; _m12 *= scale.X; _m13 *= scale.X; _m14 *= scale.X;
                _m21 *= scale.Y; _m22 *= scale.Y; _m23 *= scale.Y; _m24 *= scale.Y;           
                _m31 *= scale.Z; _m32 *= scale.Z; _m33 *= scale.Z; _m34 *= scale.Z;
            }
        }

        /// <summary>
        /// Appends scale transform to the current matrix.
        /// </summary>
        /// <param name="scale">Scaling vector for transformation.</param>
        /// <param name="center">Point around which to scale.</param>
        public void ScaleAt(Vector3D scale, Point3D center)
        {
            if (IsDistinguishedIdentity)
            {
                SetScaleMatrix(ref scale, ref center);
            }
            else
            {
                double tmp = _m14 * center.X;
                _m11 = tmp + scale.X * (_m11 - tmp);
                tmp = _m14 * center.Y;
                _m12 = tmp + scale.Y * (_m12 - tmp);
                tmp = _m14 * center.Z;
                _m13 = tmp + scale.Z * (_m13 - tmp);

                tmp = _m24 * center.X;
                _m21 = tmp + scale.X * (_m21 - tmp);
                tmp = _m24 * center.Y;
                _m22 = tmp + scale.Y * (_m22 - tmp);
                tmp = _m24 * center.Z;
                _m23 = tmp + scale.Z * (_m23 - tmp);

                tmp = _m34 * center.X;
                _m31 = tmp + scale.X * (_m31 - tmp);
                tmp = _m34 * center.Y;
                _m32 = tmp + scale.Y * (_m32 - tmp);
                tmp = _m34 * center.Z;
                _m33 = tmp + scale.Z * (_m33 - tmp);

                tmp = _m44 * center.X;
                _offsetX = tmp + scale.X * (_offsetX - tmp);
                tmp = _m44 * center.Y;
                _offsetY = tmp + scale.Y * (_offsetY - tmp);
                tmp = _m44 * center.Z;
                _offsetZ = tmp + scale.Z * (_offsetZ - tmp);
            }
        }

        /// <summary>
        /// Prepends scale transform to the current matrix.
        /// </summary>
        /// <param name="scale">Scaling vector for transformation.</param>
        /// <param name="center">Point around which to scale.</param>
        public void ScaleAtPrepend(Vector3D scale, Point3D center)
        {
            if (IsDistinguishedIdentity)
            {
                SetScaleMatrix(ref scale, ref center);
            }
            else
            {
                double csx = center.X - center.X * scale.X;
                double csy = center.Y - center.Y * scale.Y;
                double csz = center.Z - center.Z * scale.Z;

                // We have to set the bottom row first because it depends
                // on values that will change
                _offsetX += _m11 * csx + _m21 * csy + _m31 * csz; 
                _offsetY += _m12 * csx + _m22 * csy + _m32 * csz; 
                _offsetZ += _m13 * csx + _m23 * csy + _m33 * csz; 
                _m44     += _m14 * csx + _m24 * csy + _m34 * csz; 
                
                _m11 *= scale.X; _m12 *= scale.X; _m13 *= scale.X; _m14 *= scale.X;
                _m21 *= scale.Y; _m22 *= scale.Y; _m23 *= scale.Y; _m24 *= scale.Y;           
                _m31 *= scale.Z; _m32 *= scale.Z; _m33 *= scale.Z; _m34 *= scale.Z;
            }
        }

        #endregion Scale
            
        //------------------------------------------------------
        //
        //  Translate
        //
        //------------------------------------------------------

        #region Translate
        /// <summary>
        /// Appends translation transform to the current matrix.
        /// </summary>
        /// <param name="offset">Offset vector for transformation.</param>
        public void Translate(Vector3D offset)
        {
            if (IsDistinguishedIdentity)
            {
                SetTranslationMatrix(ref offset);
            }
            else
            {
                _m11     += _m14 * offset.X; _m12     += _m14 * offset.Y; _m13     += _m14 * offset.Z;
                _m21     += _m24 * offset.X; _m22     += _m24 * offset.Y; _m23     += _m24 * offset.Z;
                _m31     += _m34 * offset.X; _m32     += _m34 * offset.Y; _m33     += _m34 * offset.Z;
                _offsetX += _m44 * offset.X; _offsetY += _m44 * offset.Y; _offsetZ += _m44 * offset.Z;
            }
        }

        /// <summary>
        /// Prepends translation transform to the current matrix.
        /// </summary>
        /// <param name="offset">Offset vector for transformation.</param>
        public void TranslatePrepend(Vector3D offset)
        {
            if (IsDistinguishedIdentity)
            {
                SetTranslationMatrix(ref offset);
            }
            else
            {
                _offsetX += _m11 * offset.X + _m21 * offset.Y + _m31 * offset.Z;
                _offsetY += _m12 * offset.X + _m22 * offset.Y + _m32 * offset.Z;
                _offsetZ += _m13 * offset.X + _m23 * offset.Y + _m33 * offset.Z;
                _m44     += _m14 * offset.X + _m24 * offset.Y + _m34 * offset.Z;
            }
        }

        #endregion Translate

        //------------------------------------------------------
        //
        //  Multiplication
        //
        //------------------------------------------------------

        #region Multiplication

        /// <summary>
        /// Matrix multiplication.
        /// </summary>
        /// <param name="matrix1">Matrix to multiply.</param>
        /// <param name="matrix2">Matrix by which the first matrix is multiplied.</param>
        /// <returns>Result of multiplication.</returns>
        public static Matrix3D operator * (Matrix3D matrix1, Matrix3D matrix2)
        {
            // Check if multiplying by identity.
            if (matrix1.IsDistinguishedIdentity)
                return matrix2;
            if (matrix2.IsDistinguishedIdentity)
                return matrix1;

            // Regular 4x4 matrix multiplication.
            Matrix3D result = new Matrix3D(
                matrix1._m11 * matrix2._m11 + matrix1._m12 * matrix2._m21 +
                matrix1._m13 * matrix2._m31 + matrix1._m14 * matrix2._offsetX,
                matrix1._m11 * matrix2._m12 + matrix1._m12 * matrix2._m22 +
                matrix1._m13 * matrix2._m32 + matrix1._m14 * matrix2._offsetY,
                matrix1._m11 * matrix2._m13 + matrix1._m12 * matrix2._m23 +
                matrix1._m13 * matrix2._m33 + matrix1._m14 * matrix2._offsetZ,
                matrix1._m11 * matrix2._m14 + matrix1._m12 * matrix2._m24 +
                matrix1._m13 * matrix2._m34 + matrix1._m14 * matrix2._m44,
                matrix1._m21 * matrix2._m11 + matrix1._m22 * matrix2._m21 +
                matrix1._m23 * matrix2._m31 + matrix1._m24 * matrix2._offsetX,
                matrix1._m21 * matrix2._m12 + matrix1._m22 * matrix2._m22 +
                matrix1._m23 * matrix2._m32 + matrix1._m24 * matrix2._offsetY,
                matrix1._m21 * matrix2._m13 + matrix1._m22 * matrix2._m23 +
                matrix1._m23 * matrix2._m33 + matrix1._m24 * matrix2._offsetZ,
                matrix1._m21 * matrix2._m14 + matrix1._m22 * matrix2._m24 +
                matrix1._m23 * matrix2._m34 + matrix1._m24 * matrix2._m44,
                matrix1._m31 * matrix2._m11 + matrix1._m32 * matrix2._m21 +
                matrix1._m33 * matrix2._m31 + matrix1._m34 * matrix2._offsetX,
                matrix1._m31 * matrix2._m12 + matrix1._m32 * matrix2._m22 +
                matrix1._m33 * matrix2._m32 + matrix1._m34 * matrix2._offsetY,
                matrix1._m31 * matrix2._m13 + matrix1._m32 * matrix2._m23 +
                matrix1._m33 * matrix2._m33 + matrix1._m34 * matrix2._offsetZ,
                matrix1._m31 * matrix2._m14 + matrix1._m32 * matrix2._m24 +
                matrix1._m33 * matrix2._m34 + matrix1._m34 * matrix2._m44,
                matrix1._offsetX * matrix2._m11 + matrix1._offsetY * matrix2._m21 +
                matrix1._offsetZ * matrix2._m31 + matrix1._m44 * matrix2._offsetX,
                matrix1._offsetX * matrix2._m12 + matrix1._offsetY * matrix2._m22 +
                matrix1._offsetZ * matrix2._m32 + matrix1._m44 * matrix2._offsetY,
                matrix1._offsetX * matrix2._m13 + matrix1._offsetY * matrix2._m23 +
                matrix1._offsetZ * matrix2._m33 + matrix1._m44 * matrix2._offsetZ,
                matrix1._offsetX * matrix2._m14 + matrix1._offsetY * matrix2._m24 +
                matrix1._offsetZ * matrix2._m34 + matrix1._m44 * matrix2._m44);

            return result;
        }

        /// <summary>
        /// Matrix multiplication.
        /// </summary>
        /// <param name="matrix1">Matrix to multiply.</param>
        /// <param name="matrix2">Matrix by which the first matrix is multiplied.</param>
        /// <returns>Result of multiplication.</returns>
        public static Matrix3D Multiply(Matrix3D matrix1, Matrix3D matrix2)
        {
            return (matrix1*matrix2);
        }

        #endregion Multiplication

        //------------------------------------------------------
        //
        //  Transformation Services
        //
        //------------------------------------------------------

        #region Transformation Services

        /// <summary>
        ///  Transforms the given Point3D by this matrix, projecting the
        ///  result back into the W=1 plane.
        /// </summary>
        /// <param name="point">Point to transform.</param>
        /// <returns>Transformed point.</returns>
        public Point3D Transform(Point3D point)
        {
            MultiplyPoint(ref point);
            return point;
        }

        /// <summary>
        /// Transforms the given Point3Ds by this matrix, projecting the
        /// results back into the W=1 plane.
        /// </summary>
        /// <param name="points">Points to transform.</param>
        public void Transform(Point3D[] points)
        {
            if (points != null)
            {
                for(int i = 0; i < points.Length; i++)
                {
                    MultiplyPoint(ref points[i]);
                }
            }
        }

        /// <summary>
        /// Transforms the given point by the current matrix.
        /// </summary>
        /// <param name="point">Point to transform.</param>
        /// <returns>Transformed point.</returns>
        public Point4D Transform(Point4D point)
        {
            MultiplyPoint(ref point);
            return point;
        }

        /// <summary>
        /// Transforms the given points by the current matrix.
        /// </summary>
        /// <param name="points">Points to transform.</param>
        public void Transform(Point4D[] points)
        {
            if (points != null)
            {
                for(int i = 0; i < points.Length; i++)
                {
                    MultiplyPoint(ref points[i]);
                }
            }
        }

        /// <summary>
        /// Transforms the given vector by the current matrix.
        /// </summary>
        /// <param name="vector">Vector to transform.</param>
        /// <returns>Transformed vector.</returns>
        public Vector3D Transform(Vector3D vector)
        {
            MultiplyVector(ref vector);
            return vector;
        }

        /// <summary>
        /// Transforms the given vectors by the current matrix.
        /// </summary>
        /// <param name="vectors">Vectors to transform.</param>
        public void Transform(Vector3D[] vectors)
        {
            if (vectors != null)
            {
                for(int i = 0; i < vectors.Length; i++)
                {
                    MultiplyVector(ref vectors[i]);
                }
            }
        }

        #endregion Transformation Services


        /// <summary>
        /// Determines whether the matrix is affine.
        /// </summary>
        public bool IsAffine
        {
            get
            {
                return (IsDistinguishedIdentity ||
                        (_m14 == 0.0 && _m24 == 0.0 && _m34 == 0.0 && _m44 == 1.0));
            }
        }


        //------------------------------------------------------
        //
        //  Inversion
        //
        //------------------------------------------------------

        #region Inversion

        /// <summary>
        /// Matrix determinant.
        /// </summary>
        public double Determinant
        {
            get
            {
                if (IsDistinguishedIdentity)
                    return 1.0;
                if (IsAffine)
                    return GetNormalizedAffineDeterminant();
                
                // NOTE: The beginning of this code is duplicated between
                //       the Invert method and the Determinant property.
                
                // compute all six 2x2 determinants of 2nd two columns
                double y01 = _m13 * _m24 - _m23 * _m14;
                double y02 = _m13 * _m34 - _m33 * _m14;
                double y03 = _m13 * _m44 - _offsetZ * _m14;
                double y12 = _m23 * _m34 - _m33 * _m24;
                double y13 = _m23 * _m44 - _offsetZ * _m24;
                double y23 = _m33 * _m44 - _offsetZ * _m34;
        
                // Compute 3x3 cofactors for 1st the column
                double z30 = _m22 * y02 - _m32 * y01 - _m12 * y12;
                double z20 = _m12 * y13 - _m22 * y03 + _offsetY * y01;
                double z10 = _m32 * y03 - _offsetY * y02 - _m12 * y23;
                double z00 = _m22 * y23 - _m32 * y13 + _offsetY * y12;
        
                return _offsetX * z30 + _m31 * z20 + _m21 * z10 + _m11 * z00;
            }
        }

        /// <summary>
        /// Whether the matrix has an inverse.
        /// </summary>
        public bool HasInverse
        {
            get
            {
                return !DoubleUtil.IsZero(Determinant);
            }
        }

        /// <summary>
        ///     Computes, and substitutes in-place, the inverse of a matrix.
        ///     The determinant of the matrix must be nonzero, otherwise the matrix is not invertible.
        ///     In this case it will throw InvalidOperationException exception.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     This will throw InvalidOperationException if the matrix is not invertible.
        /// </exception>
        public void Invert()
        {
            if (!InvertCore())
            {
                throw new InvalidOperationException(SR.Get(SRID.Matrix3D_NotInvertible, null));
            }
        }

        #endregion Inversion

        //------------------------------------------------------
        //
        //  Individual Members
        //
        //------------------------------------------------------

        #region Individual Members

        /// <summary>
        /// Retrieves or sets (1,1) value of the matrix.
        /// </summary>
        public double M11
        {
            get
            {
                if (IsDistinguishedIdentity)
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
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _m11 = value;
            }
        }

        /// <summary>
        /// Retrieves or sets (1,2) value of the matrix.
        /// </summary>
        public double M12
        {
            get
            {
                return _m12;
            }
            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _m12 = value;
            }
        }

        /// <summary>
        /// Retrieves or sets (1,3) value of the matrix.
        /// </summary>
        public double M13
        {
            get
            {
                return _m13;
            }
            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _m13 = value;
            }
        }

        /// <summary>
        /// Retrieves or sets (1,4) value of the matrix.
        /// </summary>
        public double M14
        {
            get
            {
                return _m14;
            }
            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _m14 = value;
            }
        }


        /// <summary>
        /// Retrieves or sets (2,1) value of the matrix.
        /// </summary>
        public double M21
        {
            get
            {
                return _m21;
            }
            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _m21 = value;
            }
        }

        /// <summary>
        /// Retrieves or sets (2,2) value of the matrix.
        /// </summary>
        public double M22
        {
            get
            {
                if (IsDistinguishedIdentity)
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
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _m22 = value;
            }
        }

        /// <summary>
        /// Retrieves or sets (2,3) value of the matrix.
        /// </summary>
        public double M23
        {
            get
            {
                return _m23;
            }
            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _m23 = value;
            }
        }

        /// <summary>
        /// Retrieves or sets (2,4) value of the matrix.
        /// </summary>
        public double M24
        {
            get
            {
                return _m24;
            }
            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _m24 = value;
            }
        }



        /// <summary>
        /// Retrieves or sets (3,1) value of the matrix.
        /// </summary>
        public double M31
        {
            get
            {
                return _m31;
            }
            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _m31 = value;
            }
        }

        /// <summary>
        /// Retrieves or sets (3,2) value of the matrix.
        /// </summary>
        public double M32
        {
            get
            {
                return _m32;
            }
            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _m32 = value;
            }
        }

        /// <summary>
        /// Retrieves or sets (3,3) value of the matrix.
        /// </summary>
        public double M33
        {
            get
            {
                if (IsDistinguishedIdentity)
                {
                    return 1.0;
                }
                else
                {
                    return _m33;
                }
            }
            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _m33 = value;
            }
        }

        /// <summary>
        /// Retrieves or sets (3,4) value of the matrix.
        /// </summary>
        public double M34
        {
            get
            {
                return _m34;
            }
            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _m34 = value;
            }
        }


        /// <summary>
        /// Retrieves or sets X offset of the matrix.
        /// </summary>
        public double OffsetX
        {
            get
            {
                return _offsetX;
            }
            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _offsetX = value;
            }
        }
        /// <summary>
        /// Retrieves or sets Y offset of the matrix.
        /// </summary>
        public double OffsetY
        {
            get
            {
                return _offsetY;
            }
            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _offsetY = value;
            }
        }
        /// <summary>
        /// Retrieves or sets Z offset of the matrix.
        /// </summary>
        public double OffsetZ
        {
            get
            {
                return _offsetZ;
            }
            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _offsetZ = value;
            }
        }


        /// <summary>
        /// Retrieves or sets (4,4) value of the matrix.
        /// </summary>
        public double M44
        {
            get
            {
                if (IsDistinguishedIdentity)
                {
                    return 1.0;
                }
                else
                {
                    return _m44;
                }
            }
            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _m44 = value;
            }
        }



        #endregion Individual Members

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal void SetScaleMatrix(ref Vector3D scale)
        {
            Debug.Assert(IsDistinguishedIdentity);
            
            _m11 = scale.X;
            _m22 = scale.Y;
            _m33 = scale.Z;
            _m44 = 1.0;

            IsDistinguishedIdentity = false;
        }

        internal void SetScaleMatrix(ref Vector3D scale, ref Point3D center)
        {
            Debug.Assert(IsDistinguishedIdentity);
            
            _m11 = scale.X;
            _m22 = scale.Y;
            _m33 = scale.Z;
            _m44 = 1.0;

            _offsetX = center.X - center.X * scale.X;
            _offsetY = center.Y - center.Y * scale.Y;
            _offsetZ = center.Z - center.Z * scale.Z;

            IsDistinguishedIdentity = false;
        }

        internal void SetTranslationMatrix(ref Vector3D offset)
        {
            Debug.Assert(IsDistinguishedIdentity);

            _m11 = _m22 = _m33 = _m44 = 1.0;
            
            _offsetX = offset.X;
            _offsetY = offset.Y;
            _offsetZ = offset.Z;

            IsDistinguishedIdentity = false;
        }

        //  Creates a rotation matrix given a quaternion and center.
        //
        //  Quaternion and center are passed by reference for performance
        //  only and are not modified.
        //
        internal static Matrix3D CreateRotationMatrix(ref Quaternion quaternion, ref Point3D center)
        {
            Matrix3D matrix = s_identity;
            matrix.IsDistinguishedIdentity = false; // Will be using direct member access
            double wx, wy, wz, xx, yy, yz, xy, xz, zz, x2, y2, z2;

            x2 = quaternion.X + quaternion.X;
            y2 = quaternion.Y + quaternion.Y;
            z2 = quaternion.Z + quaternion.Z;
            xx = quaternion.X * x2;
            xy = quaternion.X * y2;
            xz = quaternion.X * z2;
            yy = quaternion.Y * y2;
            yz = quaternion.Y * z2;
            zz = quaternion.Z * z2;
            wx = quaternion.W * x2;
            wy = quaternion.W * y2;
            wz = quaternion.W * z2;

            matrix._m11 = 1.0 - (yy + zz);
            matrix._m12 = xy + wz;
            matrix._m13 = xz - wy;
            matrix._m21 = xy - wz;
            matrix._m22 = 1.0 - (xx + zz);
            matrix._m23 = yz + wx;
            matrix._m31 = xz + wy;
            matrix._m32 = yz - wx;
            matrix._m33 = 1.0 - (xx + yy);

            if (center.X != 0 || center.Y != 0 || center.Z != 0)
            {
                matrix._offsetX = -center.X*matrix._m11 - center.Y*matrix._m21 - center.Z*matrix._m31 + center.X;
                matrix._offsetY = -center.X*matrix._m12 - center.Y*matrix._m22 - center.Z*matrix._m32 + center.Y;
                matrix._offsetZ = -center.X*matrix._m13 - center.Y*matrix._m23 - center.Z*matrix._m33 + center.Z;
            }
            
            return matrix;
        }

        //  Multiplies the given Point3D by this matrix, projecting the
        //  result back into the W=1 plane.
        //
        //  The point is modified in place for performance.
        //
        internal void MultiplyPoint(ref Point3D point)
        {
            if (IsDistinguishedIdentity)
                return;

            double x = point.X;
            double y = point.Y;
            double z = point.Z;

            point.X = x*_m11 + y*_m21 + z*_m31 + _offsetX;
            point.Y = x*_m12 + y*_m22 + z*_m32 + _offsetY;
            point.Z = x*_m13 + y*_m23 + z*_m33 + _offsetZ;

            if (!IsAffine)
            {
                double w = x*_m14 + y*_m24 + z*_m34 + _m44;

                point.X /= w;
                point.Y /= w;
                point.Z /= w;
            }
        }

        //  Multiplies the given Point4D by this matrix.
        //
        //  The point is modified in place for performance.
        //
        internal void MultiplyPoint(ref Point4D point)
        {
            if (IsDistinguishedIdentity)
                return;

            double x = point.X;
            double y = point.Y;
            double z = point.Z;
            double w = point.W;

            point.X = x*_m11 + y*_m21 + z*_m31 + w*_offsetX;
            point.Y = x*_m12 + y*_m22 + z*_m32 + w*_offsetY;
            point.Z = x*_m13 + y*_m23 + z*_m33 + w*_offsetZ;
            point.W = x*_m14 + y*_m24 + z*_m34 + w*_m44;
        }

        //  Multiplies the given Vector3D by this matrix.
        //
        //  The vector is modified in place for performance.
        //
        internal void MultiplyVector(ref Vector3D vector)
        {
            if (IsDistinguishedIdentity)
                return;

            double x = vector.X;
            double y = vector.Y;
            double z = vector.Z;

            // Do not apply _offset to vectors.
            vector.X = x*_m11 + y*_m21 + z*_m31;
            vector.Y = x*_m12 + y*_m22 + z*_m32;
            vector.Z = x*_m13 + y*_m23 + z*_m33;
        }

        //  Computes the determinant of the matrix assuming that it's
        //  fourth column is 0,0,0,1 and it isn't identity
        internal double GetNormalizedAffineDeterminant()
        {
            Debug.Assert(!IsDistinguishedIdentity);
            Debug.Assert(IsAffine);

            // NOTE: The beginning of this code is duplicated between
            //       GetNormalizedAffineDeterminant() and NormalizedAffineInvert()
            
            double z20 = _m12 * _m23 - _m22 * _m13;
            double z10 = _m32 * _m13 - _m12 * _m33;
            double z00 = _m22 * _m33 - _m32 * _m23;
        
            return _m31 * z20 + _m21 * z10 + _m11 * z00;
        }

        // Assuming this matrix has fourth column of 0,0,0,1 and isn't identity this function:
        // Returns false if HasInverse is false, otherwise inverts the matrix.
        internal bool NormalizedAffineInvert()
        {
            Debug.Assert(!IsDistinguishedIdentity);
            Debug.Assert(IsAffine);

            // NOTE: The beginning of this code is duplicated between
            //       GetNormalizedAffineDeterminant() and NormalizedAffineInvert()
            
            double z20 = _m12 * _m23 - _m22 * _m13;
            double z10 = _m32 * _m13 - _m12 * _m33;
            double z00 = _m22 * _m33 - _m32 * _m23;
            double det = _m31 * z20 + _m21 * z10 + _m11 * z00;

            // Fancy logic here avoids using equality with possible nan values.
            Debug.Assert(!(det < Determinant || det > Determinant),
                         "Matrix3D.Inverse: Determinant property does not match value computed in Inverse.");
        
            if (DoubleUtil.IsZero(det))
            {
                return false;
            }
            
            // Compute 3x3 non-zero cofactors for the 2nd column
            double z21 = _m21 * _m13 - _m11 * _m23;
            double z11 = _m11 * _m33 - _m31 * _m13;
            double z01 = _m31 * _m23 - _m21 * _m33;
        
            // Compute all six 2x2 determinants of 1st two columns
            double y01 = _m11 * _m22 - _m21 * _m12;
            double y02 = _m11 * _m32 - _m31 * _m12;
            double y03 = _m11 * _offsetY - _offsetX * _m12;
            double y12 = _m21 * _m32 - _m31 * _m22;
            double y13 = _m21 * _offsetY - _offsetX * _m22;
            double y23 = _m31 * _offsetY - _offsetX * _m32;
        
            // Compute all non-zero and non-one 3x3 cofactors for 2nd
            // two columns
            double z23 = _m23 * y03 - _offsetZ * y01 - _m13 * y13;
            double z13 = _m13 * y23 - _m33 * y03 + _offsetZ * y02;
            double z03 = _m33 * y13 - _offsetZ * y12 - _m23 * y23;
            double z22 = y01;
            double z12 = -y02;
            double z02 = y12;
        
            double rcp = 1.0 / det;
        
            // Multiply all 3x3 cofactors by reciprocal & transpose
            _m11 = z00 * rcp;
            _m12 = z10 * rcp;
            _m13 = z20 * rcp;

            _m21 = z01 * rcp;
            _m22 = z11 * rcp;
            _m23 = z21 * rcp;

            _m31 = z02 * rcp;
            _m32 = z12 * rcp;
            _m33 = z22 * rcp;

            _offsetX = z03 * rcp;
            _offsetY = z13 * rcp;
            _offsetZ = z23 * rcp;

            return true;
        }

        // RETURNS true if has inverse & invert was done.  Otherwise returns false & leaves matrix unchanged.
        internal bool InvertCore()
        {
            if (IsDistinguishedIdentity)
                return true;

            if (IsAffine)
            {
                return NormalizedAffineInvert();
            }

            // NOTE: The beginning of this code is duplicated between
            //       the Invert method and the Determinant property.
                
            // compute all six 2x2 determinants of 2nd two columns
            double y01 = _m13 * _m24 - _m23 * _m14;
            double y02 = _m13 * _m34 - _m33 * _m14;
            double y03 = _m13 * _m44 - _offsetZ * _m14;
            double y12 = _m23 * _m34 - _m33 * _m24;
            double y13 = _m23 * _m44 - _offsetZ * _m24;
            double y23 = _m33 * _m44 - _offsetZ * _m34;
        
            // Compute 3x3 cofactors for 1st the column
            double z30 = _m22 * y02 - _m32 * y01 - _m12 * y12;
            double z20 = _m12 * y13 - _m22 * y03 + _offsetY * y01;
            double z10 = _m32 * y03 - _offsetY * y02 - _m12 * y23;
            double z00 = _m22 * y23 - _m32 * y13 + _offsetY * y12;
        
            // Compute 4x4 determinant
            double det = _offsetX * z30 + _m31 * z20 + _m21 * z10 + _m11 * z00;

            // If Determinant is computed using a different method then Inverse can throw
            // NotInvertable when HasInverse is true.  (Windows OS #901174)
            //
            // The strange logic below is equivalent to "det == Determinant", but NaN safe.
            Debug.Assert(!(det < Determinant || det > Determinant),
                "Matrix3D.Inverse: Determinant property does not match value computed in Inverse.");
        
            if (DoubleUtil.IsZero(det))
            {
                return false;
            }
        
            // Compute 3x3 cofactors for the 2nd column
            double z31 = _m11 * y12 - _m21 * y02 + _m31 * y01;
            double z21 = _m21 * y03 - _offsetX * y01 - _m11 * y13;
            double z11 = _m11 * y23 - _m31 * y03 + _offsetX * y02;
            double z01 = _m31 * y13 - _offsetX * y12 - _m21 * y23;
        
            // Compute all six 2x2 determinants of 1st two columns
            y01 = _m11 * _m22 - _m21 * _m12;
            y02 = _m11 * _m32 - _m31 * _m12;
            y03 = _m11 * _offsetY - _offsetX * _m12;
            y12 = _m21 * _m32 - _m31 * _m22;
            y13 = _m21 * _offsetY - _offsetX * _m22;
            y23 = _m31 * _offsetY - _offsetX * _m32;
        
            // Compute all 3x3 cofactors for 2nd two columns
            double z33 = _m13 * y12 - _m23 * y02 + _m33 * y01;
            double z23 = _m23 * y03 - _offsetZ * y01 - _m13 * y13;
            double z13 = _m13 * y23 - _m33 * y03 + _offsetZ * y02;
            double z03 = _m33 * y13 - _offsetZ * y12 - _m23 * y23;
            double z32 = _m24 * y02 - _m34 * y01 - _m14 * y12;
            double z22 = _m14 * y13 - _m24 * y03 + _m44 * y01;
            double z12 = _m34 * y03 - _m44 * y02 - _m14 * y23;
            double z02 = _m24 * y23 - _m34 * y13 + _m44 * y12;
        
            double rcp = 1.0 / det;
        
            // Multiply all 3x3 cofactors by reciprocal & transpose
            _m11 = z00 * rcp;
            _m12 = z10 * rcp;
            _m13 = z20 * rcp;
            _m14 = z30 * rcp;

            _m21 = z01 * rcp;
            _m22 = z11 * rcp;
            _m23 = z21 * rcp;
            _m24 = z31 * rcp;

            _m31 = z02 * rcp;
            _m32 = z12 * rcp;
            _m33 = z22 * rcp;
            _m34 = z32 * rcp;

            _offsetX = z03 * rcp;
            _offsetY = z13 * rcp;
            _offsetZ = z23 * rcp;
            _m44 = z33 * rcp;

            return true;
        }

        #endregion Internal Methods

        #region Private Methods

        private static Matrix3D CreateIdentity()
        {
            // Don't call this function, use s_identity.
            Matrix3D matrix = new Matrix3D(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1);
            matrix.IsDistinguishedIdentity = true;
            return matrix;
        }
        
        #endregion Private Methods

        #region Private Properties

        // Returns true if this matrix is guaranteed to be the identity matrix.
        // This is true when a new matrix has been created or after the Identity
        // has already been computed.
        //
        // NOTE: In the case of a new matrix, the _m* fields on the diagonal
        // will be uninitialized.  You should either use the properties which interpret
        // this state and return 1.0 or you can frequently early exit with a known
        // value for the identity matrix.
        //
        // NOTE: This property being false does not mean that the matrix is
        // not the identity matrix, it means that we do not know for certain if
        // it is the identity matrix.  Use the Identity property if you need to
        // know if the matrix is the identity matrix.  (The result will be cached
        // and this property will start returning true.)
        //
        private bool IsDistinguishedIdentity
        {
            get
            {
                Debug.Assert(
                    _isNotKnownToBeIdentity
                        || (
                            (_m11 == 0.0 || _m11 == 1.0) && (_m12 == 0.0) && (_m13 == 0.0) && (_m14 == 0.0) &&
                            (_m21 == 0.0) && (_m22 == 0.0 || _m22 == 1.0) && (_m23 == 0.0) && (_m24 == 0.0) &&
                            (_m31 == 0.0) && (_m32 == 0.0) && (_m33 == 0.0 || _m33 == 1.0) && (_m34 == 0.0) &&
                            (_offsetX == 0.0) && (_offsetY == 0.0) && (_offsetZ == 0.0) && (_m44 == 0.0 || _m44 == 1.0)),
                    "Matrix3D.IsDistinguishedIdentity - _isNotKnownToBeIdentity flag is inconsistent with matrix state.");
                
                return !_isNotKnownToBeIdentity;
            }

            set
            {
                _isNotKnownToBeIdentity = !value;

                // This not only verifies we got the inversion right, but we also hit the
                // the assert above which verifies the value matches the state of the matrix.
                Debug.Assert(IsDistinguishedIdentity == value,
                    "Matrix3D.IsDistinguishedIdentity - Error detected setting IsDistinguishedIdentity.");
            }
        }
        
        #endregion Private Properties

        private double _m11;
        private double _m12;
        private double _m13;
        private double _m14;

        private double _m21;
        private double _m22;
        private double _m23;
        private double _m24;

        private double _m31;
        private double _m32;
        private double _m33;
        private double _m34;

        private double _offsetX;
        private double _offsetY;
        private double _offsetZ;

        private double _m44;

        // Internal matrix representation
        private bool _isNotKnownToBeIdentity;

        // NOTE: The ctor used to create this identity sets the
        //       _isNotKnownToBeIdentity flag to true (i.e., s_identity
        //       is assumed not to be the identity by methods which
        //       early exit on the identity matrix.)
        //
        //       For performance you should only use s_identity to
        //       initialize a matrix before writing new values.  If
        //       you actually want an identity matrix you should use
        //       "new Matrix3D()" which takes advantage of the various
        //       opitimizations in the identity case.
        //
        private static readonly Matrix3D s_identity = CreateIdentity();

        // The hash code for a matrix is the xor of its element's hashes.
        // Since the identity matrix has 4 1's and 12 0's its hash is 0.
        private const int c_identityHashCode = 0;
    }
}
