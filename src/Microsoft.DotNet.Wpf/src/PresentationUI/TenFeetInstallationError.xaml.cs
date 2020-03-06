// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Windows.Input;
using System.Diagnostics;
using System.IO;
using MS.Internal.PresentationUI;
using System.Windows.Interop;
using System.Security;

namespace Microsoft.Internal.DeploymentUI
{
    /// <summary>
    /// Interaction logic for TenFeetInstallationError.xaml
    /// </summary>
    [FriendAccessAllowed] // Built into UI, used by Framework.
    internal partial class TenFeetInstallationError : IErrorPage
    {
        public TenFeetInstallationError()
        {
            InitializeComponent();
        }

        static TenFeetInstallationError()
        {
            CommandManager.RegisterClassCommandBinding(typeof(TenFeetInstallationError),
                    new CommandBinding(NavigationCommands.Refresh,
                        new ExecutedRoutedEventHandler(OnCommandRefresh),
                        new CanExecuteRoutedEventHandler(OnCanRefresh)));
        }

        /// <summary>
        /// 
        /// </summary>
        public Uri DeploymentPath
        {
            set
            {
                _deploymentPath = value;
            }
            get
            {
                return _deploymentPath;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ErrorTitle
        {
            set
            {
                txtTitle.Text = value;
            }
            get
            {
                return txtTitle.Text;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public string ErrorText
        {
            set
            {
                Text.Text = value;
            }
            get
            {
                return Text.Text;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool ErrorFlag
        {
            set
            {
                _errorFlag = value;
                if (!_errorFlag)
                {
                    RetryButton.Visibility = Visibility.Visible;
                    FocusManager.SetFocusedElement(this, RetryButton);
                    // Lose the bottom space on cancel UI
                    Grid_2.Height = 180;
                }
                else
                {
                    RetryButton.Visibility = Visibility.Collapsed;
                    ShowLogFileButton();
                    if (GetWinFxCallback != null)
                    {
                        GetWinFXButton.Visibility = Visibility.Visible;
                    }
                }
            }
            get
            {
                return _errorFlag;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public string LogFilePath
        {
            set
            {
                _logFilePath = value;
                ShowLogFileButton();
            }
            get
            {
                return _logFilePath;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Uri SupportUri
        {
            set
            {
                if (value != null)
                {
                    _supportUri = value;
                    SupportUriText.Visibility = Visibility.Visible;
                    SupportHyperLink.NavigateUri = value;
                }
            }
            get
            {
                return _supportUri;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public DispatcherOperationCallback RefreshCallback
        {
            set
            {
                _refresh = value;
            }
            get
            {
                return _refresh;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public DispatcherOperationCallback GetWinFxCallback
        {
            set
            {
                _getWinFX = value;
            }
            get
            {
                return _getWinFX;
            }
        }


        private void ShowLogFileButton()
        {
            if (File.Exists(LogFilePath) && ErrorFlag == true)
            {
                LogFileButton.Visibility = Visibility.Visible;
                FocusManager.SetFocusedElement(this, LogFileButton);
            }
        }

        static void OnCommandRefresh(object sender, RoutedEventArgs e)
        {
            TenFeetInstallationError page = sender as TenFeetInstallationError;
            if (page != null && page.RefreshCallback != null)
            {
                page.RefreshCallback(null);
            }
        }

        static void OnCanRefresh(object sender, CanExecuteRoutedEventArgs e)
        {
            TenFeetInstallationError page = sender as TenFeetInstallationError;
            if (page != null)
            {
                e.CanExecute = true;
                e.Handled = true;
            }
        }

        internal void OnRetry(object sender, RoutedEventArgs e)
        {
            if (RefreshCallback != null)
            {
                RefreshCallback(null);
            }
        }

        internal void OnShowLog(object sender, RoutedEventArgs e)
        {            
            Process Notepad = new Process();
            Notepad.StartInfo.FileName = "Notepad.exe";
            Notepad.StartInfo.Arguments = LogFilePath;
            Notepad.Start();
        }

        internal void OnGetWinFX(object sender, RoutedEventArgs e)
        {
            if (GetWinFxCallback != null)
            {
                GetWinFxCallback(null);
            }
        }

        private string _logFilePath;
        private Uri _deploymentPath;
        private DispatcherOperationCallback _refresh;
        private DispatcherOperationCallback _getWinFX;
        private bool _errorFlag;
        private Uri _supportUri;
    }
}
