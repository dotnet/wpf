// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Collections.Specialized;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using Microsoft.Win32;
using System.Reflection;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Collections.Generic;
using Microsoft.Test.StateManagement;

namespace Microsoft.Test.Diagnostics
{
    internal class ExecutionServiceProxy : MarshalByRefObject, IExecutionService
    {
        #region Private data

        private Dictionary<string, ExecutionServiceProxyClient> connections = new Dictionary<string, ExecutionServiceProxyClient>(StringComparer.InvariantCultureIgnoreCase);

        #endregion       

        #region Process Members

        public int BeginCreateProcess(
            string fileName,
            string arguments,
            string workingDirectory,
            bool createNoWindow,
            bool redirectStandardError,
            bool redirectStandardInput,
            bool redirectStandardOutput,
            Encoding standardErrorEncoding,
            Encoding standardOutputEncoding,
            StringDictionary environmentVariables,
            bool runAsUser,
            Nullable<int> sessionId,
            bool lowIntegrity,
            bool elevated,
            int redirectionBufferSize,
            out int threadId,
            out string stdOut,
            out string stdErr,
            out string stdIn)
        {
            //HACK: ProcessStartInfo is not marked serializable so we have to pass the data in pieces
            //create the process start info
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = fileName;
            startInfo.Arguments = arguments;
            startInfo.WorkingDirectory = workingDirectory;
            startInfo.CreateNoWindow = createNoWindow;
            startInfo.RedirectStandardError = redirectStandardError;
            startInfo.RedirectStandardInput = redirectStandardInput;
            startInfo.RedirectStandardOutput = redirectStandardOutput;
            startInfo.StandardErrorEncoding = standardErrorEncoding;
            startInfo.StandardOutputEncoding = standardOutputEncoding;

            startInfo.EnvironmentVariables.Clear();
            foreach (string key in environmentVariables.Keys)
                startInfo.EnvironmentVariables[key] = environmentVariables[key];

            return ProcessHelper.BeginCreateProcess(startInfo, runAsUser, sessionId, lowIntegrity, elevated, out threadId, out stdOut, out stdErr, out stdIn, redirectionBufferSize);
        }

        public void ResumeThread(int threadId)
        {
            ProcessHelper.ResumeThread(threadId);
        }

        public long CreateProcessGroup(int processId)
        {
            return ProcessGroup.CreateProcessGroup(processId);
        }

        public void KillProcessGroup(IntPtr hGroupObject)
        {
            ProcessGroup.KillProcessGroup(hGroupObject);
        }

        public void AddProcessToProcessGroup(IntPtr hGroupObject, int processId)
        {
            ProcessGroup.AddProcessToProcessGroup(hGroupObject, processId);
        }

        public List<string> EndNewUserProcesses(string userSid, List<int> processExclusionList)
        {
            return ProcessHelper.EndNewUserProcesses(userSid, processExclusionList);
        }

        #endregion

        #region Registry Members

        public void SetRegistryValue(string keyName, string valueName, object value)
        {
            Registry.SetValue(keyName, valueName, value);
        }

        public void DeleteRegistryValue(string keyName, string valueName)
        {
            DeleteRegistryValueInternal(keyName, valueName);
        }

        public void ClientConnectionSetRegistryValue(string connectionName, string connectionId, string userSid, string keyName, string valueName, object valueData)
        {
            EnsureValidClientConnection(connectionName, connectionId);

            RegistryState registryState = new RegistryState(userSid, keyName, valueName);
            RegistryStateValue registryStateValue = new RegistryStateValue(valueData);
            StateManager.Current.Transition(connectionName, registryState, registryStateValue);
        }

        #endregion

        #region Third Party Browser Members

        public void ClientConnectionSetBrowserState(string connectionName, string connectionId, string userSid, Hashtable currentEnvironment, string browserIdentifierString, bool isInstalled, bool isDefaultBrowser)
        {
            EnsureValidClientConnection(connectionName, connectionId);

            // CaseInsensitiveComparer needed since we can't predict the casing of a user's environment variables across all platforms.
            currentEnvironment = new Hashtable(currentEnvironment, StringComparer.InvariantCultureIgnoreCase);

            BrowserIdentifier browserIdentifier = (BrowserIdentifier)Enum.Parse(typeof(BrowserIdentifier), browserIdentifierString, true);
            BrowserState browserState = new BrowserState(userSid, currentEnvironment);
            BrowserStateValue browserValue = new BrowserStateValue(browserIdentifier, isInstalled, isDefaultBrowser);
            StateManager.Current.Transition(connectionName, browserState, browserValue);
        }

        #endregion

        #region Firewall Members

        public void ClientConnectionSetFirewallExceptionState(string connectionName, string connectionId, string programName, int firewallRule)
        {
            EnsureValidClientConnection(connectionName, connectionId);

            FirewallExceptionState firewallExceptionState = new FirewallExceptionState(programName);
            FirewallRule firewallExceptionStateValue = (FirewallRule)firewallRule;
            StateManager.Current.Transition(connectionName, firewallExceptionState, firewallExceptionStateValue);
        }

        #endregion

        #region Registration Members

        public void RegisterComServer(string connectionName, string connectionId, string filename, string clsid)
        {
            EnsureValidClientConnection(connectionName, connectionId);

            ComServerState comServerState = new ComServerState(filename, clsid);
            Boolean register = true;
            StateManager.Current.Transition(connectionName, comServerState, register);
        }

        public void UnregisterComServer(string connectionName, string connectionId, string filename, string clsid)
        {
            EnsureValidClientConnection(connectionName, connectionId);

            ComServerState comServerState = new ComServerState(filename, clsid);
            Boolean register = false;
            StateManager.Current.Transition(connectionName, comServerState, register);
        }

        #endregion

        #region Color Profile Members

        public void SetColorProfile(string connectionName, string connectionId, string profileName)
        {
            EnsureValidClientConnection(connectionName, connectionId);

            ColorProfileState colorProfileState = new ColorProfileState();
            StateManager.Current.Transition(connectionName, colorProfileState, profileName);
        }

        #endregion

        #region Client Connection Members

        public string BeginConnection(string connectionName, bool useExistingConnection)
        {
            if (String.IsNullOrEmpty(connectionName))
                throw new ArgumentNullException("connectionName");

            if (connections.ContainsKey(connectionName))
            {
                if (useExistingConnection)
                    return connections[connectionName].ConnectionId;
                else
                    connections[connectionName].Dispose();
            }
            connections[connectionName] = new ExecutionServiceProxyClient(connectionName);

            return connections[connectionName].ConnectionId;
        }

        public bool EndConnection(string connectionName, string connectionId)
        {
            //TODO: We need to add support for notifying the infrastructure if the runtime failed to end the connection
            //For the moment, we use Dispose() which returns void, so we don't know if we restored the desktop correctly
            bool connectionEnded = false;

            if (String.IsNullOrEmpty(connectionName))
                throw new ArgumentNullException("connectionName");

            if (connections.ContainsKey(connectionName) && connections[connectionName].ConnectionId == connectionId)
            {
                connections[connectionName].Dispose();
                connections.Remove(connectionName);
                connectionEnded = true;
            }

            return connectionEnded;
        }

        public void EndAllConnections()
        {
            string[] connectionNames = new string[connections.Keys.Count];
            connections.Keys.CopyTo(connectionNames, 0);

            foreach (string connectionName in connectionNames)
                EndConnection(connectionName, connections[connectionName].ConnectionId);
        }

        public string GetConnectionId(string connectionName)
        {
            if (connections.ContainsKey(connectionName))
                return connections[connectionName].ConnectionId;

            return null;
        }

        public Dictionary<string, string> GetConnectionIds()
        {
            Dictionary<string, string> connectionIds = new Dictionary<string, string>(connections.Keys.Count);

            foreach (KeyValuePair<string, ExecutionServiceProxyClient> connection in connections)
            {
                if (connection.Value != null)
                    connectionIds[connection.Key] = connection.Value.ConnectionId;
            }

            return connectionIds;
        }

        public string GetConnectionToString(string connectionName)
        {
            if (connections.ContainsKey(connectionName))
                return connections[connectionName].ToString();

            return null;
        }

        #endregion

        #region User Session Methods

        public bool UserSessionConnect(string connectionName, string connectionId, int userSessionId, string targetSessionName)
        {
            EnsureValidClientConnection(connectionName, connectionId);

            UserSessionState userSessionState = new UserSessionState(userSessionId);
            UserSession userSession = new UserSession(UserSessionType.Active);
            UserInfo userInfo = new UserInfo();
            if (UserSessionHelper.GetUserSessionUserInfo(userSessionId, out userInfo))
            {
                userSession.UserLoginInfo = userInfo;
            }
            else
            {
                // For machines without auto logon, password retrieval fails. We would like to
                // throw an exception here to fail the test case.
                throw new InvalidOperationException("Auto logon is required to be enabled for this task to succeed");
            }

            return StateManager.Current.Transition(connectionName, userSessionState, userSession, UserSessionAction.Connect);            
        }

        public bool UserSessionDisconnect(string connectionName, string connectionId, int userSessionId)
        {
            EnsureValidClientConnection(connectionName, connectionId);

            UserSessionState userSessionState = new UserSessionState(userSessionId);
            UserSession userSession = new UserSession(UserSessionType.Disconnected);
            UserInfo userInfo = new UserInfo();
            if (UserSessionHelper.GetUserSessionUserInfo(userSessionId, out userInfo))
            {
                userSession.UserLoginInfo = userInfo;
            }
            else
            {
                // For machines without auto logon, password retrieval fails. We would like to
                // throw an exception here to fail the test case.
                throw new InvalidOperationException("Auto logon is required to be enabled for this task to succeed");
            }

            return StateManager.Current.Transition(connectionName, userSessionState, userSession, UserSessionAction.Disconnect);            
        }

        public bool UserSessionUnlockConsole()
        {
            return UserSessionHelper.TryUserSessionActivateConsole(UserSessionHelper.GetUserConsoleSession());
        }

        public bool UserSessionLoginToConsole(string username, string domain, string password)
        {
            return UserSessionHelper.TryUserSessionLogonToConsole(username, domain, password);
        }

        public int UserSessionGetConsoleSessionId()
        {
            return UserSessionHelper.GetUserConsoleSession();
        }

        #endregion

        #region Marshalling Members

        public bool IsAlive
        {
            get { return GetLifetimeService() != null; }
        }

        #endregion

        #region Private Members

        private void EnsureValidClientConnection(string connectionName, string connectionId)
        {
            if (String.IsNullOrEmpty(connectionName))
                throw new ArgumentNullException("connectionName");

            if (!connections.ContainsKey(connectionName))
                throw new InvalidOperationException("A connection with name " + connectionName + " cannot be found");

            if (connections[connectionName].ConnectionId != connectionId)
                throw new InvalidOperationException("The connection id provided for " + connectionName + " is no longer valid.");

        }

        #endregion

        #region Static Members

        public static IChannel PublishLocalProxy(ExecutionServiceProxy proxy)
        {
            //We want the object to be available for as long as possible so we choose one year
            LifetimeServices.LeaseTime = TimeSpan.FromDays(365);
            RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;

            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full; //need full deserialization

            //create a server channel and make the object available remotely
            Hashtable channelSettings = new Hashtable();
            channelSettings["portName"] = ExecutionService.LocalPortName;

            //Grant access to everyone (need to get the name from the well known SID to support localized builds)
            SecurityIdentifier everyoneSid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            NTAccount everyoneAccount = (NTAccount)everyoneSid.Translate(typeof(NTAccount));
            channelSettings["authorizedGroup"] = everyoneAccount.Value;
            
            //Because use use ExecutionService to update itself we have to disallow exclusive access
            //to the port, otherwise updating ExecutionService fails.
            channelSettings["exclusiveAddressUse"] = false;

            // Workaround for Remoting breaking change.  This custom security descriptor is needed 
            // to allow Medium IL processes to use IPC to talk to elevated UAC processes.
            DiscretionaryAcl dacl = new DiscretionaryAcl(false, false, 1);

            // This is the well-known sid for network security ID
            string networkSidSddlForm = @"S-1-5-2";

            // This is the well-known sid for all users on this machine
            string everyoneSidSddlForm = @"S-1-1-0";  

            // Deny all access to NetworkSid
            dacl.AddAccess(AccessControlType.Deny, new SecurityIdentifier(networkSidSddlForm), -1, InheritanceFlags.None, PropagationFlags.None);

            // Add access to the current user creating the pipe
            dacl.AddAccess(AccessControlType.Allow, WindowsIdentity.GetCurrent().User, -1, InheritanceFlags.None, PropagationFlags.None);

            // Add access to the all users on the machine
            dacl.AddAccess(AccessControlType.Allow, new SecurityIdentifier(everyoneSidSddlForm), -1, InheritanceFlags.None, PropagationFlags.None);

            // Initialize and return the CommonSecurityDescriptor
            CommonSecurityDescriptor allowMediumILSecurityDescriptor = new CommonSecurityDescriptor(false, false, ControlFlags.OwnerDefaulted | ControlFlags.GroupDefaulted | ControlFlags.DiscretionaryAclPresent | ControlFlags.SystemAclPresent, null, null, null, dacl);

            IpcServerChannel publishedChannel = new IpcServerChannel(channelSettings, provider, allowMediumILSecurityDescriptor);
            ChannelServices.RegisterChannel(publishedChannel, false);

            RemotingServices.Marshal(proxy, ExecutionService.ProxyName, typeof(IExecutionService));
            
            return publishedChannel;
        }

        internal static void DeleteRegistryValueInternal(string keyName, string valueName)
        {
            //HACK: Registry.SetValue does not interpet null as deleting the value so we have to do it ourselves
            //      we dont want to have to duplicate the definitition for the
            object[] args = new object[] { keyName, null };
            RegistryKey baseKey = (RegistryKey)typeof(Registry).InvokeMember("GetBaseKeyFromKeyName", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Static, null, null, args);

            using (RegistryKey regKey = baseKey.OpenSubKey(keyName.Substring(keyName.IndexOf('\\') + 1), true))
            {
                regKey.DeleteValue(valueName, false);
            }
        }

        #endregion
    }
}
