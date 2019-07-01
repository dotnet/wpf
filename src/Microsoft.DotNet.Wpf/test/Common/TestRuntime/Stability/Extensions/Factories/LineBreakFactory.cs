// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(TextElement))]     
    class LineBreakFactory : InlineFactory<LineBreak>
    {
        public override LineBreak Create(DeterministicRandom random)
        {
            LineBreak lineBreak = new LineBreak();
            ApplyInlineProperties(lineBreak, random);
            return lineBreak;
        }
    }
}
