// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Ink;
using System.Windows.Input;
using System.Collections.Generic;

using MS.Internal.Ink.InkSerializedFormat;

namespace MS.Internal.Ink
{    
    /// <summary>
    /// Bezier curve generation class
    /// </summary>
    internal class Bezier
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public Bezier() {}

        /// <summary>
        /// Construct bezier control points from points
        /// </summary>
        /// <param name="stylusPoints">Original StylusPointCollection</param>
        /// <param name="fitError">Fitting error</param>
        /// <returns>Whether the algorithm succeeded</returns>
        internal bool ConstructBezierState(StylusPointCollection stylusPoints, double fitError)
        {
            // If the point count is zero, the curve cannot be constructed
            if ((null == stylusPoints) || (stylusPoints.Count == 0))
                return false;

            // Compile list of distinct points and their nodes
            CuspData dat = new CuspData();
            dat.Analyze(stylusPoints, 
                        fitError /*typically zero*/);

            return ConstructFromData(dat, fitError);
        }

        /// <summary>
        /// Flatten bezier with a given resolution
        /// </summary>
        /// <param name="tolerance">tolerance</param>
        internal List<Point> Flatten(double tolerance)
        {
            List<Point> points = new List<Point>();

            // First point
            Vector vector = GetBezierPoint(0);
            points.Add(new Point(vector.X, vector.Y));

            int last = this.BezierPointCount - 4;

            if (0 <= last)
            {
                // Tolerance needs to be non-zero positive
                if (tolerance < DoubleUtil.DBL_EPSILON)
                    tolerance = DoubleUtil.DBL_EPSILON;

                // Flatten individual segments
                for (int i = 0; i <= last; i += 3)
                    FlattenSegment(i, tolerance, points);
            }

            //convert from himetric to Avalon
            for (int x = 0; x < points.Count; x++)
            {
                Point p = points[x];
                p.X *= StrokeCollectionSerializer.HimetricToAvalonMultiplier;
                p.Y *= StrokeCollectionSerializer.HimetricToAvalonMultiplier;
                points[x] = p;
            }

            return points;
        }


        /// <summary>
        /// Extend the current bezier segment if possible
        /// </summary>
        /// <param name="error">Fitting error sqaure</param>
        /// <param name="data">Data points</param>
        /// <param name="from">Starting index</param>
        /// <param name="next_cusp">NExt cusp index</param>
        /// <param name="to">Index of the last index, updated here</param>
        /// <param name="cusp">Whether there is a cusp at the end</param>
        /// <param name="done">Whether end of the stroke is reached</param>
        /// <returns>Whether the the segment was extended</returns>
        private bool ExtendingRange(double error, CuspData data, int from, int next_cusp, ref int to, ref bool cusp, ref bool done)
        {
            to++;
            cusp = true;    // Presumed guilty
            done = to >= data.Count - 1;
            if (done)
            {
                to = data.Count - 1;
                cusp = true;
                return false;
            }

            cusp = to >= next_cusp;
            if (cusp)
            {
                to = next_cusp;
                return false;
            }

            Debug.Assert(to - from >= 4);
            int d = (to - from) / 4;
            int[] i = { from, from + d, (to + from) / 2, to - d, to };

            // Test for "cubicness"
            return CoCubic(data, i, error);
        }


        /// <summary>
        /// Add a bezier segment to the bezier buffer
        /// </summary>
        /// <param name="data">In: Data points</param>
        /// <param name="from">In: Index of the first point</param>
        /// <param name="tanStart">In: Unit tangent vector at the start</param>
        /// <param name="to">In: Index of the last point, updated here</param>
        /// <param name="tanEnd">In: Unit tangent vector at the end</param>
        /// <returns>True if the segment was added</returns>
        private bool AddBezierSegment(CuspData data, int from, ref Vector tanStart, int to, ref Vector tanEnd)
        {
            switch (to - from)
            {
                case 1 :
                    AddLine(data, from, to);
                    return true;

                case 2 :
                    AddParabola(data, from);
                    return true;
            }

            // We have at least 4 points, compute a least squares cubic
            return AddLeastSquares(data, from, ref tanStart, to, ref tanEnd);
        }


        /// <summary>
        /// Construct bezier curve from data points
        /// </summary>
        /// <param name="data">In: Data points</param>
        /// <param name="fitError">In: tolerated error</param>
        /// <returns>Whether bezier construction is possible</returns>
        private bool ConstructFromData(CuspData data, double fitError)
        {
            // Check for empty stroke 
            if (data.Count < 2)
            {
                return false;
            }

            // Add the first point
            AddBezierPoint(data.XY(0));

            // Special cases - 2 or 3 points
            if (data.Count == 3)
            {
                AddParabola(data, 0);
                return true;
            }
            else if (data.Count == 2)
            {
                AddLine(data, 0, 1);
                return true;
            }

            // For default case error passed in will be 0.
            // 3% is the default value
            if (DoubleUtil.DBL_EPSILON > fitError)
                fitError = 0.03f * (data.Distance() * StrokeCollectionSerializer.HimetricToAvalonMultiplier);

            data.SetTanLinks(0.5f * fitError);

            // otherwise use the value specified in the drawing attribute
            // get (error)^2
            fitError *= (fitError);

            bool done = false;
            int to = 0;
            int next_cusp = 0;
            int prev_cusp = 0;
            bool is_a_cusp = true;
            Vector tanEnd = new Vector(0, 0);
            Vector tanStart = new Vector(0, 0);

            for (int from = 0; !done; from = to)
            {
                if (is_a_cusp)
                {
                    prev_cusp = next_cusp;
                    next_cusp = data.GetNextCusp(from);
                    if (!data.Tangent(ref tanStart, from, prev_cusp, next_cusp, false, true))
                    {
                        return false;
                    }
                }
                else
                {
                    tanStart.X = -tanEnd.X;
                    tanStart.Y = -tanEnd.Y;
                }

                to = from + 3;

                // No meat in this loop, just extending the index range
                while (ExtendingRange(fitError, data, from, next_cusp, ref to, ref is_a_cusp, ref done));

                // Find the tangent 
                if (!data.Tangent(ref tanEnd, to, prev_cusp, next_cusp, true, is_a_cusp))
                {
                    return false;
                }

                // Add bezier segment
                if (!AddBezierSegment(data, from, ref tanStart, to, ref tanEnd))
                {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// Add parabola to the bezier
        /// </summary>
        /// <param name="data">In: Data points</param>
        /// <param name="from">In: The index of the parabola's first point</param>
        private void AddParabola(CuspData data, int from)
        {
            /* Denote s = 1-t.  We construct the parabola with Bezier points A,B,C that 
                goes thru the point P at parameter value t, that is
                    P = s^2A + 2stB + t^2C

                We know A and C, and we solve for B:
                    B = (P - s^2A - t^2C) / 2st. 

                Elevating the degree to cubic replaces B with 2 points, the first at 
                    2B/3 + A/3, and the second at 2B/3 + C/3.

                That is, one point at
                    (P/(st) - Ct/s + A(-s/t + 1)) / 3
                and the other point at
                    (P/(st) + C(-t/s + 1) - As/t) / 3
            */
            // By the way the nodes were constructed:
            //ASSERT(data.Node(from+2) - data.Node(from) > 
            //        data.Node(from+1) - data.Node(from));
            double t = (data.Node(from + 1) - data.Node(from)) / (data.Node(from + 2) - data.Node(from));
            double s = 1 - t;

            if (t < .001 || s < .001)
            {
                // A straight line will be a better approximation
                AddLine(data, from, from + 2);
                return;
            }

            double tt = 1 / t;
            double ss = 1 / s;
            const double third = 1.0d / 3.0d;
            Vector P = (tt * ss) * data.XY(from + 1);
            Vector B = third * (P + (1 - s * tt) * data.XY(from) - (t * ss) * data.XY(from + 1));

            AddBezierPoint(B);
            B = third * (P - (s * tt) * data.XY(from) + (1 - t * ss) * data.XY(from + 2));
            AddBezierPoint(B);
            AddSegmentPoint(data, from + 2);
        }


        /// <summary>
        /// Add Line to the bezier
        /// </summary>
        /// <param name="data">In: Data points</param>
        /// <param name="from">In: The index of the line's first point</param>
        /// <param name="to">In: The index of the line's last point</param>
        private void AddLine(CuspData data, int from, int to)
        {
            const double third = 1.0d / 3.0d;

            AddBezierPoint((2 * data.XY(from) + data.XY(to)) * third);
            AddBezierPoint((data.XY(from) + 2 * data.XY(to)) * third);
            AddSegmentPoint(data, to);
        }


        /// <summary>
        /// Add least square fit curve to the bezier
        /// </summary>
        /// <param name="data">In: Data points</param>
        /// <param name="from">In: Index of the first point</param>
        /// <param name="V">In: Unit tangent vector at the start</param>
        /// <param name="to">In: Index of the last point, updated here</param>
        /// <param name="W">In: Unit tangent vector at the end</param>
        /// <returns>Return true segment added</returns>
        private bool AddLeastSquares(CuspData data, int from, ref Vector V, int to, ref Vector W)
        {
            /* To do: When there is a cusp at either one of the ends, we'll get a
            better approximation if we use a construction without  a prescribed 
            tangent there */
            /*  
            The Bezier points of this segment are A, A+sV, B+uW, and B, where A,B are the
            endpoints, and V,W are the end tangents.  For the node tj, denote f0j=(1-tj)^3,
            f1j=3(1-tj)^2tj, f2j=3(1-tj)tj^2, f3j=tj^3.  Let Pj be the jth point.
            We are lookig for s,u that minimize 
                    Sum(A*f0j + (A+sV)*f1j + (B+uW)*f2j + B*f3j - Pj)^2.

            Equate the partial derivatives of this w.r.t. s and u to 0:
                    Sum(A*f0j + (A+sV)*f1j + (B+uW)*f2j + B*f3j - Pj)*(V*f1j)=0
                    Sum(A*f0j + (A+sV)*f1j + (B+uW)*f2j + B*f3j - Pj)*(W*f2j)=0
            hence

                s*Sum(V*V*f1j*f1j) + u*Sum(W*V*f1j*f2j)= -Sum(A*(f0j+f1j) + B*(f2j+f3j) - Pj)*V*f1j
                s*Sum(V*W*f1j*f2j) + u*Sum(W*W*f2j*f2j)= -Sum(A*(f0j+f1j) + B*(f2j+f3j) - Pj)*W*f2j

            so the equations are    
                s*a11 + u*a12 = b1
                s*a12 * u*a22 = b2

            with 
                a11 = W*W*Sum(f1j^2), a22 = V*V*Sum(f2j^2), a12 = W*V*Sum(f1j*f2j)
                b1 = -V*A*Sum(f0j + f1j)*f1j - V*B*Sum(f2j + f3j)*f1j + Sum(f1j*Pj*V)
                b2 = -W*A*Sum(f0j + f1j)*f2j - W*B*Sum(f2j + f3j)*f2j + Sum(f2j*Pj*W)

            V and W ae unit vectors, so V*V = W*W = 1.
            For computational efficiency, we will break b1 and b2 into 3 sums each, and add
            them up at the end

            The solution is 
                s = (b1*a22 - b2*a12) / det
                u = (b2*a11 - b1*a12) / det
            where det = a11*a22 - a22^2
            */
            // Compute the coefficients
            double a11 = 0, a12 = 0, a22 = 0, b1 = 0, b2 = 0;
            double b11 = 0, b12 = 0, b21 = 0, b22 = 0;

            for (int j = checked(from + 1); j < to; j++)
            {
                // By the way the nodes were constructed - 
                Debug.Assert(data.Node(to) - data.Node(from) > data.Node(j) - data.Node(from));
                double tj = (data.Node(j) - data.Node(from)) / (data.Node(to) - data.Node(from));
                double tj2 = tj * tj;
                double rj = 1 - tj;
                double rj2 = rj * rj;

                double f0j = rj2 * rj;
                double f1j = 3 * rj2 * tj;
                double f2j = 3 * rj * tj2;
                double f3j = tj2 * tj;

                a11 += f1j * f1j;
                a22 += f2j * f2j;
                a12 += f1j * f2j;

                b11 -= (f0j + f1j) * f1j;
                b12 -= (f2j + f3j) * f1j;
                b1 += f1j * (data.XY(j) * V);

                b21 -= (f0j + f1j) * f2j;
                b22 -= (f2j + f3j) * f2j;
                b2 += f2j * (data.XY(j) * W);
            }

            a12 *= (V * W);
            b1 += ((V * data.XY(from)) * b11 + (V * data.XY(to)) * b12);
            b2 += ((W * data.XY(from)) * b21 + (W * data.XY(to)) * b22);

            // Solve the equations
            double s = b1 * a22 - b2 * a12;
            double u = b2 * a11 - b1 * a12;
            double det = a11 * a22 - a12 * a12;
            bool accept = (Math.Abs(det) > Math.Abs(s) * DoubleUtil.DBL_EPSILON && 
                            Math.Abs(det) > Math.Abs(u) * DoubleUtil.DBL_EPSILON);

            if (accept)
            {
                s /= det;
                u /= det;

                // We'll only accept large enough positive solutions
                accept = s > 1.0e-6 && u > 1.0e-6;
            }

            if (!accept)
                s = u = (data.Node(to) - data.Node(from)) / 3;

            AddBezierPoint(data.XY(from) + s * V);
            AddBezierPoint(data.XY(to) + u * W);
            AddSegmentPoint(data, to);
            return true;
        }


        /// <summary>
        /// Checks whether five points are co-cubic within tolerance
        /// </summary>
        /// <param name="data">In: Data points</param>
        /// <param name="i">In: Array of 5 indices</param>
        /// <param name="fitError">In: tolerated error - squared</param>
        /// <returns>Return true if extended</returns>
        private static bool CoCubic(CuspData data, int[] i, double fitError)
        {
            /* Our error estimate is (t[4]-t[0])^4 times the 4th divided difference
            * of the points with resect to the nodes. The divided difference is
            * equal to Sum(c(i)*p[i]), where c(i)=Product(t[i]-t[j]: j != i)
            * (See Conte & deBoor's Elementary Numerical Analysis, Excercise 2.2-1).
            * We multiply each factor in the product by t[4]-t[0].
            */
            double d04 = data.Node(i[4]) - data.Node(i[0]);
            double d01 = d04 / (data.Node(i[1]) - data.Node(i[0]));
            double d02 = d04 / (data.Node(i[2]) - data.Node(i[0]));
            double d03 = d04 / (data.Node(i[3]) - data.Node(i[0]));
            double d12 = d04 / (data.Node(i[2]) - data.Node(i[1]));
            double d13 = d04 / (data.Node(i[3]) - data.Node(i[1]));
            double d14 = d04 / (data.Node(i[4]) - data.Node(i[1]));
            double d23 = d04 / (data.Node(i[3]) - data.Node(i[2]));
            double d24 = d04 / (data.Node(i[4]) - data.Node(i[2]));
            double d34 = d04 / (data.Node(i[4]) - data.Node(i[3]));
            Vector P =  d01 * d02 * d03 * data.XY(i[0]) - 
                        d01 * d12 * d13 * d14 * data.XY(i[1]) + 
                        d02 * d12 * d23 * d24 * data.XY(i[2]) - 
                        d03 * d13 * d23 * d34 * data.XY(i[3]) + 
                        d14 * d24 * d34 * data.XY(i[4]);

            return ((P * P) < fitError);
        }


        /// <summary>
        /// Add Bezier point to the output buffer
        /// </summary>
        /// <param name="point">In: The point to add</param>
        private void AddBezierPoint(Vector point)
        {
            _bezierControlPoints.Add((Point)point);
        }


        /// <summary>
        /// Add segment point
        /// </summary>
        /// <param name="data">In: Interpolation data</param>
        /// <param name="index">In: The index of the point to add</param>
        private void AddSegmentPoint(CuspData data, int index)
        {
            _bezierControlPoints.Add((Point)data.XY(index));
        }


        /// <summary>
        /// Evaluate on a Bezier segment a point at a given parameter 
        /// </summary>
        /// <param name="iFirst">Index of Bezier segment's first point</param>
        /// <param name="t">Parameter value t</param>
        /// <returns>Return the point at parameter t on the curve</returns>
        private Vector DeCasteljau(int iFirst, double t)
        {
            // Using the de Casteljau algorithm.  See "Curves & Surfaces for Computer
            // Aided Design" for the theory
            double s = 1.0f - t;

            // Level 1
            Vector Q0 = s * GetBezierPoint(iFirst) + t * GetBezierPoint(iFirst + 1);
            Vector Q1 = s * GetBezierPoint(iFirst + 1) + t * GetBezierPoint(iFirst + 2);
            Vector Q2 = s * GetBezierPoint(iFirst + 2) + t * GetBezierPoint(iFirst + 3);

            // Level 2
            Q0 = s * Q0 + t * Q1;
            Q1 = s * Q1 + t * Q2;

            // Level 3
            return s * Q0 + t * Q1;
        }

        /// <summary>
        ///  Flatten a Bezier segment within given resolution
        /// </summary>
        /// <param name="iFirst">Index of Bezier segment's first point</param>
        /// <param name="tolerance">tolerance</param>
        /// <param name="points"></param>
        /// <returns></returns>
        private void FlattenSegment(int iFirst, double tolerance, List<Point> points)
        {
            // We use forward differencing.  It is much faster than subdivision
            int i, k;
            int nPoints = 1;
            Vector[] Q = new Vector[4];

            // The number of points is determined by the "curvedness" of this segment,
            // which is a heuristic: it's the maximum of the 2 medians of the triangles 
            // formed by consecutive Bezier points.  Why median? because it is cheaper
            // to compute than height.
            double rCurv = 0;

            for (i = checked(iFirst + 1); i <= checked(iFirst + 2); i++)
            {
				// Get the longer median
				Q[0] = (GetBezierPoint(i - 1) + GetBezierPoint(i + 1)) * 0.5f - GetBezierPoint(i);

				double r = Q[0].Length;

				if (r > rCurv)
					rCurv = r;
			}

			// Now we look at the ratio between the medain and the error tolerance.
            // the points are collinear then one point - the endpoint - will do. 
            // Otherwise, since curvature is roughly inverse proportional
            // to the square of nPoints, we set nPoints to be the square root of this 
            // ratio, but not less than 3.
            if (rCurv <= 0.5 * tolerance)  // Flat segment
            {
                Vector vector = GetBezierPoint(iFirst + 3);
                points.Add(new Point(vector.X, vector.Y));
                return;
            }

            // Otherwise we'll have at least 3 points
            // Tolerance is assumed to be positive
            nPoints = (int)(Math.Sqrt(rCurv / tolerance)) + 3;
            if (nPoints > 1000)
                nPoints = 1000; // Arbitrary limitation, but...

            // Get the first 4 points on the segment in the buffer
            double d = 1.0f / (double)nPoints;

            Q[0] = GetBezierPoint(iFirst);
            for (i = 1; i <= 3; i++)
            {
                Q[i] = DeCasteljau(iFirst, i * d);
                points.Add(new Point(Q[i].X, Q[i].Y));
            }

            // Replace points in the buffer with differences of various levels
            for (i = 1; i <= 3; i++)
                for (k = 0; k <= (3 - i); k++)
                    Q[k] = Q[k + 1] - Q[k];

            // Now generate the rest of the points by forward differencing
            for (i = 4; i <= nPoints; i++)
            {
                for (k = 1; k <= 3; k++)
                    Q[k] += Q[k - 1];

                points.Add(new Point(Q[3].X, Q[3].Y));
            }
        }
        /// <summary>
        /// Returns a single bezier control point at index
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns></returns>
        private Vector GetBezierPoint(int index)
        {
            return (Vector)_bezierControlPoints[index];
        }


        /// <summary>
        /// Count of bezier control points
        /// </summary>
        private int BezierPointCount
        {
            get { return _bezierControlPoints.Count; }
        }

        // Bezier points
        private List<Point> _bezierControlPoints = new List<Point>();
    }
}
