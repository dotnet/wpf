// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls
#else
namespace Microsoft.Windows.Controls
#endif
{
    /// <summary>
    ///     The Control used inside the KeyTip
    /// </summary>
    public class KeyTipControl : Control
    {
        static KeyTipControl()
        {
            Type ownerType = typeof(KeyTipControl);
            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            IsHitTestVisibleProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));
            FocusableProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));
            EventManager.RegisterClassHandler(ownerType, SizeChangedEvent, new SizeChangedEventHandler(OnSizeChanged), true);
        }

        internal KeyTipAdorner KeyTipAdorner { get; set; }

        /// <summary>
        ///     Notify corresponding KeyTipAdorner regarding size change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            KeyTipControl keyTipControl = sender as KeyTipControl;
            if (keyTipControl != null &&
                keyTipControl.KeyTipAdorner != null)
            {
                keyTipControl.KeyTipAdorner.OnKeyTipControlSizeChanged(e);
            }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
                DependencyProperty.Register(
                        "Text",
                        typeof(string),
                        typeof(KeyTipControl),
                        new FrameworkPropertyMetadata(
                                string.Empty,
                                FrameworkPropertyMetadataOptions.AffectsMeasure |
                                FrameworkPropertyMetadataOptions.AffectsRender));
    }
}