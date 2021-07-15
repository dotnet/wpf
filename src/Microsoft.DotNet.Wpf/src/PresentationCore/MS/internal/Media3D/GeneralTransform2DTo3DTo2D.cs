// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Markup;

using System.Windows.Media;
using System.Windows.Media.Media3D;

using MS.Internal.PresentationCore;
using MS.Internal.Media3D;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Media3D
{
    /// <summary>
    /// Helper class that encapsulates return data needed for the
    /// hit test capture methods.
    /// </summary>
    internal class HitTestEdge
    {
        /// <summary>
        /// Constructs a new hit test edge
        /// </summary>
        /// <param name="p1">First edge point</param>
        /// <param name="p2">Second edge point</param>
        /// <param name="uv1">Texture coordinate of first edge point</param>
        /// <param name="uv2">Texture coordinate of second edge point</param>
        public HitTestEdge(Point3D p1,
                           Point3D p2,
                           Point uv1,
                           Point uv2)
        {
            _p1 = p1;
            _p2 = p2;

            _uv1 = uv1;
            _uv2 = uv2;
        }

        /// <summary>
        /// Projects the stored 3D points in to 2D.
        /// </summary>
        /// <param name="objectToViewportTransform">The transformation matrix to use</param>
        public void Project(GeneralTransform3DTo2D objectToViewportTransform)
        {
            Point projPoint1 = objectToViewportTransform.Transform(_p1);
            Point projPoint2 = objectToViewportTransform.Transform(_p2);

            _p1Transformed = new Point(projPoint1.X, projPoint1.Y);
            _p2Transformed = new Point(projPoint2.X, projPoint2.Y);
        }

        internal Point3D _p1, _p2;
        internal Point _uv1, _uv2;

        // the transformed Point3D value
        internal Point _p1Transformed, _p2Transformed;
    }
    
    /// <summary>
    /// This transform allows one to go from 2D through 3D and back in to 2D
    /// </summary>
    internal class GeneralTransform2DTo3DTo2D : GeneralTransform
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="visual3D">The Visual3D that contains the 2D visual</param>
        /// <param name="fromVisual">The visual on the Visual3D</param>
        internal GeneralTransform2DTo3DTo2D(Viewport2DVisual3D visual3D, Visual fromVisual)
        {
            IsInverse = false;

            // get a copy of the geometry information - we store our own model to reuse hit
            // test code on the GeometryModel3D
            _geometry = new MeshGeometry3D();
            _geometry.Positions = visual3D.InternalPositionsCache;
            _geometry.TextureCoordinates = visual3D.InternalTextureCoordinatesCache;
            _geometry.TriangleIndices = visual3D.InternalTriangleIndicesCache;
            _geometry.Freeze();

            Visual visual3Dchild = visual3D.Visual;

            // Special case - Setting CacheMode on V2DV3D causes an internal switch from using a VisualBrush
            // to using a BitmapCacheBrush.  It also introduces an extra 2D Visual in the Visual tree above 
            // the V2DV3D.Visual, but this extra node has no effect on transforms and can safely be ignored.  
            // The transform returned will be identical to the one created for calling TransformTo* with
            // the V2DV3D.Visual itself.
            Visual descendentVisual = (fromVisual == visual3Dchild._parent) ? visual3Dchild : fromVisual;

            // get a copy of the size of the visual brush and the rect on the 
            // visual that the transform is going to/from

            _visualBrushBounds = visual3Dchild.CalculateSubgraphRenderBoundsOuterSpace();
            _visualBounds = descendentVisual.CalculateSubgraphRenderBoundsInnerSpace();

            // get the transform that will let us go from the fromVisual to its last 2D
            // parent before it reaches the 3D part of the graph (i.e. visual3D.Child)
            GeneralTransformGroup transformGroup = new GeneralTransformGroup();
            transformGroup.Children.Add(descendentVisual.TransformToAncestor(visual3Dchild));
            transformGroup.Children.Add(visual3Dchild.TransformToOuterSpace());
            transformGroup.Freeze();
            _transform2D = transformGroup;

            // store the inverse as well
            _transform2DInverse = (GeneralTransform)_transform2D.Inverse;
            if (_transform2DInverse != null)
            {
                _transform2DInverse.Freeze();
            }

            // make a copy of the camera and other values on the Viewport3D
            Viewport3DVisual viewport3D = (Viewport3DVisual)VisualTreeHelper.GetContainingVisual2D(visual3D);

            _camera = viewport3D.Camera;
            if (_camera != null)
            {
                _camera = (Camera)viewport3D.Camera.GetCurrentValueAsFrozen();
            }
            
            _viewSize = viewport3D.Viewport.Size;
            _boundingRect = viewport3D.ComputeSubgraphBounds3D();
            _objectToViewport = visual3D.TransformToAncestor(viewport3D);

            // if the transform was not possible, it could be null - check before freezing
            if (_objectToViewport != null)
            {
                _objectToViewport.Freeze();
            }
            
            // store the needed transformations for the various operations
            _worldTransformation = M3DUtil.GetWorldTransformationMatrix(visual3D);

            _validEdgesCache = null;            
        }

        internal GeneralTransform2DTo3DTo2D()
        {
        }
        
        /// <summary>
        /// Transforms a point
        /// </summary>
        /// <param name="inPoint">input point</param>
        /// <param name="result">output point</param>
        /// <returns>false if the point cannot be transformed</returns>
        public override bool TryTransform(Point inPoint, out Point result)
        {
            if (IsInverse)
            {
                return TryInverseTransform(inPoint, out result);
            }
            else
            {
                return TryRegularTransform(inPoint, out result);
            }
        }

        /// <summary>
        /// Performs the transform that goes from the parent Viewport3DVisual down in to the 2D on 3D
        /// contained within the 3D scene
        /// </summary>
        /// <param name="inPoint">input point</param>
        /// <param name="result">output point</param>
        /// <returns>false if the point cannot be transformed</returns>
        private bool TryInverseTransform(Point inPoint, out Point result)
        {
            // set up the hit test parameters            
            double distanceAdjust;
            bool foundIntersection = false;
            
            if (_camera != null)
            {
                RayHitTestParameters rayHitTestParameters = _camera.RayFromViewportPoint(inPoint,
                                                                                         _viewSize,
                                                                                         _boundingRect,
                                                                                         out distanceAdjust);
                rayHitTestParameters.PushVisualTransform(new MatrixTransform3D(_worldTransformation));

                // perfrom the hit test
                // no back material so we only need to concern ourselves with the front faces
                Point pointHit = new Point();
                _geometry.RayHitTest(rayHitTestParameters, FaceType.Front);
                rayHitTestParameters.RaiseCallback(delegate (HitTestResult rawresult)
                                                   {
                                                       RayHitTestResult rayResult = rawresult as RayHitTestResult;

                                                       if (rayResult != null)
                                                       {
                                                           foundIntersection = Viewport2DVisual3D.GetIntersectionInfo(rayResult, out pointHit);
                                                       }

                                                       return HitTestResultBehavior.Stop;
                                                   },
                                                   null,
                                                   HitTestResultBehavior.Continue,
                                                   distanceAdjust);

                // perform capture positioning if we didn't hit anything and something has capture
                if (!foundIntersection)
                {
                    foundIntersection = HandleOffMesh(inPoint, out pointHit);                    
                }
                
                // compute final point
                result = Viewport2DVisual3D.TextureCoordsToVisualCoords(pointHit, _visualBrushBounds);        
            }
            else
            {
                result = new Point();
            }

            return foundIntersection;
        }
        
        /// <summary>
        /// Function to deal with mouse capture when off the mesh.
        /// </summary>
        /// <param name="mousePos">The location of the mouse</param>
        /// <param name="outPoint">output point</param>
        private bool HandleOffMesh(Point mousePos, out Point outPoint)
        {
            Point[] visCorners = new Point[4];

            if (_validEdgesCache == null)
            {
                // get the points relative to the parent
                visCorners[0] = _transform2D.Transform(new Point(_visualBounds.Left,  _visualBounds.Top));
                visCorners[1] = _transform2D.Transform(new Point(_visualBounds.Right, _visualBounds.Top));         
                visCorners[2] = _transform2D.Transform(new Point(_visualBounds.Right, _visualBounds.Bottom));
                visCorners[3] = _transform2D.Transform(new Point(_visualBounds.Left,  _visualBounds.Bottom));

                // get the u,v texture coordinate values of the above points
                Point[] texCoordsOfInterest = new Point[4];
                for (int i = 0; i < visCorners.Length; i++)
                {
                    texCoordsOfInterest[i] = Viewport2DVisual3D.VisualCoordsToTextureCoords(visCorners[i], _visualBrushBounds);
                }

                // get the edges that map to the given visual
                _validEdgesCache = GrabValidEdges(texCoordsOfInterest);
            }
            
            // find the closest intersection of the mouse position and the edge list
            return FindClosestIntersection(mousePos, _validEdgesCache, out outPoint);
        }

        /// <summary>
        /// Function takes the passed in list of texture coordinate points, and then finds the 
        /// visible outline of the rectangle specified by those points and returns it.
        /// </summary>
        /// <param name="visualTexCoordBounds">The points specifying the rectangle to search for</param>
        /// <returns>The edges of that rectangle</returns>
        private List<HitTestEdge> GrabValidEdges(Point[] visualTexCoordBounds)
        {
            // our final edge list
            List<HitTestEdge> hitTestEdgeList = new List<HitTestEdge>();
            Dictionary<Edge, EdgeInfo> adjInformation = new Dictionary<Edge, EdgeInfo>();

            // store some important info in local variables for easier access
            Point3DCollection positions = _geometry.Positions;
            PointCollection textureCoords = _geometry.TextureCoordinates;
            Int32Collection triIndices = _geometry.TriangleIndices;

            // if positions and texture coordinates are null, we can't really find what we need so return immediately
            if (positions == null || textureCoords == null)
            {
                return new List<HitTestEdge>();
            }
            
            // this call actually gets the object to camera transform, but we will invert it later, and because of that
            // the local variable is named cameraToObjecTransform.
            Matrix3D cameraToObjectTransform = _worldTransformation * _camera.GetViewMatrix();
            try
            {
                cameraToObjectTransform.Invert();
            }
            catch (InvalidOperationException)
            {
                return new List<HitTestEdge>();
            }

            Point3D camPosObjSpace = cameraToObjectTransform.Transform(new Point3D(0, 0, 0));

            // get the bounding box around the passed in texture coordinates to help
            // with early rejection tests
            Rect bbox = Rect.Empty;
            for (int i = 0; i < visualTexCoordBounds.Length; i++)
            {
                bbox.Union(visualTexCoordBounds[i]);
            }

            // walk through the triangles - and look for the triangles we care about
            Point3D[] triangleVertices = new Point3D[3];
            Point[] triangleTexCoords = new Point[3];

            // switch depending on if the mesh is indexed or not
            if (triIndices == null || triIndices.Count == 0)
            {
                int texCoordCount = textureCoords.Count;
                
                // in this case we have a non-indexed mesh
                int count = positions.Count;
                count = count - (count % 3);

                for (int i = 0; i < count; i+=3)
                {
                    // get the triangle indices
                    Rect triBBox = Rect.Empty;

                    for (int j = 0; j < 3; j++)
                    {
                        triangleVertices[j] = positions[i + j];

                        if (i + j < texCoordCount)
                        {
                            triangleTexCoords[j] = textureCoords[i + j];
                        }
                        else
                        {
                            // In the case you have less texture coordinates than positions, MIL will set
                            // missing ones to be 0,0.  We do the same to stay consistent. 
                            // See CMILMesh3D::CopyTextureCoordinatesFromDoubles
                            triangleTexCoords[j] = new Point(0,0);
                        }

                        triBBox.Union(triangleTexCoords[j]);
                    }

                    if (bbox.IntersectsWith(triBBox))
                    {
                        ProcessTriangle(triangleVertices, triangleTexCoords, visualTexCoordBounds, hitTestEdgeList, adjInformation, camPosObjSpace);
                    }
                }
            }
            else
            {
                // in this case we have an indexed mesh
                int count = triIndices.Count;
                int posLimit = positions.Count;
                int texCoordLimit = textureCoords.Count;
                int[] indices = new int[3];
            
                for (int i = 2; i < count; i += 3)
                {
                    // get the triangle indices
                    Rect triBBox = Rect.Empty;

                    bool validTextureCoordinates = true;
                    bool validPositions = true;
                    for (int j = 0; j < 3; j++)
                    {
                        // subtract 2 to take in to account we start i
                        // at the high range of indices
                        indices[j] = triIndices[(i-2) + j];

                        // if a point or texture coordinate is out of range, end early since this is an error
                        if (indices[j] < 0 || indices[j] >= posLimit)
                        {
                            validPositions = false;
                            break; 
                        }
                        if (indices[j] < 0 || indices[j] >= texCoordLimit)
                        {
                            validTextureCoordinates = false;
                            break;
                        }
            
                        triangleVertices[j] = positions[indices[j]];
                        triangleTexCoords[j] = textureCoords[indices[j]];

                        triBBox.Union(triangleTexCoords[j]);
                    }

                    // if the positions were ever invalid, we stop processing - see MeshGeometry3D RayHitTestIndexedList
                    // for reasoning
                    if (!validPositions)
                    {
                        break;
                    }

                    if (validTextureCoordinates && bbox.IntersectsWith(triBBox))
                    {
                        ProcessTriangle(triangleVertices, triangleTexCoords, visualTexCoordBounds, hitTestEdgeList, adjInformation, camPosObjSpace);
                    }
                }
            }
            
            // also handle the case of an edge that doesn't also have a backface - i.e a single plane            
            foreach (Edge edge in adjInformation.Keys)
            {
                EdgeInfo ei = adjInformation[edge];

                if (ei._hasFrontFace && ei._numSharing == 1)
                {
                    HandleSilhouetteEdge(ei._uv1, ei._uv2,
                                         edge._start, edge._end,
                                         visualTexCoordBounds,
                                         hitTestEdgeList);
                }
            }

            // project all the edges to get at the 2D point of interest
            if (_objectToViewport != null)

            {
                for (int i = 0; i < hitTestEdgeList.Count; i++)
                {
                    hitTestEdgeList[i].Project(_objectToViewport);
                }
            }
            else
            {
                hitTestEdgeList = new List<HitTestEdge>();
            }

            return hitTestEdgeList;
        }

        /// <summary>
        /// Processes the passed in triangle by checking to see if it is facing the camera and if
        /// so searches to see if the texture coordinate edges intersect it.  It also looks
        /// to see if there are any silhouette edges and processes these as well.
        /// </summary>
        /// <param name="p">The triangle's vertices</param>
        /// <param name="uv">The texture coordinates for those vertices</param>   
        /// <param name="visualTexCoordBounds">The texture coordinate edges to intersect with</param>
        /// <param name="edgeList">The edge list that results should be placed on</param>
        /// <param name="adjInformation">The adjacency information for the mesh</param>
        /// <param name="camPosObjSpace"></param>
        private void ProcessTriangle(Point3D[] p,
                                     Point[] uv,
                                     Point[] visualTexCoordBounds,
                                     List<HitTestEdge> edgeList,
                                     Dictionary<Edge, EdgeInfo> adjInformation,
                                     Point3D camPosObjSpace)
        {
            // calculate the normal of the mesh and the vector from a point on the mesh to the camera 
            // for back face removal calculations.
            Vector3D normal = Vector3D.CrossProduct(p[1] - p[0], p[2] - p[0]);
            Vector3D dirToCamera = camPosObjSpace - p[0];

            // ignore any triangles that have a normal of (0,0,0)
            if (!(normal.X == 0 && normal.Y == 0 && normal.Z == 0))
            {
                double dotProd = Vector3D.DotProduct(normal, dirToCamera);

                // if the dot product is > 0 then the triangle is visible, otherwise invisible
                if (dotProd > 0.0)
                {
                    // loop over the triangle and update any edge information
                    ProcessTriangleEdges(p, uv, visualTexCoordBounds, PolygonSide.FRONT, edgeList, adjInformation);

                    // intersect the bounds of the visual with the triangle
                    ProcessVisualBoundsIntersections(p, uv, visualTexCoordBounds, edgeList);
                }
                else
                {
                    ProcessTriangleEdges(p, uv, visualTexCoordBounds, PolygonSide.BACK, edgeList, adjInformation);
                }
            }
        }

        /// <summary>
        /// Function intersects the edges specified by tc with the texture coordinates
        /// on the passed in triangle.  If there are any intersections, the edges
        /// of these intersections are added to the edgelist
        /// </summary>
        /// <param name="p">The vertices of the triangle</param>
        /// <param name="uv">The texture coordinates for that triangle</param>
        /// <param name="visualTexCoordBounds">The texture coordinate edges to be intersected against</param>
        /// <param name="edgeList">The list of edges any intersecte edges should be added to</param>
        private void ProcessVisualBoundsIntersections(Point3D[] p,
                                                      Point[] uv,
                                                      Point[] visualTexCoordBounds,
                                                      List<HitTestEdge> edgeList)
        {
            Debug.Assert(uv.Length == p.Length, "vertices and texture coordinate sizes should match");
            
            List<Point3D> pointList = new List<Point3D>();
            List<Point> uvList = new List<Point>();

            // loop over the visual's texture coordinate bounds
            for (int i = 0; i < visualTexCoordBounds.Length; i++)
            {
                Point visEdgeStart = visualTexCoordBounds[i];
                Point visEdgeEnd = visualTexCoordBounds[(i + 1) % visualTexCoordBounds.Length];

                // clear out anything that used to be there
                pointList.Clear();
                uvList.Clear();

                // loop over triangle edges
                bool skipListProcessing = false;
                for (int j = 0; j < uv.Length; j++)
                {
                    Point uv1 = uv[j];
                    Point uv2 = uv[(j + 1) % uv.Length];
                    Point3D p3D1 = p[j];
                    Point3D p3D2 = p[(j + 1) % p.Length];

                    // initial rejection processing
                    if (!((Math.Max(visEdgeStart.X, visEdgeEnd.X) < Math.Min(uv1.X, uv2.X)) ||
                          (Math.Min(visEdgeStart.X, visEdgeEnd.X) > Math.Max(uv1.X, uv2.X)) ||
                          (Math.Max(visEdgeStart.Y, visEdgeEnd.Y) < Math.Min(uv1.Y, uv2.Y)) ||
                          (Math.Min(visEdgeStart.Y, visEdgeEnd.Y) > Math.Max(uv1.Y, uv2.Y))))
                    {
                        // intersect the two lines
                        bool areCoincident = false;
                        Vector dir = uv2 - uv1;
                        double t = IntersectRayLine(uv1, dir, visEdgeStart, visEdgeEnd, out areCoincident);

                        // if they are coincident then we have two intersections and don't need to
                        // do anymore processing
                        if (areCoincident)
                        {
                            HandleCoincidentLines(visEdgeStart, visEdgeEnd,
                                                  p3D1, p3D2,
                                                  uv1, uv2, edgeList);
                            skipListProcessing = true;
                            break;
                        }
                        else if (t >= 0 && t <= 1)
                        {
                            Point intersUV = uv1 + dir * t;
                            Point3D intersPoint3D = p3D1 + (p3D2 - p3D1) * t;

                            double visEdgeDiff = (visEdgeStart - visEdgeEnd).Length;

                            if ((intersUV - visEdgeStart).Length < visEdgeDiff &&
                                (intersUV - visEdgeEnd).Length < visEdgeDiff)
                            {
                                pointList.Add(intersPoint3D);
                                uvList.Add(intersUV);
                            }
                        }
                    }
                }

                if (!skipListProcessing)
                {
                    if (pointList.Count >= 2)
                    {
                        edgeList.Add(new HitTestEdge(pointList[0], pointList[1],
                                                     uvList[0], uvList[1]));
                    }
                    else if (pointList.Count == 1)
                    {
                        Point3D outputPoint;

                        // To avoid an edge cases caused by generating a point extremely
                        // close to one of the bound points, we test if both points are inside
                        // the bounds to be on the safe side - in the worst case we do 
                        // extra work or generate a small edge
                        if (M3DUtil.IsPointInTriangle(visEdgeStart, uv, p, out outputPoint))
                        {
                            edgeList.Add(new HitTestEdge(pointList[0], outputPoint,
                                                         uvList[0], visEdgeStart));
                        }

                        if (M3DUtil.IsPointInTriangle(visEdgeEnd, uv, p, out outputPoint))
                        {
                            edgeList.Add(new HitTestEdge(pointList[0], outputPoint,
                                                         uvList[0], visEdgeEnd));
                        }
                    }
                    else
                    {
                        Point3D outputPoint1, outputPoint2;

                        if (M3DUtil.IsPointInTriangle(visEdgeStart, uv, p, out outputPoint1) &&
                            M3DUtil.IsPointInTriangle(visEdgeEnd, uv, p, out outputPoint2))
                        {
                            edgeList.Add(new HitTestEdge(outputPoint1, outputPoint2,
                                                         visEdgeStart, visEdgeEnd));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles adding an edge when the two line segments are coincident.
        /// </summary>
        /// <param name="visUV1">The texture coordinates of the boundary edge</param>
        /// <param name="visUV2">The texture coordinates of the boundary edge</param>
        /// <param name="tri3D1">The 3D coordinate of the triangle edge</param>
        /// <param name="tri3D2">The 3D coordinates of the triangle edge</param>
        /// <param name="triUV1">The texture coordinates of the triangle edge</param>
        /// <param name="triUV2">The texture coordinates of the triangle edge</param>
        /// <param name="edgeList">The edge list to add to</param>
        private void HandleCoincidentLines(Point visUV1, Point visUV2,
                                           Point3D tri3D1, Point3D tri3D2,
                                           Point triUV1, Point triUV2,
                                           List<HitTestEdge> edgeList)
        {
            Point minVisUV, maxVisUV;

            Point minTriUV, maxTriUV;
            Point3D minTri3D, maxTri3D;

            // to be used in final edge creation
            Point uv1, uv2;
            Point3D p1, p2;

            // order the points and give refs to them for ease of use
            if (Math.Abs(visUV1.X - visUV2.X) > Math.Abs(visUV1.Y - visUV2.Y))
            {
                if (visUV1.X <= visUV2.X)
                {
                    minVisUV = visUV1;
                    maxVisUV = visUV2;
                }
                else
                {
                    minVisUV = visUV2;
                    maxVisUV = visUV1;
                }

                if (triUV1.X <= triUV2.X)
                {
                    minTriUV = triUV1;
                    minTri3D = tri3D1;

                    maxTriUV = triUV2;
                    maxTri3D = tri3D2;
                }
                else
                {
                    minTriUV = triUV2;
                    minTri3D = tri3D2;

                    maxTriUV = triUV1;
                    maxTri3D = tri3D1;
                }

                // now actually create the edge           
                // compute the minimum value
                if (minVisUV.X < minTriUV.X)
                {
                    uv1 = minTriUV;
                    p1 = minTri3D;
                }
                else
                {
                    uv1 = minVisUV;
                    p1 = minTri3D + (minVisUV.X - minTriUV.X) / (maxTriUV.X - minTriUV.X) * (maxTri3D - minTri3D);
                }

                // compute the maximum value
                if (maxVisUV.X > maxTriUV.X)
                {
                    uv2 = maxTriUV;
                    p2 = maxTri3D;
                }
                else
                {
                    uv2 = maxVisUV;
                    p2 = minTri3D + (maxVisUV.X - minTriUV.X) / (maxTriUV.X - minTriUV.X) * (maxTri3D - minTri3D);
                }
            }
            else
            {
                if (visUV1.Y <= visUV2.Y)
                {
                    minVisUV = visUV1;
                    maxVisUV = visUV2;
                }
                else
                {
                    minVisUV = visUV2;
                    maxVisUV = visUV1;
                }

                if (triUV1.Y <= triUV2.Y)
                {
                    minTriUV = triUV1;
                    minTri3D = tri3D1;

                    maxTriUV = triUV2;
                    maxTri3D = tri3D2;
                }
                else
                {
                    minTriUV = triUV2;
                    minTri3D = tri3D2;

                    maxTriUV = triUV1;
                    maxTri3D = tri3D1;
                }

                // now actually create the edge           
                // compute the minimum value
                if (minVisUV.Y < minTriUV.Y)
                {
                    uv1 = minTriUV;
                    p1 = minTri3D;
                }
                else
                {
                    uv1 = minVisUV;
                    p1 = minTri3D + (minVisUV.Y - minTriUV.Y) / (maxTriUV.Y - minTriUV.Y) * (maxTri3D - minTri3D);
                }

                // compute the maximum value
                if (maxVisUV.Y > maxTriUV.Y)
                {
                    uv2 = maxTriUV;
                    p2 = maxTri3D;
                }
                else
                {
                    uv2 = maxVisUV;
                    p2 = minTri3D + (maxVisUV.Y - minTriUV.Y) / (maxTriUV.Y - minTriUV.Y) * (maxTri3D - minTri3D);
                }
            }

            // add the edge
            edgeList.Add(new HitTestEdge(p1, p2, uv1, uv2));
        }

        /// <summary>
        /// Intersects a ray with the line specified by the passed in end points.  The parameterized coordinate along the ray of
        /// intersection is returned.  
        /// </summary>
        /// <param name="o">The ray origin</param>
        /// <param name="d">The ray direction</param>
        /// <param name="p1">First point of the line to intersect against</param>
        /// <param name="p2">Second point of the line to intersect against</param>
        /// <param name="coinc">Whether the ray and line are coincident</param>        
        /// <returns>
        /// The parameter along the ray of the point of intersection.
        /// If the ray and line are parallel and not coincident, this will be -1.
        /// </returns>
        private double IntersectRayLine(Point o, Vector d, Point p1, Point p2, out bool coinc)
        {
            coinc = false;

            // deltas
            double dy = p2.Y - p1.Y;
            double dx = p2.X - p1.X;

            // handle case of a vertical line
            if (dx == 0)
            {
                if (d.X == 0)
                {
                    coinc = (o.X == p1.X);
                    return -1;
                }
                else
                {
                    return (p2.X - o.X) / d.X;
                }
            }

            // now need to do more general intersection
            double numer = (o.X - p1.X) * dy / dx - o.Y + p1.Y;
            double denom = (d.Y - d.X * dy / dx);

            // if denominator is zero, then the lines are parallel
            if (denom == 0)
            {
                double b0 = -o.X * dy / dx + o.Y;
                double b1 = -p1.X * dy / dx + p1.Y;

                coinc = (b0 == b1);
                return -1;
            }
            else
            {
                return (numer / denom);
            }
        }

        /// <summary>
        /// Helper structure to represent an edge
        /// </summary>
        private struct Edge
        {
            public Edge(Point3D s, Point3D e)
            {
                _start = s;
                _end = e;
            }

            public Point3D _start;
            public Point3D _end;
        }

        /// <summary>
        /// Information about an edge such as whether it belongs to a front/back facing
        /// triangle, the texture coordinates for the edge, and how many polygons refer
        /// to that edge.
        /// </summary>
        private class EdgeInfo
        {
            public EdgeInfo()
            {
                _hasFrontFace = _hasBackFace = false;
                _numSharing = 0;
            }

            public bool _hasFrontFace;
            public bool _hasBackFace;
            public Point _uv1;
            public Point _uv2;
            public int _numSharing;
        }

        /// <summary>
        /// Processes the edges of the given triangle.  It does so by updating
        /// the adjacency information based on the direction the polygon is facing.
        /// If there is a silhouette edge found, then this edge is added to the list
        /// of edges if it is within the texture coordinate bounds passed to the function.
        /// </summary>
        /// <param name="p">The triangle's vertices</param>
        /// <param name="uv">The texture coordinates for those vertices</param>
        /// <param name="visualTexCoordBounds">The texture coordinate edges being searched for</param>
        /// <param name="polygonSide">Which side the polygon is facing (greateer than 0 front, less than 0 back)</param>
        /// <param name="edgeList">The list of edges comprosing the visual outline</param>
        /// <param name="adjInformation">The adjacency information structure</param>
        private void ProcessTriangleEdges(Point3D[] p,
                                          Point[] uv,
                                          Point[] visualTexCoordBounds,
                                          PolygonSide polygonSide,
                                          List<HitTestEdge> edgeList,
                                          Dictionary<Edge, EdgeInfo> adjInformation)
        {
            // loop over all the edges and add them to the adjacency list
            for (int i = 0; i < p.Length; i++)
            {
                Point uv1, uv2;
                Point3D p3D1 = p[i];
                Point3D p3D2 = p[(i + 1) % p.Length];

                Edge edge;

                // order the edge points so insertion in to adjInformation is consistent
                if (p3D1.X < p3D2.X ||
                   (p3D1.X == p3D2.X && p3D1.Y < p3D2.Y) ||
                   (p3D1.X == p3D2.X && p3D1.Y == p3D2.Y && p3D1.Z < p3D1.Z))
                {
                    edge = new Edge(p3D1, p3D2);
                    uv1 = uv[i];
                    uv2 = uv[(i + 1) % p.Length];
                }
                else
                {
                    edge = new Edge(p3D2, p3D1);
                    uv2 = uv[i];
                    uv1 = uv[(i + 1) % p.Length];
                }

                // look up the edge information
                EdgeInfo edgeInfo;
                if (adjInformation.ContainsKey(edge))
                {
                    edgeInfo = adjInformation[edge];
                }
                else
                {
                    edgeInfo = new EdgeInfo();
                    adjInformation[edge] = edgeInfo;
                }
                edgeInfo._numSharing++;

                // whether or not the edge has already been added to the edge list
                bool alreadyAdded = edgeInfo._hasBackFace && edgeInfo._hasFrontFace;

                // add the edge to the info list
                if (polygonSide == PolygonSide.FRONT)
                {
                    edgeInfo._hasFrontFace = true;
                    edgeInfo._uv1 = uv1;
                    edgeInfo._uv2 = uv2;
                }
                else
                {
                    edgeInfo._hasBackFace = true;
                }

                // if the sides are different we may need to add an edge
                if (!alreadyAdded && edgeInfo._hasBackFace && edgeInfo._hasFrontFace)
                {
                    HandleSilhouetteEdge(edgeInfo._uv1, edgeInfo._uv2,
                                        edge._start, edge._end,
                                        visualTexCoordBounds,
                                        edgeList);
                }
            }
        }

        /// <summary>
        /// Handles intersecting a silhouette edge against the passed in texture coordinate 
        /// bounds.  It behaves similarly to the case of intersection the bounds with a triangle 
        /// except the testing order is switched.
        /// </summary>
        /// <param name="uv1">The texture coordinates of the edge</param>
        /// <param name="uv2">The texture coordinates of the edge</param>
        /// <param name="p3D1">The 3D point of the edge</param>
        /// <param name="p3D2">The 3D point of the edge</param>
        /// <param name="bounds">The texture coordinate bounds</param>
        /// <param name="edgeList">The list of edges</param>
        private void HandleSilhouetteEdge(Point uv1, Point uv2,
                                          Point3D p3D1, Point3D p3D2,
                                          Point[] bounds,
                                          List<HitTestEdge> edgeList)
        {
            List<Point3D> pointList = new List<Point3D>();
            List<Point> uvList = new List<Point>();
            Vector dir = uv2 - uv1;

            // loop over object bounds
            for (int i = 0; i < bounds.Length; i++)
            {
                Point visEdgeStart = bounds[i];
                Point visEdgeEnd = bounds[(i + 1) % bounds.Length];

                // initial rejection processing
                if (!((Math.Max(visEdgeStart.X, visEdgeEnd.X) < Math.Min(uv1.X, uv2.X)) ||
                      (Math.Min(visEdgeStart.X, visEdgeEnd.X) > Math.Max(uv1.X, uv2.X)) ||
                      (Math.Max(visEdgeStart.Y, visEdgeEnd.Y) < Math.Min(uv1.Y, uv2.Y)) ||
                      (Math.Min(visEdgeStart.Y, visEdgeEnd.Y) > Math.Max(uv1.Y, uv2.Y))))
                {
                    // intersect the two lines
                    bool areCoincident = false;
                    double t = IntersectRayLine(uv1, dir, visEdgeStart, visEdgeEnd, out areCoincident);

                    // silhouette edge processing will only include non-coincident lines
                    if (areCoincident)
                    {
                        // if it's coincident, we'll let the normal processing handle this edge
                        return;
                    }
                    else if (t >= 0 && t <= 1)
                    {
                        Point intersUV = uv1 + dir * t;
                        Point3D intersPoint3D = p3D1 + (p3D2 - p3D1) * t;

                        double visEdgeDiff = (visEdgeStart - visEdgeEnd).Length;

                        if ((intersUV - visEdgeStart).Length < visEdgeDiff &&
                            (intersUV - visEdgeEnd).Length < visEdgeDiff)
                        {
                            pointList.Add(intersPoint3D);
                            uvList.Add(intersUV);
                        }
                    }
                }
            }

            if (pointList.Count >= 2)
            {
                edgeList.Add(new HitTestEdge(pointList[0], pointList[1],
                                             uvList[0], uvList[1]));
            }
            else if (pointList.Count == 1)
            {
                // for the case that uv1/2 is actually a point on or extremely close to the bounds
                // of the polygon, we do the pointinpolygon test on both to avoid any numerical
                // precision issues - in the worst case we end up with a very small edge and
                // the right edge
                if (IsPointInPolygon(bounds, uv1))
                {
                    edgeList.Add(new HitTestEdge(pointList[0], p3D1,
                                                 uvList[0], uv1));
                }
                if (IsPointInPolygon(bounds, uv2))
                {
                    edgeList.Add(new HitTestEdge(pointList[0], p3D2,
                                                 uvList[0], uv2));
                }
            }
            else
            {
                if (IsPointInPolygon(bounds, uv1) &&
                    IsPointInPolygon(bounds, uv2))
                {
                    edgeList.Add(new HitTestEdge(p3D1, p3D2,
                                                 uv1, uv2));
                }
            }
        }

        /// <summary>
        /// Function tests to see whether the point p is contained within the polygon
        /// specified by the list of points passed to the function.  p is considered within
        /// this polygon if it is on the same side of all the edges.  A point on any of
        /// the edges of the polygon is not considered within the polygon.
        /// </summary>
        /// <param name="polygon">The polygon to test against</param>
        /// <param name="p">The point to be tested against</param>
        /// <returns>Whether the point is in the polygon</returns>
        private bool IsPointInPolygon(Point[] polygon, Point p)
        {
            bool sign = false;

            for (int i = 0; i < polygon.Length; i++)
            {
                double crossProduct = Vector.CrossProduct(polygon[(i + 1) % polygon.Length] - polygon[i],
                                                          polygon[i] - p);

                bool currSign = crossProduct > 0;

                if (i == 0)
                {
                    sign = currSign;
                }
                else
                {
                    if (sign != currSign) return false;
                }
            }

            return true;
        }


        /// <summary>
        /// Finds the point in edges that is closest ot the mouse position.  Updates closestIntersectionInfo
        /// with the results of this calculation
        /// </summary>
        /// <param name="mousePos">The mouse position</param>
        /// <param name="edges">The edges to test against</param>
        /// <param name="finalPoint">The final intersection point</param>
        /// <returns>The closest intersection point</returns>
        private bool FindClosestIntersection(Point mousePos, List<HitTestEdge> edges, out Point finalPoint)
        {
            bool success = false;
            double closestDistance = Double.MaxValue;
            Point closestIntersection = new Point();  // the uv of the closest intersection
            finalPoint = new Point();

            // Find the closest point to the mouse position            
            for (int i=0, count = edges.Count; i < count; i++)
            {
                Vector v1 = mousePos - edges[i]._p1Transformed;
                Vector v2 = edges[i]._p2Transformed - edges[i]._p1Transformed;
                
                Point currClosest;
                double distance;

                // calculate the distance from the mouse position to this edge
                // The closest distance can be computed by projecting v1 on to v2.  If the
                // projectiong occurs between _p1Transformed and _p2Transformed, then this is the
                // closest point.  Otherwise, depending on which side it lies, it is either _p1Transformed
                // or _p2Transformed.  
                //
                // The projection equation is given as: (v1 DOT v2) / (v2 DOT v2) * v2.
                // v2 DOT v2 will always be positive.  Thus, if v1 DOT v2 is negative, we know the projection
                // will occur before _p1Transformed (and so it is the closest point).  If (v1 DOT v2) is greater
                // than (v2 DOT v2), then we have gone passed _p2Transformed and so it is the closest point.
                // Otherwise the projection gives us this value.
                //
                double denom = v2 * v2;
                if (denom == 0)
                {
                    currClosest = edges[i]._p1Transformed;
                    distance = v1.Length;
                }
                else
                {
                    double numer = v2 * v1;
                    if (numer < 0)
                    {
                        currClosest = edges[i]._p1Transformed;
                    }
                    else
                    {
                        if (numer > denom)
                        {
                            currClosest = edges[i]._p2Transformed;
                        }
                        else
                        {
                            currClosest = edges[i]._p1Transformed + (numer / denom) * v2;
                        }
                    }

                    distance = (mousePos - currClosest).Length; 
                }

                // see if we found a new closest distance
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    
                    if (denom != 0)
                    {
                        closestIntersection = ((currClosest - edges[i]._p1Transformed).Length / Math.Sqrt(denom) * 
                                               (edges[i]._uv2 - edges[i]._uv1)) + edges[i]._uv1;
                    }
                    else
                    {
                        closestIntersection = edges[i]._uv1;
                    }
                }                
            }

            if (closestDistance != Double.MaxValue)
            {
                Point ptOnVisual = Viewport2DVisual3D.TextureCoordsToVisualCoords(closestIntersection, _visualBrushBounds);

                if (_transform2DInverse != null)
                {
                    Point ptRelToCapture = _transform2DInverse.Transform(ptOnVisual);
                    
                    // we want to "ring" around the outside so things like buttons are not pressed when we move off the mesh
                    // this code here does that - the +BUFFER_SIZE and -BUFFER_SIZE are to give a bit of a 
                    // buffer for any numerical issues
                    if (ptRelToCapture.X <= _visualBounds.Left + 1)   ptRelToCapture.X -= BUFFER_SIZE;
                    if (ptRelToCapture.Y <= _visualBounds.Top + 1)    ptRelToCapture.Y -= BUFFER_SIZE;
                    if (ptRelToCapture.X >= _visualBounds.Right - 1)  ptRelToCapture.X += BUFFER_SIZE;
                    if (ptRelToCapture.Y >= _visualBounds.Bottom - 1) ptRelToCapture.Y += BUFFER_SIZE;

                    Point finalVisualPoint = _transform2D.Transform(ptRelToCapture);
                    finalPoint = Viewport2DVisual3D.VisualCoordsToTextureCoords(finalVisualPoint, _visualBrushBounds);      

                    success = true;
                }
            }

            return success;
        }
       
        /// <summary>
        /// Performs the transform that goes from 2D on 3D content contained within the 3D scene
        /// up to the containing Viewport3DVisual
        /// </summary>
        /// <param name="inPoint">input point</param>
        /// <param name="result">output point</param>
        /// <returns>false if the point cannot be transformed</returns>
        private bool TryRegularTransform(Point inPoint, out Point result)
        {
            Point texCoord = Viewport2DVisual3D.VisualCoordsToTextureCoords(inPoint, _visualBrushBounds);

            // need to walk the texture coordinates and look for where this point intersects one of them
            Point3D point3D;
            if (_objectToViewport != null &&
                Viewport2DVisual3D.Get3DPointFor2DCoordinate(texCoord, 
                                                           out point3D,
                                                           _geometry.Positions,
                                                           _geometry.TextureCoordinates,
                                                           _geometry.TriangleIndices))
            {
                // convert from this 3D point up to the containing Viewport3D
                return _objectToViewport.TryTransform(point3D, out result);
            }
            else
            {
                result = new Point();
                return false;
            }
        }        

        /// <summary>
        /// Transform the rect bounds into the smallest axis alligned bounding box that
        /// contains all the point in the original bounds.
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public override Rect TransformBounds(Rect rect)
        {
            List<HitTestEdge> edges = null;
            
            // intersect the rect given to us with the bounds of the visual brush to guarantee the rect we are
            // searching for is within the visual brush
            rect.Intersect(_visualBrushBounds);

            // get the texture coordinate values for the rect's corners
            Point[] texCoordsOfInterest = new Point[4];
            texCoordsOfInterest[0] = Viewport2DVisual3D.VisualCoordsToTextureCoords(rect.TopLeft,     _visualBrushBounds);
            texCoordsOfInterest[1] = Viewport2DVisual3D.VisualCoordsToTextureCoords(rect.TopRight,    _visualBrushBounds);
            texCoordsOfInterest[2] = Viewport2DVisual3D.VisualCoordsToTextureCoords(rect.BottomRight, _visualBrushBounds);
            texCoordsOfInterest[3] = Viewport2DVisual3D.VisualCoordsToTextureCoords(rect.BottomLeft,  _visualBrushBounds);
                        
            // get the edges that map to the given rect
            edges = GrabValidEdges(texCoordsOfInterest);

            Rect result = Rect.Empty;
            if (edges != null)
            {
                for (int i = 0, count = edges.Count; i < count; i++)
                {
                    result.Union(edges[i]._p1Transformed);
                    result.Union(edges[i]._p2Transformed);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the inverse transform if there is one, null otherwise
        /// </summary>
        public override GeneralTransform Inverse
        {
            get
            {
                // internal class - no ReadPreamble needed
                // ReadPreamble();

                GeneralTransform2DTo3DTo2D inverseTransform = (GeneralTransform2DTo3DTo2D)Clone();
                inverseTransform.IsInverse = !IsInverse;

                return inverseTransform;
            }
        }
       
        /// <summary>
        /// Returns a best effort affine transform
        /// </summary>        
        internal override Transform AffineTransform
        {
            [FriendAccessAllowed] // Built into Core, also used by Framework.
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Returns true if the transform is an inverse
        /// </summary>
        internal bool IsInverse
        {
            get { return _fInverse; }
            set { _fInverse = value; }
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new GeneralTransform2DTo3DTo2D();
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCore(Freezable)">Freezable.CloneCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            GeneralTransform2DTo3DTo2D transform = (GeneralTransform2DTo3DTo2D)sourceFreezable;
            base.CloneCore(sourceFreezable);
            CopyCommon(transform);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCurrentValueCore(Freezable)">Freezable.CloneCurrentValueCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            GeneralTransform2DTo3DTo2D transform = (GeneralTransform2DTo3DTo2D)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);
            CopyCommon(transform);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetAsFrozenCore(Freezable)">Freezable.GetAsFrozenCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            GeneralTransform2DTo3DTo2D transform = (GeneralTransform2DTo3DTo2D)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);
            CopyCommon(transform);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetCurrentValueAsFrozenCore(Freezable)">Freezable.GetCurrentValueAsFrozenCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            GeneralTransform2DTo3DTo2D transform = (GeneralTransform2DTo3DTo2D)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            CopyCommon(transform);
        }
        
        /// <summary>
        /// Clones values that do not have corresponding DPs
        /// </summary>
        /// <param name="transform"></param>
        private void CopyCommon(GeneralTransform2DTo3DTo2D transform)
        {
            _fInverse = transform._fInverse;

            _geometry = transform._geometry;

            _visualBounds = transform._visualBounds;
            _visualBrushBounds = transform._visualBrushBounds;

            _transform2D = transform._transform2D;
            _transform2DInverse = transform._transform2DInverse;

            _camera = transform._camera;
            _viewSize = transform._viewSize;
            _boundingRect = transform._boundingRect;

            _worldTransformation = transform._worldTransformation;
            _objectToViewport = transform._objectToViewport;

            _validEdgesCache = null;
        }

        private bool _fInverse;
        
        // the geometry of the 3D object
        private MeshGeometry3D _geometry;

        // the size of the visual brush and the visual on it we're interested in
        private Rect _visualBounds;
        private Rect _visualBrushBounds;

        // the transform to go in to and out of the coordinate space fo the visual we're
        // interested in
        private GeneralTransform _transform2D;
        private GeneralTransform _transform2DInverse;      

        // the camera being used on the 3D viewport
        private Camera _camera;
        private Size _viewSize;
        private Rect3D _boundingRect;

        // transformations through the 3D scene
        private Matrix3D _worldTransformation;
        private GeneralTransform3DTo2D _objectToViewport;

        // the cache of valid edges
        List<HitTestEdge> _validEdgesCache = null;

        // the "ring" around the element with capture to use in the capture case
        private const double BUFFER_SIZE = 2.0;                  
        private enum PolygonSide { FRONT, BACK };
    }
}

