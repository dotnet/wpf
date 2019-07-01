// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections;
using System.Xml;
using Microsoft.Test.Loaders.Steps.HostingRuntimePolicy;
using Microsoft.Test.Loaders.UIHandlers;
using Microsoft.Test.Logging;
using Microsoft.Win32;
using Microsoft.Test.CrossProcess;

namespace Microsoft.Test.Loaders.Steps
{

    /// <summary>
    /// The scheme by which the application should be activated
    /// </summary>
    public enum ActivationScheme
    {
        /// <summary>
        /// Activate the application using the HTTP protocol (Internet case) over an external (non-corpnet) server
        /// </summary>
        HttpInternetExternal,
        /// <summary>
        /// Activate the application using the HTTP protocol (Internet case)
        /// </summary>
        HttpInternet,
        /// <summary>
        /// Activate the application using the HTTP protocol (Intranet case)
        /// </summary>
        HttpIntranet,
        /// <summary>
        /// Activate the application using the HTTPS protocol(Internet case)
        /// </summary>
        HttpsInternet,
        /// <summary>
        /// Activate the application using the HTTPS protocol(Intranet case)
        /// </summary>
        HttpsIntranet,
        /// <summary>
        /// Activate the application using the UNC protocol
        /// </summary>
        Unc,
        /// <summary>
        /// Activate the application from the local machine
        /// </summary>
        Local
    };

    /// <summary>
    /// The Method by which the application should be activated
    /// Either opening IE and typing in the URI of the content (Navigate), direct shell activation (Launch)
    /// launch via MediaCenter (EHome), and Launch via Media Center w/ Full Screen (EHomeFullScreen)
    /// </summary>
    public enum ActivationMethod
    {
        /// <summary>
        /// Activate the application by navigating to it in Internet Explorer
        /// </summary>
        Navigate,
        /// <summary>
        /// Activate the application using ShellExec
        /// </summary>
        Launch,
        /// <summary>
        /// Launch the application in Media center (Must be browser-hostable)
        /// </summary>
        EHome,
        /// <summary>
        /// Same as Ehome but starts up full-screen for Vscan type stuff
        /// </summary>
        EHomeFullScreen
    };

    /// <summary>
    /// Support File used by ActivationStep to be hosted on the Remote server
    /// </summary>
    public class SupportFile
    {

        /// <summary>
        /// Gets or sets the Name of the SupportFile
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to host the dependencies of support file on the remote server
        /// </summary>
        public bool IncludeDependencies = false;

        /// <summary>
        /// If not null, use the server listed in the support file instead of default.
        /// </summary>
        public string CustomTestScratchServerPath = null;

        /// <summary>
        /// Preserve directory structure when copying files. Default is false.
        /// Can lead to unexpected behavior.
        /// </summary>
        public bool PreserveDirectoryStructure = false;

        /// <summary>
        /// Sets or gets the target directory for the supportfile
        /// Setting TargetDirectory sets PreserveDirectoryStructure to true
        /// </summary>
        public string TargetDirectory
        {
            set
            {
                PreserveDirectoryStructure = true;
                if (_targetDirectory == null)
                {
                    _targetDirectory = string.Empty;
                }
                else
                {
                    _targetDirectory = value;
                }
            }

            get
            {
                return _targetDirectory;
            }
        }

        private string _targetDirectory = string.Empty;
        private string _name = null;

    }

    /// <summary>
    /// Loader Step that can be used to activate an Avalon application type
    /// </summary> 
    [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
    public class ActivationStep : LoaderStep
    {
        #region Private Data

        // Class used to upload files to the 
        FileHost fileHost;
        // Class used to monitor creation of processes / UI
        ApplicationMonitor appMonitor;
        // Handlers to be executed when specific UI is seen
        UIHandler[] uiHandlers = new UIHandler[0];

        // Used to revert the hosting policy back
        // to its previous state
        IDisposable hostingPolicyResetter = null;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new ActivationStep
        /// </summary>
        public ActivationStep()
        {
        }

        #endregion

        #region Public Members

        // **********************************************************
        //     ClickOnce Deployment-related Members
        // **********************************************************

        /// <summary>
        /// Gets or sets whether the IE History should be cleared before
        /// activating the application. Default: False
        /// </summary>
        /// Related to ClickOnce since the main reason content runs in IE are ClickOnce related
        public bool ClearIEHistory = false;

        /// <summary>
        /// Gets or sets whether the Fusion Cache should be cleared before
        /// activating the application. default: true
        /// </summary>
        public bool ClearFusionCache = true;

        /// <summary>
        /// If true, launch ClickOnce browser apps (.xbap) in PresentationHost's "Debug" mode.  
        /// This does not commit app to the store.
        /// </summary>
        public bool PresentationHostDebugMode = false;

        /// <summary>
        /// If true, PresentationHost will load XBAP's and XAML's targeting 
        /// .NET Framework versions under v3.5 using v2.0 CLR, and those targeting
        /// frameworks greater than v4.0 using v4.0 CLR. 
        /// 
        /// If false (default), it will always launch v4.0 CLR. 
        /// </summary>
        public bool StrictHostingMode = false; 

        // **********************************************************
        //     Activation Related Members
        // **********************************************************

        /// <summary>
        /// Gets or sets the File that should be activated
        /// </summary>
        public string FileName = null;

        /// <summary>
        /// Gets or sets a string representing arguments to be appended to the end
        /// of the file name while activating.
        /// </summary>
        public string Arguments = "";

        /// <summary>
        /// Gets or sets Method to use for activation
        /// </summary>
        public ActivationMethod Method = ActivationMethod.Launch;

        /// <summary>
        /// Gets or sets the Scheme to when activating the application
        /// </summary>
        public ActivationScheme Scheme = ActivationScheme.Local;

        /// <summary>
        /// Gets or sets string representing folder to copy to.  Normally will copy to randomly named folder.
        /// </summary>
        public string UserDefinedDirectory = null;

        /// <summary>
        /// Gets or sets an array of SupportFiles that should be remotely hosted
        /// </summary>
        public SupportFile[] SupportFiles = new SupportFile[0];

        /// <summary>
        /// gets or sets an array of UIHandlers that will be registered with Activation Host
        /// </summary>
        public UIHandler[] UIHandlers
        {
            get
            {
                return uiHandlers;
            }
            set
            {
                foreach (UIHandler handler in value)
                {
                    handler.Step = this;
                }
                uiHandlers = value;
            }
        }

        /// <summary>
        /// Gets the instance of ApplicationMonitor that is monitoring the target application
        /// </summary>
        /// <value>the ApplicationMonitor that is monitoring the target application</value>
        public ApplicationMonitor Monitor
        {
            get 
            { 
                return appMonitor; 
            }
        }

        // **********************************************************
        //     Miscellaneous
        // **********************************************************

        /// <summary>
        /// If defined with the format "foo=bar", will set a value with index "foo" in the property bag to value "bar"
        /// Currently only one value allowed... can make this be a CSV list if someone actually needs it.
        /// </summary>
        public string PropertyBagValue = "";

        #endregion

        #region Step Implementation

        /// <summary>
        /// Performs the Activation step
        /// </summary>
        /// <returns>returns true if the rest of the steps should be executed, otherwise, false</returns>
        protected override bool BeginStep()
        {
            //Create ApplicationMonitor
            appMonitor = new ApplicationMonitor();

            // If defined, set a value in property bag.  Used for communication test variations to target app
            if (PropertyBagValue != "")
            {
                // Update this code to allow > 1 prop bag values being set at once
                string[] values = PropertyBagValue.Trim().Split('=');
                if (values.Length == 2)
                {
                    DictionaryStore.Current[values[0].Trim()] = values[1].Trim();
                }
                else
                {
                    throw new System.ArgumentException("Values must be a single 'foo=bar' format");
                }
            }

            if (hostingPolicyResetter != null)
            {
                hostingPolicyResetter.Dispose();
            }

            if (StrictHostingMode)
            {
                hostingPolicyResetter = HostingRuntimePolicyHelper.SetHostingRuntimePolicyValues(
                    doNotLaunchV3AppInV4Runtime: true);
            }
            else
            {
                hostingPolicyResetter = HostingRuntimePolicyHelper.SetHostingRuntimePolicyValues(
                    doNotLaunchV3AppInV4Runtime: false);
            }

            // upload files to FileHost is specified and scheme is not local
            if (Scheme != ActivationScheme.Local)
            {
                if (SupportFiles.Length > 0)
                {
                    // Create host to copy files to...
                    fileHost = new FileHost(UserDefinedDirectory, (Scheme == ActivationScheme.HttpInternetExternal));
                    // Upload each file
                    foreach (SupportFile suppFile in SupportFiles)
                    {
                        // Whether to copy foo\bar\baz.xbap to the foo\bar created on the remote machine or just flattened
                        fileHost.PreserveDirectoryStructure = suppFile.PreserveDirectoryStructure;

                        if (suppFile.IncludeDependencies && !string.IsNullOrEmpty(suppFile.TargetDirectory))
                        {
                            GlobalLog.LogEvidence("TargetDirectory with IncludeDependencies not yet implemented");
                            throw new NotImplementedException("TargetDirectory with IncludeDependencies not yet supported");
                        }
                        if (suppFile.CustomTestScratchServerPath == null)
                        {
                            if (suppFile.IncludeDependencies)
                            {
                                fileHost.UploadFileWithDependencies(suppFile.Name);
                            }
                            else
                            {
                                fileHost.UploadFile(suppFile.Name, suppFile.TargetDirectory);
                            }
                        }
                        else
                        {
                            fileHost.UploadFileNonDefaultServer(suppFile.Name, suppFile.CustomTestScratchServerPath);
                        }
                    }
                }

                // If no support files are listed, check the parent steps to see if one is a FileHostStep.
                // If this is the case, no need to upload the files as the FileHostStep has already.
                // Don't set throttle rate; this should be set in the markup for the parent's filehost.
                else
                {
                    LoaderStep parent = this.ParentStep;

                    while (parent != null)
                    {
                        if (parent.GetType() == typeof(Microsoft.Test.Loaders.Steps.FileHostStep))
                        {
                            this.fileHost = ((FileHostStep)parent).fileHost;
                            break;
                        }
                        // Failed to find it in the immediate parent: try til we hit null or the right one
                        parent = parent.ParentStep;
                    }
                }
            }

            // register UIHandlers
            foreach (UIHandler handler in UIHandlers)
            {
                if (handler.NamedRegistration != null)
                    appMonitor.RegisterUIHandler(handler, handler.NamedRegistration, handler.Notification);
                else
                    appMonitor.RegisterUIHandler(handler, handler.ProcessName, handler.WindowTitle, handler.Notification);
            }

            string param = "";

            if (FileName.StartsWith("&") && FileName.EndsWith("&"))
            {
                param = DictionaryStore.Current[FileName.Substring(1, FileName.Length - 2)];
                if (param == null)
                {
                    throw new InvalidOperationException(FileName + " is not defined in the property bag; cannot be used to launch app");
                }
            }
            else
            {
                // Allows for launching things in %program files%, which is localized.
                param = Environment.ExpandEnvironmentVariables(FileName);
            }            

            if (Scheme != ActivationScheme.Local)
            {
                FileHostUriScheme hostScheme = FileHostUriScheme.Unc;
                if (Scheme != ActivationScheme.HttpInternetExternal)
                {
                    hostScheme = (FileHostUriScheme)Enum.Parse(typeof(FileHostUriScheme), Scheme.ToString());
                }
                param = fileHost.GetUri(FileName, hostScheme).ToString();
            }

            // Clear the fusion cache by default.  Can be disabled for custom ClickOnce scenarios
            if (ClearFusionCache)
                ApplicationDeploymentHelper.CleanClickOnceCache();

            // Clear IE History but only if specified (defaults to false).  Only matters for history-based navigation
            if (ClearIEHistory)
                ApplicationDeploymentHelper.ClearIEHistory();

            // Launch the appropriate handler...
            switch (Method)
            {
                case ActivationMethod.Launch:
                    {
                        // This only works for local paths for security reasons.  
                        if (PresentationHostDebugMode)
                        {
                            param = Path.GetFullPath(param);
                            // Workaround ... for some reason on XP64 there's no guarantee that it will actually find PresHost
                            // Even though it verily is in the SysWOW64 directory.  Solution... find the right one before we try
                            string presHostPath = "presentationhost.exe";
                            if ((Environment.OSVersion.Version.Major == 5))
                            {
                                presHostPath = (Directory.GetFiles(Environment.GetEnvironmentVariable("SystemRoot"), "PresentationHost.exe", SearchOption.AllDirectories))[0];
                            }
                            appMonitor.StartProcess(presHostPath, " -debug \"" + param + "\"");
                        }
                        else
                        {
                            // Launch process with specified arguments.  If shell: specified, then start that way.
                            // If the arguments are for the URL, directly concatenate them.
                            if ((Arguments.Length > 6) && (Arguments.ToLowerInvariant().StartsWith("shell:")))
                            {
                                appMonitor.StartProcess(param, Environment.ExpandEnvironmentVariables(Arguments.Substring(6)));
                            }
                            else if ((Arguments.Length > 11) && (Arguments.ToLowerInvariant().StartsWith("currentdir:")))
                            {
                                appMonitor.StartProcess(param, Path.Combine(Environment.CurrentDirectory, Arguments.Substring(11)));
                            }
                            else
                            {
                                appMonitor.StartProcess(param + Arguments);
                            }
                        }
                        break;
                    }
                case ActivationMethod.Navigate:
                    {
                        // If local we need to fully qualify the path
                        if (Scheme == ActivationScheme.Local)
                        {
                            param = Path.GetFullPath(param);
                        }

                        // Fail to IE, since it has far more tests.  
                        string defaultBrowserExecutable = "iexplore.exe";

                        try
                        {
                            defaultBrowserExecutable = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Clients\StartMenuInternet", null, "iexplore.exe").ToString();
                        }
                        catch (Exception)
                        {
                            try
                            {
                                defaultBrowserExecutable = Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Clients\StartMenuInternet", null, "iexplore.exe").ToString();
                            }
                            catch (Exception)
                            {
                                // Do nothing, some machines have been seen in weird states where this is undefined.  Log it anyways.
                                GlobalLog.LogDebug("Unable to get StartMenuInternet key, FireFox or other non-standard browser tests may be affected.  Contact mattgal if this is the case");
                            }
                        }
                        // Handle the case where this value exists but isnt set to anything usable.  IE is far more common so fall back to it.
                        if (string.IsNullOrEmpty(defaultBrowserExecutable))
                        {
                            defaultBrowserExecutable = "iexplore.exe";
                        }

                        // start the default browser... currently just FF or IE.
                        if (defaultBrowserExecutable.ToLowerInvariant().Contains("iexplore"))
                        {
                            // Create & register IE navigation handler
                            // IE can be strange: About:NavigateIE sometimes gets a cancelled navigation 
                            // Workaround:  Be less sensitive about the window title we trigger on.
                            appMonitor.RegisterUIHandler(new NavigateIE(param + Arguments), "iexplore", "RegExp:(Internet Explorer)", UIHandlerNotification.All);
                            appMonitor.StartProcess("iexplore.exe", "about:NavigateIE");
                        }
                        else if (defaultBrowserExecutable.ToLowerInvariant().Contains("firefox"))
                        {
                            // Workaround for DevDiv bug # 119858. 
                            // FireFox HTTP --> File Navigations with "normal" System.Uris don't work
                            // Workaround: Give FireFox a non-qualified path so it can format it itself.
                            if (Scheme == ActivationScheme.Unc)
                            {
                                param = param.Replace("file:", "").Replace("/", @"\");
                            }

                            appMonitor.RegisterUIHandler(new NavigateFF(param + Arguments), "firefox", "RegExp:(Mozilla Firefox)", UIHandlerNotification.All);
                            appMonitor.StartProcess("firefox.exe");
                        }
                        else
                        {
                            throw new InvalidOperationException("Don't know how to navigate an instance of \"" + defaultBrowserExecutable + "\" browser!!! Contact mattgal with this message.");
                        }

                        break;
                    }
                // GOTO used here for fallthrough since there's only 2 lines difference.
                case ActivationMethod.EHome:
                    goto case ActivationMethod.EHomeFullScreen;
                case ActivationMethod.EHomeFullScreen:
                    {
                        // If local we need to fully qualify the path
                        if (Scheme == ActivationScheme.Local)
                            param = Path.GetFullPath(param);

                        // Get a reference to the path for the ehome exe...
                        string eHomePath = Environment.GetEnvironmentVariable("SystemRoot") + "\\ehome\\ehshell.exe";

                        // Fail hard if EHome isnt present on the system. 
                        // Need to mark testcases accurately in test DB to avoid this.
                        if (!File.Exists(eHomePath))
                        {
                            throw new InvalidOperationException("\"Ehome\" or \"EHomeFullScreen\" method selected but case was run on non-Media-Center-enabled SKU! \n Contact mattgal for more info on this issue.");
                        }

                        // Construct args with path to content to launch (MUST be a Uri)
                        string eHomeArgs = "/url:\"" + param + "\"";

                        // Tack on the argument for full screen if requested
                        if (Method == ActivationMethod.EHomeFullScreen)
                            eHomeArgs += " /directmedia:general";

                        // Start MCE... 
                        appMonitor.StartProcess(eHomePath, eHomeArgs);
                        break;
                    }
            }

            // Store the activation path into the property bag.  This way apps or child steps can directly figure out the deployment URI
            DictionaryStore.Current["ActivationStepUri"] = param;

            return true;
        }

        /// <summary>
        /// Waits for the ApplicationMonitor to Abort and Closes any remaining
        /// processes
        /// </summary>
        /// <returns>true</returns>
        protected override bool EndStep()
        {
            //Wait for the application to be done
            appMonitor.WaitForUIHandlerAbort();
            appMonitor.Close();

            // close the fileHost if one was created within ActivationStep. 
            // Don't close if the filehost is in the context of a FileHostStep
            if ((fileHost != null) && (SupportFiles.Length > 0))
                fileHost.Close();

            if (hostingPolicyResetter != null)
            {
                hostingPolicyResetter.Dispose();
                hostingPolicyResetter = null;
            }

            return true;
        }

        #endregion

    }

}