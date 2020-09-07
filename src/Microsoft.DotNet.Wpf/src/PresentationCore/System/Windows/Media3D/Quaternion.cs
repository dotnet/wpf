// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: 3D quaternion implementation. 
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht 
//
//
// NOTE:  The field _isNotDistinguishedIdentity is a work-around to the
//        problem that we can't define a default constructor that sets
//        _w to 1.  So the default constructor sets all fields to 0 or
//        false, and we interpret _isNotDistinguishedIdentity as follows
//
//        If false, the quaternion is the identity 0,0,0,1 even though
//        the member fields are 0,0,0,0.
//
//        If true, the quaternion has the value given by its member fields.
// 
//        Don't mess it up!

using MS.Internal;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ComponentModel.Design.Serialization;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
// These types are aliased to match the unamanaged names used in interop
using BOOL = System.Boolean;
using WORD = System.UInt16;
using Float = System.Single;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// Quaternions.
    /// Quaternions are distinctly 3D entities that represent rotation in three dimensions.
    /// Their power comes in being able to interpolate (and thus animate) between 
    /// quaternions to achieve a smooth, reliable interpolation.
    /// The default quaternion is the identity.
    /// </summary>
    public partial struct Quaternion : IFormattable
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor that sets quaternion's initial values.
        /// </summary>
        /// <param name="x">Value of the X coordinate of the new quaternion.</param>
        /// <param name="y">Value of the Y coordinate of the new quaternion.</param>
        /// <param name="z">Value of the Z coordinate of the new quaternion.</param>
        /// <param name="w">Value of the W coordinate of the new quaternion.</param>
        public Quaternion(double x, double y, double z, double w)
        {
            _x = x;
            _y = y;
            _z = z;
            _w = w;
            _isNotDistinguishedIdentity = true;
        }
        
        /// <summary>
        /// Constructs a quaternion via specified axis of rotation and an angle.
        /// Throws an InvalidOperationException if given (0,0,0) as axis vector.
        /// </summary>
        /// <param name="axisOfRotation">Vector representing axis of rotation.</param>
        /// <param name="angleInDegrees">Angle to turn around the given axis (in degrees).</param>
        public Quaternion(Vector3D axisOfRotation, double angleInDegrees)
        {
            angleInDegrees %= 360.0; // Doing the modulo before converting to radians reduces total error
            double angleInRadians = angleInDegrees * (Math.PI / 180.0);
            double length = axisOfRotation.Length;
            if (length == 0)
                throw new System.InvalidOperationException(SR.Get(SRID.Quaternion_ZeroAxisSpecified));
            Vector3D v = (axisOfRotation / length) * Math.Sin(0.5 * angleInRadians);
            _x = v.X;
            _y = v.Y;
            _z = v.Z;
            _w = Math.Cos(0.5 * angleInRadians);
            _isNotDistinguishedIdentity = true;
        }

        #endregion Constructors

        
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods
        /// <summary>
        ///     Identity quaternion
        /// </summary>
        public static Quaternion Identity
        {
            get
            {
                return s_identity;
            }
        }

        /// <summary>
        /// Retrieves quaternion's axis.
        /// </summary>
        public Vector3D Axis 
        {
            // q = M [cos(Q/2), sin(Q /2)v]
            // axis = sin(Q/2)v
            // angle = cos(Q/2)
            // M is magnitude
            get
            {
                // Handle identity (where axis is indeterminate) by
                // returning arbitrary axis.
                if (IsDistinguishedIdentity || (_x == 0 && _y == 0 && _z == 0))
                {
                    return new Vector3D(0,1,0);
                }
                else
                {
                    Vector3D v = new Vector3D(_x, _y, _z);
                    v.Normalize();
                    return v;
                }
            }
        }

        /// <summary>
        /// Retrieves quaternion's angle.
        /// </summary>
        public double Angle
        { 
            get
            {
                if (IsDistinguishedIdentity)
                {
                    return 0;
                }
                
                // Magnitude of quaternion times sine and cosine
                double msin = Math.Sqrt(_x*_x + _y*_y + _z*_z);
                double mcos = _w;
                
                if (!(msin <= Double.MaxValue))
                {
                    // Overflowed probably in squaring, so let's scale
                    // the values.  We don't need to include _w in the
                    // scale factor because we're not going to square
                    // it.
                    double maxcoeff = Math.Max(Math.Abs(_x),Math.Max(Math.Abs(_y),Math.Abs(_z)));
                    double x = _x/maxcoeff;
                    double y = _y/maxcoeff;
                    double z = _z/maxcoeff;
                    msin = Math.Sqrt(x*x + y*y + z*z);
                    // Scale mcos too.
                    mcos = _w/maxcoeff;
                }

                // Atan2 is better than acos.  (More precise and more efficient.)
                return Math.Atan2(msin,mcos) * (360.0 / Math.PI);
            }
        } 

        /// <summary>
        /// Returns whether the quaternion is normalized (i.e. has a magnitude of 1).
        /// </summary>
        public bool IsNormalized 
        { 
            get
            {
                if (IsDistinguishedIdentity)
                {
                    return true;
                }
                double norm2 = _x*_x + _y*_y + _z*_z + _w*_w;
                return DoubleUtil.IsOne(norm2);
            }
        }

        /// <summary>
        /// Tests whether or not a given quaternion is an identity quaternion.
        /// </summary>
        public bool IsIdentity
        {
            get
            {
                return IsDistinguishedIdentity || (_x == 0 && _y == 0 && _z == 0 && _w == 1);
            }
        }

        /// <summary>
        /// Relaces quaternion with its conjugate
        /// </summary>
        public void Conjugate()
        {
            if (IsDistinguishedIdentity)
            {
                return;
            }
            
            // Conjugate([x,y,z,w]) = [-x,-y,-z,w]
            _x = -_x;
            _y = -_y;
            _z = -_z;
        }
        
        /// <summary>
        /// Replaces quaternion with its inverse
        /// </summary>
        public void Invert()
        {
            if (IsDistinguishedIdentity)
            {
                return;
            }
            
            // Inverse = Conjugate / Norm Squared
            Conjugate();
            double norm2 = _x*_x + _y*_y + _z*_z + _w*_w;
            _x /= norm2;
            _y /= norm2;
            _z /= norm2;
            _w /= norm2;
        }
        
        /// <summary>
        /// Normalizes this quaternion.
        /// </summary>
        public void Normalize()
        {
            if (IsDistinguishedIdentity)
            {
                return;
            }
            
            double norm2 = _x*_x + _y*_y + _z*_z + _w*_w;
            if (norm2 > Double.MaxValue)
            {
                // Handle overflow in computation of norm2
                double rmax = 1.0/Max(Math.Abs(_x),
                                      Math.Abs(_y),
                                      Math.Abs(_z),
                                      Math.Abs(_w));
                
                _x *= rmax;
                _y *= rmax;
                _z *= rmax;
                _w *= rmax;
                norm2 = _x*_x + _y*_y + _z*_z + _w*_w;                
            }
            double normInverse = 1.0 / Math.Sqrt(norm2);
            _x *= normInverse;
            _y *= normInverse;
            _z *= normInverse;
            _w *= normInverse;
        }

        /// <summary>
        /// Quaternion addition.
        /// </summary>
        /// <param name="left">First quaternion being added.</param>
        /// <param name="right">Second quaternion being added.</param>
        /// <returns>Result of addition.</returns>
        public static Quaternion operator +(Quaternion left, Quaternion right)
        {
            if (right.IsDistinguishedIdentity)
            {
                if (left.IsDistinguishedIdentity)
                {
                    return new Quaternion(0,0,0,2);
                }
                else
                {
                    // We know left is not distinguished identity here.                    
                    left._w += 1;
                    return left;
                }
            }
            else if (left.IsDistinguishedIdentity)
            {
                // We know right is not distinguished identity here.
                right._w += 1;
                return right;
            }
            else
            {
                return new Quaternion(left._x + right._x,
                                      left._y + right._y,
                                      left._z + right._z,
                                      left._w + right._w);
            }
        }

        /// <summary>
        /// Quaternion addition.
        /// </summary>
        /// <param name="left">First quaternion being added.</param>
        /// <param name="right">Second quaternion being added.</param>
        /// <returns>Result of addition.</returns>
        public static Quaternion Add(Quaternion left, Quaternion right)
        {
            return (left + right);
        }

        /// <summary>
        /// Quaternion subtraction.
        /// </summary>
        /// <param name="left">Quaternion to subtract from.</param>
        /// <param name="right">Quaternion to subtract from the first quaternion.</param>
        /// <returns>Result of subtraction.</returns>
        public static Quaternion operator -(Quaternion left, Quaternion right)
        {
            if (right.IsDistinguishedIdentity)
            {
                if (left.IsDistinguishedIdentity)
                {
                    return new Quaternion(0,0,0,0);
                }
                else
                {
                    // We know left is not distinguished identity here.
                    left._w -= 1;
                    return left;
                }
            }
            else if (left.IsDistinguishedIdentity)
            {
                // We know right is not distinguished identity here.
                return new Quaternion(-right._x, -right._y, -right._z, 1 - right._w);
            }
            else
            {
                return new Quaternion(left._x - right._x,
                                      left._y - right._y,
                                      left._z - right._z,
                                      left._w - right._w);
            }
        }

        /// <summary>
        /// Quaternion subtraction.
        /// </summary>
        /// <param name="left">Quaternion to subtract from.</param>
        /// <param name="right">Quaternion to subtract from the first quaternion.</param>
        /// <returns>Result of subtraction.</returns>
        public static Quaternion Subtract(Quaternion left, Quaternion right)
        {
            return (left - right);
        }

        /// <summary>
        /// Quaternion multiplication.
        /// </summary>
        /// <param name="left">First quaternion.</param>
        /// <param name="right">Second quaternion.</param>
        /// <returns>Result of multiplication.</returns>
        public static Quaternion operator *(Quaternion left, Quaternion right)
        {
            if (left.IsDistinguishedIdentity)
            {
                return right;
            }
            if (right.IsDistinguishedIdentity)
            {
                return left;
            }
            
            double x = left._w * right._x + left._x * right._w + left._y * right._z - left._z * right._y;
            double y = left._w * right._y + left._y * right._w + left._z * right._x - left._x * right._z;
            double z = left._w * right._z + left._z * right._w + left._x * right._y - left._y * right._x;
            double w = left._w * right._w - left._x * right._x - left._y * right._y - left._z * right._z;
            Quaternion result = new Quaternion(x,y,z,w);
            return result;
}

        /// <summary>
        /// Quaternion multiplication.
        /// </summary>
        /// <param name="left">First quaternion.</param>
        /// <param name="right">Second quaternion.</param>
        /// <returns>Result of multiplication.</returns>
        public static Quaternion Multiply(Quaternion left, Quaternion right)
        {
            return left * right;
        }

        /// <summary>
        /// Scale this quaternion by a scalar.
        /// </summary>
        /// <param name="scale">Value to scale by.</param>            
        private void Scale( double scale )
        {
            if (IsDistinguishedIdentity)
            {
                _w = scale;
                IsDistinguishedIdentity = false;
                return;
            }
            _x *= scale;
            _y *= scale;
            _z *= scale;
            _w *= scale;
        }

        /// <summary>
        /// Return length of quaternion.
        /// </summary>
        private double Length()
        {
            if (IsDistinguishedIdentity)
            {
                return 1;
            }
            
            double norm2 = _x*_x + _y*_y + _z*_z + _w*_w;
            if (!(norm2 <= Double.MaxValue))
            {
                // Do this the slow way to avoid squaring large
                // numbers since the length of many quaternions is
                // representable even if the squared length isn't.  Of
                // course some lengths aren't representable because
                // the length can be up to twice as big as the largest
                // coefficient.

                double max = Math.Max(Math.Max(Math.Abs(_x),Math.Abs(_y)),
                                      Math.Max(Math.Abs(_z),Math.Abs(_w)));

                double x = _x/max;
                double y = _y/max;
                double z = _z/max;
                double w = _w/max;

                double smallLength = Math.Sqrt(x*x+y*y+z*z+w*w);
                // Return length of this smaller vector times the scale we applied originally.
                return smallLength * max;
            }
            return Math.Sqrt(norm2);
        }

        /// <summary>
        /// Smoothly interpolate between the two given quaternions using Spherical 
        /// Linear Interpolation (SLERP).
        /// </summary>
        /// <param name="from">First quaternion for interpolation.</param>
        /// <param name="to">Second quaternion for interpolation.</param>
        /// <param name="t">Interpolation coefficient.</param>
        /// <returns>SLERP-interpolated quaternion between the two given quaternions.</returns>
        public static Quaternion Slerp(Quaternion from, Quaternion to, double t)
        {
            return Slerp(from, to, t, /* useShortestPath = */ true);
        }
        
        /// <summary>
        /// Smoothly interpolate between the two given quaternions using Spherical 
        /// Linear Interpolation (SLERP).
        /// </summary>
        /// <param name="from">First quaternion for interpolation.</param>
        /// <param name="to">Second quaternion for interpolation.</param>
        /// <param name="t">Interpolation coefficient.</param>
        /// <param name="useShortestPath">If true, Slerp will automatically flip the sign of
        ///     the destination Quaternion to ensure the shortest path is taken.</param>
        /// <returns>SLERP-interpolated quaternion between the two given quaternions.</returns>
        public static Quaternion Slerp(Quaternion from, Quaternion to, double t, bool useShortestPath)
        {
            if (from.IsDistinguishedIdentity)
            {
                from._w = 1;
            }
            if (to.IsDistinguishedIdentity)
            {
                to._w = 1;
            }
            
            double cosOmega;
            double scaleFrom, scaleTo;

            // Normalize inputs and stash their lengths
            double lengthFrom = from.Length();
            double lengthTo = to.Length();
            from.Scale(1/lengthFrom);
            to.Scale(1/lengthTo);
            
            // Calculate cos of omega.
            cosOmega = from._x*to._x + from._y*to._y + from._z*to._z + from._w*to._w;

            if (useShortestPath)
            {
                // If we are taking the shortest path we flip the signs to ensure that
                // cosOmega will be positive.
                if (cosOmega < 0.0)
                {
                    cosOmega = -cosOmega; 
                    to._x = -to._x;
                    to._y = -to._y;
                    to._z = -to._z;
                    to._w = -to._w;
                }
            }
            else
            {
                // If we are not taking the UseShortestPath we clamp cosOmega to
                // -1 to stay in the domain of Math.Acos below.
                if (cosOmega < -1.0)
                {
                    cosOmega = -1.0;
                }
            }

            // Clamp cosOmega to [-1,1] to stay in the domain of Math.Acos below.
            // The logic above has either flipped the sign of cosOmega to ensure it
            // is positive or clamped to -1 aready.  We only need to worry about the
            // upper limit here.
            if (cosOmega > 1.0)
            {
                cosOmega = 1.0;
            }

            Debug.Assert(!(cosOmega < -1.0) && !(cosOmega > 1.0),
                "cosOmega should be clamped to [-1,1]");

            // The mainline algorithm doesn't work for extreme
            // cosine values.  For large cosine we have a better
            // fallback hence the asymmetric limits.
            const double maxCosine = 1.0 - 1e-6;
            const double minCosine = 1e-10 - 1.0;

            // Calculate scaling coefficients.
            if (cosOmega > maxCosine)
            {        
                // Quaternions are too close - use linear interpolation.
                scaleFrom = 1.0 - t;
                scaleTo = t;
            }
            else if (cosOmega < minCosine)
            {
                // Quaternions are nearly opposite, so we will pretend to 
                // is exactly -from.
                // First assign arbitrary perpendicular to "to".
                to = new Quaternion(-from.Y, from.X, -from.W, from.Z);

                double theta = t * Math.PI;
                
                scaleFrom = Math.Cos(theta);
                scaleTo = Math.Sin(theta);
            }
            else
            {
                // Standard case - use SLERP interpolation.
                double omega = Math.Acos(cosOmega);
                double sinOmega = Math.Sqrt(1.0 - cosOmega*cosOmega);
                scaleFrom = Math.Sin((1.0 - t) * omega) / sinOmega;
                scaleTo = Math.Sin(t * omega) / sinOmega;
            }

            // We want the magnitude of the output quaternion to be
            // multiplicatively interpolated between the input
            // magnitudes, i.e. lengthOut = lengthFrom * (lengthTo/lengthFrom)^t
            //                            = lengthFrom ^ (1-t) * lengthTo ^ t

            double lengthOut = lengthFrom * Math.Pow(lengthTo/lengthFrom, t);
            scaleFrom *= lengthOut;
            scaleTo *= lengthOut;

            return new Quaternion(scaleFrom*from._x + scaleTo*to._x,
                                  scaleFrom*from._y + scaleTo*to._y,
                                  scaleFrom*from._z + scaleTo*to._z,
                                  scaleFrom*from._w + scaleTo*to._w);
        }

        #endregion Public Methods

        #region Private Methods
            
        static private double Max(double a, double b, double c, double d)
        {
            if (b > a)
                a = b;
            if (c > a)
                a = c;
            if (d > a)
                a = d;
            return a;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// X - Default value is 0.
        /// </summary>
        public double X
        {
            get
            {
                return _x;
            }

            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _x = value;
            }
        }

        /// <summary>
        /// Y - Default value is 0.
        /// </summary>
        public double Y
        {
            get
            {
                return _y;
            }

            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _y = value;
            }
        }

        /// <summary>
        /// Z - Default value is 0.
        /// </summary>
        public double Z
        {
            get
            {
                return _z;
            }

            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _z = value;
            }
        }

        /// <summary>
        /// W - Default value is 1.
        /// </summary>
        public double W
        {
            get
            {
                if (IsDistinguishedIdentity)
                {
                    return 1.0;
                }
                else
                {
                    return _w;
                }
            }
            
            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _w = value;
            }
        }

        #endregion Public Properties
        
        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        internal double _x;
        internal double _y;
        internal double _z;
        internal double _w;

        #endregion Internal Fields

        #region Private Fields and Properties
            
        // If this bool is false then we are a default quaternion with
        // all doubles equal to zero, but should be treated as
        // identity.
        private bool _isNotDistinguishedIdentity;

        private bool IsDistinguishedIdentity
        {
            get 
            {
                return !_isNotDistinguishedIdentity;
            }
            set
            {
                _isNotDistinguishedIdentity = !value;
            }
        }

        private static int GetIdentityHashCode()
        {
            // This code is called only once.
            double zero = 0;
            double one = 1;
            // return zero.GetHashCode() ^ zero.GetHashCode() ^ zero.GetHashCode() ^ one.GetHashCode();
            // But this expression can be simplified because the first two hash codes cancel.
            return zero.GetHashCode() ^ one.GetHashCode();
        }

        private static Quaternion GetIdentity()
        {
            // This code is called only once.
            Quaternion q = new Quaternion(0,0,0,1);
            q.IsDistinguishedIdentity = true;
            return q;
        }
        

        // Hash code for identity.
        private static int c_identityHashCode = GetIdentityHashCode();

        // Default identity
        private static Quaternion s_identity = GetIdentity();

        #endregion Private Fields and Properties
    }
}
