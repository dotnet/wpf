// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Factory for Shapes
    /// </summary>
    abstract class ShapeFactory<T> : DiscoverableFactory<T> where T : Shape
    {

        #region Private Variables
        public Point Point { get; set; }
        public Brush Brush { get; set; }
        public Transform Transform { get; set; }
        public Geometry Geometry { get; set; }
        #endregion

        protected void ApplyShapeProperties(Shape shape, DeterministicRandom random)
        {
            shape.Fill = Brush;
            shape.Stroke = Brush;
            shape.StrokeStartLineCap = random.NextEnum<PenLineCap>();
            shape.StrokeEndLineCap = random.NextEnum<PenLineCap>();
            shape.StrokeLineJoin = random.NextEnum<PenLineJoin>();
            shape.StrokeDashCap = random.NextEnum<PenLineCap>();
            shape.StrokeThickness = random.NextDouble() * 40;
            shape.StrokeDashOffset = random.NextDouble() * 10;
            shape.RenderTransform = Transform;
            shape.Stretch = random.NextEnum<Stretch>();
            DoubleCollection doubleCollection = new DoubleCollection();

            int size = Convert.ToInt32(Math.Abs(random.NextDouble()));
            for (int i = 0; i < size; i++)
            {
                doubleCollection.Add(random.NextDouble() * 10);
            }

            shape.StrokeDashArray = doubleCollection;
            shape.StrokeMiterLimit = random.NextDouble() * 10;
        }
    }

    class LineFactory : ShapeFactory<Line>
    {
        public override Line Create(DeterministicRandom random)
        {
            Line shape = new Line();
            ApplyShapeProperties(shape, random);
            shape.X1 = random.NextDouble() * 10;
            shape.Y1 = random.NextDouble() * 20;
            shape.X2 = random.NextDouble() * 100;
            shape.Y2 = random.NextDouble() * 100;
            return shape;
        }
    }

    class EllipseFactory : ShapeFactory<Ellipse>
    {
        public override Ellipse Create(DeterministicRandom random)
        {
            Ellipse shape = new Ellipse();
            ApplyShapeProperties(shape, random);
            shape.Width = random.NextDouble() * 200;
            shape.Height = random.NextDouble() * 200;
            return shape;
        }
    }

    class RectangleFactory : ShapeFactory<Rectangle>
    {
        public override Rectangle Create(DeterministicRandom random)
        {
            Rectangle shape = new Rectangle();
            ApplyShapeProperties(shape, random);
            shape.Width = random.NextDouble() * 200;
            shape.Height = random.NextDouble() * 200;
            shape.RadiusX = random.NextDouble() * 100;
            shape.RadiusY = random.NextDouble() * 200;
            return shape;
        }
    }
    class PathFactory : ShapeFactory<Path>
    {
        public override Path Create(DeterministicRandom random)
        {
            Path shape = new Path();
            ApplyShapeProperties(shape, random);
            shape.Data = Geometry;
            return shape;
        }
    }

    class PolylineFactory : ShapeFactory<Polyline>
    {
        public PointCollection PointCollection {get; set;}

        public override Polyline Create(DeterministicRandom random)
        {
            Polyline shape = new Polyline();
            ApplyShapeProperties(shape, random);
            shape.Points = PointCollection;
            shape.FillRule = random.NextEnum<FillRule>();
            return shape;
        }
    }

    class PolygonFactory : ShapeFactory<Polygon>
    {
        public PointCollection PointCollection { get; set; }

        public override Polygon Create(DeterministicRandom random)
        {
            Polygon shape = new Polygon();
            ApplyShapeProperties(shape, random);
            shape.Points = PointCollection;
            shape.FillRule = random.NextEnum<FillRule>();
            return shape;
        }
    }
}
