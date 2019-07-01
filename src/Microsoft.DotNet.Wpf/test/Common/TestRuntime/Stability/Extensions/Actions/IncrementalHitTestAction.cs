// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Adds points to the IncrementalHitTester.
    /// </summary>
    public class IncrementalHitTestAction : SimpleDiscoverableAction
    {
        public int OptionIndex { get; set; }

        public int RandomIndex { get; set; }

        public bool IsRandomPoint { get; set; }

        public InkCanvas InkCanvas { get; set; }

        /// <summary>
        /// StylusPointCollection cannot be empty when attached to a Stroke.
        /// </summary>
        public StylusPointCollection StylusPointCollection { get; set; }

        public Point Point { get; set; }

        public PointCollection PointCollection { get; set; }

        public StylusShape EraserShape { get; set; }

        public override void Perform()
        {
            IncrementalStrokeHitTester IncrementalStroke = InkCanvas.Strokes.GetIncrementalStrokeHitTester(EraserShape);
            IncrementalLassoHitTester IncrementalLasso = InkCanvas.Strokes.GetIncrementalLassoHitTester(OptionIndex % 100);

            switch (OptionIndex % 2)
            {
                case 0:  //AddPoint
                    {
                        StrokeCollection strokeCollection = InkCanvas.Strokes;

                        if (IsRandomPoint == true || strokeCollection == null || strokeCollection.Count == 0)
                        {
                            IncrementalStroke.AddPoint(Point);
                            IncrementalLasso.AddPoint(Point);
                        }
                        else
                        {
                            //get list of all points on the stroke
                            foreach (Stroke stoke in strokeCollection)
                            {
                                StylusPointCollection.Add(stoke.StylusPoints.Reformat(StylusPointCollection.Description));
                            }

                            IncrementalStroke.AddPoint(StylusPointCollection[RandomIndex % StylusPointCollection.Count].ToPoint());
                            IncrementalLasso.AddPoint(StylusPointCollection[RandomIndex % StylusPointCollection.Count].ToPoint());
                        }
                    }
                    break;
                case 1:   //AddPoints
                    {
                        IncrementalLasso.AddPoints(PointCollection);
                        IncrementalStroke.AddPoints(PointCollection);
                    }
                    break;
            }
        }
    }
}
