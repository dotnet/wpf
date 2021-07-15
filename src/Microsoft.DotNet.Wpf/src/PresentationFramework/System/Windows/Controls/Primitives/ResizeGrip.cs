// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Implements the ResizeGrip control
//

using System;
using System.Diagnostics;

using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Shapes;
#if OLD_AUTOMATION
using System.Windows.Automation.Provider;
#endif

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     The ResizeGrip control enables the Window object to have a resize grip.  This control should be 
    ///     made part of the Window's style visual tree.
    /// </summary>
    /// <remarks>
    ///     
    /// </remarks>
#if OLD_AUTOMATION
    [Automation(AccessibilityControlType = "Thumb")]
#endif
    public class ResizeGrip : Control
    {
        //----------------------------------------------
        //
        // Constructors
        //
        //----------------------------------------------
        #region Constructors
        static ResizeGrip()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ResizeGrip), new FrameworkPropertyMetadata(typeof(ResizeGrip)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(ResizeGrip));

            Window.IWindowServiceProperty.OverrideMetadata(
                    typeof(ResizeGrip), 
                    new FrameworkPropertyMetadata(new PropertyChangedCallback(_OnWindowServiceChanged)));
        }
        /// <summary>
        ///     Default ResizeGrip constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public ResizeGrip() : base()
        {
        }

        #endregion Constructors

        //----------------------------------------------
        //
        // Private Methods
        //
        //----------------------------------------------
        #region Private Methods

        private static void _OnWindowServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ResizeGrip rg = d as ResizeGrip;
            Debug.Assert(rg != null, "DependencyObject must be of type ResizeGrip.");

            rg.OnWindowServiceChanged(e.OldValue as Window, e.NewValue as Window);
        }

        /// <summary>
        ///     When IWindowService is invalidated, it means that this control is either placed into
        ///     a window's visual tree or taken out.  If we are in a new Window's visual tree, we 
        ///     want to set the reference to this object inside the Window.  Window uses this
        ///     reference in its WM_NCHITTEST code
        /// </summary>
        private void OnWindowServiceChanged(Window oldWindow, Window newWindow)
        {
            if ((oldWindow != null) && (oldWindow != newWindow))
            {
                oldWindow.ClearResizeGripControl(this);
            }

            if (newWindow != null)
            {
                newWindow.SetResizeGripControl(this);
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
        
        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 28; }
        }
        #endregion Private Methods
    }
}

