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
    /// Container for projected geometry (decoupled from ModelRenderer)
    /// </summary>
    internal abstract class ProjectedGeometry
    {
        public ProjectedGeometry()
        {
            this.frontFaceTriangles = new List<Triangle>();
            this.backFaceTriangles = new List<Triangle>();
            this.minUV = new Point(double.MaxValue, double.MaxValue);
            this.maxUV = new Point(double.MinValue, double.MinValue);
        }

        public Rect ScreenSpaceBounds
        {
            get
            {
                Rect bounds = Rect.Empty;
                foreach (Triangle t in frontFaceTriangles)
                {
                    bounds = Rect.Union(bounds, t.Bounds);
                }
                foreach (Triangle t in backFaceTriangles)
                {
                    bounds = Rect.Union(bounds, t.Bounds);
                }
                return bounds;
            }
        }

        /// <summary>
        /// The minimum UV value prior to UV normalization
        /// </summary>
        public Point OriginalMinUV
        {
            get { return minUV; }
        }

        /// <summary>
        /// The maximum UV value prior to UV normalization
        /// </summary>
        public Point OriginalMaxUV
        {
            get { return maxUV; }
        }

        internal List<Edge> FrontFaceSilhouetteEdges
        {
            get
            {
                return edgeFinder.FrontFaceSilhouetteEdges;
            }
        }

        internal List<Edge> BackFaceSilhouetteEdges
        {
            get
            {
                return edgeFinder.BackFaceSilhouetteEdges;
            }
        }

        public Triangle[] FrontFaceTriangles
        {
            get
            {
                Triangle[] result = new Triangle[frontFaceTriangles.Count];
                frontFaceTriangles.CopyTo(result);
                return result;
            }
        }

        public Triangle[] BackFaceTriangles
        {
            get
            {
                Triangle[] result = new Triangle[backFaceTriangles.Count];
                backFaceTriangles.CopyTo(result);
                return result;
            }
        }

        protected Point minUV;
        protected Point maxUV;
        protected List<Triangle> frontFaceTriangles;
        protected List<Triangle> backFaceTriangles;
        internal ISilhouetteEdgeFinder edgeFinder;

        internal interface ISilhouetteEdgeFinder
        {
            void AddTriangleEdges(Triangle t);
            List<Edge> FrontFaceSilhouetteEdges { get; }
            List<Edge> BackFaceSilhouetteEdges { get; }
        }        

        protected abstract class Tessellator
        {        
            protected enum VerticesBehindCamera
            {
                None = 0x0,
                Vertex1 = 0x1,
                Vertex2 = 0x2,
                Vertex1And2 = 0x3,
                Vertex3 = 0x4,
                Vertex1And3 = 0x5,
                Vertex2And3 = 0x6,
                AllVertices = 0x7
            }          

            public Tessellator(Matrix3D projection)
            {
                this.projection = projection;
            }          

            protected Matrix3D projection;
            protected const double distanceAheadOfCamera = 0.000001;
        }
    }
}
