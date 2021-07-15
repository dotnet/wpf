// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Font face matching style
//


using System;
using System.Diagnostics;
using System.Security;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;


namespace MS.Internal.FontFace
{
    /// <summary>
    /// Font face style used for style matching of face
    /// </summary>
    internal struct MatchingStyle
    {
        private Vector              _vector;

        // These should be prime numbers.
        private const double FontWeightScale = 5;
        private const double FontStyleScale = 7;
        private const double FontStretchScale = 11;


        internal MatchingStyle(
            FontStyle       style,
            FontWeight      weight,
            FontStretch     stretch
            )
        {
            _vector = new Vector(
                (stretch.ToOpenTypeStretch() - FontStretches.Normal.ToOpenTypeStretch()) * FontStretchScale,
                style.GetStyleForInternalConstruction() * FontStyleScale,
                (weight.ToOpenTypeWeight() - FontWeights.Normal.ToOpenTypeWeight()) / 100.0 * FontWeightScale
                );
        }


        /// <summary>
        /// Equality testing between two matching styles
        /// </summary>
        public static bool operator ==(MatchingStyle l, MatchingStyle r)
        {
            return l._vector == r._vector;
        }

        /// <summary>
        /// Inequality testing between two matching styles
        /// </summary>
        public static bool operator !=(MatchingStyle l, MatchingStyle r)
        {
            return l._vector != r._vector;
        }

        /// <summary>
        /// Equality testing between two matching styles
        /// </summary>
        public override bool Equals(Object o)
        {
            if(o == null)
                return false;

            return o is MatchingStyle && this == (MatchingStyle)o;
}

        /// <summary>
        /// Get hash code for this style
        /// </summary>
        public override int GetHashCode()
        {
            return _vector.GetHashCode();
        }


        /// <summary>
        /// See whether this is a better match to the specified matching target style
        /// </summary>
        /// <param name="target">matching target style</param>
        /// <param name="best">current best match</param>
        /// <param name="matching">matching style</param>
        internal static bool IsBetterMatch(
            MatchingStyle           target,
            MatchingStyle           best,
            ref MatchingStyle       matching
            )
        {
            return matching.IsBetterMatch(target, best);
        }


        /// <summary>
        /// See whether this is a better match to the specified matching target
        /// </summary>
        internal bool IsBetterMatch(
            MatchingStyle   target,
            MatchingStyle   best
            )
        {
            double currentDiffSize = (_vector - target._vector).LengthSquared;
            double bestDiffSize = (best._vector - target._vector).LengthSquared;

            // better match found when...

            if(currentDiffSize < bestDiffSize)
            {
                // the distance from the current vector to target is shorter
                return true;
            }
            else if(currentDiffSize == bestDiffSize)
            {
                double dotCurrent = Vector.DotProduct(_vector, target._vector);
                double dotBest = Vector.DotProduct(best._vector, target._vector);

                if(dotCurrent > dotBest)
                {
                    // when distances are equal, the current vector has a stronger
                    // projection onto target.
                    return true;
                }
                else if(dotCurrent == dotBest)
                {
                    if(     _vector.X > best._vector.X
                        || (    _vector.X == best._vector.X
                            && (    _vector.Y > best._vector.Y
                                || (    _vector.Y == best._vector.Y
                                    &&  _vector.Z > best._vector.Z))))
                    {
                        // when projections onto target are still equally strong, the current
                        // vector has a stronger component.
                        return true;
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// Small subset of 3D-vector implementation for style matching specific use
        /// </summary>
        /// <remarks>
        /// There is a generic Vector class in 3D land, but using it would bring in
        /// many other 3D types which is unnecessary for our limited use of vector.
        ///
        /// Using 3D types here would also mean introducing 3D types to FontCacheService
        /// code which logically should have nothing to do with it.
        /// </remarks>
        private struct Vector
        {
            private double _x, _y, _z;


            /// <summary>
            /// Constructor that sets vector's initial values.
            /// </summary>
            /// <param name="x">Value of the X coordinate of the new vector.</param>
            /// <param name="y">Value of the Y coordinate of the new vector.</param>
            /// <param name="z">Value of the Z coordinate of the new vector.</param>
            internal Vector(double x, double y, double z)
            {
                _x = x;
                _y = y;
                _z = z;
            }

            /// <summary>
            /// Retrieves or sets vector's X value.
            /// </summary>
            internal double X
            {
                get { return _x; }
            }

            /// <summary>
            /// Retrieves or sets vector's Y value.
            /// </summary>
            internal double Y
            {
                get { return _y; }
            }

            /// <summary>
            /// Retrieves or sets vector's Z value.
            /// </summary>
            internal double Z
            {
                get { return _z; }
            }

            /// <summary>
            /// Length of the vector squared.
            /// </summary>
            internal double LengthSquared
            {
                get{ return _x * _x + _y * _y + _z * _z; }
            }


            /// <summary>
            /// Vector dot product.
            /// </summary>
            internal static double DotProduct(Vector l, Vector r)
            {
                return l._x * r._x + l._y * r._y + l._z * r._z;
            }


            /// <summary>
            /// Vector subtraction.
            /// </summary>
            public static Vector operator -(Vector l, Vector r)
            {
                return new Vector(l._x - r._x, l._y - r._y, l._z - r._z);
            }


            /// <summary>
            /// Equality testing between two vectors.
            /// </summary>
            public static bool operator ==(Vector l, Vector r)
            {
                return ((l._x == r._x) && (l._y == r._y) && (l._z == r._z));
            }


            /// <summary>
            /// Inequality testing between two vectors.
            /// </summary>
            public static bool operator !=(Vector l, Vector r)
            {
                return !(l == r);
            }


            /// <summary>
            /// Equality testing between the vector and a given object.
            /// </summary>
            public override bool Equals(Object o)
            {
                if(null == o)
                {
                    return false;
                }

                if(o is Vector)
                {
                    Vector vector = (Vector)o;

                    return (this == vector);
                }
                else
                {
                    return false;
                }
            }


            /// <summary>
            /// Compute a hash code for this vector.
            /// </summary>
            public override int GetHashCode()
            {
                return _x.GetHashCode() ^ _y.GetHashCode() ^ _z.GetHashCode();
            }
        }
    }
}
