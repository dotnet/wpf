// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Set Page as Window Content Action
    /// </summary>
    public class SetWindowContentUsingPageAction : SimpleDiscoverableAction
    {
        #region Public Members

        /// <summary>
        /// Gets or sets the possibility of performing this action.
        /// </summary>
        public double OccurrenceIndicator { get; set; }

        /// <summary>
        /// Gets or sets the window which content is set. 
        /// </summary>
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Window Target { get; set; }

        /// <summary>
        /// Gets or sets the page to set window content.
        /// </summary>
        public Page Page { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Do action only when OccurrenceIndicator is less than 0.05(1/20). 
        /// </summary>
        public override bool CanPerform()
        {
            return OccurrenceIndicator < 0.05;
        }

        /// <summary>
        /// Set Page as Window Content
        /// </summary>
        public override void Perform()
        {
            Target.Content = Page;
        }

        #endregion
    }
}
