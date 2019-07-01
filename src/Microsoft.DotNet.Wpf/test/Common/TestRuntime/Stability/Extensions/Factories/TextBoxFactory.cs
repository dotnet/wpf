// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(TextBox))]
    class TextBoxFactory : TextBoxBaseFactory<TextBox>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public String Text { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 MinLines { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 MaxLength { get; set; }

        public override TextBox Create(DeterministicRandom random)
        {
            TextBox textBox = new TextBox();
            ApplyTextBoxBaseProperties(textBox, random);
            textBox.MinLines = MinLines;
            textBox.MaxLines = MinLines + random.Next(50);
            textBox.MaxLength = MaxLength;
            textBox.Text = Text;
            return textBox;
        }
    }
}
