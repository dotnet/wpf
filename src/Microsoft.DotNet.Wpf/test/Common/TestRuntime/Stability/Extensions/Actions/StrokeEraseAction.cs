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
    /// Erase with Lasso or Rect or eraser & StylusShape
    /// </summary>
    public class StrokeEraseAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas InkCanvas { get; set; }

        public int StrokeIndex { get; set; }

        public int OptionIndex { get; set; }

        public PointCollection LassoPoints { get; set; }

        public Rect Rect { get; set; }

        public StylusShape StylusShape { get; set; }

        public override void Perform()
        {
            Stroke stroke = InkCanvas.Strokes[StrokeIndex % InkCanvas.Strokes.Count];

            int option = OptionIndex % 3;
            switch (option)
            {
                // Erase with Lasso
                case 0:
                    stroke.GetEraseResult(LassoPoints);
                    break;
                //Erase with Rect
                case 1:
                    stroke.GetEraseResult(Rect);
                    break;
                //Erase with eraser & StylusShape
                case 2:
                    stroke.GetEraseResult(LassoPoints, StylusShape);
                    break;
            }
        }

        public override bool CanPerform()
        {
            return InkCanvas.Strokes.Count > 0;
        }
    }
}
