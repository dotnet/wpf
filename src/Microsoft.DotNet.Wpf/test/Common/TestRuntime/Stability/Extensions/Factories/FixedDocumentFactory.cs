// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(FixedDocument))]
    class FixedDocumentFactory : DiscoverableFactory<FixedDocument>
    {
        public object PrintTicket { get; set; }
        // FixedDocument must contain at least one PageContent.
        [InputAttribute(ContentInputSource.CreateFromFactory, MinListSize = 1)]
        public List<PageContent> Children { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Size PageSize { get; set; }

        public override FixedDocument Create(DeterministicRandom random)
        {
            FixedDocument fixedDocument = new FixedDocument();
            HomelessTestHelpers.Merge(fixedDocument.Pages, Children);
            fixedDocument.DocumentPaginator.PageSize = PageSize;
            fixedDocument.PrintTicket = PrintTicket;
            return fixedDocument;
        }

        public override bool CanCreate(Type desiredType)
        {
            return desiredType == typeof(FixedDocument) || desiredType == typeof(IDocumentPaginatorSource);
        }
    }
}
