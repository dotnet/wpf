// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Add a ColumnDefinition in Grid.
    /// </summary>
    public class AddColumnDefinitionInGridAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Grid Grid { get; set; }

        public ColumnDefinition ColumnDefinition { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Grid.ColumnDefinitions.Add(ColumnDefinition);
        }

        #endregion
    }
}
