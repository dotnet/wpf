// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class FixedDocumentDocumentViewerFactory : DiscoverableFactory<DocumentViewer>
    {
        public FixedDocument FixedDocument { get; set; }

        public override DocumentViewer Create(DeterministicRandom random)
        {
            DocumentViewer documentViewer = new DocumentViewer();
            documentViewer.Document = FixedDocument;
            return documentViewer;
        }
    }

    class FixedDocumentSequenceDocumentViewerFactory : DiscoverableFactory<DocumentViewer>
    {
        public FixedDocumentSequence FixedDocumentSequence { get; set; }

        public override DocumentViewer Create(DeterministicRandom random)
        {
            DocumentViewer documentViewer = new DocumentViewer();
            documentViewer.Document = FixedDocumentSequence;
            return documentViewer;
        }
    }
}
