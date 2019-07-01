// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Remove a ColumnDefinition in Grid.
    /// </summary>
    public class RemoveColumnDefinitionInGridAction : SimpleDiscoverableAction
    {
        #region Public Members

        public Grid Grid { get; set; }

        public int RemoveIndex { get; set; }

        #endregion

        #region Override Members

        public override bool CanPerform()
        {
            return Grid.ColumnDefinitions.Count > 0;
        }

        public override void Perform()
        {
            RemoveIndex %= Grid.ColumnDefinitions.Count;
            Grid.ColumnDefinitions.RemoveAt(RemoveIndex);
        }

        #endregion
    }
}
