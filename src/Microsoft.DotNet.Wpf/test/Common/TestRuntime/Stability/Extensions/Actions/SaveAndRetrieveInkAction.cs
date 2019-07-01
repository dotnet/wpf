// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.IO;
using System.Windows;
using System.Windows.Ink;
using Microsoft.Test.Graphics;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Save and retrieve ink to StrokeCollection
    /// </summary>
    public class SaveAndRetrieveInkAction : SimpleDiscoverableAction
    {
        public StrokeCollection StrokeCollection { get; set; }

        public int OptionIndex { get; set; }

        public bool compress { get; set; }

        public override void Perform()
        {
            MemoryStream stream = new MemoryStream();
            StrokeCollection.Save(stream, compress);

            //Set the position to the beginning of the stream
            stream.Seek(0, SeekOrigin.Begin);

            StrokeCollection loadStrokes = new StrokeCollection(stream);

            /********** workaround bug 894129 **************
            //Compare the current StrokeCollection with the original StrokeCollection to check if they are the same.
            if (!CompareStrokeCollection(StrokeCollection, loadStrokes))
            {
                throw new Exception("Loaded StrokeCollection is not the same as the original StrokeCollection");
            }
            *************************************************/

            stream.Close();
        }

        private bool CompareStrokeCollection(StrokeCollection originalStrokeCollection, StrokeCollection currentStrokeCollection)
        {
            if (originalStrokeCollection.Count != currentStrokeCollection.Count)
            {
                return false;
            }

            for (int i = 0; i < originalStrokeCollection.Count; i++)
            {
                if (!CompareStroke(originalStrokeCollection[i], currentStrokeCollection[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CompareStroke(Stroke originalStroke, Stroke currerntStroke)
        {
            if (originalStroke.StylusPoints.Count != currerntStroke.StylusPoints.Count)
            {
                return false;
            }

            for (int i = 0; i < originalStroke.StylusPoints.Count; i++)
            {
                if (!(MathEx.AreCloseEnough((Point)originalStroke.StylusPoints[i], (Point)currerntStroke.StylusPoints[i])))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
