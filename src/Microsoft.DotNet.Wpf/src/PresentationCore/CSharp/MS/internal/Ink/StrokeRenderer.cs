// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//#define DEBUG_RENDERING_FEEDBACK

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Input;
using System.Diagnostics;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using MS.Internal;
using MS.Internal.PresentationCore;

namespace MS.Internal.Ink
{
    /// <summary>
    /// An internal utility class that knows how to render a stroke
    /// into an Avalon's DrawingContext.
    /// </summary>
    internal static class StrokeRenderer
    {
        #region Static API


        /// <summary>
        /// Calculate the StreamGeometry for the StrokeNodes.
        /// This method is one of our most sensitive perf paths.  It has been optimized to 
        /// create the minimum path figures in the StreamGeometry.  There are two structures
        /// we create for each point in a stroke, the strokenode and the connecting quad.  Adding
        /// strokenodes is very expensive later when MIL renders it, so this method has been optimized
        /// to only add strokenodes when either pressure changes, or the angle of the stroke changes.
        /// </summary>
        internal static void CalcGeometryAndBoundsWithTransform(StrokeNodeIterator iterator,
                                                               DrawingAttributes drawingAttributes,
                                                               MatrixTypes stylusTipMatrixType,
                                                               bool calculateBounds,
                                                               out Geometry geometry,
                                                               out Rect bounds)
        {
            Debug.Assert(iterator != null);
            Debug.Assert(drawingAttributes != null);

            StreamGeometry streamGeometry = new StreamGeometry();
            streamGeometry.FillRule = FillRule.Nonzero;

            StreamGeometryContext context = streamGeometry.Open();
            geometry = streamGeometry;
            bounds = Rect.Empty;
            try
            {
                List<Point> connectingQuadPoints = new List<Point>(iterator.Count * 4);

                //the index that the cb quad points are copied to
                int cdIndex = iterator.Count * 2;
                //the index that the ab quad points are copied to
                int abIndex = 0;
                for (int x = 0; x < cdIndex; x++)
                {
                    //initialize so we can start copying to cdIndex later
                    connectingQuadPoints.Add(new Point(0d, 0d));
                }

                List<Point> strokeNodePoints = new List<Point>();
                double lastAngle = 0.0d;
                bool previousPreviousNodeRendered = false;

                Rect lastRect = new Rect(0, 0, 0, 0);

                for (int index = 0; index < iterator.Count; index++)
                {
                    StrokeNode strokeNode = iterator[index];
                    System.Diagnostics.Debug.Assert(true == strokeNode.IsValid);

                    //the only code that calls this with !calculateBounds
                    //is dynamic rendering, which already draws enough strokeNodes
                    //to hide any visual artifacts.
                    //static rendering calculatesBounds, and we use those
                    //bounds below to figure out what angle to lay strokeNodes down for.
                    Rect strokeNodeBounds = strokeNode.GetBounds();
                    if (calculateBounds)
                    {
                        bounds.Union(strokeNodeBounds);
                    }

                    //if the angle between this and the last position has changed
                    //too much relative to the angle between the last+1 position and the last position
                    //we need to lay down stroke node
                    double delta = Math.Abs(GetAngleDeltaFromLast(strokeNode.PreviousPosition, strokeNode.Position, ref lastAngle));

                    double angleTolerance = 45d;
                    if (stylusTipMatrixType == MatrixTypes.TRANSFORM_IS_UNKNOWN)
                    {
                        //probably a skew is thrown in, we need to fall back to being very conservative 
                        //about how many strokeNodes we prune
                        angleTolerance = 10d;
                    }
                    else if (strokeNodeBounds.Height > 40d || strokeNodeBounds.Width > 40d)
                    {
                        //if the strokeNode gets above a certain size, we need to lay down more strokeNodes
                        //to prevent visual artifacts
                        angleTolerance = 20d;
                    }
                    bool directionChanged = delta > angleTolerance && delta < (360d - angleTolerance);

                    double prevArea = lastRect.Height * lastRect.Width;
                    double currArea = strokeNodeBounds.Height * strokeNodeBounds.Width;
                    bool areaChangedOverThreshold = false;
                    if ((Math.Min(prevArea, currArea) / Math.Max(prevArea, currArea)) <= 0.70d)
                    {
                        //the min area is < 70% of the max area
                        areaChangedOverThreshold = true;
                    }

                    lastRect = strokeNodeBounds;

                    //render the stroke node for the first two nodes and last two nodes always
                    if (index <= 1 || index >= iterator.Count - 2 || directionChanged || areaChangedOverThreshold)
                    {
                        //special case... the direction has changed and we need to 
                        //insert a stroke node in the StreamGeometry before we render the current one
                        if (directionChanged && !previousPreviousNodeRendered && index > 1 && index < iterator.Count - 1)
                        {
                            //insert a stroke node for the previous node
                            strokeNodePoints.Clear();
                            strokeNode.GetPreviousContourPoints(strokeNodePoints);
                            AddFigureToStreamGeometryContext(context, strokeNodePoints, strokeNode.IsEllipse/*isBezierFigure*/);

                            previousPreviousNodeRendered = true;
                        }

                        //render the stroke node
                        strokeNodePoints.Clear();
                        strokeNode.GetContourPoints(strokeNodePoints);
                        AddFigureToStreamGeometryContext(context, strokeNodePoints, strokeNode.IsEllipse/*isBezierFigure*/);
                    }

                    if (!directionChanged)
                    {
                        previousPreviousNodeRendered = false;
                    }

                    //add the end points of the connecting quad
                    Quad quad = strokeNode.GetConnectingQuad();
                    if (!quad.IsEmpty)
                    {
                        connectingQuadPoints[abIndex++] = quad.A;
                        connectingQuadPoints[abIndex++] = quad.B;
                        connectingQuadPoints.Add(quad.D);
                        connectingQuadPoints.Add(quad.C);
                    }

                    if (strokeNode.IsLastNode)
                    {
                        Debug.Assert(index == iterator.Count - 1);
                        if (abIndex > 0)
                        {
                            //we added something to the connecting quad points.
                            //now we need to do three things
                            //1) Shift the dc points down to the ab points
                            int cbStartIndex = iterator.Count * 2;
                            int cbEndIndex = connectingQuadPoints.Count - 1;
                            for (int i = abIndex, j = cbStartIndex; j <= cbEndIndex; i++, j++)
                            {
                                connectingQuadPoints[i] = connectingQuadPoints[j];
                            }

                            //2) trim the exess off the end of the array
                            int countToRemove = cbStartIndex - abIndex;
                            connectingQuadPoints.RemoveRange((cbEndIndex - countToRemove) + 1, countToRemove);

                            //3) reverse the dc points to make them cd points
                            for (int i = abIndex, j = connectingQuadPoints.Count - 1; i < j; i++, j--)
                            {
                                Point temp = connectingQuadPoints[i];
                                connectingQuadPoints[i] = connectingQuadPoints[j];
                                connectingQuadPoints[j] = temp;
                            }

                            //now render away!
                            AddFigureToStreamGeometryContext(context, connectingQuadPoints, false/*isBezierFigure*/);
                        }
                    }
                }
            }
            finally
            {
                context.Close();
                geometry.Freeze();
            }
        }


        /// <summary>
        /// Calculate the StreamGeometry for the StrokeNodes.
        /// This method is one of our most sensitive perf paths.  It has been optimized to 
        /// create the minimum path figures in the StreamGeometry.  There are two structures
        /// we create for each point in a stroke, the strokenode and the connecting quad.  Adding
        /// strokenodes is very expensive later when MIL renders it, so this method has been optimized
        /// to only add strokenodes when either pressure changes, or the angle of the stroke changes.
        /// </summary>
        [FriendAccessAllowed]
        internal static void CalcGeometryAndBounds(StrokeNodeIterator iterator,
                                                   DrawingAttributes drawingAttributes,
#if DEBUG_RENDERING_FEEDBACK
                                                   DrawingContext debugDC,
                                                   double feedbackSize,
                                                   bool showFeedback,
#endif
                                                   bool calculateBounds,
                                                   out Geometry geometry,
                                                   out Rect bounds)
        {
            Debug.Assert(iterator != null && drawingAttributes != null);

            //we can use our new algorithm for identity only.
            Matrix stylusTipTransform = drawingAttributes.StylusTipTransform;
            if (stylusTipTransform != Matrix.Identity && stylusTipTransform._type != MatrixTypes.TRANSFORM_IS_SCALING)
            {
                //second best optimization
                CalcGeometryAndBoundsWithTransform(iterator, drawingAttributes, stylusTipTransform._type, calculateBounds, out geometry, out bounds);
            }
            else
            {
                StreamGeometry streamGeometry = new StreamGeometry();
                streamGeometry.FillRule = FillRule.Nonzero;

                StreamGeometryContext context = streamGeometry.Open();
                geometry = streamGeometry;
                Rect empty = Rect.Empty;
                bounds = empty;
                try
                {
                    //
                    // We keep track of three StrokeNodes as we iterate across
                    // the Stroke. Since these are structs, the default ctor will
                    // be called and .IsValid will be false until we initialize them
                    //
                    StrokeNode emptyStrokeNode = new StrokeNode();
                    StrokeNode prevPrevStrokeNode = new StrokeNode();
                    StrokeNode prevStrokeNode = new StrokeNode();
                    StrokeNode strokeNode = new StrokeNode();

                    Rect prevPrevStrokeNodeBounds = empty;
                    Rect prevStrokeNodeBounds = empty;
                    Rect strokeNodeBounds = empty;

                    //percentIntersect is a function of drawingAttributes height / width
                    double percentIntersect = 95d;
                    double maxExtent = Math.Max(drawingAttributes.Height, drawingAttributes.Width);
                    percentIntersect += Math.Min(4.99999d, ((maxExtent / 20d) * 5d));

                    double prevAngle = double.MinValue;
                    bool isStartOfSegment = true;
                    bool isEllipse = drawingAttributes.StylusTip == StylusTip.Ellipse;
                    bool ignorePressure = drawingAttributes.IgnorePressure;
                    //
                    // Two List<Point>'s that get reused for adding figures
                    // to the streamgeometry.
                    //
                    List<Point> pathFigureABSide = new List<Point>();//don't prealloc.  It causes Gen2 collections to rise and doesn't help execution time
                    List<Point> pathFigureDCSide = new List<Point>();
                    List<Point> polyLinePoints =  new List<Point>(4);

                    int iteratorCount = iterator.Count;
                    for (int index = 0, previousIndex = -1; index < iteratorCount; )
                    {
                        if (!prevPrevStrokeNode.IsValid)
                        {
                            if (prevStrokeNode.IsValid)
                            {
                                //we're sliding our pointers forward
                                prevPrevStrokeNode = prevStrokeNode;
                                prevPrevStrokeNodeBounds = prevStrokeNodeBounds;
                                prevStrokeNode = emptyStrokeNode;
                            }
                            else
                            {
                                prevPrevStrokeNode = iterator[index++, previousIndex++];
                                prevPrevStrokeNodeBounds = prevPrevStrokeNode.GetBounds();
                                continue; //so we always check if index < iterator.Count
                            }
                        }

                        //we know prevPrevStrokeNode is valid
                        if (!prevStrokeNode.IsValid)
                        {
                            if (strokeNode.IsValid)
                            {
                                //we're sliding our pointers forward
                                prevStrokeNode = strokeNode;
                                prevStrokeNodeBounds = strokeNodeBounds;
                                strokeNode = emptyStrokeNode;
                            }
                            else
                            {
                                //get the next strokeNode, but don't automatically update previousIndex
                                prevStrokeNode = iterator[index++, previousIndex];
                                prevStrokeNodeBounds = prevStrokeNode.GetBounds();

                                RectCompareResult result = 
                                    FuzzyContains(  prevStrokeNodeBounds, 
                                                    prevPrevStrokeNodeBounds,
                                                    isStartOfSegment ? 99.99999d : percentIntersect);

                                if (result == RectCompareResult.Rect1ContainsRect2)
                                {
                                    // this node already contains the prevPrevStrokeNodeBounds (PP):
                                    //
                                    //  |------------|
                                    //  | |----|     |
                                    //  | | PP |  P  |                            
                                    //  | |----|     |
                                    //  |------------|
                                    //
                                    prevPrevStrokeNode = iterator[index - 1, prevPrevStrokeNode.Index - 1]; ;
                                    prevPrevStrokeNodeBounds = Rect.Union(prevStrokeNodeBounds, prevPrevStrokeNodeBounds);

                                    // at this point prevPrevStrokeNodeBounds already contains this node
                                    // we can just ignore this node
                                    prevStrokeNode = emptyStrokeNode;

                                    // update previousIndex to point to this node
                                    previousIndex = index - 1;

                                    // go back to our main loop
                                    continue;
                                }
                                else if (result == RectCompareResult.Rect2ContainsRect1)
                                {
                                    // this prevPrevStrokeNodeBounds (PP) already contains this node:
                                    //
                                    //  |------------|
                                    //  |      |----||
                                    //  |  PP  | P  ||                            
                                    //  |      |----||
                                    //  |------------|
                                    //

                                    //prevPrevStrokeNodeBounds already contains this node
                                    //we can just ignore this node
                                    prevStrokeNode = emptyStrokeNode;

                                    // go back to our main loop, but do not update previousIndex
                                    // because it should continue to point to previousPrevious
                                    continue;
                                }

                                Debug.Assert(!prevStrokeNode.GetConnectingQuad().IsEmpty, "prevStrokeNode.GetConnectingQuad() is Empty!");
                                
                                // if neither was true, we now have two of our three nodes required to 
                                // start our computation, we need to update previousIndex to point
                                // to our current, valid prevStrokeNode
                                previousIndex = index - 1;
                                continue; //so we always check if index < iterator.Count
                            }
                        }

                        //we know prevPrevStrokeNode and prevStrokeNode are both valid 
                        if (!strokeNode.IsValid)
                        {
                            strokeNode = iterator[index++, previousIndex];
                            strokeNodeBounds = strokeNode.GetBounds();

                            RectCompareResult result =
                                    FuzzyContains(  strokeNodeBounds,
                                                    prevStrokeNodeBounds,
                                                    isStartOfSegment ? 99.99999 : percentIntersect);

                            RectCompareResult result2 =
                                    FuzzyContains(  strokeNodeBounds,
                                                    prevPrevStrokeNodeBounds,
                                                    isStartOfSegment ? 99.99999 : percentIntersect);

                            if ( isStartOfSegment &&
                                 result == RectCompareResult.Rect1ContainsRect2 &&
                                 result2 == RectCompareResult.Rect1ContainsRect2)
                            {
                                if (pathFigureABSide.Count > 0)
                                {
                                    //we've started a stroke, we need to end it before resetting
                                    //prevPrev
#if DEBUG_RENDERING_FEEDBACK
                                    prevStrokeNode.GetPointsAtEndOfSegment(pathFigureABSide, pathFigureDCSide, debugDC, feedbackSize, showFeedback);
#else
                                    prevStrokeNode.GetPointsAtEndOfSegment(pathFigureABSide, pathFigureDCSide);
#endif
                                    //render
                                    ReverseDCPointsRenderAndClear(context, pathFigureABSide, pathFigureDCSide, polyLinePoints, isEllipse, true/*clear the point collections*/);
                                }
                                //we're resetting
                                //prevPrevStrokeNode.  We need to gen one
                                //without a connecting quad
                                prevPrevStrokeNode = iterator[index - 1, prevPrevStrokeNode.Index - 1];
                                prevPrevStrokeNodeBounds = prevPrevStrokeNode.GetBounds();
                                prevStrokeNode = emptyStrokeNode;
                                strokeNode = emptyStrokeNode;

                                // increment previousIndex to to point to this node
                                previousIndex = index - 1;
                                continue;
}
                            else if (result == RectCompareResult.Rect1ContainsRect2)
                            {
                                // this node (C) already contains the prevStrokeNodeBounds (P):
                                //
                                //          |------------|
                                //  |----|  | |----|     |
                                //  | PP |  | | P  |  C  |                            
                                //  |----|  | |----|     |
                                //          |------------|
                                //
                                //we have to generate a new stroke node that points
                                //to pp since the connecting quad from C to P could be empty
                                //if they have the same point
                                strokeNode = iterator[index - 1, prevStrokeNode.Index - 1];
                                if (!strokeNode.GetConnectingQuad().IsEmpty)
                                {
                                    //only update prevStrokeNode if we have a valid connecting quad
                                    prevStrokeNode = strokeNode;
                                    prevStrokeNodeBounds = Rect.Union(strokeNodeBounds, prevStrokeNodeBounds);

                                    // update previousIndex, since it should point to this node now
                                    previousIndex = index - 1;
                                }

                                // at this point we can just ignore this node
                                strokeNode = emptyStrokeNode;
                                //strokeNodeBounds = empty;

                                prevAngle = double.MinValue; //invalidate
                                
                                // go back to our main loop
                                continue;
                            }
                            else if (result == RectCompareResult.Rect2ContainsRect1)
                            {
                                // this prevStrokeNodeBounds (P) already contains this node (C):
                                //
                                //          |------------|
                                // |----|   |      |----||
                                // | PP |   |  P   | C  ||                            
                                // |----|   |      |----||
                                //          |------------|
                                //
                                //prevStrokeNodeBounds already contains this node
                                //we can just ignore this node
                                strokeNode = emptyStrokeNode;

                                // go back to our main loop, but do not update previousIndex
                                // because it should continue to point to previous
                                continue;
                            }

                            Debug.Assert(!strokeNode.GetConnectingQuad().IsEmpty, "strokeNode.GetConnectingQuad was empty, this is unexpected");

                            //
                            // NOTE: we do not check if C contains PP, or PP contains C because
                            // that indicates a change in direction, which we handle below
                            //
                            // if neither was true P and C are separate, 
                            // we now have all three nodes required to 
                            // start our computation, we need to update previousIndex to point
                            // to our current, valid prevStrokeNode
                            previousIndex = index - 1;
                        }


                        // see if we have an overlap between the first and third node
                        bool overlap = prevPrevStrokeNodeBounds.IntersectsWith(strokeNodeBounds);

                        // prevPrevStrokeNode, prevStrokeNode and strokeNode are all 
                        // valid nodes now.  Now we need to figure out what do add to our 
                        // PathFigure.  First calc bounds on the strokeNode we know we need to render
                        if (calculateBounds)
                        {
                            bounds.Union(prevStrokeNodeBounds);
                        }

                        // determine what points to add to pathFigureABSide and pathFigureDCSide
                        // from prevPrevStrokeNode
                        if (pathFigureABSide.Count == 0)
                        {
                            Debug.Assert(pathFigureDCSide.Count == 0);
                            if (calculateBounds)
                            {
                                bounds.Union(prevPrevStrokeNodeBounds);
                            }

                            if (isStartOfSegment && overlap)
                            {
                                //render a complete first stroke node or we can get artifacts
                                prevPrevStrokeNode.GetContourPoints(polyLinePoints);
                                AddFigureToStreamGeometryContext(context, polyLinePoints, prevPrevStrokeNode.IsEllipse/*isBezierFigure*/);
                                polyLinePoints.Clear();
                            }

                            // we're starting a new pathfigure
                            // we need to add parts of the prevPrevStrokeNode contour
                            // to pathFigureABSide and pathFigureDCSide
#if DEBUG_RENDERING_FEEDBACK
                            prevStrokeNode.GetPointsAtStartOfSegment(pathFigureABSide, pathFigureDCSide, debugDC, feedbackSize, showFeedback);
#else
                            prevStrokeNode.GetPointsAtStartOfSegment(pathFigureABSide, pathFigureDCSide);
#endif

                            //set our marker, we're no longer at the start of the stroke
                            isStartOfSegment = false;
                        }

                        

                        if (prevAngle == double.MinValue)
                        {
                            //prevAngle is no longer valid
                            prevAngle = GetAngleBetween(prevPrevStrokeNode.Position, prevStrokeNode.Position);
                        }
                        double delta = GetAngleDeltaFromLast(prevStrokeNode.Position, strokeNode.Position, ref prevAngle);
                        bool directionChangedOverAbsoluteThreshold = Math.Abs(delta) > 90d && Math.Abs(delta) < (360d - 90d);
                        bool directionChangedOverOverlapThreshold = overlap && !(ignorePressure || strokeNode.PressureFactor == 1f) && Math.Abs(delta) > 30d && Math.Abs(delta) < (360d - 30d);

                        double prevArea = prevStrokeNodeBounds.Height * prevStrokeNodeBounds.Width;
                        double currArea = strokeNodeBounds.Height * strokeNodeBounds.Width;

                        bool areaChanged = !(prevArea == currArea && prevArea == (prevPrevStrokeNodeBounds.Height * prevPrevStrokeNodeBounds.Width));
                        bool areaChangeOverThreshold = false;
                        if (overlap && areaChanged)
                        {
                            if ((Math.Min(prevArea, currArea) / Math.Max(prevArea, currArea)) <= 0.90d)
                            {
                                //the min area is < 70% of the max area
                                areaChangeOverThreshold = true;
                            }
                        }

                        if (areaChanged || delta != 0.0d || index >= iteratorCount)
                        {
                            //the area changed between the three nodes OR there was an angle delta OR we're at the end 
                            //of the stroke...  either way, this is a significant node.  If not, we're going to drop it.
                            if ((overlap && (directionChangedOverOverlapThreshold || areaChangeOverThreshold)) ||
                                directionChangedOverAbsoluteThreshold)
                            {
                                //
                                // we need to stop the pathfigure at P
                                // and render the pathfigure
                                //
                                //  |--|      |--|    |--||--|   |------|
                                //  |PP|------|P |    |PP||P |   |PP P C| 
                                //  |--|      |--|    |--||--|   |------|
                                //           /           |C |          
                                //      |--|             |--|         
                                //      |C |               
                                //      |--|               


#if DEBUG_RENDERING_FEEDBACK
                                prevStrokeNode.GetPointsAtEndOfSegment(pathFigureABSide, pathFigureDCSide, debugDC, feedbackSize, showFeedback);
#else
                                //end the figure
                                prevStrokeNode.GetPointsAtEndOfSegment(pathFigureABSide, pathFigureDCSide);
#endif
                                //render
                                ReverseDCPointsRenderAndClear(context, pathFigureABSide, pathFigureDCSide, polyLinePoints, isEllipse, true/*clear the point collections*/);

                                if (areaChangeOverThreshold)
                                {
                                    //render a complete stroke node or we can get artifacts
                                    prevStrokeNode.GetContourPoints(polyLinePoints);
                                    AddFigureToStreamGeometryContext(context, polyLinePoints, prevStrokeNode.IsEllipse/*isBezierFigure*/);
                                    polyLinePoints.Clear();
                                }
                            }
                            else
                            {
                                //
                                // direction didn't change over the threshold, add the midpoint data
                                //  |--|      |--|
                                //  |PP|------|P | 
                                //  |--|      |--|
                                //                \
                                //                  |--| 
                                //                  |C | 
                                //                  |--| 
                                bool endSegment; //flag that tell us if we missed an intersection
#if DEBUG_RENDERING_FEEDBACK
                                strokeNode.GetPointsAtMiddleSegment(prevStrokeNode, delta, pathFigureABSide, pathFigureDCSide, out endSegment, debugDC, feedbackSize, showFeedback);
#else
                                strokeNode.GetPointsAtMiddleSegment(prevStrokeNode, delta, pathFigureABSide, pathFigureDCSide, out endSegment);
#endif
                                if (endSegment)
                                {
                                    //we have a missing intersection, we need to end the 
                                    //segment at P
#if DEBUG_RENDERING_FEEDBACK
                                    prevStrokeNode.GetPointsAtEndOfSegment(pathFigureABSide, pathFigureDCSide, debugDC, feedbackSize, showFeedback);
#else
                                    //end the figure
                                    prevStrokeNode.GetPointsAtEndOfSegment(pathFigureABSide, pathFigureDCSide);
#endif
                                    //render
                                    ReverseDCPointsRenderAndClear(context, pathFigureABSide, pathFigureDCSide, polyLinePoints, isEllipse, true/*clear the point collections*/);
                                }
                             }
                        }

                        //
                        // either way... slide our pointers forward, to do this, we simply mark 
                        // our first pointer as 'empty'
                        //
                        prevPrevStrokeNode = emptyStrokeNode;
                        prevPrevStrokeNodeBounds = empty;
}

                    //
                    // anything left to render?
                    //
                    if (prevPrevStrokeNode.IsValid)
                    {
                        if (prevStrokeNode.IsValid)
                        {
                            if (calculateBounds)
                            {
                                bounds.Union(prevPrevStrokeNodeBounds);
                                bounds.Union(prevStrokeNodeBounds);
                            }
                            Debug.Assert(!strokeNode.IsValid);
                            //
                            // we never made it to strokeNode, render two points, OR 
                            // strokeNode was a dupe
                            //
                            if (pathFigureABSide.Count > 0)
                            {
#if DEBUG_RENDERING_FEEDBACK
                                prevStrokeNode.GetPointsAtEndOfSegment(pathFigureABSide, pathFigureDCSide, debugDC, feedbackSize, showFeedback);
#else
                                //
                                // strokeNode was a dupe, we just need to render the end of the stroke
                                // which is at prevStrokeNode
                                //
                                prevStrokeNode.GetPointsAtEndOfSegment(pathFigureABSide, pathFigureDCSide);
#endif
                                //render
                                ReverseDCPointsRenderAndClear(context, pathFigureABSide, pathFigureDCSide, polyLinePoints, isEllipse, false/*clear the point collections*/);
                            }
                            else
                            {
                                // we've only seen two points to render
                                Debug.Assert(pathFigureDCSide.Count == 0);
                                //contains all the logic to render two stroke nodes
                                RenderTwoStrokeNodes(   context,
                                                        prevPrevStrokeNode,
                                                        prevPrevStrokeNodeBounds,
                                                        prevStrokeNode,
                                                        prevStrokeNodeBounds,
                                                        pathFigureABSide,
                                                        pathFigureDCSide,
                                                        polyLinePoints
#if DEBUG_RENDERING_FEEDBACK
                                                       ,debugDC,
                                                       feedbackSize,
                                                       showFeedback
#endif
                                                    );
}
                        }
                        else
                        {
                            if (calculateBounds)
                            {
                                bounds.Union(prevPrevStrokeNodeBounds);
                            }

                            // we only have a single point to render
                            Debug.Assert(pathFigureABSide.Count == 0);
                            prevPrevStrokeNode.GetContourPoints(pathFigureABSide);
                            AddFigureToStreamGeometryContext(context, pathFigureABSide, prevPrevStrokeNode.IsEllipse/*isBezierFigure*/);
}
                    }
                    else if (prevStrokeNode.IsValid && strokeNode.IsValid)
                    {
                        if (calculateBounds)
                        {
                            bounds.Union(prevStrokeNodeBounds);
                            bounds.Union(strokeNodeBounds);
                        }

                        // typical case, we hit the end of the stroke
                        // see if we need to start a stroke, or just end one
                        if (pathFigureABSide.Count > 0)
                        {
#if DEBUG_RENDERING_FEEDBACK
                            strokeNode.GetPointsAtEndOfSegment(pathFigureABSide, pathFigureDCSide, debugDC, feedbackSize, showFeedback);
#else
                            strokeNode.GetPointsAtEndOfSegment(pathFigureABSide, pathFigureDCSide);
#endif

                            //render
                            ReverseDCPointsRenderAndClear(context, pathFigureABSide, pathFigureDCSide, polyLinePoints, isEllipse, false/*clear the point collections*/);

                            if (FuzzyContains(strokeNodeBounds, prevStrokeNodeBounds, 70d) != RectCompareResult.NoItersection)
                            {
                                //render a complete stroke node or we can get artifacts
                                strokeNode.GetContourPoints(polyLinePoints);
                                AddFigureToStreamGeometryContext(context, polyLinePoints, strokeNode.IsEllipse/*isBezierFigure*/);
                            }
                        }
                        else
                        {
                            Debug.Assert(pathFigureDCSide.Count == 0);
                            //contains all the logic to render two stroke nodes
                            RenderTwoStrokeNodes(   context,
                                                    prevStrokeNode,
                                                    prevStrokeNodeBounds,
                                                    strokeNode,
                                                    strokeNodeBounds,
                                                    pathFigureABSide,
                                                    pathFigureDCSide,
                                                    polyLinePoints
#if DEBUG_RENDERING_FEEDBACK
                                                   ,debugDC,
                                                   feedbackSize,
                                                   showFeedback
#endif
                                                );
} 
                    }
                }
                finally
                {
                    context.Close();
                    geometry.Freeze();
                }
            }
        }


        /// <summary>
        /// Helper routine to render two distinct stroke nodes
        /// </summary>
        private static void RenderTwoStrokeNodes(   StreamGeometryContext context,
                                                    StrokeNode strokeNodePrevious,
                                                    Rect strokeNodePreviousBounds,
                                                    StrokeNode strokeNodeCurrent,
                                                    Rect strokeNodeCurrentBounds,
                                                    List<Point> pointBuffer1,
                                                    List<Point> pointBuffer2,
                                                    List<Point> pointBuffer3
#if DEBUG_RENDERING_FEEDBACK
                                                   ,DrawingContext debugDC,
                                                   double feedbackSize,
                                                   bool showFeedback
#endif
                                                    )
        {
            Debug.Assert(pointBuffer1 != null);
            Debug.Assert(pointBuffer2 != null);
            Debug.Assert(pointBuffer3 != null);
            Debug.Assert(context != null);
            
            
            //see if we need to render a quad - if there is not at least a 70% overlap
            if (FuzzyContains(strokeNodePreviousBounds, strokeNodeCurrentBounds, 70d) != RectCompareResult.NoItersection)
            {
                //we're between 100% and 70% overlapped
                //just render two distinct figures with a connecting quad (if needed)
                strokeNodePrevious.GetContourPoints(pointBuffer1);
                AddFigureToStreamGeometryContext(context, pointBuffer1, strokeNodePrevious.IsEllipse/*isBezierFigure*/);

                Quad quad = strokeNodeCurrent.GetConnectingQuad();
                if (!quad.IsEmpty)
                {
                    pointBuffer3.Add(quad.A);
                    pointBuffer3.Add(quad.B);
                    pointBuffer3.Add(quad.C);
                    pointBuffer3.Add(quad.D);
                    AddFigureToStreamGeometryContext(context, pointBuffer3, false/*isBezierFigure*/);
                }

                strokeNodeCurrent.GetContourPoints(pointBuffer2);
                AddFigureToStreamGeometryContext(context, pointBuffer2, strokeNodeCurrent.IsEllipse/*isBezierFigure*/);
            }
            else
            {
                //we're less than 70% overlapped, it's safe to run our optimization
#if DEBUG_RENDERING_FEEDBACK
                strokeNodeCurrent.GetPointsAtStartOfSegment(pointBuffer1, pointBuffer2, debugDC, feedbackSize, showFeedback);
                strokeNodeCurrent.GetPointsAtEndOfSegment(pointBuffer1, pointBuffer2, debugDC, feedbackSize, showFeedback);
#else
                strokeNodeCurrent.GetPointsAtStartOfSegment(pointBuffer1, pointBuffer2);
                strokeNodeCurrent.GetPointsAtEndOfSegment(pointBuffer1, pointBuffer2);
#endif
                //render
                ReverseDCPointsRenderAndClear(context, pointBuffer1, pointBuffer2, pointBuffer3, strokeNodeCurrent.IsEllipse, false/*clear the point collections*/);
            }
        }

        /// <summary>
        /// ReverseDCPointsRenderAndClear
        /// </summary>
        private static void ReverseDCPointsRenderAndClear(StreamGeometryContext context, List<Point> abPoints, List<Point> dcPoints, List<Point> polyLinePoints, bool isEllipse, bool clear)
        {
            //we need to reverse the cd side points
            Point temp;
            for (int i = 0, j = dcPoints.Count - 1; i < j; i++, j--)
            {
                temp = dcPoints[i];
                dcPoints[i] = dcPoints[j];
                dcPoints[j] = temp;
            }
            if (isEllipse)
            {
                AddArcToFigureToStreamGeometryContext(context, abPoints, dcPoints, polyLinePoints);
            }
            else
            {
                //for rectangles, render a single path figure by combining both sides
                AddPolylineFigureToStreamGeometryContext(context, abPoints, dcPoints);
            }

            if (clear)
            {
                abPoints.Clear();
                dcPoints.Clear();
            }
        }
        /// <summary>
        /// FuzzyContains for two rects
        /// </summary>
        private static RectCompareResult FuzzyContains(Rect rect1, Rect rect2, double percentIntersect)
        {
            Debug.Assert(percentIntersect >= 0.0 && percentIntersect <= 100.0d);


            double intersectLeft = Math.Max(rect1.Left, rect2.Left);
            double intersectTop = Math.Max(rect1.Top, rect2.Top);
            double intersectWidth = Math.Max((double)(Math.Min(rect1.Right, rect2.Right) - intersectLeft), (double)0);
            double intersectHeight = Math.Max((double)(Math.Min(rect1.Bottom, rect2.Bottom) - intersectTop), (double)0);

            if (intersectWidth == 0.0d || intersectHeight == 0.0d)
            {
                return RectCompareResult.NoItersection;
            }

            //we have an intersection, see if it is enough
            double rect1Area = rect1.Height * rect1.Width;
            double rect2Area = rect2.Height * rect2.Width;
            double minArea = Math.Min(rect1Area, rect2Area);
            double intersectionArea = intersectWidth * intersectHeight;
            double intersect = (intersectionArea / minArea) * 100d;
            if (intersect >= percentIntersect)
            {
                if (rect1Area >= rect2Area)
                {
                    return RectCompareResult.Rect1ContainsRect2;
                }
                return RectCompareResult.Rect2ContainsRect1;
            }

            return RectCompareResult.NoItersection;
        }

        /// <summary>
        /// Private helper to render a path figure to the SGC
        /// </summary>
        private static void AddFigureToStreamGeometryContext(StreamGeometryContext context, List<Point> points, bool isBezierFigure)
        {
            Debug.Assert(context != null);
            Debug.Assert(points != null);
            Debug.Assert(points.Count > 0);

            context.BeginFigure(points[points.Count - 1], //start point
                                        true,   //isFilled
                                        true);  //IsClosed

            if (isBezierFigure)
            {
                context.PolyBezierTo(points,
                                     true,      //isStroked
                                     true);     //isSmoothJoin
            }
            else
            {
                context.PolyLineTo(points,
                                     true,      //isStroked
                                     true);     //isSmoothJoin
            }
        }


        /// <summary>
        /// Private helper to render a path figure to the SGC
        /// </summary>
        private static void AddPolylineFigureToStreamGeometryContext(StreamGeometryContext context, List<Point> abPoints, List<Point> dcPoints)
        {
            Debug.Assert(context != null);
            Debug.Assert(abPoints != null && dcPoints != null);
            Debug.Assert(abPoints.Count > 0 && dcPoints.Count > 0);

            context.BeginFigure(abPoints[0], //start point
                                        true,   //isFilled
                                        true);  //IsClosed

            context.PolyLineTo(abPoints,
                                 true,      //isStroked
                                 true);     //isSmoothJoin

            context.PolyLineTo(dcPoints,
                                 true,      //isStroked
                                 true);     //isSmoothJoin
}

        /// <summary>
        /// Private helper to render a path figure to the SGC
        /// </summary>
        private static void AddArcToFigureToStreamGeometryContext(StreamGeometryContext context, List<Point> abPoints, List<Point> dcPoints, List<Point> polyLinePoints)
        {
            Debug.Assert(context != null);
            Debug.Assert(abPoints != null && dcPoints != null);
            Debug.Assert(polyLinePoints != null);
            //Debug.Assert(abPoints.Count > 0 && dcPoints.Count > 0);
            if (abPoints.Count == 0 || dcPoints.Count == 0)
            {
                return;
            }

            context.BeginFigure(abPoints[0], //start point
                                        true,   //isFilled
                                        true);  //IsClosed

            for (int j = 0; j < 2; j++)
            {
                List<Point> points = j == 0 ? abPoints : dcPoints;
                int startIndex = j == 0 ? 1 : 0;
                for (int i = startIndex; i < points.Count; )
                {
                    Point next = points[i];
                    if (next == StrokeRenderer.ArcToMarker)
                    {
                        if (polyLinePoints.Count > 0)
                        {
                            //polyline first
                            context.PolyLineTo(  polyLinePoints,
                                                 true,      //isStroked
                                                 true);     //isSmoothJoin
                            polyLinePoints.Clear();
                        }
                        //we're arcing, pull out height, width and the arc to point
                        Debug.Assert(i + 2 < points.Count);
                        if (i + 2 < points.Count)
                        {
                            Point sizePoint = points[i + 1];
                            Size ellipseSize = new Size(sizePoint.X / 2/*width*/, sizePoint.Y / 2/*height*/);
                            Point arcToPoint = points[i + 2];

                            bool isLargeArc = false; //>= 180

                            context.ArcTo(  arcToPoint,
                                            ellipseSize,
                                            0d,             //rotation
                                            isLargeArc,     //isLargeArc
                                            SweepDirection.Clockwise,
                                            true,           //isStroked
                                            true);          //isSmoothJoin
                        }
                        i += 3; //advance past this arcTo block
                    }
                    else
                    {
                        //walk forward until we find an arc marker or the end
                        polyLinePoints.Add(next);
                        i++;
                    }
                }
                if (polyLinePoints.Count > 0)
                {
                    //polyline
                    context.PolyLineTo(polyLinePoints,
                                         true,      //isStroked
                                         true);     //isSmoothJoin
                    polyLinePoints.Clear();
                }
            }
        }

        /// <summary>
        /// calculates the angle between the previousPosition and the current one and then computes the delta between 
        /// the lastAngle.  lastAngle is also updated
        /// </summary>
        private static double GetAngleDeltaFromLast(Point previousPosition, Point currentPosition, ref double lastAngle)
        {
            double delta = 0.0d;
            
            //input points typically come in very close to each other
            double dx = (currentPosition.X * 1000) - (previousPosition.X * 1000);
            double dy = (currentPosition.Y * 1000) - (previousPosition.Y * 1000);
            if ((Int64)dx == 0 && (Int64)dy == 0)
            {
                //the points are close enough not to matter
                //don't update lastAngle
                return delta;
            }
            
            double angle = GetAngleBetween(previousPosition, currentPosition);

            //special case when angle / lastAngle span 0 degrees
            if (lastAngle >= 270 && angle <= 90)
            {
                delta = lastAngle - (360d + angle);
            }
            else if (lastAngle <= 90 && angle >= 270)
            {
                delta = (360d + lastAngle) - angle;
            }
            else
            {
                delta = (lastAngle - angle);
            }
            lastAngle = angle;

            // Return
            return delta;
        }

        /// <summary>
        /// calculates the angle between the previousPosition and the current one and then computes the delta between 
        /// the lastAngle.  lastAngle is also updated
        /// </summary>
        private static double GetAngleBetween(Point previousPosition, Point currentPosition)
        {
            double angle = 0.0d;

            //input points typically come in very close to each other
            double dx = (currentPosition.X * 1000) - (previousPosition.X * 1000);
            double dy = (currentPosition.Y * 1000) - (previousPosition.Y * 1000);
            if ((Int64)dx == 0 && (Int64)dy == 0)
            {
                //the points are close enough not to matter
                return angle;
            }

            // Calculate angle
            if (dx == 0.0)
            {
                if (dy == 0.0)
                {
                    angle = 0.0;
                }
                else if (dy > 0.0)
                {
                    angle = Math.PI / 2.0;
                }
                else
                {
                    angle = Math.PI * 3.0 / 2.0;
                }
            }
            else if (dy == 0.0)
            {
                if (dx > 0.0)
                {
                    angle = 0.0;
                }
                else
                {
                    angle = Math.PI;
                }
            }
            else
            {
                if (dx < 0.0)
                {
                    angle = Math.Atan(dy / dx) + Math.PI;
                }
                else if (dy < 0.0)
                {
                    angle = Math.Atan(dy / dx) + (2 * Math.PI);
                }
                else
                {
                    angle = Math.Atan(dy / dx);
                }
            }

            // Convert to degrees
            angle = angle * 180 / Math.PI;

            // Return
            return angle;
        }

        /// <summary>
        /// Get the DrawingAttributes to use for a highlighter stroke. The return value is a copy of
        /// the DA passed in if color.A != 255 with color.A overriden to be 255. Otherwise it returns
        /// the DA passed in.
        /// </summary>
        internal static DrawingAttributes GetHighlighterAttributes(Stroke stroke, DrawingAttributes da)
        {
            System.Diagnostics.Debug.Assert(da.IsHighlighter = true);
            if (da.Color.A != SolidStrokeAlpha)
            {
                DrawingAttributes copy = stroke.DrawingAttributes.Clone();
                copy.Color = GetHighlighterColor(copy.Color);
                return copy;
            }

            return da;
        }

        /// <summary>
        /// Get the color used to draw a highlighter.
        /// </summary>
        internal static Color GetHighlighterColor(Color color)
        {
            // For a highlighter stroke, the color.A is overriden to be 255
            color.A = SolidStrokeAlpha;
            return color;
        }

        // Opacity for highlighter container visuals
        internal static readonly double HighlighterOpacity = 0.5;
        internal static readonly byte SolidStrokeAlpha = 0xFF;
        internal static readonly Point ArcToMarker = new Point(Double.MinValue, Double.MinValue);

        /// <summary>
        /// Simple helper enum
        /// </summary>
        private enum RectCompareResult
        {
            Rect1ContainsRect2,
            Rect2ContainsRect1,
            NoItersection,
        }
        #endregion
    }
}

