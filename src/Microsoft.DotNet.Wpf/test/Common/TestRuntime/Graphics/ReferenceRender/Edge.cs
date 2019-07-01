// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics.ReferenceRender
{
    /// <summary>
    /// Abstraction for a triangle edge
    /// </summary>
    internal class Edge
    {
        public Edge(Vertex v1, Vertex v2)
        {
            start = v1.ProjectedPosition;
            end = v2.ProjectedPosition;
        }

        public Edge(Point3D projectedPosition1, Point3D projectedPosition2)
        {
            start = projectedPosition1;
            end = projectedPosition2;
        }

        public override int GetHashCode()
        {
            return start.GetHashCode() ^ end.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Edge)
            {
                Edge e = obj as Edge;
                return (!MathEx.NotCloseEnough(e.start, start) && !MathEx.NotCloseEnough(e.end, end))
                    || (!MathEx.NotCloseEnough(e.start, end) && !MathEx.NotCloseEnough(e.end, start));
            }
            return false;
        }

        public Point3D Start
        {
            get { return start; }
        }

        public Point3D End
        {
            get { return end; }
        }

        private Point3D start;
        private Point3D end;
    }
}


