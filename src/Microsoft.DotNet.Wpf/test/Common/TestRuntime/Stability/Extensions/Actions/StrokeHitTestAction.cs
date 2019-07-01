// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Gets a collection of strokes that intersect the specified point.
    /// </summary>
    public class StrokeHitTestAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas InkCanvas { get; set; }

        public int StrokeIndex { get; set; }

        public int OptionIndex { get; set; }

        public Point Point { get; set; }

        public int Percent { get; set; }

        public PointCollection LassoPoints { get; set; }

        public StylusShape EraserShape { get; set; }

        public PointCollection EraserLasso { get; set; }

        public Rect Rect { get; set; }

        public int IntDiameter { get; set; }

        public double DoubleDiameter { get; set; }

        #endregion

        public override void Perform()
        {
            Stroke stroke = InkCanvas.Strokes[StrokeIndex % InkCanvas.Strokes.Count];

            int option = OptionIndex % 5;
            switch (option)
            {
                //HitTest with Point
                case 0:
                    {
                        stroke.HitTest(Point);
                    }
                    break;

                //HitTest with Lasso & percentage
                case 1:
                    {
                        stroke.HitTest(LassoPoints, Percent % 101);
                    }
                    break;

                //HitTest with eraserPath and StylusShape
                case 2:
                    {
                        stroke.HitTest(EraserLasso, EraserShape);
                    }
                    break;

                //HitTest with point and Diameter
                case 3:
                    {
                        double diameter = (double)(IntDiameter % 50 + DoubleDiameter);
                        stroke.HitTest(Point, diameter);
                    }
                    break;

                //HitTest with Rect and percentage
                case 4:
                    {
                        stroke.HitTest(Rect, Percent % 101);
                    }
                    break;
            }
        }

        public override bool CanPerform()
        {
            return InkCanvas.Strokes.Count > 0;
        }
    }
}
