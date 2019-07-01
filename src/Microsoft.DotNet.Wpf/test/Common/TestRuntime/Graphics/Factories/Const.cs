// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Collections.Generic;

using Microsoft.Test.Graphics.TestTypes;

namespace Microsoft.Test.Graphics.Factories
{
    /// <summary>
    /// Constants used throughout testing
    /// </summary>
    public class Const
    {

        /// <summary/>
        public static bool IsVistaOrNewer
        {
            get { return EnvironmentWrapper.OSVersion.Version.Major >= 6; }
        }

        /// <summary/>
        public static double DpiX
        {
            get { return MathEx.ConvertToAbsolutePixelsX(96.0); }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static double DpiY
        {
            get { return MathEx.ConvertToAbsolutePixelsY(96.0); }
        }

        /// <summary/>
        public static double eps { get { return MathEx.Epsilon; } }

        /// <summary/>
        public static double min { get { return double.MinValue; } }

        /// <summary/>
        public static double max { get { return double.MaxValue; } }

        /// <summary/>
        public static double inf { get { return double.PositiveInfinity; } }

        /// <summary/>
        public static double nan { get { return double.NaN; } }

        /// <summary/>
        public const double pixelCenterX = 0.5;

        /// <summary/>
        public const double pixelCenterY = 0.5;

        /// <summary/>
        public static Point3D p0 { get { return new Point3D(0, 0, 0); } }
        
        /// <summary/>
        public static Point3D p1 { get { return new Point3D(1, 1, 1); } }

        /// <summary/>
        public static Point3D p10 { get { return new Point3D(10, 10, 10); } }

        /// <summary/>
        public static Point3D pNeg1 { get { return new Point3D(-1, -1, -1); } }

        /// <summary/>
        public static Point3D pMax { get { return new Point3D(max, max, max); } }

        /// <summary/>
        public static Point3D pMin { get { return new Point3D(min, min, min); } }

        /// <summary/>
        public static Point3D pEps { get { return new Point3D(eps, eps, eps); } }

        /// <summary/>
        public static Point3D pNaN { get { return new Point3D(nan, nan, nan); } }

        /// <summary/>
        public static Point3D pInf { get { return new Point3D(inf, inf, inf); } }

        /// <summary/>
        public static Point3D pInf2 { get { return new Point3D(inf + 10, inf + 10, inf + 10); } }

        /// <summary/>
        public static Point3D pNegInf { get { return new Point3D(-inf, -inf, -inf); } }

        /// <summary/>
        public static Point3D[] points0 { get { return new Point3D[] { }; } }

        /// <summary/>
        public static Point3D[] points1 { get { return new Point3D[] { p10 }; } }
        
        /// <summary/>
        public static Point3D[] points5 { get { return new Point3D[] { p0, p1, p10, pNeg1, pEps }; } }

        /// <summary/>
        public static Point3D[] cube
        {
            get
            {
                return new Point3D[]{
                    new Point3D( -1,-1,1 ), new Point3D( -1,1,1 ),
                    new Point3D( -1,1,1 ), new Point3D( 1,1,1 ),
                    new Point3D( 1,1,1 ), new Point3D( 1,-1,1 ),
                    new Point3D( 1,-1,1 ), new Point3D( -1,-1,1 ),

                    new Point3D( 1,-1,-1 ), new Point3D( 1,1,-1 ),
                    new Point3D( 1,1,-1 ), new Point3D( -1,1,-1 ),
                    new Point3D( -1,1,-1 ), new Point3D( -1,-1,-1 ),
                    new Point3D( -1,-1,-1 ), new Point3D( 1,-1,-1 ),

                    new Point3D( -1,-1,1 ), new Point3D( -1,-1,-1 ),
                    new Point3D( -1,1,1 ), new Point3D( -1,1,-1 ),
                    new Point3D( 1,1,1 ), new Point3D( 1,1,-1 ),
                    new Point3D( 1,-1,1 ), new Point3D( 1,-1,-1 )
                };
            }
        }

        /// <summary/>
        public static Vector3D v0 { get { return new Vector3D(0, 0, 0); } }

        /// <summary/>
        public static Vector3D v1 { get { return new Vector3D(1, 1, 1); } }

        /// <summary/>
        public static Vector3D v10 { get { return new Vector3D(10, 10, 10); } }
        
        /// <summary/>
        public static Vector3D vNeg1 { get { return new Vector3D(-1, -1, -1); } }

        /// <summary/>
        public static Vector3D vMax { get { return new Vector3D(max, max, max); } }

        /// <summary/>
        public static Vector3D vMin { get { return new Vector3D(min, min, min); } }

        /// <summary/>
        public static Vector3D vEps { get { return new Vector3D(eps, eps, eps); } }

        /// <summary/>
        public static Vector3D vNaN { get { return new Vector3D(nan, nan, nan); } }

        /// <summary/>
        public static Vector3D vInf { get { return new Vector3D(inf, inf, inf); } }

        /// <summary/>
        public static Vector3D vInf2 { get { return new Vector3D(inf + 10, inf + 10, inf + 10); } }

        /// <summary/>
        public static Vector3D vNegInf { get { return new Vector3D(-inf, -inf, -inf); } }

        /// <summary/>
        public static Vector3D xAxis { get { return new Vector3D(1, 0, 0); } }
        
        /// <summary/>
        public static Vector3D yAxis { get { return new Vector3D(0, 1, 0); } }

        /// <summary/>
        public static Vector3D zAxis { get { return new Vector3D(0, 0, 1); } }

        /// <summary/>
        public static Vector3D[] vectors0 { get { return new Vector3D[] { }; } }

        /// <summary/>
        public static Vector3D[] vectors1 { get { return new Vector3D[] { v10 }; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Vector3D[] vectors5 { get { return new Vector3D[] { v0, v1, v10, vNeg1, vEps }; } }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point4D p4_0 { get { return new Point4D(0, 0, 0, 0); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point4D p4_1 { get { return new Point4D(1, 1, 1, 1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point4D p4_10 { get { return new Point4D(10, 10, 10, 10); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point4D p4_Neg1 { get { return new Point4D(-1, -1, -1, -1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point4D p4_Eps { get { return new Point4D(eps, eps, eps, eps); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point4D p4_NaN { get { return new Point4D(nan, nan, nan, nan); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point4D p4_Inf { get { return new Point4D(inf, inf, inf, inf); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point4D p4_NegInf { get { return new Point4D(-inf, -inf, -inf, -inf); } }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point4D[] points4_0 { get { return new Point4D[] { }; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point4D[] points4_1 { get { return new Point4D[] { p4_Neg1 }; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point4D[] points4_5 { get { return new Point4D[] { p4_0, p4_1, p4_10, p4_Eps, p4_Neg1 }; } }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Size3D s0 { get { return new Size3D(0, 0, 0); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Size3D s1 { get { return new Size3D(1, 1, 1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Size3D s10 { get { return new Size3D(10, 10, 10); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Size3D sMax { get { return new Size3D(max, max, max); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Size3D sEps { get { return new Size3D(eps, eps, eps); } }
        
        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Size3D sEmpty { get { return Size3D.Empty; } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Size3D sNaN { get { return new Size3D(nan, nan, nan); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Size3D sInf { get { return new Size3D(inf, inf, inf); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rOrigin0 { get { return new Rect3D(p0, s0); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rOrigin1 { get { return new Rect3D(p0, s1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rPositive { get { return new Rect3D(3, 4, 5.2, 5, 5, 5); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rNegative { get { return new Rect3D(-4, -.5, -2, 4, 6, 8); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D containsAll { get { return new Rect3D(-10, -10, -10, 20, 20, 20); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D disjoint1 { get { return new Rect3D(-5, -5, -5, 3, 3, 3); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D disjoint2 { get { return new Rect3D(2, 2, 2, 1, 1, 1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D cornersTouch1 { get { return new Rect3D(0, 0, 0, 5, 5, 5); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D cornersTouch2 { get { return new Rect3D(-5, -5, -5, 5, 5, 5); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D overlap1 { get { return new Rect3D(-2, -2, -2, 3, 3, 3); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D overlap2 { get { return new Rect3D(-1, -1, -1, 3, 3, 3); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D r0Inf { get { return new Rect3D(0, 0, 0, inf, inf, inf); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D r1Inf { get { return new Rect3D(1, 1, 1, inf, inf, inf); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D r0NaN { get { return new Rect3D(0, 0, 0, nan, nan, nan); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D r1NaN { get { return new Rect3D(1, 1, 1, nan, nan, nan); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rNegInfInf { get { return new Rect3D(-inf, -inf, -inf, inf, inf, inf); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rNegInfMax { get { return new Rect3D(-inf, -inf, -inf, max, max, max); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rNegInfMid { get { return new Rect3D(-inf, -inf, -inf, max / 2, max / 2, max / 2); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rNegInf0 { get { return new Rect3D(-inf, -inf, -inf, 0, 0, 0); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rMinInf { get { return new Rect3D(min, min, min, inf, inf, inf); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rMinMax { get { return new Rect3D(min, min, min, max, max, max); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rMinMid { get { return new Rect3D(min, min, min, max / 2, max / 2, max / 2); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rMin0 { get { return new Rect3D(min, min, min, 0, 0, 0); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rOriginInf { get { return new Rect3D(0, 0, 0, inf, inf, inf); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rOriginMax { get { return new Rect3D(0, 0, 0, max, max, max); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rOriginMid { get { return new Rect3D(0, 0, 0, max / 2, max / 2, max / 2); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rMaxInf { get { return new Rect3D(max, max, max, inf, inf, inf); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rMaxMax { get { return new Rect3D(max, max, max, max, max, max); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rMaxMid { get { return new Rect3D(max, max, max, max / 2, max / 2, max / 2); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rMax0 { get { return new Rect3D(max, max, max, 0, 0, 0); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rInfInf { get { return new Rect3D(inf, inf, inf, inf, inf, inf); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rInfMax { get { return new Rect3D(inf, inf, inf, max, max, max); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rInfMid { get { return new Rect3D(inf, inf, inf, max / 2, max / 2, max / 2); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rInf0 { get { return new Rect3D(inf, inf, inf, 0, 0, 0); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rNaNInf { get { return new Rect3D(nan, nan, nan, inf, inf, inf); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rNaNMax { get { return new Rect3D(nan, nan, nan, max, max, max); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rNaNMid { get { return new Rect3D(nan, nan, nan, max / 2, max / 2, max / 2); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Rect3D rNaN0 { get { return new Rect3D(nan, nan, nan, 0, 0, 0); } }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Point3DCollection pCollection
        {
            get
            {
                Point3DCollection c = new Point3DCollection();

                c.Add(p0);
                c.Add(p1);
                c.Add(p10);
                c.Add(pNeg1);
                c.Add(pMax);
                c.Add(pMin);
                c.Add(pEps);

                return c;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Vector3DCollection vCollection
        {
            get
            {
                Vector3DCollection c = new Vector3DCollection();

                c.Add(v0);
                c.Add(v1);
                c.Add(v10);
                c.Add(vNeg1);
                c.Add(vMax);
                c.Add(vMin);
                c.Add(vEps);

                return c;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion q0 { get { return new Quaternion(0, 0, 0, 0); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion q1 { get { return new Quaternion(1, 1, 1, 1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion qNeg1 { get { return new Quaternion(-1, -1, -1, -1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion q1Norm { get { return new Quaternion(.5, .5, .5, .5); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion qNaN { get { return new Quaternion(nan, nan, nan, nan); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion qEps { get { return new Quaternion(eps, eps, eps, eps); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion qMax { get { return new Quaternion(max, max, max, max); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion qMin { get { return new Quaternion(min, min, min, min); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion qInf { get { return new Quaternion(inf, inf, inf, inf); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion qNegInf { get { return new Quaternion(-inf, -inf, -inf, -inf); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion qX45 { get { return new Quaternion(xAxis, 45); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion qX90 { get { return new Quaternion(xAxis, 90); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion qX135 { get { return new Quaternion(xAxis, 135); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion qX180 { get { return new Quaternion(xAxis, 180); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion qX360 { get { return new Quaternion(xAxis, 360); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion qX540 { get { return new Quaternion(xAxis, 540); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion qY90 { get { return new Quaternion(yAxis, 90); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion qZ45 { get { return new Quaternion(zAxis, 45); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Quaternion qZ135 { get { return new Quaternion(zAxis, 135); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static AxisAngleRotation3D rX45 { get { return new AxisAngleRotation3D(xAxis, 45); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static AxisAngleRotation3D rX90 { get { return new AxisAngleRotation3D(xAxis, 90); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static AxisAngleRotation3D rX135 { get { return new AxisAngleRotation3D(xAxis, 135); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static AxisAngleRotation3D rX180 { get { return new AxisAngleRotation3D(xAxis, 180); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static AxisAngleRotation3D rX360 { get { return new AxisAngleRotation3D(xAxis, 360); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static AxisAngleRotation3D rX540 { get { return new AxisAngleRotation3D(xAxis, 540); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static AxisAngleRotation3D rY90 { get { return new AxisAngleRotation3D(yAxis, 90); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static AxisAngleRotation3D rZ45 { get { return new AxisAngleRotation3D(zAxis, 45); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static AxisAngleRotation3D rZ135 { get { return new AxisAngleRotation3D(zAxis, 135); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static AxisAngleRotation3D r1_45 { get { return new AxisAngleRotation3D(v1, 45); } }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Matrix3D mIdent { get { return new Matrix3D(); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Matrix3D mAffine { get { return new Matrix3D(2, .5, .5, 0, .5, 2, .5, 0, .5, .5, 2, 0, 2, 2, 2, 1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Matrix3D mNAffine { get { return new Matrix3D(3, 1, .5, 0, 1, 3, 1, .25, .5, 1, 3, 0, 2, 2, 2, 1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Matrix3D mNaN { get { return new Matrix3D(4, .34, nan, 0, 7, 2.1, 1, 0, 5, 4, .9, 0, nan, 2, .6, 1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Matrix3D mInf { get { return new Matrix3D(4, inf, .34, 0, 7, 2.1, 1, 0, 5, 4, inf, 0, .9, 2, .6, 1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Matrix3D mNegInf { get { return new Matrix3D(4, .34, 2.1, 0, 7, -inf, 1, 0, 5, 4, .6, 0, .9, 2, -inf, 1); } }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static TranslateTransform3D tt1 { get { return new TranslateTransform3D(v1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static TranslateTransform3D tt10 { get { return new TranslateTransform3D(v10); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static TranslateTransform3D ttNeg1 { get { return new TranslateTransform3D(vNeg1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static TranslateTransform3D ttMax { get { return new TranslateTransform3D(vMax); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static TranslateTransform3D ttMin { get { return new TranslateTransform3D(vMin); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static TranslateTransform3D ttEps { get { return new TranslateTransform3D(vEps); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ScaleTransform3D st1 { get { return new ScaleTransform3D(v1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ScaleTransform3D st10 { get { return new ScaleTransform3D(v10); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ScaleTransform3D stNeg1 { get { return new ScaleTransform3D(vNeg1); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ScaleTransform3D stMax { get { return new ScaleTransform3D(vMax); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ScaleTransform3D stMin { get { return new ScaleTransform3D(vMin); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ScaleTransform3D stEps { get { return new ScaleTransform3D(vEps); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static RotateTransform3D rtX45 { get { return new RotateTransform3D(rX45); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static RotateTransform3D rtY90 { get { return new RotateTransform3D(rY90); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static RotateTransform3D rtZ135 { get { return new RotateTransform3D(rZ135); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static RotateTransform3D rtq { get { return new RotateTransform3D(r1_45); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MatrixTransform3D mtIdent { get { return new MatrixTransform3D(mIdent); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MatrixTransform3D mtAffine { get { return new MatrixTransform3D(mAffine); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MatrixTransform3D mtNAffine { get { return new MatrixTransform3D(mNAffine); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Transform3DGroup tg0 { get { return new Transform3DGroup(); } }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Transform3DGroup tg1
        {
            get
            {
                Transform3DGroup group = new Transform3DGroup();
                group.Children = new Transform3DCollection(new Transform3D[] { Transform3D.Identity });
                return group;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Transform3DGroup tg3
        {
            get
            {
                Transform3DGroup group = new Transform3DGroup();
                group.Children = new Transform3DCollection(new Transform3D[] { tt10, st10, rtY90 });
                return group;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ModelVisual3D mesh
        {
            get { return WrapIt(new GeometryModel3D()); }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ModelVisual3D light
        {
            get { return WrapIt(new AmbientLight()); }
        }
#if SSL
        
        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ModelVisual3D lines
        {
            get { return WrapIt( new ScreenSpaceLines3D() ); }
        }
#endif

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ModelVisual3D group
        {
            get { return WrapIt(new Model3DGroup()); }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static ModelVisual3D children
        {
            get
            {
                ModelVisual3D v = new ModelVisual3D();
                v.Children.Add(new ModelVisual3D());
                v.Children.Add(new ModelVisual3D());
                return v;
            }
        }
        private static ModelVisual3D WrapIt(Model3D model)
        {
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = model;
            return visual;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static DependencyProperty SkipProperty { get { return skipProperty; } }
        private static DependencyProperty skipProperty = DependencyProperty.RegisterAttached("Skip", typeof(string), typeof(Const), new PropertyMetadata(string.Empty));

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static DependencyProperty LookedAtProperty { get { return lookedAtProperty; } }
        private static DependencyProperty lookedAtProperty = DependencyProperty.RegisterAttached("LookedAt", typeof(bool), typeof(Const), new PropertyMetadata(false));

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static DependencyProperty NameProperty { get { return nameProperty; } }
        private static DependencyProperty nameProperty = DependencyProperty.RegisterAttached("Name", typeof(string), typeof(Const), new PropertyMetadata("(No Name)"));

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public const char valueSeparator = '@';

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static readonly double RootTwo = Math.Sqrt(2.0);

        #region Text Objects

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static FontFamily FontFamilyArial
        {
            get
            {
                return new FontFamily("Arial");
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static FontFamily FontFamilyArialTahoma
        {
            get
            {
                return new FontFamily("Arial, Tahoma");
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static FontFamily FontFamilyUnnamed
        {
            get
            {
                return new FontFamily();
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static FontFamilyMap FamilyMapLatin
        {
            get
            {
                FontFamilyMap map = new FontFamilyMap();
                map.Unicode = "0000-052F, 1D00-1FFF, FB00-FB0F";
                map.Target = "Times New Roman";
                map.Scale = 1.0;
                return map;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static FontFamilyMap FamilyMapHebrew
        {
            get
            {
                FontFamilyMap map = new FontFamilyMap();
                map.Unicode = "0590-06FF, FB1D-FDCF, FDF0-FDFF, FE70-FEFE";
                map.Target = "Tahoma";
                map.Language = System.Windows.Markup.XmlLanguage.GetLanguage("en-US");
                return map;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static FontFamilyMap FamilyMapHindi
        {
            get
            {
                FontFamilyMap map = new FontFamilyMap();
                map.Unicode = "0900-097F";
                map.Target = "Mangal";
                map.Scale = 2.0;
                return map;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static FontFamilyMap FamilyMapChinese
        {
            get
            {
                FontFamilyMap map = new FontFamilyMap();
                map.Unicode = "2000-20CF, 2100-23FF, 2460-27BF, 2980-29FF";
                map.Target = "SimSun";
                return map;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static FamilyTypeface FamilyTypefaceNormal
        {
            get
            {
                FamilyTypeface face = new FamilyTypeface();
                face.Style = FontStyles.Normal;
                face.Weight = FontWeights.Normal;
                face.Stretch = FontStretches.Normal;
                return face;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static FamilyTypeface FamilyTypefaceBold
        {
            get
            {
                FamilyTypeface face = new FamilyTypeface();
                face.Style = FontStyles.Normal;
                face.Weight = FontWeights.Bold;
                face.Stretch = FontStretches.Normal;
                return face;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static FamilyTypeface FamilyTypefaceItalic
        {
            get
            {
                FamilyTypeface face = new FamilyTypeface();
                face.Style = FontStyles.Italic;
                face.Weight = FontWeights.Normal;
                face.Stretch = FontStretches.Normal;
                return face;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static FamilyTypeface FamilyTypefaceCondensed
        {
            get
            {
                FamilyTypeface face = new FamilyTypeface();
                face.Style = FontStyles.Normal;
                face.Weight = FontWeights.Normal;
                face.Stretch = FontStretches.Condensed;
                return face;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static FamilyTypeface FamilyTypefaceExtraBold
        {
            get
            {
                FamilyTypeface face = new FamilyTypeface();
                face.Style = FontStyles.Normal;
                face.Weight = FontWeights.ExtraBold;
                face.Stretch = FontStretches.Normal;
                face.UnderlinePosition = 1.0;
                face.UnderlineThickness = 2.0;
                return face;
            }
        }

        #endregion
    }
}
