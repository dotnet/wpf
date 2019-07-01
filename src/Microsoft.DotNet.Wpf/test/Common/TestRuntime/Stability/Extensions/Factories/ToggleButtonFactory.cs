// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
   public abstract class ToggleButtonFactory<T> : ContentControlFactory<T> where T : ToggleButton
    {
        protected void ApplyToggleButtonProperties(T toggleButton, DeterministicRandom random)
        {
            ApplyContentControlProperties(toggleButton);
            toggleButton.IsChecked = random.NextBool();
            toggleButton.IsThreeState = random.NextBool();
        }
    }
}
