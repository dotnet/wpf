// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(TextDecoration))]
    class TextDecorationFactory : DiscoverableFactory<TextDecoration>
    {
        public Pen Pen { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double PenOffset { get; set; }

        public override TextDecoration Create(DeterministicRandom random)
        {
            TextDecoration textDecoration = new TextDecoration();
            textDecoration.Location = random.NextEnum<TextDecorationLocation>();
            textDecoration.Pen = Pen;
            textDecoration.PenOffset = PenOffset;
            textDecoration.PenOffsetUnit = random.NextEnum<TextDecorationUnit>();
            textDecoration.PenThicknessUnit = random.NextEnum<TextDecorationUnit>();
            return textDecoration;
        }
    }

    class TextDecorationCollectionFactory : DiscoverableCollectionFactory<TextDecorationCollection, TextDecoration> { }
}
