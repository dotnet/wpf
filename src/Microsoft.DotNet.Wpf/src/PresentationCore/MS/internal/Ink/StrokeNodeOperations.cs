// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Input;
using System.Diagnostics;
using MS.Utility;
using MS.Internal;

namespace MS.Internal.Ink
{
    /// <summary>
    /// The base operations class that implements polygonal node operations by default.
    /// </summary>
    internal partial class StrokeNodeOperations
    {
        #region Static API
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodeShape"></param>
        /// <returns></returns>
        internal static StrokeNodeOperations CreateInstance(StylusShape nodeShape)
        {
            if (nodeShape == null)
            {
                throw new ArgumentNullException("nodeShape");
            }
            if (nodeShape.IsEllipse)
            {
                return new EllipticalNodeOperations(nodeShape);
            }
            return new StrokeNodeOperations(nodeShape);
        }
        #endregion
        
        #region API

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nodeShape">shape of the nodes</param>
        internal StrokeNodeOperations(StylusShape nodeShape)
        {
            System.Diagnostics.Debug.Assert(nodeShape != null);
            _vertices = nodeShape.GetVerticesAsVectors();
        }

        /// <summary>
        /// This is probably not the best (design-wise) but the cheapest way to tell 
        /// EllipticalNodeOperations from all other implementations of node operations.
        /// </summary>
        internal virtual bool IsNodeShapeEllipse { get { return false; } }

        /// <summary>
        /// Computes the bounds of a node
        /// </summary>
        /// <param name="node">node to compute bounds of</param>
        /// <returns>bounds of the node</returns>
        internal Rect GetNodeBounds(in StrokeNodeData node)
        {
            if (_shapeBounds.IsEmpty)
            {
                int i;
                for (i = 0; (i + 1) < _vertices.Length; i += 2)
                {
                    _shapeBounds.Union(new Rect((Point)_vertices[i], (Point)_vertices[i + 1]));
                }
                if (i < _vertices.Length)
                {
                    _shapeBounds.Union((Point)_vertices[i]);
                }
            }

            Rect boundingBox = _shapeBounds;
            System.Diagnostics.Debug.Assert((boundingBox.X <= 0) && (boundingBox.Y <= 0));

            double pressureFactor = node.PressureFactor;
            if (!DoubleUtil.AreClose(pressureFactor,1d))
            {
                boundingBox = new Rect(
                    _shapeBounds.X * pressureFactor,
                    _shapeBounds.Y * pressureFactor,
                    _shapeBounds.Width * pressureFactor,
                    _shapeBounds.Height * pressureFactor);
            }
            
            boundingBox.Location += (Vector)node.Position;

            return boundingBox;
        }

        internal void GetNodeContourPoints(in StrokeNodeData node, List<Point> pointBuffer)
        {
            double pressureFactor = node.PressureFactor;
            if (DoubleUtil.AreClose(pressureFactor, 1d))
            {
                for (int i = 0; i < _vertices.Length; i++)
                {
                    pointBuffer.Add(node.Position + _vertices[i]);
                }
            }
            else
            {
                for (int i = 0; i < _vertices.Length; i++)
                {
                    pointBuffer.Add(node.Position + (_vertices[i] * pressureFactor));
                }
            }
        }
        
        /// <summary>
        /// Returns an enumerator for edges of the contour comprised by a given node 
        /// and its connecting quadrangle.
        /// Used for hit-testing a stroke against an other stroke (stroke and point erasing)
        /// </summary>
        /// <param name="node">node</param>
        /// <param name="quad">quadrangle connecting the node to the preceeding node</param>
        /// <returns>contour segments enumerator</returns>
        internal virtual IEnumerable<ContourSegment> GetContourSegments(StrokeNodeData node, Quad quad)
        {
            System.Diagnostics.Debug.Assert(node.IsEmpty == false);

            if (quad.IsEmpty)
            {
                Point vertex = node.Position + (_vertices[_vertices.Length - 1] * node.PressureFactor);
                for (int i = 0; i < _vertices.Length; i++)
                {
                    Point nextVertex = node.Position + (_vertices[i] * node.PressureFactor);
                    yield return new ContourSegment(vertex, nextVertex);
                    vertex = nextVertex;
                }
            }
            else
            {
                yield return new ContourSegment(quad.A, quad.B);

                for (int i = 0, count = _vertices.Length; i < count; i++)
                {
                    Point vertex = node.Position + (_vertices[i] * node.PressureFactor);
                    if (vertex == quad.B)
                    {
                        for (int j = 0; (j < count) && (vertex != quad.C); j++)
                        {
                            i = (i + 1) % count;
                            Point nextVertex = node.Position + (_vertices[i] * node.PressureFactor);
                            yield return new ContourSegment(vertex, nextVertex);
                            vertex = nextVertex;
                        }
                        break;
                    }
                }

                yield return new ContourSegment(quad.C, quad.D);
                yield return new ContourSegment(quad.D, quad.A);
            }
        }

        /// <summary>
        /// ISSUE-2004/06/15- temporary workaround to avoid hit-testing ellipses with ellipses
        /// </summary>
        /// <param name="beginNode"></param>
        /// <param name="endNode"></param>
        /// <returns></returns>
        internal virtual IEnumerable<ContourSegment> GetNonBezierContourSegments(StrokeNodeData beginNode, StrokeNodeData endNode)
        {
            Quad quad = beginNode.IsEmpty ? Quad.Empty : GetConnectingQuad(beginNode, endNode); 
            return GetContourSegments(endNode, quad);
        }


        /// <summary>
        /// Finds connecting points for a pair of stroke nodes (of a polygonal shape)
        /// </summary>
        /// <param name="beginNode">a node to connect</param>
        /// <param name="endNode">another node, next to beginNode</param>
        /// <returns>connecting quadrangle, that can be empty if one node is inside the other</returns>
        internal virtual Quad GetConnectingQuad(in StrokeNodeData beginNode, in StrokeNodeData endNode)
        {            
            // Return an empty quad if either of the nodes is empty (not a node) 
            // or if both nodes are at the same position.
            if (beginNode.IsEmpty || endNode.IsEmpty || DoubleUtil.AreClose(beginNode.Position, endNode.Position))
            {
                return Quad.Empty;
            }

            // By definition, Quad's vertices (A,B,C,D) are ordered clockwise with points A and D located
            // on the beginNode and B and C on the endNode. Basically, we're looking for segments AB and CD. 
            // We iterate through the vertices of the beginNode, at each vertex we analyze location of the
            // connecting segment relative to the node's edges at the vertex, and enforce these rules: 
            //  - if the vector of the connecting segment at a vertex V[i] is on the left from vector V[i]V[i+1] 
            //    and not on the left from vector V[i-1]V[i], then it's the AB segment of the quad (V[i] == A).
            //  - if the vector of the connecting segment at a vertex V[i] is on the left from vector V[i-1]V[i] 
            //    and not on the left from vector V[i]V[i+1], then it's the CD segment of the quad (V[i] == D).
            // 

            Quad quad = Quad.Empty;
            bool foundAB = false, foundCD = false;

            // There's no need to build shapes of the two nodes in order to find their connecting quad.
            // It's the spine vector between the nodes and their scaling diff (pressure delta) is all 
            // that matters here. 
            Vector spine = endNode.Position - beginNode.Position;
            double pressureDelta = endNode.PressureFactor - beginNode.PressureFactor;

            // Iterate through the vertices of the default shape
            int count = _vertices.Length;
            for (int i = 0, j = count - 1; i < count; i++, j = ((j + 1) % count))
            {
                // Compute vector of the connecting segment at the vertex [i]
                Vector connection = spine + _vertices[i] * pressureDelta;
                if ((pressureDelta != 0) && (connection.X == 0) && (connection.Y == 0))
                {
                    // One of the nodes,                       |----|
                    // as well as the connecting quad,         |__  |
                    // is entirely inside the other node.      |  | |
                    //                                 [i] --> |__|_|
                    return Quad.Empty;
                }

                // Find out where this vector is about the node edge [i][i+1]
                // (The vars names "goingTo" and "comingFrom" refer direction of the line defined 
                // by the connecting vector applied at vertex [i], relative to the contour of the node shape.
                // Using these terms, (comingFrom != Right && goingTo == Left) corresponds to the segment AB,
                // and (comingFrom == Right && goingTo != Left) describes the DC.
                HitResult goingTo = WhereIsVectorAboutVector(connection, _vertices[(i + 1) % count] - _vertices[i]);

                if (goingTo == HitResult.Left)
                {
                    if (false == foundAB)
                    {
                        // Find out where the node edge [i-1][i] is about the connecting vector 
                        HitResult comingFrom = WhereIsVectorAboutVector(_vertices[i] - _vertices[j], connection);
                        if (HitResult.Right != comingFrom)
                        {
                            foundAB = true;
                            quad.A = beginNode.Position + _vertices[i] * beginNode.PressureFactor;
                            quad.B = endNode.Position + _vertices[i] * endNode.PressureFactor;
                            if (true == foundCD)
                            {
                                // Found all 4 points. Break out from the 'for' loop.
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (false == foundCD)
                    {
                        // Find out where the node edge [i-1][i] is about the connecting vector 
                        HitResult comingFrom = WhereIsVectorAboutVector(_vertices[i] - _vertices[j], connection);
                        if (HitResult.Right == comingFrom)
                        {
                            foundCD = true;
                            quad.C = endNode.Position + _vertices[i] * endNode.PressureFactor;
                            quad.D = beginNode.Position + _vertices[i] * beginNode.PressureFactor;
                            if (true == foundAB)
                            {
                                // Found all 4 points. Break out from the 'for' loop.
                                break;
                            }
                        }
                    }
                }
            }
            
            if (!foundAB || !foundCD ||   // (2)
                ((pressureDelta != 0) && Vector.Determinant(quad.B - quad.A, quad.D - quad.A) == 0)) // (1)
            {   
                //                                          _____        _______
                // One of the nodes,                    (1) |__  |   (2) | ___  |
                // as well as the connecting quad,          |  | |       | |  | |
                // is entirely inside the other node.       |__| |       | |__| |
                //                                          |____|       |___ __|
                return Quad.Empty;
            }

            return quad;
        }

        /// <summary>
        /// Hit-tests ink segment defined by two nodes against a linear segment.
        /// </summary>
        /// <param name="beginNode">Begin node of the ink segment</param>
        /// <param name="endNode">End node of the ink segment</param>
        /// <param name="quad">Pre-computed quadrangle connecting the two ink nodes</param>
        /// <param name="hitBeginPoint">Begin point of the hitting segment</param>
        /// <param name="hitEndPoint">End point of the hitting segment</param>
        /// <returns>true if there's intersection, false otherwise</returns>
        internal virtual bool HitTest(
           in StrokeNodeData beginNode, in StrokeNodeData endNode, Quad quad, Point hitBeginPoint, Point hitEndPoint)
        {
            // Check for special cases when the endNode is the very first one (beginNode.IsEmpty) 
            // or one node is completely inside the other. In either case the connecting quad 
            // would be Empty and we need to hit-test against the biggest node (the one with 
            // the greater PressureFactor)
            if (quad.IsEmpty)
            {
                Point position;
                double pressureFactor;
                if (beginNode.IsEmpty || (endNode.PressureFactor > beginNode.PressureFactor))
                {
                    position = endNode.Position;
                    pressureFactor = endNode.PressureFactor;
                }
                else
                {
                    position = beginNode.Position;
                    pressureFactor = beginNode.PressureFactor;
                }

                // Find the coordinates of the hitting segment relative to the ink node
                Vector hitBegin = hitBeginPoint - position, hitEnd = hitEndPoint - position;
                if (pressureFactor != 1)
                {
                    // Instead of applying pressure to the node, do reverse scaling on
                    // the hitting segment. This allows us use the original array of vertices 
                    // in hit-testing.
                    System.Diagnostics.Debug.Assert(DoubleUtil.IsZero(pressureFactor) == false);
                    hitBegin /= pressureFactor;
                    hitEnd /= pressureFactor;
                }
                return HitTestPolygonSegment(_vertices, hitBegin, hitEnd);
            }
            else
            {
                // Iterate through the vertices of the contour of the ink segment
                // check where the hitting segment is about them, return false if it's 
                // on the outer (left) side of the ink contour. This implementation might
                // look more complex than straightforward separated hit-testing of three 
                // polygons (beginNode, quad, endNode), but it's supposed to be more optimal
                // because the number of edges it hit-tests is approximately twice less
                // than with the straightforward implementation.

                // Start with the segment quad.C->quad.D
                Vector hitBegin = hitBeginPoint - beginNode.Position;
                Vector hitEnd = hitEndPoint - beginNode.Position;
                HitResult hitResult = WhereIsSegmentAboutSegment(
                    hitBegin, hitEnd, quad.C - beginNode.Position, quad.D - beginNode.Position);
                if (HitResult.Left == hitResult)
                {
                    return false;
                }

                // Continue clockwise from quad.D to quad.C

                HitResult firstResult = hitResult, lastResult = hitResult;
                double pressureFactor = beginNode.PressureFactor;

                // Find the index of the vertex that is quad.D 
                // Use count var to avoid infinite loop, normally it shouldn't 
                // happen but it doesn't hurt to check it just in case.
                int i = 0, count = _vertices.Length;
                Vector vertex = new Vector();
                for (i = 0; i < count; i++)
                {
                    vertex = _vertices[i] * pressureFactor;
                    // Here and in a few more places down the code, when comparing
                    // a quad's vertex vs a scaled shape vertex, it's important to 
                    // compute them the same way as in GetConnectingQuad, so that not
                    // hit that double's computation error. For instance, sometimes the
                    // expression (vertex == quad.D - beginNode.Position) gives 'false' 
                    // while the expression below gives 'true'. (Another workaround is to
                    // use DoubleUtil.AreClose but that;d be less performant)
                    if ((beginNode.Position + vertex) == quad.D)
                    {
                        break;
                    }
                }
                System.Diagnostics.Debug.Assert(count > 0);
                // This loop does the iteration thru the edges of the ink segment 
                // clockwise from quad.D to quad.C. 
                for (int node = 0; node < 2; node++)
                {
                    Point nodePosition = (node == 0) ? beginNode.Position : endNode.Position;
                    Point end = (node == 0) ? quad.A : quad.C;

                    count = _vertices.Length;
                    while (((nodePosition + vertex) != end) && (count != 0))
                    {
                        i = (i + 1) % _vertices.Length;
                        Vector nextVertex = (pressureFactor == 1) ? _vertices[i] : (_vertices[i] * pressureFactor);
                        hitResult = WhereIsSegmentAboutSegment(hitBegin, hitEnd, vertex, nextVertex);
                        if (HitResult.Hit == hitResult)
                        {
                            return true;
                        }
                        if (true == IsOutside(hitResult, lastResult))
                        {
                            return false;
                        }
                        lastResult = hitResult;
                        vertex = nextVertex;
                        count--;
                    }
                    System.Diagnostics.Debug.Assert(count > 0);

                    if (node == 0)
                    {
                        // The first iteration is done thru the outer segments of beginNode
                        // and ends at quad.A, for the second one make some adjustments 
                        // to continue iterating through quad.AB and the outer segments of 
                        // endNode up to quad.C
                        pressureFactor = endNode.PressureFactor;

                        Vector spineVector = endNode.Position - beginNode.Position;
                        vertex -= spineVector;
                        hitBegin -= spineVector;
                        hitEnd -= spineVector;

                        // Find the index of the vertex that is quad.B
                        count = _vertices.Length;
                        while (((endNode.Position + _vertices[i] * pressureFactor) != quad.B) && (count != 0))
                        {
                            i = (i + 1) % _vertices.Length;
                            count--;
                        }
                        System.Diagnostics.Debug.Assert(count > 0);
                        i--;
                    }
                }
                return (false == IsOutside(firstResult, hitResult));
            }
        }

        /// <summary>
        /// Hit-tests a stroke segment defined by two nodes against another stroke segment.
        /// </summary>
        /// <param name="beginNode">Begin node of the stroke segment to hit-test. Can be empty (none)</param>
        /// <param name="endNode">End node of the stroke segment</param>
        /// <param name="quad">Pre-computed quadrangle connecting the two nodes. 
        /// Can be empty if the begion node is empty or when one node is entirely inside the other</param>
        /// <param name="hitContour">a collection of basic segments outlining the hitting contour</param>
        /// <returns>true if the contours intersect or overlap</returns>
        internal virtual bool HitTest(
           in StrokeNodeData beginNode, in StrokeNodeData endNode, Quad quad, IEnumerable<ContourSegment> hitContour)
        {           
            // Check for special cases when the endNode is the very first one (beginNode.IsEmpty) 
            // or one node is completely inside the other. In either case the connecting quad 
            // would be Empty and we need to hittest against the biggest node (the one with 
            // the greater PressureFactor)
            if (quad.IsEmpty)
            {               
                // Make a call to hit-test the biggest node the hitting contour.
                return HitTestPolygonContourSegments(hitContour, beginNode, endNode);
            }
            else
            {
                // HitTest the the hitting contour against the inking contour
                return HitTestInkContour(hitContour, quad, beginNode, endNode);
            }
        }

        /// <summary>
        /// Hit-tests ink segment defined by two nodes against a linear segment.
        /// </summary>
        /// <param name="beginNode">Begin node of the ink segment</param>
        /// <param name="endNode">End node of the ink segment</param>
        /// <param name="quad">Pre-computed quadrangle connecting the two ink nodes</param>
        /// <param name="hitBeginPoint">Begin point of the hitting segment</param>
        /// <param name="hitEndPoint">End point of the hitting segment</param>
        /// <returns>Exact location to cut at represented by StrokeFIndices</returns>
        internal virtual StrokeFIndices CutTest(
            in StrokeNodeData beginNode, in StrokeNodeData endNode, Quad quad, Point hitBeginPoint, Point hitEndPoint)
        {
            StrokeFIndices result = StrokeFIndices.Empty;

            // First, find out if the hitting segment intersects with either of the ink nodes
            for (int node = (beginNode.IsEmpty ? 1 : 0); node < 2; node++)
            {
                Point position = (node == 0) ? beginNode.Position : endNode.Position;
                double pressureFactor = (node == 0) ? beginNode.PressureFactor : endNode.PressureFactor;

                // Adjust the segment for the node's pressure factor
                Vector hitBegin = hitBeginPoint - position;
                Vector hitEnd = hitEndPoint - position;
                if (pressureFactor != 1)
                {
                    System.Diagnostics.Debug.Assert(DoubleUtil.IsZero(pressureFactor) == false);
                    hitBegin /= pressureFactor;
                    hitEnd /= pressureFactor;
                }
                // Hit-test the node against the segment
                if (true == HitTestPolygonSegment(_vertices, hitBegin, hitEnd))
                {
                    if (node == 0)
                    {
                        result.BeginFIndex = StrokeFIndices.BeforeFirst;
                        result.EndFIndex = 0;
                    }
                    else
                    {
                        result.EndFIndex = StrokeFIndices.AfterLast;
                        if (beginNode.IsEmpty)
                        {
                            result.BeginFIndex = StrokeFIndices.BeforeFirst;
                        }
                        else if (result.BeginFIndex != StrokeFIndices.BeforeFirst)
                        {
                            result.BeginFIndex = 1;
                        }
                    }
                }
            }

            // If both nodes are hit, return.
            if (result.IsFull)
            {
                return result;
            }
            // If there's no hit at all, return.
            if (result.IsEmpty && (quad.IsEmpty || !HitTestQuadSegment(quad, hitBeginPoint, hitEndPoint)))
            {
                return result;
            }

            // The segments do intersect. Find findices on the ink segment to cut it at.
            if (result.BeginFIndex != StrokeFIndices.BeforeFirst)
            {
                // The begin node is not hit, i.e. the begin findex is on this spine segment, find it.
                result.BeginFIndex = ClipTest(
                    (endNode.Position - beginNode.Position) / beginNode.PressureFactor,
                    (endNode.PressureFactor / beginNode.PressureFactor) - 1,
                    (hitBeginPoint - beginNode.Position) / beginNode.PressureFactor,
                    (hitEndPoint - beginNode.Position) / beginNode.PressureFactor);
            }

            if (result.EndFIndex != StrokeFIndices.AfterLast)
            {
                // The end node is not hit, i.e. the end findex is on this spine segment, find it.
                result.EndFIndex = 1 - ClipTest(
                    (beginNode.Position - endNode.Position) / endNode.PressureFactor,
                    (beginNode.PressureFactor / endNode.PressureFactor) - 1,
                    (hitBeginPoint - endNode.Position) / endNode.PressureFactor,
                    (hitEndPoint - endNode.Position) / endNode.PressureFactor);
            }

            if (IsInvalidCutTestResult(result))
            {
                return StrokeFIndices.Empty;
            }

            return result;
        }

        /// <summary>
        /// CutTest
        /// </summary>
        /// <param name="beginNode">Begin node of the stroke segment to hit-test. Can be empty (none)</param>
        /// <param name="endNode">End node of the stroke segment</param>
        /// <param name="quad">Pre-computed quadrangle connecting the two nodes. 
        /// Can be empty if the begion node is empty or when one node is entirely inside the other</param>
        /// <param name="hitContour">a collection of basic segments outlining the hitting contour</param>
        /// <returns></returns>
        internal virtual StrokeFIndices CutTest(
            in StrokeNodeData beginNode, in StrokeNodeData endNode, Quad quad, IEnumerable<ContourSegment> hitContour)
        {
            if (beginNode.IsEmpty)
            {
                if (HitTest(beginNode, endNode, quad, hitContour) == true)
                {
                    return StrokeFIndices.Full;
                }
                return StrokeFIndices.Empty;
            }

            StrokeFIndices result = StrokeFIndices.Empty;
            bool isInside = true;
            Vector spineVector = (endNode.Position - beginNode.Position) / beginNode.PressureFactor;
            Vector spineVectorReversed = (beginNode.Position - endNode.Position) / endNode.PressureFactor;
            double pressureDelta = (endNode.PressureFactor / beginNode.PressureFactor) - 1;
            double pressureDeltaReversed = (beginNode.PressureFactor / endNode.PressureFactor) - 1;

            foreach (ContourSegment hitSegment in hitContour)
            {

                // First, find out if hitSegment intersects with either of the ink nodes
                bool isHit = HitTestStrokeNodes(hitSegment,beginNode,endNode, ref result);

                // If both nodes are hit, return.
                if (result.IsFull)
                {
                    return result;
                }

                // If neither of the nodes is hit, hit-test the connecting quad
                if (isHit == false)
                {
                    // If neither of the nodes is hit and the contour of one node is entirely 
                    // inside the contour of the other node, then done with this hitting segment
                    if (!quad.IsEmpty)
                    {
                        isHit = hitSegment.IsArc
                             ? HitTestQuadCircle(quad, hitSegment.Begin + hitSegment.Radius, hitSegment.Radius)
                             : HitTestQuadSegment(quad, hitSegment.Begin, hitSegment.End);
                    }
                    
                    if (isHit == false)
                    {
                        if (isInside == true)
                        {
                            isInside = hitSegment.IsArc
                                ? (WhereIsVectorAboutArc(endNode.Position - hitSegment.Begin - hitSegment.Radius,
                                    -hitSegment.Radius, hitSegment.Vector - hitSegment.Radius) != HitResult.Hit)
                                : (WhereIsVectorAboutVector(
                                    endNode.Position - hitSegment.Begin, hitSegment.Vector) == HitResult.Right);
                        }
                        continue;
                    }
                }

                isInside = false;

                // If the begin node is not hit, find the begin findex on the ink segment to cut it at
                if (!DoubleUtil.AreClose(result.BeginFIndex, StrokeFIndices.BeforeFirst))
                {
                    double findex = CalculateClipLocation(hitSegment, beginNode, spineVector, pressureDelta);
                    if (findex != StrokeFIndices.BeforeFirst)
                    {
                        System.Diagnostics.Debug.Assert(findex >= 0 && findex <= 1);
                        if (result.BeginFIndex > findex)
                        {
                            result.BeginFIndex = findex;
                        }
                    }
                }

                // If the end node is not hit, find the end findex on the ink segment to cut it at
                if (!DoubleUtil.AreClose(result.EndFIndex, StrokeFIndices.AfterLast))
                {
                    double findex = CalculateClipLocation(hitSegment, endNode, spineVectorReversed, pressureDeltaReversed);
                    if (findex != StrokeFIndices.BeforeFirst)
                    {
                        System.Diagnostics.Debug.Assert(findex >= 0 && findex <= 1);
                        findex = 1 - findex;
                        if (result.EndFIndex < findex)
                        {
                            result.EndFIndex = findex;
                        }
                    }
                }
            }

            if (DoubleUtil.AreClose(result.BeginFIndex, StrokeFIndices.AfterLast))
            {
                if (!DoubleUtil.AreClose(result.EndFIndex, StrokeFIndices.BeforeFirst))
                {
                    result.BeginFIndex = StrokeFIndices.BeforeFirst;
                }
            }
            else if (DoubleUtil.AreClose(result.EndFIndex, StrokeFIndices.BeforeFirst))
            {
                result.EndFIndex = StrokeFIndices.AfterLast;
            }

            if (IsInvalidCutTestResult(result))
            {
                return StrokeFIndices.Empty;
            }

            return (result.IsEmpty && isInside) ? StrokeFIndices.Full : result;
        }

        /// <summary>
        /// Cutting ink with polygonal tip shapes with a linear segment
        /// </summary>
        /// <param name="spineVector">Vector representing the starting and ending point for the inking 
        ///             segment</param>
        /// <param name="pressureDelta">Represents the difference in the node size for startNode and endNode.
        ///              pressureDelta = (endNode.PressureFactor / beginNode.PressureFactor) - 1</param>
        /// <param name="hitBegin">Start point of the hitting segment</param>
        /// <param name="hitEnd">End point of the hitting segment</param>
        /// <returns>a double representing the point of clipping</returns>
        private double ClipTest(Vector spineVector, double pressureDelta, Vector hitBegin, Vector hitEnd)
        {
            // Let's represent the vertices for the startNode are N1, N2, ..., Ni and for the endNode, M1, M2,
            // ..., Mi. 
            // When ink tip shape is a convex polygon, one may iterate in a constant direction 
            // (for instance, clockwise) through the edges of the polygon P1 and hit test the cutting segment 
            // against quadrangles NIMIMI+1NI+1 with MI on the left side off the vector NINI+1. 
            // If the cutting segment intersects the quadrangle, on the intersected part of the segment, 
            // one may then find point Q (the nearest to the line NINI+1) and point QP 
            // (the point of the intersection of the segment NIMI and vector NI+1NI started at Q). 
            // Next,  
            //                      QP = NI + s * LengthOf(MI - NI)                         (1)
            //                      s = LengthOf(QP - NI ) / LengthOf(MI - NI).                     (2)
            // If the cutting segment intersects more than one quadrant, one may then use the smallest s 
            // to find the split point:  
            //                      S = P1 + s * LengthOf(P2 - P1)                          (3)
            double findex = StrokeFIndices.AfterLast;
            Vector hitVector = hitEnd - hitBegin;
            Vector lastVertex = _vertices[_vertices.Length - 1];

            // Note the definition of pressureDelta = (endNode.PressureFactor / beginNode.PressureFactor) - 1
            // So the equation below gives 
            //   nextNode = spineVector + (endNode.PressureFactor / beginNode.PressureFactor)*lastVertex - lastVertex
            // As a result, nextNode is a Vector pointing from lastVertex of the beginNode to the correspoinding "lastVertex"
            // of the endNode.
            Vector nextNode = spineVector + lastVertex * pressureDelta;
            bool testNextEdge = false;

            for (int k = 0, count = _vertices.Length; k < count || (k == count && testNextEdge); k++)
            {
                Vector vertex = _vertices[k % count];
                Vector nextVertex = vertex - lastVertex;

                // Point from vertex in beginNode to the corresponding "vertex" in endNode
                Vector nextVertexNextNode = spineVector + (vertex * pressureDelta);

                // Find out a "nextNode" on the endNode (nextNode) that is on the left side off the vector
                // (lastVertex, vertex).
                if ((DoubleUtil.IsZero(nextNode.X) && DoubleUtil.IsZero(nextNode.Y)) ||
                    (!testNextEdge && (HitResult.Left != WhereIsVectorAboutVector(nextNode, nextVertex))))
                {
                    lastVertex = vertex;
                    nextNode = nextVertexNextNode;
                    continue;
                }

                // Now we need to do hit testing of the hitting segment against quarangle (NI, MI, MI+1, NI+1),
                // that is, (lastVertex, nextNode, nextVertexNextNode, vertex)

                testNextEdge = false;
                HitResult hit = HitResult.Left;
                int side = 0;
                for (int i = 0; i < 2; i++)
                {
                    Vector hitPoint = ((0 == i) ? hitBegin : hitEnd) - lastVertex;

                    hit = WhereIsVectorAboutVector(hitPoint, nextNode);
                    if (hit == HitResult.Hit)
                    {
                        double r = (Math.Abs(nextNode.X) < Math.Abs(nextNode.Y)) //DoubleUtil.IsZero(nextNode.X)
                            ? (hitPoint.Y / nextNode.Y)
                            : (hitPoint.X / nextNode.X);
                        if ((findex > r) && DoubleUtil.IsBetweenZeroAndOne(r))
                        {
                            findex = r;
                        }
                    }
                    else if (hit == HitResult.Right)
                    {
                         side++;
                        if (HitResult.Left == WhereIsVectorAboutVector(
                            hitPoint - nextVertex, nextVertexNextNode))
                        {
                            double r = GetPositionBetweenLines(nextVertex, nextNode, hitPoint);
                            if ((findex > r) && DoubleUtil.IsBetweenZeroAndOne(r))
                            {
                                findex = r;
                            }
                        }
                        else
                        {
                            testNextEdge = true;
                        }
                    }
                    else
                    {
                        side--;
                    }
                }

                //
                if (0 == side)
                {
                    if (hit == HitResult.Hit)
                    {
                        // This segment is collinear with the edge connecting the nodes, 
                        // no need to hit-test the other edges.
                        System.Diagnostics.Debug.Assert(true == DoubleUtil.IsBetweenZeroAndOne(findex));
                        break;
                    }
                    // The hitting segment intersects the line of the edge connecting 
                    // the nodes. Find the findex of the intersection point.
                    double det = -Vector.Determinant(nextNode, hitVector);
                    if (DoubleUtil.IsZero(det) == false)
                    {
                        double s = Vector.Determinant(hitVector, hitBegin - lastVertex) / det;
                        if ((findex > s) && DoubleUtil.IsBetweenZeroAndOne(s))
                        {
                            findex = s;
                        }
                    }
                }
                //
                lastVertex = vertex;
                nextNode = nextVertexNextNode;
            }
            return AdjustFIndex(findex);
        }

        /// <summary>
        /// Clip-Testing a polygonal inking segment against an arc (circle)
        /// </summary>
        /// <param name="spineVector">Vector representing the starting and ending point for the inking 
        ///             segment</param>
        /// <param name="pressureDelta">Represents the difference in the node size for startNode and endNode.
        ///              pressureDelta = (endNode.PressureFactor / beginNode.PressureFactor) - 1</param>
        /// <param name="hitCenter">The center of the hitting circle</param>
        /// <param name="hitRadius">The radius of the hitting circle</param>
        /// <returns>a double representing the point of clipping</returns>
        private double ClipTestArc(Vector spineVector, double pressureDelta, Vector hitCenter, Vector hitRadius)
        {
            // this code is not called, but will be in VNext
            throw new NotImplementedException();
            /*
            double findex = StrokeFIndices.AfterLast;

            double radiusSquared = hitRadius.LengthSquared;
            Vector vertex, lastVertex = _vertices[_vertices.Length - 1];
            Vector nextVertexNextNode, nextNode = spineVector + lastVertex * pressureDelta;
            bool testNextEdge = false;

            for (int k = 0, count = _vertices.Length; 
                k < count || (k == count && testNextEdge);
                k++, lastVertex = vertex, nextNode = nextVertexNextNode)
            {
                vertex = _vertices[k % count];
                Vector nextVertex = vertex - lastVertex;
                nextVertexNextNode = spineVector + (vertex * pressureDelta);

                if (DoubleUtil.IsZero(nextNode.X) && DoubleUtil.IsZero(nextNode.Y))
                {
                    continue;
                }

                bool testConnectingEdge = false;

                if (HitResult.Left == WhereIsVectorAboutVector(nextNode, nextVertex))
                {
                    testNextEdge = false;

                    Vector normal = GetProjection(lastVertex - hitCenter, vertex - hitCenter);
                    if (radiusSquared <= normal.LengthSquared)
                    {
                        if (WhereIsVectorAboutVector(hitCenter - lastVertex, nextVertex) == HitResult.Left)
                        {
                            Vector hitPoint = hitCenter + (normal * Math.Sqrt(radiusSquared / normal.LengthSquared));
                            if (HitResult.Right == WhereIsVectorAboutVector(hitPoint - vertex, nextVertexNextNode))
                            {
                                testNextEdge = true;
                            }
                            else if (HitResult.Left == WhereIsVectorAboutVector(hitPoint - lastVertex, nextNode))
                            {
                                testConnectingEdge = true;
                            }
                            else
                            {
                                // this is it
                                findex = GetPositionBetweenLines(nextVertex, nextNode, hitPoint - lastVertex);
                                System.Diagnostics.Debug.Assert(DoubleUtil.IsBetweenZeroAndOne(findex));
                                break;
                            }
                        }
                    }
                    else if (HitResult.Right == WhereIsVectorAboutVector(hitCenter + normal - lastVertex, nextNode))
                    {
                        testNextEdge = true;
                    }
                    else
                    {
                        testConnectingEdge = true;
                    }
                }
                else if (testNextEdge == true)
                {
                    testNextEdge = false;
                    testConnectingEdge = true;
                }

                if (testConnectingEdge)
                {
                    // Find out the projection of hitCenter on nextNode
                    Vector v = lastVertex - hitCenter;
                    double findexNearest = GetProjectionFIndex(v, v + nextNode);

                    if (findexNearest > 0)
                    {
                        Vector nearest = nextNode * findexNearest;
                        double squaredDistanceFromNearestToHitPoint = radiusSquared - (nearest + v).LengthSquared;
                        if (DoubleUtil.IsZero(squaredDistanceFromNearestToHitPoint) && (findexNearest <= 1))
                        {
                            if (findexNearest < findex)
                            {
                                findex = findexNearest;
                            }
                        }
                        else if ((squaredDistanceFromNearestToHitPoint > 0)
                            && (nearest.LengthSquared >= squaredDistanceFromNearestToHitPoint))
                        {
                            double hitPointFIndex = findexNearest - Math.Sqrt(
                                squaredDistanceFromNearestToHitPoint / nextNode.LengthSquared);
                            System.Diagnostics.Debug.Assert(DoubleUtil.GreaterThanOrClose(hitPointFIndex, 0));
                            if (hitPointFIndex < findex)
                            {
                                findex = hitPointFIndex;
                            }
                        }
                    }
                }
            }

            return AdjustFIndex(findex);
            */
        }

        /// <summary>
        /// Internal access to __vertices
        /// </summary>
        /// <returns></returns>
        internal Vector[] GetVertices()
        {
            return _vertices;
        }

        /// <summary>
        /// Helper function to hit-test the biggest node against hitting contour segments
        /// </summary>
        /// <param name="hitContour">a collection of basic segments outlining the hitting contour</param>
        /// <param name="beginNode">Begin node of the stroke segment to hit-test. Can be empty (none)</param>
        /// <param name="endNode">End node of the stroke segment</param>
        /// <returns>true if hit; false otherwise</returns>
        private bool HitTestPolygonContourSegments(
            IEnumerable<ContourSegment> hitContour, in StrokeNodeData beginNode, in StrokeNodeData endNode)
        {
            bool isHit = false;

            // The bool variable isInside is used here to track that case. It answers to
            // 'Is ink contour inside if the hitting contour?'. It's initialized to 'true" 
            // and then verified for each edge of the hitting contour until there's a hit or
            // until it's false.
            bool isInside = true;

            Point position;
            double pressureFactor;
            if (beginNode.IsEmpty || endNode.PressureFactor > beginNode.PressureFactor)
            {
                position = endNode.Position;
                pressureFactor = endNode.PressureFactor;
            }
            else
            {
                position = beginNode.Position;
                pressureFactor = beginNode.PressureFactor;
            }

            // Enumerate through the segments of the hitting contour and test them 
            // one by one against the contour of the ink node.
            foreach (ContourSegment hitSegment in hitContour)
            {
                if (hitSegment.IsArc)
                {
                    // Adjust the arc for the node' pressure factor.
                    Vector hitCenter = hitSegment.Begin + hitSegment.Radius - position;
                    Vector hitRadius = hitSegment.Radius;
                    if (!DoubleUtil.AreClose(pressureFactor, 1d))
                    {
                        System.Diagnostics.Debug.Assert(DoubleUtil.IsZero(pressureFactor) == false);
                        hitCenter /= pressureFactor;
                        hitRadius /= pressureFactor;
                    }
                    // If the segment is an arc, hit-test against the entire circle the arc is part of.
                    if (true == HitTestPolygonCircle(_vertices, hitCenter, hitRadius))
                    {
                        isHit = true;
                        break;
                    }
                    //
                    if (isInside && (WhereIsVectorAboutArc(
                        position - hitSegment.Begin - hitSegment.Radius,
                        -hitSegment.Radius, hitSegment.Vector - hitSegment.Radius) == HitResult.Hit))
                    {
                        isInside = false;
                    }
                }
                else
                {
                    // Adjust the segment for the node's pressure factor
                    Vector hitBegin = hitSegment.Begin - position;
                    Vector hitEnd = hitBegin + hitSegment.Vector;
                    if (!DoubleUtil.AreClose(pressureFactor, 1d))
                    {
                        System.Diagnostics.Debug.Assert(DoubleUtil.IsZero(pressureFactor) == false);
                        hitBegin /= pressureFactor;
                        hitEnd /= pressureFactor;
                    }
                    // Hit-test the node against the segment
                    if (true == HitTestPolygonSegment(_vertices, hitBegin, hitEnd))
                    {
                        isHit = true;
                        break;
                    }
                    //
                    if (isInside && WhereIsVectorAboutVector(
                        position - hitSegment.Begin, hitSegment.Vector) != HitResult.Right)
                    {
                        isInside = false;
                    }
                }
            }
            return (isInside || isHit);
        }   

        /// <summary>
        /// Helper function to HitTest the the hitting contour against the inking contour
        /// </summary>
        /// <param name="hitContour">a collection of basic segments outlining the hitting contour</param>
        /// <param name="quad">A connecting quad</param>
        /// <param name="beginNode">Begin node of the stroke segment to hit-test. Can be empty (none)</param>
        /// <param name="endNode">End node of the stroke segment</param>
        /// <returns>true if hit; false otherwise</returns>
        private bool HitTestInkContour(
            IEnumerable<ContourSegment> hitContour, Quad quad, in StrokeNodeData beginNode, in StrokeNodeData endNode)
        {
            System.Diagnostics.Debug.Assert(!quad.IsEmpty);
            bool isHit = false;

            // When hit-testing a contour against another contour, like in this case,
            // the default implementation checks whether any edge (segment) of the hitting 
            // contour intersects with the contour of the ink segment. But this doesn't cover 
            // the case when the ink segment is entirely inside of the hitting segment. 
            // The bool variable isInside is used here to track that case. It answers to
            // 'Is ink contour inside if the hitting contour?'. It's initialized to 'true" 
            // and then verified for each edge of the hitting contour until there's a hit or
            // until it's false.
            bool isInside = true;

            // The ink connecting quad is not empty, enumerate through the segments of the 
            // hitting contour and hit-test them one by one against the ink contour.
            foreach (ContourSegment hitSegment in hitContour)
            {
                // Iterate through the vertices of the contour of the ink segment
                // check where the hit segment is about them, return false if it's 
                // on the left side off either of the ink contour segments.

                Vector hitBegin, hitEnd;
                HitResult hitResult;

                // Start with the segment quad.C->quad.D
                if (hitSegment.IsArc)
                {
                    hitBegin = hitSegment.Begin + hitSegment.Radius - beginNode.Position;
                    hitEnd = hitSegment.Radius;
                    hitResult = WhereIsCircleAboutSegment(
                        hitBegin, hitEnd, quad.C - beginNode.Position, quad.D - beginNode.Position);
                }
                else
                {
                    hitBegin = hitSegment.Begin - beginNode.Position;
                    hitEnd = hitBegin + hitSegment.Vector;
                    hitResult = WhereIsSegmentAboutSegment(
                        hitBegin, hitEnd, quad.C - beginNode.Position, quad.D - beginNode.Position);
                }
                if (HitResult.Left == hitResult)
                {
                    if (isInside)
                    {
                        isInside = hitSegment.IsArc
                            ? (WhereIsVectorAboutArc(-hitBegin, -hitSegment.Radius, hitSegment.Vector - hitSegment.Radius) != HitResult.Hit)
                            : (WhereIsVectorAboutVector(-hitBegin, hitSegment.Vector) == HitResult.Right);
                    }
                    // This hitSegment is completely outside of the ink contour, 
                    // continue with the next one.
                    continue;
                }

                // Continue clockwise from quad.D to quad.A, then to quad.B, ..., quad.C

                HitResult firstResult = hitResult, lastResult = hitResult;
                double pressureFactor = beginNode.PressureFactor;

                // Find the index of the vertex that is quad.D 
                // Use count var to avoid infinite loop, normally this shouldn't 
                // happen but it doesn't hurt to check it just in case.
                int i = 0, count = _vertices.Length;
                Vector vertex = new Vector();
                for (i = 0; i < count; i++)
                {
                    vertex = _vertices[i] * pressureFactor;
                    if (DoubleUtil.AreClose((beginNode.Position + vertex), quad.D))
                    {
                        break;
                    }
                }
                System.Diagnostics.Debug.Assert(i < count);

                int k;
                for (k = 0; k < 2; k++)
                {
                    count = _vertices.Length;
                    Point nodePosition = (k == 0) ? beginNode.Position : endNode.Position;
                    Point end = (k == 0) ? quad.A : quad.C;

                    // Iterate over the vertices on 
                    //          beginNode(k=0)from quad.D to quad.A 
                    //    or 
                    //          endNode(k=1)from quad.A to quad.B ... to quad.C
                    while (((nodePosition + vertex) != end) && (count != 0))
                    {
                        // Find out the next vertex
                        i = (i + 1) % _vertices.Length;
                        Vector nextVertex = _vertices[i] * pressureFactor;

                        // Hit-test the hitting segment against the current edge
                        hitResult = hitSegment.IsArc
                            ? WhereIsCircleAboutSegment(hitBegin, hitEnd, vertex, nextVertex)
                            : WhereIsSegmentAboutSegment(hitBegin, hitEnd, vertex, nextVertex);

                        if (HitResult.Hit == hitResult)
                        {
                            return true;  //Got a hit
                        }
                        if (true == IsOutside(hitResult, lastResult))
                        {
                            // This hitSegment is definitely outside the ink contour, drop it.
                            // Change k to something > 2 to leave the for loop and skip 
                            // IsOutside at the bottom 
                            k = 3;
                            break;
                        }
                        lastResult = hitResult;
                        vertex = nextVertex;
                        count--;
                    }
                    System.Diagnostics.Debug.Assert(count > 0);

                    if (k == 0)
                    {
                        // Make some adjustments for the second one to continue iterating through 
                        // quad.AB and the outer segments of endNode up to quad.C
                        pressureFactor = endNode.PressureFactor;
                        Vector spineVector = endNode.Position - beginNode.Position;
                        vertex -= spineVector; // now vertex = quad.A - spineVector
                        hitBegin -= spineVector; // adjust hitBegin to the space of endNode
                        if (hitSegment.IsArc == false)
                        {
                            hitEnd -= spineVector;
                        }

                        // Find the index of the vertex that is quad.B
                        count = _vertices.Length;
                        while (!DoubleUtil.AreClose((endNode.Position + _vertices[i] * pressureFactor), quad.B) && (count != 0))
                        {
                            i = (i + 1) % _vertices.Length;
                            count--;
                        }
                        System.Diagnostics.Debug.Assert(count > 0);
                        i--;
                    }
                }
                if ((k == 2) && (false == IsOutside(firstResult, hitResult)))
                {
                    isHit = true;
                    break;
                }
                //
                if (isInside)
                {
                    isInside = hitSegment.IsArc
                        ? (WhereIsVectorAboutArc(-hitBegin, -hitSegment.Radius, hitSegment.Vector - hitSegment.Radius) != HitResult.Hit)
                        : (WhereIsVectorAboutVector(-hitBegin, hitSegment.Vector) == HitResult.Right);
                }
            }
            return (isHit||isInside);
        }


        /// <summary>
        /// Helper function to Hit-test against the two stroke nodes only (excluding the connecting quad). 
        /// </summary>
        /// <param name="hitSegment"></param>
        /// <param name="beginNode"></param>
        /// <param name="endNode"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private bool HitTestStrokeNodes(
            in ContourSegment hitSegment, in StrokeNodeData beginNode, in StrokeNodeData endNode, ref StrokeFIndices result)
        {
            // First, find out if hitSegment intersects with either of the ink nodes
            bool isHit = false;
            for (int node = 0; node < 2; node++)
            {
                Point position;
                double pressureFactor;
                if (node == 0)
                {
                    if (isHit && DoubleUtil.AreClose(result.BeginFIndex, StrokeFIndices.BeforeFirst))
                    {
                        continue;
                    }
                    position = beginNode.Position;
                    pressureFactor = beginNode.PressureFactor;
                }
                else
                {
                    if (isHit && DoubleUtil.AreClose(result.EndFIndex, StrokeFIndices.AfterLast))
                    {
                        continue;
                    }
                    position = endNode.Position;
                    pressureFactor = endNode.PressureFactor;
                }

                Vector hitBegin, hitEnd;

                // Adjust the segment for the node's pressure factor
                if (hitSegment.IsArc)
                {
                    hitBegin = hitSegment.Begin - position + hitSegment.Radius;
                    hitEnd = hitSegment.Radius;
                }
                else
                {
                    hitBegin = hitSegment.Begin - position;
                    hitEnd = hitBegin + hitSegment.Vector;
                }

                if (pressureFactor != 1)
                {
                    System.Diagnostics.Debug.Assert(DoubleUtil.IsZero(pressureFactor) == false);
                    hitBegin /= pressureFactor;
                    hitEnd /= pressureFactor;
                }
                // Hit-test the node against the segment
                if (hitSegment.IsArc
                    ? HitTestPolygonCircle(_vertices, hitBegin, hitEnd)
                    : HitTestPolygonSegment(_vertices, hitBegin, hitEnd))
                {
                    isHit = true;
                    if (node == 0)
                    {
                        result.BeginFIndex = StrokeFIndices.BeforeFirst;
                        if (DoubleUtil.AreClose(result.EndFIndex, StrokeFIndices.AfterLast))
                        {
                            break;
                        }
                    }
                    else
                    {
                        result.EndFIndex = StrokeFIndices.AfterLast;
                        if (beginNode.IsEmpty)
                        {
                            result.BeginFIndex = StrokeFIndices.BeforeFirst;
                            break;
                        }
                        if (DoubleUtil.AreClose(result.BeginFIndex, StrokeFIndices.BeforeFirst))
                        {
                            break;
                        }
                    }
                }
            }
            return isHit;
        }

        /// <summary>
        ///  Calculate the clip location
        /// </summary>
        /// <param name="hitSegment">the hitting segment</param>
        /// <param name="beginNode">begin node</param>
        /// <param name="spineVector"></param>
        /// <param name="pressureDelta"></param>
        /// <returns>the clip location. not-clip if return StrokeFIndices.BeforeFirst</returns>
        private double CalculateClipLocation(
           in ContourSegment hitSegment, in StrokeNodeData beginNode, Vector spineVector, double pressureDelta)
        {
            double findex = StrokeFIndices.BeforeFirst;
            bool clipIt = hitSegment.IsArc ? true
                //? (WhereIsVectorAboutArc(beginNode.Position - hitSegment.Begin - hitSegment.Radius,
                //            -hitSegment.Radius, hitSegment.Vector - hitSegment.Radius) == HitResult.Hit)
                : (WhereIsVectorAboutVector(
                                   beginNode.Position - hitSegment.Begin, hitSegment.Vector) == HitResult.Left);
            if (clipIt)
            {
                findex = hitSegment.IsArc
                    ? ClipTestArc(spineVector, pressureDelta,
                        (hitSegment.Begin + hitSegment.Radius - beginNode.Position) / beginNode.PressureFactor,
                        hitSegment.Radius / beginNode.PressureFactor)
                    : ClipTest(spineVector, pressureDelta,
                        (hitSegment.Begin - beginNode.Position) / beginNode.PressureFactor,
                        (hitSegment.End - beginNode.Position) / beginNode.PressureFactor);
                
                // ClipTest returns StrokeFIndices.AfterLast to indicate a false hit test.
                // But the caller CutTest expects StrokeFIndices.BeforeFirst when there is no hit.
                if ( findex == StrokeFIndices.AfterLast )
                {
                    findex = StrokeFIndices.BeforeFirst;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(findex >= 0 && findex <= 1);
                }
            }
            return findex;
        }

        /// <summary>
        /// Helper method used to determine if we came up with a bogus result during hit testing
        /// </summary>
        protected bool IsInvalidCutTestResult(StrokeFIndices result)
        {
            //
            // check for three invalid states
            // 1) BeforeFirst == AfterLast
            // 2) BeforeFirst, < 0
            // 3) > 1, AfterLast
            //
            if (DoubleUtil.AreClose(result.BeginFIndex, result.EndFIndex) ||
                DoubleUtil.AreClose(result.BeginFIndex, StrokeFIndices.BeforeFirst) && result.EndFIndex < 0.0f ||
                result.BeginFIndex > 1.0f && DoubleUtil.AreClose(result.EndFIndex, StrokeFIndices.AfterLast))
            {
                return true;
            }
            return false;
        }

        #endregion

        #region Instance data
        
        // Shape parameters
        private Rect        _shapeBounds = Rect.Empty;
        protected Vector[]    _vertices;

        #endregion
    }
}
