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
    /// Clip with Lasso or Rect
    /// </summary>
    public class StrokeClipAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas InkCanvas { get; set; }

        public int StrokeIndex { get; set; }

        public bool IsLasso { get; set; }

        public PointCollection LassoPoints { get; set; }

        public Rect Rect { get; set; }

        public override void Perform()
        {
            Stroke stroke = InkCanvas.Strokes[StrokeIndex % InkCanvas.Strokes.Count];
            if (IsLasso)
            {
                //Clip With Lasso
                stroke.GetClipResult(LassoPoints);
            }
            else
            {
                //Clip With Rect
                stroke.GetClipResult(Rect);
            }
        }

        public override bool CanPerform()
        {
            return InkCanvas.Strokes.Count > 0;
        }
    }
}
