// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using StreamJsonRpc;
using System.IO.Pipes;

namespace Microsoft.Test.CrossProcess
{
    /// <summary>
    /// An implementation of a Client Dictionary type, which acts as a proxy to a remote Server Dictionary.
    /// </summary>
    internal class ClientDictionary : ADictionary
    {
        /// <summary>
        /// This pipe is used for JSon RPC connections
        /// </summary>
        private NamedPipeClientStream _clientStream;

        /// <summary>
        /// The JSON RPC object to the server
        /// </summary>
        private JsonRpc _server;

        internal ClientDictionary()
        {
            _clientStream = new NamedPipeClientStream(".", DictionaryContract.DictionaryPipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            // Connect the client to the server
            _clientStream.Connect(DictionaryContract.DictionaryPipeConnectionTimeoutMs);

            _server = JsonRpc.Attach(_clientStream);
        }

        #region IDisposable Members

        public override void Dispose()
        {
            _clientStream.Close();
        }

        #endregion

        #region IDictionary Members

        /// <summary>
        /// Note that StreamJSonRPC doesn't discover any methods with isSpecialName==true.
        /// This means we have to use wrapper functions for indexers.
        /// <see cref="ServerDictionary"/>
        /// </summary>
        public override string this[string key]
        {
            get
            {
                return AsyncHelper.RunSync<string>(() => _server.InvokeAsync<string>("GetVal", key));
            }
            set
            {
                AsyncHelper.RunSync(() => _server.InvokeAsync("SetVal", key, value));
            }
        }

        public override void Add(string key, string value)
        {
            AsyncHelper.RunSync(() => _server.InvokeAsync("Add", key, value));
        }

        public override bool Remove(string key)
        {
            return AsyncHelper.RunSync<bool>(() => _server.InvokeAsync<bool>("Remove", key));
        }

        public override void Clear()
        {
            AsyncHelper.RunSync(() => _server.InvokeAsync("Clear"));
        }

        public override bool ContainsKey(string key)
        {
            return AsyncHelper.RunSync<bool>(() => _server.InvokeAsync<bool>("ContainsKey", key));
        }

        #endregion
    }
}
