// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(GridViewColumn))]
    class GridViewColumnFactory : DiscoverableFactory<GridViewColumn>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public String Header { get; set; }

        public override GridViewColumn Create(DeterministicRandom random)
        {
            GridViewColumn gridViewColumn = new GridViewColumn();
            gridViewColumn.Header = Header;
            //TODO: Set up the binding property(gridViewColumn.DisplayMemberBinding)
            return gridViewColumn;
        }
    }
}
