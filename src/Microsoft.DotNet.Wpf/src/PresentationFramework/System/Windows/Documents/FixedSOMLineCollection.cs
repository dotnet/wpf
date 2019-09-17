// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++                                                              
    Description:
        Internal helper class that can store a set of sorted horizontal and vertical FixedSOMLineRanges.
        These ranges are used in construction of FixedBlocks and Tables               
--*/

namespace System.Windows.Documents
{
    using System.Collections.Generic;
    using System.Windows.Shapes;
    using System.Windows.Media;
    using System.Diagnostics;
    using System.Windows;

    //Stores a collection of horizontal and vertical lines sorted by y and x axis respectively

    // Needs performance review: It might be better to use a list while creating FixedSOMLineRanges and then convert them to an array
    //when consuming them, i.e. determinining separation etc. We are doing lots of indexed access at this stage
    
    internal sealed class FixedSOMLineCollection
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------
        
        #region Constructors
        public FixedSOMLineCollection()
        {
            _verticals = new List<FixedSOMLineRanges>();
            _horizontals = new List<FixedSOMLineRanges>();
        }
        #endregion Constructors

        //--------------------------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------------------------
        
#region Public Methods

        public bool IsVerticallySeparated(double left, double top, double right, double bottom)
        {
            return _IsSeparated(_verticals, left, top, right, bottom);
        }

        public bool IsHorizontallySeparated(double left, double top, double right, double bottom)
        {
            return _IsSeparated(_horizontals, top, left, bottom, right);
        }
        
        public void AddVertical(Point point1, Point point2)
        {
            Debug.Assert(point1.X == point2.X);
            _AddLineToRanges(_verticals, point1.X, point1.Y, point2.Y);
        }

        public void AddHorizontal(Point point1, Point point2)
        {
            Debug.Assert(point1.Y == point2.Y);
            _AddLineToRanges(_horizontals, point1.Y, point1.X, point2.X);
        }
        
        #endregion Public Methods

        #region Private Methods
        //Merge line 2 into line 1
        private void _AddLineToRanges(List<FixedSOMLineRanges> ranges, double line, double start, double end)
        {
            if (start > end)
            {
                double temp = start;
                start = end;
                end = temp;
            }

            FixedSOMLineRanges range;
            double maxSeparation = .5 * FixedSOMLineRanges.MinLineSeparation;

            for (int i=0; i < ranges.Count; i++)
            {
                if (line < ranges[i].Line - maxSeparation)
                {
                    range = new FixedSOMLineRanges();
                    range.Line = line;
                    range.AddRange(start, end);
                    ranges.Insert(i, range);
                    return;
                }
                else if (line < ranges[i].Line + maxSeparation)
                {
                    ranges[i].AddRange(start, end);
                    return;
                }
            }

            // add to end
            range = new FixedSOMLineRanges();
            range.Line = line;
            range.AddRange(start, end);
            ranges.Add(range);
            return;
        }

        //Generic function that decides whether or not a rectangle as spefied by the points is
        //divided by any of the lines in the line ranges in the passed in list
        private bool _IsSeparated(List<FixedSOMLineRanges> lines, double parallelLowEnd, double perpLowEnd, double parallelHighEnd, double perpHighEnd)
        {
            int startIndex = 0;
            int endIndex = lines.Count;
            if (endIndex == 0)
            {
                return false;
            }
            int i = 0;

            while (endIndex > startIndex)
            {
                i = (startIndex + endIndex) >> 1;
                if (lines[i].Line < parallelLowEnd)
                {
                    startIndex = i + 1;
                }
                else
                {
                    if (lines[i].Line <= parallelHighEnd)
                    {
                        break;
                    }
                    endIndex = i;    
                }
            }

            if (lines[i].Line >= parallelLowEnd && lines[i].Line <= parallelHighEnd)
            {
                do 
                {
                    i--;
                } while (i>=0 && lines[i].Line >= parallelLowEnd);
                
                i++;
                while (i<lines.Count && lines[i].Line <= parallelHighEnd)
                {
                    double allowedMargin = (perpHighEnd - perpLowEnd)*_fudgeFactor;

                    int rangeIndex = lines[i].GetLineAt(perpLowEnd + allowedMargin);
                    if (rangeIndex >=0)
                    {
                        double end = lines[i].End[rangeIndex];
                        if (end >= perpHighEnd - allowedMargin)
                        {
                            return true;
                        }
                    }
                    i++;
                };
            }
            return false;
        }
        #endregion Private Methods


        #region Public Properties

        public List<FixedSOMLineRanges> HorizontalLines
        {
            get
            {
                return _horizontals;
            }
        }

        public List<FixedSOMLineRanges> VerticalLines
        {
            get
            {
                return _verticals;
            }
        }
        
        #endregion Public Properties

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------
        #region Private Fields
        private List<FixedSOMLineRanges> _horizontals;
        private List<FixedSOMLineRanges> _verticals;
        private const double _fudgeFactor = 0.1; // We allow 10% margin at each end
        #endregion Private Fields
        
    }
}




