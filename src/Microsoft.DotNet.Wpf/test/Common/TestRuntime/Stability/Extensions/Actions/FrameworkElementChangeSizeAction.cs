// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class FrameworkElementChangeSizeAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public FrameworkElement Element { get; set; }

        public int Height { get; set; }

        public int Width { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Element.Height = Height % 2000;
            Element.Width = Width % 2000;
        }

        #endregion
    }
}
