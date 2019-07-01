// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Removing an UIElement from Panel Action.
    /// </summary>
    public class RemovePanelChildAction : SimpleDiscoverableAction
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the Panel which UIElement is added to.
        /// </summary>
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Panel Target { get; set; }

        public int ChildIndex { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Removes an UIElement from Panel.
        /// </summary>
        public override void Perform()
        {
            int count = Target.Children.Count;
            if (count > 0)
            {
                Target.Children.RemoveAt(ChildIndex % count);
            }
        }

        #endregion
    }
}
