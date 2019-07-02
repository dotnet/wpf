// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    #region usings
        using System;
        using System.IO;
        using System.Drawing;
        using System.Reflection;
        using System.Text.RegularExpressions;
        using Microsoft.Test.Loaders;
    #endregion usings

    /// <summary>
    /// Summary description for XamlLauncherReflect.
    /// </summary>
    public class XamlLauncherReflect
    {
        #region Constants & readonly values
            /// <summary>
            /// The default assembly used to launch a xaml (assembly must implement IXamlLauncher)
            /// </summary>
            static public readonly string DEFAULT_XAMLLAUNCHER = "ClientTestRuntime:Microsoft.Test.Loaders.NavigationLauncherNoChrome";
            /// <summary>
            /// The default size of the Xaml Launcher
            /// </summary>
            static public readonly Size DEFAULT_SIZE = new Size(800, 480); // same as Robo
            /// <summary>
            /// The default location of the Xaml Launcher
            /// </summary>
            static public readonly Point DEFAULT_LOCATION = new Point(0, 0);
        #endregion Constants & readonly values

        #region Properties
            #region Private Properties
                private IXamlLauncher _xamlLauncherInstance = null;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Get the Captured Image (to be called after "Start" occured)
                /// </summary>
                /// <value></value>
                public Bitmap CapturedImage
                {
                    get
                    {
                        // Retrieve the Region of Interest
                        Bitmap capturedImage = _xamlLauncherInstance.CapturedImage;

                        // check Image and return it
                        if (capturedImage == null)
                        {
                            // user closed the Navigation Window.
                            throw new RenderingVerificationException("Image not found (captured failed?)");
                        }

                        return capturedImage;
                    }
                }
                /// <summary>
                /// Get /set the ClientSize
                /// </summary>
                /// <value></value>
                public Size ClientSize
                { 
                    get 
                    {
                        // Set the Size of the Client window
                        return _xamlLauncherInstance.ClientSize;
                    }
                    set 
                    {
                        _xamlLauncherInstance.ClientSize = value;
                    }
                }
                /// <summary>
                /// Get/set the window location (screen coordinate)
                /// </summary>
                /// <value></value>
                public Point WindowLocation
                { 
                    get 
                    {
                        // Set the Size of the Client window
                        return _xamlLauncherInstance.WindowLocation;
                    }
                    set 
                    {
                        _xamlLauncherInstance.WindowLocation = value;
                    }
                }
                /// <summary>
                /// Get/set the xaml to be launched
                /// </summary>
                /// <value></value>
                public string XamlToLoad
                {
                    get 
                    {
                        return _xamlLauncherInstance.XamlToLoad;
                    }
                    set 
                    {
                        _xamlLauncherInstance.XamlToLoad = value;
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Create a XamlLauncherReflect class with the default xamlLauncher assembly
            /// </summary>
            public XamlLauncherReflect() : this(DEFAULT_XAMLLAUNCHER)
            {
            }
            /// <summary>
            /// Create a XamlLauncherReflect class with a specified xamlLauncher assembly
            /// </summary>
            /// <param name="loadingAssembly"></param>
            public XamlLauncherReflect(string loadingAssembly)
            {
                Assembly asm = null;

                Match match = Regex.Match(loadingAssembly, ".+:.+");
                if (match.Success == false)
                {
                    throw new XamlLauncherReflectException("The loadingAssembly argument provided has an incorrect syntax (Assembly:Namespace)...");
                }
                string[] assemblyInfo = loadingAssembly.Split(':');
                if (assemblyInfo.Length != 2)
                {
                    throw new XamlLauncherReflectException("The xamlLauncher argument has an incorrect syntax (multiple delimitor ':' found )...");
                }

                string asmName = assemblyInfo[0];

                try
                {
                    // Load assembly locally
                    // [dennisch] Changed from Assembly.LoadWithPartialName to Assembly.Load
                    asm = Assembly.Load(asmName);
                }
                catch (Exception e)
                {
                    throw new XamlLauncherReflectException("Unable to load Assembly (see inner exception)", e);
                }
                _xamlLauncherInstance = asm.CreateInstance(assemblyInfo[1], true) as IXamlLauncher;
                if (_xamlLauncherInstance == null)
                {
                    throw new XamlLauncherReflectException("Failed to create an instance of this assembly (are you running on LH ?)");
                }
                
            }
        #endregion Constructors

        #region Methods
            #region Public Methods
                /// <summary>
                /// Launcher the XamlLauncher and wait for rendering
                /// </summary>
                public void Start()
                {
                    if (XamlToLoad == string.Empty || XamlToLoad == null)
                    { 
                        throw new XamlLauncherReflectException("XamlToLoad must be set first");
                    }
                    if (ClientSize == Size.Empty)
                    { 
                        ClientSize = DEFAULT_SIZE;
                    }
                    if (WindowLocation == Point.Empty)
                    { 
                        WindowLocation = DEFAULT_LOCATION;
                    }

                    // Launch the Xaml
                    _xamlLauncherInstance.Start();
                }
                /// <summary>
                /// Close the Application
                /// </summary>
                public void Stop()
                {
                    // Launch the Xaml
                    _xamlLauncherInstance.Stop();
                }
            #endregion Public Methods
        #endregion Methods
    }
}
