// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Test.Win32;

namespace Microsoft.Test.Stability.Extensions.CustomTypes
{
    public class CustomHwndHostControl : HwndHost
    {
        /// <summary>
        /// Serialization need the default Constructor. 
        /// </summary>
        public CustomHwndHostControl() { }

        #region Protected Members

        protected override HandleRef BuildWindowCore(HandleRef parent)
        {
            HwndSourceParameters parameters = new HwndSourceParameters("HostedAvalon", 100, 100);
            parameters.ParentWindow = parent.Handle;
            parameters.WindowStyle = NativeConstants.WS_CHILD;

            source = new HwndSource(parameters);
            source.AddHook(new HwndSourceHook(Helper));

            StackPanel panel = new StackPanel();
            rootUIElement = (UIElement)panel;
            source.RootVisual = panel;

            Button b = new Button();
            b.Click += new RoutedEventHandler(Click);
            b.Content = "akiaki";

            panel.Children.Add(b);

            MainWindow = source.Handle;

            return new HandleRef(null, MainWindow);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            isDisposed = true;
            NativeMethods.DestroyWindow(hwnd);
        }

        /// <summary>Clicks on the Button.</summary>
        protected void Click(object o, RoutedEventArgs args) { }

        protected IntPtr Helper(IntPtr window, int message, IntPtr firstParam, IntPtr secondParam, ref bool handled)
        {
            handled = false;
            return IntPtr.Zero;
        }

        #endregion

        #region Public Members

        public UIElement RootUIElement
        {
            get
            {
                return rootUIElement;
            }
            set
            {
                if (!isDisposed)
                {
                    rootUIElement = value;
                    if (source != null)
                    {
                        source.RootVisual = rootUIElement;
                    }
                }
            }
        }

        public IntPtr MainWindow;

        #endregion

        #region Private Data

        private UIElement rootUIElement;
        private HwndSource source;
        private bool isDisposed = false;

        #endregion

        public class HwndHostControlRecord
        {
            public List<HwndHost> UnDisposeHwndHostList = new List<HwndHost>();

            public CustomHwndHostControl CreateCustomHwndHostControl()
            {
                CustomHwndHostControl control = new CustomHwndHostControl();
                lock (UnDisposeHwndHostList)
                {
                    UnDisposeHwndHostList.Add(control);
                }

                return control;
            }

            public void DisposeHwndHostControl(int index)
            {
                lock (UnDisposeHwndHostList)
                {
                    //for (int i = 0; i < UnDisposeHwndHostList.Count; i++)
                    //{
                    //    UnDisposeHwndHostList[i].Dispose();
                    //    UnDisposeHwndHostList.RemoveAt(i);
                    //}
                    int count = UnDisposeHwndHostList.Count;
                    if (count > 0)
                    {
                        index %= count;
                        UnDisposeHwndHostList[index].Dispose();
                        UnDisposeHwndHostList.RemoveAt(index);
                    }
                }
            }
        }

    }

}
