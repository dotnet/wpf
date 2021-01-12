// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Ink;
using System.Windows.Input;
using System.Diagnostics;

namespace MS.Internal.Ink
{
    /// <summary>
    /// StrokeNodeOperations implementation for elliptical nodes
    /// </summary>
    internal class EllipticalNodeOperations : StrokeNodeOperations
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nodeShape"></param>
        internal EllipticalNodeOperations(StylusShape nodeShape)
            : base(nodeShape)
        {
            System.Diagnostics.Debug.Assert((nodeShape != null) && nodeShape.IsEllipse);

            _radii = new Size(nodeShape.Width * 0.5, nodeShape.Height * 0.5);

            // All operations with ellipses become simple(r) if transfrom ellipses into circles.
            // Use the max of the radii for the radius of the circle
            _radius = Math.Max(_radii.Width, _radii.Height);

            // Compute ellipse-to-circle and circle-to-elliipse transforms. The former is used
            // in hit-testing operations while the latter is used when computing vertices of
            // a quadrangle connecting two ellipses
            _transform = nodeShape.Transform;
            _nodeShapeToCircle = _transform;

            Debug.Assert(_nodeShapeToCircle.HasInverse, "About to invert a non-invertible transform");
            _nodeShapeToCircle.Invert();
            if (DoubleUtil.AreClose(_radii.Width, _radii.Height))
            {
                _circleToNodeShape = _transform;
            }
            else
            {
                // Reverse the rotation
                if (false == DoubleUtil.IsZero(nodeShape.Rotation))
                {
                    _nodeShapeToCircle.Rotate(-nodeShape.Rotation);
                    Debug.Assert(_nodeShapeToCircle.HasInverse, "Just rotated an invertible transform and produced a non-invertible one");
                }

                // Scale to enlarge
                double sx, sy;
                if (_radii.Width > _radii.Height)
                {
                    sx = 1;
                    sy = _radii.Width / _radii.Height;
                }
                else
                {
                    sx = _radii.Height / _radii.Width;
                    sy = 1;
                }
                _nodeShapeToCircle.Scale(sx, sy);
                Debug.Assert(_nodeShapeToCircle.HasInverse, "Just scaled an invertible transform and produced a non-invertible one");

                _circleToNodeShape = _nodeShapeToCircle;
                _circleToNodeShape.Invert();
            }
        }

        /// <summary>
        /// This is probably not the best (design-wise) but the cheapest way to tell
        /// EllipticalNodeOperations from all other implementations of node operations.
        /// </summary>
        internal override bool IsNodeShapeEllipse { get { return true; } }

        /// <summary>
        /// Finds connecting points for a pair of stroke nodes
        /// </summary>
        /// <param name="beginNode">a node to connect</param>
        /// <param name="endNode">another node, next to beginNode</param>
        /// <returns>connecting quadrangle</returns>
        internal override Quad GetConnectingQuad(in StrokeNodeData beginNode,in StrokeNodeData endNode)
        {
            if (beginNode.IsEmpty || endNode.IsEmpty || DoubleUtil.AreClose(beginNode.Position, endNode.Position))
            {
                return Quad.Empty;
            }

            // Get the vector between the node positions
            Vector spine = endNode.Position - beginNode.Position;
            if (_nodeShapeToCircle.IsIdentity == false)
            {
                spine = _nodeShapeToCircle.Transform(spine);
            }

            double beginRadius = _radius * beginNode.PressureFactor;
            double endRadius = _radius * endNode.PressureFactor;

            // Get the vector and the distance between the node positions
            double distanceSquared = spine.LengthSquared;
            double delta = endRadius - beginRadius;
            double deltaSquared = DoubleUtil.IsZero(delta) ? 0 : (delta * delta);

            if (DoubleUtil.LessThanOrClose(distanceSquared, deltaSquared))
            {
                // One circle is contained within the other
                return Quad.Empty;
            }

            // Thus, at this point, distance > 0, which avoids the DivideByZero error
            // Also, here, distanceSquared > deltaSquared
            // Thus, 0 <= rSin < 1
            // Get the components of the radius vectors
            double distance = Math.Sqrt(distanceSquared);

            spine /= distance;

            Vector rad = spine;

            // Turn left
            double temp = rad.Y;
            rad.Y = -rad.X;
            rad.X = temp;

            Vector vectorToLeftTangent, vectorToRightTangent;
            double rSinSquared = deltaSquared / distanceSquared;

            if (DoubleUtil.IsZero(rSinSquared))
            {
                vectorToLeftTangent = rad;
                vectorToRightTangent = -rad;
            }
            else
            {
                rad *= Math.Sqrt(1 - rSinSquared);
                spine *= Math.Sqrt(rSinSquared);
                if (beginNode.PressureFactor < endNode.PressureFactor)
                {
                    spine = -spine;
                }

                vectorToLeftTangent = spine + rad;
                vectorToRightTangent = spine - rad;
            }

            // Get the common tangent points

            if (_circleToNodeShape.IsIdentity == false)
            {
                vectorToLeftTangent = _circleToNodeShape.Transform(vectorToLeftTangent);
                vectorToRightTangent = _circleToNodeShape.Transform(vectorToRightTangent);
            }

            return new Quad(beginNode.Position + (vectorToLeftTangent * beginRadius),
                            endNode.Position + (vectorToLeftTangent * endRadius),
                            endNode.Position + (vectorToRightTangent * endRadius),
                            beginNode.Position + (vectorToRightTangent * beginRadius));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <param name="quad"></param>
        /// <returns></returns>
        internal override IEnumerable<ContourSegment> GetContourSegments(StrokeNodeData node, Quad quad)
        {
            System.Diagnostics.Debug.Assert(node.IsEmpty == false);

            if (quad.IsEmpty)
            {
                Point point = node.Position;
                point.X += _radius;
                yield return new ContourSegment(point, point, node.Position);
            }
            else if (_nodeShapeToCircle.IsIdentity)
            {
                yield return new ContourSegment(quad.A, quad.B);
                yield return new ContourSegment(quad.B, quad.C, node.Position);
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
        internal override IEnumerable<ContourSegment> GetNonBezierContourSegments(StrokeNodeData beginNode, StrokeNodeData endNode)
        {
            Quad quad = beginNode.IsEmpty ? Quad.Empty : base.GetConnectingQuad(beginNode, endNode);
            return base.GetContourSegments(endNode, quad);
        }


        /// <summary>
        /// Hit-tests a stroke segment defined by two nodes against a linear segment.
        /// </summary>
        /// <param name="beginNode">Begin node of the stroke segment to hit-test. Can be empty (none)</param>
        /// <param name="endNode">End node of the stroke segment</param>
        /// <param name="quad">Pre-computed quadrangle connecting the two nodes.
        /// Can be empty if the begion node is empty or when one node is entirely inside the other</param>
        /// <param name="hitBeginPoint">an end point of the hitting linear segment</param>
        /// <param name="hitEndPoint">an end point of the hitting linear segment</param>
        /// <returns>true if the hitting segment intersect the contour comprised of the two stroke nodes</returns>
        internal override bool HitTest(
           in StrokeNodeData beginNode, in StrokeNodeData endNode, Quad quad, Point hitBeginPoint, Point hitEndPoint)
        {
            StrokeNodeData bigNode, smallNode;
            if (beginNode.IsEmpty || (quad.IsEmpty && (endNode.PressureFactor > beginNode.PressureFactor)))
            {
                // Need to test one node only
                bigNode = endNode;
                smallNode = StrokeNodeData.Empty;
            }
            else
            {
                // In this case the size doesn't matter.
                bigNode = beginNode;
                smallNode = endNode;
            }

            // Compute the positions of the involved points relative to bigNode.
            Vector hitBegin = hitBeginPoint - bigNode.Position;
            Vector hitEnd = hitEndPoint - bigNode.Position;

            // If the node shape is an ellipse, transform the scene to turn the shape to a circle
            if (_nodeShapeToCircle.IsIdentity == false)
            {
                hitBegin = _nodeShapeToCircle.Transform(hitBegin);
                hitEnd = _nodeShapeToCircle.Transform(hitEnd);
            }

            bool isHit = false;

            // Hit-test the big node
            double bigRadius = _radius * bigNode.PressureFactor;
            Vector nearest = GetNearest(hitBegin, hitEnd);
            if (nearest.LengthSquared <= (bigRadius * bigRadius))
            {
                isHit = true;
            }
            else if (quad.IsEmpty == false)
            {
                // Hit-test the other node
                Vector spineVector = smallNode.Position - bigNode.Position;
                if (_nodeShapeToCircle.IsIdentity == false)
                {
                    spineVector = _nodeShapeToCircle.Transform(spineVector);
                }
                double smallRadius = _radius * smallNode.PressureFactor;
                nearest = GetNearest(hitBegin - spineVector, hitEnd - spineVector);
                if ((nearest.LengthSquared <= (smallRadius * smallRadius)) || HitTestQuadSegment(quad, hitBeginPoint, hitEndPoint))
                {
                    isHit = true;
                }
            }

            return isHit;
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
        internal override bool HitTest(
            in StrokeNodeData beginNode, in StrokeNodeData endNode, Quad quad, IEnumerable<ContourSegment> hitContour)
        {
            StrokeNodeData bigNode, smallNode;
            double bigRadiusSquared, smallRadiusSquared = 0;
            Vector spineVector;
            if (beginNode.IsEmpty || (quad.IsEmpty && (endNode.PressureFactor > beginNode.PressureFactor)))
            {
                // Need to test one node only
                bigNode = endNode;
                smallNode = StrokeNodeData.Empty;
                spineVector = new Vector();
            }
            else
            {
                // In this case the size doesn't matter.
                bigNode = beginNode;
                smallNode = endNode;

                smallRadiusSquared = _radius * smallNode.PressureFactor;
                smallRadiusSquared *= smallRadiusSquared;

                // Find position of smallNode relative to the bigNode.
                spineVector = smallNode.Position - bigNode.Position;
                // If the node shape is an ellipse, transform the scene to turn the shape to a circle
                if (_nodeShapeToCircle.IsIdentity == false)
                {
                    spineVector = _nodeShapeToCircle.Transform(spineVector);
                }
            }

            bigRadiusSquared = _radius * bigNode.PressureFactor;
            bigRadiusSquared *= bigRadiusSquared;

            bool isHit = false;

            // When hit-testing a contour against another contour, like in this case,
            // the default implementation checks whether any edge (segment) of the hitting
            // contour intersects with the contour of the ink segment. But this doesn't cover
            // the case when the ink segment is entirely inside of the hitting segment.
            // The bool variable isInside is used here to track that case. It answers the question
            // 'Is ink contour inside if the hitting contour?'. It's initialized to 'true"
            // and then verified for each edge of the hitting contour until there's a hit or
            // until it's false.
            bool isInside = true;

            foreach (ContourSegment hitSegment in hitContour)
            {
                if (hitSegment.IsArc)
                {
                    // ISSUE-2004/06/15-vsmirnov - ellipse vs arc hit-testing is not implemented
                    // and currently disabled in ErasingStroke
                }
                else
                {
                    // Find position of the hitting segment relative to bigNode transformed to circle.
                    Vector hitBegin = hitSegment.Begin - bigNode.Position;
                    Vector hitEnd = hitBegin + hitSegment.Vector;
                    if (_nodeShapeToCircle.IsIdentity == false)
                    {
                        hitBegin = _nodeShapeToCircle.Transform(hitBegin);
                        hitEnd = _nodeShapeToCircle.Transform(hitEnd);
                    }

                    // Hit-test the big node
                    Vector nearest = GetNearest(hitBegin, hitEnd);
                    if (nearest.LengthSquared <= bigRadiusSquared)
                    {
                        isHit = true;
                        break;
                    }

                    // Hit-test the other node
                    if (quad.IsEmpty == false)
                    {
                        nearest = GetNearest(hitBegin - spineVector, hitEnd - spineVector);
                        if ((nearest.LengthSquared <= smallRadiusSquared) ||
                            HitTestQuadSegment(quad, hitSegment.Begin, hitSegment.End))
                        {
                            isHit = true;
                            break;
                        }
                    }

                    // While there's still a chance to find the both nodes inside the hitting contour,
                    // continue checking on position of the endNode relative to the edges of the hitting contour.
                    if (isInside &&
                        (WhereIsVectorAboutVector(endNode.Position - hitSegment.Begin, hitSegment.Vector) != HitResult.Right))
                    {
                        isInside = false;
                    }
                }
            }

            return (isHit || isInside);
        }

        /// <summary>
        /// Cut-test ink segment defined by two nodes and a connecting quad against a linear segment
        /// </summary>
        /// <param name="beginNode">Begin node of the ink segment</param>
        /// <param name="endNode">End node of the ink segment</param>
        /// <param name="quad">Pre-computed quadrangle connecting the two ink nodes</param>
        /// <param name="hitBeginPoint">egin point of the hitting segment</param>
        /// <param name="hitEndPoint">End point of the hitting segment</param>
        /// <returns>Exact location to cut at represented by StrokeFIndices</returns>
        internal override StrokeFIndices CutTest(
            in StrokeNodeData beginNode, in StrokeNodeData endNode, Quad quad, Point hitBeginPoint, Point hitEndPoint)
        {
            // Compute the positions of the involved points relative to the endNode.
            Vector spineVector = beginNode.IsEmpty ? new Vector(0, 0) : (beginNode.Position - endNode.Position);
            Vector hitBegin = hitBeginPoint - endNode.Position;
            Vector hitEnd = hitEndPoint - endNode.Position;

            // If the node shape is an ellipse, transform the scene to turn the shape to a circle
            if (_nodeShapeToCircle.IsIdentity == false)
            {
                spineVector = _nodeShapeToCircle.Transform(spineVector);
                hitBegin = _nodeShapeToCircle.Transform(hitBegin);
                hitEnd = _nodeShapeToCircle.Transform(hitEnd);
            }

            StrokeFIndices result = StrokeFIndices.Empty;

            // Hit-test the end node
            double beginRadius = 0, endRadius = _radius * endNode.PressureFactor;
            Vector nearest = GetNearest(hitBegin, hitEnd);
            if (nearest.LengthSquared <= (endRadius * endRadius))
            {
                result.EndFIndex = StrokeFIndices.AfterLast;
                result.BeginFIndex = beginNode.IsEmpty ? StrokeFIndices.BeforeFirst : 1;
            }
            if (beginNode.IsEmpty == false)
            {
                // Hit-test the first node
                beginRadius = _radius * beginNode.PressureFactor;
                nearest = GetNearest(hitBegin - spineVector, hitEnd - spineVector);
                if (nearest.LengthSquared <= (beginRadius * beginRadius))
                {
                    result.BeginFIndex = StrokeFIndices.BeforeFirst;
                    if (!DoubleUtil.AreClose(result.EndFIndex, StrokeFIndices.AfterLast))
                    {
                        result.EndFIndex = 0;
                    }
                }
            }

            // If both nodes are hit or nothing is hit at all, return.
            if (result.IsFull || quad.IsEmpty
                || (result.IsEmpty && (HitTestQuadSegment(quad, hitBeginPoint, hitEndPoint) == false)))
            {
                return result;
            }

            // Find out whether the {begin, end} segment intersects with the contour
            // of the stroke segment {_lastNode, _thisNode}, and find the lower index
            // of the fragment to cut out.
            if (!DoubleUtil.AreClose(result.BeginFIndex, StrokeFIndices.BeforeFirst))
            {
                result.BeginFIndex = ClipTest(-spineVector, beginRadius, endRadius, hitBegin - spineVector, hitEnd - spineVector);
            }

            if (!DoubleUtil.AreClose(result.EndFIndex, StrokeFIndices.AfterLast))
            {
                result.EndFIndex = 1 - ClipTest(spineVector, endRadius, beginRadius, hitBegin, hitEnd);
            }

            if (IsInvalidCutTestResult(result))
            {
                return StrokeFIndices.Empty;
            }

            return result;
        }

        /// <summary>
        /// CutTest an inking StrokeNode segment (two nodes and a connecting quadrangle) against a hitting contour
        /// (represented by an enumerator of Contoursegments).
        /// </summary>
        /// <param name="beginNode">The begin StrokeNodeData</param>
        /// <param name="endNode">The end StrokeNodeData</param>
        /// <param name="quad">Connecing quadrangle between the begin and end inking node</param>
        /// <param name="hitContour">The hitting ContourSegments</param>
        /// <returns>StrokeFIndices representing the location for cutting</returns>
        internal override StrokeFIndices CutTest(
            in StrokeNodeData beginNode, in StrokeNodeData endNode, Quad quad, IEnumerable<ContourSegment> hitContour)
        {
            // Compute the positions of the beginNode relative to the endNode.
            Vector spineVector = beginNode.IsEmpty ? new Vector(0, 0) : (beginNode.Position - endNode.Position);
            // If the node shape is an ellipse, transform the scene to turn the shape to a circle
            if (_nodeShapeToCircle.IsIdentity == false)
            {
                spineVector = _nodeShapeToCircle.Transform(spineVector);
            }

            double beginRadius = 0, endRadius;
            double beginRadiusSquared = 0, endRadiusSquared;

            endRadius = _radius * endNode.PressureFactor;
            endRadiusSquared = endRadius * endRadius;
            if (beginNode.IsEmpty == false)
            {
                beginRadius = _radius * beginNode.PressureFactor;
                beginRadiusSquared = beginRadius * beginRadius;
            }

            bool isInside = true;
            StrokeFIndices result = StrokeFIndices.Empty;

            foreach (ContourSegment hitSegment in hitContour)
            {
                if (hitSegment.IsArc)
                {
                    // ISSUE-2004/06/15-vsmirnov - ellipse vs arc hit-testing is not implemented
                    // and currently disabled in ErasingStroke
                }
                else
                {
                    Vector hitBegin = hitSegment.Begin - endNode.Position;
                    Vector hitEnd = hitBegin + hitSegment.Vector;

                    // If the node shape is an ellipse, transform the scene to turn
                    // the shape into circle.
                    if (_nodeShapeToCircle.IsIdentity == false)
                    {
                        hitBegin = _nodeShapeToCircle.Transform(hitBegin);
                        hitEnd = _nodeShapeToCircle.Transform(hitEnd);
                    }

                    bool isHit = false;

                    // Hit-test the end node
                    Vector nearest = GetNearest(hitBegin, hitEnd);
                    if (nearest.LengthSquared < endRadiusSquared)
                    {
                        isHit = true;
                        if (!DoubleUtil.AreClose(result.EndFIndex, StrokeFIndices.AfterLast))
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

                    if ((beginNode.IsEmpty == false) && (!isHit || !DoubleUtil.AreClose(result.BeginFIndex, StrokeFIndices.BeforeFirst)))
                    {
                        // Hit-test the first node
                        nearest = GetNearest(hitBegin - spineVector, hitEnd - spineVector);
                        if (nearest.LengthSquared < beginRadiusSquared)
                        {
                            isHit = true;
                            if (!DoubleUtil.AreClose(result.BeginFIndex, StrokeFIndices.BeforeFirst))
                            {
                                result.BeginFIndex = StrokeFIndices.BeforeFirst;
                                if (DoubleUtil.AreClose(result.EndFIndex, StrokeFIndices.AfterLast))
                                {
                                    break;
                                }
                            }
                        }
                    }

                    // If both nodes are hit or nothing is hit at all, return.
                    if (beginNode.IsEmpty || (!isHit && (quad.IsEmpty ||
                        (HitTestQuadSegment(quad, hitSegment.Begin, hitSegment.End) == false))))
                    {
                        if (isInside && (WhereIsVectorAboutVector(
                            endNode.Position - hitSegment.Begin, hitSegment.Vector) != HitResult.Right))
                        {
                            isInside = false;
                        }
                        continue;
                    }

                    isInside = false;

                    // Calculate the exact locations to cut.
                    CalculateCutLocations(spineVector, hitBegin, hitEnd, endRadius, beginRadius, ref result);

                    if (result.IsFull)
                    {
                        break;
                    }
                }
            }

            //
            if (!result.IsFull)
            {
                if (isInside == true)
                {
                    System.Diagnostics.Debug.Assert(result.IsEmpty);
                    result = StrokeFIndices.Full;
                }
                else if ((DoubleUtil.AreClose(result.EndFIndex, StrokeFIndices.BeforeFirst)) && (!DoubleUtil.AreClose(result.BeginFIndex, StrokeFIndices.AfterLast)))
                {
                    result.EndFIndex = StrokeFIndices.AfterLast;
                }
                else if ((DoubleUtil.AreClose(result.BeginFIndex,StrokeFIndices.AfterLast)) && (!DoubleUtil.AreClose(result.EndFIndex, StrokeFIndices.BeforeFirst)))
                {
                    result.BeginFIndex = StrokeFIndices.BeforeFirst;
                }
            }

            if (IsInvalidCutTestResult(result))
            {
                return StrokeFIndices.Empty;
            }

            return result;
        }

        /// <summary>
        /// Clip-Testing a circluar inking segment against a linear segment.
        /// See http://tabletpc/longhorn/Specs/Rendering%20and%20Hit-Testing%20Ink%20in%20Avalon%20M11.doc section
        /// 5.4.4.14	Clip-Testing a Circular Inking Segment against a Linear Segment for details of the algorithm
        /// </summary>
        /// <param name="spineVector">Represent the spine of the inking segment pointing from the beginNode to endNode</param>
        /// <param name="beginRadius">Radius of the beginNode</param>
        /// <param name="endRadius">Radius of the endNode</param>
        /// <param name="hitBegin">Hitting segment start point</param>
        /// <param name="hitEnd">Hitting segment end point</param>
        /// <returns>A double which represents the location for cutting</returns>
        private static double ClipTest(Vector spineVector, double beginRadius, double endRadius, Vector hitBegin, Vector hitEnd)
        {
            // First handle the special case when the spineVector is (0,0). In other words, this is the case
            // when the stylus stays at the the location but pressure changes.
            if (DoubleUtil.IsZero(spineVector.X) && DoubleUtil.IsZero(spineVector.Y))
            {
                System.Diagnostics.Debug.Assert(DoubleUtil.AreClose(beginRadius, endRadius) == false);

                Vector nearest = GetNearest(hitBegin, hitEnd);
                double radius;
                if (nearest.X == 0)
                {
                    radius = Math.Abs(nearest.Y);
                }
                else if (nearest.Y == 0)
                {
                    radius = Math.Abs(nearest.X);
                }
                else
                {
                    radius = nearest.Length;
                }
                return AdjustFIndex((radius - beginRadius) / (endRadius - beginRadius));
            }

            // This change to ClipTest with a point if the two hitting segment are close enough.
            if (DoubleUtil.AreClose(hitBegin, hitEnd))
            {
                return ClipTest(spineVector, beginRadius, endRadius, hitBegin);
            }

            double findex;
            Vector hitVector = hitEnd - hitBegin;

            if (DoubleUtil.IsZero(Vector.Determinant(spineVector, hitVector)))
            {
                // hitVector and spineVector are parallel
                findex = ClipTest(spineVector, beginRadius, endRadius, GetNearest(hitBegin, hitEnd));
                System.Diagnostics.Debug.Assert(!double.IsNaN(findex));
            }
            else
            {
                // On the line defined by the segment find point P1Xp, the nearest to the beginNode.Position
                double x = GetProjectionFIndex(hitBegin, hitEnd);
                Vector P1Xp = hitBegin + (hitVector * x);
                if (P1Xp.LengthSquared < (beginRadius * beginRadius))
                {
                    System.Diagnostics.Debug.Assert(DoubleUtil.IsBetweenZeroAndOne(x) == false);
                    findex = ClipTest(spineVector, beginRadius, endRadius, (0 > x) ? hitBegin : hitEnd);
                    System.Diagnostics.Debug.Assert(!double.IsNaN(findex));
                }
                else
                {
                    // Find the projection point P of endNode.Position to the line (beginNode.Position, B).
                    Vector P1P2p = spineVector + GetProjection(-spineVector, P1Xp - spineVector);

                    //System.Diagnostics.Debug.Assert(false == DoubleUtil.IsZero(P1P2p.LengthSquared));
                    //System.Diagnostics.Debug.Assert(false == DoubleUtil.IsZero(endRadius - beginRadius + P1P2p.Length));
                    // There checks are here since if either fail no real solution can be caculated and we may
                    // as well bail out now and save the caculations that are below.
                    if (DoubleUtil.IsZero(P1P2p.LengthSquared) || DoubleUtil.IsZero(endRadius - beginRadius + P1P2p.Length))
                        return 1d;

                    // Calculate the findex of the point to split the ink segment at.
                    findex = (P1Xp.Length - beginRadius) / (endRadius - beginRadius + P1P2p.Length);
                    System.Diagnostics.Debug.Assert(!double.IsNaN(findex));

                    // Find the projection of the split point to the line of this segment.
                    Vector S = spineVector * findex;

                    double r = GetProjectionFIndex(hitBegin - S, hitEnd - S);

                    // If the nearest point misses the segment, then find the findex
                    // of the node nearest to the segment.
                    if (false == DoubleUtil.IsBetweenZeroAndOne(r))
                    {
                        findex = ClipTest(spineVector, beginRadius, endRadius, (0 > r) ? hitBegin : hitEnd);
                        System.Diagnostics.Debug.Assert(!double.IsNaN(findex));
                    }
                }
            }
            return AdjustFIndex(findex);
        }

        /// <summary>
        /// Clip-Testing a circular inking segment again a hitting point.
        ///
        /// What need to find out a doulbe value s, which is between 0 and 1, such that
        ///     DistanceOf(hit - s*spine) = beginRadius + s * (endRadius - beginRadius)
        /// That is
        ///     (hit.X-s*spine.X)^2 + (hit.Y-s*spine.Y)^2 = [beginRadius + s*(endRadius-beginRadius)]^2
        /// Rearrange
        ///     A*s^2 + B*s + C = 0
        /// where the value of A, B and C are described in the source code.
        /// Solving for s:
        ///             s = (-B + sqrt(B^2-4A*C))/(2A)  or s = (-B - sqrt(B^2-4A*C))/(2A)
        /// The smaller value between 0 and 1 is the one we want and discard the other one.
        /// </summary>
        /// <param name="spine">Represent the spine of the inking segment pointing from the beginNode to endNode</param>
        /// <param name="beginRadius">Radius of the beginNode</param>
        /// <param name="endRadius">Radius of the endNode</param>
        /// <param name="hit">The hitting point</param>
        /// <returns>A double which represents the location for cutting</returns>
        private static double ClipTest(Vector spine, double beginRadius, double endRadius, Vector hit)
        {
            double radDiff = endRadius - beginRadius;
            double A = spine.X*spine.X + spine.Y*spine.Y - radDiff*radDiff;
            double B = -2.0f*(hit.X*spine.X + hit.Y * spine.Y + beginRadius*radDiff);
            double C = hit.X * hit.X + hit.Y * hit.Y - beginRadius * beginRadius;

            // There checks are here since if either fail no real solution can be caculated and we may
            // as well bail out now and save the caculations that are below.
            if (DoubleUtil.IsZero(A) || !DoubleUtil.GreaterThanOrClose(B*B, 4.0f*A*C))
                return 1d;

            double tmp = Math.Sqrt(B*B-4.0f * A * C);
            double s1 = (-B + tmp)/(2.0f * A);
            double s2 = (-B - tmp)/(2.0f * A);
            double findex;

            if (DoubleUtil.IsBetweenZeroAndOne(s1) && DoubleUtil.IsBetweenZeroAndOne(s1))
            {
                findex = Math.Min(s1, s2);
            }
            else if (DoubleUtil.IsBetweenZeroAndOne(s1))
            {
                findex = s1;
            }
            else if (DoubleUtil.IsBetweenZeroAndOne(s2))
            {
                findex = s2;
            }
            else
            {
                // There is still possiblity that value like 1.0000000000000402 is not considered
                // as "IsOne" by DoubleUtil class. We should be at either one of the following two cases:
                // 1. s1/s2 around 0 but not close enough (say -0.0000000000001)
                // 2. s1/s2 around 1 but not close enought (say 1.0000000000000402)

                if (s1 > 1d && s2 > 1d)
                {
                    findex = 1d;
                }
                else if (s1 < 0d && s2 < 0d)
                {
                    findex = 0d;
                }
                else
                {
                    findex = Math.Abs(Math.Min(s1, s2) - 0d) < Math.Abs(Math.Max(s1, s2) - 1d) ? 0d : 1d;
                }
            }
            return AdjustFIndex(findex);
        }

        /// <summary>
        /// Helper function to find out the relative location of a segment {segBegin, segEnd} against
        /// a strokeNode (spine).
        /// </summary>
        /// <param name="spine">the spineVector of the StrokeNode</param>
        /// <param name="segBegin">Start position of the line segment</param>
        /// <param name="segEnd">End position of the line segment</param>
        /// <returns>HitResult</returns>
        private static HitResult WhereIsNodeAboutSegment(Vector spine, Vector segBegin, Vector segEnd)
        {
            HitResult whereabout = HitResult.Right;
            Vector segVector = segEnd - segBegin;

            if ((WhereIsVectorAboutVector(-segBegin, segVector) == HitResult.Left)
                && !DoubleUtil.IsZero(Vector.Determinant(spine, segVector)))
            {
                whereabout = HitResult.Left;
            }
            return whereabout;
        }

        /// <summary>
        /// Helper method to calculate the exact location to cut
        /// </summary>
        /// <param name="spineVector">Vector the relative location of the two inking nodes</param>
        /// <param name="hitBegin">the begin point of the hitting segment</param>
        /// <param name="hitEnd">the end point of the hitting segment</param>
        /// <param name="endRadius">endNode radius</param>
        /// <param name="beginRadius">beginNode radius</param>
        /// <param name="result">StrokeFIndices representing the location for cutting</param>
        private void CalculateCutLocations(
            Vector spineVector, Vector hitBegin, Vector hitEnd, double endRadius, double beginRadius, ref StrokeFIndices result)
        {
            // Find out whether the {hitBegin, hitEnd} segment intersects with the contour
            // of the stroke segment, and find the lower index of the fragment to cut out.
            if (!DoubleUtil.AreClose(result.EndFIndex, StrokeFIndices.AfterLast))
            {
                if (WhereIsNodeAboutSegment(spineVector, hitBegin, hitEnd) == HitResult.Left)
                {
                    double findex = 1 - ClipTest(spineVector, endRadius, beginRadius, hitBegin, hitEnd);
                    if (findex > result.EndFIndex)
                    {
                        result.EndFIndex = findex;
                    }
                }
            }

            // Find out whether the {hitBegin, hitEnd} segment intersects with the contour
            // of the stroke segment, and find the higher index of the fragment to cut out.
            if (!DoubleUtil.AreClose(result.BeginFIndex, StrokeFIndices.BeforeFirst))
            {
                hitBegin -= spineVector;
                hitEnd -= spineVector;
                if (WhereIsNodeAboutSegment(-spineVector, hitBegin, hitEnd) == HitResult.Left)
                {
                    double findex = ClipTest(-spineVector, beginRadius, endRadius, hitBegin, hitEnd);
                    if (findex < result.BeginFIndex)
                    {
                        result.BeginFIndex = findex;
                    }
                }
            }
        }

        private double _radius = 0;
        private Size   _radii;
        private Matrix _transform;
        private Matrix _nodeShapeToCircle;
        private Matrix _circleToNodeShape;
    }
}
