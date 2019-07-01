// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Set DrawingImage's content with a Drawing.
    /// </summary>
    public class DrawingImageAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree, IsEssentialContent = true)]
        public Image TargetImage { get; set; }

        public Drawing Drawing { get; set; }

        public override void Perform()
        {
            ((DrawingImage)(TargetImage.Source)).Drawing = Drawing;
        }

        /// <summary>
        /// Only can perform when the Image's Source type is DrawingImage
        /// </summary>
        public override bool CanPerform()
        {
            if (TargetImage.Source != null)
            {
                return TargetImage.Source.GetType() == typeof(DrawingImage);
            }
            else
            {
                return false;
            }
        }
    }
}
