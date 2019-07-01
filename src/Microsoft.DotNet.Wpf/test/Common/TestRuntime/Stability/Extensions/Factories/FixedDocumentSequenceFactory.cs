// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class FixedDocumentSequenceFactory : DiscoverableFactory<FixedDocumentSequence>
    {
        public object PrintTicket { get; set; }
        // FixedDocumentSequence must contain at least one FixedDocument.
        [InputAttribute(ContentInputSource.CreateFromFactory, MinListSize = 1)]
        public List<DocumentReference> Children { get; set; }

        public override FixedDocumentSequence Create(DeterministicRandom random)
        {
            FixedDocumentSequence fixedDocumentSequence = new FixedDocumentSequence();
            Merge(fixedDocumentSequence.References, Children);
            fixedDocumentSequence.PrintTicket = PrintTicket;
            return fixedDocumentSequence;
        }

        private void Merge(DocumentReferenceCollection destination, List<DocumentReference> source)
        {
            if (source != null && destination != null)
            {
                foreach (DocumentReference o in source)
                {
                    destination.Add(o);
                }
            }
        }

        public override bool CanCreate(Type desiredType)
        {
            return desiredType == typeof(FixedDocumentSequence) || desiredType == typeof(IDocumentPaginatorSource);
        }
    }
}
