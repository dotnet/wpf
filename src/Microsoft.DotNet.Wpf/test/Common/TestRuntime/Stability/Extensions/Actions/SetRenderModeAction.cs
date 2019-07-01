// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class SetRenderModeAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Visual visual { get; set; }

        public double OccurrenceIndicator { get; set; }
        public bool SetProcessRenderMode { get; set; }
        public RenderMode MyRenderMode { get; set; }

        #region IAction Members

        public override bool CanPerform()
        {
            //Do action only when OccurrenceIndicator is less than 0.002. (1/500)
            return OccurrenceIndicator < 0.002;
        }

        public override void Perform()
        {          
#if TESTBUILD_CLR40
            if (SetProcessRenderMode)
            {
                RenderOptions.ProcessRenderMode = MyRenderMode;
            }
            else
#endif
            {
                HwndSource hwndSource = PresentationSource.FromVisual(visual) as HwndSource;
                if (hwndSource != null)
                {
                    HwndTarget hwndTarget = hwndSource.CompositionTarget;
                    if (hwndTarget != null)
                    {
                        hwndTarget.RenderMode = MyRenderMode; 
                    }
                }
            }
        }

        #endregion
    }
}
