// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Diagnostics;
using System.ComponentModel;
using MS.Internal;
using MS.Win32;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Windows.Markup;
using System.Security;
using MS.Internal.PresentationCore;                   // SecurityHelper
using System.Windows.Threading;

using System.Runtime.InteropServices;
using System.IO;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using UnsafeNativeMethods=MS.Win32.PresentationCore.UnsafeNativeMethods;

namespace System.Windows.Media
{
    #region MediaClock

    /// <summary>
    /// Maintains run-time timing state for media (audio/video) objects.
    /// </summary>
    public class MediaClock :
        Clock
    {
        #region Constructors and Finalizers

        /// <summary>
        /// Creates a MediaClock object.
        /// </summary>
        /// <param name="media">
        /// The MediaTimeline to use as a template.
        /// </param>
        /// <remarks>
        /// The returned MediaClock doesn't have any children.
        /// </remarks>
        protected internal MediaClock(MediaTimeline media)
            : base(media)
        {}

        #endregion

        #region Properties

        /// <summary>
        /// Gets the MediaTimeline object that holds the description controlling the
        /// behavior of this clock.
        /// </summary>
        /// <value>
        /// The MediaTimeline object that holds the description controlling the
        /// behavior of this clock.
        /// </value>
        public new MediaTimeline Timeline
        {
            get
            {
                return (MediaTimeline)base.Timeline;
            }
        }

        #endregion

        #region Clock Overrides

        /// <summary>
        /// Returns True because Media has the potential to slip.
        /// </summary>
        /// <returns>True</returns>
        protected override bool GetCanSlip()
        {
            return true;
        }

        /// <summary>
        /// Get the actual media time for slip synchronization
        /// </summary>
        protected override TimeSpan GetCurrentTimeCore()
        {
            if (_mediaPlayer != null)
            {
                return _mediaPlayer.Position;
            }
            else  // Otherwise use base implementation
            {
                return base.GetCurrentTimeCore();
            }
        }

        /// <summary>
        /// Called when we are stopped. This is the same as pausing and seeking
        /// to the beginning.
        /// </summary>
        protected override void Stopped()
        {
            // Only perform the operation if we're controlling a player
            if (_mediaPlayer != null)
            {
                _mediaPlayer.SetSpeedRatio(0);
                _mediaPlayer.SetPosition(TimeSpan.FromTicks(0));
            }
        }

        /// <summary>
        /// Called when our speed changes. A discontinuous time movement may or
        /// may not have occurred.
        /// </summary>
        protected override void SpeedChanged()
        {
            Sync();
        }

        /// <summary>
        /// Called when we have a discontinuous time movement, but no change in
        /// speed
        /// </summary>
        protected override void DiscontinuousTimeMovement()
        {
            Sync();
        }

        private void Sync()
        {
            // Only perform the operation if we're controlling a player
            if (_mediaPlayer != null)
            {
                double? currentSpeedProperty = this.CurrentGlobalSpeed;
                double currentSpeedValue = currentSpeedProperty.HasValue ? currentSpeedProperty.Value : 0;

                TimeSpan? currentTimeProperty = this.CurrentTime;
                TimeSpan currentTimeValue = currentTimeProperty.HasValue ? currentTimeProperty.Value : TimeSpan.Zero;

                // If speed was potentially changed to 0, make sure we set media's speed to 0 (e.g. pause) before
                // setting the position to the target frame.  Otherwise, the media's scrubbing mechanism would
                // not work correctly, because scrubbing requires media to be paused by the time it is seeked.
                if (currentSpeedValue == 0)
                {
                    _mediaPlayer.SetSpeedRatio(currentSpeedValue);
                    _mediaPlayer.SetPosition(currentTimeValue);
                }
                else
                {
                    // In the case where speed != 0, we first want to set the position and then the speed.
                    // This is because if we were previously paused, we want to be at the right position
                    // before we begin to play.
                    _mediaPlayer.SetPosition(currentTimeValue);
                    _mediaPlayer.SetSpeedRatio(currentSpeedValue);
                }
            }
        }


        /// <summary>
        /// Returns true if this timeline needs continuous frames.
        /// This is a hint that we should keep updating our time during the active period.
        /// </summary>
        /// <returns></returns>
        internal override bool NeedsTicksWhenActive
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// The instance of media that this clock is driving
        /// </summary>
        internal MediaPlayer Player
        {
            get
            {
                return _mediaPlayer;
            }
            set
            {
                MediaPlayer oldPlayer = _mediaPlayer;
                MediaPlayer newPlayer = value;

                // avoid inifite loops
                if (newPlayer != oldPlayer)
                {
                    _mediaPlayer = newPlayer;

                    // Disassociate the old player
                    if (oldPlayer != null)
                    {
                        oldPlayer.Clock = null;
                    }

                    // Associate the new player
                    if (newPlayer != null)
                    {
                        newPlayer.Clock = this;

                        Uri baseUri = ((IUriContext)Timeline).BaseUri;

                        Uri toPlay = null;

                        // ignore pack URIs for now (see work items 45396 and 41636)
                        if (baseUri != null
                         && baseUri.Scheme != System.IO.Packaging.PackUriHelper.UriSchemePack
                         && !Timeline.Source.IsAbsoluteUri)
                        {
                            toPlay = new Uri(baseUri, Timeline.Source);
                        }
                        else
                        {
                            //
                            // defaults to app domain base if Timeline.Source is
                            // relative
                            //
                            toPlay = Timeline.Source;
                        }

                        // we need to sync to the current state of the clock
                        newPlayer.SetSource(toPlay);

                        SpeedChanged();
                    }
                }
            }
        }

        #endregion

        #region Private Data members

        /// <summary>
        /// MediaPlayer -- holds all the precious resource references
        /// </summary>
        private MediaPlayer _mediaPlayer;

        #endregion
    }

    #endregion
};

