// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Test.StateManagement;

namespace Microsoft.Test.Diagnostics
{
    //This class refers to the proxy on the server that connects to the client
    internal class ExecutionServiceProxyClient : IDisposable
    {
        #region Private Data

        private string connectionName;
        private Guid connectionId;
        private DateTime startTime;

        #endregion

        #region Constructor

        public ExecutionServiceProxyClient(string clientName)
        {
            this.connectionName = clientName;
            connectionId = Guid.NewGuid();
            startTime = DateTime.Now;            
        }

        #endregion

        #region Public Members

        public string ConnectionName
        {
            get { return connectionName; }
        }

        public DateTime StartTime
        {
            get { return startTime; }
        }

        public void Dispose()
        {
            StateManager.Current.End(connectionName);
        }

        #endregion

        #region Override Members

        public override string ToString()
        {
            return "Connection Name:\t" + connectionName + Environment.NewLine + "Connection Id:\t\t" + connectionId.ToString() + Environment.NewLine + Environment.NewLine + "Start Time:\t\t" + startTime.ToString();
        }

        #endregion

        #region Internal Members

        internal string ConnectionId
        {
            get { return connectionId.ToString(); }
        }

        #endregion
    }
}
