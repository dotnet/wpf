// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;  
using MS.Internal;
using MS.Utility;
using MS.Win32;
using MS.Internal.PresentationCore;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Internal;
using System.Security;

namespace MS.Internal.FontCache
{
    [FriendAccessAllowed]
    internal class FontCacheConfig
    {
        private enum ProtocolType { Single, Dual, ALPC };

        private void DefaultConfig()
        {
#if ALPC_ENABLED
            //By default, use ALPC if running on Longhorn, or single-port if running on XP
            int majorVersion = Environment.OSVersion.Version.Major;
            if (majorVersion >= 6) // ALPC is supported on Longhorn and later.
                _protocolType = ProtocolType.ALPC;
            else
                _protocolType = ProtocolType.Single;
#else
            // We use only single port LPC protocol for WPF V1.
            // The code above will be enabled in a future version.
            _protocolType = ProtocolType.Single;
#endif
            
            _useConnThread = false;
            _restartServiceOnError = true;
            _serverPrintRequests = false;
            _clientHangOnConn = false;
            _serverHangOnConn = false;
            _serverHangOnRequest = false;
            _firstConnectTimeout = 100;
            _secondConnectTimeout = 100;
            _serverDebugStartup = false;
            _serverForceCreate = false;
            _serverIsLocalService = true;
            _shutdownTimeout = 1000;
            _serverStartTimeout = 2000;

            _clientProtocol = GetProtocol(_protocolType,_useConnThread);
        }

#if FONTCACHEDEBUG

        private ProtocolType GetProtocolTypeFromString(string str)
        {
            if (str.Equals("single", StringComparison.CurrentCultureIgnoreCase))
                return ProtocolType.Single;
            if (str.Equals("dual", StringComparison.CurrentCultureIgnoreCase))
                return ProtocolType.Dual;
            if (str.Equals("alpc", StringComparison.CurrentCultureIgnoreCase))
                return ProtocolType.ALPC;
            throw new ArgumentException();
        }

        private bool Parse <T> (string key, StreamReader sr, ref T field)
        {
            bool res = false;
            string line = ReadLine(sr);
            if (line == null)
            {
                Debug.WriteLine(String.Format("Unexpected end-of-file - using default value ({0})",field));
                return false;
            }
            key = string.Concat(key, " = ");
            object objField = field;
            if (line.StartsWith(key))
            {
                string value = line.Substring(key.Length);
                value = value.Trim();

                if(typeof(T).Equals(typeof(bool)))
                    res = ParseBoolLine(key, value, ref objField);
                else if(typeof(T).Equals(typeof(int)))
                    res = ParseIntLine(key, value, ref objField);
                else if (typeof(T).Equals(typeof(string)))
                    res = ParseStringLine(key, value, ref objField);
                else
                {
                    Debug.WriteLine(string.Format("Invalid type passed to FontCacheConfig.Parse() - using default value ({0})", field));
                    return res;
                }
                field = (T)objField;
                if (res)
                    Debug.WriteLine(String.Format("{0} {1}", key, field));
                else
                    Debug.WriteLine(String.Format("Parse error: \"{0}\" - using default value ({1})", line, field));
            }
            else
            {
                Debug.WriteLine(String.Format("Invalid field: \"{0}\" - using default value", line));
            }
            return res;
        }

        //Reads the "config" file and determines which protocol to use.
        //This allows switching of protocols without recompiling coredll and rerunning makemsi each time.
        private void Config()
        {
            //do default config, in case we have errors
            DefaultConfig();

            bool fileComplete = false;
            try
            {
                //@"E:\Documents and Settings\LocalService\Local Settings\Application Data\fontcacheconfig.txt"
                string varname = "fontcacheconfig";
                string path = Environment.GetEnvironmentVariable(varname);

                Debug.WriteLine("Config path:");
                Debug.WriteLine(path);
                using (StreamReader sr = new StreamReader(path))
                {
                    string protocolName = null;
                    Parse("Protocol", sr, ref protocolName);
                    _protocolType = GetProtocolTypeFromString(protocolName);

                    Parse("ConnThread", sr, ref _useConnThread);

                    Parse("RestartService",sr, ref _restartServiceOnError);
                    Parse("ServerPrintRequests", sr, ref _serverPrintRequests);

                    Parse("ClientHangOnConn", sr, ref _clientHangOnConn);
                    Parse("ServerHangOnConn", sr, ref _serverHangOnConn);
                    Parse("ServerHangOnRequest", sr, ref _serverHangOnRequest);
                    Parse("FirstConnectTimeout", sr, ref _firstConnectTimeout);
                    Parse("SecondConnectTimeout", sr, ref _secondConnectTimeout);
                    Parse("ShutdownTimeout", sr, ref _shutdownTimeout);

                    Parse("ServerDebugStartup", sr, ref _serverDebugStartup);
                    Parse("ServerIsLocalService", sr, ref _serverIsLocalService);
                    Parse("ServerForceCreate", sr, ref _serverForceCreate);

                    _clientProtocol = GetProtocol(_protocolType,_useConnThread);

                    fileComplete = true;
                }
            }
            catch (IOException e)
            {
                Debug.WriteLine(e);//problem reading config file
            }
            catch (UnauthorizedAccessException e)
            {
                Debug.WriteLine(e);//don't have access to config file
            }
            catch (ArgumentException e)
            {
                Debug.WriteLine(e);//invalid data in config file or invalid data in fontcacheconfig environment variable
            }
            finally
            {
                if (!fileComplete)
                {
                    Debug.WriteLine("Error processing config file");
                }
                else
                {
                    Debug.WriteLine("Config file processed successfully");
                }
                Debug.Flush();
            }
        }

        private static string ReadLine(TextReader reader)
        {
            for (; ; )
            {
                string line = reader.ReadLine();
                if (line == null)
                {
                    Debug.WriteLine("Unexpected end-of-file - using default value");
                    return null;
                }
                //If empty line, just read the next line
                line = line.Trim();
                if (line.Length != 0)
                    return line;
            }

        }

        private static bool ParseBoolLine(string key, string value, ref object result)
        {
            value = value.Trim();
            if(value.Equals("true"))
            {
                result = true;
                return true;
            }
            else if(value.Equals("false"))
            {
                result = false;
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool ParseStringLine(string key, string value, ref object result)
        {
            result = value;
            return true;
        }

        private static bool ParseIntLine(string key,string value,ref object result)
        {
            int tempres;
            if(int.TryParse(value,out tempres))
            {
                result = tempres;
                return true;
            }
            else
            {
                return false;
            }
       }

       private FontCacheConfig(bool runConfig)
       {
           DefaultConfig();
           if(runConfig)
               Config();
       }

       private FontCacheConfig()
            : this(true)
       {
       }
#else
       private FontCacheConfig()       
       {
           DefaultConfig();
       }
#endif

       private Protocol GetProtocol(ProtocolType protocolType,bool useConnThread)
        {
#if ALPC_ENABLED
            if (protocolType == ProtocolType.Dual)
                return new DualLPCProtocol(
                BuildInfo.FontCachePortName,    //name of server port
                ClientPortBaseName,             //Base name of client ports
                MakeServerPortSid(),            //expected server sid
                256,                            //maximum data size on client port
                256,                            //maximum message size on client port
                MakeClientPortSD(),             //Allow only local service to connect to client port
                useConnThread                   //Whether to use separate thread for connection
                );
            else if (protocolType == ProtocolType.Single)
                return new LPCProtocol(BuildInfo.FontCachePortName, MakeServerPortSid());
            else if (protocolType == ProtocolType.ALPC)
                return new ALPCProtocol(BuildInfo.FontCachePortName,//port name
                    null,                                           //use default security descriptor for client
                    //connection object
                    256,                                            //maximum message length
                    MakeServerPortSid()                             //Expected server sid
                    );   
            else
            {
                Debug.WriteLine("Invalid protocol type");
                return null;
            }
#else
            return new LPCProtocol(BuildInfo.FontCachePortName, MakeServerPortSid());
#endif
        }

        internal Protocol ClientProtocol { get { return _clientProtocol; } }
        internal bool RestartServiceOnError { get { return _restartServiceOnError; } }
#if ALPC_ENABLED
        internal bool IsDualPort { get { return _clientProtocol is DualLPCProtocol; } }
        internal bool IsAlpc { get { return _clientProtocol is ALPCProtocol; } }
#endif
        internal bool ServerPrintRequests { get { return _serverPrintRequests; } }
        internal string ClientPortBaseName { get { return _clientPortBaseName; } }
        internal int FirstConnectTimeout { get { return _firstConnectTimeout; } }
        internal int SecondConnectTimeout { get { return _secondConnectTimeout; } }
        internal uint ServerStartTimeout { get { return _serverStartTimeout; } }
        internal int ShutdownTimeout { get { return _shutdownTimeout; } }
        internal bool ClientHangOnConn { get { return _clientHangOnConn;} }
        internal bool ServerHangOnConn { get { return _serverHangOnConn;} }
        internal bool ServerHangOnRequest { get { return _serverHangOnRequest;} }
        internal bool ServerDebugStartup { get { return _serverDebugStartup;} }
        internal bool ServerIsLocalService { get { return _serverIsLocalService; } }
        internal bool ServerForceCreate { get { return _serverForceCreate; } }
        internal static bool IsServer { get { return _isServer; } set { _isServer = value; } }
        internal static FontCacheConfig Current { get { if (_config == null) _config = new FontCacheConfig(); return _config; } }

        //Returns the expected SID for the client port
        internal Sid MakeServerPortSid()
        {
#if DEBUG
            if (ServerIsLocalService)
#endif
                return new LocalServiceSid();
#if DEBUG
            else
                return null;
#endif
        }

        internal SecurityDescriptor MakeClientPortSD()
        {
            //Allow only local service to connect
            int errorCode = 0;
            ExplicitAccessList dacl = new ExplicitAccessList(1);
            Sid serverSid;
#if DEBUG
            if (ServerIsLocalService)
#endif
                serverSid = new LocalServiceSid();
#if DEBUG
            else
                serverSid = new WellKnownSid(WellKnownSidType.WinWorldSid);
#endif
            dacl.AddAccessAllowedAce(0,//ACE index 0
                        AccessPermissions.PortConnect,
                        AceFlags.NoInheritance,
                        serverSid,
                        out errorCode);

            if (errorCode != 0)
                return null;//error creating local service sid

            return new SecurityDescriptor(dacl);

        }

        //Protocol to use to communicate with the server
        private Protocol _clientProtocol;

        //Whether to try to restart the service if an error occurs (false only for debugging)
        private bool _restartServiceOnError;

        //Whether the server should print every data request it receives
        private bool _serverPrintRequests = false;

        //Base name of client ports
        private string _clientPortBaseName = "\\BaseNamedObjects\\FontCacheClientPort";

        //Timeouts
        private int _firstConnectTimeout;
        private int _secondConnectTimeout;
        private uint _serverStartTimeout;

        //Whether client should hang upon connection
        private bool _clientHangOnConn;

        //Whether server should hang upon connection
        private bool _serverHangOnConn;

        //Whether server should hang upon font cache name request
        private bool _serverHangOnRequest;

        //Whether server should break into debugger upon startup
        private bool _serverDebugStartup;

        //Name of protocol to use
        private ProtocolType  _protocolType;

        //Whether to use a separate thread for connection
        private bool _useConnThread;

        //Whether to force the server to construct the element every time it receives a miss report
        private bool _serverForceCreate;

        //If true, the server will only run as an official service, launched through the service control manager,
        //and clients will demand that the service is running under the LocalService account before connecting.
        //
        //If false, the server will run as an ordinary program in the context of whichever user ran it and the clients
        //will connect anyway.  This allows the debugger to attach to the font cache service in longhorn without
        //running into access denied errors.
        private bool _serverIsLocalService;

        //True if running as server, false if running as client
        private static bool _isServer = false;

        //Current configuration.  This is starts out null and is initialized the first time
        //the Config property is invoked (each time thereafter, _config is reused).  
        //This prevents unhandled exceptions in Config() from causing a TypeInitializationException, 
        //making debugging more difficult.
        private static FontCacheConfig _config = null;

        //Timeout for server shutdown
        private static int _shutdownTimeout;

    }
   

    /// <summary>
    /// Manages the client side of the inter-process communications between the client
    /// and the server regarding the font cache.
    /// </summary>
    internal class IPCCacheManager
    {
        //----------------------
        // Internal Constructors
        //------------------------
        #region Internal Constructors

        public IPCCacheManager(FontCacheConfig fc)
        {
            _fc = fc;
            _protocol = _fc.ClientProtocol;
        }

        #endregion Internal Constructors

        //-------------------------------------
        // Internal Methods
        //--------------------------------------
        #region Internal Methods

        /// <summary>
        /// Retrieves name of server cache.  If an error occurs, we return null and save the error code in 
        /// errorCode.
        /// If we are not currently connected to the server, we automatically establish the connection
        /// </summary>
        /// <param name="timeout">maximum amount of time to wait, in milliseconds</param>
        /// <param name="errorCode">On output, contains protocol-specific error code (0 if successful)</param>
        /// <returns>name of server cache, null if timeout or error</returns>
        internal string GetServerSectionName(int timeout,out int errorCode)
        {
            byte[] input = new byte[sizeof(Int32)];
            Marshal.WriteInt32(input, 0, FontCacheConstants.GetCacheNameMessage);
            byte[] output = new byte[MaxCacheNameSize];

            if (_conn == null)
            {
                _conn = _protocol.TryConnectToServer(timeout, out errorCode);
                if (_conn == null)
                    return null;
            }
            errorCode = 0;
            _protocol.TrySendRequest(_conn, input, 0, input.Length, output, 0, output.Length, timeout,out errorCode);

            if(errorCode == 0)
                return Util2.ByteArrayToString(output);
            else
                return null;
        }

        internal void CloseConnection()
        {
            if (_conn != null)
            {
                int errorCode = 0;
                //ignore return value
                _protocol.TryCloseInstance(_conn,out errorCode);
                _conn = null;
            }
        }

        internal unsafe void SendMissReport(IFontCacheElement e)
        {
            //If we are not connected to the server, there's no one to send the miss report to, so don't.
            if (_conn == null)
                return;

            byte[] request = new byte[Math.Min(MaxMissReportSize, _conn.GetMaxRequestBytes())];

            //If the connection cannot support a long enough request to hold the message type and element type,
            //we can't send the miss report.
            if (request.Length < 2 * sizeof(int))
                return;
            
            fixed (byte* ptr = &request[0])
            {
                //Store the message type
                ((int*)ptr)[0] = FontCacheConstants.SendMissReportMessage;

                //Store the element type
                ((int*)ptr)[1] = e.Type;

                //Store the key information
                CheckedPointer key = new CheckedPointer(ptr, request.Length);
                key += 2 * sizeof(int);
                try
                {
                    int realSize = 0;
                    e.StoreKey(key, out realSize);
                    Debug.Assert(realSize >= 0,"realSize is negative");
                    // realSize will be zero for non-shareable elements like in-memory fonts
                    if (realSize != 0)
                    {
                        realSize += 2 * sizeof(int);//adjust for message header and miss report header
                        Debug.Assert(realSize <= request.Length,"realSize too large");

                        //Send the request as a datagram (ignore error code)
                        int errorCode = 0;
                        _protocol.TrySendRequest(_conn, request, 0, realSize, null, 0, 0, 0,out errorCode);
                    }

                }
                catch (ArgumentOutOfRangeException)
                {
                    // the key didn't fit into the buffer, just don't send the miss report
                    return;
                }
            }
        }

        internal int ServerNotFoundErrorCode { get { return _protocol.ServerNotFoundErrorCode; } }


        #endregion Internal Methods

        //----------------------------------------
        // Internal Properties
        //----------------------------------------
#region Internal Properties
        internal bool IsConnected { get { return (_conn != null); } }
        internal int MaxRequestBytes { get { return _conn.GetMaxRequestBytes(); } }
#endregion Internal Properties

        //----------------------------------------
        // Private Fields
        //----------------------------------------
        
        //The protocol to use for connecting to the server and the current connection instance
        private static Protocol _protocol = null;
        private static ConnectionInstance _conn = null;

        //Maximum size of a miss report
        private const int MaxMissReportSize = 256;
        private const int MaxCacheNameSize = 256;

        //Font cache configuration
        private FontCacheConfig _fc;
    }
}
