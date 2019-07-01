// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Input.StylusPlugIns;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Detaches the visual of the DynamicRenderer from the InkPresenter.
    /// </summary>
    public class InkCanvasDetachVisualsAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public CustomInkCanvas TargetInkCanvas { get; set; }

        public int CountIndex { get; set; }

        public override void Perform()
        {
            if (TargetInkCanvas.StylusPlugInCollection.Count > 0)
            {
                InkPresenter inkPresenter = TargetInkCanvas.CustomInkPresenter;

                int index = CountIndex % TargetInkCanvas.StylusPlugInCollection.Count;
                DynamicRenderer dynamicRenderer = TargetInkCanvas.StylusPlugInCollection[index] as DynamicRenderer;
                if (dynamicRenderer == null)
                {
                    return;
                }

                if (dynamicRenderer != TargetInkCanvas.CustomDynamicRenderer)
                {
                    inkPresenter.DetachVisuals(dynamicRenderer.RootVisual);
                    TargetInkCanvas.StylusPlugInCollection.Remove(dynamicRenderer);
                }
                else
                {
                    // A sign of gotten a stylus plug-in object
                    bool foundOne = false;
                    foreach (DynamicRenderer plugin in TargetInkCanvas.StylusPlugInCollection)
                    {
                        if (plugin != dynamicRenderer)
                        {
                            TargetInkCanvas.CustomDynamicRenderer = plugin;
                            foundOne = true;
                            break;
                        }
                    }
                    if (!foundOne)
                    {
                        TargetInkCanvas.CustomDynamicRenderer = null;
                    }
                }
            }
        }
    }
}
