// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class RichTextBoxFactory : TextBoxBaseFactory<RichTextBox>
    {
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent=true)]
        public FlowDocument FlowDocument { get; set; }

        public override RichTextBox Create(DeterministicRandom random)
        {
            RichTextBox richTextBox = new RichTextBox();
            ApplyTextBoxBaseProperties(richTextBox, random);
            richTextBox.Document = FlowDocument;
            return richTextBox;
        }
    }
}
