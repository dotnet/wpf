// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(FixedPage))]
    class FixedPageFactory : DiscoverableFactory<FixedPage>
    {
        public List<UIElement> Children { get; set; }
        public Brush Background { get; set; }
        public object PrintTicket { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double ContentBoxLeft { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double ContentBoxTop { get; set; }

        public override FixedPage Create(DeterministicRandom random)
        {
            FixedPage fixedPage = new FixedPage();

            HomelessTestHelpers.Merge(fixedPage.Children, HomelessTestHelpers.FilterListOfType(Children, typeof(ContextMenu)));
            fixedPage.Background = Background;

            double contentBoxRight = ContentBoxLeft + 1 + random.NextDouble() * 1000;
            double contentBoxBottom = ContentBoxTop + 1 + random.NextDouble() * 1000;
            fixedPage.ContentBox = new Rect(ContentBoxLeft, ContentBoxTop, contentBoxRight - ContentBoxLeft, contentBoxBottom - ContentBoxTop);

            double bleedBoxLeft = 0.9 * ContentBoxLeft;
            double bleedBoxTop = 0.9 * ContentBoxTop;
            double bleedBoxRight = 1.1 * contentBoxRight;
            double bleedBoxBottom = 1.1 * contentBoxBottom;
            fixedPage.BleedBox = new Rect(bleedBoxLeft, bleedBoxTop, bleedBoxRight - bleedBoxLeft, bleedBoxBottom - bleedBoxTop);

            fixedPage.PrintTicket = PrintTicket;

            return fixedPage;
        }

        public override bool CanCreate(Type desiredType)
        {
            return desiredType == typeof(FixedPage);
        }
    }
}
