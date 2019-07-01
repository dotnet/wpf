// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Collections;

namespace Microsoft.Test.Diagnostics
{
    /// <summary/>
	public interface IExecutionService
    {
        #region Process Members

        /// <summary/>
        int BeginCreateProcess(
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
            out string stdIn);
        /// <summary/>
        void ResumeThread(int threadId);

        /// <summary>
        /// Creates a process group and assigns the process to the group
        /// </summary>
        /// <param name="processId"></param>
        /// <returns>Process group job Handle.</returns>
        long CreateProcessGroup(int processId);

        /// <summary>
        /// Ends a process group given a valid group job handle.
        /// </summary>
        /// <param name="hGroupObject">Process group job handle</param>
        void KillProcessGroup(IntPtr hGroupObject);

        /// <summary>
        /// Adds a process to an existing process group
        /// </summary>
        /// <param name="hGroupObject">Process group job handle.</param>
        /// <param name="processId"></param>
        void AddProcessToProcessGroup(IntPtr hGroupObject, int processId);

        /// <summary>
        /// Ends processes not in the snapshot list.
        /// </summary>
        /// <param name="userSid">User Sid of processes to kill</param>
        /// <param name="snapshotProcessIds">List of processes that will be excluded from being killed</param>
        List<string> EndNewUserProcesses(string userSid, List<int> snapshotProcessIds);

        #endregion

        #region Registry Members

        /// <summary/>
        void SetRegistryValue(string keyName, string valueName, object value);
        /// <summary/>
        void DeleteRegistryValue(string keyName, string valueName);
        /// <summary/>
        void ClientConnectionSetRegistryValue(string connectionName, string connectionId, string userSid, string keyName, string valueName, object valueData);

        #endregion

        #region Third Party Browser Members

        /// <summary>
        /// API for causing browser state transitions from System context
        /// </summary>
        /// <param name="connectionName">Execution Service connection name</param>
        /// <param name="connectionId">Execution Service connection id</param>
        /// <param name="userSid">User identity info for registry editing purposes</param>
        /// <param name="userEnvironment">Environment variables for starting processes as the current user</param>
        /// <param name="whichBrowser">Third party browser to use, specified in Microsoft.Test.StateManagement.AlternateBrowserIdentifier enum</param>
        /// <param name="isInstalled">Whether specified browser should be installed or not (Not applicable for none type)</param>
        /// <param name="isDefaultBrowser">Whether specified browser should be set as the default browser in Windows</param>
        void ClientConnectionSetBrowserState(string connectionName, string connectionId, string userSid, Hashtable userEnvironment, string whichBrowser, bool isInstalled, bool isDefaultBrowser);

        #endregion

        #region Firewall Members

        /// <summary/>
        void ClientConnectionSetFirewallExceptionState(string connectionName, string connectionId, string programName, int firewallRule);

        #endregion

        #region Registration Members

        /// <summary/>
        void RegisterComServer(string connectionName, string connectionId, string filename, string clsid);
        /// <summary/>
        void UnregisterComServer(string connectionName, string connectionId, string filename, string clsid);

        #endregion

        #region Client Connection Members

        /// <summary/>
        string BeginConnection(string connectionName, bool useExistingConnection);
        /// <summary/>
        bool EndConnection(string connectionName, string connectionId);
        /// <summary/>
        void EndAllConnections();
        /// <summary/>
        string GetConnectionId(string connectionName);
        /// <summary/>
        Dictionary<string, string> GetConnectionIds();
        /// <summary/>
        string GetConnectionToString(string connectionName);

        #endregion

        #region User Session Members

        /// <summary/>
        bool UserSessionConnect(string connectionName, string connectionId, int userSessionId, string targetSessionName);
        /// <summary/>
        bool UserSessionDisconnect(string connectionName, string connectionId, int userSessionId);
        /// <summary/>
        bool UserSessionUnlockConsole();
        /// <summary/>
        bool UserSessionLoginToConsole(string username, string domain, string password);
        /// <summary/>
        int UserSessionGetConsoleSessionId();

        #endregion

        #region Marshalling Members

        /// <summary>
        /// Ensures the proxy is alive
        /// </summary>
        bool IsAlive
        {
            get;
        }

        #endregion
    }

    /// <summary/>
    public static class ExecutionService
    {
        #region Private Members

        internal readonly static string LocalPortName = "ExecutionService";
        internal readonly static string ProxyName = "ExecutionServiceProxy";

        #endregion

        #region Public Members

        /// <summary/>
        public static IExecutionService GetProxy()
        {
            // No support for remoting in .NET Core
            //try
            //{
            //    IExecutionService proxy = RemotingServices.Connect(typeof(IExecutionService), "ipc://" + LocalPortName + "/" + ProxyName) as IExecutionService;

            //    //Get the Lifetime service to ensure that there is something on the other end
            //    //ILease lease = ((MarshalByRefObject) proxy).GetLifetimeService() as ILease;
            //    bool alive = proxy.IsAlive;
            //    return proxy;
            //}
            //catch (RemotingException) {  /* service is not running */ }

            return null;

        }

        #endregion
    }

    /// <summary>
    /// State of the Firewall rule
    /// </summary>
    [Flags]
    public enum FirewallRule
    {
        /// <summary>
        /// Program or port exception rule does not exist.  The firewall is enabled.
        /// </summary>
        None = 0,
        /// <summary>
        /// Program or port exception rule exists, but it is not enabled.  The firewall is still enabled.
        /// </summary>
        Exist = 1,
        /// <summary>
        /// Program or port exception rule is enabled. This implies the port rule exists.  The firewall is disabled.
        /// </summary>
        Enabled = 3
    }

    /// <summary>
    /// Privileges allows for execution of a process
    /// </summary>
    [Flags]
    public enum ExecutionPrivileges
    {
        /// <summary>
        /// Minimum normally uses User Access, unless Elevation is required in the manifest.
        /// </summary>
        Minimum = 0,
        /// <summary>
        /// Administrator uses Elevation
        /// </summary>
        Administrator = 1,
        /// <summary>
        /// Low Privilege similar to IE in Vista
        /// </summary>
        Low = 2
    }

    /// <summary/>
    public enum ExecutionUserContext
    {
        /// <summary>
        /// First active session
        /// </summary>
        FirstActiveSessionUser,
        /// <summary>
        /// System session
        /// </summary>
        System
    }
}
