// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class ExpanderFactory : HeaderedContentControlFactory<Expander>
    {
        public override Expander Create(Microsoft.Test.Stability.Core.DeterministicRandom random)
        {
            Expander expander = new Expander();
            ApplyHeaderedContentControlProperties(expander);
            expander.ExpandDirection = random.NextEnum<ExpandDirection>();
            expander.IsExpanded = random.NextBool();
            return expander;
        }
    }
}
