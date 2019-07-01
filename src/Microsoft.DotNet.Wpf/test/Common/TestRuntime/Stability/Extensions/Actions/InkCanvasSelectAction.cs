// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Ink;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class InkCanvasSelectAction : ClipboardLockBaseAction
    {
        public int SelectionIndex { get; set; }

        public int SubActionIndex { get; set; }

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas InkCanvas { get; set; }

        public override void Perform()
        {
            UIElement[] uiElementArray = new UIElement[InkCanvas.Children.Count];
            //Copies an UIElement to an array.
            InkCanvas.Children.CopyTo(uiElementArray, 0);

            StrokeCollection strokeCollection = new StrokeCollection();
            foreach (Stroke stroke in InkCanvas.Strokes)
            {
                strokeCollection.Add(stroke);
            }

            switch (SelectionIndex % 3)
            {
                case 0:
                    InkCanvas.Select(strokeCollection); // Selects a set of Ink stroke objects.
                    break;
                case 1:
                    InkCanvas.Select(uiElementArray); // Selects a set of UIElement objects.
                    break;
                case 2:
                    InkCanvas.Select(strokeCollection, uiElementArray); // Selects a combination of Stroke and UIElement objects.
                    break;
            }

            switch (SubActionIndex % 2)
            {
                case 0:
                    //Add a lock to avoid OpenClipboard Failed exception.
                    lock (ClipboardLock)
                    {
                        InkCanvas.CopySelection(); // Copies selected strokes and/or elements to the clipboard.
                    }
                    break;
                case 1:
                    // Close all selected Popup elements.
                    ReadOnlyCollection<UIElement> elements = InkCanvas.GetSelectedElements();
                    foreach (UIElement element in elements)
                    {
                        if (element is Popup)
                        {
                            ((Popup)element).IsOpen = false;
                        }
                    }

                    //Add a lock to avoid OpenClipboard Failed exception.
                    lock (ClipboardLock)
                    {
                        InkCanvas.CutSelection(); // Deletes the selected strokes and/or elements,and copies them to the clipboard.
                    }
                    break;
            }
        }
    }
}
