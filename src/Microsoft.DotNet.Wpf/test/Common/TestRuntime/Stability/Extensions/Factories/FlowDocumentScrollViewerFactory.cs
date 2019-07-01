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
    [TargetTypeAttribute(typeof(FlowDocumentScrollViewer))]
    class FlowDocumentScrollViewerFactory : DiscoverableFactory<FlowDocumentScrollViewer>
    {
        public FlowDocument FlowDocument { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double MinZoom { get; set; }

        public override FlowDocumentScrollViewer Create(DeterministicRandom random)
        {
            FlowDocumentScrollViewer flowDocumentScrollViewer = new FlowDocumentScrollViewer();
            flowDocumentScrollViewer.MinZoom = MinZoom;
            flowDocumentScrollViewer.MaxZoom = MinZoom * 50;
            flowDocumentScrollViewer.IsSelectionEnabled = random.NextBool();
            flowDocumentScrollViewer.IsToolBarVisible = random.NextBool();
            flowDocumentScrollViewer.HorizontalScrollBarVisibility = random.NextEnum<ScrollBarVisibility>();
            flowDocumentScrollViewer.VerticalScrollBarVisibility = random.NextEnum<ScrollBarVisibility>();
            flowDocumentScrollViewer.Document = FlowDocument;
            return flowDocumentScrollViewer;
        }
    }
}
