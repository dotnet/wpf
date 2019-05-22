// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Markup;
using System.Diagnostics;
using MS.Internal.KnownBoxes;

namespace System.Windows.Controls
{
    ///<summary>
    /// UserControl Class
    ///</summary>
    public class UserControl : ContentControl
    {
#region Constructors
        static UserControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(UserControl), new FrameworkPropertyMetadata(typeof(UserControl)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(UserControl));

            FocusableProperty.OverrideMetadata(typeof(UserControl), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));
            KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(UserControl), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

            HorizontalContentAlignmentProperty.OverrideMetadata(typeof(UserControl), new FrameworkPropertyMetadata(HorizontalAlignment.Stretch));
            VerticalContentAlignmentProperty.OverrideMetadata(typeof(UserControl), new FrameworkPropertyMetadata(VerticalAlignment.Stretch));
        }

        ///<summary>
        /// Default constructor
        ///</summary>
        public UserControl()
        {
        }
#endregion Constructors

        // Set the EventArgs' source to be this UserControl
        internal override void AdjustBranchSource(RoutedEventArgs e)
        {
            e.Source=this;
        }

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new UserControlAutomationPeer(this);
        }

        /// <summary>
        /// Gets the element that should be used as the StateGroupRoot for VisualStateMangager.GoToState calls
        /// </summary>
        internal override FrameworkElement StateGroupsRoot
        {
            get
            {
                return Content as FrameworkElement;
            }
        }


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
