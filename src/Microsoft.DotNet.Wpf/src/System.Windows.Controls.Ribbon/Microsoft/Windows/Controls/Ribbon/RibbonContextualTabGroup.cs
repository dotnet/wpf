// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    #region Using declarations

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Markup;
#if RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Automation.Peers;
#endif

    #endregion

    /// <summary>
    ///     Item container for RibbonContextualTabGroupItemsControl
    /// </summary>
    [ContentProperty("Header")]
    public class RibbonContextualTabGroup : Control
    {
        #region Fields
        
        internal const double TabHeaderSeparatorHeightDelta = 4.0;

        private TabsEnumerable _tabs = null;
        
        #endregion

        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonContextualTabGroup class.  Also
        ///   overrides the default style.
        /// </summary>
        static RibbonContextualTabGroup()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonContextualTabGroup), new FrameworkPropertyMetadata(typeof(RibbonContextualTabGroup)));
            VisibilityProperty.OverrideMetadata(typeof(RibbonContextualTabGroup), new FrameworkPropertyMetadata(Visibility.Collapsed, new PropertyChangedCallback(OnVisibilityChanged), new CoerceValueCallback(CoerceVisibility)));
            FocusableProperty.OverrideMetadata(typeof(RibbonContextualTabGroup), new FrameworkPropertyMetadata(false));
#if RIBBON_IN_FRAMEWORK
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideMetadata(typeof(RibbonContextualTabGroup), new FrameworkPropertyMetadata(IsOffscreenBehavior.FromClip));
#endif
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     The DependencyProperty for the Header property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty HeaderProperty =
                DependencyProperty.Register(
                        "Header",
                        typeof(object),
                        typeof(RibbonContextualTabGroup),
                        new FrameworkPropertyMetadata(
                                (object) null,
                                new PropertyChangedCallback(OnHeaderChanged)));


        /// <summary>
        ///     Header is the data used to for the header of each item in the control.
        /// </summary>
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>
        ///     Called when HeaderProperty is invalidated on "d."
        /// </summary>
        private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonContextualTabGroup ctrl = (RibbonContextualTabGroup) d;
            ctrl.OnHeaderChanged(e.OldValue, e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the Header property changes.
        /// </summary>
        /// <param name="oldHeader">The old value of the Header property.</param>
        /// <param name="newHeader">The new value of the Header property.</param>
        protected virtual void OnHeaderChanged(object oldHeader, object newHeader)
        {
            RemoveLogicalChild(oldHeader);
            AddLogicalChild(newHeader);
            UpdateTabs(false /* clear */);
        }

        /// <summary>
        ///     The DependencyProperty for the HeaderTemplate property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateProperty =
                DependencyProperty.Register(
                        "HeaderTemplate",
                        typeof(DataTemplate),
                        typeof(RibbonContextualTabGroup),
                        new FrameworkPropertyMetadata(
                                (DataTemplate) null));

        /// <summary>
        ///     HeaderTemplate is the template used to display the <seealso cref="Header"/>.
        /// </summary>
        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate) GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }


        /// <summary>
        ///     The DependencyProperty for the HeaderTemplateSelector property.
        ///     Flags:              none
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateSelectorProperty =
                DependencyProperty.Register(
                        "HeaderTemplateSelector",
                        typeof(DataTemplateSelector),
                        typeof(RibbonContextualTabGroup),
                        new FrameworkPropertyMetadata(
                                (DataTemplateSelector) null));

        /// <summary>
        ///     HeaderTemplateSelector allows the application writer to provide custom logic
        ///     for choosing the template used to display the <seealso cref="Header"/>.
        /// </summary>
        /// <remarks>
        ///     This property is ignored if <seealso cref="HeaderTemplate"/> is set.
        /// </remarks>
        public DataTemplateSelector HeaderTemplateSelector
        {
            get { return (DataTemplateSelector) GetValue(HeaderTemplateSelectorProperty); }
            set { SetValue(HeaderTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the HeaderStringFormat property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty HeaderStringFormatProperty =
                DependencyProperty.Register(
                        "HeaderStringFormat",
                        typeof(String),
                        typeof(RibbonContextualTabGroup),
                        new FrameworkPropertyMetadata(
                                (String) null));


        /// <summary>
        ///     HeaderStringFormat is the format used to display the header content as a string.
        ///     This arises only when no template is available.
        /// </summary>
        public String HeaderStringFormat
        {
            get { return (String) GetValue(HeaderStringFormatProperty); }
            set { SetValue(HeaderStringFormatProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for Ribbon property.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonContextualTabGroup));

        /// <summary>
        ///     This property is used to access Ribbon
        /// </summary>
        public Ribbon Ribbon
        {
            get { return RibbonControlService.GetRibbon(this); }
        }

        #endregion

        #region Internal Properties

        /// <summary>
        ///   Gets or sets a value indicating whether the contextual tab group's label should be shown.
        /// </summary>
        internal bool ShowLabelToolTip
        {
            get { return RibbonHelper.GetIsContentTooltip(VisualChild, Header); }
            set { RibbonHelper.SetContentAsToolTip(this, VisualChild, Header, value); }
        }

        private FrameworkElement VisualChild
        {
            get
            {
                return VisualChildrenCount == 0 ? null : (GetVisualChild(0) as FrameworkElement);
            }
        }

        internal IEnumerable<RibbonTab> Tabs
        {
            get
            {
                if (_tabs == null)
                {
                    _tabs = new TabsEnumerable(this);
                }
                return _tabs;
            }
        }

        internal RibbonTab FirstVisibleTab
        {
            get
            {
                IEnumerable<RibbonTab> tabs = Tabs;
                if (tabs != null)
                {
                    foreach (RibbonTab tab in tabs)
                    {
                        if (tab.Visibility == Visibility.Visible)
                        {
                            return tab;
                        }
                    }
                }
                return null;
            }
        }

        /// <summary>
        ///   Gets or sets the sum of DesiredSize.Width of TabHeaders of this group. 
        /// </summary>
        internal double TabsDesiredWidth
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the width required to display the full Header without trimming. 
        /// </summary>
        internal double IdealDesiredWidth
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Padding to be added to each TabHeader in this group, 
        /// such that ContextualTabGroupHeader can be displayed in its full length.
        /// Its computed as (IdealDesiredWidth - TabsDesiredWidth)/ (Number of visible Tabs in this group)
        /// </summary>
        internal double DesiredExtraPaddingPerTab
        {
            get;
            set;
        }

        /// <summary>
        ///  Gets or sets the arrange width of the contextual tab group.
        /// </summary>
        internal double ArrangeWidth
        {
            get;
            set;
        }

        /// <summary>
        ///   Gets or sets the arrange x-coordinate of the contextual tab group.
        /// </summary>
        internal double ArrangeX
        {
            get;
            set;
        }

        #endregion

        #region Protected Methods

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonContextualTabGroupAutomationPeer(this);
        }

        /// <summary>
        ///   Callback for mouse down.  Captures/Releases the mouse depending on whether
        ///   the tab group was clicked.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 || e.ChangedButton == MouseButton.Right )
            {
                this.CaptureMouse();
                if (this.IsMouseCaptured)
                {
                    // Though we have already checked this state, our call to CaptureMouse
                    // could also end up changing the state, so we check it again.
                    if (e.ButtonState != MouseButtonState.Pressed)
                    {
                        // Release capture since we decided not to press the button.
                        this.ReleaseMouseCapture();
                    }
                }
                e.Handled = true;
            }
            else if( e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
            {
                // On DoubleClick maximize/restore the window
                RibbonWindow ribbonWindow = Window.GetWindow(this) as RibbonWindow; 
                if( ribbonWindow != null )
                {
#if RIBBON_IN_FRAMEWORK
                    if (SystemCommands.MaximizeWindowCommand.CanExecute(null, ribbonWindow))
                    {
                        SystemCommands.MaximizeWindowCommand.Execute( /*parameter*/ null, /* target*/ ribbonWindow);
                        e.Handled = true;
                    }
                    else if (SystemCommands.RestoreWindowCommand.CanExecute(null, ribbonWindow))
                    {
                        SystemCommands.RestoreWindowCommand.Execute(/*parameter*/ null, /* target*/ ribbonWindow);
                        e.Handled = true;
                    }
#else
                    if (Microsoft.Windows.Shell.SystemCommands.MaximizeWindowCommand.CanExecute(null, ribbonWindow))
                    {
                        Microsoft.Windows.Shell.SystemCommands.MaximizeWindowCommand.Execute( /*parameter*/ null, /* target*/ ribbonWindow);
                        e.Handled = true;
                    }
                    else if (Microsoft.Windows.Shell.SystemCommands.RestoreWindowCommand.CanExecute(null, ribbonWindow))
                    {
                        Microsoft.Windows.Shell.SystemCommands.RestoreWindowCommand.Execute(/*parameter*/ null, /* target*/ ribbonWindow);
                        e.Handled = true;
                    }
#endif
                }
            }

            base.OnMouseDown(e);
        }

        /// <summary>
        ///   Callback for mouse up, releases mouse capture and sends click notifications.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (this.IsMouseCaptured)
            {
                this.ReleaseMouseCapture();
                if (IsMouseOver)
                {
                    if (e.ChangedButton == MouseButton.Left && this.Ribbon != null)
                    {
                        // Selects the first tab in this contextual group.
                        this.Ribbon.NotifyMouseClickedOnContextualTabGroup(this);
                        e.Handled = true;
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        // Show SystemMenu
                        RibbonWindow ribbonWindow = Window.GetWindow(this) as RibbonWindow;
                        if (ribbonWindow != null)
                        {
#if RIBBON_IN_FRAMEWORK
                            if (SystemCommands.ShowSystemMenuCommand.CanExecute(null, ribbonWindow))
                            {
                                SystemCommands.ShowSystemMenuCommand.Execute( /*parameter*/ null, /* target*/ ribbonWindow);
                                e.Handled = true;
                            }
#else
                            if (Microsoft.Windows.Shell.SystemCommands.ShowSystemMenuCommand.CanExecute(null, ribbonWindow))
                            {
                                Microsoft.Windows.Shell.SystemCommands.ShowSystemMenuCommand.Execute( /*parameter*/ null, /* target*/ ribbonWindow);
                                e.Handled = true;
                            }
#endif
                        }
                    }
                }
            }

            base.OnMouseUp(e);
        }

        #endregion

        #region Private Methods

        internal void PrepareTabGroupHeader(object item,
                                        DataTemplate itemTemplate,
                                        DataTemplateSelector itemTemplateSelector,
                                        string itemStringFormat)
        {
            UpdateTabs(false /* clear */);

            if (item != this)
            {
                // copy styles from the ItemsControl
                if (PropertyHelper.IsDefaultValue(this, HeaderProperty))
                {
                    Header = item;
                }
                if (itemTemplate != null)
                    SetValue(HeaderTemplateProperty, itemTemplate);
                if (itemTemplateSelector != null)
                    SetValue(HeaderTemplateSelectorProperty, itemTemplateSelector);
                if (itemStringFormat != null)
                    SetValue(HeaderStringFormatProperty, itemStringFormat);
            }
        }

        internal void ClearTabGroupHeader()
        {
            UpdateTabs(true /* clear */);
        }

        private static object CoerceVisibility(DependencyObject d, object baseValue)
        {
            // Always coerce Hidden to Collapsed
            if ((Visibility)baseValue == Visibility.Hidden)
                return Visibility.Collapsed;
            return baseValue;
        }

        /// <summary>
        ///   Callback for Visibility property changed.  
        /// </summary>
        /// <param name="sender">The RibbonContextualTabGroup whose Visibility property changed.</param>
        /// <param name="e">The event data.</param>
        private static void OnVisibilityChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((RibbonContextualTabGroup)sender).CoerceTabsVisibility();
        }

        private void UpdateTabs(bool clear)
        {
            if (Ribbon != null)
            {
                for (int i = 0; i < Ribbon.Items.Count; i++)
                {
                    RibbonTab tab = Ribbon.ItemContainerGenerator.ContainerFromIndex(i) as RibbonTab;
                    if (tab != null && tab.IsContextualTab && Object.Equals(tab.ContextualTabGroupHeader, Header))
                    {
                        if (!clear)
                        {
                            if (tab.ContextualTabGroup == null)
                            {
                                tab.ContextualTabGroup = this;
                                tab.CoerceValue(VisibilityProperty);
                            }
                        }
                        else
                        {
                            tab.CoerceValue(VisibilityProperty);
                            tab.ContextualTabGroup = null;
                        }
                    }
                }
            }
        }

        private void CoerceTabsVisibility()
        {
            IEnumerable<RibbonTab> tabs = Tabs;
            if (tabs != null)
            {
                foreach (RibbonTab tab in tabs)
                {
                    tab.CoerceValue(VisibilityProperty);
                }
            }
        }

        #endregion

        #region Tabs

        private class TabsEnumerable : IEnumerable<RibbonTab>
        {
            #region Constructor And Properties

            public TabsEnumerable(RibbonContextualTabGroup tabGroup)
            {
                ContextualTabGroup = tabGroup;
            }

            private RibbonContextualTabGroup ContextualTabGroup
            {
                get;
                set;
            }

            #endregion

            #region IEnumerable<RibbonTab> Members

            public IEnumerator<RibbonTab> GetEnumerator()
            {
                Ribbon ribbon = ContextualTabGroup.Ribbon;
                if (ribbon != null)
                {
                    int itemCount = ribbon.Items.Count;
                    for (int i = 0; i < itemCount; i++)
                    {
                        RibbonTab tab = ribbon.ItemContainerGenerator.ContainerFromIndex(i) as RibbonTab;
                        if (tab != null &&
                            tab.IsContextualTab &&
                            object.ReferenceEquals(ContextualTabGroup, tab.ContextualTabGroup))
                        {
                            yield return tab;
                        }
                    }
                }
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion
        }

        #endregion
    }
}
