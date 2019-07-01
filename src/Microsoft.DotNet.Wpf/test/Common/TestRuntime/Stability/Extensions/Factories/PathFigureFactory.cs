// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;


namespace Microsoft.Test.Stability.Extensions.Factories
{

    class PathFigureFactory : DiscoverableFactory<PathFigure>
    {
        public Point StartPoint { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public PathSegmentCollection Segments { get; set; }

        public override PathFigure Create(DeterministicRandom random)
        {
            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = StartPoint;
            pathFigure.Segments = Segments;
            pathFigure.IsClosed = random.NextBool();
            return pathFigure;
        }
    }

    class PathSegmentCollectionFactory : DiscoverableCollectionFactory<PathSegmentCollection, PathSegment> 
    {
        [InputAttribute(ContentInputSource.CreateFromFactory, MinListSize = 1)]
        public override List<PathSegment> ContentList { get; set; }
    }


    class LineSegmentFactory : DiscoverableFactory<LineSegment>
    {
        public Point Point { get; set; }

        public override LineSegment Create(DeterministicRandom random)
        {
            LineSegment segment = new LineSegment();
            segment.Point = Point;
            segment.IsStroked = random.NextBool();
            segment.IsSmoothJoin = random.NextBool();
            return segment;
        }
    }

    class BezierSegmentFactory : DiscoverableFactory<BezierSegment>
    {
        public Point Point1 { get; set; }
        public Point Point2 { get; set; }
        public Point Point3 { get; set; }

        public override BezierSegment Create(DeterministicRandom random)
        {
            BezierSegment segment = new BezierSegment();
            segment.Point1 = Point1;
            segment.Point2 = Point2;
            segment.Point3 = Point3;
            segment.IsStroked = random.NextBool();
            segment.IsSmoothJoin = random.NextBool();
            return segment;
        }
    }

    class PolyBezierSegmentFactory : DiscoverableFactory<PolyBezierSegment>
    {
        public Point Point1 { get; set; }
        public Point Point2 { get; set; }
        public Point Point3 { get; set; }

        public override PolyBezierSegment Create(DeterministicRandom random)
        {
            PolyBezierSegment segment = new PolyBezierSegment();

            PointCollection Points = new PointCollection();
            Points.Add(Point1);
            Points.Add(Point2);
            Points.Add(Point3);

            segment.Points = Points;
            segment.IsStroked = random.NextBool();
            segment.IsSmoothJoin = random.NextBool();
            return segment;
        }
    }

    class PolyLineSegmentFactory : DiscoverableFactory<PolyLineSegment>
    {
        public PointCollection Points { get; set; }

        public override PolyLineSegment Create(DeterministicRandom random)
        {
            PolyLineSegment segment = new PolyLineSegment();
            segment.Points = Points;
            segment.IsStroked = random.NextBool();
            segment.IsSmoothJoin = random.NextBool();
            return segment;
        }
    }

    class ArcSegmentFactory : DiscoverableFactory<ArcSegment>
    {
        public Point Point { get; set; }
        public Size Size { get; set; }

        public override ArcSegment Create(DeterministicRandom random)
        {
            ArcSegment segment = new ArcSegment();
            segment.IsLargeArc = random.NextBool();
            segment.RotationAngle = random.NextDouble() * 360;
            segment.IsSmoothJoin = random.NextBool();
            segment.SweepDirection = random.NextEnum<SweepDirection>();
            return segment;
        }
    }

    class QuadraticBezierSegmentFactory : DiscoverableFactory<QuadraticBezierSegment>
    {
        public Point Point1 { get; set; }
        public Point Point2 { get; set; }

        public override QuadraticBezierSegment Create(DeterministicRandom random)
        {
            QuadraticBezierSegment segment = new QuadraticBezierSegment();
            segment.Point1 = Point1;
            segment.Point2 = Point2;
            segment.IsStroked = random.NextBool();
            segment.IsSmoothJoin = random.NextBool();
            return segment;
        }
    }

    class PolyQuadraticBezierSegmentFactory : DiscoverableFactory<PolyQuadraticBezierSegment>
    {
        public Point Point1 { get; set; }
        public Point Point2 { get; set; }

        public override PolyQuadraticBezierSegment Create(DeterministicRandom random)
        {
            PolyQuadraticBezierSegment segment = new PolyQuadraticBezierSegment();
            
            PointCollection Points = new PointCollection();
            Points.Add(Point1);
            Points.Add(Point2);

            segment.Points = Points;
            segment.IsStroked = random.NextBool();
            segment.IsSmoothJoin = random.NextBool();
            return segment;
        }
    }
}
