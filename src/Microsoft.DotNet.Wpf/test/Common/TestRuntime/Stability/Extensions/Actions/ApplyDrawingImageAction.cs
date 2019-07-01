// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Media;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Use an Image control to display a DrawingImage 
    /// </summary>
    public class ApplyDrawingImageAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Image Target { get; set; }

        public DrawingImage DrawingImage { get; set; }

        public override void Perform()
        {
            Target.Source = DrawingImage;
        }
    }
}
