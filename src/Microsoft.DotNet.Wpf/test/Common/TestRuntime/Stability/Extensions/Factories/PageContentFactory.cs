// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Documents;
using System.Windows.Markup;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(PageContent))]
    internal class PageContentFactory : DiscoverableFactory<PageContent>
    {
        public FixedPage FixedPage { get; set; }

        public override PageContent Create(DeterministicRandom random)
        {
            PageContent pageContent = new PageContent();

            if (FixedPage != null)
            {
                ((IAddChild)pageContent).AddChild(FixedPage);  // Cast is needed because IAddChild interface isn’t publicly exposed directly on PageContent
            }

            return pageContent;
        }

        public override bool CanCreate(Type desiredType)
        {
            return desiredType == typeof(PageContent);
        }
    }
}
