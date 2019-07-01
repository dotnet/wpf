// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(TextElement))]
    internal class FigureFactory : AnchoredBlockFactory<Figure>
    {
        #region Public Members

        public FigureLength Height { get; set; }

        public FigureLength Width { get; set; }

        #endregion

        #region Override Members

        public override Figure Create(DeterministicRandom random)
        {
            Figure figure = new Figure();

            ApplyAnchoredBlockProperties(figure, random);
            figure.CanDelayPlacement = random.NextBool();
            figure.HorizontalAnchor = random.NextEnum<FigureHorizontalAnchor>();
            figure.VerticalAnchor = random.NextEnum<FigureVerticalAnchor>();
            figure.HorizontalOffset = random.NextDouble() * 10;
            figure.VerticalOffset = random.NextDouble() * 10;
            figure.Width = Width;
            figure.Height = Height;
            figure.WrapDirection = random.NextEnum<WrapDirection>();

            return figure;
        }

        #endregion
    }
}
