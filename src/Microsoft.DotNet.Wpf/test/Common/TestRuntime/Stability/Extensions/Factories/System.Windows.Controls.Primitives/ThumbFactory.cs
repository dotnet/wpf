// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create Thumb.
    /// </summary>
    internal class ThumbFactory : DiscoverableFactory<Thumb>
    {
        /// <summary>
        /// Create a Thumb.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override Thumb Create(DeterministicRandom random)
        {
            Thumb thumb = new Thumb();

            thumb.DragCompleted += new DragCompletedEventHandler(DragCompleted);
            thumb.DragDelta += new DragDeltaEventHandler(DragDelta);
            thumb.DragStarted += new DragStartedEventHandler(DragStarted);

            return thumb;
        }

        private void DragStarted(object sender, DragStartedEventArgs e)
        {
            Trace.WriteLine("Thumb drag started.");
        }

        private void DragDelta(object sender, DragDeltaEventArgs e)
        {
            Trace.WriteLine("Thumb drag delta.");
        }

        private void DragCompleted(object sender, DragCompletedEventArgs e)
        {
            Trace.WriteLine("Thumb drag completed.");
        }
    }
}
