// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(RadioButton))]
    class RadioButtonFactory : ToggleButtonFactory<RadioButton>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public String GroupName { get; set; }

        public override RadioButton Create(DeterministicRandom random)
        {
            RadioButton radioButton = new RadioButton();
            ApplyToggleButtonProperties(radioButton, random);
            radioButton.GroupName = GroupName;
            return radioButton;
        }
    }
}
