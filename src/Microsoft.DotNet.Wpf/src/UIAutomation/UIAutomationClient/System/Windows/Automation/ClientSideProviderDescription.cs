// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Structure containing information about a proxy

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;

namespace System.Windows.Automation 
{
    /// <summary>
    /// Enum used to indicate results of requesting a property
    /// </summary>
    [Flags]
#if (INTERNAL_COMPILE)
    internal enum ClientSideProviderMatchIndicator
#else
    public enum ClientSideProviderMatchIndicator
#endif
    {
        /// <summary>
        /// Default settings will be used for this proxy: classname will be matched
        /// using full string comparison, and a match against the underlying class name
        /// (for superclassed USER32 and Common Controls) will be allowed.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// Use substring comparison for comparing classnames
        /// </summary>
        AllowSubstringMatch = 0x01,

        /// <summary>
        /// The real class name or base class will not be checked
        /// </summary>
        DisallowBaseClassNameMatch = 0x02,
    }

    /// <summary>
    /// Implemented by HWND handlers, called by UIAutomation framework to request a proxy for specified window and item.
    /// Should return a proxy if supported, or null if not supported.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal delegate IRawElementProviderSimple ClientSideProviderFactoryCallback( IntPtr hwnd, int idChild, int idObject );
#else
    public delegate IRawElementProviderSimple ClientSideProviderFactoryCallback(IntPtr hwnd, int idChild, int idObject);
#endif

    /// <summary>
    /// Structure containing information about a proxy
    /// </summary>
#if (INTERNAL_COMPILE)
    internal struct ClientSideProviderDescription
#else
    public struct ClientSideProviderDescription
#endif
    {
        private string                _className;
        private string                _imageName;
        private ClientSideProviderMatchIndicator _flags;
        private ClientSideProviderFactoryCallback _proxyFactoryCallback;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="className">Window classname that the proxy is for. If null is used, the proxy will be called for all
        /// windows if no other proxy has been found for that window.</param>
        /// <param name="clientSideProviderFactoryCallback">Delegate that PAW will call to request the creation of a proxy</param>
        public ClientSideProviderDescription(ClientSideProviderFactoryCallback clientSideProviderFactoryCallback, string className)
        {
            // Null and Empty string mean different things here.
#pragma warning suppress 6507
            if (className != null)
                _className = className.ToLower( System.Globalization.CultureInfo.InvariantCulture );
            else
                _className = null;
            _flags = 0;
            _imageName = null;
            _proxyFactoryCallback = clientSideProviderFactoryCallback;
        }

        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="className">Window classname that the proxy is for. If null is used, the proxy will be called for all
        /// windows if no other proxy has been found for that window.</param>
        /// <param name="clientSideProviderFactoryCallback">Delegate that PAW will call to request the creation of a proxy</param>
        /// <param name="flags">Enum ProxyMatchFlags 
        /// otherwise the parameter classname can be contained in the window class name</param>
        /// <param name="imageName">Name of the executable for the process where this window resides.  For example outllib.dll or calc.exe</param>
        public ClientSideProviderDescription(ClientSideProviderFactoryCallback clientSideProviderFactoryCallback, string className, string imageName, ClientSideProviderMatchIndicator flags)
        {
            // Null and Empty string mean different things here
#pragma warning suppress 6507
            if (className != null)
                _className = className.ToLower( System.Globalization.CultureInfo.InvariantCulture );
            else
                _className = null;
                
            _flags = flags;

            // Null and Empty string mean different things here
#pragma warning suppress 6507
            if (imageName != null)
                _imageName = imageName.ToLower( System.Globalization.CultureInfo.InvariantCulture );
            else
                _imageName = null;

            _proxyFactoryCallback = clientSideProviderFactoryCallback;
        }


        /// <summary>
        /// Window classname that the proxy is for.
        /// </summary>
        public string ClassName
        {
            get
            {
                return _className;
            }
        }

        /// <summary>
        /// Returns the ClientSideProviderMatchIndicator flags that specify options for how
        /// this description should be used.
        /// </summary>
        public ClientSideProviderMatchIndicator Flags
        {
            get
            {
                return _flags;
            }
        }
        
        /// <summary>
        /// Name of the executable for the process where this window resides.  For example Winword.exe.
        /// </summary>
        public string ImageName
        {
            get
            {
                return _imageName;
            }
        }

        /// <summary>
        /// Delegate that UIAutomation will call to request the creation of a proxy
        /// </summary>
        public ClientSideProviderFactoryCallback ClientSideProviderFactoryCallback
        {
            get
            {
                return _proxyFactoryCallback;
            }
        }
    }
}
