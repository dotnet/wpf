// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class RowDefinitionFactory : DiscoverableFactory<RowDefinition>
    {
        #region Public Members

        public GridLength Height { get; set; }

        #endregion

        #region Override Members

        public override RowDefinition Create(DeterministicRandom random)
        {
            RowDefinition rowDefinition = new RowDefinition();
            rowDefinition.Height = Height;
            return rowDefinition;
        }

        #endregion
    }
}
