// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Reflection;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Navigation;

/******************************************************************************
 * 
 * This file contains the base class of any model that requires a markup file.
 * It creates a NavigationWindow and navigates to the specified xaml file on 
 * initialize and closes the window on clean up.
 * The .xtc file with the state machine should be created using MDE.
 * 
 *****************************************************************************/

namespace Microsoft.Test.TestTypes
{
    /// <summary>
    /// Base class for using model-based testing to test Avalon when test case requires a markup file.
    /// </summary>
    public abstract class XamlModel : AvalonModel
    {
        #region Private Data

        private string xamlFileName;
        private NavigationWindow navigationWindow;
        private string callingAssembly;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor for XamlModel. Model will have the same name as the .xtc file.
        /// </summary>
        /// <param name="xamlFileName">Xaml file used in this test.</param>
        /// <param name="xtcFileName">Name of xtc file.</param>
        protected XamlModel(string xamlFileName, string xtcFileName) :
            this(xamlFileName, xtcFileName, -1)
        {
        }

        /// <summary>
        /// Constructor for XamlModel. Model will have the same name as the .xtc file.
        /// </summary>
        /// <param name="xamlFileName">Xaml file used in this test.</param>
        /// <param name="xtcFileName">Name of xtc file.</param>
        /// <param name="testCaseNumber">Number of test case to run.</param>
        protected XamlModel(string xamlFileName, string xtcFileName, int testCaseNumber)
            : this("", xamlFileName, xtcFileName, testCaseNumber, testCaseNumber)
        {
        }

        /// <summary>
        /// Constructor for XamlModel. Model will have the same name as the .xtc file.
        /// </summary>
        /// <param name="xamlFileName">Xaml file used in this test.</param>
        /// <param name="xtcFileName">Name of xtc file.</param>
        /// <param name="beginTestCaseNumber">Number of first test case to run.</param>
        /// <param name="endTestCaseNumber">Number of last test case to run.</param>
        protected XamlModel(string xamlFileName, string xtcFileName, int beginTestCaseNumber, int endTestCaseNumber)
            : base("", xtcFileName, beginTestCaseNumber, endTestCaseNumber)
        {
            FileIOPermission fip = new FileIOPermission(PermissionState.Unrestricted);
            fip.Assert();
            this.callingAssembly = Assembly.GetCallingAssembly().GetName().Name;
            this.xamlFileName = xamlFileName;
        }

        /// <summary>
        /// Constructor for XamlModel.
        /// </summary>
        /// <param name="modelName">Name of the model. If no name is specified, the .xtc file name 
        /// will be used as the model name.</param>
        /// <param name="xamlFileName">Xaml file used in this test.</param>
        /// <param name="xtcFileName">Name of xtc file.</param>
        /// <param name="beginTestCaseNumber">Number of first test case to run.</param>
        /// <param name="endTestCaseNumber">Number of last test case to run.</param>
        protected XamlModel(string modelName, string xamlFileName, string xtcFileName, int beginTestCaseNumber, int endTestCaseNumber)
            : base(modelName, xtcFileName, beginTestCaseNumber, endTestCaseNumber)
        {
            FileIOPermission fip = new FileIOPermission(PermissionState.Unrestricted);
            fip.Assert();
            this.callingAssembly = Assembly.GetCallingAssembly().GetName().Name;
            this.xamlFileName = xamlFileName;
        }

        #endregion

        #region Public and Protected Members

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

        #region Overridden Members

        /// <summary>
        /// Creates the NavigationWindow and navigates to the xaml file passed in the constructor.
        /// Fires OnInitialize.
        /// </summary>
        public override bool Initialize()
        {
            navigationWindow = WindowUtil.CreateNavigationWindow(this.xamlFileName, this.callingAssembly);
            // base.Initialize fires the OnInitialize event.
            return base.Initialize();
        }

        /// <summary>
        /// Fires OnCleanUp event.
        /// Closes the NavigationWindow with the xaml file.
        /// </summary>
        public override bool CleanUp()
        {
            bool boolResult = base.CleanUp();
            WindowUtil.CloseWindow(navigationWindow);
            return boolResult;
        }

        #endregion
    }
}
