// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Automation.Peers
#else
namespace Microsoft.Windows.Automation.Peers
#endif
{

    #region Using declarations

    using System;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Media;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    public class RibbonGalleryItemAutomationPeer : FrameworkElementAutomationPeer
    {
        #region constructor

        ///
        public RibbonGalleryItemAutomationPeer(RibbonGalleryItem owner)
            : base(owner)
        { }

        #endregion constructor

        #region AutomationPeer override

        ///
        override protected string GetClassNameCore()
        {
            return "RibbonGalleryItem";
        }

        /// <summary>
        ///   Get KeyTip of the owner control.
        /// </summary>
        protected override string GetAccessKeyCore()
        {
            string accessKey = ((RibbonGalleryItem)Owner).KeyTip;
            if (string.IsNullOrEmpty(accessKey))
            {
                accessKey = base.GetAccessKeyCore();
            }
            return accessKey;
        }

        /// <summary>
        ///   Returns help text 
        /// </summary>
        protected override string GetHelpTextCore()
        {
            string helpText = base.GetHelpTextCore();
            if (String.IsNullOrEmpty(helpText))
            {
                RibbonToolTip toolTip = ((RibbonGalleryItem)Owner).ToolTip as RibbonToolTip;
                if (toolTip != null)
                {
                    helpText = toolTip.Description;
                }
            }

            return helpText;
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ListItem;
        }

#if !RIBBON_IN_FRAMEWORK
        ///
        override protected bool IsOffscreenCore()
        {
            if (!Owner.IsVisible)
                return true;

            Rect boundingRect = RibbonHelper.CalculateVisibleBoundingRect(Owner);
            return (boundingRect == Rect.Empty || boundingRect.Height == 0 || boundingRect.Width == 0);
        }
#endif

        #endregion AutomationPeer override

        #region internal static helper

        #endregion

        #region Selection Events
        // BUG 1555137: Never inline, as we don't want to unnecessarily link the automation DLL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseAutomationIsSelectedChanged(bool isSelected)
        {
            if (EventsSource != null)
            {
                EventsSource.RaisePropertyChangedEvent(
                    SelectionItemPatternIdentifiers.IsSelectedProperty,
                        !isSelected,
                        isSelected);
            }
        }


        // Selection Events needs to be raised on DataItem Peers now when they exist.
        internal void RaiseAutomationSelectionEvent(AutomationEvents eventId)
        {
            if (EventsSource != null)
            {
                EventsSource.RaiseAutomationEvent(eventId);
            }
        }

        #endregion Selection Events
    }
}
