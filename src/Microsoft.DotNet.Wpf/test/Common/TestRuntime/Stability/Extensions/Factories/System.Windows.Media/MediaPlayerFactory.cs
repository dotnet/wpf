// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;
using System.Diagnostics;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(MediaPlayer))]
    class MediaPlayerFactory : DiscoverableFactory<MediaPlayer>
    {
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent=true)]
        public MediaTimeline Timeline { get; set; }
        public override MediaPlayer Create(DeterministicRandom random)
        {
            MediaPlayer mediaPlayer = new MediaPlayer();

            mediaPlayer.Volume = random.NextDouble();
            mediaPlayer.ScrubbingEnabled = random.NextBool();
            mediaPlayer.Clock = Timeline.CreateClock();

            //events
            mediaPlayer.MediaEnded += new EventHandler(mediaPlayer_MediaEnded);
            mediaPlayer.MediaFailed += new EventHandler<ExceptionEventArgs>(mediaPlayer_MediaFailed);
            mediaPlayer.MediaOpened += new EventHandler(mediaPlayer_MediaOpened);
            mediaPlayer.BufferingStarted += new EventHandler(mediaPlayer_BufferingStarted);
            mediaPlayer.BufferingEnded += new EventHandler(mediaPlayer_BufferingEnded);

            return mediaPlayer;
        }

        void mediaPlayer_BufferingEnded(object sender, EventArgs e)
        {
            Trace.WriteLine("Media buffering ended.");
        }

        void mediaPlayer_BufferingStarted(object sender, EventArgs e)
        {
            Trace.WriteLine("Media buffering started.");
        }

        void mediaPlayer_MediaOpened(object sender, EventArgs e)
        {
            Trace.WriteLine("Media opened.");
        }

        void mediaPlayer_MediaFailed(object sender, ExceptionEventArgs e)
        {
            Trace.WriteLine("Media failed.");
        }

        void mediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            Trace.WriteLine("Media ended.");
        }
    }
}
