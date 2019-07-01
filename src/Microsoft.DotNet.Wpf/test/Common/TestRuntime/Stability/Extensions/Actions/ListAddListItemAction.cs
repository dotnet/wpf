// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Documents;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// List add a ListItem.
    /// </summary>
    public class ListAddListItemAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public List Target { get; set; }

        public ListItem item { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Target.ListItems.Add(item);
        }

        #endregion
    }
}
