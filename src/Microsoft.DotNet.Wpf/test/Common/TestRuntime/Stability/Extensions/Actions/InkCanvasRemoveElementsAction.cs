// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Removes elements from a InkCanvas
    /// </summary>
    public class InkCanvasRemoveElementsAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas InkCanvas { get; set; }

        public int OptionIndex { get; set; }

        public int RemoveIndex { get; set; }

        public override void Perform()
        {
            foreach (UIElement element in InkCanvas.Children)
            {
                if (element is Popup)
                {
                    ((Popup)element).IsOpen = false;
                }
            }

            int indexRemove = RemoveIndex % InkCanvas.Children.Count;
            switch (OptionIndex % 2)
            {
                case 0:
                    InkCanvas.Children.Remove(InkCanvas.Children[indexRemove]); // Remove the specified element from the InkCanvas.
                    break;
                case 1:
                    InkCanvas.Children.RemoveAt(indexRemove); // Remove the element at the specified index from the InkCanvas.
                    break;
            }
        }

        public override bool CanPerform()
        {
            return InkCanvas.Children.Count > 0;
        }
    }
}
