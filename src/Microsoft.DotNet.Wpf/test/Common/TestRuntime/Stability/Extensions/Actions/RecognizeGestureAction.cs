// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Ink;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Looks for gestures in the specified StrokeCollection.
    /// </summary>
    public class RecognizeGestureAction : SimpleDiscoverableAction
    {
        public InkCanvas InkCanvas { get; set; }

        public int strokeIndex { get; set; }

        public bool AddOnemore { get; set; }

        public GestureRecognizer GestureRecognizer { get; set; }

        public override void Perform()
        {
            //Create an empty StrokeCollection to add one or two strokes
            StrokeCollection strokeCollection = new StrokeCollection();

            //Pick up a Random stroke from the strokes in InkCanvas
            strokeCollection.Add(InkCanvas.Strokes[strokeIndex % InkCanvas.Strokes.Count]);

            if (AddOnemore && InkCanvas.Strokes.Count > 1)
            {
                strokeCollection.Add(InkCanvas.Strokes[(strokeIndex + 1) % InkCanvas.Strokes.Count]);
            }

            //Maximum number of strokes is two.
            GestureRecognizer.Recognize(strokeCollection);

            strokeCollection.Clear();
            strokeCollection = null;
        }

        public override bool CanPerform()
        {
            return InkCanvas.IsGestureRecognizerAvailable && InkCanvas.Strokes.Count > 0;
        }
    }
}
