// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Security.Permissions;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Test.Logging;

/******************************************************************************
 * 
 * This file contains the base class of any test case that requires a markup file.
 * It creates a NavigationWindow and navigates to the specified xaml file on 
 * initialize and closes the window on clean up.
 * 
 *****************************************************************************/

namespace Microsoft.Test.TestTypes
{
    /// <summary>
    /// Testcase base class for testing Avalon with markup
    /// </summary>
    public abstract class XamlTest : AvalonTest
    {
        #region Private Data

        private string filename;
        private NavigationWindow navigationWindow;
        private string callingAssembly;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a testcase that navigates to a xaml file.
        /// </summary>
        protected XamlTest(string filename)
        {
            this.filename = filename;
            FileIOPermission fip = new FileIOPermission(PermissionState.Unrestricted);
            fip.Assert();
            this.callingAssembly = this.GetType().Assembly.GetName().Name;
            InitializeSteps += new TestStep(InitializeTestStep);
            CleanUpSteps += new TestStep(CleanUpTestStep);
        }

        #endregion

        #region Public and Protected Members

        /// <summary>
        /// Event fired when the window finished loading.
        /// </summary>
        public event LoadCompletedEventHandler LoadCompleted;

        /// <summary>
        /// Gets the NavigationWindow used by this test case.
        /// </summary>
        public NavigationWindow Window
        {
            get { return navigationWindow; }
        }

        /// <summary>
        /// Gets the content of the NavigationWindow.
        /// </summary>
        public FrameworkElement RootElement
        {
            get { return navigationWindow.Content as FrameworkElement; }
        }

        #endregion

        #region Private Members

        private TestResult InitializeTestStep()
        {
            navigationWindow = WindowUtil.CreateNavigationWindow(this.filename, this.callingAssembly, new LoadCompletedEventHandler(navigationWindow_LoadCompleted));
            return TestResult.Pass;
        }

        private void navigationWindow_LoadCompleted(object sender, NavigationEventArgs e)
        {
            // If navigationWindow is not set here, test cases that have an event handler for
            // LoadCompleted and access RootElement cause a NullReferenceException
            navigationWindow = sender as NavigationWindow;
            navigationWindow.LoadCompleted -= new LoadCompletedEventHandler(navigationWindow_LoadCompleted);
            if (LoadCompleted != null)
            {
                LoadCompleted(sender, e);
            }
        }

        private TestResult CleanUpTestStep()
        {
            WindowUtil.CloseWindow(navigationWindow);
            return TestResult.Pass;
        }

        #endregion
    }
}
