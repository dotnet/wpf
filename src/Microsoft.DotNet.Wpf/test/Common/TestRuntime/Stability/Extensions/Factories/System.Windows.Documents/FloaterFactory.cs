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
    internal class FloaterFactory : AnchoredBlockFactory<Floater>
    {
        public override Floater Create(DeterministicRandom random)
        {
            Floater floater = new Floater();

            ApplyAnchoredBlockProperties(floater, random);
            floater.HorizontalAlignment = random.NextEnum<HorizontalAlignment>();
            floater.Width = random.NextDouble() * 300;

            return floater;
        }
    }
}
