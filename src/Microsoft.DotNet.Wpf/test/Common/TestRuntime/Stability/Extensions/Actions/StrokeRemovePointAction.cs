// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Remove a point from the stroke.
    /// </summary>
    public class StrokeRemovePointAction : SimpleDiscoverableAction
    {
        public int PointIndex { get; set; }

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas InkCanvas { get; set; }

        public int StrokeIndex { get; set; }

        public override void Perform()
        {
            Stroke stroke = InkCanvas.Strokes[StrokeIndex % InkCanvas.Strokes.Count];
            if (stroke.StylusPoints.Count > 1) //don't remove last point (StylusPointCollection can't be zero when attached to a stroke)
            {
                int index = PointIndex % stroke.StylusPoints.Count;
                StylusPoint StylusPoint = stroke.StylusPoints[index];
                //remove a point from a random index
                stroke.StylusPoints.RemoveAt(index);
            }
        }

        public override bool CanPerform()
        {
            return InkCanvas.Strokes.Count > 0;
        }
    }
}
