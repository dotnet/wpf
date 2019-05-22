// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;              // for ArrayList
using System.Diagnostics;
using System.Collections.Generic;

using System.Windows;                  // for Rect                        WindowsBase.dll

namespace Microsoft.Internal.AlphaFlattener
{
    internal class Coordinate
    {
        public double     value;
        public int        index;
        public bool       active;
        public Coordinate top;
        public Coordinate bottom;

        public Coordinate(double v, int i)
        {
            value = v;
            index = i;
        }
    }

    internal class CoordinateComparer : IComparer
    {
        int IComparer.Compare(Object x, Object y)
        {
            double vx = ((Coordinate)x).value;
            double vy = ((Coordinate)y).value;

            return vx.CompareTo(vy);
        }
    }

    internal class CoordinateSearcher : IComparer
    {
        int IComparer.Compare(Object x, Object y)
        {
            double vx = ((Coordinate)x).value;
            double vy = (double)y;

            return vx.CompareTo(vy);
        }
    }

    internal class SegmentTree
    {
        double _min;
        double _max;
        SegmentTree _left;
        SegmentTree _right;
        List<int> _sList;

        /// <summary>
        /// Build a balanced Segment Tree from a sorted intersection list
        /// </summary>
        /// <param name="coord"></param>
        /// <param name="from"></param>
        /// <param name="count"></param>
        public SegmentTree(Coordinate[] coord, int from, int count)
        {
            Debug.Assert(count >= 2);

            _min = coord[from].value;
            _max = coord[from + count - 1].value;

            if (count > 2)
            {
                int half = count / 2 + 1;

                _left = new SegmentTree(coord, from, half);
                _right = new SegmentTree(coord, from + half - 1, count - half + 1);
            }
        }

        public void Remove(int index, double x0, double x1)
        {
            if ((_min >= x0) && (_max <= x1)) // [_min.._max] is within [x0..x1]
            {
                if (_sList != null)
                {
                    _sList.Remove(index);
                }
            }
            else
            {
                if ((_left != null) && (x0 <= _left._max) && (x1 >= _left._min)) // overlap with left
                {
                    _left.Remove(index, x0, x1);
                }

                if ((_right != null) && (x0 <= _right._max) && (x1 >= _right._min)) // overlap with right
                {
                    _right.Remove(index, x0, x1);
                }
            }
        }

        public void Insert(int index, double x0, double x1)
        {
            if ((_min >= x0) && (_max <= x1)) // [_min.._max] is within [x0..x1]
            {
                if (_sList == null)
                {
                    _sList = new List<int>();
                }

                _sList.Add(index);
            }
            else
            {
                if ((_left != null) && (x0 <= _left._max) && (x1 >= _left._min)) // overlap with left
                {
                    _left.Insert(index, x0, x1);
                }

                if ((_right != null) && (x0 <= _right._max) && (x1 >= _right._min)) // overlap with right
                {
                    _right.Insert(index, x0, x1);
                }
            }
        }

        public void ReportIntersection(DisplayList dl, int index, double x)
        {
            if (_sList != null)
            {
                foreach (int i in _sList)
                {
                    if (index != i)
                    {
                        dl.ReportOverlapping(index, i);
                    }
                }
            }

            if ((_left != null) && (x >= _left._min) && (x <= _left._max))
            {
                _left.ReportIntersection(dl, index, x);
            }

            if ((_right != null) && (x >= _right._min) && (x <= _right._max))
            {
                _right.ReportIntersection(dl, index, x);
            }
        }
    }

    internal class RectangleIntersection
    {
        protected Coordinate[] _xCoord;  // = null;
        protected int          _xCount;  // = 0;

        protected Coordinate[] _yCoord;  // = null;
        protected int          _yCount;  // = 0;

        static Coordinate[] RemoveDuplication(Coordinate[] values)
        {
            int last = 0;
            int len = values.Length;

            double val = values[last].value;

            for (int i = 1; i < len; i++)
            {
                if (! Double.Equals(values[i].value, val))
                {
                    last++;
                    val = values[i].value;
                }
            }

            if (len != (last + 1))
            {
                Coordinate[] newvalues = new Coordinate[last + 1];

                last = 0;

                newvalues[0] = values[0];

                for (int i = 1; i < len; i++)
                {
                    if (! Double.Equals(values[i].value, newvalues[last].value))
                    {
                        last++;
                        newvalues[last] = values[i];
                    }
                }

                return newvalues;
            }
            else
            {
                return values;
            }
        }

        private void AddPoint(int i, int index, double x, double y)
        {
            Coordinate cx = new Coordinate(x, index);
            Coordinate cy = new Coordinate(y, index);

            _xCoord[i] = cx;
            _yCoord[i] = cy;
        }

        private void SortEndPoints(DisplayList dl, int count)
        {
            _xCoord = new Coordinate[2 * count + 2];
            _yCoord = new Coordinate[2 * count + 2];

            AddPoint(0, -1, Double.MinValue, Double.MinValue);

            int p = 1;

            for (int i = 0; i < count; i++)
            {
                Rect r = dl[i];

                AddPoint(p, i, r.Left, r.Top);
                AddPoint(p + 1, i, r.Right, r.Bottom);

                _xCoord[p].top = _yCoord[p];
                _xCoord[p].bottom = _yCoord[p + 1];

                _xCoord[p + 1].top = _yCoord[p];
                _xCoord[p + 1].bottom = _yCoord[p + 1];

                p += 2;
            }

            AddPoint(p, count + 1, Double.MaxValue, Double.MaxValue);

            Array.Sort(_xCoord, new CoordinateComparer());
            _xCount = _xCoord.Length;

            Array.Sort(_yCoord, new CoordinateComparer());
            _yCount = _yCoord.Length;
        }

        // Input N rectangles -> 2N vertical line segment, 2N horizontal line segments
        // Output all true intersections rectangles
        private void OrthogonalLineSegmentIntersection(DisplayList dl)
        {
            // Sweep through sorted x coordiates from left to right, skipping MinValue/MaxValue
            for (int i = 1; i < _xCount - 1; i++)
            {
                Coordinate c = _xCoord[i];

                bool left = Double.Equals(c.value, dl[c.index].Left);

                // Left endpoint  => insertion into range tree
                // Right endpoint => delection from range tree
                c.top.active = left;
                c.bottom.active = left;

                // Vertical segment [y0..y1] => report [y0..y1] AND range tree
                double y0 = c.top.value;
                double y1 = c.bottom.value;

                int p = Array.BinarySearch(_yCoord, y0, new CoordinateSearcher());

                if (p >= 0)
                {
                    do
                    {
                        if ((_yCoord[p].active) && (c.index != _yCoord[p].index))
                        {
                            dl.ReportOverlapping(c.index, _yCoord[p].index);
                            // Console.WriteLine("{0} {1} intersects ({2},{3})", c.index, _yCoord[p].index, c.value, _yCoord[p].value);
                        }

                        p++;
                    }
                    while (_yCoord[p].value <= y1);
                }
            }
        }

        // Input N rectangles and points (top-left corner of rectangle)
        // Output all R(r, p) where point p is within rectangle r

        private void BatchedRangeSearch(DisplayList dl)
        {
            // 1) Sort all points and left/right rectangle sides w.r.t. x-coordinate
            // 2) Sweep left-to-right while storing y-intervals of rectangles intersecting
            //    the sweepline in a segment tree T
            //    2.1) Left side => insert interval into T
            //    2.2) Right side => delete interval from T
            //    2.3) Point (x, y) stabbing query report all [y1,y2] where y is in [y1..y2]

            Coordinate[] uniqueY = RemoveDuplication(_yCoord);

            SegmentTree st = new SegmentTree(uniqueY, 0, uniqueY.Length);

            // Sweep through sorted x coordiates from left to right, skipping MinValue/MaxValue
            for (int i = 1; i < _xCount - 1; i++)
            {
                Coordinate c = _xCoord[i];

                double y0 = c.top.value;
                double y1 = c.bottom.value;

                if (Double.Equals(c.value, dl[c.index].Left))
                {
                    st.Insert(c.index, y0, y1);

                    st.ReportIntersection(dl, c.index, y0);
                }
                else
                {
                    st.Remove(c.index, y0, y1);
                }
            }
        }

        public void CalculateIntersections(DisplayList dl, int count)
        {
            SortEndPoints(dl, count);

            OrthogonalLineSegmentIntersection(dl);

            BatchedRangeSearch(dl);
        }

#if UNIT_TEST
        internal static void UnitTest()
        {
            Console.WriteLine("RectangleIntersection unit test");

            DisplayList dl = new DisplayList(8.5 * 96, 11 * 96);

            dl.Add( 3,  8,  6, 36);
            dl.Add(25, 34, 34, 38);
            dl.Add(33, 37, 21, 36);
            dl.Add(21, 38, 23, 27);
            dl.Add( 6, 26,  3,  8);
            dl.Add(31, 35, 15, 19);
            dl.Add(23, 38, 11, 14);

            dl.Add(16, 22, 3.5, 7.5);

            RectangleIntersection ri = new RectangleIntersection();

            ri.CalculateIntersections(dl);
        }
#endif
    }
}
