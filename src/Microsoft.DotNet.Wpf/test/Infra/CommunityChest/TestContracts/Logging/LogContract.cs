// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using StreamJsonRpc;
using System;
using System.Collections;
using System.IO;
using System.IO.Pipes;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Test.Logging
{
    /// <summary>
    /// Defines connection contract for logging. Hides details of underlying implmeentaiton
    /// </summary>
    public static class LogContract
    {
        public const string LoggingPipeName = "QualityVaultLoggingPipe";
        public const int LoggingPipeConnectionTimeoutMs = 1000;

        /// <summary>
        /// This pipe is used for JSon RPC connections
        /// </summary>
        private static readonly NamedPipeClientStream _clientStream = new NamedPipeClientStream(".", LoggingPipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        /// <summary>
        /// Connects a client to the Quality Vault Logging Server.
        /// </summary>        
        public static LoggingClient ConnectClient()
        {
            if (!_clientStream.IsConnected)
            {
                // Connect the client to the server
                _clientStream.Connect(LoggingPipeConnectionTimeoutMs);
            }

            // Initializes a JSonRPC wrapper to the named pipe
            var logServer = JsonRpc.Attach(_clientStream);

            // this method originally retrieved the LoggingClient instantiated by QualityVault via remoting.
            return new LoggingClient(logServer);
        }
        
        /// <summary>
        /// Disconnects client from QV Logging Server.
        /// </summary>        
        public static void DisconnectClient(LoggingClient client)
        {
            if (_clientStream.IsConnected)
            {
                _clientStream.Close();
            }
        }

        /// <summary>
        /// Registers a QualityVault Logging server to listen for incoming Logger calls.
        /// </summary>
        public static CancellationTokenSource RegisterServer(ILogger listener)
        {
            CancellationTokenSource connectionCancellation = new CancellationTokenSource();
            Task nowait = HandleConnectionsAsync(connectionCancellation, listener);

            return connectionCancellation;
        }
        
        /// <summary>
        /// Unregisters QualityVault Logging Server.
        /// </summary>
        /// <param name="channel"></param>
        public static void UnRegisterServer(CancellationTokenSource connectionCancellation)
        {
            connectionCancellation.Cancel();
        }

        /// <summary>
        /// Loops for waiting connections until the dictionary is disposed
        /// </summary>
        private static async Task HandleConnectionsAsync(CancellationTokenSource connectionCancellation, ILogger listener)
        {
            while (!connectionCancellation.IsCancellationRequested)
            {
                var serverPipe = new NamedPipeServerStream(LoggingPipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await serverPipe.WaitForConnectionAsync(connectionCancellation.Token);
                Task nowait = HandleClientAsync(serverPipe, listener);
            }
        }

        /// <summary>
        /// Handles a client connection for JSON RPC until the connection is closed.
        /// </summary>
        /// <param name="serverPipe">The named pipe to use for communication</param>
        private static async Task HandleClientAsync(NamedPipeServerStream serverPipe, ILogger listener)
        {
            var jsonrpc = JsonRpc.Attach(serverPipe, listener);
            await jsonrpc.Completion;
        }
    }
}
