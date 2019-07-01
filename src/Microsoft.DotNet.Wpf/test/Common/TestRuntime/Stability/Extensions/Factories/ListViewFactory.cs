// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class ListViewFactory : DiscoverableFactory<ListView>
    {
        public GridView GridView { get; set; }

        public override ListView Create(DeterministicRandom random)
        {
            ListView listView = new ListView();
            listView.View = GridView;
            return listView;
        }
    }
}
