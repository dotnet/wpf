// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class GridViewFactory : DiscoverableFactory<GridView>
    {
        public List<GridViewColumn> Children { get; set; }

        public override GridView Create(DeterministicRandom random)
        {
            GridView gridView = new GridView();
            HomelessTestHelpers.Merge(gridView.Columns, HomelessTestHelpers.FilterListOfType(Children, typeof(ContextMenu)));
            return gridView;
        }
    }
}
