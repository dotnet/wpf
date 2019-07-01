// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Microsoft.Test.Diagnostics;
using Microsoft.Test.Logging;
using Microsoft.Test.CrossProcess;
using Microsoft.Win32;
using System.Runtime.Loader;

namespace Microsoft.Test.Loaders
{

    /// <summary>
    /// Monitors Applications for processes and UI and allows handlers to be registered to handle specific UI.
    /// Is capable of handling TrustManager UI and understanding ClickOnce deployment for various target types.
    /// </summary>

    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public class ApplicationMonitor : IDisposable
    {

        #region Private data

        // Use a stack since the enumeration is performed from top-down
        Stack<UIHandlerRule> handlerRules = new Stack<UIHandlerRule>();
        // Signal for handlers to tell AppMonitor notify the end of monitoring, begin cleanup
        private static EventWaitHandle abortSignal;
        // Special information about the minimum set of processes.  When none of these are running,
        // monitoring will terminate.  Any launched process gets put into here.
        ProcessMonitorInfo[] processesToMonitor;
        List<Process> activeProcessesToWaitFor = new List<Process>();
        // Class that monitors the creation / name change of processes and windows along with their hwnd
        ProcessMonitor processMonitor = null;
        // Prexisting registrations for handling error UI, ClickOnce UI, and others as needed.
        // These UIHandlers are defined in NamedRegistrations.xml
        Dictionary<string, UIHandlerRule> namedRegistrations = new Dictionary<string, UIHandlerRule>();

        #endregion


        #region Constructors

        /// <summary>
        /// 1) Registers UIHandlers, defined in NamedRegistrations.xml.  This is for error UI and ClickOnce dialogs
        /// 2) Calls SetupEnvironmentForTest(), which is used for safe setup of things like important IE registry keys.  
        /// </summary>
        public ApplicationMonitor()
        {
            // Add a rule that grants the current user the 
            // right to wait on or signal the event.  Needed for Xbaps calling NotifyStopMonitoring from ACL-stripped PH.exe
            EventWaitHandleSecurity abortSignalHandleSecurity = new EventWaitHandleSecurity();
            string user = Environment.UserDomainName + "\\" + Environment.UserName;
            bool success;
            abortSignalHandleSecurity.AddAccessRule(new EventWaitHandleAccessRule(user, EventWaitHandleRights.FullControl, AccessControlType.Allow));
            abortSignal = new EventWaitHandle(false, EventResetMode.ManualReset, "ApplicationMonitorAbortSignal", out success/*, abortSignalHandleSecurity */);           

            RegisterDefaultUIHandlers();
            SetupEnvironmentForTest();
        }

        #endregion


        #region Public Members

        /// <summary>
        /// Shell executes a new process using the specified filename and monitors it
        /// </summary>
        /// <param name="filename">name of the file to shell execute</param>
        public void StartProcess(string filename)
        {
            ProcessStartInfo info = new ProcessStartInfo(filename);
            info.UseShellExecute = true;
            StartProcess(info);
        }


        /// <summary>
        /// Shell executes a new process using the specified filename and monitors it
        /// </summary>
        /// <param name="filename">name of the file to shell execute</param>
        /// <param name="arguments">argument of the file to shell execute</param>
        public void StartProcess(string filename, string arguments)
        {
            ProcessStartInfo info = new ProcessStartInfo(filename, arguments);
            info.UseShellExecute = true;
            StartProcess(info);
        }


        /// <summary>
        /// Starts a new process using the specified ProcessStartInfo and monitors it
        /// </summary>
        /// <param name="startInfo">ProcessStartInfo to start</param>
        public void StartProcess(ProcessStartInfo startInfo)
        {
            if (processMonitor != null)
                throw new InvalidOperationException("You must call Close() before Starting a new Process");

            InitProcessMonitor(startInfo);

            //Ignore any of the process that are currently running
            processMonitor.IgnoreRunningProcesses();

            //An error occurs if we pass arguments to .xbap files.  Some tests are passing arguments
            //so we are omitting these to get the change in
            if ((Path.GetExtension(startInfo.FileName) == ApplicationDeploymentHelper.STANDALONE_APPLICATION_EXTENSION) ||
                (Path.GetExtension(startInfo.FileName) == ApplicationDeploymentHelper.BROWSER_APPLICATION_EXTENSION))
            {
                startInfo.Arguments = "";
            }

            //Open .xbap file in new browser process if ApplicationMonitorStartNewProcessForXbap 
            //has been set to be true. 
            string startNewProcessForXbap = DriverState.DriverParameters["ApplicationMonitorStartNewProcessForXbap"];

            if (!string.IsNullOrEmpty(startNewProcessForXbap)
                && (string.Compare(startNewProcessForXbap.Trim(), "true", true, System.Globalization.CultureInfo.InvariantCulture) == 0)
                && startInfo.FileName.EndsWith(ApplicationDeploymentHelper.BROWSER_APPLICATION_EXTENSION, true, System.Globalization.CultureInfo.InvariantCulture))
            {
                string registeredBrowser = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Clients\StartMenuInternet", string.Empty, string.Empty) as string;
                if (!string.IsNullOrEmpty(registeredBrowser))
                {
                    startInfo.Arguments = startInfo.FileName;
                    startInfo.FileName = registeredBrowser;
                    GlobalLog.LogStatus("ApplicationMonitorStartNewProcessForXbap is true, starting a new process for XBAP file...");
                }
            }

            //Execute the Application
            GlobalLog.LogStatus(string.Format("Executing: {0} {1}", startInfo.FileName, startInfo.Arguments));
            Process proc = Process.Start(startInfo);
            if (proc != null)
                processMonitor.AddProcess(proc);

            //Begin Monitoring processes for UI
            processMonitor.Start();
        }

        /// <summary>
        /// Starts Monitoring for new applications and UI to be handled and previously ignores running processes
        /// </summary>
        /// <remarks>
        /// You should use this API when you want to invoke an application in
        /// a way other then starting the process directly (like via clicking
        /// a shortcut in the start menu).
        /// </remarks>
        public void StartMonitoring()
        {
            StartMonitoring(true);
        }


        /// <summary>
        /// Starts Monitoring for new applications and UI to be handled and ignores previously running processes if specified
        /// </summary>
        /// <param name="ignoreRunningProcesses">true to ignore previously running processes, otherwise, false</param>
        /// <remarks>
        /// You should use this API when you want to invoke an application in
        /// a way other then starting the process directly (like via clicking
        /// a shortcut in the start menu).
        /// You should ignore running processes when you are invoking the target
        /// application in some way.  If you want to monitor a process that is
        /// already running you should specify false for ignoreRunningProcesses.
        /// </remarks>
        public void StartMonitoring(bool ignoreRunningProcesses)
        {
            if (processMonitor != null)
                throw new InvalidOperationException("You must call Close() before Starting Monitoring");

            InitProcessMonitor(null);

            //Ignore any of the process that are currently running
            if (ignoreRunningProcesses)
                processMonitor.IgnoreRunningProcesses();

            //Begin Monitoring processes for UI
            processMonitor.Start();

        }

        /// <summary>
        /// Adds a process name that should be Monitored for UI
        /// </summary>
        /// <param name="processName">the name of the process to be monitored</param>
        /// <remarks>
        /// You should use this API to add processes to be monitored when
        /// your UIHandlers are not registered with a specific process name.
        /// </remarks>
        public void MonitorProcess(string processName)
        {
            if (processMonitor == null)
                throw new InvalidOperationException("You must call StartMonitoring or StartProcess before you can monitor processes");

            processMonitor.AddProcess(processName);
        }

        /// <summary>
        /// Adds a process to be Monitored for UI
        /// </summary>
        /// <param name="process">the process to be monitored</param>
        /// <remarks>
        /// You should use this API to add processes to be monitored when
        /// your UIHandlers are not registered with a specific process name.
        /// </remarks>
        public void MonitorProcess(Process process)
        {
            if (processMonitor == null)
                throw new InvalidOperationException("You must call StartMonitoring or StartProcess before you can monitor processes");

            processMonitor.AddProcess(process);
        }


        /// <summary>
        /// Waits for a registered UIHandler to return Abort or for the target application to call NotifyStopMonitoring()
        /// </summary>
        public void WaitForUIHandlerAbort()
        {
            //Wait For the Abort Signal from UIHandlers
            abortSignal.WaitOne();
            Thread.Sleep(2000);
        }

        /// <summary>
        /// Waits for a registered UIHandler to return Abort or for the target application to call NotifyStopMonitoring() using the specified timeout
        /// </summary>
        /// <param name="millisecondsTimeout">number of miliseconds to wait before timing out</param>
        /// <returns>returns false if the timeout occured, otherwise, true</returns>
        public bool WaitForUIHandlerAbort(int millisecondsTimeout)
        {
            //Wait For the Abort Signal from UIHandlers
            return abortSignal.WaitOne(millisecondsTimeout, false);
        }

        /// <summary>
        /// Stops Monitoring for new applications and UI to be handled
        /// </summary>
        /// <remarks>
        /// You should use this API when you have invoked an application in
        /// a way other then starting the process directly (like via clicking
        /// a shortcut in the start menu).  If you used StartProcess then you
        /// should use Close instead.
        /// </remarks>
        public void StopMonitoring()
        {
            if (processMonitor == null)
                throw new InvalidOperationException("You may only Stop Monitoring the ApplicationMonitor when it is Monitoring a target application.");

            //Stop Monitoring
            processMonitor.Stop();
        }


        /// <summary>
        /// Closes all process being monitored and stops monitoring
        /// </summary>
        public void Close()
        {
            if (processMonitor == null)
                throw new InvalidOperationException("You may only Close the ApplicationMonitor when it is Monitoring a target application.");

            //Kill all monitored processes and Stop Monitoring
            StopMonitoring();
            processMonitor.KillProcesses();
            processMonitor = null;
        }


        /// <summary>
        /// Registers a UIHandler to handle the specified UI
        /// </summary>
        /// <param name="handler">UIHandler to be executed when the specified UI is shown</param>
        /// <param name="processName">Name of the process (excluding extention) hosting the UI, null indicates any process</param>
        /// <param name="windowTitle">Title of the window to be handled, null indicates any windowtitle</param>
        /// <param name="notification">The events that you want to handle the targed UI</param>
        public void RegisterUIHandler(UIHandler handler, string processName, string windowTitle, UIHandlerNotification notification)
        {
            if (processMonitor != null)
                throw new InvalidOperationException("You cannot register UIHandlers after the process is executed");

            handlerRules.Push(new UIHandlerRule(handler, processName, windowTitle, notification));
        }


        /// <summary>
        /// Registers a UIHandler to handle the specified UI using a exsisiting Named Registration
        /// </summary>
        /// <param name="handler">UIHandler to be executed when the specified UI is shown</param>
        /// <param name="registrationName">name of the Registration entry in the NamedRegistrations.xml file compiled in the runtime dll</param>
        /// <param name="notification">The events that you want to handle the targed UI</param>
        public void RegisterUIHandler(UIHandler handler, string registrationName, UIHandlerNotification notification)
        {
            if (processMonitor != null)
                throw new InvalidOperationException("You cannot register UIHandlers after the process is executed");
            if (!namedRegistrations.ContainsKey(registrationName))
                throw new ArgumentException("A registration with the name '" + registrationName + "' could not be found");

            UIHandlerRule namedHandler = namedRegistrations[registrationName];
            //register the handler with the same information as the named entry
            handlerRules.Push(new UIHandlerRule(handler, namedHandler.ProcessName, namedHandler.WindowTitle, notification));
        }

        #endregion


        #region Static Members

        /// <summary>
        /// Target applications should call this method to notify the AppMonitor that it should shutdown the application
        /// </summary>
        public static void NotifyStopMonitoring()
        {
            GlobalLog.LogStatus("Notifying ApplicationMonitor to stop monitoring");

            if (abortSignal == null)
            {
                GlobalLog.LogStatus("Abort signal not available, trying to find existing wait handle by name...");

                try
                {
                    abortSignal = EventWaitHandle.OpenExisting("ApplicationMonitorAbortSignal");
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    GlobalLog.LogStatus("Abort signal did not exist, so cross-proc NotifyStopMonitoring may fail.");
                }
            }
           
            if (abortSignal != null)
            {
                abortSignal.Set();
                GlobalLog.LogStatus("Abort signal set.");
            }
            else
            {
                GlobalLog.LogStatus("WARNING: Could not get Abort Signal to use!");
            }
        }

        /// <summary>
        /// Gets the arguments used to lauch the application
        /// </summary>
        /// <returns>array of argument values as a string</returns>
        /// <remarks>Target applications can use this function to get arguments that where used to launch the application</remarks>
        public static string[] GetArguments()
        {
            string argsString = DictionaryStore.Current["ApplicationMonitorArguments"];

            // Return null here to indicate that args are not available, however it may not be necessary to diferentiate between this and no arguments
            if (argsString == null)
            {
                return new string[0];
            }
            // This won't handle cases where args that have spaces in them are surrounded by quotes.  Need to identify what to do here.
            else
            {
                return argsString.Split(' ');
            }
        }

        #endregion


        #region Private implementation

        private void InitProcessMonitor(ProcessStartInfo startInfo)
        {
            //Reset the Abort signal and register it with the remoted harness
            abortSignal.Reset();

            // Remember the arguments so tests can get to them easily
            // NOTE: startInfo may be null if we are monitoring without starting a process
            // If there are already values under ApplicationMonitorArguments, leave them and do nothing.
            if ((startInfo != null) && !string.IsNullOrEmpty(startInfo.Arguments) && string.IsNullOrEmpty(DictionaryStore.Current["ApplicationMonitorArguments"]))
            {
                DictionaryStore.Current["ApplicationMonitorArguments"] = startInfo.Arguments;
            }

            //Create the ProcessMonitor
            processMonitor = new ProcessMonitor();
            processMonitor.ProcessExited += new ProcessExitedHandler(OnProcessExited);
            processMonitor.ProcessFound += new ProcessFoundHandler(OnProcessFound);
            processMonitor.VisibleWindowFound += new VisibleWindowHandler(OnVisibleWindowFound);
            processMonitor.VisibleWindowTitleChanged += new VisibleWindowHandler(OnVisibleWindowTitleChanged);

            //Monitor the Processes that the DeploymentHelper says are important in deployment
            processesToMonitor = ApplicationDeploymentHelper.GetProcessesToMonitor(startInfo);
            foreach (ProcessMonitorInfo processInfo in processesToMonitor)
                processMonitor.AddProcess(processInfo.Name);

            //Add Processes to be monitored that are described in the handler rules
            foreach (UIHandlerRule rule in handlerRules)
            {
                if (rule.ProcessName != null)
                    processMonitor.AddProcess(rule.ProcessName);
            }
        }

        //HACK: (CoromT) We should just allow any server channel and not depend on the Tcp channel specifically
        //      On the other hand, I'm not sure if there are other channels that automatically show up
        //      (cross-domain, etc) so we are detecting the TcpServer channel to play it safe.
        private bool HasTcpServerChannel()
        {
            //foreach (IChannel channel in ChannelServices.RegisteredChannels)
            //{
            //    if (channel is IpcServerChannel)
            //        return true;
            //}
            return false;
        }

        void IDisposable.Dispose()
        {
            Close();
        }

        // Use this method to set up non-intrusive environment settings, such as IE default settings, etc.
        // Since AppMonitor can't guarantee it can clean this up, ONLY set values that can be persisted.
        // WARNING: IF THIS METHOD THROWS AN EXCEPTION ALL APPMONITOR FUNCTIONALITY WILL BE AFFECTED!!!
        // Use test APIs that will restore the environment to previous state for these
        private void SetupEnvironmentForTest()
        {
            try
            {
                // **** Set reg keys to disable Cross-domain frame navigation.  Needed because there's a test that enables it temporarily.
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\1", "1607", 3, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\3", "1607", 3, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                GlobalLog.LogDebug(" -- Exception trying to set up environment ... please fix X-Domain Frame Navigation setup code in ApplicationMonitor.SetupEnvironmentForTest() )\n" + ex.ToString());
            }

            try
            {
                // **** Set reg keys to prevent HTTPS Mixed content dialog
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\0", "1609", 0, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\1", "1609", 0, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\2", "1609", 0, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\3", "1609", 0, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                GlobalLog.LogDebug(" -- Exception trying to set up environment ... please fix HTTPS Mixed content dialog in ApplicationMonitor.SetupEnvironmentForTest()\n" + ex.ToString());
            }
            try
            {
                // **** Set reg key to prevent IE7 Phishing Filter dialog... 
                if (ApplicationDeploymentHelper.GetIEVersion() >= 7)
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Internet Explorer\PhishingFilter", "Enabled", 1, RegistryValueKind.DWord);
                }
                if (ApplicationDeploymentHelper.GetIEVersion() == 8)
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Internet Explorer\PhishingFilter", "EnabledV8", 1, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                GlobalLog.LogDebug(" -- Exception trying to set up environment ... please fix IE Phishing Filter key setup in ApplicationMonitor.SetupEnvironmentForTest()\n" + ex.ToString());
            }
            try
            {
                // **** Set reg key to prevent dialog for IE7 on Vista creating new instance for cross-zone navigation
                if (ApplicationDeploymentHelper.GetIEVersion() == 7 && Environment.OSVersion.Version.Major == 6)
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\LowRegistry\DontShowMeThisDialogAgain", "PromptForBrokerRedirect", "NO", RegistryValueKind.String);
                }
            }
            catch (Exception ex)
            {
                GlobalLog.LogDebug(" -- Exception trying to set up environment ... If this error is seen on Vista, please fix IE zone-change setting in ApplicationMonitor.SetupEnvironmentForTest()\n" + ex.ToString());
            }
            try
            {
                // **** Set reg key to prevent dialog for first-time IE7 GoldBar display
                // **** The bar still displays but this prevents a modal dialog from blocking animation.
                if (ApplicationDeploymentHelper.GetIEVersion() == 7)
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\InformationBar", "FirstTime", 0, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                GlobalLog.LogDebug(" -- Exception trying to set up environment ... Please fix IE \"First Goldbar\" code in ApplicationMonitor.SetupEnvironmentForTest()\n" + ex.ToString());
            }
            try
            {
                // **** Set reg key to prevent dialog for first-time HTTPS navigation.
                // **** If this UI shows automation can be blocked.
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings", "WarnonZoneCrossing", 0, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                GlobalLog.LogDebug(" -- Exception trying to set up environment ... Please fix IE \"first-time HTTPS navigation\" code in ApplicationMonitor.SetupEnvironmentForTest()\n" + ex.ToString());
            }

            if ((SystemInformation.Current.IsPersonalEdition) && (SystemInformation.Current.MajorVersion == 6))
            {
                try
                {
                    // **** Set reg key to prevent dialog for first-time HTTPS navigation.
                    // **** If this UI shows automation can be blocked.
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings", "WarnonZoneCrossing", 0, RegistryValueKind.DWord);
                }
                catch (Exception ex)
                {
                    GlobalLog.LogDebug(" -- Exception trying to set up environment ... Please fix IE \"Enable Intranet Zone for Home SKUs\" code in ApplicationMonitor.SetupEnvironmentForTest()\n" + ex.ToString());
                }
            }

            try
            {
                // **** Set reg key to prevent dialog when IE thinks it's not the default browser.
                // **** Needed since if this is set programatically via registry, IE still detects change on some platforms.
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main", "ShowedCheckBrowser", "no", RegistryValueKind.String);
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main", "Check_Associations", "no", RegistryValueKind.String);
            }
            catch (Exception ex)
            {
                GlobalLog.LogDebug(" -- Exception trying to set up environment ... Please fix IE \"Prevent Default Browser Check\" code in ApplicationMonitor.SetupEnvironmentForTest()\n" + ex.ToString());
            }

            try
            {
                // No special treatment for Server: WPF Automation lab disables ESC now.
                //// Add Internet zone URLs to trusted sites on any Server SKU.
                //// This allows the tests to run as expected on IE Enhanced Security mode, which is the default behavior for LH Server + Server 2K3
                //if (SystemInformation.Current.IsServer)
                //{
                //    ApplicationDeploymentHelper.AddUrlToZone(IEUrlZone.URLZONE_TRUSTED, FileHost.HttpInternetBaseUrl);
                //    ApplicationDeploymentHelper.AddUrlToZone(IEUrlZone.URLZONE_TRUSTED, FileHost.HttpsInternetBaseUrl);

                //    // Workaround for DD bugs # 129532 - LH Server: Xbap Error UI brings up IE Enhanced Security warning for local apps
                //    // Not needed on Server 2K3 SKUs.
                //    if (SystemInformation.Current.OSVersion.StartsWith("6"))
                //    {
                //        ApplicationDeploymentHelper.AddUrlToZone(IEUrlZone.URLZONE_TRUSTED, "about:security_PresentationHost.exe" );
                //    }
                //    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main", "DisplayTrustAlertDlg", 0, RegistryValueKind.DWord);
                //    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main", "RunOnceHasShown", 1, RegistryValueKind.DWord);
                //    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings", "IEHardenIENoWarn", 1, RegistryValueKind.DWord);
                //}
                //else
                //{
                ApplicationDeploymentHelper.AddUrlToZone(IEUrlZone.URLZONE_INTERNET, FileHost.HttpInternetBaseUrl);
                ApplicationDeploymentHelper.AddUrlToZone(IEUrlZone.URLZONE_INTERNET, FileHost.HttpsInternetBaseUrl);
                //}
                ApplicationDeploymentHelper.AddUrlToZone(IEUrlZone.URLZONE_INTRANET, FileHost.HttpIntranetBaseUrl);
                ApplicationDeploymentHelper.AddUrlToZone(IEUrlZone.URLZONE_INTRANET, FileHost.HttpsIntranetBaseUrl);
                ApplicationDeploymentHelper.AddUrlToZone(IEUrlZone.URLZONE_INTRANET, FileHost.UncBaseUrl);
            }
            catch (Exception ex)
            {
                GlobalLog.LogDebug(" -- Exception trying to set up environment ... Please fix IE \"IE URL->Zone mapping ... \" code in ApplicationMonitor.SetupEnvironmentForTest()\n" + ex.ToString());
            }

            try
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main", "IE8RunOnceCompleted", 1, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Recovery", "AutoRecover", 2, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                GlobalLog.LogDebug(" -- Exception trying to set up environment ... Please fix IE 8 first-run prep code in ApplicationMonitor.SetupEnvironmentForTest()\n" + ex.ToString());
            }

            // Rarely, one of two specific PartialTrust tests will fail to complete.  If this happens, a regkey gets left behind that
            // disables all access to WebBrowser or Media.  Any further tests of those items on this machine will fail.
            // Try to delete those keys to save a massive headache.
            try
            {            
                RegistryKey wpfFeaturesKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\.NETFramework\Windows Presentation Foundation\Features", true);
                if (wpfFeaturesKey != null)
                {
                    wpfFeaturesKey.DeleteValue("WebBrowserDisallow", false);
                    wpfFeaturesKey.DeleteValue("MediaAudioDisallow", false);
                    wpfFeaturesKey.DeleteValue("MediaImageDisallow", false);
                    wpfFeaturesKey.DeleteValue("MediaVideoDisallow", false);
                    wpfFeaturesKey.DeleteValue("XBAPDisallow", false);
                    wpfFeaturesKey.DeleteValue("ScriptInteropDisallow", false);
                    wpfFeaturesKey.Close();
                }
                // If there's a WOW6432Node defined, we need to check this as well.
                // Currently this only matters for Win7 because otherwise the previous call would have cleaned all these values.
                // Win7 removes hidden redirection but there still are "two" registries.
                wpfFeaturesKey = Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Microsoft\.NETFramework\Windows Presentation Foundation\Features", true);
                if (wpfFeaturesKey != null)
                {
                    wpfFeaturesKey.DeleteValue("WebBrowserDisallow", false);
                    wpfFeaturesKey.DeleteValue("MediaAudioDisallow", false);
                    wpfFeaturesKey.DeleteValue("MediaImageDisallow", false);
                    wpfFeaturesKey.DeleteValue("MediaVideoDisallow", false);
                    wpfFeaturesKey.DeleteValue("XBAPDisallow", false);
                    wpfFeaturesKey.DeleteValue("ScriptInteropDisallow", false);
                    wpfFeaturesKey.Close();
                }
            }
            catch (Exception)
            {
                // if we didn't find the regkey, ignore the exception
            }

            // Information bar first run dialog is usually ignorable but some focus tracking tests get confused by it, so prevent it from showing.
            try
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\InformationBar", "FirstTime", 0, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                GlobalLog.LogDebug(" -- Exception trying to set up environment ... Please fix IE Information Bar first run logic in ApplicationMonitor.SetupEnvironmentForTest():" + ex.ToString());
            }

            //On IE9, XBAPs are disabled in the Internet Zone.  Re-enable them so tests will start.
            //Note: We just enable no matter what Windows/IE version we're on, since we always want this on,
            //and it won't hurt to set it to 0 (enable) if it's already 0.
            try
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\3", "2400", 0, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                GlobalLog.LogDebug(" -- Exception trying to set up environment ... Please fix Internet Zone XBAP enable logic in ApplicationMonitor.SetupEnvironmentForTest():" + ex.ToString());
            }
        }

        private void RegisterDefaultUIHandlers()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NamedRegistrations.xml");
            //HACK: VS.NET renames the file to include the full path but razzle does not (this is very annoying)
            if (stream == null)
                stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Code.Microsoft.Test.Loaders.NamedRegistrations.xml");

            XmlDocument doc = new XmlDocument();
            doc.Load(stream);
            stream.Close();

            foreach (XmlElement element in doc.DocumentElement.SelectNodes("Registration"))
            {
                Type type = typeof(ApplicationMonitor).Assembly.GetType(element.GetAttribute("Handler"), true, false);
                UIHandler handler = (UIHandler)Activator.CreateInstance(type, true);
                handler.NamedRegistration = element.GetAttribute("Name");
                if (element.GetAttribute("ProcessName").Length > 0)
                    handler.ProcessName = element.GetAttribute("ProcessName");
                if (element.GetAttribute("WindowTitle").Length > 0)
                    handler.WindowTitle = element.GetAttribute("WindowTitle");
                if (element.GetAttribute("AllowMultipleInvocations").Length > 0)
                    handler.AllowMultipleInvocations = bool.Parse(element.GetAttribute("AllowMultipleInvocations"));

                UIHandlerRule rule = new UIHandlerRule(handler, handler.ProcessName, handler.WindowTitle, UIHandlerNotification.All);
                handlerRules.Push(rule);
                namedRegistrations[handler.NamedRegistration] = rule;
            }
        }

        private void OnVisibleWindowFound(IntPtr topLevelhWnd, IntPtr hWnd, Process process, string title)
        {
            GlobalLog.LogDebug(string.Format("UI Found: {0} {1} '{2}'", process.ProcessName, hWnd, title));

            //Invoke the UIHandlers registered for this UI (If any exist)
            ProcessUIHandlers(topLevelhWnd, hWnd, process, title, UIHandlerNotification.Visible);
        }

        private void OnVisibleWindowTitleChanged(IntPtr topLevelhWnd, IntPtr hWnd, Process process, string title)
        {
            GlobalLog.LogDebug(string.Format("UI Title Changed: {0} {1} '{2}'", process.ProcessName, hWnd, title));

            //Invoke the UIHandlers registered for this UI (If any exist)
            ProcessUIHandlers(topLevelhWnd, hWnd, process, title, UIHandlerNotification.TitleChanged);
        }

        //Evaluate the rules for a UIHandlers that care about this UI
        private void ProcessUIHandlers(IntPtr topLevelhWnd, IntPtr hWnd, Process process, string title, UIHandlerNotification notification)
        {
            foreach (UIHandlerRule rule in handlerRules)
            {
                string procName;
                try
                {
                    procName = process.ProcessName;
                }
                catch (InvalidOperationException)
                {
                    // The process has exited so we do not need to process any more handlers
                    GlobalLog.LogDebug("Tried to evaluate UI \"" + title + "\" but process had already exited.");
                    return;
                }

                if (rule.Evaluate(title, procName, notification, hWnd))
                {
                    rule.HasBeenInvoked = true;
                    UIHandlerAction action = UIHandlerAction.Abort;
                    if (Debugger.IsAttached)
                        // If a debugger is attached then don't try catch since it makes debugging inconvenient
                        action = rule.Handler.HandleWindow(topLevelhWnd, hWnd, process, title, notification);
                    else
                    {
                        // We can't trust the UIHandler to not throw an unhandled exception (crashes the app since the callback in on another thread)
                        try
                        {
                            action = rule.Handler.HandleWindow(topLevelhWnd, hWnd, process, title, notification);
                        }
                        catch (Exception e)
                        {
                            // If an exception occurs log the exception and return abort
                            GlobalLog.LogEvidence(e.ToString());
                        }
                    }
                    //If the action was handled then stop processing rules
                    if (action == UIHandlerAction.Handled)
                        break;
                    //If the action was aborted then set the abort signal and stop processing rules
                    else if (action == UIHandlerAction.Abort)
                    {
                        abortSignal.Set();
                        break;
                    }
                }
            }
        }

        private void OnProcessFound(Process process)
        {
            string procName;
            try
            {
                procName = process.ProcessName;
            }
            catch (InvalidOperationException)
            {
                // The process has exited
                return;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Strange whidbey bug.. the process has most likely already exited
                return;
            }

            foreach (ProcessMonitorInfo processInfo in processesToMonitor)
            {
                if (processInfo.Lifetime == ProcessLifetime.Application && StringComparer.InvariantCultureIgnoreCase.Compare(processInfo.Name, procName) == 0)
                {
                    activeProcessesToWaitFor.Add(process);
                    break;
                }
            }
        }

        private void OnProcessExited(Process process)
        {
            foreach (Process proc in activeProcessesToWaitFor)
            {
                if (proc.Id == process.Id)
                {
                    activeProcessesToWaitFor.Remove(proc);
                    if (activeProcessesToWaitFor.Count == 0)
                    {
                        GlobalLog.LogDebug("The process " + proc.Id + " has exited and no more processes with application lifetime are found. Aborting monitoring.");
                        abortSignal.Set();
                    }
                    break;
                }
            }
        }

        #endregion


        #region UIHandlerRule class

        //registration entry for UIHandlers
        class UIHandlerRule
        {
            UIHandler handler;
            string title;
            string processName;
            UIHandlerNotification notification;
            bool hasBeenInvoked = false;

            public UIHandlerRule(UIHandler handler, string processName, string title, UIHandlerNotification notification)
            {
                this.handler = handler;
                this.processName = processName;
                this.notification = handler.Notification;

                if (title != null)
                {
                    //replace Managed resource macros with values
                    //syntax:  @@PARTIAL_ASSEMBLY_NAME;BASE_NAME;RESOURCE_ID@@
                    //You can usually identify these values by looking at the source code for the managed component you need resource strings from
                    //The BASE_NAME is usually in a private static class called SR.  You can also use ILDASM do dissassemble
                    //the resource table and look up what you are interested in
                    Regex regex = new Regex("@@(?<asmName>[^@;]*);(?<baseName>[^@;]*);(?<id>[^@;]*)@@", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
                    for (Match match = regex.Match(title); match.Success; match = match.NextMatch())
                    {
                        //get the match groups
                        string matchString = match.Groups[0].Value;
                        string asmName = match.Groups["asmName"].Value;
                        string baseName = match.Groups["baseName"].Value;
                        string id = match.Groups["id"].Value;

                        //load the assembly and get the resource string from the resource manager
                        try
                        {
                            AssemblyName assemblyName = AssemblyName.GetAssemblyName(asmName);
                            Assembly asm = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
                            ResourceManager resMan = new ResourceManager(baseName, asm);
                            string value = resMan.GetString(id);

                            //replace the macro with the resource value
                            title = title.Replace(matchString, value);
                        }
                        catch (System.IO.FileNotFoundException)
                        {
                            // Do nothing.  This is for the case of an unload-able assembly reference
                            // This will happen when registering for both the 2.0 and 4.0 System.Windows.Forms resources for the ClickOnce install dialog, for instance.                        
                        }
                        catch (System.BadImageFormatException)
                        {
                            // Same as above, this just fails differently depending on the scenario.
                        }

                    }
                }

                if (title != null && title.ToLowerInvariant().StartsWith("unmanagedresourcestring:"))
                {
                    string[] resourceLookup = title.Substring(24).Split(',');
                    if (resourceLookup.Length != 2)
                        throw new ArgumentException("Resource identifiers must specify a dll and resouce id", "title");
                    string filename = resourceLookup[0].Trim();
                    int resourceId = int.Parse(resourceLookup[1].Trim());
                    this.title = ResourceHelper.GetUnmanagedResourceString(filename, resourceId);
                }
                // Loads a static property from a given assembly from the work dir
                // Can be updated later to search different paths
                // Do not change the string handling to "To...Invariant()" as class name loading will fail.
                else if (title != null && title.ToLowerInvariant().StartsWith("property:"))
                {
                    string[] inputs = title.Substring(9).Trim().Split(',');
                    string property = inputs[0].Trim();
                    string assembly = inputs[1].Trim();

                    // LoadWithPartialName here should be OK since test execution tries to ensure
                    // that only a matching TestRuntime.dll is present
                    // and, currently the only AMC files using this feature reference solely TestRuntime.
                    // We need to fix this if not.  [MattGal]
#pragma warning disable 618
                    Assembly a = Assembly.LoadWithPartialName(assembly);
#pragma warning restore 618
                    if (a == null)
                    {
                        throw new ArgumentException("Could not locate assembly " + assembly);
                    }
                    Type t = a.GetType(property.Substring(0, property.LastIndexOf(".")), true);
                    PropertyInfo pi = t.GetProperty(property.Substring(property.LastIndexOf(".") + 1));
                    // Create this as a regular expression because for IE titles we can't predict "Windows" vs "Microsoft" Internet explorer
                    this.title = "regexp:(" + pi.GetValue(null, null) + ")";
                }
                else
                    this.title = title;
            }

            public bool Evaluate(string title, string processName, UIHandlerNotification notification, IntPtr hWnd)
            {
                if (title == null)
                    title = "";

                if (handler == null)
                    return false;

                if (hasBeenInvoked && !handler.AllowMultipleInvocations)
                    return false;

                if (this.title != null)
                {
                    if (this.title.ToLowerInvariant().StartsWith("regexp:"))
                    {
                        string regexp = this.title.Substring(7);
                        if (!Regex.IsMatch(title, regexp, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                            return false;
                    }
                    else if (StringComparer.InvariantCultureIgnoreCase.Compare(this.title, title) != 0)
                        return false;
                }

                if (this.processName != null && StringComparer.InvariantCultureIgnoreCase.Compare(this.processName, processName) != 0)
                    return false;

                if (this.notification != UIHandlerNotification.All && this.notification != notification)
                    return false;

                if (this.WindowClass != WindowClassEnum.Any && (this.WindowClass == WindowClassEnum.AvalonBrowserApplication) && !ApplicationDeploymentHelper.IsAvalonApplicationHwnd(hWnd))
                    return false;


                return true;
            }

            public bool HasBeenInvoked
            {
                get { return hasBeenInvoked; }
                set { hasBeenInvoked = value; }
            }

            public UIHandler Handler
            {
                get { return handler; }
                set { handler = value; }
            }

            public string WindowTitle
            {
                get { return title; }
            }

            public string ProcessName
            {
                get { return processName; }
            }

            public UIHandlerNotification Notification
            {
                get { return notification; }
            }

            public WindowClassEnum WindowClass
            {
                get { return handler.WindowClass; }
                set { handler.WindowClass = value; }
            }
        }

        #endregion
    }


    /// <summary>
    /// Type of Notifications that a UIHandler may register for to handle UI
    /// </summary>

    public enum UIHandlerNotification
    {
        /// <summary>
        /// All Notifications (Both when the UI is visiable and when the Title changes)
        /// </summary>
        All,
        /// <summary>
        /// Notified only when the UI becomes Visible for the first time
        /// </summary>
        Visible,
        /// <summary>
        /// Notified whenever the title of the UI window changes
        /// </summary>
        TitleChanged
    }

    /// <summary>
    /// Type of UI that HWND is
    /// </summary>

    public enum WindowClassEnum
    {

        /// <summary>
        /// All window classes are allowed
        /// </summary>
        Any,
        /// <summary>
        /// Only Avalon windows are allowed
        /// </summary>
        AvalonBrowserApplication

    }
}