// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class RepeatButtonFactory : ContentControlFactory<RepeatButton>
    {
        public override RepeatButton Create(DeterministicRandom random)
        {
            RepeatButton repeatButton = new RepeatButton();
            ApplyContentControlProperties(repeatButton);
            repeatButton.Delay = random.Next(1000);
            // Interval need > 0
            repeatButton.Interval = random.Next(500) + 1;
            return repeatButton;
        }
    }
}
