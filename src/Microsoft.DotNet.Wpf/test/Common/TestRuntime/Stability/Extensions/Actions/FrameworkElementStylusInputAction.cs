// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Input;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class FrameworkElementStylusInputAction : SimpleDiscoverableAction
    {
        #region Public Members

        public CaptureMode CaptureMode { get; set; }

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public FrameworkElement Element { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Stylus.Capture(Element, CaptureMode);
        }

        #endregion
    }
}
