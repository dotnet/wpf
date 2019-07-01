// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Media;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Modifies each of the StylusPoints and optionally the StylusTipTransform for each stroke in the StrokeCollection according to the specified Matrix.
    /// </summary>
    public class TransformInkAction : SimpleDiscoverableAction
    {
        public Matrix Matrix { get; set; }

        public bool ApplyToStylusTip { get; set; }

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas InkCanvas { get; set; }

        public override void Perform()
        {
            if (Matrix.HasInverse)
            {
                InkCanvas.Strokes.Transform(Matrix, ApplyToStylusTip);
            }
        }
    }
}
