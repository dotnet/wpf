// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Contains the MediaElement class.
//

using MS.Internal;
using MS.Utility;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Automation.Peers;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Markup;
using MS.Internal.Telemetry.PresentationFramework;

namespace System.Windows.Controls
{
    /// <summary>
    /// States that can be applied to the media element automatically when the
    /// MediaElement is loaded or unloaded.
    /// </summary>
    public enum MediaState : int
    {
        /// <summary>
        /// The media element should be controlled manually, either by its associated
        /// clock, or by directly calling the Play/Pause etc. on the media element.
        /// </summary>
        Manual = 0,

        /// <summary>
        /// The media element should play.
        /// </summary>
        Play = 1,

        /// <summary>
        /// The media element should close. This stops all media processing and releases
        /// any video memory held by the media element.
        /// </summary>
        Close = 2,

        /// <summary>
        /// The media element should pause.
        /// </summary>
        Pause = 3,

        /// <summary>
        /// The media element should stop.
        /// </summary>
        Stop = 4
    }

    /// <summary>
    /// Media Element
    /// </summary>
    [Localizability(LocalizationCategory.NeverLocalize)]
    public class MediaElement : FrameworkElement, IUriContext
    {
        #region Constructors

        /// <summary>
        /// Default DependencyObject constructor
        /// </summary>
        /// <remarks>
        /// Automatic determination of current Dispatcher. Use alternative constructor
        /// that accepts a Dispatcher for best performance.
        /// </remarks>
        public MediaElement() : base()
        {
            Initialize();
        }

        static MediaElement()
        {
            Style style = CreateDefaultStyles();
            StyleProperty.OverrideMetadata(typeof(MediaElement), new FrameworkPropertyMetadata(style));

            //
            // The Stretch & StretchDirection properties are AddOwner'ed from a class which is not
            // base class for MediaElement so the metadata with flags get lost. We need to override them
            // here to make it work again.
            //
            StretchProperty.OverrideMetadata(
                typeof(MediaElement),
                new FrameworkPropertyMetadata(
                    Stretch.Uniform,
                    FrameworkPropertyMetadataOptions.AffectsMeasure
                    )
                );

            StretchDirectionProperty.OverrideMetadata(
                typeof(MediaElement),
                new FrameworkPropertyMetadata(
                    StretchDirection.Both,
                    FrameworkPropertyMetadataOptions.AffectsMeasure
                    )
                );

            ControlsTraceLogger.AddControl(TelemetryControls.MediaElement);
        }

        private static Style CreateDefaultStyles()
        {
            Style style = new Style(typeof(MediaElement), null);
            style.Setters.Add (new Setter(FlowDirectionProperty, FlowDirection.LeftToRight));
            style.Seal();
            return style;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// DependencyProperty for MediaElement Source property.
        /// </summary>
        /// <seealso cref="MediaElement.Source" />
        /// This property is cached (_source).
        public static readonly DependencyProperty SourceProperty =
                DependencyProperty.Register(
                        "Source",
                        typeof(Uri),
                        typeof(MediaElement),
                        new FrameworkPropertyMetadata(
                                null,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(AVElementHelper.OnSourceChanged)));

        /// <summary>
        ///     The DependencyProperty for the MediaElement.Volume property.
        /// </summary>
        public static readonly DependencyProperty VolumeProperty
            = DependencyProperty.Register(
                        "Volume",
                        typeof(double),
                        typeof(MediaElement),
                        new FrameworkPropertyMetadata(
                              0.5,
                              FrameworkPropertyMetadataOptions.None,
                              new PropertyChangedCallback(VolumePropertyChanged)));
        /// <summary>
        ///     The DependencyProperty for the MediaElement.Balance property.
        /// </summary>
        public static readonly DependencyProperty BalanceProperty
            = DependencyProperty.Register(
                        "Balance",
                        typeof(double),
                        typeof(MediaElement),
                        new FrameworkPropertyMetadata(
                              0.0,
                              FrameworkPropertyMetadataOptions.None,
                              new PropertyChangedCallback(BalancePropertyChanged)));

        /// <summary>
        /// The DependencyProperty for the MediaElement.IsMuted property.
        /// </summary>
        public static readonly DependencyProperty IsMutedProperty
            = DependencyProperty.Register(
                        "IsMuted",
                        typeof(bool),
                        typeof(MediaElement),
                        new FrameworkPropertyMetadata(
                            false,
                            FrameworkPropertyMetadataOptions.None,
                            new PropertyChangedCallback(IsMutedPropertyChanged)));

        /// <summary>
        /// The DependencyProperty for the MediaElement.ScrubbingEnabled property.
        /// </summary>
        public static readonly DependencyProperty ScrubbingEnabledProperty
            = DependencyProperty.Register(
                        "ScrubbingEnabled",
                        typeof(bool),
                        typeof(MediaElement),
                        new FrameworkPropertyMetadata(
                            false,
                            FrameworkPropertyMetadataOptions.None,
                            new PropertyChangedCallback(ScrubbingEnabledPropertyChanged)));

        /// <summary>
        /// The DependencyProperty for the MediaElement.UnloadedBehavior property.
        /// </summary>
        public static readonly DependencyProperty UnloadedBehaviorProperty
            = DependencyProperty.Register(
                        "UnloadedBehavior",
                        typeof(MediaState),
                        typeof(MediaElement),
                        new FrameworkPropertyMetadata(
                            MediaState.Close,
                            FrameworkPropertyMetadataOptions.None,
                            new PropertyChangedCallback(UnloadedBehaviorPropertyChanged)));

        /// <summary>
        /// The DependencyProperty for the MediaElement.LoadedBehavior property.
        /// </summary>
        public static readonly DependencyProperty LoadedBehaviorProperty
            = DependencyProperty.Register(
                        "LoadedBehavior",
                        typeof(MediaState),
                        typeof(MediaElement),
                        new FrameworkPropertyMetadata(
                            MediaState.Play,
                            FrameworkPropertyMetadataOptions.None,
                            new PropertyChangedCallback(LoadedBehaviorPropertyChanged)));

        /// <summary>
        /// Gets/Sets the Source on this MediaElement.
        ///
        /// The Source property is the Uri of the media to be played.
        /// </summary>
        public Uri Source
        {
            get { return (Uri)GetValue(SourceProperty); }

            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// Media Clock associated with this MediaElement.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MediaClock Clock
        {
            get
            {
                return _helper.Clock;
            }
            set
            {
                _helper.SetClock(value);
            }
        }

        /// <summary>
        /// Requests the the media is played. This method only has an effect if the current
        /// media element state is manual.
        /// </summary>
        public
        void
        Play()
        {
            _helper.SetState(MediaState.Play);
        }

        /// <summary>
        /// Requests the the media is paused. This method only has an effect if the current
        /// media element state is manual.
        /// </summary>
        public
        void
        Pause()
        {
            _helper.SetState(MediaState.Pause);
        }

        /// <summary>
        /// Requests the the media is stopped. This method only has an effect if the current
        /// media element state is manual.
        /// </summary>
        public
        void
        Stop()
        {
            _helper.SetState(MediaState.Stop);
        }

        /// <summary>
        /// Requests the the media is Closed. This method only has an effect if the current
        /// media element state is manual.
        /// </summary>
        public
        void
        Close()
        {
            _helper.SetState(MediaState.Close);
        }

        /// <summary>
        /// DependencyProperty for Stretch property.
        /// </summary>
        /// <seealso cref="MediaElement.Stretch" />
        /// This property is cached and grouped (AspectRatioGroup)
        public static readonly DependencyProperty StretchProperty =
                Viewbox.StretchProperty.AddOwner(typeof(MediaElement));

        /// <summary>
        /// DependencyProperty for StretchDirection property.
        /// </summary>
        /// <seealso cref="Viewbox.Stretch" />
        public static readonly DependencyProperty StretchDirectionProperty =
                Viewbox.StretchDirectionProperty.AddOwner(typeof(MediaElement));

        /// <summary>
        /// Gets/Sets the Stretch on this MediaElement.
        /// The Stretch property determines how large the MediaElement will be drawn.
        /// </summary>
        /// <seealso cref="MediaElement.StretchProperty" />
        public Stretch Stretch
        {
            get { return (Stretch) GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        /// <summary>
        /// Gets/Sets the stretch direction of the Viewbox, which determines the restrictions on
        /// scaling that are applied to the content inside the Viewbox.  For instance, this property
        /// can be used to prevent the content from being smaller than its native size or larger than
        /// its native size.
        /// </summary>
        /// <seealso cref="Viewbox.StretchDirectionProperty" />
        public StretchDirection StretchDirection
        {
            get { return (StretchDirection) GetValue(StretchDirectionProperty); }
            set { SetValue(StretchDirectionProperty, value); }
        }

        /// <summary>
        /// Gets/Sets the Volume property on the MediaElement.
        /// </summary>
        public double Volume
        {
            get
            {
                return (double) GetValue(VolumeProperty);
            }
            set
            {
                SetValue(VolumeProperty, value);
            }
        }

        /// <summary>
        /// Gets/Sets the Balance property on the MediaElement.
        /// </summary>
        public double Balance
        {
            get
            {
                return (double) GetValue(BalanceProperty);
            }
            set
            {
                SetValue(BalanceProperty, value);
            }
        }

        /// <summary>
        /// Gets/Sets the IsMuted property on the MediaElement.
        /// </summary>
        public bool IsMuted
        {
            get
            {
                return (bool) GetValue(IsMutedProperty);
            }
            set
            {
                SetValue(IsMutedProperty, value);
            }
        }

        /// <summary>
        /// Gets/Sets the ScrubbingEnabled property on the MediaElement.
        /// </summary>
        public bool ScrubbingEnabled
        {
            get
            {
                return (bool) GetValue(ScrubbingEnabledProperty);
            }
            set
            {
                SetValue(ScrubbingEnabledProperty, value);
            }
        }

        /// <summary>
        /// Specifies how the underlying media should behave when the given
        /// MediaElement is unloaded, the default behavior is to Close the
        /// media.
        /// </summary>
        public MediaState UnloadedBehavior
        {
            get
            {
                return (MediaState)GetValue(UnloadedBehaviorProperty);
            }

            set
            {
                SetValue(UnloadedBehaviorProperty, value);
            }
        }

        /// <summary>
        /// Specifies the behavior that the media element should have when it
        /// is loaded. The default behavior is that it is under manual control
        /// (i.e. the caller should call methods such as Play in order to play
        /// the media). If a source is set, then the default behavior changes to
        /// to be playing the media. If a source is set and a loaded behavior is
        /// also set, then the loaded behavior takes control.
        /// </summary>
        public MediaState LoadedBehavior
        {
            get
            {
                return (MediaState)GetValue(LoadedBehaviorProperty);
            }

            set
            {
                SetValue(LoadedBehaviorProperty, value);
            }
        }

        /// <summary>
        /// Returns whether the given media can be paused. This is only valid
        /// after the MediaOpened event has fired.
        /// </summary>
        public bool CanPause
        {
            get
            {
                return _helper.Player.CanPause;
            }
        }

        /// <summary>
        /// Returns whether the given media is currently being buffered. This
        /// applies to network accessed media only.
        /// </summary>
        public bool IsBuffering
        {
            get
            {
                return _helper.Player.IsBuffering;
            }
        }

        /// <summary>
        /// Returns the download progress of the media.
        /// </summary>
        public double DownloadProgress
        {
            get
            {
                return _helper.Player.DownloadProgress;
            }
        }

        /// <summary>
        /// Returns the buffering progress of the media.
        /// </summary>
        public double BufferingProgress
        {
            get
            {
                return _helper.Player.BufferingProgress;
            }
        }

        /// <summary>
        /// Returns the natural height of the media in the video. Only valid after
        /// the MediaOpened event has fired.
        /// </summary>
        public Int32 NaturalVideoHeight
        {
            get
            {
                return _helper.Player.NaturalVideoHeight;
            }
        }

        /// <summary>
        /// Returns the natural width of the media in the video. Only valid after
        /// the MediaOpened event has fired.
        /// </summary>
        public Int32 NaturalVideoWidth
        {
            get
            {
                return _helper.Player.NaturalVideoWidth;
            }
        }

        /// <summary>
        /// Returns whether the given media has audio. Only valid after the
        /// MediaOpened event has fired.
        /// </summary>
        public bool HasAudio
        {
            get
            {
                return _helper.Player.HasAudio;
            }
        }

        /// <summary>
        /// Returns whether the given media has video. Only valid after the
        /// MediaOpened event has fired.
        /// </summary>
        public bool HasVideo
        {
            get
            {
                return _helper.Player.HasVideo;
            }
        }

        /// <summary>
        /// Returns the natural duration of the media. Only valid after the
        /// MediaOpened event has fired.
        /// </summary>
        public Duration NaturalDuration
        {
            get
            {
                return _helper.Player.NaturalDuration;
            }
        }

        /// <summary>
        /// Returns the current position of the media. This is only valid
        /// adter the MediaOpened event has fired.
        /// </summary>
        public TimeSpan Position
        {
            get
            {
                return _helper.Position;
            }

            set
            {
                _helper.SetPosition(value);
            }
        }

        /// <summary>
        /// Allows the speed ration of the media to be controlled.
        /// </summary>
        public double SpeedRatio
        {
            get
            {
                return _helper.SpeedRatio;
            }

            set
            {
                _helper.SetSpeedRatio(value);
            }
        }

        /// <summary>
        /// MediaFailedEvent is a routed event.
        /// </summary>
        public static readonly RoutedEvent MediaFailedEvent =
            EventManager.RegisterRoutedEvent(
                            "MediaFailed",
                            RoutingStrategy.Bubble,
                            typeof(EventHandler<ExceptionRoutedEventArgs>),
                            typeof(MediaElement));
        /// <summary>
        /// Raised when there is a failure in media.
        /// </summary>
        public event EventHandler<ExceptionRoutedEventArgs> MediaFailed
        {
            add { AddHandler(MediaFailedEvent, value); }
            remove { RemoveHandler(MediaFailedEvent, value); }
        }


        /// <summary>
        /// MediaOpened is a routed event.
        /// </summary>
        public static readonly RoutedEvent MediaOpenedEvent =
            EventManager.RegisterRoutedEvent(
                            "MediaOpened",
                            RoutingStrategy.Bubble,
                            typeof(RoutedEventHandler),
                            typeof(MediaElement));

        /// <summary>
        /// Raised when the media is opened
        /// </summary>
        public event RoutedEventHandler MediaOpened
        {
            add { AddHandler(MediaOpenedEvent, value);  }
            remove { RemoveHandler(MediaOpenedEvent, value); }
        }

        /// <summary>
        /// BufferingStarted is a routed event.
        /// </summary>
        public static readonly RoutedEvent BufferingStartedEvent =
            EventManager.RegisterRoutedEvent(
                            "BufferingStarted",
                            RoutingStrategy.Bubble,
                            typeof(RoutedEventHandler),
                            typeof(MediaElement));

        /// <summary>
        /// Raised when buffering starts on the corresponding media.
        /// </summary>
        public event RoutedEventHandler BufferingStarted
        {
            add { AddHandler(BufferingStartedEvent, value); }
            remove { RemoveHandler(BufferingStartedEvent, value); }
        }

        /// <summary>
        /// BufferingEnded is a routed event.
        /// </summary>
        public static readonly RoutedEvent BufferingEndedEvent =
            EventManager.RegisterRoutedEvent(
                            "BufferingEnded",
                            RoutingStrategy.Bubble,
                            typeof(RoutedEventHandler),
                            typeof(MediaElement));

        /// <summary>
        /// Raised when buffering ends on the corresponding media.
        /// </summary>
        public event RoutedEventHandler BufferingEnded
        {
            add { AddHandler(BufferingEndedEvent, value); }
            remove { RemoveHandler(BufferingEndedEvent, value); }
        }

        /// <summary>
        /// ScriptCommand is a routed event.
        /// </summary>
        public static readonly RoutedEvent ScriptCommandEvent =
            EventManager.RegisterRoutedEvent(
                            "ScriptCommand",
                            RoutingStrategy.Bubble,
                            typeof(EventHandler<MediaScriptCommandRoutedEventArgs>),
                            typeof(MediaElement));

        /// <summary>
        /// Raised when a script command in the media is encountered during playback.
        /// </summary>
        public event EventHandler<MediaScriptCommandRoutedEventArgs> ScriptCommand
        {
            add { AddHandler(ScriptCommandEvent, value); }
            remove { RemoveHandler(ScriptCommandEvent, value); }
        }

        /// <summary>
        /// MediaEnded is a routed event
        /// </summary>
        public static readonly RoutedEvent MediaEndedEvent =
            EventManager.RegisterRoutedEvent(
                            "MediaEnded",
                            RoutingStrategy.Bubble,
                            typeof(RoutedEventHandler),
                            typeof(MediaElement));

        /// <summary>
        /// Raised when the corresponding media ends.
        /// </summary>
        public event RoutedEventHandler MediaEnded
        {
            add { AddHandler(MediaEndedEvent, value); }
            remove { RemoveHandler(MediaEndedEvent, value); }
        }

        #endregion

        #region IUriContext implementation
        /// <summary>
        /// Base Uri to use when resolving relative Uri's
        /// </summary>
        Uri IUriContext.BaseUri
        {
            get
            {
                return _helper.BaseUri;
            }
            set
            {
                _helper.BaseUri = value;
            }
        }
        #endregion

        #region Protected Methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new MediaElementAutomationPeer(this);
        }

        /// <summary>
        /// Override for <seealso cref="FrameworkElement.MeasureOverride" />.
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            return MeasureArrangeHelper(availableSize);
        }

        /// <summary>
        /// Override for <seealso cref="FrameworkElement.ArrangeOverride" />.
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            return MeasureArrangeHelper(finalSize);
        }

        //
        // protected override void OnArrange(Size arrangeSize)
        // Because MediaElement does not have children and it is inexpensive to compute it's alignment/size,
        // it does not need an OnArrange override.  It will simply use its own RenderSize (set when its
        // Arrange is called) in OnRender.
        //

        /// <summary>
        /// OnRender is called when the Visual is notified that its contents need to be rendered
        /// This lets the MediaElement element know that it needs to render its contents in the given
        /// DrawingContext
        /// </summary>
        /// <param name="drawingContext">
        /// The DrawingContext to render the video to
        /// </param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            // if nobody set a source on us, then the clock will be null, so we don't render
            // anything
            if (_helper.Player == null)
            {
                return;
            }

            drawingContext.DrawVideo(_helper.Player, new Rect(new Point(), RenderSize));

            return;
        }

        #endregion Protected Methods

        #region Internal Properties / Methods

        /// <summary>
        /// Return the helper object.
        /// </summary>
        internal AVElementHelper Helper
        {
            get
            {
                return _helper;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialization
        /// </summary>
        private void Initialize()
        {
            _helper = new AVElementHelper(this);
        }

        /// <summary>
        /// Contains the code common for MeasureOverride and ArrangeOverride.
        /// </summary>
        /// <param name="inputSize">input size is the parent-provided space that Video should use to "fit in", according to other properties.</param>
        /// <returns>MediaElement's desired size.</returns>
        private Size MeasureArrangeHelper(Size inputSize)
        {
            MediaPlayer mediaPlayer = _helper.Player;

            if (mediaPlayer == null)
            {
                return new Size();
            }

            Size naturalSize = new Size((double)mediaPlayer.NaturalVideoWidth, (double)mediaPlayer.NaturalVideoHeight);

            //get computed scale factor
            Size scaleFactor = Viewbox.ComputeScaleFactor(inputSize,
                                                          naturalSize,
                                                          this.Stretch,
                                                          this.StretchDirection);

            // Returns our minimum size & sets DesiredSize.
            return new Size(naturalSize.Width * scaleFactor.Width, naturalSize.Height * scaleFactor.Height);
        }

        private static void VolumePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange)
            {
                return;
            }

            MediaElement target = ((MediaElement) d);

            if (target != null)
            {
                target._helper.SetVolume((double)e.NewValue);
            }
        }

        private static void BalancePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange)
            {
                return;
            }

            MediaElement target = ((MediaElement) d);

            if (target != null)
            {
                target._helper.SetBalance((double)e.NewValue);
            }
        }

        private static void IsMutedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange)
            {
                return;
            }

            MediaElement target = ((MediaElement) d);

            if (target != null)
            {
                target._helper.SetIsMuted((bool)e.NewValue);
            }
        }

        private static void ScrubbingEnabledPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.IsASubPropertyChange)
            {
                return;
            }

            MediaElement target = ((MediaElement) d);

            if (target != null)
            {
                target._helper.SetScrubbingEnabled((bool)e.NewValue);
            }
        }

        private static
        void
        UnloadedBehaviorPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
            )
        {
            if (e.IsASubPropertyChange)
            {
                return;
            }

            MediaElement target = (MediaElement)d;

            if (target != null)
            {
                target._helper.SetUnloadedBehavior((MediaState)e.NewValue);
            }
        }

        private static
        void
        LoadedBehaviorPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
            )
        {
            if (e.IsASubPropertyChange)
            {
                return;
            }

            MediaElement target = (MediaElement)d;

            if (target != null)
            {
                target._helper.SetLoadedBehavior((MediaState)e.NewValue);
            }
        }

        internal
        void
        OnMediaFailed(
            object sender,
            ExceptionEventArgs args
            )
        {
            RaiseEvent(
                new ExceptionRoutedEventArgs(
                        MediaFailedEvent,
                        this,
                        args.ErrorException));
        }

        internal
        void
        OnMediaOpened(
            object sender,
            EventArgs args
            )
        {
            RaiseEvent(new RoutedEventArgs(MediaOpenedEvent, this));
        }

        internal
        void
        OnBufferingStarted(
            object sender,
            EventArgs args
            )
        {
            RaiseEvent(new RoutedEventArgs(BufferingStartedEvent, this));
        }

        internal
        void
        OnBufferingEnded(
            object sender,
            EventArgs args
            )
        {
            RaiseEvent(new RoutedEventArgs(BufferingEndedEvent, this));
        }

        internal
        void
        OnMediaEnded(
            object sender,
            EventArgs args
            )
        {
            RaiseEvent(new RoutedEventArgs(MediaEndedEvent, this));
        }

        internal
        void
        OnScriptCommand(
            object  sender,
            MediaScriptCommandEventArgs args
            )
        {
            RaiseEvent(
                new MediaScriptCommandRoutedEventArgs(
                        ScriptCommandEvent,
                        this,
                        args.ParameterType,
                        args.ParameterValue));
        }

        #endregion


        #region Data Members

        /// <summary>
        /// Helper object
        /// </summary>
        private AVElementHelper _helper;

        #endregion
    }
}
