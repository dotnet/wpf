// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(ComboBox))]
    class ComboBoxFactory : SelectorFactory<ComboBox>
    {
        public double MaxDropDownHeight { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public String Text { get; set; }

        public override ComboBox Create(DeterministicRandom random)
        {
            ComboBox comboBox = new ComboBox();
            ApplySelectorProperties(comboBox, random);
            comboBox.IsDropDownOpen = random.NextBool();
            comboBox.IsEditable = random.NextBool();
            comboBox.MaxDropDownHeight = MaxDropDownHeight;
            comboBox.StaysOpenOnEdit = random.NextBool();
            comboBox.Text = Text;
            return comboBox;
        }
    }
}
