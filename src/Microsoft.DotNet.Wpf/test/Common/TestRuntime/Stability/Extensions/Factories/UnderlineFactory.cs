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
    internal class UnderlineFactory : InlineFactory<Underline>
    {
        public List<Inline> Children { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public string Content { get; set; }

        /// <summary>
        /// If rate less than 0.1, we will insert multi inlines in Underline, else we will insert only string content in Underline.
        /// </summary>
        public double MultiInlinesRate { get; set; }

        public override Underline Create(DeterministicRandom random)
        {
            Underline underline = new Underline();

            ApplyInlineProperties(underline, random);
            if (MultiInlinesRate < 0.1)
            {
                HomelessTestHelpers.Merge(underline.Inlines, Children);
            }
            else
            {
                underline.Inlines.Add(Content);
            }

            return underline;
        }
    }
}
