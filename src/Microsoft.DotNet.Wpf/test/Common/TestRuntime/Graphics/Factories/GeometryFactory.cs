// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;

namespace Microsoft.Test.Graphics.Factories
{
    /// <summary>
    /// Geometry Factory for creating Geometry
    /// </summary>
    public class GeometryFactory
    {
        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Geometry MakeGeometry(string geometry)
        {
            string[] parsedGeometry = geometry.Split(' ');

            switch (parsedGeometry[0])
            {
                case "Circle":
                    return new EllipseGeometry(new Point(120, 120), 60, 60);
                case "Square":
                    return new RectangleGeometry(new Rect(35, 35, 100, 100));
                case "HorizontalLine":
                    return new LineGeometry(new Point(20, 100), new Point(100, 100));

                case "PGTotal":
                    return PGTotal;

                case "Star":
                    return Star;

                default:
                    throw new ArgumentException("Specified geometry (" + geometry + ") cannot be created");

            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Geometry PGTotal
        {
            get
            {
                PathGeometry pgTotal = new PathGeometry();
                LineSegment lsTotal = new LineSegment(new Point(100, 0), true);
                BezierSegment bsTotal = new BezierSegment(new Point(125, 125),
                                                            new Point(125, 75),
                                                            new Point(100, 100),
                                                            true);
                QuadraticBezierSegment qbTotal = new QuadraticBezierSegment(new Point(50, 50),
                                                            new Point(0, 100),
                                                            true);
                ArcSegment acTotal = new ArcSegment(new Point(100, 150),
                                                        new Size(100, 100),
                                                        45.0,
                                                        false,
                                                        SweepDirection.Clockwise,
                                                        true);
                PolyLineSegment plTotal = new PolyLineSegment(new Point[] {
                                                        new Point ( 100, 175 ),
                                                        new Point ( 0, 175 )
                                                        },
                                                        true
                                                    );
                PolyBezierSegment pbTotal = new PolyBezierSegment(new Point[] {
                                                        new Point ( 50, 225 ),
                                                        new Point ( 50, 275 ),
                                                        new Point ( 0, 300 ),
                                                        new Point ( 50, 325 ),
                                                        new Point ( 50, 375 ),
                                                        new Point ( 0, 400 )
                                                        },
                                                        true
                                                    );
                PolyQuadraticBezierSegment pqbTotal = new PolyQuadraticBezierSegment(new Point[] {
                                                        new Point ( 50, 450 ),
                                                        new Point ( 0, 500 ),
                                                        new Point ( 50, 550 ),
                                                        new Point ( 0, 600 )
                                                        },
                                                        true
                                                    );
                PathFigure pfTotal = new PathFigure();
                pfTotal.Segments.Add(lsTotal);
                pfTotal.Segments.Add(bsTotal);
                pfTotal.Segments.Add(qbTotal);
                pfTotal.Segments.Add(acTotal);
                pfTotal.Segments.Add(plTotal);
                pfTotal.Segments.Add(pbTotal);
                pfTotal.Segments.Add(pqbTotal);
                pfTotal.StartPoint = new Point(0, 0);
                pfTotal.IsClosed = true;
                pgTotal.Figures.Add(pfTotal);
                return pgTotal;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Geometry Star
        {
            get
            {
                PathFigure figure = new PathFigure();
                figure.StartPoint = new Point(50, 150);
                figure.Segments.Add(new LineSegment(new Point(100, 0), true));
                figure.Segments.Add(new LineSegment(new Point(150, 150), true));
                figure.Segments.Add(new LineSegment(new Point(25, 50), true));
                figure.Segments.Add(new LineSegment(new Point(175, 50), true));

                return new PathGeometry(new PathFigure[] { figure }, FillRule.EvenOdd, null);
            }
        }
    }
}
