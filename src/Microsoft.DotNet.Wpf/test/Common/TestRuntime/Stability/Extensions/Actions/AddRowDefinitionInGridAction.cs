// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Add a RowDefinition in Grid.
    /// </summary>
    public class AddRowDefinitionInGridAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Grid Grid { get; set; }

        public RowDefinition RowDefinition { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Grid.RowDefinitions.Add(RowDefinition);
        }

        #endregion
    }
}
