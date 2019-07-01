// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(TextElement))]     
    internal class SpanFactory : InlineFactory<Span>
    {
        public List<Inline> Children { get; set; }

        public override Span Create(DeterministicRandom random)
        {
            Span span = new Span();

            ApplyInlineProperties(span, random);
            HomelessTestHelpers.Merge(span.Inlines, Children);

            return span;
        }
    }
}
