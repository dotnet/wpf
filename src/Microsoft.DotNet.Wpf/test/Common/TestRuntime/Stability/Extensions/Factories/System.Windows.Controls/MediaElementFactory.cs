// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;
using System.Diagnostics;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(MediaElement))]
    class MediaElementFactory : DiscoverableFactory<MediaElement>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Uri Uri { get; set; }
        public bool IsControledWithClock { get; set; }
        public MediaTimeline Timeline { get; set; }
        public override MediaElement Create(DeterministicRandom random)
        {
            MediaElement mediaElement = new MediaElement();
            mediaElement.Source = Uri;
            
            mediaElement.Volume = random.NextDouble();
            mediaElement.ScrubbingEnabled = random.NextBool();

            if (IsControledWithClock && Timeline != null)
            {
                mediaElement.Clock = Timeline.CreateClock();
            }
            else
            {
                mediaElement.Source = Uri;
            }
            
            //Events

            mediaElement.BufferingEnded += new System.Windows.RoutedEventHandler(BufferingEnded);
            mediaElement.BufferingStarted += new System.Windows.RoutedEventHandler(BufferingStarted);
            mediaElement.MediaOpened += new System.Windows.RoutedEventHandler(MediaOpened);
            mediaElement.MediaFailed += new EventHandler<System.Windows.ExceptionRoutedEventArgs>(MediaFailed);
            mediaElement.MediaEnded += new System.Windows.RoutedEventHandler(MediaEnded);
            return mediaElement;
        }

        void MediaEnded(object sender, System.Windows.RoutedEventArgs e)
        {
            Trace.WriteLine("Media ended.");
        }

        void MediaFailed(object sender, System.Windows.ExceptionRoutedEventArgs e)
        {
            Trace.WriteLine("Media failed.");
        }

        void MediaOpened(object sender, System.Windows.RoutedEventArgs e)
        {
            Trace.WriteLine("Media opened.");
        }

        void BufferingStarted(object sender, System.Windows.RoutedEventArgs e)
        {
            Trace.WriteLine("Media buffering started.");
        }

        void BufferingEnded(object sender, System.Windows.RoutedEventArgs e)
        {
            Trace.WriteLine("Media buffering ended.");
        }
    }
}
