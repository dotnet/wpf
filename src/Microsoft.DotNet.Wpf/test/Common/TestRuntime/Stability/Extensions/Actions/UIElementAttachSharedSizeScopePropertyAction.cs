// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Attach SharedSizeScopeProperty to an UIElement.
    /// </summary>
    public class UIElementAttachSharedSizeScopePropertyAction : SimpleDiscoverableAction
    {
        #region Pubilc Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public UIElement Target { get; set; }

        public bool IsSharedSizeScope { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Grid.SetIsSharedSizeScope(Target, IsSharedSizeScope);
        }

        #endregion
    }
}
