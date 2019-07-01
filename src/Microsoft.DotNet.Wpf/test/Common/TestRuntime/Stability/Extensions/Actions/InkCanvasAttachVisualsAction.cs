// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Input.StylusPlugIns;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Attaches the visual of a DynamicRenderer to an InkPresenter. To render ink dynamically.
    /// </summary>
    public class InkCanvasAttachVisualsAction : SimpleDiscoverableAction
    {
        public DynamicRenderer DynamicRenderer { get; set; }

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public CustomInkCanvas TargetInkCanvas { get; set; }

        public override void Perform()
        {
            InkPresenter inkPresenter = TargetInkCanvas.CustomInkPresenter;
            inkPresenter.AttachVisuals(DynamicRenderer.RootVisual, DynamicRenderer.DrawingAttributes);
            TargetInkCanvas.StylusPlugInCollection.Add(DynamicRenderer);
        }
    }
}
