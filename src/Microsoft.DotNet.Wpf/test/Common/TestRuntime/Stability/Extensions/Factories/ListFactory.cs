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
    [TargetTypeAttribute(typeof(List))]
    internal class ListFactory : BlockFactory<List>
    {
        public List<ListItem> Children { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double MarkerOffset { get; set; }

        public override List Create(DeterministicRandom random)
        {
            List list = new List();
            ApplyBlockProperties(list, random);
            HomelessTestHelpers.Merge(list.ListItems, Children);
            list.MarkerOffset = MarkerOffset;
            //HACK: Work around bug 890382: WPFStress:System.NotSupportException@MS.Internal.TextFormatting.TextMetrics.FullTextLine.FormatLine
            list.TextEffects = null;

            list.MarkerStyle = random.NextEnum<TextMarkerStyle>();
            list.StartIndex = random.Next(list.ListItems.Count) + 1;
            return list;
        }
    }
}
