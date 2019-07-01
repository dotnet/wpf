// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Remove a RowDefinition in Grid.
    /// </summary>
    public class RemoveRowDefinitionInGridAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromVisualTree)]
        public Grid Grid { get; set; }

        public int RemoveIndex { get; set; }

        #endregion

        #region Override Members

        public override bool CanPerform()
        {
            return Grid.RowDefinitions.Count > 0;
        }

        public override void Perform()
        {
            RemoveIndex %= Grid.RowDefinitions.Count;
            Grid.RowDefinitions.RemoveAt(RemoveIndex);
        }

        #endregion
    }
}
