// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics.ReferenceRender
{    
    /// <summary>
    /// Container for screen space lines projected geometry
    /// </summary>
    internal class SSLProjectedGeometry : ProjectedGeometry
    {
        public SSLProjectedGeometry(Matrix3D projection)
        {
            this.edgeFinder = new SSLSilhouetteEdgeFinder();
            this.tessellator = new SSLTessellator(projection);
        }

        public void AddScreenSpaceLine(Point3D begin, Point3D end, double thickness, Color color)
        {
            foreach (Triangle triangle in tessellator.CreateScreenSpaceLines(begin, end, thickness, color))
            {
                if (triangle.IsClipped)
                {
                    continue;
                }

                frontFaceTriangles.Add(triangle);
                edgeFinder.AddTriangleEdges(triangle);
            }
        }

        private SSLTessellator tessellator;
       
        private class SSLSilhouetteEdgeFinder : ISilhouetteEdgeFinder
        {           
            public SSLSilhouetteEdgeFinder()
            {
                edges = new List<Edge>();
            }            

            public void AddTriangleEdges(Triangle t)
            {
                // There are four edges on a ScreenSpaceLine (represented by two triangles per line)
                // The edge from vertex2 -> vertex3 is along the hypotenuse and should not be included.

                AddEdge(new Edge(t.vertex1, t.vertex2));
                AddEdge(new Edge(t.vertex1, t.vertex3));

                AddEdge(t.NearPlaneEdge);
                AddEdge(t.FarPlaneEdge);
            }            

            private void AddEdge(Edge e)
            {
                if (e == null)
                {
                    return;
                }

                edges.Add(e);
            }            

            public List<Edge> FrontFaceSilhouetteEdges
            {
                get { return edges; }
            }            

            public List<Edge> BackFaceSilhouetteEdges
            {
                // ScreenSpaceLines do not have back faces
                get { return new List<Edge>(); }
            }            

            private List<Edge> edges;
        }        

        private class SSLTessellator : Tessellator
        {            
            public SSLTessellator(Matrix3D projection)
                : base(projection)
            {
            }            

            public Triangle[] CreateScreenSpaceLines(Point3D begin, Point3D end, double thickness, Color color)
            {
                VerticesBehindCamera behind = VerticesBehindCamera.None;

                if (begin.Z >= 0)
                {
                    behind |= VerticesBehindCamera.Vertex1;
                }
                if (end.Z >= 0)
                {
                    behind |= VerticesBehindCamera.Vertex2;
                }

                switch (behind)
                {
                    case VerticesBehindCamera.Vertex1:
                        {
                            // 'begin' is behind the camera.  Replace it with a new point that isn't.
                            Weights weights = Interpolator.GetWeightsToXYPlane(begin, end, -distanceAheadOfCamera);
                            begin = Interpolator.WeightedSum(begin, end, weights);
                            break;
                        }
                    case VerticesBehindCamera.Vertex2:
                        {
                            // 'end' is behind the camera.  Replace it with a new point that isn't.
                            Weights weights = Interpolator.GetWeightsToXYPlane(begin, end, -distanceAheadOfCamera);
                            end = Interpolator.WeightedSum(begin, end, weights);
                            break;
                        }

                    case VerticesBehindCamera.Vertex1And2:
                        return new Triangle[0];

                    default:
                        break;
                }
                return new Triangle[]{ new ScreenSpaceLineTriangle( begin, end, thickness, color, projection ),
                                       new ScreenSpaceLineTriangle( end, begin, thickness, color, projection ) };
            }
        }
    }
}
