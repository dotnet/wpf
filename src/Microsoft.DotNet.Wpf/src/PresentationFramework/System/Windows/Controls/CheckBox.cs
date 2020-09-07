// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Threading;

using System.Windows.Automation;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MS.Utility;
using MS.Internal.KnownBoxes;
using MS.Internal.Telemetry.PresentationFramework;

// Disable CS3001: Warning as Error: not CLS-compliant
#pragma warning disable 3001

namespace System.Windows.Controls
{
    #region CheckBox class
    /// <summary>
    ///     Use a CheckBox to give the user an option, such as true/false.
    ///     CheckBox allow the user to choose from a list of options.
    ///     CheckBox controls let the user pick a combination of options.
    /// </summary>
    [DefaultEvent("CheckStateChanged")]
    [Localizability(LocalizationCategory.CheckBox)]
    public class CheckBox : ToggleButton
    {
        #region Constructors

        static CheckBox()
        {
            // Set the default Style
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CheckBox), new FrameworkPropertyMetadata(typeof(CheckBox)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(CheckBox));

            KeyboardNavigation.AcceptsReturnProperty.OverrideMetadata(typeof(CheckBox), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

            ControlsTraceLogger.AddControl(TelemetryControls.CheckBox);
        }

        /// <summary>
        ///     Default CheckBox constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public CheckBox() : base()
        {
        }
        #endregion

        #region Override methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new System.Windows.Automation.Peers.CheckBoxAutomationPeer(this);
        }

        /// <summary>
        /// This is the method that responds to the KeyDown event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Add aditional keys "+" and "-" when we are not in IsThreeState mode
            if (!IsThreeState)
            {
                if (e.Key == Key.OemPlus || e.Key == Key.Add)
                {
                    e.Handled = true;
                    ClearValue(IsPressedPropertyKey);
                    SetCurrentValueInternal(IsCheckedProperty, BooleanBoxes.TrueBox);
                }
                else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
                {
                    e.Handled = true;
                    ClearValue(IsPressedPropertyKey);
                    SetCurrentValueInternal(IsCheckedProperty, BooleanBoxes.FalseBox);
                }
            }
        }

        /// <summary>
        /// The Access key for this control was invoked.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnAccessKey(AccessKeyEventArgs e)
        {
            if (!IsKeyboardFocused)
            {
                Focus();
            }

            base.OnAccessKey(e);
        }

        #endregion

        #region Data
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
    #endregion CheckBox class
}
