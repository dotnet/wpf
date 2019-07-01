// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Collections.Generic;

using Microsoft.Test.Graphics.TestTypes;
using Microsoft.Test.Graphics.Factories;

namespace Microsoft.Test.Graphics
{    
    /// <summary>
    /// Extra Math functions that are useful for 3D
    /// </summary>
    public class MathEx
    {
        /// <summary>
        /// This value comes from sdk\inc\crt\float.h
        /// smallest such that 1.0 + eps != 1.0
        /// </summary>
        public const double Epsilon = 2.2204460492503131e-016;

        /// <summary/>
        public const double DefaultNumericalPrecisionTolerance = 0.00000000001; // 1.0E-11

        private static double numericalPrecisionTolerance = DefaultNumericalPrecisionTolerance;

        /// <summary/>
        public static double NumericalPrecisionTolerance
        {
            get { return numericalPrecisionTolerance; }
            set { numericalPrecisionTolerance = value; }
        }

        private static double dpiConversionX;
        private static double dpiConversionY;

        static MathEx()
        {
            // Some magic to make DPI work on Vista.  This is a no-op for XP.
            Interop.MakeProcessDpiAware();

            // Grab the desktop's DC and query the DPI it renders at
            IntPtr hdc = Interop.GetDC(IntPtr.Zero);
            dpiConversionX = Interop.GetDpiX(hdc);
            dpiConversionY = Interop.GetDpiY(hdc);

            dpiConversionX /= 96.0;
            dpiConversionY /= 96.0;

            Interop.ReleaseDC(IntPtr.Zero, hdc);
        }

        #region                 Are/NotCloseEnough
        //
        // Relativistic equality comparison for two doubles.
        // The tolerance for error increases with the size of the two numbers compared.
        // Special consideration for numbers such as Infinity and NaN are also taken into account.
        //
        // The relativistic tolerance can be changed by modifying MathEx.NumericalPrecisionTolerance.
        //

        /// <summary/>
        public static bool AreCloseEnough(double d1, double d2)
        {
            // Special cases ( NaN, +/-Infinity )
            bool specialAnswer;
            if (IsSpecialComparison(d1, d2, out specialAnswer))
            {
                return specialAnswer;
            }

            // Early-out -- this is obvious
            if (d1 == d2)
            {
                return true;
            }

            // Zero-values will disallow computing a ratio.
            // Just make sure the other number is within tolerance of 0.
            if (d1 == 0)
            {
                return (-numericalPrecisionTolerance < d2 && d2 < numericalPrecisionTolerance);
            }
            if (d2 == 0)
            {
                return (-numericalPrecisionTolerance < d1 && d1 < numericalPrecisionTolerance);
            }

            // ratio will always be less than 1.
            // Return true if the ratio is barely less than 1 (defined by tolerance value)

            double ratio = (Math.Abs(d1) < Math.Abs(d2)) ? d1 / d2 : d2 / d1;
            return (1 - numericalPrecisionTolerance < ratio);
        }

        private static bool IsSpecialComparison(double d1, double d2, out bool areEqual)
        {
            if ((double.IsPositiveInfinity(d1) && double.IsPositiveInfinity(d2)) ||
                 (double.IsNegativeInfinity(d1) && double.IsNegativeInfinity(d2)) ||
                 (double.IsNaN(d1) && double.IsNaN(d2)))
            {
                areEqual = true;
                return true;
            }

            areEqual = false;
            return double.IsInfinity(d1) || double.IsInfinity(d2) || double.IsNaN(d1) || double.IsNaN(d2);
        }

        /// <summary/>
        public static bool AreCloseEnough(Matrix m1, Matrix m2)
        {
            return AreCloseEnough(m1.M11, m2.M11) &&
                    AreCloseEnough(m1.M12, m2.M12) &&
                    AreCloseEnough(m1.M21, m2.M21) &&
                    AreCloseEnough(m1.M22, m2.M22) &&
                    AreCloseEnough(m1.OffsetX, m2.OffsetX) &&
                    AreCloseEnough(m1.OffsetY, m2.OffsetY);
        }

        /// <summary/>
        public static bool AreCloseEnough(Matrix3D m1, Matrix3D m2)
        {
            return AreCloseEnough(m1.M11, m2.M11) &&
                    AreCloseEnough(m1.M12, m2.M12) &&
                    AreCloseEnough(m1.M13, m2.M13) &&
                    AreCloseEnough(m1.M14, m2.M14) &&
                    AreCloseEnough(m1.M21, m2.M21) &&
                    AreCloseEnough(m1.M22, m2.M22) &&
                    AreCloseEnough(m1.M23, m2.M23) &&
                    AreCloseEnough(m1.M24, m2.M24) &&
                    AreCloseEnough(m1.M31, m2.M31) &&
                    AreCloseEnough(m1.M32, m2.M32) &&
                    AreCloseEnough(m1.M33, m2.M33) &&
                    AreCloseEnough(m1.M34, m2.M34) &&
                    AreCloseEnough(m1.OffsetX, m2.OffsetX) &&
                    AreCloseEnough(m1.OffsetY, m2.OffsetY) &&
                    AreCloseEnough(m1.OffsetZ, m2.OffsetZ) &&
                    AreCloseEnough(m1.M44, m2.M44);
        }

        /// <summary/>
        public static bool AreCloseEnough(Point p1, Point p2)
        {
            return AreCloseEnough(p1.X, p2.X) && AreCloseEnough(p1.Y, p2.Y);
        }

        /// <summary/>
        public static bool AreCloseEnough(Point[] p1, Point[] p2)
        {
            if (p1 == null)
            {
                return (p2 == null);
            }
            if (p2 == null)
            {
                return (p1 == null);
            }
            if (p1.Length != p2.Length)
            {
                return false;
            }
            else // the same size
            {
                for (int i = 0; i < p1.Length; i++)
                {
                    if (!AreCloseEnough(p1[i], p2[i]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary/>
        public static bool AreCloseEnough(Point3D p1, Point3D p2)
        {
            return AreCloseEnough(p1.X, p2.X) && AreCloseEnough(p1.Y, p2.Y) &&
                    AreCloseEnough(p1.Z, p2.Z);
        }

        /// <summary/>
        public static bool AreCloseEnough(Point4D p1, Point4D p2)
        {
            return AreCloseEnough(p1.X, p2.X) && AreCloseEnough(p1.Y, p2.Y) &&
                    AreCloseEnough(p1.Z, p2.Z) && AreCloseEnough(p1.W, p2.W);
        }

        /// <summary/>
        public static bool AreCloseEnough(Quaternion q1, Quaternion q2)
        {
            return AreCloseEnough(q1.X, q2.X) && AreCloseEnough(q1.Y, q2.Y) &&
                    AreCloseEnough(q1.Z, q2.Z) && AreCloseEnough(q1.W, q2.W);
        }

        /// <summary/>
        public static bool AreCloseEnough(Rect r1, Rect r2)
        {
            return AreCloseEnough(r1.X, r2.X) && AreCloseEnough(r1.Width, r2.Width) &&
                    AreCloseEnough(r1.Y, r2.Y) && AreCloseEnough(r1.Height, r2.Height);
        }

        /// <summary/>
        public static bool AreCloseEnough(Rect3D r1, Rect3D r2)
        {
            return AreCloseEnough(r1.X, r2.X) && AreCloseEnough(r1.SizeX, r2.SizeX) &&
                    AreCloseEnough(r1.Y, r2.Y) && AreCloseEnough(r1.SizeY, r2.SizeY) &&
                    AreCloseEnough(r1.Z, r2.Z) && AreCloseEnough(r1.SizeZ, r2.SizeZ);
        }

        /// <summary/>
        public static bool AreCloseEnough(Size s1, Size s2)
        {
            return AreCloseEnough(s1.Width, s2.Width) && AreCloseEnough(s1.Height, s2.Height);
        }

        /// <summary/>
        public static bool AreCloseEnough(Size3D s1, Size3D s2)
        {
            return AreCloseEnough(s1.X, s2.X) && AreCloseEnough(s1.Y, s2.Y) && AreCloseEnough(s1.Z, s2.Z);
        }

        /// <summary/>
        public static bool AreCloseEnough(Vector v1, Vector v2)
        {
            return AreCloseEnough(v1.X, v2.X) && AreCloseEnough(v1.Y, v2.Y);
        }

        /// <summary/>
        public static bool AreCloseEnough(Vector[] v1, Vector[] v2)
        {
            if (v1 == null)
            {
                return (v2 == null);
            }
            if (v2 == null)
            {
                return (v1 == null);
            }
            if (v1.Length != v2.Length)
            {
                return false;
            }
            else // the same size
            {
                for (int i = 0; i < v1.Length; i++)
                {
                    if (!AreCloseEnough(v1[i], v2[i]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary/>
        public static bool AreCloseEnough(Vector3D v1, Vector3D v2)
        {
            return AreCloseEnough(v1.X, v2.X) && AreCloseEnough(v1.Y, v2.Y) && AreCloseEnough(v1.Z, v2.Z);
        }

        /// <summary/>
        public static bool NotCloseEnough(double d1, double d2)
        {
            return !AreCloseEnough(d1, d2);
        }

        /// <summary/>
        public static bool NotCloseEnough(Point3D p1, Point3D p2)
        {
            return !AreCloseEnough(p1, p2);
        }

        /// <summary/>
        public static bool NotCloseEnough(Vector3D v1, Vector3D v2)
        {
            return !AreCloseEnough(v1, v2);
        }

        /// <summary/>
        public static bool NotCloseEnough(Size3D s1, Size3D s2)
        {
            return !AreCloseEnough(s1, s2);
        }

        /// <summary/>
        public static bool NotCloseEnough(Rect r1, Rect r2)
        {
            return !AreCloseEnough(r1, r2);
        }

        /// <summary/>
        public static bool NotCloseEnough(Rect3D r1, Rect3D r2)
        {
            return !AreCloseEnough(r1, r2);
        }

        /// <summary/>
        public static bool NotCloseEnough(Quaternion q1, Quaternion q2)
        {
            return !AreCloseEnough(q1, q2);
        }

        /// <summary/>
        public static bool NotCloseEnough(Point4D p1, Point4D p2)
        {
            return !AreCloseEnough(p1, p2);
        }

        /// <summary/>
        public static bool NotCloseEnough(Matrix m1, Matrix m2)
        {
            return !AreCloseEnough(m1, m2);
        }

        /// <summary/>
        public static bool NotCloseEnough(Matrix3D m1, Matrix3D m2)
        {
            return !AreCloseEnough(m1, m2);
        }

        #endregion

        #region                 [Not]Equals
        //
        // Equality comparison for two doubles.
        // Mimics the standard operator == but has the added bonus that
        //  special consideration for Infinity and NaN is taken into account.
        //

        /// <summary/>
        public static bool Equals(double d1, double d2)
        {
            bool specialAnswer;
            if (IsSpecialComparison(d1, d2, out specialAnswer))
            {
                return specialAnswer;
            }
            return d1 == d2;
        }

        /// <summary/>
        public static bool Equals(Matrix m1, Matrix m2)
        {
            return Equals(m1.M11, m2.M11) &&
                    Equals(m1.M12, m2.M12) &&
                    Equals(m1.M21, m2.M21) &&
                    Equals(m1.M22, m2.M22) &&
                    Equals(m1.OffsetX, m2.OffsetX) &&
                    Equals(m1.OffsetY, m2.OffsetY);
        }

        /// <summary/>
        public static bool Equals(Matrix3D m1, Matrix3D m2)
        {
            return Equals(m1.M11, m2.M11) &&
                    Equals(m1.M12, m2.M12) &&
                    Equals(m1.M13, m2.M13) &&
                    Equals(m1.M14, m2.M14) &&
                    Equals(m1.M21, m2.M21) &&
                    Equals(m1.M22, m2.M22) &&
                    Equals(m1.M23, m2.M23) &&
                    Equals(m1.M24, m2.M24) &&
                    Equals(m1.M31, m2.M31) &&
                    Equals(m1.M32, m2.M32) &&
                    Equals(m1.M33, m2.M33) &&
                    Equals(m1.M34, m2.M34) &&
                    Equals(m1.OffsetX, m2.OffsetX) &&
                    Equals(m1.OffsetY, m2.OffsetY) &&
                    Equals(m1.OffsetZ, m2.OffsetZ) &&
                    Equals(m1.M44, m2.M44);
        }

        /// <summary/>
        public static bool Equals(Point p1, Point p2)
        {
            return Equals(p1.X, p2.X) && Equals(p1.Y, p2.Y);
        }

        /// <summary/>
        public static bool Equals(double p1x, double p1y, Point p2)
        {
            return Equals(p1x, p2.X) && Equals(p1y, p2.Y);
        }

        /// <summary/>
        public static bool Equals(Rect r1, Rect r2)
        {
            return Equals(r1.X, r2.X) && Equals(r1.Y, r2.Y) && Equals(r1.Width, r2.Width) && Equals(r1.Height, r2.Height);
        }

        /// <summary/>
        public static bool Equals(Size s1, Size s2)
        {
            return Equals(s1.Width, s2.Width) && Equals(s1.Height, s2.Height);
        }

        /// <summary/>
        public static bool Equals(Vector v1, Vector v2)
        {
            return Equals(v1.X, v2.X) && Equals(v1.Y, v2.Y);
        }

        /// <summary/>
        public static bool NotEquals(bool b1, bool b2)
        {
            return b1 != b2;
        }

        /// <summary/>
        public static bool NotEquals(double d1, double d2)
        {
            return !Equals(d1, d2);
        }

        /// <summary/>
        public static bool NotEquals(DoubleCollection dc1, DoubleCollection dc2)
        {
            if (dc1.Count != dc2.Count)
            {
                return true;
            }
            for (int i = 0; i < dc1.Count; i++)
            {
                if (NotEquals(dc1[i], dc2[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary/>
        public static bool NotEquals(Color c1, Color c2)
        {
            return c1.A != c2.A || c1.R != c2.R || c1.G != c2.G || c1.B != c2.B;
        }

        /// <summary/>
        public static bool NotEquals(Point p1, Point p2)
        {
            return !(Equals(p1.X, p2.X) && Equals(p1.Y, p2.Y));
        }

        /// <summary/>
        public static bool NotEquals(Point3D p1, Point3D p2)
        {
            return !(Equals(p1.X, p2.X) && Equals(p1.Y, p2.Y) && Equals(p1.Z, p2.Z));
        }

        /// <summary/>
        public static bool NotEquals(Point3DCollection c1, Point3DCollection c2)
        {
            if (c1.Count != c2.Count)
            {
                return true;
            }
            for (int i = 0; i < c1.Count; i++)
            {
                if (NotEquals(c1[i], c2[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary/>
        public static bool NotEquals(Vector3D v1, Vector3D v2)
        {
            return !(Equals(v1.X, v2.X) && Equals(v1.Y, v2.Y) && Equals(v1.Z, v2.Z));
        }

        /// <summary/>
        public static bool NotEquals(Vector3DCollection c1, Vector3DCollection c2)
        {
            if (c1.Count != c2.Count)
            {
                return true;
            }
            for (int i = 0; i < c1.Count; i++)
            {
                if (NotEquals(c1[i], c2[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary/>
        public static bool NotEquals(Size s1, Size s2)
        {
            return !(Equals(s1.Width, s2.Width) && Equals(s1.Height, s2.Height));
        }

        /// <summary/>
        public static bool NotEquals(Size3D s1, Size3D s2)
        {
            return !(Equals(s1.X, s2.X) && Equals(s1.Y, s2.Y) && Equals(s1.Z, s2.Z));
        }

        /// <summary/>
        public static bool NotEquals(Rect r1, Rect r2)
        {
            return NotEquals(r1.Location, r2.Location) || NotEquals(r1.Size, r2.Size);
        }

        /// <summary/>
        public static bool NotEquals(Rect3D r1, Rect3D r2)
        {
            return NotEquals(r1.Location, r2.Location) || NotEquals(r1.Size, r2.Size);
        }

        /// <summary/>
        public static bool NotEquals(Quaternion q1, Quaternion q2)
        {
            return !(Equals(q1.X, q2.X) &&
                      Equals(q1.Y, q2.Y) &&
                      Equals(q1.Z, q2.Z) &&
                      Equals(q1.W, q2.W));
        }

        /// <summary/>
        public static bool NotEquals(Point4D p1, Point4D p2)
        {
            return !(Equals(p1.X, p2.X) &&
                      Equals(p1.Y, p2.Y) &&
                      Equals(p1.Z, p2.Z) &&
                      Equals(p1.W, p2.W));
        }

        /// <summary/>
        public static bool NotEquals(Matrix3D m1, Matrix3D m2)
        {
            return !Equals(m1.M11, m2.M11);
        }

        #endregion

        #region                 ClrOperator[Not]Equals
        //
        // Wrappers for operator ==
        // No consideration for Infinity and NaN is taken into account.
        //
        // These exist because we don't want to trust the built in equality operators for WPF types in our unit tests
        //
        /// <summary/>
        public static bool ClrOperatorEquals(double d1, double d2)
        {
            return d1 == d2;
        }

        /// <summary/>
        public static bool ClrOperatorEquals(Matrix3D m1, Matrix3D m2)
        {
            return ClrOperatorEquals(m1.M11, m2.M11) &&
                    ClrOperatorEquals(m1.M12, m2.M12) &&
                    ClrOperatorEquals(m1.M13, m2.M13) &&
                    ClrOperatorEquals(m1.M14, m2.M14) &&
                    ClrOperatorEquals(m1.M21, m2.M21) &&
                    ClrOperatorEquals(m1.M22, m2.M22) &&
                    ClrOperatorEquals(m1.M23, m2.M23) &&
                    ClrOperatorEquals(m1.M24, m2.M24) &&
                    ClrOperatorEquals(m1.M31, m2.M31) &&
                    ClrOperatorEquals(m1.M32, m2.M32) &&
                    ClrOperatorEquals(m1.M33, m2.M33) &&
                    ClrOperatorEquals(m1.M34, m2.M34) &&
                    ClrOperatorEquals(m1.OffsetX, m2.OffsetX) &&
                    ClrOperatorEquals(m1.OffsetY, m2.OffsetY) &&
                    ClrOperatorEquals(m1.OffsetZ, m2.OffsetZ) &&
                    ClrOperatorEquals(m1.M44, m2.M44);
        }

        /// <summary/>
        public static bool ClrOperatorEquals(Rect r1, Rect r2)
        {
            return ClrOperatorEquals(r1.X, r2.X) && ClrOperatorEquals(r1.Y, r2.Y) &&
                   ClrOperatorEquals(r1.Width, r2.Width) && ClrOperatorEquals(r1.Height, r2.Height);
        }

        /// <summary/>
        public static bool ClrOperatorNotEquals(double d1, double d2)
        {
            return !ClrOperatorEquals(d1, d2);
        }

        /// <summary/>
        public static bool ClrOperatorNotEquals(Point3D p1, Point3D p2)
        {
            return !(ClrOperatorEquals(p1.X, p2.X) && ClrOperatorEquals(p1.Y, p2.Y) && ClrOperatorEquals(p1.Z, p2.Z));
        }

        /// <summary/>
        public static bool ClrOperatorNotEquals(Vector3D v1, Vector3D v2)
        {
            return !(ClrOperatorEquals(v1.X, v2.X) && ClrOperatorEquals(v1.Y, v2.Y) && ClrOperatorEquals(v1.Z, v2.Z));
        }

        /// <summary/>
        public static bool ClrOperatorNotEquals(Size3D s1, Size3D s2)
        {
            return !(ClrOperatorEquals(s1.X, s2.X) && ClrOperatorEquals(s1.Y, s2.Y) && ClrOperatorEquals(s1.Z, s2.Z));
        }

        /// <summary/>
        public static bool ClrOperatorNotEquals(Rect r1, Rect r2)
        {
            return !ClrOperatorEquals(r1, r2);
        }

        /// <summary/>
        public static bool ClrOperatorNotEquals(Rect3D r1, Rect3D r2)
        {
            return ClrOperatorNotEquals(r1.Location, r2.Location) || ClrOperatorNotEquals(r1.Size, r2.Size);
        }

        /// <summary/>
        public static bool ClrOperatorNotEquals(Quaternion q1, Quaternion q2)
        {
            return !(ClrOperatorEquals(q1.X, q2.X) &&
                      ClrOperatorEquals(q1.Y, q2.Y) &&
                      ClrOperatorEquals(q1.Z, q2.Z) &&
                      ClrOperatorEquals(q1.W, q2.W));
        }

        /// <summary/>
        public static bool ClrOperatorNotEquals(Point4D p1, Point4D p2)
        {
            return !(ClrOperatorEquals(p1.X, p2.X) &&
                      ClrOperatorEquals(p1.Y, p2.Y) &&
                      ClrOperatorEquals(p1.Z, p2.Z) &&
                      ClrOperatorEquals(p1.W, p2.W));
        }

        /// <summary/>
        public static bool ClrOperatorNotEquals(Matrix3D m1, Matrix3D m2)
        {
            return !ClrOperatorEquals(m1.M11, m2.M11);
        }

        #endregion

        /// <summary>
        /// Return a fallback value if the double checked is NaN.
        /// Note that this method may return NaN if the fallback value is NaN
        /// </summary>
        public static double FallbackIfNaN(double value, double fallback)
        {
            if (double.IsNaN(value))
            {
                return fallback;
            }
            return value;
        }

        /// <summary>
        /// Determine if one Rect contains another, ignoring precision issues of up to one pixel center.
        /// If the two Rects are both Empty, this method returns true.
        /// If only one is Empty, this method returns false.
        /// </summary>
        public static bool ContainsCloseEnough(Rect bigger, Rect smaller)
        {
            if (bigger.IsEmpty && smaller.IsEmpty)
            {
                return true;
            }
            if (bigger.IsEmpty || smaller.IsEmpty)
            {
                return false;
            }

            // They can be off by up to one pixel center
            bool lCheck = (bigger.Left < smaller.Left || Math.Abs(bigger.Left - smaller.Left) < Const.pixelCenterX);
            bool tCheck = (bigger.Top < smaller.Top || Math.Abs(bigger.Top - smaller.Top) < Const.pixelCenterY);
            bool rCheck = (bigger.Right > smaller.Right || Math.Abs(bigger.Right - smaller.Right) < Const.pixelCenterX);
            bool bCheck = (bigger.Bottom > smaller.Bottom || Math.Abs(bigger.Bottom - smaller.Bottom) < Const.pixelCenterY);

            return lCheck && tCheck && rCheck && bCheck;
        }

        /// <summary/>
        public static Rect InflateToIntegerBounds(Rect rect)
        {
            double left = Math.Floor(rect.Left);
            double top = Math.Floor(rect.Top);
            double right = Math.Ceiling(rect.Right);
            double bottom = Math.Ceiling(rect.Bottom);

            return new Rect(left, top, right - left, bottom - top);
        }

        /// <summary/>
        public static Rect Inflate(Rect rect, double scale)
        {
            if (scale < 0)
            {
                throw new ArgumentException("scale cannot be less than 0");
            }
            return new Rect(rect.X - scale, rect.Y - scale, rect.Width + scale + scale, rect.Height + scale + scale);
        }

        /// <summary>
        /// Determine whether the two doubles have the same sign.
        /// 0 is defined as positive for this method.
        /// </summary>
        public static bool SameSign(double d1, double d2)
        {
            return (d1 < 0) ? d2 < 0 : 0 <= d2;
        }

        /// <summary/>
        public static bool IsIdentity(Quaternion q)
        {
            return q.X == 0 && q.Y == 0 && q.Z == 0 && q.W == 1;
        }

        /// <summary>
        /// This is strictly a numerical comparison, so a NaN in either Point3D will make this false
        /// </summary>
        public static bool LessThanOrEquals(Point3D p1, Point3D p2)
        {
            if (double.IsNaN(p1.X) || double.IsNaN(p1.Y) || double.IsNaN(p1.Z) ||
                 double.IsNaN(p2.X) || double.IsNaN(p2.Y) || double.IsNaN(p2.Z))
            {
                return false;
            }
            return !NotEquals(p1, p2) || (p1.X < p2.X && p1.Y < p2.Y && p1.Z < p2.Z);
        }

        /// <summary/>
        public static double ToDegrees(double radians)
        {
            return radians / Math.PI * 180.0;
        }

        /// <summary/>
        public static double ToRadians(double degrees)
        {
            return degrees / 180.0 * Math.PI;
        }

        /// <summary/>
        public static Point3D DivideByW(Point4D point)
        {
            if (point.W == 1.0)
            {
                return new Point3D(point.X, point.Y, point.Z);
            }
            return new Point3D(point.X / point.W, point.Y / point.W, point.Z / point.W);
        }

        /// <summary/>
        public static Point3D WeightedSum(Point3D p1, double s1, Point3D p2, double s2, Point3D p3, double s3)
        {
            return MathEx.Add(MathEx.Multiply(p1, s1),
                               MathEx.Multiply(p2, s2),
                               MathEx.Multiply(p3, s3));
        }

        /// <summary/>
        public static Point3D Multiply(Point3D point, double scalar)
        {
            return new Point3D(point.X * scalar, point.Y * scalar, point.Z * scalar);
        }

        /// <summary/>
        public static Quaternion Multiply(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(q1.W * q2.X + q1.X * q2.W + q1.Y * q2.Z - q1.Z * q2.Y,
                                    q1.W * q2.Y - q1.X * q2.Z + q1.Y * q2.W + q1.Z * q2.X,
                                    q1.W * q2.Z + q1.X * q2.Y - q1.Y * q2.X + q1.Z * q2.W,
                                    q1.W * q2.W - q1.X * q2.X - q1.Y * q2.Y - q1.Z * q2.Z);
        }

        /// <summary/>
        public static Point3D Add(params Point3D[] points)
        {
            if (points == null || points.Length < 1)
            {
                throw new ArgumentException("Cannot add nothing.");
            }
            Point3D result = new Point3D();
            foreach (Point3D p in points)
            {
                result.X += p.X;
                result.Y += p.Y;
                result.Z += p.Z;
            }
            return result;
        }

        /// <summary/>
        public static Vector3D CrossProduct(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(
                            v1.Y * v2.Z - v1.Z * v2.Y,
                            v1.Z * v2.X - v1.X * v2.Z,
                            v1.X * v2.Y - v1.Y * v2.X
                            );
        }

        /// <summary/>
        public static double DotProduct(Vector v1, Vector v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y;
        }

        /// <summary/>
        public static double DotProduct(Vector3D v1, Vector3D v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        /// <summary/>
        public static double DotProduct(Quaternion q1, Quaternion q2)
        {
            return q1.X * q2.X + q1.Y * q2.Y + q1.Z * q2.Z + q1.W * q2.W;
        }

        /// <summary/>
        public static double AngleBetween(Vector v1, Vector v2)
        {
            // Cos(t) = norn(u) . norm(v)

            v1 = Normalize(v1);
            v2 = Normalize(v2);
            double dotProduct = DotProduct(v1, v2);
            if (double.IsNaN(dotProduct))
            {
                return double.NaN;
            }
            if (Math.Abs(dotProduct) > 1.0)
            {
                // Set it to +/-1 if it is out of range for Math.Acos
                dotProduct /= Math.Abs(dotProduct);
            }
            double theta = Math.Acos(dotProduct);

            return ToDegrees(theta);
        }

        /// <summary/>
        public static double AngleBetween(Vector3D v1, Vector3D v2)
        {
            //Removed old Acos(v1 dot v2) based angle calculation for determining angle to avoid loss of precision

            v1 = Normalize(v1);
            v2 = Normalize(v2);

            double dotProduct = DotProduct(v1, v2);
            if (double.IsNaN(dotProduct))
            {
                return double.NaN;
            }

            double theta = 0;
            if (dotProduct < 0.0)
            {
                theta = Math.PI - 2 * Math.Asin(Length(-v2 - v1) / 2);
            }
            else
            {
                theta = 2 * Math.Asin(Length(v2 - v1) / 2);
            }

            return ToDegrees(theta);
        }

        /// <summary/>
        public static Vector Normalize(Vector v)
        {
            double x = v.X;
            double y = v.Y;

            if (double.IsNaN(x) || double.IsNaN(y))
            {
                return new Vector(Const.nan, Const.nan);
            }

            return NormalizeInternal(x, y);
        }

        /// <summary/>
        public static Vector3D Normalize(Vector3D v)
        {
            double x = v.X;
            double y = v.Y;
            double z = v.Z;

            if (double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(z))
            {
                return Const.vNaN;
            }

            return NormalizeInternal(x, y, z);
        }

        private static Vector NormalizeInternal(double x, double y)
        {
            double length = Length(x, y);
            if (length == 0.0)
            {
                // Following Avalon's expected behavior
                return new Vector(Const.nan, Const.nan);
            }

            // Overflow in length.  Scale the vector and try again.
            if (double.IsInfinity(length))
            {
                double scale = AbsMax(x, y, 0, 0);
                return NormalizeInternal(x / scale, y / scale);
            }
            return new Vector(x / length, y / length);
        }

        private static Vector3D NormalizeInternal(double x, double y, double z)
        {
            double length = Length(x, y, z);
            if (length == 0.0)
            {
                // Following Avalon's expected behavior
                return Const.vNaN;
            }

            // Overflow in length.  Scale the vector and try again.
            if (double.IsInfinity(length))
            {
                double scale = AbsMax(x, y, z, 0);
                return NormalizeInternal(x / scale, y / scale, z / scale);
            }
            return new Vector3D(x / length, y / length, z / length);
        }

        /// <summary/>
        public static Quaternion Normalize(Quaternion q)
        {
            double x = q.X;
            double y = q.Y;
            double z = q.Z;
            double w = q.W;

            if (double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(z) || double.IsNaN(w))
            {
                return Const.qNaN;
            }

            return NormalizeInternal(x, y, z, w);
        }

        private static Quaternion NormalizeInternal(double x, double y, double z, double w)
        {
            double length = Length(x, y, z, w);
            if (length == 0.0)
            {
                return Const.qNaN;
            }

            // Overflow in length.  Scale the quaternion and try again.
            if (double.IsInfinity(length))
            {
                double scale = AbsMax(x, y, z, w);
                return NormalizeInternal(x / scale, y / scale, z / scale, w / scale);
            }
            return new Quaternion(x / length, y / length, z / length, w / length);
        }

        /// <summary/>
        public static double Length(Vector v)
        {
            return Length(v.X, v.Y, 0, 0);
        }

        /// <summary/>
        public static double Length(Vector3D v)
        {
            return Length(v.X, v.Y, v.Z, 0);
        }

        /// <summary/>
        public static double Length(double x, double y)
        {
            return Length(x, y, 0, 0);
        }

        /// <summary/>
        public static double Length(double x, double y, double z)
        {
            return Length(x, y, z, 0);
        }

        /// <summary/>
        public static double Length(double x, double y, double z, double w)
        {
            double lengthSq = LengthSquared(x, y, z, w);
            if (double.IsInfinity(lengthSq))
            {
                // For overflow cases:
                //  - scale the 4-tuple by some factor (absolute value of largest component in 4-tuple)
                //  - find its length
                //  - then multiply the scale factor back in

                double scale = AbsMax(x, y, z, w);
                if (double.IsInfinity(scale))
                {
                    return scale;
                }
                return Length(x / scale, y / scale, z / scale, w / scale) * scale;
            }
            return Math.Sqrt(lengthSq);
        }

        private static double AbsMax(params double[] args)
        {
            if (args == null || args.Length < 1)
            {
                throw new ArgumentException("Cannot find the maximum of nothing.");
            }

            double currentMaximum = 0.0;
            foreach (double value in args)
            {
                if (Math.Abs(value) > currentMaximum)
                {
                    currentMaximum = Math.Abs(value);
                }
            }
            return currentMaximum;
        }

        /// <summary/>
        public static double LengthSquared(Vector v)
        {
            return LengthSquared(v.X, v.Y, 0, 0);
        }

        /// <summary/>
        public static double LengthSquared(Vector3D v)
        {
            return LengthSquared(v.X, v.Y, v.Z, 0);
        }

        /// <summary/>
        public static double LengthSquared(double x, double y, double z)
        {
            return LengthSquared(x, y, z, 0);
        }

        /// <summary/>
        public static double LengthSquared(double x, double y, double z, double w)
        {
            return x * x + y * y + z * z + w * w;
        }

        /// <summary/>
        public static Vector3D Axis(Rotation3D rotation)
        {
            if (rotation is AxisAngleRotation3D)
            {
                return ((AxisAngleRotation3D)rotation).Axis;
            }
            else if (rotation is QuaternionRotation3D)
            {
                return ToAxisAngle(((QuaternionRotation3D)rotation).Quaternion).Axis;
            }
            throw new ArgumentException("rotation specified is not supported");
        }

        /// <summary/>
        public static double Angle(Rotation3D rotation)
        {
            if (rotation is AxisAngleRotation3D)
            {
                return ((AxisAngleRotation3D)rotation).Angle;
            }
            else if (rotation is QuaternionRotation3D)
            {
                return ToAxisAngle(((QuaternionRotation3D)rotation).Quaternion).Angle;
            }
            throw new ArgumentException("rotation specified is not supported");
        }

        /// <summary/>
        public static Quaternion Quaternion(Rotation3D rotation)
        {
            if (rotation is QuaternionRotation3D)
            {
                return ((QuaternionRotation3D)rotation).Quaternion;
            }
            else if (rotation is AxisAngleRotation3D)
            {
                return ToQuaternion(((AxisAngleRotation3D)rotation).Axis, ((AxisAngleRotation3D)rotation).Angle);
            }
            throw new ArgumentException("rotation specified is not supported");
        }

        /// <summary/>
        public static Quaternion ToQuaternion(Vector3D axis, double angle)
        {
            if (AreCloseEnough(LengthSquared(axis), 0))
            {
                throw new ArgumentException("Quaternion axis cannot be the zero vector", "axis");
            }

            // An axis-angle pair is converted to a normalized quaternion according to the following algorithm:

            // normalize axis;
            // x := axis.x * sin( angle/2 );
            // y := axis.y * sin( angle/2 );
            // z := axis.z * sin( angle/2 );
            // w := cos( angle/2 );

            // Proof that the result is normalized:
            //                                                               2    2    2    2
            //                                                              x  + y  + z  + w   =  1
            //
            //                    2                      2                      2           2
            // (axis.x * sin(a/2))  + (axis.x * sin(a/2))  + (axis.x * sin(a/2))  + cos(a/2)   =  1
            //
            //                          2             2           2           2             2
            //                  sin(a/2)  * ( (axis.x)  + (axis.y)  + (axis.z)  ) + cos(a/2)   =  1
            //
            //                                                                  2           2
            //                                                          sin(a/2)  + cos(a/2)   =  1

            angle %= 360;
            angle /= 2.0;
            axis = Normalize(axis);

            double sin_a = Math.Sin(ToRadians(angle));
            double cos_a = Math.Cos(ToRadians(angle));

            return new Quaternion(axis.X * sin_a, axis.Y * sin_a, axis.Z * sin_a, cos_a);
        }

        /// <summary/>
        public static AxisAngleRotation3D ToAxisAngle(Quaternion q)
        {
            // This will return NaN for zero quaternion
            Quaternion normalized = Normalize(q);

            if (IsIdentity(normalized) || LengthSquared(q.X, q.Y, q.Z, q.W) == 0)
            {
                return new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
            }

            // Do the inverse of the algorithm in ToQuaternion
            double cos_a = normalized.W;

            double angle = ToDegrees(Math.Acos(cos_a) * 2.0);
            Vector3D axis = Normalize(new Vector3D(normalized.X, normalized.Y, normalized.Z));

            // TODO: robbrow - 32616 - rework this
            //      Currently, when axis is the zero vector (Normalizes to NaN),
            //      Avalon returns +y axis.  I don't think this is the right behavior in all cases
            //if ( NotCloseEnough( axis, Const.vNaN ) )
            {
                return new AxisAngleRotation3D(axis, angle);
            }
            //else
            //{
            //    return new AxisAngleRotation3D( new Vector3D( 0,1,0 ), angle );
            //}
        }

        /// <summary/>
        public static Quaternion Slerp(Quaternion from, Quaternion to, double interval, bool useShortestPath)
        {
            double scale1;
            double scale2;
            double cosOmega;

            // cosOmega is the angle between two normalized quaternions as represented on the unit circle.
            //
            //     q1  ,,-----,,        The angle 'a' between q1 and q2 is half the angle between
            //       o'         ', q2     the 3D rotations.
            //      / ',   a     ,o
            //     ;    ',    ,''  ;    When useShortestPath is true, we take the shorter path between
            //     |      'o''     |      q1 and either q2 or q2'
            //     ;  ,,''         ;    When useShortestPath is false, we always take the shortest path between
            //      o'            /       q1 and q2 (Yes, I just said we take the shortest path when the
            //   q2' ",         ,"        bool is false.  But it is the shortest path between q1 and q2, never q2')
            //         ''-----''        Therefore, useShortestPath has no effect when 'a' < 90 degrees (<180 degree
            //                            rotation in 3D)
            //
            //     q1  ,,-----,,        We now only suffer when 'a' is 180 degrees (a 360 degree rotation in 3D)
            //       o'         ',
            //      / ',          \     In this case it is not obvious which path is shorter so we grab an arbitrary
            //     ;    ',   a     ;      perpendicular Quaternion as a midpoint and take that direction
            //     |      'o       |
            //     ;        ',     ;
            //      \         ',  /
            //       ",         o"
            //         ''-----'' q2
            //

            // Get the cosine of the angle between the two quaternions by using dot-product
            // acos( -1 ) == 180 degrees
            // acos( 0 )  == 90 degrees
            // acos( 1 )  == 0 degrees

            // We normalize them to get the correct value for cosOmega.
            // Normalize will return NaN for zero quaternion.
            cosOmega = DotProduct(Normalize(from), Normalize(to));
            if (double.IsNaN(cosOmega))
            {
                return Const.qNaN;
            }

            // There are two ways to slerp between every Quaternion pair.
            // Avalon defaults to choosing the shortest path (rotations are less than 180 degrees at a time).
            // For shortest path, if omega falls within the (90,180] degree range, negate one of the quaternions
            //  so that we will choose this "optimal" path instead of the longer path.
            //
            // Property of quaternions: ( [x,y,z] w ) == ( [-x,-y,-z] -w )

            if (useShortestPath && cosOmega < 0)
            {
                // Negate the quaternion so that we're guaranteed to take the shortest path.
                cosOmega *= -1;
                to = new Quaternion(-to.X, -to.Y, -to.Z, -to.W);
            }

            if (cosOmega > 1.0 - 1e-6)        // This is the limit the devs chose.
            {
                // Quaternions are practically identical, interpolate linearly
                scale1 = 1.0 - interval;
                scale2 = interval;
            }
            else if (cosOmega < 1e-10 - 1.0)  // This is the limit the devs chose.
            {
                // Quaternions are practically opposite, we need a point on the outer circle to give us direction
                //  (otherwise we interpolate right through the center)
                // Quaternion perpendicular to both of these has cosOmega == 0
                // Easiest way to get 0 is to swap and negate components:
                to = new Quaternion(-from.Y, from.X, -from.W, from.Z);

                // "interval" represents a fraction of a 180 degree rotation:
                double phi = interval * Math.PI;

                // The weight of the from Quaternion will move from positive to negative
                //  because to == from and we will be using the "from" Quaterion to represent both
                //      This is a cosine pattern: 1 -> 0 -> -1
                // The weight of the perpendicular Quaternion will move from 0 to 1 and back to 0
                //  because we pass through that point during the interpolation
                //      This is a sine pattern: 0 -> 1 -> 0
                scale1 = Math.Cos(phi);
                scale2 = Math.Sin(phi);
            }
            else
            {
                // Get the weights determining the contribution of each quaternion
                double omega = Math.Acos(cosOmega);
                scale1 = Math.Sin((1.0 - interval) * omega) / Math.Sin(omega);
                scale2 = Math.Sin(interval * omega) / Math.Sin(omega);
            }

            return new Quaternion(from.X * scale1 + to.X * scale2,
                                   from.Y * scale1 + to.Y * scale2,
                                   from.Z * scale1 + to.Z * scale2,
                                   from.W * scale1 + to.W * scale2);
        }

        /// <summary/>
        public static Point3D RightTopFront(Rect3D rect)
        {
            return new Point3D(Equals(rect.SizeX, Const.inf) ? Const.inf : rect.X + rect.SizeX,
                                Equals(rect.SizeY, Const.inf) ? Const.inf : rect.Y + rect.SizeY,
                                Equals(rect.SizeZ, Const.inf) ? Const.inf : rect.Z + rect.SizeZ);
        }

        /// <summary/>
        public static Point3D LeftBottomBack(Rect3D rect)
        {
            return rect.Location;
        }

        /// <summary/>
        public static int RoundUpToNextPowerOfTwo(int size)
        {
            if (size <= 0)
            {
                return 0;
            }

            int pow2 = 1;
            while (pow2 < size)
            {
                pow2 <<= 1;
            }
            return pow2;
        }

        /// <summary>
        /// Convert a number string to use a specific locale
        /// </summary>
        /// <remarks>
        /// If the input string represents a group of numbers, it is assumed
        /// that you are using Const.valueSeparator or whitespace to separate them.
        /// Numbers in the input string are assumed to have been serialized using InvariantCulture.
        /// </remarks>
        public static string ToLocale(string s, CultureInfo info)
        {
            if (s == null)
            {
                return s;
            }

            // Get one value at a time and convert it. 
            //  - can't trim whitespace.
            // Glue all the values back together at the end.

            int index = 0;
            int length = s.Length;
            char[] separatorChars = new char[] { Const.valueSeparator, ' ', '\r', '\n', '\t', '\0' };
            string specialSeparator = "�";
           
            string result = string.Empty;
            while (index < length)
            {
                switch (s[index])
                {
                    case Const.valueSeparator:
                        result += info.TextInfo.ListSeparator;
                        index++;
                        break;

                    case ' ':
                    case '\t':
                    case '\n':
                    case '\0':
                    case '\r':
                        result += s[index];
                        index++;
                        break;

                    default:
                        {
                            int endIndex = s.IndexOfAny(separatorChars, index);
                            if (endIndex < 0)
                            {
                                endIndex = length;
                            }
                            string value = s.Substring(index, endIndex - index);
                            bool useSpecialSeparator = (info.NumberFormat.NumberGroupSeparator == "." && info.NumberFormat.NumberDecimalSeparator == ",");

                            if (useSpecialSeparator)
                            {
                                value = value.Replace(CultureInfo.InvariantCulture.NumberFormat.NumberGroupSeparator, specialSeparator);
                            }
                            else
                            {
                                value = value.Replace(CultureInfo.InvariantCulture.NumberFormat.NumberGroupSeparator, info.NumberFormat.NumberGroupSeparator);
                            }

                            value = AdjustNegativeSign(value, info.NumberFormat.NumberNegativePattern);

                            value = AdjustDecimalAndSpecialValues(value, info);

                            if (useSpecialSeparator)
                            {
                                value = value.Replace(specialSeparator, info.NumberFormat.NumberGroupSeparator);
                            }

                            result += value;
                            index = endIndex;
                            break;
                        }
                }
            }
            return result;
        }
        
        /// <summary>
        /// Purpose of this is to allow us to compare numerical data without having to worry about whether 
        /// we've constructed the correct locale-specific seperator logic.
        /// Replace all , . ; @ characters with _. Stabilizes numerical tests against subtle language differences.
        /// </summary>		  
        private static string AmbiguateSeparators(string a)
        {
            string b = a.Replace(',','_');
            b = b.Replace('.','_');
            b = b.Replace(';','_');
            b = b.Replace('@','_');
            return b;
        }

        
        /// <summary>
        /// Replace all , . ; characters with _. Stabilizes numerical tests against language differences.
        /// </summary>		  
        public static bool EqualsIgnoringSeparators(string a, string b)
        {
            return AmbiguateSeparators(a).Equals(AmbiguateSeparators(b));
        }


        /// <summary>
        /// Adjust the negative sign for a double according to the format pattern.
        /// </summary>
        private static string AdjustNegativeSign(string s, int negativePattern)
        {
            // Acceptable values for "negativePattern" (from MSDN docs)
            //
            // Value     Associated Pattern
            //   0          (n)
            //   1          -n
            //   2          - n
            //   3          n-
            //   4          n -
            //
            // For now, we only need to support 1 and 3.  We can add others later if we find a need for it.

            if (negativePattern == 1)
            {
                // This is the same as InvariantCulture's NumberFormat.  No changes need to be made.
                return s;
            }
            else if (negativePattern == 3)
            {
                if (s.StartsWith("-"))
                {
                    // For some reason, the CLR does not put the "-" at the end when dealing with a number
                    //  in scientific notation.  We need to comply.
                    if (!s.ToLowerInvariant().Contains("e"))
                    {
                        return s.TrimStart('-') + '-';
                    }
                }
                return s;
            }
            throw new NotImplementedException("MathEx does not yet support this negative pattern.");
        }

        private static string AdjustDecimalAndSpecialValues(string value, CultureInfo info)
        {
            if (value == "NaN")
            {
                value = double.NaN.ToString(info);
            }
            else if (value == "Infinity")
            {
                value = double.PositiveInfinity.ToString(info);
            }
            else if (value == "-Infinity" || value == "Infinity-")
            {
                value = double.NegativeInfinity.ToString(info);
            }
            else
            {
                // change decimal format if necessary
                if (info.NumberFormat.NumberDecimalSeparator == ",")
                {
                    value = value.Replace(',', Const.valueSeparator);
                    value = value.Replace('.', ',');
                    value = value.Replace(Const.valueSeparator, '.');
                }
            }
            return value;
        }

        /// <summary/>
        public static Point Min(params Point[] points)
        {
            if (points == null || points.Length < 1)
            {
                throw new ArgumentException("Cannot find the minimum of nothing.");
            }
            double minX = double.PositiveInfinity;
            double minY = double.PositiveInfinity;
            foreach (Point p in points)
            {
                if (p.X < minX)
                {
                    minX = p.X;
                }
                if (p.Y < minY)
                {
                    minY = p.Y;
                }
            }
            return new Point(minX, minY);
        }

        /// <summary/>
        public static Point Max(params Point[] points)
        {
            if (points == null || points.Length < 1)
            {
                throw new ArgumentException("Cannot find the maximum of nothing.");
            }
            double maxX = double.NegativeInfinity;
            double maxY = double.NegativeInfinity;
            foreach (Point p in points)
            {
                if (p.X > maxX)
                {
                    maxX = p.X;
                }
                if (p.Y > maxY)
                {
                    maxY = p.Y;
                }
            }
            return new Point(maxX, maxY);
        }

        /// <summary/>
        public static double Max(params double[] args)
        {
            if (args == null || args.Length < 1)
            {
                throw new ArgumentException("Cannot find the maximum of nothing.");
            }

            double currentMaximum = double.NegativeInfinity;
            foreach (double value in args)
            {
                if (value > currentMaximum)
                {
                    currentMaximum = value;
                }
            }
            return currentMaximum;
        }

        /// <summary/>
        public static double Min(params double[] args)
        {
            if (args == null || args.Length < 1)
            {
                throw new ArgumentException("Cannot find the minimum of nothing.");
            }

            double currentMinimum = double.PositiveInfinity;
            foreach (double value in args)
            {
                if (value < currentMinimum)
                {
                    currentMinimum = value;
                }
            }
            return currentMinimum;
        }

        /// <summary/>
        public static Color Average(params Color?[] colors)
        {
            Color? c = Average((IEnumerable<Color?>)colors);
            if (c.HasValue)
            {
                return c.Value;
            }
            throw new ArgumentException("colors does not contain any valid WPF Color structs");
        }

        /// <summary/>
        public static Color? Average(IEnumerable<Color?> colors)
        {
            int a = 0;
            int r = 0;
            int g = 0;
            int b = 0;
            int numColors = 0;
            int numInvalidColors = 0;
            foreach (Color? color in colors)
            {
                if (color.HasValue)
                {
                    Color c = color.Value;
                    a += (int)c.A;
                    r += (int)c.R;
                    g += (int)c.G;
                    b += (int)c.B;
                }
                else
                {
                    numInvalidColors++;
                }
                numColors++;
            }

            if (numInvalidColors == numColors)
            {
                return null;
            }

            // Invalid colors don't count against the alpha average
            a /= (numColors - numInvalidColors);
            r /= numColors;
            g /= numColors;
            b /= numColors;
            return Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
        }

        /// <summary/>
        public static float Average(params float[] floats)
        {
            double average = 0;
            for (int i = 0; i < floats.Length; i++)
            {
                average += (double)floats[i];
            }
            average /= floats.Length;
            return (float)average;
        }

        /// <summary/>
        public static double Determinant(double m11, double m12, double m13, double m14,
                                             double m21, double m22, double m23, double m24,
                                             double m31, double m32, double m33, double m34,
                                             double m41, double m42, double m43, double m44)
        {
            /*
            | m11 m12 m13 m14 |          | m22 m23 m24 |         | m21 m23 m24 |         | m21 m22 m24 |         | m21 m22 m23 |
            | m21 m22 m23 m24 | == m11 * | m32 m33 m34 | - m12 * | m31 m33 m34 | + m13 * | m31 m32 m34 | - m14 * | m31 m32 m33 |
            | m31 m32 m33 m34 |          | m42 m43 m44 |         | m41 m43 m44 |         | m41 m42 m44 |         | m41 m42 m43 |
            | m41 m42 m43 m44 |
            */

            double part1 = m11 * Determinant(m22, m23, m24, m32, m33, m34, m42, m43, m44);
            double part2 = m12 * Determinant(m21, m23, m24, m31, m33, m34, m41, m43, m44);
            double part3 = m13 * Determinant(m21, m22, m24, m31, m32, m34, m41, m42, m44);
            double part4 = m14 * Determinant(m21, m22, m23, m31, m32, m33, m41, m42, m43);

            return part1 - part2 + part3 - part4;
        }

        /// <summary/>
        public static double Determinant(double m11, double m12, double m13,
                                             double m21, double m22, double m23,
                                             double m31, double m32, double m33)
        {
            double part1 = m11 * Determinant(m22, m23, m32, m33);
            double part2 = m12 * Determinant(m21, m23, m31, m33);
            double part3 = m13 * Determinant(m21, m22, m31, m32);

            return part1 - part2 + part3;
        }

        /// <summary/>
        public static double Determinant(double m11, double m12, double m21, double m22)
        {
            return m11 * m22 - m12 * m21;
        }

        /// <summary>
        /// Find the distance a given point lies away from a line passing through two points.
        /// The Z components are zeroed out before computing the value.
        /// </summary>
        public static double DistanceFromLine2D(Point3D point, Point3D linePoint1, Point3D linePoint2)
        {
            // Just zero out copies of points, and pass to 3D version.
            point.Z = 0;
            linePoint1.Z = 0;
            linePoint2.Z = 0;
            return DistanceFromLine(point, linePoint1, linePoint2);
        }

        /// <summary>
        /// Find the distance a given point lies away from a line passing through two points.
        /// </summary>
        public static double DistanceFromLine(Point3D point, Point3D linePoint1, Point3D linePoint2)
        {
            Vector3D v1 = linePoint2 - linePoint1;
            Vector3D v2 = linePoint1 - point;
            Vector3D crossProduct = MathEx.CrossProduct(v1, v2);

            // Math.Sign throws on NaN, avoid that.
            if (double.IsNaN(crossProduct.Z))
            {
                return 0;
            }
            else
            {
                return Math.Sign(crossProduct.Z) * MathEx.Length(crossProduct) / MathEx.Length(v1);
            }
        }

        /// <summary>
        /// Convert any RelativeToBoundingBox measurements on a brush to Absolute measurements.
        /// This method changes the values in place instead of returning a copy of the brush.
        /// </summary>
        public static void RelativeToAbsolute(Brush brush, Rect boundingBox)
        {
            // I don't want to complicate the code unless I absolutely have to...
            System.Diagnostics.Debug.Assert(
                        AreCloseEnough(0, boundingBox.X) && AreCloseEnough(0, boundingBox.Y),
                        "Expected bounding box to be based at 0,0.  This method needs to be fixed to offset the values using Absolute units");

            TileBrush tb = brush as TileBrush;
            if (tb != null)
            {
                if (tb.ViewboxUnits == BrushMappingMode.RelativeToBoundingBox)
                {
                    tb.Viewbox = ScaleRect(tb.Viewbox, boundingBox);
                    tb.ViewboxUnits = BrushMappingMode.Absolute;
                }
                if (tb.ViewportUnits == BrushMappingMode.RelativeToBoundingBox)
                {
                    tb.Viewport = ScaleRect(tb.Viewport, boundingBox);
                    tb.ViewportUnits = BrushMappingMode.Absolute;
                }
                return;
            }

            LinearGradientBrush lgb = brush as LinearGradientBrush;
            if (lgb != null)
            {
                if (lgb.MappingMode == BrushMappingMode.RelativeToBoundingBox)
                {
                    lgb.MappingMode = BrushMappingMode.Absolute;
                    lgb.StartPoint = ScalePoint(lgb.StartPoint, boundingBox);
                    lgb.EndPoint = ScalePoint(lgb.EndPoint, boundingBox);
                }
                return;
            }

            RadialGradientBrush rgb = brush as RadialGradientBrush;
            if (rgb != null)
            {
                if (rgb.MappingMode == BrushMappingMode.RelativeToBoundingBox)
                {
                    rgb.MappingMode = BrushMappingMode.Absolute;
                    rgb.Center = ScalePoint(rgb.Center, boundingBox);
                    rgb.GradientOrigin = ScalePoint(rgb.GradientOrigin, boundingBox);
                    rgb.RadiusX *= boundingBox.Width;
                    rgb.RadiusY *= boundingBox.Height;
                }
            }
        }

        /// <summary/>
        public static Rect ScaleRect(Rect rect, Rect scale)
        {
            return new Rect(
                        scale.X + (rect.X * scale.Width),
                        scale.Y + (rect.Y * scale.Height),
                        rect.Width * scale.Width,
                        rect.Height * scale.Height);
        }

        private static Point ScalePoint(Point point, Rect scale)
        {
            return new Point(
                        scale.X + (point.X * scale.Width),
                        scale.Y + (point.Y * scale.Height));
        }

        #region                 DpiConversions

        /// <summary>
        /// Convert a Device Independent measurement into a Device Dependent measurement
        /// i.e. 100 DIUs (Device Independent Units) is:
        ///      50 DDUs on 48-dpi displays,
        ///     100 DDUs on 96-dpi displays,
        ///     200 DDUs on 192-dpi displays
        /// </summary>
        /// <param name="diValue">A Device Independent horizontal measurement</param>
        /// <returns>A Device Dependent horizontal measurement</returns>
        public static double ConvertToAbsolutePixelsX(double diValue)
        {
            return diValue * dpiConversionX;
        }

        /// <summary>
        /// Convert a Device Independent measurement into a Device Dependent measurement
        /// i.e. 100 DIUs (Device Independent Units) is:
        ///      50 DDUs on 48-dpi displays,
        ///     100 DDUs on 96-dpi displays,
        ///     200 DDUs on 192-dpi displays
        /// </summary>
        /// <param name="diValue">A Device Independent vertical measurement</param>
        /// <returns>A Device Dependent vertical measurement</returns>
        public static double ConvertToAbsolutePixelsY(double diValue)
        {
            return diValue * dpiConversionY;
        }

        /// <summary>
        /// Convert a Device Independent measurement into a Device Dependent measurement
        /// i.e. 100 DIUs (Device Independent Units) is:
        ///      50 DDUs on 48-dpi displays,
        ///     100 DDUs on 96-dpi displays,
        ///     200 DDUs on 192-dpi displays
        /// </summary>
        /// <param name="diValue">A Device Independent measurement</param>
        /// <returns>A Device Dependent measurement</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when DpiX and DpiY are not equal.
        /// </exception>
        public static double ConvertToAbsolutePixels(double diValue)
        {
            if (NotCloseEnough(dpiConversionX, dpiConversionY))
            {
                throw new InvalidOperationException("Cannot convert doubles when DpiX != DpiY");
            }
            return ConvertToAbsolutePixelsX(diValue);
        }

        /// <summary>
        /// Convert a Device Independent Point into a Device Dependent Point
        /// i.e. 100,100 is:
        ///       50,50 on 48-dpi displays,
        ///     100,100 on 96-dpi displays,
        ///     200,200 on 192-dpi displays
        /// </summary>
        /// <param name="diValue">A Device Independent Point</param>
        /// <returns>A Device Dependent Point</returns>
        public static Point ConvertToAbsolutePixels(Point diValue)
        {
            return new Point(
                        ConvertToAbsolutePixelsX(diValue.X),
                        ConvertToAbsolutePixelsY(diValue.Y));
        }

        /// <summary>
        /// Convert a Device Independent Size into a Device Dependent Size
        /// i.e. 100,100 is:
        ///       50,50 on 48-dpi displays,
        ///     100,100 on 96-dpi displays,
        ///     200,200 on 192-dpi displays
        /// </summary>
        /// <param name="diValue">A Device Independent Size</param>
        /// <returns>A Device Dependent Size</returns>
        public static Size ConvertToAbsolutePixels(Size diValue)
        {
            return new Size(
                        ConvertToAbsolutePixelsX(diValue.Width),
                        ConvertToAbsolutePixelsY(diValue.Height));
        }

        /// <summary>
        /// Convert a Device Independent Rect into a Device Dependent Rect
        /// i.e. 50,50,100,100 is:
        ///         25,25,50,50 on 48-dpi displays,
        ///       50,50,100,100 on 96-dpi displays,
        ///     100,100,200,200 on 192-dpi displays
        /// </summary>
        /// <param name="diValue">A Device Independent Rect</param>
        /// <returns>A Device Dependent Rect</returns>
        public static Rect ConvertToAbsolutePixels(Rect diValue)
        {
            return new Rect(
                        ConvertToAbsolutePixelsX(diValue.X),
                        ConvertToAbsolutePixelsY(diValue.Y),
                        ConvertToAbsolutePixelsX(diValue.Width),
                        ConvertToAbsolutePixelsY(diValue.Height));
        }

        /// <summary>
        /// Convert a Device Independent Matrix into a Device Dependent Matrix
        /// (because some transforms are offset by "Center" properties in Device Independent space)
        /// i.e. 50,50,100,100 is:
        ///         25,25,50,50 on 48-dpi displays,
        ///       50,50,100,100 on 96-dpi displays,
        ///     100,100,200,200 on 192-dpi displays
        /// </summary>
        /// <param name="diValue">A Device Independent Rect</param>
        /// <returns>A Device Dependent Rect</returns>
        public static Matrix ConvertToAbsolutePixels(Matrix diValue)
        {
            // The math is as follows:
            //  - This essentially converts the DD point to a DI point, applies the transform,
            //      then converts back to a DD point.
            // [ 1/x  0  0 ]   [ a  b  0 ]   [ x 0 0 ]   [   a    b*y/x  0 ]
            // [  0  1/y 0 ] x [ c  d  0 ] x [ 0 y 0 ] = [ c*x/y    d    0 ]
            // [  0   0  1 ]   [ dx dy 1 ]   [ 0 0 1 ]   [ dx*x   dy*y   1 ]

            return new Matrix(
                        diValue.M11,
                        diValue.M12 * dpiConversionY / dpiConversionX,
                        diValue.M21 * dpiConversionX / dpiConversionY,
                        diValue.M22,
                        diValue.OffsetX * dpiConversionX,
                        diValue.OffsetY * dpiConversionY);
        }

        /// <summary>
        /// Convert a Device Dependent measurement into a Device Independent measurement
        /// i.e. 100 DDUs (Device Dependent Units) is:
        ///     200 DIUs on 48-dpi displays,
        ///     100 DIUs on 96-dpi displays,
        ///      50 DIUs on 192-dpi displays
        /// </summary>
        /// <param name="ddValue">A Device Dependent horizontal measurement</param>
        /// <returns>A Device Independent horizontal measurement</returns>
        public static double ConvertToDeviceIndependentPixelsX(double ddValue)
        {
            return ddValue / dpiConversionX;
        }

        /// <summary>
        /// Convert a Device Dependent measurement into a Device Independent measurement
        /// i.e. 100 DDUs (Device Dependent Units) is:
        ///     200 DIUs on 48-dpi displays,
        ///     100 DIUs on 96-dpi displays,
        ///      50 DIUs on 192-dpi displays
        /// </summary>
        /// <param name="ddValue">A Device Dependent vertical measurement</param>
        /// <returns>A Device Independent vertical measurement</returns>
        public static double ConvertToDeviceIndependentPixelsY(double ddValue)
        {
            return ddValue / dpiConversionY;
        }

        /// <summary>
        /// Convert a Device Dependent measurement into a Device Independent measurement
        /// i.e. 100 DDUs (Device Dependent Units) is:
        ///     200 DIUs on 48-dpi displays,
        ///     100 DIUs on 96-dpi displays,
        ///      50 DIUs on 192-dpi displays
        /// </summary>
        /// <param name="ddValue">A Device Dependent measurement</param>
        /// <returns>A Device Independent measurement</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when DpiX and DpiY are not equal.
        /// </exception>
        public static double ConvertToDeviceIndependentPixels(double ddValue)
        {
            if (NotCloseEnough(dpiConversionX, dpiConversionY))
            {
                throw new InvalidOperationException("Cannot convert doubles when DpiX != DpiY");
            }
            return ConvertToDeviceIndependentPixelsX(ddValue);
        }

        /// <summary>
        /// Convert a Device Dependent Point into a Device Independent Point
        /// i.e. 100,100 is:
        ///     200,200 on 48-dpi displays,
        ///     100,100 on 96-dpi displays,
        ///       50,50 on 192-dpi displays
        /// </summary>
        /// <param name="ddValue">A Device Dependent Point</param>
        /// <returns>A Device Independent Point</returns>
        public static Point ConvertToDeviceIndependentPixels(Point ddValue)
        {
            return new Point(
                        ConvertToDeviceIndependentPixelsX(ddValue.X),
                        ConvertToDeviceIndependentPixelsY(ddValue.Y));
        }

        /// <summary>
        /// Convert a Device Dependent Size into a Device Independent Size
        /// i.e. 100,100 is:
        ///     200,200 on 48-dpi displays,
        ///     100,100 on 96-dpi displays,
        ///       50,50 on 192-dpi displays
        /// </summary>
        /// <param name="ddValue">A Device Dependent Size</param>
        /// <returns>A Device Independent Size</returns>
        public static Size ConvertToDeviceIndependentPixels(Size ddValue)
        {
            return new Size(
                        ConvertToDeviceIndependentPixelsX(ddValue.Width),
                        ConvertToDeviceIndependentPixelsY(ddValue.Height));
        }

        /// <summary>
        /// Convert a Device Dependent Rect into a Device Independent Rect
        /// i.e. 100,100 is:
        ///     200,200 on 48-dpi displays,
        ///     100,100 on 96-dpi displays,
        ///       50,50 on 192-dpi displays
        /// </summary>
        /// <param name="ddValue">A Device Dependent Rect</param>
        /// <returns>A Device Independent Rect</returns>
        public static Rect ConvertToDeviceIndependentPixels(Rect ddValue)
        {
            return new Rect(
                        ConvertToDeviceIndependentPixelsX(ddValue.X),
                        ConvertToDeviceIndependentPixelsY(ddValue.Y),
                        ConvertToDeviceIndependentPixelsX(ddValue.Width),
                        ConvertToDeviceIndependentPixelsY(ddValue.Height));
        }

        #endregion
    }
}
