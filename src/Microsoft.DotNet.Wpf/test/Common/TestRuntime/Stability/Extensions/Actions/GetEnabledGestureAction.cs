// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Ink;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Gets the gestures that are recognized by InkCanvas or Recognizer.
    /// </summary>
    public class GetEnabledGestureAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas InkCanvas { get; set; }

        public GestureRecognizer GestureRecognizer { get; set; }

        public bool IsFromInkCanvas { get; set; }

        public override void Perform()
        {
            if (InkCanvas.IsGestureRecognizerAvailable)
            {
                if (IsFromInkCanvas)
                {
                    //Gets the gestures that are recognized by InkCanvas
                    InkCanvas.GetEnabledGestures();
                }
                else
                {
                    //Gets the gestures that are recognized by GestureRecognizer
                    GestureRecognizer.GetEnabledGestures();
                }
            }
        }
    }
}
