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
    internal class CuspData
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        internal CuspData()
        {
        }

        /// <summary>
        /// Constructs internal data structure from points for doing operations like
        /// cusp detection or tangent computation
        /// </summary>
        /// <param name="stylusPoints">Points to be analyzed</param>
        /// <param name="rSpan">Distance between two consecutive distinct points</param>
        internal void Analyze(StylusPointCollection stylusPoints, double rSpan)
        {
            // If the count is less than 1, return
            if ((null == stylusPoints) || (stylusPoints.Count == 0))
                return;

            _points = new List<CDataPoint>(stylusPoints.Count);
            _nodes = new List<double>(stylusPoints.Count);

            // Construct the lists of data points and nodes
            _nodes.Add(0);
            CDataPoint cdp0 = new CDataPoint();
            cdp0.Index = 0;
            //convert from Avalon to Himetric
            Point point = (Point)stylusPoints[0];
            point.X *= StrokeCollectionSerializer.AvalonToHimetricMultiplier;
            point.Y *= StrokeCollectionSerializer.AvalonToHimetricMultiplier;
            cdp0.Point = point;
            _points.Add(cdp0);

            //drop duplicates
            int index = 0;
            for (int i = 1; i < stylusPoints.Count; i++)
            {
                if (!DoubleUtil.AreClose(stylusPoints[i].X, stylusPoints[i - 1].X) ||
                    !DoubleUtil.AreClose(stylusPoints[i].Y, stylusPoints[i - 1].Y))
                {
                    //this is a unique point, add it
                    index++;

                    CDataPoint cdp = new CDataPoint();
                    cdp.Index = index;

                    //convert from Avalon to Himetric
                    Point point2 = (Point)stylusPoints[i];
                    point2.X *= StrokeCollectionSerializer.AvalonToHimetricMultiplier;
                    point2.Y *= StrokeCollectionSerializer.AvalonToHimetricMultiplier;
                    cdp.Point = point2;

                    _points.Insert(index, cdp);
                    _nodes.Insert(index, _nodes[index - 1] + (XY(index) - XY(index - 1)).Length);
                }
            }
 
            SetLinks(rSpan);
        }

        /// <summary>
        /// Set links amongst the points for tangent computation
        /// </summary>
        /// <param name="rError">Shortest distance between two distinct points</param>
        internal void SetTanLinks(double rError)
        {
            int count = Count;

            if (rError < 1.0)
                rError = 1.0f;

            for (int i = 0; i < count; ++i)
            {
                // Find a StylusPoint at distance-_span forward
                for (int j = i + 1; j < count; j++)
                {
                    if (_nodes[j] - _nodes[i] >= rError)
                    {
                        CDataPoint cdp = _points[i];
                        cdp.TanNext = j;
                        _points[i] = cdp;

                        CDataPoint cdp2 = _points[j];
                        cdp2.TanPrev = i;
                        _points[j] = cdp2;
                        break;
                    }
                }

                if (0 > _points[i].TanPrev)
                {
                    for (int j = i - 1; 0 <= j; --j)
                    {
                        if (_nodes[i] - _nodes[j] >= rError)
                        {
                            CDataPoint cdp = _points[i];
                            cdp.TanPrev = j;
                            _points[i] = cdp;
                            break;
                        }
                    }
                }

                if (0 > _points[i].TanNext)
                {
                    CDataPoint cdp = _points[i];
                    cdp.TanNext = count - 1;
                    _points[i] = cdp;
                }

                if (0 > _points[i].TanPrev)
                {
                    CDataPoint cdp = _points[i];
                    cdp.TanPrev = 0;
                    _points[i] = cdp;
                }
            }
        }

        


        /// <summary>
        /// Return the Index of the next cusp or the 
        /// Index of the last StylusPoint if no cusp was found
        /// </summary>
        /// <param name="iCurrent">Current StylusPoint Index</param>
        /// <returns>Index into CuspData object for the next cusp </returns>
        internal int GetNextCusp(int iCurrent)
        {
            int last = Count - 1;

            if (iCurrent < 0)
                return 0;

            if (iCurrent >= last)
                return last;

            // Perform a binary search
            int s = 0, e = _cusps.Count;
            int m = (s + e) / 2;

            while (s < m)
            {
                if (_cusps[m] <= iCurrent)
                    s = m;
                else
                    e = m;

                m = (s + e) / 2;
            }

            return _cusps[m + 1];
        }

        /// <summary>
        /// Point at Index i into the cusp data structure
        /// </summary>
        /// <param name="i">Index</param>
        /// <returns>StylusPoint</returns>
        /// <remarks>The Index is within the bounds</remarks>
        internal Vector XY(int i)
        {
            return new Vector(_points[i].Point.X, _points[i].Point.Y);
        }


        /// <summary>
        /// Number of points in the internal data structure
        /// </summary>
        internal int Count
        {
            get { return _points.Count; }
        }


        /// <summary>
        /// Returns the chord length of the i-th StylusPoint from start of the stroke
        /// </summary>
        /// <param name="i">StylusPoint Index</param>
        /// <returns>distance</returns>
        /// <remarks>The Index is within the bounds</remarks>
        internal double Node(int i)
        {
            return _nodes[i];
        }


        /// <summary>
        /// Returns the Index into original points given an Index into cusp data
        /// </summary>
        /// <param name="nodeIndex">Cusp data Index</param>
        /// <returns>Original StylusPoint Index</returns>
        internal int GetPointIndex(int nodeIndex)
        {
            return _points[nodeIndex].Index;
        }


        /// <summary>
        /// Distance
        /// </summary>
        /// <returns>distance</returns>
        internal double Distance()
        {
            return _dist;
        }


        /// <summary>
        /// Finds the approximante tangent at a given StylusPoint
        /// </summary>
        /// <param name="ptT">Tangent vector</param>
        /// <param name="nAt">Index at which the tangent is calculated</param>
        /// <param name="nPrevCusp">Index of the previous cusp</param>
        /// <param name="nNextCusp">Index of the next cusp</param>
        /// <param name="bReverse">Forward or reverse tangent</param>
        /// <param name="bIsCusp">Whether the current idex is a cusp StylusPoint</param>
        /// <returns>Return whether the tangent computation succeeded</returns>
        internal bool Tangent(ref Vector ptT, int nAt, int nPrevCusp, int nNextCusp, bool bReverse, bool bIsCusp)
        {
            // Tangent is computed as the unit vector along 
            // PT = (P1 - P0) + (P2 - P0) + (P3 - P0)
            // => PT = P1 + P2 + P3 - 3 * P0
            int i_1, i_2, i_3;

            if (bIsCusp)
            {
                if (bReverse)
                {
                    i_1 = _points[nAt].TanPrev;
                    if (i_1 < nPrevCusp || (0 > i_1))
                    {
                        i_2 = nPrevCusp;
                        i_1 = (i_2 + nAt) / 2;
                    }
                    else
                    {
                        i_2 = _points[i_1].TanPrev;
                        if (i_2 < nPrevCusp)
                            i_2 = nPrevCusp;
                    }
                }
                else
                {
                    i_1 = _points[nAt].TanNext;
                    if (i_1 > nNextCusp || (0 > i_1))
                    {
                        i_2 = nNextCusp;
                        i_1 = (i_2 + nAt) / 2;
                    }
                    else
                    {
                        i_2 = _points[i_1].TanNext;
                        if (i_2 > nNextCusp)
                            i_2 = nNextCusp;
                    }
                }
                ptT = XY(i_1) + 0.5 * XY(i_2) - 1.5 * XY(nAt);
            }
            else
            {
                Debug.Assert(bReverse);
                i_1 = nAt;
                i_2 = _points[nAt].TanPrev;
                if (i_2 < nPrevCusp)
                {
                    i_3 = nPrevCusp;
                    i_2 = (i_3 + i_1) / 2;
                }
                else
                {
                    i_3 = _points[i_2].TanPrev;
                    if (i_3 < nPrevCusp)
                        i_3 = nPrevCusp;
                }

                nAt = _points[nAt].TanNext;
                if (nAt > nNextCusp)
                    nAt = nNextCusp;

                ptT = XY(i_1) + XY(i_2) + 0.5 * XY(i_3) - 2.5 * XY(nAt);
            }

            if (DoubleUtil.IsZero(ptT.LengthSquared))
            {
                return false;
            }

            ptT.Normalize();
            return true;
        }

        /// <summary>
        /// This "curvature" is not the theoretical curvature.  it is a number between
        /// 0 and 2 that is defined as 1 - cos(angle between segments) at this StylusPoint.
        /// </summary>
        /// <param name="iPrev">Previous data StylusPoint Index</param>
        /// <param name="iCurrent">Current data StylusPoint Index </param>
        /// <param name="iNext">Next data StylusPoint Index</param>
        /// <returns>"Curvature"</returns>
        private double GetCurvature(int iPrev, int iCurrent, int iNext)
        {
            Vector V = XY(iCurrent) - XY(iPrev);
            Vector W = XY(iNext) - XY(iCurrent);
            double r = V.Length * W.Length;

            if (DoubleUtil.IsZero(r))
                return 0;

            return 1 - (V * W) / r;
        }


        /// <summary>
        /// Find all cusps for the stroke
        /// </summary>
        private void FindAllCusps()
        {
            // Clear the existing cusp indices
            _cusps.Clear();

            // There is nothing to find out from
            if (1 > this.Count)
                return;

            // First StylusPoint is always a cusp
            _cusps.Add(0);

            int iPrev = 0, iNext = 0, iCuspPrev = 0;

            // Find the next StylusPoint for Index 0
            // The following check will cover coincident points, stroke with 
            // less than 3 points
            if (!FindNextAndPrev(0, iCuspPrev, ref iPrev, ref iNext))
            {
                // Point count is zero, thus, there can't be any cusps
                if (0 == this.Count)
                    _cusps.Clear();
                else if (1 < this.Count) // Last StylusPoint is always a cusp
                    _cusps.Add(iNext);

                return;
            }

            // Start the algorithm with the next StylusPoint
            int iPoint = iNext;
            double rCurv = 0;

            // Check all the points on the chord of the stroke
            while (FindNextAndPrev(iPoint, iCuspPrev, ref iPrev, ref iNext))
            {
                // Find the curvature at iPoint
                rCurv = GetCurvature(iPrev, iPoint, iNext);

                /*
                    We'll look at every StylusPoint where rPrevCurv is a local maximum, and the 
                    curvature is more than the noise threashold.  If we're near the beginning
                    of the stroke then we'll ignore it and carry on.  If we're near the end 
                    then we'll skip to the end.  Otherwise, we'll flag it as a cusp if it 
                    deviates is significantly from the curvature at nearby points, forward
                    and backward
                */
                if (0.80 < rCurv)
                {
                    double rMaxCurv = rCurv;
                    int iMaxCurv = iPoint;
                    int m = 0, k = 0;

                    if (!FindNextAndPrev(iNext, iCuspPrev, ref k, ref m))
                    {
                        // End of the stroke has been reached
                        break;
                    }

                    for (int i = iPrev + 1; (i <= m) && FindNextAndPrev(i, iCuspPrev, ref iPrev, ref iNext); ++i)
                    {
                        rCurv = GetCurvature(iPrev, i, iNext);
                        if (rCurv > rMaxCurv)
                        {
                            rMaxCurv = rCurv;
                            iMaxCurv = i;
                        }
                    }

                    // Save the Index with max curvature
                    _cusps.Add(iMaxCurv);

                    // Continue the search with next StylusPoint
                    iPoint = m + 1;
                    iCuspPrev = iMaxCurv;
                }
                else if (0.035 > rCurv)
                {
                    // If the angle is less than 15 degree, skip the segment
                    iPoint = iNext;
                }
                else
                    ++iPoint;
            }

            // If everything went right, add the last StylusPoint to the list of cusps
            _cusps.Add(this.Count - 1);
        }


        /// <summary>
        /// Finds the next and previous data StylusPoint Index for the given data Index
        /// </summary>
        /// <param name="iPoint">Index at which the computation is performed</param>
        /// <param name="iPrevCusp">Previous cusp</param>
        /// <param name="iPrev">Previous data Index</param>
        /// <param name="iNext">Next data Index</param>
        /// <returns>Returns true if the end has NOT been reached.</returns>
        private bool FindNextAndPrev(int iPoint, int iPrevCusp, ref int iPrev, ref int iNext)
        {
            bool bHasMore = true;

            if (iPoint >= Count)
            {
                bHasMore = false;
                iPoint = Count - 1;
            }

			// Find a StylusPoint at distance-_span forward
            for (iNext = checked(iPoint + 1); iNext < Count; ++iNext)
                if (_nodes[iNext] - _nodes[iPoint] >= _span)
					break;

			if (iNext >= Count)
			{
				bHasMore = false;
				iNext = Count - 1;
			}

            for (iPrev = checked(iPoint - 1); iPrevCusp <= iPrev; --iPrev)
                if (_nodes[iPoint] - _nodes[iPrev] >= _span)
					break;

			if (iPrev < 0)
                iPrev = 0;

            return bHasMore;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="rMin"></param>
        /// <param name="rMax"></param>
        private static void UpdateMinMax(double a, ref double rMin, ref double rMax)
        {
            rMin = Math.Min(rMin, a);
            rMax = Math.Max(a, rMax);
        }
        /// <summary>
        /// Sets up the internal data structure to construct chain of points
        /// </summary>
        /// <param name="rSpan">Shortest distance between two distinct points</param>
        private void SetLinks(double rSpan)
        {
            // NOP, if there is only one StylusPoint
            int count = Count;

            if (2 > count)
                return;

            // Set up the links to next and previous probe
            double rL = XY(0).X;
            double rT = XY(0).Y;
            double rR = rL;
            double rB = rT;

            for (int i = 0; i < count; ++i)
            {
                UpdateMinMax(XY(i).X, ref rL, ref rR);
                UpdateMinMax(XY(i).Y, ref rT, ref rB);
            }

            rR -= rL;
            rB -= rT;
            _dist = Math.Abs(rR) + Math.Abs(rB);
            if (false == DoubleUtil.IsZero(rSpan))
                _span = rSpan;
            else if (0 < _dist)
            {
                /***
                _nodes[count - 1] at this StylusPoint contains the length of the stroke.
                _dist is the half peripheri of the bounding box of the stroke.
                The idea here is that the Length/_dist is somewhat analogous to the 
                "fractal dimension" of the stroke (or in other words, how much the stroke
                winds.)
                Length/count is the average distance between two consequitive points
                on the stroke. Thus, this average distance is multiplied by the winding
                factor.
                If the stroke were a PURE straight line across the diagone of a square, 
                Lenght/Dist will be approximately 1.41. And if there were one pixel per
                co-ordinate, the span would have been 1.41, which works fairly well in
                cusp detection
                ***/
                _span = 0.75f * (_nodes[count - 1] * _nodes[count - 1]) / (count * _dist);
            }

            if (_span < 1.0)
                _span = 1.0f;

            FindAllCusps();
        }


        private List<CDataPoint>        _points;
        private List<double>            _nodes;
        private double                  _dist = 0;
        private List<int>               _cusps = new List<int>();

        // Parameters governing the cusp detection algorithm
        // Distance between probes for curvature checking
        private double _span = 3; // Default span
    
        struct CDataPoint
        {
            public Point        Point;       // Point (coordinates are double)
            public int          Index;       // Index into the original array
            public int          TanPrev;    // Previous StylusPoint Index for tangent computation
            public int          TanNext;    // Next StylusPoint Index for tangent computation
        };
    }
}
