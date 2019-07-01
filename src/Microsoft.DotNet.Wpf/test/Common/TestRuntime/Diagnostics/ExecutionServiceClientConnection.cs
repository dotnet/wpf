// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Principal;
using System.Collections;

namespace Microsoft.Test.Diagnostics
{
    internal class ExecutionServiceClientConnection : IDisposable
    {
        #region Private Data

        private string name;
        private string id;

        #endregion

        #region Constructor

        internal ExecutionServiceClientConnection(string name, string id)
        {
            this.name = name;
            this.id = id;
        }

        #endregion

        #region ClientConnection Members

        public string Name
        {
            get { return name; }
        }

        public string Id
        {
            get { return id; }
        }

        public void Dispose()
        {
            ExecutionServiceClient.Proxy.EndConnection(name, id);
        }

        public override string ToString()
        {
            //TODO: This should go away once we clarify how to proxy the session object,
            //since the object on the proxy has different data than the one on the client
            return ExecutionServiceClient.Proxy.GetConnectionToString(name);
        }

        #endregion

        #region Registry Members

        public void SetRegistryValue(string keyName, string valueName, object valueData)
        {
            ExecutionServiceClient.Proxy.ClientConnectionSetRegistryValue(name, id, WindowsIdentity.GetCurrent().User.Value, keyName, valueName, valueData);
        }

        #endregion

        #region Third Party Browser Members

        public void SetBrowserState(BrowserIdentifier browserIdentifier, bool isInstalled, bool isDefaultBrowser)
        {
#if TESTBUILD_CLR20
            Hashtable currentUserEnvironment = (Hashtable)Environment.GetEnvironmentVariables();
#endif
#if TESTBUILD_CLR40
            // Workaround for Dev10 bugs 441762 or 392532, depending on who you ask...
            // As described in 441762, Environment.GetEnvironmentVariables() now returns Dictionary<string,string>
            // This was supposedly fixed in the CLR branch on 9/29 but is not in NetFX yet, so use this simple workaround
            // (Doesnt ever need to be backed out)
            object environmentContents = Environment.GetEnvironmentVariables();
            Hashtable currentUserEnvironment = null;
            if (environmentContents.GetType() == typeof(Hashtable))
            {
                currentUserEnvironment = (Hashtable)environmentContents;
            }
            else
            {
                currentUserEnvironment = new Hashtable((IDictionary)environmentContents);
            }
#endif
            ExecutionServiceClient.Proxy.ClientConnectionSetBrowserState(name, id, WindowsIdentity.GetCurrent().User.Value, currentUserEnvironment, ((string)browserIdentifier.ToString()), isInstalled, isDefaultBrowser);
        }

        #endregion

        #region Registration Members

        public void RegisterComServer(string filename, string clsid)
        {
            ExecutionServiceClient.Proxy.RegisterComServer(name, id, filename, clsid);
        }

        public void UnregisterComServer(string filename, string clsid)
        {
            ExecutionServiceClient.Proxy.UnregisterComServer(name, id, filename, clsid);
        }

        #endregion

        #region User Session Members

        public bool UserSessionConnect(int sessionId, string targetSessionName)
        {
            return ExecutionServiceClient.Proxy.UserSessionConnect(name, id, sessionId, targetSessionName);
        }

        public bool UserSessionDisconnect(int sessionId)
        {
            return ExecutionServiceClient.Proxy.UserSessionDisconnect(name, id, sessionId);
        }

        public bool UserSessionUnlockConsole()
        {
            return ExecutionServiceClient.Proxy.UserSessionUnlockConsole();
        }

        public int UserSessionGetConsoleSessionId()
        {
            return ExecutionServiceClient.Proxy.UserSessionGetConsoleSessionId();
        }

        #endregion

        #region Firewall Members

        public void DisableFirewallForExecutingApplication()
        {
            ExecutionServiceClient.Proxy.ClientConnectionSetFirewallExceptionState(name, id, FirewallHelper.GetCurrentProcessExePath(), (int)FirewallRule.Enabled);
        }

        public void EnableFirewallForExecutingApplication()
        {
            ExecutionServiceClient.Proxy.ClientConnectionSetFirewallExceptionState(name, id, FirewallHelper.GetCurrentProcessExePath(), (int)FirewallRule.None);
        }

        #endregion

    }
}
