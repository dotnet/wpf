// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    using System;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Threading;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon.Primitives;
#else
    using Microsoft.Windows.Automation.Peers;
    using Microsoft.Windows.Controls.Ribbon.Primitives;
#endif

    /// <summary>
    ///     The ItemsControl which hosts TabHeaders in Ribbon.
    /// </summary>
    public class RibbonTabHeaderItemsControl : ItemsControl
    {
        #region Constructors

        static RibbonTabHeaderItemsControl()
        {
            Type ownerType = typeof(RibbonTabHeaderItemsControl);
            ItemsPanelProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new ItemsPanelTemplate(new FrameworkElementFactory(typeof(RibbonTabHeadersPanel)))));
            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(typeof(RibbonTabHeaderItemsControl)));
            FocusableProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));
            KeyboardNavigation.ControlTabNavigationProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));
        }

        #endregion

        #region Properties

#if RIBBON_IN_FRAMEWORK
        protected internal override bool HandlesScrolling
#else
        protected override bool HandlesScrolling
#endif
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        ///     Items panel instance of this ItemsControl
        /// </summary>
        internal Panel InternalItemsHost
        {
            get
            {
                return _itemsHost;
            }
            set
            {
                _itemsHost = value;
            }
        }

        #endregion

        #region Protected Methods

        public override void OnApplyTemplate()
        {
            // If a new template has just been generated then 
            // be sure to clear any stale ItemsHost references
            if (InternalItemsHost != null && !this.IsAncestorOf(InternalItemsHost))
            {
                InternalItemsHost = null;
            }

            base.OnApplyTemplate();
        }
        
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonTabHeaderItemsControlAutomationPeer(this);
        }

        /// <summary>
        ///     Returns a new instance of RibbonTabHeader as the item container
        /// </summary>
        /// <returns></returns>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new RibbonTabHeader();
        }

        /// <summary>
        ///     Prepares an item container before its use.
        /// </summary>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            RibbonTabHeader header = element as RibbonTabHeader;
            if (header != null)
            {
                header.PrepareRibbonTabHeader();
            }
        }

        /// <summary>
        ///     An item is its own container if it is a RibbonTabHeader
        /// </summary>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return (item is RibbonTabHeader);
        }

        #endregion
        
        #region Internal Methods

        /// <summary>
        ///     Helper method which scrolls item at given index into view.
        /// </summary>
        /// <param name="index"></param>
        internal void ScrollIntoView(int index)
        {
            if (index < 0 || index >= Items.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                ScrollContainerIntoView(index);
            }
            else
            {
                // The items aren't generated, try at a later time
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(ScrollContainerIntoView), index);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Helper method which scrolls item at given index into view.
        ///     Can be used as a dispatcher operation.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private object ScrollContainerIntoView(object arg)
        {
            int index = (int)arg;
            FrameworkElement element = ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
            if (element != null)
            {
                element.BringIntoView();

                // If there is a margin on TabHeader on the end, BringIntoView call
                // may not scroll to the end. Explicitly scroll to end in such cases.
                IScrollInfo scrollInfo = InternalItemsHost as IScrollInfo;
                if (scrollInfo != null)
                {
                    ScrollViewer scrollViewer = scrollInfo.ScrollOwner;
                    if (scrollViewer != null)
                    {
                        Ribbon ribbon = RibbonControlService.GetRibbon(this);
                        if (ribbon != null)
                        {
                            int displayIndex = ribbon.GetTabDisplayIndexForIndex(index);
                            if (displayIndex == 0)
                            {
                                // If this tab header is the first tab header displayed
                                // then scroll to the left end.
                                scrollViewer.ScrollToLeftEnd();
                            }
                            else if (ribbon.GetTabIndexForDisplayIndex(displayIndex + 1) < 0)
                            {
                                // If this tab header is the last tab header displayed
                                // then scroll to the right end.
                                scrollViewer.ScrollToRightEnd();
                            }
                        }
                    }
                }
            }
            return null;
        }

        #endregion

        #region Private Data

        private Panel _itemsHost; // ItemsPanel instance for this ItemsControl

        #endregion
    }
}
