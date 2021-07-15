// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Threading;
using System.Security;
using System.Diagnostics;
using System.ComponentModel;
using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper
using MS.Win32;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Windows.Threading;
using System.Windows.Navigation;
using System.Runtime.InteropServices;
using System.IO;
using System.Security.AccessControl;//for semaphore access permissions
using System.Net;
using Microsoft.Win32;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using UnsafeNativeMethods=MS.Win32.PresentationCore.UnsafeNativeMethods;

//
// Disable the warnings that C# emmits when it finds pragmas it does not recognize, this is to
// get rid of false positive PreSharp warning
//
#pragma warning disable 1634, 1691


namespace System.Windows.Media
{
    #region MediaPlayer

    /// <summary>
    /// MediaPlayer
    /// Provides helper methods for media related tasks
    /// </summary>
    public class MediaPlayer : Animatable, DUCE.IResource
    {
        #region Constructors and Finalizers

        /// <summary>
        /// Constructor
        /// </summary>
        public MediaPlayer()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Indicates whether the media element is currently buffering.
        /// </summary>
        public bool IsBuffering
        {
            get
            {
                ReadPreamble();

                return _mediaPlayerState.IsBuffering;
            }
        }

        /// <summary>
        /// Indicates whether given media can be paused.
        /// </summary>
        public bool CanPause
        {
            get
            {
                ReadPreamble();

                return _mediaPlayerState.CanPause;
            }
        }

        /// <summary>
        /// Returns the download progress of the media.
        /// </summary>
        public double DownloadProgress
        {
            get
            {
                ReadPreamble();

                return _mediaPlayerState.DownloadProgress;
            }
        }

        /// <summary>
        /// Returns the buffering progress of the media.
        /// </summary>
        public double BufferingProgress
        {
            get
            {
                ReadPreamble();

                return _mediaPlayerState.BufferingProgress;
            }
        }

        /// <summary>
        /// Returns the natural height of the video.
        /// </summary>
        public Int32 NaturalVideoHeight
        {
            get
            {
                ReadPreamble();

                return _mediaPlayerState.NaturalVideoHeight;
            }
        }

        /// <summary>
        /// Returns the natural width the media.
        /// </summary>
        public Int32 NaturalVideoWidth
        {
            get
            {
                ReadPreamble();

                return _mediaPlayerState.NaturalVideoWidth;
            }
        }

        /// <summary>
        /// Returns whether the given media has audio content.
        /// </summary>
        public bool HasAudio
        {
            get
            {
                ReadPreamble();

                return _mediaPlayerState.HasAudio;
            }
        }

        /// <summary>
        /// Returns whether the given media has video content
        /// </summary>
        public bool HasVideo
        {
            get
            {
                ReadPreamble();

                return _mediaPlayerState.HasVideo;
            }
        }

        /// <summary>
        /// Location of the media to play. Open opens the media, this property
        /// allows the source that is currently playing to be retrieved.
        /// </summary>
        public Uri Source
        {
            get
            {
                ReadPreamble();

                return _mediaPlayerState.Source;
            }
        }

        /// <summary>
        /// The volume of the currently playing media.
        /// </summary>
        public double Volume
        {
            get
            {
                ReadPreamble();

                return _mediaPlayerState.Volume;
            }

            set
            {
                WritePreamble();

                _mediaPlayerState.Volume = value;
            }
        }

        /// <summary>
        /// Returns the balance that the current media has been set to.
        /// </summary>
        public double Balance
        {
            get
            {
                ReadPreamble();

                return _mediaPlayerState.Balance;
            }

            set
            {
                WritePreamble();

                _mediaPlayerState.Balance = value;
            }
        }

        /// <summary>
        /// Whether or not scrubbing is enabled
        /// </summary>
        public bool ScrubbingEnabled
        {
            get
            {
                ReadPreamble();

                return _mediaPlayerState.ScrubbingEnabled;
            }

            set
            {
                WritePreamble();

                _mediaPlayerState.ScrubbingEnabled = value;
            }
        }

        /// <summary>
        /// Returns the whether the given media is muted and sets whether it is
        /// muted.
        /// </summary>
        public bool IsMuted
        {
            get
            {
                ReadPreamble();

                return _mediaPlayerState.IsMuted;
            }

            set
            {
                WritePreamble();

                _mediaPlayerState.IsMuted = value;
            }
        }

        /// <summary>
        /// Returns the natural duration of the given media.
        /// </summary>
        public Duration NaturalDuration
        {
            get
            {
                ReadPreamble();

                return _mediaPlayerState.NaturalDuration;
            }
        }

        /// <summary>
        /// Seek to specified position
        /// </summary>
        public TimeSpan Position
        {
            set
            {
                WritePreamble();

                _mediaPlayerState.Position = value;
            }

            get
            {
                ReadPreamble();

                return _mediaPlayerState.Position;
            }
        }

        /// <summary>
        /// The current speed. This cannot be changed if a clock is controlling this player
        /// </summary>
        public double SpeedRatio
        {
            get
            {
                ReadPreamble();

                return _mediaPlayerState.SpeedRatio;
            }

            set
            {
                WritePreamble();

                _mediaPlayerState.SpeedRatio = value;
            }
        }

        #endregion

        #region EventHandlers

        /// <summary>
        /// Raised when there is an error opening or playing media
        /// </summary>
        public event EventHandler<ExceptionEventArgs> MediaFailed
        {
            add
            {
                WritePreamble();

                _mediaPlayerState.MediaFailed += value;
            }

            remove
            {
                WritePreamble();

                _mediaPlayerState.MediaFailed -= value;
            }
        }

        /// <summary>
        /// Raised when the media has been opened.
        /// </summary>
        public event EventHandler MediaOpened
        {
            add
            {
                WritePreamble();

                _mediaPlayerState.MediaOpened += value;
            }

            remove
            {
                WritePreamble();

                _mediaPlayerState.MediaOpened -= value;
            }
        }


        /// <summary>
        /// Raised when the media has finished.
        /// </summary>
        public event EventHandler MediaEnded
        {
            add
            {
                WritePreamble();

                _mediaPlayerState.MediaEnded += value;
            }

            remove
            {
                WritePreamble();

                _mediaPlayerState.MediaEnded -= value;
            }
        }


        /// <summary>
        /// Raised when media begins buffering.
        /// </summary>
        public event EventHandler BufferingStarted
        {
            add
            {
                WritePreamble();

                _mediaPlayerState.BufferingStarted += value;
            }

            remove
            {
                WritePreamble();

                _mediaPlayerState.BufferingStarted -= value;
            }
        }

        /// <summary>
        /// Raised when media finishes buffering.
        /// </summary>
        public event EventHandler BufferingEnded
        {
            add
            {
                WritePreamble();

                _mediaPlayerState.BufferingEnded += value;
            }

            remove
            {
                WritePreamble();

                _mediaPlayerState.BufferingEnded -= value;
            }
        }

        /// <summary>
        /// Raised when a script command embedded in the media is encountered.
        /// </summary>
        public event EventHandler<MediaScriptCommandEventArgs> ScriptCommand
        {
            add
            {
                WritePreamble();

                _mediaPlayerState.ScriptCommand += value;
            }

            remove
            {
                WritePreamble();

                _mediaPlayerState.ScriptCommand -= value;
            }
        }

        #endregion

        #region Clock dependent properties and methods

        /// <summary>
        /// The clock driving this instance of media
        /// </summary>
        public MediaClock Clock
        {
            get
            {
                ReadPreamble();

                return _mediaPlayerState.Clock;
            }

            set
            {
                WritePreamble();

                _mediaPlayerState.SetClock(value, this);
            }
        }

        /// <summary>
        /// Open the media, at this point the underlying native resources are
        /// created. The media player cannot be controlled when it isn't opened.
        /// </summary>
        public
        void
        Open(
            Uri      source
            )
        {
            WritePreamble();

            _mediaPlayerState.Open(source);
        }

        /// <summary>
        /// Begin playback. This operation is not allowed if a clock is
        /// controlling this player
        /// </summary>
        public void Play()
        {
            WritePreamble();

            _mediaPlayerState.Play();
        }

        /// <summary>
        /// Halt playback at current position. This operation is not allowed if
        /// a clock is controlling this player
        /// </summary>
        public void Pause()
        {
            WritePreamble();

            _mediaPlayerState.Pause();
        }

        /// <summary>
        /// Halt playback and seek to the beginning of media. This operation is
        /// not allowed if a clock is controlling this player
        /// </summary>
        public void Stop()
        {
            WritePreamble();

            _mediaPlayerState.Stop();
        }

        /// <summary>
        /// Closes the underlying media. This de-allocates all of the native resources in
        /// the media. The mediaplayer can be opened again by calling the Open method.
        /// </summary>
        public
        void
        Close()
        {
            WritePreamble();

            _mediaPlayerState.Close();
        }

        #endregion

        #region DUCE

        /// <summary>
        /// AddRefOnChannel
        /// </summary>
        DUCE.ResourceHandle DUCE.IResource.AddRefOnChannel(DUCE.Channel channel)
        {
            EnsureState();

            using (CompositionEngineLock.Acquire())
            {
                return AddRefOnChannelCore(channel);
            }
        }

        /// <summary>
        /// AddRefOnChannelCore
        /// </summary>
        /// <param name="channel">Channel</param>
        internal DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
            //
            // Create a media resource
            //
            if (_duceResource._duceResource.CreateOrAddRefOnChannel(this, channel, DUCE.ResourceType.TYPE_MEDIAPLAYER))
            {
                //
                // By definition we need an update whenever our channel changes.
                //
                _needsUpdate = true;

                UpdateResource(
                    channel,
                    true);  // Don't need a channel since we just created the resource.
            }

            return _duceResource._duceResource.GetHandle(channel);
        }

        /// <summary>
        /// ReleaseOnChannel
        /// </summary>
        void DUCE.IResource.ReleaseOnChannel(DUCE.Channel channel)
        {
            EnsureState();

            using (CompositionEngineLock.Acquire())
            {
                ReleaseOnChannelCore(channel);
            }
        }

        /// <summary>
        /// ReleaseOnChannelCore
        /// </summary>
        /// <param name="channel">Channel</param>
        internal void ReleaseOnChannelCore(DUCE.Channel channel)
        {
            Debug.Assert(_duceResource._duceResource.IsOnChannel(channel));

            _duceResource._duceResource.ReleaseOnChannel(channel);
        }

        /// <summary>
        /// GetHandle
        /// </summary>
        DUCE.ResourceHandle DUCE.IResource.GetHandle(DUCE.Channel channel)
        {
            EnsureState();

            using (CompositionEngineLock.Acquire())
            {
                return GetHandleCore(channel);
            }
        }

        /// <summary>
        /// GetHandleCore
        /// </summary>
        /// <param name="channel">Channel</param>
        internal DUCE.ResourceHandle GetHandleCore(DUCE.Channel channel)
        {
            return _duceResource._duceResource.GetHandle(channel);
        }

        int DUCE.IResource.GetChannelCount()
        {
            return _duceResource._duceResource.GetChannelCount();
        }

        DUCE.Channel DUCE.IResource.GetChannel(int index)
        {
            return _duceResource._duceResource.GetChannel(index);
        }

        #endregion

        #region Animatable

        /// <summary>
        /// UpdateResource is called when we need to update our resource
        /// on a particular channel.
        /// </summary>
        /// <param name="channel">Channel</param>
        /// <param name="skipOnChannelCheck">Whether we know we are on the
        /// channel</param>
        internal override
        void
        UpdateResource(
            DUCE.Channel channel,
            bool skipOnChannelCheck
            )
        {
            if (skipOnChannelCheck || _duceResource._duceResource.IsOnChannel(channel))
            {
                //
                // Chain this up through the base always otherwise our next
                // registration won't work.
                //
                base.UpdateResource(channel, true);

                //
                // Only actually send this if we are dirty.
                //
                if (_needsUpdate)
                {
                    //
                    // Send a new resource update
                    //
                    UpdateResourceInternal(channel);
                }
            }
        }

        #endregion

        #region Freezable

        /// <summary>
        /// CreateInstanceCore must return the new instance of the object to be
        /// created.
        /// </summary>
        protected override
        Freezable
        CreateInstanceCore()
        {
            return new MediaPlayer();
        }

        /// <summary>
        /// Clones the object, implemented as part of Freezable contract, must
        /// be chained to the base class.
        /// </summary>
        protected override
        void
        CloneCore(
            Freezable       sourceFreezable
            )
        {
            base.CloneCore(sourceFreezable);
            
            CloneCommon(sourceFreezable);
        }

        /// <summary>
        /// Clones the current value of the object, implemented as part of
        /// Freezable contract.
        /// </summary>
        protected override
        void
        CloneCurrentValueCore(
            Freezable       sourceFreezable
            )
        {
            base.CloneCurrentValueCore(sourceFreezable);
            
            CloneCommon(sourceFreezable);
        }

        /// <summary>
        /// Returns the object as frozen, media state is not really ammenable
        /// to being frozen.
        /// </summary>
        protected override
        void
        GetAsFrozenCore(
            Freezable       sourceFreezable
            )
        {
            base.GetAsFrozenCore(sourceFreezable);
            
            CloneCommon(sourceFreezable);
        }

        //
        // We don't need to implement FreezeCore and SetFreezableContextCore
        // because we don't really freeze our data.
        //
        private
        void
        CloneCommon(
            Freezable       sourceFreezable
            )
        {
            MediaPlayer player = (MediaPlayer)sourceFreezable;

            _mediaPlayerState = player._mediaPlayerState;
            _duceResource = player._duceResource;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Freezable forces us to have a new instance without parameters. However
        /// Creating MediaPlayerState is expensive so we use EnsureState to make sure
        /// that media state is created if we are not cloned after instantiation.
        /// </summary>
        private
        void
        EnsureState()
        {
            if (null == _mediaPlayerState)
            {
                _mediaPlayerState = new MediaPlayerState(this);
            }
        }


        /// <summary>
        /// Called before any read, ensures that the media player is called from only
        /// one thread. We also create our state if necessary at this point.
        /// </summary>
        protected new
        void
        ReadPreamble()
        {
            base.ReadPreamble();

            EnsureState();
        }

        /// <summary>
        /// Called before a write to check whether the given object is frozen.
        /// We also ensure that we have the media state initialized if this passes.
        /// </summary>
        protected new
        void
        WritePreamble()
        {
            base.WritePreamble();

            EnsureState();
        }

        /// <summary>
        /// Our event handler for receiving frame updates. When we get a new frame,
        /// we cause an async resource update.
        /// </summary>
        private
        void
        OnNewFrame(
            object      sender,
            EventArgs   args
            )
        {
            _needsUpdate = true;

            //
            // This means "call us back on the channel when the render pass happens"
            //
            RegisterForAsyncUpdateResource();

            //
            // Tell the freezable that we have changed.
            //
            FireChanged();
        }

        /// <summary>
        /// Update our resources on the channel, how we do this depends on what the
        /// channel is.
        /// </summary>
        private
        void
        UpdateResourceInternal(
            DUCE.Channel        channel
            )
        {
            bool    notifyUceDirectly = false;

            //
            // Check what sort of channel type we have, we do quite different
            // things depending on what the channel type is.
            //
            switch(channel.MarshalType)
            {
                case ChannelMarshalType.ChannelMarshalTypeSameThread:
                    break;

                case ChannelMarshalType.ChannelMarshalTypeCrossThread:
                    notifyUceDirectly = true;
                    break;

                default:
                    throw new System.NotSupportedException(SR.Get(SRID.Media_UnknownChannelType));
            }

            //
            // If we aren't going to notify the Uce directly, then we need to register for a
            // frame update.
            //
            if (!notifyUceDirectly)
            {
                if (null == _newFrameHandler)
                {
                    _newFrameHandler = new EventHandler(OnNewFrame);

                    _mediaPlayerState.NewFrame += _newFrameHandler;
                }
            }
            else
            {
                if (null != _newFrameHandler)
                {
                    _mediaPlayerState.NewFrame -= _newFrameHandler;

                    _newFrameHandler = null;
                }
            }

            _mediaPlayerState.SendCommandMedia(
                    channel,
                    _duceResource._duceResource.GetHandle(channel),
                    notifyUceDirectly
                    );

            //
            // We don't need to update anymore until we get a new frame or
            // a new channel.
            //
            _needsUpdate = false;
        }

        #endregion

        #region Properties called by the clock

        // Set the current speed.
        //
        internal
        void
        SetSpeedRatio(
            double value
            )
        {
            _mediaPlayerState.SetSpeedRatio(value);
        }

        /// <summary>
        /// Sets the source of the media (and opens it), without checking whether
        /// we are under clock control. This is called by the clock.
        /// </summary>
        internal
        void
        SetSource(
            Uri      source
            )
        {
            _mediaPlayerState.SetSource(source);
        }

        internal
        void
        SetPosition(
            TimeSpan value
            )
        {
            _mediaPlayerState.SetPosition(value);
        }

        #endregion

        #region Data Members

        private MediaPlayerState      _mediaPlayerState = null;

        /// <summary>
        /// DUCE resource handle - we need to use ShareableDUCEMultiChannelResource, because clones
        /// of a MediaPlayer all share the same underlying DUCE.MultiChannelResource.
        /// </summary>
        internal DUCE.ShareableDUCEMultiChannelResource _duceResource = new DUCE.ShareableDUCEMultiChannelResource();

        private EventHandler    _newFrameHandler = null;

        private bool            _needsUpdate = false;

        #endregion
    }

    #endregion
};

