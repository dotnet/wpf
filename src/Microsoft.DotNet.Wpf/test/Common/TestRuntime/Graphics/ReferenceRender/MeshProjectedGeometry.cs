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
    /// Container for mesh projected geometry
    /// </summary>
    internal class MeshProjectedGeometry : ProjectedGeometry
    {
        public MeshProjectedGeometry(Matrix3D projection)
        {
            this.edgeFinder = new MeshSilhouetteEdgeFinder();
            this.tessellator = new MeshTessellator(projection);
        }

        public void AddTriangle(Vertex v1, Vertex v2, Vertex v3)
        {
            minUV = MathEx.Min(minUV, v1.TextureCoordinates, v2.TextureCoordinates, v3.TextureCoordinates);
            maxUV = MathEx.Max(maxUV, v1.TextureCoordinates, v2.TextureCoordinates, v3.TextureCoordinates);

            foreach (Triangle triangle in tessellator.CreateTriangles(v1, v2, v3))
            {
                // We discard triangles that are outside the 0-1 Z-range
                //  or have two or more (near) identical vertices.
                if (triangle.IsClipped || triangle.IsDegenerate)
                {
                    continue;
                }

                // We keep two lists: front ( a.k.a. CCW ) and back ( a.k.a CW ) facing triangles
                if (triangle.IsBackFacing)
                {
                    // Back facing ... flip normals for rendering
                    triangle.vertex1.Normal *= -1;
                    triangle.vertex2.Normal *= -1;
                    triangle.vertex3.Normal *= -1;
                    backFaceTriangles.Add(triangle);
                }
                else if (triangle.IsFrontFacing)
                {
                    // Front facing
                    frontFaceTriangles.Add(triangle);
                }
                else
                {
                    // else skip it (degenerate triangle which doesn't face either direction)
                    continue;
                }

                edgeFinder.AddTriangleEdges(triangle);
            }
        }

        public void NormalizeTextureCoordinates()
        {
            // Shortcuts for common cases 
            if (MathEx.AreCloseEnough(minUV, new Point()))
            {
                if (MathEx.AreCloseEnough(maxUV, new Point()) ||
                     MathEx.AreCloseEnough(maxUV, new Point(1, 1)))
                {
                    // pre-normalized or no UV's
                    return;
                }
            }

            double scaleX = maxUV.X - minUV.X;
            double scaleY = maxUV.Y - minUV.Y;
            scaleX = MathEx.AreCloseEnough(scaleX, 0.0) ? 0.0 : 1.0 / scaleX;
            scaleY = MathEx.AreCloseEnough(scaleY, 0.0) ? 0.0 : 1.0 / scaleY;

            double offsetX = -minUV.X * scaleX;
            double offsetY = -minUV.Y * scaleY;

            foreach (Triangle t in frontFaceTriangles)
            {
                t.vertex1.TextureCoordinates.X = offsetX + (t.vertex1.U * scaleX);
                t.vertex1.TextureCoordinates.Y = offsetY + (t.vertex1.V * scaleY);
                t.vertex2.TextureCoordinates.X = offsetX + (t.vertex2.U * scaleX);
                t.vertex2.TextureCoordinates.Y = offsetY + (t.vertex2.V * scaleY);
                t.vertex3.TextureCoordinates.X = offsetX + (t.vertex3.U * scaleX);
                t.vertex3.TextureCoordinates.Y = offsetY + (t.vertex3.V * scaleY);
            }
            foreach (Triangle t in backFaceTriangles)
            {
                t.vertex1.TextureCoordinates.X = offsetX + (t.vertex1.U * scaleX);
                t.vertex1.TextureCoordinates.Y = offsetY + (t.vertex1.V * scaleY);
                t.vertex2.TextureCoordinates.X = offsetX + (t.vertex2.U * scaleX);
                t.vertex2.TextureCoordinates.Y = offsetY + (t.vertex2.V * scaleY);
                t.vertex3.TextureCoordinates.X = offsetX + (t.vertex3.U * scaleX);
                t.vertex3.TextureCoordinates.Y = offsetY + (t.vertex3.V * scaleY);
            }
        }

        private MeshTessellator tessellator;

        private class MeshSilhouetteEdgeFinder : ISilhouetteEdgeFinder
        {  

            public MeshSilhouetteEdgeFinder()
            {
                // The hashtables use the edge as the key and
                // the number of triangles an edge is on as the value
                backFaceEdges = new Hashtable();
                frontFaceEdges = new Hashtable();
            }

            public void AddTriangleEdges(Triangle t)
            {
                if (t.IsBackFacing)
                {
                    AddEdge(new Edge(t.vertex1, t.vertex2), backFaceEdges);
                    AddEdge(new Edge(t.vertex2, t.vertex3), backFaceEdges);
                    AddEdge(new Edge(t.vertex3, t.vertex1), backFaceEdges);
                    AddEdge(t.NearPlaneEdge, backFaceEdges);
                    AddEdge(t.FarPlaneEdge, backFaceEdges);
                }
                else
                {
                    AddEdge(new Edge(t.vertex1, t.vertex2), frontFaceEdges);
                    AddEdge(new Edge(t.vertex2, t.vertex3), frontFaceEdges);
                    AddEdge(new Edge(t.vertex3, t.vertex1), frontFaceEdges);
                    AddEdge(t.NearPlaneEdge, frontFaceEdges);
                    AddEdge(t.FarPlaneEdge, frontFaceEdges);
                }
            }

            private void AddEdge(Edge e, Hashtable edges)
            {
                if (e == null)
                {
                    return;
                }

                if (edges.Contains(e))
                {
                    // edge is already in the list, increment count
                    edges[e] = ((int)edges[e]) + 1;
                }
                else
                {
                    // new edge, add it
                    edges.Add(e, 1);
                }
            }

            public List<Edge> FrontFaceSilhouetteEdges
            {
                get
                {
                    List<Edge> outlineEdges = new List<Edge>();
                    foreach (Edge e in frontFaceEdges.Keys)
                    {
                        if ((int)frontFaceEdges[e] == 1)
                        {
                            outlineEdges.Add(e);
                        }
                    }
                    return outlineEdges;
                }
            }            

            public List<Edge> BackFaceSilhouetteEdges
            {
                get
                {
                    List<Edge> outlineEdges = new List<Edge>();
                    foreach (Edge e in backFaceEdges.Keys)
                    {
                        if ((int)backFaceEdges[e] == 1)
                        {
                            outlineEdges.Add(e);
                        }
                    }
                    return outlineEdges;
                }
            }            

            private Hashtable backFaceEdges;
            private Hashtable frontFaceEdges;
        }        

        private class MeshTessellator : Tessellator
        {            
            public MeshTessellator(Matrix3D projection)
                : base(projection)
            {
            }            

            public Triangle[] CreateTriangles(Vertex v1, Vertex v2, Vertex v3)
            {
                VerticesBehindCamera behind = VerticesBehindCamera.None;

                if (v1.PositionZ >= 0)
                {
                    behind |= VerticesBehindCamera.Vertex1;
                }
                if (v2.PositionZ >= 0)
                {
                    behind |= VerticesBehindCamera.Vertex2;
                }
                if (v3.PositionZ >= 0)
                {
                    behind |= VerticesBehindCamera.Vertex3;
                }

                // Set v1 (and possibly v2) as the clipped vertex (vertices)
                // We do this to simplify the tessellation code (in the 'switch' block below)
                ReorderVertices(ref v1, ref v2, ref v3, behind);

                switch (behind)
                {
                    case VerticesBehindCamera.Vertex1:
                    case VerticesBehindCamera.Vertex2:
                    case VerticesBehindCamera.Vertex3:
                        {
                            //  The two triangles created are 2-3-4 and 2-4-5
                            //  Vertices 4 and 5 are actually just in front of the camera plane (unlike the picture shows)
                            //
                            //     3---------2   ^ camera look direction
                            //      \    ." /    |
                            //       \ ."  /     |
                            //  <-----4---5-------> camera plane (NOT the near clipping plane)
                            //         \ /
                            //          1          behind the camera
                            //
                            Vertex v4 = GetVertexAheadOfCamera(v1, v3);
                            Vertex v5 = GetVertexAheadOfCamera(v1, v2);
                            return new Triangle[]{ new Triangle( v2, v3, v4, projection ),
                                               new Triangle( v2, v4, v5, projection ) };
                        }

                    case VerticesBehindCamera.Vertex1And2:
                    case VerticesBehindCamera.Vertex1And3:
                    case VerticesBehindCamera.Vertex2And3:
                        {
                            // The triangle created is 3-4-5
                            // Vertices 4 and 5 are actually just in front of the camera plane (unlike the picture shows)
                            //
                            //          3        ^ camera look direction
                            //         / \       |
                            //        /   \      |
                            //  <----4-----5------> camera plane (NOT the near clipping plane)
                            //      /       \
                            //     1---------2      behind the camera

                            Vertex v4 = GetVertexAheadOfCamera(v1, v3);
                            Vertex v5 = GetVertexAheadOfCamera(v2, v3);
                            return new Triangle[] { new Triangle(v3, v4, v5, projection) };
                        }

                    case VerticesBehindCamera.AllVertices:
                        return new Triangle[0];

                    case VerticesBehindCamera.None:
                    default:
                        return new Triangle[] { new Triangle(v1, v2, v3, projection) };
                }
            }            

            private void ReorderVertices(ref Vertex v1, ref Vertex v2, ref Vertex v3, VerticesBehindCamera behind)
            {
                Vertex temp = null;

                switch (behind)
                {
                    case VerticesBehindCamera.None:
                    case VerticesBehindCamera.AllVertices:
                    case VerticesBehindCamera.Vertex1:
                    case VerticesBehindCamera.Vertex1And2:
                        // Do nothing
                        break;

                    case VerticesBehindCamera.Vertex2:
                    case VerticesBehindCamera.Vertex2And3:
                        // Rotate CCW
                        temp = v1;
                        v1 = v2;
                        v2 = v3;
                        v3 = temp;
                        break;

                    case VerticesBehindCamera.Vertex3:
                    case VerticesBehindCamera.Vertex1And3:
                        // Rotate CW
                        temp = v1;
                        v1 = v3;
                        v3 = v2;
                        v2 = temp;
                        break;
                }
            }            

            private Vertex GetVertexAheadOfCamera(Vertex v1, Vertex v2)
            {
                // At this point, being ahead of camera is being in -z
                Weights weights = Interpolator.GetWeightsToXYPlane(v1.PositionAsPoint3D, v2.PositionAsPoint3D, -distanceAheadOfCamera);

                Vertex result = new Vertex();
                result.Color = Interpolator.WeightedSum(v1.Color, v2.Color, weights);
                result.ColorTolerance = Interpolator.WeightedSum(v1.ColorTolerance, v2.ColorTolerance, weights);
                result.ModelSpacePosition = Interpolator.WeightedSum(v1.ModelSpacePosition, v2.ModelSpacePosition, weights);
                result.Position = Interpolator.WeightedSum(v1.Position, v2.Position, weights);
                result.TextureCoordinates = Interpolator.WeightedSum(v1.TextureCoordinates, v2.TextureCoordinates, weights);

                result.ModelSpaceNormal = MathEx.Normalize(Interpolator.WeightedSum(v1.ModelSpaceNormal, v2.ModelSpaceNormal, weights));
                result.Normal = MathEx.Normalize(Interpolator.WeightedSum(v1.Normal, v2.Normal, weights));

                return result;
            }
        }
    }
}