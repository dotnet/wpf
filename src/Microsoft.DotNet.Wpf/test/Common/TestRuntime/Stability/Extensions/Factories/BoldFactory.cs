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
    internal class BoldFactory : InlineFactory<Bold>
    {
        public List<Inline> Children { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public string Content { get; set; }

        /// <summary>
        /// If rate less than 0.1, we will insert multi inlines in Bold, else we will insert only string content in Bold.
        /// </summary>
        public double MultiInlinesRate { get; set; }

        public override Bold Create(DeterministicRandom random)
        {
            Bold bold = new Bold();
            ApplyInlineProperties(bold, random);
            if (MultiInlinesRate < 0.1)
            {
                HomelessTestHelpers.Merge(bold.Inlines, Children);
            }
            else
            {
                bold.Inlines.Add(Content);
            }

            return bold;
        }
    }
}
