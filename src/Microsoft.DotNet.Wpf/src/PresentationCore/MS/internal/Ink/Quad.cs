// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Windows.Media;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MS.Utility;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Ink
{
    /// <summary>
    /// A helper structure used in StrokeNode and StrokeNodeOperation implementations
    /// to store endpoints of the quad connecting two nodes of a stroke. 
    /// The vertices of a quad are supposed to be clockwise with points A and D located
    /// on the begin node and B and C on the end one.
    /// </summary>
    internal struct Quad
    {
        #region Statics

        private static readonly Quad s_empty = new Quad(new Point(0, 0), new Point(0, 0), new Point(0, 0), new Point(0, 0));

        #endregion 
        
        #region API

        /// <summary> Returns the static object representing an empty (unitialized) quad </summary>
        internal static Quad Empty { get { return s_empty; } }

        /// <summary> Constructor </summary>
        internal Quad(Point a, Point b, Point c, Point d)
        {
            _A = a; _B = b; _C = c; _D = d;
        }

        /// <summary> The A vertex of the quad </summary>
        internal Point A { get { return _A; } set { _A = value; } }
        
        /// <summary> The B vertex of the quad </summary>
        internal Point B { get { return _B; } set { _B = value; } }
        
        /// <summary> The C vertex of the quad </summary>
        internal Point C { get { return _C; } set { _C = value; } }

        /// <summary> The D vertex of the quad </summary>
        internal Point D { get { return _D; } set { _D = value; } }

        // Returns quad's vertex by index where A is of the index 0, B - is 1, etc
        internal Point this[int index]
        {
            get 
            {
                switch (index)
                {
                    case 0: return _A;
                    case 1: return _B;
                    case 2: return _C;
                    case 3: return _D;
                    default:
                        throw new IndexOutOfRangeException("index");
                }
            }
        }

        /// <summary> Tells whether the quad is invalid (empty) </summary>
        internal bool IsEmpty 
        { 
            get { return (_A == _B) && (_C == _D); } 
        }

        internal void GetPoints(List<Point> pointBuffer)
        {
            pointBuffer.Add(_A);
            pointBuffer.Add(_B);
            pointBuffer.Add(_C);
            pointBuffer.Add(_D);
        }
        
        /// <summary> Returns the bounds of the quad </summary>
        internal Rect Bounds 
        {
            get { return IsEmpty ? Rect.Empty : Rect.Union(new Rect(_A, _B), new Rect(_C, _D)); }
        }
        
        #endregion

        #region Fields

        private Point _A;
        private Point _B;
        private Point _C;
        private Point _D;

        #endregion
    }
}
