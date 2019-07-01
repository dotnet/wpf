// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Reformat points and the strokes points so they match, then add them together.
    /// </summary>
    public class StylusPointCollectionReformatAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas InkCanvas { get; set; }

        public int StrokeIndex { get; set; }

        public StylusPointDescription StylusPointDescription { get; set; }

        public StylusPointCollection StylusPointCollection { get; set; }

        public override void Perform()
        {
            Stroke stroke = InkCanvas.Strokes[StrokeIndex % InkCanvas.Strokes.Count];
            StylusPointDescription commonSPD = StylusPointDescription.GetCommonDescription(stroke.StylusPoints.Description, StylusPointDescription);
            //reformat points and the strokes points so they match, then add them together.
            StylusPointCollection spcReformat = StylusPointCollection.Reformat(commonSPD);
            StylusPointCollection strokeReformat = stroke.StylusPoints.Reformat(commonSPD);
            stroke.StylusPoints = strokeReformat;
            stroke.StylusPoints.Add(spcReformat);
        }

        public override bool CanPerform()
        {
            return InkCanvas.Strokes.Count > 0;
        }
    }
}
