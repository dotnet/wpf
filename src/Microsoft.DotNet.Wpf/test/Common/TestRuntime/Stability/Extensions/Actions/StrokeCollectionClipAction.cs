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
    /// Clip with Lasso or Rect.
    /// </summary>
    public class StrokeCollectionClipAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas InkCanvas { get; set; }

        public bool IsLasso { get; set; }

        public PointCollection Lasso { get; set; }

        public Rect Rect { get; set; }

        public override void Perform()
        {
            //Strokes are from InkCanvas.
            StrokeCollection strokeCollection = InkCanvas.Strokes;

            //2 options
            if (IsLasso)
            {
                //Clip With Lasso
                strokeCollection.Clip(Lasso);
            }
            else
            {
                //Clip With Rect
                strokeCollection.Clip(Rect);
            }
        }
    }
}
