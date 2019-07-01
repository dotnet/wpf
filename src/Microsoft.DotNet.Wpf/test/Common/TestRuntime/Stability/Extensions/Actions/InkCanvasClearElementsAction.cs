// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Clears all elements from a InkCanvas
    /// </summary>
    public class InkCanvasClearElementsAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas InkCanvas { get; set; }

        public override void Perform()
        {
            foreach (UIElement element in InkCanvas.Children)
            {
                if (element is Popup)
                {
                    ((Popup)element).IsOpen = false;
                }
            }

            // Clear all elements from the InkCanvas.
            InkCanvas.Children.Clear(); 
        }
    }
}

