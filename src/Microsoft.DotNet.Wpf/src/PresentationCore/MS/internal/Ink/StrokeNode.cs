// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//#define DEBUG_RENDERING_FEEDBACK

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Input;
using System.Diagnostics;

namespace MS.Internal.Ink
{
    #region StrokeNode

    /// <summary>
    /// StrokeNode represents a single segment on a stroke spine.
    /// It's used in enumerating through basic geometries making a stroke contour.
    /// </summary>
    internal struct StrokeNode
    {
        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="operations">StrokeNodeOperations object created for particular rendering</param>
        /// <param name="index">Index of the node on the stroke spine</param>
        /// <param name="nodeData">StrokeNodeData for this node</param>
        /// <param name="lastNodeData">StrokeNodeData for the precedeng node</param>
        /// <param name="isLastNode">Whether the current node is the last node</param>
        internal StrokeNode(
            StrokeNodeOperations operations,
            int index,
            in StrokeNodeData nodeData,
            in StrokeNodeData lastNodeData,
            bool isLastNode)
        {
            System.Diagnostics.Debug.Assert(operations != null);
            System.Diagnostics.Debug.Assert((nodeData.IsEmpty == false) && (index >= 0));
          

            _operations = operations;
            _index = index;
            _thisNode = nodeData;
            _lastNode = lastNodeData;
            _isQuadCached = lastNodeData.IsEmpty;
            _connectingQuad = Quad.Empty;
            _isLastNode = isLastNode;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Position of the node on the stroke spine.
        /// </summary>
        /// <value></value>
        internal Point Position { get { return _thisNode.Position; } }

        /// <summary>
        /// Position of the previous StrokeNode
        /// </summary>
        /// <value></value>
        internal Point PreviousPosition { get { return _lastNode.Position; } }

        /// <summary>
        /// PressureFactor of the node on the stroke spine.
        /// </summary>
        /// <value></value>
        internal float PressureFactor { get { return _thisNode.PressureFactor; } }

        /// <summary>
        /// PressureFactor of the previous StrokeNode
        /// </summary>
        /// <value></value>
        internal float PreviousPressureFactor { get { return _lastNode.PressureFactor; } }

        /// <summary>
        /// Tells whether the node shape (the stylus shape used in the rendering)
        /// is elliptical or polygonal. If the shape is an ellipse, GetContourPoints
        /// returns the control points for the quadratic Bezier that defines the ellipse.
        /// </summary>
        /// <value>true if the shape is ellipse, false otherwise</value>
        internal bool IsEllipse { get { return IsValid && _operations.IsNodeShapeEllipse; } }

        /// <summary>
        /// Returns true if this is the last node in the enumerator
        /// </summary>
        internal bool IsLastNode { get { return _isLastNode; } }

        /// <summary>
        /// Returns the bounds of the node shape w/o connecting quadrangle
        /// </summary>
        /// <returns></returns>
        internal Rect GetBounds()
        {
            return IsValid ? _operations.GetNodeBounds(_thisNode) : Rect.Empty;
        }

        /// <summary>
        /// Returns the bounds of the node shape and connecting quadrangle
        /// </summary>
        /// <returns></returns>
        internal Rect GetBoundsConnected()
        {
            return IsValid ? Rect.Union(_operations.GetNodeBounds(_thisNode), ConnectingQuad.Bounds) : Rect.Empty;
        }

        /// <summary>
        /// Returns the points that make up the stroke node shape (minus the connecting quad)
        /// </summary>
        internal void GetContourPoints(List<Point> pointBuffer)
        {
            if (IsValid)
            {
                _operations.GetNodeContourPoints(_thisNode, pointBuffer);
            }
        }

        /// <summary>
        /// Returns the points that make up the stroke node shape (minus the connecting quad)
        /// </summary>
        internal void GetPreviousContourPoints(List<Point> pointBuffer)
        {
            if (IsValid)
            {
                _operations.GetNodeContourPoints(_lastNode, pointBuffer);
            }
        }

        /// <summary>
        /// Returns the connecting quad
        /// </summary>
        internal Quad GetConnectingQuad()
        {
            if (IsValid)
            {
                return ConnectingQuad;
            }
            return Quad.Empty;
        }

        ///// <summary>
        ///// IsPointWithinRectOrEllipse
        ///// </summary>
        //internal bool IsPointWithinRectOrEllipse(Point point, double xRadiusOrHalfWidth, double yRadiusOrHalfHeight, Point center, bool isEllipse)
        //{
        //    if (isEllipse)
        //    {
        //        //determine what delta is required to move the rect to be 
        //        //centered at 0,0
        //        double xDelta = center.X + xRadiusOrHalfWidth;
        //        double yDelta = center.Y + yRadiusOrHalfHeight;

        //        //offset the point by that delta
        //        point.X -= xDelta;
        //        point.Y -= yDelta;

        //        //formula for ellipse is x^2/a^2 + y^2/b^2 = 1
        //        double a = xRadiusOrHalfWidth;
        //        double b = yRadiusOrHalfHeight;
        //        double res = (((point.X * point.X) / (a * a)) +
        //                     ((point.Y * point.Y) / (b * b)));

        //        if (res <= 1)
        //        {
        //            return true;
        //        }
        //        return false;
        //    }
        //    else
        //    {
        //        if (point.X >= (center.X - xRadiusOrHalfWidth) &&
        //            point.X <= (center.X + xRadiusOrHalfWidth) &&
        //            point.Y >= (center.Y - yRadiusOrHalfHeight) &&
        //            point.Y <= (center.Y + yRadiusOrHalfHeight))
        //        {
        //            return true;
        //        }
        //        return false;
        //    }
        //}

        /// <summary>
        /// GetPointsAtStartOfSegment
        /// </summary>
        internal void GetPointsAtStartOfSegment(List<Point> abPoints, 
                                                List<Point> dcPoints
#if DEBUG_RENDERING_FEEDBACK
                                                , DrawingContext debugDC, double feedbackSize, bool showFeedback
#endif
                                                )
        {
            if (IsValid)
            {
                Quad quad = ConnectingQuad;
                if (IsEllipse)
                {
                    Rect startNodeBounds = _operations.GetNodeBounds(_lastNode);

                    //add instructions to arc from D to A
                    abPoints.Add(quad.D);
                    abPoints.Add(StrokeRenderer.ArcToMarker);
                    abPoints.Add(new Point(startNodeBounds.Width, startNodeBounds.Height));
                    abPoints.Add(quad.A);

                    //simply start at D
                    dcPoints.Add(quad.D);

#if DEBUG_RENDERING_FEEDBACK
                    if (showFeedback)
                    {
                        debugDC.DrawEllipse(null, new Pen(Brushes.Pink, feedbackSize / 2), _lastNode.Position, startNodeBounds.Width / 2, startNodeBounds.Height / 2);
                        debugDC.DrawEllipse(Brushes.Red, null, quad.A, feedbackSize, feedbackSize);
                        debugDC.DrawEllipse(Brushes.Blue, null, quad.D, feedbackSize, feedbackSize);
                    }
#endif
                }
                else
                {
                    //we're interested in the A, D points as well as the 
                    //nodecountour points between them
                    Rect endNodeRect = _operations.GetNodeBounds(_thisNode);

#if DEBUG_RENDERING_FEEDBACK
                    if (showFeedback)
                    {
                        debugDC.DrawRectangle(null, new Pen(Brushes.Pink, feedbackSize / 2), _operations.GetNodeBounds(_lastNode));
                    }
#endif
                    Vector[] vertices = _operations.GetVertices();
                    double pressureFactor = _lastNode.PressureFactor;
                    int maxCount = vertices.Length * 2;
                    int i = 0;
                    bool dIsInEndNode = true;
                    for (; i < maxCount; i++)
                    {
                        //look for the d point first
                        Point point = _lastNode.Position + (vertices[i % vertices.Length] * pressureFactor);
                        if (point == quad.D)
                        {
                            //ab always starts with the D position (only add if it's not in endNode's bounds)
                            if (!endNodeRect.Contains(quad.D))
                            {
                                dIsInEndNode = false;
                                abPoints.Add(quad.D);
                                dcPoints.Add(quad.D);
                            }
#if DEBUG_RENDERING_FEEDBACK
                            if (showFeedback)
                            {
                                debugDC.DrawEllipse(Brushes.Blue, null, quad.D, feedbackSize, feedbackSize);
                            }
#endif
                            break;
                        }
                    }

                    if (i == maxCount)
                    {
                        Debug.Assert(false, "StrokeNodeOperations.GetPointsAtStartOfSegment failed to find the D position");
                        //we didn't find the d point, return
                        return;
                    }


                    //now look for the A position
                    //advance i
                    i++;
                    for (int j = 0; i < maxCount && j < vertices.Length; i++, j++)
                    {
                        //look for the A point now
                        Point point = _lastNode.Position + (vertices[i % vertices.Length] * pressureFactor);
                        //add everything in between to ab as long as it's not already in endNode's bounds
                        if (!endNodeRect.Contains(point))
                        {
                            abPoints.Add(point);
#if DEBUG_RENDERING_FEEDBACK
                        if (showFeedback)
                        {
                            debugDC.DrawEllipse(Brushes.Wheat, null, point, feedbackSize, feedbackSize);
                        }
#endif
                        }
                        if (dIsInEndNode)
                        {
                            Debug.Assert(!endNodeRect.Contains(point));

                            //add the first point after d, clockwise
                            dIsInEndNode = false;
                            dcPoints.Add(point);
                        }
                        if (point == quad.A)
                        {
#if DEBUG_RENDERING_FEEDBACK
                            if (showFeedback)
                            {
                                debugDC.DrawEllipse(Brushes.Red, null, point, feedbackSize, feedbackSize);
                            }
#endif
                            break;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// GetPointsAtEndOfSegment
        /// </summary>
        internal void GetPointsAtEndOfSegment(  List<Point> abPoints, 
                                                List<Point> dcPoints
#if DEBUG_RENDERING_FEEDBACK
                                                , DrawingContext debugDC, double feedbackSize, bool showFeedback
#endif
                                                )
        {
            if (IsValid)
            {
                Quad quad = ConnectingQuad;
                if (IsEllipse)
                {
                    Rect bounds = GetBounds();
                    //add instructions to arc from D to A
                    abPoints.Add(quad.B);
                    abPoints.Add(StrokeRenderer.ArcToMarker);
                    abPoints.Add(new Point(bounds.Width, bounds.Height));
                    abPoints.Add(quad.C);

                    //don't add to the dc points
#if DEBUG_RENDERING_FEEDBACK
                    if (showFeedback)
                    {
                        debugDC.DrawEllipse(null, new Pen(Brushes.Pink, feedbackSize / 2), _thisNode.Position, bounds.Width / 2, bounds.Height / 2);
                        debugDC.DrawEllipse(Brushes.Green, null, quad.B, feedbackSize, feedbackSize);
                        debugDC.DrawEllipse(Brushes.Yellow, null, quad.C, feedbackSize, feedbackSize);
                    }
#endif
                }
                else
                {
#if DEBUG_RENDERING_FEEDBACK
                    if (showFeedback)
                    {
                        debugDC.DrawRectangle(null, new Pen(Brushes.Pink, feedbackSize / 2), GetBounds());
                    }
#endif
                    //we're interested in the B, C points as well as the 
                    //nodecountour points between them
                    double pressureFactor = _thisNode.PressureFactor;
                    Vector[] vertices = _operations.GetVertices();
                    int maxCount = vertices.Length * 2;
                    int i = 0;
                    for (; i < maxCount; i++)
                    {
                        //look for the d point first
                        Point point = _thisNode.Position + (vertices[i % vertices.Length] * pressureFactor);
                        if (point == quad.B)
                        {
                            abPoints.Add(quad.B);
#if DEBUG_RENDERING_FEEDBACK
                            if (showFeedback)
                            {
                                debugDC.DrawEllipse(Brushes.Green, null, point, feedbackSize, feedbackSize);
                            }
#endif
                            break;
                        }
                    }

                    if (i == maxCount)
                    {
                        Debug.Assert(false, "StrokeNodeOperations.GetPointsAtEndOfSegment failed to find the B position");
                        //we didn't find the d point, return
                        return;
                    }

                    //now look for the C position
                    //advance i
                    i++;
                    for (int j = 0; i < maxCount && j < vertices.Length; i++, j++)
                    {
                        //look for the c point last
                        Point point = _thisNode.Position + (vertices[i % vertices.Length] * pressureFactor);
                        if (point == quad.C)
                        {
                            break;
                        }
                        //only add to ab if we didn't find C
                        abPoints.Add(point);

#if DEBUG_RENDERING_FEEDBACK
                        if (showFeedback)
                        {
                            debugDC.DrawEllipse(Brushes.Wheat, null, quad.C, feedbackSize, feedbackSize);
                        }
#endif
                    }
                    //finally, add the D point
                    dcPoints.Add(quad.C);

#if DEBUG_RENDERING_FEEDBACK
                    if (showFeedback)
                    {
                        debugDC.DrawEllipse(Brushes.Yellow, null, quad.C, feedbackSize, feedbackSize);
                    }
#endif
                }
            }
        }

        /// <summary>
        /// GetPointsAtMiddleSegment
        /// </summary>
        internal void GetPointsAtMiddleSegment( StrokeNode previous, 
                                                double angleBetweenNodes, 
                                                List<Point> abPoints, 
                                                List<Point> dcPoints,
                                                out bool missingIntersection
#if DEBUG_RENDERING_FEEDBACK
                                                , DrawingContext debugDC, double feedbackSize, bool showFeedback
#endif
                                                )
        {
            missingIntersection = false;
            if (IsValid && previous.IsValid)
            {
                Quad quad1 = previous.ConnectingQuad;
                if (!quad1.IsEmpty)
                {
                    Quad quad2 = ConnectingQuad;
                    if (!quad2.IsEmpty)
                    {
                        if (IsEllipse)
                        {
                            Rect node1Bounds = _operations.GetNodeBounds(previous._lastNode);
                            Rect node2Bounds = _operations.GetNodeBounds(_lastNode);
                            Rect node3Bounds = _operations.GetNodeBounds(_thisNode);
#if DEBUG_RENDERING_FEEDBACK
                            if (showFeedback)
                            {
                                debugDC.DrawEllipse(null, new Pen(Brushes.Pink, feedbackSize / 2), _lastNode.Position, node2Bounds.Width / 2, node2Bounds.Height / 2);
                            }
#endif
                            if (angleBetweenNodes == 0.0d || ((quad1.B == quad2.A) && (quad1.C == quad2.D)))
                            {
                                //quads connections are the same, just add them
                                abPoints.Add(quad1.B);
                                dcPoints.Add(quad1.C);
#if DEBUG_RENDERING_FEEDBACK
                                if (showFeedback)
                                {
                                    debugDC.DrawEllipse(Brushes.Green, null, quad1.B, feedbackSize, feedbackSize);
                                    debugDC.DrawEllipse(Brushes.Yellow, null, quad1.C, feedbackSize, feedbackSize);
                                }
#endif
                            }
                            else if (angleBetweenNodes > 0.0)
                            {
                                //the stroke angled towards the AB side
                                //this part is easy
                                if (quad1.B == quad2.A)
                                {
                                    abPoints.Add(quad1.B);
#if DEBUG_RENDERING_FEEDBACK
                                    if (showFeedback)
                                    {
                                        debugDC.DrawEllipse(Brushes.Green, null, quad1.B, feedbackSize, feedbackSize);
                                    }
#endif
                                }
                                else
                                {
                                    Point intersection = GetIntersection(quad1.A, quad1.B, quad2.A, quad2.B);
                                    Rect union = Rect.Union(node1Bounds, node2Bounds);
                                    union.Inflate(1.0, 1.0);
                                    //make sure we're not off in space

#if DEBUG_RENDERING_FEEDBACK
                                    if (showFeedback)
                                    {
                                        debugDC.DrawEllipse(Brushes.Green, null, quad1.B, feedbackSize * 1.5, feedbackSize * 1.5);
                                        debugDC.DrawEllipse(Brushes.Red, null, quad2.A, feedbackSize, feedbackSize);
                                    }
#endif

                                    if (union.Contains(intersection))
                                    {
                                        abPoints.Add(intersection);
#if DEBUG_RENDERING_FEEDBACK
                                        if (showFeedback)
                                        {
                                            debugDC.DrawEllipse(Brushes.Orange, null, intersection, feedbackSize, feedbackSize);
                                        }
#endif
                                    }
                                    else
                                    {
                                        //if we missed the intersection we'll need to close the stroke segment
                                        //this work is done in StrokeRenderer
                                        missingIntersection = true;
                                        return; //we're done.
                                    }
                                }

                                if (quad1.C == quad2.D)
                                {
                                    dcPoints.Add(quad1.C);
#if DEBUG_RENDERING_FEEDBACK
                                    if (showFeedback)
                                    {
                                        debugDC.DrawEllipse(Brushes.Yellow, null, quad1.C, feedbackSize, feedbackSize);
                                    }
#endif
                                }
                                else
                                {
                                    //add instructions to arc from quad1.C to quad2.D in reverse order (since we walk this array backwards to render)
                                    dcPoints.Add(quad1.C);
                                    dcPoints.Add(new Point(node2Bounds.Width, node2Bounds.Height));
                                    dcPoints.Add(StrokeRenderer.ArcToMarker);
                                    dcPoints.Add(quad2.D);
#if DEBUG_RENDERING_FEEDBACK
                                    if (showFeedback)
                                    {
                                        debugDC.DrawEllipse(Brushes.Yellow, null, quad1.C, feedbackSize, feedbackSize);
                                        debugDC.DrawEllipse(Brushes.Blue, null, quad2.D, feedbackSize, feedbackSize);
                                    }
#endif
                                }
                            }
                            else
                            {
                                //the stroke angled towards the CD side
                                //this part is easy
                                if (quad1.C == quad2.D)
                                {
                                    dcPoints.Add(quad1.C);
#if DEBUG_RENDERING_FEEDBACK
                                    if (showFeedback)
                                    {
                                        debugDC.DrawEllipse(Brushes.Yellow, null, quad1.C, feedbackSize, feedbackSize);
                                    }
#endif
                                }
                                else
                                {
                                    Point intersection = GetIntersection(quad1.D, quad1.C, quad2.D, quad2.C);
                                    Rect union = Rect.Union(node1Bounds, node2Bounds);
                                    union.Inflate(1.0, 1.0);
                                    //make sure we're not off in space

#if DEBUG_RENDERING_FEEDBACK
                                    if (showFeedback)
                                    {
                                        debugDC.DrawEllipse(Brushes.Yellow, null, quad1.C, feedbackSize * 1.5, feedbackSize * 1.5);
                                        debugDC.DrawEllipse(Brushes.Blue, null, quad2.D, feedbackSize, feedbackSize);
                                    }
#endif

                                    if (union.Contains(intersection))
                                    {
                                        dcPoints.Add(intersection);
#if DEBUG_RENDERING_FEEDBACK
                                        if (showFeedback)
                                        {
                                            debugDC.DrawEllipse(Brushes.Orange, null, intersection, feedbackSize, feedbackSize);
                                        }
#endif
                                    }
                                    else
                                    {
                                        //if we missed the intersection we'll need to close the stroke segment
                                        //this work is done in StrokeRenderer
                                        missingIntersection = true;
                                        return; //we're done.
                                    }
                                }

                                if (quad1.B == quad2.A)
                                {
                                    abPoints.Add(quad1.B);
#if DEBUG_RENDERING_FEEDBACK
                                    if (showFeedback)
                                    {
                                        debugDC.DrawEllipse(Brushes.Green, null, quad1.B, feedbackSize, feedbackSize);
                                    }
#endif

                                }
                                else
                                {
                                    //we need to arc between quad1.B and quad2.A along node2
                                    abPoints.Add(quad1.B);
                                    abPoints.Add(StrokeRenderer.ArcToMarker);
                                    abPoints.Add(new Point(node2Bounds.Width, node2Bounds.Height));
                                    abPoints.Add(quad2.A);
#if DEBUG_RENDERING_FEEDBACK
                                    if (showFeedback)
                                    {
                                        debugDC.DrawEllipse(Brushes.Green, null, quad1.B, feedbackSize, feedbackSize);
                                        debugDC.DrawEllipse(Brushes.Red, null, quad2.A, feedbackSize, feedbackSize);
                                    }
#endif
                                }
                            }
                        }
                        else
                        {
                            //rectangle
                            int indexA = -1;
                            int indexB = -1;
                            int indexC = -1;
                            int indexD = -1;

                            Vector[] vertices = _operations.GetVertices();
                            double pressureFactor = _lastNode.PressureFactor;
                            for (int i = 0; i < vertices.Length; i++)
                            {
                                Point point = _lastNode.Position + (vertices[i % vertices.Length] * pressureFactor);
                                if (point == quad2.A)
                                {
                                    indexA = i;
                                }
                                if (point == quad1.B)
                                {
                                    indexB = i;
                                }
                                if (point == quad1.C)
                                {
                                    indexC = i;
                                }
                                if (point == quad2.D)
                                {
                                    indexD = i;
                                }
                            }

                            if (indexA == -1 || indexB == -1 || indexC == -1 || indexD == -1)
                            {
                                Debug.Assert(false, "Couldn't find all 4 indexes in StrokeNodeOperations.GetPointsAtMiddleSegment");
                                return;
                            }

#if DEBUG_RENDERING_FEEDBACK
                            if (showFeedback)
                            {

                                debugDC.DrawRectangle(null, new Pen(Brushes.Pink, feedbackSize / 2), _operations.GetNodeBounds(_lastNode));
                                debugDC.DrawEllipse(Brushes.Red, null, quad2.A, feedbackSize, feedbackSize);
                                debugDC.DrawEllipse(Brushes.Green, null, quad1.B, feedbackSize, feedbackSize);
                                debugDC.DrawEllipse(Brushes.Yellow, null, quad1.C, feedbackSize, feedbackSize);
                                debugDC.DrawEllipse(Brushes.Blue, null, quad2.D, feedbackSize, feedbackSize);
                            }
#endif

                            Rect node3Rect = _operations.GetNodeBounds(_thisNode);
                            //take care of a-b first
                            if (indexA == indexB)
                            {
                                //quad connection is the same, just add it
                                if (!node3Rect.Contains(quad1.B))
                                {
                                    abPoints.Add(quad1.B);
                                }
                            }
                            else if ((indexA == 0 && indexB == 3) || ((indexA != 3 || indexB != 0) && (indexA > indexB)))
                            {
                                if (!node3Rect.Contains(quad1.B))
                                {
                                    abPoints.Add(quad1.B);
                                }
                                if (!node3Rect.Contains(quad2.A))
                                {
                                    abPoints.Add(quad2.A);
                                }
                            }
                            else
                            {
                                Point intersection = GetIntersection(quad1.A, quad1.B, quad2.A, quad2.B);
                                Rect node12 = Rect.Union(_operations.GetNodeBounds(previous._lastNode), _operations.GetNodeBounds(_lastNode));
                                node12.Inflate(1.0, 1.0);
                                //make sure we're not off in space
                                if (node12.Contains(intersection))
                                {
                                    abPoints.Add(intersection);
#if DEBUG_RENDERING_FEEDBACK
                                    if (showFeedback)
                                    {
                                        debugDC.DrawEllipse(Brushes.Orange, null, intersection, feedbackSize, feedbackSize * 1.5);
                                    }
#endif
                                }
                                else
                                {
                                    //if we missed the intersection we'll need to close the stroke segment
                                    //this work is done in StrokeRenderer.
                                    missingIntersection = true;
                                    return; //we're done.
                                }
                            }
                            
                            // now take care of c-d.  
                            if (indexC == indexD)
                            {
                                //quad connection is the same, just add it
                                if (!node3Rect.Contains(quad1.C))
                                {
                                    dcPoints.Add(quad1.C);
                                }
                            }
                            else if ((indexC == 0 && indexD == 3) || ((indexC != 3 || indexD != 0) && (indexC > indexD)))
                            {
                                if (!node3Rect.Contains(quad1.C))
                                {
                                    dcPoints.Add(quad1.C);
                                }
                                if (!node3Rect.Contains(quad2.D))
                                {
                                    dcPoints.Add(quad2.D);
                                }
                            }
                            else
                            {
                                Point intersection = GetIntersection(quad1.D, quad1.C, quad2.D, quad2.C);
                                Rect node12 = Rect.Union(_operations.GetNodeBounds(previous._lastNode), _operations.GetNodeBounds(_lastNode));
                                node12.Inflate(1.0, 1.0);
                                //make sure we're not off in space
                                if (node12.Contains(intersection))
                                {
                                    dcPoints.Add(intersection);
#if DEBUG_RENDERING_FEEDBACK 
                                    if (showFeedback)
                                    {
                                        debugDC.DrawEllipse(Brushes.Orange, null, intersection, feedbackSize, feedbackSize * 1.5);
                                    }
#endif
                                }
                                else 
                                {
                                    //if we missed the intersection we'll need to close the stroke segment
                                    //this work is done in StrokeRenderer.
                                    missingIntersection = true;
                                    return; //we're done.
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the intersection between two lines.  This code assumes there is an intersection
        /// and should only be called if that assumption is valid
        /// </summary>
        /// <returns></returns>
        internal static Point GetIntersection(Point line1Start, Point line1End, Point line2Start, Point line2End)
        {
            double a1 = line1End.Y - line1Start.Y;
            double b1 = line1Start.X - line1End.X;
            double c1 = (line1End.X * line1Start.Y) - (line1Start.X * line1End.Y);
            double a2 = line2End.Y - line2Start.Y;
            double b2 = line2Start.X - line2End.X;
            double c2 = (line2End.X * line2Start.Y) - (line2Start.X * line2End.Y);

            double d = (a1 * b2) - (a2 * b1);
            if (d != 0.0)
            {
                double x = ((b1 * c2) - (b2 * c1)) / d;
                double y = ((a2 * c1) - (a1 * c2)) / d;

                //capture the min and max points
                double line1XMin, line1XMax, line1YMin, line1YMax, line2XMin, line2XMax, line2YMin, line2YMax;
                if (line1Start.X < line1End.X)
                {
                    line1XMin = Math.Floor(line1Start.X);
                    line1XMax = Math.Ceiling(line1End.X);
                }
                else
                {
                    line1XMin = Math.Floor(line1End.X);
                    line1XMax = Math.Ceiling(line1Start.X);
                }

                if (line2Start.X < line2End.X)
                {
                    line2XMin = Math.Floor(line2Start.X);
                    line2XMax = Math.Ceiling(line2End.X);
                }
                else
                {
                    line2XMin = Math.Floor(line2End.X);
                    line2XMax = Math.Ceiling(line2Start.X);
                }

                if (line1Start.Y < line1End.Y)
                {
                    line1YMin = Math.Floor(line1Start.Y);
                    line1YMax = Math.Ceiling(line1End.Y);
                }
                else
                {
                    line1YMin = Math.Floor(line1End.Y);
                    line1YMax = Math.Ceiling(line1Start.Y);
                }

                if (line2Start.Y < line2End.Y)
                {
                    line2YMin = Math.Floor(line2Start.Y);
                    line2YMax = Math.Ceiling(line2End.Y);
                }
                else
                {
                    line2YMin = Math.Floor(line2End.Y);
                    line2YMax = Math.Ceiling(line2Start.Y);
                }


                // now see if we have an intersection between the lines
                // and not just the projection of the lines
                if ((line1XMin <= x && x <= line1XMax) &&
                    (line1YMin <= y && y <= line1YMax) &&
                    (line2XMin <= x && x <= line2XMax) &&
                    (line2YMin <= y && y <= line2YMax))
                {
                    return new Point(x, y);
                }
            }

            if ((long)line1End.X == (long)line2Start.X &&
                (long)line1End.Y == (long)line2Start.Y)
            {
                return new Point(line1End.X, line1End.Y);
            }

            return new Point(Double.NaN, Double.NaN);
        }

        /// <summary>
        /// This method tells whether the contour of a given stroke node
        /// intersects with the contour of this node. The contours of both nodes
        /// include their connecting quadrangles.
        /// </summary>
        /// <param name="hitNode"></param>
        /// <returns></returns>
        internal bool HitTest(StrokeNode hitNode)
        {
            if (!IsValid || !hitNode.IsValid)
            {
                return false;
            }

            IEnumerable<ContourSegment> hittingContour = hitNode.GetContourSegments();

            return _operations.HitTest(_lastNode, _thisNode, ConnectingQuad, hittingContour);
        }

        /// <summary>
        /// Finds out if a given node intersects with this one,
        /// and returns findices of the intersection.
        /// </summary>
        /// <param name="hitNode"></param>
        /// <returns></returns>
        internal StrokeFIndices CutTest(StrokeNode hitNode)
        {
            if ((IsValid == false) || (hitNode.IsValid == false))
            {
                return StrokeFIndices.Empty;
            }

            IEnumerable<ContourSegment> hittingContour = hitNode.GetContourSegments();

            // If the node contours intersect, the result is a pair of findices
            // this segment should be cut at to let the hitNode's contour through it.
            StrokeFIndices cutAt = _operations.CutTest(_lastNode, _thisNode, ConnectingQuad, hittingContour);

            return (_index == 0) ? cutAt : BindFIndices(cutAt);
        }

        /// <summary>
        /// Finds out if a given linear segment intersects with the contour of this node
        /// (including connecting quadrangle), and returns findices of the intersection.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        internal StrokeFIndices CutTest(Point begin, Point end)
        {
            if (IsValid == false)
            {
                return StrokeFIndices.Empty;
            }

            // If the node contours intersect, the result is a pair of findices
            // this segment should be cut at to let the hitNode's contour through it.
            StrokeFIndices cutAt = _operations.CutTest(_lastNode, _thisNode, ConnectingQuad, begin, end);

            System.Diagnostics.Debug.Assert(!double.IsNaN(cutAt.BeginFIndex) && !double.IsNaN(cutAt.EndFIndex));

            // Bind the found findices to the node and return the result
            return BindFIndicesForLassoHitTest(cutAt);
        }

        #endregion

        #region Private helpers

        /// <summary>
        /// Binds a local fragment to this node by setting the integer part of the
        /// fragment findices equal to the index of the previous node
        /// </summary>
        /// <param name="fragment"></param>
        /// <returns></returns>
        private StrokeFIndices BindFIndices(StrokeFIndices fragment)
        {
            System.Diagnostics.Debug.Assert(IsValid && (_index >= 0));

            if (fragment.IsEmpty == false)
            {
                // Adjust only findices which are on this segment of thew spine (i.e. between 0 and 1)
                if (!DoubleUtil.AreClose(fragment.BeginFIndex, StrokeFIndices.BeforeFirst))
                {
                    System.Diagnostics.Debug.Assert(fragment.BeginFIndex >= 0 && fragment.BeginFIndex <= 1);
                    fragment.BeginFIndex += _index - 1;
                }
                if (!DoubleUtil.AreClose(fragment.EndFIndex, StrokeFIndices.AfterLast))
                {
                    System.Diagnostics.Debug.Assert(fragment.EndFIndex >= 0 && fragment.EndFIndex <= 1);
                    fragment.EndFIndex += _index - 1;
                }
            }
            return fragment;
        }

        internal int Index
        {
            get { return _index; }
        }

        /// <summary>
        /// Bind the StrokeFIndices for lasso hit test results.
        /// </summary>
        /// <param name="fragment"></param>
        /// <returns></returns>
        private StrokeFIndices BindFIndicesForLassoHitTest(StrokeFIndices fragment)
        {
            System.Diagnostics.Debug.Assert(IsValid);
            if (!fragment.IsEmpty)
            {
                // Adjust BeginFIndex
                if (DoubleUtil.AreClose(fragment.BeginFIndex, StrokeFIndices.BeforeFirst))
                {
                    // set it to be the index of the previous node, indicating intersection start from previous node
                     fragment.BeginFIndex = (_index == 0 ? StrokeFIndices.BeforeFirst:_index - 1);
                }
                else
                {
                    // Adjust findices which are on this segment of the spine (i.e. between 0 and 1)
                    System.Diagnostics.Debug.Assert(DoubleUtil.GreaterThanOrClose(fragment.BeginFIndex, 0f));
                    
                    System.Diagnostics.Debug.Assert(DoubleUtil.LessThanOrClose(fragment.BeginFIndex, 1f));

                    // Adjust the value to consider index, say from 0.75 to 3.75 (for _index = 4)
                    fragment.BeginFIndex += _index - 1;
                }

                //Adjust EndFIndex
                if (DoubleUtil.AreClose(fragment.EndFIndex, StrokeFIndices.AfterLast))
                {
                    // set it to be the index of the current node, indicating the intersection cover the end of the node
                    fragment.EndFIndex = (_isLastNode ? StrokeFIndices.AfterLast:_index);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(DoubleUtil.GreaterThanOrClose(fragment.EndFIndex, 0f));

                    System.Diagnostics.Debug.Assert(DoubleUtil.LessThanOrClose(fragment.EndFIndex, 1f));
                    // Ajust the value to consider the index
                    fragment.EndFIndex += _index - 1;
                }
            }
            return fragment;
        }

        /// <summary>
        /// Tells whether the StrokeNode instance is valid or not (created via the default ctor)
        /// </summary>
        internal bool IsValid { get { return _operations != null; } }

        /// <summary>
        /// The quadrangle that connects this and the previous node.
        /// Can be empty if this node is the first one or if one of the nodes is
        /// completely inside the other.
        /// The type Quad is supposed to be internal even if we surface StrokeNode.
        /// External users of StrokeNode should use GetConnectionPoints instead.
        /// </summary>
        private Quad ConnectingQuad
        {
            get
            {
                System.Diagnostics.Debug.Assert(IsValid);

                if (_isQuadCached == false)
                {
                    _connectingQuad = _operations.GetConnectingQuad(_lastNode, _thisNode);
                    _isQuadCached = true;
                }
                return _connectingQuad;
            }
        }

        /// <summary>
        /// Returns an enumerator for edges of the contour comprised by the node
        /// and connecting quadrangle (_lastNode is excluded)
        /// Used for hit-testing a stroke against an other stroke (stroke and point erasing)
        /// </summary>
        private IEnumerable<ContourSegment> GetContourSegments()
        {
            System.Diagnostics.Debug.Assert(IsValid);

            // Calls thru to the StrokeNodeOperations object
            if (IsEllipse)
            {
                // ISSUE-2004/06/15- temporary workaround to avoid hit-testing with ellipses
                return _operations.GetNonBezierContourSegments(_lastNode, _thisNode);
            }
            return  _operations.GetContourSegments(_thisNode, ConnectingQuad);
        }

        /// <summary>
        /// Returns the spine point that corresponds to the given findex.
        /// </summary>
        /// <param name="findex">A local findex between the previous index and this one (ex: between 2.0 and 3.0)</param>
        /// <returns>Point on the spine</returns>
        internal Point GetPointAt(double findex)
        {
            System.Diagnostics.Debug.Assert(IsValid);

            if (_lastNode.IsEmpty)
            {
                System.Diagnostics.Debug.Assert(findex == 0);
                return _thisNode.Position;
            }

            System.Diagnostics.Debug.Assert((findex >= _index - 1) && (findex <= _index));

            if (DoubleUtil.AreClose(findex, (double)_index))
            {
                //
                // we're being asked for this exact point
                // if we don't return it here, our algorithm
                // below doesn't work
                //
                return _thisNode.Position;
            }

            //
            // get the spare change to the left of the decimal point
            // eg turn 2.75 into .75
            //
            double floor = Math.Floor(findex);
            findex = findex - floor;

            double xDiff = (_thisNode.Position.X - _lastNode.Position.X) * findex;
            double yDiff = (_thisNode.Position.Y - _lastNode.Position.Y) * findex;

            //
            // return the previous point plus the delta's
            //
            return new Point(   _lastNode.Position.X + xDiff,
                                _lastNode.Position.Y + yDiff);
        }

        #endregion

        #region Fields

        // Internal objects created for particular rendering
        private StrokeNodeOperations    _operations;

        // Node's index on the stroke spine
        private int             _index;

        // This and the previous node data that used by the StrokeNodeOperations object to build
        // and/or hit-test the contour of the node/segment
        private StrokeNodeData  _thisNode;
        private StrokeNodeData  _lastNode;

        // Calculating of the connecting quadrangle is not a cheap operations, therefore,
        // first, it's computed only by request, and second, once computed it's cached in the StrokeNode
        private bool            _isQuadCached;
        private Quad            _connectingQuad;

        // Is the current stroke node the last node?
        private bool _isLastNode;

        #endregion
    }
    #endregion
}
