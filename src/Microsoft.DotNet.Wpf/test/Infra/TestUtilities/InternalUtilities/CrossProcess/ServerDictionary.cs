// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Test.CrossProcess
{
    /// <summary>
    /// Implements a dictionary on Server Side - This is the definitive store of the data.
    /// Honors the ADictionary contract and exposes itself to external access through the ClientDictionaryProxy.
    /// </summary>
    internal class ServerDictionary : ADictionary
    {
        /// <summary>
        /// The underlying dictionary
        /// </summary>
        private Dictionary<string, string> table;

        /// <summary>
        /// Used to cancel the connection loop
        /// </summary>
        private CancellationTokenSource _connectionCancellation = new CancellationTokenSource();

        internal ServerDictionary()
        {
            table = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Task nowait = HandleConnectionsAsync();
        }

        /// <summary>
        /// Loops for waiting connections until the dictionary is disposed
        /// </summary>
        private async Task HandleConnectionsAsync()
        {
            while(!_connectionCancellation.IsCancellationRequested)
            {
                var serverPipe = new NamedPipeServerStream(DictionaryContract.DictionaryPipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await serverPipe.WaitForConnectionAsync(_connectionCancellation.Token);
                Task nowait = HandleClientAsync(serverPipe);
            }
        }

        /// <summary>
        /// Handles a client connection for JSON RPC until the connection is closed.
        /// </summary>
        /// <param name="serverPipe">The named pipe to use for communication</param>
        private async Task HandleClientAsync(NamedPipeServerStream serverPipe)
        {
            var jsonrpc = JsonRpc.Attach(serverPipe, this);
            await jsonrpc.Completion;
        }

        #region IDisposable Members

        public override void Dispose()
        {
            // Cancel the connection loop
            _connectionCancellation.Cancel();
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// Wraps indexers so we can use them with StreamJSonRPC
        /// </summary>
        public void SetVal(string key, string value)
        {
            table[key] = value;
        }

        /// <summary>
        /// Wraps indexers so we can use them with StreamJSonRPC
        /// </summary>
        public string GetVal(string key)
        {
            return table[key];
        }

        public override string this[string key]
        {
            get
            {
                // NOTE: This is PropertyBag behavior - standard dictionary
                // implementation throws on getting values for which a key
                // does not exist. Would like to fix this down the road.
                if (key != null && table.ContainsKey(key))
                {
                    return table[key];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                table[key] = value;
            }
        }

        #region IDictionaryService Members

        public override bool ContainsKey(string key)
        {
            return table.ContainsKey(key);
        }

        public override void Add(string key, string value)
        {
            table.Add(key, value);
        }

        public override bool Remove(string key)
        {
            return table.Remove(key);
        }

        public override void Clear()
        {
            table.Clear();
        }

        #endregion
    }
}
