// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(Paragraph))]
    class ParagraphFactory : BlockFactory<Paragraph>
    {
        public Boolean KeepTogether { get; set; }
        public Boolean KeepWithNext { get; set; }
        public TextDecorationCollection TextDecorations { get; set; }
        public List<Inline> Children { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 MinOrphanLines { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 MinWidowLines { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double TextIndent { get; set; }

        public override Paragraph Create(DeterministicRandom random)
        {
            Paragraph paragraph = new Paragraph();
            ApplyBlockProperties(paragraph, random);
            HomelessTestHelpers.Merge(paragraph.Inlines, Children); 
            paragraph.KeepTogether = KeepTogether;
            paragraph.KeepWithNext = KeepWithNext;
            paragraph.MinOrphanLines = MinOrphanLines;
            paragraph.MinWidowLines = MinWidowLines;
            paragraph.TextDecorations = TextDecorations;
            paragraph.TextIndent = TextIndent;
            return paragraph;
        }
    }
}
