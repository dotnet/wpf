// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(Frame))]
    class FrameFactory : DiscoverableFactory<Frame>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Uri Source { get; set; }

        public override Frame Create(DeterministicRandom random)
        {
            Frame frame = new Frame();
            frame.JournalOwnership = random.NextEnum<JournalOwnership>();
            frame.NavigationUIVisibility = random.NextEnum<NavigationUIVisibility>();
            frame.SandboxExternalContent = random.NextBool();
            frame.Source = Source;
            frame.NavigationFailed += new NavigationFailedEventHandler(ignoreNavigationFailure);
            return frame;
        }

        // On machines where the internet is inaccessible (many stress machines) Frames may be created with inaccessible Source URIs.
        // Since this is not anomalous but actually quite realistic, just handle the exception like a real app would do.
        void ignoreNavigationFailure(object sender, NavigationFailedEventArgs e)
        {
            e.Handled = true;
        }
    }
}
