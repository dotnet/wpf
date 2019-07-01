// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(Block))]      
    internal class SectionFactory : BlockFactory<Section>
    {
        public List<Block> Children { get; set; }

        public override Section Create(DeterministicRandom random)
        {
            Section section = new Section();
            ApplyBlockProperties(section, random);
            HomelessTestHelpers.Merge(section.Blocks, Children);
            section.HasTrailingParagraphBreakOnPaste = random.NextBool();
            return section;
        }
    }
}
