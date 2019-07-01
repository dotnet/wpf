// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Change a ColumnDefinition or RowDefinition SharedSizeGroup in Grid.
    /// </summary>
    public class ChangeSharedSizeGroupAction : SimpleDiscoverableAction
    {
        #region Pubilc Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Grid Grid { get; set; }

        public int SharedSizeGroupIndex { get; set; }

        public int DefinitionIndex { get; set; }

        public bool IsColumnDefinition { get; set; }

        #endregion

        #region Override Members

        public override bool CanPerform()
        {
            return !(Grid.ColumnDefinitions.Count == 0 && Grid.RowDefinitions.Count == 0);
        }

        public override void Perform()
        {
            if (Grid.ColumnDefinitions.Count == 0)
            {
                IsColumnDefinition = false;
            }

            if (Grid.RowDefinitions.Count == 0)
            {
                IsColumnDefinition = true;
            }

            DefinitionBase definitionBase;
            if (IsColumnDefinition)
            {
                DefinitionIndex %= Grid.ColumnDefinitions.Count;
                definitionBase = Grid.ColumnDefinitions[DefinitionIndex];
            }
            else
            {
                DefinitionIndex %= Grid.RowDefinitions.Count;
                definitionBase = Grid.RowDefinitions[DefinitionIndex];
            }

            //Randomly choose Group0 to Group19.
            string SharedSizeGroupName = "Group" + SharedSizeGroupIndex % 20;
            definitionBase.SharedSizeGroup = SharedSizeGroupName;
        }

        #endregion
    }
}
