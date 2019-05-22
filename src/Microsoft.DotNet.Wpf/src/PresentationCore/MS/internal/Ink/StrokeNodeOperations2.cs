// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

namespace MS.Internal.Ink
{
    /// <summary>
    /// Static methods implementing generic hit-testing operations
    /// </summary>
    internal partial class StrokeNodeOperations
    {
        #region enum HitResult

        /// <summary> A set of possible results frequently used in StrokeNodeOperations and derived classes</summary>
        internal enum HitResult
        {
            Hit,
            Left,
            Right,
            InFront,
            Behind
        }

        #endregion

        #region HitTestXxxYyy

        /// <summary>
        /// Hit-tests a linear segment against a convex polygon.
        /// </summary>
        /// <param name="vertices">Vertices of the polygon (in clockwise order)</param>
        /// <param name="hitBegin">an end point of the hitting segment</param>
        /// <param name="hitEnd">an end point of the hitting segment</param>
        /// <returns>true if hit; false otherwise</returns>
        internal static bool HitTestPolygonSegment(Vector[] vertices, Vector hitBegin, Vector hitEnd)
        {
            System.Diagnostics.Debug.Assert((null != vertices) && (2 < vertices.Length));

            HitResult hitResult = HitResult.Right, firstResult = HitResult.Right, prevResult = HitResult.Right;
            int count = vertices.Length;
            Vector vertex = vertices[count - 1];
            for (int i = 0; i < count; i++)
            {
                Vector nextVertex = vertices[i];
                hitResult = WhereIsSegmentAboutSegment(hitBegin, hitEnd, vertex, nextVertex);
                if (HitResult.Hit == hitResult)
                {
                    return true;
                }
                if (IsOutside(hitResult, prevResult))
                {
                    return false;
                }
                if (i == 0)
                {
                    firstResult = hitResult;
                }
                prevResult = hitResult;
                vertex = nextVertex;
            }
            return (false == IsOutside(firstResult, hitResult));
        }

        /// <summary>
        /// This is a specialized version of HitTestPolygonSegment that takes
        /// a Quad for a polygon. This method is called very intensively by
        /// hit-testing API and we don't want to create Vector[] for every quad it hit-tests.
        /// </summary>
        /// <param name="quad">the connecting quad to test against</param>
        /// <param name="hitBegin">begin point of the hitting segment</param>
        /// <param name="hitEnd">end point of the hitting segment</param>
        /// <returns>true if hit, false otherwise</returns>
        internal static bool HitTestQuadSegment(Quad quad, Point hitBegin, Point hitEnd)
        {
            System.Diagnostics.Debug.Assert(quad.IsEmpty == false);

            HitResult hitResult = HitResult.Right, firstResult = HitResult.Right, prevResult = HitResult.Right;
            int count = 4;
            Vector zeroVector = new Vector(0, 0);
            Vector hitVector = hitEnd - hitBegin;
            Vector vertex = quad[count - 1] - hitBegin;

            for (int i = 0; i < count; i++)
            {
                Vector nextVertex = quad[i] - hitBegin;
                hitResult = WhereIsSegmentAboutSegment(zeroVector, hitVector, vertex, nextVertex);
                if (HitResult.Hit == hitResult)
                {
                    return true;
                }
                if (true == IsOutside(hitResult, prevResult))
                {
                    return false;
                }
                if (i == 0)
                {
                    firstResult = hitResult;
                }
                prevResult = hitResult;
                vertex = nextVertex;
            }
            return (false == IsOutside(firstResult, hitResult));
        }

        /// <summary>
        /// Hit-test a polygin against a circle
        /// </summary>
        /// <param name="vertices">Vectors representing the vertices of the polygon, ordered in clockwise order</param>
        /// <param name="center">Vector representing the center of the circle</param>
        /// <param name="radius">Vector representing the radius of the circle</param>
        /// <returns>true if hit, false otherwise</returns>
        internal static bool HitTestPolygonCircle(Vector[] vertices, Vector center, Vector radius)
        {
            // this code is not called, but will be in VNext
            throw new NotImplementedException();
            /*
            System.Diagnostics.Debug.Assert((null != vertices) && (2 < vertices.Length));

            HitResult hitResult = HitResult.Right, firstResult = HitResult.Right, prevResult = HitResult.Right;
            int count = vertices.Length;
            Vector vertex = vertices[count - 1];

            for (int i = 0; i < count; i++)
            {
                Vector nextVertex = vertices[i];
                hitResult = WhereIsCircleAboutSegment(center, radius, vertex, nextVertex);
                if (HitResult.Hit == hitResult)
                {
                    return true;
                }
                if (true == IsOutside(hitResult, prevResult))
                {
                    return false;
                }
                if (i == 0)
                {
                    firstResult = hitResult;
                }
                prevResult = hitResult;
                vertex = nextVertex;
            }
            return (false == IsOutside(firstResult, hitResult));
            */
        }

        /// <summary>
        /// This is a specialized version of HitTestPolygonCircle that takes
        /// a Quad for a polygon. This method is called very intensively by
        /// hit-testing API and we don't want to create Vector[] for every quad it hit-tests.
        /// </summary>
        /// <param name="quad">the connecting quad</param>
        /// <param name="center">center of the circle</param>
        /// <param name="radius">radius of the circle </param>
        /// <returns>true if hit; false otherwise</returns>
        internal static bool HitTestQuadCircle(Quad quad, Point center, Vector radius)
        {
            // this code is not called, but will be in VNext
            throw new NotImplementedException();
            /*
            System.Diagnostics.Debug.Assert(quad.IsEmpty == false);

            Vector centerVector = (Vector)center;
            HitResult hitResult = HitResult.Right, firstResult = HitResult.Right, prevResult = HitResult.Right;
            int count = 4;
            Vector vertex = (Vector)quad[count - 1];

            for (int i = 0; i < count; i++)
            {
                Vector nextVertex = (Vector)quad[i];
                hitResult = WhereIsCircleAboutSegment(centerVector, radius, vertex, nextVertex);
                if (HitResult.Hit == hitResult)
                {
                    return true;
                }
                if (true == IsOutside(hitResult, prevResult))
                {
                    return false;
                }
                if (i == 0)
                {
                    firstResult = hitResult;
                }
                prevResult = hitResult;
                vertex = nextVertex;
            }
            return (false == IsOutside(firstResult, hitResult));
            */
        }

        #endregion

        #region Whereabouts

        /// <summary>
        /// Finds out where the segment [hitBegin, hitEnd]
        /// is about the segment [orgBegin, orgEnd].
        /// </summary>
        internal static HitResult WhereIsSegmentAboutSegment(
            Vector hitBegin, Vector hitEnd, Vector orgBegin, Vector orgEnd)
        {
            if (hitEnd == hitBegin)
            {
                return WhereIsCircleAboutSegment(hitBegin, new Vector(0, 0), orgBegin, orgEnd);
            }

            //----------------------------------------------------------------------
            // Source: http://isc.faqs.org/faqs/graphics/algorithms-faq/
            // Subject 1.03: How do I find intersections of 2 2D line segments?
            //
            // Let A,B,C,D be 2-space position vectors.  Then the directed line
            // segments AB & CD are given by:
            //
            // AB=A+r(B-A), r in [0,1]
            // CD=C+s(D-C), s in [0,1]
            //
            // If AB & CD intersect, then
            //
            // A+r(B-A)=C+s(D-C), or  Ax+r(Bx-Ax)=Cx+s(Dx-Cx)
            // Ay+r(By-Ay)=Cy+s(Dy-Cy)  for some r,s in [0,1]
            //
            // Solving the above for r and s yields
            //
            //      (Ay-Cy)(Dx-Cx)-(Ax-Cx)(Dy-Cy)
            //  r = -----------------------------  (eqn 1)
            //      (Bx-Ax)(Dy-Cy)-(By-Ay)(Dx-Cx)
            //
            //      (Ay-Cy)(Bx-Ax)-(Ax-Cx)(By-Ay)
            //  s = -----------------------------  (eqn 2)
            //      (Bx-Ax)(Dy-Cy)-(By-Ay)(Dx-Cx)
            //
            // Let P be the position vector of the intersection point, then
            //
            //  P=A+r(B-A) or Px=Ax+r(Bx-Ax) and Py=Ay+r(By-Ay)
            //
            // By examining the values of r & s, you can also determine some
            // other limiting conditions:
            //  If 0 <= r <= 1 && 0 <= s <= 1, intersection exists
            //  r < 0 or r > 1 or s < 0 or s > 1 line segments do not intersect
            //  If the denominator in eqn 1 is zero, AB & CD are parallel
            //  If the numerator in eqn 1 is also zero, AB & CD are collinear.
            //  If they are collinear, then the segments may be projected to the x-
            //  or y-axis, and overlap of the projected intervals checked.
            //
            // If the intersection point of the 2 lines are needed (lines in this
            // context mean infinite lines) regardless whether the two line
            // segments intersect, then
            //  If r > 1, P is located on extension of AB
            //  If r < 0, P is located on extension of BA
            //  If s > 1, P is located on extension of CD
            //  If s < 0, P is located on extension of DC
            // Also note that the denominators of eqn 1 & 2 are identical.
            //
            // References:
            // [O'Rourke (C)] pp. 249-51
            // [Gems III] pp. 199-202 "Faster Line Segment Intersection,"
            //----------------------------------------------------------------------
            // The result tells where the segment CD is about the vector AB.
            // Return "Right" if either C or D is not on the left from AB.
            HitResult result = HitResult.Right;

            // Calculate the vectors.
            Vector AB = orgEnd - orgBegin;          // B - A
            Vector CA = orgBegin - hitBegin;        // A - C
            Vector CD = hitEnd - hitBegin;          // D - C
            double det = Vector.Determinant(AB, CD);

            if (DoubleUtil.IsZero(det))
            {
                // The segments are parallel.
                /*if (DoubleUtil.IsZero(Vector.Determinant(CD, CA)))
                {
                    // The segments are collinear.
                    // Check if their X and Y projections overlap.
                    if ((Math.Max(orgBegin.X, orgEnd.X) >= Math.Min(hitBegin.X, hitEnd.X)) &&
                        (Math.Min(orgBegin.X, orgEnd.X) <= Math.Max(hitBegin.X, hitEnd.X)) &&
                        (Math.Max(orgBegin.Y, orgEnd.Y) >= Math.Min(hitBegin.Y, hitEnd.Y)) &&
                        (Math.Min(orgBegin.Y, orgEnd.Y) <= Math.Max(hitBegin.Y, hitEnd.Y)))
                    {
                        // The segments overlap.
                        result = HitResult.Hit;
                    }
                    else if (false == DoubleUtil.IsZero(AB.X))
                    {
                        result = ((AB.X * CA.X) > 0) ? HitResult.Behind : HitResult.InFront;
                    }
                    else
                    {
                        result = ((AB.Y * CA.Y) > 0) ? HitResult.Behind : HitResult.InFront;
                    }
                }
                else */
                if (DoubleUtil.IsZero(Vector.Determinant(CD, CA)) || DoubleUtil.GreaterThan(Vector.Determinant(AB, CA), 0))
                {
                    // C is on the left from AB, and, since the segments are parallel, D is also on the left.
                    result = HitResult.Left;
                }
            }
            else
            {
                double r = AdjustFIndex(Vector.Determinant(AB, CA) / det);

                if (r > 0 && r < 1)
                {
                    // The line defined AB does cross the segment CD.
                    double s = AdjustFIndex(Vector.Determinant(CD, CA) / det);
                    if (s > 0 && s < 1)
                    {
                        // The crossing point is on the segment AB as well.
                        result = HitResult.Hit;
                    }
                    else
                    {
                        result = (0 < s) ? HitResult.InFront : HitResult.Behind;
                    }
                }
                else if ((WhereIsVectorAboutVector(hitBegin - orgBegin, AB) == HitResult.Left)
                    || (WhereIsVectorAboutVector(hitEnd - orgBegin, AB) == HitResult.Left))
                {
                    // The line defined AB doesn't cross the segment CD, and neither C nor D
                    // is on the right from AB
                    result = HitResult.Left;
                }
            }

            return result;
        }

        /// <summary>
        /// Find out the relative location of a circle relative to a line segment
        /// </summary>
        /// <param name="center">center of the circle</param>
        /// <param name="radius">radius of the circle. center.radius is a point on the circle</param>
        /// <param name="segBegin">begin point of the line segment</param>
        /// <param name="segEnd">end point of the line segment</param>
        /// <returns>test result</returns>
        internal static HitResult WhereIsCircleAboutSegment(
            Vector center, Vector radius, Vector segBegin, Vector segEnd)
        {
            segBegin -= center;
            segEnd -= center;
            double radiusSquared = radius.LengthSquared;

            // This will find out the nearest path from center to a point on the segment
            double distanceSquared = GetNearest(segBegin, segEnd).LengthSquared;

            // The segment must cross the circle, hit
            if (radiusSquared > distanceSquared)
            {
                return HitResult.Hit;
            }

            Vector segVector = segEnd - segBegin;
            HitResult result = HitResult.Right;

            // resolved two issues with the original code:
            // 1. The local varial "normal" is assigned a value but it is never used afterwards. \
            // 2.  the code indicates that that only case result is HitResult.InFront or HitResult.Behind is
            //  when WhereIsVectorAboutVector(-segBegin, segVector) == HitResult.Left.

            HitResult vResult = WhereIsVectorAboutVector(-segBegin, segVector);

            //either front or behind
            if (vResult == HitResult.Hit)
            {
                result = DoubleUtil.LessThan(segBegin.LengthSquared, segEnd.LengthSquared) ? HitResult.InFront :
                    HitResult.Behind;
            }
            else
            {
                // Find the projection of center on the segment.
                double findex = GetProjectionFIndex(segBegin, segEnd);

                // Get the normal vector, pointing from center to the projection point
                Vector normal = segBegin + (segVector * findex);

                // recalculate distanceSquared using normal
                distanceSquared = normal.LengthSquared;

                // The extension of the segment won't hit the circle
                if (radiusSquared <= distanceSquared)
                {
                    // either left or right
                    result = vResult;
                }
                else
                {
                    result = (findex > 0) ? HitResult.InFront : HitResult.Behind;
                }
            }

            return result;
        }

        /// <summary>
        /// Finds out where the vector1 is about the vector2.
        /// </summary>
        internal static HitResult WhereIsVectorAboutVector(Vector vector1, Vector vector2)
        {
            double determinant = Vector.Determinant(vector1, vector2);
            if (DoubleUtil.IsZero(determinant))
            {
                return HitResult.Hit;   // collinear
            }
            return (0 < determinant) ? HitResult.Left : HitResult.Right;
        }

        /// <summary>
        /// Tells whether the hitVector intersects the arc defined by two vectors.
        /// </summary>
        internal static HitResult WhereIsVectorAboutArc(Vector hitVector, Vector arcBegin, Vector arcEnd)
        {
            //HitResult result = HitResult.Right;
            if (arcBegin == arcEnd)
            {
                // full circle
                return HitResult.Hit;
            }

            if (HitResult.Right == WhereIsVectorAboutVector(arcEnd, arcBegin))
            {
                // small arc
                if ((HitResult.Left != WhereIsVectorAboutVector(hitVector, arcBegin)) &&
                    (HitResult.Right != WhereIsVectorAboutVector(hitVector, arcEnd)))
                {
                    return HitResult.Hit;
                }
            }
            else if ((HitResult.Left != WhereIsVectorAboutVector(hitVector, arcBegin)) ||
                    (HitResult.Right != WhereIsVectorAboutVector(hitVector, arcEnd)))
            {
                return HitResult.Hit;
            }

            if ((WhereIsVectorAboutVector(hitVector - arcBegin, TurnLeft(arcBegin)) != HitResult.Left) ||
                (WhereIsVectorAboutVector(hitVector - arcEnd, TurnRight(arcEnd)) != HitResult.Right))
            {
                return HitResult.Left;
            }

            return HitResult.Right;
        }

        #endregion

        #region Misc. helpers

        /// <summary>
        ///
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        internal static Vector TurnLeft(Vector vector)
        {
            // this code is not called, but will be in VNext
            throw new NotImplementedException();
            //return new Vector(-vector.Y, vector.X);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        internal static Vector TurnRight(Vector vector)
        {
            // this code is not called, but will be in VNext
            throw new NotImplementedException();
            //return new Vector(vector.Y, -vector.X);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="hitResult"></param>
        /// <param name="prevHitResult"></param>
        /// <returns></returns>
        internal static bool IsOutside(HitResult hitResult, HitResult prevHitResult)
        {
            // ISSUE-2004/10/08-XiaoTu For Polygon and Circle, ((HitResult.Behind == hitResult) && (HitResult.InFront == prevHitResult))
            // cannot be true.
            return ((HitResult.Left == hitResult)
                || ((HitResult.Behind == hitResult) && (HitResult.InFront == prevHitResult)));
        }

        /// <summary>
        /// Internal helper function to find out the ratio of the distance from hitpoint to lineVector
        /// and the distance from lineVector to (lineVector+nextLine)
        /// </summary>
        /// <param name="linesVector">This is one edge of a polygonal node</param>
        /// <param name="nextLine">The connection vector between the same edge on biginNode and ednNode</param>
        /// <param name="hitPoint">a point</param>
        /// <returns>the relative position of hitPoint</returns>
        internal static double GetPositionBetweenLines(Vector linesVector, Vector nextLine, Vector hitPoint)
        {
            Vector nearestOnFirst = GetProjection(-hitPoint, linesVector - hitPoint);

            hitPoint = nextLine - hitPoint;
            Vector nearestOnSecond = GetProjection(hitPoint, hitPoint + linesVector);

            Vector shortest = nearestOnFirst - nearestOnSecond;
            System.Diagnostics.Debug.Assert((false == DoubleUtil.IsZero(shortest.X)) || (false == DoubleUtil.IsZero(shortest.Y)));

            //return DoubleUtil.IsZero(shortest.X) ? (nearestOnFirst.Y / shortest.Y) : (nearestOnFirst.X / shortest.X);
            return Math.Sqrt(nearestOnFirst.LengthSquared / shortest.LengthSquared);
        }

        /// <summary>
        /// On a line defined buy two points finds the findex of the point
        /// nearest to the origin (0,0). Same as FindNearestOnLine just
        /// different output.
        /// </summary>
        /// <param name="begin">A point on the line.</param>
        /// <param name="end">Another point on the line.</param>
        /// <returns></returns>
        internal static double GetProjectionFIndex(Vector begin, Vector end)
        {
            Vector segment = end - begin;
            double lengthSquared = segment.LengthSquared;

            if (DoubleUtil.IsZero(lengthSquared))
            {
                return 0;
            }

            double dotProduct = -(begin * segment);
            return AdjustFIndex(dotProduct / lengthSquared);
        }

        /// <summary>
        /// On a line defined buy two points finds the point nearest to the origin (0,0).
        /// </summary>
        /// <param name="begin">A point on the line.</param>
        /// <param name="end">Another point on the line.</param>
        /// <returns></returns>
        internal static Vector GetProjection(Vector begin, Vector end)
        {
            double findex = GetProjectionFIndex(begin, end);
            return (begin + (end - begin) * findex);
        }

        /// <summary>
        /// On a given segment finds the point nearest to the origin (0,0).
        /// </summary>
        /// <param name="begin">The segment's begin point.</param>
        /// <param name="end">The segment's end point.</param>
        /// <returns></returns>
        internal static Vector GetNearest(Vector begin, Vector end)
        {
            double findex = GetProjectionFIndex(begin, end);
            if (findex <= 0)
            {
                return begin;
            }
            if (findex >= 1)
            {
                return end;
            }
            return (begin + ((end - begin) * findex));
        }

        /// <summary>
        /// Clears double's computation fuzz around 0 and 1
        /// </summary>
        internal static double AdjustFIndex(double findex)
        {
            return DoubleUtil.IsZero(findex) ? 0 : (DoubleUtil.IsOne(findex) ? 1 : findex);
        }

        #endregion
    }
}

