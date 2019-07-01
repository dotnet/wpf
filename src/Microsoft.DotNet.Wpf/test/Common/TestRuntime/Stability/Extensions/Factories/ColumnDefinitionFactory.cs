// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class ColumnDefinitionFactory : DiscoverableFactory<ColumnDefinition>
    {
        #region Public Members

        public GridLength Width { get; set; }

        #endregion

        #region Override Members

        public override ColumnDefinition Create(DeterministicRandom random)
        {
            ColumnDefinition columnDefinition = new ColumnDefinition();
            columnDefinition.Width = Width;
            return columnDefinition;
        }

        #endregion
    }
}
