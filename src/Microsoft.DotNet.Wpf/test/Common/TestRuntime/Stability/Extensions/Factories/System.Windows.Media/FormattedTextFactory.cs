// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(FormattedText))]
    internal class FormattedTextFactory : DiscoverableFactory<FormattedText>
    {
        #region Public Members

        public Brush Foreground { get; set; }

        public CultureInfo Culture { get; set; }

        public FontFamily FontFamily { get; set; }

        public FontFamily FallbackFontFamily { get; set; }

        public Typeface Typeface { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double EmSize { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public String TextToFormat { get; set; }

        #endregion

        public override FormattedText Create(DeterministicRandom random)
        {
            FlowDirection flowDirection = random.NextEnum<FlowDirection>();

            return new FormattedText(TextToFormat, Culture, flowDirection, Typeface, EmSize, Foreground);
        }
    }
}
