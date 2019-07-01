// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Input.StylusPlugIns;

namespace Microsoft.Test.Stability.Extensions.CustomTypes
{
    public class CustomInkCanvas : InkCanvas
    {
        public CustomInkCanvas() { }

        public InkPresenter CustomInkPresenter
        {
            get { return this.InkPresenter; }
        }

        public StylusPlugInCollection StylusPlugInCollection
        {
            get { return this.StylusPlugIns; }
        }

        public DynamicRenderer CustomDynamicRenderer
        {
            get { return this.DynamicRenderer; }
            set { this.DynamicRenderer = value; }
        }
    }
}
