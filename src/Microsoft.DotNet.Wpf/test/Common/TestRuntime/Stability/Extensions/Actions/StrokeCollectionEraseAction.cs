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
    /// Erase with Lasso or Rect or eraser & StylusShape.
    /// </summary>
    public class StrokeCollectionEraseAction : SimpleDiscoverableAction
    {
        public InkCanvas InkCanvas { get; set; }

        public int Rnd { get; set; }

        public PointCollection Lasso { get; set; }

        public Rect Rect { get; set; }

        public StylusShape StylusShape { get; set; }

        public override void Perform()
        {
            //Strokes are from InkCanvas.
            StrokeCollection strokeCollection = InkCanvas.Strokes;

            int option = Rnd % 3;
            switch (option)
            {
                // Erase with Lasso
                case 0:
                    {
                        strokeCollection.Erase(Lasso);
                    }
                    break;
                //Erase with Rect
                case 1:
                    {
                        strokeCollection.Erase(Rect);
                    }
                    break;
                //Erase with eraser & StylusShape
                case 2:
                    {
                        strokeCollection.Erase(Lasso, StylusShape);
                    }
                    break;
            }
        }
    }
}
