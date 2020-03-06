// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls
#else
namespace Microsoft.Windows.Controls
#endif
{

    #region Using declarations

    using System;
    using System.Windows;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    /// <summary>
    ///     Event args for KeyTipService.ActivatingKeyTipEvent
    /// </summary>
    public class ActivatingKeyTipEventArgs : RoutedEventArgs
    {
        #region Constructor

        public ActivatingKeyTipEventArgs()
        {
            RoutedEvent = KeyTipService.ActivatingKeyTipEvent;

            KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter;
            KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetCenter;
            PlacementTarget = null;
            KeyTipHorizontalOffset = 0;
            KeyTipVerticalOffset = 0;
            KeyTipVisibility = Visibility.Visible;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Horizontal placement for keytip
        /// </summary>
        public KeyTipHorizontalPlacement KeyTipHorizontalPlacement
        {
            get;
            set;
        }

        /// <summary>
        ///     Vertical placement for keytip
        /// </summary>
        public KeyTipVerticalPlacement KeyTipVerticalPlacement
        {
            get;
            set;
        }

        /// <summary>
        ///     Placement target for keytip
        /// </summary>
        public UIElement PlacementTarget
        {
            get;
            set;
        }

        /// <summary>
        ///     Horizontal offset from the defined horizontal placement.
        /// </summary>
        public double KeyTipHorizontalOffset
        {
            get
            {
                return _horizontalOffset;
            }
            set
            {
                if (double.IsInfinity(value) || double.IsNaN(value))
                {
                    throw new ArgumentException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.InvalidKeyTipOffset));
                }
                _horizontalOffset = value;
            }
        }

        /// <summary>
        ///     Vertical offset from the defined vertical placement.
        /// </summary>
        public double KeyTipVerticalOffset
        {
            get
            {
                return _verticalOffset;
            }
            set
            {
                if (double.IsInfinity(value) || double.IsNaN(value))
                {
                    throw new ArgumentException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.InvalidKeyTipOffset));
                }
                _verticalOffset = value;
            }
        }

        /// <summary>
        ///     Visibility for the keytip.
        ///     Visible: KeyTip will be visible (if it can) and functional / accessible.
        ///     Hidden: KeyTip will be hidden but will be accessible.
        ///     Collapsed: KeyTip will not be visible and will not be accessible.
        /// </summary>
        public Visibility KeyTipVisibility
        {
            get;
            set;
        }

        /// <summary>
        ///     Used for nudging vertical position to RibbonGroup's top/bottom axis
        /// </summary>
        internal RibbonGroup OwnerRibbonGroup
        {
            get;
            set;
        }

        #endregion

        #region Protected Methods

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            ActivatingKeyTipEventHandler handler = (ActivatingKeyTipEventHandler)genericHandler;
            handler(genericTarget, this);
        }

        #endregion

        #region Private Data

        private double _horizontalOffset = 0;
        private double _verticalOffset = 0;

        #endregion
    }

    /// <summary>
    ///     Event handler type for KeyTipService.ActivatingKeyTipEvent
    /// </summary>
    public delegate void ActivatingKeyTipEventHandler(object sender,
                                   ActivatingKeyTipEventArgs e);
}
