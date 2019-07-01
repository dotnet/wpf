// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Input;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class UIElementCaptureMouseAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public UIElement Element { get; set; }

        public bool IsCaptureMouse { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            if (IsCaptureMouse)
            {
                Element.CaptureMouse();
            }
            else
            {
                Mouse.Capture(null);//Release Capture everywhere.
            }
        }

        #endregion
    }
}
