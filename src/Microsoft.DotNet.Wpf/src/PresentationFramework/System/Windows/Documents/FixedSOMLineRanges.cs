// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++                                                           
    Description:
        Internal helper class that can store a set of sorted lines by their start and end indices    
--*/

namespace System.Windows.Documents
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Windows.Shapes;
    using System.Windows.Controls;
    using System.Diagnostics;
    using System.Windows.Media;

    internal class FixedSOMLineRanges
    {
        public void AddRange(double start, double end)
        {
            for (int i = 0; i < Start.Count; )
            {
                if (start > End[i] + _minLineSeparation)
                {
                    i++;
                }
                else if (end + _minLineSeparation < Start[i])
                {
                    Start.Insert(i, start);
                    End.Insert(i, end);
                    return;
                }
                else
                {
                    // overlap !!
                    if (Start[i] < start)
                    {
                        start = Start[i];
                    }
                    if (End[i] > end)
                    {
                        end = End[i];
                    }
                    Start.RemoveAt(i);
                    End.RemoveAt(i);
                }
            }
            Start.Add(start);
            End.Add(end);
        }

        public int GetLineAt(double line)
        {
            //use binary search
            int startIndex = 0;
            int endIndex = Start.Count - 1;
            while (endIndex > startIndex)
            {
                int i = (startIndex + endIndex) >> 1;
                // Invariant: i < endIndex
                if (line > End[i])
                {
                    startIndex = i + 1;
                }
                else
                {
                    endIndex = i;
                }
            }

            if (startIndex == endIndex && line <= End[startIndex] && line >= Start[startIndex])
            {
                return startIndex;
            }
            else
            {
                return -1;
            }
        }

        public double Line
        {
            set { _line = value; }
            get { return _line; }
        }

        public List<double> Start
        {
            get 
            {
                if (_start == null)
                {
                    _start = new List<double>();
                }
                return _start; 
            }
        }

        public List<double> End
        {
            get
            {
                if (_end == null)
                {
                    _end = new List<double>();
                }
                return _end;
            }
        }

        public int Count
        {
            get { return Start.Count; }
        }

        static public double MinLineSeparation
        {
            get { return _minLineSeparation; }
        }

        private double _line; // X or Y value for set of lines
        private List<double> _start; // where lines start.  Invariant: _start[i] < _end[i]
        private List<double> _end; // where lines end.  Invariant: _end[i] < _start[i+1]

        private const double _minLineSeparation = 3; // lines closer than this are considered one line
    }
}


