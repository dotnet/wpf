// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class ProgressBarFactory : RangeBaseFactory<ProgressBar>
    {
        public override ProgressBar Create(DeterministicRandom random)
        {
            ProgressBar progressBar = new ProgressBar();
            ApplyRangeBaseProperties(progressBar, random);
            progressBar.IsIndeterminate = random.NextBool();
            progressBar.Orientation = random.NextEnum<Orientation>();
            return progressBar;
        }
    }
}
