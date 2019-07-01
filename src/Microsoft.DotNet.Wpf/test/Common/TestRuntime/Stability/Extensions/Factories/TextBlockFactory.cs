// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(TextBlock))]
    class TextBlockFactory : DiscoverableFactory<TextBlock>
    {
        public List<Inline> Children { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public String Text { get; set; }

        public override TextBlock Create(DeterministicRandom random)
        {
            TextBlock textBlock = new TextBlock();

            if (random.NextBool())
            {
                textBlock.Text = Text;
            }
            else
            {
                //HACK: Floater and Figure types are not valid as children of TextBlock. 
                //Elements in the collection need to satisfy InlineCollection.ValidateChild behavior. This step filters those types
                List<Inline> filteredChildren = HomelessTestHelpers.FilterListOfType(Children, typeof(Floater));
                filteredChildren = HomelessTestHelpers.FilterListOfType(filteredChildren, typeof(Figure));

                // assign filtered inlines collection to the TextBlock. 
                HomelessTestHelpers.Merge(textBlock.Inlines, filteredChildren);
            }
            
            return textBlock;
        }
    }
}
