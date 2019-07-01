// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(DocumentReference))]
    class DocumentReferenceFactory : DiscoverableFactory<DocumentReference>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Uri Uri { get; set; }

        public override DocumentReference Create(DeterministicRandom random)
        {
            DocumentReference documentReference = new DocumentReference();
            documentReference.Source = Uri;
            return documentReference;
        }

        public override bool CanCreate(Type desiredType)
        {
            return desiredType == typeof(DocumentReference);
        }
    }
}
