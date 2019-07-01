// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Security.Principal;

namespace Microsoft.Test.Diagnostics
{
    internal class ExecutionServiceClient
    {
        #region Private Data

        private static IExecutionService proxy;
        
        #endregion

        #region Constructors

        private ExecutionServiceClient()
        {
        }

        #endregion

        #region Process Members

        public Process StartProcess(ProcessStartInfo startInfo, ExecutionUserContext context, ExecutionPrivileges privileges, bool fullProcessAccess)
        {
            return StartProcess(startInfo, context, privileges, ProcessHelper.DefaultRedirectionBufferSize, fullProcessAccess);
        }

        public Process StartProcess(ProcessStartInfo startInfo, ExecutionUserContext context, ExecutionPrivileges privileges, int redirectionBufferSize, bool fullProcessAccess)
        {
            IntPtr hGroupObject = IntPtr.Zero;
            return StartProcessInternal(startInfo, context, privileges, redirectionBufferSize, fullProcessAccess);
        }

        public IntPtr StartProcessGroup(ProcessStartInfo startInfo, ExecutionUserContext context, ExecutionPrivileges privileges, bool fullProcessAccess, out Process process)
        {
            process = StartProcessInternal(startInfo, context, privileges, ProcessHelper.DefaultRedirectionBufferSize, fullProcessAccess);
            return (IntPtr) CreateProcessGroup(process.Id);
        }

        private Process StartProcessInternal(ProcessStartInfo startInfo, ExecutionUserContext context, ExecutionPrivileges privileges, int redirectionBufferSize, bool fullProcessAccess)
        {
            if (startInfo.UseShellExecute == true)
                throw new ArgumentException("You cannot use shell execute to execute applications under LUA", "startInfo");
            if (startInfo.UserName.Length > 0)
                throw new ArgumentException("You cannot use user credencials to execute applications under LUA", "startInfo");

            bool runAsUser = context == ExecutionUserContext.FirstActiveSessionUser;
            bool elevated = (privileges & ExecutionPrivileges.Administrator) == ExecutionPrivileges.Administrator;
            bool lowIntegrity = (privileges & ExecutionPrivileges.Low) == ExecutionPrivileges.Low;

            //TODO: Pass only Process environment variables to the service so they get inherited but user and system don't

            //Because the process hosting the proxy has a different Current Directory then
            //this process, we fully qualify the path if it is relative to the current directory. 
            string filename = ProcessHelper.GetExeFullPath(startInfo.FileName);

            int threadId;
            string stdOut, stdErr, stdIn;
            int pid = proxy.BeginCreateProcess(
                    filename,
                    startInfo.Arguments,
                    startInfo.WorkingDirectory,
                    startInfo.CreateNoWindow,
                    startInfo.RedirectStandardError,
                    startInfo.RedirectStandardInput,
                    startInfo.RedirectStandardOutput,
                    startInfo.StandardErrorEncoding,
                    startInfo.StandardOutputEncoding,
                    startInfo.EnvironmentVariables,
                    runAsUser,
                    null,
                    lowIntegrity,
                    elevated,
                    redirectionBufferSize,
                    out threadId,
                    out stdOut,
                    out stdErr,
                    out stdIn);

            //Get the process object
            return ProcessHelper.EndCreateProcess(startInfo, pid, threadId, stdOut, stdErr, stdIn, redirectionBufferSize, fullProcessAccess);
        }

        public void ResumeThread(int threadId)
        {
            proxy.ResumeThread(threadId);
        }

        //
        public IntPtr CreateProcessGroup(int processId)
        {
            return (IntPtr) proxy.CreateProcessGroup(processId);
        }

        public void EndProcessGroup(IntPtr hGroupObject)
        {
            proxy.KillProcessGroup(hGroupObject);
        }

        public void AddProcessToProcessGroup(IntPtr hGroupObject, int processId)
        {
            proxy.AddProcessToProcessGroup(hGroupObject, processId);
        }

        public List<string> EndNewUserProcesses(List<int> snapshotProcessIds)
        {
            return proxy.EndNewUserProcesses(WindowsIdentity.GetCurrent().User.Value, snapshotProcessIds);
        }

        #endregion

        #region Client Connection Members

        public ExecutionServiceClientConnection BeginConnection(string connectionName, bool useExistingConnection)
        {
            return new ExecutionServiceClientConnection(connectionName, proxy.BeginConnection(connectionName, useExistingConnection));
        }

        public bool EndConnection(ExecutionServiceClientConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");

            return proxy.EndConnection(connection.Name, connection.Id);
        }

        public void EndAllConnections()
        {
            proxy.EndAllConnections();
        }

        public ExecutionServiceClientConnection GetConnection(string connectionName)
        {
            string connectionId = proxy.GetConnectionId(connectionName);

            if (!String.IsNullOrEmpty(connectionId))
                return new ExecutionServiceClientConnection(connectionName, connectionId);

            return null;
        }

        public List<ExecutionServiceClientConnection> GetConnections()
        {
            Dictionary<string, string> connectionIds = proxy.GetConnectionIds();
            List<ExecutionServiceClientConnection> connections = new List<ExecutionServiceClientConnection>(connectionIds.Keys.Count);

            foreach (KeyValuePair<string, string> connection in connectionIds)
                connections.Add(new ExecutionServiceClientConnection(connection.Key, connection.Value));

            return connections;
        }

        #endregion

        #region Static Members

        public static ExecutionServiceClient Connect()
        {
            if (proxy == null)
                proxy = ExecutionService.GetProxy();
            
            if (proxy != null)
                return new ExecutionServiceClient();
            
            return null;
        }

        internal static IExecutionService Proxy
        {
            get 
            {
                if (proxy == null)
                    throw new NullReferenceException("Proxy");

                return proxy;
            }
        }

        #endregion
    }
}
