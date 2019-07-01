// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class GroupBoxFactory : HeaderedContentControlFactory<GroupBox>
    {
        public override GroupBox Create(DeterministicRandom random)
        {
            GroupBox groupBox = new GroupBox();
            ApplyHeaderedContentControlProperties(groupBox);
            return groupBox;
        }
    }
}
