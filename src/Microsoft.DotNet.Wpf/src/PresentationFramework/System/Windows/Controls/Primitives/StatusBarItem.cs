// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using MS.Internal.KnownBoxes;


namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     Control that implements a item inside a StatusBar.
    /// </summary>
    [Localizability(LocalizationCategory.Inherit)]
    public class StatusBarItem : ContentControl
    {
        #region Constructors

        static StatusBarItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StatusBarItem), new FrameworkPropertyMetadata(typeof(StatusBarItem)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(StatusBarItem));

            IsTabStopProperty.OverrideMetadata(typeof(StatusBarItem), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideMetadata(typeof(StatusBarItem), new FrameworkPropertyMetadata(IsOffscreenBehavior.FromClip));
        }

        #endregion

        #region Accessibility

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new StatusBarItemAutomationPeer(this);
        }

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
