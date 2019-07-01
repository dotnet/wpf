// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Linq;
using System.Windows.Documents;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Remove a Block from ListItem.
    /// </summary>
    public class ListItemRemoveBlockAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public ListItem Target { get; set; }

        public int RemoveIndex { get; set; }

        #endregion

        #region Override Members

        public override bool CanPerform()
        {
            return Target.Blocks.Count > 0;
        }

        public override void Perform()
        {
            RemoveIndex %= Target.Blocks.Count;
            Block block = Target.Blocks.ElementAtOrDefault<Block>(RemoveIndex);
            Target.Blocks.Remove(block);
        }

        #endregion
    }
}
