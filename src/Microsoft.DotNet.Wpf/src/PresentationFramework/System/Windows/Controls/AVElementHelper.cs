// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Contains the AVElementHelper class.
//


using MS.Internal;
using MS.Utility;
using System.Diagnostics;
using System.Windows.Threading;
using System;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Security;

namespace System.Windows.Controls
{
    #region SettableState

    /// <summary>
    /// Settable State, keeps track of what state was set and whether it has
    /// been set recently.
    /// </summary>
    internal struct SettableState<T>
    {
        internal    T       _value;
        internal    bool    _isSet;
        internal    bool    _wasSet;

        internal
        SettableState(
            T   value
            )
        {
            _value = value;
            _isSet = _wasSet = false;
        }
    }

    #endregion

    #region AVElementHelper

    /// <summary>
    /// AVElementHelper
    /// </summary>
    internal class AVElementHelper
    {
        #region Constructor

        /// <summary>
        /// Constructor, point to the corresponding MediaElement.
        /// </summary>
        internal AVElementHelper(MediaElement element)
        {
            Debug.Assert((element != null), "Element is null");

            _element = element;

            _position = new SettableState<TimeSpan>(new TimeSpan(0));

            //
            // We always start off in a closed state.
            //
            _mediaState = new SettableState<MediaState>(MediaState.Close);

            _source = new SettableState<Uri>(null);

            _clock = new SettableState<MediaClock>(null);

            _speedRatio = new SettableState<double>(1.0);

            _volume = new SettableState<double>(0.5);

            _isMuted = new SettableState<bool>(false);

            _balance = new SettableState<double>(0.0);

            _isScrubbingEnabled = new SettableState<bool>(false);

            _mediaPlayer = new MediaPlayer();

            HookEvents();
        }

        #endregion

        #region Internal and Private Properties / Methods

        /// <summary>
        /// Returns the helper class given a dependency object
        /// </summary>
        internal static AVElementHelper GetHelper(DependencyObject d)
        {
            MediaElement mediaElement = d as MediaElement;

            if (mediaElement != null)
            {
                return mediaElement.Helper;
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.AudioVideo_InvalidDependencyObject));
            }
        }

        /// <summary>
        /// MediaPlayer associated with the element.
        /// </summary>
        internal MediaPlayer Player
        {
            get
            {
                return _mediaPlayer;
            }
        }

        /// <summary>
        /// Base Uri to use when resolving relative Uri's
        /// </summary>
        internal Uri BaseUri
        {
            get
            {
                return _baseUri;
            }
            set
            {
                // ignore pack URIs for now (see work items 45396 and 41636)
                if (value.Scheme != System.IO.Packaging.PackUriHelper.UriSchemePack)
                {
                    _baseUri = value;
                }
                else
                {
                    _baseUri = null;
                }
            }
        }

        /// <summary>
        /// Allows the behavior the Media Element should have when it is unloaded
        /// to be expressed. This is a method because it can have side-effects
        /// (Media could start playing or pause or perform a number of other
        /// actions).
        /// </summary>
        internal
        void
        SetUnloadedBehavior(
            MediaState      unloadedBehavior
            )
        {
            _unloadedBehavior = unloadedBehavior;

            HandleStateChange();
        }

        /// <summary>
        /// Changes the loaded behavior. This is a method because it can cause
        /// side effects on other properties. (It could cause me to start or
        /// stop playing, for example).
        /// </summary>
        internal
        void
        SetLoadedBehavior(
            MediaState      loadedBehavior
            )
        {
            _loadedBehavior = loadedBehavior;

            HandleStateChange();
        }

        /// <summary>
        /// Returns the current position of the media.
        /// </summary>
        internal TimeSpan Position
        {
            get
            {
                //
                // If we have been closed, position is just a cached value,
                // return it.
                //
                if (_currentState == MediaState.Close)
                {
                    return _position._value;
                }
                else
                {
                    return _mediaPlayer.Position;
                }
            }
        }

        /// <summary>
        /// Sets the current position of the media. This is a method
        /// and not a property because it has side effects.
        /// </summary>
        internal
        void
        SetPosition(
            TimeSpan        position
            )
        {
            _position._isSet = true;
            _position._value = position;

            //
            // If the media isn't closed, then we can actually send
            // this down to the unmanaged state engine. It gets
            // snippy if you try to change the position when it
            // is closed.
            //
            HandleStateChange();
        }

        internal MediaClock Clock
        {
            get
            {
                return _clock._value;
            }
        }

        internal
        void
        SetClock(
            MediaClock      clock
            )
        {
            _clock._value = clock;

            //
            // We don't use _wasSet for clocks because our behavior changes dramatically
            // whether _value is null or not.
            //
            _clock._isSet = true;

            HandleStateChange();
        }

        internal double SpeedRatio
        {
            get
            {
                return _speedRatio._value;
            }
        }

        internal
        void
        SetSpeedRatio(
            double          speedRatio
            )
        {
            _speedRatio._wasSet = _speedRatio._isSet = true;
            _speedRatio._value = speedRatio;

            HandleStateChange();
        }

        internal
        void
        SetState(
            MediaState      mediaState
            )
        {
            //
            // If the caller hasn't requested any loaded or unloaded behavior to be manual
            // then calls to Play/Pause/Stop etc. will never take effect.
            //
            if (_loadedBehavior != MediaState.Manual && _unloadedBehavior != MediaState.Manual)
            {
                throw new NotSupportedException(SR.Get(SRID.AudioVideo_CannotControlMedia));
            }

            _mediaState._value = mediaState;
            _mediaState._isSet = true;

            HandleStateChange();
        }

        internal
        void
        SetVolume(
            double          volume
            )
        {
            _volume._wasSet = _volume._isSet = true;
            _volume._value = volume;

            HandleStateChange();
        }

        internal
        void
        SetBalance(
            double          balance
            )
        {
            _balance._wasSet = _balance._isSet = true;
            _balance._value = balance;

            HandleStateChange();
        }

        internal
        void
        SetIsMuted(
            bool            isMuted
            )
        {
            _isMuted._wasSet = _isMuted._isSet = true;
            _isMuted._value = isMuted;

            HandleStateChange();
        }

        internal
        void
        SetScrubbingEnabled(
            bool            isScrubbingEnabled
            )
        {
            _isScrubbingEnabled._wasSet = _isScrubbingEnabled._isSet = true;
            _isScrubbingEnabled._value = isScrubbingEnabled;

            HandleStateChange();
        }

        /// <summary>
        /// Hook Events when clock is created/changed
        /// </summary>
        private void HookEvents()
        {
            // register the new clock events
            _mediaPlayer.MediaOpened += new EventHandler(OnMediaOpened);

            _mediaPlayer.MediaFailed += new EventHandler<ExceptionEventArgs>(OnMediaFailed);

            _mediaPlayer.BufferingStarted += new EventHandler(OnBufferingStarted);

            _mediaPlayer.BufferingEnded += new EventHandler(OnBufferingEnded);

            _mediaPlayer.MediaEnded += new EventHandler(OnMediaEnded);

            _mediaPlayer.ScriptCommand += new EventHandler<MediaScriptCommandEventArgs>(OnScriptCommand);

            _element.Loaded += new RoutedEventHandler(this.OnLoaded);

            _element.Unloaded += new RoutedEventHandler(this.OnUnloaded);
        }

        /// <summary>
        /// All state changes to the media element come through this code, we first
        /// look at all of the media element properties and the loaded behavior and
        /// source property to see whether to open or close the media, the we set
        /// other properties that actually control media and have been cached up.
        /// </summary>
        private
        void
        HandleStateChange(
            )
        {
            //
            // First, just assume that our requested actions are going to be
            // the same as the media requested actions.
            //
            MediaState  thisStateRequest = _mediaState._value;
            bool        openClock = false;
            bool        actionRequested = false;

            //
            // If the element is loaded
            //
            if (_isLoaded)
            {
                //
                // If we have a clock, then our loaded behavior is always manual.
                // The clock always wins.
                //
                if (_clock._value != null)
                {
                    thisStateRequest = MediaState.Manual;

                    openClock = true;
                }
                //
                // If the loaded behavior was set, it wins over whether the
                // source was set or not.
                //
                else if (_loadedBehavior != MediaState.Manual)
                {
                    //
                    // If it is manual, it doesn't override the requested state.
                    //
                    thisStateRequest = _loadedBehavior;
                }
                else if (_source._wasSet)
                {
                    if (_loadedBehavior != MediaState.Manual)
                    {
                        thisStateRequest = MediaState.Play;
                    }
                    else
                    {
                        actionRequested = true;
                    }
                }
            }
            else
            {
                //
                // If the unloaded behavior is manual, it doesn't override the
                // requested state,
                //
                if (_unloadedBehavior != MediaState.Manual)
                {
                    thisStateRequest = _unloadedBehavior;
                }
                else
                {
                    //
                    // For situations in which we don't received loaded and unloaded
                    // events, (like VisualBrush), we need to set our UnloadedBehavior
                    // to Manual in order to allow storyboards to be able to control
                    // the media element.
                    //
                    Invariant.Assert(_unloadedBehavior == MediaState.Manual);

                    if (_clock._value != null)
                    {
                        thisStateRequest = MediaState.Manual;

                        openClock = true;
                    }
                    //
                    // Otherwise, an action was requested, we need to take it.
                    //
                    else
                    {
                        actionRequested = true;
                    }
                }
            }

            bool    openedMedia = false;

            //
            // If the media state is anything other than close
            // and the current state is closed, the media needs to be opened.
            //
            if (      thisStateRequest != MediaState.Close
                   && thisStateRequest != MediaState.Manual)
            {
                //
                // We shouldn't have a clock to open because this would have a state
                // request of MediaState.Manual.
                //
                Invariant.Assert(openClock == false);

                //
                // If we had a clock, get rid of it now. This is to handle the case
                // where UnloadedBehavior is specified after the timing engine has
                // been in control.
                //
                if (_mediaPlayer.Clock != null)
                {
                    _mediaPlayer.Clock = null;
                }

                //
                // If we are currently closed, we should open. If the source property
                // was assigned, we should also open.
                //
                if (_currentState == MediaState.Close || _source._isSet)
                {
                    //
                    // ScrubbingEnabled needs to be set before opening media in
                    // order to get the initial scrub.
                    //
                    if (_isScrubbingEnabled._wasSet)
                    {
                        _mediaPlayer.ScrubbingEnabled = _isScrubbingEnabled._value;

                        _isScrubbingEnabled._isSet = false;
                    }

                    if (_clock._value == null)
                    {
                        _mediaPlayer.Open(UriFromSourceUri(_source._value));
                    }

                    openedMedia = true;
                }
            }
            else if (openClock)
            {
                //
                // If either we were closed before, or if a clock was re-assigned without
                // a state transition, then, re-apply the clock.
                //
                if (_currentState == MediaState.Close || _clock._isSet)
                {
                    //
                    // ScrubbingEnabled needs to be set before opening media in
                    // order to get the initial scrub.
                    //
                    if (_isScrubbingEnabled._wasSet)
                    {
                        _mediaPlayer.ScrubbingEnabled = _isScrubbingEnabled._value;

                        _isScrubbingEnabled._isSet = false;
                    }

                    _mediaPlayer.Clock = _clock._value;

                    _clock._isSet = false;

                    openedMedia = true;
                }
            }
            //
            // Otherwise, if the request is for a Close and and we aren't in a closed state,
            // we need to close.
            //
            else if (thisStateRequest == MediaState.Close)
            {
                if (_currentState != MediaState.Close)
                {
                    //
                    // Dis-associate the clock from the player (if it has one).
                    // (Otherwise, it won't let us close it).
                    //
                    _mediaPlayer.Clock = null;

                    _mediaPlayer.Close();

                    _currentState = MediaState.Close;
                }
            }

            //
            // If we either just opened the media, or if we weren't closed in the
            // first place, we get to perform all of the other actions.
            //
            if (_currentState != MediaState.Close || openedMedia)
            {
                //
                // If we have a position request, do it now.
                //
                if (_position._isSet)
                {
                    _mediaPlayer.Position = _position._value;

                    _position._isSet = false;
                }

                //
                // Do volume state changes before a play so that we don't get either
                // no sound or a loud sound on the play transition.
                //
                if (_volume._isSet || openedMedia && _volume._wasSet)
                {
                    _mediaPlayer.Volume = _volume._value;

                    _volume._isSet = false;
                }

                if (_balance._isSet || openedMedia && _balance._wasSet)
                {
                    _mediaPlayer.Balance = _balance._value;

                    _balance._isSet = false;
                }

                if (_isMuted._isSet || openedMedia && _isMuted._wasSet)
                {
                    _mediaPlayer.IsMuted = _isMuted._value;

                    _isMuted._isSet = false;
                }

                //
                // In the case that openedMedia is true, we will have already
                // applied the scrubbing enabled property prior to opening the
                // media. This is necessary for initial scrubbing to work
                //
                if (_isScrubbingEnabled._isSet)
                {
                    _mediaPlayer.ScrubbingEnabled = _isScrubbingEnabled._value;

                    _isScrubbingEnabled._isSet = false;
                }

                //
                // If we are asked to play because the source was reset,
                // then, start playing the media again.
                //
                if (thisStateRequest == MediaState.Play && _source._isSet)
                {
                    //
                    // We always want to Play() and then set the SpeedRatio to
                    // 1. This ensures that whwn you change the source of a
                    // MediaElement via databinding, we start playing the
                    // media again.
                    //
                    _mediaPlayer.Play();

                    if (!_speedRatio._wasSet)
                    {
                        _mediaPlayer.SpeedRatio = 1;
                    }

                    //
                    // We have effectively `swallowed the "Play" request, if this is
                    // what finally brought us into this code path.
                    //
                    _source._isSet = false;
                    _mediaState._isSet = false;
                }
                //
                // Might be missing out on a Play, Pause, or a stop.
                // If the source is changed, we always want to do the
                // requested action. Also, we want to mirror each call
                // to Play, Pause and Stop down to the underlying player.
                //
                else if (   _currentState != thisStateRequest
                         || (actionRequested && _mediaState._isSet))
                {
                    switch(thisStateRequest)
                    {
                    case MediaState.Play:
                        _mediaPlayer.Play();
                        break;

                    case MediaState.Pause:
                        _mediaPlayer.Pause();
                        break;

                    case MediaState.Stop:
                        _mediaPlayer.Stop();
                        break;

                    case MediaState.Manual:
                        break;

                    default:
                        Invariant.Assert(false, "Unexpected state request.");
                        break;
                    }

                    //
                    // If we did this transition because an action was requested, make sure
                    // we don't do it again when we come in.
                    //
                    if (actionRequested)
                    {
                        _mediaState._isSet = false;
                    }
                }

                _currentState = thisStateRequest;

                //
                // Finally, if the speed ratio has been set, change it.
                //
                if (_speedRatio._isSet || openedMedia && _speedRatio._wasSet)
                {
                    _mediaPlayer.SpeedRatio = _speedRatio._value;

                    _speedRatio._isSet = false;
                }
            }
        }

        /// <summary>
        /// Looks at the current uri and the base uri and uses it to determine
        /// whether we should normalize the uri to the base or not.
        /// </summary>
        private
        Uri
        UriFromSourceUri(
            Uri     sourceUri
            )
        {
            if (sourceUri != null)
            {
                if (sourceUri.IsAbsoluteUri)
                {
                    return sourceUri;
                }
                else if (BaseUri != null)
                {
                    return new Uri(BaseUri, sourceUri);
                }
            }

            return sourceUri;
        }

        #endregion

        #region Delegates

        /// <summary>
        /// Raised when source is changed
        /// </summary>
        internal static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange)
            {
                return;
            }

            AVElementHelper aveh = AVElementHelper.GetHelper(d);
            aveh.MemberOnInvalidateSource(e); // call non-static
        }

        private void MemberOnInvalidateSource(DependencyPropertyChangedEventArgs e)
        {
            if (_clock._value != null)
            {
                throw new InvalidOperationException(SR.Get(SRID.MediaElement_CannotSetSourceOnMediaElementDrivenByClock));
            }

            _source._value = (Uri)e.NewValue;
            _source._wasSet = _source._isSet = true;

            HandleStateChange();
        }

        /// <summary>
        /// Raised when there is a error with media playback
        /// </summary>
        private
        void
        OnMediaFailed(
            object sender,
            ExceptionEventArgs args
            )
        {
            //
            // Propagate the error to the media element.
            //
            _element.OnMediaFailed(sender, args);
        }

        /// <summary>
        /// Raised when media is opened
        /// </summary>
        private
        void
        OnMediaOpened(
            object sender,
            EventArgs args
            )
        {
            // Whenever a new file is opened the size of the MediaElement may change
            _element.InvalidateMeasure();

            _element.OnMediaOpened(sender, args);
        }

        private
        void
        OnBufferingStarted(
            object  sender,
            EventArgs args
            )
        {
            _element.OnBufferingStarted(sender, args);
        }

        private
        void
        OnBufferingEnded(
            object      sender,
            EventArgs   args
            )
        {
            _element.OnBufferingEnded(sender, args);
        }

        private
        void
        OnMediaEnded(
            object      sender,
            EventArgs   args
            )
        {
            _element.OnMediaEnded(sender, args);
        }

        private
        void
        OnScriptCommand(
            object                          sender,
            MediaScriptCommandEventArgs     args
            )
        {
            _element.OnScriptCommand(sender, args);
        }

        private
        void
        OnLoaded(
            object                          sender,
            RoutedEventArgs                 args
            )
        {
            _isLoaded = true;

            HandleStateChange();
        }

        private
        void
        OnUnloaded(
            object                          sender,
            RoutedEventArgs                 args
            )
        {
           _isLoaded = false;

            HandleStateChange();
        }

        #endregion

        #region Data Members

        /// <summary>
        /// MediaPlayer
        /// </summary>
        private MediaPlayer _mediaPlayer;

        /// <summary>
        /// UIElement that owns this helper
        /// </summary>
        private MediaElement _element;

        private Uri _baseUri;

        private MediaState  _unloadedBehavior = MediaState.Close;
        private MediaState  _loadedBehavior = MediaState.Play;

        private MediaState    _currentState = MediaState.Close;

        private bool          _isLoaded = false;

        //
        // The requested state, we need to know for each one whether it
        // was ever set and for
        //
        SettableState<TimeSpan>     _position;
        SettableState<MediaState>   _mediaState;
        SettableState<Uri>          _source;
        SettableState<MediaClock>   _clock;
        SettableState<double>       _speedRatio;
        SettableState<double>       _volume;
        SettableState<bool>         _isMuted;
        SettableState<double>       _balance;
        SettableState<bool>         _isScrubbingEnabled;

        #endregion
    }

    #endregion
}
