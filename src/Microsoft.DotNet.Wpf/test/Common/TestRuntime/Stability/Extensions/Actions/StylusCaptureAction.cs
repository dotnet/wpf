// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Captures the stylus to the specified element.
    /// </summary>
    public class StylusCaptureAction : SimpleDiscoverableAction
    {
        public int OptionIndex { get; set; }

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas InkCanvas { get; set; }

        public int Index { get; set; }

        public override void Perform()
        {
            int option = OptionIndex % 2;
            switch (option)
            {
                case 1:
                    Stylus.Capture(InkCanvas);
                    break;
                case 2:
                    if (InkCanvas.Children.Count > 0)
                    {
                        int indexToCapture = Index % InkCanvas.Children.Count;
                        Stylus.Capture(InkCanvas.Children[indexToCapture]);
                    }
                    break;
            }
        }
    }
}
