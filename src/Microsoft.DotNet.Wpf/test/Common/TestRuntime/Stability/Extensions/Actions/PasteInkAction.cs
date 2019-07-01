// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Pastes the contents of the Clipboard to the InkCanvas at a given point.
    /// </summary>
    public class PasteInkAction : ClipboardLockBaseAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas InkCanvas { get; set; }

        public double RandomX { get; set; }

        public double RandomY { get; set; }

        public override void Perform()
        {
            //The point is from the InkCanvas.
            Point point = new Point(InkCanvas.ActualWidth * RandomX, InkCanvas.ActualHeight * RandomY);

            MemoryStream stream = new MemoryStream();
            InkCanvas.Strokes.Save(stream);
            stream.Seek(0, SeekOrigin.Begin);

            // Add a lock to avoid OpenClipboard Failed exception.
            lock (ClipboardLock)
            {
                Clipboard.SetData(StrokeCollection.InkSerializedFormat, stream);
                //Pastes the contents of the clipboard to the InkCanvas at a given point.
                InkCanvas.Paste(point);
            }

            stream.Close();
        }
    }
}
