// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(MediaTimeline))]
    class MediaTimelineFactory : DiscoverableFactory<MediaTimeline>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Uri Uri { get; set; }
        public override MediaTimeline Create(DeterministicRandom random)
        {
            MediaTimeline mediaTimeline = null;
            RepeatBehavior repeatBehavior = RepeatBehavior.Forever;

            TimeSpan begintime = TimeSpan.FromSeconds(random.Next(60));
            Duration playbackDuration = new Duration(TimeSpan.FromSeconds(random.Next(60)));

            switch (random.Next(4))
            {
                case 0: //Initializes a new instance of a MediaTimeline class using the supplied Uri as the media source.                
                    mediaTimeline = new System.Windows.Media.MediaTimeline(Uri);
                    break;

                case 1: //Initializes a new instance of the MediaTimeline that begins at the specified time. 
                    mediaTimeline = new System.Windows.Media.MediaTimeline(begintime);
                    mediaTimeline.Source = Uri;
                    break;

                case 2://new instance of the MediaTimeline that begins at the specified time and lasts for the specified duration. 
                    mediaTimeline =
                        new System.Windows.Media.MediaTimeline(
                                                begintime,
                                                playbackDuration);
                    mediaTimeline.Source = Uri;
                    break;

                case 3:
                    mediaTimeline =
                        new System.Windows.Media.MediaTimeline(
                                                begintime,
                                                playbackDuration,
                                                repeatBehavior);
                    mediaTimeline.Source = Uri;
                    break;

            }

            return mediaTimeline;
        }
    }
}
