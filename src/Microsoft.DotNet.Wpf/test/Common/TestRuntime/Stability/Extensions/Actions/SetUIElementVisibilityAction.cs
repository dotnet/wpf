// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class SetUIElementVisibilityAction : SimpleDiscoverableAction
    {
        #region Public Members

        public UIElement Element { get; set; }

        public Visibility Visibility { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Element.Visibility = Visibility;
        }

        #endregion
    }
}
