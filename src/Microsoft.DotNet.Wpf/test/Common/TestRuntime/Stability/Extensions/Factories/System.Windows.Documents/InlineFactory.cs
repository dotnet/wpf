// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows;
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal abstract class InlineFactory<InlineType> : TextElementFactory<InlineType> where InlineType : Inline
    {
        #region Public Members

        public Object Tag { get; set; }

        public TextDecorationCollection TextDecorations { get; set; }

        #endregion

        #region Override Members

        protected void ApplyInlineProperties(InlineType inline, DeterministicRandom random)
        {
            ApplyTextElementFactory(inline, random);
            inline.BaselineAlignment = random.NextEnum<BaselineAlignment>();
            inline.FlowDirection = random.NextEnum<FlowDirection>();
            inline.Tag = Tag;
            inline.TextDecorations = TextDecorations;
        }

        #endregion
    }
}
