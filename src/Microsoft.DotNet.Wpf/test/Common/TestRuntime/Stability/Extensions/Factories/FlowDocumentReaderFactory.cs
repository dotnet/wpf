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
    [TargetTypeAttribute(typeof(FlowDocumentReader))]
    class FlowDocumentReaderFactory : DiscoverableFactory<FlowDocumentReader>
    {
        public FlowDocument FlowDocument { get; set; }
        public Boolean IsPageViewEnabled { get; set; }
        public Boolean IsScrollViewEnabled { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double MinZoom { get; set; }

        public override FlowDocumentReader Create(DeterministicRandom random)
        {
            FlowDocumentReader flowDocumentReader = new FlowDocumentReader();
            flowDocumentReader.MinZoom = MinZoom;
            flowDocumentReader.MaxZoom = MinZoom * 50;
            flowDocumentReader.IsFindEnabled = random.NextBool();
            flowDocumentReader.IsPrintEnabled = random.NextBool();
            flowDocumentReader.ViewingMode = random.NextEnum<FlowDocumentReaderViewingMode>();
            flowDocumentReader.Document = FlowDocument;
            return flowDocumentReader;
        }
    }
}
