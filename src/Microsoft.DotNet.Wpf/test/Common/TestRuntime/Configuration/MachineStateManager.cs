// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Test.Diagnostics;using Microsoft.Test.StateManagement;
using Microsoft.Win32;

/******************************************************************************
 * Machine State Manager: Public Interface to ExecutionService State Management
 *****************************************************************************/

namespace Microsoft.Test.Configuration
{
    /// <summary>
    /// Provides services for Setting up Machine states that can be reverted by the test infrastructure
    /// </summary>
	public static class MachineStateManager
	{
        #region Private Data

        private const string RuntimeConnectionName = "TestRuntime";
        
        #endregion

        #region Public Members

        /// <summary>
        /// Sets a registry value, using the same syntax as Registry.SetValue
        /// </summary>
        /// <param name="keyName">name of the registry key including hive</param>
        /// <param name="valueName">name of the key value, string.empty = default</param>
        /// <param name="value">registry key value, null will delete the registry value</param>
        public static void SetRegistryValue(string keyName, string valueName, object value)
        {
            //Connect to the exectution service
            ExecutionServiceClient executionServiceClient = ExecutionServiceClient.Connect();
            if (executionServiceClient != null)
            {
                ExecutionServiceClientConnection connection = executionServiceClient.BeginConnection(RuntimeConnectionName, true);
                connection.SetRegistryValue(keyName, valueName, value);
            }
            else
            {
                //The Execution Service is not istalled so do this in process
                //NOTE: this wont work for HKEY_LM keys under LUA without ExecutionService
                if (valueName != null && !Object.Equals(value, null))
                    Registry.SetValue(keyName, valueName, value);
                else
                    RegistryState.DeleteRegistryValue(keyName, valueName);
            }
        }


        //TODO-Miguep: only xbaps?
        ///// <summary>
        ///// Sets the state of alternate browsers on the machine.  Currently only FireFox supported
        ///// </summary>
        ///// <param name="browserIdentifier">Enum representing state of 3rd party browser on the machine.  *Unknown states represent backed up folders for user-installed version of FireFox.  None means no 3rd party browser installed that can be used for test.</param>
        ///// <param name="isInstalled">Whether the browser specified by browserIdentifier is installed or not.</param>
        ///// <param name="isDefaultBrowser">Whether the browser specified by browserIdentifier is the default or IE if false.</param>
        //public static void SetBrowserState(BrowserIdentifier browserIdentifier, bool isInstalled, bool isDefaultBrowser)
        //{
        //    ExecutionServiceClient executionServiceClient = ExecutionServiceClient.Connect();
        //    if (executionServiceClient != null)
        //    {
        //        ExecutionServiceClientConnection connection = executionServiceClient.BeginConnection(RuntimeConnectionName, true);
        //        connection.SetBrowserState(browserIdentifier, isInstalled, isDefaultBrowser);
        //    }
        //    else
        //    {
        //        throw new InvalidOperationException("Can't get a connection to ExecutionServiceClient");
        //    }
        //}
        ////COM and Firewall access is now regulated as deployment based state management.

        /// <summary>
        /// Gets the session id of user console session
        /// </summary>
        /// <returns>Session id of user console session</returns>
        public static int GetConsoleSessionId()
        {
            ExecutionServiceClient executionServiceClient = ExecutionServiceClient.Connect();
            if (executionServiceClient != null)
            {
                ExecutionServiceClientConnection connection = executionServiceClient.BeginConnection(RuntimeConnectionName, true);
                return connection.UserSessionGetConsoleSessionId();
            }
            else
            {
                throw new InvalidOperationException("Can't get a connection to ExecutionServiceClient");
            }
        }

        /// <summary>
        /// Connects to user session specified by sessionId
        /// </summary>
        /// <param name="sessionId">Session Id to disconnect from</param>
        public static void ConnectToUserSession(int sessionId)
        {
            ExecutionServiceClient executionServiceClient = ExecutionServiceClient.Connect();
            if (executionServiceClient != null)
            {
                ExecutionServiceClientConnection connection = executionServiceClient.BeginConnection(RuntimeConnectionName, true);
                connection.UserSessionConnect(sessionId, "Console");
            }
            else
            {
                throw new InvalidOperationException("Can't get a connection to ExecutionServiceClient");
            }
        }

        /// <summary>
        /// Disconnects from user session specified by sessionId
        /// </summary>
        /// <param name="sessionId">Session Id to connect to</param>
        public static void DisconnectFromUserSession(int sessionId)
        {
            ExecutionServiceClient executionServiceClient = ExecutionServiceClient.Connect();
            if (executionServiceClient != null)
            {
                ExecutionServiceClientConnection connection = executionServiceClient.BeginConnection(RuntimeConnectionName, true);
                connection.UserSessionDisconnect(sessionId);
            }
            else
            {
                throw new InvalidOperationException("Can't get a connection to ExecutionServiceClient");
            }
        }

        #endregion
	}
}
