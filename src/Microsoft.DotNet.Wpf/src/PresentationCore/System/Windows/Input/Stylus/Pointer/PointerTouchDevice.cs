// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using MS.Internal.PresentationCore;
using System.Security;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace System.Windows.Input.StylusPointer
{
    /// <summary>
    /// A WM_POINTER specific implementation of StylusTouchDeviceBase.
    /// </summary>
    internal sealed class PointerTouchDevice : StylusTouchDeviceBase
    {
        #region Member Variables

        /// <summary>
        /// The owning StylusDevice
        /// </summary>
        private PointerStylusDevice _stylusDevice;

        #endregion 

        #region Constructor

        internal PointerTouchDevice(PointerStylusDevice stylusDevice)
            : base(stylusDevice)
        {
            _stylusDevice = stylusDevice;
        }

        #endregion

        #region Manipulation

        protected override void OnManipulationEnded(bool cancel)
        {
            base.OnManipulationEnded(cancel);

            if (cancel)
            {
                // Set PromotingToOther to true because the
                // the manipulation has been canceled and hence
                // we want the promotions to happen.
                PromotingToOther = true;
            }
            else
            {
                // Set PromotingToOther to false because we
                // dont want any mouse promotions to happen for
                // rest of this touch cycle.
                PromotingToOther = false;
            }
        }

        protected override void OnManipulationStarted()
        {
            base.OnManipulationStarted();
            PromotingToOther = false;
        }

        #endregion

        #region StylusTouchDeviceBase Implementation

        /// <summary>
        /// Get the width or height of the stylus point's bounding box.
        /// </summary>
        /// <param name="stylusPoint">The point for which the width or height is being calculated</param>
        /// <param name="isWidth">True if this should calculate width, false for height</param>
        /// <returns>The width or height of the stylus poing</returns>
        protected override double GetStylusPointWidthOrHeight(StylusPoint stylusPoint, bool isWidth)
        {
            double pixelsPerInch = DpiUtil.DefaultPixelsPerInch;

            // If we have an active source and root visual use the DPI from there
            if (ActiveSource?.RootVisual != null)
            {
                pixelsPerInch = VisualTreeHelper.GetDpi(ActiveSource.RootVisual).PixelsPerInchX;
            }

            StylusPointProperty property = (isWidth ? StylusPointProperties.Width : StylusPointProperties.Height);

            double value = 0d;

            if (stylusPoint.HasProperty(property))
            {
                // Get the property value in the corresponding units
                value = (double)stylusPoint.GetPropertyValue(property);

                StylusPointPropertyInfo propertyInfo = stylusPoint.Description.GetPropertyInfo(property);

                if (!DoubleUtil.AreClose(propertyInfo.Resolution, 0d))
                {
                    value /= propertyInfo.Resolution;
                }
                else
                {
                    value = 0;
                }

                // Convert the value to Inches
                if (propertyInfo.Unit == StylusPointPropertyUnit.Centimeters)
                {
                    value /= CentimetersPerInch;
                }

                // Convert the value to pixels
                value *= pixelsPerInch;
            }

            return value;
        }

        protected override void OnActivateImpl()
        {
            // Note this is not needed for pointer stack.  It is here that WISP stack
            // sets primary pointer for mouse promotion, but this is now tracked by windows.
        }

        protected override void OnDeactivateImpl()
        {
            // Note this is not needed for the pointer stack.  In the WISP stack this clears
            // mouse promotion and staging vars.
        }

        #endregion
    }
}
