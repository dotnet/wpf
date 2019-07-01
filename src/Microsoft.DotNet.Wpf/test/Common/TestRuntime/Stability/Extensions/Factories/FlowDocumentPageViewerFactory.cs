// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(FlowDocumentPageViewer))]
    class FlowDocumentPageViewerFactory : DiscoverableFactory<FlowDocumentPageViewer>
    {
        public FlowDocument FlowDocument { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double MinZoom { get; set; }

        public override FlowDocumentPageViewer Create(DeterministicRandom random)
        {
            FlowDocumentPageViewer flowDocumentPageViewer = new FlowDocumentPageViewer();
            flowDocumentPageViewer.MinZoom = MinZoom;
            flowDocumentPageViewer.MaxZoom = MinZoom * 50;
            flowDocumentPageViewer.Document = FlowDocument;
            return flowDocumentPageViewer;
        }
    }
}
