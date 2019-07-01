// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Gets a collection of strokes that intersect the specified point.
    /// </summary>
    public class StrokeCollectionHitTestAction : SimpleDiscoverableAction
    {
        #region Public members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas InkCanvas { get; set; }

        public int Rnd { get; set; }

        public int PercentLasso { get; set; }

        public int PercentRect { get; set; }

        public Point Point { get; set; }

        public PointCollection Lasso { get; set; }

        public PointCollection EraserLasso { get; set; }

        public StylusShape EraserShape { get; set; }

        public Point Position { get; set; }

        public int RndDiameter { get; set; }

        public double RndDouble { get; set; }

        public Rect Rect { get; set; }

        #endregion

        public override void Perform()
        {
            //Strokes are from InkCanvas.
            StrokeCollection strokeCollection = InkCanvas.Strokes;

            int option = Rnd % 5;
            switch (option)
            {
                //HitTest with Point
                case 0:
                    {
                        strokeCollection.HitTest(Point);
                    }
                    break;

                //HitTest with Lasso & percentage
                case 1:
                    {
                        strokeCollection.HitTest(Lasso, PercentLasso % 101);
                    }
                    break;

                //HitTest with eraserPath and StylusShape
                case 2:
                    {
                        strokeCollection.HitTest(EraserLasso, EraserShape);
                    }
                    break;

                //HitTest with point and Diameter
                case 3:
                    {
                        double diameter = (double)(RndDiameter % 50) + RndDouble;
                        strokeCollection.HitTest(Position, diameter);
                    }
                    break;

                //HitTest with Rect and percentage
                case 4:
                    {
                        strokeCollection.HitTest(Rect, PercentRect % 101);
                    }
                    break;
            }
        }
    }
}
