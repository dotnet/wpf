// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using MS.Internal.KnownBoxes;
using MS.Internal.Telemetry.PresentationFramework;

namespace System.Windows.Controls
{
    /// <summary>
    /// Specifies the expanding direction of a expansion.
    /// </summary>
    public enum ExpandDirection
    {
        /// <summary>
        /// Expander will expand to the down direction.
        /// </summary>
        Down = 0,
        /// <summary>
        /// Expander will expand to the up direction.
        /// </summary>
        Up = 1,
        /// <summary>
        /// Expander will expand to the left direction.
        /// </summary>
        Left = 2,
        /// <summary>
        /// Expander will expand to the right direction.
        /// </summary>
        Right = 3,
    }

    /// <summary>
    /// An Expander control allows a user to view a header and expand that 
    /// header to see further details of the content, or to collapse the section 
    /// up to the header to save space. 
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)] // cannot be read & localized as string    
    public class Expander : HeaderedContentControl
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        static Expander()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Expander), new FrameworkPropertyMetadata(typeof(Expander)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(Expander));

            IsTabStopProperty.OverrideMetadata(typeof(Expander), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

            IsMouseOverPropertyKey.OverrideMetadata(typeof(Expander), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));
            IsEnabledProperty.OverrideMetadata(typeof(Expander), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));

            ControlsTraceLogger.AddControl(TelemetryControls.Expander);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// ExpandDirection specifies to which direction the content will expand
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public ExpandDirection ExpandDirection
        {
            get { return (ExpandDirection) GetValue(ExpandDirectionProperty); }
            set { SetValue(ExpandDirectionProperty, value); }
        }

        /// <summary>
        /// The DependencyProperty for the ExpandDirection property.
        /// Default Value: ExpandDirection.Down
        /// </summary>
        public static readonly DependencyProperty ExpandDirectionProperty =
                DependencyProperty.Register(
                        "ExpandDirection",
                        typeof(ExpandDirection),
                        typeof(Expander),
                        new FrameworkPropertyMetadata(
                                ExpandDirection.Down /* default value */,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                new PropertyChangedCallback(OnVisualStatePropertyChanged)),
                        new ValidateValueCallback(IsValidExpandDirection));

        private static bool IsValidExpandDirection(object o)
        {
            ExpandDirection value = (ExpandDirection)o;

            return (value == ExpandDirection.Down ||
                    value == ExpandDirection.Left ||
                    value == ExpandDirection.Right ||
                    value == ExpandDirection.Up);
        }

        /// <summary>
        ///     The DependencyProperty for the IsExpanded property.
        ///     Default Value: false
        /// </summary>
        public static readonly DependencyProperty IsExpandedProperty =
                DependencyProperty.Register(
                        "IsExpanded",
                        typeof(bool),
                        typeof(Expander),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox /* default value */,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
                                new PropertyChangedCallback(OnIsExpandedChanged)));

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Expander ep = (Expander) d;
            
            bool newValue = (bool) e.NewValue;

            // Fire accessibility event
            ExpanderAutomationPeer peer = UIElementAutomationPeer.FromElement(ep) as ExpanderAutomationPeer;
            if(peer != null)
            {
                peer.RaiseExpandCollapseAutomationEvent(!newValue, newValue);
            }

            if (newValue)
            {
                ep.OnExpanded();
            }
            else
            {
                ep.OnCollapsed();
            }

            ep.UpdateVisualState();
        }

        /// <summary>
        /// IsExpanded indicates whether the expander is currently expanded.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public bool IsExpanded
        {
            get { return (bool) GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        /// Expanded event.
        /// </summary>
        public static readonly RoutedEvent ExpandedEvent =
            EventManager.RegisterRoutedEvent("Expanded",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(Expander)
            );

        /// <summary>
        /// Expanded event. It is fired when IsExpanded changed from false to true.
        /// </summary>
        public event RoutedEventHandler Expanded
        {
            add { AddHandler(ExpandedEvent, value); }
            remove { RemoveHandler(ExpandedEvent, value); }
        }

        /// <summary>
        /// Collapsed event.
        /// </summary>
        public static readonly RoutedEvent CollapsedEvent =
            EventManager.RegisterRoutedEvent("Collapsed",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(Expander)
            );

        /// <summary>
        /// Collapsed event. It is fired when IsExpanded changed from true to false.
        /// </summary>
        public event RoutedEventHandler Collapsed
        {
            add { AddHandler(CollapsedEvent, value); }
            remove { RemoveHandler(CollapsedEvent, value); }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        internal override void ChangeVisualState(bool useTransitions)
        {
            // Handle the Common states
            if (!IsEnabled)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateDisabled, VisualStates.StateNormal);
            }
            else if (IsMouseOver)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateMouseOver, VisualStates.StateNormal);
            }
            else
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateNormal);
            }

            // Handle the Focused states
            if (IsKeyboardFocused)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateFocused, VisualStates.StateUnfocused);
            }
            else
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateUnfocused);
            }
        
            if (IsExpanded)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateExpanded);
            }
            else
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateCollapsed);
            }

            switch (ExpandDirection)
            {
                case ExpandDirection.Down:
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateExpandDown); 
                    break;
                
                case ExpandDirection.Up:
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateExpandUp); 
                    break;

                case ExpandDirection.Left:
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateExpandLeft); 
                    break;
                
                default:
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateExpandRight); 
                    break;
            }
            
            base.ChangeVisualState(useTransitions);
        }

        /// <summary>
        /// A virtual function that is called when the IsExpanded property is changed to true. 
        /// Default behavior is to raise an ExpandedEvent.
        /// </summary>
        protected virtual void OnExpanded()
        {
            RoutedEventArgs args = new RoutedEventArgs();
            args.RoutedEvent = Expander.ExpandedEvent;
            args.Source = this;
            RaiseEvent(args);
        }

        /// <summary>
        /// A virtual function that is called when the IsExpanded property is changed to false. 
        /// Default behavior is to raise a CollapsedEvent.
        /// </summary>
        protected virtual void OnCollapsed()
        {
            RaiseEvent(new RoutedEventArgs(Expander.CollapsedEvent, this));
        }

        #endregion
        
        #region Accessibility

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer() 
        {
            return new ExpanderAutomationPeer(this);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  FrameworkElement overrides
        //
        //-------------------------------------------------------------------

        #region FrameworkElement overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            
            _expanderToggleButton = GetTemplateChild(ExpanderToggleButtonTemplateName) as ToggleButton;
        }
        
        #endregion
        
        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties
        
        internal bool IsExpanderToggleButtonFocused
        {
            get
            {
                return _expanderToggleButton?.IsKeyboardFocused ?? false;
            }
        }
        
        internal ToggleButton ExpanderToggleButton
        {
            get
            {
                return _expanderToggleButton;
            }
        }
        
        #endregion
        
        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private const string ExpanderToggleButtonTemplateName = "HeaderSite";
        private ToggleButton _expanderToggleButton;
        
        #endregion

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default 
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey
    }
}
