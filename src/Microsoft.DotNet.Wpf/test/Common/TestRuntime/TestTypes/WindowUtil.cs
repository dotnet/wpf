// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.Test.Logging;
using Microsoft.Test.Threading;

/******************************************************************************
 * 
 * Provides utilities for creating and closing Windows and NavigationWindows.
 * Used in XamlModel and XamlTest.
 * 
 *****************************************************************************/

namespace Microsoft.Test.TestTypes
{
    /// <summary>
    /// Provides utilities for creating and closing Windows and NavigationWindows. 
    /// </summary>
    internal static class WindowUtil
    {
        #region Internal Members

        // Used in XamlModel
        internal static NavigationWindow CreateNavigationWindow(string filename, string callingAssembly)
        {
            return CreateNavigationWindow(filename, callingAssembly, null);
        }

        // Used in XamlTest
        // TODO: Consider throwing exception here when navigating to the window and showing it fails
        internal static NavigationWindow CreateNavigationWindow(string filename, string callingAssembly, LoadCompletedEventHandler handler)
        {
            NavigationWindow navigationWindow;

            if (BrowserInteropHelper.IsBrowserHosted)
            {
                GlobalLog.LogDebug("Browser hosted");
                navigationWindow = (NavigationWindow)Application.Current.MainWindow;
            }
            else
            {
                GlobalLog.LogDebug("not Browser hosted");
                GlobalLog.LogEvidence("Creating a NavigationWindow");
                NavigationWindow navwinsw = new NavigationWindow();
                navigationWindow = navwinsw;
            }

            if (handler != null)
            {
                navigationWindow.LoadCompleted += handler;
            }
            string strUri = "pack://application:,,,/" + callingAssembly + ";component/" + filename;
            GlobalLog.LogEvidence("Navigating to " + strUri);
            navigationWindow.Navigate(new Uri(strUri, UriKind.RelativeOrAbsolute));

            if (!BrowserInteropHelper.IsBrowserHosted)
            {
                GlobalLog.LogEvidence("Showing the Window");
                navigationWindow.Show();
            }

            DispatcherHelper.DoEvents(0, DispatcherPriority.Input);

            return navigationWindow;
        }

        // Used in WindowTest / WindowModel
        internal static Window CreateWindow()
        {
            return CreateWindow(false);
        }

        internal static Window CreateWindow(bool isTransparent)
        {
            Window window;
            if (BrowserInteropHelper.IsBrowserHosted)
            {
                window = Application.Current.MainWindow;
            }
            else
            {
                GlobalLog.LogEvidence("Creating a Window");
                Window winsw = new Window();
                window = winsw;

                if (isTransparent)
                {
                    GlobalLog.LogEvidence("Setting Window to Allow Transparency");
                    window.WindowStyle = WindowStyle.None;
                    window.AllowsTransparency = isTransparent;
                }

                GlobalLog.LogEvidence("Showing the Window");
                window.Show();
                DispatcherHelper.DoEvents(0, DispatcherPriority.Input);
            }

            return window;
        }

        // Used in all tests and models to close Windows and NavigationWindows.
        // If the application is browser hosted, then the Naviagtion window's 
        // journal is cleared instead of closing the window.
        internal static void CloseWindow(Window window)
        {
            if (BrowserInteropHelper.IsBrowserHosted)
            {
                NavigationWindow navWindow = (NavigationWindow)window;
                NavigateToObject(navWindow, null);
                ClearJournalBackStack(navWindow);
            }
            else
            {
                window.Close();
            }
        }

        /// <summary>
        /// Navigates the NavigationWindow to the specified object, and waits until the Application is Idle
        /// </summary>
        /// <param name="navigationWindow">NavigationWindow to Navigate</param>
        /// <param name="destination">Object to Navigate to</param>
        internal static void NavigateToObject(NavigationWindow navigationWindow, object destination)
        {
            navigationWindow.Navigate(destination);
            DispatcherHelper.DoEvents(0, DispatcherPriority.ApplicationIdle);
        }

        /// <summary>
        /// Clears the NavigationWindow's Journal BackStack
        /// </summary>
        /// <param name="navigationWindow">NavigationWindow to Clear the BackStack on</param>
        internal static void ClearJournalBackStack(NavigationWindow navigationWindow)
        {
            JournalEntry entry;
            do
            {
                entry = navigationWindow.RemoveBackEntry();
            }
            while (entry != null);
        }

        #endregion
    }
}
