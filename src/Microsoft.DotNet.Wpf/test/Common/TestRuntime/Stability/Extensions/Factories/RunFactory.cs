// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(Run))]
    class RunFactory : InlineFactory<Run>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public String Text { get; set; }

        public override Run Create(DeterministicRandom random)
        {
            Run run = new Run();
            ApplyInlineProperties(run, random);
            run.Text = Text;
            return run;
        }
    }
}
